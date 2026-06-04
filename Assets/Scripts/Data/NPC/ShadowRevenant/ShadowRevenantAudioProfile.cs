using Beavermania.Audio;
using UnityEngine;

namespace Beavermania.Data.NPC
{
    [CreateAssetMenu(fileName = "ShadowRevenantAudioProfile", menuName = "Beavermania/NPC/Shadow Revenant Audio Profile")]
    public sealed class ShadowRevenantAudioProfile : ScriptableObject
    {
        [Header("Boss Lifecycle")]
        public SfxEventDefinition bossSpawn;
        public SfxEventDefinition bossAggro;
        public SfxEventDefinition phaseOut;
        public SfxEventDefinition phaseIn;
        public SfxEventDefinition bossHit;
        public SfxEventDefinition lightBreak;
        public SfxEventDefinition bossDeath;
        public SfxEventDefinition bossRemainsSettle;

        [Header("Projectile")]
        public SfxEventDefinition projectileWindup;
        public SfxEventDefinition projectileFire;
        public SfxEventDefinition projectileImpact;

        [Header("Fog")]
        public SfxEventDefinition fogTelegraph;
        public SfxEventDefinition fogActiveStart;
        public SfxEventDefinition fogDisappear;

        [Header("Summon")]
        public SfxEventDefinition summonWindup;
        public SfxEventDefinition summonComplete;

        [Header("Charge")]
        public SfxEventDefinition chargeWindup;
        public SfxEventDefinition chargeDash;
        public SfxEventDefinition chargeImpact;

        [Header("Minions")]
        public SfxEventDefinition shadeSpawn;
        public SfxEventDefinition shadeAttack;
        public SfxEventDefinition shadeHit;
        public SfxEventDefinition shadeDeath;
        public SfxEventDefinition shadeOrbitLoop;
        public SfxEventDefinition shadeApproachMove;

        [Header("Boss Movement")]
        public SfxEventDefinition bossStrafePulse;

        [Header("Optional Loop")]
        public AudioClip ambientLoopClip;
        [Range(0f, 1f)] public float ambientLoopVolume = 0.35f;
    }
}
