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


            var logRigidbody = Log.GetComponent<Rigidbody>();

            //Check if IsHolding
            if (IsHolding == true)
            {
                logRigidbody.velocity = Vector3.zero;
                logRigidbody.angularVelocity = Vector3.zero;
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
                logRigidbody.useGravity = true;
                Log.transform.position = objectPos;
            }

        }
    
        public void FixedUpdate()
        {
            if (distance <= 2.5f && PlayerInputReader.IsSecondaryHeld())
            {
                IsHolding = true;
                var logRigidbody = Log.GetComponent<Rigidbody>();
                logRigidbody.useGravity = false;
                logRigidbody.detectCollisions = true;
            }

            else
            {
                IsHolding = false;
            }
        }




    }
}
