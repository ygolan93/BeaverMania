using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossDamage : MonoBehaviour
{
    public float JawClamp;
    public float Sting;
    public Behaviour Player;
    public NPC_Audio BossAudio;
    public BossScript Boss;

    private void Awake()
    {
        BossAudio = GameObject.FindGameObjectWithTag("Boss").GetComponent<NPC_Audio>();
        Player = transform.GetComponent<Behaviour>();
    }
    public void OnTriggerEnter(Collider OBJ)
    {
        if (OBJ.gameObject.CompareTag("Arena"))
        {
            Boss = GameObject.FindGameObjectWithTag("Boss").GetComponent<BossScript>();
        }
        if (OBJ.gameObject.CompareTag("ScorpionDamage"))
        {
            if (Boss.combo < Boss.comboLimit)
            {
                if (Player.isParried == false)
                {
                    Player.TakeDamage(JawClamp);
                    BossAudio.Sting();
                }
                if (Player.isParried == true)
                {
                    Player.TakeDamage(6);
                    Boss.TakeDamage(10);
                    Boss.combo++;
                }
            }

        }
        if (OBJ.gameObject.CompareTag("ScorpionSting"))
        {
            if (Boss.combo < Boss.comboLimit)
            {
                if (Player.isParried == false)
                {
                    Player.TakeDamage(Sting);
                    BossAudio.Sting();
                }
                if (Player.isParried == true)
                {
                    Player.TakeDamage(6f);
                    Boss.TakeDamage(10);
                    Boss.combo++;
                }
            }
        }
    }
}
