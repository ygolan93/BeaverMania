using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Trader : MonoBehaviour
{
    public GameObject Merchant;
    public Behavior Player;
    public GameObject TradeText;
    public GameObject DialoguePanel;
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
            TradeText.SetActive(true);
            DialoguePanel.SetActive(true);
            if (PlayerDistance.magnitude<6)
            {
                if (Input.GetKeyDown(KeyCode.E) &&  Player.Currency>0)
                {

                    if (Player.Currency >=3)
                    {
                        Player.NutCount++;
                        Player.Currency -= 3;
                    }
                }
                if (Input.GetKeyDown(KeyCode.Q) && Player.NutCount > 0)
                {
                        if (Player.NutCount > 0)
                        {
                            Player.NutCount--;
                            Player.Currency += 3;
                        }
                }



            }
        }
        else
        {
            TradeText.SetActive(false);
            DialoguePanel.SetActive(false);
            transform.rotation = Quaternion.Slerp(transform.rotation, FormalLook, 0.1f);
        }
    }
}
