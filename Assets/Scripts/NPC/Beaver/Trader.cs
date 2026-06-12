using TMPro;
using UnityEngine;
using Beavermania.Core.GameFlow;
using Beavermania.Core.Input;
using Beavermania.Data.Dialogue;
using Beavermania.Player;
using BeaverPlayer = Beavermania.Player.BeaverPlayerBehaviour;

namespace Beavermania.NPC
{
    public class Trader : MonoBehaviour
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
        [SerializeField] KeyCode interactKey = KeyCode.E;
        [SerializeField] GameObject playerCanvas;
        [SerializeField] TextMeshProUGUI objectiveText;
        [SerializeField] string interactionPromptMessage = DefaultInteractionPromptMessage;
        [SerializeField] TraderDialogueData dialogueData;

        public TraderDialogueData DialogueData => dialogueData;

        bool isPlayerInRange;
        bool wasPlayerInRange;
        bool isInteracting;
        bool isShopOpen;
        bool hasStoredPreviousObjectiveText;
        bool isShowingInteractionPrompt;
        bool traderOfferPresentationActive;
        string previousObjectiveText;
        PlayerHudState playerHudState;
        Quaternion FormalLook;
        bool loggedMissingMerchant;
        bool loggedMissingDialoguePanel;
        bool loggedMissingShop;
        bool loggedMissingObjectiveText;
        bool loggedMissingPlayerCanvas;
        bool loggedMissingPlayerCamera;

        public float GetOfferPanelDistance() => PanelPopUp;

        public bool IsInteractionEngaged() => isInteracting;

        public bool IsTraderSessionActive() => isInteracting || isShopOpen;

