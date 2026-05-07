using UnityEngine;

public sealed class DebugSceneResetShortcut : MonoBehaviour
{
    [SerializeField] DebugQAConfig config;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public void Configure(DebugQAConfig debugConfig) => config = debugConfig;

    void Update()
    {
        if (config != null && config.enableSceneResetShortcut && Input.GetKeyDown(config.sceneResetKey))
        {
            SceneTransitionService.ReloadActiveScene();
        }
    }
#else
    public void Configure(DebugQAConfig debugConfig) => config = debugConfig;
#endif
}
