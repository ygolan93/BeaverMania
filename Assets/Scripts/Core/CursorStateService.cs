using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class CursorStateService : MonoBehaviour
{
    public static CursorStateService Instance { get; private set; }

    public static CursorStateService GetOrCreate()
    {
        return RuntimeServices.GetRequired<CursorStateService>(ServiceLifetime.Persistent);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            BuildSafeLogger.WarnOnce(
                nameof(CursorStateService) + ".DuplicateManager",
                "Duplicate manager destroyed: " + nameof(CursorStateService) + ".",
                this,
                nameof(CursorStateService));
            Destroy(gameObject);
            return;
        }

        if (!RuntimeServices.Register(this, ServiceLifetime.Persistent))
        {
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            RuntimeServices.Unregister(this);
        }
    }

    public void ShowCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void HideCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
