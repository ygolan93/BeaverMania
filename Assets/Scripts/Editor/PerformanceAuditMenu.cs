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

    static readonly KeyValuePair<string, Regex>[] FlaggedCalls =
    {
        new KeyValuePair<string, Regex>("Camera.main", new Regex(@"\bCamera\s*\.\s*main\b", RegexOptions.Compiled)),
        new KeyValuePair<string, Regex>("FindGameObjectWithTag", new Regex(@"\bFindGameObjectWithTag\s*\(", RegexOptions.Compiled)),
        new KeyValuePair<string, Regex>("Instantiate", new Regex(@"\bInstantiate\s*\(", RegexOptions.Compiled)),
        new KeyValuePair<string, Regex>("Destroy", new Regex(@"\bDestroy\s*\(", RegexOptions.Compiled)),
        new KeyValuePair<string, Regex>("Debug.Log", new Regex(@"\bDebug\s*\.\s*Log\s*\(", RegexOptions.Compiled))
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
        report.AppendLine("Flagged calls (runtime scripts only):");

        var rows = new List<string>();
        foreach (var script in scripts.Where(IsRuntimeScript))
        {
            var source = StripCommentsAndStrings(AssetDatabase.LoadAssetAtPath<MonoScript>(script).text);
            foreach (var flaggedCall in FlaggedCalls)
            {
                if (flaggedCall.Value.IsMatch(source))
                {
                    rows.Add("- " + script + ": " + flaggedCall.Key);
                }
            }
        }

        AppendRowsOrNone(report, rows);
    }

    static bool IsRuntimeScript(string path)
    {
        return path.IndexOf("/Editor/", StringComparison.OrdinalIgnoreCase) < 0;
    }

    static string StripCommentsAndStrings(string source)
    {
        var chars = source.ToCharArray();
        var inLineComment = false;
        var inBlockComment = false;
        var inString = false;
        var inVerbatimString = false;
        var inCharacter = false;

        for (var i = 0; i < chars.Length; i++)
        {
            var current = chars[i];
            var next = i + 1 < chars.Length ? chars[i + 1] : '\0';

            if (inLineComment)
            {
                if (current == '\r' || current == '\n')
                {
                    inLineComment = false;
                }
                else
                {
                    chars[i] = ' ';
                }

                continue;
            }

            if (inBlockComment)
            {
                if (current == '*' && next == '/')
                {
                    chars[i] = ' ';
                    chars[i + 1] = ' ';
                    i++;
                    inBlockComment = false;
                }
                else if (current != '\r' && current != '\n')
                {
                    chars[i] = ' ';
                }

                continue;
            }

            if (inString)
            {
                if (current == '\r' || current == '\n')
                {
                    inString = false;
                    continue;
                }

                chars[i] = ' ';
                if (current == '"')
                {
                    if (inVerbatimString && next == '"')
                    {
                        chars[i + 1] = ' ';
                        i++;
                    }
                    else
                    {
                        inString = false;
                        inVerbatimString = false;
                    }
                }
                else if (!inVerbatimString && current == '\\' && next != '\0')
                {
                    chars[i + 1] = ' ';
                    i++;
                }

                continue;
            }

            if (inCharacter)
            {
                if (current == '\r' || current == '\n')
                {
                    inCharacter = false;
                    continue;
                }

                chars[i] = ' ';
                if (current == '\'')
                {
                    inCharacter = false;
                }
                else if (current == '\\' && next != '\0')
                {
                    chars[i + 1] = ' ';
                    i++;
                }

                continue;
            }

            if (current == '/' && next == '/')
            {
                chars[i] = ' ';
                chars[i + 1] = ' ';
                i++;
                inLineComment = true;
            }
            else if (current == '/' && next == '*')
            {
                chars[i] = ' ';
                chars[i + 1] = ' ';
                i++;
                inBlockComment = true;
            }
            else if (current == '"')
            {
                chars[i] = ' ';
                inString = true;
                inVerbatimString = i > 0 && chars[i - 1] == '@';
            }
            else if (current == '\'')
            {
                chars[i] = ' ';
                inCharacter = true;
            }
        }

        return new string(chars);
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
