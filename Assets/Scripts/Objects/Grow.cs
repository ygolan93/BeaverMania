using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grow : MonoBehaviour
{
    public GameObject GivingTree;
    float Clock;
    Vector3 scaleChange;
    // Start is called before the first frame update
    void Start()
    {
        Clock = 12f;
        scaleChange = new Vector3(0.002f, 0.001f, 0.002f);
    }

    // Update is called once per frame
    void Update()
    {
        gameObject.transform.localScale += scaleChange;
        Clock -= Time.deltaTime;
        if (Clock <= 0)
        {
            Instantiate(GivingTree, transform.position, Quaternion.identity);
            GameObject.Destroy(gameObject);
        }
    }
}
