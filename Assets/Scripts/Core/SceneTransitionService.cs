using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1000)]
public class SceneTransitionService : MonoBehaviour
{
    public static SceneTransitionService Instance { get; private set; }

    internal bool isLoading;

    public static SceneTransitionService GetOrCreate()
    {
        if (Instance != null)
        {
            return Instance;
        }

        Instance = FindObjectOfType<SceneTransitionService>();
        if (Instance != null)
        {
            return Instance;
        }

        var gameObject = new GameObject(nameof(SceneTransitionService));
        return gameObject.AddComponent<SceneTransitionService>();
    }

    public static void LoadMenu()
    {
        GetOrCreate().LoadSceneInternal(SceneNames.Menu, true);
    }

    public static void LoadLevel1()
    {
        GetOrCreate().LoadSceneInternal(SceneNames.Level1, false);
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
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
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
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        isLoading = false;
    }

    private void LoadSceneInternal(string sceneName, bool showCursor)
    {
        if (!PrepareLoad(showCursor))
        {
            return;
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
            return;
        }

        var player = playerObject.GetComponent<Behaviour>();
        if (player != null)
        {
            player.enabled = false;
        }
    }
}
