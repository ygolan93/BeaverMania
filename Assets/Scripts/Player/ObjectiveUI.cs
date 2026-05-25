using UnityEngine;
using BeaverPlayer = Beavermania.Player.BeaverPlayerBehaviour;

namespace Beavermania.UI.Objectives
{

    public class ObjectiveUI : MonoBehaviour
    {
        public BeaverPlayer Player;
        public string[] Objective;
        public int i;
        public WayPoint currentPoint;
        public string Instruction;

        void Awake()
        {
            if (currentPoint == null)
                currentPoint = GetComponent<WayPoint>();
        }

        public void Update()
        {
            WayPoint wp = currentPoint != null ? currentPoint : GetComponent<WayPoint>();
            if (wp == null)
                return;
            i = wp.i;
            if (Objective == null || i < 0 || i >= Objective.Length)
                return;
            Player = transform.GetComponent<BeaverPlayer>();
            Instruction = Objective[i];
        }

        public void UpdateObjective()
        {
            i++;
        }

        //public void OnTriggerStay(Collider GameObjective) 
        //{
        //    if (GameObjective.CompareTag("Objective"))
        //    {
        //        i = GameObjective.GetComponent<ChangeOBJ>().ObjectiveNum;
        //    }
        //}
        //public void /*OnTriggerExit*/(Collider GameObjective)
        //{
        //    if (GameObjective.CompareTag("Objective"))
        //    {
        //        i = 0;
        //    }
        //}
    }
}
