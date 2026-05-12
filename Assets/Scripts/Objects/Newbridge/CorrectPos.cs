using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Beavermania.Core.Input;
using Beavermania.Player.Movement;

namespace Beavermania.Objects
{

    public class CorrectPos : MonoBehaviour
    {
        [SerializeField] Transform spawnHere;
        private void OnTriggerStay(Collider OBJ)
        {
            if (OBJ.gameObject.CompareTag("Player"))
            {
                int counter = OBJ.GetComponent<Carry>().i;
                if (PlayerInputReader.IsRollHeld() && counter > 0)
                {
                    OBJ.transform.position = spawnHere.position;
                }
            }
        }
    }
}
