using TMPro;
using Beavermania.Data.Dialogue;
using Beavermania.Player;
using Beavermania.UI;
using BeaverPlayer = Beavermania.Player.BeaverPlayerBehaviour;
using UnityEngine;

namespace Beavermania.NPC
{
    public class Trader : MonoBehaviour, INpcDialogueInteractionSource
    {
        const float LookRotationEpsilon = 0.0001f;
        const string DefaultInteractionPromptMessage = "Press E to interact";

        public GameObject Merchant;
        public Transform PlayerRoot;
        public BeaverPlayer Player;
        public GameObject TradeText;
        public GameObject DialoguePanel;
        public GameObject Shop;
        public Vector3 PlayerDistance;
        public bool skipPressed = false;
        [SerializeField] bool Rotate;
        [SerializeField] float PanelPopUp;
        [SerializeField] GameObject playerCanvas;
        [SerializeField] TextMeshProUGUI objectiveText;
        [SerializeField] string interactionPromptMessage = DefaultInteractionPromptMessage;
        [SerializeField] TraderDialogueData dialogueData;

        public TraderDialogueData DialogueData => dialogueData;

        public Transform InteractionTransform => Merchant != null ? Merchant.transform : transform;

        public Transform InteractionLookTarget => InteractionTransform;

        public string InteractionPromptText => string.IsNullOrEmpty(interactionPromptMessage)
            ? DefaultInteractionPromptMessage
            : interactionPromptMessage;

        public float InteractionDistance => isPlayerInRange ? PlayerDistance.magnitude : float.MaxValue;

        public bool IsInteractionAvailable => isPlayerInRange
            && !isInteracting
            && !isShopOpen
            && !skipPressed
            && Player != null
            && Merchant != null;

        bool isPlayerInRange;
        bool isInteracting;
        bool isShopOpen;
        bool isRegisteredWithPresenter;
        bool loggedMissingMerchant;
        bool loggedMissingDialoguePanel;
        bool loggedMissingShop;
        bool loggedMissingLegacyDialogue;
        Quaternion FormalLook;

        public float GetOfferPanelDistance() => PanelPopUp;

        public bool IsInteractionEngaged() => isInteracting;

        public bool IsTraderSessionActive() => isInteracting || isShopOpen;

        void Start()
        {
            FormalLook = transform.rotation;

            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                Player = playerObject.GetComponent<BeaverPlayer>();

            GameObject rootObject = GameObject.FindGameObjectWithTag("PlayerRoot");
            if (rootObject != null)
                PlayerRoot = rootObject.transform;

            DeactivateLegacyPromptObjects();
            ValidateSerializedReferences();
            UpdatePresenterRegistration();
        }

        void Update()
        {
            if (Player == null || Merchant == null)
                return;

            PlayerDistance = Player.transform.position - Merchant.transform.position;
            float distance = PlayerDistance.magnitude;
            isPlayerInRange = PanelPopUp > 0f && distance < PanelPopUp;

            if (!isPlayerInRange && distance > PanelPopUp)
                skipPressed = false;

            UpdatePresenterRegistration();

            if (isInteracting || isShopOpen)
                MaintainTraderPresentation();

            UpdateNpcRotation();
        }

        public NpcDialogueSessionContext CreateDialogueSessionContext()
        {
            Dialogue legacyDialogue = ResolveLegacyDialogue();
            TraderDialogueData contextDialogueData = dialogueData != null
                ? dialogueData
                : legacyDialogue != null
                    ? legacyDialogue.ConfiguredDialogueData
                    : null;

            string[] fallbackLines = contextDialogueData == null && legacyDialogue != null
                ? legacyDialogue.ConfiguredLines
                : null;

            float fallbackTextSpeed = legacyDialogue != null ? legacyDialogue.ConfiguredTextSpeed : 0f;
            bool fallbackIsBoss = legacyDialogue != null && legacyDialogue.ConfiguredIsBoss;
            bool fallbackAdvanceObjectiveOnEnd = legacyDialogue == null || legacyDialogue.ConfiguredAdvanceObjectiveOnEnd;

            if (contextDialogueData == null && (fallbackLines == null || fallbackLines.Length == 0))
            {
                if (!loggedMissingLegacyDialogue)
                {
                    loggedMissingLegacyDialogue = true;
                    Debug.LogWarning($"{nameof(Trader)} on '{name}' has no dialogue data or legacy dialogue lines to present.", this);
                }

                return null;
            }

            return new NpcDialogueSessionContext(
                contextDialogueData,
                fallbackLines,
                fallbackTextSpeed,
                fallbackIsBoss,
                fallbackAdvanceObjectiveOnEnd,
                Shop);
        }

