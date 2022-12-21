using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class Dialogue : MonoBehaviour
{
    public Behavior Player;
    public TextMeshProUGUI textComponent;
    public string[] lines;
    public float textSpeed;

    private int index;

    // Start is called before the first frame update
    void Start()
    {
        Player = GameObject.FindGameObjectWithTag("Player").GetComponent<Behavior>();
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
    public void Buy()
    {
        if (Player.Currency > 0)
        {

            if (Player.Currency >= 3)
            {
                Player.NutCount++;
                Player.Currency -= 3;
            }
        }
    }

    public void Sell()
    {
        if (Player.NutCount > 0)
        {
            if (Player.NutCount > 0)
            {
                Player.NutCount--;
                Player.Currency += 3;
            }
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
            textComponent.text = string.Empty;
            StartCoroutine(TypeLine());
        }
        else
        {
            index = 0;
            //gameObject.SetActive(false);
        }
    }
}
