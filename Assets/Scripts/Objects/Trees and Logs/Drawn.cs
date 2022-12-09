using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Drawn : MonoBehaviour
{
    public Rigidbody Log;
    public bool Click;
    public ParticleSystem DrawnEffect;

   
    private void OnCollisionEnter(Collision OBJ)
    {
        if (OBJ.gameObject.tag == "Strike")
        {
            Destroy(gameObject);
        }
    }

    [System.Obsolete]
    private void OnTriggerStay(Collider OBJ)
    {
        if (OBJ.gameObject.tag=="Player")
        {
            var Player = OBJ.GetComponent<Behavior>();
            var PlayerLoad = OBJ.GetComponent<Carry>();
            //Check if the log is close enough to be drawn + player's health above minimum **         
            if (Input.GetKey(KeyCode.Mouse1) && !Input.GetKey(KeyCode.LeftControl))
            {
       
                //Check if player's load is still clear to carry more
                bool CanDraw =PlayerLoad.CanCarry;
                if (CanDraw == true)
                {
                    Log.velocity = (Player.transform.position - Log.transform.position).normalized * 1.5f;
                    DrawnEffect.enableEmission = true;
                }

            }
            else
            {
                DrawnEffect.enableEmission = false;
            }
        }
    }
}