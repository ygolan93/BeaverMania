using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class PrefabIntegrationValidator
{
    const string MenuRoot = "Tools/BeaverMania/Validation/";
    const string RegisterCall = "RuntimeServices.Register";
    const string PersistentBootstrapPrefab = "Assets/Prefabs/Objects/UI/GameMusic.prefab";
    const string PlayerBootstrapPrefab = "Assets/Prefabs/OtterPlayer/Otter_Shapekeys/Player.prefab";
    const string RuntimeBootstrapOwnerId = "runtime-bootstrap";

    static readonly Regex LocalIdRegex = new Regex(@"^--- !u!\d+ &(\-?\d+)$", RegexOptions.Compiled | RegexOptions.Multiline);
    static readonly Regex ObjectReferenceRegex = new Regex(@"\{fileID: (\-?\d+)(?:, guid: ([0-9a-fA-F]{32}))?(?:, type: \d+)?\}", RegexOptions.Compiled);

    static readonly string[] ApprovedServicePrefabs =
    {
        PersistentBootstrapPrefab,
        PlayerBootstrapPrefab
    };

    static readonly FocusPrefabRule[] FocusPrefabRules =
    {
        new FocusPrefabRule(PlayerBootstrapPrefab, typeof(PrefabRuntimeHardening), null),
        new FocusPrefabRule("Assets/Prefabs/Wasp/LVL1 Wasp.prefab", typeof(PrefabRuntimeHardening), null),
        new FocusPrefabRule("Assets/Prefabs/Scorpion/ScorpionBoss.prefab", typeof(PrefabRuntimeHardening), null),
        new FocusPrefabRule(PersistentBootstrapPrefab, typeof(RuntimeBootstrapOwner), RuntimeBootstrapOwnerId)
    };

    static readonly RequiredReferenceRule[] RequiredReferenceRules =
    {
        new RequiredReferenceRule(PersistentBootstrapPrefab, typeof(MusicPlaylist), nameof(MusicPlaylist.MusicSource))
    };

    [MenuItem(MenuRoot + "Run Prefab Integration Validation")]
    public static void RunPrefabIntegrationValidationMenu()
    {
        RunValidation(true, true, true, true, true);
    }

    [MenuItem(MenuRoot + "Validate References")]
    public static void ValidateReferencesMenu()
    {
        RunValidation(true, true, false, false, false);
    }

    [MenuItem(MenuRoot + "Validate Service Ownership")]
    public static void ValidateServiceOwnershipMenu()
    {
        RunValidation(false, false, true, true, true);
    }

    public static bool RunValidation(
        bool validateMissingScripts,
        bool validateSerializedReferences,
        bool validateDuplicateServices,
        bool validateFocusOwnership,
        bool validateServiceLocations)
    {
        var prefabPaths = AssetDatabase.FindAssets("t:Prefab")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => !string.IsNullOrEmpty(path))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var context = new ValidationContext(prefabPaths);

        if (validateMissingScripts)
        {
            ValidateMissingMonoBehaviours(context);
        }

        if (validateSerializedReferences)
        {
            ValidateSerializedObjectReferences(context);
            ValidateRequiredPrefabReferences(context);
        }

        if (validateDuplicateServices || validateServiceLocations)
        {
            CollectServiceComponents(context);
        }

        if (validateDuplicateServices)
        {
            ValidateDuplicateServiceComponents(context);
        }

        if (validateFocusOwnership)
        {
            ValidateFocusPrefabOwnershipMarkers(context);
        }

        if (validateServiceLocations)
        {
            ValidateServiceComponentLocations(context);
        }

        context.LogReport();
        return context.Errors.Count == 0;
    }

    static void ValidateMissingMonoBehaviours(ValidationContext context)
    {
        foreach (var prefabPath in context.PrefabPaths)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                continue;
            }

            foreach (var transform in prefab.GetComponentsInChildren<Transform>(true).OrderBy(TransformPath, StringComparer.Ordinal))
            {
                var components = transform.GetComponents<Component>();
                for (var i = 0; i < components.Length; i++)
                {
                    if (components[i] == null)
                    {
                        context.AddError("MissingMonoBehaviour", prefabPath, TransformPath(transform) + " componentIndex=" + i);
                    }
                }
            }
        }
    }

    static void ValidateSerializedObjectReferences(ValidationContext context)
    {
        foreach (var prefabPath in context.PrefabPaths)
        {
            if (!File.Exists(prefabPath))
            {
                continue;
            }

            var yaml = File.ReadAllText(prefabPath);
            var localIds = new HashSet<string>(LocalIdRegex.Matches(yaml).Cast<Match>().Select(match => match.Groups[1].Value));
            var refs = new SortedSet<string>(StringComparer.Ordinal);

            foreach (Match match in ObjectReferenceRegex.Matches(yaml))
            {
                var fileId = match.Groups[1].Value;
                var guid = match.Groups[2].Success ? match.Groups[2].Value : string.Empty;
                if (fileId == "0")
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(guid))
                {
                    if (string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(guid)))
                    {
                        refs.Add("missing asset guid=" + guid + " fileID=" + fileId);
                    }

                    continue;
                }

                if (!localIds.Contains(fileId) && !IsPrefabInstanceBackedYamlDocument(yaml, match.Index))
                {
                    refs.Add("missing local fileID=" + fileId);
                }
            }

            foreach (var reference in refs)
            {
                context.AddError("MissingSerializedObjectReference", prefabPath, reference);
            }
        }
    }

    static void ValidateRequiredPrefabReferences(ValidationContext context)
    {
        foreach (var rule in RequiredReferenceRules.OrderBy(rule => rule.PrefabPath, StringComparer.Ordinal))
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(rule.PrefabPath);
            if (prefab == null)
            {
                context.AddError("MissingRequiredReference", rule.PrefabPath, rule.ComponentType.FullName + "." + rule.PropertyName + " prefab not found");
                continue;
            }

            var components = prefab.GetComponentsInChildren(rule.ComponentType, true).Cast<Component>().Where(component => component != null).ToArray();
            if (components.Length == 0)
            {
                context.AddError("MissingRequiredReference", rule.PrefabPath, rule.ComponentType.FullName + "." + rule.PropertyName + " component not found");
                continue;
            }

            foreach (var component in components.OrderBy(component => TransformPath(component.transform), StringComparer.Ordinal))
            {
                var serializedObject = new SerializedObject(component);
                var property = serializedObject.FindProperty(rule.PropertyName);
                if (property == null || property.propertyType != SerializedPropertyType.ObjectReference || property.objectReferenceValue == null)
                {
                    context.AddError("MissingRequiredReference", rule.PrefabPath, rule.ComponentType.FullName + "." + rule.PropertyName + " at " + TransformPath(component.transform));
                }
            }
        }
    }

    static void CollectServiceComponents(ValidationContext context)
    {
        foreach (var prefabPath in context.PrefabPaths)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                continue;
            }

            foreach (var component in prefab.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component == null || !RegistersRuntimeService(component))
                {
                    continue;
                }

                context.ServiceComponents.Add(new ServiceComponentRecord(
                    prefabPath,
                    component.GetType(),
                    TransformPath(component.transform),
                    IsNestedPrefabInstance(component.gameObject)));
            }
        }

        context.ServiceComponents.Sort(ServiceComponentRecord.Compare);
    }

    static void ValidateDuplicateServiceComponents(ValidationContext context)
    {
        foreach (var group in context.ServiceComponents.GroupBy(record => record.Type.FullName).OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            var prefabPaths = group.Select(record => record.PrefabPath).Distinct().OrderBy(path => path, StringComparer.Ordinal).ToArray();
            if (prefabPaths.Length <= 1)
            {
                continue;
            }

            context.AddError("DuplicateServiceComponent", prefabPaths[0], group.Key + " prefabs=" + string.Join(", ", prefabPaths));
        }
    }

    static void ValidateFocusPrefabOwnershipMarkers(ValidationContext context)
    {
        foreach (var rule in FocusPrefabRules.OrderBy(rule => rule.PrefabPath, StringComparer.Ordinal))
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(rule.PrefabPath);
            if (prefab == null)
            {
                context.AddError("MissingFocusPrefab", rule.PrefabPath, "prefab not found");
                continue;
            }

            if (rule.MarkerType == typeof(RuntimeBootstrapOwner))
            {
                var owners = prefab.GetComponentsInChildren<RuntimeBootstrapOwner>(true);
                if (!owners.Any(owner => owner != null && owner.OwnerId == rule.RequiredOwnerId))
                {
                    context.AddError("MissingOwnershipMarker", rule.PrefabPath, "RuntimeBootstrapOwner.ownerId=" + rule.RequiredOwnerId);
                }

                continue;
            }

            if (!prefab.GetComponentsInChildren(rule.MarkerType, true).Cast<Component>().Any(component => component != null))
            {
                context.AddError("MissingOwnershipMarker", rule.PrefabPath, rule.MarkerType.FullName);
            }
        }
    }

    static void ValidateServiceComponentLocations(ValidationContext context)
    {
        foreach (var record in context.ServiceComponents)
        {
            if (!ApprovedServicePrefabs.Contains(record.PrefabPath))
            {
                context.AddError("UnapprovedServicePrefab", record.PrefabPath, record.Type.FullName + " at " + record.TransformPath);
            }

            if (record.IsNestedPrefabInstance && !ApprovedServicePrefabs.Contains(record.PrefabPath))
            {
                context.AddError("NestedServicePrefabInstance", record.PrefabPath, record.Type.FullName + " at " + record.TransformPath);
            }
        }
    }

    static bool IsPrefabInstanceBackedYamlDocument(string yaml, int index)
    {
        var documentStart = yaml.LastIndexOf("--- !u!", index, StringComparison.Ordinal);
        if (documentStart < 0)
        {
            return false;
        }

        var nextDocument = yaml.IndexOf("\n--- !u!", documentStart + 1, StringComparison.Ordinal);
        var documentLength = nextDocument < 0 ? yaml.Length - documentStart : nextDocument - documentStart;
        var document = yaml.Substring(documentStart, documentLength);
        return document.Contains("m_PrefabInstance: {fileID: ") && !document.Contains("m_PrefabInstance: {fileID: 0}");
    }

    static bool RegistersRuntimeService(MonoBehaviour component)
    {
        var script = MonoScript.FromMonoBehaviour(component);
        return script != null && script.text.Contains(RegisterCall);
    }

    static bool IsNestedPrefabInstance(GameObject gameObject)
    {
        return PrefabUtility.IsPartOfPrefabInstance(gameObject) && PrefabUtility.GetNearestPrefabInstanceRoot(gameObject) != null;
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

    sealed class FocusPrefabRule
    {
        public readonly string PrefabPath;
        public readonly Type MarkerType;
        public readonly string RequiredOwnerId;

        public FocusPrefabRule(string prefabPath, Type markerType, string requiredOwnerId)
        {
            PrefabPath = prefabPath;
            MarkerType = markerType;
            RequiredOwnerId = requiredOwnerId;
        }
    }

    sealed class RequiredReferenceRule
    {
        public readonly string PrefabPath;
        public readonly Type ComponentType;
        public readonly string PropertyName;

        public RequiredReferenceRule(string prefabPath, Type componentType, string propertyName)
        {
            PrefabPath = prefabPath;
            ComponentType = componentType;
            PropertyName = propertyName;
        }
    }

    sealed class ServiceComponentRecord
    {
        public readonly string PrefabPath;
        public readonly Type Type;
        public readonly string TransformPath;
        public readonly bool IsNestedPrefabInstance;

        public ServiceComponentRecord(string prefabPath, Type type, string transformPath, bool isNestedPrefabInstance)
        {
            PrefabPath = prefabPath;
            Type = type;
            TransformPath = transformPath;
            IsNestedPrefabInstance = isNestedPrefabInstance;
        }

        public static int Compare(ServiceComponentRecord left, ServiceComponentRecord right)
        {
            var typeCompare = string.Compare(left.Type.FullName, right.Type.FullName, StringComparison.Ordinal);
            if (typeCompare != 0)
            {
                return typeCompare;
            }

            var prefabCompare = string.Compare(left.PrefabPath, right.PrefabPath, StringComparison.Ordinal);
            if (prefabCompare != 0)
            {
                return prefabCompare;
            }

            return string.Compare(left.TransformPath, right.TransformPath, StringComparison.Ordinal);
        }
    }

    sealed class ValidationContext
    {
        public readonly string[] PrefabPaths;
        public readonly List<string> Errors = new List<string>();
        public readonly List<ServiceComponentRecord> ServiceComponents = new List<ServiceComponentRecord>();

        public ValidationContext(string[] prefabPaths)
        {
            PrefabPaths = prefabPaths;
        }

        public void AddError(string rule, string prefabPath, string detail)
        {
            Errors.Add(rule + " | " + prefabPath + " | " + detail);
        }

        public void LogReport()
        {
            Errors.Sort(StringComparer.Ordinal);

            var builder = new StringBuilder();
            builder.AppendLine("[BeaverMania Prefab Integration Validation]");
            builder.AppendLine("Prefabs scanned: " + PrefabPaths.Length);
            builder.AppendLine("Errors: " + Errors.Count);
            foreach (var error in Errors)
            {
                builder.AppendLine("ERROR | " + error);
            }

            if (Errors.Count == 0)
            {
                Debug.Log(builder.ToString());
            }
            else
            {
                Debug.LogError(builder.ToString());
            }
        }
    }
}
