using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BuildSafeLogger
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    const string LoggingConfigResourcePath = "RuntimeConfig/LoggingConfig";
    static LoggingConfig loggingConfig;
    private static readonly HashSet<string> infoKeys = new HashSet<string>();
    private static readonly HashSet<string> warnKeys = new HashSet<string>();
    private static readonly HashSet<string> errorKeys = new HashSet<string>();
#endif

    public static void InfoOnce(string key, string message, Object owner = null, string missingField = null, string missingTag = null, string missingMethod = null)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Config.logInfo && infoKeys.Add(ResolveKey(key, message, owner, missingField, missingTag, missingMethod)))
        {
            Debug.Log(Format(message, owner, missingField, missingTag, missingMethod), owner);
        }
#endif
    }

    public static void WarnOnce(string key, string message, Object owner = null, string missingField = null, string missingTag = null, string missingMethod = null)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Config.logWarnings && warnKeys.Add(ResolveKey(key, message, owner, missingField, missingTag, missingMethod)))
        {
            Debug.LogWarning(Format(message, owner, missingField, missingTag, missingMethod), owner);
        }
#endif
    }

    public static void ErrorOnce(string key, string message, Object owner = null, string missingField = null, string missingTag = null, string missingMethod = null)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Config.logErrors && errorKeys.Add(ResolveKey(key, message, owner, missingField, missingTag, missingMethod)))
        {
            Debug.LogError(Format(message, owner, missingField, missingTag, missingMethod), owner);
        }
#endif
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    static LoggingConfig Config => loggingConfig != null ? loggingConfig : (loggingConfig = Resources.Load<LoggingConfig>(LoggingConfigResourcePath)) ?? ScriptableObject.CreateInstance<LoggingConfig>();

    private static string ResolveKey(string key, string message, Object owner, string missingField, string missingTag, string missingMethod)
    {
        if (!string.IsNullOrEmpty(key))
        {
            return key;
        }

        return SceneManager.GetActiveScene().name + "|" + OwnerName(owner) + "|" + message + "|" + missingField + "|" + missingTag + "|" + missingMethod;
    }

    private static string Format(string message, Object owner, string missingField, string missingTag, string missingMethod)
    {
        var scene = SceneManager.GetActiveScene().name;
        var formatted = Config.includeSceneName ? "[" + (string.IsNullOrEmpty(scene) ? "<no scene>" : scene) + "] " + message : message;
        if (Config.includeOwnerPath)
        {
            formatted += " Owner=" + OwnerName(owner);
        }

        if (!string.IsNullOrEmpty(missingField))
        {
            formatted += " MissingField=" + missingField;
        }

        if (!string.IsNullOrEmpty(missingTag))
        {
            formatted += " MissingTag=" + missingTag;
        }

        if (!string.IsNullOrEmpty(missingMethod))
        {
            formatted += " MissingMethod=" + missingMethod;
        }

        return formatted;
    }

    private static string OwnerName(Object owner)
    {
        var component = owner as Component;
        if (component != null)
        {
            return component.GetType().Name + "(" + GameObjectPath(component.transform) + ")";
        }

        return owner != null ? owner.GetType().Name + "(" + owner.name + ")" : "<null>";
    }

    private static string GameObjectPath(Transform transform)
    {
        if (transform == null)
        {
            return "<null>";
        }

        var path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }

        return path;
    }
#endif
}
