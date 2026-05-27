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
            EnsureComponent<AutoPlayerPerception>();
            EnsureComponent<AutoPlayerCameraLock>();
            EnsureComponent<AutoPlayerCombatStaminaGate>();
            EnsureComponent<AutoPlayerCombatRanged>();
            EnsureComponent<AutoPlayerCombatSelector>();
            EnsureComponent<AutoPlayerTerrainSense>();
            EnsureComponent<AutoPlayerNavigationPlanner>();
            EnsureComponent<AutoPlayerMovementAgent>();
            EnsureComponent<AutoPlayerActionAdapter>();
            EnsureComponent<AutoPlayerTaskMemory>();
            EnsureComponent<AutoPlayerShopAdapter>();
            EnsureComponent<AutoPlayerIdlePlanner>();
            EnsureComponent<AutoPlayerPlaystyleApplier>();
            EnsureComponent<AutoPlayerManualPlayRecorder>();
            EnsureComponent<AutoPlayerDebugHUD>();
            EnsureComponent<AutoPlayerBrain>();
        }

        void EnsureComponent<T>() where T : Component
        {
            if (GetComponent<T>() == null)
                gameObject.AddComponent<T>();
        }
    }
}
