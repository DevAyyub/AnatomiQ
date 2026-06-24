#if UNITY_EDITOR
using UnityEditor;            // UnityStats (drawcalls/triangles) — editor-only
using UnityEngine.InputSystem; // Keyboard.f1Key — editor-only toggle convenience
#endif
using System.Text;
using AnatomiQ.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace AnatomiQ.UI
{
    /// <summary>
    /// A.12 performance debug overlay (CORE-002 chunk 5). A toggleable on-screen readout of the
    /// signals CORE-007 publishes through <see cref="IFallbackManager.Metrics"/> — FPS, frame time,
    /// RAM vs the A.4 ceiling, thermal, and tier — plus editor-only draw/triangle counts and "n/a"
    /// placeholders for the A.12 rows that have no source yet (GPU memory, inference time, API queue,
    /// battery temp), so the layout matches the full A.12 intent.
    ///
    /// It is a DEV TOOL: strings are plain constants (decision G — the day-1 localization exception),
    /// and it only ever READS Core's snapshot via the <see cref="ServiceRegistry"/>, so the UI pillar
    /// never references another pillar. Self-contained: it builds its own top-most Canvas, a corner
    /// toggle button, and the metrics panel in code, so the only scene step is dropping the component
    /// and assigning the registry. It refreshes at <see cref="REFRESH_INTERVAL"/> (4 Hz, not
    /// per-frame) and writes through a cached <see cref="StringBuilder"/> via TMP's
    /// <c>SetText(StringBuilder)</c>, so while it's visible during the chunk-6 sustained-load test it
    /// adds no per-frame GC against the A.2 budget.
    /// </summary>
    public sealed class PerformanceOverlay : MonoBehaviour
    {
        [Tooltip("Cross-cutting service access (CORE). The overlay reads IFallbackManager.Metrics from here.")]
        [SerializeField] private ServiceRegistry _services;

        [Tooltip("If true the metrics panel starts visible. The corner button (and F1 in editor) toggle it either way.")]
        [SerializeField] private bool _startVisible = false;

        private const float REFRESH_INTERVAL = 0.25f;            // 4 Hz — readable without thrashing
        private const float RAM_CEILING_MB   = 1400f;            // A.4 soft ceiling
        private const float RAM_WARN_MB      = RAM_CEILING_MB * 0.8f;
        private const float FPS_WARN         = 45f;
        private const float FPS_BAD          = 30f;
        private const float FRAME_WARN_MS    = 22f;
        private const float FRAME_BAD_MS     = 33f;              // 30 FPS ceiling (A.2)
        private const float THERMAL_WARN     = 0.6f;
        private const float THERMAL_BAD      = 0.8f;

        // Rich-text severity ramp.
        private const string C_OK    = "#7CFC8A";
        private const string C_WARN  = "#FFD24A";
        private const string C_HOT   = "#FF9F45";
        private const string C_BAD   = "#FF6B6B";
        private const string C_MUTED = "#9AA0A6";

        private readonly StringBuilder _sb = new StringBuilder(320);
        private TextMeshProUGUI _text;
        private GameObject _panel;
        private bool _panelVisible;

        private float _lastRefresh;
        private float _frameMsAccum;
        private int _frameCount;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private GameObject _debugRow; // dev-only tier-forcer button strip; toggles with the panel
#endif

        private void Awake()
        {
            if (_services == null)
            {
                Debug.LogWarning("[PerformanceOverlay] ServiceRegistry not assigned; overlay will show placeholders.");
            }

            EnsureEventSystem();
            BuildUi();
            SetPanelVisible(_startVisible);
        }

        private void Update()
        {
            _frameMsAccum += Time.unscaledDeltaTime * 1000f;
            _frameCount++;

#if UNITY_EDITOR
            if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
            {
                SetPanelVisible(!_panelVisible);
            }
#endif

            if (Time.unscaledTime - _lastRefresh < REFRESH_INTERVAL)
            {
                return;
            }
            _lastRefresh = Time.unscaledTime;

            if (_panelVisible && _text != null)
            {
                RefreshSb();
                _text.SetText(_sb);
            }

            _frameMsAccum = 0f;
            _frameCount = 0;
        }

        /// <summary>Toggle the metrics panel. The corner button stays visible either way.</summary>
        private void SetPanelVisible(bool visible)
        {
            _panelVisible = visible;
            if (_panel != null)
            {
                _panel.SetActive(visible);
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_debugRow != null)
            {
                _debugRow.SetActive(visible);
            }
#endif

            if (visible && _text != null)
            {
                RefreshSb();      // immediate paint so opening never shows a stale/blank frame
                _text.SetText(_sb);
            }
        }

        /// <summary>
        /// Fills <see cref="_sb"/> with the current metrics block. Pure read of
        /// <see cref="IFallbackManager.Metrics"/> (plus the local frame-time accumulator and, in
        /// editor, <c>UnityStats</c>); touches no Canvas state, so it is safe to call before
        /// <see cref="Awake"/> (the test seam does exactly that). Degrades to placeholders rather
        /// than throwing when the registry or the FallbackManager is not present.
        /// </summary>
        private void RefreshSb()
        {
            _sb.Clear();
            _sb.Append("AnatomiQ \u2022 A.12\n");

            if (_services == null)
            {
                AppendMuted("ServiceRegistry not assigned");
                return;
            }

            IFallbackManager fm = _services.FallbackManager;
            if (fm == null)
            {
                AppendMuted("FallbackManager not registered");
                return;
            }

            PerformanceMetrics m = fm.Metrics;
            float frameMs = _frameCount > 0 ? _frameMsAccum / _frameCount : 0f;

            AppendMetric("FPS", m.RollingFps, "0.0", FpsColor(m.RollingFps));
            AppendMetric("Frame", frameMs, "0.0", FrameColor(frameMs), " ms");

            if (m.RamMegabytes < 0f)
            {
                AppendLabeledMuted("RAM", "n/a");
            }
            else
            {
                _sb.Append("RAM: <color=").Append(RamColor(m.RamMegabytes)).Append('>')
                   .Append(m.RamMegabytes.ToString("0")).Append(" / 1400 MB</color>\n");
            }

            if (m.TemperatureLevel < 0f)
            {
                AppendLabeledMuted("Thermal", "\u2014"); // em dash = no source
            }
            else
            {
                AppendMetric("Thermal", m.TemperatureLevel, "0.00", ThermalColor(m.TemperatureLevel));
            }

            _sb.Append("Tier: <color=").Append(TierColor(m.Tier)).Append('>')
               .Append(m.Tier.ToString()).Append("</color>\n");

#if UNITY_EDITOR
            _sb.Append("Draws: ").Append(UnityStats.drawCalls)
               .Append("  Tris: ").Append(UnityStats.triangles).Append('\n');
#else
            _sb.Append("<color=").Append(C_MUTED).Append(">Draws/Tris: n/a (device)</color>\n");
#endif
            // A.12 rows with no source yet — shown so the layout reflects the full intent.
            _sb.Append("<color=").Append(C_MUTED).Append(">GPU/Inf/API/Batt: n/a</color>");
        }

        // --- Compose helpers -------------------------------------------------------------------

        private void AppendMetric(string label, float value, string fmt, string hex, string suffix = null)
        {
            _sb.Append(label).Append(": <color=").Append(hex).Append('>').Append(value.ToString(fmt));
            if (suffix != null)
            {
                _sb.Append(suffix);
            }
            _sb.Append("</color>\n");
        }

        private void AppendLabeledMuted(string label, string value) =>
            _sb.Append(label).Append(": <color=").Append(C_MUTED).Append('>').Append(value).Append("</color>\n");

        private void AppendMuted(string value) =>
            _sb.Append("<color=").Append(C_MUTED).Append('>').Append(value).Append("</color>");

        private static string FpsColor(float fps) => fps < FPS_BAD ? C_BAD : fps < FPS_WARN ? C_WARN : C_OK;
        private static string FrameColor(float ms) => ms > FRAME_BAD_MS ? C_BAD : ms > FRAME_WARN_MS ? C_WARN : C_OK;
        private static string RamColor(float mb) => mb >= RAM_CEILING_MB ? C_BAD : mb >= RAM_WARN_MB ? C_WARN : C_OK;
        private static string ThermalColor(float t) => t >= THERMAL_BAD ? C_BAD : t >= THERMAL_WARN ? C_WARN : C_OK;

        private static string TierColor(PerformanceTier tier) => tier switch
        {
            PerformanceTier.Nominal    => C_OK,
            PerformanceTier.Reduced    => C_WARN,
            PerformanceTier.Aggressive => C_HOT,
            PerformanceTier.Critical   => C_BAD,
            _                          => C_MUTED
        };

        // --- UI construction -------------------------------------------------------------------

        /// <summary>
        /// Creates an EventSystem with the new Input System UI module IF the scene has none, so the
        /// toggle button receives taps even in a bare scene (e.g. AR_Main during the chunk-6 gate).
        /// This is UI-input bootstrap, not service location — it doesn't resolve an app service, it
        /// guarantees UGUI raycasts have a driver. Guarded so it never adds a second EventSystem.
        /// </summary>
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
            var canvasGo = new GameObject("PerfOverlayCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue - 8; // above app UI

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 2400f);
            scaler.matchWidthOrHeight = 0.5f;

            BuildToggleButton(canvasGo.transform);
            BuildPanel(canvasGo.transform);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            BuildDebugTierRow(canvasGo.transform);
#endif
        }

        private void BuildToggleButton(Transform parent)
        {
            var btnGo = new GameObject("PerfToggle", typeof(Image), typeof(Button));
            btnGo.transform.SetParent(parent, false);

            var rt = (RectTransform)btnGo.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f); // top-left
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(16f, -16f);
            rt.sizeDelta = new Vector2(132f, 68f);

            btnGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
            btnGo.GetComponent<Button>().onClick.AddListener(() => SetPanelVisible(!_panelVisible));

            var lblGo = new GameObject("Label", typeof(TextMeshProUGUI));
            lblGo.transform.SetParent(btnGo.transform, false);
            var lblRt = (RectTransform)lblGo.transform;
            lblRt.anchorMin = Vector2.zero;
            lblRt.anchorMax = Vector2.one;
            lblRt.offsetMin = lblRt.offsetMax = Vector2.zero;

            var lbl = lblGo.GetComponent<TextMeshProUGUI>();
            lbl.text = "PERF";
            lbl.fontSize = 30f;
            lbl.color = Color.white;
            lbl.alignment = TextAlignmentOptions.Center;
            lbl.raycastTarget = false;
        }

        private void BuildPanel(Transform parent)
        {
            _panel = new GameObject("PerfPanel", typeof(Image));
            _panel.transform.SetParent(parent, false);

            var rt = (RectTransform)_panel.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f); // top-left
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(16f, -96f);      // below the toggle button
            rt.sizeDelta = new Vector2(560f, 360f);

            _panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.62f);

            var textGo = new GameObject("PerfText", typeof(TextMeshProUGUI));
            textGo.transform.SetParent(_panel.transform, false);
            var trt = (RectTransform)textGo.transform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(18f, 14f);
            trt.offsetMax = new Vector2(-18f, -14f);

            _text = textGo.GetComponent<TextMeshProUGUI>();
            _text.fontSize = 26f;
            _text.color = Color.white;
            _text.alignment = TextAlignmentOptions.TopLeft;
            _text.textWrappingMode = TextWrappingModes.NoWrap;
            _text.richText = true;
            _text.raycastTarget = false;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// DEV-ONLY: builds a row of buttons under the panel that force CORE-007's published tier
        /// (Nominal / Reduced / Aggressive / Critical) or release it back to Auto. A chunk-6 aid for
        /// watching CORE-002's URP levers engage on device without real throttling. Stripped from
        /// release builds along with the rest of this block.
        /// </summary>
        private void BuildDebugTierRow(Transform parent)
        {
            _debugRow = new GameObject("PerfDebugTierRow", typeof(RectTransform));
            _debugRow.transform.SetParent(parent, false);

            var rt = (RectTransform)_debugRow.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(16f, -468f); // just below the metrics panel
            rt.sizeDelta = new Vector2(560f, 60f);

            const float w = 104f;
            const float gap = 10f;
            float x = 0f;
            MakeButton(_debugRow.transform, "Nom",  x, w, () => ForceTier(PerformanceTier.Nominal));    x += w + gap;
            MakeButton(_debugRow.transform, "Red",  x, w, () => ForceTier(PerformanceTier.Reduced));    x += w + gap;
            MakeButton(_debugRow.transform, "Agg",  x, w, () => ForceTier(PerformanceTier.Aggressive)); x += w + gap;
            MakeButton(_debugRow.transform, "Crit", x, w, () => ForceTier(PerformanceTier.Critical));   x += w + gap;
            MakeButton(_debugRow.transform, "Auto", x, w, () => ForceTier(null));
        }

        /// <summary>DEV-ONLY: creates a labelled button at the given x offset within a top-left row.</summary>
        private static void MakeButton(Transform parent, string label, float x, float width, System.Action onClick)
        {
            var go = new GameObject("Btn_" + label, typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, 0f);
            rt.sizeDelta = new Vector2(width, 60f);

            go.GetComponent<Image>().color = new Color(0.16f, 0.16f, 0.16f, 0.88f);
            go.GetComponent<Button>().onClick.AddListener(() => onClick());

            var lblGo = new GameObject("Label", typeof(TextMeshProUGUI));
            lblGo.transform.SetParent(go.transform, false);
            var lrt = (RectTransform)lblGo.transform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;

            var lbl = lblGo.GetComponent<TextMeshProUGUI>();
            lbl.text = label;
            lbl.fontSize = 24f;
            lbl.color = Color.white;
            lbl.alignment = TextAlignmentOptions.Center;
            lbl.raycastTarget = false;
        }

        /// <summary>
        /// DEV-ONLY: pins (or releases) CORE-007's published tier through the real signal path, so
        /// BodyRenderer's subscriber applies the URP levers exactly as it would under real throttling.
        /// Reaches the concrete FallbackManager deliberately — this is gated out of release, so the
        /// access-via-interface rule still holds for shipping code.
        /// </summary>
        private void ForceTier(PerformanceTier? tier)
        {
            if (_services != null && _services.FallbackManager is FallbackManager fm)
            {
                fm.DebugSetTierOverride(tier);
                RefreshSb();
                if (_text != null)
                {
                    _text.SetText(_sb);
                }
            }
        }
#endif

#if UNITY_INCLUDE_TESTS
        /// <summary>Test-only: inject the registry without going through the inspector/Awake.</summary>
        internal void ConfigureServicesForTest(ServiceRegistry services) => _services = services;

        /// <summary>
        /// Test-only: compose the metrics block and return it as a string. Exercises the same
        /// <see cref="RefreshSb"/> path the live overlay uses, without building the Canvas.
        /// </summary>
        internal string ComposeMetricsText()
        {
            RefreshSb();
            return _sb.ToString();
        }
#endif
    }
}
