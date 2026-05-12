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
        public const string DefaultLevelSceneName = "Level 1";

        public static void LoadLevel1Single()
        {
            GameTimeScaleGate.ClearAll();
            SceneManager.LoadScene(DefaultLevelSceneName, LoadSceneMode.Single);
        }

        public static void LoadSceneSingle(string sceneName)
        {
            GameTimeScaleGate.ClearAll();
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }

        public static void ReloadActiveSceneSingle()
        {
            GameTimeScaleGate.ClearAll();
            var active = SceneManager.GetActiveScene();
            SceneManager.LoadScene(active.buildIndex, LoadSceneMode.Single);
        }

        public void LoadLevel1() => LoadLevel1Single();

        public void LoadSceneByName(string sceneName) => LoadSceneSingle(sceneName);

        public void ReloadActiveScene() => ReloadActiveSceneSingle();
    }
}
