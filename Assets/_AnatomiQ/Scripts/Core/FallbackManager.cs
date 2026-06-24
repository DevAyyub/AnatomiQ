using System;
using System.Collections;
using UnityEngine;

namespace AnatomiQ.Core
{
    /// <summary>
    /// CORE-007 — Fallback &amp; State Manager. The always-on authority that owns the global
    /// <see cref="AppState"/> (AR context), <see cref="PerformanceTier"/> (quality), and
    /// <see cref="Connectivity"/> (network). It has no fallback of its own; it IS the fallback
    /// system, and is initialized before any other service (see <see cref="DefaultExecutionOrder"/>).
    ///
    /// Each global signal is driven by one or more monitoring checks on the <see cref="MonitorLoop"/>
    /// cadence. <see cref="AppState"/> has a single writer, <see cref="CheckArTracking"/> (CORE-001),
    /// which pulls AR status from the registered <see cref="IArTrackingProvider"/> and maps it.
    /// <see cref="Connectivity"/> is driven by <see cref="CheckConnectivity"/> (debounced).
    /// <see cref="PerformanceTier"/> is the more severe of the FPS and thermal sub-tiers.
    /// <see cref="CheckInferenceState"/> and <see cref="CheckApiAvailability"/> remain documented
    /// stubs until their sources exist (CORE-006 / on-device inference).
    /// </summary>
    [DefaultExecutionOrder(-1000)] // Runs first so it registers before all other services.
    public sealed class FallbackManager : MonoBehaviour, IFallbackManager
    {
        [Header("Service wiring")]
        [Tooltip("The single ServiceRegistry asset. Assign in the inspector.")]
        [SerializeField] private ServiceRegistry _services;

        [Header("Initial state")]
        [Tooltip("Safe baseline before monitoring promotes/demotes. The 3D viewer always works.")]
        [SerializeField] private AppState _initialState = AppState.AR_VIEWER_MODE;

        [Header("Monitoring")]
        [Tooltip("Seconds between monitoring passes. The logic phase reads the monitored signals here.")]
        [SerializeField] private float _monitorIntervalSeconds = 1f;

        [Header("Frame rate")]
        [Tooltip("Frame rate requested at startup via Application.targetFrameRate. Android's default is " +
                 "~30, so this makes the target an explicit decision. 60 = ask the device to run smooth; " +
                 "if AR pins to ~30 on device (ARCore camera cap), set this to 30 and the FPS tiers " +
                 "recalibrate automatically (thresholds are fractions of this value). Requires VSync OFF " +
                 "(Quality ▸ VSync Count = Don't Sync), which this project uses.")]
        [SerializeField] private int _targetFrameRate = 60;

        private AppState _currentState;
        private PerformanceTier _currentTier = PerformanceTier.Nominal;
        private Coroutine _monitorRoutine;

        // --- Signal providers (testability seams). Default to real Unity-backed implementations;
        //     PlayMode tests inject fakes via the internal Configure* hooks below. ---
        private IConnectivityProvider _connectivity = new UnityConnectivityProvider();
        private IFrameClock _frameClock = new UnityFrameClock();
        // Thermal source. The real Adaptive Performance adapter is activated on the CORE-001 device
        // pass by adding the ANATOMIQ_ADAPTIVE_PERFORMANCE scripting define (Project Settings ▸ Player
        // ▸ Android), after installing + enabling the AP Android (Google) provider. Until the define is
        // set, the inert NullThermalProvider keeps CheckThermal from ever moving the thermal tier
        // (editor / PlayMode have no thermal source). AdaptivePerformanceThermalProvider is itself
        // guarded by the same define, so this reference compiles in both configurations.
#if ANATOMIQ_ADAPTIVE_PERFORMANCE
        private IThermalProvider _thermal = new AdaptivePerformanceThermalProvider();
#else
        private IThermalProvider _thermal = new NullThermalProvider();
#endif

