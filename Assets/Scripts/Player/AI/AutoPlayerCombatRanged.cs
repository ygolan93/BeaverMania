using UnityEngine;

namespace Beavermania.Player.AI
{
    [DisallowMultipleComponent]
    public class AutoPlayerCombatRanged : MonoBehaviour
    {
        const float DirectionEpsilon = 1e-6f;

        [SerializeField] AutoPlayerActionAdapter adapter;
        [SerializeField] AutoPlayerCombatStaminaGate staminaGate;
        [SerializeField] AutoPlayerCameraLock cameraLock;
        [SerializeField] float meleeMaxDistance = 2.5f;
        [SerializeField] float stoneMinDistance = 3f;
        [SerializeField] float stoneMaxDistance = 10f;
        [SerializeField] float bowMinDistance = 5f;
        [SerializeField] float bowMaxDistance = 16f;
        [SerializeField] float bowPreferredDistance = 8f;
        [SerializeField] float stonePreferredDistance = 5.5f;
        [SerializeField] float stoneAimDuration = 0.35f;
        [SerializeField] float bowDrawDuration = 0.3f;
        [SerializeField] float rangedShotCooldown = 0.85f;
        [SerializeField] float stoneStaminaReserve = 22f;
        [SerializeField] float bowStaminaReserve = 32f;

        CombatRangeMode _activeMode = CombatRangeMode.None;
        float _rangedPhaseTimer;
        int _rangedPhase;
        float _nextShotAllowedTime;

        public CombatRangeMode ActiveMode => _activeMode;

        void Awake()
        {
            if (adapter == null)
                adapter = GetComponent<AutoPlayerActionAdapter>();
            if (cameraLock == null)
                cameraLock = GetComponent<AutoPlayerCameraLock>();
            if (staminaGate == null)
                staminaGate = GetComponent<AutoPlayerCombatStaminaGate>();
        }

        public void ResetRangedState()
        {
            _activeMode = CombatRangeMode.None;
            _rangedPhase = 0;
            _rangedPhaseTimer = 0f;
            adapter?.SetCombatSecondaryHeld(false);
        }

        public CombatRangeMode EvaluateMode(Transform threat, float distance)
        {
            if (threat == null || adapter == null || adapter.Player == null)
                return CombatRangeMode.None;

            if (staminaGate != null && !staminaGate.CanPerformCombatAttacks())
                return CombatRangeMode.None;

            BeaverPlayerBehaviour player = adapter.Player;

            if (distance <= meleeMaxDistance)
                return CombatRangeMode.Melee;

            if (distance >= bowMinDistance
                && distance <= bowMaxDistance
                && player.OwnsArsenalItem("Bow")
                && player.arrowMunition > 0
                && player.CurrentStamina >= bowStaminaReserve)
            {
                return CombatRangeMode.Bow;
            }

            if (distance >= stoneMinDistance
                && distance <= stoneMaxDistance
                && !player.bowEquipped
                && adapter.IsGrounded
                && player.CurrentStamina >= stoneStaminaReserve)
            {
                return CombatRangeMode.Stone;
            }

            if (distance > meleeMaxDistance && player.OwnsArsenalItem("Bow") && player.arrowMunition > 0)
                return CombatRangeMode.Bow;

            if (distance > meleeMaxDistance && !player.bowEquipped && adapter.IsGrounded)
                return CombatRangeMode.Stone;

            return CombatRangeMode.Melee;
        }

        public float GetPreferredCombatDistance(CombatRangeMode mode)
        {
            switch (mode)
            {
                case CombatRangeMode.Bow:
                    return bowPreferredDistance;
                case CombatRangeMode.Stone:
                    return stonePreferredDistance;
                default:
                    return meleeMaxDistance * 0.75f;
            }
        }

        public void TickRangedCombat(Transform threat, float distance, AutoPlayerMovementAgent movement)
        {
            if (threat == null || adapter == null || movement == null)
                return;

            if (staminaGate != null && !staminaGate.CanPerformCombatAttacks())
            {
                ResetRangedState();
                return;
            }

            if (Time.time < _nextShotAllowedTime)
            {
                adapter.SetCombatSecondaryHeld(false);
                return;
            }

            CombatRangeMode mode = EvaluateMode(threat, distance);
            _activeMode = mode;

            cameraLock?.AimAtTarget(threat);
            adapter.FaceWorldPosition(threat.position);

            switch (mode)
            {
                case CombatRangeMode.Bow:
                    TickBowCombat(threat, distance, movement);
                    break;
                case CombatRangeMode.Stone:
                    TickStoneCombat(threat, distance, movement);
                    break;
                default:
                    ResetRangedPhase();
                    break;
            }
        }

        void TickBowCombat(Transform threat, float distance, AutoPlayerMovementAgent movement)
        {
            adapter.TryEquipArsenal("Bow");
            MaintainCombatDistance(threat, distance, bowPreferredDistance, movement);

            Vector3 toEnemy = threat.position - transform.position;
            toEnemy.y = 0f;
            if (toEnemy.sqrMagnitude > DirectionEpsilon)
                adapter.SetWorldMoveDirection(toEnemy.normalized);

            _rangedPhaseTimer += Time.deltaTime;

            switch (_rangedPhase)
            {
                case 0:
                    adapter.SetCombatSecondaryHeld(false);
                    if (_rangedPhaseTimer >= 0.05f)
                    {
                        _rangedPhase = 1;
                        _rangedPhaseTimer = 0f;
                    }
                    break;
                case 1:
                    adapter.SetCombatSecondaryHeld(true);
                    if (_rangedPhaseTimer >= bowDrawDuration)
                    {
                        _rangedPhase = 2;
                        _rangedPhaseTimer = 0f;
                    }
                    break;
                case 2:
                    adapter.SetCombatSecondaryHeld(false);
                    _nextShotAllowedTime = Time.time + rangedShotCooldown;
                    ResetRangedPhase();
                    break;
            }
        }

        void TickStoneCombat(Transform threat, float distance, AutoPlayerMovementAgent movement)
        {
            if (adapter.Player != null && adapter.Player.bowEquipped)
                adapter.TryEquipArsenal("Bare Hands");

            MaintainCombatDistance(threat, distance, stonePreferredDistance, movement);

            Vector3 toEnemy = threat.position - transform.position;
            toEnemy.y = 0f;
            if (toEnemy.sqrMagnitude > DirectionEpsilon)
                adapter.SetWorldMoveDirection(toEnemy.normalized);

            _rangedPhaseTimer += Time.deltaTime;

            switch (_rangedPhase)
            {
                case 0:
                    adapter.SetCombatSecondaryHeld(true);
                    if (_rangedPhaseTimer >= stoneAimDuration)
                    {
                        _rangedPhase = 1;
                        _rangedPhaseTimer = 0f;
                    }
                    break;
                case 1:
                    adapter.SetCombatSecondaryHeld(false);
                    _nextShotAllowedTime = Time.time + rangedShotCooldown;
                    ResetRangedPhase();
                    break;
            }
        }

        void MaintainCombatDistance(Transform threat, float distance, float preferredDistance, AutoPlayerMovementAgent movement)
        {
            if (threat == null)
                return;

            float minBand = preferredDistance * 0.85f;
            float maxBand = preferredDistance * 1.15f;

            if (distance > maxBand)
                movement.SetDestination(threat.position);
            else if (distance < minBand)
                movement.ClearDestination();
            else
                movement.ClearDestination();
        }

        void ResetRangedPhase()
        {
            _rangedPhase = 0;
            _rangedPhaseTimer = 0f;
            adapter?.SetCombatSecondaryHeld(false);
        }
    }
}
