using UnityEngine;

namespace Beavermania.Core.Input
{
    public static class PlayerInputReader
    {
        public static bool IsPrimaryHeld()
        {
            if (PlayerInputOverride.IsActive)
                return PlayerInputOverride.PrimaryHeld;
            return UnityEngine.Input.GetKey(KeyCode.Mouse0);
        }

        public static bool WasPrimaryPressed()
        {
            if (PlayerInputOverride.IsActive)
                return PlayerInputOverride.PrimaryPressedThisFrame;
            return UnityEngine.Input.GetKeyDown(KeyCode.Mouse0);
        }

        public static bool IsSecondaryHeld()
        {
            if (PlayerInputOverride.IsActive)
                return PlayerInputOverride.SecondaryHeld;
            return UnityEngine.Input.GetKey(KeyCode.Mouse1);
        }

        public static bool WasInteractPressed()
        {
            if (PlayerInputOverride.IsActive)
                return PlayerInputOverride.SecondaryPressedThisFrame;
            return UnityEngine.Input.GetKeyDown(KeyCode.Mouse1);
        }

        /// <summary>World interact (traders, doors, etc.) — keyboard E by default.</summary>
        public static bool WasWorldInteractPressed()
        {
            if (PlayerInputOverride.IsActive)
                return PlayerInputOverride.WorldInteractPressedThisFrame;
            return UnityEngine.Input.GetKeyDown(KeyCode.E);
        }

        public static bool WasKeyPressed(KeyCode key)
        {
            if (key == KeyCode.E && PlayerInputOverride.IsActive)
                return PlayerInputOverride.WorldInteractPressedThisFrame;
            return UnityEngine.Input.GetKeyDown(key);
        }

        /// <summary>Same physical binding as <see cref="WasInteractPressed"/> (RMB down); named for combat clarity.</summary>
        public static bool WasSecondaryPressed()
        {
            if (PlayerInputOverride.IsActive)
                return PlayerInputOverride.SecondaryPressedThisFrame;
            return UnityEngine.Input.GetKeyDown(KeyCode.Mouse1);
        }

        /// <summary>Gameplay pause: Escape (primary) or backquote / tilde (alias).</summary>
        public static bool WasPausePressed()
        {
            return UnityEngine.Input.GetKeyDown(KeyCode.Escape)
                || UnityEngine.Input.GetKeyDown(KeyCode.BackQuote);
        }

        public static bool IsSprintHeld()
        {
            if (PlayerInputOverride.IsActive)
                return PlayerInputOverride.SprintHeld;
            return UnityEngine.Input.GetKey(KeyCode.LeftShift);
        }

        public static bool IsRollHeld()
        {
            if (PlayerInputOverride.IsActive)
                return PlayerInputOverride.RollHeld;
            return UnityEngine.Input.GetKey(KeyCode.LeftControl);
        }

        public static bool WasRollPressed()
        {
            if (PlayerInputOverride.IsActive)
                return PlayerInputOverride.RollPressedThisFrame;
            return UnityEngine.Input.GetKeyDown(KeyCode.LeftControl);
        }

        /// <summary>Holds Q to show the waypoint / compass arrow overlay (WayPoint UI).</summary>
        public static bool IsWaypointCompassHeld()
        {
            return UnityEngine.Input.GetKey(KeyCode.Q);
        }

        public static bool WasRollReleased()
        {
            if (PlayerInputOverride.IsActive)
                return PlayerInputOverride.RollReleasedThisFrame;
            return UnityEngine.Input.GetKeyUp(KeyCode.LeftControl);
        }

        public static bool WasSecondaryReleased()
        {
            if (PlayerInputOverride.IsActive)
                return PlayerInputOverride.SecondaryReleasedThisFrame;
            return UnityEngine.Input.GetKeyUp(KeyCode.Mouse1);
        }

        public static bool WasPrimaryOrSecondaryReleased()
        {
            if (PlayerInputOverride.IsActive)
                return PlayerInputOverride.WasPrimaryOrSecondaryReleasedThisFrame();
            return UnityEngine.Input.GetKeyUp(KeyCode.Mouse0) || UnityEngine.Input.GetKeyUp(KeyCode.Mouse1);
        }

