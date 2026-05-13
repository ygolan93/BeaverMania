using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Beavermania.Core.GameFlow;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Beavermania.EditorTools
{
    /// <summary>
    /// Copies gameplay roots from <c>Level 1.unity</c> into <c>Level 1 - Remastered - Steam.unity</c>,
    /// fixes GameMaster tag, replaces Main Camera / EventSystem when missing, and validates singletons.
    /// Run once from the menu; re-running may duplicate movable NPC roots — delete duplicates first if needed.
    /// </summary>
    public static class Level1RemasterSteamMigration
    {
        const string SourceScenePath = "Assets/Scenes/Level 1.unity";
        const string DestinationScenePath = "Assets/Scenes/Level 1 - Remastered - Steam.unity";

        /// <summary>Batch / CI entry (Unity -executeMethod Beavermania.EditorTools.Level1RemasterSteamMigration.ExecuteBatch).</summary>
        public static void ExecuteBatch()
        {
            try
            {
                RunInternal();
                Debug.Log("[Level1RemasterSteamMigration] Completed successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError("[Level1RemasterSteamMigration] Failed: " + ex);
                EditorApplication.Exit(1);
                return;
            }

            EditorApplication.Exit(0);
        }

        [MenuItem("Beavermania/Migration/Level 1 → Remastered Steam (copy gameplay)")]
        public static void ExecuteFromMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "Scene migration",
                    "Copy filtered gameplay roots from Level 1 into Level 1 - Remastered - Steam, replace Main Camera from Level 1, add EventSystem if missing, set GameMaster tag to GM, and remove placeholder Isle Cube?\n\nSave all scenes first. Recommended: commit or backup before running.",
                    "Run",
                    "Cancel"))
                return;

            RunInternal();
            EditorUtility.DisplayDialog("Migration", "Done. Check the Console for details and run Play Mode smoke tests.", "OK");
        }

        [MenuItem("Beavermania/Migration/Validate Level 1 - Remastered - Steam scene")]
        public static void ValidateDestinationMenu()
        {
            var dst = EditorSceneManager.OpenScene(DestinationScenePath, OpenSceneMode.Single);
            var report = BuildValidationReport(dst);
            Debug.Log(report);
            EditorUtility.DisplayDialog("Validation", report, "OK");
        }

        static void RunInternal()
        {
            var log = new StringBuilder();
            log.AppendLine("[Level1RemasterSteamMigration] Starting…");

            var dstScene = EditorSceneManager.OpenScene(DestinationScenePath, OpenSceneMode.Single);
            var srcScene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Additive);

            try
            {
                EnsureFolderRoots(dstScene, log);
                RemovePlaceholderIsleCube(dstScene, log);

                var dstRoots = new Dictionary<string, GameObject>(StringComparer.Ordinal);
                foreach (var go in dstScene.GetRootGameObjects())
                {
                    if (!dstRoots.ContainsKey(go.name))
                        dstRoots[go.name] = go;
                }

                ReplaceMainCameraFromSource(srcScene, dstScene, dstRoots, log);
                EnsureEventSystemFromSource(srcScene, dstScene, log);
                FixGameMasterTag(dstScene, log);

                CopyGameplayRoots(srcScene, dstScene, dstRoots, log);

                EditorSceneManager.MarkSceneDirty(dstScene);
            }
            finally
            {
                EditorSceneManager.CloseScene(srcScene, true);
            }

            log.AppendLine(BuildValidationReport(dstScene));
            Debug.Log(log.ToString());
            EditorSceneManager.SaveScene(dstScene);
        }

        static void EnsureFolderRoots(Scene dstScene, StringBuilder log)
        {
            Transform Ensure(string name)
            {
                var existing = dstScene.GetRootGameObjects().FirstOrDefault(go => go.name == name);
                if (existing != null)
                    return existing.transform;

                var go = new GameObject(name);
                SceneManager.MoveGameObjectToScene(go, dstScene);
                go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                log.AppendLine("Created folder root: " + name);
                return go.transform;
            }

            Ensure("_Systems");
            Ensure("_Player");
            Ensure("_UI");
            Ensure("_NPCs");
            Ensure("_Enemies");
            Ensure("_Checkpoints");
        }

        static void RemovePlaceholderIsleCube(Scene dstScene, StringBuilder log)
        {
            foreach (var go in dstScene.GetRootGameObjects())
            {
                if (go.name != "Cube")
                    continue;
                if (!string.Equals(go.tag, "Isle", StringComparison.Ordinal))
                    continue;
                UnityEngine.Object.DestroyImmediate(go);
                log.AppendLine("Removed placeholder root Cube (tag Isle).");
                return;
            }
        }

        static void ReplaceMainCameraFromSource(Scene srcScene, Scene dstScene, Dictionary<string, GameObject> dstRoots, StringBuilder log)
        {
            var srcCam = FindRoot(srcScene, "Main Camera");
            if (srcCam == null)
            {
                log.AppendLine("WARNING: Source scene has no root Main Camera.");
                return;
            }

            if (dstRoots.TryGetValue("Main Camera", out var dstCam))
            {
                UnityEngine.Object.DestroyImmediate(dstCam);
                log.AppendLine("Removed destination Main Camera (replaced from Level 1).");
            }

            var clone = UnityEngine.Object.Instantiate(srcCam);
            clone.name = "Main Camera";
            SceneManager.MoveGameObjectToScene(clone, dstScene);
            ParentUnder(clone.transform, dstScene, "_Systems");
            log.AppendLine("Cloned Main Camera from Level 1 into destination under _Systems.");
        }

        static void EnsureEventSystemFromSource(Scene srcScene, Scene dstScene, StringBuilder log)
        {
            if (UnityEngine.Object.FindObjectsOfType<EventSystem>().Any(es => es.gameObject.scene == dstScene))
            {
                log.AppendLine("EventSystem already present in destination — skipped.");
                return;
            }

            var srcEs = FindRoot(srcScene, "EventSystem");
            if (srcEs == null)
            {
                log.AppendLine("WARNING: Source has no root EventSystem — add one manually in destination.");
                return;
            }

            var clone = UnityEngine.Object.Instantiate(srcEs);
            clone.name = "EventSystem";
            SceneManager.MoveGameObjectToScene(clone, dstScene);
            ParentUnder(clone.transform, dstScene, "_Systems");
            log.AppendLine("Cloned EventSystem from Level 1 under _Systems.");
        }

        static void FixGameMasterTag(Scene dstScene, StringBuilder log)
        {
            foreach (var go in dstScene.GetRootGameObjects())
            {
                if (go.name != "GameMaster")
                    continue;
                if (!string.Equals(go.tag, "GM", StringComparison.Ordinal))
                {
                    Undo.RecordObject(go, "Set GameMaster tag GM");
                    go.tag = "GM";
                    log.AppendLine("Set GameMaster tag to GM.");
                }
                else
                {
                    log.AppendLine("GameMaster already tagged GM.");
                }

                ParentUnder(go.transform, dstScene, "_Systems");
                return;
            }

            log.AppendLine("WARNING: No root GameMaster found in destination.");
        }

        static void CopyGameplayRoots(Scene srcScene, Scene dstScene, Dictionary<string, GameObject> dstRoots, StringBuilder log)
        {
            var copied = 0;
            foreach (var root in srcScene.GetRootGameObjects())
            {
                if (!ShouldCopyRoot(root, dstScene))
                    continue;

                if (dstRoots.ContainsKey(root.name) && IsSingletonDestinationName(root.name))
                {
                    log.AppendLine("Skip duplicate singleton name in destination: " + root.name);
                    continue;
                }

                var clone = UnityEngine.Object.Instantiate(root);
                clone.name = root.name;
                SceneManager.MoveGameObjectToScene(clone, dstScene);
                ParentForGameplay(clone.transform, dstScene);
                copied++;
                log.AppendLine("Copied root: " + root.name);
            }

            log.AppendLine("Total gameplay roots copied: " + copied);
        }

        static bool IsSingletonDestinationName(string name)
        {
            return name == "Player"
                   || name == "PlayerCanvas"
                   || name == "GameMusic"
                   || name == "GameMaster"
                   || name == "Directional Light";
        }

        static bool ShouldCopyRoot(GameObject go, Scene dstScene)
        {
            var srcByPath = SceneManager.GetSceneByPath(SourceScenePath);
            if (!srcByPath.IsValid() || go.scene != srcByPath)
                return false;

            var n = go.name;
            if (n == "Main Camera" || n == "EventSystem" || n == "GameMaster" || n == "Directional Light")
                return false;
            if (n == "Player" || n == "PlayerCanvas" || n == "GameMusic")
                return false;

            if (go.GetComponent<Terrain>() != null)
                return false;
            if (string.Equals(n, "BeaverMania Village", StringComparison.Ordinal))
                return false;

            if (string.Equals(n, "Player at Startpoint", StringComparison.Ordinal))
                return true;
            if (string.Equals(n, "TraderSpawn", StringComparison.Ordinal) || string.Equals(n, "WayPoints", StringComparison.Ordinal))
                return true;
            if (n.IndexOf("Trader", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (n.StartsWith("Beaverius", StringComparison.OrdinalIgnoreCase))
                return true;
            if (n.StartsWith("Scorpion", StringComparison.OrdinalIgnoreCase))
                return true;
            if (n.StartsWith("CheckPoint", StringComparison.OrdinalIgnoreCase))
                return true;
            if (n.StartsWith("Wasp", StringComparison.OrdinalIgnoreCase))
                return true;
            if (n.StartsWith("insect", StringComparison.OrdinalIgnoreCase))
                return true;
            if (string.Equals(n, "MusicIntro", StringComparison.Ordinal) || string.Equals(n, "MusicKeys", StringComparison.Ordinal))
                return true;
            if (string.Equals(n, "Beaver MillitaryCaptain", StringComparison.Ordinal))
                return true;

            if (go.CompareTag("SwitchMusic"))
                return true;
            if (go.CompareTag("Trader"))
                return true;

            return false;
        }

        static void ParentForGameplay(Transform t, Scene dstScene)
        {
            var n = t.name;
            if (n.StartsWith("Scorpion", StringComparison.OrdinalIgnoreCase)
                || n.StartsWith("Wasp", StringComparison.OrdinalIgnoreCase)
                || n.StartsWith("insect", StringComparison.OrdinalIgnoreCase))
            {
                ParentUnder(t, dstScene, "_Enemies");
                return;
            }

            if (n.StartsWith("CheckPoint", StringComparison.OrdinalIgnoreCase))
            {
                ParentUnder(t, dstScene, "_Checkpoints");
                return;
            }

            if (n.IndexOf("Trader", StringComparison.OrdinalIgnoreCase) >= 0
                || n.StartsWith("Beaverius", StringComparison.OrdinalIgnoreCase)
                || string.Equals(n, "Beaver MillitaryCaptain", StringComparison.Ordinal))
            {
                ParentUnder(t, dstScene, "_NPCs");
                return;
            }

            if (string.Equals(n, "Player at Startpoint", StringComparison.Ordinal))
            {
                ParentUnder(t, dstScene, "_Player");
                return;
            }

            if (string.Equals(n, "WayPoints", StringComparison.Ordinal)
                || string.Equals(n, "TraderSpawn", StringComparison.Ordinal)
                || n == "MusicIntro"
                || n == "MusicKeys"
                || t.CompareTag("SwitchMusic"))
            {
                ParentUnder(t, dstScene, "_Systems");
                return;
            }

            ParentUnder(t, dstScene, "_Systems");
        }

        static void ParentUnder(Transform t, Scene dstScene, string folderName)
        {
            var folder = dstScene.GetRootGameObjects().FirstOrDefault(go => go.name == folderName);
            if (folder == null)
                return;
            t.SetParent(folder.transform, true);
        }

        static GameObject FindRoot(Scene scene, string name)
        {
            return scene.GetRootGameObjects().FirstOrDefault(go => go.name == name);
        }

        static string BuildValidationReport(Scene dstScene)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Validation: Level 1 - Remastered - Steam ===");

            var gmList = UnityEngine.Object.FindObjectsOfType<GameMaster>()
                .Where(g => g != null && g.gameObject.scene == dstScene)
                .Select(g => g.gameObject)
                .ToList();

            var gm = gmList.FirstOrDefault();
            if (gm == null)
                sb.AppendLine("FAIL: No GameMaster component in destination.");
            else if (!string.Equals(gm.tag, "GM", StringComparison.Ordinal))
                sb.AppendLine("FAIL: GameMaster present but tag is not GM (current: " + gm.tag + ").");
            else
                sb.AppendLine("OK: GameMaster with tag GM.");

            var eventSystems = UnityEngine.Object.FindObjectsOfType<EventSystem>()
                .Where(es => es.gameObject.scene == dstScene).ToList();
            if (eventSystems.Count == 0)
                sb.AppendLine("FAIL: No EventSystem in destination.");
            else if (eventSystems.Count > 1)
                sb.AppendLine("WARN: Multiple EventSystems in destination (" + eventSystems.Count + ").");
            else
                sb.AppendLine("OK: Single EventSystem.");

            var listeners = UnityEngine.Object.FindObjectsOfType<AudioListener>()
                .Where(al => al.enabled && al.gameObject.scene == dstScene).ToList();
            if (listeners.Count == 0)
                sb.AppendLine("WARN: No enabled AudioListener in destination.");
            else if (listeners.Count > 1)
                sb.AppendLine("WARN: Multiple enabled AudioListeners (" + listeners.Count + ").");
            else
                sb.AppendLine("OK: Single enabled AudioListener.");

            var player = FindTaggedObjectInScene(dstScene, "Player");
            if (player == null)
                sb.AppendLine("WARN: No GameObject with tag Player in destination scene.");
            else
                sb.AppendLine("OK: Player with tag Player exists.");

            return sb.ToString();
        }

        static GameObject FindTaggedObjectInScene(Scene scene, string tag)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.gameObject.scene == scene && t.CompareTag(tag))
                        return t.gameObject;
                }
            }

            return null;
        }
    }
}