        // Memory footprint source. Unlike thermal there's no "unavailable" state to guard — the
        // Profiler memory API is always present (it returns -1 via the probe if it can't read).
        // RAM is sampled for the A.12 overlay only; it does NOT drive a tier this chunk (RAM-driven
        // degradation is tied to the still-deferred mesh/LOD lever).
        private IMemoryProbe _memoryProbe = new UnityMemoryProbe();

        // Connectivity debounce: require N consecutive agreeing samples before flipping the
        // Connectivity signal, so a single dropped poll can't toggle it. 2 = current + one
        // confirmation.
        private const int CONNECTIVITY_DEBOUNCE_SAMPLES = 2;
        private int _offlineStreak;
        private int _onlineStreak;

        // Current network reachability (Axis 3). Lifted off AppState at CORE-001 so AR context and
        // connectivity never mask each other. Starts Online (the non-offline startup assumption);
        // CheckConnectivity debounces before flipping it.
        private Connectivity _connectivityState = Connectivity.Online;

        // AR tracking source (CORE-001). The RUNTIME source is the registered IArTrackingProvider,
        // pulled from the ServiceRegistry inside CheckArTracking. This field is a TEST-ONLY override
        // (set via ConfigureArTrackingProvider) that takes precedence when present, mirroring how the
        // other Configure* seams swap their sources; null means "use the registry".
        private IArTrackingProvider _arTrackingTestOverride;

        // --- Framerate (Axis 2 / FPS sub-tier) -------------------------------------------------
        // Two-layer design: a frame ring buffer SMOOTHS instantaneous FPS (kills per-frame jitter),
        // and separate sustain timers track how long the smoothed value has stayed past a threshold
        // before we act. Thresholds + dwell come from the Performance doc (A.3/A.54) and the global
        // 30fps floor in the Fallback Rules.
        private const int FPS_SAMPLE_WINDOW = 30;          // frames smoothed into the rolling average

        // FPS sub-tier thresholds are FRACTIONS of the requested target frame rate, not absolute
        // numbers. This is the fix for the CORE-001 calibration bug: when the demote threshold was a
        // hard 30 and the device's real cap was also 30 (ARCore camera), the rolling average sat a
        // hair under (29.9) and false-degraded Nominal→Critical forever. Tying the thresholds to the
        // target means a 30-capped device targeting 30 reads as "on target" (29.9 > 0.75*30 = 22.5),
        // while a device targeting 60 that only manages 30 correctly reads as "missing target".
        private const float FPS_DEMOTE_FRACTION = 0.75f;   // sustained below 75% of target = struggling
        private const float FPS_PROMOTE_FRACTION = 0.87f;  // must clear 87% of target to recover (hysteresis)
        private const float FPS_DEMOTE_SUSTAIN_SECONDS = 3f; // sustained-below time before stepping down
        private const float FPS_PROMOTE_SUSTAIN_SECONDS = 5f; // longer recovery dwell, anti-flap

        // Absolute thresholds derived from the active target in Awake (after Application.targetFrameRate
        // is set). Computed once; an uncapped/0 target would break the math, so the basis floors at 60.
        private float _fpsDemoteThreshold;
        private float _fpsPromoteThreshold;

        private readonly float[] _frameDurations = new float[FPS_SAMPLE_WINDOW];
        private int _frameWriteIndex;
        private int _frameSampleCount;          // ramps to FPS_SAMPLE_WINDOW, then stays full
        private float _frameDurationSum;        // running sum of the buffer, for O(1) average

        private float _belowThresholdSeconds;   // accumulated time rolling FPS < demote threshold
        private float _aboveThresholdSeconds;   // accumulated time rolling FPS > promote threshold
        private float _lastFpsEvalTime;         // timestamp of previous CheckFramerate evaluation

