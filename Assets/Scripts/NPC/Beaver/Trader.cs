using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Trader : MonoBehaviour
{
    public GameObject Merchant;
    public Transform PlayerRoot;
    public Behaviour Player;
    public GameObject TradeText;
    public GameObject DialoguePanel;
    public GameObject Shop;
    public Vector3 PlayerDistance;
    public bool skipPressed = false;
    [SerializeField] bool Rotate;
    [SerializeField] float PanelPopUp;
    Quaternion FormalLook;
    // Start is called before the first frame update
    void Start()
    {
        FormalLook = transform.rotation;

        bool valid = RuntimeReferenceValidator.Require(Merchant, this, nameof(Merchant)) &
            RuntimeReferenceValidator.Require(TradeText, this, nameof(TradeText)) &
            RuntimeReferenceValidator.Require(DialoguePanel, this, nameof(DialoguePanel)) &
            RuntimeReferenceValidator.Require(Shop, this, nameof(Shop)) &
            RuntimeReferenceValidator.RequireTaggedComponent("Player", this, out Player) &
            RuntimeReferenceValidator.RequireTaggedComponent("PlayerRoot", this, out PlayerRoot);

        if (!valid)
        {
            return;
        }
    }

    // Update is called once per frame
    public void Update()
    {

        PlayerDistance = Player.transform.position - Merchant.transform.position;
        var Distance = Mathf.Abs(PlayerDistance.magnitude);
        if (Distance<PanelPopUp&&skipPressed==false)
        {
            TradeText.SetActive(true);
            DialoguePanel.SetActive(true);
            if (Rotate == true)
            {
                Player.rotGoal = SafeRotation.LookRotationOrCurrent(Player.transform.position - Merchant.transform.position, Player.transform.rotation);
                transform.rotation = Quaternion.Slerp(transform.rotation, SafeRotation.LookRotationOrCurrent(PlayerDistance, transform.rotation), 0.1f);
            }
        }

        else
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, FormalLook, 0.1f);
            TradeText.SetActive(false);
            DialoguePanel.SetActive(false);
            Shop.SetActive(false);
        }

        if (Distance > PanelPopUp)
        {
            skipPressed = false;
        }
    }
    public void activateSkip()
    {
        skipPressed = true;
        TradeText.SetActive(false);
        DialoguePanel.SetActive(false);
        Shop.SetActive(false);
    }

    public void CloseShop()
    {
        Shop.SetActive(false);
    }

    public void Honey()
    {
        Player.HoneyON();
    }
}


