using UnityEngine;

public struct DamageEvent
{
    public float Amount;
    public GameObject Source;
    public Vector3 Point;
    public DamageType Type;
    public bool CanStun;
}
