using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Beavermania.Tests.EditMode.NPC.Scorpion
{
    public sealed class ScorpionComboRewardTests
    {
        const string ScorpionTypeName = "Beavermania.NPC.ScorpionScript";
        const string DamageTypeName = "Beavermania.NPC.EnemyDamageType";
        const string StatsTypeName = "Beavermania.Data.NPC.ScorpionStatsData";
        const int ConfiguredMaxHealth = 100;

        GameObject scorpionObject;
        Component scorpion;
        ScriptableObject stats;

        [SetUp]
        public void SetUp()
        {
            scorpionObject = new GameObject("ScorpionComboRewardTest");
            scorpionObject.SetActive(false);
            scorpionObject.AddComponent<Rigidbody>();
            scorpion = scorpionObject.AddComponent(ResolveType(ScorpionTypeName));

            // Owned stats instead of the legacy fallback: the fallback path creates a HideAndDontSave
            // ScriptableObject the fixture cannot destroy and logs a missing-reference warning.
            stats = ScriptableObject.CreateInstance(ResolveType(StatsTypeName));
            SetFieldValue(stats, "advancedAiEnabled", false);
            SetFieldValue(stats, "maxHealth", ConfiguredMaxHealth);
            SetFieldValue(scorpion, "statsData", stats);
            SetFieldValue(scorpion, "CurrentHealth", ConfiguredMaxHealth);
            SetFieldValue(scorpion, "combo", 0);
            SetFieldValue(scorpion, "rotGoal", Quaternion.identity);
        }

        [TearDown]
        public void TearDown()
        {
            if (stats != null)
                UnityEngine.Object.DestroyImmediate(stats);
            if (scorpionObject != null)
                UnityEngine.Object.DestroyImmediate(scorpionObject);
        }

        [Test]
        public void ReceiveDamage_AcceptedNormalHit_AwardsOneCombo()
        {
            // Arrange
            object normalDamage = CreateDamageType("Normal");

            // Act
            bool accepted = (bool)InvokeMethod(scorpion, "ReceiveDamage", 10, normalDamage, null);

            // Assert
            Assert.That(accepted, Is.True);
            Assert.That(GetFieldValue<int>(scorpion, "combo"), Is.EqualTo(1));
        }

        [Test]
        public void ReceiveCounterHit_AcceptedParryCounter_AwardsTwoCombo()
        {
            // Arrange
            object lightDamage = CreateDamageType("Light");

            // Act
            bool accepted = (bool)InvokeMethod(scorpion, "ReceiveCounterHit", 10, lightDamage, null);

            // Assert
            Assert.That(accepted, Is.True);
            Assert.That(GetFieldValue<int>(scorpion, "combo"), Is.EqualTo(2));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void ReceiveCounterHit_RejectedDamage_AwardsNoCombo(int damage)
        {
            // Arrange
            object lightDamage = CreateDamageType("Light");

            // Act
            bool accepted = (bool)InvokeMethod(scorpion, "ReceiveCounterHit", damage, lightDamage, null);

            // Assert
            Assert.That(accepted, Is.False);
            Assert.That(GetFieldValue<int>(scorpion, "combo"), Is.Zero);
        }

        [TestCase("ReceiveDamage", "Normal")]
        [TestCase("ReceiveCounterHit", "Light")]
        public void DamageEntryPoint_DeadScorpion_RejectsDamageWithoutChangingCombo(
            string methodName,
            string damageTypeName)
        {
            // Arrange
            const int ExistingCombo = 3;
            SetFieldValue(scorpion, "CurrentHealth", 0);
            SetFieldValue(scorpion, "combo", ExistingCombo);
            object damageType = CreateDamageType(damageTypeName);

            // Act
            bool accepted = (bool)InvokeMethod(scorpion, methodName, 10, damageType, null);

            // Assert
            Assert.That(accepted, Is.False);
            Assert.That(GetFieldValue<int>(scorpion, "combo"), Is.EqualTo(ExistingCombo));
        }

        static object CreateDamageType(string name)
        {
            return Enum.Parse(ResolveType(DamageTypeName), name);
        }

        static object InvokeMethod(object target, string methodName, params object[] parameters)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (method == null)
                throw new MissingMethodException(target.GetType().FullName, methodName);

            return method.Invoke(target, parameters);
        }

        static T GetFieldValue<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (field == null)
                throw new MissingFieldException(target.GetType().FullName, fieldName);

            return (T)field.GetValue(target);
        }

        static void SetFieldValue(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (field == null)
                throw new MissingFieldException(target.GetType().FullName, fieldName);

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
