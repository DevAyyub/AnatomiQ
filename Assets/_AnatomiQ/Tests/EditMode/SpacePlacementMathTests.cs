using AnatomiQ.Core;
using NUnit.Framework;
using UnityEngine;

namespace AnatomiQ.Tests.EditMode
{
    /// <summary>
    /// ATLAS-003 (chunk 2) — verifies <see cref="PlacementMath.ComputeSpacePose"/>: the body lands the
    /// configured distance in front of the camera, upright and facing the camera, with a safe fallback
    /// when the facing direction is vertical/degenerate.
    ///
    /// Lives in the standard Core-referencing EditMode assembly — the math is in Core (no AR pillar
    /// reference needed), which is why this compiles in Tests/EditMode as-is.
    /// </summary>
    public sealed class SpacePlacementMathTests
    {
        private const float Tolerance = 1e-4f;

        [Test]
        public void Pose_IsDistanceMetres_InFrontOfCamera()
        {
            // Camera at origin looking down +Z.
            Pose pose = PlacementMath.ComputeSpacePose(Vector3.zero, Quaternion.identity, 2.0f);

            Assert.That(Vector3.Distance(pose.position, new Vector3(0f, 0f, 2f)), Is.LessThan(Tolerance));
        }

        [Test]
        public void Pose_FacesTheCamera_Horizontally()
        {
            Pose pose = PlacementMath.ComputeSpacePose(Vector3.zero, Quaternion.identity, 2.0f);

            // The body's forward (+Z) should point back toward the camera, i.e. roughly -Z here.
            Vector3 bodyForward = pose.rotation * Vector3.forward;
            Assert.That(Vector3.Distance(bodyForward, new Vector3(0f, 0f, -1f)), Is.LessThan(Tolerance));
        }

        [Test]
        public void Pose_StaysUpright_RegardlessOfCameraPitch()
        {
            // Camera pitched down 30°, still looking generally forward.
            Quaternion pitched = Quaternion.Euler(30f, 0f, 0f);

            Pose pose = PlacementMath.ComputeSpacePose(Vector3.zero, pitched, 2.0f);

            // Body's up axis should remain world-up (no roll/pitch leaked in).
            Vector3 bodyUp = pose.rotation * Vector3.up;
            Assert.That(Vector3.Distance(bodyUp, Vector3.up), Is.LessThan(Tolerance));
        }

        [Test]
        public void Pose_RespectsCameraPositionAndDistance()
        {
            Vector3 camPos = new Vector3(5f, 1.5f, -3f);
            // Look along +X.
            Quaternion lookX = Quaternion.LookRotation(Vector3.right, Vector3.up);

            Pose pose = PlacementMath.ComputeSpacePose(camPos, lookX, 1.0f);

            Assert.That(Vector3.Distance(pose.position, new Vector3(6f, 1.5f, -3f)), Is.LessThan(Tolerance));
        }

        [Test]
        public void Pose_Degenerate_VerticalGaze_DoesNotThrowOrNaN()
        {
            // Camera looking straight down: horizontal facing is undefined → falls back to cam rotation.
            Quaternion lookDown = Quaternion.LookRotation(Vector3.down, Vector3.forward);

            Pose pose = PlacementMath.ComputeSpacePose(Vector3.zero, lookDown, 2.0f);

            Assert.IsFalse(float.IsNaN(pose.rotation.x) || float.IsNaN(pose.rotation.y) ||
                           float.IsNaN(pose.rotation.z) || float.IsNaN(pose.rotation.w),
                "Degenerate vertical gaze produced a NaN rotation.");
            Assert.That(Vector3.Distance(pose.position, new Vector3(0f, -2f, 0f)), Is.LessThan(Tolerance));
        }
    }
}
