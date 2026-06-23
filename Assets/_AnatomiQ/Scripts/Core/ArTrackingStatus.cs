namespace AnatomiQ.Core
{
    /// <summary>
    /// AR tracking status, mirrored into the Core assembly so CORE-007's <c>CheckArTracking</c> and
    /// the <see cref="AppState"/> mapping do NOT depend on AR Foundation's namespaces
    /// (<c>UnityEngine.XR.ARSubsystems.TrackingState</c> / <c>ARSessionState</c>). Core must not
    /// reference the AR pillar, so CORE-001's ARSessionManager (in the AR assembly) translates AR
    /// Foundation's session + tracking state into this Core-local enum and publishes it through
    /// <see cref="IArTrackingProvider"/>; the fallback logic and its tests run against this type,
    /// AR-package-free.
    ///
    /// Maps to <see cref="AppState"/> as follows (the mapping is OWNED by CORE-007):
    /// <list type="bullet">
    /// <item><see cref="Tracking"/> → <see cref="AppState.AR_ACTIVE"/>.</item>
    /// <item><see cref="Limited"/> / <see cref="NotTracking"/> → <see cref="AppState.AR_LIMITED"/>
    /// (freeze at last anchor + warn + lock-to-screen-center).</item>
    /// <item><see cref="Unavailable"/> / <see cref="PermissionDenied"/> / <see cref="Initializing"/>
    /// → <see cref="AppState.AR_VIEWER_MODE"/> (the safe 3D-only baseline).</item>
    /// </list>
    /// <see cref="Unavailable"/> and <see cref="PermissionDenied"/> are kept distinct (both map to
    /// viewer mode) so a UI layer can read <see cref="IArTrackingProvider.Status"/> directly and pick
    /// the right explanation — "device doesn't support AR" vs "camera permission needed". Likewise
    /// <see cref="Limited"/> and <see cref="NotTracking"/> are distinct (both map to AR_LIMITED) so
    /// CORE-001's own change-only event can drive a softer vs harder visual response.
    /// </summary>
    public enum ArTrackingStatus
    {
        /// <summary>No AR session is possible: ARCore unsupported, install unavailable, or no AR
        /// provider is registered (the default before any AR scene is active).</summary>
        Unavailable = 0,

        /// <summary>AR is supported but the camera permission was refused.</summary>
        PermissionDenied = 1,

        /// <summary>Session is starting / checking availability / installing — not yet tracking.</summary>
        Initializing = 2,

        /// <summary>Session is tracking normally; full AR experience available.</summary>
        Tracking = 3,

        /// <summary>Session is running but tracking is degraded (excess motion, low light / features).</summary>
        Limited = 4,

        /// <summary>Session is running but tracking is lost.</summary>
        NotTracking = 5
    }
}
