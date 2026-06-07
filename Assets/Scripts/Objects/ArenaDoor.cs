using Beavermania.NPC;
using UnityEngine;

namespace Beavermania.Objects
{
    public class ArenaDoor : MonoBehaviour
    {
        [SerializeField] ShadowRevenantController shadowRevenant;

        float initialHeight;
        public float Clock;
        public float LiftFactor;

        void Start()
        {
            initialHeight = transform.position.y;
        }

        void Update()
        {
            if (shadowRevenant == null)
                return;

            if (shadowRevenant.State != ShadowRevenantState.Dead)
                return;

            if (Clock > 0f)
            {
                Clock -= Time.deltaTime;
                transform.position += new Vector3(0f, LiftFactor, 0f);
            }
        }
    }
}
