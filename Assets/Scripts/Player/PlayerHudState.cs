using UnityEngine;

[DisallowMultipleComponent]
public class PlayerHudState : MonoBehaviour
{
    [Header("Objective")]
    public string ObjectiveText;

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

    public void CopyFrom(Behaviour player, ObjectiveUI objective)
    {
        if (objective != null)
            ObjectiveText = objective.Instruction;

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
