using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

namespace Beavermania.Tests.EditMode.Display
{
    public sealed class FrameBudgetGovernorEditModeTests
    {
        readonly Dictionary<string, Type> cachedRuntimeTypes = new(StringComparer.Ordinal);

        [Test]
        public void SlowFrames_DegradeAfterFortyFiveConsecutiveFramesOverBudget()
        {
            object governor = CreateGovernor(startingTier: 1);

            RecordFrames(governor, 0.018f, 103);

            Assert.That(TryStepTier(governor, out _), Is.False);

            RecordFrame(governor, 0.018f);

            Assert.That(TryStepTier(governor, out int nextTier), Is.True);
            Assert.That(nextTier, Is.EqualTo(2));
            Assert.That(GetCurrentTier(governor), Is.EqualTo(2));
        }

        [Test]
        public void FastFrames_RecoverAfterThreeHundredConsecutiveFramesUnderBudget()
        {
            object governor = CreateGovernor(startingTier: 2);

            RecordFrames(governor, 0.013f, 358);

            Assert.That(TryStepTier(governor, out _), Is.False);

            RecordFrame(governor, 0.013f);

            Assert.That(TryStepTier(governor, out int nextTier), Is.True);
            Assert.That(nextTier, Is.EqualTo(1));
            Assert.That(GetCurrentTier(governor), Is.EqualTo(1));
        }

        [Test]
        public void Cooldown_PreventsAnotherTierStepUntilEnoughTimePasses()
        {
            object governor = Activator.CreateInstance(
                ResolveRuntimeType("Beavermania.Display.FrameBudgetGovernor"),
                1,
                0,
                3,
                2,
                17.2f,
                1,
                14.9f,
                1,
                1f);

            RecordFrames(governor, 0.020f, 2);

            Assert.That(TryStepTier(governor, out int firstTier), Is.True);
            Assert.That(firstTier, Is.EqualTo(2));

            RecordFrames(governor, 0.100f, 9);

            Assert.That(TryStepTier(governor, out _), Is.False);

            RecordFrame(governor, 0.100f);

            Assert.That(TryStepTier(governor, out int secondTier), Is.True);
            Assert.That(secondTier, Is.EqualTo(3));
        }

        [Test]
        public void TierBounds_ClampAtHighAndCanopySafe()
        {
            object highestGovernor = CreateGovernor(startingTier: 0);
            RecordFrames(highestGovernor, 0.013f, 359);
            Assert.That(TryStepTier(highestGovernor, out _), Is.False);
            Assert.That(GetCurrentTier(highestGovernor), Is.EqualTo(0));

            object lowestGovernor = CreateGovernor(startingTier: 3);
            RecordFrames(lowestGovernor, 0.018f, 104);
            Assert.That(TryStepTier(lowestGovernor, out _), Is.False);
            Assert.That(GetCurrentTier(lowestGovernor), Is.EqualTo(3));
        }

        object CreateGovernor(int startingTier)
        {
            return Activator.CreateInstance(
                ResolveRuntimeType("Beavermania.Display.FrameBudgetGovernor"),
                startingTier,
                0,
                3,
                60,
                17.2f,
                45,
                14.9f,
                300,
                1f);
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

        static int GetCurrentTier(object governor)
        {
            return GetPropertyValue<int>(governor, "CurrentTier");
        }

        static void RecordFrame(object governor, float deltaTime)
        {
            InvokeMethod(governor, "RecordFrame", deltaTime);
        }

        static bool TryStepTier(object governor, out int nextTier)
        {
            MethodInfo methodInfo = GetMethodInfo(governor, "TryStepTier");
            object[] parameters = { 0 };
            bool stepped = (bool)methodInfo.Invoke(governor, parameters);
            nextTier = (int)parameters[0];
            return stepped;
        }

        static void RecordFrames(object governor, float deltaTime, int count)
        {
            for (int index = 0; index < count; index++)
                RecordFrame(governor, deltaTime);
        }

        static T GetPropertyValue<T>(object target, string propertyName)
        {
            PropertyInfo propertyInfo = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (propertyInfo == null)
                throw new MissingMemberException(target.GetType().FullName, propertyName);

            return (T)propertyInfo.GetValue(target);
        }

        static object InvokeMethod(object target, string methodName, params object[] parameters)
        {
            return GetMethodInfo(target, methodName).Invoke(target, parameters);
        }

        static MethodInfo GetMethodInfo(object target, string methodName)
        {
            MethodInfo methodInfo = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (methodInfo == null)
                throw new MissingMethodException(target.GetType().FullName, methodName);

            return methodInfo;
        }
    }
}
