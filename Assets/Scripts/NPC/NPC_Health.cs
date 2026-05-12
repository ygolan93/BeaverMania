using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Beavermania.NPC
{

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
}
