using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Beavermania.Core.Input;

namespace Beavermania.Objects
{

    public class PickUp : MonoBehaviour
    {
        Vector3 objectPos;
        float distance;

        public GameObject Log;
        public GameObject tempParent;
        public bool IsHolding = false;
        public void Update()
        {

            distance = Vector3.Distance(Log.transform.position, tempParent.transform.position);
            if (distance >= 2.5f)

            {
                IsHolding = false;
            }


            //Check if IsHolding
            if (IsHolding == true)
            {
                Log.GetComponent<Rigidbody>().velocity = Vector3.zero;
                Log.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
                Log.transform.SetParent(tempParent.transform);
                if (PlayerInputReader.WasInteractPressed())
                {
                    IsHolding = false;
                }
            }
            else
            {
                objectPos = Log.transform.position;
                Log.transform.SetParent(null);
                Log.GetComponent<Rigidbody>().useGravity = true;
                Log.transform.position = objectPos;
            }

        }
    
        public void FixedUpdate()
        {
            if (distance <= 2.5f && PlayerInputReader.IsSecondaryHeld())
            {
                IsHolding = true;
                Log.GetComponent<Rigidbody>().useGravity = false;
                Log.GetComponent<Rigidbody>().detectCollisions = true;
            }

            else
            {
                IsHolding = false;
            }
        }




    }
}
