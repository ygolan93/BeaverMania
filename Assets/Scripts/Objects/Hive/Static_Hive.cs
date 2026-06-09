using Beavermania.Audio;
using Beavermania.Core.GameFlow;
using Beavermania.UI.Objectives;
using UnityEngine;
using Beavermania.NPC;

namespace Beavermania.Objects
{
    public class Static_Hive : MonoBehaviour
    {
        public int MaxHealth = 10000;
        public int CurrentHealth;
        public NPC_Health HiveBar;
        public GameObject Hive;
        [SerializeField] GameObject[] SpawnedObjects;
        public Transform Wasp;
        public GameObject Explosion;
        public AudioSource Sound;
        public GameObject HitEffect;
        [SerializeField] bool advancesObjectiveOnDeath;
        [SerializeField] int advanceToObjectiveIndex = 1;

        bool deathHandled;

        public void Start()
        {
            CurrentHealth = MaxHealth;
            Explosion.SetActive(false);
            AudioSourceRouting.EnsureRoute(Sound, AudioSourceRoute.Sfx);
        }

        void LateUpdate()
        {
            if (HitEffect != null)
                HitEffect.SetActive(false);

            if (CurrentHealth <= 0 && !deathHandled)
                Death();
        }

        public void Death()
        {
            if (deathHandled)
                return;

            deathHandled = true;
            TryAdvanceObjectiveOnDeath();

            Explosion.SetActive(true);
            Explosion.transform.parent = null;
            foreach (var obj in SpawnedObjects)
            {
                if (obj != null)
                    Instantiate(obj, transform.position, Quaternion.identity);
            }

            if (Wasp != null)
            {
                for (int spawnIndex = 0; spawnIndex < 30; spawnIndex++)
                    Instantiate(Wasp, transform.position, Quaternion.identity);
            }

            if (Hive != null)
                Destroy(Hive);
        }

        void TryAdvanceObjectiveOnDeath()
        {
            if (!advancesObjectiveOnDeath)
                return;

            var objectiveService = ObjectiveSyncService.Instance;
            if (objectiveService != null)
            {
                if (!objectiveService.TrySetObjectiveIndex(advanceToObjectiveIndex, ObjectiveAdvanceReason.HiveDestroyed))
                    Debug.LogWarning($"[Static_Hive] Objective advance to index {advanceToObjectiveIndex} did not apply.", this);
                return;
            }

            var playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject == null)
            {
                Debug.LogWarning("[Static_Hive] Cannot advance objective: Player tag not found.", this);
                return;
            }

            var wayPoint = playerObject.GetComponent<WayPoint>();
            if (wayPoint == null)
            {
                Debug.LogWarning("[Static_Hive] Cannot advance objective: Player has no WayPoint.", this);
                return;
            }

            if (!wayPoint.TryApplyObjectiveIndexDirect(advanceToObjectiveIndex))
                Debug.LogWarning($"[Static_Hive] Objective advance to index {advanceToObjectiveIndex} did not apply.", this);
        }

        public void TakeDamage(int Damage)
        {
            if (deathHandled || Damage <= 0)
                return;

            if (HitEffect != null)
                HitEffect.SetActive(true);

            CurrentHealth -= Damage;

            if (AudioSourceRouting.EnsureRoute(Sound, AudioSourceRoute.Sfx) && Sound.clip != null)
                GameplayAudio.TryPlayOneShot(Sound, Sound.clip, "HiveHit", 0.1f, 1f, 1f);

            if (HiveBar != null)
                HiveBar.SetNPCHealth(CurrentHealth);
        }
    }
}
