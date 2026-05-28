using System;
using UnityEngine;

namespace Beavermania.UI.Tips
{
    public static class TipsSettings
    {
        public const string TipsEnabledPrefKey = "Beavermania.Tips.Enabled";
        const int EnabledValue = 1;
        const int DisabledValue = 0;

        public static event Action<bool> Changed;

        public static bool Enabled
        {
            get => PlayerPrefs.GetInt(TipsEnabledPrefKey, EnabledValue) == EnabledValue;
            set
            {
                int nextValue = value ? EnabledValue : DisabledValue;
                if (PlayerPrefs.GetInt(TipsEnabledPrefKey, EnabledValue) == nextValue)
                    return;

                PlayerPrefs.SetInt(TipsEnabledPrefKey, nextValue);
                PlayerPrefs.Save();
                Changed?.Invoke(value);
            }
        }
    }
}
