using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Beavermania.Tests.PlayMode.Core.NPC
{
    // Animation-only smoke test for the Wasp Queen boss assets (rig + controller + clip events).
    // The runtime boss controller is Codex-owned; this verifies the animator drives states and that
    // gameplay animation events fire on the prefab root with correct timing and the death rules hold.
    public sealed class WaspQueenAnimationPlayModeTests
    {
        const string PrefabPath = "Assets/Prefabs/WaspQueen/WaspQueen.prefab";
        readonly List<GameObject> spawned = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < spawned.Count; i++)
                if (spawned[i] != null) UnityEngine.Object.DestroyImmediate(spawned[i]);
            spawned.Clear();
        }

        sealed class EventProbe : MonoBehaviour
        {
            public readonly Dictionary<string, int> Counts = new(StringComparer.Ordinal);
            void Bump(string k) { Counts.TryGetValue(k, out int c); Counts[k] = c + 1; }
            public void FireProjectile() { Bump("FireProjectile"); }
            public void ActivateAoE() { Bump("ActivateAoE"); }
            public void EnableChargeHitbox() { Bump("EnableChargeHitbox"); }
            public void DisableChargeHitbox() { Bump("DisableChargeHitbox"); }
            public void SummonWasps() { Bump("SummonWasps"); }
            public void PhasePulse() { Bump("PhasePulse"); }
            public void ExplodeFragments() { Bump("ExplodeFragments"); }
            public void QueenScream() { Bump("QueenScream"); }
            public int Get(string k) { Counts.TryGetValue(k, out int c); return c; }
        }

        (Animator anim, EventProbe probe) Spawn()
        {
            var prefab = LoadPrefab();
            Assert.That(prefab, Is.Not.Null, "WaspQueen prefab not found at " + PrefabPath);
            var inst = UnityEngine.Object.Instantiate(prefab);
            spawned.Add(inst);
            var anim = inst.GetComponent<Animator>();
            Assert.That(anim, Is.Not.Null, "WaspQueen prefab has no Animator");
            Assert.That(anim.runtimeAnimatorController, Is.Not.Null, "Animator has no controller");
            anim.logWarnings = false;
            anim.applyRootMotion = false;
            var probe = inst.AddComponent<EventProbe>();
            return (anim, probe);
        }

        static GameObject LoadPrefab()
        {
            Type adb = ResolveType("UnityEditor.AssetDatabase");
            if (adb == null) return null;
            MethodInfo m = adb.GetMethod("LoadAssetAtPath", new[] { typeof(string), typeof(Type) });
            return (GameObject)m.Invoke(null, new object[] { PrefabPath, typeof(GameObject) });
        }

        static Type ResolveType(string fullName)
        {
            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type t = a.GetType(fullName, false);
                if (t != null) return t;
            }
            return null;
        }

        static IEnumerator WaitUntil(Func<bool> cond, float timeout)
        {
            float t = 0f;
            while (!cond() && t < timeout) { t += Time.deltaTime; yield return null; }
        }

        [UnityTest]
        public IEnumerator DefaultState_IsIdleCombat_AndLoops()
        {
            var (anim, _) = Spawn();
            yield return null;
            yield return null;
            var info = anim.GetCurrentAnimatorStateInfo(0);
            Assert.That(info.IsName("Idle_Combat"), Is.True, "Default state should be Idle_Combat");
            Assert.That(info.loop, Is.True, "Idle_Combat should loop");
        }

        [UnityTest]
        public IEnumerator RangedAttack_FiresProjectile_AfterTelegraph()
        {
            var (anim, probe) = Spawn();
            yield return null;
            anim.SetTrigger("RangedAttack");
            yield return WaitUntil(() => probe.Get("FireProjectile") >= 1, 4f);
            Assert.That(probe.Get("FireProjectile"), Is.EqualTo(1), "FireProjectile should fire exactly once");
        }

        [UnityTest]
        public IEnumerator Charge_BracketsHitbox_EnableThenDisableOnce()
        {
            var (anim, probe) = Spawn();
            yield return null;
            anim.SetTrigger("Charge");
            yield return WaitUntil(() => probe.Get("DisableChargeHitbox") >= 1, 5f);
            Assert.That(probe.Get("EnableChargeHitbox"), Is.EqualTo(1), "EnableChargeHitbox once");
            Assert.That(probe.Get("DisableChargeHitbox"), Is.EqualTo(1), "DisableChargeHitbox once");
        }

        [UnityTest]
        public IEnumerator Die_ReachesDeath_FiresExplodeOnce_AndDoesNotExit()
        {
            var (anim, probe) = Spawn();
            yield return null;
            anim.SetTrigger("Die");
            yield return WaitUntil(() => anim.GetCurrentAnimatorStateInfo(0).IsName("Death"), 2f);
            Assert.That(anim.GetCurrentAnimatorStateInfo(0).IsName("Death"), Is.True, "Should enter Death");
            yield return WaitUntil(() => probe.Get("ExplodeFragments") >= 1, 3f);
            Assert.That(probe.Get("ExplodeFragments"), Is.EqualTo(1), "ExplodeFragments once");
            yield return new WaitForSeconds(0.6f);
            Assert.That(anim.GetCurrentAnimatorStateInfo(0).IsName("Death"), Is.True, "Death has no exit transition");
            Assert.That(probe.Get("ExplodeFragments"), Is.EqualTo(1), "ExplodeFragments still only once");
        }
    }
}
