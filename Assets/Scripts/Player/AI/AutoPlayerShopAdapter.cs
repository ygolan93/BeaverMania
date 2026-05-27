using Beavermania.Player;
using Beavermania.UI.Menus;
using UnityEngine;

namespace Beavermania.Player.AI
{
    [DisallowMultipleComponent]
    public class AutoPlayerShopAdapter : MonoBehaviour
    {
        const int LowArrowThreshold = 3;
        const float LowHealthRatio = 0.45f;

        [SerializeField] AutoPlayerActionAdapter actionAdapter;
        [SerializeField] int minCurrencyReserve = 10;

        Shop _resolvedShop;
        float _lastPurchaseTime;
        [SerializeField] float purchaseCooldown = 1.5f;

        void Awake()
        {
            if (actionAdapter == null)
                actionAdapter = GetComponent<AutoPlayerActionAdapter>();
        }

        public void BindShop(Shop shop)
        {
            if (shop != null)
                _resolvedShop = shop;
        }

        public Shop ResolveShopFromTrader(Transform traderRoot)
        {
            if (_resolvedShop != null)
                return _resolvedShop;

            if (traderRoot == null)
                return null;

            _resolvedShop = traderRoot.GetComponentInChildren<Shop>(true);
            return _resolvedShop;
        }

        public bool PlayerNeedsShopVisit()
        {
            BeaverPlayerBehaviour player = actionAdapter != null ? actionAdapter.Player : null;
            if (player == null)
                return false;

            if (!player.OwnsArsenalItem("Hammers"))
                return true;

            if (!player.OwnsArsenalItem("Bow"))
                return true;

            if (player.OwnsArsenalItem("Bow") && player.arrowMunition <= LowArrowThreshold)
                return true;

            if (actionAdapter != null && actionAdapter.HealthRatio() < LowHealthRatio && player.Apple <= 0)
                return true;

            return false;
        }

        public bool CanAffordAnyUsefulPurchase()
        {
            BeaverPlayerBehaviour player = actionAdapter != null ? actionAdapter.Player : null;
            if (player == null)
                return false;

            int wallet = player.Currency;
            if (wallet <= minCurrencyReserve)
                return false;

            if (!player.OwnsArsenalItem("Hammers"))
                return wallet > minCurrencyReserve + 40;

            if (!player.OwnsArsenalItem("Bow"))
                return wallet > minCurrencyReserve + 120;

            if (player.OwnsArsenalItem("Bow") && player.arrowMunition <= LowArrowThreshold)
                return wallet > minCurrencyReserve + 25;

            if (actionAdapter != null && actionAdapter.HealthRatio() < LowHealthRatio && player.Apple <= 0)
                return wallet > minCurrencyReserve + 5;

            return false;
        }

        public bool TryAutoPurchaseBestNeed()
        {
            if (Time.time - _lastPurchaseTime < purchaseCooldown)
                return false;

            BeaverPlayerBehaviour player = actionAdapter != null ? actionAdapter.Player : null;
            Shop shop = _resolvedShop;
            if (player == null || shop == null)
                return false;

            if (player.Currency <= minCurrencyReserve)
                return false;

            if (!player.OwnsArsenalItem("Hammers"))
            {
                shop.BuyHammer();
                if (player.OwnsArsenalItem("Hammers"))
                {
                    _lastPurchaseTime = Time.time;
                    return true;
                }
            }

            if (!player.OwnsArsenalItem("Bow"))
            {
                shop.BuyBowAndArrow();
                if (player.OwnsArsenalItem("Bow"))
                {
                    _lastPurchaseTime = Time.time;
                    return true;
                }
            }

            if (player.OwnsArsenalItem("Bow") && player.arrowMunition <= LowArrowThreshold)
            {
                int before = player.arrowMunition;
                shop.BuyArrowBundle();
                if (player.arrowMunition > before)
                {
                    _lastPurchaseTime = Time.time;
                    return true;
                }
            }

            if (actionAdapter != null && actionAdapter.HealthRatio() < LowHealthRatio && player.Apple <= 0)
            {
                int before = player.Apple;
                shop.BuyApple();
                if (player.Apple > before)
                {
                    _lastPurchaseTime = Time.time;
                    return true;
                }
            }

            return false;
        }
    }
}
