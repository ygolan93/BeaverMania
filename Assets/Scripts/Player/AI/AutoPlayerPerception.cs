using Beavermania.NPC;
using Beavermania.Objects;
using UnityEngine;

namespace Beavermania.Player.AI
{
    public class AutoPlayerPerception : MonoBehaviour
    {
        const int OverlapBufferSize = 48;
        const float LookRotationEpsilon = 1e-6f;

        [Header("Radii")]
        [SerializeField] float combatDetectionRadius = 12f;
        [SerializeField] float npcDetectionRadius = 8f;
        [SerializeField] float treeDetectionRadius = 14f;
        [SerializeField] float logDetectionRadius = 10f;
        [SerializeField] float bridgeDetectionRadius = 16f;

        [Header("Layers")]
        [SerializeField] LayerMask enemyLayers = ~0;
        [SerializeField] LayerMask worldLayers = ~0;
        [SerializeField] LayerMask hazardLayers;
        [SerializeField] LayerMask groundLayers = ~0;

        [Header("References")]
        [SerializeField] Transform scanOrigin;

        readonly Collider[] _overlapBuffer = new Collider[OverlapBufferSize];

        public Transform NearestThreat { get; private set; }
        public float NearestThreatDistance { get; private set; }
        public Transform NearestInteractable { get; private set; }
        public float NearestInteractableDistance { get; private set; }
        public Transform NearestTree { get; private set; }
        public float NearestTreeDistance { get; private set; }
        public Transform NearestLog { get; private set; }
        public float NearestLogDistance { get; private set; }
        public Transform NearestBridge { get; private set; }
        public float NearestBridgeDistance { get; private set; }
        public bool HazardAhead { get; private set; }
        public Vector3 HazardAvoidDirection { get; private set; }

        public float CombatDetectionRadius => combatDetectionRadius;

        void Awake()
        {
            if (scanOrigin == null)
                scanOrigin = transform;

            var player = GetComponent<BeaverPlayerBehaviour>();
            if (player != null)
                enemyLayers = player.enemyLayers;
        }

        public void Scan(AutoPlayerTaskMemory memory)
        {
            ClearResults();
            if (scanOrigin == null)
                return;

            memory?.ClearExpiredBlacklists();
            Vector3 origin = scanOrigin.position;

            ScanThreats(origin, memory);
            ScanInteractables(origin, memory);
            ScanTrees(origin, memory);
            ScanLogs(origin, memory);
            ScanBridges(origin, memory);
            ScanHazards(origin);
        }

        void ClearResults()
        {
            NearestThreat = null;
            NearestThreatDistance = float.MaxValue;
            NearestInteractable = null;
            NearestInteractableDistance = float.MaxValue;
            NearestTree = null;
            NearestTreeDistance = float.MaxValue;
            NearestLog = null;
            NearestLogDistance = float.MaxValue;
            NearestBridge = null;
            NearestBridgeDistance = float.MaxValue;
            HazardAhead = false;
            HazardAvoidDirection = Vector3.zero;
        }

        void ScanThreats(Vector3 origin, AutoPlayerTaskMemory memory)
        {
            int count = Physics.OverlapSphereNonAlloc(origin, combatDetectionRadius, _overlapBuffer, enemyLayers, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                Collider col = _overlapBuffer[i];
                if (col == null || !IsThreat(col.gameObject))
                    continue;

                if (memory != null && memory.IsBlacklisted(col.gameObject))
                    continue;

                float dist = Vector3.Distance(origin, col.transform.position);
                if (dist >= NearestThreatDistance)
                    continue;

                NearestThreatDistance = dist;
                NearestThreat = col.transform;
            }
        }

        static bool IsThreat(GameObject go)
        {
            if (go == null)
                return false;

            string tag = go.tag;
            return tag == "NPC" || tag == "Scorpion" || tag == "Hive";
        }

        void ScanInteractables(Vector3 origin, AutoPlayerTaskMemory memory)
        {
            int count = Physics.OverlapSphereNonAlloc(origin, npcDetectionRadius, _overlapBuffer, worldLayers, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                Collider col = _overlapBuffer[i];
                if (col == null)
                    continue;

                IAutoInteractable interactable = col.GetComponentInParent<IAutoInteractable>();
                Trader trader = interactable == null ? col.GetComponentInParent<Trader>() : null;
                if (interactable == null && trader == null)
                    continue;

                GameObject root = interactable != null ? (interactable as MonoBehaviour)?.gameObject : trader.gameObject;
                if (root == null || (memory != null && memory.IsBlacklisted(root)))
                    continue;

                float dist = Vector3.Distance(origin, col.transform.position);
                if (dist >= NearestInteractableDistance)
                    continue;

                NearestInteractableDistance = dist;
                NearestInteractable = root.transform;
            }
        }

