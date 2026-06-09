using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Beavermania.Core.Input;
using BeaverPlayer = Beavermania.Player.BeaverPlayerBehaviour;

namespace Beavermania.Objects
{

    public class Fan : MonoBehaviour
    {
        [Range(0, 100)]
        [SerializeField] float elevateSpeed;
        [SerializeField] Animator rotor;
        public bool turnON;
        [SerializeField] float counter;
        float initialCounter;
        [SerializeField] AudioSource sound;
        [SerializeField] AudioClip clip;
        bool fanSoundPlaying;

        void Awake()
        {
            Beavermania.Audio.AudioSourceRouting.EnsureRoute(sound, Beavermania.Audio.AudioSourceRoute.Sfx);
        }

        private void Start()
        {
            initialCounter = counter;
        }

        private void FixedUpdate()
        {
            if (turnON)
            {
                rotor.SetBool("turnOn", true);
                if (!fanSoundPlaying
                    && sound != null
                    && clip != null
                    && Beavermania.Audio.AudioSourceRouting.EnsureRoute(sound, Beavermania.Audio.AudioSourceRoute.Sfx))
                {
                    sound.clip = clip;
                    sound.loop = true;
                    sound.Play();
                    fanSoundPlaying = true;
                }

                counter -= Time.deltaTime;
                if (counter <= 0)
                    turnON = false;
            }
            else
            {
                rotor.SetBool("turnOn", false);
                if (fanSoundPlaying && sound != null)
                {
                    sound.Stop();
                    fanSoundPlaying = false;
                }

                counter = initialCounter;
            }
        }
        public void OnTriggerStay(Collider OBJ)
        {
            if (OBJ.gameObject.CompareTag("Player")&&turnON==true)
            {
                bool isGrounded = OBJ.gameObject.GetComponent<BeaverPlayer>().grounded;
                Rigidbody Player = OBJ.gameObject.GetComponent<Rigidbody>();
                if (isGrounded == false && PlayerInputReader.WasPrimaryPressed())
                {
                    Player.velocity += new Vector3(0, elevateSpeed, 0);
                }
            }
        }
    }
}
