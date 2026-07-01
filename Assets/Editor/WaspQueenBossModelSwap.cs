using Beavermania.NPC;
using UnityEditor;
using UnityEngine;

namespace BeavermaniaEditor
{
    // Swaps the placeholder wasp visual on PF_WaspQueen_Boss for the new rigged WaspQueen.fbx,
    // re-points WaspQueenBoss.Animator, preserves boss anchors, and attaches the no-op event sink.
    public static class WaspQueenBossModelSwap
    {
        const string BossPrefab = "Assets/Prefabs/NPC/WaspQueen/PF_WaspQueen_Boss.prefab";
        const string Fbx = "Assets/Prefabs/WaspQueen/Animations/WaspQueen.fbx";
        const string BossController = "Assets/Prefabs/NPC/WaspQueen/WaspQueen.controller";
        const float VisualScale = 1.5f; // tunable: boss silhouette size

        [MenuItem("Beavermania/WaspQueen/5 Swap Boss Model")]
        public static void Swap()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(Fbx);
            if (model == null) { Debug.LogError("WaspQueen.fbx not found at " + Fbx); return; }
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(BossController);
            if (controller == null) { Debug.LogError("Boss controller not found at " + BossController); return; }
            Avatar avatar = null;
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(Fbx))
            {
                avatar = o as Avatar;
                if (avatar != null) break;
            }

            var root = PrefabUtility.LoadPrefabContents(BossPrefab);
            try
            {
                var boss = root.GetComponent<WaspQueenBoss>();
                Animator oldAnim = boss != null ? boss.Animator : root.GetComponentInChildren<Animator>();
                Transform oldVisual = oldAnim != null ? oldAnim.transform : root.transform.Find("Visual");

                Vector3 lp = oldVisual != null ? oldVisual.localPosition : Vector3.zero;
                Quaternion lr = oldVisual != null ? oldVisual.localRotation : Quaternion.identity;

                // Move any boss-referenced anchors that live under the old visual up to the root so the swap can't break refs.
                if (oldVisual != null && boss != null)
                {
                    Reparent(boss.ProjectileSpawnPoint, oldVisual, root.transform);
                    Reparent(boss.AoeOrigin, oldVisual, root.transform);
                    if (boss.WaspSpawnPoints != null)
                        foreach (var t in boss.WaspSpawnPoints) Reparent(t, oldVisual, root.transform);
                    if (boss.ChargeHitbox != null) Reparent(boss.ChargeHitbox.transform, oldVisual, root.transform);
                }

                var newVis = (GameObject)PrefabUtility.InstantiatePrefab(model, root.scene);
                newVis.transform.SetParent(root.transform, false);
                newVis.name = "Visual";
                newVis.transform.localPosition = lp;
                newVis.transform.localRotation = lr;
                newVis.transform.localScale = Vector3.one * VisualScale;

                var newAnim = newVis.GetComponent<Animator>();
                if (newAnim == null) newAnim = newVis.AddComponent<Animator>();
                newAnim.runtimeAnimatorController = controller;
                if (avatar != null) newAnim.avatar = avatar;
                newAnim.applyRootMotion = false;

                if (newVis.GetComponent<WaspQueenAnimationEventSink>() == null)
                    newVis.AddComponent<WaspQueenAnimationEventSink>();

                if (boss != null) boss.Animator = newAnim;

                if (oldVisual != null) Object.DestroyImmediate(oldVisual.gameObject);

                PrefabUtility.SaveAsPrefabAsset(root, BossPrefab);
                Debug.Log("WaspQueen boss model swapped to WaspQueen.fbx (controller=" + BossController + ", scale=" + VisualScale + ").");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static void Reparent(Transform t, Transform under, Transform newParent)
        {
            if (t != null && t != under && t.IsChildOf(under)) t.SetParent(newParent, true);
        }
    }
}
