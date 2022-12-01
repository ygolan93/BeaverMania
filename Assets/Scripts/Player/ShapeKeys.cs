using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShapeKeys : MonoBehaviour
{
    
    [SerializeField] float Blink;
    bool Open;

    [SerializeField] float LookRight;
    [SerializeField] float LookLeft;
    private void Start()
    {
        GetComponent<SkinnedMeshRenderer>();

        Blink = 100f;
        LookRight = 0;
        LookLeft = 0;
        Open = true;
}
    // Update is called once per frame
    void Update()
    {
        Blinking();

        if (Blink >= 100)
        {
            Open = true;
        }
        if (Blink<=0)
        {
            Open = false;
        }

    }

    void Blinking()
    {
        if (Open==true)
        {
            GetComponent<SkinnedMeshRenderer>().SetBlendShapeWeight(0, Blink-=10);
        }
        if (Open==false)
        {
            GetComponent<SkinnedMeshRenderer>().SetBlendShapeWeight(0, Blink+=10);
        }

    }
}
