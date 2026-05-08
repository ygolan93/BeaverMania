using UnityEngine;

[DisallowMultipleComponent]
public sealed class DebugBootstrapper : MonoBehaviour
{
    [SerializeField] DebugQAConfig config;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    const string RootName = "Debug QA Runtime";
    static GameObject root;

    void Awake()
    {
        if (config == null || !config.enableDebugBootstrapper)
        {
            enabled = false;
            return;
        }

        if (root != null)
        {
            return;
        }

        root = new GameObject(RootName);
        DontDestroyOnLoad(root);

        if (config.enableFpsDisplay)
        {
            root.AddComponent<FpsDisplay>().Configure(config);
        }

        if (config.enableSceneResetShortcut)
        {
            root.AddComponent<DebugSceneResetShortcut>().Configure(config);
        }

        if (config.enableCheckpointTeleport)
        {
            root.AddComponent<DebugCheckpointTeleport>().Configure(config);
        }

        if (config.enableDamageTrigger)
        {
            root.AddComponent<DebugDamageTrigger>().Configure(config);
        }
    }
#endif
}
