using Beavermania.Display;
using Beavermania.Player.Combat;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Beavermania.Core.GameFlow
{
    /// <summary>
    /// Centralizes scene reload / level restart. Static helpers allow UI callbacks without a prefab
    /// reference; instance methods are available when this component is placed on a scene object.
    /// </summary>
    public sealed class SceneRestartController : MonoBehaviour
    {
        public const string DefaultLevelSceneName = "Level 1 - Remastered - Steam";
        public const string MainMenuSceneName = "Menu";

        public static void LoadLevel1Single()
        {
            GameTimeScaleGate.ClearAll();
            Projectile.ClearAllPools();
            SceneManager.LoadScene(DefaultLevelSceneName, LoadSceneMode.Single);
        }

        public static void LoadSceneSingle(string sceneName)
        {
            GameTimeScaleGate.ClearAll();
            Projectile.ClearAllPools();
            if (IsMainMenuScene(sceneName))
                PlayerCursorRules.ApplyUnlockedVisible();
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }

        public static bool IsMainMenuScene(string sceneName) =>
            sceneName == MainMenuSceneName;

        public static void ReloadActiveSceneSingle()
        {
            GameTimeScaleGate.ClearAll();
            Projectile.ClearAllPools();
            var active = SceneManager.GetActiveScene();
            SceneManager.LoadScene(active.buildIndex, LoadSceneMode.Single);
        }

        public void LoadLevel1() => LoadLevel1Single();

        public void LoadSceneByName(string sceneName) => LoadSceneSingle(sceneName);

        public void ReloadActiveScene() => ReloadActiveSceneSingle();
    }
}