        // Per-axis tiers; published CurrentTier is the more severe (max) of the two.
        private PerformanceTier _fpsTier = PerformanceTier.Nominal;
        private PerformanceTier _thermalTier = PerformanceTier.Nominal;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Chunk-6 verification aid: when set, pins the published tier regardless of the FPS/thermal
        // sub-tiers so CORE-002's URP levers can be exercised on device without waiting for real
        // throttling. Honored inside ReconcileTier; stripped entirely from release builds.
        private PerformanceTier? _debugTierOverride;
#endif

        // --- Thermal (Axis 2 / thermal sub-tier) -----------------------------------------------
        // The TemperatureLevel that splits ThrottlingImminent into Moderate (Reduced) vs Severe
        // (Aggressive), per Unity's own Adaptive Performance guidance and the Performance doc A.10.
        private const float THERMAL_SEVERE_TEMPERATURE = 0.8f;
        // Cooling dwell: how many consecutive cooler passes before stepping the thermal tier DOWN.
        // Prevents a brief cool reading from yanking quality back up mid-cascade. Heating is applied
        // immediately (safety: shed load as soon as the device reports it's hot).
        private const int THERMAL_COOLDOWN_DWELL = 3;
        private int _thermalCooldownStreak;

        // --- Localization keys for the thermal user messages (UIStrings table). DEFINED here as the
        //     contract; CORE-007 does NOT display them. A UI consumer subscribes to
        //     OnPerformanceTierChanged and shows the matching string at Aggressive/Critical. Keeping
        //     CORE-007 signal-only (no presentation) mirrors the deferred debug-overlay decision.
        //     The English VALUES live in the UIStrings table, authored by the Editor tool
        //     AnatomiQ.Editor.Localization.ThermalStringsInstaller — if these key strings change,
        //     update the matching literals in that tool too. ---
        /// <summary>UIStrings key shown when the device is getting warm (tier reaches Aggressive).</summary>
        public const string THERMAL_WARNING_STRING_KEY = "ui.system.thermal.warning";
        /// <summary>UIStrings key for the "taking a quick break" overlay (tier reaches Critical).</summary>
        public const string THERMAL_CRITICAL_STRING_KEY = "ui.system.thermal.critical";

        // Latest per-signal readings, surfaced through Metrics for the A.12 overlay/logging.
        // Populated by the signal checks during the logic phase; defaults mean "not yet sampled".
        private float _rollingFps;
        private float _temperatureLevel = -1f; // -1 = no thermal source active (editor / pre-CORE-001)
        private float _ramMegabytes = -1f;     // -1 = not wired until CORE-002

        /// <inheritdoc />
        public AppState CurrentState => _currentState;

        /// <inheritdoc />
        public event Action<AppState> OnAppStateChanged;

        /// <inheritdoc />
        public PerformanceTier CurrentTier => _currentTier;

        /// <inheritdoc />
        public event Action<PerformanceTier> OnPerformanceTierChanged;

        /// <inheritdoc />
        public Connectivity CurrentConnectivity => _connectivityState;

        /// <inheritdoc />
        public event Action<Connectivity> OnConnectivityChanged;

        /// <inheritdoc />
        public PerformanceMetrics Metrics =>
            new PerformanceMetrics(_rollingFps, _currentTier, _temperatureLevel, _ramMegabytes);

        private void Awake()
        {
            _currentState = _initialState;

            // Make the frame-rate target an explicit decision rather than the Android default (~30).
            // VSync is off in the Quality settings, so this is honored. The FPS tier thresholds are
            // derived from the SAME basis, so they stay correct whatever target is chosen.
            Application.targetFrameRate = _targetFrameRate;
            float fpsBasis = _targetFrameRate > 0 ? _targetFrameRate : 60f;
            _fpsDemoteThreshold = fpsBasis * FPS_DEMOTE_FRACTION;
            _fpsPromoteThreshold = fpsBasis * FPS_PROMOTE_FRACTION;

            if (_services == null)
            {
                Debug.LogError("[FallbackManager] ServiceRegistry not assigned in the inspector.");
                return;
            }

            // Self-register first (DefaultExecutionOrder guarantees this Awake runs before others).
            _services.Register(this);
        }

