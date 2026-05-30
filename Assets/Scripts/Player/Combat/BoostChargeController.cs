using Beavermania.Data.Combat;
using UnityEngine;

namespace Beavermania.Player.Combat
{
    [DisallowMultipleComponent]
    public sealed class BoostChargeController : MonoBehaviour
    {
        [SerializeField] BoostChargeSettings settings;
        [SerializeField] bool debugBoostCharge;

        float currentCharge;
        int hitsSinceComboBonus;
        float lastCombatActivityTime;
        bool boostActive;
        float boostDurationSeconds = 10f;
        float boostTimeRemainingSeconds;

        public float CurrentCharge => currentCharge;
        public float MaxCharge => ActiveSettings.maxCharge;
        public float NormalizedCharge => ActiveSettings.maxCharge > 0f ? currentCharge / ActiveSettings.maxCharge : 0f;
        public float NormalizedBoostTimeRemaining => boostActive && boostDurationSeconds > 0f
            ? Mathf.Clamp01(boostTimeRemainingSeconds / boostDurationSeconds)
            : 0f;
        public bool IsReady => !boostActive && currentCharge >= ActiveSettings.readyThreshold;
        public bool IsBoostActive => boostActive;

        BoostChargeSettings ActiveSettings => settings != null ? settings : GetDefaultSettings();

        static BoostChargeSettings s_defaultSettings;

        void Awake()
        {
            currentCharge = 0f;
            lastCombatActivityTime = Time.time;
        }

        void Update()
        {
            if (boostActive)
                return;

            ApplyOutOfCombatDecay();
        }

        public void SetBoostActive(bool active)
        {
            boostActive = active;
            if (!active)
            {
                boostTimeRemainingSeconds = 0f;
            }
        }

        public void SetActiveBoostTime(float remainingSeconds, float durationSeconds)
        {
            if (durationSeconds > 0f)
                boostDurationSeconds = durationSeconds;

            boostTimeRemainingSeconds = Mathf.Max(0f, remainingSeconds);
        }

        public bool TryBeginBoost()
        {
            if (boostActive || currentCharge < ActiveSettings.readyThreshold)
            {
                if (debugBoostCharge)
                    Debug.Log($"[BoostCharge] Denied begin. active={boostActive} charge={currentCharge}", this);
                return false;
            }

            currentCharge = 0f;
            hitsSinceComboBonus = 0;
            boostActive = true;
            lastCombatActivityTime = Time.time;

            if (debugBoostCharge)
                Debug.Log("[BoostCharge] Boost consumed — active.", this);

            return true;
        }

        public void RegisterHit(int damageAmount)
        {
            if (damageAmount <= 0)
                return;

            MarkCombatActivity();
            AddCharge(ActiveSettings.chargePerHit);

            hitsSinceComboBonus++;
            if (hitsSinceComboBonus >= ActiveSettings.comboHitsForBonus)
            {
                hitsSinceComboBonus = 0;
                AddCharge(ActiveSettings.comboBonusCharge);
            }
        }

        public void RegisterKill()
        {
            MarkCombatActivity();
            AddCharge(ActiveSettings.chargePerKill);
        }

        public void RegisterPlayerDamaged()
        {
            if (boostActive)
                return;

            currentCharge = Mathf.Max(0f, currentCharge - ActiveSettings.chargeLostOnPlayerDamage);
            lastCombatActivityTime = Time.time;
        }

        public string GetHudLabel()
        {
            if (boostActive)
                return "BOOST ACTIVE";

            if (IsReady)
                return "BOOST READY — press Y";

            int percent = Mathf.RoundToInt(NormalizedCharge * 100f);
            return $"Boost {percent}%";
        }

        void MarkCombatActivity()
        {
            lastCombatActivityTime = Time.time;
        }

        void AddCharge(float amount)
        {
            if (amount <= 0f || boostActive)
                return;

            currentCharge = Mathf.Min(ActiveSettings.maxCharge, currentCharge + amount);
        }

        void ApplyOutOfCombatDecay()
        {
            if (ActiveSettings.decayPerSecond <= 0f)
                return;

            if (Time.time - lastCombatActivityTime < ActiveSettings.outOfCombatDelay)
                return;

            currentCharge = Mathf.Max(0f, currentCharge - ActiveSettings.decayPerSecond * Time.deltaTime);
        }

        static BoostChargeSettings GetDefaultSettings()
        {
            if (s_defaultSettings != null)
                return s_defaultSettings;

            s_defaultSettings = ScriptableObject.CreateInstance<BoostChargeSettings>();
            return s_defaultSettings;
        }
    }
}
