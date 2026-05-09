using UnityEngine;

[DisallowMultipleComponent]
public sealed class DebugBootstrapper : MonoBehaviour
{
    public static DebugBootstrapper Instance { get; private set; }

    [SerializeField] DebugQAConfig config;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    const string RootName = "Debug QA Runtime";
    static GameObject root;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (!RuntimeServices.Register(this, ServiceLifetime.Persistent))
        {
            return;
        }

        Instance = this;

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

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            RuntimeServices.Unregister(this);
        }
    }
#endif
}
