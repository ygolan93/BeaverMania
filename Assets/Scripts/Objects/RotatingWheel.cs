using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotatingWheel : MonoBehaviour
{
    // Start is called before the first frame update
    //[SerializeField] Transform Ramp1;
    //[SerializeField] Transform Ramp2;
    //[SerializeField] Transform Ramp3;
    //[SerializeField] Transform Ramp4;
    [SerializeField] float RotateWheel;


    // Update is called once per frame
    void FixedUpdate()
    {

        transform.Rotate(Vector3.forward*RotateWheel, Space.World);
        //Ramp1.rotation = Quaternion.EulerAngles(0, 0, 0);
        //Ramp2.rotation = Quaternion.EulerAngles(0, 0, 0);
        //Ramp3.rotation = Quaternion.EulerAngles(0, 0, 0);
        //Ramp4.rotation = Quaternion.EulerAngles(0, 0, 0);
    }
}
