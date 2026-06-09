using Beavermania.Audio;
using UnityEngine;
using UnityEngine.Audio;

namespace Beavermania.NPC
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class ShadowRevenantShadeAudio : MonoBehaviour
    {
        [SerializeField] AudioSource actionSource;

        SfxEventDefinition spawnSfx;
        SfxEventDefinition attackSfx;
        SfxEventDefinition hitSfx;
        SfxEventDefinition deathSfx;
        SfxEventDefinition orbitLoopSfx;
        SfxEventDefinition approachMoveSfx;
        float attackSfxCooldown = 1.1f;
        float orbitSfxInterval = 2.4f;
        float nextAttackSfxTime;
        float nextOrbitSfxTime;

        void Awake()
        {
            if (actionSource == null)
                actionSource = GetComponent<AudioSource>();

            AudioSourceRouting.EnsureRoute(actionSource, AudioSourceRoute.Enemy);

            if (actionSource != null && actionSource.outputAudioMixerGroup != null && AudioVolumeSettings.Instance == null)
            {
                AudioMixer mixer = actionSource.outputAudioMixerGroup.audioMixer;
                if (mixer != null)
                    AudioVolumeSettings.EnsureSfxVolumesFromPrefs(mixer);
            }
        }

        public void Configure(
            SfxEventDefinition spawn,
            SfxEventDefinition attack,
            SfxEventDefinition hit,
            SfxEventDefinition death,
            SfxEventDefinition orbitLoop,
            SfxEventDefinition approachMove,
            float attackCooldown,
            float orbitInterval)
        {
            spawnSfx = spawn;
            attackSfx = attack;
            hitSfx = hit;
            deathSfx = death;
            orbitLoopSfx = orbitLoop;
            approachMoveSfx = approachMove;
            attackSfxCooldown = Mathf.Max(0.05f, attackCooldown);
            orbitSfxInterval = Mathf.Max(0.25f, orbitInterval);
            nextAttackSfxTime = 0f;
            nextOrbitSfxTime = 0f;
        }

        public void PlaySpawn()
        {
            TryPlay(spawnSfx, "shadowrevenant.shade.spawn");
        }

        public void PlayAttack()
        {
            if (Time.time < nextAttackSfxTime)
                return;

            if (TryPlay(attackSfx, "shadowrevenant.shade.attack"))
                nextAttackSfxTime = Time.time + attackSfxCooldown;
        }

        public void PlayHit()
        {
            TryPlay(hitSfx, "shadowrevenant.shade.hit");
        }

        public void PlayDeath()
        {
            TryPlay(deathSfx, "shadowrevenant.shade.death");
        }

        public void PlayOrbitLoop()
        {
            if (Time.time < nextOrbitSfxTime)
                return;

            if (TryPlay(orbitLoopSfx, "shadowrevenant.shade.orbit"))
                nextOrbitSfxTime = Time.time + orbitSfxInterval;
        }

        public void PlayApproachMove()
        {
            TryPlay(approachMoveSfx, "shadowrevenant.shade.approach");
        }

        public void StopAndReset()
        {
            if (actionSource != null && actionSource.isPlaying)
                actionSource.Stop();

            nextAttackSfxTime = 0f;
            nextOrbitSfxTime = 0f;
        }

        bool TryPlay(SfxEventDefinition definition, string channel)
        {
            if (definition == null || !AudioSourceRouting.EnsureRoute(actionSource, AudioSourceRoute.Enemy))
                return false;

            return definition.TryPlay(actionSource, channel);
        }
    }
}
