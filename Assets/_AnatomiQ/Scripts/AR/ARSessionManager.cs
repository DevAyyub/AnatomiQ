using System;
using System.Collections;
using AnatomiQ.Core;
using Unity.XR.CoreUtils;
using UnityEngine;
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

        [Header("Behaviour")]
        [Tooltip("If the device supports AR but needs the ARCore app installed/updated, attempt it on start.")]
        [SerializeField] private bool _attemptInstallIfNeeded = true;

        private ArTrackingStatus _status = ArTrackingStatus.Initializing;

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
        /// the actual visual behaviour lives in CORE-002.
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

            // Register as the AR tracking provider. CheckArTracking pulls Status from the registry.
            _services.Register(this);
        }

        /// <summary>
        /// Drives the AR session bring-up per AR Foundation's recommended pattern: resolve
        /// availability (async), optionally install ARCore, then enable the session — unless the device
        /// is unsupported, in which case the session stays disabled and <see cref="Status"/> reports
        /// <see cref="ArTrackingStatus.Unavailable"/> so CORE-007 falls back to 3D viewer mode.
        /// </summary>
        private IEnumerator Start()
        {
            if (_session == null)
            {
                Debug.LogError("[ARSessionManager] ARSession not assigned; staying in viewer mode.");
                yield break;
            }

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
                // No ARCore here — leave the session off. Status → Unavailable → CORE-007 viewer mode.
                UpdateStatus();
                yield break;
            }

            RequestCameraPermissionIfNeeded();
            _session.enabled = true;
            UpdateStatus();
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
        /// ARCore also requests this; Android de-duplicates, so the double request is harmless.
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
