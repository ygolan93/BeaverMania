using UnityEngine;

namespace Beavermania.Player.AI
{
    public enum PlaystyleActivityKind : byte
    {
        Explore = 0,
        Combat = 1,
        ChopTree = 2,
        CollectLog = 3,
        BuildBridge = 4,
        Interact = 5
    }

    /// <summary>
    /// One recorded snapshot while the human controls the player (AutoPlayer off).
    /// </summary>
    public struct AutoPlayerPlaystyleSample
    {
        public float Time;
        public Vector3 Position;
        public Vector3 MoveDirection;
        public string ThreatTag;
        public float ThreatDistance;
        public string ActiveArsenal;
        public AutoCombatActionKind InferredAction;
        public PlaystyleActivityKind Activity;
        public bool SprintHeld;
        public bool JumpPressed;
        public bool RollHeld;
        public bool PrimaryHeld;
        public bool SecondaryHeld;
        public bool DefendHeld;
        public bool Grounded;
        public float HealthRatio;
        public float StaminaRatio;
        public int CarriedLogs;
        public bool NearPrimaryBridge;
        public bool ObstacleBlockedAhead;
        public float ObstacleAheadDistance;
    }
}
