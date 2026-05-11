using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class Dialogue : MonoBehaviour
{
    public Behaviour Player;
    public ScorpionScript Scorpion;
    public ObjectiveUI PlayerObjective;
    public TextMeshProUGUI textComponent;
    public GameObject ContinueButton;
    public GameObject SkipButton;
    public Transform panel;
    public Trader Merchant;
    public string[] lines;
    public float textSpeed;
    private int index;
    public bool isBoss;
    // Start is called before the first frame update
    void Start()
    {
        var playerObject = GameObject.FindGameObjectWithTag("Player");
        if (!RuntimeReferenceValidator.Require(playerObject, this, "Player tag"))
        {
            return;
        }

        Player = playerObject.GetComponent<Behaviour>();
        //Boss = GameObject.FindGameObjectWithTag("Boss").GetComponent<BossScript>();
        PlayerObjective = playerObject.GetComponent<ObjectiveUI>();
        panel = transform.parent;

        bool valid = RuntimeReferenceValidator.Require(Player, this, nameof(Player)) &
            RuntimeReferenceValidator.Require(PlayerObjective, this, nameof(PlayerObjective)) &
            RuntimeReferenceValidator.Require(panel, this, nameof(panel)) &
            RuntimeReferenceValidator.Require(textComponent, this, nameof(textComponent)) &
            RuntimeReferenceValidator.Require(SkipButton, this, nameof(SkipButton));

        if (!isBoss)
        {
            valid &= RuntimeReferenceValidator.Require(Merchant, this, nameof(Merchant));
        }

        if (!valid)
        {
            return;
        }

        SkipButton.SetActive(true);
        textComponent.text = string.Empty;
        StartDialogue();
        index = 0;
    }

    void OnDisable()
    {
        if (Merchant != null)
        {
            Merchant.RestoreGameplayAfterDialogueCancelled();
        }
        else if (Player != null && Player.isAtTrader)
        {
            Player.ExitTraderInteraction();
        }
    }

    // Update is called once per frame
    public void Continue()
    {

        if (textComponent.text == lines[index])
        {
            NextLine();
        }
        else
        {
            StopAllCoroutines();
            textComponent.text = lines[index];
        }
    }

    void StartDialogue()
    {
        //index = 0;
        StartCoroutine(TypeLine());
    }
    IEnumerator TypeLine()
    {
        foreach (char c in lines[index].ToCharArray())
        {
            textComponent.text += c;
            yield return new WaitForSeconds(textSpeed);
        }
    }

    void NextLine()
    {
        if (index < lines.Length - 1)
        {
            index++;
            textComponent.text =/* string.Empty*/ lines[index];
            //StartCoroutine(TypeLine());
        }
        else
        {
            EndConversation();
        }
    }

    public void EndConversation()
    {
        PlayerObjective.UpdateObjective();
        if (isBoss==true)
        {
            EndBossDialogue();
        }
        else
        {
            Merchant.activateSkip();
        }
    }

    public void EndBossDialogue()
    {
        PlayerObjective.UpdateObjective();
        Player.GetComponent<BossHandler>().SkipBossChat();
        //Scorpion.InitiateCharge();
    }
}
