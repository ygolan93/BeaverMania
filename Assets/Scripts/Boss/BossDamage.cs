using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossDamage : MonoBehaviour
{
    public float JawClamp;
    public float Sting;
    public Behavior Player;
    public NPC_Audio BossAudio;
    public BossScript Boss;


    public void OnTriggerEnter(Collider OBJ)
    {
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
                    Player.TakeDamage(0.1f);
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
                    Player.TakeDamage(0.5f);
                    Boss.TakeDamage(10);
                    Boss.combo++;
                }
            }
        }
    }
}
