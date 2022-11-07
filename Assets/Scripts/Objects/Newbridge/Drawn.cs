using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Drawn : MonoBehaviour
{
    public Rigidbody Log;
    Transform Player;

    //Distance from log to player
    Vector3 distance;
    public float Distance;
    public bool Click;

    public GameObject DrawnEffect;

    //Conditions to be drawn
    public Carry Load;

    [System.Obsolete]
    //Start draw effect on click
    public void InitiateEffect()
    {
        if (Click==true)
            DrawnEffect.GetComponent<ParticleSystem>().enableEmission = true;
        else
            DrawnEffect.GetComponent<ParticleSystem>().enableEmission = false;
    }

    private void OnCollisionEnter(Collision OBJ)
    {
        if (OBJ.gameObject.tag == "Strike")
        {
            Destroy(gameObject);
        }
    }
    private void Start()
    {
        Player = GameObject.FindGameObjectWithTag("Player").GetComponent<Transform>();
    }
    [System.Obsolete]
    public void FixedUpdate()
    {
        InitiateEffect();
        
        Vector3 distance = (transform.position - Player.transform.position);
        Distance = distance.magnitude;
        //Check if the log is close enough to be drawn + player's health above minimum **         
        if (Input.GetKey(KeyCode.Mouse1) && Distance <= 15)
        {
            Log.velocity = new Vector3(-distance.x, 1, -distance.z);
            Click = true;
            ////Check if player's load is still clear to carry more
            //bool CanDraw = Load.CanCarry;
            //if (CanDraw == true)
            //{

            //}
        }
        else
        {
            Click = false;
        }
    }
}