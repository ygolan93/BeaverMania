#if UNITY_EDITOR
using Cinemachine;
using UnityEditor;
using UnityEngine;

namespace Beavermania.EditorTools
{
    public static class PlayerPackPrefabBuilder
    {
        private const string PlayerPrefabPath = "Assets/Prefabs/OtterPlayer/Otter_Shapekeys/Player.prefab";
        private const string PlayerPackPrefabPath = "Assets/Prefabs/OtterPlayer/PlayerPack.prefab";

        [MenuItem("Beavermania/Build PlayerPack Prefab")]
        public static void BuildPlayerPackPrefab()
        {
            GameObject root = null;

            try
            {
                var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
                if (playerPrefab == null)
                {
                    Debug.LogError($"Player prefab not found: {PlayerPrefabPath}");
                    return;
                }

                root = new GameObject("PlayerPack");

                var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
                player.name = playerPrefab.name;
                player.transform.SetParent(root.transform, false);
                player.transform.localPosition = Vector3.zero;
                player.transform.localRotation = Quaternion.identity;
                player.transform.localScale = Vector3.one;

                var freeLook = player.GetComponentInChildren<CinemachineFreeLook>(true);
                if (freeLook == null)
                {
                    Debug.LogError("No CinemachineFreeLook found in Player prefab instance.");
                    return;
                }

                freeLook.Priority = 10;
                freeLook.enabled = true;

                var mainCamera = new GameObject("Main Camera");
                mainCamera.tag = "MainCamera";
                mainCamera.transform.SetParent(root.transform, false);

                var camera = mainCamera.AddComponent<Camera>();
                camera.enabled = true;
                mainCamera.AddComponent<CinemachineBrain>();

                if (!HasEnabledAudioListener(root))
                {
                    mainCamera.AddComponent<AudioListener>();
                }

                PrefabUtility.SaveAsPrefabAsset(root, PlayerPackPrefabPath, out var success);
                if (!success)
                {
                    Debug.LogError($"Failed to save prefab: {PlayerPackPrefabPath}");
                    return;
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"Saved {PlayerPackPrefabPath}");
            }
            finally
            {
                if (root != null)
                {
                    Object.DestroyImmediate(root);
                }
            }
        }

        private static bool HasEnabledAudioListener(GameObject root)
        {
            var listeners = root.GetComponentsInChildren<AudioListener>(true);
            for (var i = 0; i < listeners.Length; i++)
            {
                if (listeners[i] != null && listeners[i].enabled)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
#endif
