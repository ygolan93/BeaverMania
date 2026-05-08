using UnityEngine;

[System.Obsolete("Use BossHitbox on scorpion hitbox colliders. Kept only as a migration shim.")]
public class ScorpionDamage : MonoBehaviour
{
    [SerializeField] HazardDamageConfig damageConfig;
    public float JawClamp;
    public float Sting;
    public Behaviour Player;
    public NPC_Audio BossAudio;
    public ScorpionScript Scorpion;

    private void Awake()
    {
        ApplyConfig();

        if (Scorpion == null)
        {
            var bossObject = GameObject.FindGameObjectWithTag("Boss");
            Scorpion = bossObject != null ? bossObject.GetComponent<ScorpionScript>() : null;
        }
    }

    void ApplyConfig()
    {
        if (damageConfig == null)
        {
            return;
        }

        JawClamp = damageConfig.scorpionJawClampDamage;
        Sting = damageConfig.scorpionStingDamage;
    }

    [System.Obsolete("Use BossHitbox on scorpion hitbox colliders.")]
    public void OnTriggerEnter(Collider OBJ)
    {
        var hitbox = OBJ.GetComponent<BossHitbox>();
        if (hitbox == null)
        {
            return;
        }

        if (hitbox.owner == null)
        {
            hitbox.owner = Scorpion;
        }
    }
}
