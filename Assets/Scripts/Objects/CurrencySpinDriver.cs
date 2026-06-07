using System.Collections.Generic;
using UnityEngine;

namespace Beavermania.Objects
{
    public sealed class CurrencySpinDriver : MonoBehaviour
    {
        const float SpinSpeedDegrees = 170f;
        const float MaxSpinDistance = 45f;

        static readonly List<CurrencySpin> ActiveCoins = new List<CurrencySpin>(512);
        static CurrencySpinDriver instance;
        static Camera cachedCamera;
        static float cachedCameraLookupTime = -999f;
        const float CameraLookupInterval = 0.5f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void EnsureDriver()
        {
            if (instance != null)
                return;

            var driverObject = new GameObject(nameof(CurrencySpinDriver));
            driverObject.hideFlags = HideFlags.HideAndDontSave;
            instance = driverObject.AddComponent<CurrencySpinDriver>();
            DontDestroyOnLoad(driverObject);
        }

        public static void Register(CurrencySpin coin)
        {
            if (coin == null || ActiveCoins.Contains(coin))
                return;

            ActiveCoins.Add(coin);
        }

        public static void Unregister(CurrencySpin coin)
        {
            if (coin == null)
                return;

            ActiveCoins.Remove(coin);
        }

        void Update()
        {
            if (ActiveCoins.Count == 0)
                return;

            Camera camera = ResolveCamera();
            Vector3 cameraPosition = camera != null ? camera.transform.position : Vector3.zero;
            bool hasCamera = camera != null;
            float maxDistanceSq = MaxSpinDistance * MaxSpinDistance;
            float deltaTime = Time.deltaTime;
            float spinDelta = SpinSpeedDegrees * deltaTime;

            for (int i = ActiveCoins.Count - 1; i >= 0; i--)
            {
                CurrencySpin coin = ActiveCoins[i];
                if (coin == null)
                {
                    ActiveCoins.RemoveAt(i);
                    continue;
                }

                if (hasCamera)
                {
                    Vector3 offset = coin.transform.position - cameraPosition;
                    if (offset.sqrMagnitude > maxDistanceSq)
                        continue;
                }

                coin.ApplySpin(spinDelta);
            }
        }

        static Camera ResolveCamera()
        {
            if (cachedCamera != null)
                return cachedCamera;

            if (Time.unscaledTime - cachedCameraLookupTime < CameraLookupInterval)
                return null;

            cachedCameraLookupTime = Time.unscaledTime;
            cachedCamera = Camera.main;
            return cachedCamera;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetDriver()
        {
            ActiveCoins.Clear();
            instance = null;
            cachedCamera = null;
            cachedCameraLookupTime = -999f;
        }
    }
}
