using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RestartGameLooseMenu : MonoBehaviour
{
    public void ResetartGame()
    {
        SceneTransitionService.LoadLevel1();
    }
}
