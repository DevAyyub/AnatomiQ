using AnatomiQ.Core;
using NUnit.Framework;

namespace AnatomiQ.Tests.EditMode
{
    /// <summary>
    /// ATLAS-003 chunk 5 — EditMode tests for the pure placement arbitration logic. No AR Foundation,
    /// no MonoBehaviour, no play loop: <see cref="PlacementPolicy"/> is a referentially-transparent
    /// function of (requested mode, AppState), so every branch is checked exhaustively here, the same
    /// way PlacementMath / ManipulationMath are. The controller's side-effecting reactions
    /// (EnterAr/place/screen-lock) are covered by the chunk-6 PlayMode pass over a mock AR seam.
    /// </summary>
    [TestFixture]
    public sealed class PlacementPolicyTests
    {
        // ── ResolveRequest: Viewer always forces Viewer regardless of AppState ───────────────────────

        [TestCase(AppState.AR_ACTIVE)]
        [TestCase(AppState.AR_LIMITED)]
        [TestCase(AppState.AR_VIEWER_MODE)]
        public void ResolveRequest_Viewer_AlwaysForcesViewer(AppState state)
        {
            Assert.AreEqual(PlacementAction.ForceViewer,
                PlacementPolicy.ResolveRequest(PlacementMode.Viewer, state));
        }

        // ── ResolveRequest: an AR mode places immediately only when AR is already active ─────────────

        [TestCase(PlacementMode.Surface)]
        [TestCase(PlacementMode.Space)]
        public void ResolveRequest_ArMode_WhenActive_PlacesNow(PlacementMode mode)
        {
            Assert.AreEqual(PlacementAction.PlaceNow,
                PlacementPolicy.ResolveRequest(mode, AppState.AR_ACTIVE));
        }

        // ── ResolveRequest: an AR mode while not active defers behind an AR bring-up ─────────────────

        [TestCase(PlacementMode.Surface, AppState.AR_VIEWER_MODE)]
        [TestCase(PlacementMode.Space, AppState.AR_VIEWER_MODE)]
        [TestCase(PlacementMode.Surface, AppState.AR_LIMITED)]
        [TestCase(PlacementMode.Space, AppState.AR_LIMITED)]
        public void ResolveRequest_ArMode_WhenNotActive_EntersArThenPlaces(PlacementMode mode, AppState state)
        {
            Assert.AreEqual(PlacementAction.EnterArThenPlace,
                PlacementPolicy.ResolveRequest(mode, state));
        }

        // ── ShouldCollapseToViewer: only in AR_VIEWER_MODE, and only when no AR entry is in flight ───

        [Test]
        public void ShouldCollapseToViewer_ViewerMode_NoPendingEntry_True()
        {
            Assert.IsTrue(PlacementPolicy.ShouldCollapseToViewer(
                AppState.AR_VIEWER_MODE, hasPendingArEntry: false));
        }

        [Test]
        public void ShouldCollapseToViewer_ViewerMode_PendingEntry_False()
        {
            // AR_VIEWER_MODE during a bring-up is transient — don't collapse, the session is initializing.
            Assert.IsFalse(PlacementPolicy.ShouldCollapseToViewer(
                AppState.AR_VIEWER_MODE, hasPendingArEntry: true));
        }

        [TestCase(AppState.AR_ACTIVE, false)]
        [TestCase(AppState.AR_ACTIVE, true)]
        [TestCase(AppState.AR_LIMITED, false)]
        [TestCase(AppState.AR_LIMITED, true)]
        public void ShouldCollapseToViewer_NonViewerStates_AlwaysFalse(AppState state, bool hasPending)
        {
            Assert.IsFalse(PlacementPolicy.ShouldCollapseToViewer(state, hasPending));
        }

        // ── IsTrackingLocked: exactly AR_LIMITED ─────────────────────────────────────────────────────

        [Test]
        public void IsTrackingLocked_Limited_True()
        {
            Assert.IsTrue(PlacementPolicy.IsTrackingLocked(AppState.AR_LIMITED));
        }

        [TestCase(AppState.AR_ACTIVE)]
        [TestCase(AppState.AR_VIEWER_MODE)]
        public void IsTrackingLocked_NonLimited_False(AppState state)
        {
            Assert.IsFalse(PlacementPolicy.IsTrackingLocked(state));
        }
    }
}
