using UnityEngine;

public sealed class LoseMenuController : MonoBehaviour
{
    public GameObject LosePanel;

    private void Start()
    {
        HideLosePanel();
    }

    public void ShowLosePanel()
    {
        if (!TrySetGameOver())
        {
            return;
        }

        CursorStateService.GetOrCreate().ShowCursor();
        SetPanel(true);
    }

    public void HideLosePanel()
    {
        SetPanel(false);
    }

    public void RestartGame()
    {
        SceneTransitionService.ReloadActiveScene();
    }

    public void ResetartGame()
    {
        RestartGame();
    }

    public void MainMenu()
    {
        SceneTransitionService.LoadMenu();
    }

    bool TrySetGameOver()
    {
        var gameFlow = GameFlowController.GetOrCreate();
        if (gameFlow.State == GameFlowState.GameOver)
        {
            return true;
        }

        if (gameFlow.State == GameFlowState.Transitioning)
        {
            return false;
        }

        return gameFlow.SetGameOver();
    }

    void SetPanel(bool active)
    {
        if (LosePanel != null)
        {
            if (LosePanel.activeSelf != active)
            {
                LosePanel.SetActive(active);
            }
            return;
        }

        BuildSafeLogger.WarnOnce(
            nameof(LoseMenuController) + ".NullPanel." + nameof(LosePanel),
            "UI panel is null; cannot set active=" + active + ".",
            this,
            nameof(LosePanel));
    }
}
