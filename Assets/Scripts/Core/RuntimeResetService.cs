using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class RuntimeResetService : MonoBehaviour
{
    static RuntimeResetService instance;
    readonly List<IRuntimeResettable> resettables = new List<IRuntimeResettable>();
    Scene cachedScene;

    public static RuntimeResetService Instance => instance;

    void Awake()
    {
        instance = this;
        CacheSceneResettables();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (instance == this)
        {
            instance = null;
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CacheSceneResettables();
    }

    public void ResetAll()
    {
        if (cachedScene != gameObject.scene)
        {
            CacheSceneResettables();
        }

        for (int i = 0; i < resettables.Count; i++)
        {
            var resettable = resettables[i];
            if (resettable is Object unityObject && unityObject == null)
            {
                continue;
            }

            resettable?.RuntimeReset();
        }
    }

    void CacheSceneResettables()
    {
        cachedScene = gameObject.scene;
        resettables.Clear();

        var behaviours = FindObjectsOfType<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            var behaviour = behaviours[i];
            if (behaviour == null || behaviour == this || behaviour.gameObject.scene != cachedScene)
            {
                continue;
            }

            if (behaviour is IRuntimeResettable resettable)
            {
                resettables.Add(resettable);
            }
        }
    }
}
