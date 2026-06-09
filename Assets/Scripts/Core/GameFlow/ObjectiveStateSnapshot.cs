using UnityEngine;

namespace Beavermania.Core.GameFlow
{
    public enum ObjectiveAdvanceReason
    {
        Bootstrap = 0,
        RefreshBindings = 1,
        DialogueCompleted = 2,
        BossDialogueCompleted = 3,
        WaypointTrigger = 4,
        LegacyWaypointAdvanceRequest = 5,
        HiveDestroyed = 6,
        ObjectiveTargetDestroyed = 7,
        LogCollected = 8,
        PlayerNearBridgeFrame = 9,
        BridgeConstructionLocked = 10,
        BridgeCompleted = 11
    }

    public readonly struct ObjectiveStateSnapshot
    {
        public ObjectiveStateSnapshot(
            int previousObjectiveIndex,
            int currentObjectiveIndex,
            string currentObjectiveText,
            int currentWaypointIndex,
            Transform currentWaypointTarget,
            ObjectiveAdvanceReason reason)
        {
            PreviousObjectiveIndex = previousObjectiveIndex;
            CurrentObjectiveIndex = currentObjectiveIndex;
            CurrentObjectiveText = currentObjectiveText;
            CurrentWaypointIndex = currentWaypointIndex;
            CurrentWaypointTarget = currentWaypointTarget;
            Reason = reason;
        }

        public int PreviousObjectiveIndex { get; }

        public int CurrentObjectiveIndex { get; }

        public string CurrentObjectiveText { get; }

        public int CurrentWaypointIndex { get; }

        public Transform CurrentWaypointTarget { get; }

        public ObjectiveAdvanceReason Reason { get; }
    }
}
