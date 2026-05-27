using Beavermania.Core.Input;
using Beavermania.Player.Movement;
using UnityEngine;

namespace Beavermania.Player.AI
{
    [DisallowMultipleComponent]
    public class AutoPlayerActionAdapter : MonoBehaviour
    {
        const float DirectionEpsilon = 1e-6f;

        [SerializeField] BeaverPlayerBehaviour player;
        [SerializeField] Carry carry;
        [SerializeField] Rigidbody playerRigidbody;
        [SerializeField] Transform moveReference;

        bool _primaryAttackRequested;
        bool _defendRequested;
        bool _chopRequested;
        bool _drawLogMode;
        bool _secondaryCombatHeld;
        bool _sprintEnabled;

        public BeaverPlayerBehaviour Player => player;
        public Carry Carry => carry;
        public bool IsCarryingLogs => carry != null && carry.i > 0;
        public int CarriedLogCount => carry != null ? carry.i : 0;
        public bool CanCarryMore => carry != null && carry.CanCarry;
        public bool IsGrounded => player != null && player.grounded;

        void Awake()
        {
            if (player == null)
                player = GetComponent<BeaverPlayerBehaviour>();
            if (carry == null && player != null)
                carry = player.Load;
            if (playerRigidbody == null && player != null)
                playerRigidbody = player.Player;
            if (moveReference == null)
                moveReference = transform;
        }

        public void ApplyToInputOverride()
        {
            if (!PlayerInputOverride.IsActive)
                return;

            PlayerInputOverride.SetSprint(_sprintEnabled);
            PlayerInputOverride.SetRollHeld(false);
            PlayerInputOverride.SetDefendHeld(_defendRequested);
            PlayerInputOverride.SetPrimaryHeld(_primaryAttackRequested || _chopRequested);
            PlayerInputOverride.SetSecondaryHeld(_secondaryCombatHeld || _drawLogMode);
        }

        public void ClearActionRequests()
        {
            _primaryAttackRequested = false;
            _defendRequested = false;
            _chopRequested = false;
            _drawLogMode = false;
            _secondaryCombatHeld = false;
            _sprintEnabled = false;
        }

        public void SetMoveInput(Vector2 input)
        {
            if (moveReference == null)
                return;

            Vector3 worldDir = new Vector3(input.x, 0f, input.y);
            if (worldDir.sqrMagnitude < DirectionEpsilon)
            {
                PlayerInputOverride.SetMoveAxes(false, false, false, false);
                return;
            }

            PlayerInputOverride.SetWorldMoveDirection(worldDir, moveReference);
        }

        public void SetWorldMoveDirection(Vector3 worldDirection)
        {
            if (moveReference == null)
                return;

            if (worldDirection.sqrMagnitude < DirectionEpsilon)
            {
                PlayerInputOverride.SetMoveAxes(false, false, false, false);
                return;
            }

            PlayerInputOverride.SetWorldMoveDirection(worldDirection, moveReference);
        }

        public void SetSprint(bool enabled) => _sprintEnabled = enabled;

        public void RequestJump() => PlayerInputOverride.PulseJump();

        public void RequestPrimaryAttack() => _primaryAttackRequested = true;

        public void RequestDefend() => _defendRequested = true;

        public void RequestBowAimFire() => _secondaryCombatHeld = true;

        public void RequestInteract() => PlayerInputOverride.PulseWorldInteract();

        public void RequestChop() => _chopRequested = true;

        public void RequestPickUp()
        {
            _primaryAttackRequested = false;
            _chopRequested = false;
            _drawLogMode = false;
            PlayerInputOverride.SetPrimaryHeld(false);
            PlayerInputOverride.SetSecondaryHeld(false);
        }

        public void RequestDrawLog() => _drawLogMode = true;

        public void RequestRoll(bool held)
        {
            if (held)
                PlayerInputOverride.SetRollHeld(true);
            else
                PlayerInputOverride.SetRollHeld(false);
        }

        public void PulseRollPress() => PlayerInputOverride.PulseRollPress();

        public void RequestDropOrPlaceLog()
        {
            PlayerInputOverride.SetRollHeld(true);
            PlayerInputOverride.PulseSecondaryPress();
        }

        public void ReleaseBridgeBuildInput()
        {
            PlayerInputOverride.SetRollHeld(false);
            PlayerInputOverride.SetSecondaryHeld(false);
        }

        public bool IsGameplayBlocked()
        {
            return player != null && player.IsGameplayInputLocked();
        }

        public float HealthRatio()
        {
            if (player == null || player.MaxHealth <= 0f)
                return 1f;
            return player.CurrentHealth / player.MaxHealth;
        }

        public bool WasHurtRecently()
        {
            return player != null && player.hurt;
        }

        public float CurrentHealth()
        {
            return player != null ? player.CurrentHealth : 0f;
        }

        public float StaminaRatio()
        {
            if (player == null || player.MaxStamina <= 0f)
                return 1f;
            return Mathf.Clamp01(player.CurrentStamina / player.MaxStamina);
        }

        public float CurrentStamina()
        {
            return player != null ? player.CurrentStamina : 0f;
        }

        public void ClearCombatInputs()
        {
            _primaryAttackRequested = false;
            _defendRequested = false;
            _secondaryCombatHeld = false;
            _chopRequested = false;
        }

        public void SetCombatSecondaryHeld(bool held) => _secondaryCombatHeld = held;

        public void FaceWorldPosition(Vector3 worldPosition)
        {
            if (player == null)
                return;

            Vector3 to = worldPosition - player.transform.position;
            to.y = 0f;
            if (to.sqrMagnitude < DirectionEpsilon)
                return;

            player.rotGoal = Quaternion.LookRotation(to.normalized);
            player.transform.rotation = Quaternion.Slerp(
                player.transform.rotation,
                player.rotGoal,
                Time.deltaTime * 10f);
        }

        public bool TryEquipArsenal(string entryName)
        {
            return player != null && player.TryEquipArsenalByName(entryName);
        }

        public string GetActiveArsenalName()
        {
            return player != null ? player.GetActiveArsenalEntryName() : string.Empty;
        }

        public void EquipForChop()
        {
            if (player == null)
                return;

            if (player.OwnsArsenalItem("Hammers"))
                player.TryEquipArsenalByName("Hammers");
        }

        public void EquipForCombatExplore()
        {
            if (player == null)
                return;

            if (player.OwnsArsenalItem("Bow") && player.arrowMunition > 0)
                player.TryEquipArsenalByName("Bow");
            else if (player.OwnsArsenalItem("Hammers"))
                player.TryEquipArsenalByName("Hammers");
        }
    }
}
