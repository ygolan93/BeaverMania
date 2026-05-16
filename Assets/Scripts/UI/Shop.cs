using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BeaverPlayer = Beavermania.Player.BeaverPlayerBehaviour;

namespace Beavermania.UI.Menus
{

    public class Shop : MonoBehaviour
    {
        public BeaverPlayer Player;

        [SerializeField] private int hammerPrice = 40;
        [SerializeField] private int bowAndArrowPrice = 120;
        [SerializeField] private int arrowBundlePrice = 25;
        [SerializeField] private int swordAndShieldPrice = 150;
        [SerializeField] private int arrowsPerBundle = 10;
        [SerializeField] private int bowStarterArrows = 5;

        const int GoldBrickPrice = 150;

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

        bool TrySpendCurrency(int price)
        {
            if (Player == null)
                return false;

            if (Player.Currency < price)
                return false;

            Player.Currency -= price;
            return true;
        }

        public void BuyNuts()
        {
            if (Player == null)
                return;

            if (Player.Currency >= 3)
            {
                Player.NutCount++;
                Player.Currency -= 3;
            }
        }

        public void SellNuts()
        {
            if (Player == null)
                return;

            if (Player.NutCount > 0)
            {
                Player.NutCount--;
                Player.Currency += 3;
            }
        }

        public void BuyApple()
        {
            if (Player == null)
                return;

            if (Player.Currency >= 5)
            {
                Player.Apple++;
                Player.Currency -= 5;
            }
        }

        public void SellApple()
        {
            if (Player == null)
                return;

            if (Player.Apple > 0)
            {
                Player.Apple--;
                Player.Currency += 5;
            }
        }

        public void BuyAccesory()
        {
            if (Player == null)
                return;

            if (Player.Currency >= 60)
            {
                Player.GobletPickup++;
                Player.Currency -= 60;
            }
        }

        public void SellAccesory()
        {
            if (Player == null)
                return;

            if (Player.GobletPickup > 0)
            {
                Player.GobletPickup--;
                Player.Currency += 60;
            }
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

            if (!TrySpendCurrency(hammerPrice))
                return;

            Player.TryAcquireHammersFromShop();
        }

        public void BuyBowAndArrow()
        {
            if (Player == null)
                return;

            if (Player.OwnsArsenalItem("Bow"))
                return;

            if (!TrySpendCurrency(bowAndArrowPrice))
                return;

            Player.TryAcquireBowFromShop(bowStarterArrows);
        }

        public void BuyArrowBundle()
        {
            if (Player == null)
                return;

            if (!TrySpendCurrency(arrowBundlePrice))
                return;

            Player.AddArrowMunition(arrowsPerBundle);
        }

        public void BuySwordAndShield()
        {
            if (Player == null)
                return;

            if (Player.OwnsArsenalItem("ArmorSet"))
                return;

            if (!TrySpendCurrency(swordAndShieldPrice))
                return;

            Player.TryAcquireArmorSetFromShop();
        }
    }
}
