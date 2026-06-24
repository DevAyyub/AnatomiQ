using UnityEngine;

namespace AnatomiQ.Core
{
    /// <summary>
    /// Pure, AR-type-free geometry helpers for ATLAS-003 placement. Lives in Core (no AR Foundation
    /// dependency) so the math is trivially unit-testable from the standard Core-referencing test
    /// assembly without pulling in the AR pillar or XR Simulation. PlacementController (AR) calls these;
    /// AR Foundation types never appear here.
    /// </summary>
    public static class PlacementMath
    {
        /// <summary>
        /// Computes the world pose for Space placement: a point <paramref name="distance"/> metres along
        /// the camera's forward axis, oriented upright (yaw-only) so the body faces the camera.
        /// Side-effect-free. Degenerate case (camera looking straight up/down, so the horizontal facing
        /// is undefined) falls back to the camera's own rotation.
        /// </summary>
        /// <param name="cameraPosition">World position of the AR camera.</param>
        /// <param name="cameraRotation">World rotation of the AR camera.</param>
        /// <param name="distance">Metres in front of the camera to place the body.</param>
        /// <returns>The world <see cref="Pose"/> to anchor the body at.</returns>
        public static Pose ComputeSpacePose(Vector3 cameraPosition, Quaternion cameraRotation, float distance)
        {
            Vector3 forward = cameraRotation * Vector3.forward;
            Vector3 position = cameraPosition + forward * distance;

            // Upright, facing the camera: project the body→camera vector onto the horizontal plane.
            Vector3 toCamera = cameraPosition - position;
            toCamera.y = 0f;

            Quaternion rotation = toCamera.sqrMagnitude > 1e-6f
                ? Quaternion.LookRotation(toCamera.normalized, Vector3.up)
                : cameraRotation;

            return new Pose(position, rotation);
        }

        /// <summary>
        /// Computes the world pose for Surface placement: the body stands ON the detected plane (feet
        /// flush with the surface) and is yawed upright to face the user. The plane hit gives the floor
        /// point; <paramref name="footOffset"/> is how far the model's lowest point sits BELOW its root
        /// origin, so the origin is lifted by that amount and the feet land exactly on the plane.
        ///
        /// Side-effect-free and AR-type-free (callers pass a plain plane-hit position from the raycast).
        /// Always upright — the surface is assumed horizontal, so the up axis stays world-up regardless
        /// of the hit's own orientation. Degenerate case (camera directly above the hit, so the
        /// horizontal facing is undefined) falls back to identity rotation (still upright), never NaN.
        /// </summary>
        /// <param name="planeHitPosition">World position of the plane raycast hit (the floor point).</param>
        /// <param name="cameraPosition">World position of the AR camera (the body is yawed to face it).</param>
        /// <param name="footOffset">
        /// Metres from the model root origin down to its lowest point (its soles). Pass 0 if the model
        /// origin is already at the feet; pass a positive value to lift the body so it stands on the plane.
        /// </param>
        /// <returns>The world <see cref="Pose"/> to anchor the body at.</returns>
        public static Pose ComputeSurfacePose(Vector3 planeHitPosition, Vector3 cameraPosition, float footOffset)
        {
            Vector3 position = planeHitPosition + Vector3.up * footOffset;

            // Upright, facing the camera: project the body→camera vector onto the horizontal plane.
            Vector3 toCamera = cameraPosition - position;
            toCamera.y = 0f;

            Quaternion rotation = toCamera.sqrMagnitude > 1e-6f
                ? Quaternion.LookRotation(toCamera.normalized, Vector3.up)
                : Quaternion.identity;

            return new Pose(position, rotation);
        }
    }
}
