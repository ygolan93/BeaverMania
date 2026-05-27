using UnityEngine;

namespace Beavermania.Player.AI
{
    [DisallowMultipleComponent]
    public class AutoPlayerNavigationPlanner : MonoBehaviour
    {
        const float DirectionEpsilon = 1e-6f;

        [SerializeField] AutoPlayerTerrainSense terrainSense;
        [SerializeField] Transform body;
        [SerializeField] float maxStepHeight = 1.05f;
        [SerializeField] float waypointSpacing = 1.5f;
        [SerializeField] int maxSteps = 6;
        [SerializeField] LayerMask obstacleLayers = ~0;

        void Awake()
        {
            if (terrainSense == null)
                terrainSense = GetComponent<AutoPlayerTerrainSense>();
            if (body == null)
                body = transform;
        }

        public bool TryGetReachablePoint(Vector3 goal, out Vector3 waypoint)
        {
            waypoint = goal;
            if (body == null || terrainSense == null)
                return false;

            Vector3 start = body.position;
            Vector3 flatGoal = goal;
            flatGoal.y = start.y;

            if (IsDirectlyReachable(start, goal))
            {
                waypoint = goal;
                return true;
            }

            Vector3 direction = flatGoal - start;
            direction.y = 0f;
            float distance = direction.magnitude;
            if (distance < DirectionEpsilon)
                return false;

            direction /= distance;
            int steps = Mathf.Min(maxSteps, Mathf.CeilToInt(distance / waypointSpacing));
            Vector3 cursor = start;

            for (int i = 0; i < steps; i++)
            {
                float stepDist = Mathf.Min(waypointSpacing, distance - i * waypointSpacing);
                Vector3 next = cursor + direction * stepDist;
                if (!terrainSense.TrySampleGround(next, out RaycastHit hit))
                    return false;

                if (!terrainSense.IsWalkableNormal(hit.normal))
                    return false;

                float heightDelta = hit.point.y - cursor.y;
                if (heightDelta > maxStepHeight || heightDelta < -maxStepHeight * 1.5f)
                    return false;

                if (IsBlocked(cursor, hit.point))
                    return false;

                cursor = hit.point;
            }

            waypoint = cursor;
            return true;
        }

        public bool TryValidateExplorePoint(Vector3 candidate, AutoPlayerTaskMemory memory, out Vector3 validated)
        {
            validated = candidate;
            if (terrainSense == null)
                return false;

            if (!terrainSense.TrySampleGround(candidate, out RaycastHit hit))
                return false;

            if (!terrainSense.IsWalkableNormal(hit.normal))
                return false;

            validated = hit.point;

            if (memory != null && memory.IsWaypointBlacklisted(validated))
                return false;

            return TryGetReachablePoint(validated, out validated);
        }

        bool IsDirectlyReachable(Vector3 from, Vector3 to)
        {
            if (terrainSense == null)
                return false;

            if (!terrainSense.TrySampleGround(to, out RaycastHit goalHit))
                return false;

            if (!terrainSense.IsWalkableNormal(goalHit.normal))
                return false;

            float heightDelta = goalHit.point.y - from.y;
            if (heightDelta > maxStepHeight || heightDelta < -maxStepHeight * 1.5f)
                return false;

            return !IsBlocked(from, goalHit.point);
        }

        bool IsBlocked(Vector3 from, Vector3 to)
        {
            Vector3 delta = to - from;
            float dist = delta.magnitude;
            if (dist < DirectionEpsilon)
                return false;

            Vector3 origin = from + Vector3.up * 0.45f;
            return Physics.Raycast(origin, delta.normalized, dist, obstacleLayers, QueryTriggerInteraction.Ignore);
        }
    }
}