        private void OnEnable()
        {
            _monitorRoutine = StartCoroutine(MonitorLoop());
        }

        private void OnDisable()
        {
            if (_monitorRoutine != null)
            {
                StopCoroutine(_monitorRoutine);
                _monitorRoutine = null;
            }
        }

        /// <summary>
        /// Per-frame FPS collection. Runs every frame (NOT on the 1s monitor cadence) because a
        /// 30-frame rolling average is meaningless if sampled once per second. Accumulates each
        /// frame's duration into a ring buffer; <see cref="CheckFramerate"/> later reads the smoothed
        /// average on the 1s tick. Collection here is O(1) and allocation-free.
        /// </summary>
        private void Update()
        {
            RecordFrame(_frameClock.UnscaledDeltaTime);
        }

        /// <summary>
        /// Pushes one frame duration into the ring buffer, maintaining a running sum so the rolling
        /// average is O(1). Ignores non-positive durations (e.g. a paused or first frame) so they
        /// can't poison the average with a divide-by-zero FPS.
        /// </summary>
        /// <param name="frameDuration">Seconds the frame took (unscaled).</param>
        private void RecordFrame(float frameDuration)
        {
            if (frameDuration <= 0f)
            {
                return;
            }

            // Subtract the slot we're about to overwrite, add the new sample, advance the ring.
            _frameDurationSum -= _frameDurations[_frameWriteIndex];
            _frameDurations[_frameWriteIndex] = frameDuration;
            _frameDurationSum += frameDuration;

            _frameWriteIndex = (_frameWriteIndex + 1) % FPS_SAMPLE_WINDOW;
            if (_frameSampleCount < FPS_SAMPLE_WINDOW)
            {
                _frameSampleCount++;
            }
        }

        /// <summary>
        /// Rolling average FPS over the populated portion of the ring buffer, or 0 before any frame
        /// has been recorded. Average frame duration → FPS via reciprocal.
        /// </summary>
        private float RollingFps
        {
            get
            {
                if (_frameSampleCount == 0 || _frameDurationSum <= 0f)
                {
                    return 0f;
                }

                float averageDuration = _frameDurationSum / _frameSampleCount;
                return 1f / averageDuration;
            }
        }


        /// <summary>
        /// Monitoring loop SCAFFOLD. Runs forever at <see cref="_monitorIntervalSeconds"/> and calls
        /// each signal check. The checks are empty at scaffold time — NO thresholds, NO transitions.
        /// </summary>
        private IEnumerator MonitorLoop()
        {
            var wait = new WaitForSeconds(_monitorIntervalSeconds);
            while (true)
            {
                MonitorPass();
                yield return wait;
            }
        }

        /// <summary>One monitoring pass — invokes every signal check. No logic yet.</summary>
        private void MonitorPass()
        {
            CheckArTracking();
            CheckConnectivity();
            CheckThermal();
            CheckFramerate();
            CheckRam();
            CheckInferenceState();
            CheckApiAvailability();
        }

        // --- Signal checks. CheckArTracking / CheckConnectivity / CheckThermal / CheckFramerate are
        //     implemented. CheckInferenceState / CheckApiAvailability remain documented stubs until
        //     their sources exist (CORE-006 / on-device inference). ---

        /// <summary>
        /// AR tracking signal → <see cref="AppState"/> (CORE-001). PULLS the current
        /// <see cref="ArTrackingStatus"/> from the registered <see cref="IArTrackingProvider"/> (or a
        /// test override) and maps it via <see cref="MapArState"/>. A missing provider (no AR scene
        /// active, or no ARCore) resolves to <see cref="ArTrackingStatus.Unavailable"/> →
        /// <see cref="AppState.AR_VIEWER_MODE"/>, the safe baseline — so this is NOT inert when the
        /// source is absent (unlike thermal): "no AR" legitimately means viewer mode.
        ///
        /// This is the SOLE writer of <see cref="AppState"/>. Connectivity and thermal/FPS drive their
        /// own signals, so there is no cross-signal contention here (the reason connectivity was lifted
        /// off the AppState axis at CORE-001). Sampling latency is the ~1s monitor cadence; the instant
        /// visual reaction to tracking loss is CORE-001's own change-only event, not this path.
        /// </summary>
        private void CheckArTracking()
        {
            IArTrackingProvider provider = _arTrackingTestOverride ?? _services?.ArTrackingProvider;
            ArTrackingStatus status = provider?.Status ?? ArTrackingStatus.Unavailable;
            SetState(MapArState(status));
        }

