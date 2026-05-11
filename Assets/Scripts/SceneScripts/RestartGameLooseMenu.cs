using UnityEngine;

public sealed class RestartGameLooseMenu : MonoBehaviour
{
    [SerializeField] LoseMenuController loseMenuController;

    public void ResetartGame()
    {
        ResolveLoseMenuController();
        if (loseMenuController != null)
        {
            loseMenuController.RestartGame();
        }
    }

    void ResolveLoseMenuController()
    {
        if (loseMenuController == null)
        {
            loseMenuController = GetComponentInParent<LoseMenuController>();
        }
    }
}
