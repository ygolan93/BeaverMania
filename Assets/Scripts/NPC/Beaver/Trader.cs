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

        // Update is called once per frame
        public void Update()
        {
            if (Player == null || Merchant == null)
                return;

            PlayerDistance = Player.transform.position - Merchant.transform.position;
            var Distance = Mathf.Abs(PlayerDistance.magnitude);
            bool wantOfferPresentation = Player != null && Distance < PanelPopUp && skipPressed == false;
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

            if (Distance<PanelPopUp&&skipPressed==false)
            {
                SafeSetActive(TradeText, true);
                SafeSetActive(DialoguePanel, true);
                if (Rotate == true)
                {
                    Vector3 toPlayer = Player.transform.position - Merchant.transform.position;
                    if (toPlayer.sqrMagnitude > LookRotationEpsilon)
                        Player.rotGoal = Quaternion.LookRotation(toPlayer);
                    if (PlayerDistance.sqrMagnitude > LookRotationEpsilon)
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(PlayerDistance), 0.1f);
                }
            }

            else
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, FormalLook, 0.1f);
                SafeSetActive(TradeText, false);
                SafeSetActive(DialoguePanel, false);
                SafeSetActive(Shop, false);
            }

            if (Distance > PanelPopUp)
            {
                skipPressed = false;
            }
        }
        public void activateSkip()
        {
            skipPressed = true;
            traderOfferPresentationActive = false;
            SafeSetActive(TradeText, false);
            SafeSetActive(DialoguePanel, false);
            SafeSetActive(Shop, false);
            if (Player != null)
                Player.RestoreGameplayAfterTrader();
        }

        public void CloseShop()
        {
            SafeSetActive(Shop, false);
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
            traderOfferPresentationActive = false;
            if (Player != null && Player.isAtTrader)
                Player.RestoreGameplayAfterTrader();
        }
    }
}
