using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Beavermania.Tests.EditMode.NPC.Scorpion
{
    public sealed class ScorpionCombatContractTests
    {
        const string BoostChargeSettingsTypeName = "Beavermania.Data.Combat.BoostChargeSettings";
        const string BoostChargeTypeName = "Beavermania.Player.Combat.BoostChargeController";
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
        ScriptableObject boostSettings;

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

            // Keep accepted hits from resolving and charging a player in the open scene.
            Component boostCharge = scorpionObject.AddComponent(ResolveType(BoostChargeTypeName));
            boostSettings = ScriptableObject.CreateInstance(ResolveType(BoostChargeSettingsTypeName));
            SetFieldValue(boostCharge, "settings", boostSettings);
            SetFieldValue(scorpion, "boostCharge", boostCharge);

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
            if (boostSettings != null)
                UnityEngine.Object.DestroyImmediate(boostSettings);
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

        [TestCase("BareHands")]
        [TestCase("HurricaneKick")]
        [TestCase("Arrow")]
        [TestCase("SwordSwing")]
        [TestCase("HurricaneSword")]
        public void ReceivePlayerAttack_NormalScorpion_IgnoresTypedMultiplier(string attackKindName)
        {
            // Arrange
            SetFieldValue(stats, "advancedAiEnabled", false);
            SetState("Attack");

            // Act
            bool accepted = ReceivePlayerAttack(10, attackKindName);

            // Assert
            Assert.That(accepted, Is.True);
            Assert.That(GetFieldValue<int>(scorpion, "CurrentHealth"), Is.EqualTo(MaxHealth - 10));
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

        [TestCase("HurricaneKick", "HurricaneKick")]
        [TestCase("HurricaneKick", "HurricaneSword")]
        [TestCase("HurricaneSword", "HurricaneKick")]
        [TestCase("HurricaneSword", "HurricaneSword")]
        public void HurricaneAttack_AdditionalHitDuringForcedReverse_DoesNotExtendOrRedirect(
            string firstAttackKindName,
            string secondAttackKindName)
        {
            // Arrange
            SetState("Attack");
            ReceivePlayerAttack(10, firstAttackKindName);
            Vector3 originalDirection = GetFieldValue<Vector3>(scorpion, "hurricaneRetreatDirection");
            Quaternion originalFacing = GetFieldValue<Quaternion>(scorpion, "rotGoal");
            SetFieldValue(scorpion, "stateTimer", 0.25f);
            sourceObject.transform.position = Vector3.right;

            // Act
            bool accepted = ReceivePlayerAttack(10, secondAttackKindName);

            // Assert
            Assert.That(accepted, Is.True);
            Assert.That(GetStateName(), Is.EqualTo("Reverse"));
            Assert.That(GetFieldValue<bool>(scorpion, "hurricaneRetreatActive"), Is.True);
            Assert.That(GetFieldValue<float>(scorpion, "stateTimer"), Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(Vector3.Distance(GetFieldValue<Vector3>(scorpion, "hurricaneRetreatDirection"), originalDirection),
                Is.LessThan(0.0001f));
            Assert.That(Quaternion.Angle(GetFieldValue<Quaternion>(scorpion, "rotGoal"), originalFacing),
                Is.LessThan(0.0001f));
        }

        [TestCase("HurricaneKick", "Attack")]
        [TestCase("HurricaneSword", "Attack")]
        [TestCase("HurricaneKick", "Reverse")]
        [TestCase("HurricaneSword", "Reverse")]
        public void HurricaneAttack_StartsCoveringReverseImmediatelyAndKeepsConfiguredSpeed(
            string attackKindName,
            string initialStateName)
        {
            // Arrange
            SetState(initialStateName);
            Rigidbody rigidbody = GetFieldValue<Rigidbody>(scorpion, "rbScorpion");

            // Act
            ReceivePlayerAttack(10, attackKindName);
            float initialTimer = GetFieldValue<float>(scorpion, "stateTimer");
            Vector3 immediateVelocity = rigidbody.velocity;
            bool immediatelyAttacking = GetFieldValue<bool>(scorpion, "isAttacking");
            Vector3 coveringFacing = GetFieldValue<Quaternion>(scorpion, "rotGoal") * Vector3.forward;
            InvokeMethod(scorpion, "TickReverse");

            // Assert
            Assert.That(GetStateName(), Is.EqualTo("Reverse"));
            Assert.That(initialTimer, Is.EqualTo(0.6f).Within(0.0001f));
            Assert.That(Vector3.Distance(immediateVelocity, Vector3.forward * 12f), Is.LessThan(0.0001f));
            Assert.That(immediatelyAttacking, Is.True, "Covering-claw damage must start on the accepted hit, before the next tick.");
            Assert.That(Vector3.Dot(coveringFacing, immediateVelocity.normalized), Is.EqualTo(-1f).Within(0.0001f),
                "The claws must face the attacker while the boss moves backwards, including hits from behind.");
            Assert.That(Vector3.Distance(rigidbody.velocity, immediateVelocity), Is.LessThan(0.0001f));
            Assert.That(GetFieldValue<bool>(scorpion, "isAttacking"), Is.True);
            Assert.That(GetFieldValue<float>(scorpion, "stateTimer"),
                Is.EqualTo(0.6f - Time.fixedDeltaTime).Within(0.0001f));
        }

        [TestCase("HurricaneKick")]
        [TestCase("HurricaneSword")]
        public void HurricaneAttack_RetreatExpires_StopsMotionAndCoveringClawDamage(string attackKindName)
        {
            // Arrange
            SetState("Attack");
            ReceivePlayerAttack(10, attackKindName);
            SetFieldValue(scorpion, "targetTransform", null);
            SetFieldValue(scorpion, "stateTimer", Time.fixedDeltaTime);

            // Act
            InvokeMethod(scorpion, "TickReverse");

            // Assert
            Assert.That(GetStateName(), Is.EqualTo("Idle"));
            Assert.That(GetFieldValue<bool>(scorpion, "hurricaneRetreatActive"), Is.False);
            Assert.That(GetFieldValue<bool>(scorpion, "isAttacking"), Is.False);
            Assert.That(GetFieldValue<Rigidbody>(scorpion, "rbScorpion").velocity.sqrMagnitude,
                Is.LessThan(0.0001f));
        }

        [TestCase("Unspecified")]
        [TestCase("BareHands")]
        [TestCase("Arrow")]
        [TestCase("SwordSwing")]
        public void OrdinaryAttack_DamagesWithoutForcingRetreat(string attackKindName)
        {
            // Arrange
            SetState("Attack");

            // Act
            bool accepted = ReceivePlayerAttack(10, attackKindName);

            // Assert
            Assert.That(accepted, Is.True);
            Assert.That(GetStateName(), Is.EqualTo("Attack"));
            Assert.That(GetFieldValue<bool>(scorpion, "hurricaneRetreatActive"), Is.False);
        }

        [TestCase("HurricaneKick")]
        [TestCase("HurricaneSword")]
        public void HurricaneAttack_NormalScorpion_DoesNotForceRetreat(string attackKindName)
        {
            // Arrange
            SetFieldValue(stats, "advancedAiEnabled", false);
            SetState("Attack");

            // Act
            bool accepted = ReceivePlayerAttack(10, attackKindName);

            // Assert
            Assert.That(accepted, Is.True);
            Assert.That(GetStateName(), Is.EqualTo("Attack"));
            Assert.That(GetFieldValue<bool>(scorpion, "hurricaneRetreatActive"), Is.False);
        }

        [TestCase("HurricaneKick", 16)]
        [TestCase("HurricaneSword", 30)]
        public void HurricaneAttack_WhileStunned_DamagesWithoutRetreating(string attackKindName, int expectedDamage)
        {
            // Arrange
            SetState("Stunned");

            // Act
            bool accepted = ReceivePlayerAttack(10, attackKindName);

            // Assert
            Assert.That(accepted, Is.True);
            Assert.That(GetFieldValue<int>(scorpion, "CurrentHealth"), Is.EqualTo(MaxHealth - expectedDamage));
            Assert.That(GetStateName(), Is.EqualTo("Stunned"));
            Assert.That(GetFieldValue<bool>(scorpion, "hurricaneRetreatActive"), Is.False);
        }

        [TestCase("HurricaneKick")]
        [TestCase("HurricaneSword")]
        public void HurricaneAttack_WhileDead_IsRejectedWithoutRetreating(string attackKindName)
        {
            // Arrange
            SetState("Dead");
            SetFieldValue(scorpion, "CurrentHealth", 0);
            SetFieldValue(scorpion, "deathHandled", true);

            // Act
            bool accepted = ReceivePlayerAttack(10, attackKindName);

            // Assert
            Assert.That(accepted, Is.False);
            Assert.That(GetStateName(), Is.EqualTo("Dead"));
            Assert.That(GetFieldValue<bool>(scorpion, "hurricaneRetreatActive"), Is.False);
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
