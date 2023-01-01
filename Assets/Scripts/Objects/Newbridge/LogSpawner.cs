using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LogSpawner : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] Transform SpawnPoint;
    public Transform[] Prefab;
    //[SerializeField] Transform Log2;
    //[SerializeField] Transform Log3;
    //[SerializeField] Transform Log4;


    public void OnTriggerStay(Collider OBJ)
    {
        if (OBJ.gameObject.tag=="Player")
        {
            if (Input.GetKey(KeyCode.Mouse0))
            {
                DestroyTree();
            }
        }
    }

    public void DestroyTree()
    {
        foreach (var item in Prefab)
        {
            Instantiate(item, SpawnPoint.position, Quaternion.identity);
        }
        Destroy(gameObject);
    }
}
