using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Beavermania.Objects
{

    public class WoodenGateSound : MonoBehaviour
    {
        [SerializeField] AudioSource Gate;

        void Awake()
        {
            Beavermania.Audio.AudioSourceRouting.EnsureRoute(Gate, Beavermania.Audio.AudioSourceRoute.Sfx);
        }

        private void OnCollisionEnter(Collision OBJ)
        { 
          if (OBJ.gameObject.CompareTag("Player"))
            {
                if (Beavermania.Audio.AudioSourceRouting.EnsureRoute(Gate, Beavermania.Audio.AudioSourceRoute.Sfx))
                    Gate.Play();
            }
        }
    }
}
