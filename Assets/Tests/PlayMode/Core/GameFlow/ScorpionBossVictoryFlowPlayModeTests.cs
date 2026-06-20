using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Beavermania.Tests.PlayMode.Core.GameFlow
{
    public sealed class ScorpionBossVictoryFlowPlayModeTests
    {
        readonly Dictionary<string, Type> cachedRuntimeTypes = new(StringComparer.Ordinal);
        readonly List<GameObject> spawnedObjects = new();

        [TearDown]
        public void TearDown()
        {
            InvokeStaticMethod("Beavermania.Core.GameFlow.GameTimeScaleGate", "ClearAll");

            for (int index = 0; index < spawnedObjects.Count; index++)
            {
                if (spawnedObjects[index] != null)
                    UnityEngine.Object.DestroyImmediate(spawnedObjects[index]);
            }

            spawnedObjects.Clear();
        }

        [UnityTest]
        public IEnumerator BossDefeatedEvent_FiresOnce_WhenDeathIsTriggeredMultipleWays()
        {
            BossHarness harness = CreateHarness(victoryDelay: 0.1f);
            int defeatEvents = 0;
            EventInfo defeatedEvent = harness.Boss.GetType().GetEvent("Defeated");
            Delegate defeatedHandler = CreateTypedActionDelegate(defeatedEvent.EventHandlerType, () => defeatEvents++);

            defeatedEvent.AddEventHandler(harness.Boss, defeatedHandler);

            yield return null;

            Assert.That(GetPropertyValue<float>(harness.Boss, "VictoryDelay"), Is.EqualTo(0.1f).Within(0.0001f));

            InvokeMethod(harness.Boss, "ReceiveDamage", GetFieldValue<int>(harness.Boss, "CurrentHealth"), CreateEnemyDamageType("Normal"), null);
            InvokeMethod(harness.Boss, "TakeDamage", 1);
            InvokeMethod(harness.Boss, "ReceiveDamage", 1, CreateEnemyDamageType("Normal"), null);

            Assert.That(defeatEvents, Is.EqualTo(1));

            defeatedEvent.RemoveEventHandler(harness.Boss, defeatedHandler);
        }

        [UnityTest]
        public IEnumerator VictoryFlow_WaitsForDelay_ThenShowsVictoryExactlyOnce()
        {
            BossHarness harness = CreateHarness(victoryDelay: 0.15f);

            yield return null;

            harness.BossBar.SetActive(true);
            KillBoss(harness.Boss);

            Assert.That(harness.BossBar.activeSelf, Is.False);
            Assert.That(harness.VictoryScreen.activeSelf, Is.False);
            Assert.That(((Behaviour)harness.Player).enabled, Is.True);

            yield return new WaitForSecondsRealtime(0.05f);

            Assert.That(harness.VictoryScreen.activeSelf, Is.False);
            Assert.That(((Behaviour)harness.Player).enabled, Is.True);

            yield return new WaitForSecondsRealtime(0.2f);

            Assert.That(harness.VictoryScreen.activeSelf, Is.True);
            Assert.That(harness.BossBar.activeSelf, Is.False);
            Assert.That(((Behaviour)harness.Player).enabled, Is.False);
            Assert.That(GetStaticPropertyValue<bool>("Beavermania.Core.GameFlow.GameTimeScaleGate", "IsFrozen"), Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(0f));
        }

        [UnityTest]
        public IEnumerator VictoryFlow_DoesNotActivate_WhenLoseScreenWinsDuringDelay()
        {
            BossHarness harness = CreateHarness(victoryDelay: 0.2f);

            yield return null;

            harness.BossBar.SetActive(true);
            KillBoss(harness.Boss);
            yield return new WaitForSecondsRealtime(0.05f);

            InvokeMethod(harness.Player, "ActivateLooseMenu");
            yield return new WaitForSecondsRealtime(0.25f);

            Assert.That(harness.LoseScreen.activeSelf, Is.True);
            Assert.That(harness.VictoryScreen.activeSelf, Is.False);
            Assert.That(harness.BossBar.activeSelf, Is.False);
            Assert.That(((Behaviour)harness.Player).enabled, Is.True);
            Assert.That(GetStaticPropertyValue<bool>("Beavermania.Core.GameFlow.GameTimeScaleGate", "IsFrozen"), Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(0f));
        }

        [UnityTest]
        public IEnumerator VictoryFlow_UsesGenericBossVictorySource_WhenConfigured()
        {
            BossHarness harness = CreateHarness(
                victoryDelay: 0.1f,
                assignLegacyBoss: false,
                assignGenericBossSource: true,
                bossObjectName: "WaspQueenProxyBoss");

            yield return null;

            harness.BossBar.SetActive(true);
            KillBoss(harness.Boss);
            yield return new WaitForSecondsRealtime(0.15f);

            Assert.That(harness.VictoryScreen.activeSelf, Is.True);
            Assert.That(harness.BossBar.activeSelf, Is.False);
        }

        BossHarness CreateHarness(
            float victoryDelay,
            bool assignLegacyBoss = true,
            bool assignGenericBossSource = false,
            string bossObjectName = "ScorpionBoss")
        {
            GameObject cameraObject = Spawn("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.AddComponent<Camera>();

            GameObject playerObject = Spawn("Player");
            playerObject.tag = "Player";
            playerObject.AddComponent<Rigidbody>();

            Animator playerAnimator = playerObject.AddComponent<Animator>();
            playerAnimator.logWarnings = false;

            Component player = playerObject.AddComponent(ResolveRuntimeType("Beavermania.Player.BeaverPlayerBehaviour"));
            SetFieldValue(player, "Otter", playerAnimator);
            SetFieldValue(player, "HoneyJar", Spawn("HoneyJar"));
            SetFieldValue(player, "GoldBrick", Spawn("GoldBrick"));
            SetFieldValue(player, "HologramedBridge", Spawn("HologramedBridge"));
            SetFieldValue(player, "AttackPoint", Spawn("AttackPoint").transform);
            SetFieldValue(player, "PlacedGold", Spawn("PlacedGold"));
            SetFieldValue(player, "HealLight", Spawn("HealLight").AddComponent<Light>());
            SetFieldValue(player, "HurtLight", Spawn("HurtLight").AddComponent<Light>());

            GameObject loseScreen = Spawn("LoseScreen");
            loseScreen.SetActive(false);
            SetFieldValue(player, "LooseScreen", loseScreen);

            GameObject bossBar = Spawn("BossBar");
            bossBar.SetActive(false);

            GameObject bossPanel = Spawn("BossPanel");
            bossPanel.SetActive(false);

            GameObject victoryScreen = Spawn("VictoryScreen");
            victoryScreen.SetActive(false);

            GameObject chatCollider = Spawn("ChatCollider");
            chatCollider.SetActive(true);

            Component handler = playerObject.AddComponent(ResolveRuntimeType("Beavermania.Player.Combat.BossHandler"));
            SetFieldValue(handler, "ChatCollider", chatCollider);
            SetFieldValue(handler, "BossBar", bossBar);
            SetFieldValue(handler, "BossPanel", bossPanel);
            SetFieldValue(handler, "VictoryScreen", victoryScreen);

            GameObject bossObject = Spawn(bossObjectName);
            bossObject.AddComponent<Rigidbody>();
            Component boss = bossObject.AddComponent(ResolveRuntimeType("Beavermania.NPC.ScorpionScript"));
            SetFieldValue(boss, "victoryDelay", victoryDelay);
            ((Behaviour)boss).enabled = false;

            if (assignLegacyBoss)
                SetFieldValue(handler, "Boss", boss);

            if (assignGenericBossSource)
                SetFieldValue(handler, "bossVictorySourceBehaviour", boss);

            return new BossHarness(player, handler, boss, bossBar, victoryScreen, loseScreen);
        }

        void KillBoss(Component boss)
        {
            InvokeMethod(boss, "ReceiveDamage", GetFieldValue<int>(boss, "CurrentHealth"), CreateEnemyDamageType("Normal"), null);
        }

        object CreateEnemyDamageType(string enumName)
        {
            return Enum.Parse(ResolveRuntimeType("Beavermania.NPC.EnemyDamageType"), enumName);
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

        GameObject Spawn(string name)
        {
            var gameObject = new GameObject(name);
            spawnedObjects.Add(gameObject);
            return gameObject;
        }

        static Delegate CreateTypedActionDelegate(Type eventHandlerType, Action callback)
        {
            Type parameterType = eventHandlerType.GetGenericArguments()[0];
            MethodInfo factoryMethod = typeof(ScorpionBossVictoryFlowPlayModeTests)
                .GetMethod(nameof(CreateTypedActionDelegateInternal), BindingFlags.Static | BindingFlags.NonPublic)
                .MakeGenericMethod(parameterType);

            return (Delegate)factoryMethod.Invoke(null, new object[] { callback });
        }

        static Delegate CreateTypedActionDelegateInternal<T>(Action callback)
        {
            Action<T> handler = _ => callback();
            return handler;
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

        static T GetPropertyValue<T>(object target, string propertyName)
        {
            PropertyInfo propertyInfo = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (propertyInfo == null)
                throw new MissingMemberException(target.GetType().FullName, propertyName);

            return (T)propertyInfo.GetValue(target);
        }

        static T GetStaticPropertyValue<T>(string fullName, string propertyName)
        {
            Type type = ResolveLoadedType(fullName);
            PropertyInfo propertyInfo = type.GetProperty(propertyName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (propertyInfo == null)
                throw new MissingMemberException(fullName, propertyName);

            return (T)propertyInfo.GetValue(null);
        }

        static object InvokeMethod(object target, string methodName, params object[] parameters)
        {
            MethodInfo methodInfo = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (methodInfo == null)
                throw new MissingMethodException(target.GetType().FullName, methodName);

            return methodInfo.Invoke(target, parameters);
        }

        static object InvokeStaticMethod(string fullName, string methodName, params object[] parameters)
        {
            Type type = ResolveLoadedType(fullName);
            MethodInfo methodInfo = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (methodInfo == null)
                throw new MissingMethodException(fullName, methodName);

            return methodInfo.Invoke(null, parameters);
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

        readonly struct BossHarness
        {
            public BossHarness(
                Component player,
                Component handler,
                Component boss,
                GameObject bossBar,
                GameObject victoryScreen,
                GameObject loseScreen)
            {
                Player = player;
                Handler = handler;
                Boss = boss;
                BossBar = bossBar;
                VictoryScreen = victoryScreen;
                LoseScreen = loseScreen;
            }

            public Component Player { get; }

            public Component Handler { get; }

            public Component Boss { get; }

            public GameObject BossBar { get; }

            public GameObject VictoryScreen { get; }

            public GameObject LoseScreen { get; }
        }
    }
}
