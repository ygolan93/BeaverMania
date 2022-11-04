using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BridgeConstructor : MonoBehaviour
{
    public GameObject[] BridgeParts;

    public int i = 0;
    public void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.tag == "Part")
        {
            if (i < BridgeParts.Length)
                BridgeParts[i].SetActive(true);
            i++;
            
            Destroy(collision.transform.gameObject);
        }
    }
}
