using System.Collections;
using System.Collections.Generic;
using Beavermania.NPC;
using Beavermania.Player;
using BeaverPlayer = Beavermania.Player.BeaverPlayerBehaviour;
using UnityEngine;

namespace Beavermania.Player.Combat
{

    public class BossHandler : MonoBehaviour, IBossDialogueSkippable
    {
        public BeaverPlayer player;
        public ScorpionScript Boss;
        //NPC_Audio bossSound;
        public GameObject ChatCollider;
        public GameObject BossBar;
        public GameObject BossPanel;
        //public GameObject BossContinueButton;
        //public GameObject BossSkipButton;


        void Start()
        {
            player = GetComponent<BeaverPlayer>();
            var bossObject = GameObject.Find("ScorpionBoss");
            if (bossObject != null)
                Boss = bossObject.GetComponent<ScorpionScript>();
            if (BossBar != null)
                BossBar.SetActive(false);
        }

        void Update()
        {
            if (Boss == null)
            {
                if (BossBar != null && BossBar.activeSelf)
                    BossBar.SetActive(false);
            }
        }


        public void SkipBossChat()
        {
            if (Boss == null || player == null)
                return;

            if (ChatCollider != null)
                ChatCollider.SetActive(false);
            player.isAtTrader = false;
            player.HideCursor();
            if (BossPanel != null)
                BossPanel.SetActive(false);
            Boss.isAttacking = true;
            if (BossBar != null)
                BossBar.SetActive(true);
            if (player.FreeLook != null)
            {
                player.FreeLook.m_Orbits[1].m_Radius = 6;
                player.FreeLook.m_XAxis.m_MaxSpeed = 300;
                player.FreeLook.m_YAxis.m_MaxSpeed = 2;
                if (player.Root != null)
                    player.FreeLook.m_LookAt = player.Root;
            }
        }

        public void OnTriggerStay(Collider OBJ)
        {
            if (Boss == null || player == null || player.FreeLook == null)
                return;

            if (OBJ.gameObject.CompareTag("Arena"))
            {
                player.ShowCursor();
                player.isAtTrader = true;
                Boss.isAttacking = false;
                if (BossPanel != null)
                    BossPanel.SetActive(true);
                player.FreeLook.m_Orbits[1].m_Radius = 15;
                player.FreeLook.m_XAxis.m_MaxSpeed = 0;
                player.FreeLook.m_YAxis.m_MaxSpeed = 0;
                player.FreeLook.m_LookAt = Boss.transform;
            }
        }

        public void OnTriggerEnter(Collider OBJ)
        {
            if (player == null)
                return;

            if (OBJ.gameObject.CompareTag("ScorpionDamage"))
                player.TakeDamage(15);
            else if (OBJ.gameObject.CompareTag("ScorpionSting"))
                player.TakeDamage(30);
        }



    }
}
