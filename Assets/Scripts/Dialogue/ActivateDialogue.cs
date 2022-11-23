using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActivateDialogue : MonoBehaviour
{
    public GameObject Dialogue;
    public Behavior Player;
    // Start is called before the first frame update
    void Start()
    {
        Player = GameObject.FindGameObjectWithTag("Player").GetComponent<Behavior>();
        Dialogue.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if ((transform.position - Player.transform.position).magnitude <= 5)
        {
            Dialogue.SetActive(true);
        }
        else
            Dialogue.SetActive(false);
    }
}
