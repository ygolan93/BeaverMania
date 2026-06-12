using System.Collections;
using TMPro;
using Beavermania.Data.Dialogue;
using Beavermania.NPC;
using Beavermania.Player;
using BeaverPlayer = Beavermania.Player.BeaverPlayerBehaviour;
using Beavermania.UI.Objectives;
using UnityEngine;

namespace Beavermania.UI
{
    public class Dialogue : MonoBehaviour
    {
        [SerializeField] public BeaverPlayer Player;
        [SerializeField] public ObjectiveUI PlayerObjective;
        [SerializeField] public TextMeshProUGUI textComponent;
        [SerializeField] public GameObject ContinueButton;
        [SerializeField] public GameObject SkipButton;
        [SerializeField] public GameObject ShopButton;
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
        bool loggedMissingTextComponent;
        bool loggedEmptyDialogue;
        NpcDialogueSessionContext runtimeSession;
        INpcDialogueInteractionSource runtimeSource;
        NpcDialoguePresenter runtimePresenter;

        public GameObject PanelRoot =>
            panel != null
                ? panel.gameObject
                : transform.parent != null
                    ? transform.parent.gameObject
                    : gameObject;

        public TraderDialogueData ConfiguredDialogueData => dialogueData;

        public string[] ConfiguredLines => lines;

        public float ConfiguredTextSpeed => textSpeed;

        public bool ConfiguredIsBoss => isBoss;

        public bool ConfiguredAdvanceObjectiveOnEnd =>
            dialogueData == null || dialogueData.advanceObjectiveOnEnd;

        string[] EffectiveLines =>
            runtimeSession != null
                ? runtimeSession.ResolveLines()
                : dialogueData != null && dialogueData.dialogueLines != null && dialogueData.dialogueLines.Length > 0
                    ? dialogueData.dialogueLines
                    : lines;

        float EffectiveTextSpeed =>
            runtimeSession != null ? runtimeSession.ResolveTextSpeed() : dialogueData != null ? dialogueData.textSpeed : textSpeed;

        bool EffectiveIsBoss =>
            runtimeSession != null ? runtimeSession.ResolveIsBoss() : dialogueData != null ? dialogueData.isBossDialogue : isBoss;

        bool EffectiveAdvanceObjectiveOnEnd =>
            runtimeSession != null
                ? runtimeSession.ResolveAdvanceObjectiveOnEnd()
                : dialogueData == null || dialogueData.advanceObjectiveOnEnd;

        bool EffectiveHasShop =>
            runtimeSession != null
                ? runtimeSession.HasShop
                : dialogueData == null || dialogueData.hasShop;

        void Awake()
        {
            ResolveStaticReferences();
            ApplySessionUiBindings();
        }

        void Start()
        {
            RestartDialogue();
        }

        public void BindRuntimeSession(
            NpcDialogueSessionContext session,
            INpcDialogueInteractionSource source,
            NpcDialoguePresenter presenter)
        {
            runtimeSession = session;
            runtimeSource = source;
            runtimePresenter = presenter;
            Merchant = source as Trader;

            ResolveStaticReferences();
            ApplySessionUiBindings();
        }

        public void ClearRuntimeSession()
        {
            runtimeSession = null;
            runtimeSource = null;
            runtimePresenter = null;
            Merchant = null;

            ApplySessionUiBindings();
        }

        public void SetDialogueData(TraderDialogueData data)
        {
            dialogueData = data;
            ApplySessionUiBindings();
        }

        public void RestartDialogue()
        {
            ResolveStaticReferences();
            ApplySessionUiBindings();
            var effectiveLines = EffectiveLines;
            if (!HasDialogueLines(effectiveLines) || textComponent == null)
                return;

            StopAllCoroutines();
            textComponent.text = string.Empty;
            index = 0;
            StartDialogue();
        }