        public void OnDialogueSessionOpened(NpcDialogueSessionContext context)
        {
            isInteracting = true;
            isShopOpen = false;

            if (Player != null)
                Player.ApplyTraderOfferPresentation(InteractionLookTarget);
        }

        public void OnDialogueShopOpened()
        {
            isInteracting = true;
            isShopOpen = true;

            if (Player != null)
                Player.ShowCursor();
        }

        public void OnDialogueShopClosed()
        {
            isInteracting = true;
            isShopOpen = false;

            if (Player != null)
                Player.ShowCursor();
        }

        public void OnDialogueSessionClosed(NpcDialogueSessionCloseReason reason)
        {
            if (reason == NpcDialogueSessionCloseReason.DialogueCompleted
                || reason == NpcDialogueSessionCloseReason.ExternalCancel
                || reason == NpcDialogueSessionCloseReason.SourceDisabled)
            {
                skipPressed = true;
            }

            isInteracting = false;
            isShopOpen = false;
            HideLegacyUi();

            if (Player != null)
                Player.RestoreGameplayAfterTrader();
        }

        public void OpenShop()
        {
            if (Shop == null)
            {
                Debug.LogWarning($"{nameof(Trader)} on '{name}' cannot open shop: {nameof(Shop)} reference is missing.", this);
                return;
            }

            NpcDialoguePresenter presenter = NpcDialoguePresenter.ResolveInstance();
            if (presenter != null && presenter.TryOpenShop(this))
                return;

            isInteracting = true;
            isShopOpen = true;
            RefreshLegacyShopUiState();
        }

        public void activateSkip()
        {
            NpcDialoguePresenter presenter = NpcDialoguePresenter.ResolveInstance();
            if (presenter != null && presenter.TryCloseSession(this, NpcDialogueSessionCloseReason.ExternalCancel))
                return;

            skipPressed = true;
            isInteracting = false;
            isShopOpen = false;
            HideLegacyUi();

            if (Player != null)
                Player.RestoreGameplayAfterTrader();
        }

        public void CloseShop()
        {
            NpcDialoguePresenter presenter = NpcDialoguePresenter.ResolveInstance();
            if (presenter != null && presenter.TryCloseShop(this))
                return;

            isShopOpen = false;
            RefreshLegacyShopUiState();
        }

        public void Honey()
        {
            if (Player == null)
            {
                Debug.LogError($"{nameof(Trader)}.{nameof(Honey)} on '{name}' has null {nameof(Player)}.", this);
                return;
            }

            Player.HoneyON();
        }

        void OnDisable()
        {
            if (isRegisteredWithPresenter)
            {
                var presenter = NpcDialoguePresenter.ResolveInstance();
                if (presenter != null)
                    presenter.UnregisterSource(this, NpcDialogueSessionCloseReason.SourceDisabled);
            }

            isRegisteredWithPresenter = false;
            isPlayerInRange = false;
            isInteracting = false;
            isShopOpen = false;
            HideLegacyUi();

            if (Player != null && Player.isAtTrader)
                Player.RestoreGameplayAfterTrader();
        }

        void DeactivateLegacyPromptObjects()
        {
            if (TradeText != null)
                TradeText.SetActive(false);
        }

        void ValidateSerializedReferences()
        {
            if (Merchant == null)
            {
                Merchant = gameObject;
                if (!loggedMissingMerchant)
                {
                    loggedMissingMerchant = true;
                    Debug.LogError($"{nameof(Trader)} on '{name}' had null {nameof(Merchant)}; defaulted to this GameObject.", this);
                }
            }

            if (DialoguePanel == null && !loggedMissingDialoguePanel)
            {
                loggedMissingDialoguePanel = true;
                Debug.LogWarning($"{nameof(Trader)} on '{name}' has no {nameof(DialoguePanel)} assigned.", this);
            }

            if (Shop == null && !loggedMissingShop)
            {
                loggedMissingShop = true;
                Debug.LogWarning($"{nameof(Trader)} on '{name}' has no {nameof(Shop)} assigned (assign an inactive placeholder if no shop).", this);
            }

            if (PanelPopUp <= 0f)
                Debug.LogWarning($"{nameof(Trader)} on '{name}' has {nameof(PanelPopUp)} <= 0; interaction range will never activate.", this);
        }

