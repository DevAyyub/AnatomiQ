using AnatomiQ.Core;
using UnityEngine;

namespace AnatomiQ.UI
{
    /// <summary>
    /// Dev-only IMGUI readout of the AR-relevant global signals CORE-001 / CORE-007 publish:
    /// <see cref="AppState"/> (the single-writer AR-context state), the raw
    /// <see cref="ArTrackingStatus"/> from the AR tracking provider, and <see cref="Connectivity"/>.
    ///
    /// It exists because AppState promotion prints no log, so there was no in-play way to SEE whether
    /// the session had reached tracking — guessing from the stale startup status-bar line caused false
    /// "still viewer mode" reads. Pairs with (does NOT modify) the A.12 PerformanceOverlay: this one is
    /// AR-state, that one is perf. Reads only Core interfaces via the <see cref="ServiceRegistry"/>, so
    /// the UI assembly never references a pillar.
    ///
    /// A verification aid, same spirit as CORE-001's ARStatusLogger / CORE-002's tier-forcer: the draw
    /// path is compiled out of release builds (<c>UNITY_EDITOR || DEVELOPMENT_BUILD</c>), leaving an
    /// inert component (so no missing-script warning if it lingers in the scene). Remove the component
    /// at the ATLAS-003 cleanup pass if you no longer want it.
    /// </summary>
    public sealed class ArStateReadout : MonoBehaviour
    {
        [SerializeField, Tooltip("The shared cross-cutting service registry asset.")]
        private ServiceRegistry _services;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private const float LINE_HEIGHT = 18f;

        private GUIStyle _boxStyle;
        private GUIStyle _labelStyle;
        private Texture2D _background;

        private void OnGUI()
        {
            if (_services == null)
            {
                return;
            }

            EnsureStyles();

            const float width = 230f;
            const float height = 88f;
            const float pad = 8f;

            var box = new Rect(Screen.width - width - pad, pad, width, height);
            GUI.Box(box, GUIContent.none, _boxStyle);

            IFallbackManager fallback = _services.FallbackManager;
            IArTrackingProvider arTracking = _services.ArTrackingProvider;

            string appState = fallback != null ? fallback.CurrentState.ToString() : "—";
            string net = fallback != null ? fallback.CurrentConnectivity.ToString() : "—";
            string tracking = arTracking != null ? arTracking.Status.ToString() : "— (no AR provider)";

            var line = new Rect(box.x + 10f, box.y + 8f, width - 20f, LINE_HEIGHT);
            DrawLine(ref line, "AR STATE (dev)", Color.white);
            DrawLine(ref line, $"AppState:  {appState}", AppStateColor(fallback));
            DrawLine(ref line, $"Tracking:  {tracking}", Color.white);
            DrawLine(ref line, $"Net:       {net}", Color.white);
        }

        private void DrawLine(ref Rect line, string text, Color color)
        {
            _labelStyle.normal.textColor = color;
            GUI.Label(line, text, _labelStyle);
            line.y += LINE_HEIGHT;
        }

        private void EnsureStyles()
        {
            if (_boxStyle != null)
            {
                return;
            }

            _background = new Texture2D(1, 1);
            _background.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.6f));
            _background.Apply();

            _boxStyle = new GUIStyle(GUI.skin.box);
            _boxStyle.normal.background = _background;

            _labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, richText = false };
        }

        /// <summary>Green when AR is active, amber when tracking is degraded, grey in viewer mode.</summary>
        private static Color AppStateColor(IFallbackManager fallback)
        {
            if (fallback == null)
            {
                return Color.gray;
            }

            switch (fallback.CurrentState)
            {
                case AppState.AR_ACTIVE:
                    return new Color(0.40f, 0.90f, 0.40f);
                case AppState.AR_LIMITED:
                    return new Color(0.95f, 0.80f, 0.30f);
                default: // AR_VIEWER_MODE
                    return new Color(0.70f, 0.70f, 0.70f);
            }
        }
#endif
    }
}
