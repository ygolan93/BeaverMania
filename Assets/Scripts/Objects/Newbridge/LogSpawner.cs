using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LogSpawner : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject Logger;
    public bool AtRange;
    [SerializeField] Transform SpawnPoint;
    [SerializeField] Transform Log1;
    [SerializeField] Transform Log2;
    [SerializeField] Transform Log3;
    [SerializeField] Transform Log4;


    void Start()
    {
        Logger = GetComponent<GameObject>();
    }
    void Update()
    {
        if (AtRange==true)
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
    private void OnCollisionEnter(Collision OBJ)
    {
        if (OBJ.gameObject.tag == "Player")
        {
            AtRange = true;
        }
    }
    private void OnCollisionStay(Collision OBJ)
    {
        if (OBJ.gameObject.tag=="Player")
        {
            AtRange = true;
        }
    }

    private void OnCollisionExit(Collision OBJ)
    {
        if (OBJ.gameObject.tag == "Player")
        {
                AtRange = false;
        }
    }

}
