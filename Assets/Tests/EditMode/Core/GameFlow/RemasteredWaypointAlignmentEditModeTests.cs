using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Beavermania.Tests.EditMode.Core.GameFlow
{
    public sealed class RemasteredWaypointAlignmentEditModeTests
    {
        const string RemasteredScenePath = "Assets/Scenes/Level 1 - Remastered - Steam.unity";
        const string PlayerPackObjectName = "PlayerPack-Drop and Play";
        const int ExpectedObjectiveCount = 13;

        readonly Dictionary<string, Type> cachedRuntimeTypes = new(StringComparer.Ordinal);

        Scene previousActiveScene;

        [SetUp]
        public void SetUp()
        {
            previousActiveScene = SceneManager.GetActiveScene();
        }

        [TearDown]
        public void TearDown()
        {
            if (!string.IsNullOrEmpty(previousActiveScene.path))
                EditorSceneManager.OpenScene(previousActiveScene.path, OpenSceneMode.Single);
        }

        [Test]
        public void RemasteredScene_PlayerPack_ObjectiveAndWaypointSlots_AreAligned()
        {
            EditorSceneManager.OpenScene(RemasteredScenePath, OpenSceneMode.Single);

            GameObject playerPack = GameObject.Find(PlayerPackObjectName);
            Assert.That(playerPack, Is.Not.Null, $"Scene is missing '{PlayerPackObjectName}'.");

            Type wayPointType = ResolveRuntimeType("Beavermania.UI.Objectives.WayPoint");
            Type objectiveUiType = ResolveRuntimeType("Beavermania.UI.Objectives.ObjectiveUI");
            Type logSpawnerType = ResolveRuntimeType("Beavermania.Objects.LogSpawner");

            Component wayPoint = playerPack.GetComponentInChildren(wayPointType, true);
            Assert.That(wayPoint, Is.Not.Null, "PlayerPack is missing nested WayPoint.");

            Component objectiveUi = wayPoint.GetComponent(objectiveUiType);
            Assert.That(objectiveUi, Is.Not.Null, "Player root is missing ObjectiveUI bound to ObjectiveSyncService.");

            string[] objectives = GetFieldValue<string[]>(objectiveUi, "Objective");
            Transform[] locations = GetFieldValue<Transform[]>(wayPoint, "Locations");

            Assert.That(objectives, Is.Not.Null.And.Not.Empty, "ObjectiveUI.Objective is empty.");
            Assert.That(objectives.Length, Is.EqualTo(ExpectedObjectiveCount), "ObjectiveUI should expose the 13-step Remastered chain.");
            Assert.That(locations, Is.Not.Null, "WayPoint.Locations is null on the Remastered PlayerPack instance.");
            Assert.That(locations.Length, Is.EqualTo(ExpectedObjectiveCount), "WayPoint.Locations should match the 13 objective slots.");
            Assert.That(objectives.Length, Is.LessThanOrEqualTo(locations.Length), "Objective text count must not exceed waypoint slot count.");

            for (int index = 0; index <= 5; index++)
                Assert.That(locations[index], Is.Not.Null, $"Locations[{index}] must be assigned for early progression.");

            for (int index = 3; index <= 5; index++)
            {
                Assert.That(
                    IsValidChopWaypointTarget(locations[index], logSpawnerType),
                    Is.True,
                    $"Locations[{index}] must reference a LogSpawner tree or a documented chop zone marker.");
            }
        }

        static bool IsValidChopWaypointTarget(Transform location, Type logSpawnerType)
        {
            if (location == null)
                return false;

            if (logSpawnerType != null && location.GetComponent(logSpawnerType) != null)
                return true;

            if (location.CompareTag("Objective"))
                return true;

            string name = location.name;
            return name.IndexOf("TreesToCut", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("WP", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Cliff", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        Type ResolveRuntimeType(string fullName)
        {
            if (cachedRuntimeTypes.TryGetValue(fullName, out Type cachedType))
                return cachedType;

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type resolvedType = assembly.GetType(fullName, throwOnError: false);
                if (resolvedType != null)
                {
                    cachedRuntimeTypes[fullName] = resolvedType;
                    return resolvedType;
                }
            }

            throw new InvalidOperationException($"Failed to resolve runtime type '{fullName}'.");
        }

        static T GetFieldValue<T>(object target, string fieldName)
        {
            FieldInfo fieldInfo = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (fieldInfo == null)
                throw new MissingFieldException(target.GetType().FullName, fieldName);

            return (T)fieldInfo.GetValue(target);
        }
    }
}
