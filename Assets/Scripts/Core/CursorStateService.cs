using System;
using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class CursorStateService : MonoBehaviour
{
    public static CursorStateService Instance { get; private set; }

    int cursorStateVersion;

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
        cursorStateVersion++;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void HideCursor()
    {
        cursorStateVersion++;
        ApplyGameplayCursorState();
    }

    public void RestoreGameplayCursorAfterUiClose(Func<bool> canRestore = null)
    {
        StartCoroutine(RestoreGameplayCursorAfterUiCloseCoroutine(cursorStateVersion, canRestore));
    }

    IEnumerator RestoreGameplayCursorAfterUiCloseCoroutine(int requestVersion, Func<bool> canRestore)
    {
        yield return null;
        yield return new WaitForEndOfFrame();

        if (requestVersion != cursorStateVersion ||
            !CanRestoreGameplayCursor(canRestore))
        {
            yield break;
        }

        cursorStateVersion++;
        ApplyGameplayCursorState();
    }

    static bool CanRestoreGameplayCursor(Func<bool> canRestore)
    {
        if (canRestore != null && !canRestore())
        {
            return false;
        }

        var flow = GameFlowController.Instance;
        return flow == null || flow.State == GameFlowState.Playing;
    }

    static void ApplyGameplayCursorState()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
