using UnityEngine;

namespace Beavermania.NPC
{
    // Bridges the WaspQueen clip animation events (baked with these exact names) to the boss controller.
    // Lives on the same GameObject as the Animator (the visual), while WaspQueenBoss sits on the prefab root.
    // Each event forwards to a guarded WaspQueenBoss.AnimEvent_* method, so attack impacts release on the
    // animation frame instead of an FSM timer. If no boss is found (e.g. the standalone display prefab),
    // the calls are safe no-ops, which also prevents "AnimationEvent has no receiver" warnings.
    [DisallowMultipleComponent]
    public sealed class WaspQueenAnimationEventSink : MonoBehaviour
    {
        WaspQueenBoss boss;

        void Awake()
        {
            boss = GetComponentInParent<WaspQueenBoss>();
        }

        public void FireProjectile()
        {
            if (boss != null)
                boss.AnimEvent_FireProjectile();
        }

        public void ActivateAoE()
        {
            if (boss != null)
                boss.AnimEvent_ActivateAoE();
        }

        public void EnableChargeHitbox()
        {
            if (boss != null)
                boss.AnimEvent_EnableChargeHitbox();
        }

        public void DisableChargeHitbox()
        {
            if (boss != null)
                boss.AnimEvent_DisableChargeHitbox();
        }

        public void SummonWasps()
        {
            if (boss != null)
                boss.AnimEvent_SummonWasps();
        }

        public void PhasePulse()
        {
            if (boss != null)
                boss.AnimEvent_PhasePulse();
        }

        public void ExplodeFragments()
        {
            if (boss != null)
                boss.AnimEvent_ExplodeFragments();
        }

        public void QueenScream()
        {
            if (boss != null)
                boss.AnimEvent_QueenScream();
        }
    }
}