        void UpdatePresenterRegistration()
        {
            var presenter = NpcDialoguePresenter.ResolveInstance();
            if (presenter == null)
            {
                isRegisteredWithPresenter = false;
                return;
            }

            bool shouldRegister = isPlayerInRange;
            if (shouldRegister)
            {
                presenter.RegisterSource(this);
                isRegisteredWithPresenter = true;
            }
            else if (isRegisteredWithPresenter)
            {
                presenter.UnregisterSource(this, NpcDialogueSessionCloseReason.PlayerLeftRange);
                isRegisteredWithPresenter = false;
            }
        }

        void MaintainTraderPresentation()
        {
            if (!isInteracting && !isShopOpen)
                return;

            if (Player != null)
                Player.ApplyTraderOfferPresentation(InteractionLookTarget);
        }

        void UpdateNpcRotation()
        {
            if (Player == null || Merchant == null || !Rotate)
                return;

            if (isPlayerInRange && !skipPressed && (IsInteractionAvailable || isInteracting || isShopOpen))
            {
                Vector3 toPlayer = Player.transform.position - Merchant.transform.position;
                if (toPlayer.sqrMagnitude > LookRotationEpsilon)
                    Player.rotGoal = Quaternion.LookRotation(toPlayer);

                if (PlayerDistance.sqrMagnitude > LookRotationEpsilon)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(PlayerDistance), 0.1f);

                return;
            }

            transform.rotation = Quaternion.Slerp(transform.rotation, FormalLook, 0.1f);
        }

        Dialogue ResolveLegacyDialogue()
        {
            if (DialoguePanel == null)
                return null;

            Dialogue legacyDialogue = DialoguePanel.GetComponent<Dialogue>();
            if (legacyDialogue != null)
                return legacyDialogue;

            return DialoguePanel.GetComponentInChildren<Dialogue>(true);
        }

        static void SafeSetActive(GameObject go, bool active)
        {
            if (go != null)
                go.SetActive(active);
        }

        static void SetActiveWithParents(Transform target, bool active)
        {
            if (target == null)
                return;

            if (active)
            {
                Transform node = target;
                while (node != null)
                {
                    if (!node.gameObject.activeSelf)
                        node.gameObject.SetActive(true);

                    node = node.parent;
                }
            }

            target.gameObject.SetActive(active);
        }

        bool IsShopNestedUnderDialoguePanel()
        {
            return DialoguePanel != null && Shop != null && Shop.transform.IsChildOf(DialoguePanel.transform);
        }

        void ShowOnlyShopUnderDialoguePanel()
        {
            SafeSetActive(DialoguePanel, true);
            for (int i = 0; i < DialoguePanel.transform.childCount; i++)
            {
                Transform child = DialoguePanel.transform.GetChild(i);
                bool shouldShow = child.gameObject == Shop || Shop.transform.IsChildOf(child);
                child.gameObject.SetActive(shouldShow);
            }

            SetActiveWithParents(Shop.transform, true);
        }

        void ShowDialogueHideShop()
        {
            if (IsShopNestedUnderDialoguePanel())
            {
                SafeSetActive(DialoguePanel, true);
                for (int i = 0; i < DialoguePanel.transform.childCount; i++)
                {
                    Transform child = DialoguePanel.transform.GetChild(i);
                    bool isShopBranch = child.gameObject == Shop || Shop.transform.IsChildOf(child);
                    child.gameObject.SetActive(!isShopBranch);
                }
            }
            else
            {
                SafeSetActive(DialoguePanel, true);
                SafeSetActive(Shop, false);
            }
        }

        void RefreshLegacyShopUiState()
        {
            if (!isShopOpen)
            {
                if (isInteracting)
                    ShowDialogueHideShop();
                else
                    HideLegacyUi();

                return;
            }

            if (Shop == null)
                return;

            if (IsShopNestedUnderDialoguePanel())
                ShowOnlyShopUnderDialoguePanel();
            else
            {
                SafeSetActive(DialoguePanel, false);
                SetActiveWithParents(Shop.transform, true);
            }
        }

        void HideLegacyUi()
        {
            SafeSetActive(DialoguePanel, false);
            SafeSetActive(Shop, false);
        }
    }
}
