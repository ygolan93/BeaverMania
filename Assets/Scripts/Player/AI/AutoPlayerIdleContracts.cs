using UnityEngine;

namespace Beavermania.Player.AI
{
    public enum IdleActionKind
    {
        None,
        KeepMoving,
        BuildBridge,
        VisitTrader,
        SwitchArsenal
    }

    public struct IdlePlannerResult
    {
        public IdleActionKind Action;
        public Transform Target;
        public float Score;
        public string ArsenalEntryName;
    }

    public struct TerrainProbeSnapshot
    {
        public bool IsOnWalkableGround;
        public bool HasGroundAhead;
        public bool CliffAhead;
        public bool HazardAhead;
        public float ForwardDropDepth;
        public Vector3 SuggestedAvoidDirection;
        public Vector3 NearestWalkablePoint;
    }
