using AnatomiQ.Core;
using NUnit.Framework;
using UnityEngine;

namespace AnatomiQ.Tests.EditMode
{
    /// <summary>
    /// ATLAS-003 chunk 4. Unit tests for <see cref="ManipulationMath"/> — the pure gesture geometry
    /// behind pinch-to-scale and two-finger drag (rotate/reposition). Core-referencing EditMode
    /// assembly; no scene or touch hardware needed, same pattern as the placement-math tests.
    /// </summary>
    public sealed class ManipulationMathTests
    {
        private const float Tolerance = 1e-4f;

        [Test]
        public void PinchScaleFactor_ReturnsRatioOfDistances()
        {
            Assert.AreEqual(1.5f, ManipulationMath.PinchScaleFactor(100f, 150f), Tolerance);
            Assert.AreEqual(0.5f, ManipulationMath.PinchScaleFactor(200f, 100f), Tolerance);
        }

        [Test]
        public void PinchScaleFactor_GuardsZeroPreviousDistance()
        {
            Assert.AreEqual(1f, ManipulationMath.PinchScaleFactor(0f, 150f), Tolerance,
                "A zero previous distance must yield no change, not a divide-by-zero blow-up.");
        }

        [Test]
        public void ClampUniformScale_PassesValuesWithinRange()
        {
            Assert.AreEqual(2f, ManipulationMath.ClampUniformScale(2f, 1f, 0.4f, 3f), Tolerance);
        }

        [Test]
        public void ClampUniformScale_ClampsBelowMinAndAboveMax()
        {
            Assert.AreEqual(0.4f, ManipulationMath.ClampUniformScale(0.1f, 1f, 0.4f, 3f), Tolerance);
            Assert.AreEqual(3f, ManipulationMath.ClampUniformScale(9f, 1f, 0.4f, 3f), Tolerance);
        }

        [Test]
        public void ScreenPanToWorld_CombinesCameraBasis()
        {
            Vector3 result = ManipulationMath.ScreenPanToWorld(
                Vector3.right, Vector3.up, new Vector2(2f, 4f), 0.5f);

            Assert.AreEqual(1f, result.x, Tolerance);
            Assert.AreEqual(2f, result.y, Tolerance);
            Assert.AreEqual(0f, result.z, Tolerance);
        }

        [Test]
        public void DragToYawPitch_ScalesYawAndInvertsPitch()
        {
            Vector2 yawPitch = ManipulationMath.DragToYawPitchDegrees(new Vector2(10f, 20f), 0.5f);

            Assert.AreEqual(5f, yawPitch.x, Tolerance, "Horizontal drag scales into yaw.");
            Assert.AreEqual(-10f, yawPitch.y, Tolerance, "Vertical drag inverts into pitch (turntable feel).");
        }
    }
}
