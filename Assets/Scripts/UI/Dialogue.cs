using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class Dialogue : MonoBehaviour
{
    public Behavior Player;
    public ObjectiveUI PlayerObjective;
    public TextMeshProUGUI textComponent;
    public GameObject ContinueButton;
    public GameObject SkipButton;
    public string[] lines;
    public float textSpeed;

    private int index;

    // Start is called before the first frame update
    void Start()
    {
        Player = GameObject.FindGameObjectWithTag("Player").GetComponent<Behavior>();
        PlayerObjective = GameObject.FindGameObjectWithTag("Player").GetComponent<ObjectiveUI>();
        textComponent.text = string.Empty;
        StartDialogue();
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
        index = 0;
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
        //Destroy(ContinueButton);
        //Destroy(SkipButton);
    }
    public void EndBossConversation()
    {
        PlayerObjective.UpdateObjective();
        Destroy(ContinueButton);
        Destroy(SkipButton);
    }
}
