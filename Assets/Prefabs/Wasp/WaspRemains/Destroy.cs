using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Destroy : MonoBehaviour
{
    float Clock;
    // Start is called before the first frame update
    void Start()
    {
         Clock = 10f;
    }

    // Update is called once per frame
    void Update()
    {
        Clock -= Time.deltaTime;
        if (Clock <= 0)
            GameObject.Destroy(gameObject);
    }
}
