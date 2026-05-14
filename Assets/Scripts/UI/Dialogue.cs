using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Beavermania.NPC;
using Beavermania.Player;
using BeaverPlayer = Beavermania.Player.BeaverPlayerBehaviour;
using Beavermania.UI.Objectives;

namespace Beavermania.UI
{
    public class Dialogue : MonoBehaviour
    {
        [SerializeField] public BeaverPlayer Player;
        [SerializeField] public ObjectiveUI PlayerObjective;
        [SerializeField] public TextMeshProUGUI textComponent;
        [SerializeField] public GameObject ContinueButton;
        [SerializeField] public GameObject SkipButton;
        [SerializeField] public Transform panel;
        public Trader Merchant;
        public string[] lines;
        public float textSpeed;
        private int index;
        public bool isBoss;
        bool loggedMissingPlayer;
        bool loggedMissingPlayerObjective;
        bool loggedMissingPanel;
        // Start is called before the first frame update
        void Start()
        {
            if (PlayerObjective == null && Player != null)
                PlayerObjective = Player.GetComponent<ObjectiveUI>();

            if (Player == null || PlayerObjective == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                {
                    if (Player == null)
                        Player = playerObject.GetComponent<BeaverPlayer>();

                    if (PlayerObjective == null)
                        PlayerObjective = playerObject.GetComponent<ObjectiveUI>();
                }

                if (Player == null)
                    LogMissingReference(nameof(Player), ref loggedMissingPlayer);

                if (PlayerObjective == null)
                    LogMissingReference(nameof(PlayerObjective), ref loggedMissingPlayerObjective);
            }

            //Boss = GameObject.FindGameObjectWithTag("Boss").GetComponent<BossScript>();
            if (panel == null && transform.parent != null)
                panel = transform.parent;

            if (panel == null)
                LogMissingReference(nameof(panel), ref loggedMissingPanel);

            SkipButton.SetActive(true);
            textComponent.text = string.Empty;
            StartDialogue();
            index = 0;
        }

        // Update is called once per frame
        public void Continue()
        {

            if (textComponent.text == lines[index])
            {
                NextLine();
            }
            else
            {
                StopAllCoroutines();
                textComponent.text = lines[index];
            }
        }

        void StartDialogue()
        {
            //index = 0;
            StartCoroutine(TypeLine());
        }
        IEnumerator TypeLine()
        {
            foreach (char c in lines[index].ToCharArray())
            {
                textComponent.text += c;
                yield return new WaitForSeconds(textSpeed);
            }
        }

        void NextLine()
        {
            if (index < lines.Length - 1)
            {
                index++;
                textComponent.text =/* string.Empty*/ lines[index];
                //StartCoroutine(TypeLine());
            }
            else
            {
                EndConversation();
            }
        }

        public void EndConversation()
        {
            if (PlayerObjective != null)
                PlayerObjective.UpdateObjective();

            if (isBoss==true)
            {
                EndBossDialogue();
            }
            else
            {
                Merchant.activateSkip();
            }
        }

        public void EndBossDialogue()
        {
            if (PlayerObjective != null)
                PlayerObjective.UpdateObjective();

            if (Player != null)
            {
                var bossFlow = Player.GetComponent<IBossDialogueSkippable>();
                if (bossFlow != null)
                    bossFlow.SkipBossChat();
            }
        }

        void LogMissingReference(string referenceName, ref bool logged)
        {
            if (logged)
                return;

            logged = true;
    #if DEVELOPMENT_BUILD
            Debug.LogWarning($"{nameof(Dialogue)} could not resolve {referenceName} fallback.", this);
    #endif
        }
    }
}
