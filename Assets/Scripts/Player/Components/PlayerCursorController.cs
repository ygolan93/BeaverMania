using Beavermania.Core.GameFlow;
using Cinemachine;
using UnityEngine;

namespace Beavermania.Player
{
public class PlayerCursorController : MonoBehaviour
{
    public Transform Root;
    public CinemachineFreeLook FreeLook;

    public void Bind(Transform root, CinemachineFreeLook freeLook)
    {
        Root = root;
        FreeLook = freeLook;
    }

    public void ShowCursor()
    {
        PlayerCursorRules.ApplyUnlockedVisible();
    }

    public void HideCursor()
    {
        PlayerCursorRules.ApplyLockedHidden(FreeLook, Root);
    }
}
}
