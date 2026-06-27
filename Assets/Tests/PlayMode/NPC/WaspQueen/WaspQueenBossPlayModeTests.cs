using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Beavermania.Tests.PlayMode.NPC.WaspQueen
{
    public sealed class WaspQueenBossPlayModeTests
    {
        const string BossControllerPath = "Assets/Prefabs/NPC/WaspQueen/WaspQueen.controller";

        readonly Dictionary<string, Type> cachedRuntimeTypes = new(StringComparer.Ordinal);
        readonly List<UnityEngine.Object> spawnedObjects = new();
        readonly List<ScriptableObject> createdAssets = new();

        [TearDown]
        public void TearDown()
        {
            InvokeStaticMethod("Beavermania.Display.PooledOneShotVfx", "ClearAllPools");
            InvokeStaticMethod("Beavermania.NPC.PooledDeathDebris", "ClearAllPools");

            for (int index = spawnedObjects.Count - 1; index >= 0; index--)
            {
                if (spawnedObjects[index] != null)
                    UnityEngine.Object.DestroyImmediate(spawnedObjects[index]);
            }

            for (int index = createdAssets.Count - 1; index >= 0; index--)
            {
                if (createdAssets[index] != null)
                    UnityEngine.Object.DestroyImmediate(createdAssets[index]);
            }

            spawnedObjects.Clear();
            createdAssets.Clear();
        }

        [UnityTest]
        public IEnumerator PhaseTransitions_HappenOncePerThreshold()
        {
            BossHarness harness = CreateHarness();
            InvokeMethod(harness.Boss, "ActivateBoss");
            yield return null;

            InvokeMethod(harness.Boss, "ReceiveDamage", 35, CreateEnemyDamageType("Normal"), null);
            yield return new WaitForSeconds(0.12f);

            Assert.That(GetPropertyValue<int>(harness.Boss, "CurrentPhaseNumber"), Is.EqualTo(2));

            yield return new WaitForSeconds(0.18f);

            Assert.That(GetPropertyValue<int>(harness.Boss, "CurrentPhaseNumber"), Is.EqualTo(2));

            InvokeMethod(harness.Boss, "ReceiveDamage", 40, CreateEnemyDamageType("Normal"), null);
            yield return new WaitForSeconds(0.12f);

            Assert.That(GetPropertyValue<int>(harness.Boss, "CurrentPhaseNumber"), Is.EqualTo(3));

            yield return new WaitForSeconds(0.18f);

            Assert.That(GetPropertyValue<int>(harness.Boss, "CurrentPhaseNumber"), Is.EqualTo(3));
        }

        [UnityTest]
        public IEnumerator DeathEvent_FiresOnce_WhenDeathIsTriggeredMultipleWays()
        {
            BossHarness harness = CreateHarness();
            InvokeMethod(harness.Boss, "ActivateBoss");
            yield return null;

            int defeatEvents = 0;
            EventInfo defeatedEvent = harness.Boss.GetType().GetEvent("Defeated");
            Delegate defeatedHandler = CreateTypedActionDelegate(defeatedEvent.EventHandlerType, () => defeatEvents++);
            defeatedEvent.AddEventHandler(harness.Boss, defeatedHandler);

            InvokeMethod(harness.Boss, "ReceiveDamage", GetPropertyValue<int>(harness.Boss, "CurrentHealth"), CreateEnemyDamageType("Normal"), null);
            InvokeMethod(harness.Boss, "TakeDamage", 1);
            InvokeMethod(harness.Boss, "ReceiveDamage", 1, CreateEnemyDamageType("Normal"), null);

            Assert.That(defeatEvents, Is.EqualTo(1));

            defeatedEvent.RemoveEventHandler(harness.Boss, defeatedHandler);
        }

        [UnityTest]
        public IEnumerator Intro_TriggersAnimatorIntroState_WhenBossActivates()
        {
            BossHarness harness = CreateHarness();
            SetFieldValue(harness.Config, "introDuration", 1f);

            GameObject visual = CreateChildGameObject(((Component)harness.Boss).transform, "AnimatorVisual");
            Animator animator = visual.AddComponent<Animator>();
            animator.runtimeAnimatorController = LoadController(BossControllerPath);
            Assert.That(animator.runtimeAnimatorController, Is.Not.Null, "Production Wasp Queen boss controller should load");
            animator.logWarnings = false;
            animator.applyRootMotion = false;
            SetFieldValue(harness.Boss, "Animator", animator);

            InvokeMethod(harness.Boss, "ActivateBoss");
            yield return WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0).IsName("Intro"), 1f);

            Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Intro"), Is.True, "Activating the boss should trigger the intro animation");
        }

        [UnityTest]
        public IEnumerator Charge_LocksDirectionAtStart()
        {
            BossHarness harness = CreateHarness();
            object phase1 = GetFieldValue<object>(harness.Config, "phase1");
            SetFieldValue(phase1, "rangedWeight", 0f);
            SetFieldValue(phase1, "aoeWeight", 0f);
            SetFieldValue(phase1, "summonWeight", 0f);
            SetFieldValue(phase1, "chargeWeight", 10f);
            SetFieldValue(phase1, "chargeTelegraphDuration", 0f);
            SetFieldValue(phase1, "chargeDuration", 0.4f);
            SetFieldValue(phase1, "chargeRecoveryDuration", 1f);
            harness.PlayerTransform.position = new Vector3(8f, 0f, 0f);

            InvokeMethod(harness.Boss, "ActivateBoss");

            yield return new WaitUntil(() => GetPropertyValue<object>(harness.Boss, "State").ToString() == "Charge");

            Vector3 lockedDirection = GetPropertyValue<Vector3>(harness.Boss, "CurrentChargeDirection");
            harness.PlayerTransform.position = new Vector3(-8f, 0f, 0f);

            yield return new WaitForFixedUpdate();

            Assert.That(
                Vector3.Angle(lockedDirection, GetPropertyValue<Vector3>(harness.Boss, "CurrentChargeDirection")),
                Is.LessThan(0.01f));
        }

        [UnityTest]
        public IEnumerator PoisonAoe_TicksAtConfiguredInterval_InsteadOfEveryFrame()
        {
            BossHarness harness = CreateHarness();
            object phase1 = GetFieldValue<object>(harness.Config, "phase1");
            SetFieldValue(phase1, "rangedWeight", 0f);
            SetFieldValue(phase1, "chargeWeight", 0f);
            SetFieldValue(phase1, "summonWeight", 0f);
            SetFieldValue(phase1, "aoeWeight", 10f);
            SetFieldValue(phase1, "aoeTelegraphDuration", 0f);
            SetFieldValue(phase1, "aoeDuration", 0.45f);
            SetFieldValue(phase1, "aoeTickRate", 0.2f);
            SetFieldValue(phase1, "aoeDamage", 10f);
            SetFieldValue(phase1, "aoeRecoveryDuration", 1f);
            harness.PlayerTransform.position = new Vector3(1.5f, 0f, 0f);
            SetFieldValue(harness.Player, "CurrentHealth", 100f);

            InvokeMethod(harness.Boss, "ActivateBoss");
            yield return new WaitUntil(() => GetPropertyValue<object>(harness.Boss, "State").ToString() == "Recovery");

            yield return new WaitForSeconds(0.05f);
            float healthAfterFirstTick = GetFieldValue<float>(harness.Player, "CurrentHealth");

            yield return new WaitForSeconds(0.1f);

            Assert.That(healthAfterFirstTick, Is.EqualTo(90f).Within(0.001f));
            Assert.That(GetFieldValue<float>(harness.Player, "CurrentHealth"), Is.EqualTo(healthAfterFirstTick).Within(0.001f));

            yield return new WaitForSeconds(0.15f);

            Assert.That(GetFieldValue<float>(harness.Player, "CurrentHealth"), Is.EqualTo(80f).Within(0.001f));
        }

        BossHarness CreateHarness()
        {
            GameObject cameraObject = Spawn(new GameObject("Main Camera"));
            cameraObject.tag = "MainCamera";
            cameraObject.AddComponent<Camera>();

            GameObject playerObject = Spawn(new GameObject("Player"));
            playerObject.tag = "Player";
            playerObject.AddComponent<Rigidbody>().isKinematic = true;
            playerObject.AddComponent<CapsuleCollider>();
            Component player = playerObject.AddComponent(ResolveRuntimeType("Beavermania.Player.BeaverPlayerBehaviour"));
            ((Behaviour)player).enabled = false;
            SetFieldValue(player, "MaxHealth", 100f);
            SetFieldValue(player, "CurrentHealth", 100f);
            SetFieldValue(player, "MaxStamina", 100f);
            SetFieldValue(player, "CurrentStamina", 100f);

            ScriptableObject config = CreateConfig();

            GameObject bossObject = Spawn(new GameObject("WaspQueenBoss"));
            Rigidbody bossBody = bossObject.AddComponent<Rigidbody>();
            bossBody.isKinematic = true;
            bossBody.useGravity = false;
            bossObject.AddComponent<SphereCollider>();

            Transform projectileSpawnPoint = CreateChildTransform(bossObject.transform, "ProjectileSpawn");
            Transform aoeOrigin = CreateChildTransform(bossObject.transform, "AoeOrigin");
            Transform summonPoint = CreateChildTransform(bossObject.transform, "SummonPoint");

            GameObject audioHost = CreateChildGameObject(bossObject.transform, "AudioSource");
            AudioSource audioSource = audioHost.AddComponent<AudioSource>();

            Component chargeAttack = bossObject.AddComponent(ResolveRuntimeType("Beavermania.NPC.WaspQueenChargeAttack"));
            Component boss = bossObject.AddComponent(ResolveRuntimeType("Beavermania.NPC.WaspQueenBoss"));
            SetFieldValue(boss, "Config", config);
            SetFieldValue(boss, "Body", bossBody);
            SetFieldValue(boss, "Player", player);
            SetFieldValue(boss, "ProjectileSpawnPoint", projectileSpawnPoint);
            SetFieldValue(boss, "AoeOrigin", aoeOrigin);
            SetFieldValue(boss, "WaspSpawnPoints", new[] { summonPoint });
            SetFieldValue(boss, "ChargeAttack", chargeAttack);
            SetFieldValue(boss, "AudioSource", audioSource);
            SetFieldValue(boss, "ActivateOnStart", false);

            return new BossHarness(config, boss, player, playerObject.transform);
        }

        ScriptableObject CreateConfig()
        {
            ScriptableObject config = ScriptableObject.CreateInstance(ResolveRuntimeType("Beavermania.Data.NPC.WaspQueenConfig"));
            createdAssets.Add(config);

            SetFieldValue(config, "maxHealth", 100);
            SetFieldValue(config, "activateRange", 25f);
            SetFieldValue(config, "closeRange", 4f);
            SetFieldValue(config, "mediumRange", 10f);
            SetFieldValue(config, "farRange", 16f);
            SetFieldValue(config, "chargeMinRange", 5f);
            SetFieldValue(config, "chargeMaxRange", 10f);
            SetFieldValue(config, "introDuration", 0f);
            SetFieldValue(config, "phaseTransitionDuration", 0.05f);
            SetFieldValue(config, "idleDecisionDelay", 0f);
            SetFieldValue(config, "phaseTwoHealthThresholdNormalized", 0.7f);
            SetFieldValue(config, "phaseThreeHealthThresholdNormalized", 0.3f);
            SetFieldValue(config, "maxActiveProjectiles", 2);
            SetFieldValue(config, "maxActivePoisonZones", 1);

            ConfigurePhase(GetFieldValue<object>(config, "phase1"), 3, 1);
            ConfigurePhase(GetFieldValue<object>(config, "phase2"), 5, 2);
            ConfigurePhase(GetFieldValue<object>(config, "phase3"), 7, 3);

            SetFieldValue(config, "poisonProjectilePrefab", CreateProjectilePrefab());
            SetFieldValue(config, "poisonZonePrefab", CreatePoisonZonePrefab());
            SetFieldValue(config, "deathExplosionPrefab", CreateVfxPrefab("DeathExplosionVfx"));
            SetFieldValue(config, "fragmentPrefabs", new[]
            {
                CreateDebrisPrefab("FragmentBody"),
                CreateDebrisPrefab("FragmentWing")
            });

            return config;
        }

        static void ConfigurePhase(object phase, int maxSummons, int summonsPerCast)
        {
            SetFieldValue(phase, "maxActiveSummonedWasps", maxSummons);
            SetFieldValue(phase, "waspsPerSummon", summonsPerCast);
            SetFieldValue(phase, "rangedCooldown", 3f);
            SetFieldValue(phase, "rangedTelegraphDuration", 0.05f);
            SetFieldValue(phase, "rangedRecoveryDuration", 0.4f);
            SetFieldValue(phase, "rangedDamage", 10);
            SetFieldValue(phase, "projectileSpeed", 10f);
            SetFieldValue(phase, "aoeCooldown", 3f);
            SetFieldValue(phase, "aoeTelegraphDuration", 0.05f);
            SetFieldValue(phase, "aoeRecoveryDuration", 0.4f);
            SetFieldValue(phase, "aoeRadius", 3f);
            SetFieldValue(phase, "aoeDamage", 8f);
            SetFieldValue(phase, "aoeDuration", 0.35f);
            SetFieldValue(phase, "aoeTickRate", 0.2f);
            SetFieldValue(phase, "chargeCooldown", 3f);
            SetFieldValue(phase, "chargeTelegraphDuration", 0.05f);
            SetFieldValue(phase, "chargeSpeed", 10f);
            SetFieldValue(phase, "chargeDuration", 0.2f);
            SetFieldValue(phase, "chargeDamage", 15f);
            SetFieldValue(phase, "chargeRecoveryDuration", 0.55f);
            SetFieldValue(phase, "summonCooldown", 3f);
            SetFieldValue(phase, "summonTelegraphDuration", 0.05f);
            SetFieldValue(phase, "summonRecoveryDuration", 0.45f);
            SetFieldValue(phase, "rangedWeight", 4f);
            SetFieldValue(phase, "aoeWeight", 4f);
            SetFieldValue(phase, "chargeWeight", 4f);
            SetFieldValue(phase, "summonWeight", 1f);
            SetFieldValue(phase, "sameAbilityPenalty", 2f);
        }

        Component CreateProjectilePrefab()
        {
            GameObject projectile = Spawn(new GameObject("PoisonProjectilePrefab"));
            projectile.SetActive(false);
            Rigidbody body = projectile.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.isKinematic = true;
            SphereCollider collider = projectile.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            return projectile.AddComponent(ResolveRuntimeType("Beavermania.NPC.WaspQueenProjectile"));
        }

        Component CreatePoisonZonePrefab()
        {
            GameObject zone = Spawn(new GameObject("PoisonZonePrefab"));
            zone.SetActive(false);
            SphereCollider collider = zone.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            return zone.AddComponent(ResolveRuntimeType("Beavermania.NPC.WaspQueenPoisonZone"));
        }

        GameObject CreateVfxPrefab(string name)
        {
            GameObject vfx = Spawn(new GameObject(name));
            vfx.SetActive(false);
            vfx.AddComponent<ParticleSystem>();
            return vfx;
        }

        GameObject CreateDebrisPrefab(string name)
        {
            GameObject debris = Spawn(new GameObject(name));
            debris.SetActive(false);
            debris.AddComponent<Rigidbody>();
            debris.AddComponent<BoxCollider>();
            Component effect = debris.AddComponent(ResolveRuntimeType("Beavermania.NPC.EffectObject"));
            SetFieldValue(effect, "time", 0.2f);
            return debris;
        }

        object CreateEnemyDamageType(string enumName)
        {
            return Enum.Parse(ResolveRuntimeType("Beavermania.NPC.EnemyDamageType"), enumName);
        }

        static RuntimeAnimatorController LoadController(string controllerPath)
        {
            Type adb = ResolveLoadedType("UnityEditor.AssetDatabase");
            MethodInfo methodInfo = adb.GetMethod("LoadAssetAtPath", new[] { typeof(string), typeof(Type) });
            return (RuntimeAnimatorController)methodInfo.Invoke(null, new object[] { controllerPath, typeof(RuntimeAnimatorController) });
        }

        Transform CreateChildTransform(Transform parent, string name)
        {
            return CreateChildGameObject(parent, name).transform;
        }

        static IEnumerator WaitUntil(Func<bool> condition, float timeout)
        {
            float elapsed = 0f;
            while (!condition() && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        GameObject CreateChildGameObject(Transform parent, string name)
        {
            GameObject child = Spawn(new GameObject(name));
            child.transform.SetParent(parent, false);
            return child;
        }

        GameObject Spawn(GameObject gameObject)
        {
            spawnedObjects.Add(gameObject);
            return gameObject;
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

        static Delegate CreateTypedActionDelegate(Type eventHandlerType, Action callback)
        {
            Type parameterType = eventHandlerType.GetGenericArguments()[0];
            MethodInfo factoryMethod = typeof(WaspQueenBossPlayModeTests)
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
                ScriptableObject config,
                Component boss,
                Component player,
                Transform playerTransform)
            {
                Config = config;
                Boss = boss;
                Player = player;
                PlayerTransform = playerTransform;
            }

            public ScriptableObject Config { get; }
            public Component Boss { get; }
            public Component Player { get; }
            public Transform PlayerTransform { get; }
        }
    }
}