        /// <summary>
        /// Pure mapping from AR tracking status to global <see cref="AppState"/>. Static and
        /// side-effect-free so it's trivially unit-testable in isolation.
        /// </summary>
        private static AppState MapArState(ArTrackingStatus status)
        {
            switch (status)
            {
                case ArTrackingStatus.Tracking:
                    return AppState.AR_ACTIVE;
                case ArTrackingStatus.Limited:
                case ArTrackingStatus.NotTracking:
                    return AppState.AR_LIMITED;
                default: // Unavailable, PermissionDenied, Initializing
                    return AppState.AR_VIEWER_MODE;
            }
        }

        /// <summary>
        /// Connectivity signal → <see cref="Connectivity"/> (Axis 3). Reads reachability through
        /// <see cref="_connectivity"/> and debounces by <see cref="CONNECTIVITY_DEBOUNCE_SAMPLES"/>
        /// consecutive agreeing samples so a single dropped poll cannot flip the signal.
        ///
        /// CORE-001: this no longer touches <see cref="AppState"/>. Connectivity is its own axis, so
        /// going offline cannot mask AR context (and AR context cannot mask offline). The debounce is
        /// unchanged from the logic-phase implementation; only the target signal moved.
        /// <see cref="SetConnectivity"/>'s equality guard means staying offline/online never re-fires.
        /// </summary>
        private void CheckConnectivity()
        {
            bool reachable = _connectivity.IsReachable;

            if (reachable)
            {
                _onlineStreak++;
                _offlineStreak = 0;
            }
            else
            {
                _offlineStreak++;
                _onlineStreak = 0;
            }

            if (!reachable && _offlineStreak >= CONNECTIVITY_DEBOUNCE_SAMPLES)
            {
                SetConnectivity(Connectivity.Offline);
            }
            else if (reachable && _onlineStreak >= CONNECTIVITY_DEBOUNCE_SAMPLES)
            {
                SetConnectivity(Connectivity.Online);
            }
        }

        /// <summary>
        /// Thermal signal → thermal sub-tier. Reads <see cref="_thermal"/> and maps the device
        /// warning level (split by <see cref="THERMAL_SEVERE_TEMPERATURE"/>) onto a tier:
        /// <list type="bullet">
        /// <item>None → Nominal.</item>
        /// <item>ThrottlingImminent, temp ≤ 0.8 → Reduced (Moderate stage).</item>
        /// <item>ThrottlingImminent, temp &gt; 0.8 → Aggressive (Severe stage; UI shows the warning).</item>
        /// <item>Throttling → Critical (UI shows the break overlay).</item>
        /// </list>
        /// Hysteresis is ASYMMETRIC on purpose. Heating applies IMMEDIATELY — if the device reports a
        /// hotter level we jump straight to that (more severe) tier, because shedding load late risks
        /// the OS throttling or killing the app. Cooling is gated by
        /// <see cref="THERMAL_COOLDOWN_DWELL"/> consecutive cooler passes and steps DOWN only one
        /// level at a time, so a momentary cool reading can't snap quality back up mid-cascade.
        ///
        /// Inert until the real provider exists: <see cref="NullThermalProvider"/> reports
        /// <see cref="IThermalProvider.IsAvailable"/> = false, so this returns before touching the
        /// tier (Decision B — concrete provider wired at CORE-001).
        /// </summary>
        private void CheckThermal()
        {
            if (!_thermal.IsAvailable)
            {
                _temperatureLevel = -1f; // surface "no source" to the A.12 metrics snapshot
                return;
            }

            _temperatureLevel = _thermal.TemperatureLevel;
            PerformanceTier target = MapThermalTier(_thermal.Warning, _thermal.TemperatureLevel);

            if (target > _thermalTier)
            {
                // Heating: respond immediately, jump to the hotter tier.
                _thermalTier = target;
                _thermalCooldownStreak = 0;
                ReconcileTier();
            }
            else if (target < _thermalTier)
            {
                // Cooling: require sustained cooler readings, then step down ONE level.
                _thermalCooldownStreak++;
                if (_thermalCooldownStreak >= THERMAL_COOLDOWN_DWELL)
                {
                    _thermalTier = (PerformanceTier)((int)_thermalTier - 1);
                    _thermalCooldownStreak = 0;
                    ReconcileTier();
                }
            }
            else
            {
                // Holding at the current tier: reset the cooldown streak.
                _thermalCooldownStreak = 0;
            }
        }

