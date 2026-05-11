using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerEnemyContactController : MonoBehaviour
{
    readonly Dictionary<Collider, ScorpionScript> activeScorpionContacts = new Dictionary<Collider, ScorpionScript>();
    Behaviour owner;

    public void Initialize(Behaviour behaviour)
    {
        owner = behaviour;
    }

    public void HandleTriggerStay(Collider enemyCollider)
    {
        if (!IsScorpion(enemyCollider))
        {
            return;
        }

        if (!activeScorpionContacts.ContainsKey(enemyCollider))
        {
            activeScorpionContacts.Add(enemyCollider, enemyCollider.GetComponent<ScorpionScript>());
        }

        UpdateScorpionContactState();
    }

    public void HandleTriggerExit(Collider enemyCollider)
    {
        if (!IsScorpion(enemyCollider))
        {
            return;
        }

        activeScorpionContacts.Remove(enemyCollider);
        UpdateScorpionContactState();
    }

    public void ClearScorpionContacts()
    {
        activeScorpionContacts.Clear();
        owner?.SetScorpionContactAttacking(false);
    }

    void OnDisable()
    {
        ClearScorpionContacts();
    }

    void UpdateScorpionContactState()
    {
        var attacking = false;
        foreach (var contact in activeScorpionContacts)
        {
            if (contact.Value != null && contact.Value.isAttacking)
            {
                attacking = true;
                break;
            }
        }

        owner?.SetScorpionContactAttacking(attacking);
    }

    static bool IsScorpion(Collider enemyCollider)
    {
        return enemyCollider != null && enemyCollider.gameObject.CompareTag("Scorpion");
    }
}
