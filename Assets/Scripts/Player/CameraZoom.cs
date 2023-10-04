using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Cinemachine;

public class CameraZoom : MonoBehaviour
{
    public float zoomSpeed = 10.0f;
    public float minFOV = 20.0f;
    public float maxFOV = 60.0f;
    public Image aim;
    [SerializeField] Transform playerParent;
    [SerializeField] Transform aimPoint;

    private CinemachineFreeLook freeLookCamera;

    private void Start()
    {
        // Find the Cinemachine FreeLook camera in the scene
        freeLookCamera = GetComponent<CinemachineFreeLook>();
        freeLookCamera.m_Lens.FieldOfView = 40;
    }

    private void Update()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            // Adjust the FOV of the Cinemachine FreeLook camera
            freeLookCamera.m_Lens.FieldOfView = Mathf.Clamp(freeLookCamera.m_Lens.FieldOfView - (scroll * zoomSpeed), minFOV, maxFOV);
            //aimPoint.position -= /*playerParent.InverseTransformPoint(*/new Vector3(0, scroll * zoomSpeed * 0.001f, 0);
        }


        //Aim Mark
        {
            aim.transform.position = Camera.main.WorldToScreenPoint(aimPoint.position + new Vector3(0, 0.65f, 0));
        }
    }
}
