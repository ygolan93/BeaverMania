using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Beavermania.Tests.EditMode.NPC.Scorpion
{
    public sealed class ScorpionCombatDecisionTests
    {
        const float FloatTolerance = 0.000001f;
        const string ActionTypeName = "Beavermania.NPC.ScorpionMacroAction";
        const string ChargeVariantTypeName = "Beavermania.NPC.ScorpionChargeVariant";
        const string ChargeVariantWeightsTypeName = "Beavermania.NPC.ScorpionChargeVariantWeights";
        const string ContextTypeName = "Beavermania.NPC.ScorpionDecisionContext";
        const string DecisionTypeName = "Beavermania.NPC.ScorpionCombatDecision";
        const string HealthProfileTypeName = "Beavermania.NPC.ScorpionHealthProfile";
        const string StatsTypeName = "Beavermania.Data.NPC.ScorpionStatsData";
        const string WeightsTypeName = "Beavermania.NPC.ScorpionDecisionWeights";

        [TestCase(1f, "Controlled")]
        [TestCase(0.6501f, "Controlled")]
        [TestCase(0.65f, "Aggressive")]
        [TestCase(0.3f, "Aggressive")]
        [TestCase(0.2999f, "Frenzy")]
        [TestCase(0f, "Frenzy")]
        public void SelectHealthProfile_UsesExplicitHealthThresholds(float normalizedHealth, string expected)
        {
            object selected = InvokeStatic("SelectHealthProfile", normalizedHealth);

            Assert.That(selected.ToString(), Is.EqualTo(expected));
        }

        [Test]
        public void SelectAction_ConsumesWeightsForSelectedHealthProfile()
        {
            object controlledWeights = CreateWeights(attack: 0f, charge: 0f, reverse: 0f, hold: 1f);
            object aggressiveWeights = CreateWeights(attack: 0f, charge: 1f, reverse: 0f, hold: 0f);
            object frenzyWeights = CreateWeights(attack: 1f, charge: 0f, reverse: 0f, hold: 0f);
            object context = CreateContext(distance: 1f);

            object controlled = SelectAction(
                context,
                SelectProfileValue("Controlled", controlledWeights, aggressiveWeights, frenzyWeights),
                roll: 0f);
            object aggressive = SelectAction(
                context,
                SelectProfileValue("Aggressive", controlledWeights, aggressiveWeights, frenzyWeights),
                roll: 0f);
            object frenzy = SelectAction(
                context,
                SelectProfileValue("Frenzy", controlledWeights, aggressiveWeights, frenzyWeights),
                roll: 0f);

            Assert.That(controlled.ToString(), Is.EqualTo("Hold"));
            Assert.That(aggressive.ToString(), Is.EqualTo("Charge"));
            Assert.That(frenzy.ToString(), Is.EqualTo("Attack"));
        }

        [TestCase(0f, "Short")]
        [TestCase(0.25f, "Normal")]
        [TestCase(1f, "Committed")]
        public void SelectChargeVariant_UsesInjectedWeightedRoll(float roll, string expected)
        {
            object weights = Activator.CreateInstance(
                ResolveType(ChargeVariantWeightsTypeName),
                1f,
                2f,
                1f);

            object selected = InvokeStatic("SelectChargeVariant", weights, roll);

            Assert.That(selected.ToString(), Is.EqualTo(expected));
        }

        [Test]
        public void SelectChargeVariant_MaximumRollReturnsLastPositiveVariant()
        {
            object weights = Activator.CreateInstance(
                ResolveType(ChargeVariantWeightsTypeName),
                1f,
                2f,
                0f);

            object selected = InvokeStatic("SelectChargeVariant", weights, 1f);

            Assert.That(selected.ToString(), Is.EqualTo("Normal"));
        }

        [Test]
        public void SelectChargeVariant_AllWeightsZeroFallsBackToNormal()
        {
            object weights = Activator.CreateInstance(
                ResolveType(ChargeVariantWeightsTypeName),
                0f,
                0f,
                0f);

            object selected = InvokeStatic("SelectChargeVariant", weights, 1f);

            Assert.That(selected.ToString(), Is.EqualTo("Normal"));
        }

        [Test]
        public void SelectAction_BlocksThirdChargeAtMacroLevelRegardlessOfVariantSelection()
        {
            object context = CreateContext(
                distance: 5f,
                previousAction: GetAction("Charge"),
                consecutiveSelections: 2);
            object weights = CreateWeights(attack: 0f, charge: 10f, reverse: 0f, hold: 1f);

            object selected = SelectAction(context, weights, roll: 0f);

            Assert.That(selected.ToString(), Is.EqualTo("Hold"));
        }

        [TestCase("Short", 1.5f, 12f, 0.4f)]
        [TestCase("Normal", 2.5f, 20f, 0.4f)]
        [TestCase("Committed", 3.5f, 28f, 0.2f)]
        public void ResolveChargeLimits_AppliesVariantMultipliers(
            string variantName,
            float expectedDuration,
            float expectedDistance,
            float expectedTracking)
        {
            object limits = InvokeStatic(
                "ResolveChargeLimits",
                Enum.Parse(ResolveType(ChargeVariantTypeName), variantName),
                2.5f,
                20f,
                0.4f,
                0.6f,
                0.6f,
                1.4f,
                1.4f,
                0.5f);

            Assert.That(GetProperty<float>(limits, "MaximumDuration"), Is.EqualTo(expectedDuration).Within(0.0001f));
            Assert.That(GetProperty<float>(limits, "MaximumDistance"), Is.EqualTo(expectedDistance).Within(0.0001f));
            Assert.That(GetProperty<float>(limits, "TrackingDuration"), Is.EqualTo(expectedTracking).Within(0.0001f));
        }

        [Test]
        public void ApplyPostStunPressure_ReducesHoldAndIncreasesCharge()
        {
            object weights = CreateWeights(attack: 4f, charge: 5f, reverse: 2f, hold: 3f);

            object modified = InvokeStatic("ApplyPostStunPressure", weights, 1.25f, 0.4f);

            Assert.That(GetProperty<float>(modified, "Attack"), Is.EqualTo(4f));
            Assert.That(GetProperty<float>(modified, "Charge"), Is.EqualTo(6.25f));
            Assert.That(GetProperty<float>(modified, "Reverse"), Is.EqualTo(2f));
            Assert.That(GetProperty<float>(modified, "Hold"), Is.EqualTo(1.2f));
        }

        [Test]
        public void ResolveRecovery_PreservesProfileOrderingAndPostStunBoundary()
        {
            float controlled = (float)InvokeStatic("ResolveRecovery", 1.1f, false, 0.75f);
            float aggressive = (float)InvokeStatic("ResolveRecovery", 0.8f, false, 0.75f);
            float frenzy = (float)InvokeStatic("ResolveRecovery", 0.55f, false, 0.75f);
            float postStun = (float)InvokeStatic("ResolveRecovery", frenzy, true, 0.75f);

            Assert.That(controlled, Is.GreaterThan(aggressive));
            Assert.That(aggressive, Is.GreaterThan(frenzy));
            Assert.That(postStun, Is.EqualTo(0.4125f).Within(0.0001f));
            Assert.That(postStun, Is.GreaterThan(0f));
        }

        [TestCase(0f, false)]
        [TestCase(-1f, false)]
        [TestCase(0f, true)]
        [TestCase(-1f, true)]
        public void ResolveRecovery_InvalidConfiguredValueReturnsReadableMinimum(
            float configuredRecovery,
            bool postStunPressureActive)
        {
            // Arrange
            float minimumRecovery = GetStatsMinimumActionRecovery();

            // Act
            float resolved = (float)InvokeStatic(
                "ResolveRecovery",
                configuredRecovery,
                postStunPressureActive,
                0.75f);

            // Assert
            Assert.That(resolved, Is.EqualTo(minimumRecovery).Within(FloatTolerance));
        }

        [Test]
        public void OnValidate_InvalidProfileRecoveriesClampToNamedMinimum()
        {
            // Arrange
            Type statsType = ResolveType(StatsTypeName);
            ScriptableObject stats = ScriptableObject.CreateInstance(statsType);
            string[] recoveryFields =
            {
                "phaseOneAttackRecovery",
                "phaseTwoAttackRecovery",
                "phaseThreeAttackRecovery",
                "phaseOneChargeRecovery",
                "phaseTwoChargeRecovery",
                "phaseThreeChargeRecovery",
                "reverseVulnerabilityDuration",
            };
            float minimumRecovery = GetStatsMinimumActionRecovery();

            try
            {
                for (int index = 0; index < recoveryFields.Length; index++)
                    SetFieldValue(stats, recoveryFields[index], index % 2 == 0 ? 0f : -1f);

                // Act
                MethodInfo onValidate = statsType.GetMethod(
                    "OnValidate",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(onValidate, Is.Not.Null);
                onValidate.Invoke(stats, null);

                // Assert
                foreach (string fieldName in recoveryFields)
                {
                    Assert.That(
                        GetFieldValue<float>(stats, fieldName),
                        Is.EqualTo(minimumRecovery).Within(FloatTolerance),
                        fieldName);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(stats);
            }
        }

        [Test]
        public void SelectAction_AllowsSameActionTwiceButNotThirdTime()
        {
            object weights = CreateWeights(attack: 10f, charge: 0f, reverse: 0f, hold: 1f);
            object attack = GetAction("Attack");

            object secondSelection = SelectAction(
                CreateContext(distance: 1f, previousAction: attack, consecutiveSelections: 1),
                weights,
                roll: 0f);
            object thirdSelection = SelectAction(
                CreateContext(distance: 1f, previousAction: attack, consecutiveSelections: 2),
                weights,
                roll: 0f);

            Assert.That(secondSelection.ToString(), Is.EqualTo("Attack"));
            Assert.That(thirdSelection.ToString(), Is.EqualTo("Hold"));
        }

        [Test]
        public void SelectAction_NeverReturnsAttackOutsideAttackRange()
        {
            object context = CreateContext(distance: 5f);
            object weights = CreateWeights(attack: 100f, charge: 1f, reverse: 0f, hold: 0f);

            object selected = SelectAction(context, weights, roll: 0f);

            Assert.That(selected.ToString(), Is.EqualTo("Charge"));
        }

        [Test]
        public void SelectAction_AllowsReverseAtVeryCloseRange()
        {
            object context = CreateContext(distance: 1f);
            object weights = CreateWeights(attack: 0f, charge: 0f, reverse: 5f, hold: 0f);

            object selected = SelectAction(context, weights, roll: 0.5f);

            Assert.That(selected.ToString(), Is.EqualTo("Reverse"));
        }

        [Test]
        public void SelectAction_ReturnsHoldOutsideLookDistance()
        {
            object context = CreateContext(distance: 31f);
            object weights = CreateWeights(attack: 10f, charge: 10f, reverse: 10f, hold: 0f);

            object selected = SelectAction(context, weights, roll: 0.5f);

            Assert.That(selected.ToString(), Is.EqualTo("Hold"));
        }

        [TestCase(-10f, "Attack")]
        [TestCase(10f, "Hold")]
        public void SelectAction_ClampsInjectedRollToEligibleWeightedBounds(float roll, string expected)
        {
            object context = CreateContext(distance: 1f);
            object weights = CreateWeights(attack: 1f, charge: 1f, reverse: 1f, hold: 1f);

            object selected = SelectAction(context, weights, roll);

            Assert.That(selected.ToString(), Is.EqualTo(expected));
        }

        [Test]
        public void SelectAction_MaximumRollReturnsLastPositiveEligibleWeight()
        {
            object context = CreateContext(distance: 5f);
            object weights = CreateWeights(attack: 0f, charge: 1f, reverse: 0f, hold: 0f);

            object selected = SelectAction(context, weights, roll: 1f);

            Assert.That(selected.ToString(), Is.EqualTo("Charge"));
        }

        [Test]
        public void SelectAction_PreventsThirdHoldBeyondPreferredChargeDistance()
        {
            object context = CreateContext(
                distance: 25f,
                previousAction: GetAction("Hold"),
                consecutiveSelections: 2);
            object weights = CreateWeights(attack: 0f, charge: 1f, reverse: 0f, hold: 10f);

            object selected = SelectAction(context, weights, roll: 0.5f);

            Assert.That(selected.ToString(), Is.EqualTo("Charge"));
        }

        [Test]
        public void LockHorizontalDirection_NormalizesAndRemovesVerticalComponent()
        {
            MethodInfo method = ResolveType(DecisionTypeName).GetMethod(
                "LockHorizontalDirection",
                BindingFlags.Public | BindingFlags.Static);

            Assert.That(method, Is.Not.Null);
            Vector3 direction = (Vector3)method.Invoke(null, new object[] { new Vector3(3f, 8f, 4f) });

            Assert.That(direction.x, Is.EqualTo(0.6f).Within(0.0001f));
            Assert.That(direction.y, Is.Zero.Within(0.0001f));
            Assert.That(direction.z, Is.EqualTo(0.8f).Within(0.0001f));
        }

        [Test]
        public void AccumulateHorizontalDistance_UsesTravelledPathInsteadOfStraightLineDisplacement()
        {
            MethodInfo method = ResolveType(DecisionTypeName).GetMethod(
                "AccumulateHorizontalDistance",
                BindingFlags.Public | BindingFlags.Static);

            Assert.That(method, Is.Not.Null);
            float accumulated = (float)method.Invoke(
                null,
                new object[] { 0f, Vector3.zero, new Vector3(3f, 5f, 0f) });
            accumulated = (float)method.Invoke(
                null,
                new object[] { accumulated, new Vector3(3f, 5f, 0f), new Vector3(3f, -2f, 4f) });

            Assert.That(accumulated, Is.EqualTo(7f).Within(0.0001f));
            Assert.That(accumulated, Is.GreaterThan(new Vector3(3f, 0f, 4f).magnitude));
        }

        static object SelectAction(object context, object weights, float roll)
        {
            return InvokeStatic("SelectAction", context, weights, roll);
        }

        static object SelectProfileValue(
            string profileName,
            object controlled,
            object aggressive,
            object frenzy)
        {
            MethodInfo method = ResolveType(DecisionTypeName)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Single(candidate => candidate.Name == "SelectProfileValue" && candidate.IsGenericMethodDefinition);

            return method
                .MakeGenericMethod(ResolveType(WeightsTypeName))
                .Invoke(
                    null,
                    new[]
                    {
                        Enum.Parse(ResolveType(HealthProfileTypeName), profileName),
                        controlled,
                        aggressive,
                        frenzy,
                    });
        }

        static object CreateContext(
            float distance,
            object previousAction = null,
            int consecutiveSelections = 0)
        {
            return Activator.CreateInstance(
                ResolveType(ContextTypeName),
                distance,
                2.2f,
                20f,
                30f,
                previousAction ?? GetAction("Hold"),
                consecutiveSelections);
        }

        static object CreateWeights(float attack, float charge, float reverse, float hold)
        {
            return Activator.CreateInstance(
                ResolveType(WeightsTypeName),
                attack,
                charge,
                reverse,
                hold);
        }

        static object GetAction(string name)
        {
            return Enum.Parse(ResolveType(ActionTypeName), name);
        }

        static object InvokeStatic(string methodName, params object[] parameters)
        {
            MethodInfo method = ResolveType(DecisionTypeName)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .SingleOrDefault(candidate =>
                    candidate.Name == methodName
                    && !candidate.IsGenericMethodDefinition
                    && candidate.GetParameters().Length == parameters.Length);

            Assert.That(method, Is.Not.Null, $"Missing public static method {methodName}.");
            return method.Invoke(null, parameters);
        }

        static T GetProperty<T>(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public);

            Assert.That(property, Is.Not.Null, $"Missing property {propertyName}.");
            return (T)property.GetValue(target);
        }

        static float GetStatsMinimumActionRecovery()
        {
            FieldInfo field = ResolveType(StatsTypeName).GetField(
                "MinimumActionRecovery",
                BindingFlags.Public | BindingFlags.Static);

            Assert.That(field, Is.Not.Null);
            return (float)field.GetRawConstantValue();
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
