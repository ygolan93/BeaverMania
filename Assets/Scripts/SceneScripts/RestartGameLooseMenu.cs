using UnityEngine;

public sealed class RestartGameLooseMenu : MonoBehaviour
{
    [SerializeField] LoseMenuController loseMenuController;

    public void ResetartGame()
    {
        if (loseMenuController != null)
        {
            loseMenuController.RestartGame();
            return;
        }

        SceneTransitionService.ReloadActiveScene();
    }
}
