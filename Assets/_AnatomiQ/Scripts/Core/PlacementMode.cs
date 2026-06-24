namespace AnatomiQ.Core
{
    /// <summary>
    /// The three ways ATLAS-003 can present the body, exposed through <see cref="IPlacementProvider"/>.
    /// This is ATLAS-003's OWN state, deliberately distinct from the global <see cref="AppState"/>
    /// (which CORE-007 solely owns): AppState answers "what AR context is the app in?", PlacementMode
    /// answers "how is the body currently being placed?". ATLAS-003 reconciles the two — e.g. it forces
    /// <see cref="Viewer"/> while AppState is <see cref="AppState.AR_VIEWER_MODE"/> — but NEVER writes
    /// AppState.
    /// </summary>
    public enum PlacementMode
    {
        /// <summary>Body anchored on a detected horizontal surface (table/floor). AR session required.</summary>
        Surface,

        /// <summary>Body anchored floating at a fixed distance in front of the camera, no surface needed.
        /// The demo-preferred path — must be instant and rock-solid. AR session required.</summary>
        Space,

        /// <summary>Non-AR fallback: body rendered without the AR camera; touch rotate/zoom only. The
        /// always-available baseline (D.4: the app opens here; AR failure silently falls back here).</summary>
        Viewer
    }
}
