using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grow : MonoBehaviour
{
    public GameObject GivingTree;
    float Clock;
    public bool AtRange;
    Vector3 scaleChange;
    [SerializeField] Transform Log1;
    [SerializeField] Transform Log2;

    // Start is called before the first frame update
    void Start()
    {
        Clock = 0.83f;
        scaleChange = new Vector3(0.01f, 0.014f, 0.01f);
    }


    // Update is called once per frame
    void Update()
    {
        gameObject.transform.localScale += scaleChange;
        transform.Rotate(Vector3.up * 20f, Space.World);
        Clock -= Time.deltaTime;
        if (Clock <= 0)
        {
            Instantiate(GivingTree, transform.position, Quaternion.identity);
            GameObject.Destroy(gameObject);
        }
    }

    private void OnTriggerStay(Collider OBJ)
    {
        if (OBJ.gameObject.tag == "Player")
        {
            if (Input.GetKey(KeyCode.Mouse0))
            {
                Instantiate(Log1, transform.position, Quaternion.identity);
                Instantiate(Log2, transform.position, Quaternion.identity);
                Destroy(gameObject);
            }
        }
    }
}
