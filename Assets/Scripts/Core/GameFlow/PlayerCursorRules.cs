using Cinemachine;
using UnityEngine;

namespace Beavermania.Core.GameFlow
{
    public static class PlayerCursorRules
    {
        public static void ApplyUnlockedVisible()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public static void ApplyLockedHidden(CinemachineFreeLook freeLook, Transform root)
        {
            if (freeLook != null && root != null)
            {
                freeLook.m_LookAt = root;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
