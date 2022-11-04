using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateUI : MonoBehaviour
{
   GameObject CameraTarget;
    public Vector3 Distance;
    private void Awake()
    {
        CameraTarget = GameObject.FindGameObjectWithTag("MainCamera");

    }
    private void Update()
    {
        Distance = CameraTarget.transform.position - transform.position;
        Quaternion rotGoal = Quaternion.LookRotation(Distance);
        if (Distance.magnitude>0.5f)
        transform.rotation = Quaternion.Slerp(transform.rotation, rotGoal, 0.1f);

    }

}
