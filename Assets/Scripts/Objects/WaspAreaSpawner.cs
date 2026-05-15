using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Beavermania.Objects
{
    public class WaspAreaSpawner : MonoBehaviour
    {
        const int MaxSpawnAttemptsPerWasp = 12;
        const float NullCheckInterval = 0.5f;
        const float MinGroundNormalY = 0.5f;
        const float SpawnClearanceRadius = 0.4f;
        const float SpawnClearanceHeight = 0.5f;

        [SerializeField] GameObject waspPrefab;
        [SerializeField] int maxActiveWasps = 5;
        [SerializeField] float respawnDelayMin = 3f;
        [SerializeField] float respawnDelayMax = 8f;
        [SerializeField] Vector3 spawnAreaSize = new Vector3(30f, 0f, 30f);
        [SerializeField] Transform spawnAreaCenter;
        [SerializeField] bool spawnOnStart = true;
        [SerializeField] LayerMask groundLayer;
        [SerializeField] float groundCheckHeight = 10f;
        [SerializeField] float groundCheckDistance = 30f;

        readonly List<GameObject> activeWasps = new List<GameObject>();

        Coroutine respawnCoroutine;
        float nullCheckTimer;

        void Start()
        {
            if (respawnDelayMin > respawnDelayMax)
            {
                float swap = respawnDelayMin;
                respawnDelayMin = respawnDelayMax;
                respawnDelayMax = swap;
            }

            respawnDelayMin = Mathf.Max(0f, respawnDelayMin);
            respawnDelayMax = Mathf.Max(0f, respawnDelayMax);

            if (spawnOnStart)
                FillToMax();
        }

        void Update()
        {
            nullCheckTimer += Time.deltaTime;
            if (nullCheckTimer < NullCheckInterval)
                return;

            nullCheckTimer = 0f;
            PurgeDestroyedWasps();
            TryScheduleRespawn();
        }

        void OnDisable()
        {
            StopAllCoroutines();
            respawnCoroutine = null;
        }

        public void NotifyWaspDied(GameObject wasp)
        {
            if (wasp == null)
                return;

            activeWasps.Remove(wasp);
            TryScheduleRespawn();
        }

        public void FillToMax()
        {
            if (!CanSpawn())
                return;

            PurgeDestroyedWasps();

            while (activeWasps.Count < maxActiveWasps)
            {
                if (!TrySpawnOneWasp())
                    break;
            }
        }

        void TryScheduleRespawn()
        {
            if (!CanSpawn())
                return;

            PurgeDestroyedWasps();

            if (activeWasps.Count >= maxActiveWasps || respawnCoroutine != null)
                return;

            respawnCoroutine = StartCoroutine(RespawnCoroutine());
        }

        IEnumerator RespawnCoroutine()
        {
            float delay = Random.Range(respawnDelayMin, respawnDelayMax);
            yield return new WaitForSeconds(delay);

            respawnCoroutine = null;

            if (!CanSpawn())
                yield break;

            PurgeDestroyedWasps();

            int missing = maxActiveWasps - activeWasps.Count;
            for (int i = 0; i < missing; i++)
            {
                if (!TrySpawnOneWasp())
                    break;
            }

            TryScheduleRespawn();
        }

        bool CanSpawn()
        {
            return waspPrefab != null && maxActiveWasps > 0;
        }

        bool TrySpawnOneWasp()
        {
            if (!CanSpawn() || activeWasps.Count >= maxActiveWasps)
                return false;

            if (!TryFindGroundPosition(out Vector3 spawnPosition))
                return false;

            Quaternion rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            GameObject wasp = Instantiate(waspPrefab, spawnPosition, rotation);

            SpawnedWaspTracker tracker = wasp.GetComponent<SpawnedWaspTracker>();
            if (tracker == null)
                tracker = wasp.AddComponent<SpawnedWaspTracker>();

            tracker.Initialize(this);
            activeWasps.Add(wasp);
            return true;
        }

        bool TryFindGroundPosition(out Vector3 position)
        {
            position = default;

            Vector3 center = GetSpawnAreaCenter();
            float rayLength = groundCheckHeight + groundCheckDistance;

            for (int attempt = 0; attempt < MaxSpawnAttemptsPerWasp; attempt++)
            {
                Vector3 candidate = center + GetRandomOffsetInArea();
                Vector3 rayOrigin = candidate + Vector3.up * groundCheckHeight;

                if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, rayLength, groundLayer, QueryTriggerInteraction.Ignore))
                    continue;

                if (hit.normal.y < MinGroundNormalY)
                    continue;

                Vector3 clearanceCenter = hit.point + Vector3.up * SpawnClearanceHeight;
                if (Physics.CheckSphere(clearanceCenter, SpawnClearanceRadius, groundLayer, QueryTriggerInteraction.Ignore))
                    continue;

                position = hit.point;
                return true;
            }

            return false;
        }

        Vector3 GetSpawnAreaCenter()
        {
            return spawnAreaCenter != null ? spawnAreaCenter.position : transform.position;
        }

        Vector3 GetRandomOffsetInArea()
        {
            return new Vector3(
                Random.Range(-spawnAreaSize.x * 0.5f, spawnAreaSize.x * 0.5f),
                Random.Range(-spawnAreaSize.y * 0.5f, spawnAreaSize.y * 0.5f),
                Random.Range(-spawnAreaSize.z * 0.5f, spawnAreaSize.z * 0.5f));
        }

        void PurgeDestroyedWasps()
        {
            for (int i = activeWasps.Count - 1; i >= 0; i--)
            {
                if (activeWasps[i] == null)
                    activeWasps.RemoveAt(i);
            }
        }

        void OnDrawGizmosSelected()
        {
            Vector3 center = spawnAreaCenter != null ? spawnAreaCenter.position : transform.position;
            Gizmos.color = new Color(1f, 0.6f, 0f, 0.85f);
            Gizmos.DrawWireCube(center, spawnAreaSize);
        }
    }
}
