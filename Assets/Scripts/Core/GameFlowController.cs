using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class GameFlowController : MonoBehaviour
{
    public static GameFlowController Instance { get; private set; }

    public GameFlowState State { get; private set; } = GameFlowState.Boot;

    public static GameFlowController GetOrCreate()
    {
        return RuntimeServices.GetRequired<GameFlowController>(ServiceLifetime.Persistent);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            BuildSafeLogger.WarnOnce(
                nameof(GameFlowController) + ".DuplicateManager",
                "Duplicate manager destroyed: " + nameof(GameFlowController) + ".",
                this,
                nameof(GameFlowController));
            Destroy(gameObject);
            return;
        }

        if (!RuntimeServices.Register(this, ServiceLifetime.Persistent))
        {
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            RuntimeServices.Unregister(this);
        }
    }

    public void SetPlaying()
    {
        State = GameFlowState.Playing;
        Time.timeScale = 1f;
        SetGameplayInput();
    }

    public bool SetPaused(bool paused)
    {
        if (State == GameFlowState.GameOver)
        {
            BuildSafeLogger.WarnOnce(
                nameof(GameFlowController) + ".BlockedPauseDuringGameOver",
                "Blocked pause state change during game over.",
                this,
                null,
                null,
                nameof(SetPaused));
            return false;
        }

        State = paused ? GameFlowState.Paused : GameFlowState.Playing;
        Time.timeScale = paused ? 0f : 1f;
        if (paused)
        {
            SetUiInput();
            return true;
        }

        SetGameplayInput();
        return true;
    }

    public bool SetGameOver()
    {
        if (State == GameFlowState.GameOver)
        {
            BuildSafeLogger.WarnOnce(
                nameof(GameFlowController) + ".BlockedDuplicateGameOver",
                "Blocked duplicate game-over request.",
                this,
                null,
                null,
                nameof(SetGameOver));
            return false;
        }

        State = GameFlowState.GameOver;
        Time.timeScale = 0f;
        SetInputDisabled();
        return true;
    }

    public void SetShop()
    {
        State = GameFlowState.Shop;
        Time.timeScale = 1f;
        SetUiInput();
    }

    public void BeginSceneTransition()
    {
        State = GameFlowState.Transitioning;
        Time.timeScale = 1f;
        SetInputDisabled();
    }

    void SetGameplayInput()
    {
        GameInputReader.GetOrCreate().EnableGameplayInput();
    }

    void SetUiInput()
    {
        GameInputReader.GetOrCreate().EnableUiInput();
    }

    void SetInputDisabled()
    {
        GameInputReader.GetOrCreate().DisableGameplayInput();
    }
}
