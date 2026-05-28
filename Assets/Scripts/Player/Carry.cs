using System.Collections;
using System.Collections.Generic;
using Beavermania.Core.Input;
using BeaverPlayer = Beavermania.Player.BeaverPlayerBehaviour;
using UnityEngine;

namespace Beavermania.Player.Movement
{

    public class Carry : MonoBehaviour
    {
        [Header("Objects & Prefabs")]
        public GameObject[] CarryPoint;
        public GameObject Bridge;
        public Rigidbody Log;
        public Transform BridgeDrop;
        public Transform LogDrop;
        public BeaverPlayer Goal;
        public GameObject Poof;

        [Header("Movement affect factors")]
        public int i = 0;
        public float WalkFactor=0.07f;
        public float RunFactor=0.8f;
        public float JumpFactor=0.3f;
        public float SlowAnim = 0.1f;
        public bool CanCarry;
        public void OnCollisionEnter(Collision OBJ)
        {
            if (OBJ.gameObject.CompareTag("Part"))
            {
                if (i < CarryPoint.Length - 1 && !PlayerInputReader.IsPrimaryHeld())
                {
                    //Goal.Otter.Play("Crouch");
                    CarryPoint[i].SetActive(true);
                    i++;
                    Goal.JumpForce -= JumpFactor;
                    Goal.Walk -= WalkFactor;
                    Goal.Run -= RunFactor;
                    Goal.Otter.speed -=SlowAnim;
                    Destroy(OBJ.transform.gameObject);
                }
            }
        }

        public void Update()
        {
            Vector3 SpawnPos = LogDrop.transform.position;
            Goal.LogCount = i + "/9";

            if (Goal.grounded == true)
            {
                if (i == 9)
                    Goal.LogCount = "9/9";
                if (PlayerInputReader.WasRollReleased() && i > 0)
                {
                    i--;
                    Goal.JumpForce += JumpFactor;
                    Goal.Walk += WalkFactor;
                    Goal.Run += RunFactor;
                    Goal.Otter.speed += SlowAnim;
                    Instantiate(Log, SpawnPos, Quaternion.identity);
                    CarryPoint[i].SetActive(false);
                }

                if (PlayerInputReader.IsRollHeld() && PlayerInputReader.WasInteractPressed() && i == 9)
                {
                    Goal.JumpForce += 9 * JumpFactor;
                    Goal.Otter.speed = 1;
                    Goal.Walk += 9 * WalkFactor;
                    Goal.Run += 9 * RunFactor;
                    LogDrop.transform.localRotation = Quaternion.Slerp(transform.rotation, Quaternion.identity, 0);
                    //SpawnPos = new Vector3(LogDrop.transform.position.x, LogDrop.position.y - 0.5f, LogDrop.position.z);
                    Bridge.transform.rotation = Goal.rotGoal * Quaternion.Euler(-90, 0, 0);
                    var newPoof= Instantiate(Poof, BridgeDrop.position + new Vector3(0, 0.5f, 0), Goal.transform.rotation * Quaternion.Euler(0, 180, 0));
                    Instantiate(Bridge, BridgeDrop.position + new Vector3(0,0.5f,0), Goal.transform.rotation*Quaternion.Euler(0,180,0));
                    Destroy(newPoof);

                    i = 0;
                    for (int x = 0; x < 9; x++)
                    {
                        CarryPoint[x].SetActive(false);
                    }
                }



                if (i < CarryPoint.Length - 1)
                    CanCarry = true;
                else
                    CanCarry = false;
            }
        }


    }
}
