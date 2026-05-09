using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1000)]
public class SceneTransitionService : MonoBehaviour
{
    public static SceneTransitionService Instance { get; private set; }

    internal bool isLoading;
    string loadingSceneName;
    CursorLockMode previousCursorLockState;
    bool previousCursorVisible;
    InputMode previousInputMode;
    GameFlowState previousFlowState;

    public static SceneTransitionService GetOrCreate()
    {
        return RuntimeServices.GetOrCreate<SceneTransitionService>(ServiceLifetime.Persistent);
    }

    public static void LoadMenu()
    {
        GetOrCreate().LoadSceneInternal(SceneNames.Menu, true);
    }

    public static void LoadLevel1()
    {
        GetOrCreate().LoadSceneInternal(SceneNames.Level1, false, true);
    }

    public static void ReloadActiveScene()
    {
        var activeScene = SceneManager.GetActiveScene();
        GetOrCreate().LoadSceneInternal(activeScene.name, activeScene.name == SceneNames.Menu, true);
    }

    public static void LoadScene(string sceneName)
    {
        GetOrCreate().LoadSceneInternal(sceneName, sceneName == SceneNames.Menu);
    }

    public static AsyncOperation LoadSceneAsync(string sceneName)
    {
        return GetOrCreate().LoadSceneAsyncInternal(sceneName, sceneName == SceneNames.Menu);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            BuildSafeLogger.WarnOnce(
                nameof(SceneTransitionService) + ".DuplicateManager",
                "Duplicate manager destroyed: " + nameof(SceneTransitionService) + ".",
                this,
                nameof(SceneTransitionService));
            Destroy(gameObject);
            return;
        }

        if (!RuntimeServices.Register(this, ServiceLifetime.Persistent))
        {
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            RuntimeServices.Unregister(this);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!isLoading)
        {
            return;
        }

        CompleteLoad(scene.name, true);
    }

    private void LoadSceneInternal(string sceneName, bool showCursor)
    {
        LoadSceneInternal(sceneName, showCursor, false);
    }

    private void LoadSceneInternal(string sceneName, bool showCursor, bool resetSceneServices)
    {
        if (!PrepareLoad(sceneName, showCursor, resetSceneServices))
        {
            return;
        }

        try
        {
            if (resetSceneServices)
            {
                RuntimeServices.ResetSceneServices();
            }

            SceneManager.LoadScene(sceneName);
        }
        catch (System.Exception exception)
        {
            BuildSafeLogger.ErrorOnce(
                nameof(SceneTransitionService) + ".LoadSceneFailed." + sceneName,
                "Scene load failed: " + sceneName + " (" + exception.GetType().Name + ").",
                this,
                null,
                null,
                nameof(LoadSceneInternal));
            CompleteLoad(sceneName, false);
        }
    }

    private AsyncOperation LoadSceneAsyncInternal(string sceneName, bool showCursor)
    {
        if (!PrepareLoad(sceneName, showCursor, false))
        {
            return null;
        }

        try
        {
            var operation = SceneManager.LoadSceneAsync(sceneName);
            if (operation == null)
            {
                CompleteLoad(sceneName, false);
            }

            return operation;
        }
        catch (System.Exception exception)
        {
            BuildSafeLogger.ErrorOnce(
                nameof(SceneTransitionService) + ".LoadSceneAsyncFailed." + sceneName,
                "Async scene load failed: " + sceneName + " (" + exception.GetType().Name + ").",
                this,
                null,
                null,
                nameof(LoadSceneAsyncInternal));
            CompleteLoad(sceneName, false);
            return null;
        }
    }

    private bool PrepareLoad(string sceneName, bool showCursor, bool forceReload)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            BuildSafeLogger.WarnOnce(
                nameof(SceneTransitionService) + ".BlockedEmptySceneLoad",
                "Blocked empty scene load request.",
                this,
                null,
                null,
                nameof(PrepareLoad));
            return false;
        }

        if (isLoading)
        {
            BuildSafeLogger.WarnOnce(
                nameof(SceneTransitionService) + ".BlockedDuplicateSceneLoad." + loadingSceneName,
                "Blocked duplicate scene load request: " + sceneName + ". Current request: " + loadingSceneName + ".",
                this,
                null,
                null,
                nameof(PrepareLoad));
            return false;
        }

        if (!IsSceneLoadable(sceneName))
        {
            BuildSafeLogger.ErrorOnce(
                nameof(SceneTransitionService) + ".BlockedInvalidSceneLoad." + sceneName,
                "Blocked invalid scene load request: " + sceneName + ".",
                this,
                null,
                null,
                nameof(PrepareLoad));
            return false;
        }

        if (!forceReload && SceneManager.GetActiveScene().name == sceneName)
        {
            BuildSafeLogger.WarnOnce(
                nameof(SceneTransitionService) + ".BlockedAlreadyActiveScene." + sceneName,
                "Blocked scene load because scene is already active: " + sceneName + ".",
                this,
                null,
                null,
                nameof(PrepareLoad));
            return false;
        }

        CaptureRuntimeState();
        isLoading = true;
        loadingSceneName = sceneName;
        GameFlowController.GetOrCreate().BeginSceneTransition();

        if (showCursor)
        {
            CursorStateService.GetOrCreate().ShowCursor();
        }

        BuildSafeLogger.InfoOnce(
            nameof(SceneTransitionService) + ".BeginLoad." + sceneName,
            "Beginning scene transition: " + sceneName + ".",
            this,
            null,
            null,
            nameof(PrepareLoad));
        return true;
    }

    void CaptureRuntimeState()
    {
        previousCursorLockState = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        var inputReader = GameInputReader.GetOrCreate();
        previousInputMode = inputReader.Mode;
        previousFlowState = GameFlowController.GetOrCreate().State;
    }

    void CompleteLoad(string sceneName, bool success)
    {
        isLoading = false;
        loadingSceneName = null;
        Time.timeScale = 1f;

        if (success)
        {
            var isMenu = sceneName == SceneNames.Menu;
            if (isMenu)
            {
                CursorStateService.GetOrCreate().ShowCursor();
                GameInputReader.GetOrCreate().EnableUiInput();
            }
            else
            {
                CursorStateService.GetOrCreate().HideCursor();
                GameFlowController.GetOrCreate().SetPlaying();
            }
        }
        else
        {
            Cursor.lockState = previousCursorLockState;
            Cursor.visible = previousCursorVisible;
            RestoreInputMode(previousInputMode);
        }

        BuildSafeLogger.InfoOnce(
            nameof(SceneTransitionService) + ".CompleteLoad." + sceneName + "." + success,
            "Completed scene transition: " + sceneName + " success=" + success + ".",
            this,
            null,
            null,
            nameof(CompleteLoad));
    }

    void RestoreInputMode(InputMode mode)
    {
        var flow = GameFlowController.GetOrCreate();
        if (previousFlowState == GameFlowState.Paused)
        {
            flow.SetPaused(true);
            return;
        }

        if (previousFlowState == GameFlowState.GameOver)
        {
            flow.SetGameOver();
            return;
        }

        var inputReader = GameInputReader.GetOrCreate();
        switch (mode)
        {
            case InputMode.Ui:
                inputReader.EnableUiInput();
                break;
            case InputMode.Disabled:
                inputReader.DisableGameplayInput();
                break;
            default:
                flow.SetPlaying();
                break;
        }
    }

    bool IsSceneLoadable(string sceneName)
    {
        if (SceneManager.GetSceneByName(sceneName).IsValid())
        {
            return true;
        }

        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (name == sceneName)
            {
                return true;
            }
        }

        return false;
    }
}
