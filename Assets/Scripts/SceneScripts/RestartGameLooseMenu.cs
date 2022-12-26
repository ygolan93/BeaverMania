using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RestartGameLooseMenu : MonoBehaviour
{
    public void ResetartGame()
    {
        SceneManager.LoadScene("Level 1");
    }
}
