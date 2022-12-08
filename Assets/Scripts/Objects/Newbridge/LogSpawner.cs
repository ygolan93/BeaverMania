using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LogSpawner : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] Transform SpawnPoint;
    [SerializeField] Transform Log1;
    [SerializeField] Transform Log2;
    [SerializeField] Transform Log3;
    [SerializeField] Transform Log4;


    private void OnTriggerStay(Collider OBJ)
    {
        if (OBJ.gameObject.CompareTag("Player"))
        {
            if (Input.GetKey(KeyCode.Mouse0))
            {
                Instantiate(Log1, SpawnPoint.position, Quaternion.identity);
                Instantiate(Log2, SpawnPoint.position, Quaternion.identity);
                Instantiate(Log3, SpawnPoint.position, Quaternion.identity);
                Instantiate(Log4, SpawnPoint.position, Quaternion.identity);
                Destroy(gameObject);
            }
        }
    }
}
