using System;
using System.Collections;
using AnatomiQ.Core;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem.XR; // TrackedPoseDriver (Input System) — the XR Origin (Mobile AR) camera driver.
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace AnatomiQ.AR
{
    /// <summary>
    /// CORE-001 — AR Session Manager. The single owner of the ARCore session lifecycle (via AR
    /// Foundation): availability check, install, enable, plane detection, anchor management, and
    /// teardown. Every AR feature routes session/tracking concerns through this manager; nothing else
    /// touches the AR Foundation components directly.
    ///
    /// It is also the runtime <see cref="IArTrackingProvider"/>: it translates AR Foundation's
    /// <see cref="ARSession.state"/> + <see cref="ARSession.notTrackingReason"/> into the Core-local
    /// <see cref="ArTrackingStatus"/> and registers itself with the <see cref="ServiceRegistry"/>, so
    /// CORE-007's <c>CheckArTracking</c> can PULL the status and map it to <see cref="AppState"/>.
    /// AR Foundation types never leave this assembly — only the translated enum crosses into Core.
    ///
    /// Two consumers, two paths (CORE-001 design): the GLOBAL <see cref="AppState"/> is reconciled by
    /// CORE-007 on its ~1s monitor cadence (pull). The INSTANT visual reaction to tracking loss
    /// (freeze the model at the last anchor + lock-to-screen-center) is driven by this manager's own
    /// change-only <see cref="OnTrackingStateChanged"/> event, which CORE-002's renderer subscribes to
    /// when it lands. The model freeze/lock visual itself is CORE-002's responsibility; CORE-001
    /// provides the signal, the content anchor, and the <see cref="LockToScreenCenter"/> hint.
    ///
    /// CHUNK 5 (ATLAS-003) — D.4 session gating + Viewer/AR presentation. Per the UX D.4 rule the app
    /// opens in 3D Viewer with NO launch-time camera request: the session stays DISABLED at start
    /// (<see cref="_enterArOnStart"/> = false), <see cref="_cameraBackground"/> is off, the
    /// <see cref="_trackedPoseDriver"/> is off (the camera does not chase device motion), and the
    /// camera clears to <see cref="_viewerBackdropColor"/>. ATLAS-003 calls <see cref="EnterAr"/> when
    /// the user taps "AR Mode" — THAT is when ARCore raises the OS camera prompt — and
    /// <see cref="ExitToViewer"/> to return to the non-AR baseline. This keeps "ALL AR state changes
    /// routed through ARSessionManager only": the placement controller decides WHEN, this manager owns
    /// the HOW. (This replaced the prior device-verified "enable at Start" behavior — re-verify on the
    /// Poco; flip <see cref="_enterArOnStart"/> to reproduce the old launch path for isolated testing.)
    /// </summary>
    [DefaultExecutionOrder(-900)] // After FallbackManager (-1000); registers early, before AppBootstrap (-500).
    public sealed class ARSessionManager : MonoBehaviour, IArTrackingProvider
    {
        [Header("Service wiring")]
        [Tooltip("The single ServiceRegistry asset, shared with all services. Assign in the inspector.")]
        [SerializeField] private ServiceRegistry _services;

        [Header("AR Foundation components (assign from this scene)")]
        [SerializeField] private ARSession _session;
        [SerializeField] private XROrigin _xrOrigin;
        [SerializeField] private ARPlaneManager _planeManager;
        [SerializeField] private ARAnchorManager _anchorManager;

        [Header("Presentation (chunk 5 — Viewer vs AR camera)")]
        [Tooltip("The XR Origin camera. If left null, falls back to XROrigin.Camera. Its clear flags / " +
                 "background colour are swapped between AR (passthrough) and Viewer (solid backdrop).")]
        [SerializeField] private Camera _arCamera;
        [Tooltip("Renders the device camera feed as the AR background. Disabled in Viewer mode. If null, " +
                 "resolved from the AR camera in Awake.")]
        [SerializeField] private ARCameraBackground _cameraBackground;
        [Tooltip("Drives the camera pose from device tracking. Disabled in Viewer so the camera stays " +
                 "put (model-centric Viewer). If null, resolved from the AR camera in Awake.")]
        [SerializeField] private TrackedPoseDriver _trackedPoseDriver;
        [Tooltip("Solid backdrop shown behind the body in Viewer mode (no camera feed).")]
        [SerializeField] private Color _viewerBackdropColor = new Color(0.06f, 0.07f, 0.10f, 1f);

        [Header("Behaviour")]
        [Tooltip("If the device supports AR but needs the ARCore app installed/updated, attempt it on entry.")]
        [SerializeField] private bool _attemptInstallIfNeeded = true;
        [Tooltip("D.4: leave OFF so the app opens in Viewer with no launch-time camera request; AR (and " +
                 "the camera prompt) start only when the user taps AR Mode. Turn ON only to reproduce the " +
                 "old 'session up at Start' path for isolated CORE-001 testing.")]
        [SerializeField] private bool _enterArOnStart = false;

        private ArTrackingStatus _status = ArTrackingStatus.Initializing;

        // Captured once in Awake so AR presentation can restore exactly what the scene authored.
        private CameraClearFlags _originalClearFlags;
        private Color _originalBackgroundColor;
        private bool _presentationCaptured;

        // True only while an EnterAr bring-up coroutine is in flight (idempotency guard).
        private bool _enteringAr;

#if UNITY_ANDROID
        // Set by the camera-permission request callbacks. When true, status reports PermissionDenied
        // (→ CORE-007 AR_VIEWER_MODE) so the UI can show the "camera needed" explainer.
        private bool _cameraPermissionDenied;
#endif

        /// <summary>
        /// CORE-001's content anchor — the world anchor virtual content (e.g. CORE-002's body model)
        /// is parented to. Null until <see cref="TryCreateContentAnchorAsync"/> succeeds.
        /// </summary>
        public ARAnchor ContentAnchor { get; private set; }

        /// <inheritdoc />
        public ArTrackingStatus Status => _status;

        /// <summary>True while the AR session component is enabled (the session is running or starting).</summary>
        public bool IsArSessionActive => _session != null && _session.enabled;

        /// <summary>
        /// Raised only when <see cref="Status"/> changes (change-only broadcast — no per-frame
        /// re-fire). CORE-002 subscribes for the instant freeze + lock-to-screen-center reaction; a
        /// UI layer may subscribe for the tracking-lost warning. Late subscribers should read
        /// <see cref="Status"/> immediately after subscribing, since they miss the prior transition.
        /// </summary>
        public event Action<ArTrackingStatus> OnTrackingStateChanged;

        /// <summary>
        /// Advisory hint that CORE-002 should freeze the model at the last anchor and lock it to the
        /// screen centre — true while tracking is degraded or lost. Derived from <see cref="Status"/>;
        /// the actual visual behaviour lives in CORE-002 / ATLAS-003.
        /// </summary>
        public bool LockToScreenCenter =>
            _status == ArTrackingStatus.Limited || _status == ArTrackingStatus.NotTracking;

        private void Awake()
        {
            if (_services == null)
            {
                Debug.LogError("[ARSessionManager] ServiceRegistry not assigned in the inspector.");
                return;
            }

            // Resolve presentation components and capture the authored camera background so AR mode can
            // restore it exactly. Done before any presentation change so the capture is pristine.
            if (_arCamera == null && _xrOrigin != null)
            {
                _arCamera = _xrOrigin.Camera;
            }

            if (_arCamera != null)
            {
                if (_cameraBackground == null)
                {
                    _arCamera.TryGetComponent(out _cameraBackground);
                }

                if (_trackedPoseDriver == null)
                {
                    _arCamera.TryGetComponent(out _trackedPoseDriver);
                }

                _originalClearFlags = _arCamera.clearFlags;
                _originalBackgroundColor = _arCamera.backgroundColor;
                _presentationCaptured = true;
            }
            else
            {
                Debug.LogWarning("[ARSessionManager] No AR camera resolved; Viewer/AR presentation swap disabled.");
            }

            // Register as the AR tracking provider. CheckArTracking pulls Status from the registry.
            _services.Register(this);
        }

        /// <summary>
        /// D.4: resolve AR availability cheaply at startup WITHOUT enabling the session or requesting the
        /// camera, then open in the non-AR Viewer presentation. AR is started later, on demand, via
        /// <see cref="EnterAr"/> (unless <see cref="_enterArOnStart"/> is set for testing). Resolving
        /// availability here means <see cref="Status"/> already distinguishes Unavailable from
        /// Initializing before the user ever taps AR Mode.
        /// </summary>
        private IEnumerator Start()
        {
            if (_session == null)
            {
                Debug.LogError("[ARSessionManager] ARSession not assigned; staying in viewer mode.");
                yield break;
            }

            // Open in the non-AR presentation (session stays disabled — no camera request yet).
            ApplyViewerPresentation();

            if (ARSession.state == ARSessionState.None ||
                ARSession.state == ARSessionState.CheckingAvailability)
            {
                yield return ARSession.CheckAvailability();
            }

            UpdateStatus();

            if (_enterArOnStart && ARSession.state != ARSessionState.Unsupported)
            {
                EnterAr();
            }
        }

        // Recompute every frame: ARSession.notTrackingReason can change while ARSession.state holds at
        // SessionInitializing (e.g. excessive motion → insufficient light), and there is no event for
        // that. The computation is a couple of enum reads + a switch — negligible. UpdateStatus only
        // fires the event when the mapped status actually changes (change-only).
        private void Update() => UpdateStatus();

        private void OnDestroy()
        {
            // AR scenes load/unload during a session, so clear our registry slot on teardown. The
            // registry's identity check makes this safe even if a newer manager already registered.
            if (_services != null)
            {
                _services.ClearArTrackingProvider(this);
            }
        }

        // ── D.4 session gating + presentation (chunk 5) ───────────────────────────────────────────────

        /// <summary>
        /// Brings the AR session up on demand (the user tapped "AR Mode"): resolve availability / install
        /// if needed, request the camera permission (this is when the OS prompt appears — D.4: never at
        /// launch), enable the session, and switch the camera to AR passthrough presentation. Idempotent
        /// and safe to call when AR is already active. If the device is unsupported it stays in Viewer
        /// and <see cref="Status"/> reports <see cref="ArTrackingStatus.Unavailable"/>.
        /// </summary>
        public void EnterAr()
        {
            if (_session == null || _enteringAr)
            {
                return;
            }

            StartCoroutine(EnterArRoutine());
        }

        private IEnumerator EnterArRoutine()
        {
            _enteringAr = true;

            try
            {
                if (ARSession.state == ARSessionState.None ||
                    ARSession.state == ARSessionState.CheckingAvailability)
                {
                    yield return ARSession.CheckAvailability();
                }

                if (ARSession.state == ARSessionState.NeedsInstall && _attemptInstallIfNeeded)
                {
                    yield return ARSession.Install();
                }

                if (ARSession.state == ARSessionState.Unsupported)
                {
                    // No ARCore here — leave the session off, stay in Viewer. CORE-007 reads
                    // Unavailable → AR_VIEWER_MODE and the UI explains "device doesn't support AR".
                    ApplyViewerPresentation();
                    UpdateStatus();
                    yield break;
                }

                RequestCameraPermissionIfNeeded();
                _session.enabled = true;
                ApplyArPresentation();
                UpdateStatus();
            }
            finally
            {
                _enteringAr = false;
            }
        }

        /// <summary>
        /// Stops the AR session and returns to the non-AR Viewer presentation (camera feed off, pose
        /// driver off, solid backdrop). Called by ATLAS-003 when the user selects Viewer or when global
        /// AppState collapses to viewer. Idempotent.
        /// </summary>
        public void ExitToViewer()
        {
            if (_session != null)
            {
                _session.enabled = false;
            }

            ApplyViewerPresentation();
            UpdateStatus();
        }

        /// <summary>Switches the camera to AR passthrough: background on, pose driver on, authored clears.</summary>
        private void ApplyArPresentation()
        {
            if (_cameraBackground != null)
            {
                _cameraBackground.enabled = true;
            }

            if (_trackedPoseDriver != null)
            {
                _trackedPoseDriver.enabled = true;
            }

            if (_arCamera != null && _presentationCaptured)
            {
                _arCamera.clearFlags = _originalClearFlags;
                _arCamera.backgroundColor = _originalBackgroundColor;
            }
        }

        /// <summary>
        /// Switches the camera to the non-AR Viewer: background off, pose driver off (camera stays put —
        /// the body is framed in front of it by ATLAS-003), solid backdrop colour.
        /// </summary>
        private void ApplyViewerPresentation()
        {
            if (_cameraBackground != null)
            {
                _cameraBackground.enabled = false;
            }

            if (_trackedPoseDriver != null)
            {
                _trackedPoseDriver.enabled = false;
            }

            if (_arCamera != null)
            {
                _arCamera.clearFlags = CameraClearFlags.SolidColor;
                _arCamera.backgroundColor = _viewerBackdropColor;
            }
        }

        // ── Plane detection + anchors (unchanged) ─────────────────────────────────────────────────────

        /// <summary>Enables or disables plane detection without exposing the ARPlaneManager directly.</summary>
        /// <param name="enabled">True to detect planes, false to stop.</param>
        public void SetPlaneDetectionEnabled(bool enabled)
        {
            if (_planeManager != null)
            {
                _planeManager.enabled = enabled;
            }
        }

        /// <summary>
        /// Asynchronously creates the content anchor at the given world pose and stores it as
        /// <see cref="ContentAnchor"/>. Async per AR Foundation 6.x (<c>TryAddAnchorAsync</c>) and the
        /// project's async rule. Returns the anchor on success, or null if anchoring is unavailable or
        /// the attempt failed (caller stays in a degraded-but-functional state).
        /// </summary>
        /// <param name="pose">The world pose to anchor at (e.g. from a raycast hit on a plane).</param>
        /// <returns>The created <see cref="ARAnchor"/>, or null on failure.</returns>
        public async Awaitable<ARAnchor> TryCreateContentAnchorAsync(Pose pose)
        {
            if (_anchorManager == null)
            {
                Debug.LogError("[ARSessionManager] ARAnchorManager not assigned; cannot create anchor.");
                return null;
            }

            var result = await _anchorManager.TryAddAnchorAsync(pose);
            if (result.status.IsSuccess())
            {
                ContentAnchor = result.value;
                return ContentAnchor;
            }

            Debug.LogWarning("[ARSessionManager] Content anchor creation failed.");
            return null;
        }

        /// <summary>Removes the current <see cref="ContentAnchor"/> if one exists.</summary>
        /// <returns>True if an anchor was removed.</returns>
        public bool RemoveContentAnchor()
        {
            if (_anchorManager == null || ContentAnchor == null)
            {
                return false;
            }

            bool removed = _anchorManager.TryRemoveAnchor(ContentAnchor);
            if (removed)
            {
                ContentAnchor = null;
            }

            return removed;
        }

        // ── Status mapping (unchanged) ────────────────────────────────────────────────────────────────

        /// <summary>Recomputes the status and raises <see cref="OnTrackingStateChanged"/> on a change.</summary>
        private void UpdateStatus()
        {
            ArTrackingStatus next = ComputeStatus();
            if (next == _status)
            {
                return;
            }

            _status = next;
            OnTrackingStateChanged?.Invoke(_status);
        }

        private ArTrackingStatus ComputeStatus()
        {
#if UNITY_ANDROID
            if (_cameraPermissionDenied)
            {
                return ArTrackingStatus.PermissionDenied;
            }
#endif
            return MapStatus(ARSession.state, ARSession.notTrackingReason);
        }

        /// <summary>
        /// Pure mapping from AR Foundation's session state to the Core-local <see cref="ArTrackingStatus"/>.
        /// Static and side-effect-free so it can be unit-tested in isolation. AR Foundation tracking is
        /// effectively binary (SessionTracking vs SessionInitializing); degradation detail comes from
        /// <see cref="NotTrackingReason"/>, mapped by <see cref="MapNotTracking"/>.
        /// </summary>
        internal static ArTrackingStatus MapStatus(ARSessionState state, NotTrackingReason reason)
        {
            switch (state)
            {
                case ARSessionState.Unsupported:
                case ARSessionState.NeedsInstall:
                    return ArTrackingStatus.Unavailable;
                case ARSessionState.SessionTracking:
                    return ArTrackingStatus.Tracking;
                case ARSessionState.SessionInitializing:
                    return MapNotTracking(reason);
                default:
                    // None, CheckingAvailability, Installing, Ready: coming up, not yet tracking.
                    return ArTrackingStatus.Initializing;
            }
        }

        /// <summary>
        /// Maps the not-tracking reason (meaningful while the session is initializing/lost) to a
        /// status: recoverable environmental issues → <see cref="ArTrackingStatus.Limited"/>, camera
        /// taken away → <see cref="ArTrackingStatus.NotTracking"/>, otherwise still initializing.
        /// </summary>
        internal static ArTrackingStatus MapNotTracking(NotTrackingReason reason)
        {
            switch (reason)
            {
                case NotTrackingReason.ExcessiveMotion:
                case NotTrackingReason.InsufficientLight:
                case NotTrackingReason.InsufficientFeatures:
                case NotTrackingReason.Relocalizing:
                    return ArTrackingStatus.Limited;
                case NotTrackingReason.CameraUnavailable:
                    return ArTrackingStatus.NotTracking;
                case NotTrackingReason.Unsupported:
                    return ArTrackingStatus.Unavailable;
                default:
                    // None, Initializing: the session is still establishing tracking.
                    return ArTrackingStatus.Initializing;
            }
        }

#if UNITY_ANDROID
        /// <summary>
        /// Requests the camera permission if not already granted, recording an explicit denial so
        /// <see cref="ComputeStatus"/> can report <see cref="ArTrackingStatus.PermissionDenied"/>.
        /// ARCore also requests this; Android de-duplicates, so the double request is harmless. Called
        /// from <see cref="EnterArRoutine"/> — i.e. on the AR-Mode tap (D.4), never at launch.
        /// </summary>
        private void RequestCameraPermissionIfNeeded()
        {
            if (Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                _cameraPermissionDenied = false;
                return;
            }

            // PermissionDenied fires for refusal (incl. don't-ask-again); the separate
            // PermissionDeniedAndDontAskAgain callback is obsolete in current Unity (unreliable —
            // query ShouldShowRequestPermissionRationale if you need to distinguish), so we don't use it.
            var callbacks = new PermissionCallbacks();
            callbacks.PermissionGranted += _ => _cameraPermissionDenied = false;
            callbacks.PermissionDenied += _ => _cameraPermissionDenied = true;
            Permission.RequestUserPermission(Permission.Camera, callbacks);
        }
#else
        /// <summary>No-op off-device (editor / non-Android): there is no runtime camera permission.</summary>
        private void RequestCameraPermissionIfNeeded() { }
#endif
    }
}
