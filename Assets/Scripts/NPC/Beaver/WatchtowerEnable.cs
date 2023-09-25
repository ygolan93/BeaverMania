using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WatchtowerEnable : MonoBehaviour
{
    [SerializeField] Trader Guard;
    private void Start()
    {
        Guard.enabled = false;
    }
    private void OnTriggerEnter(Collider OBJ)
    {
        if (OBJ.gameObject.CompareTag("Honey"))
        {
            Guard.enabled = true; 
            Destroy(OBJ.gameObject);
        }
    }
}
