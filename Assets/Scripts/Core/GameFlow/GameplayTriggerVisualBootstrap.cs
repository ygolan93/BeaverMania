using UnityEngine;

namespace Beavermania.Core.GameFlow
{
    /// <summary>
    /// Adds <see cref="TriggerVolumeVisualHider"/> to trigger colliders that still have debug renderers.
    /// </summary>
    public static class GameplayTriggerVisualBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void HideDebugTriggerMeshes()
        {
            var colliders = Object.FindObjectsOfType<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider == null || !collider.isTrigger)
                    continue;

                if (collider.GetComponent<TriggerVolumeVisualHider>() != null)
                    continue;

                bool hasMesh = collider.GetComponent<MeshRenderer>() != null;
                bool hasSprite = collider.GetComponent<SpriteRenderer>() != null;
                if (!hasMesh && !hasSprite)
                    continue;

                if (collider.CompareTag("Life"))
                    continue;

                collider.gameObject.AddComponent<TriggerVolumeVisualHider>();
            }
        }
    }
}
