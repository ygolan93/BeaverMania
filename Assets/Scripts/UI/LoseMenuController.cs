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

    void SetPanel(bool active)
    {
        if (LosePanel != null)
        {
            LosePanel.SetActive(active);
        }
    }
}
