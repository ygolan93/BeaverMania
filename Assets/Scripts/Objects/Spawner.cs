using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Beavermania.Objects
{

    public class Spawner : MonoBehaviour
    {
        public GameObject Constructor;
        // Start is called before the first frame update
        public void Spawn()
        {
            Instantiate(Constructor, transform.position, Quaternion.identity);
        }
    }
}
