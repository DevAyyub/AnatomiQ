using System;

namespace AnatomiQ.Core
{
    /// <summary>
    /// Contract ATLAS-003's placement controller exposes so other pillars — the UI mode switcher,
    /// primarily — can read and request the current <see cref="PlacementMode"/> WITHOUT referencing the
    /// AR pillar or AR Foundation. The controller lives AR-side, translates plane/raycast/anchor work
    /// into this AR-type-free surface, registers itself through the <see cref="ServiceRegistry"/>, and
    /// is reached via <c>_services.PlacementProvider</c>.
    ///
    /// Same pull-not-push shape as <see cref="IArTrackingProvider"/>: this exposes ATLAS-003's mode and
    /// a best-effort <see cref="RequestMode"/>; it does NOT touch <see cref="AppState"/>. AppState has a
    /// single writer (CORE-007's <c>CheckArTracking</c>). ATLAS-003 SUBSCRIBES to AppState changes and
    /// reconciles them against user requests internally — so <see cref="RequestMode"/> is advisory: the
    /// controller may decline or override a request to satisfy the current AppState (e.g. it forces
    /// <see cref="PlacementMode.Viewer"/> while AppState is <see cref="AppState.AR_VIEWER_MODE"/>).
    ///
    /// Anchor primitives stay in CORE-001 (ATLAS-003 boundary decision): the controller computes a
    /// target pose and calls the AR session manager's existing async anchor seam rather than owning
    /// anchoring itself — so <c>ARAnchor</c> and the other AR Foundation types never reach this
    /// interface, exactly as they never reach <see cref="IArTrackingProvider"/>.
    ///
    /// Extends <see cref="IService"/> so it registers via the standard
    /// <see cref="ServiceRegistry.Register"/> switch. Because the AR scene can load and unload during a
    /// session, the implementer MUST clear its registry slot on teardown via
    /// <see cref="ServiceRegistry.ClearPlacementProvider"/> — an interface reference to a destroyed
    /// MonoBehaviour does not read as Unity-null, so a stale provider would otherwise keep being read.
    /// </summary>
    public interface IPlacementProvider : IService
    {
        /// <summary>
        /// The body's current placement mode. Starts at <see cref="PlacementMode.Viewer"/> (D.4: the
        /// app opens in 3D viewer with no launch-time camera request).
        /// </summary>
        PlacementMode CurrentMode { get; }

        /// <summary>
        /// Raised only when <see cref="CurrentMode"/> changes (change-only — no per-frame re-fire). The
        /// UI mode switcher subscribes to reflect the active mode. Late subscribers should read
        /// <see cref="CurrentMode"/> immediately after subscribing, since they miss the prior transition.
        /// </summary>
        event Action<PlacementMode> OnPlacementModeChanged;

        /// <summary>
        /// Best-effort request to switch to <paramref name="mode"/>, e.g. from the single-tap mode
        /// switcher. The controller arbitrates against the current <see cref="AppState"/> and may
        /// decline (an AR mode requested while AR is unavailable stays in
        /// <see cref="PlacementMode.Viewer"/>) or trigger the D.4 camera-permission flow before
        /// honouring an AR request. Read <see cref="CurrentMode"/> (or await the event) for the result.
        /// </summary>
        /// <param name="mode">The placement mode the user/UI is requesting.</param>
        void RequestMode(PlacementMode mode);
    }
}
