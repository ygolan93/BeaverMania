using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Beavermania.Data.Dialogue;
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
        [SerializeField] TraderDialogueData dialogueData;
        public Trader Merchant;
        public string[] lines;
        public float textSpeed;
        private int index;
        public bool isBoss;
        bool loggedMissingPlayer;
        bool loggedMissingPlayerObjective;
        bool loggedMissingPanel;
        bool loggedMissingMerchant;
        bool loggedMissingSkipButton;
        bool loggedEmptyDialogue;

        string[] EffectiveLines =>
            dialogueData != null && dialogueData.dialogueLines != null && dialogueData.dialogueLines.Length > 0
                ? dialogueData.dialogueLines
                : lines;

        float EffectiveTextSpeed => dialogueData != null ? dialogueData.textSpeed : textSpeed;

        bool EffectiveIsBoss => dialogueData != null ? dialogueData.isBossDialogue : isBoss;

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

            if (panel == null && transform.parent != null)
                panel = transform.parent;

            if (panel == null)
                LogMissingReference(nameof(panel), ref loggedMissingPanel);

            if (SkipButton != null)
                SkipButton.SetActive(true);
            else if (!loggedMissingSkipButton)
            {
                loggedMissingSkipButton = true;
                Debug.LogError($"{nameof(Dialogue)} on '{name}' has no {nameof(SkipButton)} assigned.", this);
            }

            if (EffectiveLines == null || EffectiveLines.Length == 0)
            {
                if (!loggedEmptyDialogue)
                {
                    loggedEmptyDialogue = true;
                    Debug.LogError($"{nameof(Dialogue)} on '{name}' has no dialogue lines.", this);
                }
                return;
            }

            textComponent.text = string.Empty;
            index = 0;
            StartDialogue();
        }

        public void Continue()
        {
            var effectiveLines = EffectiveLines;
            if (effectiveLines == null || index < 0 || index >= effectiveLines.Length)
                return;

            if (textComponent.text == effectiveLines[index])
            {
                NextLine();
            }
            else
            {
                StopAllCoroutines();
                textComponent.text = effectiveLines[index];
            }
        }

        void StartDialogue()
        {
            StartCoroutine(TypeLine());
        }

        IEnumerator TypeLine()
        {
            var effectiveLines = EffectiveLines;
            if (effectiveLines == null || index < 0 || index >= effectiveLines.Length)
                yield break;

            foreach (char c in effectiveLines[index].ToCharArray())
            {
                textComponent.text += c;
                yield return new WaitForSeconds(EffectiveTextSpeed);
            }
        }

        void NextLine()
        {
            var effectiveLines = EffectiveLines;
            if (effectiveLines == null || effectiveLines.Length == 0)
                return;

            if (index < effectiveLines.Length - 1)
            {
                index++;
                textComponent.text = effectiveLines[index];
            }
            else
            {
                EndConversation();
            }
        }

        public void EndConversation()
        {
            bool shouldAdvanceObjective = dialogueData == null || dialogueData.advanceObjectiveOnEnd;
            if (shouldAdvanceObjective && PlayerObjective != null)
                PlayerObjective.UpdateObjective();

            if (EffectiveIsBoss)
            {
                EndBossDialogue();
            }
            else
            {
                if (Merchant != null)
                    Merchant.activateSkip();
                else if (!loggedMissingMerchant)
                {
                    loggedMissingMerchant = true;
                    Debug.LogError($"{nameof(Dialogue)} on '{name}' has no {nameof(Merchant)}; cannot close trader UI.", this);
                }
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

        public void OpenShop()
        {
            Debug.Log("Dialogue: OpenShop clicked.", this);

            if (Merchant != null)
            {
                Merchant.OpenShop();
                return;
            }

            Debug.LogWarning(
                $"{nameof(Dialogue)} on '{name}' cannot open shop: {nameof(Merchant)} is not assigned on this dialogue.",
                this);
        }

        public void CloseShop()
        {
            Debug.Log("Dialogue: CloseShop clicked.", this);

            if (Merchant != null)
            {
                Merchant.CloseShop();
                return;
            }

            Debug.LogWarning(
                $"{nameof(Dialogue)} on '{name}' cannot close shop: {nameof(Merchant)} is not assigned.",
                this);
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
