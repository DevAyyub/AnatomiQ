using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AnatomiQ.Data;
using UnityEditor;
using UnityEngine;

namespace AnatomiQ.Editor
{
    /// <summary>
    /// CORE-008 build-time import tool (Data Schemas §8.7). Reads the authored JSON under
    /// <c>Content/organs</c> and <c>Content/diseases</c>, runs the same <see cref="ContentImporter"/>
    /// and <see cref="ContentValidator"/> the runtime uses, writes one validated ScriptableObject
    /// <c>.asset</c> per item, and rebuilds the <see cref="ContentManifest"/> the DataLayer loads.
    ///
    /// JSON is the source of truth; the <c>.asset</c> files are generated artifacts. Invalid items are
    /// skipped and reported (never written), so authors get immediate feedback and the runtime never
    /// sees malformed content. Existing assets are updated in place via
    /// <see cref="EditorUtility.CopySerialized"/> so their GUIDs (and therefore manifest references)
    /// stay stable across re-imports.
    /// </summary>
    public static class ContentImportTool
    {
        private const string ContentRoot = "Assets/_AnatomiQ/Content";
        private const string OrgansSource = ContentRoot + "/organs";
        private const string DiseasesSource = ContentRoot + "/diseases";

        private const string OutputRoot = "Assets/_AnatomiQ/ScriptableObjects";
        private const string OrgansOut = OutputRoot + "/Organs";
        private const string DiseasesOut = OutputRoot + "/Diseases";
        private const string ManifestPath = OutputRoot + "/ContentManifest.asset";

        [MenuItem("AnatomiQ/Content/Import JSON")]
        public static void Import()
        {
            var report = new StringBuilder();
            var organSkips = 0;
            var diseaseSkips = 0;

            // Folders must exist before batching; CreateFolder can be deferred during StartAssetEditing.
            EnsureFolder(OrgansOut);
            EnsureFolder(DiseasesOut);

            AssetDatabase.StartAssetEditing();
            try
            {
                // ----- Organs: parse + intra-validate + uniqueness, then write -----
                var organAssets = new Dictionary<string, OrganAsset>();
                foreach (var (organ, sourceFile) in LoadAll<OrganAsset>(OrgansSource, ContentImporter.ParseOrgans))
                {
                    var result = ContentValidator.ValidateOrgan(organ);
                    if (!result.IsValid)
                    {
                        organSkips++;
                        report.AppendLine($"  SKIP organ ({sourceFile}): {string.Join("; ", result.Errors)}");
                        Object.DestroyImmediate(organ);
                        continue;
                    }

                    if (organAssets.ContainsKey(organ.OrganId))
                    {
                        organSkips++;
                        report.AppendLine($"  SKIP organ '{organ.OrganId}' ({sourceFile}): duplicate organId");
                        Object.DestroyImmediate(organ);
                        continue;
                    }

                    organAssets[organ.OrganId] = WriteAsset(organ, $"{OrgansOut}/{organ.OrganId}.asset");
                }

                // Cross-reference pass: report unresolved edges/parents as warnings (assets are kept).
                var organIds = new HashSet<string>(organAssets.Keys);
                foreach (var organ in organAssets.Values)
                {
                    foreach (var warning in ContentValidator.ValidateOrgan(organ, organIds).Warnings)
                    {
                        report.AppendLine($"  WARN {warning}");
                    }
                }

                // ----- Diseases: validate against the organ set + uniqueness, then write -----
                var diseaseAssets = new Dictionary<string, DiseaseAsset>();
                foreach (var (disease, sourceFile) in LoadAll<DiseaseAsset>(DiseasesSource, ContentImporter.ParseDiseases))
                {
                    var result = ContentValidator.ValidateDisease(disease, organIds, diseaseAssets.Keys);
                    if (!result.IsValid)
                    {
                        diseaseSkips++;
                        report.AppendLine($"  SKIP disease ({sourceFile}): {string.Join("; ", result.Errors)}");
                        Object.DestroyImmediate(disease);
                        continue;
                    }

                    foreach (var warning in result.Warnings)
                    {
                        report.AppendLine($"  WARN disease '{disease.DiseaseId}': {warning}");
                    }

                    diseaseAssets[disease.DiseaseId] = WriteAsset(disease, $"{DiseasesOut}/{disease.DiseaseId}.asset");
                }

                // ----- Manifest -----
                var manifest = LoadOrCreate<ContentManifest>(ManifestPath);
                manifest.Organs = organAssets.Values.OrderBy(o => o.OrganId).ToList();
                manifest.Diseases = diseaseAssets.Values.OrderBy(d => d.DiseaseId).ToList();
                EditorUtility.SetDirty(manifest);

                report.Insert(0,
                    $"[ContentImport] Imported {organAssets.Count} organs ({organSkips} skipped), " +
                    $"{diseaseAssets.Count} diseases ({diseaseSkips} skipped).\n");
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            var summary = report.ToString().TrimEnd();
            if (organSkips + diseaseSkips > 0)
            {
                Debug.LogWarning(summary);
            }
            else
            {
                Debug.Log(summary);
            }

            EditorUtility.DisplayDialog("AnatomiQ Content Import", summary, "OK");
        }

        /// <summary>Parses every *.json file in a folder via the given collection parser.</summary>
        private static IEnumerable<(T asset, string file)> LoadAll<T>(
            string folder, ParseCollection<T> parse) where T : ScriptableObject
        {
            if (!Directory.Exists(folder))
            {
                Debug.LogWarning($"[ContentImport] Source folder not found: {folder}");
                yield break;
            }

            foreach (var path in Directory.GetFiles(folder, "*.json").OrderBy(p => p))
            {
                var fileName = Path.GetFileName(path);
                var parsed = parse(File.ReadAllText(path), out var errors);
                foreach (var error in errors)
                {
                    Debug.LogError($"[ContentImport] Parse error in {fileName}: {error}");
                }

                foreach (var asset in parsed)
                {
                    yield return (asset, fileName);
                }
            }
        }

        private delegate List<T> ParseCollection<T>(string json, out List<string> errors) where T : ScriptableObject;

        /// <summary>Writes a freshly parsed asset to <paramref name="path"/>, preserving an existing GUID.</summary>
        private static T WriteAsset<T>(T parsed, string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(parsed, existing);
                EditorUtility.SetDirty(existing);
                Object.DestroyImmediate(parsed); // the parsed instance was a temporary
                return existing;
            }

            AssetDatabase.CreateAsset(parsed, path); // takes ownership of parsed
            return parsed;
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                return existing;
            }

            var created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, path);
            return created;
        }

        private static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
            {
                return;
            }

            var parent = Path.GetDirectoryName(assetPath).Replace('\\', '/');
            var leaf = Path.GetFileName(assetPath);
            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
