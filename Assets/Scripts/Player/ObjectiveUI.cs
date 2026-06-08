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
            SyncMirrorFromWayPoint();
        }

        public void UpdateObjective()
        {
            var objectiveService = Beavermania.Core.GameFlow.ObjectiveSyncService.Instance;
            if (objectiveService != null)
            {
                bool advanced = objectiveService.TryAdvanceObjective(1, Beavermania.Core.GameFlow.ObjectiveAdvanceReason.DialogueCompleted);
                if (advanced || !objectiveService.ShouldUseLegacyObjectiveFallback())
                    return;
            }

            WayPoint wp = cachedWayPoint != null ? cachedWayPoint : ResolveWayPoint();
            if (wp == null)
                return;

            if (wp.TryAdvanceToNextDirect())
                SyncMirrorFromWayPoint();
        }

        internal bool TryGetObjectiveText(int index, out string objectiveText)
        {
            objectiveText = null;
            if (Objective == null || index < 0 || index >= Objective.Length)
                return false;

            objectiveText = Objective[index];
            return true;
        }

        internal void ApplyObjectiveMirror(int index, string instruction)
        {
            i = index;
            Instruction = instruction ?? string.Empty;
        }

        WayPoint ResolveWayPoint()
        {
            if (currentPoint == null)
                currentPoint = GetComponent<WayPoint>();

            cachedWayPoint = currentPoint;
            return cachedWayPoint;
        }

        void SyncMirrorFromWayPoint()
        {
            WayPoint wp = cachedWayPoint != null ? cachedWayPoint : ResolveWayPoint();
            if (wp == null)
                return;

            i = wp.i;
            if (TryGetObjectiveText(i, out string objectiveText))
                Instruction = objectiveText;
        }
    }
}
