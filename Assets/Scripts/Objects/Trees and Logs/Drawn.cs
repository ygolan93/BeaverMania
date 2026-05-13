using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Beavermania.Core.Input;
using BeaverPlayer = Beavermania.Player.BeaverPlayerBehaviour;
using Beavermania.Player.Movement;

namespace Beavermania.Objects
{

    public class Drawn : MonoBehaviour
    {
        public Rigidbody Log;
        public bool Click;
        public ParticleSystem DrawnEffect;

   
        private void OnCollisionEnter(Collision OBJ)
        {
            if (OBJ.gameObject.CompareTag("Strike"))
            {
                Destroy(gameObject);
            }
        }

        [System.Obsolete]
        private void OnTriggerStay(Collider OBJ)
        {
            if (OBJ.gameObject.CompareTag("Player"))
            {
                var Player = OBJ.GetComponent<BeaverPlayer>();
                var PlayerLoad = OBJ.GetComponent<Carry>();
                //Check if the log is close enough to be drawn + player's health above minimum **         
                if (PlayerInputReader.IsSecondaryHeld() && !PlayerInputReader.IsRollHeld())
                {
                    Player.Plattering = ("To me! my loyal logs");
                    Player.ChangeSpeech = 1;
                    //Check if player's load is still clear to carry more
                    bool CanDraw = PlayerLoad.CanCarry;
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
}
