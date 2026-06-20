using System;

namespace AnatomiQ.Core
{
    /// <summary>
    /// Contract for CORE-007. Owns and publishes the two orthogonal global signals other systems
    /// read: the AR/connectivity <see cref="AppState"/> (Axis 1) and the quality/degradation
    /// <see cref="PerformanceTier"/> (Axis 2). It is the single source of truth for both.
    ///
    /// The two axes are deliberately separate. AppState answers "what mode is the app in?"
    /// (AR active, viewer-only, AR-limited, offline). PerformanceTier answers "how much quality must
    /// the renderer shed to stay in budget?" Low FPS and thermal throttling do not change AppState —
    /// they raise the tier. Keeping these apart avoids a combinatorial AppState explosion and stops
    /// every AppState subscriber from having to care about render-quality concerns.
    /// See AnatomiQ_Build_Environment.md Part D and the CORE-007 logic-phase decision log.
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

        // --- Monitoring snapshot (A.12) --------------------------------------------------------

        /// <summary>
        /// Allocation-free snapshot of the currently monitored signals, for the A.12 debug overlay
        /// and the over-budget logging hook. The overlay UI itself is deferred to CORE-002.
        /// </summary>
        PerformanceMetrics Metrics { get; }
    }
}
