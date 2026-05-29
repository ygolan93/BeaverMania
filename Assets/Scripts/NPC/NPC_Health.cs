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
            if (NPCslider == null)
                return;

            NPCslider.maxValue = NPCH;
            NPCslider.value = NPCH;
        }
        public void SetNPCHealth(int NPCH)
        {
            if (NPCslider == null)
                return;

            NPCslider.value = NPCH;
        }
    }
}
