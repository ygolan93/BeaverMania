using System.Collections;
using Beavermania.Core.GameFlow;
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
        public MonoBehaviour bossVictorySourceBehaviour;
        //NPC_Audio bossSound;
        public GameObject ChatCollider;
        public GameObject BossBar;
        public GameObject BossPanel;
        public GameObject VictoryScreen;
        //public GameObject BossContinueButton;
        //public GameObject BossSkipButton;
        BossDialogueInteractionSource dialogueInteractionSource;
        Coroutine victorySequenceCoroutine;
        IBossVictorySource subscribedBossVictorySource;
        bool victorySequenceStarted;

        void Awake()
        {
            ResolvePlayerReference();
            EnsureDialogueSource();
        }

        void OnEnable()
        {
            RefreshBossSubscription();
        }

        void Start()
        {
            ResolvePlayerReference();
            ResolveBossReference();
            EnsureDialogueSource();
            RefreshBossSubscription();
            if (BossBar != null)
                BossBar.SetActive(false);
        }

        void OnDisable()
        {
            StopVictorySequence();
            UnsubscribeFromBoss();
        }

        void Update()
        {
            ResolvePlayerReference();
            ResolveBossReference();
            RefreshBossSubscription();

            if (ResolveBossVictorySource() == null)
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
            if (dialogueInteractionSource != null)
                dialogueInteractionSource.SetInteractionAvailable(false);

            EndBossDialoguePresentation();
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

        public void BeginBossDialoguePresentation()
        {
            if (Boss == null || player == null || player.FreeLook == null)
                return;

            player.ShowCursor();
            player.isAtTrader = true;
            Boss.isAttacking = false;
            if (BossPanel != null)
                BossPanel.SetActive(false);
            player.FreeLook.m_Orbits[1].m_Radius = 15;
            player.FreeLook.m_XAxis.m_MaxSpeed = 0;
            player.FreeLook.m_YAxis.m_MaxSpeed = 0;
            player.FreeLook.m_LookAt = Boss.transform;
        }

        public void EndBossDialoguePresentation()
        {
            if (player == null)
                return;

            player.isAtTrader = false;
            player.HideCursor();
            if (BossPanel != null)
                BossPanel.SetActive(false);
        }

        public void OnTriggerStay(Collider OBJ)
        {
            if (Boss == null || player == null)
                return;

            if (OBJ.gameObject.CompareTag("Arena"))
            {
                EnsureDialogueSource();
                if (dialogueInteractionSource != null)
                    dialogueInteractionSource.SetInteractionAvailable(true);
            }
        }

        public void OnTriggerExit(Collider OBJ)
        {
            if (!OBJ.gameObject.CompareTag("Arena"))
                return;

            EnsureDialogueSource();
            if (dialogueInteractionSource != null)
                dialogueInteractionSource.SetInteractionAvailable(false);
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

        void EnsureDialogueSource()
        {
            if (ChatCollider == null)
                return;

            dialogueInteractionSource = ChatCollider.GetComponent<BossDialogueInteractionSource>();
            if (dialogueInteractionSource == null)
                dialogueInteractionSource = ChatCollider.AddComponent<BossDialogueInteractionSource>();

            dialogueInteractionSource.Configure(this);
        }

        void ResolvePlayerReference()
        {
            if (player == null)
                player = GetComponent<BeaverPlayer>();
        }

        void ResolveBossReference()
        {
            if (Boss != null)
                return;

            var bossObject = GameObject.Find("ScorpionBoss");
            if (bossObject != null)
                Boss = bossObject.GetComponent<ScorpionScript>();
        }

        void RefreshBossSubscription()
        {
            IBossVictorySource currentSource = ResolveBossVictorySource();
            if (ReferenceEquals(subscribedBossVictorySource, currentSource))
                return;

            UnsubscribeFromBoss();

            if (currentSource == null)
                return;

            currentSource.Defeated += OnBossDefeated;
            subscribedBossVictorySource = currentSource;
        }

        void UnsubscribeFromBoss()
        {
            if (subscribedBossVictorySource == null)
                return;

            subscribedBossVictorySource.Defeated -= OnBossDefeated;
            subscribedBossVictorySource = null;
        }

        void OnBossDefeated(IBossVictorySource defeatedBoss)
        {
            if (victorySequenceStarted)
                return;

            victorySequenceStarted = true;
            if (BossBar != null)
                BossBar.SetActive(false);
            if (BossPanel != null)
                BossPanel.SetActive(false);
            if (ChatCollider != null)
                ChatCollider.SetActive(false);
            if (dialogueInteractionSource != null)
                dialogueInteractionSource.SetInteractionAvailable(false);

            StopVictorySequence();
            victorySequenceCoroutine = StartCoroutine(TriggerVictoryAfterDelay(defeatedBoss != null ? defeatedBoss.VictoryDelay : 0f));
        }

        IBossVictorySource ResolveBossVictorySource()
        {
            if (bossVictorySourceBehaviour is IBossVictorySource configuredSource)
                return configuredSource;

            if (Boss != null)
                return Boss;

            return null;
        }

        IEnumerator TriggerVictoryAfterDelay(float delaySeconds)
        {
            if (delaySeconds > 0f)
                yield return new WaitForSecondsRealtime(delaySeconds);

            victorySequenceCoroutine = null;
            if (IsLoseScreenActive())
                yield break;

            TriggerVictory();
        }

        void TriggerVictory()
        {
            GameTimeScaleGate.SetFreeze(GameTimeScaleGate.FreezeToken.VictoryScreen, true);

            if (player != null)
            {
                if (player.Music != null)
                    player.Music.StopMusic();

                player.isAtTrader = false;
                player.ShowCursor();

                if (player.FreeLook != null)
                    player.FreeLook.enabled = false;
                if (player.CamForTraders != null)
                    player.CamForTraders.enabled = false;

                player.enabled = false;
            }

            if (BossBar != null)
                BossBar.SetActive(false);
            if (BossPanel != null)
                BossPanel.SetActive(false);
            if (VictoryScreen != null)
                VictoryScreen.SetActive(true);
        }

        bool IsLoseScreenActive()
        {
            return player != null
                && player.LooseScreen != null
                && player.LooseScreen.activeSelf;
        }

        void StopVictorySequence()
        {
            if (victorySequenceCoroutine == null)
                return;

            StopCoroutine(victorySequenceCoroutine);
            victorySequenceCoroutine = null;
        }



    }
}
