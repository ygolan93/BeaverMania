using Beavermania.Objects;
using Beavermania.Player.Movement;
using UnityEngine;

namespace Beavermania.Player.AI
{
    [DisallowMultipleComponent]
    public class AutoBridgeBuildPoint : MonoBehaviour, IAutoBuildTarget
    {
        [SerializeField] NewConstructor newConstructor;
        [SerializeField] Transform buildPoint;
        [SerializeField] Carry oldBridgeCarry;
        [SerializeField] int oldBridgeLogsRequired = 9;

        public bool NeedsLogs
        {
            get
            {
                if (newConstructor != null)
                    return !newConstructor.isLocked || newConstructor.PartCount < newConstructor.BridgeLimit;
                if (oldBridgeCarry != null)
                    return oldBridgeCarry.i < oldBridgeLogsRequired;
                return true;
            }
        }

        public int LogsRequired
        {
            get
            {
                if (newConstructor != null)
                    return newConstructor.BridgeLimit;
                return oldBridgeLogsRequired;
            }
        }

        public int LogsApplied
        {
            get
            {
                if (newConstructor != null)
                    return newConstructor.PartCount;
                if (oldBridgeCarry != null)
                    return oldBridgeCarry.i;
                return 0;
            }
        }

        public bool IsComplete
        {
            get
            {
                if (newConstructor != null)
                    return newConstructor.PartCount >= newConstructor.BridgeLimit && newConstructor.isLocked;
                if (oldBridgeCarry != null)
                    return false;
                return false;
            }
        }

        public Transform BuildPoint => buildPoint != null ? buildPoint : transform;

        void Awake()
        {
            if (newConstructor == null)
                newConstructor = GetComponent<NewConstructor>();
            if (buildPoint == null)
                buildPoint = transform;
        }
    }
}
