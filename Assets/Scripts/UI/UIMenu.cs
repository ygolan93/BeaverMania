using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIMenu : MonoBehaviour
{
    public GameObject PauseMenu;
    public GameObject Question;
    public Behaviour Player;
    public bool ActivePause = false;
    GameFlowController gameFlow;
    GameInputReader inputReader;
    [SerializeField] Slider volumeSlider;
    [SerializeField] AudioSource Music;

    // Start is called before the first frame update
    private void Start()
    {
        var playerObject = GameObject.FindGameObjectWithTag("Player");
        if (!RuntimeReferenceValidator.Require(playerObject, this, "Player tag"))
        {
            return;
        }

        gameFlow = GameFlowController.GetOrCreate();
        inputReader = GameInputReader.GetOrCreate();
        inputReader.EnableGameplayInput();
        gameFlow.SetPlaying();
        Player = playerObject.GetComponent<Behaviour>();
        if (!RuntimeReferenceValidator.Require(Player, this, nameof(Player)) ||
            !RuntimeReferenceValidator.Require(PauseMenu, this, nameof(PauseMenu)) ||
            !RuntimeReferenceValidator.Require(Question, this, nameof(Question)))
        {
            return;
        }

        if (Player.seekMusic==true)
        {
            var musicObject = GameObject.FindGameObjectWithTag("Music");
            if (!RuntimeReferenceValidator.Require(musicObject, this, "Music tag"))
            {
                return;
            }

            Music = musicObject.GetComponent<AudioSource>();
            if (!RuntimeReferenceValidator.Require(Music, this, nameof(Music)))
            {
                return;
            }
        }
        Player.HideCursor();
        PauseMenu.SetActive(false);
        Question.SetActive(false);
    }
    public void Pause()
    {
        if (inputReader != null && inputReader.PausePressed)
        {
            ChangeBolean();
        }

        if (ActivePause == true)
        {
            PauseMenu.SetActive(true);
            Player.ShowCursor();
        }


        if (ActivePause == false)
        {
            PauseMenu.SetActive(false);
            Question.SetActive(false);
            if (Player.isAtTrader == false)
                Player.HideCursor();
        }
    }

    public void ChangeBolean()
    {
        ActivePause = !ActivePause;
        if (gameFlow == null)
        {
            gameFlow = GameFlowController.GetOrCreate();
        }
        gameFlow.SetPaused(ActivePause);
        if (inputReader == null)
        {
            inputReader = GameInputReader.GetOrCreate();
        }
        if (ActivePause)
        {
            inputReader.EnableUiInput();
        }
        else if (Player != null && Player.isAtTrader)
        {
            inputReader.EnableUiInput();
        }
        else
        {
            inputReader.EnableGameplayInput();
        }
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
        SceneTransitionService.LoadMenu();
    }
    public void Volume()
    {
        //AudioListener.volume = volumeSlider.value;
        if (Player.seekMusic==true)
        {
            Music.volume = volumeSlider.value;
        }
    }

    // Update is called once per frame
    void Update()
    {
        Pause();
    }
}
