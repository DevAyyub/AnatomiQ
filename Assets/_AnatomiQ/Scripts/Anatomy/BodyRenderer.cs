using System;
using AnatomiQ.Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AnatomiQ.Anatomy
{
    /// <summary>
    /// CORE-002 (3D Body Model Renderer). Owns the body model's root transform, self-registers with
    /// the <see cref="ServiceRegistry"/> as the single <see cref="IBodyModelRenderer"/>, guards a
    /// missing/empty model with a placeholder, assigns the authored organ + ghost-shell materials, and
    /// is the first consumer of CORE-007's <see cref="IFallbackManager.OnPerformanceTierChanged"/>
    /// signal — translating each <see cref="PerformanceTier"/> into concrete URP degradation levers.
    ///
    /// The render levers (render scale, shadow distance, post-processing) act on the ACTIVE URP asset.
    /// Because that asset is a project ScriptableObject, the originals are cached on Awake and restored
    /// on teardown so play-mode mutations never persist into the asset (Build Environment Part D
    /// runtime-mutation rule). No LOD-mesh lever yet — the model has no LODGroups, so it would be a
    /// no-op; it arrives with LOD generation. Lives in the Anatomy assembly and references no pillar.
    /// </summary>
    [DefaultExecutionOrder(-800)] // After CORE-007 (-1000) and CORE-001 (-900); before AppBootstrap (-500).
    public sealed class BodyRenderer : MonoBehaviour, IBodyModelRenderer
    {
        /// <summary>One authored base material bound to every child renderer whose name contains the token.</summary>
        [Serializable]
        private struct OrganMaterial
        {
            [Tooltip("Substring matched (case-insensitive) against child renderer names under the " +
                     "model root, e.g. \"AQ_Heart\".")]
            public string nameToken;

            [Tooltip("Authored URP material assigned to every matching renderer.")]
            public Material material;
        }

        /// <summary>The concrete URP levers a single <see cref="PerformanceTier"/> maps to.</summary>
        internal readonly struct TierRenderSettings
        {
            public readonly float RenderScale;
            public readonly float ShadowDistance;
            public readonly bool PostProcessing;

            public TierRenderSettings(float renderScale, float shadowDistance, bool postProcessing)
            {
                RenderScale = renderScale;
                ShadowDistance = shadowDistance;
                PostProcessing = postProcessing;
            }
        }

        [Header("Wiring")]
        [SerializeField, Tooltip("The shared cross-cutting service registry asset.")]
        private ServiceRegistry _services;

        [SerializeField, Tooltip("Clean root the body model is parented under. A later AR-placement " +
                                 "feature anchors this transform. Assign the BodyRoot GameObject.")]
        private Transform _modelRoot;

        [Header("Base materials")]
        [SerializeField, Tooltip("Token → material map for the organ meshes (opaque base colours).")]
        private OrganMaterial[] _organMaterials = Array.Empty<OrganMaterial>();

        [Header("Body shell")]
        [SerializeField, Tooltip("Substring matched (case-insensitive) against child renderer names to " +
                                 "locate the body shell, e.g. \"AQ_BodyShell\".")]
        private string _bodyShellToken = "AQ_BodyShell";

        [SerializeField, Tooltip("Translucent ghost material for the body shell (AnatomiQ/GhostShell). " +
                                 "Optional: if unset, the shell is left as imported.")]
        private Material _bodyShellMaterial;

        [Header("Performance")]
        [SerializeField, Tooltip("Optional. The scene's global post-processing Volume. Its weight is " +
                                 "driven to 0 below Nominal tier and restored at Nominal. Leave unset " +
                                 "to skip the post-processing lever.")]
        private Volume _postProcessVolume;

        private const string PLACEHOLDER_NAME = "BodyModel_Placeholder";

        // Tier-lever state. Originals are cached on Awake and restored on teardown.
        private IFallbackManager _fallback;
        private UniversalRenderPipelineAsset _urpAsset;
        private float _originalRenderScale;
        private float _originalShadowDistance;
        private float _originalVolumeWeight;

#if UNITY_INCLUDE_TESTS
        private PerformanceTier _appliedTier = PerformanceTier.Nominal;
#endif

        /// <inheritdoc />
        public Transform ModelRoot => _modelRoot;

        /// <inheritdoc />
        public bool IsModelReady { get; private set; }

        /// <summary>
        /// Registers the renderer, runs the model-load guard, applies the authored materials, then
        /// caches the URP originals and subscribes to the performance-tier signal. Registration order
        /// is guaranteed by <see cref="DefaultExecutionOrderAttribute"/>; CORE-007 has already
        /// registered by this point, so the tier subscription resolves.
        /// </summary>
        private void Awake()
        {
            if (_services == null)
            {
                Debug.LogError("[BodyRenderer] ServiceRegistry not assigned; CORE-002 cannot register.");
                return;
            }

            _services.Register(this);

            EnsureModelRoot();
            VerifyModelOrFallback();

            if (IsModelReady)
            {
                ApplyOrganMaterials();
                ApplyShellMaterial();
            }

            CacheOriginalRenderSettings();
            SubscribeToTier();
        }

        /// <summary>Unsubscribes from the tier signal and restores the cached URP originals.</summary>
        private void OnDestroy()
        {
            if (_fallback != null)
            {
                _fallback.OnPerformanceTierChanged -= HandleTierChanged;
            }

            RestoreOriginalRenderSettings();
        }

        /// <summary>
        /// Guarantees a non-null <see cref="_modelRoot"/> so consumers never receive a null root. If
        /// the inspector reference is missing, a root is created under this object as a last resort.
        /// </summary>
        private void EnsureModelRoot()
        {
            if (_modelRoot != null) return;

            Debug.LogError("[BodyRenderer] _modelRoot not assigned; creating an empty fallback root.");
            var root = new GameObject("BodyRoot_Fallback");
            root.transform.SetParent(transform, worldPositionStays: false);
            _modelRoot = root.transform;
        }

        /// <summary>
        /// CORE-002 load fallback: if the model root has no renderers (model missing or failed to
        /// import), spawn a simple labelled placeholder so the app degrades to a visible-but-minimal
        /// state instead of an empty scene. Sets <see cref="IsModelReady"/> accordingly.
        /// </summary>
        private void VerifyModelOrFallback()
        {
            var renderers = _modelRoot.GetComponentsInChildren<MeshRenderer>(includeInactive: true);
            if (renderers.Length > 0)
            {
                IsModelReady = true;
                return;
            }

            Debug.LogError("[BodyRenderer] No mesh renderers under the model root; spawning placeholder.");
            SpawnPlaceholder();
            IsModelReady = false;
        }

        /// <summary>
        /// Applies each authored organ material to every child renderer whose name contains the entry's
        /// token (case-insensitive). Uses <see cref="Renderer.sharedMaterial"/> to swap the reference
        /// without instantiating per-renderer copies. A token that matches nothing logs a warning. The
        /// body shell is handled separately by <see cref="ApplyShellMaterial"/>.
        /// </summary>
        private void ApplyOrganMaterials()
        {
            if (_organMaterials == null || _organMaterials.Length == 0) return;

            var renderers = _modelRoot.GetComponentsInChildren<MeshRenderer>(includeInactive: true);

            foreach (var entry in _organMaterials)
            {
                if (string.IsNullOrWhiteSpace(entry.nameToken) || entry.material == null)
                {
                    Debug.LogWarning("[BodyRenderer] Skipping organ-material entry with an empty token " +
                                     "or null material.");
                    continue;
                }

                var matched = false;
                foreach (var r in renderers)
                {
                    if (r.name.IndexOf(entry.nameToken, StringComparison.OrdinalIgnoreCase) < 0) continue;
                    r.sharedMaterial = entry.material;
                    matched = true;
                }

                if (!matched)
                {
                    Debug.LogWarning($"[BodyRenderer] No renderer matched organ token '{entry.nameToken}'.");
                }
            }
        }

        /// <summary>
        /// Assigns the translucent ghost material to the body-shell renderer (the child whose name
        /// contains <see cref="_bodyShellToken"/>). Optional: if no material is assigned the shell is
        /// left as imported (a benign degraded state). Warns only on a real misconfiguration: a
        /// material is assigned but the token matches no renderer.
        /// </summary>
        private void ApplyShellMaterial()
        {
            if (_bodyShellMaterial == null) return;

            if (string.IsNullOrWhiteSpace(_bodyShellToken))
            {
                Debug.LogWarning("[BodyRenderer] Body-shell material assigned but token is empty; " +
                                 "shell not applied.");
                return;
            }

            var renderers = _modelRoot.GetComponentsInChildren<MeshRenderer>(includeInactive: true);
            foreach (var r in renderers)
            {
                if (r.name.IndexOf(_bodyShellToken, StringComparison.OrdinalIgnoreCase) < 0) continue;
                r.sharedMaterial = _bodyShellMaterial;
                return;
            }

            Debug.LogWarning($"[BodyRenderer] No renderer matched body-shell token '{_bodyShellToken}'.");
        }

        /// <summary>
        /// Pure mapping from a <see cref="PerformanceTier"/> to its URP levers. Kept side-effect-free so
        /// the values can be unit-tested without a scene or an active render pipeline. Render scale is
        /// monotonically non-increasing with severity; the floor (0.75) matches the URP mobile budget.
        /// </summary>
        internal static TierRenderSettings GetSettingsForTier(PerformanceTier tier) => tier switch
        {
            PerformanceTier.Nominal    => new TierRenderSettings(0.90f, 15f, true),
            PerformanceTier.Reduced    => new TierRenderSettings(0.85f, 15f, false),
            PerformanceTier.Aggressive => new TierRenderSettings(0.75f, 8f,  false),
            PerformanceTier.Critical   => new TierRenderSettings(0.75f, 0f,  false),
            _                          => new TierRenderSettings(0.90f, 15f, true),
        };

        /// <summary>
        /// Caches the active URP asset and its original render-scale / shadow-distance, plus the
        /// optional Volume's original weight, so <see cref="RestoreOriginalRenderSettings"/> can undo
        /// every play-mode mutation on teardown.
        /// </summary>
        private void CacheOriginalRenderSettings()
        {
            _urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (_urpAsset != null)
            {
                _originalRenderScale = _urpAsset.renderScale;
                _originalShadowDistance = _urpAsset.shadowDistance;
            }
            else
            {
                Debug.LogWarning("[BodyRenderer] Active render pipeline is not URP; tier render levers " +
                                 "are inactive.");
            }

            if (_postProcessVolume != null)
            {
                _originalVolumeWeight = _postProcessVolume.weight;
            }
        }

        /// <summary>
        /// Resolves the FallbackManager, applies whatever tier is ALREADY current (the device may come
        /// up hot), then subscribes to future changes.
        /// </summary>
        private void SubscribeToTier()
        {
            _fallback = _services.FallbackManager;
            if (_fallback == null)
            {
                Debug.LogWarning("[BodyRenderer] FallbackManager not available; tier response inactive.");
                return;
            }

            _fallback.OnPerformanceTierChanged += HandleTierChanged;
            ApplyTier(_fallback.CurrentTier);
        }

        private void HandleTierChanged(PerformanceTier tier) => ApplyTier(tier);

        /// <summary>
        /// Translates the given tier into the active render levers. Restores the original Volume weight
        /// (rather than forcing 1) when post-processing is re-enabled, so we never override the author's
        /// intended weight.
        /// </summary>
        private void ApplyTier(PerformanceTier tier)
        {
#if UNITY_INCLUDE_TESTS
            _appliedTier = tier;
#endif
            var settings = GetSettingsForTier(tier);

            if (_urpAsset != null)
            {
                _urpAsset.renderScale = settings.RenderScale;
                _urpAsset.shadowDistance = settings.ShadowDistance;
            }

            if (_postProcessVolume != null)
            {
                _postProcessVolume.weight = settings.PostProcessing ? _originalVolumeWeight : 0f;
            }
        }

        /// <summary>Restores the URP asset and Volume to the values cached on Awake.</summary>
        private void RestoreOriginalRenderSettings()
        {
            if (_urpAsset != null)
            {
                _urpAsset.renderScale = _originalRenderScale;
                _urpAsset.shadowDistance = _originalShadowDistance;
            }

            if (_postProcessVolume != null)
            {
                _postProcessVolume.weight = _originalVolumeWeight;
            }
        }

        /// <summary>
        /// Creates a primitive sphere plus a world-space text label as the load fallback. The label
        /// uses the legacy <see cref="TextMesh"/> so it needs no UI canvas in this guard path. Note:
        /// on Unity 6 the built-in legacy font was removed, so the label may need a font assigned to
        /// render glyphs — the sphere alone still proves the fallback is active.
        /// </summary>
        private void SpawnPlaceholder()
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = PLACEHOLDER_NAME;
            sphere.transform.SetParent(_modelRoot, worldPositionStays: false);
            sphere.transform.localScale = Vector3.one * 0.3f;

            var labelObj = new GameObject("PlaceholderLabel");
            labelObj.transform.SetParent(sphere.transform, worldPositionStays: false);
            labelObj.transform.localPosition = Vector3.up * 1.5f;

            var label = labelObj.AddComponent<TextMesh>();
            label.text = "Body model unavailable";
            label.anchor = TextAnchor.MiddleCenter;
            label.characterSize = 0.1f;
            label.fontSize = 64;
        }

#if UNITY_INCLUDE_TESTS
        /// <summary>
        /// Test-only seam mirroring CORE-007's <c>Configure*</c> hooks: injects wiring before
        /// <see cref="Awake"/> runs. Call on an INACTIVE GameObject, then activate it so Awake fires
        /// with the fields populated. Visible to the PlayMode test assembly via InternalsVisibleTo.
        /// </summary>
        internal void ConfigureForTest(ServiceRegistry services, Transform modelRoot)
        {
            _services = services;
            _modelRoot = modelRoot;
        }

        /// <summary>Test-only seam: appends one token → material entry to the organ-material map.</summary>
        internal void SetOrganMaterialForTest(string nameToken, Material material)
        {
            var existing = _organMaterials ?? Array.Empty<OrganMaterial>();
            var grown = new OrganMaterial[existing.Length + 1];
            Array.Copy(existing, grown, existing.Length);
            grown[existing.Length] = new OrganMaterial { nameToken = nameToken, material = material };
            _organMaterials = grown;
        }

        /// <summary>Test-only seam: sets the body-shell token and material.</summary>
        internal void SetShellMaterialForTest(string nameToken, Material material)
        {
            _bodyShellToken = nameToken;
            _bodyShellMaterial = material;
        }

        /// <summary>Test-only seam: injects the post-processing Volume used by the tier lever.</summary>
        internal void SetPostProcessVolumeForTest(Volume volume)
        {
            _postProcessVolume = volume;
        }

        /// <summary>Test-only observable: the most recent tier passed through <see cref="ApplyTier"/>.</summary>
        internal PerformanceTier AppliedTierForTest => _appliedTier;
#endif
    }
}
