using AnatomiQ.Core;
using NUnit.Framework;
using UnityEngine;

namespace AnatomiQ.Tests.EditMode
{
    /// <summary>
    /// ATLAS-003 chunk 3. Unit tests for <see cref="PlacementMath.ComputeSurfacePose"/> — the pure
    /// geometry behind Surface placement (lift the body so its feet rest on the detected plane, yaw it
    /// upright to face the user). Lives in the Core-referencing EditMode assembly; no AR scene needed,
    /// same as <c>SpacePlacementMathTests</c>.
    /// </summary>
    public sealed class SurfacePlacementMathTests
    {
        private const float Tolerance = 1e-4f;

        [Test]
        public void ZeroFootOffset_PlacesExactlyOnHit()
        {
            var hit = new Vector3(1f, 0.5f, -2f);
            var cam = new Vector3(1f, 2f, 1f);

            Pose pose = PlacementMath.ComputeSurfacePose(hit, cam, 0f);

            Assert.AreEqual(hit.x, pose.position.x, Tolerance);
            Assert.AreEqual(hit.y, pose.position.y, Tolerance);
            Assert.AreEqual(hit.z, pose.position.z, Tolerance);
        }

        [Test]
        public void FootOffset_RaisesBodyByOffset_LeavesPlanarPositionUntouched()
        {
            var hit = new Vector3(0f, 0f, 0f);
            var cam = new Vector3(0f, 1.6f, 3f);
            const float footDrop = 0.9f;

            Pose pose = PlacementMath.ComputeSurfacePose(hit, cam, footDrop);

            Assert.AreEqual(hit.x, pose.position.x, Tolerance);
            Assert.AreEqual(footDrop, pose.position.y, Tolerance,
                "Origin should lift by the foot drop so the feet land on the plane.");
            Assert.AreEqual(hit.z, pose.position.z, Tolerance);
        }

        [Test]
        public void FacesCamera_YawOnly_TowardCameraInHorizontalPlane()
        {
            var hit = new Vector3(0f, 0f, 0f);
            var cam = new Vector3(3f, 1.6f, 0f); // off to the +X side and above

            Pose pose = PlacementMath.ComputeSurfacePose(hit, cam, 0f);

            Vector3 forward = pose.rotation * Vector3.forward;
            // Body should face the camera horizontally: forward ≈ +X, with no vertical component.
            Assert.AreEqual(1f, forward.x, 1e-3f);
            Assert.AreEqual(0f, forward.y, 1e-3f, "Surface placement must stay upright (yaw only).");
        }

        [Test]
        public void StaysUpright_WorldUpPreserved()
        {
            var hit = new Vector3(2f, 1f, -1f);
            var cam = new Vector3(-1f, 5f, 4f);

            Pose pose = PlacementMath.ComputeSurfacePose(hit, cam, 0.5f);

            Vector3 up = pose.rotation * Vector3.up;
            Assert.AreEqual(0f, up.x, 1e-3f);
            Assert.AreEqual(1f, up.y, 1e-3f);
            Assert.AreEqual(0f, up.z, 1e-3f);
        }

        [Test]
        public void CameraDirectlyAbove_FallsBackToIdentity_NoNaN()
        {
            var hit = new Vector3(0f, 0f, 0f);
            var cam = new Vector3(0f, 2f, 0f); // same x,z → horizontal facing undefined
            const float footDrop = 0.3f;

            Pose pose = PlacementMath.ComputeSurfacePose(hit, cam, footDrop);

            Assert.AreEqual(Quaternion.identity.x, pose.rotation.x, Tolerance);
            Assert.AreEqual(Quaternion.identity.y, pose.rotation.y, Tolerance);
            Assert.AreEqual(Quaternion.identity.z, pose.rotation.z, Tolerance);
            Assert.AreEqual(Quaternion.identity.w, pose.rotation.w, Tolerance);

            Assert.IsFalse(
                float.IsNaN(pose.position.x) || float.IsNaN(pose.position.y) || float.IsNaN(pose.position.z),
                "Degenerate facing must not produce NaN.");
            Assert.AreEqual(footDrop, pose.position.y, Tolerance);
        }
    }
}
