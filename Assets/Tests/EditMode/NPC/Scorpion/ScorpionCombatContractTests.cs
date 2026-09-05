using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Beavermania.Tests.EditMode.NPC.Scorpion
{
    public sealed class ScorpionCombatContractTests
    {
        const string DamageRulesTypeName = "Beavermania.Player.Combat.PlayerAttackDamageRules";
        const string DamageTypeName = "Beavermania.NPC.EnemyDamageType";
        const string PlayerAttackKindTypeName = "Beavermania.Player.Combat.PlayerAttackKind";
        const string ScorpionStateTypeName = "Beavermania.NPC.ScorpionState";
        const string ScorpionTypeName = "Beavermania.NPC.ScorpionScript";
        const string StatsTypeName = "Beavermania.Data.NPC.ScorpionStatsData";
        const int MaxHealth = 1000;

        GameObject scorpionObject;
        GameObject sourceObject;
        Component scorpion;
        ScriptableObject stats;

        [SetUp]
        public void SetUp()
        {
            scorpionObject = new GameObject("ScorpionCombatContractBoss");
            scorpionObject.SetActive(false);
            Rigidbody rigidbody = scorpionObject.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            scorpion = scorpionObject.AddComponent(ResolveType(ScorpionTypeName));
            stats = ScriptableObject.CreateInstance(ResolveType(StatsTypeName));

            SetFieldValue(stats, "advancedAiEnabled", true);
            SetFieldValue(stats, "maxHealth", MaxHealth);
            SetFieldValue(stats, "bossStunDuration", 3.5f);
            SetFieldValue(stats, "bossStunCooldown", 10f);
            SetFieldValue(stats, "stunnedDamageMultiplier", 2f);
            SetFieldValue(stats, "hurricaneKickRetreatSpeed", 12f);
            SetFieldValue(stats, "hurricaneKickRetreatDuration", 0.6f);
            SetFieldValue(scorpion, "statsData", stats);
            SetFieldValue(scorpion, "rbScorpion", rigidbody);
            SetFieldValue(scorpion, "CurrentHealth", MaxHealth);
            SetFieldValue(scorpion, "rotGoal", Quaternion.identity);

            sourceObject = new GameObject("ScorpionCombatContractSource");
            sourceObject.transform.position = Vector3.back;
        }

        [TearDown]
        public void TearDown()
        {
            if (sourceObject != null)
                UnityEngine.Object.DestroyImmediate(sourceObject);
            if (stats != null)
                UnityEngine.Object.DestroyImmediate(stats);
            if (scorpionObject != null)
                UnityEngine.Object.DestroyImmediate(scorpionObject);
        }

        [TestCase("Unspecified", 1f)]
        [TestCase("BareHands", 1f)]
        [TestCase("HurricaneKick", 0.8f)]
        [TestCase("Arrow", 2f)]
        [TestCase("SwordSwing", 3f)]
        [TestCase("HurricaneSword", 1.5f)]
        public void GetAttackMultiplier_ReturnsApprovedValue(string attackKindName, float expected)
        {
            // Arrange
            object attackKind = CreateEnum(PlayerAttackKindTypeName, attackKindName);

            // Act
            float actual = (float)InvokeStaticMethod(
                ResolveType(DamageRulesTypeName),
                "GetAttackMultiplier",
                attackKind);

            // Assert
            Assert.That(actual, Is.EqualTo(expected).Within(0.0001f));
        }

        [Test]
        public void ResolveDamage_AppliesAttackThenStunnedMultiplierBeforeRounding()
        {
            // Arrange
            object hurricaneKick = CreateEnum(PlayerAttackKindTypeName, "HurricaneKick");

            // Act
            int resolved = (int)InvokeStaticMethod(
                ResolveType(DamageRulesTypeName),
                "ResolveDamage",
                11,
                hurricaneKick,
                true,
                1.5f);

            // Assert
            Assert.That(resolved, Is.EqualTo(13), "11 x 0.8 x 1.5 = 13.2 and rounds once at the end.");
        }

        [TestCase("Idle")]
        [TestCase("Attack")]
        [TestCase("Charge")]
        [TestCase("Reverse")]
        [TestCase("Look")]
        public void ReceiveDamage_AdvancedBossInNormalState_IsAlwaysDamageable(string stateName)
        {
            // Arrange
            SetState(stateName);

            // Act
            bool accepted = (bool)InvokeMethod(
                scorpion,
                "ReceiveDamage",
                10,
                CreateEnum(DamageTypeName, "Normal"),
                null);

            // Assert
            Assert.That(accepted, Is.True);
            Assert.That(GetFieldValue<int>(scorpion, "CurrentHealth"), Is.EqualTo(MaxHealth - 10));
        }

        [TestCase("BareHands", 10)]
        [TestCase("HurricaneKick", 8)]
        [TestCase("Arrow", 20)]
        [TestCase("SwordSwing", 30)]
        [TestCase("HurricaneSword", 15)]
        public void ReceivePlayerAttack_UsesTypedMultiplier(string attackKindName, int expectedDamage)
        {
            // Arrange
            SetState("Attack");

            // Act
            bool accepted = ReceivePlayerAttack(10, attackKindName);

            // Assert
            Assert.That(accepted, Is.True);
            Assert.That(GetFieldValue<int>(scorpion, "CurrentHealth"), Is.EqualTo(MaxHealth - expectedDamage));
        }

        [Test]
        public void ReceivePlayerAttack_WhileStunned_AppliesConfiguredStunnedMultiplier()
        {
            // Arrange
            SetState("Stunned");

            // Act
            ReceivePlayerAttack(10, "SwordSwing");

            // Assert
            Assert.That(GetFieldValue<int>(scorpion, "CurrentHealth"), Is.EqualTo(MaxHealth - 60));
        }

        [Test]
        public void HurricaneKick_SecondHitDuringForcedReverse_DoesNotExtendTimer()
        {
            // Arrange
            SetState("Attack");
            ReceivePlayerAttack(10, "HurricaneKick");
            SetFieldValue(scorpion, "stateTimer", 0.25f);

            // Act
            ReceivePlayerAttack(10, "HurricaneKick");

            // Assert
            Assert.That(GetStateName(), Is.EqualTo("Reverse"));
            Assert.That(GetFieldValue<bool>(scorpion, "hurricaneKickRetreatActive"), Is.True);
            Assert.That(GetFieldValue<float>(scorpion, "stateTimer"), Is.EqualTo(0.25f).Within(0.0001f));
        }

        [Test]
        public void HurricaneKick_StartsCoveringReverseAtConfiguredSpeedAndDuration()
        {
            // Arrange
            SetState("Attack");

            // Act
            ReceivePlayerAttack(10, "HurricaneKick");
            float initialTimer = GetFieldValue<float>(scorpion, "stateTimer");
            InvokeMethod(scorpion, "TickReverse");

            // Assert
            Assert.That(GetStateName(), Is.EqualTo("Reverse"));
            Assert.That(initialTimer, Is.EqualTo(0.6f).Within(0.0001f));
            Assert.That(GetFieldValue<Rigidbody>(scorpion, "rbScorpion").velocity.z, Is.EqualTo(12f).Within(0.0001f));
            Assert.That(GetFieldValue<bool>(scorpion, "isAttacking"), Is.True);
        }

        [Test]
        public void HurricaneKick_WhileStunned_DamagesWithoutRetreating()
        {
            // Arrange
            SetState("Stunned");

            // Act
            ReceivePlayerAttack(10, "HurricaneKick");

            // Assert
            Assert.That(GetStateName(), Is.EqualTo("Stunned"));
            Assert.That(GetFieldValue<bool>(scorpion, "hurricaneKickRetreatActive"), Is.False);
        }

        [Test]
        public void HurricaneKick_WhileDead_IsRejectedWithoutRetreating()
        {
            // Arrange
            SetState("Dead");
            SetFieldValue(scorpion, "CurrentHealth", 0);
            SetFieldValue(scorpion, "deathHandled", true);

            // Act
            bool accepted = ReceivePlayerAttack(10, "HurricaneKick");

            // Assert
            Assert.That(accepted, Is.False);
            Assert.That(GetStateName(), Is.EqualTo("Dead"));
            Assert.That(GetFieldValue<bool>(scorpion, "hurricaneKickRetreatActive"), Is.False);
        }

        [Test]
        public void StunRecovery_StartsCooldownOnlyAfterRecoveredStateEnds()
        {
            // Arrange
            SetState("Attack");
            InvokeMethod(scorpion, "EnterState", CreateEnum(ScorpionStateTypeName, "Stunned"));
            Assert.That(GetFieldValue<float>(scorpion, "stateTimer"), Is.EqualTo(3.5f).Within(0.0001f));
            SetFieldValue(scorpion, "stateTimer", 0f);

            // Act
            InvokeMethod(scorpion, "TickStunned");
            float cooldownDuringRecovery = GetFieldValue<float>(scorpion, "stunCooldownRemaining");
            InvokeMethod(scorpion, "TickRecovered");

            // Assert
            Assert.That(cooldownDuringRecovery, Is.Zero);
            Assert.That(GetFieldValue<float>(scorpion, "stunCooldownRemaining"), Is.EqualTo(10f).Within(0.0001f));
        }

        [Test]
        public void OrdinaryScorpion_ComboLimitStillEntersStunned()
        {
            // Arrange
            SetFieldValue(stats, "advancedAiEnabled", false);
            SetFieldValue(stats, "comboLimit", 2);
            SetState("Attack");
            SetFieldValue(scorpion, "combo", 2);

            // Act
            InvokeMethod(scorpion, "FixedUpdate");

            // Assert
            Assert.That(GetStateName(), Is.EqualTo("Stunned"));
        }

        [Test]
        public void AdvancedBoss_OrdinaryHitAtComboLimitNeverStuns()
        {
            // Arrange
            SetFieldValue(stats, "comboLimit", 1);
            SetState("Attack");

            // Act
            InvokeMethod(scorpion, "ReceiveDamage", 10, CreateEnum(DamageTypeName, "Normal"), null);
            InvokeMethod(scorpion, "FixedUpdate");

            // Assert
            Assert.That(GetStateName(), Is.Not.EqualTo("Stunned"));
        }

        bool ReceivePlayerAttack(int damage, string attackKindName)
        {
            return (bool)InvokeMethod(
                scorpion,
                "ReceivePlayerAttack",
                damage,
                CreateEnum(PlayerAttackKindTypeName, attackKindName),
                CreateEnum(DamageTypeName, "Normal"),
                sourceObject.transform);
        }

        void SetState(string stateName)
        {
            SetFieldValue(scorpion, "currentState", CreateEnum(ScorpionStateTypeName, stateName));
            SetFieldValue(scorpion, "state", stateName);
        }

        string GetStateName()
        {
            return GetFieldValue<object>(scorpion, "currentState").ToString();
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
