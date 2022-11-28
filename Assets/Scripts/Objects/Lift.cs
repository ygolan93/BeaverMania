using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lift : MonoBehaviour
{
    [SerializeField] Rigidbody Beams;
    [SerializeField] float LiftSpeed;
    [SerializeField] float CurrentPos;
    public int HighLimit = 18;
    public int LowLimit = 18;
    bool Up;
    

    public float HeightStart;
    private void Start()
    {
        HeightStart = Beams.transform.position.y;
    }



    // Update is called once per frame
    void FixedUpdate()
    {
        CurrentPos = Beams.transform.position.y;
        if (Up==true)
        {
            Beams.velocity = new Vector3(0, LiftSpeed, 0);
        }
        else
        {
            Beams.velocity = new Vector3(0, -LiftSpeed, 0);
        }

        if (CurrentPos>HeightStart+HighLimit)
        {
            Up = false;
        }
        if (CurrentPos<HeightStart-LowLimit)
        {
            Up = true;
        }



    }

    
}
