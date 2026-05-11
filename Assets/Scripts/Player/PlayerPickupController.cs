using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerPickupController : MonoBehaviour
{
    readonly HashSet<int> processedPickupIds = new HashSet<int>();
    Behaviour owner;

    public void Initialize(Behaviour behaviour)
    {
        owner = behaviour;
    }

    void Awake()
    {
        if (owner == null)
        {
            owner = GetComponent<Behaviour>();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        TryProcessPickup(collision);
    }

    void OnCollisionStay(Collision collision)
    {
        TryProcessPickup(collision);
    }

    void TryProcessPickup(Collision collision)
    {
        if (owner == null || collision == null)
        {
            return;
        }

        GameObject pickup = collision.gameObject;
        if (pickup == null || !pickup.activeInHierarchy)
        {
            return;
        }

        string tag = pickup.tag;
        if (!IsPickupTag(tag))
        {
            return;
        }

        int id = pickup.GetInstanceID();
        if (!processedPickupIds.Add(id))
        {
            return;
        }

        if (!HandlePickupByTag(pickup, tag))
        {
            processedPickupIds.Remove(id);
        }
    }

    bool IsPickupTag(string tag)
    {
        switch (tag)
        {
            case "Part":
            case "Seed":
            case "Apple":
            case "GobletKey":
            case "Coin":
            case "Arrow":
            case "ArrowBundle":
            case "Weapon":
            case "Bow":
            case "Armor":
            case "Honey":
            case "Gold":
                return true;
            default:
                return false;
        }
    }

    bool HandlePickupByTag(GameObject pickup, string tag)
    {
        switch (tag)
        {
            case "Part":
                return TryPickupPart(pickup);
            case "Seed":
                PlayCrouchPickup(pickup);
                owner.AddSeedPickup();
                PlayPickItemSound();
                Destroy(pickup);
                return true;
            case "Apple":
                PlayCrouchPickup(pickup);
                PlayPickup2Sound();
                owner.AddApplePickup();
                Destroy(pickup);
                return true;
            case "GobletKey":
                PlayCrouchPickup(pickup);
                PlayPickup2Sound();
                owner.AddGobletPickup();
                Destroy(pickup);
                return true;
            case "Coin":
                owner.PlayPickupEffect(pickup.transform.position);
                PlayCoinSound();
                return true;
            case "Arrow":
                return TryPickupArrow(pickup);
            case "ArrowBundle":
                return TryPickupArrowBundle(pickup);
            case "Weapon":
                return PickupArsenalItem(pickup, "Hammers!", "Hammers", 0);
            case "Bow":
                return PickupArsenalItem(pickup, "Booya!", "Bow", 5);
            case "Armor":
                return PickupArsenalItem(pickup, "Oh my!", "ArmorSet", 0);
            case "Honey":
                return TryPickupHoney(pickup);
            case "Gold":
                return TryPickupGold(pickup);
            default:
                return false;
        }
    }

    bool TryPickupPart(GameObject pickup)
    {
        if (owner.Load == null || !owner.Load.CanCarry || !owner.grounded || Input.GetKey(KeyCode.Mouse0) || Input.GetKey(KeyCode.Mouse1))
        {
            return false;
        }

        PlayCrouch();
        PlayPickItemSound();
        owner.PlayPickupEffect(pickup.transform.position);
        Destroy(pickup);
        return true;
    }

    bool TryPickupArrow(GameObject pickup)
    {
        if (!owner.CanCollectArrowPickup)
        {
            return false;
        }

        PlayCrouch();
        PlayPickup2Sound();
        owner.arrowMunition++;
        owner.PlayPickupEffect(pickup.transform.position);
        owner.CountArrows();
        Destroy(pickup);
        return true;
    }

    bool TryPickupArrowBundle(GameObject pickup)
    {
        if (!owner.CanCollectArrowPickup)
        {
            return false;
        }

        PlayCrouch();
        PlayPickup2Sound();
        owner.PlayPickupEffect(pickup.transform.position);
        if (owner.arrowMunition + 10 < owner.ArrowCapacity)
        {
            owner.arrowMunition += 10;
        }
        else
        {
            for (int i = 0; i < (owner.arrowMunition - 10); i++)
            {
                Instantiate(owner.ArrowPickup, pickup.transform.position + new Vector3(0, i * 0.3f, 0), Quaternion.Euler(0, 0, 90));
            }
            owner.arrowMunition = owner.ArrowCapacity;
        }
        owner.CountArrows();
        Destroy(pickup);
        return true;
    }

    bool PickupArsenalItem(GameObject pickup, string message, string arsenalItem, int arrowsOnPickup)
    {
        owner.Plattering = message;
        owner.ChangeSpeech = 1;
        PlayCrouch();
        owner.Arsenal.Add(arsenalItem);
        owner.ArsenalCounter++;
        PlayPickItemSound();
        if (arrowsOnPickup > 0)
        {
            owner.arrowMunition = arrowsOnPickup;
            owner.CountArrows();
        }
        owner.PlayPickupEffect(pickup.transform.position);
        Destroy(pickup);
        return true;
    }

    bool TryPickupHoney(GameObject pickup)
    {
        if (owner.Honeypicked)
        {
            return false;
        }

        PlayCrouch();
        owner.HoneyON();
        PlayPickItemSound();
        owner.PlayPickupEffect(pickup.transform.position);
        Destroy(pickup);
        return true;
    }

    bool TryPickupGold(GameObject pickup)
    {
        if (owner.GoldPicked)
        {
            return false;
        }

        PlayCrouch();
        owner.GoldON();
        Destroy(pickup);
        return true;
    }

    void PlayCrouchPickup(GameObject pickup)
    {
        PlayCrouch();
        owner.PlayPickupEffect(pickup.transform.position);
    }

    void PlayCrouch()
    {
        if (owner.Otter != null)
        {
            owner.Otter.Play("Crouch");
        }
    }

    void PlayPickItemSound()
    {
        if (owner.Sound != null)
        {
            owner.Sound.PickItem();
        }
    }

    void PlayPickup2Sound()
    {
        if (owner.Sound != null)
        {
            owner.Sound.PickUp2();
        }
    }

    void PlayCoinSound()
    {
        if (owner.Sound != null)
        {
            owner.Sound.Coin();
        }
    }
}
