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

        public void SetObjectiveText(string objectiveText)
        {
            ObjectiveText = objectiveText ?? string.Empty;
        }

        public void CopyPlayerStatsFrom(BeaverPlayer player)
        {
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

        public void CopyFrom(BeaverPlayer player, bool hasObjective, string objectiveInstruction)
        {
            if (hasObjective)
                SetObjectiveText(objectiveInstruction);

            CopyPlayerStatsFrom(player);
        }
    }
}
