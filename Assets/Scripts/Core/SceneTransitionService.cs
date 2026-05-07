using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1000)]
public class SceneTransitionService : MonoBehaviour
{
    public static SceneTransitionService Instance { get; private set; }

    internal bool isLoading;

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
        GetOrCreate().LoadSceneInternal(activeScene.name, activeScene.name == SceneNames.Menu);
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
    }

    private void LoadSceneInternal(string sceneName, bool showCursor)
    {
        LoadSceneInternal(sceneName, showCursor, false);
    }

    private void LoadSceneInternal(string sceneName, bool showCursor, bool resetSceneServices)
    {
        if (!PrepareLoad(showCursor))
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
        if (!PrepareLoad(showCursor))
        {
            return null;
        }

        return SceneManager.LoadSceneAsync(sceneName);
    }

    private bool PrepareLoad(bool showCursor)
    {
        if (isLoading)
        {
            BuildSafeLogger.WarnOnce(
                nameof(SceneTransitionService) + ".BlockedDuplicateSceneLoad",
                "Blocked duplicate scene load request.",
                this,
                null,
                null,
                nameof(PrepareLoad));
            return false;
        }

        isLoading = true;
        GameFlowController.GetOrCreate().BeginSceneTransition();
        Time.timeScale = 1f;

        if (showCursor)
        {
            CursorStateService.GetOrCreate().ShowCursor();
        }

        DisablePlayerInput();
        return true;
    }

    private void DisablePlayerInput()
    {
        var playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null)
        {
            BuildSafeLogger.WarnOnce(
                nameof(SceneTransitionService) + ".MissingPlayer",
                "Cannot disable player input because Player tag was not found.",
                this,
                null,
                "Player",
                nameof(DisablePlayerInput));
            return;
        }

        var player = playerObject.GetComponent<Behaviour>();
        if (player != null)
        {
            player.enabled = false;
        }
    }
}
