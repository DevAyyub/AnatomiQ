using AnatomiQ.Core;
using AnatomiQ.UI;
using NUnit.Framework;
using UnityEngine;

namespace AnatomiQ.Tests.PlayMode
{
    /// <summary>
    /// CORE-002 chunk 5 — A.12 overlay. Covers the high-risk paths only (per the testing standard):
    /// the null-safety degradation when no FallbackManager is registered, and that a registered
    /// snapshot's values actually reach the composed text. The Canvas/EventSystem construction is not
    /// exercised — the overlay's <c>ComposeMetricsText</c> seam runs the same <c>RefreshSb</c> path
    /// the live tool uses without building UI, so these stay fast and deterministic.
    /// </summary>
    public sealed class PerformanceOverlayTests
    {
        // Builds the overlay host INACTIVE so Awake (which builds the Canvas) never runs; we only
        // call the compose seam, which reads the injected registry and touches no UI state.
        private static PerformanceOverlay NewOverlay(out ServiceRegistry registry)
        {
            registry = ScriptableObject.CreateInstance<ServiceRegistry>();
            var host = new GameObject("PerfOverlay_Test");
            host.SetActive(false);
            var overlay = host.AddComponent<PerformanceOverlay>();
            overlay.ConfigureServicesForTest(registry);
            return overlay;
        }

        [Test]
        public void Compose_FallbackManagerNotRegistered_ShowsPlaceholder_NoThrow()
        {
            var overlay = NewOverlay(out var registry);

            string text = overlay.ComposeMetricsText();

            Assert.IsNotNull(text);
            StringAssert.Contains("not registered", text,
                "With no FallbackManager registered, the overlay must show a placeholder, not throw.");

            Object.DestroyImmediate(overlay.gameObject);
            Object.DestroyImmediate(registry);
        }

        [Test]
        public void Compose_WithMetrics_ReflectsFpsTierAndRam()
        {
            var overlay = NewOverlay(out var registry);
            registry.Register(new FakeFallbackManager
            {
                Metrics = new PerformanceMetrics(58f, PerformanceTier.Reduced, 0.3f, 812f)
            });

            string text = overlay.ComposeMetricsText();

            StringAssert.Contains("58", text, "Rolling FPS value should appear.");
            StringAssert.Contains("Reduced", text, "Current tier name should appear.");
            StringAssert.Contains("812", text, "RAM value should appear.");

            Object.DestroyImmediate(overlay.gameObject);
            Object.DestroyImmediate(registry);
        }

        // Minimal IFallbackManager stand-in: only Metrics is read by the overlay. Events are required
        // by the interface but unused here (67 = "event never used").
#pragma warning disable 67
        private sealed class FakeFallbackManager : IFallbackManager
        {
            public AppState CurrentState { get; set; }
            public PerformanceTier CurrentTier { get; set; }
            public Connectivity CurrentConnectivity { get; set; }
            public PerformanceMetrics Metrics { get; set; }

            public event System.Action<AppState> OnAppStateChanged;
            public event System.Action<PerformanceTier> OnPerformanceTierChanged;
            public event System.Action<Connectivity> OnConnectivityChanged;
        }
#pragma warning restore 67
    }
}
