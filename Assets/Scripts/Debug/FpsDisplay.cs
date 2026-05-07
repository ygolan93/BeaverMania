using UnityEngine;

public sealed class FpsDisplay : MonoBehaviour
{
    [SerializeField] DebugQAConfig config;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    float smoothedDeltaTime;
    GUIStyle style;

    public void Configure(DebugQAConfig debugConfig) => config = debugConfig;

    void Update()
    {
        smoothedDeltaTime += (Time.unscaledDeltaTime - smoothedDeltaTime) * 0.1f;
    }

    void OnGUI()
    {
        if (config == null || !config.enableFpsDisplay)
        {
            return;
        }

        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Max(12, config.fpsFontSize)
            };
            style.normal.textColor = Color.white;
        }

        float fps = smoothedDeltaTime > 0f ? 1f / smoothedDeltaTime : 0f;
        GUI.Label(new Rect(10f, 10f, 220f, 32f), $"FPS: {fps:0}", style);
    }
#else
    public void Configure(DebugQAConfig debugConfig) => config = debugConfig;
#endif
}
