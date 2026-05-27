using Beavermania.Objects;
using UnityEngine;

namespace Beavermania.Player.AI
{
    [DisallowMultipleComponent]
    public class AutoChoppableTree : MonoBehaviour, IAutoChoppable
    {
        [SerializeField] Transform chopTargetPoint;
        [SerializeField] LogSpawner logSpawner;
        [SerializeField] Grow growTree;

        public Transform ChopTargetPoint => chopTargetPoint != null ? chopTargetPoint : transform;

        void Awake()
        {
            if (logSpawner == null)
                logSpawner = GetComponent<LogSpawner>();
            if (growTree == null)
                growTree = GetComponent<Grow>();
            if (chopTargetPoint == null)
                chopTargetPoint = transform;
        }

        public bool CanAutoChop(GameObject interactor)
        {
            if (!isActiveAndEnabled)
                return false;

            return logSpawner != null || growTree != null;
        }
    }
}
