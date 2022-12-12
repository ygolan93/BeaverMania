using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisappearOnAway : MonoBehaviour
{
    [SerializeField] GameObject Item;

    public Behavior Player;
    public Vector3 DistanceVec;
    [SerializeField] float ActualDistance;
    [SerializeField] float PopUpDistance;
    private void Update()
    {
        DistanceVec = Player.transform.position - Item.transform.position;
        ActualDistance = Mathf.Abs(DistanceVec.magnitude);
        if (ActualDistance < PopUpDistance)
        {
            Item.SetActive(true);
        }
        else
            Item.SetActive(false);
    }

    //private void Start()
    //{
    //    Player = GameObject.FindGameObjectWithTag("Player").GetComponent<Behavior>();
    //}
    //private void OnTriggerStay(Collider OBJ)
    //{
    //    if (OBJ.gameObject == Player)
    //        Item.SetActive(true);
    //}
    //private void OnTriggerExit(Collider OBJ)
    //{
    //    if (OBJ.gameObject == Player)
    //        Item.SetActive(false);
    //}

}