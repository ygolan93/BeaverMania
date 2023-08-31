using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArenaDoor : MonoBehaviour
{
    public BossScript Boss;
    float InitialHeight;
    public float Clock;
    public float LiftFactor;
    // Start is called before the first frame update
    void Start()
    {
        //Boss = GameObject.FindGameObjectWithTag("Boss").GetComponent<BossScript>();
        InitialHeight = transform.position.y;
    }

    // Update is called once per frame
    void Update()
    {
        if (Boss == null)
        {
            if (Clock > 0)
            {
                Clock -= Time.deltaTime;
                gameObject.transform.position += new Vector3(0, LiftFactor, 0);
            }
        }
    }
}
