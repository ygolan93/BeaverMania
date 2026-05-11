using UnityEngine;

[DisallowMultipleComponent]
public class PlayerCheckpointHealController : MonoBehaviour
{
    [SerializeField] float checkpointMessageSeconds = 3f;
    [SerializeField] float checkpointSaveInterval = 0.5f;
    [SerializeField] float healPerTick = 2f;

    Behaviour owner;
    Transform activeLife;
    float nextCheckpointSaveTime;

    public void Initialize(Behaviour behaviour)
    {
        owner = behaviour;
    }

    public void HandleTriggerEnter(Collider other)
    {
        if (!IsLife(other))
        {
            return;
        }

        activeLife = other.transform;
        owner.SetCheckpointMessageUntil(Time.time + checkpointMessageSeconds);
        SaveCheckpoint(activeLife.position, true);
    }

    public void HandleTriggerStay(Collider other)
    {
        if (!IsLife(other))
        {
            return;
        }

        activeLife = other.transform;
        SaveCheckpoint(activeLife.position, false);
        Heal();
    }

    public void HandleTriggerExit(Collider other)
    {
        if (!IsLife(other))
        {
            return;
        }

        HandleLifeExit(other.transform);
    }

    public void HandleCollisionExit(Collision collision)
    {
        if (collision == null || !collision.gameObject.CompareTag("Life"))
        {
            return;
        }

        HandleLifeExit(collision.gameObject.transform);
    }

    void HandleLifeExit(Transform life)
    {
        if (activeLife == null || activeLife == life)
        {
            ClearTouchShroom();
        }
    }

    void OnDisable()
    {
        ClearTouchShroom();
    }

    void SaveCheckpoint(Vector3 position, bool force)
    {
        if (!force && Time.time < nextCheckpointSaveTime)
        {
            return;
        }

        nextCheckpointSaveTime = Time.time + checkpointSaveInterval;
        owner.SavePlayerCheckpoint(position);
    }

    void Heal()
    {
        owner.Plattering = "Shroom!";
        owner.ChangeSpeech = 1f;

        if (owner.CurrentHealth >= owner.MaxHealth)
        {
            owner.SetTouchShroom(false);
            return;
        }

        owner.SetTouchShroom(true);
        owner.TakeDamage(-healPerTick);

        if (owner.CurrentHealth >= owner.MaxHealth)
        {
            owner.SetTouchShroom(false);
        }
    }

    public void ClearTouchShroom()
    {
        activeLife = null;
        nextCheckpointSaveTime = 0f;
        owner?.SetTouchShroom(false);
    }

    static bool IsLife(Collider other)
    {
        return other != null && other.gameObject.CompareTag("Life");
    }
}
