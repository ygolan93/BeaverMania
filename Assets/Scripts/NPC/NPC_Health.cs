using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NPC_Health : MonoBehaviour
{
    public Slider NPCslider;

    public void SetMaxNPCHealth(int NPCH)
    {
        NPCslider.maxValue = NPCH;
        NPCslider.value = NPCH;
    }
    public void SetNPCHealth(int NPCH)
    {
        NPCslider.value = NPCH;
    }
}
