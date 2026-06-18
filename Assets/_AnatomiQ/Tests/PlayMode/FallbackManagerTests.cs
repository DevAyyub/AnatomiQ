using System.Collections;
using System.Reflection;
using AnatomiQ.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AnatomiQ.Tests.PlayMode
{
    /// <summary>
    /// Section 6 gate. Verifies the CORE-007 shell wires into the architecture: the FallbackManager
    /// self-registers into the ServiceRegistry during Awake, and its initial AppState is readable.
    /// PlayMode (not EditMode) because it exercises the MonoBehaviour lifecycle. No device build.
    /// </summary>
    public sealed class FallbackManagerTests
    {
        [UnityTest]
        public IEnumerator FallbackManager_RegistersItself_AndExposesInitialState()
        {
            var registry = ScriptableObject.CreateInstance<ServiceRegistry>();

            // Create inactive so Awake doesn't run until after we inject the registry reference.
            var go = new GameObject("FallbackManager_Test");
            go.SetActive(false);
            var fm = go.AddComponent<FallbackManager>();

            // Inject the private [SerializeField] ServiceRegistry the way the inspector would.
            typeof(FallbackManager)
                .GetField("_services", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(fm, registry);

            // Activating runs Awake (self-register) then OnEnable (start monitoring loop).
            go.SetActive(true);
            yield return null; // let one frame pass

            Assert.AreSame(fm, registry.FallbackManager,
                "FallbackManager should register itself as IFallbackManager on Awake.");
            Assert.AreEqual(AppState.AR_VIEWER_MODE, registry.FallbackManager.CurrentState,
                "Initial state should be the safe AR_VIEWER_MODE baseline.");

            Object.Destroy(go);
            Object.Destroy(registry);
        }
    }
}
