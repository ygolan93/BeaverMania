using UnityEngine;

[DisallowMultipleComponent]
public class PlayerPlatformController : MonoBehaviour
{
    Behaviour owner;

    public void Initialize(Behaviour behaviour)
    {
        owner = behaviour;
    }

    void OnTriggerEnter(Collider OBJ)
    {
        if (IsTile(OBJ))
        {
            AttachToPlatform(OBJ.transform);
        }
    }

    void OnTriggerStay(Collider OBJ)
    {
        if (IsTile(OBJ))
        {
            AttachToPlatform(OBJ.transform);
        }

        if (IsStairs(OBJ))
        {
            owner?.SetStep(true);
        }
    }

    void OnTriggerExit(Collider OBJ)
    {
        if (IsStairs(OBJ))
        {
            owner?.SetStep(false);
        }

        if (IsTile(OBJ) && owner != null)
        {
            owner.grounded = false;
            owner.SetOnPlatform(false);
            owner.DetachFromPlatform();
        }
    }

    void OnDisable()
    {
        owner?.SetOnPlatform(false);
        owner?.SetStep(false);
        owner?.DetachFromPlatform();
    }

    void AttachToPlatform(Transform platform)
    {
        if (owner == null || owner.Player == null)
        {
            return;
        }

        owner.Player.transform.SetParent(platform, true);
        owner.SetOnPlatform(true);
    }

    static bool IsTile(Collider OBJ)
    {
        return OBJ != null && OBJ.gameObject.CompareTag("Tile");
    }

    static bool IsStairs(Collider OBJ)
    {
        return OBJ != null && OBJ.gameObject.CompareTag("stairs");
    }
}