        /// <summary>
        /// Pure mapping from a thermal reading to its sub-tier. Static and side-effect-free so it's
        /// trivially unit-testable in isolation.
        /// </summary>
        private static PerformanceTier MapThermalTier(ThermalWarning warning, float temperatureLevel)
        {
            switch (warning)
            {
                case ThermalWarning.Throttling:
                    return PerformanceTier.Critical;
                case ThermalWarning.ThrottlingImminent:
                    return temperatureLevel > THERMAL_SEVERE_TEMPERATURE
                        ? PerformanceTier.Aggressive
                        : PerformanceTier.Reduced;
                default:
                    return PerformanceTier.Nominal;
            }
        }

        /// <summary>
        /// Framerate signal → FPS sub-tier. Reads the smoothed <see cref="RollingFps"/> and applies
        /// sustained-duration logic with hysteresis so it doesn't flap at the 30 FPS boundary:
        /// <list type="bullet">
        /// <item>Below the demote threshold (<see cref="FPS_DEMOTE_FRACTION"/> of target) for
        /// <see cref="FPS_DEMOTE_SUSTAIN_SECONDS"/> → step the FPS tier UP one level (more severe).</item>
        /// <item>Above the promote threshold (<see cref="FPS_PROMOTE_FRACTION"/> of target) for
        /// <see cref="FPS_PROMOTE_SUSTAIN_SECONDS"/> → step the FPS tier DOWN one level (recover).</item>
        /// </list>
        /// The separate enter (30) / exit (40) thresholds plus the longer recovery dwell are the
        /// anti-flap mechanism. Stepwise (one level per qualifying interval), never a jump. The
        /// published tier is reconciled against the thermal sub-tier in <see cref="ReconcileTier"/>.
        ///
        /// Until the buffer has filled at least once we skip evaluation, so a cold start (0 FPS)
        /// can't instantly demote before any real frames are measured.
        /// </summary>
        private void CheckFramerate()
        {
            // Surface the current smoothed value for the A.12 metrics snapshot regardless.
            _rollingFps = RollingFps;

            if (_frameSampleCount < FPS_SAMPLE_WINDOW)
            {
                return; // not enough frames yet to trust the average
            }

            float elapsed = _monitorIntervalSeconds; // nominal step; tests can override via TickForTest

            if (_rollingFps < _fpsDemoteThreshold)
            {
                _belowThresholdSeconds += elapsed;
                _aboveThresholdSeconds = 0f;
            }
            else if (_rollingFps > _fpsPromoteThreshold)
            {
                _aboveThresholdSeconds += elapsed;
                _belowThresholdSeconds = 0f;
            }
            else
            {
                // In the hysteresis dead-band (between the demote and promote thresholds): hold
                // steady, decay both timers.
                _belowThresholdSeconds = 0f;
                _aboveThresholdSeconds = 0f;
            }

            if (_belowThresholdSeconds >= FPS_DEMOTE_SUSTAIN_SECONDS
                && _fpsTier < PerformanceTier.Critical)
            {
                _fpsTier = (PerformanceTier)((int)_fpsTier + 1);
                _belowThresholdSeconds = 0f; // reset so each step needs a fresh sustained window
                ReconcileTier();
            }
            else if (_aboveThresholdSeconds >= FPS_PROMOTE_SUSTAIN_SECONDS
                     && _fpsTier > PerformanceTier.Nominal)
            {
                _fpsTier = (PerformanceTier)((int)_fpsTier - 1);
                _aboveThresholdSeconds = 0f;
                ReconcileTier();
            }
        }

