namespace AnatomiQ.Core
{
    /// <summary>
    /// Global application state, owned and published by CORE-007 (FallbackManager).
    /// Every UI and feature system reads this to adjust behaviour. Members use the
    /// UPPER_SNAKE_CASE convention from the project docs (e.g. <c>AppState.OFFLINE_MODE</c>).
    /// </summary>
    public enum AppState
    {
        /// <summary>AR session is tracking; full AR experience available.</summary>
        AR_ACTIVE,

        /// <summary>AR unavailable/declined; 3D-only viewer experience is active.</summary>
        AR_VIEWER_MODE,

        /// <summary>AR running but degraded (tracking lost, thermal/perf throttle, etc.).</summary>
        AR_LIMITED,

        /// <summary>No network; cached responses and pre-baked data only.</summary>
        OFFLINE_MODE
    }
}
