using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ShowGrowth : MonoBehaviour
{
    public SeedPlant Seed;
    public TextMeshProUGUI NutDisplay;

    // Update is called once per frame
    void Update()
    {
        NutDisplay.text = Seed.GrowDisplay;
    }
}
