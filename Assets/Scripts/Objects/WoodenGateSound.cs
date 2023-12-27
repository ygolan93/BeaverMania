using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WoodenGateSound : MonoBehaviour
{
    [SerializeField] AudioSource Gate;

    private void OnCollisionEnter(Collision OBJ)
    { 
      if (OBJ.gameObject.CompareTag("Player"))
        {
            Gate.Play();
        }
    }
}
