using BeaverPlayer = Beavermania.Player.BeaverPlayerBehaviour;
using UnityEngine;

namespace Beavermania.NPC
{
    public class ScorpionDamage : MonoBehaviour
    {
        const float CounterDamage = 10f;
        const float ParryChipDamage = 6f;

        public float JawClamp;
        public float Sting;
        public BeaverPlayer Player;
        public NPC_Audio BossAudio;
        public ScorpionScript Scorpion;

        void Awake()
        {
            if (Player == null)
                Player = GetComponent<BeaverPlayer>();

            ResolveBossReferences();
        }

        public void OnTriggerEnter(Collider OBJ)
        {
            if (OBJ.gameObject.CompareTag("Arena"))
                ResolveBossReferences();

            if (OBJ.gameObject.CompareTag("ScorpionDamage"))
                ApplyHit(isStingHit: false);

            if (OBJ.gameObject.CompareTag("ScorpionSting"))
                ApplyHit(isStingHit: true);
        }

        void ApplyHit(bool isStingHit)
        {
            if (Player == null || Scorpion == null || Scorpion.combo >= Scorpion.comboLimit)
                return;

            if (!Player.isParried)
            {
                float damage = isStingHit ? ResolveStingDamage() : ResolveJawDamage();
                Player.TakeDamage(damage);
                BossAudio?.Sting();
                return;
            }

            Player.TakeDamage(ParryChipDamage);
            Scorpion.ReceiveDamage(Mathf.RoundToInt(CounterDamage), EnemyDamageType.Light, transform);
            Scorpion.combo++;
        }

        float ResolveJawDamage()
        {
            if (Scorpion != null)
                return Scorpion.AttackDamageAmount;

            return JawClamp;
        }

        float ResolveStingDamage()
        {
            if (Scorpion != null)
                return Scorpion.StingDamageAmount;

            return Sting;
        }

        void ResolveBossReferences()
        {
            if (Scorpion == null || !Scorpion.CompareTag("Boss"))
            {
                ScorpionScript[] scorpions = FindObjectsOfType<ScorpionScript>();
                for (int i = 0; i < scorpions.Length; i++)
                {
                    if (scorpions[i] != null && scorpions[i].CompareTag("Boss"))
                    {
                        Scorpion = scorpions[i];
                        break;
                    }
                }

                if (Scorpion == null && scorpions.Length > 0)
                    Scorpion = scorpions[0];
            }

            if (BossAudio == null && Scorpion != null)
                BossAudio = Scorpion.Sound;
        }
    }
}
