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
            public AppState CurrentState { get; set; } = AppState.AR_VIEWER_MODE;
            public event Action<AppState> OnAppStateChanged;
            public void Fire(AppState state) => OnAppStateChanged?.Invoke(state);
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
            _registry.Clear();
            Assert.IsNull(_registry.FallbackManager);
        }
    }
}
