using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Beavermania.Tests.PlayMode.NPC.Scorpion
{
    public sealed class ScorpionBossCombatContractPlayModeTests
    {
        const string BoostChargeSettingsTypeName = "Beavermania.Data.Combat.BoostChargeSettings";
        const string BoostChargeTypeName = "Beavermania.Player.Combat.BoostChargeController";
        const string LogSpawnerTypeName = "Beavermania.Objects.LogSpawner";
        const string DamageTypeName = "Beavermania.NPC.EnemyDamageType";
        const string PlayerAttackKindTypeName = "Beavermania.Player.Combat.PlayerAttackKind";
        const string ScorpionStateTypeName = "Beavermania.NPC.ScorpionState";
        const string ScorpionTypeName = "Beavermania.NPC.ScorpionScript";
        const string StatsTypeName = "Beavermania.Data.NPC.ScorpionStatsData";
        const string TestLogName = "ScorpionContractTestLog";

        GameObject bossObject;
        GameObject treeObject;
        GameObject logPrefabObject;
        GameObject animatorObject;
        Component boss;
        Component tree;
        ScriptableObject stats;
        ScriptableObject boostSettings;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            GameObject[] objects = UnityEngine.Object.FindObjectsOfType<GameObject>();
            for (int index = 0; index < objects.Length; index++)
            {
                if (objects[index] != null && objects[index].name.StartsWith(TestLogName, StringComparison.Ordinal))
                    UnityEngine.Object.Destroy(objects[index]);
            }

            if (treeObject != null)
                UnityEngine.Object.Destroy(treeObject);
            if (logPrefabObject != null)
                UnityEngine.Object.Destroy(logPrefabObject);
            if (bossObject != null)
                UnityEngine.Object.Destroy(bossObject);
            if (animatorObject != null)
                UnityEngine.Object.Destroy(animatorObject);
            if (stats != null)
                UnityEngine.Object.Destroy(stats);
            if (boostSettings != null)
                UnityEngine.Object.Destroy(boostSettings);

            yield return null;
        }

        [UnityTest]
        public IEnumerator MarkedTree_FrontalCharge_DestroysTreeProducesLogsAndStuns()
        {
            // Arrange
            CreateBossInCharge();
            CreateTree(marked: true, withLogs: true);

            // Act
            bool handled = (bool)InvokeMethod(
                boss,
                "HandleMarkedGivingTreeChargeContact",
                tree,
                Vector3.back);
            bool logsDropped = GetFieldValue<bool>(tree, "_logsDropped");
            int producedLogs = CountProducedLogs();
            yield return null;

            // Assert
            Assert.That(handled, Is.True);
            Assert.That(logsDropped, Is.True);
            Assert.That(producedLogs, Is.EqualTo(4));
            Assert.That(GetStateName(), Is.EqualTo("Stunned"));
            Assert.That(treeObject == null, Is.True, "LogSpawner must own delayed tree destruction.");
        }

        [UnityTest]
        public IEnumerator UnmarkedTree_FrontalCharge_DoesNotDestroyOrStun()
        {
            // Arrange
            CreateBossInCharge();
            CreateTree(marked: false, withLogs: false);

            // Act
            bool handled = (bool)InvokeMethod(
                boss,
                "HandleMarkedGivingTreeChargeContact",
                tree,
                Vector3.back);
            yield return null;

            // Assert
            Assert.That(handled, Is.False);
            Assert.That(GetFieldValue<bool>(tree, "_logsDropped"), Is.False);
            Assert.That(GetStateName(), Is.EqualTo("Charge"));
            Assert.That(treeObject != null, Is.True);
        }

        [UnityTest]
        public IEnumerator MarkedTree_SideCharge_DoesNotDestroyOrStun()
        {
            // Arrange
            CreateBossInCharge();
            CreateTree(marked: true, withLogs: false);

            // Act
            bool handled = (bool)InvokeMethod(
                boss,
                "HandleMarkedGivingTreeChargeContact",
                tree,
                Vector3.left);
            yield return null;

            // Assert
            Assert.That(handled, Is.False);
            Assert.That(GetFieldValue<bool>(tree, "_logsDropped"), Is.False);
            Assert.That(GetStateName(), Is.EqualTo("Charge"));
        }

        [UnityTest]
        public IEnumerator MarkedTree_DuringCooldown_DestroysTreeAndEndsChargeWithoutStun()
        {
            // Arrange
            CreateBossInCharge();
            CreateTree(marked: true, withLogs: false);
            SetFieldValue(boss, "stunCooldownRemaining", 5f);

            // Act
            bool handled = (bool)InvokeMethod(
                boss,
                "HandleMarkedGivingTreeChargeContact",
                tree,
                Vector3.back);
            bool logsDropped = GetFieldValue<bool>(tree, "_logsDropped");
            yield return null;

            // Assert
            Assert.That(handled, Is.True);
            Assert.That(logsDropped, Is.True);
            Assert.That(GetStateName(), Is.EqualTo("Idle"));
            Assert.That(GetFieldValue<float>(boss, "stunCooldownRemaining"), Is.EqualTo(5f).Within(0.0001f));
            Assert.That(treeObject == null, Is.True);
        }

#if UNITY_EDITOR
        [UnityTest]
        public IEnumerator HurricaneKick_FromWalk_ImmediatelyPlaysCoveringClawsWithoutCrossKindRewind()
        {
            // Arrange
            const string firstAttackKind = "HurricaneKick";
            const string secondAttackKind = "HurricaneSword";

            // Act
            IEnumerator scenario = VerifyHurricaneRetreatAnimation(firstAttackKind, secondAttackKind);

            // Assert
            yield return scenario;
        }

        [UnityTest]
        public IEnumerator HurricaneSword_FromWalk_ImmediatelyPlaysCoveringClawsWithoutCrossKindRewind()
        {
            // Arrange
            const string firstAttackKind = "HurricaneSword";
            const string secondAttackKind = "HurricaneKick";

            // Act
            IEnumerator scenario = VerifyHurricaneRetreatAnimation(firstAttackKind, secondAttackKind);

            // Assert
            yield return scenario;
        }

        IEnumerator VerifyHurricaneRetreatAnimation(string firstAttackKind, string secondAttackKind)
        {
            // Arrange
            CreateBossInCharge();
            SetFieldValue(boss, "rotGoal", Quaternion.identity);
            SetFieldValue(stats, "hurricaneKickRetreatSpeed", 12f);
            SetFieldValue(stats, "hurricaneKickRetreatDuration", 0.6f);
            treeObject = new GameObject("ScorpionHurricaneAttackSource");
            treeObject.transform.position = Vector3.back;
            animatorObject = new GameObject("ScorpionHurricaneControllerTest");
            Animator animator = animatorObject.AddComponent<Animator>();
            animator.fireEvents = false;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.runtimeAnimatorController = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                "Assets/Prefabs/Scorpion/BossAnimations/Scorpion.controller");
            Assert.That(animator.runtimeAnimatorController, Is.Not.Null);
            animator.SetBool("Walk", true);
            animator.Play("Base Layer.Walk", 0, 0f);
            animator.Update(0f);
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Walk"), Is.True);
            SetFieldValue(boss, "Scorpion", animator);

            // Act
            bool firstAccepted = ReceivePlayerAttack(firstAttackKind);
            animator.Update(0f);
            AnimatorStateInfo immediateState = animator.GetCurrentAnimatorStateInfo(0);
            animator.Update(0.05f);
            float timeBeforeSecondHit = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            bool secondAccepted = ReceivePlayerAttack(secondAttackKind);
            animator.Update(0f);
            AnimatorStateInfo stateAfterSecondHit = animator.GetCurrentAnimatorStateInfo(0);

            // Assert
            Assert.That(firstAccepted, Is.True);
            Assert.That(secondAccepted, Is.True);
            Assert.That(immediateState.IsName("Base Layer.Backwards"), Is.True,
                "The short retreat cannot wait for Walk's ordinary exit transition.");
            Assert.That(immediateState.speed, Is.EqualTo(2f).Within(0.0001f));
            Assert.That(animator.speed, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(stateAfterSecondHit.IsName("Base Layer.Backwards"), Is.True);
            Assert.That(timeBeforeSecondHit, Is.GreaterThan(0f));
            Assert.That(stateAfterSecondHit.normalizedTime, Is.EqualTo(timeBeforeSecondHit).Within(0.0001f),
                "Additional Hurricane hits must not restart the covering-claw animation.");
            Assert.That(GetFieldValue<bool>(boss, "isAttacking"), Is.True);

            // Act
            SetFieldValue(boss, "stateTimer", 0f);
            InvokeMethod(boss, "TickReverse");
            animator.Update(0.1f);

            // Assert
            Assert.That(animator.GetBool("Backwards"), Is.False);
            Assert.That(GetFieldValue<bool>(boss, "isAttacking"), Is.False);
            Assert.That(GetFieldValue<Rigidbody>(boss, "rbScorpion").velocity.sqrMagnitude,
                Is.LessThan(0.0001f));

            // Timing above is manually stepped; do not auto-advance animation during the cleanup frame.
            animatorObject.SetActive(false);
            yield return null;
        }

        bool ReceivePlayerAttack(string attackKindName)
        {
            return (bool)InvokeMethod(
                boss,
                "ReceivePlayerAttack",
                10,
                CreateEnum(PlayerAttackKindTypeName, attackKindName),
                CreateEnum(DamageTypeName, "Normal"),
                treeObject.transform);
        }
#endif

        void CreateBossInCharge()
        {
            bossObject = new GameObject("ScorpionBossCombatContractPlayMode");
            bossObject.SetActive(false);
            Rigidbody rigidbody = bossObject.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            boss = bossObject.AddComponent(ResolveType(ScorpionTypeName));
            stats = ScriptableObject.CreateInstance(ResolveType(StatsTypeName));
            SetFieldValue(stats, "advancedAiEnabled", true);
            SetFieldValue(stats, "maxHealth", 100);
            SetFieldValue(stats, "bossStunDuration", 3.5f);
            SetFieldValue(stats, "bossStunCooldown", 10f);
            SetFieldValue(boss, "statsData", stats);
            SetFieldValue(boss, "rbScorpion", rigidbody);
            SetFieldValue(boss, "CurrentHealth", 100);
            SetFieldValue(boss, "currentState", CreateEnum(ScorpionStateTypeName, "Charge"));
            SetFieldValue(boss, "lockedChargeDirection", Vector3.forward);

            // Keep manually driven hits isolated from the current scene's player and Boost Charge.
            Component boostCharge = bossObject.AddComponent(ResolveType(BoostChargeTypeName));
            boostSettings = ScriptableObject.CreateInstance(ResolveType(BoostChargeSettingsTypeName));
            SetFieldValue(boostCharge, "settings", boostSettings);
            SetFieldValue(boss, "boostCharge", boostCharge);
        }

        void CreateTree(bool marked, bool withLogs)
        {
            treeObject = new GameObject("ScorpionBossCombatContractTree");
            treeObject.AddComponent<BoxCollider>();
            tree = treeObject.AddComponent(ResolveType(LogSpawnerTypeName));
            SetFieldValue(tree, "canStunScorpionBoss", marked);

            if (!withLogs)
                return;

            logPrefabObject = new GameObject(TestLogName);
            SetFieldValue(tree, "Prefab", new[] { logPrefabObject.transform });
        }

        int CountProducedLogs()
        {
            return UnityEngine.Object.FindObjectsOfType<GameObject>()
                .Count(candidate => candidate != null && candidate.name == TestLogName + "(Clone)");
        }

        string GetStateName()
        {
            return GetFieldValue<object>(boss, "currentState").ToString();
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
