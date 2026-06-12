using Beavermania.Data.Dialogue;
using UnityEngine;

namespace Beavermania.UI
{
    public sealed class NpcDialogueSessionContext
    {
        public NpcDialogueSessionContext(
            TraderDialogueData dialogueData,
            string[] fallbackLines,
            float fallbackTextSpeed,
            bool fallbackIsBoss,
            bool fallbackAdvanceObjectiveOnEnd,
            GameObject shopContentRoot)
        {
            DialogueData = dialogueData;
            FallbackLines = fallbackLines;
            FallbackTextSpeed = fallbackTextSpeed;
            FallbackIsBoss = fallbackIsBoss;
            FallbackAdvanceObjectiveOnEnd = fallbackAdvanceObjectiveOnEnd;
            ShopContentRoot = shopContentRoot;
        }

        public TraderDialogueData DialogueData { get; }

        public string[] FallbackLines { get; }

        public float FallbackTextSpeed { get; }

        public bool FallbackIsBoss { get; }

        public bool FallbackAdvanceObjectiveOnEnd { get; }

        public GameObject ShopContentRoot { get; }

        public bool HasShop => ShopContentRoot != null && (DialogueData == null || DialogueData.hasShop);

        public string[] ResolveLines()
        {
            if (DialogueData != null && DialogueData.dialogueLines != null && DialogueData.dialogueLines.Length > 0)
                return DialogueData.dialogueLines;

            return FallbackLines;
        }

        public float ResolveTextSpeed()
        {
            return DialogueData != null ? DialogueData.textSpeed : FallbackTextSpeed;
        }

        public bool ResolveIsBoss()
        {
            return DialogueData != null ? DialogueData.isBossDialogue : FallbackIsBoss;
        }

        public bool ResolveAdvanceObjectiveOnEnd()
        {
            return DialogueData != null ? DialogueData.advanceObjectiveOnEnd : FallbackAdvanceObjectiveOnEnd;
        }

        public bool HasAnyLines()
        {
            var lines = ResolveLines();
            return lines != null && lines.Length > 0;
        }
    }
}
