using System.Collections;
using Beavermania.Audio;
using UnityEngine;
using Beavermania.NPC;

namespace Beavermania.Objects
{
    public class Static_Hive : MonoBehaviour
    {
        const int MaxTotalWaspsOnDeath = 30;
        const int WaspsPerSpawnBatch = 6;
        const float WaspSpawnBatchInterval = 0.12f;

        public int MaxHealth = 10000;
        public int CurrentHealth;
        public NPC_Health HiveBar;
        public GameObject Hive;
        [SerializeField] GameObject[] SpawnedObjects;
        public Transform Wasp;
        public GameObject Explosion;
        public AudioSource Sound;
        public GameObject HitEffect;

        bool deathHandled;

        public void Start()
        {
            CurrentHealth = MaxHealth;
            if (Explosion != null)
                Explosion.SetActive(false);
        }

        void LateUpdate()
        {
            if (deathHandled)
                return;

            if (HitEffect != null)
                HitEffect.SetActive(false);

            if (CurrentHealth <= 0)
                Death();
        }

        public void Death()
        {
            if (deathHandled)
                return;

            deathHandled = true;

            if (Explosion != null)
            {
                Explosion.SetActive(true);
                Explosion.transform.parent = null;
            }

            if (SpawnedObjects != null)
            {
                for (int i = 0; i < SpawnedObjects.Length; i++)
                {
                    GameObject spawnedObject = SpawnedObjects[i];
                    if (spawnedObject == null)
                        continue;

                    Instantiate(spawnedObject, transform.position, Quaternion.identity);
                }
            }

            if (Wasp != null)
                StartCoroutine(SpawnWaspsStaggered());

            if (Hive != null)
                Destroy(Hive);
        }

        IEnumerator SpawnWaspsStaggered()
        {
            int spawned = 0;
            Vector3 spawnPosition = transform.position;

            while (spawned < MaxTotalWaspsOnDeath)
            {
                int batchCount = Mathf.Min(WaspsPerSpawnBatch, MaxTotalWaspsOnDeath - spawned);
                for (int i = 0; i < batchCount; i++)
                {
                    Instantiate(Wasp, spawnPosition, Quaternion.identity);
                    spawned++;
                }

                if (spawned < MaxTotalWaspsOnDeath)
                    yield return new WaitForSeconds(WaspSpawnBatchInterval);
            }
        }

        public void TakeDamage(int Damage)
        {
            if (deathHandled || Damage <= 0)
                return;

            if (HitEffect != null)
                HitEffect.SetActive(true);

            CurrentHealth -= Damage;

            if (Sound != null && Sound.clip != null)
                GameplayAudio.TryPlayOneShot(Sound, Sound.clip, "HiveHit", 0.1f, 1f, 1f);

            if (HiveBar != null)
                HiveBar.SetNPCHealth(CurrentHealth);
        }
    }
}
