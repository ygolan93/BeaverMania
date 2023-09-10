using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Trader : MonoBehaviour
{
    public GameObject Merchant;
    public Transform PlayerRoot;
    public Behavior Player;
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
        Player = GameObject.FindGameObjectWithTag("Player").GetComponent<Behavior>();
        PlayerRoot = GameObject.FindGameObjectWithTag("PlayerRoot").GetComponent<Transform>();
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
                Player.rotGoal = Quaternion.LookRotation(Player.transform.position - Merchant.transform.position);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(PlayerDistance), 0.1f);
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
    }

    public void Honey()
    {
        Player.HoneyON();
    }
}


