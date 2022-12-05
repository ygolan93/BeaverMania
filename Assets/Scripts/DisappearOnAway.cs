using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisappearOnAway : MonoBehaviour
{
    [SerializeField] GameObject Item;

    public Behavior Player;
    public float DistanceFromPlayer;
    [Header("Desired Distance")]
    [SerializeField] float ActiveDistance;

    private void Start()
    {
        Player = GameObject.FindGameObjectWithTag("Player").GetComponent<Behavior>();
    }
    // Update is called once per frame
    void FixedUpdate()
    {
        DistanceFromPlayer = Mathf.Abs((Item.transform.position - Player.transform.position).magnitude);
        if (DistanceFromPlayer <= ActiveDistance)
        {
            Item.SetActive(true);
        }
        else
        {
            Item.SetActive(false);
        }
    }
}
