using UnityEngine;

namespace Beavermania.NPC
{
    public interface ILightBreakReactive
    {
        bool ApplyLightBreak(float duration, Transform source);
    }
}
