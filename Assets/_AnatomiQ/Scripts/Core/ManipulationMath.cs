using UnityEngine;

namespace AnatomiQ.Core
{
    /// <summary>
    /// Pure, input-free gesture geometry for ATLAS-003 chunk 4 body manipulation (pinch-to-scale,
    /// two-finger drag to rotate/reposition). Lives in Core with no Input System or AR dependency so
    /// the math is unit-testable from the standard Core-referencing test assembly; the MonoBehaviour
    /// (<c>BodyManipulator</c>, AR pillar) does the touch I/O and calls these. Mirrors the split used
    /// for <see cref="PlacementMath"/>.
    /// </summary>
    public static class ManipulationMath
    {
        /// <summary>
        /// The multiplicative scale change for a pinch this frame: the ratio of the current two-finger
        /// distance to the previous one. Guards a zero/degenerate previous distance by returning 1
        /// (no change) so the first frame of a gesture never explodes the scale.
        /// </summary>
        public static float PinchScaleFactor(float previousDistance, float currentDistance)
        {
            if (previousDistance <= 1e-4f)
            {
                return 1f;
            }

            return currentDistance / previousDistance;
        }

        /// <summary>
        /// Clamps a proposed uniform scale to [<paramref name="baseScale"/> * <paramref name="minFactor"/>,
        /// <paramref name="baseScale"/> * <paramref name="maxFactor"/>], so the body can never shrink to
        /// nothing or grow past the framing budget. Used in "factor space" (baseScale = 1) by the
        /// manipulator, where the result is a clamped multiplier of the model's authored scale.
        /// </summary>
        public static float ClampUniformScale(float proposed, float baseScale, float minFactor, float maxFactor)
        {
            return Mathf.Clamp(proposed, baseScale * minFactor, baseScale * maxFactor);
        }

        /// <summary>
        /// Maps a screen-space two-finger drag delta (pixels) into a world-space translation in the
        /// plane facing the camera, by combining the camera's right and up axes. Used for AR
        /// "reposition": dragging slides the body across the view. <paramref name="metresPerPixel"/>
        /// is the drag sensitivity.
        /// </summary>
        public static Vector3 ScreenPanToWorld(
            Vector3 cameraRight, Vector3 cameraUp, Vector2 screenDelta, float metresPerPixel)
        {
            return (cameraRight * screenDelta.x + cameraUp * screenDelta.y) * metresPerPixel;
        }

        /// <summary>
        /// Maps a screen-space two-finger drag delta (pixels) into yaw/pitch rotation in degrees, used
        /// for Viewer "rotate model": horizontal drag → yaw (about world up), vertical drag → pitch
        /// (about the camera's right axis). Pitch is inverted so dragging up tips the top of the model
        /// toward the viewer, the conventional turntable feel. <paramref name="degreesPerPixel"/> is the
        /// rotation sensitivity.
        /// </summary>
        public static Vector2 DragToYawPitchDegrees(Vector2 screenDelta, float degreesPerPixel)
        {
            return new Vector2(screenDelta.x * degreesPerPixel, -screenDelta.y * degreesPerPixel);
        }
    }
}
