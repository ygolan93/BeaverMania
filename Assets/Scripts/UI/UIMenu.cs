using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIMenu : MonoBehaviour
{
    public GameObject PauseMenu;
    public GameObject Question;
    public Behaviour Player;
    public bool ActivePause = false;
    [SerializeField] Slider volumeSlider;

    // Start is called before the first frame update
    private void Start()
    {
        Player = GameObject.FindGameObjectWithTag("Player").GetComponent<Behaviour>();
        Player.HideCursor();
        PauseMenu.SetActive(false);
        Question.SetActive(false);
    }
    public void Pause()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ChangeBolean();
        }

        if (ActivePause == true)
        {
            PauseMenu.SetActive(true);
            Time.timeScale = 0;
            Player.ShowCursor();
        }


        if (ActivePause == false)
        {
            PauseMenu.SetActive(false);
            Time.timeScale = 1f;
            Question.SetActive(false);
            if (Player.isAtTrader == false)
                Player.HideCursor();
        }
    }

    public void ChangeBolean()
    {
        ActivePause = !ActivePause;
    }

    public void RestartCheckpointFromMenu()
    {
        Player.RestartCheckpoint();
        Question.SetActive(false);
    }
    public void QuitGame()
    {
        Application.Quit();
    }

    public void MainMenu()
    {
        SceneManager.LoadScene("Menu");
    }
    public void Volume()
    {
        AudioListener.volume = volumeSlider.value;
    }

    // Update is called once per frame
    void Update()
    {
        Pause();
    }
}
