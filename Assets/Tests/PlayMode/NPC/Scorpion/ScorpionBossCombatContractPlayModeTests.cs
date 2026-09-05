using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Beavermania.Tests.PlayMode.NPC.Scorpion
{
    public sealed class ScorpionBossCombatContractPlayModeTests
    {
        const string LogSpawnerTypeName = "Beavermania.Objects.LogSpawner";
        const string ScorpionStateTypeName = "Beavermania.NPC.ScorpionState";
        const string ScorpionTypeName = "Beavermania.NPC.ScorpionScript";
        const string StatsTypeName = "Beavermania.Data.NPC.ScorpionStatsData";
        const string TestLogName = "ScorpionContractTestLog";

        GameObject bossObject;
        GameObject treeObject;
        GameObject logPrefabObject;
        Component boss;
        Component tree;
        ScriptableObject stats;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            GameObject[] objects = UnityEngine.Object.FindObjectsOfType<GameObject>();
            for (int index = 0; index < objects.Length; index++)
            {
                if (objects[index] != null && objects[index].name.StartsWith(TestLogName, StringComparison.Ordinal))
                    UnityEngine.Object.Destroy(objects[index]);
            }

            if (treeObject != null)
                UnityEngine.Object.Destroy(treeObject);
            if (logPrefabObject != null)
                UnityEngine.Object.Destroy(logPrefabObject);
            if (bossObject != null)
                UnityEngine.Object.Destroy(bossObject);
            if (stats != null)
                UnityEngine.Object.Destroy(stats);

            yield return null;
        }

        [UnityTest]
        public IEnumerator MarkedTree_FrontalCharge_DestroysTreeProducesLogsAndStuns()
        {
            // Arrange
            CreateBossInCharge();
            CreateTree(marked: true, withLogs: true);

            // Act
            bool handled = (bool)InvokeMethod(
                boss,
                "HandleMarkedGivingTreeChargeContact",
                tree,
                Vector3.back);
            bool logsDropped = GetFieldValue<bool>(tree, "_logsDropped");
            int producedLogs = CountProducedLogs();
            yield return null;

            // Assert
            Assert.That(handled, Is.True);
            Assert.That(logsDropped, Is.True);
            Assert.That(producedLogs, Is.EqualTo(4));
            Assert.That(GetStateName(), Is.EqualTo("Stunned"));
            Assert.That(treeObject == null, Is.True, "LogSpawner must own delayed tree destruction.");
        }

        [UnityTest]
        public IEnumerator UnmarkedTree_FrontalCharge_DoesNotDestroyOrStun()
        {
            // Arrange
            CreateBossInCharge();
            CreateTree(marked: false, withLogs: false);

            // Act
            bool handled = (bool)InvokeMethod(
                boss,
                "HandleMarkedGivingTreeChargeContact",
                tree,
                Vector3.back);
            yield return null;

            // Assert
            Assert.That(handled, Is.False);
            Assert.That(GetFieldValue<bool>(tree, "_logsDropped"), Is.False);
            Assert.That(GetStateName(), Is.EqualTo("Charge"));
            Assert.That(treeObject != null, Is.True);
        }

        [UnityTest]
        public IEnumerator MarkedTree_SideCharge_DoesNotDestroyOrStun()
        {
            // Arrange
            CreateBossInCharge();
            CreateTree(marked: true, withLogs: false);

            // Act
            bool handled = (bool)InvokeMethod(
                boss,
                "HandleMarkedGivingTreeChargeContact",
                tree,
                Vector3.left);
            yield return null;

            // Assert
            Assert.That(handled, Is.False);
            Assert.That(GetFieldValue<bool>(tree, "_logsDropped"), Is.False);
            Assert.That(GetStateName(), Is.EqualTo("Charge"));
        }

        [UnityTest]
        public IEnumerator MarkedTree_DuringCooldown_DestroysTreeAndEndsChargeWithoutStun()
        {
            // Arrange
            CreateBossInCharge();
            CreateTree(marked: true, withLogs: false);
            SetFieldValue(boss, "stunCooldownRemaining", 5f);

            // Act
            bool handled = (bool)InvokeMethod(
                boss,
                "HandleMarkedGivingTreeChargeContact",
                tree,
                Vector3.back);
            bool logsDropped = GetFieldValue<bool>(tree, "_logsDropped");
            yield return null;

            // Assert
            Assert.That(handled, Is.True);
            Assert.That(logsDropped, Is.True);
            Assert.That(GetStateName(), Is.EqualTo("Idle"));
            Assert.That(GetFieldValue<float>(boss, "stunCooldownRemaining"), Is.EqualTo(5f).Within(0.0001f));
            Assert.That(treeObject == null, Is.True);
        }

        void CreateBossInCharge()
        {
            bossObject = new GameObject("ScorpionBossCombatContractPlayMode");
            bossObject.SetActive(false);
            Rigidbody rigidbody = bossObject.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            boss = bossObject.AddComponent(ResolveType(ScorpionTypeName));
            stats = ScriptableObject.CreateInstance(ResolveType(StatsTypeName));
            SetFieldValue(stats, "advancedAiEnabled", true);
            SetFieldValue(stats, "maxHealth", 100);
            SetFieldValue(stats, "bossStunDuration", 3.5f);
            SetFieldValue(stats, "bossStunCooldown", 10f);
            SetFieldValue(boss, "statsData", stats);
            SetFieldValue(boss, "rbScorpion", rigidbody);
            SetFieldValue(boss, "CurrentHealth", 100);
            SetFieldValue(boss, "currentState", CreateEnum(ScorpionStateTypeName, "Charge"));
            SetFieldValue(boss, "state", "Charge");
            SetFieldValue(boss, "lockedChargeDirection", Vector3.forward);
        }

        void CreateTree(bool marked, bool withLogs)
        {
            treeObject = new GameObject("ScorpionBossCombatContractTree");
            treeObject.AddComponent<BoxCollider>();
            tree = treeObject.AddComponent(ResolveType(LogSpawnerTypeName));
            SetFieldValue(tree, "canStunScorpionBoss", marked);

            if (!withLogs)
                return;

            logPrefabObject = new GameObject(TestLogName);
            SetFieldValue(tree, "Prefab", new[] { logPrefabObject.transform });
        }

        int CountProducedLogs()
        {
            return UnityEngine.Object.FindObjectsOfType<GameObject>()
                .Count(candidate => candidate != null && candidate.name == TestLogName + "(Clone)");
        }

        string GetStateName()
        {
            return GetFieldValue<object>(boss, "currentState").ToString();
        }

        static object CreateEnum(string typeName, string value)
        {
            return Enum.Parse(ResolveType(typeName), value);
        }

        static object InvokeMethod(object target, string methodName, params object[] parameters)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Missing method {methodName}.");
            return method.Invoke(target, parameters);
        }

        static T GetFieldValue<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field {fieldName}.");
            return (T)field.GetValue(target);
        }

        static void SetFieldValue(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field {fieldName}.");
            field.SetValue(target, value);
        }

        static Type ResolveType(string fullName)
        {
            Type type = AppDomain.CurrentDomain
                .GetAssemblies()
                .Select(assembly => assembly.GetType(fullName, throwOnError: false))
                .FirstOrDefault(candidate => candidate != null);
            if (type == null)
                throw new InvalidOperationException($"Failed to resolve runtime type '{fullName}'.");
            return type;
        }
    }
}
