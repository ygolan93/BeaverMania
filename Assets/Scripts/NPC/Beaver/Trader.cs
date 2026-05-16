using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using BeaverPlayer = Beavermania.Player.BeaverPlayerBehaviour;

namespace Beavermania.NPC
{

    public class Trader : MonoBehaviour
    {
        const float LookRotationEpsilon = 0.0001f;
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
        bool shopOpen;
        bool traderOfferPresentationActive;
        Quaternion FormalLook;
        bool loggedMissingMerchant;
        bool loggedMissingDialoguePanel;
        bool loggedMissingShop;
        bool loggedMissingTradeText;

        public float GetOfferPanelDistance()
        {
            return PanelPopUp;
        }
        // Start is called before the first frame update
        void Start()
        {
            FormalLook = transform.rotation;
            var playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                Player = playerObject.GetComponent<BeaverPlayer>();
            var rootObject = GameObject.FindGameObjectWithTag("PlayerRoot");
            if (rootObject != null)
                PlayerRoot = rootObject.transform;
            ValidateSerializedReferences();
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
                Debug.LogError($"{nameof(Trader)} on '{name}' has no {nameof(DialoguePanel)} assigned.", this);
            }

            if (Shop == null && !loggedMissingShop)
            {
                loggedMissingShop = true;
                Debug.LogError($"{nameof(Trader)} on '{name}' has no {nameof(Shop)} assigned (assign an inactive placeholder if no shop).", this);
            }

            if (PanelPopUp <= 0f)
                Debug.LogWarning($"{nameof(Trader)} on '{name}' has {nameof(PanelPopUp)} <= 0; proximity UI will never activate.", this);

            if (TradeText == null && !loggedMissingTradeText)
            {
                loggedMissingTradeText = true;
                Debug.LogWarning($"{nameof(Trader)} on '{name}' has no {nameof(TradeText)}; proximity prompt will be skipped.", this);
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

        void ShowOnlyShopUnderDialoguePanel()
        {
            SafeSetActive(DialoguePanel, true);
            for (int i = 0; i < DialoguePanel.transform.childCount; i++)
            {
                Transform child = DialoguePanel.transform.GetChild(i);
                bool showShop = child.gameObject == Shop;
                Debug.Log($"Trader '{name}': ShowOnlyShop child '{child.name}' => {(showShop ? "show" : "hide")}", this);
                child.gameObject.SetActive(showShop);
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
                    bool showDialogueChild = child.gameObject != Shop;
                    Debug.Log($"Trader '{name}': ShowDialogueHideShop child '{child.name}' => {(showDialogueChild ? "show" : "hide")}", this);
                    child.gameObject.SetActive(showDialogueChild);
                }
            }
            else
            {
                SafeSetActive(DialoguePanel, true);
                SafeSetActive(Shop, false);
            }
        }

        void RefreshShopUiState(bool inRange)
        {
            if (!inRange || skipPressed)
            {
                shopOpen = false;
                SafeSetActive(TradeText, false);
                SafeSetActive(DialoguePanel, false);
                SafeSetActive(Shop, false);
                return;
            }

            if (shopOpen)
            {
                SafeSetActive(TradeText, false);
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

            SafeSetActive(TradeText, true);
            ShowDialogueHideShop();
        }

        // Update is called once per frame
        public void Update()
        {
            if (Player == null || Merchant == null)
                return;

            PlayerDistance = Player.transform.position - Merchant.transform.position;
            var Distance = Mathf.Abs(PlayerDistance.magnitude);
            bool inRange = Distance < PanelPopUp;
            bool wantOfferPresentation = inRange && skipPressed == false;
            if (wantOfferPresentation)
            {
                traderOfferPresentationActive = true;
                Player.ApplyTraderOfferPresentation(transform);
            }
            else if (traderOfferPresentationActive && Player != null)
            {
                traderOfferPresentationActive = false;
                Player.RestoreGameplayAfterTrader();
            }

            RefreshShopUiState(inRange);

            if (inRange && skipPressed == false && !shopOpen)
            {
                if (Rotate == true)
                {
                    Vector3 toPlayer = Player.transform.position - Merchant.transform.position;
                    if (toPlayer.sqrMagnitude > LookRotationEpsilon)
                        Player.rotGoal = Quaternion.LookRotation(toPlayer);
                    if (PlayerDistance.sqrMagnitude > LookRotationEpsilon)
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(PlayerDistance), 0.1f);
                }
            }
            else if (!inRange)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, FormalLook, 0.1f);
            }

            if (Distance > PanelPopUp)
                skipPressed = false;
        }

        public void OpenShop()
        {
            if (Shop == null)
            {
                Debug.LogWarning($"{nameof(Trader)} on '{name}' cannot open shop: {nameof(Shop)} reference is missing.", this);
                return;
            }

            Debug.Log($"Trader '{name}': OpenShop. Shop={Shop.name}", this);
            shopOpen = true;
            RefreshShopUiState(inRange: true);
        }

        public void activateSkip()
        {
            skipPressed = true;
            shopOpen = false;
            traderOfferPresentationActive = false;
            SafeSetActive(TradeText, false);
            SafeSetActive(DialoguePanel, false);
            SafeSetActive(Shop, false);
            if (Player != null)
                Player.RestoreGameplayAfterTrader();
        }

        public void CloseShop()
        {
            Debug.Log($"Trader '{name}': CloseShop. Restoring dialogue UI.", this);
            shopOpen = false;
            SafeSetActive(Shop, false);
            RefreshShopUiState(inRange: true);
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
            shopOpen = false;
            traderOfferPresentationActive = false;
            if (Player != null && Player.isAtTrader)
                Player.RestoreGameplayAfterTrader();
        }
    }
}
