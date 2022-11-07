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
        Clock = 12f;
        scaleChange = new Vector3(0.002f, 0.001f, 0.002f);
    }

    // Update is called once per frame
    void Update()
    {
        if (AtRange == true)
        {
            if (Input.GetKey(KeyCode.Mouse0))
            {
                Instantiate(Log1, transform.position, Quaternion.identity);
                Instantiate(Log2, transform.position, Quaternion.identity);
                Destroy(gameObject);
            }
        }

    gameObject.transform.localScale += scaleChange;
        Clock -= Time.deltaTime;
        if (Clock <= 0)
        {
            Instantiate(GivingTree, transform.position, Quaternion.identity);
            GameObject.Destroy(gameObject);
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
        if (OBJ.gameObject.tag == "Player")
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
