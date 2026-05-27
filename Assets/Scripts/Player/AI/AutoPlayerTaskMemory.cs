using System.Collections.Generic;
using UnityEngine;

namespace Beavermania.Player.AI
{
    public class AutoPlayerTaskMemory : MonoBehaviour
    {
        const int DefaultBlacklistCapacity = 32;

        [SerializeField] float defaultBlacklistDuration = 12f;

        readonly Dictionary<int, float> _blacklistUntil = new Dictionary<int, float>(DefaultBlacklistCapacity);
        const int WaypointBlacklistCapacity = 24;
        const float WaypointBlacklistCellSize = 2f;

        Vector3 _lastExplorePoint;
        bool _hasLastExplorePoint;
        int _carriedLogCount;
        float _nextShopVisitAllowedTime;
        readonly Dictionary<int, float> _waypointBlacklistUntil = new Dictionary<int, float>(WaypointBlacklistCapacity);

        public int CarriedLogCount => _carriedLogCount;
        public Vector3 LastExplorePoint => _lastExplorePoint;
        public bool HasLastExplorePoint => _hasLastExplorePoint;
        public float NextShopVisitAllowedTime
        {
            get => _nextShopVisitAllowedTime;
            set => _nextShopVisitAllowedTime = value;
        }

        public void SetCarriedLogCount(int count) => _carriedLogCount = count;

        public void RememberExplorePoint(Vector3 point)
        {
            _lastExplorePoint = point;
            _hasLastExplorePoint = true;
        }

        public void ScheduleNextShopVisit(float cooldownSeconds)
        {
            _nextShopVisitAllowedTime = Time.time + Mathf.Max(0f, cooldownSeconds);
        }

        static int WaypointCellKey(Vector3 point)
        {
            int x = Mathf.RoundToInt(point.x / WaypointBlacklistCellSize);
            int z = Mathf.RoundToInt(point.z / WaypointBlacklistCellSize);
            unchecked
            {
                return (x * 397) ^ z;
            }
        }

        public bool IsWaypointBlacklisted(Vector3 point)
        {
            int key = WaypointCellKey(point);
            if (!_waypointBlacklistUntil.TryGetValue(key, out float until))
                return false;

            if (Time.time >= until)
            {
                _waypointBlacklistUntil.Remove(key);
                return false;
            }

            return true;
        }

        public void BlacklistWaypoint(Vector3 point, float durationSeconds = -1f)
        {
            float duration = durationSeconds > 0f ? durationSeconds : defaultBlacklistDuration;
            _waypointBlacklistUntil[WaypointCellKey(point)] = Time.time + duration;
        }

        public bool IsBlacklisted(GameObject target)
        {
            if (target == null)
                return true;

            int id = target.GetInstanceID();
            if (!_blacklistUntil.TryGetValue(id, out float until))
                return false;

            if (Time.time >= until)
            {
                _blacklistUntil.Remove(id);
                return false;
            }

            return true;
        }

        public void Blacklist(GameObject target, float durationSeconds = -1f)
        {
            if (target == null)
                return;

            float duration = durationSeconds > 0f ? durationSeconds : defaultBlacklistDuration;
            _blacklistUntil[target.GetInstanceID()] = Time.time + duration;
        }

        public void ClearExpiredBlacklists()
        {
            if (_blacklistUntil.Count == 0)
                return;

            float now = Time.time;
            List<int> toRemove = null;
            foreach (var pair in _blacklistUntil)
            {
                if (now >= pair.Value)
                {
                    toRemove ??= new List<int>(4);
                    toRemove.Add(pair.Key);
                }
            }

            if (toRemove == null)
                return;

            for (int i = 0; i < toRemove.Count; i++)
                _blacklistUntil.Remove(toRemove[i]);
        }
    }
}
