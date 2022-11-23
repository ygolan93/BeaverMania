using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActivateDialogue : MonoBehaviour
{
    public GameObject Dialogue;
    public GameObject Target;
    public GameObject Player;
    public float Distance;
    // Start is called before the first frame update
    void Start()
    {
        Dialogue.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        Distance = (Player.transform.position - Target.transform.position).magnitude;
        if ( Distance<= 5&& Target.transform.position!=null)
        {
            Dialogue.SetActive(true);
        }
        else
            Dialogue.SetActive(false);
    }
}
