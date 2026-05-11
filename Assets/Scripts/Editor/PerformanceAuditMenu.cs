using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class PerformanceAuditMenu
{
    const string MenuPath = "Tools/Beavermania/Run Performance Audit";

    static readonly Regex TickMethodRegex = new Regex(@"\b(?:void|IEnumerator)\s+(Update|FixedUpdate|LateUpdate)\s*\(", RegexOptions.Compiled);

    static readonly string[] FlaggedCalls =
    {
        "Camera.main",
        "FindGameObjectWithTag",
        "Instantiate",
        "Destroy",
        "Debug.Log"
    };

    [MenuItem(MenuPath)]
    public static void RunPerformanceAudit()
    {
        var scripts = AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets/Scripts" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        var report = new StringBuilder();
        report.AppendLine("[BeaverMania Performance Audit]");
        report.AppendLine("Scripts scanned: " + scripts.Length);
        AppendTickMethods(report, scripts);
        AppendFlaggedCalls(report, scripts);
        AppendPrefabValidation(report);

        Debug.Log(report.ToString());
        PrefabPerformanceValidator.ValidateAllowedPrefabs();
    }

    static void AppendTickMethods(StringBuilder report, string[] scripts)
    {
        report.AppendLine();
        report.AppendLine("Tick methods:");

        var rows = new List<string>();
        foreach (var script in scripts)
        {
            var source = AssetDatabase.LoadAssetAtPath<MonoScript>(script).text;
            var methods = TickMethodRegex.Matches(source)
                .Cast<Match>()
                .Select(match => match.Groups[1].Value)
                .Distinct()
                .OrderBy(method => method, StringComparer.Ordinal)
                .ToArray();

            if (methods.Length > 0)
            {
                rows.Add("- " + script + ": " + string.Join(", ", methods));
            }
        }

        AppendRowsOrNone(report, rows);
    }

    static void AppendFlaggedCalls(StringBuilder report, string[] scripts)
    {
        report.AppendLine();
        report.AppendLine("Flagged calls:");

        var rows = new List<string>();
        foreach (var script in scripts)
        {
            var source = AssetDatabase.LoadAssetAtPath<MonoScript>(script).text;
            foreach (var flaggedCall in FlaggedCalls)
            {
                if (source.Contains(flaggedCall))
                {
                    rows.Add("- " + script + ": " + flaggedCall);
                }
            }
        }

        AppendRowsOrNone(report, rows);
    }

    static void AppendPrefabValidation(StringBuilder report)
    {
        report.AppendLine();
        report.AppendLine("Allowed prefab validation:");
        report.AppendLine("- Invoked after this audit; see the [BeaverMania Allowed Prefab Validation] console report.");
    }

    static void AppendRowsOrNone(StringBuilder report, List<string> rows)
    {
        if (rows.Count == 0)
        {
            report.AppendLine("- none");
            return;
        }

        foreach (var row in rows.OrderBy(row => row, StringComparer.Ordinal))
        {
            report.AppendLine(row);
        }
    }
}
