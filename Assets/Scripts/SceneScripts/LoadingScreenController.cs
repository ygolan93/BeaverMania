using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LoadingScreenController : MonoBehaviour
{
    public GameObject loadingScreen; // Reference to your loading screen canvas
    public Slider loadingBar; // Reference to a loading progress bar (if applicable)
    public float fillSpeed = 0.5f; // Speed at which the loading bar fills (adjust as needed)

    void Start()
    {
        // Disable the loading screen canvas on Start
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(false);
        }
    }

    public void LoadScene(string sceneName)
    {
        // Activate the loading screen canvas
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(true);
        }

        // Start loading the scene in the background
        StartCoroutine(LoadSceneAsync(sceneName));
    }

    System.Collections.IEnumerator LoadSceneAsync(string sceneName)
    {
        // Create an operation to load the scene asynchronously
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);

        // Reset the fill amount to zero at the start of loading
        if (loadingBar != null)
        {
            loadingBar.value = 0f;
        }

        // While the scene is loading
        while (!operation.isDone)
        {
            // Interpolate loading progress over time for a smoother fill effect
            float progress = Mathf.Clamp01(operation.progress / 0.9f); // 0.9 is the completion value
            float elapsedTime = 0f;

            while (elapsedTime < fillSpeed)
            {
                if (loadingBar != null)
                {
                    loadingBar.value = Mathf.Lerp(loadingBar.value, progress, (elapsedTime / fillSpeed));
                }

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            if (loadingBar != null)
            {
                loadingBar.value = progress; // Ensure the final progress value is set accurately
            }

            yield return null; // Wait for the next frame
        }
    }
}
