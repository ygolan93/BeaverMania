using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossHandler : MonoBehaviour
{
    public Behaviour player;
    public ScorpionScript Boss;
    //NPC_Audio bossSound;
    public GameObject ChatCollider;
    public GameObject BossBar;
    public GameObject BossPanel;
    //public GameObject BossContinueButton;
    //public GameObject BossSkipButton;


    // Start is called before the first frame update
    void Start()
    {
        player = gameObject.GetComponent<Behaviour>();
        Boss = GameObject.Find("ScorpionBoss").GetComponent<ScorpionScript>();
        //bossSound = Boss.GetComponent<NPC_Audio>();
        BossBar.SetActive(false);
    }


    private void Update()
    {
        if (Boss.transform==null)
        {
            BossBar.SetActive(false);
        }
    }


    public void SkipBossChat()
    {
        ChatCollider.SetActive(false);
        player.isAtTrader = false;
        player.HideCursor();
        BossPanel.SetActive(false);
        Boss.Charge = true;
        BossBar.SetActive(true);
        player.FreeLook.m_Orbits[1].m_Radius = 6;
        player.FreeLook.m_XAxis.m_MaxSpeed = 300;
        player.FreeLook.m_YAxis.m_MaxSpeed = 2;
        player.FreeLook.m_LookAt = player.Root;
    }
    public void OnTriggerStay(Collider OBJ)
    {
        if (OBJ.gameObject.CompareTag("Arena"))
        {  
            player.ShowCursor();
            player.isAtTrader = true;
            Boss.Charge = false;
            BossPanel.SetActive(true);
            player.FreeLook.m_Orbits[1].m_Radius = 15;
            player.FreeLook.m_XAxis.m_MaxSpeed = 0;
            player.FreeLook.m_YAxis.m_MaxSpeed = 0;
            player.FreeLook.m_LookAt = Boss.transform;   
        }
    }
    public void OnTriggerEnter(Collider OBJ)
    {
        if (Boss.Charge==true)
        {
            if (OBJ.gameObject.CompareTag("ScorpionDamage"))
            {
                player.TakeDamage(15);
                //bossSound.Sting();
            }
            if (OBJ.gameObject.CompareTag("ScorpionSting"))
            {
                player.TakeDamage(30);
                //bossSound.Sting();
            }

        }

    }



}
