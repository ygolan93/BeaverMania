using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossDamage : MonoBehaviour
{
    public float JawClamp;
    public float Sting;
    public Behavior Player;

    public void OnTriggerEnter(Collider OBJ)
    {
        if (OBJ.gameObject.CompareTag("ScorpionDamage"))
        {
            Player.TakeDamage(JawClamp);
        }
        if (OBJ.gameObject.CompareTag("ScorpionSting"))
        {
            Player.TakeDamage(Sting);
        }
    }
}