        void Start()
        {
            FormalLook = transform.rotation;
            var playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                Player = playerObject.GetComponent<BeaverPlayer>();
            var rootObject = GameObject.FindGameObjectWithTag("PlayerRoot");
            if (rootObject != null)
                PlayerRoot = rootObject.transform;
            DeactivateLegacyPromptObjects();
            ValidateSerializedReferences();
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
                Debug.LogWarning($"{nameof(Trader)} on '{name}' has {nameof(PanelPopUp)} <= 0; proximity UI will never activate.", this);

            if (playerCanvas == null && DialoguePanel != null)
            {
                var canvas = DialoguePanel.GetComponentInParent<Canvas>();
                if (canvas != null)
                    playerCanvas = canvas.gameObject;
            }

            if (playerCanvas == null)
            {
                var canvasObject = GameObject.Find("PlayerCanvas");
                if (canvasObject != null)
                    playerCanvas = canvasObject;
            }

            if (objectiveText == null && playerCanvas != null)
            {
                var texts = playerCanvas.GetComponentsInChildren<TextMeshProUGUI>(true);
                for (int i = 0; i < texts.Length; i++)
                {
                    if (texts[i].gameObject.name == "ObjectiveText")
                    {
                        objectiveText = texts[i];
                        break;
                    }
                }
            }

            if (objectiveText == null && !loggedMissingObjectiveText)
            {
                loggedMissingObjectiveText = true;
                Debug.LogWarning("Trader interaction is missing ObjectiveText reference.", this);
            }

            if (playerCanvas == null && !loggedMissingPlayerCanvas)
            {
                loggedMissingPlayerCanvas = true;
                Debug.LogWarning($"{nameof(Trader)} on '{name}' could not resolve {nameof(playerCanvas)}.", this);
            }

            if (Player != null)
            {
                playerHudState = Player.GetComponent<PlayerHudState>();
                if (Player.CamForTraders == null && !loggedMissingPlayerCamera)
                {
                    loggedMissingPlayerCamera = true;
                    Debug.LogWarning(
                        $"{nameof(Trader)} on '{name}': player {nameof(BeaverPlayer.CamForTraders)} is not assigned; trader camera transition will not run.",
                        this);
                }
            }
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

        bool IsDialogueCurrentlyOpen()
        {
            if (!isInteracting || isShopOpen || DialoguePanel == null)
                return false;
            return DialoguePanel.activeInHierarchy;
        }

        bool IsShopCurrentlyOpen()
        {
            return isShopOpen && Shop != null && Shop.activeInHierarchy;
        }

        bool CanShowProximityPrompt()
        {
            return isPlayerInRange && !isInteracting && !isShopOpen && !skipPressed;
        }

        string ReadDisplayedObjectiveText()
        {
            if (playerHudState != null && !playerHudState.ObjectiveTextOverrideActive && !string.IsNullOrEmpty(playerHudState.ObjectiveText))
                return playerHudState.ObjectiveText;

            if (objectiveText != null)
                return objectiveText.text;

            if (Player != null && Player.PlayerObjective != null && !string.IsNullOrEmpty(Player.PlayerObjective.Instruction))
                return Player.PlayerObjective.Instruction;

            return string.Empty;
        }

        void StorePreviousObjectiveTextOnce()
        {
            if (hasStoredPreviousObjectiveText)
                return;

            previousObjectiveText = ReadDisplayedObjectiveText();
            hasStoredPreviousObjectiveText = true;
        }

        void ShowInteractionPromptOnObjective()
        {
            if (objectiveText == null && playerHudState == null)
                return;

            StorePreviousObjectiveTextOnce();

            string message = string.IsNullOrEmpty(interactionPromptMessage)
                ? DefaultInteractionPromptMessage
                : interactionPromptMessage;

            if (playerHudState != null)
            {
                playerHudState.ObjectiveTextOverride = message;
                playerHudState.ObjectiveTextOverrideActive = true;
            }

            if (objectiveText != null)
                objectiveText.text = message;

            isShowingInteractionPrompt = true;
        }

        void RestorePreviousObjectiveText()
        {
            if (playerHudState != null)
            {
                playerHudState.ObjectiveTextOverrideActive = false;
                playerHudState.ObjectiveTextOverride = null;
            }

            var objectiveService = ObjectiveSyncService.Instance;
            if (objectiveService != null)
            {
                objectiveService.RefreshBindingsAndReapply();
            }
            else
            {
                if (playerHudState != null && hasStoredPreviousObjectiveText)
                    playerHudState.ObjectiveText = previousObjectiveText;

                if (objectiveText != null && hasStoredPreviousObjectiveText)
                    objectiveText.text = previousObjectiveText ?? string.Empty;
            }

            hasStoredPreviousObjectiveText = false;
            isShowingInteractionPrompt = false;
        }

        void BeginProximityPrompt()
        {
            if (!CanShowProximityPrompt() || isShowingInteractionPrompt)
                return;

            ShowInteractionPromptOnObjective();
        }

        void ShowOnlyShopUnderDialoguePanel()
        {
            SafeSetActive(DialoguePanel, true);
            for (int i = 0; i < DialoguePanel.transform.childCount; i++)
            {
                Transform child = DialoguePanel.transform.GetChild(i);
                child.gameObject.SetActive(child.gameObject == Shop);
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
                    child.gameObject.SetActive(child.gameObject != Shop);
                }
            }
            else
            {
                SafeSetActive(DialoguePanel, true);
                SafeSetActive(Shop, false);
            }
        }

        void RefreshShopUiState()
        {
            if (!isPlayerInRange || skipPressed)
            {
                isShopOpen = false;
                SafeSetActive(DialoguePanel, false);
                SafeSetActive(Shop, false);
                return;
            }

            if (isShopOpen)
            {
                if (IsShopNestedUnderDialoguePanel())
                    ShowOnlyShopUnderDialoguePanel();
                else
                {
                    SafeSetActive(DialoguePanel, false);
                    SetActiveWithParents(Shop.transform, true);
                }

                if (Player != null)
                    Player.ShowCursor();
                return;
            }

            if (!isInteracting)
            {
                SafeSetActive(DialoguePanel, false);
                SafeSetActive(Shop, false);
                return;
            }

            ShowDialogueHideShop();
        }

        bool WasInteractKeyPressed()
        {
            if (interactKey == KeyCode.E)
                return PlayerInputReader.WasWorldInteractPressed();
            return PlayerInputReader.WasKeyPressed(interactKey);
        }

        void OpenInteraction()
        {
            if (isInteracting || isShopOpen || skipPressed || Player == null || !isPlayerInRange)
                return;

            RestorePreviousObjectiveText();
            isInteracting = true;
            traderOfferPresentationActive = true;
            Player.ApplyTraderOfferPresentation(transform);
            RefreshShopUiState();
        }

