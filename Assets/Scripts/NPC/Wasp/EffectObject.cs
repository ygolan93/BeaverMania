using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectObject : MonoBehaviour
{
    // Start is called before the first frame update

    public float time = 2f;
    private void Start()
    {

        Destroy(gameObject, time);
    }
}
