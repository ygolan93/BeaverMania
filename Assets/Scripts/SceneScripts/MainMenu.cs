using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
    private bool playRequested;

    //double Timer=57;
    //Behavior Ottis;
    //Start is called before the first frame update
    //private void Update()
    //{
    //    Timer -= Time.deltaTime;
    //    if (Timer <= 0)
    //    {
    //        PlayGame();
    //    }
    //}
    public void PlayGame()
    {
        if (playRequested)
        {
            return;
        }

        playRequested = true;
        SceneTransitionService.LoadLevel1();
    }
    public void QuitGame()
    {
        BuildSafeLogger.InfoOnce("MainMenu.QuitGame", "Quit requested.", this, null, null, nameof(QuitGame));
        Application.Quit();
    }
}