        void CloseInteraction()
        {
            if (!IsTraderSessionActive() && !IsDialogueCurrentlyOpen() && !IsShopCurrentlyOpen())
                return;

            isInteracting = false;
            isShopOpen = false;
            traderOfferPresentationActive = false;
            SafeSetActive(DialoguePanel, false);
            SafeSetActive(Shop, false);

            if (Player != null)
                Player.RestoreGameplayAfterTrader();

            RestorePreviousObjectiveText();

            if (CanShowProximityPrompt())
                BeginProximityPrompt();
        }

        void LeaveTraderRange()
        {
            isInteracting = false;
            isShopOpen = false;
            RestorePreviousObjectiveText();
            SafeSetActive(DialoguePanel, false);
            SafeSetActive(Shop, false);

            if (traderOfferPresentationActive && Player != null)
            {
                traderOfferPresentationActive = false;
                Player.RestoreGameplayAfterTrader();
            }
        }

        void MaintainTraderPresentation()
        {
            if (!isInteracting && !isShopOpen)
                return;

            if (skipPressed)
                return;

            traderOfferPresentationActive = true;
            if (Player != null)
                Player.ApplyTraderOfferPresentation(transform);
        }

        void UpdateNpcRotation()
        {
            if (Player == null || Merchant == null || !Rotate)
                return;

            if (isPlayerInRange && !skipPressed && (isInteracting || CanShowProximityPrompt()))
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

        public void Update()
        {
            if (Player == null || Merchant == null)
                return;

            PlayerDistance = Player.transform.position - Merchant.transform.position;
            float distance = PlayerDistance.magnitude;
            wasPlayerInRange = isPlayerInRange;
            isPlayerInRange = distance < PanelPopUp;

            if (!isPlayerInRange)
            {
                if (wasPlayerInRange)
                    LeaveTraderRange();

                if (distance > PanelPopUp)
                    skipPressed = false;

                UpdateNpcRotation();
                return;
            }

            if (isPlayerInRange && !wasPlayerInRange && CanShowProximityPrompt())
                BeginProximityPrompt();

            if (WasInteractKeyPressed())
            {
                if (IsTraderSessionActive() || IsDialogueCurrentlyOpen() || IsShopCurrentlyOpen())
                    CloseInteraction();
                else if (!skipPressed)
                    OpenInteraction();
            }

            if (!isInteracting && !isShopOpen && !skipPressed)
            {
                if (traderOfferPresentationActive && Player != null)
                {
                    traderOfferPresentationActive = false;
                    Player.RestoreGameplayAfterTrader();
                }

                if (!isShowingInteractionPrompt && CanShowProximityPrompt())
                    BeginProximityPrompt();

                RefreshShopUiState();
                UpdateNpcRotation();
                return;
            }

            if (isShowingInteractionPrompt)
                RestorePreviousObjectiveText();

            MaintainTraderPresentation();
            RefreshShopUiState();
            UpdateNpcRotation();
        }

        public void OpenShop()
        {
            if (Shop == null)
            {
                Debug.LogWarning($"{nameof(Trader)} on '{name}' cannot open shop: {nameof(Shop)} reference is missing.", this);
                return;
            }

            RestorePreviousObjectiveText();
            isInteracting = true;
            isShopOpen = true;
            RefreshShopUiState();
        }

        public void activateSkip()
        {
            skipPressed = true;
            isInteracting = false;
            isShopOpen = false;
            traderOfferPresentationActive = false;
            RestorePreviousObjectiveText();
            SafeSetActive(DialoguePanel, false);
            SafeSetActive(Shop, false);
            if (Player != null)
                Player.RestoreGameplayAfterTrader();
        }

        public void CloseShop()
        {
            isShopOpen = false;
            SafeSetActive(Shop, false);
            RefreshShopUiState();
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
            isPlayerInRange = false;
            wasPlayerInRange = false;
            isInteracting = false;
            isShopOpen = false;
            RestorePreviousObjectiveText();
            SafeSetActive(DialoguePanel, false);
            SafeSetActive(Shop, false);
            if (Player != null && Player.isAtTrader)
                Player.RestoreGameplayAfterTrader();
        }
    }
}
