using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Chronoforge.Editor
{
    /// <summary>
    /// Creates the PanelSettings asset runtime UI Toolkit requires and attaches it to a
    /// <see cref="UIDocument"/>. Without PanelSettings the overlay renders nothing silently, so
    /// this exists to make the one non-obvious setup step a single click.
    /// </summary>
    public static class BuildOrderOverlaySetup
    {
        private const string k_Folder = "Assets/Chronoforge Demo";
        private const string k_PanelPath = k_Folder + "/Chronoforge Overlay Panel.asset";

        /// <summary>
        /// Returns the shared PanelSettings, creating it if needed. Returns null when no
        /// ThemeStyleSheet exists in the project — runtime UI Toolkit cannot render without one.
        /// </summary>
        public static PanelSettings GetOrCreatePanelSettings(out string error)
        {
            error = "";
            EnsureFolder();

            var existing = AssetDatabase.LoadAssetAtPath<PanelSettings>(k_PanelPath);
            if (existing != null)
                return existing;

            ThemeStyleSheet theme = FindTheme();
            if (theme == null)
            {
                error =
                    "No ThemeStyleSheet found in the project. Create one via " +
                    "Assets ▸ Create ▸ UI Toolkit ▸ Panel Settings Asset (Unity generates the default " +
                    "runtime theme alongside it), then run this again.";
                return null;
            }

            var settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.themeStyleSheet = theme;
            AssetDatabase.CreateAsset(settings, k_PanelPath);
            AssetDatabase.SaveAssets();
            return settings;
        }

        /// <summary>Assigns PanelSettings to the document when it has none. Returns false on failure.</summary>
        public static bool Configure(UIDocument document, out string error)
        {
            error = "";
            if (document == null)
            {
                error = "No UIDocument to configure.";
                return false;
            }
            if (document.panelSettings != null)
                return true;

            PanelSettings settings = GetOrCreatePanelSettings(out error);
            if (settings == null)
                return false;

            document.panelSettings = settings;
            EditorUtility.SetDirty(document);
            return true;
        }

        [MenuItem("Chronoforge/Create Overlay Setup", priority = 21)]
        private static void CreateOverlaySetupMenu()
        {
            PanelSettings settings = GetOrCreatePanelSettings(out string error);
            if (settings == null)
            {
                EditorUtility.DisplayDialog("Chronoforge — Overlay setup", error, "OK");
                return;
            }

            Selection.activeObject = settings;
            EditorUtility.DisplayDialog(
                "Chronoforge — Overlay setup",
                "PanelSettings ready at:\n" + k_PanelPath +
                "\n\nAdd a Build Order Overlay component to a GameObject; assign this asset to its UIDocument.",
                "OK");
        }

        private static ThemeStyleSheet FindTheme()
        {
            string[] guids = AssetDatabase.FindAssets("t:ThemeStyleSheet");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(path);
                if (theme != null)
                    return theme;
            }
            return null;
        }

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder(k_Folder))
                AssetDatabase.CreateFolder("Assets", "Chronoforge Demo");
        }
    }
}
