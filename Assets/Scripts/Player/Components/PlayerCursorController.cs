using Beavermania.Display;
using Cinemachine;
using UnityEngine;

namespace Beavermania.Player
{
    public class PlayerCursorController : MonoBehaviour
    {
        public Transform Root;
        public CinemachineFreeLook FreeLook;

        IPlayerCursorPresentation presentation;

        public void Bind(Transform root, CinemachineFreeLook freeLook)
        {
            Root = root;
            FreeLook = freeLook;
            presentation = new CinemachinePlayerCursorPresentation(freeLook);
        }

        public void ShowCursor()
        {
            EnsurePresentation();
            presentation.ApplyUnlockedVisible();
        }

        public void HideCursor()
        {
            EnsurePresentation();
            presentation.ApplyLockedHidden(Root);
        }

        void EnsurePresentation()
        {
            if (presentation == null)
                presentation = new CinemachinePlayerCursorPresentation(FreeLook);
        }
    }
}
