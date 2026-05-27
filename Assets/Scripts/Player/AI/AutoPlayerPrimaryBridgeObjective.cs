using Beavermania.Objects;
using UnityEngine;

namespace Beavermania.Player.AI
{
    /// <summary>
    /// Marks the primary bridge goal (Island 1 → Island 2 on the first terrain tile).
    /// Auto-discovered via <see cref="FirstBridgeChecker"/> when not placed manually.
    /// </summary>
    [DisallowMultipleComponent]
    public class AutoPlayerPrimaryBridgeObjective : MonoBehaviour
    {
        public static AutoPlayerPrimaryBridgeObjective Active { get; private set; }

        [Header("Bridge")]
        [SerializeField] NewConstructor bridge;
        [Tooltip("Center of the first terrain tile (Level 1 Remastered: Terrain_(1000, 0, 0)).")]
        [SerializeField] Vector3 firstTerrainCenter = new Vector3(1000f, 0f, 0f);
        [SerializeField] float firstTerrainRadius = 2200f;
        [SerializeField] Transform island1Hint;
        [SerializeField] Transform island2Hint;

        public NewConstructor Bridge => bridge;
        public Vector3 FirstTerrainCenter => firstTerrainCenter;
        public Transform Island1Hint => island1Hint;
        public Transform Island2Hint => island2Hint;

        public bool IsAssigned => bridge != null;

        public bool IsComplete
        {
            get
            {
                if (bridge == null)
                    return false;
                return bridge.isLocked && bridge.PartCount >= bridge.BridgeLimit;
            }
        }

        public bool NeedsMoreLogs
        {
            get
            {
                if (bridge == null)
                    return true;
                return !IsComplete && bridge.PartCount < bridge.BridgeLimit;
            }
        }

        public Transform BuildTarget => bridge != null ? bridge.transform : transform;

        public int LogsStillNeeded
        {
            get
            {
                if (bridge == null)
                    return 9;
                return Mathf.Max(0, bridge.BridgeLimit - bridge.PartCount);
            }
        }

        void Awake()
        {
            if (bridge == null)
                bridge = GetComponent<NewConstructor>();
            RegisterAsActive();
        }

        void OnEnable() => RegisterAsActive();

        void OnDisable()
        {
            if (Active == this)
                Active = null;
        }

        void RegisterAsActive()
        {
            if (bridge == null)
                return;

            if (Active == null || !Active.IsAssigned)
                Active = this;
        }

        public bool IsOnFirstTerrain(Vector3 worldPosition)
        {
            Vector3 flat = worldPosition - firstTerrainCenter;
            flat.y = 0f;
            return flat.sqrMagnitude <= firstTerrainRadius * firstTerrainRadius;
        }

        public float ScoreWorldPosition(Vector3 worldPosition)
        {
            if (!IsOnFirstTerrain(worldPosition))
                return float.MaxValue;

            if (bridge == null)
                return worldPosition.sqrMagnitude;

            Vector3 toBridge = worldPosition - bridge.transform.position;
            toBridge.y = 0f;
            return toBridge.sqrMagnitude;
        }

        public static AutoPlayerPrimaryBridgeObjective ResolveInScene()
        {
            if (Active != null && Active.IsAssigned)
                return Active;

            var checker = Object.FindObjectOfType<FirstBridgeChecker>();
            if (checker != null && checker.Bridge != null)
            {
                var existing = checker.Bridge.GetComponent<AutoPlayerPrimaryBridgeObjective>();
                if (existing != null)
                    return existing;

                return checker.Bridge.gameObject.AddComponent<AutoPlayerPrimaryBridgeObjective>();
            }

            var constructors = Object.FindObjectsOfType<NewConstructor>(true);
            AutoPlayerPrimaryBridgeObjective best = null;
            float bestScore = float.MaxValue;

            for (int i = 0; i < constructors.Length; i++)
            {
                NewConstructor c = constructors[i];
                if (c == null)
                    continue;

                var candidate = c.GetComponent<AutoPlayerPrimaryBridgeObjective>();
                if (candidate == null)
                    candidate = c.gameObject.AddComponent<AutoPlayerPrimaryBridgeObjective>();

                float score = candidate.ScoreWorldPosition(c.transform.position);
                if (score < bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            return best;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(firstTerrainCenter, firstTerrainRadius);
            if (bridge != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(transform.position, bridge.transform.position);
            }
        }
    }
}
