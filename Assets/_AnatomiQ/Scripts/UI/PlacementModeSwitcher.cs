using System.Collections.Generic;
using AnatomiQ.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace AnatomiQ.UI
{
    /// <summary>
    /// ATLAS-003 chunk 5 — the persistent placement-mode control. A single-tap segmented control
    /// (3D Viewer / Space / Surface, A.2) that reflects the live <see cref="PlacementMode"/> and
    /// requests changes through <see cref="IPlacementProvider.RequestMode"/>. It also carries the D.4
    /// affordances: a one-sentence camera rationale shown BEFORE the first AR entry (so the rationale
    /// precedes the OS permission prompt that <see cref="RequestMode"/> ultimately triggers), and a
    /// status line that explains why AR isn't available — "Tap to enable AR" vs "camera access off" vs
    /// "device doesn't support AR".
    ///
    /// UI-pillar discipline (mirrors <see cref="PerformanceOverlay"/>): it only READS Core via the
    /// <see cref="ServiceRegistry"/> — <see cref="IPlacementProvider"/>, <see cref="IFallbackManager"/>
    /// for <see cref="AppState"/>, and <see cref="IArTrackingProvider.Status"/> for the finer-grained
    /// explainer (all Core interfaces, so the UI never references the AR pillar). It builds its own
    /// Canvas + widgets in code, so the only scene step is dropping the component and assigning the
    /// registry. User-facing strings come from Unity Localization (table <c>UIStrings</c>, Build
    /// Environment Part C) — never hardcoded; the keys are owned here as constants and their English
    /// values are authored by <c>PlacementStringsInstaller</c> (Editor), exactly like CORE-007's
    /// thermal strings.
    /// </summary>
    public sealed class PlacementModeSwitcher : MonoBehaviour
    {
        [Tooltip("Cross-cutting service access (CORE). The switcher reads the placement provider, " +
                 "AppState, and AR tracking status from here.")]
        [SerializeField] private ServiceRegistry _services;

        // ── Localization (table + keys; values authored by PlacementStringsInstaller) ────────────────
        private const string UI_STRINGS_TABLE = "UIStrings";
        private const string KEY_MODE_VIEWER  = "ui.placement.mode.viewer";
        private const string KEY_MODE_SPACE   = "ui.placement.mode.space";
        private const string KEY_MODE_SURFACE = "ui.placement.mode.surface";
        private const string KEY_ENABLE_AR    = "ui.placement.enable_ar";
        private const string KEY_RATIONALE    = "ui.placement.camera_rationale";
        private const string KEY_CONTINUE     = "ui.placement.rationale.continue";
        private const string KEY_CANCEL       = "ui.placement.rationale.cancel";
        private const string KEY_ENTERING_AR  = "ui.placement.entering_ar";
        private const string KEY_CAM_DENIED   = "ui.ar.camera_denied";
        private const string KEY_UNSUPPORTED  = "ui.ar.unsupported";

        private const float STATUS_POLL_INTERVAL = 0.25f; // 4 Hz, matching the overlay; status has no Core event

        // Segmented-button palette.
        private static readonly Color BtnActive   = new Color(0.16f, 0.55f, 0.85f, 0.95f);
        private static readonly Color BtnInactive = new Color(0f, 0f, 0f, 0.55f);
        private static readonly Color PanelTint    = new Color(0f, 0f, 0f, 0.78f);

        private IPlacementProvider _placement;
        private IFallbackManager _fallback;

        private readonly Dictionary<PlacementMode, Image> _modeButtons = new();
        private TextMeshProUGUI _statusLabel;
        private GameObject _rationalePanel;
        private TextMeshProUGUI _rationaleBody;
        private TextMeshProUGUI _continueLabel;
        private TextMeshProUGUI _cancelLabel;

        private AppState _appState = AppState.AR_VIEWER_MODE;
        private PlacementMode _currentMode = PlacementMode.Viewer;
        private bool _cameraRationaleAcknowledged;
        private PlacementMode _rationalePendingMode;
        private float _lastStatusPoll;

        private void Awake()
        {
            if (_services == null)
            {
                Debug.LogWarning("[PlacementModeSwitcher] ServiceRegistry not assigned; switcher will be inert.");
            }

            EnsureEventSystem();
            BuildUi();
            RefreshModeButtons();
            RefreshStatusLine();
        }

        private void OnEnable()
        {
            _placement = _services != null ? _services.PlacementProvider : null;
            _fallback = _services != null ? _services.FallbackManager : null;

            if (_placement != null)
            {
                _placement.OnPlacementModeChanged += HandleModeChanged;
                _currentMode = _placement.CurrentMode; // change-only event: sync the last transition we missed
            }

            if (_fallback != null)
            {
                _fallback.OnAppStateChanged += HandleAppStateChanged;
                _appState = _fallback.CurrentState;
            }

            LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;

            RefreshModeButtons();
            RefreshStatusLine();
            RefreshLocalizedText();
        }

        private void OnDisable()
        {
            if (_placement != null)
            {
                _placement.OnPlacementModeChanged -= HandleModeChanged;
            }

            if (_fallback != null)
            {
                _fallback.OnAppStateChanged -= HandleAppStateChanged;
            }

            LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;
        }

        private void Update()
        {
            // ArTrackingStatus has no Core-side event (CORE-001's event is AR-pillar-internal), and not
            // every status change crosses an AppState boundary — e.g. Initializing → PermissionDenied
            // both read AR_VIEWER_MODE. Poll the explainer at a low rate, like the perf overlay does.
            if (Time.unscaledTime - _lastStatusPoll < STATUS_POLL_INTERVAL)
            {
                return;
            }

            _lastStatusPoll = Time.unscaledTime;
            RefreshStatusLine();
        }

        // ── Event handlers ────────────────────────────────────────────────────────────────────────────

        private void HandleModeChanged(PlacementMode mode)
        {
            _currentMode = mode;
            RefreshModeButtons();
        }

        private void HandleAppStateChanged(AppState state)
        {
            _appState = state;
            RefreshStatusLine();
        }

        private void HandleLocaleChanged(UnityEngine.Localization.Locale _) => RefreshLocalizedText();

        // ── Tap handling + D.4 rationale gate ───────────────────────────────────────────────────────

        private void OnModeTapped(PlacementMode mode)
        {
            if (_placement == null)
            {
                return;
            }

            // D.4: the first time the user reaches for an AR mode (and AR isn't already active), show the
            // one-sentence camera rationale, THEN proceed — the proceed is what triggers the OS prompt.
            bool isArMode = mode != PlacementMode.Viewer;
            if (isArMode && _appState != AppState.AR_ACTIVE && !_cameraRationaleAcknowledged)
            {
                _rationalePendingMode = mode;
                SetRationaleVisible(true);
                return;
            }

            _placement.RequestMode(mode);
        }

        private void OnRationaleContinue()
        {
            _cameraRationaleAcknowledged = true;
            SetRationaleVisible(false);
            _placement?.RequestMode(_rationalePendingMode);
        }

        private void OnRationaleCancel() => SetRationaleVisible(false);

        // ── View refresh ──────────────────────────────────────────────────────────────────────────────

        private void RefreshModeButtons()
        {
            foreach (var kvp in _modeButtons)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.color = kvp.Key == _currentMode ? BtnActive : BtnInactive;
                }
            }
        }

        /// <summary>
        /// Updates the status line beneath the buttons. Reads <see cref="IArTrackingProvider.Status"/>
        /// (a Core interface) so it can tell the three AR-unavailable reasons apart, as that interface's
        /// own contract invites. A null provider reads as Unavailable.
        /// </summary>
        private void RefreshStatusLine()
        {
            if (_statusLabel == null)
            {
                return;
            }

            // While the rationale is up the user is mid-decision; keep the line quiet.
            if (_rationalePanel != null && _rationalePanel.activeSelf)
            {
                _statusLabel.text = string.Empty;
                return;
            }

            string text;
            switch (_appState)
            {
                case AppState.AR_ACTIVE:
                case AppState.AR_LIMITED:
                    text = string.Empty; // AR is up (the toast handles the limited-tracking message)
                    break;

                default: // AR_VIEWER_MODE
                    ArTrackingStatus status = _services != null && _services.ArTrackingProvider != null
                        ? _services.ArTrackingProvider.Status
                        : ArTrackingStatus.Unavailable;

                    switch (status)
                    {
                        case ArTrackingStatus.PermissionDenied:
                            text = Localize(KEY_CAM_DENIED);
                            break;
                        case ArTrackingStatus.Unavailable:
                            text = Localize(KEY_UNSUPPORTED);
                            break;
                        case ArTrackingStatus.Initializing:
                            // If an AR mode is pending we're starting up; otherwise invite the user in.
                            text = _currentMode == PlacementMode.Viewer
                                ? Localize(KEY_ENABLE_AR)
                                : Localize(KEY_ENTERING_AR);
                            break;
                        default:
                            text = Localize(KEY_ENABLE_AR);
                            break;
                    }
                    break;
            }

            _statusLabel.text = text;
        }

        /// <summary>Re-resolves every localized string (initial paint + on locale change).</summary>
        private void RefreshLocalizedText()
        {
            if (_modeButtons.TryGetValue(PlacementMode.Viewer, out var vImg) && vImg != null)
            {
                SetButtonLabel(vImg, Localize(KEY_MODE_VIEWER));
            }
            if (_modeButtons.TryGetValue(PlacementMode.Space, out var spImg) && spImg != null)
            {
                SetButtonLabel(spImg, Localize(KEY_MODE_SPACE));
            }
            if (_modeButtons.TryGetValue(PlacementMode.Surface, out var suImg) && suImg != null)
            {
                SetButtonLabel(suImg, Localize(KEY_MODE_SURFACE));
            }

            if (_rationaleBody != null) _rationaleBody.text = Localize(KEY_RATIONALE);
            if (_continueLabel != null) _continueLabel.text = Localize(KEY_CONTINUE);
            if (_cancelLabel != null) _cancelLabel.text = Localize(KEY_CANCEL);

            RefreshStatusLine();
        }

        private void SetRationaleVisible(bool visible)
        {
            if (_rationalePanel != null)
            {
                _rationalePanel.SetActive(visible);
            }

            RefreshStatusLine();
        }

        /// <summary>
        /// Unity Localization fetch (table <c>UIStrings</c>, Build Environment C). Synchronous — fine
        /// for the rare, event-driven refreshes here (never per-frame). Falls back to the key rather
        /// than a blank string if localization isn't ready yet or the entry is missing.
        /// </summary>
        private static string Localize(string key)
        {
            try
            {
                string s = LocalizationSettings.StringDatabase.GetLocalizedString(UI_STRINGS_TABLE, key);
                return string.IsNullOrEmpty(s) ? key : s;
            }
            catch
            {
                return key;
            }
        }

        // ── UI construction (in code, like PerformanceOverlay) ────────────────────────────────────────

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        private void BuildUi()
        {
            var canvasGo = new GameObject("PlacementSwitcherCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50; // app UI level — below the debug overlay, above the AR view

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 2400f);
            scaler.matchWidthOrHeight = 0.5f;

            BuildSegmentedControl(canvasGo.transform);
            BuildStatusLine(canvasGo.transform);
            BuildRationalePanel(canvasGo.transform);
        }

        private void BuildSegmentedControl(Transform parent)
        {
            // A bottom-centre row of three equal buttons. Anchored to bottom-centre so it survives
            // aspect changes (CanvasScaler handles the rest).
            var row = new GameObject("ModeRow", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            var rrt = (RectTransform)row.transform;
            rrt.anchorMin = rrt.anchorMax = new Vector2(0.5f, 0f);
            rrt.pivot = new Vector2(0.5f, 0f);
            rrt.anchoredPosition = new Vector2(0f, 48f);
            rrt.sizeDelta = new Vector2(660f, 96f);

            const float gap = 12f;
            float w = (rrt.sizeDelta.x - gap * 2f) / 3f;
            float x = -(rrt.sizeDelta.x * 0.5f) + w * 0.5f;

            _modeButtons[PlacementMode.Viewer]  = MakeSegment(row.transform, PlacementMode.Viewer,  x, w); x += w + gap;
            _modeButtons[PlacementMode.Space]   = MakeSegment(row.transform, PlacementMode.Space,   x, w); x += w + gap;
            _modeButtons[PlacementMode.Surface] = MakeSegment(row.transform, PlacementMode.Surface, x, w);
        }

        private Image MakeSegment(Transform parent, PlacementMode mode, float x, float width)
        {
            var go = new GameObject($"Mode_{mode}", typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x, 0f);
            rt.sizeDelta = new Vector2(width, 96f);

            var img = go.GetComponent<Image>();
            img.color = BtnInactive;

            PlacementMode captured = mode;
            go.GetComponent<Button>().onClick.AddListener(() => OnModeTapped(captured));

            var lblGo = new GameObject("Label", typeof(TextMeshProUGUI));
            lblGo.transform.SetParent(go.transform, false);
            var lrt = (RectTransform)lblGo.transform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;

            var lbl = lblGo.GetComponent<TextMeshProUGUI>();
            lbl.text = mode.ToString(); // replaced by the localized label in RefreshLocalizedText
            lbl.fontSize = 30f;
            lbl.color = Color.white;
            lbl.alignment = TextAlignmentOptions.Center;
            lbl.raycastTarget = false;
            return img;
        }

        private static void SetButtonLabel(Image buttonImage, string text)
        {
            var lbl = buttonImage.GetComponentInChildren<TextMeshProUGUI>();
            if (lbl != null)
            {
                lbl.text = text;
            }
        }

        private void BuildStatusLine(Transform parent)
        {
            var go = new GameObject("PlacementStatus", typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 156f); // just above the button row
            rt.sizeDelta = new Vector2(720f, 56f);

            _statusLabel = go.GetComponent<TextMeshProUGUI>();
            _statusLabel.fontSize = 26f;
            _statusLabel.color = new Color(1f, 1f, 1f, 0.92f);
            _statusLabel.alignment = TextAlignmentOptions.Center;
            _statusLabel.raycastTarget = false;
            _statusLabel.text = string.Empty;
        }

        private void BuildRationalePanel(Transform parent)
        {
            _rationalePanel = new GameObject("CameraRationale", typeof(Image));
            _rationalePanel.transform.SetParent(parent, false);

            var rt = (RectTransform)_rationalePanel.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(760f, 380f);
            _rationalePanel.GetComponent<Image>().color = PanelTint;

            var bodyGo = new GameObject("Body", typeof(TextMeshProUGUI));
            bodyGo.transform.SetParent(_rationalePanel.transform, false);
            var brt = (RectTransform)bodyGo.transform;
            brt.anchorMin = new Vector2(0f, 0.35f);
            brt.anchorMax = new Vector2(1f, 1f);
            brt.offsetMin = new Vector2(36f, 12f);
            brt.offsetMax = new Vector2(-36f, -28f);
            _rationaleBody = bodyGo.GetComponent<TextMeshProUGUI>();
            _rationaleBody.fontSize = 30f;
            _rationaleBody.color = Color.white;
            _rationaleBody.alignment = TextAlignmentOptions.Center;
            _rationaleBody.textWrappingMode = TextWrappingModes.Normal;
            _rationaleBody.raycastTarget = false;

            _cancelLabel   = MakeDialogButton(_rationalePanel.transform, -180f, BtnInactive, OnRationaleCancel);
            _continueLabel = MakeDialogButton(_rationalePanel.transform,  180f, BtnActive,   OnRationaleContinue);

            _rationalePanel.SetActive(false);
        }

        private TextMeshProUGUI MakeDialogButton(Transform parent, float x, Color color, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject("DialogButton", typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(x, 36f);
            rt.sizeDelta = new Vector2(300f, 88f);

            go.GetComponent<Image>().color = color;
            go.GetComponent<Button>().onClick.AddListener(onClick);

            var lblGo = new GameObject("Label", typeof(TextMeshProUGUI));
            lblGo.transform.SetParent(go.transform, false);
            var lrt = (RectTransform)lblGo.transform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;

            var lbl = lblGo.GetComponent<TextMeshProUGUI>();
            lbl.fontSize = 28f;
            lbl.color = Color.white;
            lbl.alignment = TextAlignmentOptions.Center;
            lbl.raycastTarget = false;
            return lbl;
        }

#if UNITY_INCLUDE_TESTS
        /// <summary>Test-only: inject the registry without going through the inspector/Awake.</summary>
        internal void ConfigureServicesForTest(ServiceRegistry services) => _services = services;

        /// <summary>Test-only: the mode the rationale gate will request once the user confirms.</summary>
        internal PlacementMode RationalePendingModeForTest => _rationalePendingMode;
#endif
    }
}
