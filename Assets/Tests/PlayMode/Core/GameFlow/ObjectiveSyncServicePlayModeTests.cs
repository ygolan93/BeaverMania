using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Beavermania.Tests.PlayMode.Core.GameFlow
{
    public sealed class ObjectiveSyncServicePlayModeTests
    {
        readonly Dictionary<string, Type> cachedRuntimeTypes = new(StringComparer.Ordinal);
        readonly List<GameObject> spawnedObjects = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < spawnedObjects.Count; i++)
            {
                if (spawnedObjects[i] != null)
                    UnityEngine.Object.DestroyImmediate(spawnedObjects[i]);
            }

            spawnedObjects.Clear();
        }

        [UnityTest]
        public IEnumerator Initialize_PublishesMatchingObjectiveTextAndWaypointTarget()
        {
            var harness = CreateHarness(initialObjectiveIndex: 1);

            yield return null;

            Assert.That(harness.GetServiceInt("CurrentObjectiveIndex"), Is.EqualTo(1));
            Assert.That(harness.GetServiceString("CurrentObjectiveText"), Is.EqualTo("Talk to the trader"));
            Assert.That(harness.GetServiceInt("CurrentWaypointIndex"), Is.EqualTo(1));
            Assert.That(harness.GetServiceTransform("CurrentWaypointTarget"), Is.SameAs(harness.GetWayPointLocations()[1]));
            Assert.That(harness.GetObjectiveInstruction(), Is.EqualTo("Talk to the trader"));
            Assert.That(harness.GetHudObjectiveText(), Is.EqualTo("Talk to the trader"));
        }

        [UnityTest]
        public IEnumerator TryAdvanceObjective_AdvancesOnceAndUpdatesTextAndWaypointTogether()
        {
            var harness = CreateHarness(initialObjectiveIndex: 0);

            yield return null;

            bool advanced = harness.InvokeServiceBool(
                "TryAdvanceObjective",
                1,
                CreateObjectiveAdvanceReason("DialogueCompleted"));

            Assert.That(advanced, Is.True);
            Assert.That(harness.GetServiceInt("CurrentObjectiveIndex"), Is.EqualTo(1));
            Assert.That(harness.GetServiceString("CurrentObjectiveText"), Is.EqualTo("Talk to the trader"));
            Assert.That(harness.GetServiceTransform("CurrentWaypointTarget"), Is.SameAs(harness.GetWayPointLocations()[1]));
            Assert.That(harness.GetObjectiveInstruction(), Is.EqualTo("Talk to the trader"));
            Assert.That(harness.GetHudObjectiveText(), Is.EqualTo("Talk to the trader"));
        }

        [UnityTest]
        public IEnumerator DuplicateRequests_AreNoOpsAndDoNotSkipObjectives()
        {
            var harness = CreateHarness(initialObjectiveIndex: 1);

            yield return null;

            object waypointTriggerReason = CreateObjectiveAdvanceReason("WaypointTrigger");
            bool duplicateAdvance = harness.InvokeServiceBool("TrySetObjectiveIndex", 1, waypointTriggerReason);
            bool regressiveAdvance = harness.InvokeServiceBool("TrySetObjectiveIndex", 0, waypointTriggerReason);

            Assert.That(duplicateAdvance, Is.False);
            Assert.That(regressiveAdvance, Is.False);
            Assert.That(harness.GetServiceInt("CurrentObjectiveIndex"), Is.EqualTo(1));
            Assert.That(harness.GetServiceString("CurrentObjectiveText"), Is.EqualTo("Talk to the trader"));
        }

        [UnityTest]
        public IEnumerator WaypointRequests_DoNotRegressOrDoubleAdvanceOnRepeatedTrigger()
        {
            var harness = CreateHarness(initialObjectiveIndex: 0);

            yield return null;

            object waypointTriggerReason = CreateObjectiveAdvanceReason("WaypointTrigger");
            bool firstRequest = harness.InvokeServiceBool("TrySetObjectiveIndex", 2, waypointTriggerReason);
            bool duplicateRequest = harness.InvokeServiceBool("TrySetObjectiveIndex", 2, waypointTriggerReason);
            bool regressiveRequest = harness.InvokeServiceBool("TrySetObjectiveIndex", 1, waypointTriggerReason);

            Assert.That(firstRequest, Is.True);
            Assert.That(duplicateRequest, Is.False);
            Assert.That(regressiveRequest, Is.False);
            Assert.That(harness.GetServiceInt("CurrentObjectiveIndex"), Is.EqualTo(2));
            Assert.That(harness.GetServiceString("CurrentObjectiveText"), Is.EqualTo("Buy weapons"));
            Assert.That(harness.GetServiceTransform("CurrentWaypointTarget"), Is.SameAs(harness.GetWayPointLocations()[2]));
        }

        [UnityTest]
        public IEnumerator RefreshBindingsAndReapply_RestoresCurrentObjectiveTextAfterOverrideClears()
        {
            var harness = CreateHarness(initialObjectiveIndex: 2);

            yield return null;

            harness.SetHudField("ObjectiveText", "STALE");
            harness.SetHudField("ObjectiveTextOverride", "Press E to interact");
            harness.SetHudField("ObjectiveTextOverrideActive", false);

            harness.InvokeServiceVoid("RefreshBindingsAndReapply");

            Assert.That(harness.GetServiceString("CurrentObjectiveText"), Is.EqualTo("Buy weapons"));
            Assert.That(harness.GetHudObjectiveText(), Is.EqualTo("Buy weapons"));
        }

        [UnityTest]
        public IEnumerator LegacyBridgeStageRequests_ResolveAgainstSceneAuthoredObjectiveSequence()
        {
            var harness = CreateHarness(
                initialObjectiveIndex: 5,
                objectives: new[]
                {
                    "Clear the wasp nest",
                    "Talk to the trader",
                    "Buy weapons",
                    "Chop the first tree",
                    "Chop the second tree",
                    "Chop the third tree",
                    "Construct the bridge over the cliff",
                    "Clear the second nest"
                });

            yield return null;

            bool movedToBridge = harness.InvokeServiceBool("OnPlayerNearBridgeFrame");
            bool movedBeyondBridge = harness.InvokeServiceBool("OnBridgeCompleted");

            Assert.That(movedToBridge, Is.True);
            Assert.That(movedBeyondBridge, Is.True);
            Assert.That(harness.GetServiceInt("CurrentObjectiveIndex"), Is.EqualTo(7));
            Assert.That(harness.GetServiceString("CurrentObjectiveText"), Is.EqualTo("Clear the second nest"));
            Assert.That(harness.GetServiceTransform("CurrentWaypointTarget"), Is.SameAs(harness.GetWayPointLocations()[7]));
        }

        TestHarness CreateHarness(int initialObjectiveIndex, string[] objectives = null)
        {
            GameObject player = Spawn("Player");
            player.tag = "Player";

            var hudState = player.AddComponent(ResolveRuntimeType("Beavermania.Player.PlayerHudState"));
            var wayPoint = player.AddComponent(ResolveRuntimeType("Beavermania.UI.Objectives.WayPoint"));
            var objectiveUi = player.AddComponent(ResolveRuntimeType("Beavermania.UI.Objectives.ObjectiveUI"));

            string[] configuredObjectives = objectives ?? new[]
            {
                "Clear the wasp nest",
                "Talk to the trader",
                "Buy weapons",
                "Chop the first tree"
            };
            TrackSpawnedWaypointHolders(wayPoint);
            SetFieldValue(wayPoint, "Locations", CreateWaypointTargets(configuredObjectives.Length));
            SetFieldValue(wayPoint, "i", initialObjectiveIndex);

            SetFieldValue(objectiveUi, "Objective", configuredObjectives);
            SetFieldValue(objectiveUi, "i", initialObjectiveIndex);
            SetFieldValue(objectiveUi, "currentPoint", wayPoint);

            GameObject serviceObject = Spawn("ObjectiveSyncService");
            var service = serviceObject.AddComponent(ResolveRuntimeType("Beavermania.Core.GameFlow.ObjectiveSyncService"));

            return new TestHarness(service, objectiveUi, wayPoint, hudState);
        }

        GameObject Spawn(string name)
        {
            var go = new GameObject(name);
            spawnedObjects.Add(go);
            return go;
        }

        Transform[] CreateWaypointTargets(int count)
        {
            var waypoints = new Transform[count];
            for (int index = 0; index < count; index++)
                waypoints[index] = Spawn($"Waypoint_{index}").transform;

            return waypoints;
        }

        void TrackSpawnedWaypointHolders(Component wayPoint)
        {
            Transform[] existingLocations = GetFieldValue<Transform[]>(wayPoint, "Locations");
            if (existingLocations == null)
                return;

            for (int index = 0; index < existingLocations.Length; index++)
            {
                Transform existingLocation = existingLocations[index];
                if (existingLocation != null && !spawnedObjects.Contains(existingLocation.gameObject))
                    spawnedObjects.Add(existingLocation.gameObject);
            }
        }

        object CreateObjectiveAdvanceReason(string name)
        {
            Type objectiveAdvanceReasonType = ResolveRuntimeType("Beavermania.Core.GameFlow.ObjectiveAdvanceReason");
            return Enum.Parse(objectiveAdvanceReasonType, name);
        }

        Type ResolveRuntimeType(string fullName)
        {
            if (cachedRuntimeTypes.TryGetValue(fullName, out Type cachedType))
                return cachedType;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
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
            return (T)GetFieldInfo(target, fieldName).GetValue(target);
        }

        static void SetFieldValue(object target, string fieldName, object value)
        {
            GetFieldInfo(target, fieldName).SetValue(target, value);
        }

        static FieldInfo GetFieldInfo(object target, string fieldName)
        {
            FieldInfo fieldInfo = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (fieldInfo == null)
                throw new MissingFieldException(target.GetType().FullName, fieldName);

            return fieldInfo;
        }

        static T GetPropertyValue<T>(object target, string propertyName)
        {
            PropertyInfo propertyInfo = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (propertyInfo == null)
                throw new MissingMemberException(target.GetType().FullName, propertyName);

            return (T)propertyInfo.GetValue(target);
        }

        static object InvokeMethod(object target, string methodName, params object[] parameters)
        {
            MethodInfo methodInfo = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (methodInfo == null)
                throw new MissingMethodException(target.GetType().FullName, methodName);

            return methodInfo.Invoke(target, parameters);
        }

        readonly struct TestHarness
        {
            public TestHarness(
                Component service,
                Component objectiveUi,
                Component wayPoint,
                Component hudState)
            {
                Service = service;
                ObjectiveUi = objectiveUi;
                WayPoint = wayPoint;
                HudState = hudState;
            }

            public Component Service { get; }

            public Component ObjectiveUi { get; }

            public Component WayPoint { get; }

            public Component HudState { get; }

            public int GetServiceInt(string propertyName)
            {
                return GetPropertyValue<int>(Service, propertyName);
            }

            public string GetServiceString(string propertyName)
            {
                return GetPropertyValue<string>(Service, propertyName);
            }

            public Transform GetServiceTransform(string propertyName)
            {
                return GetPropertyValue<Transform>(Service, propertyName);
            }

            public Transform[] GetWayPointLocations()
            {
                return GetFieldValue<Transform[]>(WayPoint, "Locations");
            }

            public string GetObjectiveInstruction()
            {
                return GetFieldValue<string>(ObjectiveUi, "Instruction");
            }

            public string GetHudObjectiveText()
            {
                return GetFieldValue<string>(HudState, "ObjectiveText");
            }

            public void SetHudField(string fieldName, object value)
            {
                SetFieldValue(HudState, fieldName, value);
            }

            public bool InvokeServiceBool(string methodName, params object[] parameters)
            {
                return (bool)InvokeMethod(Service, methodName, parameters);
            }

            public void InvokeServiceVoid(string methodName, params object[] parameters)
            {
                InvokeMethod(Service, methodName, parameters);
            }
        }
    }
}
