using UnityEngine;
using BeaverPlayer = Beavermania.Player.BeaverPlayerBehaviour;

namespace Beavermania.UI.Objectives
{
    public class ObjectiveUI : MonoBehaviour
    {
        public BeaverPlayer Player;
        public string[] Objective;
        public int i;
        public WayPoint currentPoint;
        public string Instruction;

        WayPoint cachedWayPoint;

        void Awake()
        {
            if (currentPoint == null)
                currentPoint = GetComponent<WayPoint>();

            cachedWayPoint = currentPoint != null ? currentPoint : GetComponent<WayPoint>();
            if (Player == null)
                Player = GetComponent<BeaverPlayer>();
        }

        public void Update()
        {
            WayPoint wp = cachedWayPoint != null ? cachedWayPoint : ResolveWayPoint();
            if (wp == null)
                return;

            i = wp.i;
            if (Objective == null || i < 0 || i >= Objective.Length)
                return;

            Instruction = Objective[i];
        }

        public void UpdateObjective()
        {
            WayPoint wp = cachedWayPoint != null ? cachedWayPoint : ResolveWayPoint();
            if (wp == null)
                return;

            wp.AdvanceToNext();
        }

        WayPoint ResolveWayPoint()
        {
            if (currentPoint == null)
                currentPoint = GetComponent<WayPoint>();

            cachedWayPoint = currentPoint;
            return cachedWayPoint;
        }
    }
}
