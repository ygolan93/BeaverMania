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
        public float WalkFactor = 0.07f;
        public float RunFactor = 0.8f;
        public float JumpFactor = 0.3f;
        public float SlowAnim = 0.1f;
        public bool CanCarry;

        [Header("Carried log weight tuning")]
        [SerializeField] int logsCountThreshold = 1;
        [SerializeField, Range(0f, 1f)] float maxWeightPenalty = 1f;
        [SerializeField] float maxWalkPenalty = 0.63f;
        [SerializeField] float maxRunPenalty = 7.2f;
        [SerializeField] float maxJumpPenalty = 2.7f;
        [SerializeField] float animationSpeedMultiplier = 1f;
        [SerializeField] float maxAnimationSpeedPenalty = 0.9f;
        [SerializeField] float minimumAnimationSpeed = 0.2f;

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
                }
            }
        }

        public void Update()
        {
            if (!HasRequiredReferences())
                return;

            Vector3 SpawnPos = LogDrop.transform.position;
            Goal.LogCount = i + "/9";
            ApplyCarryWeightPenalty();

            if (Goal.grounded == true)
            {
                if (i == 9)
                    Goal.LogCount = "9/9  LCtrl+RMB to build";
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

            CurrentWeightPenalty01 = 0f;
            float walkPenalty = CalculatePenalty(WalkFactor, maxWalkPenalty);
            float runPenalty = CalculatePenalty(RunFactor, maxRunPenalty);
            float jumpPenalty = CalculatePenalty(JumpFactor, maxJumpPenalty);
            float animationPenalty = CalculatePenalty(SlowAnim * animationSpeedMultiplier, maxAnimationSpeedPenalty);

            float baseWalk = ResolveBaseValue(Goal.Walk, lastAppliedWalkValue, appliedWalkPenalty);
            float baseRun = ResolveBaseValue(Goal.Run, lastAppliedRunValue, appliedRunPenalty);
            float baseJump = ResolveBaseValue(Goal.JumpForce, lastAppliedJumpValue, appliedJumpPenalty);

            Goal.Walk = baseWalk - walkPenalty;
            Goal.Run = baseRun - runPenalty;
            Goal.JumpForce = baseJump - jumpPenalty;

            if (Goal.Otter != null)
            {
                float baseAnimationSpeed = ResolveBaseValue(Goal.Otter.speed, lastAppliedAnimationSpeed, appliedAnimationPenalty);
                Goal.Otter.speed = Mathf.Max(minimumAnimationSpeed, baseAnimationSpeed - animationPenalty);
                appliedAnimationPenalty = baseAnimationSpeed - Goal.Otter.speed;
                lastAppliedAnimationSpeed = Goal.Otter.speed;
            }

            appliedWalkPenalty = walkPenalty;
            appliedRunPenalty = runPenalty;
            appliedJumpPenalty = jumpPenalty;
            lastAppliedWalkValue = Goal.Walk;
            lastAppliedRunValue = Goal.Run;
            lastAppliedJumpValue = Goal.JumpForce;
            hasAppliedStats = true;
        }

        float CalculatePenalty(float penaltyPerLog, float maxPenalty)
        {
            int effectiveLogCount = Mathf.Max(0, i - Mathf.Max(1, logsCountThreshold) + 1);
            float cappedMaxPenalty = Mathf.Max(0f, maxPenalty) * Mathf.Clamp01(maxWeightPenalty);
            float penalty = Mathf.Min(effectiveLogCount * Mathf.Max(0f, penaltyPerLog), cappedMaxPenalty);
            CurrentWeightPenalty01 = Mathf.Max(CurrentWeightPenalty01, cappedMaxPenalty > 0f ? penalty / cappedMaxPenalty : 0f);
            return penalty;
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
