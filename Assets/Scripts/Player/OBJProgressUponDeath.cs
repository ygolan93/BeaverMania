using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OBJProgressUponDeath : MonoBehaviour
{
    public GameObject OBJ;
    public ObjectiveUI Player;
    // Start is called before the first frame update
    void Start()
    {
        Player = GameObject.FindGameObjectWithTag("Player").GetComponent<ObjectiveUI>();
    }

    // Update is called once per frame
    void Update()
    {
        if (OBJ == null)
        {
            Player.i++;
            Destroy(gameObject);
        }
    }
}
