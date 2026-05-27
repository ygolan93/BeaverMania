using UnityEngine;

namespace Beavermania.Player.AI
{
    [DisallowMultipleComponent]
    public class AutoPlayerTerrainSense : MonoBehaviour
    {
        const float DirectionEpsilon = 1e-6f;

        [SerializeField] Transform probeOrigin;
        [SerializeField] LayerMask groundLayers = ~0;
        [SerializeField] LayerMask hazardLayers;
        [SerializeField] float probeRadius = 2.5f;
        [SerializeField] float forwardProbeDistance = 2.2f;
        [SerializeField] float groundRayHeight = 4f;
        [SerializeField] float groundRayLength = 12f;
        [SerializeField] float maxSlopeAngle = 48f;
        [SerializeField] float cliffDropThreshold = 1.4f;
        [SerializeField] float updateInterval = 0.15f;

        readonly Vector3[] _probeDirections = new Vector3[9];
        readonly RaycastHit[] _hits = new RaycastHit[1];

        float _updateTimer;
        TerrainProbeSnapshot _snapshot;

        public TerrainProbeSnapshot Snapshot => _snapshot;
        public LayerMask GroundLayers => groundLayers;
        public LayerMask HazardLayers => hazardLayers;

        void Awake()
        {
            if (probeOrigin == null)
                probeOrigin = transform;
            RebuildProbeDirections();
        }

        void Start()
        {
            var perception = GetComponent<AutoPlayerPerception>();
            if (perception == null)
                return;

            if (groundLayers.value == ~0)
                groundLayers = perception.GroundLayers;
            if (hazardLayers.value == 0)
                hazardLayers = perception.HazardLayers;
        }

        void RebuildProbeDirections()
        {
            _probeDirections[0] = Vector3.zero;
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f * Mathf.Deg2Rad;
                _probeDirections[i + 1] = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            }
        }

        public void TickSense()
        {
            _updateTimer -= Time.deltaTime;
            if (_updateTimer > 0f)
                return;

            _updateTimer = updateInterval;
            _snapshot = BuildSnapshot();
        }

        TerrainProbeSnapshot BuildSnapshot()
        {
            var result = new TerrainProbeSnapshot
            {
                IsOnWalkableGround = false,
                HasGroundAhead = false,
                CliffAhead = false,
                HazardAhead = false,
                ForwardDropDepth = 0f,
                SuggestedAvoidDirection = Vector3.zero,
                NearestWalkablePoint = probeOrigin != null ? probeOrigin.position : transform.position
            };

            if (probeOrigin == null)
                return result;

            Vector3 origin = probeOrigin.position;
            float bestWalkableScore = -1f;
            Vector3 bestWalkable = origin;

            for (int i = 0; i < _probeDirections.Length; i++)
            {
                Vector3 offset = _probeDirections[i] * (i == 0 ? 0.2f : probeRadius);
                Vector3 sample = origin + offset;
                if (!TrySampleGround(sample, out RaycastHit hit))
                    continue;

                bool walkable = IsWalkableNormal(hit.normal);
                if (!walkable)
                    continue;

                float score = 1f / (offset.sqrMagnitude + 0.1f);
                if (score > bestWalkableScore)
                {
                    bestWalkableScore = score;
                    bestWalkable = hit.point;
                }

                if (i == 0)
                    result.IsOnWalkableGround = true;
            }

            result.NearestWalkablePoint = bestWalkable;

            Vector3 forward = probeOrigin.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < DirectionEpsilon)
                forward = transform.forward;

            forward.Normalize();
            Vector3 ahead = origin + forward * forwardProbeDistance;

            if (TrySampleGround(ahead, out RaycastHit aheadHit) && IsWalkableNormal(aheadHit.normal))
            {
                result.HasGroundAhead = true;
            }
            else if (TrySampleGround(origin, out RaycastHit originHit))
            {
                float aheadHeight;
                if (TrySampleGround(ahead, out aheadHit))
                    aheadHeight = aheadHit.point.y;
                else
                    aheadHeight = originHit.point.y - cliffDropThreshold * 2f;

                float drop = originHit.point.y - aheadHeight;
                result.ForwardDropDepth = Mathf.Max(0f, drop);
                if (!result.HasGroundAhead && result.ForwardDropDepth >= cliffDropThreshold)
                    result.CliffAhead = true;
            }

            if (hazardLayers.value != 0)
            {
                Vector3 rayOrigin = origin + Vector3.up * 0.5f;
                if (Physics.Raycast(rayOrigin, forward, forwardProbeDistance, hazardLayers, QueryTriggerInteraction.Ignore))
                {
                    result.HazardAhead = true;
                    result.SuggestedAvoidDirection = Vector3.Cross(Vector3.up, forward).normalized;
                }
            }

            if (result.CliffAhead && result.SuggestedAvoidDirection.sqrMagnitude < DirectionEpsilon)
            {
                Vector3 left = Vector3.Cross(Vector3.up, forward).normalized;
                Vector3 right = -left;
                bool leftOk = IsDirectionWalkable(origin, left);
                bool rightOk = IsDirectionWalkable(origin, right);
                if (leftOk && !rightOk)
                    result.SuggestedAvoidDirection = left;
                else if (rightOk && !leftOk)
                    result.SuggestedAvoidDirection = right;
                else
                    result.SuggestedAvoidDirection = left;
            }

            return result;
        }

        bool IsDirectionWalkable(Vector3 origin, Vector3 direction)
        {
            Vector3 sample = origin + direction * probeRadius;
            return TrySampleGround(sample, out RaycastHit hit) && IsWalkableNormal(hit.normal);
        }

        public bool TrySampleGround(Vector3 near, out RaycastHit hit)
        {
            Vector3 rayStart = near + Vector3.up * groundRayHeight;
            if (Physics.Raycast(rayStart, Vector3.down, out hit, groundRayLength, groundLayers, QueryTriggerInteraction.Ignore))
                return true;

            hit = default;
            return false;
        }

        public bool IsWalkableNormal(Vector3 normal)
        {
            float slope = Vector3.Angle(normal, Vector3.up);
            return slope <= maxSlopeAngle;
        }

        public bool IsWalkablePoint(Vector3 worldPoint)
        {
            if (!TrySampleGround(worldPoint, out RaycastHit hit))
                return false;

            return IsWalkableNormal(hit.normal);
        }

        void OnDrawGizmosSelected()
        {
            if (probeOrigin == null)
                probeOrigin = transform;

            Gizmos.color = _snapshot.IsOnWalkableGround ? Color.green : Color.red;
            Gizmos.DrawWireSphere(probeOrigin.position, 0.25f);

            if (_snapshot.CliffAhead)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(probeOrigin.position, probeOrigin.position + probeOrigin.forward * forwardProbeDistance);
            }
        }
    }
}
