using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Cinemachine;

namespace Beavermania.Display
{

    public class CameraZoom : MonoBehaviour
    {
        public float zoomSpeed = 10.0f;
        public float minFOV = 20.0f;
        public float maxFOV = 60.0f;
        public Image aim;
        [SerializeField] Transform playerParent;
        [SerializeField] Transform aimPoint;

        CinemachineFreeLook freeLookCamera;
        static bool loggedMissingAimImage;

        void Start()
        {
            freeLookCamera = GetComponent<CinemachineFreeLook>();
            if (freeLookCamera != null)
                freeLookCamera.m_Lens.FieldOfView = 40;
        }

        void Update()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Approximately(scroll, 0f))
                return;

            if (freeLookCamera != null)
            {
                freeLookCamera.m_Lens.FieldOfView = Mathf.Clamp(
                    freeLookCamera.m_Lens.FieldOfView - (scroll * zoomSpeed),
                    minFOV,
                    maxFOV);
            }

            if (aim == null)
            {
                if (!loggedMissingAimImage)
                {
                    loggedMissingAimImage = true;
                    Debug.LogWarning($"{nameof(CameraZoom)}: aim Image is not assigned; scroll will adjust FOV only.", this);
                }
                return;
            }

            aim.transform.position -= new Vector3(0, scroll * zoomSpeed * 0.001f, 0);
        }
    }
}
