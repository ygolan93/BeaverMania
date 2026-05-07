using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        var bossObject = GameObject.FindGameObjectWithTag("Boss");
        BossAudio = bossObject != null ? bossObject.GetComponent<NPC_Audio>() : null;
        Scorpion = bossObject != null ? bossObject.GetComponent<ScorpionScript>() : null;
        Player = transform.GetComponent<Behaviour>();

        if (!ValidateReferences())
        {
            return;
        }
    }

    bool ValidateReferences()
    {
        return RuntimeReferenceValidator.Require(Player, this, nameof(Player)) &
            RuntimeReferenceValidator.Require(BossAudio, this, nameof(BossAudio)) &
            RuntimeReferenceValidator.Require(Scorpion, this, nameof(Scorpion));
    }
    float ParryChipDamage => damageConfig != null ? damageConfig.scorpionParryChipDamage : 6f;
    int ParryCounterDamage => damageConfig != null ? damageConfig.scorpionParryCounterDamage : 10;

    void ApplyConfig()
    {
        if (damageConfig == null)
        {
            BuildSafeLogger.WarnOnce(nameof(ScorpionDamage) + ".MissingConfig", "Missing hazard damage config; using prefab values.", this, nameof(damageConfig));
            return;
        }

        JawClamp = damageConfig.scorpionJawClampDamage;
        Sting = damageConfig.scorpionStingDamage;
    }

    public void OnTriggerEnter(Collider OBJ)
    {
        if (OBJ.gameObject.CompareTag("Arena"))
        {
            Scorpion = GameObject.FindGameObjectWithTag("Boss").GetComponent<ScorpionScript>();
        }
        if (OBJ.gameObject.CompareTag("ScorpionDamage"))
        {
            if (Scorpion.combo < Scorpion.comboLimit)
            {
                if (Player.isParried == false)
                {
                    if (CombatDebugGate.AllowsDamage(Player.gameObject))
                    {
                        Player.TakeDamage(JawClamp);
                    }
                    BossAudio.Sting();
                }
                if (Player.isParried == true)
                {
                    Player.TakeDamage(ParryChipDamage);
                    Scorpion.TakeDamage(ParryCounterDamage);
                    Scorpion.combo++;
                }
            }

        }
        if (OBJ.gameObject.CompareTag("ScorpionSting"))
        {
            if (Scorpion.combo < Scorpion.comboLimit)
            {
                if (Player.isParried == false)
                {
                    if (CombatDebugGate.AllowsDamage(Player.gameObject))
                    {
                        Player.TakeDamage(Sting);
                    }
                    BossAudio.Sting();
                }
                if (Player.isParried == true)
                {
                    Player.TakeDamage(ParryChipDamage);
                    Scorpion.TakeDamage(ParryCounterDamage);
                    Scorpion.combo++;
                }
            }
        }
    }
}
