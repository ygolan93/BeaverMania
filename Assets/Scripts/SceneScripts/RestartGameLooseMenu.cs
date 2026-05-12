using Beavermania.Core.GameFlow;
using UnityEngine;

public class RestartGameLooseMenu : MonoBehaviour
{
    public void ResetartGame()
    {
        SceneRestartController.LoadLevel1Single();
    }
}
