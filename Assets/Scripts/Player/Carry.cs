using Beavermania.Core.GameFlow;
using Beavermania.Core.Input;
using BeaverPlayer = Beavermania.Player.BeaverPlayerBehaviour;
using UnityEngine;

namespace Beavermania.Player.Movement
{

    public class Carry : MonoBehaviour
    {
        [Header("Objects & Prefabs")]
        public GameObject[] CarryPoint;
        public GameObject Bridge;
        public Rigidbody Log;
        public Transform BridgeDrop;
        public Transform LogDrop;
        public BeaverPlayer Goal;
        public GameObject Poof;

        [Header("Movement affect factors")]
        public int i = 0;
        public float JumpFactor = 0.3f;
        public bool CanCarry;

        [Header("Carried log weight tuning")]
        [SerializeField, Range(0, MaxLogCount - 1)] int maxBalancedLogCount = 3;
        [SerializeField, Range(0f, 1f)] float minMovementMultiplier = 0.5f;
        [SerializeField, Range(0.75f, 0.85f)] float minAnimationMultiplier = 0.8f;
        [SerializeField] float maxJumpPenalty = 2.7f;

        const int MaxLogCount = 9;
        const string BuildHintText = "9/9  LCtrl+RMB to build";

        int lastDisplayedLogCount = -1;
        int lastPenaltyLogCount = -1;
        bool lastDisplayedBuildHint;
        float appliedWalkPenalty;
        float appliedRunPenalty;
        float appliedJumpPenalty;
        float appliedAnimationPenalty;
        float lastAppliedWalkValue;
        float lastAppliedRunValue;
        float lastAppliedJumpValue;
        float lastAppliedAnimationSpeed;
        bool hasAppliedStats;
        bool loggedMissingGoal;
        bool loggedMissingLogDrop;
        bool loggedMissingCarryPoint;
        bool loggedMissingBridgeDrop;
        bool loggedMissingBridge;

        public float CurrentWeightPenalty01 { get; private set; }

        void Awake()
        {
            ResolveGoalIfMissing();
        }

        public void OnCollisionEnter(Collision OBJ)
        {
            if (OBJ == null || OBJ.gameObject == null)
                return;

            if (OBJ.gameObject.CompareTag("Part"))
            {
                if (CanAddLog() && !PlayerInputReader.IsPrimaryHeld())
                {
                    //Goal.Otter.Play("Crouch");
                    CarryPoint[i].SetActive(true);
                    i++;
                    ApplyCarryWeightPenalty();
                    Destroy(OBJ.transform.gameObject);
                    if (ObjectiveSyncService.Instance != null)
                        ObjectiveSyncService.Instance.OnLogCollected();
                }
            }
        }

        public void Update()
        {
            if (!HasRequiredReferences())
                return;

            Vector3 SpawnPos = LogDrop.transform.position;
            UpdateLogCountDisplay();
            if (ShouldRefreshCarryWeightPenalty())
                ApplyCarryWeightPenalty();

            if (Goal.grounded == true)
            {
                if (PlayerInputReader.WasRollReleased() && i > 0)
                {
                    i--;
                    ApplyCarryWeightPenalty();
                    Instantiate(Log, SpawnPos, Quaternion.identity);
                    if (i >= 0 && i < CarryPoint.Length && CarryPoint[i] != null)
                        CarryPoint[i].SetActive(false);
                }

                if (PlayerInputReader.IsRollHeld() && PlayerInputReader.WasInteractPressed() && i == 9)
                {
                    if (!CanBuildBridge())
                        return;

                    LogDrop.transform.localRotation = Quaternion.Slerp(transform.rotation, Quaternion.identity, 0);
                    //SpawnPos = new Vector3(LogDrop.transform.position.x, LogDrop.position.y - 0.5f, LogDrop.position.z);
                    if (Bridge != null)
                        Bridge.transform.rotation = Goal.rotGoal * Quaternion.Euler(-90, 0, 0);
                    if (BridgeDrop != null)
                    {
                        if (Poof != null)
                        {
                            var newPoof = Instantiate(Poof, BridgeDrop.position + new Vector3(0, 0.5f, 0), Goal.transform.rotation * Quaternion.Euler(0, 180, 0));
                            Destroy(newPoof);
                        }
                        if (Bridge != null)
                            Instantiate(Bridge, BridgeDrop.position + new Vector3(0, 0.5f, 0), Goal.transform.rotation * Quaternion.Euler(0, 180, 0));
                    }

                    i = 0;
                    ApplyCarryWeightPenalty();
                    int carriedVisualCount = CarryPoint != null ? Mathf.Min(9, CarryPoint.Length) : 0;
                    for (int x = 0; x < carriedVisualCount; x++)
                    {
                        if (CarryPoint[x] != null)
                            CarryPoint[x].SetActive(false);
                    }
                }



                if (i < CarryPoint.Length - 1)
                    CanCarry = true;
                else
                    CanCarry = false;
            }
        }

