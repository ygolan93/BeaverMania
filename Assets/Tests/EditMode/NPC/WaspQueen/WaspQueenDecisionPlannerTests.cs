using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Beavermania.Tests.EditMode.NPC.WaspQueen
{
    public sealed class WaspQueenDecisionPlannerTests
    {
        readonly Dictionary<string, Type> cachedRuntimeTypes = new(StringComparer.Ordinal);
        ScriptableObject config;

        [SetUp]
        public void SetUp()
        {
            config = ScriptableObject.CreateInstance(ResolveRuntimeType("Beavermania.Data.NPC.WaspQueenConfig"));
            SetFieldValue(config, "closeRange", 4f);
            SetFieldValue(config, "mediumRange", 10f);
            SetFieldValue(config, "farRange", 18f);
            SetFieldValue(config, "chargeMinRange", 5f);
            SetFieldValue(config, "chargeMaxRange", 10f);

            ConfigurePhase(GetFieldValue<object>(config, "phase1"), rangedWeight: 5f, aoeWeight: 5f, chargeWeight: 5f, summonWeight: 5f);
        }

        [TearDown]
        public void TearDown()
        {
            if (config != null)
                UnityEngine.Object.DestroyImmediate(config);
        }

        [Test]
        public void ChooseAbility_ReturnsRanged_WhenPlayerFarAndRangedAvailable()
        {
            object context = CreateContext(distanceToPlayer: 17f);

            object ability = ChooseAbility(GetFieldValue<object>(config, "phase1"), context);

            Assert.That(ability.ToString(), Is.EqualTo("RangedPoisonShot"));
        }

        [Test]
        public void ChooseAbility_ReturnsPoisonAoe_WhenPlayerNearAndAoeAvailable()
        {
            object context = CreateContext(distanceToPlayer: 2f);

            object ability = ChooseAbility(GetFieldValue<object>(config, "phase1"), context);

            Assert.That(ability.ToString(), Is.EqualTo("PoisonAoE"));
        }

        [Test]
        public void ChooseAbility_ReturnsCharge_WhenPlayerAtMediumRange()
        {
            object context = CreateContext(distanceToPlayer: 7f);

            object ability = ChooseAbility(GetFieldValue<object>(config, "phase1"), context);

            Assert.That(ability.ToString(), Is.EqualTo("Charge"));
        }

        [Test]
        public void ChooseAbility_SkipsSummon_WhenSummonCapReached()
        {
            object phase = GetFieldValue<object>(config, "phase1");
            ConfigurePhase(phase, rangedWeight: 0f, aoeWeight: 0f, chargeWeight: 0f, summonWeight: 10f);

            object context = CreateContext(
                distanceToPlayer: 7f,
                activeSummonedWasps: GetFieldValue<int>(phase, "maxActiveSummonedWasps"),
                rangedCooldownRemaining: 3f,
                aoeCooldownRemaining: 3f,
                chargeCooldownRemaining: 3f);

            object ability = ChooseAbility(phase, context);

            Assert.That(ability.ToString(), Is.EqualTo("Idle"));
        }

        [Test]
        public void ChooseAbility_SkipsAbilitiesOnCooldown()
        {
            object phase = GetFieldValue<object>(config, "phase1");
            object context = CreateContext(
                distanceToPlayer: 7f,
                activeSummonedWasps: GetFieldValue<int>(phase, "maxActiveSummonedWasps"),
                rangedCooldownRemaining: 1f,
                aoeCooldownRemaining: 1f,
                chargeCooldownRemaining: 1f,
                summonCooldownRemaining: 1f);

            object ability = ChooseAbility(phase, context);

            Assert.That(ability.ToString(), Is.EqualTo("Idle"));
        }

        [Test]
        public void ChooseAbility_AvoidsRepeatingSameAttack_WhenAlternativeExists()
        {
            object phase = GetFieldValue<object>(config, "phase1");
            ConfigurePhase(phase, rangedWeight: 7f, aoeWeight: 0f, chargeWeight: 6f, summonWeight: 0f);
            SetFieldValue(phase, "sameAbilityPenalty", 3f);

            object context = CreateContext(
                distanceToPlayer: 7f,
                previousAbility: GetEnumValue("Beavermania.NPC.WaspQueenAbility", "Charge"),
                repeatedAbilityCount: 1);

            object ability = ChooseAbility(phase, context);

            Assert.That(ability.ToString(), Is.EqualTo("RangedPoisonShot"));
        }

        object ChooseAbility(object phase, object context)
        {
            return InvokeStaticMethod(
                "Beavermania.NPC.WaspQueenDecisionPlanner",
                "ChooseAbility",
                config,
                phase,
                context);
        }

        static void ConfigurePhase(
            object phase,
            float rangedWeight,
            float aoeWeight,
            float chargeWeight,
            float summonWeight)
        {
            SetFieldValue(phase, "rangedWeight", rangedWeight);
            SetFieldValue(phase, "aoeWeight", aoeWeight);
            SetFieldValue(phase, "chargeWeight", chargeWeight);
            SetFieldValue(phase, "summonWeight", summonWeight);
            SetFieldValue(phase, "maxActiveSummonedWasps", 3);
            SetFieldValue(phase, "sameAbilityPenalty", 2f);
        }

        object CreateContext(
            float distanceToPlayer,
            int activeSummonedWasps = 0,
            float rangedCooldownRemaining = 0f,
            float aoeCooldownRemaining = 0f,
            float chargeCooldownRemaining = 0f,
            float summonCooldownRemaining = 0f,
            object previousAbility = null,
            int repeatedAbilityCount = 0)
        {
            return Activator.CreateInstance(
                ResolveRuntimeType("Beavermania.NPC.WaspQueenDecisionContext"),
                distanceToPlayer,
                activeSummonedWasps,
                rangedCooldownRemaining,
                aoeCooldownRemaining,
                chargeCooldownRemaining,
                summonCooldownRemaining,
                previousAbility ?? GetEnumValue("Beavermania.NPC.WaspQueenAbility", "None"),
                repeatedAbilityCount,
                false);
        }

        object GetEnumValue(string fullName, string valueName)
        {
            return Enum.Parse(ResolveRuntimeType(fullName), valueName);
        }

        Type ResolveRuntimeType(string fullName)
        {
            if (cachedRuntimeTypes.TryGetValue(fullName, out Type cachedType))
                return cachedType;

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
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

        static object InvokeStaticMethod(string fullName, string methodName, params object[] parameters)
        {
            Type type = ResolveLoadedType(fullName);
            MethodInfo methodInfo = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (methodInfo == null)
                throw new MissingMethodException(fullName, methodName);

            return methodInfo.Invoke(null, parameters);
        }

        static T GetFieldValue<T>(object target, string fieldName)
        {
            FieldInfo fieldInfo = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (fieldInfo == null)
                throw new MissingFieldException(target.GetType().FullName, fieldName);

            return (T)fieldInfo.GetValue(target);
        }

        static void SetFieldValue(object target, string fieldName, object value)
        {
            FieldInfo fieldInfo = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (fieldInfo == null)
                throw new MissingFieldException(target.GetType().FullName, fieldName);

            fieldInfo.SetValue(target, value);
        }

        static Type ResolveLoadedType(string fullName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type resolvedType = assembly.GetType(fullName, throwOnError: false);
                if (resolvedType != null)
                    return resolvedType;
            }

            throw new InvalidOperationException($"Failed to resolve loaded type '{fullName}'.");
        }
    }
}
