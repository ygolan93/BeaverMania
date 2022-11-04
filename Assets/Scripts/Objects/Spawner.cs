using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    public GameObject Constructor;
    // Start is called before the first frame update
    public void Spawn()
    {
        Instantiate(Constructor, transform.position, Quaternion.identity);
    }
}
