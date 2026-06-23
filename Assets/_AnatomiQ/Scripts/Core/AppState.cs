namespace AnatomiQ.Core
{
    /// <summary>
    /// Global AR-context state, owned and published by CORE-007 (FallbackManager). Every UI and
    /// feature system reads this to adjust behaviour. Members use the UPPER_SNAKE_CASE convention from
    /// the project docs (e.g. <c>AppState.AR_VIEWER_MODE</c>).
    ///
    /// CORE-001 design decision: AppState now encodes AR tracking context ONLY, and has a SINGLE
    /// writer — CORE-007's <c>CheckArTracking</c>. The two other global concerns live on their own
    /// signals so they can never mask AR context (and vice versa): network reachability is
    /// <see cref="Connectivity"/>, and quality/throttling severity is <see cref="PerformanceTier"/>.
    /// Neither maps onto this enum.
    /// </summary>
    public enum AppState
    {
        /// <summary>AR session is tracking; full AR experience available.</summary>
        AR_ACTIVE,

        /// <summary>AR unavailable/declined; 3D-only viewer experience is active.</summary>
        AR_VIEWER_MODE,

        /// <summary>AR running but tracking is degraded/lost (owned by CORE-001): freeze at last
        /// anchor, warn, lock-to-screen-center. NOTE: thermal/perf throttling does NOT map here —
        /// that is the orthogonal <see cref="PerformanceTier"/> axis; and no-network does NOT map
        /// here — that is the orthogonal <see cref="Connectivity"/> signal. This state is for AR
        /// tracking degradation only.</summary>
        AR_LIMITED
    }
}
