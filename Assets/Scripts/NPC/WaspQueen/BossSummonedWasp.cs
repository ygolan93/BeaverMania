using UnityEngine;

namespace Beavermania.NPC
{
    // Marks a wasp summoned by the Wasp Queen. NPC_Basic.Death() reads this to give boss-summoned wasps a
    // lightweight death (few short-lived debris, no currency drop) so the arena does not fill with corpses,
    // without changing how world-placed wasps die.
    [DisallowMultipleComponent]
    public sealed class BossSummonedWasp : MonoBehaviour
    {
        [SerializeField] float debrisLifetime = 3f;

        public float DebrisLifetime => Mathf.Max(0.1f, debrisLifetime);
    }
}
