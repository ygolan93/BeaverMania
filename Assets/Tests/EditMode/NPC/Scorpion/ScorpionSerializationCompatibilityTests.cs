using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Beavermania.Tests.EditMode.NPC.Scorpion
{
    public sealed class ScorpionSerializationCompatibilityTests
    {
        const string ScorpionStateTypeName = "Beavermania.NPC.ScorpionState";
        const string ScorpionTypeName = "Beavermania.NPC.ScorpionScript";
        const string StatsTypeName = "Beavermania.Data.NPC.ScorpionStatsData";
        const string BoostChargeSettingsTypeName = "Beavermania.Data.Combat.BoostChargeSettings";
        const string BoostChargeTypeName = "Beavermania.Player.Combat.BoostChargeController";
        const int EnemyLayer = 10;

        GameObject scorpionObject;
        Component scorpion;
        ScriptableObject stats;
        ScriptableObject boostSettings;
        bool originalEnemyLayerCollisionIgnored;

        [SetUp]
        public void SetUp()
        {
            originalEnemyLayerCollisionIgnored = Physics.GetIgnoreLayerCollision(EnemyLayer, EnemyLayer);

            scorpionObject = new GameObject("ScorpionSerializationCompatibility");
            scorpionObject.SetActive(false);
            Rigidbody rigidbody = scorpionObject.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            Component boostCharge = scorpionObject.AddComponent(ResolveType(BoostChargeTypeName));
            scorpion = scorpionObject.AddComponent(ResolveType(ScorpionTypeName));
            stats = ScriptableObject.CreateInstance(ResolveType(StatsTypeName));
            boostSettings = ScriptableObject.CreateInstance(ResolveType(BoostChargeSettingsTypeName));
            SetFieldValue(stats, "advancedAiEnabled", true);
            SetFieldValue(stats, "maxHealth", 100);
            SetFieldValue(scorpion, "statsData", stats);
            SetFieldValue(scorpion, "rbScorpion", rigidbody);
            SetFieldValue(boostCharge, "settings", boostSettings);
            SetFieldValue(scorpion, "boostCharge", boostCharge);
            SetFieldValue(scorpion, "CurrentHealth", 100);
        }

        [TearDown]
        public void TearDown()
        {
            Physics.IgnoreLayerCollision(EnemyLayer, EnemyLayer, originalEnemyLayerCollisionIgnored);
            if (boostSettings != null)
                UnityEngine.Object.DestroyImmediate(boostSettings);
            if (stats != null)
                UnityEngine.Object.DestroyImmediate(stats);
            if (scorpionObject != null)
                UnityEngine.Object.DestroyImmediate(scorpionObject);
        }

        [TestCase("Jaw1A")]
        [TestCase("Jaw1B")]
        [TestCase("Jaw2A")]
        [TestCase("Jaw2B")]
        [TestCase("Sting")]
        public void ColliderIdentityField_RemainsPublicAndSerializationCompatible(string fieldName)
        {
            // Arrange
            Type scorpionType = ResolveType(ScorpionTypeName);

            // Act
            FieldInfo field = scorpionType.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);

            // Assert
            Assert.That(field, Is.Not.Null, $"Missing serialized identity field '{fieldName}'.");
            Assert.That(field.FieldType, Is.EqualTo(typeof(Collider)));
        }

        [Test]
        public void StateField_RemainsPublicStringWithIdleDefault()
        {
            // Arrange
            FieldInfo field = ResolveType(ScorpionTypeName).GetField(
                "state",
                BindingFlags.Instance | BindingFlags.Public);

            // Act
            string value = field?.GetValue(scorpion) as string;

            // Assert
            Assert.That(field, Is.Not.Null);
            Assert.That(field.FieldType, Is.EqualTo(typeof(string)));
            Assert.That(value, Is.EqualTo("Idle"));
        }

        [Test]
        public void Awake_SynchronizesStateMirrorToIdle()
        {
            // Arrange
            SetFieldValue(scorpion, "currentState", CreateState("Attack"));
            SetFieldValue(scorpion, "state", "Stale");

            // Act
            InvokeMethod(scorpion, "Awake");

            // Assert
            Assert.That(GetFieldValue<object>(scorpion, "currentState").ToString(), Is.EqualTo("Idle"));
            Assert.That(GetFieldValue<string>(scorpion, "state"), Is.EqualTo("Idle"));
        }

        [Test]
        public void EnterState_SynchronizesStateMirror()
        {
            // Arrange
            SetFieldValue(scorpion, "currentState", CreateState("Idle"));
            SetFieldValue(scorpion, "state", "Idle");

            // Act
            InvokeMethod(scorpion, "EnterState", CreateState("Attack"));

            // Assert
            Assert.That(GetFieldValue<string>(scorpion, "state"), Is.EqualTo("Attack"));
        }

        [Test]
        public void Death_SynchronizesStateMirror()
        {
            // Arrange
            SetFieldValue(scorpion, "currentState", CreateState("Attack"));
            SetFieldValue(scorpion, "state", "Attack");

            // Act
            InvokeMethod(scorpion, "Death");

            // Assert
            Assert.That(GetFieldValue<object>(scorpion, "currentState").ToString(), Is.EqualTo("Dead"));
            Assert.That(GetFieldValue<string>(scorpion, "state"), Is.EqualTo("Dead"));
        }

        [Test]
        public void HurricaneRetreat_WithoutRigidbody_ReturnsWithoutChangingState()
        {
            // Arrange
            SetFieldValue(scorpion, "rbScorpion", null);
            SetFieldValue(scorpion, "currentState", CreateState("Attack"));
            SetFieldValue(scorpion, "state", "Attack");
            string methodName = ResolveType(ScorpionTypeName).GetMethod(
                "TryStartHurricaneRetreat",
                BindingFlags.Instance | BindingFlags.NonPublic) != null
                    ? "TryStartHurricaneRetreat"
                    : "TryStartHurricaneKickRetreat";

            // Act / Assert
            Assert.DoesNotThrow(() => InvokeMethod(scorpion, methodName, (object)null));
            Assert.That(GetFieldValue<object>(scorpion, "currentState").ToString(), Is.EqualTo("Attack"));
            Assert.That(GetFieldValue<string>(scorpion, "state"), Is.EqualTo("Attack"));
        }

        static object CreateState(string name)
        {
            return Enum.Parse(ResolveType(ScorpionStateTypeName), name);
        }

        static object InvokeMethod(object target, string methodName, params object[] parameters)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Missing method '{methodName}'.");
            return method.Invoke(target, parameters);
        }

        static T GetFieldValue<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field '{fieldName}'.");
            return (T)field.GetValue(target);
        }

        static void SetFieldValue(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field '{fieldName}'.");
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
