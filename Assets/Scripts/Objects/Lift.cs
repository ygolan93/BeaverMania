using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lift : MonoBehaviour
{
    [SerializeField] Rigidbody Beams;
    [SerializeField] float LiftSpeed;
    [SerializeField] Transform BottomPoint;
    [SerializeField] Transform UpperPoint;
    [SerializeField] GameObject Stage;
    bool OnBoard;
    [SerializeField] float Clock;
    float InitialClock;

    private void Start()
    {
        InitialClock = Clock;
    }


    // Update is called once per frame
    void FixedUpdate()
    {

        if (OnBoard == true)
        {
            Clock -= Time.deltaTime;
            if (Clock <= 0)
            {
                if (Stage.transform.position.y < UpperPoint.transform.position.y)
                    Beams.velocity = new Vector3(0, LiftSpeed, 0);
                else
                    Beams.velocity = new Vector3(0, 0, 0);
            }
        }
        if (OnBoard == false)
        {
            Clock = InitialClock;
            if (Stage.transform.position.y > BottomPoint.transform.position.y)
                Beams.velocity = new Vector3(0, -LiftSpeed, 0);
            else
                Beams.velocity = new Vector3(0, 0, 0);
        }

    }


     void OnTriggerStay(Collider OBJ)
    {
        if (OBJ.gameObject.CompareTag("Player"))
        {
            OnBoard = true;
        }
    }

      void OnTriggerExit(Collider OBJ)
    {
        if (OBJ.gameObject.CompareTag("Player"))
        {
            OnBoard = false;
        }
    }

}

