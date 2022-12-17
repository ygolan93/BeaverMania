using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorShut : MonoBehaviour
{
    public GameObject Shutters;
    public Behavior Player;
    [SerializeField] int GobletNum;
    private void Start()
    {
        Player = GameObject.FindGameObjectWithTag("Player").GetComponent<Behavior>();
    }
    private void Update()
    {
        if (Player.GobletPickup == GobletNum)
        {
            Destroy(Shutters);
        }
    }

}
