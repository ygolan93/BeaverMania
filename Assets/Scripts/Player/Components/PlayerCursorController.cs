using Cinemachine;
using UnityEngine;

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
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void HideCursor()
    {
        if (FreeLook != null && Root != null)
        {
            FreeLook.m_LookAt = Root;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
