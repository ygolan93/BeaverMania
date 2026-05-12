using Cinemachine;
using UnityEngine;

namespace Beavermania.Display
{
    /// <summary>
    /// Cursor + gameplay camera look binding without pulling Cinemachine into Core/GameFlow.
    /// </summary>
    public interface IPlayerCursorPresentation
    {
        void ApplyUnlockedVisible();
        void ApplyLockedHidden(Transform root);
    }

    /// <summary>Cinemachine-backed implementation; safe if <see cref="CinemachineFreeLook"/> is null (cursor still locks).</summary>
    public sealed class CinemachinePlayerCursorPresentation : IPlayerCursorPresentation
    {
        readonly CinemachineFreeLook freeLook;

        public CinemachinePlayerCursorPresentation(CinemachineFreeLook freeLook)
        {
            this.freeLook = freeLook;
        }

        public void ApplyUnlockedVisible() => PlayerCursorPresentationCore.ApplyUnlockedVisible();

        public void ApplyLockedHidden(Transform root) =>
            PlayerCursorPresentationCore.ApplyLockedHidden(freeLook, root);
    }

    /// <summary>Static entry points for call sites that pass <see cref="CinemachineFreeLook"/> each time.</summary>
    public static class PlayerCursorRules
    {
        public static void ApplyUnlockedVisible() => PlayerCursorPresentationCore.ApplyUnlockedVisible();

        public static void ApplyLockedHidden(CinemachineFreeLook freeLook, Transform root) =>
            PlayerCursorPresentationCore.ApplyLockedHidden(freeLook, root);
    }

    static class PlayerCursorPresentationCore
    {
        internal static void ApplyUnlockedVisible()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        internal static void ApplyLockedHidden(CinemachineFreeLook freeLook, Transform root)
        {
            if (freeLook != null && root != null)
                freeLook.m_LookAt = root;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
