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

        public void OnCollisionEnter(Collision OBJ)
        {
            if (OBJ.gameObject.CompareTag("Part"))
            {
                if (i < CarryPoint.Length - 1 && !PlayerInputReader.IsPrimaryHeld())
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
                    CarryPoint[i].SetActive(false);
                }

                if (PlayerInputReader.IsRollHeld() && PlayerInputReader.WasInteractPressed() && i == 9)
                {
                    LogDrop.transform.localRotation = Quaternion.Slerp(transform.rotation, Quaternion.identity, 0);
                    //SpawnPos = new Vector3(LogDrop.transform.position.x, LogDrop.position.y - 0.5f, LogDrop.position.z);
                    Bridge.transform.rotation = Goal.rotGoal * Quaternion.Euler(-90, 0, 0);
                    var newPoof= Instantiate(Poof, BridgeDrop.position + new Vector3(0, 0.5f, 0), Goal.transform.rotation * Quaternion.Euler(0, 180, 0));
                    Instantiate(Bridge, BridgeDrop.position + new Vector3(0,0.5f,0), Goal.transform.rotation*Quaternion.Euler(0,180,0));
                    Destroy(newPoof);

                    i = 0;
                    ApplyCarryWeightPenalty();
                    for (int x = 0; x < 9; x++)
                    {
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
            return Mathf.Min(effectiveLogCount * Mathf.Max(0f, penaltyPerLog), cappedMaxPenalty);
        }

        float ResolveBaseValue(float currentValue, float lastAppliedValue, float appliedPenalty)
        {
            if (!hasAppliedStats)
                return currentValue + appliedPenalty;

            return Mathf.Approximately(currentValue, lastAppliedValue)
                ? currentValue + appliedPenalty
                : currentValue;
        }


    }
}
