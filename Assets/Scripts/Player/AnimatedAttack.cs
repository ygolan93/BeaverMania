using System.Collections;
using System.Collections.Generic;
using Beavermania.NPC;
using Beavermania.Objects;
using BeaverPlayer = Beavermania.Player.BeaverPlayerBehaviour;
using UnityEngine;

namespace Beavermania.Player.Combat
{

    public class AnimatedAttack : MonoBehaviour
    {
        [SerializeField] BeaverPlayer Player;
        [SerializeField] Transform AttackPoint;
        [SerializeField] Transform Sphere;
        [SerializeField] GameObject GlowEffect;
        [SerializeField] LayerMask enemyLayers;

        private void Start()
        {
            Player = transform.parent.GetComponent<BeaverPlayer>();
            if (GlowEffect != null)
                GlowEffect.SetActive(false);
        }

        public void CauseDamage(Vector3 origin, float range, int Damage)
        {
            Collider[] hitEnemies = Physics.OverlapSphere(origin, range, enemyLayers);
            foreach (Collider enemy in hitEnemies)
            {
                if (enemy.name != null)
                {
                    Debug.Log("Hit " + enemy.name);
                }
                switch (enemy.tag)
                {
                    case "NPC":
                        {
                            var Wasp = enemy.gameObject.GetComponent<NPC_Basic>();
                            Wasp.TakeDamage(Damage);
                            break;
                        }
                    case "Hive":
                        {
                            var Hive = enemy.gameObject.GetComponent<Static_Hive>();
                            Hive.TakeDamage(Damage);
                            break;
                        }
                    case "Scorpion":
                        {
                            var Scorpion = enemy.gameObject.GetComponent<ScorpionScript>();
                            Scorpion.TakeDamage(Damage);
                            break;
                        }
                }
            }
        }


        public void RollAttack()
        {
            CauseDamage(AttackPoint.position, 1.5f, 200);
        }

        public void GroundAttack()
        {
            var arsenal = Player.GetComponent<BeaverPlayer>().Arsenal;
            var weapon = Player.GetComponent<BeaverPlayer>().arsenalBrowser;
            switch (arsenal[weapon])
            {
                case "Bare Hands":
                    {
                        CauseDamage(AttackPoint.position, 0.7f, 50);
                        break;
                    }
                case "Bow":
                    {
                        CauseDamage(AttackPoint.position, 1f, 50);
                        break;
                    }
                case "Hammers":
                    {
                        CauseDamage(AttackPoint.position, 2f, 700);
                        break;
                    }
                case "ArmorSet":
                    {
                        var feetPos = Sphere.position + new Vector3(0, 0.5f, 0);
                        CauseDamage(feetPos, 4f, 200);
                        break;
                    }
            }

        }
        public void AirAttack()
        {
            var arsenal = Player.GetComponent<BeaverPlayer>().Arsenal;
            var weapon = Player.GetComponent<BeaverPlayer>().arsenalBrowser;
            switch (arsenal[weapon])
            {
                case "Bare Hands":
                    {
                        CauseDamage(Sphere.position, 2.5f, 20);
                        break;
                    }
                case "Hammers":
                    {
                        CauseDamage(Sphere.position, 2.5f, 20);
                        break;
                    }
                case "ArmorSet":
                    {
                        CauseDamage(Sphere.position+new Vector3(0,0.5f,0), 4f, 200);
                        break;
                    }
            }
        }

        public void ShieldParryON()
        {
            Player.ParryON();
        }
        public void ShieldParryOFF()
        {
            Player.ParryOFF();
        }

        public void TurnOnGlow()
        {
            if (GlowEffect != null)
                GlowEffect.SetActive(true);
        }

        public void TurnOffGlow()
        {
            if (GlowEffect != null)
                GlowEffect.SetActive(false);
        }
    }
}
