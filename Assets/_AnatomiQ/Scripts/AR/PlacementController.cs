using AnatomiQ.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Debug = UnityEngine.Debug;

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
    ///   contrast, are placement-owned and live here. SESSION lifecycle + camera presentation also stay
    ///   in CORE-001: chunk 5 drives them through <see cref="ARSessionManager.EnterAr"/> /
    ///   <see cref="ARSessionManager.ExitToViewer"/> rather than touching ARSession directly.
    /// • It NEVER writes the global <see cref="AppState"/> (CORE-007 is the sole writer); it SUBSCRIBES
    ///   to AppState and reconciles its own <see cref="PlacementMode"/> against it (chunk 5).
    ///
    /// Registered as the runtime <see cref="IPlacementProvider"/> on the <see cref="ServiceRegistry"/>
    /// so the UI mode switcher (and any other Core-side reader) can pull the mode and request changes
    /// without referencing the AR pillar.
    ///
    /// CHUNK 2 added Space placement (the demo-preferred, instant path): the body floats at a fixed
    /// distance in front of the camera, anchored, facing the user.
    ///
    /// CHUNK 3 adds Surface placement: with horizontal plane detection on, a reticle tracks the floor
    /// under the screen centre; once the centre ray holds on a single plane for a short dwell ("stable
    /// plane"), the body auto-places standing on it (NOT tap-to-place — single tap is reserved for
    /// CORE-004 organ selection per the D.1 gesture contract). If no stable surface is found within the
    /// timeout, it auto-falls back to Space placement (ATLAS-003 spec: surface &gt;5s → offer Space).
    /// The pose geometry for both modes lives in <see cref="PlacementMath"/> (Core), unit-testable
    /// without the AR pillar.
    ///
    /// CHUNK 5 adds the AppState/permission reconciliation. It SUBSCRIBES to
    /// <see cref="IFallbackManager.OnAppStateChanged"/> and reacts: AR_VIEWER_MODE → force Viewer;
    /// AR_LIMITED → screen-lock the body (freeze + lock-to-screen-centre, C.6) and suspend seeking;
    /// AR_ACTIVE → AR available (consume any pending request / re-anchor after recovery). A user/UI
    /// <see cref="RequestMode"/> for an AR mode while AR is not active triggers the D.4 flow: it calls
    /// <see cref="ARSessionManager.EnterAr"/> (which raises the OS camera prompt — never at launch) and
    /// defers the actual placement until AppState reaches AR_ACTIVE. Arbitration is the pure, unit-tested
    /// <see cref="PlacementPolicy"/> (Core). Viewer presentation (camera backdrop, no pose-driven camera)
    /// is delegated to CORE-001's <see cref="ARSessionManager.ExitToViewer"/>.
    /// </summary>
    [DefaultExecutionOrder(-850)] // After ARSessionManager (-900) registers; before AppBootstrap (-500).
    public sealed class PlacementController : MonoBehaviour, IPlacementProvider
    {
        [Header("Service wiring")]
        [Tooltip("The single ServiceRegistry asset, shared with all services. Assign in the inspector.")]
        [SerializeField] private ServiceRegistry _services;

        [Header("CORE-001 session (same AR pillar — the anchor primitive lives here)")]
        [Tooltip("The AR session manager whose async anchor seam + EnterAr/ExitToViewer this controller calls.")]
        [SerializeField] private ARSessionManager _arSession;

        [Tooltip("The AR camera under XR Origin. Placement poses are computed relative to this transform.")]
        [SerializeField] private Camera _arCamera;

        [Header("Placement AR components (assign from this scene's XR Origin)")]
        [Tooltip("Placement-owned: horizontal plane detection for Surface placement. Toggled on only " +
                 "while actively seeking a surface (perf/battery).")]
        [SerializeField] private ARPlaneManager _planeManager;
        [Tooltip("Placement-owned: screen-point raycasts against detected planes for the reticle.")]
        [SerializeField] private ARRaycastManager _raycastManager;

        [Header("Space placement")]
        [Tooltip("Metres in front of the camera to float the body in Space mode. Tune on device.")]
        [Range(0.5f, 5f)]
        [SerializeField] private float _spaceDistance = 2.0f;

        [Tooltip("Viewer / screen-lock only: extra vertical nudge (metres, + = up) applied AFTER the body is " +
                 "centred on the camera sight-line. 0 centres the whole figure; raise slightly if you want " +
                 "the head higher in frame. Does not affect anchored AR (Surface/Space) placement.")]
        [Range(-1f, 1f)]
        [SerializeField] private float _viewerVerticalOffset = 0f;

        [Header("Surface placement (chunk 3)")]
        [Tooltip("Seconds the screen-centre ray must hold on the SAME plane before the body auto-places. " +
                 "Avoids placing on a flickering first detection.")]
        [Range(0.1f, 2f)]
        [SerializeField] private float _surfaceStableDwell = 0.6f;

        [Tooltip("Seconds to look for a horizontal surface before auto-falling back to Space placement " +
                 "(ATLAS-003 spec: surface >5s → offer Space).")]
        [Range(1f, 15f)]
        [SerializeField] private float _surfaceSeekTimeout = 5f;

        [Tooltip("Optional reticle prefab shown on the detected floor while seeking a surface. If unset, " +
                 "a simple procedural disc is used so the reticle is never invisible.")]
        [SerializeField] private GameObject _reticlePrefab;

        [Header("AR entry (chunk 5 — D.4)")]
        [Tooltip("Seconds to wait for the AR session to start tracking after the user taps an AR mode " +
                 "before giving up and returning to Viewer (covers denial / unsupported / stalled bring-up).")]
        [Range(3f, 30f)]
        [SerializeField] private float _arEntryTimeout = 12f;

        // Radius (metres) of the dev-grade procedural reticle used when no prefab is assigned.
        private const float RETICLE_DISC_RADIUS = 0.075f;

        // D.4: the app opens in 3D viewer with no launch-time camera request, so Viewer is the seed.
        private PlacementMode _mode = PlacementMode.Viewer;

        // Surface-seek state (chunk 3). Driven from Update only while _seekingSurface is true.
        private readonly List<ARRaycastHit> _raycastHits = new();
        private bool _seekingSurface;
        private bool _placing; // guards against overlapping async placements
        private float _seekStartTime;
        private float _stableDwell;
        private TrackableId _lastHitTrackable = TrackableId.invalidId;
        private GameObject _reticle;

        // AppState reconciliation state (chunk 5).
        private IFallbackManager _fallback;       // cached subscription target
        private AppState _appState = AppState.AR_VIEWER_MODE;
        private bool _trackingLocked;             // AR_LIMITED → body screen-pinned, seeking suspended
        private bool _resummonOnRecovery;         // re-anchor in Space once tracking returns
        private PlacementMode? _pendingArMode;    // an AR mode requested while AR was not yet active
        private float _pendingDeadline;           // watchdog time for the pending AR entry

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

            // Surface plane detection is off until the user actively seeks a surface (perf/battery).
            // ATLAS-003 owns this plane manager; ARSessionManager's own plane field must be left None.
            if (_planeManager != null)
            {
                _planeManager.enabled = false;
            }
        }

        private void Start()
        {
            // Subscribe AFTER all Awakes (FallbackManager registers first at -1000), then sync to the
            // current AppState — which is AR_VIEWER_MODE at launch (D.4), so this frames the body in Viewer.
            _fallback = _services != null ? _services.FallbackManager : null;
            if (_fallback != null)
            {
                _fallback.OnAppStateChanged += HandleAppStateChanged;
                HandleAppStateChanged(_fallback.CurrentState);
            }
            else
            {
                Debug.LogWarning("[PlacementController] FallbackManager unavailable; AppState reconciliation off.");
                EnterViewer(); // safe baseline regardless
            }
        }

        private void OnDestroy()
        {
            if (_fallback != null)
            {
                _fallback.OnAppStateChanged -= HandleAppStateChanged;
            }

            if (_reticle != null)
            {
                Destroy(_reticle);
            }

            // AR scenes load/unload during a session, so clear our registry slot on teardown. The
            // registry's identity check makes this safe even if a newer controller already registered.
            if (_services != null)
            {
                _services.ClearPlacementProvider(this);
            }
        }

        private void Update()
        {
            // Watchdog: an AR entry that never reaches AR_ACTIVE (permission denied / unsupported /
            // stalled bring-up) returns to Viewer so the user is never stuck "entering AR".
            if (_pendingArMode.HasValue && Time.time >= _pendingDeadline)
            {
                Debug.LogWarning("[PlacementController] AR entry timed out; returning to Viewer.");
                ResolvePendingFailure();
            }

            if (_trackingLocked)
            {
                // C.6 + CORE-001 LockToScreenCenter hint: keep the body usable through degraded tracking.
                LockBodyToScreenCenter();
                return; // suspend surface-seek while tracking is degraded
            }

            if (_seekingSurface)
            {
                TickSurfaceSeek();
            }
        }

        /// <inheritdoc />
        public void RequestMode(PlacementMode mode)
        {
            // Arbitrate the request against the current AppState (pure Core logic, unit-tested).
            switch (PlacementPolicy.ResolveRequest(mode, _appState))
            {
                case PlacementAction.ForceViewer:
                    EnterViewer();
                    break;

                case PlacementAction.PlaceNow:
                    PlaceForMode(mode);
                    break;

                case PlacementAction.EnterArThenPlace:
                    // D.4: bring the session up now (this is when ARCore raises the OS camera prompt).
                    // The actual placement waits for CORE-007 to promote AppState to AR_ACTIVE — see
                    // HandleAppStateChanged. The UI shows the one-sentence rationale BEFORE calling here.
                    _pendingArMode = mode;
                    _pendingDeadline = Time.time + _arEntryTimeout;
                    _arSession?.EnterAr();
                    break;
            }
        }

        /// <summary>Places the body in the requested AR mode immediately (AR is already active).</summary>
        private void PlaceForMode(PlacementMode mode)
        {
            if (mode == PlacementMode.Surface)
            {
                BeginSurfaceSeek();
            }
            else
            {
                EndSurfaceSeek(); // cancel any in-progress surface seek before re-placing
                _ = PlaceInSpaceAsync();
            }
        }

        // ── AppState reconciliation (chunk 5) ─────────────────────────────────────────────────────────

        /// <summary>
        /// Reconciles the controller's placement state against CORE-007's global <see cref="AppState"/>
        /// (the single AppState writer). AR_ACTIVE consumes a pending AR request or re-anchors after a
        /// tracking recovery; AR_LIMITED screen-locks the body; AR_VIEWER_MODE collapses to Viewer
        /// UNLESS an AR entry is in flight (then it is just a transient bring-up step — wait, or bail if
        /// CORE-001 reports the entry actually failed).
        /// </summary>
        /// <param name="state">The new global application state.</param>
        private void HandleAppStateChanged(AppState state)
        {
            _appState = state;

            switch (state)
            {
                case AppState.AR_ACTIVE:
                    ExitTrackingLock();
                    if (_pendingArMode.HasValue)
                    {
                        PlacementMode pending = _pendingArMode.Value;
                        ClearPending();
                        PlaceForMode(pending);
                    }
                    else if (_resummonOnRecovery)
                    {
                        _resummonOnRecovery = false;
                        _ = PlaceInSpaceAsync(); // re-anchor after tracking recovered from a lock
                    }
                    break;

                case AppState.AR_LIMITED:
                    EnterTrackingLock();
                    break;

                case AppState.AR_VIEWER_MODE:
                    if (_pendingArMode.HasValue)
                    {
                        // AR is either still initializing (wait) or has actually failed. AppState alone
                        // can't tell these apart — read CORE-001's finer-grained status to bail early on
                        // a genuine failure (permission denied / unsupported) rather than wait the watchdog.
                        ArTrackingStatus status =
                            _arSession != null ? _arSession.Status : ArTrackingStatus.Unavailable;
                        if (status == ArTrackingStatus.PermissionDenied ||
                            status == ArTrackingStatus.Unavailable)
                        {
                            ResolvePendingFailure();
                        }
                        // else Initializing: keep waiting; the watchdog still guards a stall.
                    }
                    else if (PlacementPolicy.ShouldCollapseToViewer(state, hasPendingArEntry: false))
                    {
                        ExitTrackingLock();
                        EnterViewer();
                    }
                    break;
            }
        }

        /// <summary>Begins the AR_LIMITED screen-lock: detach the body and pin it to the screen each frame.</summary>
        private void EnterTrackingLock()
        {
            if (_trackingLocked)
            {
                return;
            }

            _trackingLocked = true;
            _resummonOnRecovery = true; // re-anchor when tracking returns
            EndSurfaceSeek();
            _arSession?.RemoveContentAnchor(); // release the (now-unreliable) anchor
            DetachBody();                       // keep world pose; LockBodyToScreenCenter takes over next frame
        }

        /// <summary>Ends the screen-lock (tracking recovered or we left AR). The re-anchor is handled by caller.</summary>
        private void ExitTrackingLock() => _trackingLocked = false;

        /// <summary>
        /// Pins the body a fixed distance in front of the camera (screen-centre), recomputed each frame,
        /// so it stays visible and usable while tracking is degraded/lost — even as the camera pose
        /// jitters. Reuses the Space pose math. No-op if the body or camera is unavailable.
        /// </summary>
        private void LockBodyToScreenCenter()
        {
            IBodyModelRenderer renderer = _services != null ? _services.BodyRenderer : null;
            if (renderer == null || renderer.ModelRoot == null || _arCamera == null)
            {
                return;
            }

            Transform cam = _arCamera.transform;
            Pose pose = PlacementMath.ComputeSpacePose(cam.position, cam.rotation, _spaceDistance);
            renderer.ModelRoot.SetPositionAndRotation(pose.position, pose.rotation);
            AlignBodyVerticalCenter(renderer.ModelRoot, pose.position.y);
        }

        /// <summary>
        /// Vertically centres the body on the camera sight-line. The model's pivot is at its feet, so
        /// placing the pivot at the sight-line height pushes the head off the top of the screen — fine in
        /// anchored AR where the user tilts the phone, but wrong for the fixed-camera Viewer / screen-lock.
        /// This shifts the body so its rendered-bounds centre sits at <paramref name="targetWorldY"/>
        /// (plus the optional <see cref="_viewerVerticalOffset"/> nudge), framing the whole figure. The
        /// body is upright (yaw-only), so the vertical extent is orientation-stable. No-op if there are
        /// no renderers yet (placeholder/not-ready) so framing degrades safely.
        /// </summary>
        private void AlignBodyVerticalCenter(Transform modelRoot, float targetWorldY)
        {
            var renderers = modelRoot.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            float delta = (targetWorldY + _viewerVerticalOffset) - bounds.center.y;
            modelRoot.position += new Vector3(0f, delta, 0f);
        }
        private void ResolvePendingFailure()
        {
            ClearPending();
            ExitTrackingLock();
            EnterViewer();
        }

        private void ClearPending() => _pendingArMode = null;

        // ── Space placement ─────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Places the body floating <see cref="_spaceDistance"/> metres in front of the camera, anchored
        /// and facing the user. Re-entrant: a fresh request drops the previous anchor first, so the demo
        /// can re-summon the body instantly. On any failure (no session/camera/body, anchor creation
        /// fails, or an exception) it degrades to Viewer per the spec ("Space fails → auto Viewer").
        /// </summary>
        private async Awaitable PlaceInSpaceAsync()
        {
            _placing = true;
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

                Transform cam = _arCamera.transform;
                Pose pose = PlacementMath.ComputeSpacePose(cam.position, cam.rotation, _spaceDistance);

                bool placed = await TryAnchorAndAttachAsync(pose, renderer);
                if (placed)
                {
                    SetMode(PlacementMode.Space); // mode reflects reality: Space only once actually placed
                }
                else
                {
                    Debug.LogWarning("[PlacementController] Space anchor creation failed; falling back to Viewer.");
                    FallBackToViewer();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlacementController] Space placement threw: {ex.Message}");
                FallBackToViewer();
            }
            finally
            {
                _placing = false;
            }
        }

        // ── Surface placement (chunk 3) ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Begins seeking a horizontal surface: turns on plane detection and starts the per-frame reticle
        /// raycast + dwell/timeout state machine in <see cref="TickSurfaceSeek"/>. The mode does NOT flip
        /// to Surface until a stable plane is actually found and the body anchors (mode reflects reality).
        /// If the AR prerequisites are missing it skips straight to the Space fallback.
        /// </summary>
        private void BeginSurfaceSeek()
        {
            if (_arSession == null || _arCamera == null || _raycastManager == null || _planeManager == null)
            {
                Debug.LogError("[PlacementController] Surface placement needs AR session, camera, plane and " +
                               "raycast managers; falling back to Space.");
                _ = PlaceInSpaceAsync();
                return;
            }

            if (_placing || _seekingSurface)
            {
                return; // a placement is resolving, or we're already seeking
            }

            _planeManager.requestedDetectionMode = PlaneDetectionMode.Horizontal;
            _planeManager.enabled = true;

            _stableDwell = 0f;
            _lastHitTrackable = TrackableId.invalidId;
            _seekStartTime = Time.time;
            _seekingSurface = true;
        }

        /// <summary>
        /// Per-frame while seeking: enforce the timeout, raycast the screen centre against horizontal
        /// planes, drive the reticle, and place once the centre ray has held on a single plane long
        /// enough to count as "stable". Tap-free by design — single tap belongs to CORE-004 (D.1).
        /// </summary>
        private void TickSurfaceSeek()
        {
            // ATLAS-003 spec: no surface within the timeout → auto-offer Space (the always-working path).
            if (Time.time - _seekStartTime >= _surfaceSeekTimeout)
            {
                Debug.Log("[PlacementController] No stable surface within timeout; falling back to Space placement.");
                EndSurfaceSeek();
                _ = PlaceInSpaceAsync();
                return;
            }

            var screenCentre = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            if (!_raycastManager.Raycast(screenCentre, _raycastHits, TrackableType.PlaneWithinPolygon))
            {
                HideReticle();
                _stableDwell = 0f;
                _lastHitTrackable = TrackableId.invalidId;
                return;
            }

            // Hits are sorted nearest-first.
            ARRaycastHit hit = _raycastHits[0];
            ShowReticleAt(hit.pose);

            // "Stable" = the centre ray stays on the SAME plane for the dwell window. Switching planes
            // restarts the dwell so we never place on a plane that's still settling/jumping.
            if (hit.trackableId == _lastHitTrackable)
            {
                _stableDwell += Time.deltaTime;
            }
            else
            {
                _lastHitTrackable = hit.trackableId;
                _stableDwell = 0f;
            }

            if (_stableDwell >= _surfaceStableDwell)
            {
                Pose hitPose = hit.pose;
                EndSurfaceSeek();
                _ = PlaceOnSurfaceAsync(hitPose);
            }
        }

        /// <summary>
        /// Stops the surface-seek loop, hides the reticle, and turns plane detection back off. Safe to
        /// call when not seeking.
        /// </summary>
        private void EndSurfaceSeek()
        {
            _seekingSurface = false;
            _stableDwell = 0f;
            _lastHitTrackable = TrackableId.invalidId;
            HideReticle();

            if (_planeManager != null)
            {
                _planeManager.enabled = false;
            }
        }

        /// <summary>
        /// Anchors the body standing on the detected plane, lifted so its feet rest on the surface and
        /// yawed to face the user. On failure, degrades to the Space path (which itself degrades to
        /// Viewer), preserving the spec's "always a working mode" guarantee.
        /// </summary>
        /// <param name="surfaceHitPose">The plane raycast hit pose (floor point) that triggered placement.</param>
        private async Awaitable PlaceOnSurfaceAsync(Pose surfaceHitPose)
        {
            _placing = true;
            try
            {
                IBodyModelRenderer renderer = _services != null ? _services.BodyRenderer : null;
                if (_arSession == null || _arCamera == null || renderer == null || renderer.ModelRoot == null)
                {
                    Debug.LogError("[PlacementController] Surface placement prerequisites missing; falling back to Space.");
                    await PlaceInSpaceAsync();
                    return;
                }

                // Lift the body so its feet (model's lowest point) rest on the plane, and yaw it to face
                // the user. Geometry is pure + unit-tested in Core (PlacementMath.ComputeSurfacePose).
                float footDrop = MeasureFootDrop(renderer.ModelRoot);
                Pose bodyPose = PlacementMath.ComputeSurfacePose(
                    surfaceHitPose.position, _arCamera.transform.position, footDrop);

                bool placed = await TryAnchorAndAttachAsync(bodyPose, renderer);
                if (placed)
                {
                    SetMode(PlacementMode.Surface); // mode reflects reality: Surface only once placed
                }
                else
                {
                    Debug.LogWarning("[PlacementController] Surface anchor creation failed; falling back to Space.");
                    await PlaceInSpaceAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlacementController] Surface placement threw: {ex.Message}");
                FallBackToViewer();
            }
            finally
            {
                _placing = false;
            }
        }

        /// <summary>
        /// Measures how far the model's lowest rendered point sits BELOW its root origin, in world
        /// metres at the model's current (authored) scale — i.e. the foot offset for Surface placement.
        /// Returns 0 if the root has no renderers (placeholder/empty), so placement still works. The
        /// body is only ever placed upright/yaw-only, so world bounds min.y is orientation-stable.
        /// </summary>
        private static float MeasureFootDrop(Transform modelRoot)
        {
            var renderers = modelRoot.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return 0f;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return Mathf.Max(0f, modelRoot.position.y - bounds.min.y);
        }

        // ── Shared anchor + attach ──────────────────────────────────────────────────────────────────

        /// <summary>
        /// Drops any existing anchor, creates a fresh content anchor at <paramref name="pose"/> via the
        /// CORE-001 seam, and reparents the body under it. Returns false (no side effect on mode) if the
        /// anchor could not be created, so the caller can run its own fallback. Shared by Space and
        /// Surface so both paths anchor identically.
        /// </summary>
        private async Awaitable<bool> TryAnchorAndAttachAsync(Pose pose, IBodyModelRenderer renderer)
        {
            // Re-place cleanly: drop any previous anchor before creating the new one.
            _arSession.RemoveContentAnchor();

            ARAnchor anchor = await _arSession.TryCreateContentAnchorAsync(pose);
            if (anchor == null)
            {
                return false;
            }

            AttachBody(renderer.ModelRoot, anchor.transform);
            return true;
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

        // ── Viewer fallback ─────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Enters Viewer mode deliberately (user/UI request, or AppState collapse to viewer): stop the
        /// AR session + switch the camera to the non-AR backdrop (CORE-001 <see cref="ARSessionManager.ExitToViewer"/>,
        /// chunk 5), drop the anchor, frame the body in front of the camera, flip mode. Clears any pending
        /// AR entry and the tracking-lock.
        /// </summary>
        private void EnterViewer()
        {
            ClearPending();
            ExitTrackingLock();
            EndSurfaceSeek();

            _arSession?.RemoveContentAnchor(); // while the session is still up, so the op is valid
            _arSession?.ExitToViewer();        // then stop the session + apply the Viewer backdrop
            FrameBodyForViewer();
            SetMode(PlacementMode.Viewer);
        }

        /// <summary>
        /// Spec fallback "Space fails → auto Viewer" / placement-prerequisites missing. A LIGHT parking
        /// state: it frames the body as Viewer and flips the mode, but does NOT tear the AR session down
        /// (a fluke anchor miss while AR is healthy shouldn't kill the session — the user can retry an AR
        /// mode instantly). A genuine AR-unavailable situation is handled by the AppState path, which
        /// routes through <see cref="EnterViewer"/> and does stop the session.
        /// </summary>
        private void FallBackToViewer()
        {
            FrameBodyForViewer();
            SetMode(PlacementMode.Viewer);
        }

        /// <summary>
        /// Viewer presentation (chunk 4): detach the body to the scene root, then frame it at the
        /// standard distance in front of the camera, facing the user (reusing the Space pose math, no
        /// anchor). The user then turns/zooms it with two-finger gestures (BodyManipulator). Scale is
        /// left as-is so a user's zoom survives a mode toggle. If the camera/body is unavailable it
        /// degrades to a plain detach so the body is never orphaned.
        /// </summary>
        private void FrameBodyForViewer()
        {
            DetachBody(); // reparent to scene root, keeping world pose (and null-safe)

            IBodyModelRenderer renderer = _services != null ? _services.BodyRenderer : null;
            if (renderer == null || renderer.ModelRoot == null || _arCamera == null)
            {
                return;
            }

            Transform cam = _arCamera.transform;
            Pose pose = PlacementMath.ComputeSpacePose(cam.position, cam.rotation, _spaceDistance);
            renderer.ModelRoot.SetPositionAndRotation(pose.position, pose.rotation);
            AlignBodyVerticalCenter(renderer.ModelRoot, pose.position.y);
        }

        /// <summary>
        /// Detaches the body from any anchor, keeping its current world pose so it never vanishes.
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

        // ── Reticle ─────────────────────────────────────────────────────────────────────────────────

        /// <summary>Moves the reticle to <paramref name="pose"/> (flush on the plane) and shows it.</summary>
        private void ShowReticleAt(Pose pose)
        {
            EnsureReticle();
            _reticle.transform.SetPositionAndRotation(pose.position, pose.rotation);
            if (!_reticle.activeSelf)
            {
                _reticle.SetActive(true);
            }
        }

        /// <summary>Hides the reticle without destroying it (reused across seeks).</summary>
        private void HideReticle()
        {
            if (_reticle != null && _reticle.activeSelf)
            {
                _reticle.SetActive(false);
            }
        }

        /// <summary>Lazily creates the reticle from the prefab, or a procedural disc if none is set.</summary>
        private void EnsureReticle()
        {
            if (_reticle != null)
            {
                return;
            }

            _reticle = _reticlePrefab != null ? Instantiate(_reticlePrefab) : BuildProceduralReticle();
            _reticle.name = "PlacementReticle";
            _reticle.SetActive(false);
        }

        /// <summary>
        /// Dev-grade fallback reticle so the surface-seek aim point is never invisible (mirrors CORE-002's
        /// placeholder approach). A thin disc lying flat; its collider is stripped so it never intercepts
        /// CORE-004 organ-selection physics rays (chunk 4). Replace with <see cref="_reticlePrefab"/> for
        /// the real visual.
        /// </summary>
        private static GameObject BuildProceduralReticle()
        {
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            if (disc.TryGetComponent(out Collider col))
            {
                Destroy(col);
            }

            // Default cylinder is 2 units tall (local Y) and 1 unit diameter. Flatten Y, scale X/Z to
            // the disc diameter so it reads as a ring lying on the plane (local Y = plane normal).
            disc.transform.localScale = new Vector3(RETICLE_DISC_RADIUS * 2f, 0.0015f, RETICLE_DISC_RADIUS * 2f);

            if (disc.TryGetComponent(out Renderer r))
            {
                r.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"))
                {
                    color = new Color(0.2f, 0.8f, 1f, 1f)
                };
            }

            return disc;
        }

        // ── Mode bookkeeping ────────────────────────────────────────────────────────────────────────

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
        [ContextMenu("DEV — Request Surface placement")]
        private void DevRequestSurface() => RequestMode(PlacementMode.Surface);

        [ContextMenu("DEV — Request Space placement")]
        private void DevRequestSpace() => RequestMode(PlacementMode.Space);

        [ContextMenu("DEV — Request Viewer")]
        private void DevRequestViewer() => RequestMode(PlacementMode.Viewer);
#endif
    }
}
