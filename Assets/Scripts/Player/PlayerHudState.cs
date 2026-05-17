using UnityEngine;
using BeaverPlayer = Beavermania.Player.BeaverPlayerBehaviour;

namespace Beavermania.Player
{

    [DisallowMultipleComponent]
    public class PlayerHudState : MonoBehaviour
    {
        [Header("Objective")]
        public string ObjectiveText;
        [HideInInspector] public bool ObjectiveTextOverrideActive;
        [HideInInspector] public string ObjectiveTextOverride;

        [Header("Player HUD")]
        public string DebugText;
        public string StaminaText;
        public string LogCount;
        public string HealingText;
        public string Wallet;
        public string SeedText;
        public string GobletText;
        public string AppleText;
        public string ArrowText;

        /// <param name="hasObjective">When true, <see cref="ObjectiveText"/> is set to <paramref name="objectiveInstruction"/> (which may be null). When false, objective HUD text is unchanged.</param>
        public void CopyFrom(BeaverPlayer player, bool hasObjective, string objectiveInstruction)
        {
            if (hasObjective)
                ObjectiveText = objectiveInstruction;

            if (player == null)
                return;

            DebugText = player.DebugText;
            StaminaText = player.StaminaText;
            LogCount = player.LogCount;
            HealingText = player.HealingText;
            Wallet = player.Wallet;
            SeedText = player.SeedText;
            GobletText = player.GobletText;
            AppleText = player.AppleText;
            ArrowText = player.ArrowText;
        }
    }
}
