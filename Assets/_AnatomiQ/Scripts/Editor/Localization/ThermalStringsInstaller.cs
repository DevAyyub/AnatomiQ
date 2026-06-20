#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;

namespace AnatomiQ.Editor.Localization
{
    /// <summary>
    /// One-shot Editor utility that authors CORE-007's two thermal user-message strings into the
    /// existing <c>UIStrings</c> String Table (English locale). Run once via
    /// <c>AnatomiQ ▸ Localization ▸ Add CORE-007 Thermal Strings</c>.
    ///
    /// Why a tool instead of hand-editing the .asset: the scaffold added <c>ui.app.name</c> through
    /// the Localization editor, and a String Table is a GUID-wired YAML asset that is easy to corrupt
    /// by hand. This adds the entries through the documented <see cref="LocalizationEditorSettings"/>
    /// API (the same path the editor UI uses) and is idempotent — re-running skips keys that already
    /// exist, so it is safe to run more than once.
    ///
    /// The KEYS are owned by code as constants on <c>AnatomiQ.Core.FallbackManager</c>
    /// (THERMAL_WARNING_STRING_KEY / THERMAL_CRITICAL_STRING_KEY); this tool only populates their
    /// English values. CORE-007 does not DISPLAY these — a UI consumer (CORE-002/UI) subscribes to
    /// OnPerformanceTierChanged and shows the matching string at the Aggressive / Critical tier.
    /// </summary>
    public static class ThermalStringsInstaller
    {
        private const string TABLE_NAME = "UIStrings";
        private const string ENGLISH_CODE = "en";

        // Keys mirror AnatomiQ.Core.FallbackManager.THERMAL_WARNING_STRING_KEY / _CRITICAL_STRING_KEY.
        // Duplicated as literals because the Editor.Localization assembly does not reference Core; if
        // those constants ever change, update these to match (FallbackManager carries a note saying so).
        private const string WARNING_KEY = "ui.system.thermal.warning";
        private const string CRITICAL_KEY = "ui.system.thermal.critical";

        // English copy. Deliberately non-alarming and action-light: the device is managing itself, the
        // app is degrading gracefully, and the user is told what is happening, not asked to act.
        private const string WARNING_TEXT =
            "Your device is getting warm, so some visual detail has been reduced to keep things running smoothly.";
        private const string CRITICAL_TEXT =
            "Your device is hot. AnatomiQ is briefly easing off to let it cool down — this will pass in a moment.";

        [MenuItem("AnatomiQ/Localization/Add CORE-007 Thermal Strings")]
        public static void AddThermalStrings()
        {
            var collection = LocalizationEditorSettings.GetStringTableCollection(TABLE_NAME);
            if (collection == null)
            {
                Debug.LogError(
                    $"[ThermalStringsInstaller] String Table '{TABLE_NAME}' not found. " +
                    "Create it (scaffold Localization step) before running this tool.");
                return;
            }

            if (!(collection.GetTable(ENGLISH_CODE) is StringTable englishTable))
            {
                Debug.LogError(
                    $"[ThermalStringsInstaller] '{TABLE_NAME}' has no English ('{ENGLISH_CODE}') table.");
                return;
            }

            int added = 0;
            added += AddIfMissing(englishTable, WARNING_KEY, WARNING_TEXT) ? 1 : 0;
            added += AddIfMissing(englishTable, CRITICAL_KEY, CRITICAL_TEXT) ? 1 : 0;

            if (added > 0)
            {
                EditorUtility.SetDirty(englishTable);
                EditorUtility.SetDirty(englishTable.SharedData);
                AssetDatabase.SaveAssets();
            }

            Debug.Log(
                $"[ThermalStringsInstaller] Done. {added} new entr{(added == 1 ? "y" : "ies")} " +
                $"added to '{TABLE_NAME}'.");
        }

        /// <summary>
        /// Adds a key+English value via the documented <see cref="StringTable.AddEntry(string,string)"/>
        /// path (which also creates the shared key). Skips if the key already exists in the shared
        /// data — idempotent. Returns true only when a new entry was added.
        /// </summary>
        private static bool AddIfMissing(StringTable englishTable, string key, string englishValue)
        {
            if (englishTable.SharedData.Contains(key))
            {
                Debug.Log($"[ThermalStringsInstaller] '{key}' already present — skipped.");
                return false;
            }

            englishTable.AddEntry(key, englishValue);
            Debug.Log($"[ThermalStringsInstaller] Added '{key}'.");
            return true;
        }
    }
}
#endif
