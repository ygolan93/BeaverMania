using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WatchtowerEnable : MonoBehaviour
{
    [SerializeField] Trader Guard;
    [SerializeField] GameObject Hammers;
    private void Start()
    {
        Guard.enabled = false;
    }
    private void OnTriggerEnter(Collider OBJ)
    {
        if (OBJ.gameObject.CompareTag("Honey"))
        {
            Guard.enabled = true;
            Instantiate(Hammers, OBJ.gameObject.transform.position+new Vector3(0,1,0), Quaternion.identity);
            Destroy(OBJ.gameObject);
        }
    }
}
