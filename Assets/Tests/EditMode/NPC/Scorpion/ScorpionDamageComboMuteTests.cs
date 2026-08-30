using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Beavermania.Tests.EditMode.NPC.Scorpion
{
    public sealed class ScorpionDamageComboMuteTests
    {
        const string ScorpionDamageTypeName = "Beavermania.NPC.ScorpionDamage";
        const string ScorpionTypeName = "Beavermania.NPC.ScorpionScript";
        const string StatsTypeName = "Beavermania.Data.NPC.ScorpionStatsData";
        const int ComboLimit = 3;

        GameObject scorpionObject;
        GameObject damageObject;
        Component scorpion;
        Component scorpionDamage;
        ScriptableObject stats;

        [SetUp]
        public void SetUp()
        {
            scorpionObject = new GameObject("ScorpionDamageComboMuteScorpion");
            scorpionObject.SetActive(false);
            scorpionObject.AddComponent<Rigidbody>();
            scorpion = scorpionObject.AddComponent(ResolveType(ScorpionTypeName));
            stats = ScriptableObject.CreateInstance(ResolveType(StatsTypeName));

            SetFieldValue(stats, "comboLimit", ComboLimit);
            SetFieldValue(scorpion, "statsData", stats);

            damageObject = new GameObject("ScorpionDamageComboMuteReceiver");
            damageObject.SetActive(false);
            scorpionDamage = damageObject.AddComponent(ResolveType(ScorpionDamageTypeName));
            SetFieldValue(scorpionDamage, "Scorpion", scorpion);
        }

        [TearDown]
        public void TearDown()
        {
            if (stats != null)
                UnityEngine.Object.DestroyImmediate(stats);
            if (damageObject != null)
                UnityEngine.Object.DestroyImmediate(damageObject);
            if (scorpionObject != null)
                UnityEngine.Object.DestroyImmediate(scorpionObject);
        }

        [Test]
        public void IsContactDamageMuted_OrdinaryScorpionAtComboLimit_MutesContactDamage()
        {
            // Arrange
            SetFieldValue(stats, "advancedAiEnabled", false);
            SetFieldValue(scorpion, "combo", ComboLimit);

            // Act
            bool muted = (bool)InvokeMethod(scorpionDamage, "IsContactDamageMuted");

            // Assert
            Assert.That(muted, Is.True);
        }

        [Test]
        public void IsContactDamageMuted_OrdinaryScorpionBelowComboLimit_AllowsContactDamage()
        {
            // Arrange
            SetFieldValue(stats, "advancedAiEnabled", false);
            SetFieldValue(scorpion, "combo", ComboLimit - 1);

            // Act
            bool muted = (bool)InvokeMethod(scorpionDamage, "IsContactDamageMuted");

            // Assert
            Assert.That(muted, Is.False);
        }

        [Test]
        public void IsContactDamageMuted_AdvancedBossAtComboLimit_AllowsContactDamage()
        {
            // Arrange
            SetFieldValue(stats, "advancedAiEnabled", true);
            SetFieldValue(scorpion, "combo", ComboLimit);

            // Act
            bool muted = (bool)InvokeMethod(scorpionDamage, "IsContactDamageMuted");

            // Assert
            Assert.That(muted, Is.False);
        }

        static object InvokeMethod(object target, string methodName, params object[] parameters)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null, $"Missing method {methodName}.");
            return method.Invoke(target, parameters);
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
