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
        bool _pickUpMode;
        bool _drawLogMode;
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
            PlayerInputOverride.SetSecondaryHeld(_drawLogMode);
        }

        public void ClearActionRequests()
        {
            _primaryAttackRequested = false;
            _defendRequested = false;
            _chopRequested = false;
            _pickUpMode = false;
            _drawLogMode = false;
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

        public void RequestInteract() => PlayerInputOverride.PulseWorldInteract();

        public void RequestChop() => _chopRequested = true;

        public void RequestPickUp()
        {
            _pickUpMode = true;
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
    }
}
