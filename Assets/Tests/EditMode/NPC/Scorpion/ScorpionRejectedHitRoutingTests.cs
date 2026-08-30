using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Beavermania.Tests.EditMode.NPC.Scorpion
{
    /// <summary>
    /// Guards the caller contract that a rejected advanced-boss hit is still owned by the damage receiver,
    /// so melee and projectile callers never fall through to the legacy tag path and hit the same boss twice.
    /// </summary>
    public sealed class ScorpionRejectedHitRoutingTests
    {
        const string AnimatedAttackTypeName = "Beavermania.Player.Combat.AnimatedAttack";
        const string DamageTypeName = "Beavermania.NPC.EnemyDamageType";
        const string ProjectileTypeName = "Beavermania.Player.Combat.Projectile";
        const string ScorpionTypeName = "Beavermania.NPC.ScorpionScript";
        const string StatsTypeName = "Beavermania.Data.NPC.ScorpionStatsData";
        const string VulnerabilityWindowFieldName = "vulnerabilityWindowRemaining";
        const int LegacyScorpionComboBonus = 3;
        const int HitDamage = 10;

        GameObject scorpionObject;
        GameObject attackObject;
        Component scorpion;
        Component animatedAttack;
        ScriptableObject stats;

        [SetUp]
        public void SetUp()
        {
            scorpionObject = new GameObject("ScorpionRejectedHitRoutingBoss");
            scorpionObject.SetActive(false);
            Rigidbody rigidbody = scorpionObject.AddComponent<Rigidbody>();
            scorpion = scorpionObject.AddComponent(ResolveType(ScorpionTypeName));
            stats = ScriptableObject.CreateInstance(ResolveType(StatsTypeName));

            SetFieldValue(stats, "advancedAiEnabled", true);
            SetFieldValue(stats, "comboLimit", 15);
            SetFieldValue(scorpion, "statsData", stats);
            SetFieldValue(scorpion, "rbScorpion", rigidbody);
            SetFieldValue(scorpion, "CurrentHealth", 100);
            SetFieldValue(scorpion, "combo", 0);
            SetFieldValue(scorpion, "rotGoal", Quaternion.identity);

            attackObject = new GameObject("ScorpionRejectedHitRoutingAttacker");
            attackObject.SetActive(false);
            animatedAttack = attackObject.AddComponent(ResolveType(AnimatedAttackTypeName));
        }

        [TearDown]
        public void TearDown()
        {
            if (stats != null)
                UnityEngine.Object.DestroyImmediate(stats);
            if (attackObject != null)
                UnityEngine.Object.DestroyImmediate(attackObject);
            if (scorpionObject != null)
                UnityEngine.Object.DestroyImmediate(scorpionObject);
        }

        [Test]
        public void TryRouteInterfaceDamage_AdvancedBossOutsideWindow_ConsumesHitWithoutFallbackDamage()
        {
            // Act
            bool handled = (bool)InvokeMethod(animatedAttack, "TryRouteInterfaceDamage", scorpion, HitDamage);

            // Assert
            Assert.That(handled, Is.True, "A rejected boss hit must still be reported as handled.");
            Assert.That(GetFieldValue<int>(scorpion, "CurrentHealth"), Is.EqualTo(100));
            Assert.That(GetFieldValue<int>(scorpion, "combo"), Is.EqualTo(1));
        }

        [Test]
        public void TryRouteInterfaceDamage_AdvancedBossInsideWindow_AppliesHealthDamage()
        {
            // Arrange
            SetFieldValue(scorpion, VulnerabilityWindowFieldName, 0.5f);

            // Act
            bool handled = (bool)InvokeMethod(animatedAttack, "TryRouteInterfaceDamage", scorpion, HitDamage);

            // Assert
            Assert.That(handled, Is.True);
            Assert.That(GetFieldValue<int>(scorpion, "CurrentHealth"), Is.EqualTo(90));
            Assert.That(GetFieldValue<int>(scorpion, "combo"), Is.EqualTo(1));
        }

        [Test]
        public void TryRouteInterfaceDamage_NoReceiver_LeavesLegacyFallbackAvailable()
        {
            // Act
            bool handled = (bool)InvokeMethod(animatedAttack, "TryRouteInterfaceDamage", null, HitDamage);

            // Assert
            Assert.That(handled, Is.False);
        }

        [Test]
        public void TryRouteDamageToReceiver_AdvancedBossOutsideWindow_ConsumesHitWithoutLegacyComboBonus()
        {
            // Act
            bool handled = RouteProjectileDamage("Normal");

            // Assert
            Assert.That(handled, Is.True, "A rejected boss hit must still be reported as handled.");
            Assert.That(GetFieldValue<int>(scorpion, "CurrentHealth"), Is.EqualTo(100));
            Assert.That(GetFieldValue<int>(scorpion, "combo"), Is.EqualTo(1));
        }

        [Test]
        public void TryRouteDamageToReceiver_AdvancedBossInsideWindow_AppliesLegacyComboBonus()
        {
            // Arrange
            SetFieldValue(scorpion, VulnerabilityWindowFieldName, 0.5f);

            // Act
            bool handled = RouteProjectileDamage("Normal");

            // Assert
            Assert.That(handled, Is.True);
            Assert.That(GetFieldValue<int>(scorpion, "CurrentHealth"), Is.EqualTo(90));
            Assert.That(GetFieldValue<int>(scorpion, "combo"), Is.EqualTo(1 + LegacyScorpionComboBonus));
        }

        [Test]
        public void TryRouteDamageToReceiver_NoReceiver_LeavesLegacyFallbackAvailable()
        {
            // Act
            bool handled = (bool)InvokeStaticMethod(
                ResolveType(ProjectileTypeName),
                "TryRouteDamageToReceiver",
                null,
                HitDamage,
                CreateDamageType("Normal"),
                null,
                LegacyScorpionComboBonus);

            // Assert
            Assert.That(handled, Is.False);
        }

        bool RouteProjectileDamage(string damageTypeName)
        {
            return (bool)InvokeStaticMethod(
                ResolveType(ProjectileTypeName),
                "TryRouteDamageToReceiver",
                scorpion,
                HitDamage,
                CreateDamageType(damageTypeName),
                null,
                LegacyScorpionComboBonus);
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

            Assert.That(method, Is.Not.Null, $"Missing method {methodName}.");
            return method.Invoke(target, parameters);
        }

        static object InvokeStaticMethod(Type type, string methodName, params object[] parameters)
        {
            MethodInfo method = type.GetMethod(
                methodName,
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null, $"Missing static method {methodName}.");
            return method.Invoke(null, parameters);
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
