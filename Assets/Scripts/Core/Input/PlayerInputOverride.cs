using UnityEngine;

namespace Beavermania.Core.Input
{
    /// <summary>
    /// Virtual gameplay input source for AutoPlayer. When <see cref="IsActive"/>, gameplay queries
    /// on <see cref="PlayerInputReader"/> read from here instead of hardware (pause/menu stay hardware).
    /// </summary>
    public static class PlayerInputOverride
    {
        const float MoveAxisThreshold = 0.35f;

        public static bool IsActive { get; private set; }

        public static bool MoveForward { get; private set; }
        public static bool MoveBack { get; private set; }
        public static bool MoveLeft { get; private set; }
        public static bool MoveRight { get; private set; }
        public static bool SprintHeld { get; private set; }
        public static bool RollHeld { get; private set; }
        public static bool PrimaryHeld { get; private set; }
        public static bool SecondaryHeld { get; private set; }
        public static bool DefendHeld { get; private set; }
        public static bool JumpHeld { get; private set; }

        public static bool JumpPressedThisFrame { get; private set; }
        public static bool RollPressedThisFrame { get; private set; }
        public static bool RollReleasedThisFrame { get; private set; }
        public static bool PrimaryPressedThisFrame { get; private set; }
        public static bool SecondaryPressedThisFrame { get; private set; }
        public static bool SecondaryReleasedThisFrame { get; private set; }
        public static bool WorldInteractPressedThisFrame { get; private set; }
        public static bool JumpReleasedThisFrame { get; private set; }
        public static bool AnyMoveAxisPressedThisFrame { get; private set; }

        static bool _rollHeldPrevious;

        public static void SetActive(bool active)
        {
            if (!active)
                ClearAll();
            IsActive = active;
        }

        public static void ClearAll()
        {
            MoveForward = false;
            MoveBack = false;
            MoveLeft = false;
            MoveRight = false;
            SprintHeld = false;
            RollHeld = false;
            PrimaryHeld = false;
            SecondaryHeld = false;
            DefendHeld = false;
            JumpHeld = false;
            ClearFramePulses();
            _rollHeldPrevious = false;
        }

        public static void ClearFramePulses()
        {
            JumpPressedThisFrame = false;
            RollPressedThisFrame = false;
            RollReleasedThisFrame = false;
            PrimaryPressedThisFrame = false;
            SecondaryPressedThisFrame = false;
            SecondaryReleasedThisFrame = false;
            WorldInteractPressedThisFrame = false;
            JumpReleasedThisFrame = false;
            AnyMoveAxisPressedThisFrame = false;
        }

        public static void SetMoveAxes(bool forward, bool back, bool left, bool right)
        {
            bool prevAny = MoveForward || MoveBack || MoveLeft || MoveRight;
            MoveForward = forward;
            MoveBack = back;
            MoveLeft = left;
            MoveRight = right;
            bool nextAny = MoveForward || MoveBack || MoveLeft || MoveRight;
            if (!prevAny && nextAny)
                AnyMoveAxisPressedThisFrame = true;
        }

        public static void SetWorldMoveDirection(Vector3 worldDirection, Transform reference)
        {
            if (reference == null)
            {
                SetMoveAxes(false, false, false, false);
                return;
            }

            Vector3 flat = new Vector3(worldDirection.x, 0f, worldDirection.z);
            if (flat.sqrMagnitude < 1e-8f)
            {
                SetMoveAxes(false, false, false, false);
                return;
            }

            flat.Normalize();
            Vector3 forward = reference.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 1e-8f)
                forward = Vector3.forward;
            else
                forward.Normalize();

            Vector3 right = reference.right;
            right.y = 0f;
            if (right.sqrMagnitude < 1e-8f)
                right = Vector3.right;
            else
                right.Normalize();

            float forwardDot = Vector3.Dot(flat, forward);
            float rightDot = Vector3.Dot(flat, right);

            SetMoveAxes(
                forwardDot > MoveAxisThreshold,
                forwardDot < -MoveAxisThreshold,
                rightDot < -MoveAxisThreshold,
                rightDot > MoveAxisThreshold);
        }

        public static void SetSprint(bool enabled) => SprintHeld = enabled;

        public static void SetRollHeld(bool held)
        {
            if (held && !_rollHeldPrevious)
                RollPressedThisFrame = true;
            if (!held && _rollHeldPrevious)
                RollReleasedThisFrame = true;
            _rollHeldPrevious = held;
            RollHeld = held;
        }

        public static void SetPrimaryHeld(bool held)
        {
            if (held && !PrimaryHeld)
                PrimaryPressedThisFrame = true;
            PrimaryHeld = held;
        }

        public static void SetSecondaryHeld(bool held)
        {
            if (held && !SecondaryHeld)
                SecondaryPressedThisFrame = true;
            if (!held && SecondaryHeld)
                SecondaryReleasedThisFrame = true;
            SecondaryHeld = held;
        }

        public static void SetDefendHeld(bool held) => DefendHeld = held;

        public static void PulseJump()
        {
            JumpPressedThisFrame = true;
            JumpHeld = true;
        }

        public static void SetJumpHeld(bool held)
        {
            if (!held && JumpHeld)
                JumpReleasedThisFrame = true;
            JumpHeld = held;
        }

        public static void PulseWorldInteract() => WorldInteractPressedThisFrame = true;

        public static void PulseSecondaryPress() => SecondaryPressedThisFrame = true;

        public static void PulseRollPress()
        {
            RollPressedThisFrame = true;
            SetRollHeld(true);
        }

        public static bool IsAnyGameplayKeyHeld()
        {
            return MoveForward || MoveBack || MoveLeft || MoveRight
                || SprintHeld || RollHeld || PrimaryHeld || SecondaryHeld
                || DefendHeld || JumpHeld;
        }

        public static bool WasPrimaryOrSecondaryReleasedThisFrame()
        {
            return SecondaryReleasedThisFrame;
        }
    }
}
