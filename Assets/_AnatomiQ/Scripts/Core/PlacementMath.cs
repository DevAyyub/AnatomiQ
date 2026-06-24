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
    }
}
