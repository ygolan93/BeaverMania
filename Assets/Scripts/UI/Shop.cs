using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Beavermania.Data.Shop;
using BeaverPlayer = Beavermania.Player.BeaverPlayerBehaviour;

namespace Beavermania.UI.Menus
{

    public class Shop : MonoBehaviour
    {
        public BeaverPlayer Player;

        [SerializeField] ShopPricingData pricingData;
        [SerializeField] private int hammerPrice = 40;
        [SerializeField] private int bowAndArrowPrice = 120;
        [SerializeField] private int arrowBundlePrice = 25;
        [SerializeField] private int swordAndShieldPrice = 150;
        [SerializeField] private int arrowsPerBundle = 10;
        [SerializeField] private int bowStarterArrows = 5;

        const int FallbackNutPrice = 3;
        const int FallbackApplePrice = 5;
        const int FallbackAccessoryPrice = 60;
        const int FallbackGoldBrickPrice = 150;

        bool loggedMissingPricingData;

        int NutPrice => pricingData != null ? pricingData.nutPrice : FallbackNutPrice;
        int ApplePrice => pricingData != null ? pricingData.applePrice : FallbackApplePrice;
        int AccessoryPrice => pricingData != null ? pricingData.accessoryPrice : FallbackAccessoryPrice;
        int GoldBrickPrice => pricingData != null ? pricingData.goldBrickPrice : FallbackGoldBrickPrice;
        int HammerPrice => pricingData != null ? pricingData.hammerPrice : hammerPrice;
        int BowAndArrowPrice => pricingData != null ? pricingData.bowAndArrowPrice : bowAndArrowPrice;
        int ArrowBundlePrice => pricingData != null ? pricingData.arrowBundlePrice : arrowBundlePrice;
        int SwordAndShieldPrice => pricingData != null ? pricingData.swordAndShieldPrice : swordAndShieldPrice;
        int ArrowsPerBundle => pricingData != null ? pricingData.arrowsPerBundle : arrowsPerBundle;
        int BowStarterArrows => pricingData != null ? pricingData.bowStarterArrows : bowStarterArrows;

        void Start()
        {
            var playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject == null)
            {
                Debug.LogWarning($"{nameof(Shop)}: No GameObject with tag \"Player\" found. Shop purchases will not work.", this);
                return;
            }

            Player = playerObject.GetComponent<BeaverPlayer>();
            if (Player == null)
            {
                Debug.LogWarning(
                    $"{nameof(Shop)}: Player object \"{playerObject.name}\" is missing {nameof(BeaverPlayer)}. Shop purchases will not work.",
                    this);
            }
        }

        void LogMissingPricingDataOnce()
        {
            if (loggedMissingPricingData)
                return;

            loggedMissingPricingData = true;
            Debug.LogWarning($"{nameof(Shop)} on '{name}' has no {nameof(pricingData)}; using legacy serialized prices.", this);
        }

        bool TrySpendCurrency(int price)
        {
            if (Player == null)
                return false;

            if (pricingData == null)
                LogMissingPricingDataOnce();

            if (price < 0)
            {
                Debug.LogWarning($"{nameof(Shop)} on '{name}' blocked purchase: invalid price {price}.", this);
                return false;
            }

            if (Player.Currency < price)
                return false;

            Player.Currency -= price;
            return true;
        }

        public void BuyNuts()
        {
            if (Player == null)
                return;

            int price = NutPrice;
            if (Player.Currency >= price)
            {
                Player.NutCount++;
                Player.Currency -= price;
            }
        }

        public void SellNuts()
        {
            if (Player == null)
                return;

            int price = NutPrice;
            if (Player.NutCount > 0)
            {
                Player.NutCount--;
                Player.Currency += price;
            }
        }

        public void BuyApple()
        {
            if (Player == null)
                return;

            int price = ApplePrice;
            if (Player.Currency >= price)
            {
                Player.Apple++;
                Player.Currency -= price;
            }
        }

        public void SellApple()
        {
            if (Player == null)
                return;

            int price = ApplePrice;
            if (Player.Apple > 0)
            {
                Player.Apple--;
                Player.Currency += price;
            }
        }

        public void BuyAccesory()
        {
            Debug.LogWarning($"{nameof(Shop)}: Goblets are no longer sold at the trader shop.", this);
        }

        public void SellAccesory()
        {
            Debug.LogWarning($"{nameof(Shop)}: Goblets are no longer sold at the trader shop.", this);
        }

        public void BuyGoldBrick()
        {
            if (Player == null)
                return;

            if (Player.GoldPicked)
                return;

            if (!TrySpendCurrency(GoldBrickPrice))
                return;

            Player.GoldON();
        }

        public void SellGoldBrick()
        {
            if (Player == null)
                return;

            if (Player.GoldPicked == true)
            {
                Player.GoldOFF();
                Player.Currency += GoldBrickPrice;
            }
        }

        public void BuyHammer()
        {
            if (Player == null)
                return;

            if (Player.OwnsArsenalItem("Hammers"))
                return;

            if (!TrySpendCurrency(HammerPrice))
                return;

            Player.TryAcquireHammersFromShop();
        }

        public void BuyBowAndArrow()
        {
            if (Player == null)
                return;

            if (Player.OwnsArsenalItem("Bow"))
                return;

            if (!TrySpendCurrency(BowAndArrowPrice))
                return;

            Player.TryAcquireBowFromShop(BowStarterArrows);
        }

        public void BuyArrowBundle()
        {
            if (Player == null)
                return;

            if (!TrySpendCurrency(ArrowBundlePrice))
                return;

            Player.AddArrowMunition(ArrowsPerBundle);
        }

        public void BuySwordAndShield()
        {
            if (Player == null)
                return;

            if (Player.OwnsArsenalItem("ArmorSet"))
                return;

            if (!TrySpendCurrency(SwordAndShieldPrice))
                return;

            Player.TryAcquireArmorSetFromShop();
        }
    }
}

