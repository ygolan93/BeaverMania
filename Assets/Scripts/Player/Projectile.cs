using System.Collections;
using System.Collections.Generic;
using Beavermania.Display;
using Beavermania.NPC;
using Beavermania.Objects;
using BeaverPlayer = Beavermania.Player.BeaverPlayerBehaviour;
using UnityEngine;

namespace Beavermania.Player.Combat
{

    public class Projectile : MonoBehaviour
    {
       public Rigidbody Ball;
        BeaverPlayer Player;
        public bool isArrow;
        public bool isFireBall;
        [SerializeField] AudioSource Sound;
        [SerializeField] float clock;
        [SerializeField] int forwardVel;
        [SerializeField] int upwardVel;
        [SerializeField] GameObject Explosion;
        [SerializeField] int Damage;
        [SerializeField] GameObject arrowPickup;
        // Start is called before the first frame update
        void Start()
        {
            Vector3 launchDirection = Camera.main.transform.TransformDirection(Vector3.forward);
            Physics.IgnoreLayerCollision(1, 3);
            Player = GameObject.FindGameObjectWithTag("Player").GetComponent<BeaverPlayer>();
            Ball.velocity = launchDirection * forwardVel + Vector3.up * upwardVel;
        }
        private void Update()
        {
            clock -= Time.deltaTime;
            if (clock<=0)
            {
                Destroy(gameObject);
                if (isArrow==true)
                {
                    Instantiate(arrowPickup, transform.position, Quaternion.identity);
                }
            }
        
        }


        public void Explode()
        {
            PooledOneShotVfx.Spawn(Explosion, transform.position, transform.rotation);
            Destroy(transform.gameObject);
        }

        public void RockHit()
        {
            Sound.PlayOneShot(Sound.clip);
            Sound.volume = 0.2f;
            Sound.pitch = 0.8f;
        }
        private void OnCollisionEnter(Collision OBJ)
        {
            if (OBJ.gameObject.CompareTag("NPC"))
            {
                if (OBJ.gameObject.TryGetComponent(out NPC_Basic npc))
                {
                    npc.TakeDamage(Damage);
                    npc.combo += npc.hit2stun;
                }
                if (isFireBall == true)
                {
                    Explode();
                }
                if (isFireBall == false)
                {
                    RockHit();
                }
                Player.Plattering = "Bam! Take that";
                Player.ChangeSpeech = 1f;
            }
            if (OBJ.gameObject.CompareTag("Scorpion"))
            {
                if (OBJ.gameObject.TryGetComponent(out ScorpionScript scorpion))
                {
                    scorpion.TakeDamage(Damage);
                    scorpion.combo += 3;
                }
                if (isFireBall == true)
                {
                    Explode();
                }
                if (isFireBall == false)
                {
                    RockHit();
                }
            }
            if (OBJ.gameObject.CompareTag("Hive"))
            {
                if (OBJ.gameObject.TryGetComponent(out Static_Hive hive))
                {
                    hive.TakeDamage(Damage);
                }
                if (isFireBall == true)
                {
                    Explode();
                }
                if (isFireBall == false)
                {
                    RockHit();
                }
            }
            if (OBJ.gameObject.CompareTag("Isle"))
            {
                if (isFireBall == true)
                {
                    Explode();
                }
                if (isFireBall == false)
                {
                    RockHit();
                }
            }
        }
    }
}
