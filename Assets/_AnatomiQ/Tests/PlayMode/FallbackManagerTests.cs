using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using AnatomiQ.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AnatomiQ.Tests.PlayMode
{
    /// <summary>
    /// CORE-007 logic-phase tests. Extends the Section-6 shell gate (self-registration + initial
    /// state) with behavioural coverage of the signal checks whose sources exist now. AR / API /
    /// inference checks remain stubs and are covered only by the "inert stubs" assertion.
    ///
    /// PlayMode because it drives the MonoBehaviour lifecycle. No device build: the live signal
    /// sources are swapped for fakes through the internal Configure* seams, and a single monitoring
    /// pass is driven synchronously via TickForTest() instead of waiting on the 1s coroutine.
    /// </summary>
    public sealed class FallbackManagerTests
    {
        // Fake reachability source so offline/online transitions are deterministic.
        private sealed class FakeConnectivity : IConnectivityProvider
        {
            public bool Reachable = true;
            public bool IsReachable => Reachable;
        }

        // Builds an active FallbackManager wired to a fresh registry, exactly as the inspector would.
        private static FallbackManager NewManager(out ServiceRegistry registry)
        {
            registry = ScriptableObject.CreateInstance<ServiceRegistry>();

            var go = new GameObject("FallbackManager_Test");
            go.SetActive(false); // hold Awake until the registry is injected
            var fm = go.AddComponent<FallbackManager>();

            typeof(FallbackManager)
                .GetField("_services", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(fm, registry);

            go.SetActive(true); // runs Awake (self-register) + OnEnable (start monitoring loop)
            return fm;
        }

        // ----- Shell gate (carried over from Section 6) ---------------------------------------

        [UnityTest]
        public IEnumerator FallbackManager_RegistersItself_AndExposesInitialState()
        {
            var fm = NewManager(out var registry);
            yield return null;

            Assert.AreSame(fm, registry.FallbackManager,
                "FallbackManager should register itself as IFallbackManager on Awake.");
            Assert.AreEqual(AppState.AR_VIEWER_MODE, registry.FallbackManager.CurrentState,
                "Initial state should be the safe AR_VIEWER_MODE baseline.");
            Assert.AreEqual(PerformanceTier.Nominal, registry.FallbackManager.CurrentTier,
                "Initial tier should be Nominal.");

            Object.Destroy(fm.gameObject);
            Object.Destroy(registry);
        }

        // ----- Connectivity → AppState (debounced) --------------------------------------------

        [UnityTest]
        public IEnumerator Connectivity_TwoOfflineSamples_EntersOfflineMode_Once()
        {
            var fm = NewManager(out var registry);
            yield return null;

            var conn = new FakeConnectivity { Reachable = true };
            fm.ConfigureConnectivityProvider(conn);

            var seen = new List<AppState>();
            fm.OnAppStateChanged += s => seen.Add(s);

            conn.Reachable = false;
            fm.TickForTest();
            Assert.AreEqual(AppState.AR_VIEWER_MODE, fm.CurrentState,
                "A single offline sample must not flip state (debounce).");
            Assert.AreEqual(0, seen.Count, "No event should fire on a single offline sample.");

            fm.TickForTest();
            Assert.AreEqual(AppState.OFFLINE_MODE, fm.CurrentState,
                "Two consecutive offline samples should enter OFFLINE_MODE.");
            Assert.AreEqual(1, seen.Count, "Exactly one state-change event should fire.");
            Assert.AreEqual(AppState.OFFLINE_MODE, seen[0]);

            fm.TickForTest();
            fm.TickForTest();
            Assert.AreEqual(1, seen.Count, "Staying offline must not re-fire the event.");

            Object.Destroy(fm.gameObject);
            Object.Destroy(registry);
        }

        [UnityTest]
        public IEnumerator Connectivity_Restored_PromotesToViewerMode_NotArActive()
        {
            var fm = NewManager(out var registry);
            yield return null;

            var conn = new FakeConnectivity { Reachable = false };
            fm.ConfigureConnectivityProvider(conn);
            fm.TickForTest();
            fm.TickForTest(); // now OFFLINE_MODE
            Assert.AreEqual(AppState.OFFLINE_MODE, fm.CurrentState);

            var seen = new List<AppState>();
            fm.OnAppStateChanged += s => seen.Add(s);

            conn.Reachable = true;
            fm.TickForTest();
            Assert.AreEqual(AppState.OFFLINE_MODE, fm.CurrentState,
                "A single online sample must not promote yet (debounce).");

            fm.TickForTest();
            Assert.AreEqual(AppState.AR_VIEWER_MODE, fm.CurrentState,
                "Restored connectivity promotes to the safe baseline, never directly to AR_ACTIVE.");
            Assert.AreEqual(1, seen.Count);
            Assert.AreEqual(AppState.AR_VIEWER_MODE, seen[0]);

            Object.Destroy(fm.gameObject);
            Object.Destroy(registry);
        }

        [UnityTest]
        public IEnumerator Connectivity_Flapping_NeverReachesDebounce_NoTransition()
        {
            var fm = NewManager(out var registry);
            yield return null;

            var conn = new FakeConnectivity { Reachable = true };
            fm.ConfigureConnectivityProvider(conn);

            var seen = new List<AppState>();
            fm.OnAppStateChanged += s => seen.Add(s);

            // Alternate every sample so neither streak ever reaches 2.
            conn.Reachable = false; fm.TickForTest();
            conn.Reachable = true;  fm.TickForTest();
            conn.Reachable = false; fm.TickForTest();
            conn.Reachable = true;  fm.TickForTest();

            Assert.AreEqual(AppState.AR_VIEWER_MODE, fm.CurrentState,
                "Alternating samples never satisfy the debounce, so state must not change.");
            Assert.AreEqual(0, seen.Count, "Flapping must fire no events.");

            Object.Destroy(fm.gameObject);
            Object.Destroy(registry);
        }

        // ----- Framerate → PerformanceTier (sustain + hysteresis) -----------------------------

        [UnityTest]
        public IEnumerator Framerate_BelowThreshold_Sustained3s_DemotesOneStep()
        {
            var fm = NewManager(out var registry);
            yield return null;
            fm.ConfigureConnectivityProvider(new FakeConnectivity { Reachable = true });

            var tiers = new List<PerformanceTier>();
            fm.OnPerformanceTierChanged += t => tiers.Add(t);

            fm.PrimeFramerateForTest(20f); // steady 20 FPS, buffer full

            fm.TickForTest(); // 1s below
            fm.TickForTest(); // 2s below
            Assert.AreEqual(PerformanceTier.Nominal, fm.CurrentTier,
                "Must not demote before the 3s sustain window elapses.");

            fm.TickForTest(); // 3s below
            Assert.AreEqual(PerformanceTier.Reduced, fm.CurrentTier,
                "Sustained sub-30 FPS for 3s should step the tier to Reduced.");
            Assert.AreEqual(1, tiers.Count, "Exactly one tier event on the first demote.");

            Object.Destroy(fm.gameObject);
            Object.Destroy(registry);
        }

        [UnityTest]
        public IEnumerator Framerate_DeadBand_NoDemoteNoPromote()
        {
            var fm = NewManager(out var registry);
            yield return null;
            fm.ConfigureConnectivityProvider(new FakeConnectivity { Reachable = true });

            var tiers = new List<PerformanceTier>();
            fm.OnPerformanceTierChanged += t => tiers.Add(t);

            fm.PrimeFramerateForTest(35f); // between the 30 demote and 40 promote thresholds
            for (int i = 0; i < 6; i++)
            {
                fm.TickForTest();
            }

            Assert.AreEqual(PerformanceTier.Nominal, fm.CurrentTier,
                "FPS in the 30–40 hysteresis dead-band must not change the tier.");
            Assert.AreEqual(0, tiers.Count);

            Object.Destroy(fm.gameObject);
            Object.Destroy(registry);
        }

        [UnityTest]
        public IEnumerator Framerate_Recovery_RequiresAbove40_For5s()
        {
            var fm = NewManager(out var registry);
            yield return null;
            fm.ConfigureConnectivityProvider(new FakeConnectivity { Reachable = true });

            // Drive down to Reduced first.
            fm.PrimeFramerateForTest(20f);
            fm.TickForTest(); fm.TickForTest(); fm.TickForTest();
            Assert.AreEqual(PerformanceTier.Reduced, fm.CurrentTier);

            // Recover at 60 FPS: must hold for 5s before stepping back.
            fm.PrimeFramerateForTest(60f);
            fm.TickForTest(); fm.TickForTest(); fm.TickForTest(); fm.TickForTest();
            Assert.AreEqual(PerformanceTier.Reduced, fm.CurrentTier,
                "4s above the promote threshold is not enough; recovery needs 5s.");

            fm.TickForTest();
            Assert.AreEqual(PerformanceTier.Nominal, fm.CurrentTier,
                "5s above 40 FPS should step the tier back down to Nominal.");

            Object.Destroy(fm.gameObject);
            Object.Destroy(registry);
        }

        [UnityTest]
        public IEnumerator Framerate_InterruptedDip_ResetsSustain_NoDemote()
        {
            var fm = NewManager(out var registry);
            yield return null;
            fm.ConfigureConnectivityProvider(new FakeConnectivity { Reachable = true });

            fm.PrimeFramerateForTest(60f);
            fm.TickForTest();
            fm.PrimeFramerateForTest(20f); fm.TickForTest();          // 1s below
            fm.PrimeFramerateForTest(60f); fm.TickForTest();          // recovers → resets sustain
            fm.PrimeFramerateForTest(20f); fm.TickForTest(); fm.TickForTest(); // only 2s below again

            Assert.AreEqual(PerformanceTier.Nominal, fm.CurrentTier,
                "A dip interrupted before 3s must reset the sustain timer and not demote.");

            Object.Destroy(fm.gameObject);
            Object.Destroy(registry);
        }

        // ----- Thermal → PerformanceTier (mapping + asymmetric hysteresis) --------------------

        // Fake thermal source so each stage is deterministic without a hot device.
        private sealed class FakeThermal : IThermalProvider
        {
            public bool Available = true;
            public ThermalWarning Level = ThermalWarning.None;
            public float Temperature;
            public bool IsAvailable => Available;
            public ThermalWarning Warning => Level;
            public float TemperatureLevel => Temperature;
        }

        [UnityTest]
        public IEnumerator Thermal_UnavailableProvider_StaysInert()
        {
            var fm = NewManager(out var registry);
            yield return null;
            fm.ConfigureConnectivityProvider(new FakeConnectivity { Reachable = true });
            fm.ConfigureThermalProvider(new FakeThermal
            {
                Available = false, Level = ThermalWarning.Throttling, Temperature = 1f
            });

            var tiers = new List<PerformanceTier>();
            fm.OnPerformanceTierChanged += t => tiers.Add(t);

            for (int i = 0; i < 5; i++)
            {
                fm.TickForTest();
            }

            Assert.AreEqual(PerformanceTier.Nominal, fm.CurrentTier,
                "An unavailable thermal source must never drive a tier change.");
            Assert.AreEqual(0, tiers.Count);

            Object.Destroy(fm.gameObject);
            Object.Destroy(registry);
        }

        [UnityTest]
        public IEnumerator Thermal_Stages_MapToExpectedTiers()
        {
            var fm = NewManager(out var registry);
            yield return null;
            fm.ConfigureConnectivityProvider(new FakeConnectivity { Reachable = true });
            var thermal = new FakeThermal { Available = true };
            fm.ConfigureThermalProvider(thermal);

            thermal.Level = ThermalWarning.ThrottlingImminent; thermal.Temperature = 0.5f;
            fm.TickForTest();
            Assert.AreEqual(PerformanceTier.Reduced, fm.ThermalTierForTest,
                "ThrottlingImminent at <=0.8 maps to Reduced (Moderate stage).");

            thermal.Temperature = 0.85f; // heating applies immediately
            fm.TickForTest();
            Assert.AreEqual(PerformanceTier.Aggressive, fm.ThermalTierForTest,
                "ThrottlingImminent above 0.8 maps to Aggressive (Severe stage).");

            thermal.Level = ThermalWarning.Throttling; thermal.Temperature = 0.95f;
            fm.TickForTest();
            Assert.AreEqual(PerformanceTier.Critical, fm.ThermalTierForTest,
                "Throttling maps to Critical.");
            Assert.AreEqual(PerformanceTier.Critical, fm.CurrentTier,
                "Published tier reflects the thermal Critical.");

            Object.Destroy(fm.gameObject);
            Object.Destroy(registry);
        }

        [UnityTest]
        public IEnumerator Thermal_Cooling_IsGated_StepsDownOneLevel()
        {
            var fm = NewManager(out var registry);
            yield return null;
            fm.ConfigureConnectivityProvider(new FakeConnectivity { Reachable = true });
            var thermal = new FakeThermal { Available = true, Level = ThermalWarning.Throttling, Temperature = 1f };
            fm.ConfigureThermalProvider(thermal);

            fm.TickForTest(); // heat to Critical immediately
            Assert.AreEqual(PerformanceTier.Critical, fm.ThermalTierForTest);

            thermal.Level = ThermalWarning.None; thermal.Temperature = 0.2f;
            fm.TickForTest();
            fm.TickForTest();
            Assert.AreEqual(PerformanceTier.Critical, fm.ThermalTierForTest,
                "Cooling must dwell; it cannot snap Critical->Nominal in fewer passes than the dwell.");

            fm.TickForTest(); // third cool pass completes the dwell
            Assert.AreEqual(PerformanceTier.Aggressive, fm.ThermalTierForTest,
                "After the cooldown dwell the thermal tier steps down exactly one level.");

            Object.Destroy(fm.gameObject);
            Object.Destroy(registry);
        }

        [UnityTest]
        public IEnumerator Thermal_Reheat_DuringCooldown_JumpsBackImmediately()
        {
            var fm = NewManager(out var registry);
            yield return null;
            fm.ConfigureConnectivityProvider(new FakeConnectivity { Reachable = true });
            var thermal = new FakeThermal { Available = true, Level = ThermalWarning.Throttling, Temperature = 1f };
            fm.ConfigureThermalProvider(thermal);

            fm.TickForTest(); // Critical
            thermal.Level = ThermalWarning.None; thermal.Temperature = 0f;
            fm.TickForTest();
            fm.TickForTest(); // 2 cool passes, not yet the full dwell

            thermal.Level = ThermalWarning.Throttling; thermal.Temperature = 1f;
            fm.TickForTest();
            Assert.AreEqual(PerformanceTier.Critical, fm.ThermalTierForTest,
                "A re-heat mid-cooldown jumps straight back to the hot tier (safety-first).");

            Object.Destroy(fm.gameObject);
            Object.Destroy(registry);
        }

        // ----- Cross-axis reconciliation: published tier = max(fps, thermal) ------------------

        [UnityTest]
        public IEnumerator Reconciliation_HotDeviceGoodFps_DegradesViaThermal()
        {
            var fm = NewManager(out var registry);
            yield return null;
            fm.ConfigureConnectivityProvider(new FakeConnectivity { Reachable = true });
            fm.ConfigureThermalProvider(new FakeThermal
            {
                Available = true, Level = ThermalWarning.Throttling, Temperature = 1f
            });

            fm.PrimeFramerateForTest(60f); // great FPS
            fm.TickForTest();

            Assert.AreEqual(PerformanceTier.Nominal, fm.FpsTierForTest, "FPS tier stays Nominal at 60.");
            Assert.AreEqual(PerformanceTier.Critical, fm.CurrentTier,
                "max(FPS Nominal, thermal Critical) = Critical: a hot device degrades despite good FPS.");

            Object.Destroy(fm.gameObject);
            Object.Destroy(registry);
        }

        [UnityTest]
        public IEnumerator Reconciliation_CoolDeviceBadFps_DegradesViaFramerate()
        {
            var fm = NewManager(out var registry);
            yield return null;
            fm.ConfigureConnectivityProvider(new FakeConnectivity { Reachable = true });
            fm.ConfigureThermalProvider(new FakeThermal
            {
                Available = true, Level = ThermalWarning.None, Temperature = 0f
            });

            fm.PrimeFramerateForTest(20f);
            fm.TickForTest(); fm.TickForTest(); fm.TickForTest(); // 3s sustained low FPS

            Assert.AreEqual(PerformanceTier.Nominal, fm.ThermalTierForTest, "Thermal tier stays Nominal while cool.");
            Assert.AreEqual(PerformanceTier.Reduced, fm.CurrentTier,
                "max(FPS Reduced, thermal Nominal) = Reduced: a cool device still degrades on bad FPS.");

            Object.Destroy(fm.gameObject);
            Object.Destroy(registry);
        }

        [UnityTest]
        public IEnumerator UnimplementedStubs_CauseNoTransition_WhenConnectivityStable()
        {
            var fm = NewManager(out var registry);
            yield return null;

            // Reachable + cool + good FPS throughout: every LIVE signal is in its no-op range, so any
            // state/tier change could only come from a stub check (AR/API/inference). None should.
            fm.ConfigureConnectivityProvider(new FakeConnectivity { Reachable = true });
            fm.ConfigureThermalProvider(new FakeThermal
            {
                Available = true, Level = ThermalWarning.None, Temperature = 0f
            });
            fm.PrimeFramerateForTest(60f);

            var stateEvents = 0;
            var tierEvents = 0;
            fm.OnAppStateChanged += _ => stateEvents++;
            fm.OnPerformanceTierChanged += _ => tierEvents++;

            for (int i = 0; i < 5; i++)
            {
                fm.TickForTest();
            }

            Assert.AreEqual(AppState.AR_VIEWER_MODE, fm.CurrentState);
            Assert.AreEqual(PerformanceTier.Nominal, fm.CurrentTier);
            Assert.AreEqual(0, stateEvents, "No AppState event should fire from inert stubs.");
            Assert.AreEqual(0, tierEvents, "No tier event should fire from inert stubs.");

            Object.Destroy(fm.gameObject);
            Object.Destroy(registry);
        }
    }
}
