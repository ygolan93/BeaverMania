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
            Words = "Hey, you there! Buy Nuts with E and sell them with Q";
            if (PlayerDistance.magnitude<6)
            {
                if (Input.GetKeyDown(KeyCode.E) &&  Player.Currency>0)
                {
                    PlayerResponse = "Ok";

                    if (Player.Currency >=3)
                    {
                        Player.NutCount++;
                        Player.Currency -= 3;
                    }
                }
                if (Input.GetKeyDown(KeyCode.Q) && Player.NutCount > 0)
                {
                    {
                        PlayerResponse = "Ok";

                        if (Player.NutCount > 0)
                        {
                            Player.NutCount--;
                            Player.Currency += 3;
                        }
                    }
                }
                else
                    PlayerResponse = "";



            }
        }
        else
        {
            Words = "Who's gonna grab my tiny nuts?";
            transform.rotation = Quaternion.Slerp(transform.rotation, FormalLook, 0.1f);
        }
    }
}
