using UnityEngine;

namespace Beavermania.Player.AI
{
    [DisallowMultipleComponent]
    public class AutoPlayerCombatStaminaGate : MonoBehaviour
    {
        [SerializeField] AutoPlayerActionAdapter adapter;
        [SerializeField, Range(0.5f, 0.7f)] float minResumeStaminaRatio = 0.6f;
        [SerializeField, Range(0f, 0.15f)] float depletedStaminaRatio = 0.02f;

        bool _mustRecoverStamina;

        public bool MustRecoverStamina => _mustRecoverStamina;
        public float MinResumeStaminaRatio => minResumeStaminaRatio;

        void Awake()
        {
            if (adapter == null)
                adapter = GetComponent<AutoPlayerActionAdapter>();
        }

        void Update()
        {
            UpdateRecoveryState();
        }

        public void ResetRecoveryState()
        {
            _mustRecoverStamina = false;
        }

        public float GetStaminaRatio()
        {
            if (adapter == null || adapter.Player == null || adapter.Player.MaxStamina <= 0f)
                return 1f;

            return Mathf.Clamp01(adapter.Player.CurrentStamina / adapter.Player.MaxStamina);
        }

        public bool CanPerformCombatAttacks()
        {
            UpdateRecoveryState();
            return !_mustRecoverStamina && GetStaminaRatio() > depletedStaminaRatio;
        }

        public void UpdateRecoveryState()
        {
            float ratio = GetStaminaRatio();

            if (!_mustRecoverStamina && ratio <= depletedStaminaRatio)
                _mustRecoverStamina = true;

            if (_mustRecoverStamina && ratio >= minResumeStaminaRatio)
                _mustRecoverStamina = false;
        }
    }
}
