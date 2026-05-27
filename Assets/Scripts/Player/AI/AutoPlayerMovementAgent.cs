using UnityEngine;

namespace Beavermania.Player.AI
{
    public class AutoPlayerMovementAgent : MonoBehaviour
    {
        const float DirectionEpsilon = 1e-6f;
        const int ObstacleRayCount = 5;

        [SerializeField] AutoPlayerActionAdapter adapter;
        [SerializeField] Transform body;
        [SerializeField] float interactionDistance = 1.8f;
        [SerializeField] float sprintDistance = 6f;
        [SerializeField] float obstacleAvoidDistance = 1.2f;
        [SerializeField] float stuckDistanceThreshold = 0.35f;
        [SerializeField] float stuckTimeThreshold = 2.5f;
        [SerializeField] LayerMask obstacleLayers = ~0;

        readonly RaycastHit[] _rayHits = new RaycastHit[4];

        Vector3 _currentDestination;
        bool _hasDestination;
        float _stuckTimer;
        float _lastDistanceToTarget = float.MaxValue;
        Vector3 _lastPosition;

        public bool IsStuck { get; private set; }

        public void ResetStuck()
        {
            IsStuck = false;
            _stuckTimer = 0f;
            _lastDistanceToTarget = float.MaxValue;
        }
        public float StuckTimer => _stuckTimer;
        public Vector3 CurrentDestination => _currentDestination;
        public bool HasDestination => _hasDestination;
        public float DistanceToDestination { get; private set; }

        void Awake()
        {
            if (adapter == null)
                adapter = GetComponent<AutoPlayerActionAdapter>();
            if (body == null)
                body = transform;
            _lastPosition = body != null ? body.position : Vector3.zero;
        }

        public void SetDestination(Vector3 worldPoint)
        {
            _currentDestination = worldPoint;
            _hasDestination = true;
            _stuckTimer = 0f;
            _lastDistanceToTarget = float.MaxValue;
            IsStuck = false;
        }

        public void ClearDestination()
        {
            _hasDestination = false;
            IsStuck = false;
            _stuckTimer = 0f;
            if (adapter != null)
                adapter.SetWorldMoveDirection(Vector3.zero);
        }

        public void TickMovement(bool allowJump)
        {
            if (adapter == null || body == null || !_hasDestination)
            {
                if (adapter != null)
                    adapter.SetWorldMoveDirection(Vector3.zero);
                return;
            }

            Vector3 toTarget = _currentDestination - body.position;
            toTarget.y = 0f;
            DistanceToDestination = toTarget.magnitude;

            if (DistanceToDestination <= interactionDistance)
            {
                adapter.SetWorldMoveDirection(Vector3.zero);
                adapter.SetSprint(false);
                UpdateStuckDetection(true);
                return;
            }

            Vector3 direction = toTarget.normalized;
            direction = ApplyObstacleAvoidance(direction);

            if (direction.sqrMagnitude < DirectionEpsilon)
            {
                adapter.SetWorldMoveDirection(Vector3.zero);
                UpdateStuckDetection(false);
                return;
            }

            adapter.SetWorldMoveDirection(direction);
            adapter.SetSprint(DistanceToDestination > sprintDistance);

            if (allowJump && adapter.IsGrounded && NeedsJump(direction))
                adapter.RequestJump();

            UpdateStuckDetection(false);
        }

        Vector3 ApplyObstacleAvoidance(Vector3 desiredDirection)
        {
            if (body == null)
                return desiredDirection;

            Vector3 origin = body.position + Vector3.up * 0.4f;
            Vector3 best = desiredDirection;
            float bestClearance = 0f;

            for (int i = 0; i < ObstacleRayCount; i++)
            {
                float angle = (i - ObstacleRayCount / 2) * 25f;
                Vector3 dir = Quaternion.Euler(0f, angle, 0f) * desiredDirection;
                if (dir.sqrMagnitude < DirectionEpsilon)
                    continue;

                dir.Normalize();
                bool blocked = Physics.Raycast(origin, dir, obstacleAvoidDistance, obstacleLayers, QueryTriggerInteraction.Ignore);
                float clearance = blocked ? 0f : 1f;
                if (clearance > bestClearance)
                {
                    bestClearance = clearance;
                    best = dir;
                }
            }

            return best;
        }

        bool NeedsJump(Vector3 moveDirection)
        {
            if (body == null)
                return false;

            Vector3 origin = body.position + Vector3.up * 0.2f;
            if (!Physics.Raycast(origin, moveDirection, out RaycastHit hit, 1.2f, obstacleLayers, QueryTriggerInteraction.Ignore))
                return false;

            float stepHeight = hit.point.y - body.position.y;
            return stepHeight > 0.25f && stepHeight < 1.1f;
        }

        void UpdateStuckDetection(bool atDestination)
        {
            if (body == null)
                return;

            if (atDestination)
            {
                IsStuck = false;
                _stuckTimer = 0f;
                _lastPosition = body.position;
                return;
            }

            float moved = (body.position - _lastPosition).magnitude;
            _lastPosition = body.position;

            float progress = _lastDistanceToTarget - DistanceToDestination;
            _lastDistanceToTarget = DistanceToDestination;

            if (moved < stuckDistanceThreshold * Time.deltaTime && progress < stuckDistanceThreshold * 0.5f)
                _stuckTimer += Time.deltaTime;
            else
                _stuckTimer = 0f;

            IsStuck = _stuckTimer >= stuckTimeThreshold;
        }

        public Vector3 GetRecoveryDirection()
        {
            if (body == null)
                return Vector3.zero;

            Vector3 back = -body.forward;
            back.y = 0f;
            if (back.sqrMagnitude < DirectionEpsilon)
                back = Vector3.back;
            return back.normalized;
        }
    }
}
