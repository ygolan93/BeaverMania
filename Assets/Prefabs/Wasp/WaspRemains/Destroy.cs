using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Destroy : MonoBehaviour
{
    float Clock;
    public GameObject effect;
    // Start is called before the first frame update
    void Start()
    {
         Clock = 5f;
        Physics.IgnoreLayerCollision(0, 7);
    }

    // Update is called once per frame
    void Update()
    {
        Clock -= Time.deltaTime;
        if (Clock <= 0)
        {
            Instantiate(effect, gameObject.transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
    }
}
