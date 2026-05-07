using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class CursorStateService : MonoBehaviour
{
    public static CursorStateService Instance { get; private set; }

    public static CursorStateService GetOrCreate()
    {
        if (Instance != null)
        {
            return Instance;
        }

        Instance = FindObjectOfType<CursorStateService>();
        if (Instance != null)
        {
            return Instance;
        }

        var gameObject = new GameObject(nameof(CursorStateService));
        return gameObject.AddComponent<CursorStateService>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
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