        public void Continue()
        {
            var effectiveLines = EffectiveLines;
            if (!HasDialogueLines(effectiveLines) || textComponent == null || index < 0 || index >= effectiveLines.Length)
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

        public void EndConversation()
        {
            if (EffectiveAdvanceObjectiveOnEnd)
            {
                var objectiveService = Beavermania.Core.GameFlow.ObjectiveSyncService.Instance;
                if (objectiveService != null)
                {
                    objectiveService.TryAdvanceObjective(
                        1,
                        EffectiveIsBoss
                            ? Beavermania.Core.GameFlow.ObjectiveAdvanceReason.BossDialogueCompleted
                            : Beavermania.Core.GameFlow.ObjectiveAdvanceReason.DialogueCompleted);
                }
                else if (PlayerObjective != null)
                {
                    PlayerObjective.UpdateObjective();
                }
            }

            if (runtimePresenter != null && runtimeSource != null)
            {
                runtimePresenter.CloseCurrentSession(NpcDialogueSessionCloseReason.DialogueCompleted);
                return;
            }

            if (EffectiveIsBoss)
            {
                EndBossDialogue();
                return;
            }

            TryResolveMerchant();

            if (Merchant != null)
                Merchant.activateSkip();
            else if (!loggedMissingMerchant)
            {
                loggedMissingMerchant = true;
                Debug.LogError($"{nameof(Dialogue)} on '{name}' has no {nameof(Merchant)}; cannot close trader UI.", this);
            }
        }

        public void SkipConversation()
        {
            if (runtimePresenter != null && runtimeSource != null)
            {
                runtimePresenter.CloseCurrentSession(NpcDialogueSessionCloseReason.ExternalCancel);
                return;
            }

            if (EffectiveIsBoss)
            {
                EndBossDialogue();
                return;
            }

            TryResolveMerchant();

            if (Merchant != null)
                Merchant.activateSkip();
            else if (!loggedMissingMerchant)
            {
                loggedMissingMerchant = true;
                Debug.LogError($"{nameof(Dialogue)} on '{name}' has no {nameof(Merchant)}; cannot skip trader UI.", this);
            }
        }

        public void EndBossDialogue()
        {
            if (Player == null)
                return;

            var bossFlow = Player.GetComponent<IBossDialogueSkippable>();
            if (bossFlow != null)
                bossFlow.SkipBossChat();
        }

        public void OpenShop()
        {
            Debug.Log("Dialogue: OpenShop clicked.", this);

            if (runtimePresenter != null && runtimeSource != null)
            {
                runtimePresenter.TryOpenShop(runtimeSource);
                return;
            }

            TryResolveMerchant();

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

            if (runtimePresenter != null && runtimeSource != null)
            {
                runtimePresenter.TryCloseShop(runtimeSource);
                return;
            }

            TryResolveMerchant();

            if (Merchant != null)
            {
                Merchant.CloseShop();
                return;
            }

            Debug.LogWarning(
                $"{nameof(Dialogue)} on '{name}' cannot close shop: {nameof(Merchant)} is not assigned.",
                this);
        }

        void ResolveStaticReferences()
        {
            TryResolveMerchant();

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
            {
                SkipButton.SetActive(true);
            }
            else if (!loggedMissingSkipButton)
            {
                loggedMissingSkipButton = true;
                Debug.LogError($"{nameof(Dialogue)} on '{name}' has no {nameof(SkipButton)} assigned.", this);
            }

            if (textComponent == null)
                LogMissingTextComponent();
        }

        void ApplySessionUiBindings()
        {
            if (ShopButton != null)
                ShopButton.SetActive(EffectiveHasShop);
        }

        void StartDialogue()
        {
            if (textComponent == null)
            {
                LogMissingTextComponent();
                return;
            }

            StartCoroutine(TypeLine());
        }

        IEnumerator TypeLine()
        {
            var effectiveLines = EffectiveLines;
            if (!HasDialogueLines(effectiveLines) || textComponent == null || index < 0 || index >= effectiveLines.Length)
                yield break;

            textComponent.text = string.Empty;

            foreach (char c in effectiveLines[index].ToCharArray())
            {
                textComponent.text += c;
                yield return new WaitForSeconds(EffectiveTextSpeed);
            }
        }

        void NextLine()
        {
            var effectiveLines = EffectiveLines;
            if (!HasDialogueLines(effectiveLines) || textComponent == null)
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

        void TryResolveMerchant()
        {
            if (Merchant != null || runtimeSession != null)
                return;

            var traders = FindObjectsOfType<Trader>(true);
            for (int i = 0; i < traders.Length; i++)
            {
                if (traders[i] != null && traders[i].IsTraderSessionActive())
                {
                    Merchant = traders[i];
                    return;
                }
            }

            Transform anchor = panel != null ? panel : transform;
            for (int i = 0; i < traders.Length; i++)
            {
                GameObject dialoguePanel = traders[i].DialoguePanel;
                if (dialoguePanel == null)
                    continue;

                Transform dialogueTransform = dialoguePanel.transform;
                if (dialoguePanel == anchor.gameObject
                    || anchor.IsChildOf(dialogueTransform)
                    || dialogueTransform.IsChildOf(anchor))
                {
                    Merchant = traders[i];
                    return;
                }
            }
        }

        bool HasDialogueLines(string[] effectiveLines)
        {
            if (effectiveLines != null && effectiveLines.Length > 0)
                return true;

            if (!loggedEmptyDialogue)
            {
                loggedEmptyDialogue = true;
                Debug.LogError($"{nameof(Dialogue)} on '{name}' has no dialogue lines.", this);
            }

            return false;
        }

        void LogMissingTextComponent()
        {
            if (loggedMissingTextComponent)
                return;

            loggedMissingTextComponent = true;
            Debug.LogError($"{nameof(Dialogue)} on '{name}' has no {nameof(textComponent)} assigned.", this);
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
