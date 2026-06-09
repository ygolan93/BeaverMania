#if UNITY_EDITOR
using System.Collections.Generic;
using Beavermania.Audio;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace Beavermania.Tests.Audio
{
    public static class AudioRoutingPrefabAudit
    {
        public const string MixerPath = "Assets/Scripts/SceneScripts/NewAudioMixer.mixer";

        static readonly string[] AllowlistPrefabPaths =
        {
            "Assets/Prefabs/Objects/UI/GameMusic.prefab",
            "Assets/Prefabs/Objects/UI/PlayerCanvas.prefab",
            "Assets/Prefabs/Player/Otter_Shapekeys/Player.prefab",
            "Assets/Prefabs/Player/PlayerPack-Drop and Play.prefab",
            "Assets/Prefabs/NPC/ShadowRevenant/ShadowRevenant.prefab",
            "Assets/Prefabs/NPC/ShadowRevenant/ShadowRevenantShadeMinion.prefab",
            "Assets/Prefabs/Scorpion/ScorpionBoss.prefab",
            "Assets/Prefabs/ProjectEffects/Electric Effect.prefab",
            "Assets/Prefabs/Objects/Interactable/Elevator/FirstLift.prefab",
        };

        public static IReadOnlyList<string> Allowlist => AllowlistPrefabPaths;

        public static List<AudioRoutingPrefabIssue> ScanAllowlist()
        {
            var issues = new List<AudioRoutingPrefabIssue>();

            for (int i = 0; i < AllowlistPrefabPaths.Length; i++)
            {
                string prefabPath = AllowlistPrefabPaths[i];
                if (!AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath))
                {
                    issues.Add(new AudioRoutingPrefabIssue(
                        prefabPath,
                        string.Empty,
                        "Missing prefab asset in allowlist."));
                    continue;
                }

                ScanPrefab(prefabPath, issues);

                if (i > 0 && i % 4 == 0)
                    EditorUtility.UnloadUnusedAssetsImmediate();
            }

            return issues;
        }

        static void ScanPrefab(string prefabPath, List<AudioRoutingPrefabIssue> issues)
        {
            GameObject contents = null;
            try
            {
                contents = PrefabUtility.LoadPrefabContents(prefabPath);
                ScanHierarchy(contents.transform, prefabPath, issues);
            }
            finally
            {
                if (contents != null)
                    PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        static void ScanHierarchy(Transform root, string prefabPath, List<AudioRoutingPrefabIssue> issues)
        {
            ScanGameObject(root.gameObject, prefabPath, issues);

            for (int i = 0; i < root.childCount; i++)
                ScanHierarchy(root.GetChild(i), prefabPath, issues);
        }

        static void ScanGameObject(GameObject gameObject, string prefabPath, List<AudioRoutingPrefabIssue> issues)
        {
            AudioVolumeSettings settings = gameObject.GetComponent<AudioVolumeSettings>();
            if (settings != null)
                ScanAudioVolumeSettings(settings, prefabPath, issues);

            AudioSource[] sources = gameObject.GetComponents<AudioSource>();
            for (int i = 0; i < sources.Length; i++)
            {
                AudioSource source = sources[i];
                if (source == null || !source.enabled)
                    continue;

                if (source.outputAudioMixerGroup == null)
                {
                    issues.Add(new AudioRoutingPrefabIssue(
                        prefabPath,
                        BuildHierarchyPath(source.transform),
                        $"Enabled AudioSource '{source.name}' has no OutputAudioMixerGroup."));
                }
            }
        }

        static void ScanAudioVolumeSettings(AudioVolumeSettings settings, string prefabPath, List<AudioRoutingPrefabIssue> issues)
        {
            string hierarchyPath = BuildHierarchyPath(settings.transform);
            SerializedObject serializedObject = new SerializedObject(settings);

            RequireObjectReference(serializedObject, "audioMixer", prefabPath, hierarchyPath, issues);
            RequireObjectReference(serializedObject, "musicGroup", prefabPath, hierarchyPath, issues);
            RequireObjectReference(serializedObject, "sfxGroup", prefabPath, hierarchyPath, issues);
            RequireObjectReference(serializedObject, "enemiesGroup", prefabPath, hierarchyPath, issues);
            RequireObjectReference(serializedObject, "uiGroup", prefabPath, hierarchyPath, issues);
        }

        static void RequireObjectReference(
            SerializedObject serializedObject,
            string propertyName,
            string prefabPath,
            string hierarchyPath,
            List<AudioRoutingPrefabIssue> issues)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                issues.Add(new AudioRoutingPrefabIssue(
                    prefabPath,
                    hierarchyPath,
                    $"AudioVolumeSettings is missing serialized field '{propertyName}'."));
                return;
            }

            if (property.objectReferenceValue == null)
            {
                issues.Add(new AudioRoutingPrefabIssue(
                    prefabPath,
                    hierarchyPath,
                    $"AudioVolumeSettings.{propertyName} is not assigned."));
            }
        }

        static string BuildHierarchyPath(Transform transform)
        {
            if (transform == null)
                return string.Empty;

            var segments = new Stack<string>();
            Transform current = transform;
            while (current != null)
            {
                segments.Push(current.name);
                current = current.parent;
            }

            return string.Join("/", segments);
        }

        public readonly struct AudioRoutingPrefabIssue
        {
            public AudioRoutingPrefabIssue(string prefabPath, string hierarchyPath, string message)
            {
                PrefabPath = prefabPath;
                HierarchyPath = hierarchyPath;
                Message = message;
            }

            public string PrefabPath { get; }
            public string HierarchyPath { get; }
            public string Message { get; }

            public override string ToString()
            {
                if (string.IsNullOrEmpty(HierarchyPath))
                    return $"{PrefabPath}: {Message}";

                return $"{PrefabPath} ({HierarchyPath}): {Message}";
            }
        }
    }
}
#endif
