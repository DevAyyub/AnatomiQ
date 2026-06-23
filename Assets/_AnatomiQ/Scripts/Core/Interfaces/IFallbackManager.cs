using System;

namespace AnatomiQ.Core
{
    /// <summary>
    /// Contract for CORE-007. Owns and publishes the three orthogonal global signals other systems
    /// read: the AR-context <see cref="AppState"/>, the quality/degradation
    /// <see cref="PerformanceTier"/>, and network <see cref="Connectivity"/>. It is the single source
    /// of truth for all three.
    ///
    /// The signals are deliberately separate. AppState answers "what AR mode is the app in?"
    /// (AR active, viewer-only, AR-limited). PerformanceTier answers "how much quality must the
    /// renderer shed to stay in budget?" Connectivity answers "is there a network?" Keeping them
    /// apart means none can mask another: a device can be <see cref="AppState.AR_ACTIVE"/> while
    /// <see cref="Connectivity.Offline"/> and at <see cref="PerformanceTier.Reduced"/>, all reported
    /// truthfully at once. (CORE-001 lifted connectivity off the AppState axis to complete this
    /// separation — see AnatomiQ_Build_Environment.md Part D and the CORE-007 / CORE-001 decision log.)
    /// </summary>
    public interface IFallbackManager : IService
    {
        // --- Axis 1: AR / connectivity context -------------------------------------------------

        /// <summary>The current global application state.</summary>
        AppState CurrentState { get; }

        /// <summary>Raised whenever <see cref="CurrentState"/> changes.</summary>
        event Action<AppState> OnAppStateChanged;

        // --- Axis 2: quality / degradation severity --------------------------------------------

        /// <summary>
        /// The current quality tier, driven by the rolling FPS average and device thermal state.
        /// Consumed by CORE-002 to drive URP degradation levers (render scale, LOD, shadows,
        /// post-processing, inference frequency).
        /// </summary>
        PerformanceTier CurrentTier { get; }

        /// <summary>
        /// Raised whenever <see cref="CurrentTier"/> changes. No consumer exists yet at CORE-007
        /// logic-phase time; CORE-002 subscribes when the renderer lands. Published regardless —
        /// the contract is real now.
        /// </summary>
        event Action<PerformanceTier> OnPerformanceTierChanged;

        // --- Axis 3: network connectivity ------------------------------------------------------

        /// <summary>
        /// Current network reachability. Lifted off <see cref="AppState"/> at CORE-001 so it is
        /// independent of AR context. Debounced inside CORE-007 so a single dropped poll can't flip
        /// it. AI features (CORE-006) read this to decide cached-vs-live; it is NOT inferred from
        /// <see cref="AppState"/>.
        /// </summary>
        Connectivity CurrentConnectivity { get; }

        /// <summary>Raised whenever <see cref="CurrentConnectivity"/> changes.</summary>
        event Action<Connectivity> OnConnectivityChanged;

        // --- Monitoring snapshot (A.12) --------------------------------------------------------

        /// <summary>
        /// Allocation-free snapshot of the currently monitored signals, for the A.12 debug overlay
        /// and the over-budget logging hook. The overlay UI itself is deferred to CORE-002.
        /// </summary>
        PerformanceMetrics Metrics { get; }
    }
}