        void ApplyCarryWeightPenalty()
        {
            if (Goal == null)
                return;

            float load01 = CalculateLoad01();
            float movementMultiplier = Mathf.Lerp(1f, minMovementMultiplier, load01);
            float animationMultiplier = Mathf.Lerp(1f, minAnimationMultiplier, load01);
            float jumpPenalty = Mathf.Min(i * Mathf.Max(0f, JumpFactor), Mathf.Max(0f, maxJumpPenalty));
            CurrentWeightPenalty01 = load01;

            float baseWalk = ResolveBaseValue(Goal.Walk, lastAppliedWalkValue, appliedWalkPenalty);
            float baseRun = ResolveBaseValue(Goal.Run, lastAppliedRunValue, appliedRunPenalty);
            float baseJump = ResolveBaseValue(Goal.JumpForce, lastAppliedJumpValue, appliedJumpPenalty);

            Goal.Walk = baseWalk * movementMultiplier;
            Goal.Run = baseRun * movementMultiplier;
            Goal.JumpForce = baseJump - jumpPenalty;

            if (Goal.Otter != null)
            {
                float baseAnimationSpeed = ResolveBaseValue(Goal.Otter.speed, lastAppliedAnimationSpeed, appliedAnimationPenalty);
                Goal.Otter.speed = baseAnimationSpeed * animationMultiplier;
                appliedAnimationPenalty = baseAnimationSpeed - Goal.Otter.speed;
                lastAppliedAnimationSpeed = Goal.Otter.speed;
            }

            appliedWalkPenalty = baseWalk - Goal.Walk;
            appliedRunPenalty = baseRun - Goal.Run;
            appliedJumpPenalty = jumpPenalty;
            lastAppliedWalkValue = Goal.Walk;
            lastAppliedRunValue = Goal.Run;
            lastAppliedJumpValue = Goal.JumpForce;
            lastPenaltyLogCount = i;
            hasAppliedStats = true;
        }

        bool ShouldRefreshCarryWeightPenalty()
        {
            if (i != lastPenaltyLogCount)
                return true;

            if (Goal == null)
                return false;

            if (!Mathf.Approximately(Goal.Walk, lastAppliedWalkValue))
                return true;
            if (!Mathf.Approximately(Goal.Run, lastAppliedRunValue))
                return true;
            if (!Mathf.Approximately(Goal.JumpForce, lastAppliedJumpValue))
                return true;

            if (Goal.Otter != null && !Mathf.Approximately(Goal.Otter.speed, lastAppliedAnimationSpeed))
                return true;

            return false;
        }

        float CalculateLoad01()
        {
            int balancedCount = Mathf.Clamp(maxBalancedLogCount, 0, MaxLogCount - 1);
            return Mathf.Clamp01((i - balancedCount) / (float)(MaxLogCount - balancedCount));
        }

        void UpdateLogCountDisplay()
        {
            bool showBuildHint = Goal.grounded && i == MaxLogCount;
            if (i == lastDisplayedLogCount && showBuildHint == lastDisplayedBuildHint)
                return;

            lastDisplayedLogCount = i;
            lastDisplayedBuildHint = showBuildHint;
            Goal.LogCount = showBuildHint ? BuildHintText : i + "/" + MaxLogCount;
        }

        float ResolveBaseValue(float currentValue, float lastAppliedValue, float appliedPenalty)
        {
            if (!hasAppliedStats)
                return currentValue + appliedPenalty;

            return Mathf.Approximately(currentValue, lastAppliedValue)
                ? currentValue + appliedPenalty
                : currentValue;
        }

        bool CanAddLog()
        {
            return CarryPoint != null
                && CarryPoint.Length > 0
                && i >= 0
                && i < CarryPoint.Length - 1
                && CarryPoint[i] != null;
        }

        bool CanBuildBridge()
        {
            bool canBuildBridge = true;
            if (BridgeDrop == null)
            {
                LogMissingBuildReference(nameof(BridgeDrop), ref loggedMissingBridgeDrop);
                canBuildBridge = false;
            }

            if (Bridge == null)
            {
                LogMissingBuildReference(nameof(Bridge), ref loggedMissingBridge);
                canBuildBridge = false;
            }

            return canBuildBridge;
        }

        bool HasRequiredReferences()
        {
            ResolveGoalIfMissing();

            bool hasReferences = true;
            if (Goal == null)
            {
                LogMissingReference(nameof(Goal), ref loggedMissingGoal);
                hasReferences = false;
            }

            if (LogDrop == null)
            {
                LogMissingReference(nameof(LogDrop), ref loggedMissingLogDrop);
                hasReferences = false;
            }

            if (CarryPoint == null || CarryPoint.Length == 0)
            {
                LogMissingReference(nameof(CarryPoint), ref loggedMissingCarryPoint);
                hasReferences = false;
            }

            return hasReferences;
        }

        void ResolveGoalIfMissing()
        {
            if (Goal != null)
                return;

            Goal = GetComponent<BeaverPlayer>();
        }

        void LogMissingReference(string referenceName, ref bool logged)
        {
            if (logged)
                return;

            logged = true;
            Debug.LogWarning($"{nameof(Carry)} on '{name}' is missing {referenceName}; carried-log behavior is disabled until it is assigned.", this);
        }

        void LogMissingBuildReference(string referenceName, ref bool logged)
        {
            if (logged)
                return;

            logged = true;
            Debug.LogWarning($"{nameof(Carry)} on '{name}' is missing {referenceName}; bridge build is blocked and carried logs are preserved until it is assigned.", this);
        }


    }
}
