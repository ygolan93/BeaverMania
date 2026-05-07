using System.Collections.Generic;
using UnityEngine;

public static class BuildSafeLogger
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private static readonly HashSet<string> warnedKeys = new HashSet<string>();
#endif

    public static void WarnOnce(string key, string message, Object context)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (warnedKeys.Add(string.IsNullOrEmpty(key) ? message : key))
        {
            Debug.LogWarning(message, context);
        }
#endif
    }
}
