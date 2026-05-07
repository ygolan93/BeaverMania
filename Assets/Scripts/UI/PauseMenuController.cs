using UnityEngine;
using UnityEngine.UI;

public class PauseMenuController : MonoBehaviour
{
    public GameObject PauseMenu;
    public GameObject Question;
    public Behaviour Player;
    public bool ActivePause = false;

    GameFlowController gameFlow;
    GameInputReader inputReader;
    [SerializeField] Slider volumeSlider;
    [SerializeField] AudioSource Music;

    protected virtual void Start()
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

        if (Player.seekMusic)
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
        HidePausePanel();
        HideQuestionPanel();
    }

    protected virtual void Update()
    {
        Pause();
    }

    public void Pause()
    {
        if (inputReader != null && inputReader.PausePressed)
        {
            ChangeBolean();
        }

        if (ActivePause)
        {
            ShowPausePanel();
            Player.ShowCursor();
            return;
        }

        HidePausePanel();
        HideQuestionPanel();
        if (Player != null && !Player.isAtTrader)
        {
            Player.HideCursor();
        }
    }

    public void ChangeBolean()
    {
        ActivePause = !ActivePause;
        SetPaused(ActivePause);
    }

    public void RestartCheckpointFromMenu()
    {
        if (Player != null)
        {
            Player.RestartCheckpoint();
        }

        HideQuestionPanel();
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
        if (Player != null && Player.seekMusic && Music != null && volumeSlider != null)
        {
            Music.volume = volumeSlider.value;
        }
    }

    public void ShowPausePanel()
    {
        SetPanel(PauseMenu, true);
    }

    public void HidePausePanel()
    {
        SetPanel(PauseMenu, false);
    }

    public void ShowQuestionPanel()
    {
        SetPanel(Question, true);
    }

    public void HideQuestionPanel()
    {
        SetPanel(Question, false);
    }

    void SetPaused(bool paused)
    {
        if (gameFlow == null)
        {
            gameFlow = GameFlowController.GetOrCreate();
        }

        gameFlow.SetPaused(paused);
        if (inputReader == null)
        {
            inputReader = GameInputReader.GetOrCreate();
        }

        if (paused || (Player != null && Player.isAtTrader))
        {
            inputReader.EnableUiInput();
            return;
        }

        inputReader.EnableGameplayInput();
    }

    static void SetPanel(GameObject panel, bool active)
    {
        if (panel != null)
        {
            panel.SetActive(active);
        }
    }
}
