using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WayPoint : MonoBehaviour
{
    public Image Mark;
    public Transform target;
    public Transform Position1;
    public Transform Position2;
    public Transform Position3;
    public Transform Position4;
    public Transform Position5;
    public int i;
    // Update is called once per frame
    private void Start()
    {
        i = 0;
    }
    void Update()
    {
        switch (i)
        {
            case 0:
                target = Position1;
                break;

            case 1:
                target = Position2;
                break;

            case 2:
                target = Position3;
                break;

            case 3:
                target = Position4;
                break;

            case 4:
                target = Position5;
                break;

        }

        float minX = Mark.GetPixelAdjustedRect().width / 2;
        float maxX = Screen.width - minX;
        float minY = Mark.GetPixelAdjustedRect().width / 2;
        float maxY = Screen.width - minY;
        Vector2 Pos = Camera.main.WorldToScreenPoint(target.position+new Vector3(0,3,0));
        

        if (Vector3.Dot((target.position - transform.position), transform.forward) < 0)
        {
            //Target is behind the player
            if (Pos.x < Screen.width / 2)
                Pos.x = maxX;
            else
                Pos.x = minX;
        }

        Pos.x = Mathf.Clamp(Pos.x, minX, maxX);
        Pos.y = Mathf.Clamp(Pos.y, minY, maxY);



        Mark.transform.position = Pos;
        //Mark.transform.position = target.position;
    }

    private void OnCollisionEnter(Collision OBJ)
    {
        if (OBJ.gameObject.CompareTag("WayPoint"))
        {
            i++;
            Destroy(OBJ.gameObject);
        }
    }

}
