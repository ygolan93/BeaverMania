using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Testscript : MonoBehaviour
{
    public Rigidbody Player;
    Quaternion rotGoal;

    // Start is called before the first frame update
    void Start()
    {
        Player = GetComponent<Rigidbody>();
    }
    //Rotate and grant velocity to player in accordance to movement input and camera position
    public void RotatePlayer(Vector3 Target)
    {
            Player.velocity = (Target  + new Vector3(0, Player.velocity.y, 0));
            rotGoal = Quaternion.LookRotation(Target);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotGoal, 0.2f);
    }
    // Update is called once per frame
    void FixedUpdate()
    {
        //Camera direction vectors
        Vector3 cameraRelativeForward = Camera.main.transform.TransformDirection(Vector3.forward);
        Vector3 cameraRelativeBack = Camera.main.transform.TransformDirection(Vector3.back);
        Vector3 cameraRelativeRight = Camera.main.transform.TransformDirection(Vector3.right);
        Vector3 cameraRelativeLeft = Camera.main.transform.TransformDirection(Vector3.left);


        //Camera vectors on horizontal plane 
        Vector3 XZForward = new Vector3(cameraRelativeForward.x, 0, cameraRelativeForward.z);
        Vector3 XZBack = new Vector3(cameraRelativeBack.x, 0, cameraRelativeBack.z);
        Vector3 XZRight = new Vector3(cameraRelativeRight.x, 0, cameraRelativeRight.z);
        Vector3 XZLeft = new Vector3(cameraRelativeLeft.x, 0, cameraRelativeLeft.z);
        Vector3 XZForwardLeft = XZForward + XZLeft;
        Vector3 XZForwardRight = XZForward + XZRight;
        Vector3 XZBackLeft = XZBack + XZLeft;
        Vector3 XZBackRight = XZBack + XZRight;
        //Basic character movement 
        if (Input.GetKey(KeyCode.W))
        {
            Player.velocity = new Vector3(1, 1, 1);
        }
        if (Input.GetKey(KeyCode.S))
        {
            RotatePlayer(XZBack);
        }
        if (Input.GetKey(KeyCode.D))
        {
            RotatePlayer(XZRight);
        }
        if (Input.GetKey(KeyCode.A))
        {
            RotatePlayer(XZLeft);
        }
        if (Input.GetKey(KeyCode.W) && Input.GetKey(KeyCode.D))
        {
            RotatePlayer(XZForwardRight);

        }
        if (Input.GetKey(KeyCode.W) && Input.GetKey(KeyCode.A))
        {
            RotatePlayer(XZForwardLeft); ;
        }
        if (Input.GetKey(KeyCode.S) && Input.GetKey(KeyCode.D))
        {
            RotatePlayer(XZBackRight);
        }
        if (Input.GetKey(KeyCode.S) && Input.GetKey(KeyCode.A))
        {
            RotatePlayer(XZBackLeft);
        }
    }
}
