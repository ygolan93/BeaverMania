using System;
using Beavermania.Data.NPC;
using Beavermania.Display;
using Beavermania.NPC;
using UnityEditor;
using UnityEngine;

namespace Beavermania.EditorTools
{
    /// <summary>
    /// Repairs Shadow Revenant combat prefab references and creates lightweight pooled VFX prefabs.
    /// Menu: Beavermania/Build/Shadow Revenant Combat Assets
    /// Batch: Unity -executeMethod Beavermania.EditorTools.ShadowRevenantCombatAssetsBuilder.ExecuteBatch
    /// </summary>
    public static class ShadowRevenantCombatAssetsBuilder
    {
        const string ConfigPath = "Assets/Data/NPC/ShadowRevenant/ShadowRevenantConfig.asset";
        const string PrefabFolder = "Assets/Prefabs/NPC/ShadowRevenant";
        const string PlaceholderMaterialPath = PrefabFolder + "/ShadowRevenantPlaceholder.mat";
        const string ShadeEyeMaterialPath = PrefabFolder + "/ShadowRevenantShadeEye.mat";
        const string ProjectilePrefabPath = PrefabFolder + "/ShadowRevenantProjectile.prefab";
        const string FogPrefabPath = PrefabFolder + "/ShadowRevenantDreadFogZone.prefab";
        const string ShadePrefabPath = PrefabFolder + "/ShadowRevenantShadeMinion.prefab";
        const string HitVfxPath = PrefabFolder + "/ShadowRevenantHitVFX.prefab";
        const string DeathVfxPath = PrefabFolder + "/ShadowRevenantDeathVFX.prefab";
        const string PhaseVfxPath = PrefabFolder + "/ShadowRevenantPhaseVFX.prefab";
        const string LightBreakVfxPath = PrefabFolder + "/ShadowRevenantLightBreakVFX.prefab";

        static readonly int EnemyLayer = LayerMask.NameToLayer("Enemy");

        public static void ExecuteBatch()
        {
            try
            {
                BuildAndAssignCombatAssets();
                Debug.Log("[ShadowRevenantCombatAssetsBuilder] Completed successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError("[ShadowRevenantCombatAssetsBuilder] Failed: " + ex);
                EditorApplication.Exit(1);
                return;
            }

            EditorApplication.Exit(0);
        }

        [MenuItem("Beavermania/Build/Shadow Revenant Combat Assets")]
        public static void ExecuteFromMenu()
        {
            BuildAndAssignCombatAssets();
            EditorUtility.DisplayDialog(
                "Shadow Revenant Combat Assets",
                "Combat prefabs and VFX assigned on ShadowRevenantConfig. Check Console for details.",
                "OK");
        }

        public static void BuildAndAssignCombatAssets()
        {
            Material bodyMaterial = LoadOrCreatePlaceholderMaterial();
            Material eyeMaterial = LoadOrCreateShadeEyeMaterial();

            GameObject projectilePrefab = EnsureProjectilePrefab(bodyMaterial);
            GameObject fogPrefab = EnsureFogPrefab(bodyMaterial);
            GameObject shadePrefab = BuildShadeMinionPrefab(bodyMaterial, eyeMaterial);

            GameObject hitVfx = BuildBurstVfxPrefab(
                HitVfxPath,
                "ShadowRevenantHitVFX",
                new Color(0.12f, 0.1f, 0.18f, 0.85f),
                new Color(0.2f, 0.95f, 0.35f, 0f),
                0.45f,
                0.35f,
                24,
                0.5f);

            GameObject deathVfx = BuildBurstVfxPrefab(
                DeathVfxPath,
                "ShadowRevenantDeathVFX",
                new Color(0.08f, 0.06f, 0.12f, 0.9f),
                new Color(0.15f, 0.85f, 0.3f, 0f),
                0.9f,
                0.55f,
                48,
                0.85f);

            GameObject phaseVfx = BuildBurstVfxPrefab(
                PhaseVfxPath,
                "ShadowRevenantPhaseVFX",
                new Color(0.1f, 0.08f, 0.16f, 0.75f),
                new Color(0.25f, 0.9f, 0.45f, 0f),
                0.7f,
                0.5f,
                36,
                0.75f);

            GameObject lightBreakVfx = BuildBurstVfxPrefab(
                LightBreakVfxPath,
                "ShadowRevenantLightBreakVFX",
                new Color(1f, 0.92f, 0.55f, 0.95f),
                new Color(0.35f, 1f, 0.4f, 0f),
                0.65f,
                0.45f,
                40,
                0.7f);

            ShadowRevenantConfig config = AssetDatabase.LoadAssetAtPath<ShadowRevenantConfig>(ConfigPath);
            if (config == null)
                throw new InvalidOperationException("Missing config at " + ConfigPath);

            config.projectilePrefab = projectilePrefab.GetComponent<ShadowRevenantProjectile>();
            config.fogPrefab = fogPrefab.GetComponent<ShadowRevenantDreadFogZone>();
            config.shadeMinionPrefab = shadePrefab.GetComponent<ShadowRevenantShadeMinion>();
            config.hitVfxPrefab = hitVfx;
            config.deathVfxPrefab = deathVfx;
            config.phaseVfxPrefab = phaseVfx;
            config.lightBreakVfxPrefab = lightBreakVfx;

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[ShadowRevenantCombatAssetsBuilder] Assigned projectile, fog, shade, and four VFX prefabs on ShadowRevenantConfig. deathDropPrefabs left empty (optional).");
        }

        static Material LoadOrCreatePlaceholderMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(PlaceholderMaterialPath);
            if (existing != null)
                return existing;

            var shader = Shader.Find("Standard");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Lit");

            var material = new Material(shader)
            {
                color = new Color(0.08f, 0.07f, 0.12f, 1f)
            };
            AssetDatabase.CreateAsset(material, PlaceholderMaterialPath);
            return material;
        }

        static Material LoadOrCreateShadeEyeMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(ShadeEyeMaterialPath);
            if (existing != null)
                return existing;

            var shader = Shader.Find("Standard");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Lit");

            var material = new Material(shader)
            {
                color = new Color(0.15f, 0.9f, 0.35f, 1f)
            };
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", new Color(0.2f, 1.2f, 0.45f));
            AssetDatabase.CreateAsset(material, ShadeEyeMaterialPath);
            return material;
        }

        static GameObject EnsureProjectilePrefab(Material material)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePrefabPath);
            if (existing != null && existing.GetComponent<ShadowRevenantProjectile>() != null)
                return existing;

            var root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            root.name = "ShadowRevenantProjectile";
            root.transform.localScale = Vector3.one * 0.45f;
            var meshCollider = root.GetComponent<MeshCollider>();
            if (meshCollider != null)
                UnityEngine.Object.DestroyImmediate(meshCollider);

            var sphereCollider = root.AddComponent<SphereCollider>();
            sphereCollider.isTrigger = true;
            sphereCollider.radius = 0.5f;

            var rigidbody = root.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            rigidbody.isKinematic = false;

            ApplyMaterial(root, material);
            root.AddComponent<ShadowRevenantProjectile>();
            return SavePrefab(root, ProjectilePrefabPath);
        }

        static GameObject EnsureFogPrefab(Material material)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(FogPrefabPath);
            if (existing != null && existing.GetComponent<ShadowRevenantDreadFogZone>() != null)
                return existing;

            var root = new GameObject("ShadowRevenantDreadFogZone");
            var fogCollider = root.AddComponent<SphereCollider>();
            fogCollider.isTrigger = true;
            fogCollider.radius = 5f;

            var visualRoot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visualRoot.name = "visualRoot";
            visualRoot.transform.SetParent(root.transform, false);
            visualRoot.transform.localPosition = Vector3.zero;
            visualRoot.transform.localScale = new Vector3(1f, 0.05f, 1f);
            UnityEngine.Object.DestroyImmediate(visualRoot.GetComponent<Collider>());
            ApplyMaterial(visualRoot, material);

            var fog = root.AddComponent<ShadowRevenantDreadFogZone>();
            var serializedFog = new SerializedObject(fog);
            serializedFog.FindProperty("fogCollider").objectReferenceValue = fogCollider;
            serializedFog.FindProperty("visualRoot").objectReferenceValue = visualRoot.transform;
            serializedFog.ApplyModifiedPropertiesWithoutUndo();

            return SavePrefab(root, FogPrefabPath);
        }

        static GameObject BuildShadeMinionPrefab(Material bodyMaterial, Material eyeMaterial)
        {
            var root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            root.name = "ShadowRevenantShadeMinion";
            root.transform.localScale = new Vector3(0.55f, 0.55f, 0.55f);
            if (EnemyLayer >= 0)
                root.layer = EnemyLayer;

            var meshCollider = root.GetComponent<MeshCollider>();
            if (meshCollider != null)
                UnityEngine.Object.DestroyImmediate(meshCollider);

            var sphereCollider = root.AddComponent<SphereCollider>();
            sphereCollider.isTrigger = true;
            sphereCollider.radius = 0.5f;

            var rigidbody = root.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            rigidbody.isKinematic = false;
            rigidbody.constraints = RigidbodyConstraints.FreezeRotation;

            ApplyMaterial(root, bodyMaterial);

            CreateShadeEye(root.transform, "EyeLeft", new Vector3(-0.18f, 0.12f, 0.42f), eyeMaterial);
            CreateShadeEye(root.transform, "EyeRight", new Vector3(0.18f, 0.12f, 0.42f), eyeMaterial);

            var shade = root.AddComponent<ShadowRevenantShadeMinion>();
            var serializedShade = new SerializedObject(shade);
            serializedShade.FindProperty("shadeRigidbody").objectReferenceValue = rigidbody;
            serializedShade.FindProperty("damageCollider").objectReferenceValue = sphereCollider;
            serializedShade.ApplyModifiedPropertiesWithoutUndo();

            return SavePrefab(root, ShadePrefabPath);
        }

        static void CreateShadeEye(Transform parent, string name, Vector3 localPosition, Material eyeMaterial)
        {
            var eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eye.name = name;
            eye.transform.SetParent(parent, false);
            eye.transform.localPosition = localPosition;
            eye.transform.localScale = Vector3.one * 0.22f;
            if (EnemyLayer >= 0)
                eye.layer = EnemyLayer;

            var collider = eye.GetComponent<Collider>();
            if (collider != null)
                UnityEngine.Object.DestroyImmediate(collider);

            ApplyMaterial(eye, eyeMaterial);
        }

        static GameObject BuildBurstVfxPrefab(
            string path,
            string objectName,
            Color startColor,
            Color endColor,
            float particleLifetime,
            float startSize,
            int burstCount,
            float duration)
        {
            var root = new GameObject(objectName);
            var particleSystem = root.AddComponent<ParticleSystem>();
            root.AddComponent<ParticleSystemRenderer>();
            root.AddComponent<PooledOneShotVfx>();

            var main = particleSystem.main;
            main.duration = duration;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = particleLifetime;
            main.startSize = startSize;
            main.startSpeed = 2.5f;
            main.maxParticles = Mathf.Max(burstCount, 8);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.None;

            var emission = particleSystem.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)burstCount) });

            var shape = particleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.15f;

            var colorOverLifetime = particleSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(startColor, 0f),
                    new GradientColorKey(endColor, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(startColor.a, 0f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = gradient;

            var sizeOverLifetime = particleSystem.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));

            var renderer = root.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat");

            return SavePrefab(root, path);
        }

        static void ApplyMaterial(GameObject target, Material material)
        {
            var renderer = target.GetComponent<Renderer>();
            if (renderer != null && material != null)
                renderer.sharedMaterial = material;
        }

        static GameObject SavePrefab(GameObject temporaryRoot, string path)
        {
            var saved = PrefabUtility.SaveAsPrefabAsset(temporaryRoot, path);
            UnityEngine.Object.DestroyImmediate(temporaryRoot);
            return saved;
        }
    }
}
