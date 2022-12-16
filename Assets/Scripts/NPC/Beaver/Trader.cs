using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Trader : MonoBehaviour
{
    public GameObject Merchant;
    public Transform PlayerRoot;
    public Behavior Player;
    [SerializeField] Transform ConversationFocus;
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
        if (PlayerDistance.magnitude<8)
        {
            Player.rotGoal = Quaternion.LookRotation(Player.transform.position - Merchant.transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(PlayerDistance), 0.1f);
            Player.FreeLook.m_Orbits[2].m_Radius = 6;
            Player.FreeLook.m_XAxis.m_MaxSpeed = 0;
            Player.FreeLook.m_YAxis.m_MaxSpeed = 0;
            Player.FreeLook.m_LookAt = ConversationFocus;
            Player.ShowCursor();
            TradeText.SetActive(true);
            DialoguePanel.SetActive(true);
        }

        else
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, FormalLook, 0.1f);
            Player.FreeLook.m_Orbits[2].m_Radius = 4.7F;
            Player.FreeLook.m_XAxis.m_MaxSpeed = 300;
            Player.FreeLook.m_YAxis.m_MaxSpeed = 2;
            Player.FreeLook.m_LookAt = PlayerRoot;
            Player.HideCursor();
            TradeText.SetActive(false);
            DialoguePanel.SetActive(false);
        }
    }





    public void Buy()
    {
        if (Player.Currency > 0)
        {

            if (Player.Currency >= 3)
            {
                Player.NutCount++;
                Player.Currency -= 3;
            }
        }
    }

    public void Sell()
    {
        if (Player.NutCount > 0)
        {
            if (Player.NutCount > 0)
            {
                Player.NutCount--;
                Player.Currency += 3;
            }
        }
    }



}
