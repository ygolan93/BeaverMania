using UnityEngine;

[System.Serializable]
public sealed class PlayerState
{
    [Header("Vitals")]
    public float maxHealth = 1000f;
    public float currentHealth;
    public float maxStamina = 100f;
    public float currentStamina;
    public int lives;

    [Header("Checkpoint")]
    public Vector3 checkpointPosition;

    [Header("Inventory")]
    public int currency;
    public int nutCount;
    public int apple;
    public int gobletPickup;
}
