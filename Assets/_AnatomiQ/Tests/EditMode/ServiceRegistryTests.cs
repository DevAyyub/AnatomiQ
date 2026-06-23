using System;
using AnatomiQ.Core;
using NUnit.Framework;
using UnityEngine;

namespace AnatomiQ.Tests.EditMode
{
    /// <summary>
    /// Verifies the runtime-registration contract of <see cref="ServiceRegistry"/>:
    /// services start null, register under the correct interface, and clear cleanly.
    /// Section 5 gate — pure Edit Mode logic, no scene or device build.
    /// </summary>
    public sealed class ServiceRegistryTests
    {
        private sealed class MockFallbackManager : IFallbackManager
        {
            // Axis 1: AR / connectivity
            public AppState CurrentState { get; set; } = AppState.AR_VIEWER_MODE;
            public event Action<AppState> OnAppStateChanged;
            public void Fire(AppState state) => OnAppStateChanged?.Invoke(state);

            // Axis 2: quality / degradation (added with the CORE-007 two-axis logic phase).
            // This mock only needs to satisfy the contract for registration tests, so the tier
            // members are minimal stubs with a Fire helper mirroring the AppState one.
            public PerformanceTier CurrentTier { get; set; } = PerformanceTier.Nominal;
            public event Action<PerformanceTier> OnPerformanceTierChanged;
            public void FireTier(PerformanceTier tier) => OnPerformanceTierChanged?.Invoke(tier);

            // Axis 3: connectivity (added with the CORE-001 connectivity-axis split).
            public Connectivity CurrentConnectivity { get; set; } = Connectivity.Online;
            public event Action<Connectivity> OnConnectivityChanged;
            public void FireConnectivity(Connectivity c) => OnConnectivityChanged?.Invoke(c);

            public PerformanceMetrics Metrics =>
                new PerformanceMetrics(0f, CurrentTier, -1f, -1f);
        }

        // Minimal IArTrackingProvider (CORE-001) for the registration/clear tests.
        private sealed class MockArTracking : IArTrackingProvider
        {
            public ArTrackingStatus Status { get; set; } = ArTrackingStatus.Tracking;
        }

        private ServiceRegistry _registry;

        [SetUp]
        public void SetUp() => _registry = ScriptableObject.CreateInstance<ServiceRegistry>();

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(_registry);

        [Test]
        public void Services_AreNull_BeforeRegistration()
        {
            Assert.IsNull(_registry.FallbackManager);
            Assert.IsNull(_registry.AIOrchestrator);
            Assert.IsNull(_registry.BodyRenderer);
            Assert.IsNull(_registry.DataLayer);
            Assert.IsNull(_registry.ArTrackingProvider);
        }

        [Test]
        public void Register_StoresService_UnderCorrectInterface()
        {
            var mock = new MockFallbackManager();
            _registry.Register(mock);
            Assert.AreSame(mock, _registry.FallbackManager);
            Assert.IsNull(_registry.AIOrchestrator);
        }

        [Test]
        public void Clear_RemovesAllRegistrations()
        {
            _registry.Register(new MockFallbackManager());
            _registry.Register(new MockArTracking());
            _registry.Clear();
            Assert.IsNull(_registry.FallbackManager);
            Assert.IsNull(_registry.ArTrackingProvider);
        }

        [Test]
        public void Register_StoresArTrackingProvider_UnderCorrectInterface()
        {
            var ar = new MockArTracking();
            _registry.Register(ar);
            Assert.AreSame(ar, _registry.ArTrackingProvider);
            Assert.IsNull(_registry.FallbackManager, "AR registration must not touch other slots.");
        }

        [Test]
        public void ClearArTrackingProvider_ClearsMatchingInstanceOnly()
        {
            var ar = new MockArTracking();
            _registry.Register(ar);

            _registry.ClearArTrackingProvider(new MockArTracking()); // different instance
            Assert.AreSame(ar, _registry.ArTrackingProvider,
                "Clearing a different instance must be a no-op (avoids a torn-down provider wiping a newer one).");

            _registry.ClearArTrackingProvider(ar); // matching instance
            Assert.IsNull(_registry.ArTrackingProvider, "Clearing the registered instance removes it.");
        }
    }
}
