using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScorpionDamage : MonoBehaviour
{
    public float JawClamp;
    public float Sting;
    public Behaviour Player;
    public NPC_Audio BossAudio;
    public ScorpionScript Scorpion;

    private void Awake()
    {
        BossAudio = GameObject.FindGameObjectWithTag("Boss").GetComponent<NPC_Audio>();
        Player = transform.GetComponent<Behaviour>();
    }
    public void OnTriggerEnter(Collider OBJ)
    {
        if (OBJ.gameObject.CompareTag("Arena"))
        {
            Scorpion = GameObject.FindGameObjectWithTag("Boss").GetComponent<ScorpionScript>();
        }
        if (OBJ.gameObject.CompareTag("ScorpionDamage"))
        {
            if (Scorpion.combo < Scorpion.comboLimit)
            {
                if (Player.isParried == false)
                {
                    Player.TakeDamage(JawClamp);
                    BossAudio.Sting();
                }
                if (Player.isParried == true)
                {
                    Player.TakeDamage(6);
                    Scorpion.TakeDamage(10);
                    Scorpion.combo++;
                }
            }

        }
        if (OBJ.gameObject.CompareTag("ScorpionSting"))
        {
            if (Scorpion.combo < Scorpion.comboLimit)
            {
                if (Player.isParried == false)
                {
                    Player.TakeDamage(Sting);
                    BossAudio.Sting();
                }
                if (Player.isParried == true)
                {
                    Player.TakeDamage(6f);
                    Scorpion.TakeDamage(10);
                    Scorpion.combo++;
                }
            }
        }
    }
}