        /// <summary>
        /// Memory signal → the A.12 metrics snapshot. Pure sample-and-surface: reads the current
        /// footprint from <see cref="_memoryProbe"/> into <see cref="_ramMegabytes"/> so
        /// <see cref="Metrics"/> can report MB vs the A.4 1400 MB ceiling. Deliberately does NOT
        /// reconcile a tier — RAM-driven degradation lands with the deferred mesh/LOD lever, so for
        /// now this is display-only (decision F, scoped at CORE-002 chunk 5).
        /// </summary>
        private void CheckRam()
        {
            _ramMegabytes = _memoryProbe.SampleMegabytes();
        }

        /// <summary>TODO (logic phase): Inference Engine model load state → disable dependent features.</summary>
        private void CheckInferenceState() { }

        /// <summary>TODO (logic phase): AI Orchestrator API health → cached-fallback messaging.</summary>
        private void CheckApiAvailability() { }

        /// <summary>
        /// Sets the global AR-context state and raises <see cref="OnAppStateChanged"/> when it
        /// changes. This is the state-change MECHANISM (infrastructure), not decision logic; its sole
        /// caller is <see cref="CheckArTracking"/> (CORE-001). The equality guard makes the broadcast
        /// change-only — re-asserting the same status each tick fires no event.
        /// </summary>
        /// <param name="next">The state to transition to.</param>
        private void SetState(AppState next)
        {
            if (next == _currentState)
            {
                return;
            }

            _currentState = next;
            OnAppStateChanged?.Invoke(_currentState);
        }

        /// <summary>
        /// Sets the network connectivity signal and raises <see cref="OnConnectivityChanged"/> when it
        /// changes. Mirror of <see cref="SetState"/> for Axis 3 — the change MECHANISM, not the
        /// decision logic. Called by <see cref="CheckConnectivity"/> after its debounce. The equality
        /// guard means staying offline (or online) never re-fires the event.
        /// </summary>
        /// <param name="next">The connectivity value to transition to.</param>
        private void SetConnectivity(Connectivity next)
        {
            if (next == _connectivityState)
            {
                return;
            }

            _connectivityState = next;
            OnConnectivityChanged?.Invoke(_connectivityState);
        }

        /// <summary>
        /// Sets the global quality tier and raises <see cref="OnPerformanceTierChanged"/> when it
        /// changes. Mirror of <see cref="SetState"/> for Axis 2 — the tier-change MECHANISM, not the
        /// decision logic. The FPS and thermal checks call this once they have reconciled their tiers.
        /// </summary>
        /// <param name="next">The tier to transition to.</param>
        private void SetTier(PerformanceTier next)
        {
            if (next == _currentTier)
            {
                return;
            }

            _currentTier = next;
            OnPerformanceTierChanged?.Invoke(_currentTier);
        }

