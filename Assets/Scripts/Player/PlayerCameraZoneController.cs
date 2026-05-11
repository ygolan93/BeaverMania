using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerCameraZoneController : MonoBehaviour
{
    readonly HashSet<Collider> activeHouseZones = new HashSet<Collider>();
    Behaviour owner;

    public bool IsInsideHouseCameraZone => activeHouseZones.Count > 0;

    public void Initialize(Behaviour behaviour)
    {
        owner = behaviour;
    }

    public void HandleTriggerStay(Collider zoneCollider)
    {
        if (!IsHouseCameraZone(zoneCollider))
        {
            return;
        }

        if (activeHouseZones.Add(zoneCollider) && activeHouseZones.Count == 1)
        {
            owner?.ApplyHouseCameraOrbits();
        }
    }

    public void HandleTriggerExit(Collider zoneCollider)
    {
        if (!activeHouseZones.Remove(zoneCollider) || activeHouseZones.Count > 0)
        {
            return;
        }

        owner?.RestoreDefaultCameraOrbits();
    }

    public void RestoreDefaultCameraOrbits()
    {
        if (!IsInsideHouseCameraZone)
        {
            return;
        }

        activeHouseZones.Clear();
        owner?.RestoreDefaultCameraOrbits();
    }

    void OnDisable()
    {
        RestoreDefaultCameraOrbits();
    }

    static bool IsHouseCameraZone(Collider zoneCollider)
    {
        return zoneCollider != null && zoneCollider.gameObject.CompareTag("House");
    }
}