        public static bool IsMoveForwardHeld()
        {
            if (PlayerInputOverride.IsActive)
                return PlayerInputOverride.MoveForward;
            return UnityEngine.Input.GetKey(KeyCode.W);
        }

        public static bool IsMoveBackHeld()
        {
            if (PlayerInputOverride.IsActive)
                return PlayerInputOverride.MoveBack;
            return UnityEngine.Input.GetKey(KeyCode.S);
        }

        public static bool IsMoveLeftHeld()
        {
            if (PlayerInputOverride.IsActive)
                return PlayerInputOverride.MoveLeft;
            return UnityEngine.Input.GetKey(KeyCode.A);
        }

        public static bool IsMoveRightHeld()
        {
            if (PlayerInputOverride.IsActive)
                return PlayerInputOverride.MoveRight;
            return UnityEngine.Input.GetKey(KeyCode.D);
        }

        public static bool WasAnyMoveAxisPressedDown()
        {
            if (PlayerInputOverride.IsActive)
                return PlayerInputOverride.AnyMoveAxisPressedThisFrame;
            return UnityEngine.Input.GetKeyDown(KeyCode.W)
                || UnityEngine.Input.GetKeyDown(KeyCode.A)
                || UnityEngine.Input.GetKeyDown(KeyCode.S)
                || UnityEngine.Input.GetKeyDown(KeyCode.D);
        }

        public static bool IsAnyKeyHeld()
        {
            if (PlayerInputOverride.IsActive)
                return PlayerInputOverride.IsAnyGameplayKeyHeld();
            return UnityEngine.Input.anyKey;
        }

        public static bool WasAnyKeyPressedDown()
        {
            if (PlayerInputOverride.IsActive)
            {
                return PlayerInputOverride.AnyMoveAxisPressedThisFrame
                    || PlayerInputOverride.JumpPressedThisFrame
                    || PlayerInputOverride.PrimaryPressedThisFrame
                    || PlayerInputOverride.SecondaryPressedThisFrame
                    || PlayerInputOverride.RollPressedThisFrame;
            }
            return UnityEngine.Input.anyKeyDown;
        }

        public static bool WasJumpPressed()
        {
            if (PlayerInputOverride.IsActive)
                return PlayerInputOverride.JumpPressedThisFrame;
            return UnityEngine.Input.GetKeyDown(KeyCode.Space);
        }

        public static bool IsJumpHeld()
        {
            if (PlayerInputOverride.IsActive)
                return PlayerInputOverride.JumpHeld;
            return UnityEngine.Input.GetKey(KeyCode.Space);
        }

        public static bool WasJumpReleased()
        {
            if (PlayerInputOverride.IsActive)
                return PlayerInputOverride.JumpReleasedThisFrame;
            return UnityEngine.Input.GetKeyUp(KeyCode.Space);
        }

        public static bool WasArsenalBrowsePressed()
        {
            return UnityEngine.Input.GetKeyDown(KeyCode.C);
        }

        public static bool WasNutThrowPressed()
        {
            return UnityEngine.Input.GetKeyDown(KeyCode.R);
        }

        public static bool WasAppleUsePressed()
        {
            return UnityEngine.Input.GetKeyDown(KeyCode.T);
        }

        public static bool WasAppleUseReleased()
        {
            return UnityEngine.Input.GetKeyUp(KeyCode.T);
        }

        public static bool WasGobletUsePressed()
        {
            return UnityEngine.Input.GetKeyDown(KeyCode.Y);
        }

        public static bool WasGobletUseReleased()
        {
            return UnityEngine.Input.GetKeyUp(KeyCode.Y);
        }

        public static bool IsDefendKeyHeld()
        {
            if (PlayerInputOverride.IsActive)
                return PlayerInputOverride.DefendHeld;
            return UnityEngine.Input.GetKey(KeyCode.F);
        }
    }
}
