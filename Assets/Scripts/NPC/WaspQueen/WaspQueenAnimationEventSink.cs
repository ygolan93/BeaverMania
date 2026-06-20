using UnityEngine;

namespace Beavermania.NPC
{
    // Visual-only stopgap: the shared WaspQueen animation clips carry baked animation events
    // (FireProjectile, ActivateAoE, etc.), but WaspQueenBoss drives all gameplay from its FSM timers.
    // This sink lives on the same GameObject as the boss Animator and absorbs those clip events as
    // no-ops, preventing "AnimationEvent has no receiver" warnings and any double-trigger.
    // If the boss is later migrated to event-driven timing, replace this with the real
    // WaspQueenAnimationEvents bridge described in AI_CONTEXT/WaspQueen_Animation_Contract.md.
    [DisallowMultipleComponent]
    public sealed class WaspQueenAnimationEventSink : MonoBehaviour
    {
        public void FireProjectile() { }
        public void ActivateAoE() { }
        public void EnableChargeHitbox() { }
        public void DisableChargeHitbox() { }
        public void SummonWasps() { }
        public void PhasePulse() { }
        public void ExplodeFragments() { }
        public void QueenScream() { }
    }
}
