using UnityEngine;

namespace Beavermania.Core.Input
{
    public static class PlayerInputReader
    {
        public static bool IsPrimaryHeld()
        {
            return UnityEngine.Input.GetKey(KeyCode.Mouse0);
        }

        public static bool IsSecondaryHeld()
        {
            return UnityEngine.Input.GetKey(KeyCode.Mouse1);
        }

        public static bool WasInteractPressed()
        {
            return UnityEngine.Input.GetKeyDown(KeyCode.Mouse1);
        }

        public static bool WasPausePressed()
        {
            return UnityEngine.Input.GetKeyDown(KeyCode.Escape);
        }

        public static bool IsSprintHeld()
        {
            return UnityEngine.Input.GetKey(KeyCode.LeftShift);
        }

        public static bool IsRollHeld()
        {
            return UnityEngine.Input.GetKey(KeyCode.LeftControl);
        }
    }
}
