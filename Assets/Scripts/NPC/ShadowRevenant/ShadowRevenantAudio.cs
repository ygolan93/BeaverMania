using Beavermania.Audio;
using Beavermania.Data.NPC;
using UnityEngine;
using UnityEngine.Audio;

namespace Beavermania.NPC
{
    public enum ShadowRevenantAudioEvent
    {
        BossSpawn,
        BossAggro,
        PhaseOut,
        PhaseIn,
        ProjectileWindup,
        ProjectileFire,
        ProjectileImpact,
        FogTelegraph,
        FogActiveStart,
        FogDisappear,
        SummonWindup,
        SummonComplete,
        ChargeWindup,
        ChargeDash,
        ChargeImpact,
        BossHit,
        LightBreak,
        BossDeath,
        BossRemainsSettle,
        ShadeSpawn,
        ShadeAttack,
        ShadeHit,
        ShadeDeath,
        ShadeOrbitLoop,
        ShadeApproachMove,
        BossStrafePulse
    }

    public sealed class ShadowRevenantAudio : MonoBehaviour
    {
        [SerializeField] AudioSource actionSource;
        [SerializeField] AudioSource ambientLoopSource;
        [SerializeField] ShadowRevenantAudioProfile profile;

        bool ambientPlaying;

        public ShadowRevenantAudioProfile Profile => profile;

        public void SetProfile(ShadowRevenantAudioProfile audioProfile)
        {
            profile = audioProfile;
        }

        void Awake()
        {
            if (actionSource == null)
                actionSource = GetComponent<AudioSource>();

            if (ambientLoopSource == null)
            {
                Transform loopChild = transform.Find("AmbientLoopSource");
                if (loopChild != null)
                    ambientLoopSource = loopChild.GetComponent<AudioSource>();
            }

            if (actionSource != null && actionSource.outputAudioMixerGroup != null && AudioVolumeSettings.Instance == null)
            {
                AudioMixer mixer = actionSource.outputAudioMixerGroup.audioMixer;
                if (mixer != null)
                    AudioVolumeSettings.EnsureSfxVolumesFromPrefs(mixer);
            }
        }

        public void Play(ShadowRevenantAudioEvent audioEvent)
        {
            if (profile == null)
                return;

            SfxEventDefinition definition = ResolveDefinition(audioEvent);
            if (definition == null || actionSource == null)
                return;

            definition.TryPlay(actionSource, "shadowrevenant." + audioEvent);
        }

        public void PlayMinion(ShadowRevenantAudioEvent audioEvent, AudioSource minionSource)
        {
            if (profile == null || minionSource == null)
                return;

            SfxEventDefinition definition = ResolveDefinition(audioEvent);
            if (definition == null)
                return;

            definition.TryPlay(minionSource, "shadowrevenant.minion." + audioEvent);
        }

        public void StartAmbientLoop()
        {
            if (ambientPlaying || ambientLoopSource == null || profile == null || profile.ambientLoopClip == null)
                return;

            ambientLoopSource.clip = profile.ambientLoopClip;
            ambientLoopSource.volume = profile.ambientLoopVolume;
            ambientLoopSource.loop = true;
            ambientLoopSource.Play();
            ambientPlaying = true;
        }

        public void StopLoops()
        {
            if (ambientLoopSource != null && ambientLoopSource.isPlaying)
                ambientLoopSource.Stop();

            ambientPlaying = false;
        }

        SfxEventDefinition ResolveDefinition(ShadowRevenantAudioEvent audioEvent)
        {
            if (profile == null)
                return null;

            switch (audioEvent)
            {
                case ShadowRevenantAudioEvent.BossSpawn: return profile.bossSpawn;
                case ShadowRevenantAudioEvent.BossAggro: return profile.bossAggro;
                case ShadowRevenantAudioEvent.PhaseOut: return profile.phaseOut;
                case ShadowRevenantAudioEvent.PhaseIn: return profile.phaseIn;
                case ShadowRevenantAudioEvent.ProjectileWindup: return profile.projectileWindup;
                case ShadowRevenantAudioEvent.ProjectileFire: return profile.projectileFire;
                case ShadowRevenantAudioEvent.ProjectileImpact: return profile.projectileImpact;
                case ShadowRevenantAudioEvent.FogTelegraph: return profile.fogTelegraph;
                case ShadowRevenantAudioEvent.FogActiveStart: return profile.fogActiveStart;
                case ShadowRevenantAudioEvent.FogDisappear: return profile.fogDisappear;
                case ShadowRevenantAudioEvent.SummonWindup: return profile.summonWindup;
                case ShadowRevenantAudioEvent.SummonComplete: return profile.summonComplete;
                case ShadowRevenantAudioEvent.ChargeWindup: return profile.chargeWindup;
                case ShadowRevenantAudioEvent.ChargeDash: return profile.chargeDash;
                case ShadowRevenantAudioEvent.ChargeImpact: return profile.chargeImpact;
                case ShadowRevenantAudioEvent.BossHit: return profile.bossHit;
                case ShadowRevenantAudioEvent.LightBreak: return profile.lightBreak;
                case ShadowRevenantAudioEvent.BossDeath: return profile.bossDeath;
                case ShadowRevenantAudioEvent.BossRemainsSettle: return profile.bossRemainsSettle;
                case ShadowRevenantAudioEvent.ShadeSpawn: return profile.shadeSpawn;
                case ShadowRevenantAudioEvent.ShadeAttack: return profile.shadeAttack;
                case ShadowRevenantAudioEvent.ShadeHit: return profile.shadeHit;
                case ShadowRevenantAudioEvent.ShadeDeath: return profile.shadeDeath;
                case ShadowRevenantAudioEvent.ShadeOrbitLoop: return profile.shadeOrbitLoop;
                case ShadowRevenantAudioEvent.ShadeApproachMove: return profile.shadeApproachMove;
                case ShadowRevenantAudioEvent.BossStrafePulse: return profile.bossStrafePulse;
                default: return null;
            }
        }
    }
}
