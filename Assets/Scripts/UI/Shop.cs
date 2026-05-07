using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shop : MonoBehaviour
{
    public Behaviour Player;
    // Start is called before the first frame update
    void Start()
    {
        var playerObject = GameObject.FindGameObjectWithTag("Player");
        if (!RuntimeReferenceValidator.Require(playerObject, this, "Player tag"))
        {
            return;
        }

        Player = playerObject.GetComponent<Behaviour>();
        RuntimeReferenceValidator.Require(Player, this, nameof(Player));
    }

    // Update is called once per frame

    public void BuyNuts()
    {
        if (Player.Currency >= 3)
        {
            Player.NutCount++;
            Player.Currency -= 3;
        }
    }
    public void SellNuts()
    {
        if (Player.NutCount > 0)
        {
            Player.NutCount--;
            Player.Currency += 3;
        }
    }
    public void BuyApple()
    {
        if (Player.Currency >= 5)
        {
            Player.Apple++;
            Player.Currency -= 5;
        }
    }
    public void SellApple()
    {
        if (Player.Apple > 0)
        {
            Player.Apple--;
            Player.Currency += 5;
        }
    }
    public void BuyAccesory()
    {
        if (Player.Currency >= 60)
        {
            Player.GobletPickup++;
            Player.Currency -= 60;
        }
    }
    public void SellAccesory()
    {
        if (Player.GobletPickup > 0)
        {
            Player.GobletPickup--;
            Player.Currency += 60;
        }
    }
    public void BuyGoldBrick()
    {
        if (Player.GoldPicked == false)
        {
            Player.GoldON();
            Player.Currency -= 150;
        }
    }
    public void SellGoldBrick()
    {
        if (Player.GoldPicked==true)
        {
            Player.GoldOFF();
            Player.Currency += 150;
        }
    }
       

}
