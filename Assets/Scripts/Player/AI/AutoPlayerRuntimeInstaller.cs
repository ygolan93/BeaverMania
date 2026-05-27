using UnityEngine;

namespace Beavermania.Player.AI
{
    /// <summary>
    /// Adds missing AutoPlayer components at runtime. Attach to the player root prefab (single component).
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-100)]
    public class AutoPlayerRuntimeInstaller : MonoBehaviour
    {
        [SerializeField] bool installOnAwake = true;

        void Awake()
        {
            if (installOnAwake)
                Install();
        }

        [ContextMenu("Install AutoPlayer Components")]
        public void Install()
        {
            if (GetComponent<AutoPlayerBrain>() == null)
            {
                gameObject.AddComponent<AutoPlayerPerception>();
                gameObject.AddComponent<AutoPlayerMovementAgent>();
                gameObject.AddComponent<AutoPlayerActionAdapter>();
                gameObject.AddComponent<AutoPlayerTaskMemory>();
                gameObject.AddComponent<AutoPlayerDebugHUD>();
                gameObject.AddComponent<AutoPlayerBrain>();
            }
        }
    }
}
