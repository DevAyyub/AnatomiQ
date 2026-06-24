using System;
using AnatomiQ.Core;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace AnatomiQ.AR
{
    /// <summary>
    /// ATLAS-003 — AR Placement Modes. Owns the placement-mode state machine (Surface / Space / Viewer
    /// + user selection) and the placement POLICY: where to put the body and how to react to AR
    /// context. It adds plane detection and raycasting (<see cref="ARPlaneManager"/> /
    /// <see cref="ARRaycastManager"/>) on top of CORE-001's session, computes a target pose per mode,
    /// and reparents CORE-002's <see cref="IBodyModelRenderer.ModelRoot"/> under an anchor.
    ///
    /// Boundary held (ATLAS-003 decisions):
    /// • The ANCHOR PRIMITIVE stays in CORE-001 — this controller calls
    ///   <see cref="ARSessionManager.TryCreateContentAnchorAsync"/> /
    ///   <see cref="ARSessionManager.RemoveContentAnchor"/> rather than owning an
    ///   <see cref="ARAnchorManager"/>, so all anchor lifecycle routes through the session manager
    ///   ("all AR state changes through ARSessionManager only"). The plane + raycast managers, by
    ///   contrast, are placement-owned and live here (used from chunk 3).
    /// • It NEVER writes the global <see cref="AppState"/> (CORE-007 is the sole writer); it SUBSCRIBES
    ///   to AppState and reconciles its own <see cref="PlacementMode"/> against it (chunk 5).
    ///
    /// Registered as the runtime <see cref="IPlacementProvider"/> on the <see cref="ServiceRegistry"/>
    /// so the UI mode switcher (and any other Core-side reader) can pull the mode and request changes
    /// without referencing the AR pillar.
    ///
    /// CHUNK 2 adds Space placement (the demo-preferred, instant path): place the body floating at a
    /// fixed distance in front of the camera, anchored, facing the user. The placement geometry itself
    /// lives in <see cref="PlacementMath"/> (Core) so it is unit-testable without the AR pillar. Surface
    /// placement + reticle (chunk 3), Viewer rendering + gestures (chunk 4), and the AppState/permission
    /// reconciliation (chunk 5) are still to come. Viewer here is only a safe parking state for the
    /// space-fail fallback — it flips the mode and detaches the body so nothing is orphaned; chunk 4
    /// gives Viewer its real non-AR render path and positioning.
    /// </summary>
    [DefaultExecutionOrder(-850)] // After ARSessionManager (-900) registers; before AppBootstrap (-500).
    public sealed class PlacementController : MonoBehaviour, IPlacementProvider
    {
        [Header("Service wiring")]
        [Tooltip("The single ServiceRegistry asset, shared with all services. Assign in the inspector.")]
        [SerializeField] private ServiceRegistry _services;

        [Header("CORE-001 session (same AR pillar — the anchor primitive lives here)")]
        [Tooltip("The AR session manager whose async anchor seam this controller calls when placing.")]
        [SerializeField] private ARSessionManager _arSession;

        [Tooltip("The AR camera under XR Origin. Space placement is computed relative to this transform.")]
        [SerializeField] private Camera _arCamera;

        [Header("Placement AR components (assign from this scene's XR Origin)")]
        [Tooltip("Placement-owned: horizontal plane detection for Surface placement. Used from chunk 3.")]
        [SerializeField] private ARPlaneManager _planeManager;
        [Tooltip("Placement-owned: screen-point raycasts against detected planes. Used from chunk 3.")]
        [SerializeField] private ARRaycastManager _raycastManager;

        [Header("Space placement")]
        [Tooltip("Metres in front of the camera to float the body in Space mode. Tune on device.")]
        [Range(0.5f, 5f)]
        [SerializeField] private float _spaceDistance = 2.0f;

        // D.4: the app opens in 3D viewer with no launch-time camera request, so Viewer is the seed.
        private PlacementMode _mode = PlacementMode.Viewer;

        /// <inheritdoc />
        public PlacementMode CurrentMode => _mode;

        /// <inheritdoc />
        public event Action<PlacementMode> OnPlacementModeChanged;

        private void Awake()
        {
            if (_services == null)
            {
                Debug.LogError("[PlacementController] ServiceRegistry not assigned in the inspector.");
                return;
            }

            // Register as the placement provider so the UI switcher / Core readers can pull the mode.
            _services.Register(this);
        }

        private void OnDestroy()
        {
            // AR scenes load/unload during a session, so clear our registry slot on teardown. The
            // registry's identity check makes this safe even if a newer controller already registered.
            if (_services != null)
            {
                _services.ClearPlacementProvider(this);
            }
        }

        /// <inheritdoc />
        public void RequestMode(PlacementMode mode)
        {
            // CHUNK 2: Space + Viewer are live; Surface lands in chunk 3. AppState arbitration and the
            // D.4 permission flow (decline AR while AR_VIEWER_MODE, prompt before entering AR) land in
            // chunk 5 — for now a Space request that can't anchor degrades to Viewer via the guard
            // inside PlaceInSpaceAsync, which is the safe outcome regardless.
            switch (mode)
            {
                case PlacementMode.Space:
                    _ = PlaceInSpaceAsync(); // fire-and-forget; all faults handled inside
                    break;
                case PlacementMode.Viewer:
                    EnterViewer();
                    break;
                case PlacementMode.Surface:
                    Debug.LogWarning(
                        "[PlacementController] Surface placement is implemented in chunk 3; request ignored.");
                    break;
            }
        }

        /// <summary>
        /// Places the body floating <see cref="_spaceDistance"/> metres in front of the camera, anchored
        /// and facing the user. Re-entrant: a fresh request drops the previous anchor first, so the demo
        /// can re-summon the body instantly. On any failure (no session/camera/body, anchor creation
        /// fails, or an exception) it degrades to Viewer per the spec ("Space fails → auto Viewer").
        /// </summary>
        private async Awaitable PlaceInSpaceAsync()
        {
            try
            {
                if (_arSession == null || _arCamera == null)
                {
                    Debug.LogError("[PlacementController] Missing AR session or camera; cannot place in space.");
                    FallBackToViewer();
                    return;
                }

                IBodyModelRenderer renderer = _services != null ? _services.BodyRenderer : null;
                if (renderer == null || renderer.ModelRoot == null)
                {
                    Debug.LogError("[PlacementController] Body renderer/model root unavailable; cannot place.");
                    FallBackToViewer();
                    return;
                }

                // Re-place cleanly: drop any previous anchor before creating the new one.
                _arSession.RemoveContentAnchor();

                Transform cam = _arCamera.transform;
                Pose pose = PlacementMath.ComputeSpacePose(cam.position, cam.rotation, _spaceDistance);

                ARAnchor anchor = await _arSession.TryCreateContentAnchorAsync(pose);
                if (anchor == null)
                {
                    Debug.LogWarning("[PlacementController] Space anchor creation failed; falling back to Viewer.");
                    FallBackToViewer();
                    return;
                }

                AttachBody(renderer.ModelRoot, anchor.transform);
                SetMode(PlacementMode.Space); // mode reflects reality: Space only once actually placed
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlacementController] Space placement threw: {ex.Message}");
                FallBackToViewer();
            }
        }

        /// <summary>
        /// Reparents the body under <paramref name="anchor"/> at the anchor's pose, preserving the
        /// model's authored world scale (anchors carry identity scale, so local == world here).
        /// </summary>
        /// <param name="body">The body model root (CORE-002's reparentable transform).</param>
        /// <param name="anchor">The anchor transform to parent the body under.</param>
        private static void AttachBody(Transform body, Transform anchor)
        {
            Vector3 worldScale = body.lossyScale;
            body.SetParent(anchor, worldPositionStays: false);
            body.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            body.localScale = worldScale;
        }

        /// <summary>
        /// Enters Viewer mode directly (user/UI request): drop the anchor, detach the body, flip mode.
        /// Full Viewer rendering + gestures + positioning land in chunk 4.
        /// </summary>
        private void EnterViewer()
        {
            _arSession?.RemoveContentAnchor();
            DetachBody();
            SetMode(PlacementMode.Viewer);
        }

        /// <summary>
        /// Spec fallback "Space fails → auto Viewer". Same effect as <see cref="EnterViewer"/> but the
        /// anchor (if any) is already gone or invalid by the time we get here.
        /// </summary>
        private void FallBackToViewer()
        {
            DetachBody();
            SetMode(PlacementMode.Viewer);
        }

        /// <summary>
        /// Detaches the body from any anchor, keeping its current world pose so it never vanishes.
        /// Chunk 4 gives Viewer a deliberate non-AR position; this is just the safe interim.
        /// </summary>
        private void DetachBody()
        {
            IBodyModelRenderer renderer = _services != null ? _services.BodyRenderer : null;
            if (renderer == null || renderer.ModelRoot == null)
            {
                return;
            }

            renderer.ModelRoot.SetParent(null, worldPositionStays: true);
        }

        /// <summary>
        /// Sets <see cref="CurrentMode"/> and raises <see cref="OnPlacementModeChanged"/> on a change.
        /// Change-only: re-entering the active mode is a no-op and does not re-fire the event.
        /// </summary>
        /// <param name="mode">The mode to transition to.</param>
        private void SetMode(PlacementMode mode)
        {
            if (mode == _mode)
            {
                return;
            }

            _mode = mode;
            OnPlacementModeChanged?.Invoke(_mode);
        }

#if UNITY_EDITOR
        // Temporary dev triggers for in-editor (XR Simulation) verification — invoke from the
        // component's context menu (⋮) in Play mode. Editor-only; removed before ship, same spirit as
        // CORE-001's ARStatusLogger / CORE-002's DebugSetTierOverride.
        [ContextMenu("DEV — Request Space placement")]
        private void DevRequestSpace() => RequestMode(PlacementMode.Space);

        [ContextMenu("DEV — Request Viewer")]
        private void DevRequestViewer() => RequestMode(PlacementMode.Viewer);
#endif
    }
}
