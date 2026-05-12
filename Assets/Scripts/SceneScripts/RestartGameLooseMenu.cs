using Beavermania.Core.GameFlow;
using UnityEngine;

namespace Beavermania.UI.Menus
{

    public class RestartGameLooseMenu : MonoBehaviour
    {
        public void ResetartGame()
        {
            SceneRestartController.LoadLevel1Single();
        }
    }
}
