using System.Collections.Generic;
using UnityEngine;

namespace Beavermania.NPC
{
    static class ActiveWaspRegistry
    {
        static readonly List<NPC_Basic> ActiveWasps = new List<NPC_Basic>(32);

        public static void Register(NPC_Basic wasp)
        {
            if (wasp == null || ActiveWasps.Contains(wasp))
                return;

            ActiveWasps.Add(wasp);
        }

        public static void Unregister(NPC_Basic wasp)
        {
            if (wasp == null)
                return;

            ActiveWasps.Remove(wasp);
        }

        public static bool TryFindNearestOtherWaspCollider(NPC_Basic self, out Collider otherCollider, out GameObject otherWasp)
        {
            otherCollider = null;
            otherWasp = null;

            if (self == null)
                return false;

            float bestDistanceSq = float.MaxValue;
            Vector3 selfPosition = self.transform.position;

            for (int i = ActiveWasps.Count - 1; i >= 0; i--)
            {
                NPC_Basic candidate = ActiveWasps[i];
                if (candidate == null)
                {
                    ActiveWasps.RemoveAt(i);
                    continue;
                }

                if (candidate == self)
                    continue;

                float distanceSq = (candidate.transform.position - selfPosition).sqrMagnitude;
                if (distanceSq >= bestDistanceSq)
                    continue;

                Collider candidateCollider = candidate.GetComponent<Collider>();
                if (candidateCollider == null)
                    continue;

                bestDistanceSq = distanceSq;
                otherCollider = candidateCollider;
                otherWasp = candidate.gameObject;
            }

            return otherCollider != null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetRegistry()
        {
            ActiveWasps.Clear();
        }
    }
}
