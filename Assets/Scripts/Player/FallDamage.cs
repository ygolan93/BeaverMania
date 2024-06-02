using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FallDamage : MonoBehaviour
{
    [Range(0, 10)]
    [SerializeField] float fallDamageFactor;
    [SerializeField] float damageVelocity;
    float fallReader;
    Behaviour Player;
    Rigidbody RB_Player;
    bool midAir=true;

    private void Start()
    {
        Player = gameObject.GetComponent<Behaviour>();
        RB_Player = gameObject.GetComponent<Rigidbody>();
        fallReader = 0;
    }

    private void FixedUpdate()
    {
        if (midAir==true)
        {
            fallReader = Mathf.Round(Mathf.Abs(RB_Player.velocity.y));
        }

        ////Update UI text
        //if (Player.Plattering != fallReader+"/"+damageVelocity)
        //{
        //    Player.ChangeSpeech -= Time.deltaTime;
        //    if (Player.ChangeSpeech <= 0)
        //    {
        //        Player.Plattering = fallReader + "/" + damageVelocity;
        //    }
        //}
    }
    public void OnTriggerEnter(Collider OBJ)
    {

        if (OBJ.gameObject.layer == 0)
        {
            midAir = false;
            if (fallReader >= damageVelocity)
            {
                Player.TakeDamage(fallDamageFactor * fallReader);
            }

        }
    }
    public void OnTriggerExit(Collider OBJ)
    {
        if (OBJ.gameObject.layer == 0)
        {
            midAir = true;
        }
    }
}