using System.Collections.Generic;
using UnityEngine;

namespace Beavermania.Player.AI
{
    public class AutoPlayerTaskMemory : MonoBehaviour
    {
        const int DefaultBlacklistCapacity = 32;

        [SerializeField] float defaultBlacklistDuration = 12f;

        readonly Dictionary<int, float> _blacklistUntil = new Dictionary<int, float>(DefaultBlacklistCapacity);
        Vector3 _lastExplorePoint;
        bool _hasLastExplorePoint;
        int _carriedLogCount;

        public int CarriedLogCount => _carriedLogCount;
        public Vector3 LastExplorePoint => _lastExplorePoint;
        public bool HasLastExplorePoint => _hasLastExplorePoint;

        public void SetCarriedLogCount(int count) => _carriedLogCount = count;

        public void RememberExplorePoint(Vector3 point)
        {
            _lastExplorePoint = point;
            _hasLastExplorePoint = true;
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
