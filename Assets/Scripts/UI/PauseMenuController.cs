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

    protected virtual void OnEnable()
    {
        inputReader = GameInputReader.GetOrCreate();
        inputReader.PausePressedEvent += HandlePausePressed;
    }

    protected virtual void OnDisable()
    {
        if (inputReader != null)
        {
            inputReader.PausePressedEvent -= HandlePausePressed;
        }
    }

    protected virtual void Start()
    {
        var playerObject = GameObject.FindGameObjectWithTag("Player");
        if (!RuntimeReferenceValidator.Require(playerObject, this, "Player tag"))
        {
            return;
        }

        gameFlow = GameFlowController.GetOrCreate();
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

    void HandlePausePressed()
    {
        ChangeBolean();
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
        SetPanel(PauseMenu, true, this, nameof(PauseMenu));
    }

    public void HidePausePanel()
    {
        SetPanel(PauseMenu, false, this, nameof(PauseMenu));
    }

    public void ShowQuestionPanel()
    {
        SetPanel(Question, true, this, nameof(Question));
    }

    public void HideQuestionPanel()
    {
        SetPanel(Question, false, this, nameof(Question));
    }

    void SetPaused(bool paused)
    {
        if (gameFlow == null)
        {
            gameFlow = GameFlowController.GetOrCreate();
        }

        if (!gameFlow.SetPaused(paused))
        {
            ActivePause = false;
        }
    }

    static void SetPanel(GameObject panel, bool active, PauseMenuController owner, string fieldName)
    {
        if (panel != null)
        {
            panel.SetActive(active);
            return;
        }

        BuildSafeLogger.WarnOnce(
            nameof(PauseMenuController) + ".NullPanel." + fieldName,
            "UI panel is null; cannot set active=" + active + ".",
            owner,
            fieldName);
    }
}
