using System.Collections.Generic;
using System.IO;
using System.Linq;
using AnatomiQ.Data;
using NUnit.Framework;
using UnityEngine;

namespace AnatomiQ.Tests.EditMode
{
    /// <summary>
    /// CORE-008 chunk 4 gate. Validates the real Phase 1 content under <c>Content/organs</c> and
    /// <c>Content/diseases</c> — the 24 canonical organ nodes and the three cascades (T2D, HTN, CKD) —
    /// by running the same importer + validator path the runtime uses. Asserts the expected counts and
    /// zero validation errors, so medical-content edits that break a reference, drop a narration, or
    /// mis-key a stage are caught immediately.
    ///
    /// This reads the authored JSON directly (not the generated .assets), so it gates the source of
    /// truth and does not depend on the Editor import having run.
    /// </summary>
    public sealed class Phase1ContentTests
    {
        private const string ContentDir = "_AnatomiQ/Content";
        private const int ExpectedOrganCount = 24;
        private const int ExpectedDiseaseCount = 3;

        private readonly List<OrganAsset> _organs = new List<OrganAsset>();
        private readonly List<DiseaseAsset> _diseases = new List<DiseaseAsset>();

        private static string Dir(string sub) => Path.Combine(Application.dataPath, ContentDir, sub);

        [OneTimeSetUp]
        public void LoadContent()
        {
            foreach (var path in EnumerateJson("organs"))
            {
                _organs.AddRange(ContentImporter.ParseOrgans(File.ReadAllText(path), out _));
            }

            foreach (var path in EnumerateJson("diseases"))
            {
                _diseases.AddRange(ContentImporter.ParseDiseases(File.ReadAllText(path), out _));
            }
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            foreach (var organ in _organs)
            {
                if (organ != null)
                {
                    Object.DestroyImmediate(organ);
                }
            }

            foreach (var disease in _diseases)
            {
                if (disease != null)
                {
                    Object.DestroyImmediate(disease);
                }
            }
        }

        private static IEnumerable<string> EnumerateJson(string sub)
        {
            var folder = Dir(sub);
            Assert.IsTrue(Directory.Exists(folder),
                $"Content folder missing: Assets/{ContentDir}/{sub}. Add the Phase 1 JSON files.");
            return Directory.GetFiles(folder, "*.json").OrderBy(p => p);
        }

        [Test]
        public void Organs_LoadExpectedCount()
        {
            Assert.AreEqual(ExpectedOrganCount, _organs.Count);
        }

        [Test]
        public void Organs_AllPassIntraValidation()
        {
            var failures = new List<string>();
            foreach (var organ in _organs)
            {
                var result = ContentValidator.ValidateOrgan(organ);
                if (!result.IsValid)
                {
                    failures.Add($"{organ.OrganId}: {string.Join("; ", result.Errors)}");
                }
            }

            Assert.IsEmpty(failures, "Organ validation errors:\n" + string.Join("\n", failures));
        }

        [Test]
        public void OrganGraph_AllEdgesAndParentsResolve()
        {
            var ids = new HashSet<string>(_organs.Select(o => o.OrganId));
            var warnings = new List<string>();
            foreach (var organ in _organs)
            {
                warnings.AddRange(ContentValidator.ValidateOrgan(organ, ids).Warnings);
            }

            Assert.IsEmpty(warnings,
                "Unresolved organ edges/parents (every Phase 1 reference should resolve):\n"
                + string.Join("\n", warnings));
        }

        [Test]
        public void Diseases_LoadExpectedCount()
        {
            Assert.AreEqual(ExpectedDiseaseCount, _diseases.Count);
        }

        [Test]
        public void Diseases_AllValidateAgainstOrganSet()
        {
            var organIds = new HashSet<string>(_organs.Select(o => o.OrganId));
            var acceptedDiseaseIds = new HashSet<string>();
            var failures = new List<string>();

            foreach (var disease in _diseases)
            {
                var result = ContentValidator.ValidateDisease(disease, organIds, acceptedDiseaseIds);
                if (!result.IsValid)
                {
                    failures.Add($"{disease.DiseaseId}: {string.Join("; ", result.Errors)}");
                }
                else
                {
                    acceptedDiseaseIds.Add(disease.DiseaseId);
                }
            }

            Assert.IsEmpty(failures, "Disease validation errors:\n" + string.Join("\n", failures));
            Assert.AreEqual(ExpectedDiseaseCount, acceptedDiseaseIds.Count, "All diseases should be unique and valid.");
        }
    }
}
