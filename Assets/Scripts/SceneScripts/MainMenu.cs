using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    double Timer=57;
    //Behavior Ottis;
    //Start is called before the first frame update
    private void Update()
    {
        Timer -= Time.deltaTime;
        if (Timer <= 0)
        {
            PlayGame();
        }
    }
    public void PlayGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex +1);
    }
    public void QuitGame()
    {
        Debug.Log("Quit");
        Application.Quit();
    }
}
