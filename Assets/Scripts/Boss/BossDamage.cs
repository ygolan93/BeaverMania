using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossDamage : MonoBehaviour
{
    public float JawClamp;
    public float Sting;
    public Behavior Player;
    public NPC_Audio BossAudio;
    public void OnTriggerEnter(Collider OBJ)
    {
        if (OBJ.gameObject.CompareTag("ScorpionDamage"))
        {
            Player.TakeDamage(JawClamp);
            BossAudio.Sting();
        }
        if (OBJ.gameObject.CompareTag("ScorpionSting"))
        {
            Player.TakeDamage(Sting);
            BossAudio.Sting();
        }
    }
}
