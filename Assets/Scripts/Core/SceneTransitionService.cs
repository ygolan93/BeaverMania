using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1000)]
public class SceneTransitionService : MonoBehaviour
{
    public static SceneTransitionService Instance { get; private set; }

    internal bool isLoading;
    string loadingSceneName;

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
        isLoading = false;
        loadingSceneName = null;
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

        if (resetSceneServices)
        {
            RuntimeServices.ResetSceneServices();
        }

        SceneManager.LoadScene(sceneName);
    }

    private AsyncOperation LoadSceneAsyncInternal(string sceneName, bool showCursor)
    {
        if (!PrepareLoad(sceneName, showCursor, false))
        {
            return null;
        }

        return SceneManager.LoadSceneAsync(sceneName);
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
                "Blocked duplicate scene load request: " + sceneName + ".",
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

        isLoading = true;
        loadingSceneName = sceneName;
        GameFlowController.GetOrCreate().BeginSceneTransition();

        if (showCursor)
        {
            CursorStateService.GetOrCreate().ShowCursor();
        }

        return true;
    }
}
