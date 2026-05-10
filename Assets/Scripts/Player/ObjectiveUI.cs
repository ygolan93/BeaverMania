using UnityEngine;

public class ObjectiveUI: MonoBehaviour
{
    public Behaviour Player;
    public string[] Objective;
    public int i;
    public WayPoint currentPoint;
    public string Instruction;

    public void Update()
    {
        if (Player == null)
        {
            Player = GetComponent<Behaviour>();
        }

        if (Player == null || currentPoint == null || Objective == null || Objective.Length == 0)
        {
            return;
        }

        i = currentPoint.i;
        if (i < 0 || i >= Objective.Length)
        {
            return;
        }

        Instruction = Objective[i] ?? string.Empty;
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
    public void UpdateObjective()
    {
        if (Objective == null || Objective.Length == 0)
        {
            return;
        }

        i = Mathf.Min(i + 1, Objective.Length - 1);
    }

}
