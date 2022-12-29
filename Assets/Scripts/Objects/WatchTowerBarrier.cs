using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WatchTowerBarrier : MonoBehaviour
{
    public GameObject Barrier;
    private void OnTriggerStay(Collider OBJ)
    {
        if (OBJ.gameObject.CompareTag("Player"))
        {
            Destroy(Barrier);
        }
    }
}
