using System;
using UnityEngine;

namespace Beavermania.Core.GameFlow
{
    /// <summary>
    /// Single owner for gameplay <see cref="Time.timeScale"/>: multiple UI layers (pause, lose screen)
    /// can request a freeze independently without clobbering each other.
    /// </summary>
    public static class GameTimeScaleGate
    {
        [Flags]
        public enum FreezeToken
        {
            None = 0,
            PauseMenu = 1 << 0,
            LooseScreen = 1 << 1,
        }

        const float FrozenScale = 0f;
        const float RunningScale = 1f;

        static FreezeToken s_activeTokens;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStaticsForDomainReload()
        {
            s_activeTokens = FreezeToken.None;
            Time.timeScale = RunningScale;
        }

        public static bool IsFrozen => s_activeTokens != FreezeToken.None;

        /// <summary>
        /// Sets or clears a freeze token and reapplies the effective time scale.
        /// Idempotent for repeated identical calls.
        /// </summary>
        public static void SetFreeze(FreezeToken token, bool frozen)
        {
            if (token == FreezeToken.None)
                throw new ArgumentOutOfRangeException(nameof(token), "Use a non-None token.");

            if (frozen)
                s_activeTokens |= token;
            else
                s_activeTokens &= ~token;

            ApplyEffectiveScale();
        }

        /// <summary>
        /// Clears every token (e.g. before scene load). Always restores running scale.
        /// </summary>
        public static void ClearAll()
        {
            s_activeTokens = FreezeToken.None;
            Time.timeScale = RunningScale;
        }

        static void ApplyEffectiveScale()
        {
            Time.timeScale = IsFrozen ? FrozenScale : RunningScale;
        }
    }
}
