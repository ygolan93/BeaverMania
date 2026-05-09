using UnityEngine;

public class DevelopmentRuntimeDiagnostics : MonoBehaviour
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        if (FindObjectOfType<DevelopmentRuntimeDiagnostics>() != null)
        {
            return;
        }

        var diagnostics = new GameObject(nameof(DevelopmentRuntimeDiagnostics));
        DontDestroyOnLoad(diagnostics);
        diagnostics.AddComponent<DevelopmentRuntimeDiagnostics>();
    }

    [ContextMenu("QA/Force Game Over")]
    public void ForceGameOver()
    {
        var player = FindObjectOfType<Behaviour>();
        if (player != null)
        {
            player.Lives = 0;
            player.HandleFailure(PlayerFailureReason.DebugForcedGameOver);
            return;
        }

        GameFlowController.GetOrCreate().SetGameOver();
    }

    [ContextMenu("QA/Log Runtime Services")]
    public void LogRuntimeServices()
    {
        BuildSafeLogger.InfoOnce(nameof(DevelopmentRuntimeDiagnostics) + ".RuntimeServices." + Time.frameCount, RuntimeServices.GetDiagnostics(), this);
    }

    [ContextMenu("QA/Log Cursor/Input")]
    public void LogCursorInput()
    {
        var input = GameInputReader.GetOrCreate();
        BuildSafeLogger.InfoOnce(
            nameof(DevelopmentRuntimeDiagnostics) + ".CursorInput." + Time.frameCount,
            "Cursor visible=" + Cursor.visible + " lock=" + Cursor.lockState + " input=" + input.Mode + ".",
            this);
    }

    [ContextMenu("QA/Log Scene Transition")]
    public void LogSceneTransition()
    {
        var service = SceneTransitionService.GetOrCreate();
        BuildSafeLogger.InfoOnce(
            nameof(DevelopmentRuntimeDiagnostics) + ".SceneTransition." + Time.frameCount,
            "Scene transition loading=" + service.isLoading + ".",
            this);
    }
#endif
}
