using System.Collections;
using AnatomiQ.Anatomy;
using AnatomiQ.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AnatomiQ.Tests.PlayMode
{
    /// <summary>
    /// CORE-002 chunks 1–4. Verifies the BodyRenderer self-registers as the single
    /// <see cref="IBodyModelRenderer"/>, always exposes a non-null <see cref="IBodyModelRenderer.ModelRoot"/>,
    /// falls back to a placeholder when the model root has no renderers, applies authored organ and
    /// body-shell materials, and responds to the CORE-007 performance-tier signal.
    ///
    /// A shared <see cref="FakeFallbackManager"/> is registered in <see cref="SetUp"/> so the renderer's
    /// tier subscription always resolves (it is the single source of the tier signal in the real app),
    /// keeping the test console free of "FallbackManager not available" warnings.
    /// </summary>
    public sealed class BodyRendererRegistrationTests
    {
        private ServiceRegistry _registry;
        private FakeFallbackManager _fakeFallback;

        [SetUp]
        public void SetUp()
        {
            _registry = ScriptableObject.CreateInstance<ServiceRegistry>();
            _fakeFallback = new FakeFallbackManager { CurrentTier = PerformanceTier.Nominal };
            _registry.Register(_fakeFallback);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_registry);

        /// <summary>
        /// Builds the host INACTIVE so Awake is deferred, runs <paramref name="configure"/> to inject
        /// wiring/materials, then activates so Awake runs against the populated fields.
        /// </summary>
        private static BodyRenderer CreateConfigured(System.Action<BodyRenderer> configure)
        {
            var host = new GameObject("BodyRenderer_Test");
            host.SetActive(false);
            var renderer = host.AddComponent<BodyRenderer>();
            configure(renderer);
            host.SetActive(true);
            return renderer;
        }

        private static Material NewUrpLitMaterial() => new(Shader.Find("Universal Render Pipeline/Lit"));

        [UnityTest]
        public IEnumerator Registers_AsBodyModelRenderer_WhenModelHasRenderers()
        {
            var modelRoot = new GameObject("BodyRoot").transform;
            GameObject.CreatePrimitive(PrimitiveType.Cube).transform.SetParent(modelRoot);

            var renderer = CreateConfigured(r => r.ConfigureForTest(_registry, modelRoot));
            yield return null;

            Assert.AreSame(renderer, _registry.BodyRenderer, "BodyRenderer must register itself.");
            Assert.IsTrue(renderer.IsModelReady, "A root with renderers should report ready.");
            Assert.IsNotNull(renderer.ModelRoot, "ModelRoot must never be null after init.");

            Object.Destroy(renderer.gameObject);
            yield return null;
            Object.Destroy(modelRoot.gameObject);
        }

        [UnityTest]
        public IEnumerator FallsBackToPlaceholder_WhenRootHasNoRenderers()
        {
            var modelRoot = new GameObject("BodyRoot_Empty").transform;

            // The fallback path legitimately logs an error; the framework would otherwise fail the
            // test on it. Declare it expected BEFORE Awake runs (CreateConfigured activates the host).
            LogAssert.Expect(LogType.Error,
                "[BodyRenderer] No mesh renderers under the model root; spawning placeholder.");

            var renderer = CreateConfigured(r => r.ConfigureForTest(_registry, modelRoot));
            yield return null;

            Assert.IsFalse(renderer.IsModelReady, "An empty root should report not-ready.");
            Assert.Greater(
                modelRoot.GetComponentsInChildren<MeshRenderer>(true).Length, 0,
                "Placeholder should add a visible renderer under the root.");

            Object.Destroy(renderer.gameObject);
            yield return null;
            Object.Destroy(modelRoot.gameObject);
        }

        [UnityTest]
        public IEnumerator AppliesOrganMaterial_ToMatchingRenderer()
        {
            var modelRoot = new GameObject("BodyRoot").transform;
            var heart = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            heart.name = "AQ_Heart";
            heart.transform.SetParent(modelRoot);

            var mat = NewUrpLitMaterial();

            CreateConfigured(r =>
            {
                r.ConfigureForTest(_registry, modelRoot);
                r.SetOrganMaterialForTest("AQ_Heart", mat);
            });
            yield return null;

            Assert.AreSame(mat, heart.GetComponent<MeshRenderer>().sharedMaterial,
                "The matching renderer should receive the mapped material.");

            Object.Destroy(modelRoot.gameObject);
            Object.Destroy(mat);
        }

        [UnityTest]
        public IEnumerator WarnsWhenOrganTokenMatchesNothing()
        {
            var modelRoot = new GameObject("BodyRoot").transform;
            GameObject.CreatePrimitive(PrimitiveType.Cube).transform.SetParent(modelRoot); // not "AQ_*"
            var mat = NewUrpLitMaterial();

            LogAssert.Expect(LogType.Warning,
                "[BodyRenderer] No renderer matched organ token 'AQ_DoesNotExist'.");

            CreateConfigured(r =>
            {
                r.ConfigureForTest(_registry, modelRoot);
                r.SetOrganMaterialForTest("AQ_DoesNotExist", mat);
            });
            yield return null;

            Object.Destroy(modelRoot.gameObject);
            Object.Destroy(mat);
        }

        [UnityTest]
        public IEnumerator AppliesShellMaterial_ToMatchingRenderer()
        {
            var modelRoot = new GameObject("BodyRoot").transform;
            var shell = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shell.name = "AQ_BodyShell";
            shell.transform.SetParent(modelRoot);

            // Assignment is shader-agnostic, so a URP Lit material is enough to verify the wiring;
            // the GhostShell shader's visual correctness is checked in the editor and on device.
            var shellMat = NewUrpLitMaterial();

            CreateConfigured(r =>
            {
                r.ConfigureForTest(_registry, modelRoot);
                r.SetShellMaterialForTest("AQ_BodyShell", shellMat);
            });
            yield return null;

            Assert.AreSame(shellMat, shell.GetComponent<MeshRenderer>().sharedMaterial,
                "The shell renderer should receive the assigned shell material.");

            Object.Destroy(modelRoot.gameObject);
            Object.Destroy(shellMat);
        }

        [Test]
        public void TierMapping_RenderScaleMonotonic_AndCriticalMostAggressive()
        {
            var nominal    = BodyRenderer.GetSettingsForTier(PerformanceTier.Nominal);
            var reduced    = BodyRenderer.GetSettingsForTier(PerformanceTier.Reduced);
            var aggressive = BodyRenderer.GetSettingsForTier(PerformanceTier.Aggressive);
            var critical   = BodyRenderer.GetSettingsForTier(PerformanceTier.Critical);

            Assert.AreEqual(0.90f, nominal.RenderScale, 1e-4f, "Nominal render scale.");
            Assert.AreEqual(0.75f, critical.RenderScale, 1e-4f, "Critical render-scale floor.");
            Assert.GreaterOrEqual(nominal.RenderScale, reduced.RenderScale);
            Assert.GreaterOrEqual(reduced.RenderScale, aggressive.RenderScale);
            Assert.GreaterOrEqual(aggressive.RenderScale, critical.RenderScale);

            Assert.IsTrue(nominal.PostProcessing, "Post-processing on at Nominal.");
            Assert.IsFalse(reduced.PostProcessing, "Post-processing off below Nominal.");
            Assert.IsFalse(critical.PostProcessing);

            Assert.GreaterOrEqual(nominal.ShadowDistance, aggressive.ShadowDistance);
            Assert.AreEqual(0f, critical.ShadowDistance, 1e-4f, "Shadows off at Critical.");
        }

        [UnityTest]
        public IEnumerator AppliesCurrentTierOnSubscribe_AndRespondsToChanges()
        {
            _fakeFallback.CurrentTier = PerformanceTier.Aggressive; // device may come up hot

            var modelRoot = new GameObject("BodyRoot").transform;
            GameObject.CreatePrimitive(PrimitiveType.Cube).transform.SetParent(modelRoot);

            var renderer = CreateConfigured(r => r.ConfigureForTest(_registry, modelRoot));
            yield return null;

            Assert.AreEqual(PerformanceTier.Aggressive, renderer.AppliedTierForTest,
                "The already-current tier should be applied on subscribe.");

            _fakeFallback.RaiseTier(PerformanceTier.Critical);
            Assert.AreEqual(PerformanceTier.Critical, renderer.AppliedTierForTest,
                "A tier change should propagate to the renderer.");

            Object.Destroy(renderer.gameObject);
            yield return null; // let OnDestroy restore the cached URP originals
            Object.Destroy(modelRoot.gameObject);
        }

        /// <summary>
        /// Minimal IFallbackManager stand-in: lets a test set the current tier and raise the tier
        /// signal. The state/connectivity raise helpers exist only to consume their events (avoiding
        /// unused-event warnings) and to keep the interface honestly implemented.
        /// </summary>
        private sealed class FakeFallbackManager : IFallbackManager
        {
            public AppState CurrentState { get; set; }
            public PerformanceTier CurrentTier { get; set; }
            public Connectivity CurrentConnectivity { get; set; }
            public PerformanceMetrics Metrics { get; set; }

            public event System.Action<AppState> OnAppStateChanged;
            public event System.Action<PerformanceTier> OnPerformanceTierChanged;
            public event System.Action<Connectivity> OnConnectivityChanged;

            public void RaiseTier(PerformanceTier tier)
            {
                CurrentTier = tier;
                OnPerformanceTierChanged?.Invoke(tier);
            }

            public void RaiseState(AppState state) => OnAppStateChanged?.Invoke(state);
            public void RaiseConnectivity(Connectivity connectivity) => OnConnectivityChanged?.Invoke(connectivity);
        }
    }
}
