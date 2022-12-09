using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisappearOnAway : MonoBehaviour
{
    [SerializeField] GameObject Item;

    public Behavior Player;

    private void Start()
    {
        Player = GameObject.FindGameObjectWithTag("Player").GetComponent<Behavior>();
    }
    private void OnTriggerStay(Collider OBJ)
    {
        if (OBJ.gameObject == Player)
            Item.SetActive(true);
    }
    private void OnTriggerExit(Collider OBJ)
    {
        if (OBJ.gameObject == Player)
            Item.SetActive(false);
    }

}