using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Trader : MonoBehaviour
{
    public GameObject Merchant;
    public Behavior Player;
    public string Words;
    public string PlayerResponse;
    public Vector3 PlayerDistance;
    Quaternion FormalLook;
    // Start is called before the first frame update
    void Start()
    {
        FormalLook=transform.rotation;
    }

    // Update is called once per frame
   public void Update()
    {
        PlayerDistance = Player.transform.position - Merchant.transform.position;
        if (PlayerDistance.magnitude<10)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(PlayerDistance), 0.1f);
            Words = "Hey, you there!";
            if (PlayerDistance.magnitude<6)
            {
                Words = "Ever heard of my magical nuts? Betcha didn't";
                if (Input.GetKeyDown(KeyCode.T))
                PlayerResponse = "Sounds tempting but I think I'll pass.";
                //if (Input.GetKeyDown(KeyCode.T))
                //    Words = "Oh come on, Don't be like that.";


            }
        }
        else
        {
            Words = "Who's gonna grab my tiny nuts?";
            transform.rotation = Quaternion.Slerp(transform.rotation, FormalLook, 0.1f);
            PlayerResponse = "";
        }
    }
}
