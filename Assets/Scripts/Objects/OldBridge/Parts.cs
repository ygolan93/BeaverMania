using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Parts : MonoBehaviour
{
     [SerializeField] Transform Backwards;
    // Start is called before the first frame update
    public void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Part")
        {
            //Instantiate(transform, transform.position + transform.forward * 0.5f, transform.rotation);
             collision.transform.position = Backwards.position + new Vector3 (0,1,0);
        }
    }
}