using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorShut : MonoBehaviour
{
    public GameObject Wall;
    [SerializeField] Trader Guard;
    bool Open = false;
    float Clock=7f;


    private void Update()
    {
        if (Open==true)
        {
            if (Clock > 0)
            {
                Clock -= Time.deltaTime;
                Wall.transform.position += new Vector3(0, 0.5f, 0);
            }
        }
    }

    private void OnTriggerStay(Collider OBJ)
    {
        if (OBJ.gameObject.CompareTag("Gold"))
        {
            Guard.activateSkip();
            Guard.enabled = false;
            Open = true;
            Destroy(OBJ.gameObject);
        }
    }
}
