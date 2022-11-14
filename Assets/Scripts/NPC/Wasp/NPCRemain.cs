using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCRemain : MonoBehaviour
{
    public GameObject GrowingTree;
    float Clock;
    public bool grounded;
    // Start is called before the first frame update
    void Start()
    {
        Clock = 5f;
    }

    // Update is called once per frame
    void Update()
    {
        {
            Clock -= Time.deltaTime;
            if (Clock <= 0 && grounded == true)
            {
                Instantiate(GrowingTree, transform.position+new Vector3(0,-0.4f,0), Quaternion.identity);
            }
        }
    }
    private void OnCollisionStay(Collision OBJ)
    {
       if (OBJ.gameObject.CompareTag("Isle"))
        {
            grounded = true;
        }
    }
    private void OnCollisionExit(Collision OBJ)
    {
        if (OBJ.gameObject.CompareTag("Isle"))
        {
            grounded = false;
        }
    }
}
