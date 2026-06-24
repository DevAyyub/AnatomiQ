using AnatomiQ.Core;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace AnatomiQ.AR
{
    /// <summary>
    /// ATLAS-003 chunk 4 — body manipulation gestures. Applies pinch (uniform scale) and two-finger
    /// drag (rotate in Viewer / reposition in AR) to CORE-002's <see cref="IBodyModelRenderer.ModelRoot"/>,
    /// so all three placement modes share one manipulation vocabulary (A.2: AR is a mode, not a wrapper).
    /// The concrete drag behaviour is the only thing that changes with mode, per the D.1 contract:
    /// pinch = zoom/scale everywhere; two-finger drag = rotate (Viewer) vs reposition (AR).
    ///
    /// Deliberately acts ONLY on exactly two active touches. Single-finger input (tap / drag) is left
    /// entirely untouched so CORE-004 can own organ selection — this is the "single tap → no-op organ
    /// seam, do NOT consume for placement" requirement, satisfied by never reading single-finger touches.
    ///
    /// Pull-only and AR-type-free: it reads the body via <see cref="IBodyModelRenderer"/> and the mode
    /// via <see cref="IPlacementProvider"/> through the <see cref="ServiceRegistry"/>, and uses a plain
    /// <see cref="Camera"/> for screen-relative axes. It references no AR Foundation types, so it works
    /// identically whether the body is anchored (AR) or framed at the scene root (Viewer). Because
    /// PlacementController only flips to an AR mode once the body is actually placed ("mode reflects
    /// reality"), CurrentMode is a sufficient proxy for "ready to manipulate" — no separate placed-state
    /// is needed.
    ///
    /// Gesture geometry is the pure, unit-tested <see cref="ManipulationMath"/> (Core); this component
    /// only does touch I/O and applies the results to the transform.
    /// </summary>
    [DefaultExecutionOrder(-840)] // After providers register (-900/-850); tolerant of nulls regardless.
    public sealed class BodyManipulator : MonoBehaviour
    {
        [Header("Service wiring")]
        [Tooltip("The single ServiceRegistry asset, shared with all services. Assign in the inspector.")]
        [SerializeField] private ServiceRegistry _services;

        [Tooltip("Camera whose right/up axes define screen-relative pan & rotate. Assign the AR/Main Camera.")]
        [SerializeField] private Camera _camera;

        [Header("Pinch — zoom (Viewer) / scale (AR)")]
        [Tooltip("Smallest the body may shrink to, as a fraction of its authored scale.")]
        [Range(0.1f, 1f)]
        [SerializeField] private float _minScaleFactor = 0.4f;
        [Tooltip("Largest the body may grow to, as a multiple of its authored scale.")]
        [Range(1f, 10f)]
        [SerializeField] private float _maxScaleFactor = 3f;

        [Header("Two-finger drag")]
        [Tooltip("Viewer: degrees the model rotates per screen pixel of two-finger drag.")]
        [SerializeField] private float _rotateDegreesPerPixel = 0.2f;
        [Tooltip("AR: metres the body repositions per screen pixel of two-finger drag.")]
        [SerializeField] private float _panMetresPerPixel = 0.002f;

        // Authored scale is captured once (the body's intrinsic scale, constant across modes); the live
        // zoom is expressed as a clamped factor of it. The factor is re-synced from the body's actual
        // scale at the start of each gesture, so a (re)placement that resets the body to authored scale
        // never makes the next pinch jump.
        private Vector3 _authoredScale = Vector3.one;
        private bool _authoredScaleCaptured;
        private float _scaleFactor = 1f;

        // Two-finger gesture tracking.
        private bool _twoFingerActive;
        private float _prevPinchDistance;
        private Vector2 _prevCentroid;

        private void OnEnable() => EnhancedTouchSupport.Enable();

        private void OnDisable()
        {
            EnhancedTouchSupport.Disable();
            _twoFingerActive = false;
        }

        private void Update()
        {
            Transform body = ResolveBody();
            if (body == null || _camera == null)
            {
                _twoFingerActive = false;
                return;
            }

            var touches = Touch.activeTouches;
            if (touches.Count != 2)
            {
                // Fewer than two touches (single tap/drag is CORE-004's, untouched here) or more than
                // two — not a body-manipulation gesture. End any gesture in progress.
                _twoFingerActive = false;
                return;
            }

            Vector2 p0 = touches[0].screenPosition;
            Vector2 p1 = touches[1].screenPosition;
            float distance = Vector2.Distance(p0, p1);
            Vector2 centroid = (p0 + p1) * 0.5f;

            if (!_twoFingerActive)
            {
                BeginTwoFinger(body, distance, centroid);
                return; // baseline established this frame; manipulation applies from the next
            }

            ApplyPinch(body, distance);
            ApplyDrag(body, centroid);

            _prevPinchDistance = distance;
            _prevCentroid = centroid;
        }

        private Transform ResolveBody()
        {
            IBodyModelRenderer renderer = _services != null ? _services.BodyRenderer : null;
            return renderer != null ? renderer.ModelRoot : null;
        }

        private PlacementMode CurrentMode =>
            _services != null && _services.PlacementProvider != null
                ? _services.PlacementProvider.CurrentMode
                : PlacementMode.Viewer;

        /// <summary>Captures the authored scale once and re-syncs the zoom factor to the body's current scale.</summary>
        private void BeginTwoFinger(Transform body, float distance, Vector2 centroid)
        {
            if (!_authoredScaleCaptured)
            {
                _authoredScale = body.localScale;
                _authoredScaleCaptured = true;
            }

            _scaleFactor = Mathf.Abs(_authoredScale.x) > 1e-6f ? body.localScale.x / _authoredScale.x : 1f;
            _prevPinchDistance = distance;
            _prevCentroid = centroid;
            _twoFingerActive = true;
        }

        /// <summary>Scales the body uniformly by the pinch ratio, clamped to the authored-scale band.</summary>
        private void ApplyPinch(Transform body, float distance)
        {
            float ratio = ManipulationMath.PinchScaleFactor(_prevPinchDistance, distance);
            _scaleFactor = ManipulationMath.ClampUniformScale(
                _scaleFactor * ratio, baseScale: 1f, _minScaleFactor, _maxScaleFactor);
            body.localScale = _authoredScale * _scaleFactor;
        }

        /// <summary>Rotates the model (Viewer) or repositions it (AR), per the D.1 two-finger-drag mapping.</summary>
        private void ApplyDrag(Transform body, Vector2 centroid)
        {
            Vector2 delta = centroid - _prevCentroid;
            if (delta.sqrMagnitude < 1e-6f)
            {
                return;
            }

            if (CurrentMode == PlacementMode.Viewer)
            {
                Vector2 yawPitch = ManipulationMath.DragToYawPitchDegrees(delta, _rotateDegreesPerPixel);
                body.Rotate(Vector3.up, yawPitch.x, Space.World);                  // horizontal → yaw
                body.Rotate(_camera.transform.right, yawPitch.y, Space.World);     // vertical → pitch
            }
            else
            {
                Vector3 worldDelta = ManipulationMath.ScreenPanToWorld(
                    _camera.transform.right, _camera.transform.up, delta, _panMetresPerPixel);
                body.position += worldDelta;
            }
        }

#if UNITY_EDITOR
        // Editor-only smoke tests for the apply path (the editor Game view can't generate two-finger
        // touches without Unity Remote / a device). Invoke from the component context menu in Play mode.
        [ContextMenu("DEV — Scale up")]
        private void DevScaleUp() => DevScale(1.2f);

        [ContextMenu("DEV — Scale down")]
        private void DevScaleDown() => DevScale(1f / 1.2f);

        private void DevScale(float ratio)
        {
            Transform body = ResolveBody();
            if (body == null)
            {
                return;
            }

            if (!_authoredScaleCaptured)
            {
                _authoredScale = body.localScale;
                _authoredScaleCaptured = true;
            }

            float current = Mathf.Abs(_authoredScale.x) > 1e-6f ? body.localScale.x / _authoredScale.x : 1f;
            _scaleFactor = ManipulationMath.ClampUniformScale(
                current * ratio, baseScale: 1f, _minScaleFactor, _maxScaleFactor);
            body.localScale = _authoredScale * _scaleFactor;
        }

        [ContextMenu("DEV — Rotate yaw 15°")]
        private void DevRotateYaw()
        {
            Transform body = ResolveBody();
            if (body != null)
            {
                body.Rotate(Vector3.up, 15f, Space.World);
            }
        }
#endif
    }
}
