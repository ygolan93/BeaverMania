using System.Collections.Generic;
using Beavermania.Core.Input;
using Beavermania.Data.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Beavermania.Player.AI
{
    /// <summary>
    /// Records human gameplay while AutoPlayer is disabled for playstyle learning.
    /// </summary>
    [DisallowMultipleComponent]
    public class AutoPlayerManualPlayRecorder : MonoBehaviour
    {
        const int MaxSamples = 4096;
        const float LookRotationEpsilon = 1e-6f;

        [SerializeField] bool recordOnPlay = true;
        [SerializeField] bool toggleRecordingWithF9 = true;
        [SerializeField] float sampleInterval = 0.15f;
        [SerializeField] AutoPlayerActionAdapter adapter;
        [SerializeField] AutoPlayerPerception perception;
        [SerializeField] LayerMask obstacleLayers = ~0;
        [SerializeField] float obstacleProbeDistance = 1.2f;
        [SerializeField] AutoPlayerPlaystyleProfile bakeTargetProfile;

        readonly List<AutoPlayerPlaystyleSample> _sessionSamples = new List<AutoPlayerPlaystyleSample>(512);
        readonly Collider[] _threatBuffer = new Collider[16];

        float _sampleTimer;
        bool _isRecording;
        bool _loggedBakeMissingProfile;

        public bool IsRecording => _isRecording;
        public int SessionSampleCount => _sessionSamples.Count;
        public IReadOnlyList<AutoPlayerPlaystyleSample> SessionSamples => _sessionSamples;
        public AutoPlayerPlaystyleProfile BakeTargetProfile => bakeTargetProfile;

        void Awake()
        {
            if (adapter == null)
                adapter = GetComponent<AutoPlayerActionAdapter>();
            if (perception == null)
                perception = GetComponent<AutoPlayerPerception>();
        }

        void OnEnable()
        {
            if (recordOnPlay && !PlayerInputOverride.IsActive)
                StartRecording();
        }

        void Update()
        {
            if (toggleRecordingWithF9 && Input.GetKeyDown(KeyCode.F9))
            {
                if (_isRecording)
                    StopRecording();
                else
                    StartRecording();
            }

            if (!_isRecording || !recordOnPlay)
                return;

            if (PlayerInputOverride.IsActive)
                return;

            if (adapter != null && adapter.IsGameplayBlocked())
                return;

            _sampleTimer -= Time.deltaTime;
            if (_sampleTimer > 0f)
                return;

            _sampleTimer = sampleInterval;
            RecordSample();
        }

        public void StartRecording()
        {
            _sessionSamples.Clear();
            _isRecording = true;
            _sampleTimer = 0f;
        }

        public void StopRecording()
        {
            _isRecording = false;
        }

        public void ClearSession()
        {
            _sessionSamples.Clear();
        }

        [ContextMenu("Bake Session To Assigned Profile")]
        public void BakeSessionToProfile()
        {
            if (bakeTargetProfile == null)
            {
                if (!_loggedBakeMissingProfile)
                {
                    _loggedBakeMissingProfile = true;
                    Debug.LogWarning($"{nameof(AutoPlayerManualPlayRecorder)}: assign {nameof(bakeTargetProfile)} before baking.", this);
                }
                return;
            }

            string sceneName = SceneManager.GetActiveScene().name;
            bakeTargetProfile.BakeFromSamples(_sessionSamples, sceneName, mergeSession: true);
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(bakeTargetProfile);
#endif
            Debug.Log($"[AutoPlayer] Baked {_sessionSamples.Count} samples into '{bakeTargetProfile.name}'.", this);
        }

        void RecordSample()
        {
            if (_sessionSamples.Count >= MaxSamples)
                return;

            BeaverPlayerBehaviour player = adapter != null ? adapter.Player : null;
            if (player == null)
                return;

            AutoPlayerPlaystyleSample sample = new AutoPlayerPlaystyleSample
            {
                Time = Time.time,
                Position = transform.position,
                MoveDirection = ReadMoveDirection(),
                ActiveArsenal = player.GetActiveArsenalEntryName(),
                SprintHeld = PlayerInputReader.IsSprintHeld(),
                JumpPressed = PlayerInputReader.WasJumpPressed(),
                RollHeld = PlayerInputReader.IsRollHeld(),
                PrimaryHeld = PlayerInputReader.IsPrimaryHeld(),
                SecondaryHeld = PlayerInputReader.IsSecondaryHeld(),
                DefendHeld = PlayerInputReader.IsDefendKeyHeld(),
                Grounded = player.grounded,
                HealthRatio = adapter.HealthRatio(),
                StaminaRatio = player.MaxStamina > 0f ? player.CurrentStamina / player.MaxStamina : 1f,
                CarriedLogs = adapter.CarriedLogCount,
                NearPrimaryBridge = IsNearPrimaryBridge(),
                Activity = InferActivity(player),
                ThreatTag = string.Empty,
                ThreatDistance = -1f,
                ObstacleBlockedAhead = false,
                ObstacleAheadDistance = obstacleProbeDistance
            };

            ScanThreat(ref sample, player);
            sample.InferredAction = InferCombatAction(player, sample);
            ScanObstacle(ref sample);

            _sessionSamples.Add(sample);
        }

        Vector3 ReadMoveDirection()
        {
            Vector3 move = Vector3.zero;
            if (PlayerInputReader.IsMoveForwardHeld()) move += transform.forward;
            if (PlayerInputReader.IsMoveBackHeld()) move -= transform.forward;
            if (PlayerInputReader.IsMoveRightHeld()) move += transform.right;
            if (PlayerInputReader.IsMoveLeftHeld()) move -= transform.right;
            move.y = 0f;
            if (move.sqrMagnitude > LookRotationEpsilon)
                move.Normalize();
            return move;
        }

        void ScanThreat(ref AutoPlayerPlaystyleSample sample, BeaverPlayerBehaviour player)
        {
            float radius = perception != null ? perception.CombatDetectionRadius : 12f;
            LayerMask mask = player.enemyLayers;
            int count = Physics.OverlapSphereNonAlloc(transform.position, radius, _threatBuffer, mask, QueryTriggerInteraction.Collide);
            float nearest = float.MaxValue;
            string tag = string.Empty;

            for (int i = 0; i < count; i++)
            {
                Collider col = _threatBuffer[i];
                if (col == null)
                    continue;

                string t = col.gameObject.tag;
                if (t != "NPC" && t != "Scorpion" && t != "Hive")
                    continue;

                float dist = Vector3.Distance(transform.position, col.transform.position);
                if (dist >= nearest)
                    continue;

                nearest = dist;
                tag = t;
            }

            if (nearest < float.MaxValue)
            {
                sample.ThreatDistance = nearest;
                sample.ThreatTag = tag;
                if (nearest <= (perception != null ? perception.CombatEngageRadius : 10f))
                    sample.Activity = PlaystyleActivityKind.Combat;
            }
        }

        static AutoCombatActionKind InferCombatAction(BeaverPlayerBehaviour player, AutoPlayerPlaystyleSample sample)
        {
            if (sample.DefendHeld && player.OwnsArsenalItem("ArmorSet"))
                return AutoCombatActionKind.Defend;

            if (sample.RollHeld)
                return AutoCombatActionKind.RollEvade;

            if (sample.SecondaryHeld && player.bowEquipped)
                return AutoCombatActionKind.Bow;

            if (sample.PrimaryHeld && player.ArmorEquipped && player.CanActivateFireBreath())
                return AutoCombatActionKind.FireBreath;

            if (sample.PrimaryHeld)
                return AutoCombatActionKind.Melee;

            return AutoCombatActionKind.None;
        }

        static PlaystyleActivityKind InferActivity(BeaverPlayerBehaviour player)
        {
            if (player == null)
                return PlaystyleActivityKind.Explore;

            var carry = player.Load;
            if (carry != null && carry.i > 0)
                return PlaystyleActivityKind.BuildBridge;

            return PlaystyleActivityKind.Explore;
        }

        static bool IsNearPrimaryBridge()
        {
            AutoPlayerPrimaryBridgeObjective objective = AutoPlayerPrimaryBridgeObjective.Active;
            if (objective == null || !objective.IsAssigned)
                return false;

            return !objective.IsComplete;
        }

        void ScanObstacle(ref AutoPlayerPlaystyleSample sample)
        {
            if (sample.MoveDirection.sqrMagnitude < LookRotationEpsilon)
                return;

            Vector3 origin = transform.position + Vector3.up * 0.4f;
            if (Physics.Raycast(origin, sample.MoveDirection, out RaycastHit hit, obstacleProbeDistance, obstacleLayers, QueryTriggerInteraction.Ignore))
            {
                sample.ObstacleBlockedAhead = true;
                sample.ObstacleAheadDistance = hit.distance;
            }
        }
    }
}
