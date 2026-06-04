using Beavermania.Display;
using UnityEngine;

namespace Beavermania.Objects
{
    /// <summary>
    /// Spawns the shared PickUpEffect pooled VFX when a world collectible is collected.
    /// Assign on collectible prefabs (e.g. GoldBrick) and call <see cref="TryPlay"/> before Destroy.
    /// </summary>
    public sealed class CollectiblePickUpVfxEmitter : MonoBehaviour
    {
        [SerializeField] GameObject pickUpEffectPrefab;
        [SerializeField] Vector3 spawnOffset = new Vector3(0f, 0.3f, 0f);

        public static void TryPlay(GameObject collectible)
        {
            if (collectible == null)
                return;

            CollectiblePickUpVfxEmitter emitter = collectible.GetComponent<CollectiblePickUpVfxEmitter>();
            emitter?.PlayPickUpEffect();
        }

        public void PlayPickUpEffect()
        {
            if (pickUpEffectPrefab == null)
                return;

            PooledOneShotVfx.Spawn(pickUpEffectPrefab, transform.position + spawnOffset, Quaternion.identity);
        }
    }
}
