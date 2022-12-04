using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lift : MonoBehaviour
{
    [SerializeField] Rigidbody Beams;
    [SerializeField] float LiftSpeed;
    [SerializeField] float CurrentPos;
    [Header("Height factor limits")]
    public float HighLimit = 18;
    public float LowLimit = 18;
    public float HeightStart;
    [Header("World Height limits")]
    public float MaxHeight;
    public float MinHeight;
    float NormalSpeed;
    [Header("Rest timer")]
    [SerializeField] float StopTimer = 3f;
    float InitialTimer;
    bool Up;
    bool Stop;

    private void Start()
    {
        HeightStart = Beams.transform.position.y;
        NormalSpeed = LiftSpeed;
        InitialTimer = StopTimer;
         MaxHeight = HeightStart + HighLimit;
         MinHeight = HeightStart - LowLimit;
    }



    // Update is called once per frame
    void FixedUpdate()
    {
        CurrentPos = Beams.transform.position.y;
        if (Up==true&&Stop==false)
        {
            Beams.velocity = new Vector3(0, LiftSpeed, 0);
        }
        if (Up == false && Stop == false)
        {
            Beams.velocity = new Vector3(0, -LiftSpeed, 0);
        }

        if (CurrentPos<MaxHeight&&CurrentPos>MinHeight)
        {
            StopTimer = InitialTimer;
        }
        if (CurrentPos>= MaxHeight)
        {
            StopLift();
            Up = false;
        }
        if (CurrentPos<=MinHeight)
        {
            StopLift();
            Up = true;
        }



    }

    void StopLift()
    {
        Stop = true;
        if (StopTimer > 0 && Stop==true)
        {
            Beams.velocity = new Vector3(0, 0, 0);
            StopTimer -= Time.deltaTime;
        }
        if (StopTimer<=0)
        {
            Stop = false;
        }
    }

}
