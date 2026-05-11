using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class PrefabPerformanceValidator
{
    const string MenuPath = "Tools/Beavermania/Validate Allowed Prefabs";

    static readonly string[] AllowedPrefabPaths =
    {
        "Assets/Prefabs/OtterPlayer/Otter_Shapekeys/Player.prefab",
        "Assets/Prefabs/OtterPlayer/Player Updated.prefab",
        "Assets/Prefabs/Wasp/LVL1 Wasp.prefab",
        "Assets/Prefabs/Scorpion/ScorpionBoss.prefab",
        "Assets/Prefabs/Scorpion/Scorpion.prefab",
        "Assets/Prefabs/Hive/NewHive.prefab",
        "Assets/Prefabs/Objects/UI/PlayerCanvas.prefab",
        "Assets/Prefabs/Objects/UI/GameMusic.prefab"
    };

    static readonly string[] EnemyOrPlayerPrefabPaths =
    {
        "Assets/Prefabs/OtterPlayer/Otter_Shapekeys/Player.prefab",
        "Assets/Prefabs/OtterPlayer/Player Updated.prefab",
        "Assets/Prefabs/Wasp/LVL1 Wasp.prefab",
        "Assets/Prefabs/Scorpion/ScorpionBoss.prefab",
        "Assets/Prefabs/Scorpion/Scorpion.prefab"
    };

    static readonly string[] CriticalMonoBehaviourTypeNames =
    {
        "Behaviour",
        "NPC_Basic",
        "ScorpionScript",
        "Static_Hive",
        "HudPresenter",
        "DebugReference",
        "MusicPlaylist"
    };

    static readonly Type[] CriticalUnityComponentTypes =
    {
        typeof(AudioSource),
        typeof(Animator)
    };

    [MenuItem(MenuPath)]
    public static void ValidateAllowedPrefabs()
    {
        var report = new ValidationReport();

        foreach (var prefabPath in AllowedPrefabPaths)
        {
            ValidatePrefab(prefabPath, report.ForPrefab(prefabPath));
        }

        report.Log();
    }

    static void ValidatePrefab(string prefabPath, PrefabReport report)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            report.Errors.Add("Prefab asset not found.");
            return;
        }

        ValidateMissingScripts(prefab, report);
        ValidateDuplicateCriticalComponents(prefab, report);

        if (EnemyOrPlayerPrefabPaths.Contains(prefabPath))
        {
            ValidateAlwaysActiveEffects(prefab, report);
        }
    }

    static void ValidateMissingScripts(GameObject prefab, PrefabReport report)
    {
        foreach (var transform in OrderedTransforms(prefab))
        {
            var components = transform.GetComponents<Component>();
            for (var i = 0; i < components.Length; i++)
            {
                if (components[i] == null)
                {
                    report.Errors.Add("Missing script at " + TransformPath(transform) + " componentIndex=" + i);
                }
            }
        }
    }

    static void ValidateDuplicateCriticalComponents(GameObject prefab, PrefabReport report)
    {
        foreach (var typeName in CriticalMonoBehaviourTypeNames)
        {
            var matches = prefab.GetComponentsInChildren<MonoBehaviour>(true)
                .Where(component => component != null && component.GetType().Name == typeName)
                .Select(component => TransformPath(component.transform))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            AddDuplicateComponentWarning(typeName, matches, report);
        }

        foreach (var type in CriticalUnityComponentTypes)
        {
            var matches = prefab.GetComponentsInChildren(type, true)
                .Cast<Component>()
                .Where(component => component != null && component.GetType() == type)
                .Select(component => TransformPath(component.transform))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            AddDuplicateComponentWarning(type.Name, matches, report);
        }
    }

    static void ValidateAlwaysActiveEffects(GameObject prefab, PrefabReport report)
    {
        foreach (var particleSystem in prefab.GetComponentsInChildren<ParticleSystem>(true)
            .Where(component => component != null && IsActiveInPrefabHierarchy(component.transform) && component.main.playOnAwake)
            .OrderBy(component => TransformPath(component.transform), StringComparer.Ordinal))
        {
            report.Warnings.Add("Always-active ParticleSystem for manual review at " + TransformPath(particleSystem.transform));
        }

        foreach (var light in prefab.GetComponentsInChildren<Light>(true)
            .Where(component => component != null && component.enabled && IsActiveInPrefabHierarchy(component.transform))
            .OrderBy(component => TransformPath(component.transform), StringComparer.Ordinal))
        {
            report.Warnings.Add("Always-active Light for manual review at " + TransformPath(light.transform));
        }
    }

    static bool IsActiveInPrefabHierarchy(Transform transform)
    {
        while (transform != null)
        {
            if (!transform.gameObject.activeSelf)
            {
                return false;
            }

            transform = transform.parent;
        }

        return true;
    }

    static void AddDuplicateComponentWarning(string componentName, string[] matches, PrefabReport report)
    {
        if (matches.Length <= 1)
        {
            return;
        }

        report.Errors.Add("Duplicate critical component " + componentName + " count=" + matches.Length + " at " + string.Join(", ", matches));
    }

    static IEnumerable<Transform> OrderedTransforms(GameObject prefab)
    {
        return prefab.GetComponentsInChildren<Transform>(true).OrderBy(TransformPath, StringComparer.Ordinal);
    }

    static string TransformPath(Transform transform)
    {
        var names = new Stack<string>();
        while (transform != null)
        {
            names.Push(transform.name);
            transform = transform.parent;
        }

        return string.Join("/", names.ToArray());
    }

    sealed class ValidationReport
    {
        readonly List<PrefabReport> prefabReports = new List<PrefabReport>();

        public PrefabReport ForPrefab(string prefabPath)
        {
            var report = new PrefabReport(prefabPath);
            prefabReports.Add(report);
            return report;
        }

        public void Log()
        {
            var errorCount = prefabReports.Sum(report => report.Errors.Count);
            var warningCount = prefabReports.Sum(report => report.Warnings.Count);
            var builder = new StringBuilder();

            builder.AppendLine("[BeaverMania Allowed Prefab Validation]");
            builder.AppendLine("Prefabs scanned: " + prefabReports.Count);
            builder.AppendLine("Errors: " + errorCount);
            builder.AppendLine("Warnings: " + warningCount);

            foreach (var prefabReport in prefabReports.OrderBy(report => report.PrefabPath, StringComparer.Ordinal))
            {
                builder.AppendLine();
                builder.AppendLine(prefabReport.PrefabPath);
                AppendMessages(builder, "ERROR", prefabReport.Errors);
                AppendMessages(builder, "WARN", prefabReport.Warnings);

                if (prefabReport.Errors.Count == 0 && prefabReport.Warnings.Count == 0)
                {
                    builder.AppendLine("  OK");
                }
            }

            if (errorCount > 0)
            {
                Debug.LogError(builder.ToString());
            }
            else if (warningCount > 0)
            {
                Debug.LogWarning(builder.ToString());
            }
            else
            {
                Debug.Log(builder.ToString());
            }
        }

        static void AppendMessages(StringBuilder builder, string prefix, List<string> messages)
        {
            foreach (var message in messages.OrderBy(message => message, StringComparer.Ordinal))
            {
                builder.AppendLine("  " + prefix + " | " + message);
            }
        }
    }

    sealed class PrefabReport
    {
        public readonly string PrefabPath;
        public readonly List<string> Errors = new List<string>();
        public readonly List<string> Warnings = new List<string>();

        public PrefabReport(string prefabPath)
        {
            PrefabPath = prefabPath;
        }
    }
}
