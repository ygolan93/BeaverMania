using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class Dialogue : MonoBehaviour
{
    public Behaviour Player;
    public ObjectiveUI PlayerObjective;
    public TextMeshProUGUI textComponent;
    public GameObject ContinueButton;
    public GameObject SkipButton;
    public Transform panel;
    public string[] lines;
    public float textSpeed;
    private int index;

    // Start is called before the first frame update
    void Start()
    {
        Player = GameObject.FindGameObjectWithTag("Player").GetComponent<Behaviour>();
        PlayerObjective = GameObject.FindGameObjectWithTag("Player").GetComponent<ObjectiveUI>();
        panel = gameObject.transform.parent.GetComponent<Transform>();
        textComponent.text = string.Empty;
        StartDialogue();
        index = 0;
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
        Player.isAtTrader = false;
        panel.gameObject.SetActive(false);
        //Destroy(ContinueButton);
        //Destroy(SkipButton);
    }
}
