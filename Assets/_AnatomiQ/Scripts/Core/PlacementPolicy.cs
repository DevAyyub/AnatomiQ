namespace AnatomiQ.Core
{
    /// <summary>
    /// The action ATLAS-003's controller should take in response to a placement-mode request, decided
    /// purely from the requested <see cref="PlacementMode"/> and the current <see cref="AppState"/>.
    /// Returned by <see cref="PlacementPolicy.ResolveRequest"/>.
    /// </summary>
    public enum PlacementAction
    {
        /// <summary>Switch to the non-AR 3D Viewer (the always-working baseline).</summary>
        ForceViewer = 0,

        /// <summary>AR is active now — place the body immediately in the requested AR mode.</summary>
        PlaceNow = 1,

        /// <summary>
        /// AR is not active yet — bring the session up (D.4: this is when the OS camera prompt fires)
        /// and remember the requested mode so the body places once AppState reaches
        /// <see cref="AppState.AR_ACTIVE"/>.
        /// </summary>
        EnterArThenPlace = 2
    }

    /// <summary>
    /// ATLAS-003 chunk 5 — pure, AR-package-free placement POLICY. Mirrors the project pattern of
    /// keeping decision geometry/logic in small testable Core types (<see cref="PlacementMath"/>,
    /// <see cref="ManipulationMath"/>) so the controller's MonoBehaviour only does I/O and side effects.
    ///
    /// CORE-007 is the single writer of <see cref="AppState"/>; ATLAS-003 SUBSCRIBES and reconciles its
    /// own <see cref="PlacementMode"/> against it (per <see cref="IPlacementProvider"/>). These helpers
    /// encode that reconciliation as referentially-transparent functions of the inputs, with no AR
    /// Foundation dependency — the EditMode suite exercises every branch without an AR session.
    ///
    /// Note on the AR_VIEWER_MODE collapse vs. AR bring-up: <see cref="AppState.AR_VIEWER_MODE"/> is
    /// reported both when AR is genuinely unavailable AND transiently while the session is starting
    /// (initializing → tracking). The two are distinguished by whether an AR entry is in flight, NOT by
    /// AppState alone — hence <see cref="ShouldCollapseToViewer"/> takes a <c>hasPendingArEntry</c>
    /// flag. Telling a genuine failure (permission denied / unsupported) from a still-initializing
    /// session needs the finer-grained <see cref="ArTrackingStatus"/>, which lives AR-side; the
    /// controller reads it directly there. This type intentionally does not see it.
    /// </summary>
    public static class PlacementPolicy
    {
        /// <summary>
        /// Decides what to do with a user/UI placement request. A Viewer request always wins
        /// (<see cref="PlacementAction.ForceViewer"/>). An AR request places immediately when AR is
        /// active, otherwise defers behind an AR bring-up (<see cref="PlacementAction.EnterArThenPlace"/>),
        /// including while tracking is merely limited — placement then waits for tracking to recover.
        /// </summary>
        /// <param name="requested">The placement mode the user/UI asked for.</param>
        /// <param name="state">The current global <see cref="AppState"/> (owned by CORE-007).</param>
        /// <returns>The action the controller should take.</returns>
        public static PlacementAction ResolveRequest(PlacementMode requested, AppState state)
        {
            if (requested == PlacementMode.Viewer)
            {
                return PlacementAction.ForceViewer;
            }

            // requested is an AR mode (Surface / Space)
            return state == AppState.AR_ACTIVE
                ? PlacementAction.PlaceNow
                : PlacementAction.EnterArThenPlace;
        }

        /// <summary>
        /// True when an <see cref="AppState.AR_VIEWER_MODE"/> state should collapse the body to the 3D
        /// Viewer. It should NOT collapse while an AR entry is in flight, because AR_VIEWER_MODE is then
        /// just a transient step on the way to <see cref="AppState.AR_ACTIVE"/> (session initializing).
        /// </summary>
        /// <param name="state">The new global state.</param>
        /// <param name="hasPendingArEntry">Whether the controller is mid AR bring-up.</param>
        public static bool ShouldCollapseToViewer(AppState state, bool hasPendingArEntry)
            => state == AppState.AR_VIEWER_MODE && !hasPendingArEntry;

        /// <summary>
        /// True when the body should be frozen and screen-locked: AppState is
        /// <see cref="AppState.AR_LIMITED"/> (tracking degraded or lost).
        /// </summary>
        /// <param name="state">The current global state.</param>
        public static bool IsTrackingLocked(AppState state) => state == AppState.AR_LIMITED;
    }
}
