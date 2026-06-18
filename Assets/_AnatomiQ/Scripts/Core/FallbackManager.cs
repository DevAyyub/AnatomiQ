using System;
using System.Collections;
using UnityEngine;

namespace AnatomiQ.Core
{
    /// <summary>
    /// CORE-007 — Fallback &amp; State Manager. The always-on authority that owns the global
    /// <see cref="AppState"/>. It has no fallback of its own; it IS the fallback system, and is
    /// initialized before any other service (see <see cref="DefaultExecutionOrder"/> below).
    ///
    /// SHELL ONLY (Section 6): this class holds the state and the monitoring-loop SCAFFOLD. It
    /// contains NO decision logic — no thresholds, no transitions. Each signal check is an empty
    /// stub documenting what the CORE-007 logic phase will implement, at which point those checks
    /// will call <see cref="SetState"/> to drive <see cref="AppState"/>.
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

        private AppState _currentState;
        private Coroutine _monitorRoutine;

        /// <inheritdoc />
        public AppState CurrentState => _currentState;

        /// <inheritdoc />
        public event Action<AppState> OnAppStateChanged;

        private void Awake()
        {
            _currentState = _initialState;

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
            CheckInferenceState();
            CheckApiAvailability();
        }

        // --- Signal checks: SCAFFOLD ONLY. The logic phase fills these in; each will read its
        //     source and call SetState(...) when a threshold is crossed. They do nothing now. ---

        /// <summary>TODO (logic phase): ARCore tracking state → AR_ACTIVE / AR_LIMITED / AR_VIEWER_MODE.</summary>
        private void CheckArTracking() { }

        /// <summary>TODO (logic phase): Application.internetReachability → OFFLINE_MODE toggling.</summary>
        private void CheckConnectivity() { }

        /// <summary>TODO (logic phase): device thermal state → progressive quality reduction.</summary>
        private void CheckThermal() { }

        /// <summary>TODO (logic phase): rolling FPS average → LOD/quality reduction below 30fps.</summary>
        private void CheckFramerate() { }

        /// <summary>TODO (logic phase): Inference Engine model load state → disable dependent features.</summary>
        private void CheckInferenceState() { }

        /// <summary>TODO (logic phase): AI Orchestrator API health → cached-fallback messaging.</summary>
        private void CheckApiAvailability() { }

        /// <summary>
        /// Sets the global state and raises <see cref="OnAppStateChanged"/> when it changes. This is
        /// the state-change MECHANISM (infrastructure), not decision logic. It is intentionally not
        /// invoked yet; the logic phase calls it from the signal checks above.
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
    }
}
