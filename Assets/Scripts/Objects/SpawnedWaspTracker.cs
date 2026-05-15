using UnityEngine;

namespace Beavermania.Objects
{
    public class SpawnedWaspTracker : MonoBehaviour
    {
        WaspAreaSpawner spawner;

        public void Initialize(WaspAreaSpawner owner)
        {
            spawner = owner;
        }

        void OnDestroy()
        {
            if (spawner != null)
                spawner.NotifyWaspDied(gameObject);
        }
    }
}
