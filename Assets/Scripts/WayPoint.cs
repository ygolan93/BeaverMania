using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WayPoint : MonoBehaviour
{
    public Image Mark;
    Transform target;
    public Transform[] Locations;
    public int i;


    // Update is called once per frame
    public void Start()
    {
        for (int index=0; index<Locations.Length; index++)
        {
            if (index!=i && index!=21 && index!=22)
            {
                Locations[index].gameObject.SetActive(false);
            }
        }
        Locations[i].gameObject.SetActive(true);
    }

    void Update()
    {
        target = Locations[i];
        if (target==null)
        {
            i++;
        }

        float minX = Mark.GetPixelAdjustedRect().width / 2;
        float maxX = Screen.width - minX;
        float minY = Mark.GetPixelAdjustedRect().width / 2;
        float maxY = Screen.width - minY;
        Vector2 Pos = Camera.main.WorldToScreenPoint(target.position+new Vector3(0,2,0));


        if (Vector3.Dot((target.position - transform.position), transform.forward) < 0)
        {
            //Target is behind player
            Mark.enabled = false;
        }
        else
        {
            Mark.enabled = true;
        }

        Pos.x = Mathf.Clamp(Pos.x, minX, maxX);
        Pos.y = Mathf.Clamp(Pos.y, minY, maxY);



        Mark.transform.position = Pos;
        //Mark.transform.position = target.position;
    }

    private void OnTriggerEnter(Collider OBJ)
    {
        if (OBJ.gameObject.CompareTag("WayPoint"))
        {
            OBJ.gameObject.SetActive(false);
            i++;
            Locations[i].gameObject.SetActive(true);
        }
    }

}
