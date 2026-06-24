using System;
using AnatomiQ.Core;
using NUnit.Framework;
using UnityEngine;

namespace AnatomiQ.Tests.EditMode
{
    /// <summary>
    /// ATLAS-003 (chunk 1) — verifies the <see cref="ServiceRegistry"/> plumbing added for
    /// <see cref="IPlacementProvider"/>: generic registration via the <see cref="ServiceRegistry.Register"/>
    /// switch, identity-checked <see cref="ServiceRegistry.ClearPlacementProvider"/>, and
    /// <see cref="ServiceRegistry.Clear"/>. Uses a plain mock provider so no AR scene is required —
    /// mirrors the existing IArTrackingProvider coverage.
    ///
    /// NOTE: adjust the namespace / place this under whatever asmdef your existing EditMode tests use
    /// (the project's ServiceRegistryTests live in Tests/EditMode).
    /// </summary>
    public sealed class PlacementProviderRegistryTests
    {
        /// <summary>Minimal in-memory <see cref="IPlacementProvider"/> for registry tests.</summary>
        private sealed class MockPlacementProvider : IPlacementProvider
        {
            public PlacementMode CurrentMode { get; private set; } = PlacementMode.Viewer;
            public event Action<PlacementMode> OnPlacementModeChanged;

            public void RequestMode(PlacementMode mode)
            {
                if (mode == CurrentMode)
                {
                    return;
                }

                CurrentMode = mode;
                OnPlacementModeChanged?.Invoke(mode);
            }
        }

        private ServiceRegistry _registry;

        [SetUp]
        public void SetUp() => _registry = ScriptableObject.CreateInstance<ServiceRegistry>();

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(_registry);

        [Test]
        public void Register_ExposesPlacementProvider_ViaGenericSwitch()
        {
            var provider = new MockPlacementProvider();

            _registry.Register(provider);

            Assert.AreSame(provider, _registry.PlacementProvider);
        }

        [Test]
        public void ClearPlacementProvider_OnlyClears_TheMatchingInstance()
        {
            var first = new MockPlacementProvider();
            var second = new MockPlacementProvider();
            _registry.Register(first);
            _registry.Register(second); // last-write-wins (logs a benign replace warning)

            // A torn-down OLD provider must not wipe the newer one that already re-registered.
            _registry.ClearPlacementProvider(first);
            Assert.AreSame(second, _registry.PlacementProvider, "Stale clear wiped the live provider.");

            _registry.ClearPlacementProvider(second);
            Assert.IsNull(_registry.PlacementProvider);
        }

        [Test]
        public void Clear_NullsThePlacementProvider()
        {
            _registry.Register(new MockPlacementProvider());

            _registry.Clear();

            Assert.IsNull(_registry.PlacementProvider);
        }

        [Test]
        public void RequestMode_ChangeOnly_FiresOnceAndIgnoresRepeat()
        {
            var provider = new MockPlacementProvider();
            int fires = 0;
            PlacementMode last = PlacementMode.Viewer;
            provider.OnPlacementModeChanged += m => { fires++; last = m; };

            provider.RequestMode(PlacementMode.Space);
            provider.RequestMode(PlacementMode.Space); // repeat → no-op

            Assert.AreEqual(1, fires, "Change-only event should fire exactly once for a real change.");
            Assert.AreEqual(PlacementMode.Space, last);
            Assert.AreEqual(PlacementMode.Space, provider.CurrentMode);
        }
    }
}
