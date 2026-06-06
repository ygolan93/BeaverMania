using System.IO;
using UnityEditor;
using UnityEngine;

namespace Beavermania.EditorTools
{
    /// <summary>
    /// Resolves "Invalid editor window of type: UnityEditor.FallbackEditorWindow, title: Failed to load"
    /// when a saved layout references an EditorWindow that no longer exists or failed to compile.
    /// </summary>
    public static class ResetEditorLayoutUtility
    {
        const string ProjectLayoutFileName = "CurrentLayout-default.dwlt";
        const string UserLastLayoutRelativePath = @"Unity\Editor-5.x\Preferences\Layouts\default\LastLayout.dwlt";

        [MenuItem("Beavermania/Fix/Reset Editor Window Layout (silent)")]
        public static void ResetFromMenuSilent()
        {
            ResetLayout();
        }

        [MenuItem("Beavermania/Fix/Reset Editor Window Layout")]
        public static void ResetFromMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "Reset Editor window layout",
                    "Load Unity's default window layout and remove saved layout files?\n\n"
                    + "Use this when you see \"FallbackEditorWindow / Failed to load\" in the Console.\n\n"
                    + "Unsaved docked tool windows may close.",
                    "Reset",
                    "Cancel"))
                return;

            ResetLayout();
        }

        public static void ResetLayout()
        {
            int deletedCount = 0;
            deletedCount += DeleteIfExists(GetProjectLayoutPath());
            deletedCount += DeleteIfExists(GetUserLastLayoutPath());

            if (!EditorApplication.ExecuteMenuItem("Window/Layouts/Default"))
            {
                Debug.LogWarning("[ResetEditorLayoutUtility] Could not run Window/Layouts/Default. "
                    + "Use Window > Layouts > Default manually, then restart the Editor if the warning persists.");
                return;
            }

            Debug.Log("[ResetEditorLayoutUtility] Default layout loaded. Deleted " + deletedCount
                + " saved layout file(s). Restart Unity if \"Failed to load\" still appears on next domain reload.");
        }

        static string GetProjectLayoutPath()
        {
            return Path.Combine(Directory.GetCurrentDirectory(), "Library", ProjectLayoutFileName);
        }

        static string GetUserLastLayoutPath()
        {
            string appData = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, UserLastLayoutRelativePath);
        }

        static int DeleteIfExists(string path)
        {
            if (!File.Exists(path))
                return 0;

            File.Delete(path);
            Debug.Log("[ResetEditorLayoutUtility] Deleted " + path);
            return 1;
        }
    }
}
