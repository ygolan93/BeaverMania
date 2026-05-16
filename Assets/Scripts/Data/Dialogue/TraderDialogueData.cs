using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Beavermania.Data.Dialogue
{
    [CreateAssetMenu(fileName = "TraderDialogueData", menuName = "Beavermania/Dialogue/Trader Dialogue Data")]
    public class TraderDialogueData : ScriptableObject
    {
        public string traderId = "";
        public string displayName = "";
        public string[] dialogueLines;
        public bool isBossDialogue;
        public float textSpeed;
        public bool advanceObjectiveOnEnd = true;
        public bool hasShop = true;

        void OnValidate()
        {
#if UNITY_EDITOR
            if (EditorApplication.isUpdating || BuildPipeline.isBuildingPlayer)
                return;
#endif
            if (dialogueLines == null || dialogueLines.Length == 0)
                Debug.LogWarning($"{name}: dialogueLines is empty.", this);

            if (textSpeed < 0f)
                Debug.LogWarning($"{name}: textSpeed must be >= 0.", this);
        }
    }
}