        void ScanTrees(Vector3 origin, AutoPlayerTaskMemory memory)
        {
            int count = Physics.OverlapSphereNonAlloc(origin, treeDetectionRadius, _overlapBuffer, worldLayers, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                Collider col = _overlapBuffer[i];
                if (col == null)
                    continue;

                IAutoChoppable choppable = col.GetComponentInParent<IAutoChoppable>();
                LogSpawner spawner = choppable == null ? col.GetComponentInParent<LogSpawner>() : null;
                if (choppable == null && spawner == null)
                    continue;

                GameObject root = choppable != null
                    ? ((MonoBehaviour)choppable).gameObject
                    : spawner.gameObject;

                if (memory != null && memory.IsBlacklisted(root))
                    continue;

                if (choppable != null && !choppable.CanAutoChop(gameObject))
                    continue;

                float dist = Vector3.Distance(origin, col.transform.position);
                if (dist >= NearestTreeDistance)
                    continue;

                NearestTreeDistance = dist;
                NearestTree = choppable != null ? choppable.ChopTargetPoint : root.transform;
            }
        }

        void ScanLogs(Vector3 origin, AutoPlayerTaskMemory memory)
        {
            int count = Physics.OverlapSphereNonAlloc(origin, logDetectionRadius, _overlapBuffer, worldLayers, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                Collider col = _overlapBuffer[i];
                if (col == null)
                    continue;

                if (!col.CompareTag("Part"))
                    continue;

                if (memory != null && memory.IsBlacklisted(col.gameObject))
                    continue;

                float dist = Vector3.Distance(origin, col.transform.position);
                if (dist >= NearestLogDistance)
                    continue;

                NearestLogDistance = dist;
                NearestLog = col.transform;
            }
        }

        void ScanBridges(Vector3 origin, AutoPlayerTaskMemory memory)
        {
            int count = Physics.OverlapSphereNonAlloc(origin, bridgeDetectionRadius, _overlapBuffer, worldLayers, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                Collider col = _overlapBuffer[i];
                if (col == null)
                    continue;

                IAutoBuildTarget buildTarget = col.GetComponentInParent<IAutoBuildTarget>();
                NewConstructor constructor = buildTarget == null ? col.GetComponentInParent<NewConstructor>() : null;
                if (buildTarget == null && constructor == null)
                    continue;

                GameObject root = buildTarget != null
                    ? ((MonoBehaviour)buildTarget).gameObject
                    : constructor.gameObject;

                if (memory != null && memory.IsBlacklisted(root))
                    continue;

                if (buildTarget != null && buildTarget.IsComplete)
                    continue;

                float dist = Vector3.Distance(origin, col.transform.position);
                if (dist >= NearestBridgeDistance)
                    continue;

                NearestBridgeDistance = dist;
                NearestBridge = buildTarget != null ? buildTarget.BuildPoint : root.transform;
            }
        }

        void ScanHazards(Vector3 origin)
        {
            if (hazardLayers.value == 0)
                return;

            Vector3 forward = scanOrigin.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < LookRotationEpsilon)
                return;

            forward.Normalize();
            Vector3 probeOrigin = origin + Vector3.up * 0.5f;
            if (Physics.Raycast(probeOrigin, forward, 2f, hazardLayers, QueryTriggerInteraction.Ignore))
            {
                HazardAhead = true;
                HazardAvoidDirection = Vector3.Cross(Vector3.up, forward).normalized;
            }
        }

        public bool TryGetGroundPoint(Vector3 near, float maxRadius, out Vector3 groundPoint)
        {
            groundPoint = near;
            Vector3 rayStart = near + Vector3.up * 4f;
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 12f, groundLayers, QueryTriggerInteraction.Ignore))
            {
                groundPoint = hit.point;
                return true;
            }

            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * maxRadius;
                Vector3 sample = near + offset + Vector3.up * 4f;
                if (Physics.Raycast(sample, Vector3.down, out hit, 12f, groundLayers, QueryTriggerInteraction.Ignore))
                {
                    groundPoint = hit.point;
                    return true;
                }
            }

            return false;
        }

        void OnDrawGizmosSelected()
        {
            if (scanOrigin == null)
                scanOrigin = transform;

            Vector3 p = scanOrigin.position;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(p, combatDetectionRadius);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(p, npcDetectionRadius);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(p, treeDetectionRadius);
            Gizmos.color = new Color(0.6f, 0.4f, 0.1f);
            Gizmos.DrawWireSphere(p, logDetectionRadius);
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(p, bridgeDetectionRadius);

            if (NearestThreat != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(p, NearestThreat.position);
            }
        }
    }
}
