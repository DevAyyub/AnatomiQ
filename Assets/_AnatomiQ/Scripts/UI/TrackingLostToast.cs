using AnatomiQ.Core;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace AnatomiQ.UI
{
    /// <summary>
    /// ATLAS-003 chunk 5 — the tracking-lost toast (UX C.6). A non-modal floating message that fades in
    /// while the app is in <see cref="AppState.AR_LIMITED"/> (AR running but tracking degraded/lost) and
    /// fades back out when tracking returns. It never blocks input — the body stays usable (ATLAS-003
    /// screen-locks it) and the toast is purely advisory.
    ///
    /// UI-pillar discipline (mirrors <see cref="PerformanceOverlay"/> / <see cref="PlacementModeSwitcher"/>):
    /// it only READS Core — it subscribes to <see cref="IFallbackManager.OnAppStateChanged"/> through the
    /// <see cref="ServiceRegistry"/> and references no AR types. It builds its own Canvas + label in code,
    /// so the only scene step is dropping the component and assigning the registry. The message is a Unity
    /// Localization key (table <c>UIStrings</c>); the value is authored by <c>PlacementStringsInstaller</c>.
    /// </summary>
    public sealed class TrackingLostToast : MonoBehaviour
    {
        [Tooltip("Cross-cutting service access (CORE). The toast subscribes to AppState changes from here.")]
        [SerializeField] private ServiceRegistry _services;

        [Tooltip("Seconds for a full fade in/out. The toast is non-modal — it never blocks touches.")]
        [Range(0.05f, 1f)]
        [SerializeField] private float _fadeSeconds = 0.25f;

        private const string UI_STRINGS_TABLE = "UIStrings";
        private const string KEY_TRACKING_LOST = "ui.ar.tracking_lost";

        private IFallbackManager _fallback;
        private CanvasGroup _group;
        private TextMeshProUGUI _label;
        private float _targetAlpha;

        private void Awake()
        {
            if (_services == null)
            {
                Debug.LogWarning("[TrackingLostToast] ServiceRegistry not assigned; toast will never show.");
            }

            BuildUi();
        }

        private void OnEnable()
        {
            _fallback = _services != null ? _services.FallbackManager : null;
            if (_fallback != null)
            {
                _fallback.OnAppStateChanged += HandleAppStateChanged;
                HandleAppStateChanged(_fallback.CurrentState); // change-only event: sync the current state
            }
            else
            {
                _targetAlpha = 0f;
            }

            LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;
            RefreshLocalizedText();
        }

        private void OnDisable()
        {
            if (_fallback != null)
            {
                _fallback.OnAppStateChanged -= HandleAppStateChanged;
            }

            LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;
        }

        private void Update()
        {
            if (_group == null)
            {
                return;
            }

            if (!Mathf.Approximately(_group.alpha, _targetAlpha))
            {
                float step = (_fadeSeconds > 0f ? Time.unscaledDeltaTime / _fadeSeconds : 1f);
                _group.alpha = Mathf.MoveTowards(_group.alpha, _targetAlpha, step);
            }
        }

        private void HandleAppStateChanged(AppState state)
        {
            // C.6: visible only while tracking is degraded/lost; fades out the moment it recovers.
            _targetAlpha = state == AppState.AR_LIMITED ? 1f : 0f;
        }

        private void HandleLocaleChanged(UnityEngine.Localization.Locale _) => RefreshLocalizedText();

        private void RefreshLocalizedText()
        {
            if (_label != null)
            {
                _label.text = Localize(KEY_TRACKING_LOST);
            }
        }

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

        private void BuildUi()
        {
            var canvasGo = new GameObject("TrackingLostCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 60; // above the mode switcher, below the debug overlay

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 2400f);
            scaler.matchWidthOrHeight = 0.5f;

            // Non-modal: the group never blocks raycasts, so touches always reach the AR view / switcher.
            _group = canvasGo.GetComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.interactable = false;
            _group.blocksRaycasts = false;

            var panel = new GameObject("Toast", typeof(Image));
            panel.transform.SetParent(canvasGo.transform, false);
            var prt = (RectTransform)panel.transform;
            prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 1f); // top-centre, floating
            prt.pivot = new Vector2(0.5f, 1f);
            prt.anchoredPosition = new Vector2(0f, -120f);
            prt.sizeDelta = new Vector2(820f, 96f);
            panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);
            panel.GetComponent<Image>().raycastTarget = false;

            var textGo = new GameObject("Label", typeof(TextMeshProUGUI));
            textGo.transform.SetParent(panel.transform, false);
            var trt = (RectTransform)textGo.transform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(24f, 8f);
            trt.offsetMax = new Vector2(-24f, -8f);

            _label = textGo.GetComponent<TextMeshProUGUI>();
            _label.fontSize = 28f;
            _label.color = Color.white;
            _label.alignment = TextAlignmentOptions.Center;
            _label.textWrappingMode = TextWrappingModes.Normal;
            _label.raycastTarget = false;
        }

#if UNITY_INCLUDE_TESTS
        /// <summary>Test-only: inject the registry without going through the inspector/Awake.</summary>
        internal void ConfigureServicesForTest(ServiceRegistry services) => _services = services;

        /// <summary>Test-only: the alpha the toast is fading toward (1 while AR_LIMITED, else 0).</summary>
        internal float TargetAlphaForTest => _targetAlpha;
#endif
    }
}
