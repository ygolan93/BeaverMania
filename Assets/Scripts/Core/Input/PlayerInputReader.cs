using UnityEngine;

namespace Beavermania.Core.Input
{
    public static class PlayerInputReader
    {
        public static bool IsPrimaryHeld()
        {
            return UnityEngine.Input.GetKey(KeyCode.Mouse0);
        }

        public static bool WasPrimaryPressed()
        {
            return UnityEngine.Input.GetKeyDown(KeyCode.Mouse0);
        }

        public static bool IsSecondaryHeld()
        {
            return UnityEngine.Input.GetKey(KeyCode.Mouse1);
        }

        public static bool WasInteractPressed()
        {
            return UnityEngine.Input.GetKeyDown(KeyCode.Mouse1);
        }

        /// <summary>World interact (traders, doors, etc.) — keyboard E by default.</summary>
        public static bool WasWorldInteractPressed()
        {
            return UnityEngine.Input.GetKeyDown(KeyCode.E);
        }

        public static bool WasKeyPressed(KeyCode key)
        {
            return UnityEngine.Input.GetKeyDown(key);
        }

        /// <summary>Same physical binding as <see cref="WasInteractPressed"/> (RMB down); named for combat clarity.</summary>
        public static bool WasSecondaryPressed()
        {
            return UnityEngine.Input.GetKeyDown(KeyCode.Mouse1);
        }

        /// <summary>Gameplay pause: Escape (primary) or backquote / tilde (alias).</summary>
        public static bool WasPausePressed()
        {
            return WasPauseTogglePressed();
        }

        /// <summary>Pause-menu resume uses the same performed/down edge as pause.</summary>
        public static bool WasResumePressed()
        {
            return WasPauseTogglePressed();
        }

        static bool WasPauseTogglePressed()
        {
            return UnityEngine.Input.GetKeyDown(KeyCode.Escape)
                || UnityEngine.Input.GetKeyDown(KeyCode.BackQuote);
        }

        public static bool IsSprintHeld()
        {
            return UnityEngine.Input.GetKey(KeyCode.LeftShift);
        }

        public static bool IsRollHeld()
        {
            return UnityEngine.Input.GetKey(KeyCode.LeftControl);
        }

        public static bool WasRollPressed()
        {
            return UnityEngine.Input.GetKeyDown(KeyCode.LeftControl);
        }

        /// <summary>Holds Q to show the waypoint / compass arrow overlay (WayPoint UI).</summary>
        public static bool IsWaypointCompassHeld()
        {
            return UnityEngine.Input.GetKey(KeyCode.Q);
        }

        public static bool WasRollReleased()
        {
            return UnityEngine.Input.GetKeyUp(KeyCode.LeftControl);
        }

        public static bool WasSecondaryReleased()
        {
            return UnityEngine.Input.GetKeyUp(KeyCode.Mouse1);
        }

        public static bool WasPrimaryOrSecondaryReleased()
        {
            return UnityEngine.Input.GetKeyUp(KeyCode.Mouse0) || UnityEngine.Input.GetKeyUp(KeyCode.Mouse1);
        }

        public static bool IsMoveForwardHeld()
        {
            return UnityEngine.Input.GetKey(KeyCode.W);
        }

        public static bool IsMoveBackHeld()
        {
            return UnityEngine.Input.GetKey(KeyCode.S);
        }

        public static bool IsMoveLeftHeld()
        {
            return UnityEngine.Input.GetKey(KeyCode.A);
        }

        public static bool IsMoveRightHeld()
        {
            return UnityEngine.Input.GetKey(KeyCode.D);
        }

        public static bool WasAnyMoveAxisPressedDown()
        {
            return UnityEngine.Input.GetKeyDown(KeyCode.W)
                || UnityEngine.Input.GetKeyDown(KeyCode.A)
                || UnityEngine.Input.GetKeyDown(KeyCode.S)
                || UnityEngine.Input.GetKeyDown(KeyCode.D);
        }

        public static bool IsAnyKeyHeld()
        {
            return UnityEngine.Input.anyKey;
        }

        public static bool WasAnyKeyPressedDown()
        {
            return UnityEngine.Input.anyKeyDown;
        }

        public static bool WasJumpPressed()
        {
            return UnityEngine.Input.GetKeyDown(KeyCode.Space);
        }

        public static bool IsJumpHeld()
        {
            return UnityEngine.Input.GetKey(KeyCode.Space);
        }

        public static bool WasJumpReleased()
        {
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
            return UnityEngine.Input.GetKey(KeyCode.F);
        }
    }
}
