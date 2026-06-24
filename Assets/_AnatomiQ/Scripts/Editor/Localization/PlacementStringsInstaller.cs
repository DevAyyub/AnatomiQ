#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;

namespace AnatomiQ.Editor.Localization
{
    /// <summary>
    /// One-shot Editor utility that authors ATLAS-003 chunk 5's placement / AR-status user strings into
    /// the existing <c>UIStrings</c> String Table (English locale). Run once via
    /// <c>AnatomiQ ▸ Localization ▸ Add ATLAS-003 Placement Strings</c>.
    ///
    /// Same rationale as <c>ThermalStringsInstaller</c>: a String Table is a GUID-wired YAML asset that
    /// is easy to corrupt by hand, so entries are added through the documented
    /// <see cref="LocalizationEditorSettings"/> API (the path the editor UI itself uses). Idempotent —
    /// re-running skips keys that already have a value, so it is safe to run more than once.
    ///
    /// The KEYS are owned by code as constants on the UI consumers (PlacementModeSwitcher /
    /// TrackingLostToast); this tool only populates their English values. They are duplicated here as
    /// literals because the Editor.Localization assembly does not reference AnatomiQ.UI; if a key ever
    /// changes, update it in both places (the UI components carry a matching note).
    /// </summary>
    public static class PlacementStringsInstaller
    {
        private const string TABLE_NAME = "UIStrings";
        private const string ENGLISH_CODE = "en";

        // key → English value. Keys follow Build Environment C.4 (ui.<screen>.<element>.<state>).
        private static readonly (string Key, string Value)[] Entries =
        {
            ("ui.placement.mode.viewer",          "3D Viewer"),
            ("ui.placement.mode.space",           "Space"),
            ("ui.placement.mode.surface",         "Surface"),
            ("ui.placement.enable_ar",            "Tap to enable AR"),
            ("ui.placement.camera_rationale",     "AnatomiQ uses your camera to place the body in the room around you."),
            ("ui.placement.rationale.continue",   "Continue"),
            ("ui.placement.rationale.cancel",     "Not now"),
            ("ui.placement.entering_ar",          "Starting AR\u2026"),
            ("ui.ar.tracking_lost",               "Move your phone slowly to find your surroundings."),
            ("ui.ar.camera_denied",               "Camera access is off. Enable it in Settings to use AR."),
            ("ui.ar.unsupported",                 "This device doesn\u2019t support AR \u2014 showing the 3D viewer."),
        };

        [MenuItem("AnatomiQ/Localization/Add ATLAS-003 Placement Strings")]
        public static void AddPlacementStrings()
        {
            var collection = LocalizationEditorSettings.GetStringTableCollection(TABLE_NAME);
            if (collection == null)
            {
                Debug.LogError(
                    $"[PlacementStringsInstaller] String Table '{TABLE_NAME}' not found. " +
                    "Create it (scaffold Localization step) before running this tool.");
                return;
            }

            StringTable english = GetEnglishTable(collection);
            if (english == null)
            {
                Debug.LogError(
                    $"[PlacementStringsInstaller] No '{ENGLISH_CODE}' table in '{TABLE_NAME}'. " +
                    "Add the English locale (scaffold Localization step) before running this tool.");
                return;
            }

            var shared = collection.SharedData;
            int added = 0;

            foreach (var (key, value) in Entries)
            {
                var sharedEntry = shared.GetEntry(key) ?? shared.AddKey(key);

                StringTableEntry existing = english.GetEntry(sharedEntry.Id);
                if (existing != null && !string.IsNullOrEmpty(existing.Value))
                {
                    continue; // idempotent: leave already-authored values untouched
                }

                english.AddEntry(sharedEntry.Id, value);
                added++;
            }

            EditorUtility.SetDirty(shared);
            EditorUtility.SetDirty(english);
            AssetDatabase.SaveAssets();

            Debug.Log($"[PlacementStringsInstaller] Added {added} new string(s) to '{TABLE_NAME}' ({ENGLISH_CODE}); " +
                      $"{Entries.Length - added} already present.");
        }

        private static StringTable GetEnglishTable(StringTableCollection collection)
        {
            foreach (var table in collection.StringTables)
            {
                if (table.LocaleIdentifier.Code == ENGLISH_CODE)
                {
                    return table;
                }
            }

            return null;
        }
    }
}
#endif
