using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LogSpawner : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] Transform SpawnPoint;
    public Transform[] Prefab;
    public GameObject TreeDeath;
    //[SerializeField] Transform Log2;
    //[SerializeField] Transform Log3;
    //[SerializeField] Transform Log4;


    public void OnTriggerStay(Collider OBJ)
    {
        if (OBJ.gameObject.tag=="Player")
        {
           var isGrounded= OBJ.gameObject.GetComponent<Behaviour>().grounded;
            if (Input.GetKey(KeyCode.Mouse0)&&isGrounded==false)
            {
                DestroyTree(OBJ.transform);
            }
        }
    }
    public void OnCollisionStay(Collision OBJ)
    {
        if (OBJ.gameObject.tag == "Player")
        {
            if (Input.GetKey(KeyCode.Mouse0))
            {
                DestroyTree(OBJ.transform);
            }
        }
    }

    public void DestroyTree(Transform OBJ)
    {
        Instantiate(TreeDeath, new Vector3(SpawnPoint.position.x, OBJ.transform.position.y, SpawnPoint.position.z), Quaternion.identity);
        var counter = 0;
        foreach (var item in Prefab)
        {
            Instantiate(item, SpawnPoint.position+new Vector3(0,counter,0), Quaternion.Euler(0, 0, 90));
            counter++;
        }
        Destroy(gameObject);
    }
}
