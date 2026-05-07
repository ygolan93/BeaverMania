using UnityEngine;

[CreateAssetMenu(fileName = "LoggingConfig", menuName = "BeaverMania/Config/Logging Config")]
public class LoggingConfig : ScriptableObject
{
    public bool logInfo = true;
    public bool logWarnings = true;
    public bool logErrors = true;
    public bool includeSceneName = true;
    public bool includeOwnerPath = true;
}
