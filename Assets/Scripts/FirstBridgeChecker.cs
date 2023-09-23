using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirstBridgeChecker : MonoBehaviour
{
    public NewConstructor Bridge;
    public ObjectiveUI Player;
    WayPoint wp;
    Carry playerLoad;
    // Start is called before the first frame update
    void Start()
    {
        Player = GameObject.FindGameObjectWithTag("Player").GetComponent<ObjectiveUI>();
        wp = Player.GetComponent<WayPoint>();
        playerLoad = Player.GetComponent<Carry>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Bridge.isLocked == true)
        {
            if (playerLoad.i==0)
            {
                Player.i = 3;
                wp.i = 3;
                wp.Locations[2].gameObject.SetActive(false);
                wp.Locations[3].gameObject.SetActive(true);
            }
            if (playerLoad.i>0)
            {
                Player.i = 4;
                wp.i = 4;
                wp.Locations[3].gameObject.SetActive(false);
                wp.Locations[4].gameObject.SetActive(true);
                Destroy(gameObject);
            }
        }
        //if (Bridge.transform==null)
        //{
        //    Player.i = 1;
        //    wp.i = 1;
        //}
    }
}
