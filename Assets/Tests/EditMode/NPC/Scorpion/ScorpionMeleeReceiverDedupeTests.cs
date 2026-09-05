using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Beavermania.Tests.EditMode.NPC.Scorpion
{
    /// <summary>
    /// Drives the real <c>AnimatedAttack.CauseDamage</c> overlap loop against a boss whose hitboxes are split
    /// across several colliders. One animation event must reach the damage receiver exactly once, whether the
    /// receiver accepts the hit. Combo is the observable call counter: every reached
    /// <c>ApplyDamage</c> pass awards exactly one combo.
    /// </summary>
    public sealed class ScorpionMeleeReceiverDedupeTests
    {
        const string AnimatedAttackTypeName = "Beavermania.Player.Combat.AnimatedAttack";
        const string BoostChargeSettingsTypeName = "Beavermania.Data.Combat.BoostChargeSettings";
        const string BoostChargeTypeName = "Beavermania.Player.Combat.BoostChargeController";
        const string ScorpionTypeName = "Beavermania.NPC.ScorpionScript";
        const string StatsTypeName = "Beavermania.Data.NPC.ScorpionStatsData";
        const int ConfiguredMaxHealth = 100;
        const int HitDamage = 10;
        const int BossColliderCount = 2;
        const float SwingRadius = 2f;

        /// <summary>Unnamed in <c>TagManager.asset</c>, so no production object can share it.</summary>
        const int FixtureLayer = 31;

        /// <summary>The layer pair <c>ScorpionScript.Awake</c> mutates through <c>IgnoreLayerCollision</c>.</summary>
        const int EnemyLayer = 10;

        static readonly Vector3 IsolatedFixtureOrigin = new Vector3(4000f, 4000f, 4000f);
        static readonly LayerMask FixtureLayerMask = 1 << FixtureLayer;

        GameObject scorpionObject;
        GameObject attackObject;
        GameObject boostChargeObject;
        Component scorpion;
        Component animatedAttack;
        Component boostCharge;
        ScriptableObject stats;
        ScriptableObject boostSettings;
        bool originalIgnoreEnemyLayerCollision;
        bool ignoreLayerCollisionCaptured;

        [SetUp]
        public void SetUp()
        {
            // ScorpionScript.Awake calls Physics.IgnoreLayerCollision(10, 10), a global runtime setting.
            // Captured before activation and restored in teardown so this fixture cannot leak into the
            // rest of the Editor session.
            originalIgnoreEnemyLayerCollision = Physics.GetIgnoreLayerCollision(EnemyLayer, EnemyLayer);
            ignoreLayerCollisionCaptured = true;

            scorpionObject = new GameObject("ScorpionMeleeDedupeBoss");
            scorpionObject.SetActive(false);
            scorpionObject.transform.position = IsolatedFixtureOrigin;
            Rigidbody rigidbody = scorpionObject.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;

            stats = ScriptableObject.CreateInstance(ResolveType(StatsTypeName));
            SetFieldValue(stats, "advancedAiEnabled", true);
            SetFieldValue(stats, "maxHealth", ConfiguredMaxHealth);

            // Owned and inactive, so its own Awake never runs. A non-null boostCharge makes
            // ScorpionScript.EnsureBoostChargeResolved early-return, which is what stops the accepted-damage
            // path from re-running FindObjectOfType and touching a player in whatever scene is open.
            boostChargeObject = new GameObject("ScorpionMeleeDedupeBoostCharge");
            boostChargeObject.SetActive(false);
            boostCharge = boostChargeObject.AddComponent(ResolveType(BoostChargeTypeName));
            boostSettings = ScriptableObject.CreateInstance(ResolveType(BoostChargeSettingsTypeName));
            SetFieldValue(boostSettings, "chargePerHit", 8f);
            SetFieldValue(boostSettings, "comboHitsForBonus", 99);
            SetFieldValue(boostCharge, "settings", boostSettings);

            scorpion = scorpionObject.AddComponent(ResolveType(ScorpionTypeName));
            SetFieldValue(scorpion, "statsData", stats);
            SetFieldValue(scorpion, "boostCharge", boostCharge);

            AddHitboxChild("Jaw");
            AddHitboxChild("Sting");

            // Activated so the child colliders register with the physics scene. Awake therefore runs with the
            // stats reference already assigned, which keeps it off the legacy fallback warning path.
            scorpionObject.SetActive(true);

            // Assigned explicitly rather than relying on Awake, which Unity does not guarantee for
            // runtime-created objects outside Play Mode.
            SetFieldValue(scorpion, "rbScorpion", rigidbody);
            SetFieldValue(scorpion, "CurrentHealth", ConfiguredMaxHealth);
            SetFieldValue(scorpion, "combo", 0);
            SetFieldValue(scorpion, "rotGoal", Quaternion.identity);

            // Awake's ResolvePlayerReferences may have found a player in the open scene. Clearing both
            // references keeps every test path inside the fixture.
            SetFieldValue(scorpion, "Player", null);
            SetFieldValue(scorpion, "targetTransform", null);

            attackObject = new GameObject("ScorpionMeleeDedupeAttacker");
            attackObject.SetActive(false);
            attackObject.transform.position = IsolatedFixtureOrigin;
            animatedAttack = attackObject.AddComponent(ResolveType(AnimatedAttackTypeName));
            SetFieldValue(animatedAttack, "enemyLayers", FixtureLayerMask);
        }

        [TearDown]
        public void TearDown()
        {
            // Every step is independently guarded so a partially built fixture still tears down cleanly.
            if (ignoreLayerCollisionCaptured)
            {
                Physics.IgnoreLayerCollision(EnemyLayer, EnemyLayer, originalIgnoreEnemyLayerCollision);
                ignoreLayerCollisionCaptured = false;
            }

            if (attackObject != null)
                UnityEngine.Object.DestroyImmediate(attackObject);
            if (scorpionObject != null)
                UnityEngine.Object.DestroyImmediate(scorpionObject);
            if (boostChargeObject != null)
                UnityEngine.Object.DestroyImmediate(boostChargeObject);
            if (boostSettings != null)
                UnityEngine.Object.DestroyImmediate(boostSettings);
            if (stats != null)
                UnityEngine.Object.DestroyImmediate(stats);
        }

        [Test]
        public void CauseDamage_MultipleCollidersOnReceiver_RoutesExactlyOneHit()
        {
            // Arrange
            AssertPhysicsSeesEveryBossCollider();

            // Act
            InvokeMethod(animatedAttack, "CauseDamage", IsolatedFixtureOrigin, SwingRadius, HitDamage);

            // Assert
            Assert.That(
                GetFieldValue<int>(scorpion, "combo"),
                Is.EqualTo(1),
                "The hit must reach the boss once per animation event, not once per overlapping collider.");
            Assert.That(
                GetFieldValue<int>(scorpion, "CurrentHealth"),
                Is.EqualTo(ConfiguredMaxHealth - HitDamage),
                "An accepted hit must subtract one hit of damage, not one per overlapping collider.");
            Assert.That(
                GetFieldValue<float>(boostCharge, "currentCharge"),
                Is.EqualTo(8f).Within(0.0001f),
                "One animation event must register Boost Charge once, not once per overlapping collider.");
        }

        [Test]
        public void CauseDamage_ConsecutiveSwings_RouteToTheSameReceiverAgain()
        {
            // Arrange
            AssertPhysicsSeesEveryBossCollider();

            // Act
            InvokeMethod(animatedAttack, "CauseDamage", IsolatedFixtureOrigin, SwingRadius, HitDamage);
            InvokeMethod(animatedAttack, "CauseDamage", IsolatedFixtureOrigin, SwingRadius, HitDamage);

            // Assert
            Assert.That(
                GetFieldValue<int>(scorpion, "combo"),
                Is.EqualTo(2),
                "Deduplication is per swing; the claim set must be cleared at the start of every CauseDamage call.");
            Assert.That(
                GetFieldValue<float>(boostCharge, "currentCharge"),
                Is.EqualTo(16f).Within(0.0001f),
                "Each distinct animation event must register exactly one Boost Charge hit.");
        }

        void AddHitboxChild(string name)
        {
            var hitbox = new GameObject(name) { layer = FixtureLayer };
            hitbox.transform.SetParent(scorpionObject.transform, worldPositionStays: false);
            hitbox.transform.localPosition = Vector3.zero;
            BoxCollider collider = hitbox.AddComponent<BoxCollider>();
            collider.size = Vector3.one * 0.5f;
        }

        /// <summary>
        /// Fails on the fixture rather than on production code if the EditMode physics scene did not register
        /// the runtime colliders, so a failure message cannot be mistaken for a deduplication regression.
        /// Queried through the same dedicated layer mask the production call uses.
        /// </summary>
        void AssertPhysicsSeesEveryBossCollider()
        {
            Collider[] overlapped = Physics.OverlapSphere(IsolatedFixtureOrigin, SwingRadius, FixtureLayerMask);
            int bossColliders = overlapped.Count(
                candidate => candidate != null && candidate.transform.IsChildOf(scorpionObject.transform));

            Assert.That(
                bossColliders,
                Is.EqualTo(BossColliderCount),
                "Fixture precondition: the EditMode physics scene must expose both boss hitbox colliders.");
            Assert.That(
                overlapped.Length,
                Is.EqualTo(BossColliderCount),
                "Fixture precondition: the dedicated fixture layer must contain nothing but this fixture.");
        }

        static object InvokeMethod(object target, string methodName, params object[] parameters)
        {
            MethodInfo method = target.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .SingleOrDefault(candidate =>
                    candidate.Name == methodName
                    && candidate.GetParameters().Length == parameters.Length);

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