        /// <summary>
        /// Publishes the more severe of the two independent quality sub-tiers (FPS and thermal) as
        /// the single <see cref="CurrentTier"/>. A hot device at good FPS still degrades, and a
        /// struggling-FPS device that's cool still degrades — whichever is worse wins. Called by the
        /// FPS and thermal checks whenever their sub-tier changes.
        /// </summary>
        private void ReconcileTier()
        {
            var worst = (PerformanceTier)Math.Max((int)_fpsTier, (int)_thermalTier);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // A forced tier wins over reconciliation so a real (cool/good-FPS) pass can't un-pin it.
            if (_debugTierOverride.HasValue)
            {
                worst = _debugTierOverride.Value;
            }
#endif
            SetTier(worst);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// DEV-ONLY (editor + development builds): pin the published <see cref="CurrentTier"/> to a
        /// forced value, or pass <c>null</c> to return to automatic FPS/thermal reconciliation. A
        /// chunk-6 aid for watching CORE-002's URP levers engage without real throttling. The forced
        /// tier is published immediately and survives subsequent monitor passes until cleared.
        /// Stripped from release builds.
        /// </summary>
        /// <param name="tier">The tier to pin, or <c>null</c> to resume automatic reconciliation.</param>
        public void DebugSetTierOverride(PerformanceTier? tier)
        {
            _debugTierOverride = tier;
            ReconcileTier();
        }
#endif

        // --- Test seams ------------------------------------------------------------------------
        // internal (not public) so production code can't reach them, but the PlayMode test assembly
        // can via [assembly: InternalsVisibleTo("AnatomiQ.Tests.PlayMode")]. They let tests drive the
        // signal sources deterministically without a device/network.

        /// <summary>Test-only: replace the connectivity source. No-op guard against null.</summary>
        internal void ConfigureConnectivityProvider(IConnectivityProvider provider)
        {
            if (provider != null)
            {
                _connectivity = provider;
            }
        }

        /// <summary>Test-only: replace the frame clock source. No-op guard against null.</summary>
        internal void ConfigureFrameClock(IFrameClock clock)
        {
            if (clock != null)
            {
                _frameClock = clock;
            }
        }

        /// <summary>Test-only: replace the thermal source. No-op guard against null.</summary>
        internal void ConfigureThermalProvider(IThermalProvider provider)
        {
            if (provider != null)
            {
                _thermal = provider;
            }
        }

        /// <summary>Test-only: replace the memory source. No-op guard against null.</summary>
        internal void ConfigureMemoryProbe(IMemoryProbe probe)
        {
            if (probe != null)
            {
                _memoryProbe = probe;
            }
        }

        /// <summary>
        /// Test-only: set the AR tracking source override that <see cref="CheckArTracking"/> prefers
        /// over the registry-provided one. Unlike the other Configure* seams, null is ALLOWED here and
        /// CLEARS the override (so a test can fall back to the registry path).
        /// </summary>
        internal void ConfigureArTrackingProvider(IArTrackingProvider provider)
        {
            _arTrackingTestOverride = provider;
        }

        /// <summary>Test-only: read the thermal sub-tier in isolation (before reconciliation).</summary>
        internal PerformanceTier ThermalTierForTest => _thermalTier;

        /// <summary>
        /// Test-only: fill the FPS ring buffer to a steady target so <see cref="CheckFramerate"/> has
        /// a trustworthy average immediately, without running real frames. Records exactly one full
        /// window of identical frame durations.
        /// </summary>
        /// <param name="targetFps">The steady FPS the buffer should report.</param>
        internal void PrimeFramerateForTest(float targetFps)
        {
            float duration = 1f / targetFps;
            for (int i = 0; i < FPS_SAMPLE_WINDOW; i++)
            {
                RecordFrame(duration);
            }
        }

        /// <summary>Test-only: record a single frame duration (drives the ring buffer directly).</summary>
        internal void RecordFrameForTest(float frameDuration) => RecordFrame(frameDuration);

        /// <summary>Test-only: run a single monitoring pass synchronously (bypasses the 1s coroutine).</summary>
        internal void TickForTest() => MonitorPass();

        /// <summary>Test-only: read the FPS sub-tier in isolation (before thermal reconciliation).</summary>
        internal PerformanceTier FpsTierForTest => _fpsTier;
    }
}
