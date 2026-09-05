using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Beavermania.Tests.EditMode.NPC.Scorpion
{
    public sealed class ScorpionStateBoundaryTests
    {
        const string ScorpionStateTypeName = "Beavermania.NPC.ScorpionState";
        const string ScorpionTypeName = "Beavermania.NPC.ScorpionScript";
        const string StatsTypeName = "Beavermania.Data.NPC.ScorpionStatsData";
        const int ConfiguredMaxHealth = 100;
        const float PhaseOneAttackRecovery = 0.4f;

        GameObject scorpionObject;
        GameObject targetObject;
        Component scorpion;
        ScriptableObject stats;

        [SetUp]
        public void SetUp()
        {
            scorpionObject = new GameObject("ScorpionStateBoundaryTest");
            scorpionObject.SetActive(false);
            Rigidbody rigidbody = scorpionObject.AddComponent<Rigidbody>();
            scorpion = scorpionObject.AddComponent(ResolveType(ScorpionTypeName));
            stats = ScriptableObject.CreateInstance(ResolveType(StatsTypeName));

            SetFieldValue(stats, "advancedAiEnabled", true);
            // Pinned against CurrentHealth so the normalized health is 1.0 and the Controlled profile
            // selects the phase-one recovery configured here. The default maxHealth of 8000 would select
            // Frenzy and silently test phase-three values instead.
            SetFieldValue(stats, "maxHealth", ConfiguredMaxHealth);
            SetFieldValue(stats, "attackWindowDuration", 0.5f);
            SetFieldValue(stats, "phaseOneAttackRecovery", PhaseOneAttackRecovery);
            SetFieldValue(stats, "stunDuration", 1f);
            SetFieldValue(stats, "recoveryDuration", 0.5f);
            SetFieldValue(stats, "postStunPressureDuration", 2f);
            SetFieldValue(stats, "comboLimit", 2);
            SetFieldValue(scorpion, "statsData", stats);
            SetFieldValue(scorpion, "rbScorpion", rigidbody);
            SetFieldValue(scorpion, "currentState", CreateState("Charge"));
            SetFieldValue(scorpion, "state", "Charge");
            SetFieldValue(scorpion, "CurrentHealth", ConfiguredMaxHealth);
            SetFieldValue(scorpion, "lockedChargeDirection", Vector3.forward);

            targetObject = new GameObject("ScorpionStateBoundaryTarget");
            SetFieldValue(scorpion, "targetTransform", targetObject.transform);
            SetFieldValue(scorpion, "currentDistance", 1f);
        }

        [TearDown]
        public void TearDown()
        {
            if (stats != null)
                UnityEngine.Object.DestroyImmediate(stats);
            if (targetObject != null)
                UnityEngine.Object.DestroyImmediate(targetObject);
            if (scorpionObject != null)
                UnityEngine.Object.DestroyImmediate(scorpionObject);
        }

        [Test]
        public void HandleChargeCollisionNormal_WhenGroundContactPersists_DoesNotEndCharge()
        {
            InvokeMethod(scorpion, "HandleChargeCollisionNormal", Vector3.up);

            Assert.That(GetFieldValue<object>(scorpion, "currentState").ToString(), Is.EqualTo("Charge"));
            Assert.That(GetFieldValue<string>(scorpion, "state"), Is.EqualTo("Charge"));
        }

        [Test]
        public void HandleChargeCollisionNormal_WhenUphillGroundContactPersists_DoesNotEndCharge()
        {
            Vector3 uphillGroundNormal = new Vector3(0f, 1f, -0.5f).normalized;

            InvokeMethod(scorpion, "HandleChargeCollisionNormal", uphillGroundNormal);

            Assert.That(GetFieldValue<object>(scorpion, "currentState").ToString(), Is.EqualTo("Charge"));
            Assert.That(GetFieldValue<string>(scorpion, "state"), Is.EqualTo("Charge"));
        }

        [Test]
        public void HandleChargeCollisionNormal_WhenOpposingWallContactPersists_EndsCharge()
        {
            InvokeMethod(scorpion, "HandleChargeCollisionNormal", Vector3.back);

            Assert.That(GetFieldValue<object>(scorpion, "currentState").ToString(), Is.EqualTo("Look"));
            Assert.That(GetFieldValue<string>(scorpion, "state"), Is.EqualTo("Look"));
        }

        [Test]
        public void TickAttack_WhenWindowExpires_EntersLookWithRecovery()
        {
            InvokeMethod(scorpion, "EnterState", CreateState("Attack"));
            SetFieldValue(scorpion, "stateTimer", Time.fixedDeltaTime * 0.5f);

            InvokeMethod(scorpion, "TickAttack");

            Assert.That(GetFieldValue<object>(scorpion, "currentState").ToString(), Is.EqualTo("Look"));
            Assert.That(
                GetFieldValue<float>(scorpion, "stateTimer"),
                Is.EqualTo(PhaseOneAttackRecovery).Within(0.0001f),
                "Look recovery must be the configured phase-one attack recovery.");
        }

        [Test]
        public void ResolveActionRecovery_Reverse_UsesConfiguredDuration()
        {
            // Arrange
            FieldInfo reverseRecoveryField = stats.GetType().GetField(
                "reverseVulnerabilityDuration",
                BindingFlags.Instance | BindingFlags.Public);

            Assert.That(reverseRecoveryField, Is.Not.Null);
            reverseRecoveryField.SetValue(stats, 0.73f);

            // Act
            float resolvedRecovery = (float)InvokeMethod(
                scorpion,
                "ResolveActionRecovery",
                CreateState("Reverse"));

            // Assert
            Assert.That(resolvedRecovery, Is.EqualTo(0.73f).Within(0.0001f));
        }

        [Test]
        public void TickAdvancedCharge_WhenDurationExpires_EntersLook()
        {
            SetFieldValue(scorpion, "chargeElapsed", float.MaxValue);

            InvokeMethod(scorpion, "TickAdvancedCharge");

            Assert.That(GetFieldValue<object>(scorpion, "currentState").ToString(), Is.EqualTo("Look"));
        }

        [Test]
        public void TickRecovered_WhenRecoveryExpires_EntersLookAndStartsPostStunPressure()
        {
            SetFieldValue(scorpion, "currentState", CreateState("Recovered"));
            SetFieldValue(scorpion, "state", "Recovered");
            SetFieldValue(scorpion, "stateTimer", Time.fixedDeltaTime * 0.5f);

            InvokeMethod(scorpion, "TickRecovered");

            Assert.That(GetFieldValue<object>(scorpion, "currentState").ToString(), Is.EqualTo("Look"));
            Assert.That(GetFieldValue<float>(scorpion, "postStunPressureTimer"), Is.EqualTo(2f));
        }

        [Test]
        public void FixedUpdate_WhenComboLimitReached_InterruptsAttackWithStun()
        {
            SetFieldValue(stats, "advancedAiEnabled", false);
            SetFieldValue(scorpion, "currentState", CreateState("Attack"));
            SetFieldValue(scorpion, "state", "Attack");
            SetFieldValue(scorpion, "combo", 2);

            InvokeMethod(scorpion, "FixedUpdate");

            Assert.That(GetFieldValue<object>(scorpion, "currentState").ToString(), Is.EqualTo("Stunned"));
            Assert.That(GetFieldValue<float>(scorpion, "stateTimer"), Is.GreaterThan(0f));
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
