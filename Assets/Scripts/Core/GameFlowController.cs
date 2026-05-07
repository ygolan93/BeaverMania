using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class GameFlowController : MonoBehaviour
{
    public static GameFlowController Instance { get; private set; }

    public GameFlowState State { get; private set; } = GameFlowState.Boot;

    public static GameFlowController GetOrCreate()
    {
        return RuntimeServices.GetOrCreate<GameFlowController>(ServiceLifetime.Scene);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (!RuntimeServices.Register(this, ServiceLifetime.Scene))
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
    }

    public void SetPaused(bool paused)
    {
        State = paused ? GameFlowState.Paused : GameFlowState.Playing;
        Time.timeScale = paused ? 0f : 1f;
    }

    public void SetGameOver()
    {
        State = GameFlowState.GameOver;
        Time.timeScale = 0f;
    }

    public void BeginSceneTransition()
    {
        State = GameFlowState.Transitioning;
        Time.timeScale = 1f;
    }
}
