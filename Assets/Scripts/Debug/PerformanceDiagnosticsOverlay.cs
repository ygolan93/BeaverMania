#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class PerformanceDiagnosticsOverlay : MonoBehaviour
{
    [SerializeField] bool overlayEnabled;
    [SerializeField] KeyCode toggleKey = KeyCode.F8;
    [SerializeField] float sampleIntervalSeconds = 0.5f;

    readonly List<string> warnings = new List<string>();
    float smoothedDeltaTime;
    float nextSampleTime;
    int activeObjectCount;
    int particleSystemCount;
    int audioSourceCount;
    GUIStyle labelStyle;
    GUIStyle warningStyle;

    public bool OverlayEnabled
    {
        get { return overlayEnabled; }
        set { overlayEnabled = value; }
    }

    void Update()
    {
        if (toggleKey != KeyCode.None && Input.GetKeyDown(toggleKey))
        {
            overlayEnabled = !overlayEnabled;
        }

        if (!overlayEnabled)
        {
            return;
        }

        smoothedDeltaTime += (Time.unscaledDeltaTime - smoothedDeltaTime) * 0.1f;
        if (Time.unscaledTime >= nextSampleTime)
        {
            SampleSceneDiagnostics();
            nextSampleTime = Time.unscaledTime + Mathf.Max(0.1f, sampleIntervalSeconds);
        }
    }

    void OnGUI()
    {
        if (!overlayEnabled)
        {
            return;
        }

        EnsureStyles();

        var ms = smoothedDeltaTime * 1000f;
        var fps = smoothedDeltaTime > 0f ? 1f / smoothedDeltaTime : 0f;
        var y = 10f;

        GUI.Label(new Rect(10f, y, 360f, 24f), string.Format("FPS: {0:0} ({1:0.0} ms)", fps, ms), labelStyle);
        y += 20f;
        GUI.Label(new Rect(10f, y, 360f, 24f), "Active GameObjects: " + activeObjectCount, labelStyle);
        y += 20f;
        GUI.Label(new Rect(10f, y, 360f, 24f), "ParticleSystems: " + particleSystemCount, labelStyle);
        y += 20f;
        GUI.Label(new Rect(10f, y, 360f, 24f), "AudioSources: " + audioSourceCount, labelStyle);
        y += 24f;

        foreach (var warning in warnings)
        {
            GUI.Label(new Rect(10f, y, 900f, 24f), warning, warningStyle);
            y += 20f;
        }
    }

    void SampleSceneDiagnostics()
    {
        var gameObjects = Resources.FindObjectsOfTypeAll<GameObject>()
            .Where(IsRuntimeSceneObject)
            .ToArray();

        activeObjectCount = gameObjects.Count(gameObject => gameObject.activeInHierarchy);
        particleSystemCount = CountRuntimeComponents<ParticleSystem>();
        audioSourceCount = CountRuntimeComponents<AudioSource>();

        warnings.Clear();
        AddDuplicateMusicWarning();
        AddDuplicateRuntimeServiceWarnings();
    }

    static int CountRuntimeComponents<T>() where T : Component
    {
        return Resources.FindObjectsOfTypeAll<T>()
            .Count(component => component != null && IsRuntimeSceneObject(component.gameObject));
    }

    void AddDuplicateMusicWarning()
    {
        var musicObjects = Resources.FindObjectsOfTypeAll<MusicPlaylist>()
            .Where(component => component != null && IsRuntimeSceneObject(component.gameObject))
            .Select(component => ScenePath(component.transform))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        if (musicObjects.Length > 1)
        {
            warnings.Add("WARN duplicate MusicPlaylist objects: " + string.Join(", ", musicObjects));
        }
    }

    void AddDuplicateRuntimeServiceWarnings()
    {
        var serviceComponents = Resources.FindObjectsOfTypeAll<MonoBehaviour>()
            .Where(component => component != null && IsRuntimeSceneObject(component.gameObject))
            .Where(component => IsDeclaredRuntimeService(component))
            .GroupBy(component => component.GetType())
            .OrderBy(group => group.Key.FullName, StringComparer.Ordinal);

        foreach (var group in serviceComponents)
        {
            var paths = group.Select(component => ScenePath(component.transform))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            if (paths.Length > 1)
            {
                warnings.Add("WARN duplicate runtime service " + group.Key.Name + ": " + string.Join(", ", paths));
            }
        }
    }

    static bool IsDeclaredRuntimeService(MonoBehaviour component)
    {
        ServiceLifetime lifetime;
        return RuntimeServices.TryGetDeclaredLifetime(component.GetType(), out lifetime);
    }

    static bool IsRuntimeSceneObject(GameObject gameObject)
    {
        return gameObject != null && gameObject.scene.IsValid() && gameObject.scene.isLoaded;
    }

    static string ScenePath(Transform transform)
    {
        var names = new Stack<string>();
        while (transform != null)
        {
            names.Push(transform.name);
            transform = transform.parent;
        }

        return string.Join("/", names.ToArray());
    }

    void EnsureStyles()
    {
        if (labelStyle != null)
        {
            return;
        }

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
        };
        labelStyle.normal.textColor = Color.white;
        warningStyle = new GUIStyle(labelStyle);
        warningStyle.normal.textColor = Color.yellow;
    }
}
#endif
