using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PauseMenuController : MonoBehaviour
{
    public GameObject PauseMenu;
    public GameObject Question;
    public Behaviour Player;
    public bool ActivePause = false;

    GameFlowController gameFlow;
    GameInputReader inputReader;
    bool keepCursorVisibleAfterResume;
    [SerializeField] Slider volumeSlider;
    [SerializeField] AudioSource Music;
    [SerializeField] SettingsController settingsController;
    [SerializeField] GameObject firstPauseSelection;
    [SerializeField] GameObject firstQuestionSelection;

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
        gameFlow.TrySetPlayingFromSceneStartup(nameof(PauseMenuController));
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

        CursorStateService.GetOrCreate().HideCursor();
        HidePausePanel();
        HideQuestionPanel();
    }

    protected virtual void Update()
    {
    }

    public void Pause()
    {
        ApplyPauseVisualState(ActivePause);
    }

    public void ChangeBolean()
    {
        var currentFlow = GetGameFlow();
        if (IsPauseToggleBlocked(currentFlow))
        {
            return;
        }

        var pauseRequested = currentFlow != null
            ? currentFlow.State != GameFlowState.Paused
            : !ActivePause;
        var keepCursorVisible = !pauseRequested &&
            (keepCursorVisibleAfterResume || IsPlayerInTraderOrDialogueUi());

        if (pauseRequested)
        {
            keepCursorVisibleAfterResume = IsPlayerInTraderOrDialogueUi();
        }

        if (!SetPaused(pauseRequested))
        {
            return;
        }

        ActivePause = pauseRequested;
        ApplyCursorState(pauseRequested, keepCursorVisible);
        ApplyPauseVisualState(pauseRequested);

        if (!pauseRequested)
        {
            keepCursorVisibleAfterResume = false;
        }
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
        if (volumeSlider == null)
        {
            return;
        }

        var adapter = GetSettingsController();
        if (adapter != null)
        {
            adapter.SetMusicVolume(volumeSlider.value);
            return;
        }

        if (Player != null && Player.seekMusic && Music != null)
        {
            Music.volume = volumeSlider.value;
        }
    }

    public void ShowPausePanel()
    {
        SetPanel(PauseMenu, true, firstPauseSelection, this, nameof(PauseMenu));
    }

    public void HidePausePanel()
    {
        SetPanel(PauseMenu, false, firstPauseSelection, this, nameof(PauseMenu));
    }

    public void ShowQuestionPanel()
    {
        SetPanel(Question, true, firstQuestionSelection, this, nameof(Question));
    }

    public void HideQuestionPanel()
    {
        SetPanel(Question, false, firstQuestionSelection, this, nameof(Question));
    }

    internal void ApplyPauseVisualState(bool paused)
    {
        if (paused)
        {
            ShowPausePanel();
            return;
        }

        HidePausePanel();
        HideQuestionPanel();
    }

    bool SetPaused(bool paused)
    {
        gameFlow = GetGameFlow();

        if (gameFlow == null || !gameFlow.SetPaused(paused))
        {
            ActivePause = false;
            return false;
        }

        return true;
    }

    void ApplyCursorState(bool paused, bool keepCursorVisible)
    {
        var cursorState = CursorStateService.GetOrCreate();
        if (paused)
        {
            cursorState.ShowCursor();
            return;
        }

        if (!keepCursorVisible)
        {
            cursorState.HideCursor();
        }
    }

    bool IsPlayerInTraderOrDialogueUi()
    {
        if (Player != null && Player.isAtTrader)
        {
            return true;
        }

        var currentFlow = gameFlow != null ? gameFlow : GameFlowController.Instance;
        return currentFlow != null &&
            (currentFlow.State == GameFlowState.Shop || currentFlow.State == GameFlowState.Dialogue);
    }

    SettingsController GetSettingsController()
    {
        if (settingsController == null)
        {
            settingsController = GetComponentInChildren<SettingsController>(true);
        }

        return settingsController;
    }

    GameFlowController GetGameFlow()
    {
        if (gameFlow == null)
        {
            gameFlow = GameFlowController.Instance != null
                ? GameFlowController.Instance
                : GameFlowController.GetOrCreate();
        }

        return gameFlow;
    }

    static bool IsPauseToggleBlocked(GameFlowController currentFlow)
    {
        return currentFlow != null &&
            (currentFlow.State == GameFlowState.GameOver ||
             currentFlow.State == GameFlowState.Transitioning);
    }

    static void SetPanel(GameObject panel, bool active, GameObject firstSelection, PauseMenuController owner, string fieldName)
    {
        if (panel != null)
        {
            if (!active)
            {
                ClearSelectionIfPanelOwnsIt(panel, owner, fieldName);
            }

            panel.SetActive(active);

            if (active)
            {
                SelectPanelObject(firstSelection, owner, fieldName);
            }

            return;
        }

        BuildSafeLogger.WarnOnce(
            nameof(PauseMenuController) + ".NullPanel." + fieldName,
            "UI panel is null; cannot set active=" + active + ".",
            owner,
            fieldName);
    }

    static void SelectPanelObject(GameObject selection, PauseMenuController owner, string fieldName)
    {
        var eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            WarnMissingEventSystem(owner, fieldName);
            return;
        }

        eventSystem.SetSelectedGameObject(selection);
    }

    static void ClearSelectionIfPanelOwnsIt(GameObject panel, PauseMenuController owner, string fieldName)
    {
        var eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            WarnMissingEventSystem(owner, fieldName);
            return;
        }

        var selected = eventSystem.currentSelectedGameObject;
        if (selected != null && (selected == panel || selected.transform.IsChildOf(panel.transform)))
        {
            eventSystem.SetSelectedGameObject(null);
        }
    }

    static void WarnMissingEventSystem(PauseMenuController owner, string fieldName)
    {
        BuildSafeLogger.WarnOnce(
            nameof(PauseMenuController) + ".MissingEventSystem." + fieldName,
            "EventSystem is missing; UI selection update skipped.",
            owner);
    }
}
