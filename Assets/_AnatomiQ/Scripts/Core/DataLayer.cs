using System.Collections.Generic;
using AnatomiQ.Data;
using UnityEngine;

namespace AnatomiQ.Core
{
    /// <summary>
    /// CORE-008 — Data Layer service. The single runtime source of truth for static content
    /// (organs, diseases). It loads the validated assets listed in a <see cref="ContentManifest"/>,
    /// builds id → asset lookups, and self-registers with the <see cref="ServiceRegistry"/> so other
    /// systems reach it as <see cref="IDataLayer"/> — never via singletons or FindObjectOfType.
    ///
    /// Self-registration mirrors the FallbackManager pattern: a <c>[SerializeField] ServiceRegistry</c>
    /// is assigned in the inspector, and <see cref="Awake"/> registers through the dedicated
    /// <see cref="ServiceRegistry.RegisterDataLayer"/> entry point (IDataLayer does not extend
    /// IService, to avoid a Data→Core assembly cycle). Execution order sits after the FallbackManager
    /// (-1000) and before the AppBootstrap verify (-500).
    ///
    /// Fallback (CORE-008): a missing or malformed item is logged and skipped; the rest still load.
    /// Validation runs again here at load (not only at import time) so a deleted asset or a hand-edited
    /// inspector value degrades gracefully instead of crashing. Unresolved connection edges are logged
    /// as warnings and left inert — consumers resolve edge targets via <see cref="TryGetOrgan"/>, which
    /// returns false for a missing node, so the asset is never mutated.
    /// </summary>
    [DefaultExecutionOrder(-900)] // After FallbackManager (-1000), before AppBootstrap verify (-500).
    public sealed class DataLayer : MonoBehaviour, IDataLayer
    {
        [Header("Service wiring")]
        [Tooltip("The single ServiceRegistry asset. Assign in the inspector.")]
        [SerializeField] private ServiceRegistry _services;

        [Header("Content")]
        [Tooltip("The content manifest listing imported organ and disease assets. Assign in the inspector.")]
        [SerializeField] private ContentManifest _manifest;

        private readonly Dictionary<string, OrganAsset> _organs = new Dictionary<string, OrganAsset>();
        private readonly Dictionary<string, DiseaseAsset> _diseases = new Dictionary<string, DiseaseAsset>();

        /// <inheritdoc />
        public bool IsLoaded { get; private set; }

        /// <inheritdoc />
        public IReadOnlyCollection<OrganAsset> Organs => _organs.Values;

        /// <inheritdoc />
        public IReadOnlyCollection<DiseaseAsset> Diseases => _diseases.Values;

        private void Awake()
        {
            if (_services == null)
            {
                Debug.LogError("[DataLayer] ServiceRegistry not assigned in the inspector.");
                return;
            }

            _services.RegisterDataLayer(this);
            Load();
        }

        /// <summary>
        /// Loads and validates all content from the manifest, rebuilding the lookups. Safe to call
        /// more than once. Never throws; every per-item failure is logged and skipped.
        /// </summary>
        private void Load()
        {
            _organs.Clear();
            _diseases.Clear();
            IsLoaded = false;

            if (_manifest == null)
            {
                // The 3D viewer still works with no content; this is degraded, not a crash.
                Debug.LogError("[DataLayer] ContentManifest not assigned; loaded 0 organs, 0 diseases.");
                IsLoaded = true;
                return;
            }

            LoadOrgans();
            LoadDiseases();

            IsLoaded = true;
            Debug.Log($"[DataLayer] Loaded {_organs.Count} organs, {_diseases.Count} diseases.");
        }

        private void LoadOrgans()
        {
            // Pass 1: intra-asset validation + id uniqueness. Build the accepted-id set.
            foreach (var organ in _manifest.Organs)
            {
                if (organ == null)
                {
                    Debug.LogError("[DataLayer] Manifest contains a null organ reference; skipped.");
                    continue;
                }

                var result = ContentValidator.ValidateOrgan(organ);
                if (!result.IsValid)
                {
                    Debug.LogError($"[DataLayer] Skipping organ '{organ.OrganId}': {string.Join("; ", result.Errors)}");
                    continue;
                }

                if (_organs.ContainsKey(organ.OrganId))
                {
                    Debug.LogError(
                        $"[DataLayer] Duplicate organId '{organ.OrganId}'; keeping the first, skipping this one.");
                    continue;
                }

                _organs.Add(organ.OrganId, organ);
            }

            // Pass 2: cross-reference parent and connection targets against the accepted ids.
            // These are warnings only (the node stays; unresolved edges are left inert).
            var ids = new HashSet<string>(_organs.Keys);
            foreach (var organ in _organs.Values)
            {
                var result = ContentValidator.ValidateOrgan(organ, ids);
                foreach (var warning in result.Warnings)
                {
                    Debug.LogWarning($"[DataLayer] {warning}");
                }
            }
        }

        private void LoadDiseases()
        {
            var organIds = new HashSet<string>(_organs.Keys);
            foreach (var disease in _manifest.Diseases)
            {
                if (disease == null)
                {
                    Debug.LogError("[DataLayer] Manifest contains a null disease reference; skipped.");
                    continue;
                }

                // _diseases.Keys is the set of already-accepted ids, used for the uniqueness rule.
                var result = ContentValidator.ValidateDisease(disease, organIds, _diseases.Keys);
                if (!result.IsValid)
                {
                    Debug.LogError(
                        $"[DataLayer] Skipping disease '{disease.DiseaseId}': {string.Join("; ", result.Errors)}");
                    continue;
                }

                foreach (var warning in result.Warnings)
                {
                    Debug.LogWarning($"[DataLayer] disease '{disease.DiseaseId}': {warning}");
                }

                _diseases.Add(disease.DiseaseId, disease);
            }
        }

        /// <inheritdoc />
        public bool TryGetOrgan(string organId, out OrganAsset organ)
        {
            if (string.IsNullOrEmpty(organId))
            {
                organ = null;
                return false;
            }

            return _organs.TryGetValue(organId, out organ);
        }

        /// <inheritdoc />
        public bool TryGetDisease(string diseaseId, out DiseaseAsset disease)
        {
            if (string.IsNullOrEmpty(diseaseId))
            {
                disease = null;
                return false;
            }

            return _diseases.TryGetValue(diseaseId, out disease);
        }

        /// <inheritdoc />
        public OrganAsset GetOrgan(string organId) => TryGetOrgan(organId, out var organ) ? organ : null;

        /// <inheritdoc />
        public DiseaseAsset GetDisease(string diseaseId) => TryGetDisease(diseaseId, out var disease) ? disease : null;
    }
}
