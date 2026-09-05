using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Beavermania.Tests.EditMode.NPC.Scorpion
{
    public sealed class ScorpionAttackContextRoutingTests
    {
        const string AnimatedAttackTypeName = "Beavermania.Player.Combat.AnimatedAttack";
        const string BoostChargeSettingsTypeName = "Beavermania.Data.Combat.BoostChargeSettings";
        const string BoostChargeTypeName = "Beavermania.Player.Combat.BoostChargeController";
        const string DamageTypeName = "Beavermania.NPC.EnemyDamageType";
        const string PlayerAttackKindTypeName = "Beavermania.Player.Combat.PlayerAttackKind";
        const string ProjectileTypeName = "Beavermania.Player.Combat.Projectile";
        const string ScorpionTypeName = "Beavermania.NPC.ScorpionScript";
        const string StatsTypeName = "Beavermania.Data.NPC.ScorpionStatsData";
        const int LegacyScorpionComboBonus = 3;
        const int HitDamage = 10;

        GameObject scorpionObject;
        GameObject attackObject;
        GameObject boostObject;
        Component scorpion;
        Component animatedAttack;
        Component boostCharge;
        ScriptableObject stats;
        ScriptableObject boostSettings;

        [SetUp]
        public void SetUp()
        {
            scorpionObject = new GameObject("ScorpionAttackContextBoss");
            scorpionObject.SetActive(false);
            Rigidbody rigidbody = scorpionObject.AddComponent<Rigidbody>();
            scorpion = scorpionObject.AddComponent(ResolveType(ScorpionTypeName));
            stats = ScriptableObject.CreateInstance(ResolveType(StatsTypeName));
            SetFieldValue(stats, "advancedAiEnabled", true);
            SetFieldValue(stats, "maxHealth", 100);
            SetFieldValue(scorpion, "statsData", stats);
            SetFieldValue(scorpion, "rbScorpion", rigidbody);
            SetFieldValue(scorpion, "CurrentHealth", 100);
            SetFieldValue(scorpion, "rotGoal", Quaternion.identity);

            boostObject = new GameObject("ScorpionAttackContextBoost");
            boostObject.SetActive(false);
            boostCharge = boostObject.AddComponent(ResolveType(BoostChargeTypeName));
            boostSettings = ScriptableObject.CreateInstance(ResolveType(BoostChargeSettingsTypeName));
            SetFieldValue(boostSettings, "chargePerHit", 8f);
            SetFieldValue(boostSettings, "comboHitsForBonus", 99);
            SetFieldValue(boostCharge, "settings", boostSettings);
            SetFieldValue(scorpion, "boostCharge", boostCharge);

            attackObject = new GameObject("ScorpionAttackContextAttacker");
            attackObject.SetActive(false);
            animatedAttack = attackObject.AddComponent(ResolveType(AnimatedAttackTypeName));
        }

        [TearDown]
        public void TearDown()
        {
            if (boostSettings != null)
                UnityEngine.Object.DestroyImmediate(boostSettings);
            if (boostObject != null)
                UnityEngine.Object.DestroyImmediate(boostObject);
            if (stats != null)
                UnityEngine.Object.DestroyImmediate(stats);
            if (attackObject != null)
                UnityEngine.Object.DestroyImmediate(attackObject);
            if (scorpionObject != null)
                UnityEngine.Object.DestroyImmediate(scorpionObject);
        }

        [Test]
        public void AnimatedAttack_RoutesExplicitSwordSwingContext()
        {
            // Arrange
            object swordSwing = CreateEnum(PlayerAttackKindTypeName, "SwordSwing");

            // Act
            bool handled = (bool)InvokeMethod(animatedAttack, "TryRouteInterfaceDamage", scorpion, HitDamage, swordSwing);

            // Assert
            Assert.That(handled, Is.True);
            Assert.That(GetFieldValue<int>(scorpion, "CurrentHealth"), Is.EqualTo(70));
        }

        [Test]
        public void Projectile_RoutesArrowContextAndRegistersBoostOnce()
        {
            // Arrange
            object arrow = CreateEnum(PlayerAttackKindTypeName, "Arrow");
            object normalDamage = CreateEnum(DamageTypeName, "Normal");

            // Act
            bool handled = (bool)InvokeStaticMethod(
                ResolveType(ProjectileTypeName),
                "TryRouteDamageToReceiver",
                scorpion,
                HitDamage,
                arrow,
                normalDamage,
                null,
                LegacyScorpionComboBonus);

            // Assert
            Assert.That(handled, Is.True);
            Assert.That(GetFieldValue<int>(scorpion, "CurrentHealth"), Is.EqualTo(80));
            Assert.That(GetFieldValue<int>(scorpion, "combo"), Is.EqualTo(1 + LegacyScorpionComboBonus));
            Assert.That(GetFieldValue<float>(boostCharge, "currentCharge"), Is.EqualTo(8f).Within(0.0001f));
        }

        [Test]
        public void Projectile_UnspecifiedStoneContextDamagesWithoutStunningAdvancedBoss()
        {
            // Arrange
            SetFieldValue(stats, "comboLimit", 1);
            object unspecified = CreateEnum(PlayerAttackKindTypeName, "Unspecified");

            // Act
            InvokeStaticMethod(
                ResolveType(ProjectileTypeName),
                "TryRouteDamageToReceiver",
                scorpion,
                HitDamage,
                unspecified,
                CreateEnum(DamageTypeName, "Normal"),
                null,
                LegacyScorpionComboBonus);
            InvokeMethod(scorpion, "FixedUpdate");

            // Assert
            Assert.That(GetFieldValue<int>(scorpion, "CurrentHealth"), Is.EqualTo(90));
            Assert.That(GetFieldValue<object>(scorpion, "currentState").ToString(), Is.Not.EqualTo("Stunned"));
        }

        [Test]
        public void RoutingMethods_NullReceiverLeaveLegacyFallbackAvailable()
        {
            // Arrange
            object unspecified = CreateEnum(PlayerAttackKindTypeName, "Unspecified");

            // Act
            bool meleeHandled = (bool)InvokeMethod(
                animatedAttack,
                "TryRouteInterfaceDamage",
                null,
                HitDamage,
                unspecified);
            bool projectileHandled = (bool)InvokeStaticMethod(
                ResolveType(ProjectileTypeName),
                "TryRouteDamageToReceiver",
                null,
                HitDamage,
                unspecified,
                CreateEnum(DamageTypeName, "Normal"),
                null,
                LegacyScorpionComboBonus);

            // Assert
            Assert.That(meleeHandled, Is.False);
            Assert.That(projectileHandled, Is.False);
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
