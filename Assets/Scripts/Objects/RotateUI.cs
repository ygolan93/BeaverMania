using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateUI : MonoBehaviour
{
    const float LookRotationEpsilon = 0.0001f;
   GameObject CameraTarget;
    public Vector3 Distance;
    private void Awake()
    {
        CameraTarget = GameObject.FindGameObjectWithTag("MainCamera");

    }
    private void Update()
    {
        Distance = CameraTarget.transform.position - transform.position;
        if (Distance.sqrMagnitude > LookRotationEpsilon && Distance.magnitude > 0.5f)
        {
            Quaternion rotGoal = Quaternion.LookRotation(Distance);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotGoal, 0.1f);
        }

    }

}
