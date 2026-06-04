using System;
using Beavermania.Audio;
using Beavermania.Data.NPC;
using Beavermania.Display;
using Beavermania.NPC;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

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
        const string ShadeHitVfxPath = PrefabFolder + "/ShadowRevenantShadeHitVFX.prefab";
        const string ShadeDeathVfxPath = PrefabFolder + "/ShadowRevenantShadeDeathVFX.prefab";
        const string RemainsPrefabPath = PrefabFolder + "/ShadowRevenantRemains.prefab";
        const string ChargeWindupVfxPath = PrefabFolder + "/ShadowRevenantChargeWindupVFX.prefab";
        const string ChargeImpactVfxPath = PrefabFolder + "/ShadowRevenantChargeImpactVFX.prefab";
        const string ProjectileTracerVfxPath = PrefabFolder + "/ShadowRevenantProjectileTracerVFX.prefab";
        const string AudioProfilePath = "Assets/Data/Audio/ShadowRevenant/ShadowRevenantAudioProfile.asset";
        const string AudioSfxFolder = "Assets/Data/Audio/ShadowRevenant/Sfx";
        const string BossPrefabPath = PrefabFolder + "/ShadowRevenant.prefab";
        const string AimLineMaterialPath = PrefabFolder + "/ShadowRevenantAimLine.mat";

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
                new Color(0.1f, 0.08f, 0.14f, 0.9f),
                new Color(0.25f, 1f, 0.4f, 0f),
                0.4f,
                0.72f,
                34,
                0.45f);

            GameObject deathVfx = BuildBurstVfxPrefab(
                DeathVfxPath,
                "ShadowRevenantDeathVFX",
                new Color(0.06f, 0.05f, 0.1f, 0.95f),
                new Color(0.2f, 0.95f, 0.35f, 0f),
                1f,
                1.1f,
                64,
                1f);

            GameObject phaseVfx = BuildBurstVfxPrefab(
                PhaseVfxPath,
                "ShadowRevenantPhaseVFX",
                new Color(0.18f, 0.06f, 0.28f, 0.85f),
                new Color(0.35f, 0.15f, 0.55f, 0f),
                0.85f,
                1f,
                48,
                0.8f);

            GameObject lightBreakVfx = BuildBurstVfxPrefab(
                LightBreakVfxPath,
                "ShadowRevenantLightBreakVFX",
                new Color(1f, 0.95f, 0.65f, 1f),
                new Color(0.4f, 1f, 0.45f, 0f),
                0.55f,
                1.05f,
                58,
                0.75f);

            GameObject shadeHitVfx = BuildBurstVfxPrefab(
                ShadeHitVfxPath,
                "ShadowRevenantShadeHitVFX",
                new Color(0.12f, 0.1f, 0.16f, 0.75f),
                new Color(0.3f, 1f, 0.4f, 0f),
                0.25f,
                0.38f,
                16,
                0.3f);

            GameObject shadeDeathVfx = BuildBurstVfxPrefab(
                ShadeDeathVfxPath,
                "ShadowRevenantShadeDeathVFX",
                new Color(0.08f, 0.06f, 0.12f, 0.85f),
                new Color(0.35f, 1f, 0.45f, 0f),
                0.35f,
                0.52f,
                22,
                0.4f);

            GameObject remainsPrefab = BuildRemainsPrefab(bodyMaterial);

            GameObject chargeWindupVfx = BuildBurstVfxPrefab(
                ChargeWindupVfxPath,
                "ShadowRevenantChargeWindupVFX",
                new Color(0.15f, 0.9f, 0.35f, 0.85f),
                new Color(0.05f, 0.2f, 0.1f, 0f),
                0.35f,
                0.6f,
                26,
                0.45f);

            GameObject chargeImpactVfx = BuildBurstVfxPrefab(
                ChargeImpactVfxPath,
                "ShadowRevenantChargeImpactVFX",
                new Color(0.1f, 0.08f, 0.14f, 0.9f),
                new Color(0.35f, 1f, 0.45f, 0f),
                0.45f,
                0.85f,
                38,
                0.55f);

            GameObject projectileTracerVfx = BuildBurstVfxPrefab(
                ProjectileTracerVfxPath,
                "ShadowRevenantProjectileTracerVFX",
                new Color(0.25f, 1f, 0.45f, 0.95f),
                new Color(0.05f, 0.25f, 0.1f, 0f),
                0.2f,
                0.32f,
                18,
                0.25f);

            Material aimLineMaterial = LoadOrCreateAimLineMaterial();

            ShadowRevenantAudioProfile audioProfile = LoadOrCreateAudioProfile();
            WireAudioProfile(audioProfile);

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
            config.shadeHitVfxPrefab = shadeHitVfx;
            config.shadeDeathVfxPrefab = shadeDeathVfx;
            config.remainsPrefab = remainsPrefab;
            config.remainsLifetime = 45f;
            config.chargeWindupVfxPrefab = chargeWindupVfx;
            config.chargeImpactVfxPrefab = chargeImpactVfx;
            config.projectileTracerVfxPrefab = projectileTracerVfx;
            config.audioProfile = audioProfile;

            SerializedObject serializedConfig = new SerializedObject(config);
            serializedConfig.FindProperty("projectileObstructionMask").intValue = 1 << 9;
            serializedConfig.FindProperty("chargeObstructionMask").intValue = 1 << 9;
            serializedConfig.ApplyModifiedPropertiesWithoutUndo();

            EnsureShadeMinionAudioComponent(shadePrefab);
            EnsureBossAimLineVisuals(aimLineMaterial);

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[ShadowRevenantCombatAssetsBuilder] Assigned projectile, fog, shade, VFX, charge/tracer VFX, audio profile, and remains on ShadowRevenantConfig.");
        }

        static ShadowRevenantAudioProfile LoadOrCreateAudioProfile()
        {
            var existing = AssetDatabase.LoadAssetAtPath<ShadowRevenantAudioProfile>(AudioProfilePath);
            if (existing != null)
                return existing;

            EnsureFolder("Assets/Data/Audio/ShadowRevenant");
            var profile = ScriptableObject.CreateInstance<ShadowRevenantAudioProfile>();
            AssetDatabase.CreateAsset(profile, AudioProfilePath);
            return profile;
        }

        static void WireAudioProfile(ShadowRevenantAudioProfile profile)
        {
            if (profile == null)
                return;

            EnsureFolder(AudioSfxFolder);

            profile.shadeAttack = LoadOrCreateSfxEvent(
                AudioSfxFolder + "/ShadeAttack.asset",
                "Assets/Sounds/Knife.mp3",
                1f,
                0.15f);
            profile.shadeOrbitLoop = LoadOrCreateSfxEvent(
                AudioSfxFolder + "/ShadeOrbitLoop.asset",
                "Assets/Sounds/AHH.mp3",
                0.55f,
                1.2f);
            profile.shadeSpawn = LoadOrCreateSfxEvent(
                AudioSfxFolder + "/ShadeSpawn.asset",
                "Assets/Sounds/Buzz.mp3",
                0.6f,
                0.2f);
            profile.shadeApproachMove = LoadOrCreateSfxEvent(
                AudioSfxFolder + "/ShadeApproachMove.asset",
                "Assets/Sounds/Buzz.mp3",
                0.7f,
                0.15f);
            profile.shadeHit = LoadOrCreateSfxEvent(
                AudioSfxFolder + "/ShadeHit.asset",
                "Assets/Sounds/SwordDamageLite.mp3",
                0.35f,
                0.12f);
            profile.shadeDeath = LoadOrCreateSfxEvent(
                AudioSfxFolder + "/ShadeDeath.asset",
                "Assets/Sounds/SwordDamageHeavy.mp3",
                1f,
                0.08f);
            profile.bossStrafePulse = LoadOrCreateSfxEvent(
                AudioSfxFolder + "/BossStrafePulse.asset",
                "Assets/Sounds/Beat.ogg",
                0.4f,
                0.45f);

            profile.bossSpawn = LoadOrCreateSfxEvent(
                AudioSfxFolder + "/BossSpawn.asset",
                "Assets/Sounds/Monster Breathing.mp3",
                0.65f,
                0.5f);
            profile.bossAggro = LoadOrCreateSfxEvent(
                AudioSfxFolder + "/BossAggro.asset",
                "Assets/Sounds/Electric - Sound Effect.mp3",
                0.7f,
                1f);
            profile.phaseOut = LoadOrCreateSfxEvent(
                AudioSfxFolder + "/PhaseOut.asset",
                "Assets/Sounds/Poof.mp3",
                0.6f,
                0.3f);
            profile.phaseIn = LoadOrCreateSfxEvent(
                AudioSfxFolder + "/PhaseIn.asset",
                "Assets/Sounds/Whoosh.ogg",
                0.65f,
                0.3f);
            profile.bossHit = LoadOrCreateSfxEvent(
                AudioSfxFolder + "/BossHit.asset",
                "Assets/Sounds/SwordDamageLite.mp3",
                0.35f,
                0.12f);
            profile.lightBreak = LoadOrCreateSfxEvent(
                AudioSfxFolder + "/LightBreak.asset",
                "Assets/Sounds/Growth.mp3",
                0.75f,
                0.4f);
            profile.bossDeath = LoadOrCreateSfxEvent(
                AudioSfxFolder + "/BossDeath.asset",
                "Assets/Sounds/Boom (mp3cut.net).mp3",
                1f,
                0.5f);
            profile.bossRemainsSettle = LoadOrCreateSfxEvent(
                AudioSfxFolder + "/BossRemainsSettle.asset",
                "Assets/Sounds/Pop.ogg",
                0.5f,
                0.3f);
            profile.projectileWindup = LoadOrCreateSfxEvent(
                AudioSfxFolder + "/ProjectileWindup.asset",
                "Assets/Sounds/cartoon fireball sound effect.mp3",
                0.55f,
                0.25f);
            profile.projectileFire = LoadOrCreateSfxEvent(
                AudioSfxFolder + "/ProjectileFire.asset",
                "Assets/Sounds/ArrowShoot.mp3",
                0.7f,
                0.2f);
            profile.projectileImpact = LoadOrCreateSfxEvent(
                AudioSfxFolder + "/ProjectileImpact.asset",
                "Assets/Sounds/AirHit.mp3",
                0.65f,
                0.15f);
            profile.fogTelegraph = LoadOrCreateSfxEvent(
                AudioSfxFolder + "/FogTelegraph.asset",
                "Assets/Sounds/Wind.ogg",
                0.45f,
                0.4f);
            profile.fogActiveStart = LoadOrCreateSfxEvent(
                AudioSfxFolder + "/FogActiveStart.asset",
                "Assets/Sounds/Underwater.ogg",
                0.5f,
                0.5f);
            profile.fogDisappear = LoadOrCreateSfxEvent(
                AudioSfxFolder + "/FogDisappear.asset",
                "Assets/Sounds/Poof.mp3",
                0.4f,
                0.3f);
            profile.summonWindup = LoadOrCreateSfxEvent(
                AudioSfxFolder + "/SummonWindup.asset",
                "Assets/Sounds/Monster Breathing.mp3",
                0.55f,
                0.35f);
            profile.summonComplete = LoadOrCreateSfxEvent(
                AudioSfxFolder + "/SummonComplete.asset",
                "Assets/Sounds/Growth.mp3",
                0.7f,
                0.4f);
            profile.chargeWindup = LoadOrCreateSfxEvent(
                AudioSfxFolder + "/ChargeWindup.asset",
                "Assets/Sounds/ArrowDraw.mp3",
                0.55f,
                0.3f);
            profile.chargeDash = LoadOrCreateSfxEvent(
                AudioSfxFolder + "/ChargeDash.asset",
                "Assets/Sounds/Slide.ogg",
                0.65f,
                0.25f);
            profile.chargeImpact = LoadOrCreateSfxEvent(
                AudioSfxFolder + "/ChargeImpact.asset",
                "Assets/Sounds/SwordDamageHeavy.mp3",
                0.85f,
                0.2f);

            if (profile.ambientLoopClip == null)
            {
                profile.ambientLoopClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Eden/Music/Interface and Item Sounds/MISC/Danger [loop].wav");
                profile.ambientLoopVolume = 0.35f;
            }

            EditorUtility.SetDirty(profile);
        }

        static SfxEventDefinition LoadOrCreateSfxEvent(string assetPath, string clipPath, float volume, float minInterval)
        {
            var existing = AssetDatabase.LoadAssetAtPath<SfxEventDefinition>(assetPath);
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
            if (clip == null)
            {
                Debug.LogWarning("[ShadowRevenantCombatAssetsBuilder] Missing audio clip at " + clipPath, null);
                return existing;
            }

            if (existing == null)
            {
                existing = ScriptableObject.CreateInstance<SfxEventDefinition>();
                AssetDatabase.CreateAsset(existing, assetPath);
            }

            existing.clip = clip;
            existing.volume = volume;
            existing.pitchMin = 1f;
            existing.pitchMax = 1f;
            existing.minInterval = minInterval;
            EditorUtility.SetDirty(existing);
            return existing;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            string folderName = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            if (!string.IsNullOrEmpty(parent))
                AssetDatabase.CreateFolder(parent, folderName);
        }

        static void EnsureShadeMinionAudioComponent(GameObject shadePrefab)
        {
            if (shadePrefab == null)
                return;

            ShadowRevenantShadeMinion shade = shadePrefab.GetComponent<ShadowRevenantShadeMinion>();
            if (shade == null)
                return;

            if (shadePrefab.GetComponent<AudioSource>() == null)
                shadePrefab.AddComponent<AudioSource>();

            if (shadePrefab.GetComponent<ShadowRevenantShadeAudio>() == null)
                shadePrefab.AddComponent<ShadowRevenantShadeAudio>();

            AudioSource audioSource = shadePrefab.GetComponent<AudioSource>();
            ShadowRevenantShadeAudio shadeAudio = shadePrefab.GetComponent<ShadowRevenantShadeAudio>();
            if (audioSource != null)
            {
                audioSource.playOnAwake = false;
                audioSource.loop = false;
                audioSource.spatialBlend = 1f;
                audioSource.rolloffMode = AudioRolloffMode.Linear;
                audioSource.minDistance = 1f;
                audioSource.maxDistance = 25f;
            }

            if (shadeAudio != null && audioSource != null)
            {
                SerializedObject serializedAudio = new SerializedObject(shadeAudio);
                serializedAudio.FindProperty("actionSource").objectReferenceValue = audioSource;
                serializedAudio.ApplyModifiedPropertiesWithoutUndo();
            }

            Transform rootTransform = shadePrefab.transform;
            rootTransform.localScale = new Vector3(2f / 1.5f, 2f / 1.5f, 2f / 1.5f);

            PrefabUtility.SavePrefabAsset(shadePrefab);
        }

        static GameObject BuildRemainsPrefab(Material bodyMaterial)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(RemainsPrefabPath);
            if (existing != null && existing.GetComponent<ShadowRevenantRemains>() != null)
                return existing;

            var root = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            root.name = "ShadowRevenantRemains";
            root.transform.localScale = new Vector3(0.85f, 0.14f, 0.85f);

            var meshCollider = root.GetComponent<MeshCollider>();
            if (meshCollider != null)
                UnityEngine.Object.DestroyImmediate(meshCollider);

            ApplyMaterial(root, bodyMaterial);
            var remains = root.AddComponent<ShadowRevenantRemains>();
            var serializedRemains = new SerializedObject(remains);
            serializedRemains.FindProperty("lifetimeSeconds").floatValue = 45f;
            serializedRemains.ApplyModifiedPropertiesWithoutUndo();

            return SavePrefab(root, RemainsPrefabPath);
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

        static Material LoadOrCreateFogTelegraphMaterial()
        {
            const string path = PrefabFolder + "/ShadowRevenantFogTelegraph.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
                return existing;

            var shader = Shader.Find("Standard");
            var material = new Material(shader) { color = new Color(0.82f, 0.14f, 0.12f, 0.28f) };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static Material LoadOrCreateFogActiveMaterial()
        {
            const string path = PrefabFolder + "/ShadowRevenantFogActive.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
                return existing;

            var shader = Shader.Find("Standard");
            var material = new Material(shader) { color = new Color(0.6f, 0.1f, 0.1f, 0.24f) };
            AssetDatabase.CreateAsset(material, path);
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

            Material telegraphMaterial = LoadOrCreateFogTelegraphMaterial();
            Material activeMaterial = LoadOrCreateFogActiveMaterial();

            var telegraphRoot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            telegraphRoot.name = "TelegraphRing";
            telegraphRoot.transform.SetParent(root.transform, false);
            telegraphRoot.transform.localPosition = new Vector3(0f, 0.01f, 0f);
            telegraphRoot.transform.localScale = new Vector3(10f, 0.04f, 10f);
            UnityEngine.Object.DestroyImmediate(telegraphRoot.GetComponent<Collider>());
            ApplyMaterial(telegraphRoot, telegraphMaterial);
            telegraphRoot.SetActive(false);

            var activeVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            activeVisual.name = "ActiveHazard";
            activeVisual.transform.SetParent(root.transform, false);
            activeVisual.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            activeVisual.transform.localScale = new Vector3(10f, 0.03f, 10f);
            UnityEngine.Object.DestroyImmediate(activeVisual.GetComponent<Collider>());
            ApplyMaterial(activeVisual, activeMaterial);
            activeVisual.SetActive(false);

            var fog = root.AddComponent<ShadowRevenantDreadFogZone>();
            var serializedFog = new SerializedObject(fog);
            serializedFog.FindProperty("fogCollider").objectReferenceValue = fogCollider;
            serializedFog.FindProperty("telegraphRoot").objectReferenceValue = telegraphRoot.transform;
            serializedFog.FindProperty("activeVisualRoot").objectReferenceValue = activeVisual.transform;
            serializedFog.ApplyModifiedPropertiesWithoutUndo();

            return SavePrefab(root, FogPrefabPath);
        }

        static GameObject BuildShadeMinionPrefab(Material bodyMaterial, Material eyeMaterial)
        {
            var root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            root.name = "ShadowRevenantShadeMinion";
            root.transform.localScale = new Vector3(2f / 1.5f, 2f / 1.5f, 2f / 1.5f);
            if (EnemyLayer >= 0)
                root.layer = EnemyLayer;

            var meshCollider = root.GetComponent<MeshCollider>();
            if (meshCollider != null)
                UnityEngine.Object.DestroyImmediate(meshCollider);

            var sphereCollider = root.AddComponent<SphereCollider>();
            sphereCollider.isTrigger = true;
            sphereCollider.radius = 0.42f;

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
            root.transform.localScale = Vector3.one * 1.25f;
            var particleSystem = root.AddComponent<ParticleSystem>();
            if (root.GetComponent<ParticleSystemRenderer>() == null)
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

        static Material LoadOrCreateAimLineMaterial()
        {
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(AimLineMaterialPath);
            if (existing != null)
                return existing;

            Shader shader = Shader.Find("Sprites/Default")
                ?? Shader.Find("Unlit/Color")
                ?? Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                Debug.LogWarning("[ShadowRevenantCombatAssetsBuilder] Could not resolve aim line shader.", null);
                return null;
            }

            var material = new Material(shader);
            material.name = "ShadowRevenantAimLine";
            material.color = Color.white;

            if (shader.name.Contains("Universal Render Pipeline"))
            {
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 0f);
                material.SetFloat("_AlphaClip", 0f);
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0f);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }

            AssetDatabase.CreateAsset(material, AimLineMaterialPath);
            return material;
        }

        static void EnsureBossAimLineVisuals(Material aimLineMaterial)
        {
            GameObject bossRoot = PrefabUtility.LoadPrefabContents(BossPrefabPath);
            try
            {
                Transform aimLineTransform = bossRoot.transform.Find("ProjectileAimLine");
                if (aimLineTransform == null)
                {
                    Debug.LogWarning("[ShadowRevenantCombatAssetsBuilder] ProjectileAimLine child missing on boss prefab.", bossRoot);
                    return;
                }

                LineRenderer lineRenderer = aimLineTransform.GetComponent<LineRenderer>();
                ShadowRevenantProjectileAimLine aimLine = aimLineTransform.GetComponent<ShadowRevenantProjectileAimLine>();
                if (lineRenderer == null || aimLine == null)
                {
                    Debug.LogWarning("[ShadowRevenantCombatAssetsBuilder] ProjectileAimLine components missing on boss prefab.", aimLineTransform);
                    return;
                }

                if (aimLineMaterial != null)
                    lineRenderer.sharedMaterial = aimLineMaterial;

                lineRenderer.useWorldSpace = true;
                lineRenderer.shadowCastingMode = ShadowCastingMode.Off;
                lineRenderer.receiveShadows = false;
                lineRenderer.alignment = LineAlignment.View;
                lineRenderer.textureMode = LineTextureMode.Stretch;
                lineRenderer.numCapVertices = 4;
                lineRenderer.numCornerVertices = 2;
                lineRenderer.sortingOrder = 10;

                SerializedObject serializedAimLine = new SerializedObject(aimLine);
                if (aimLineMaterial != null)
                    serializedAimLine.FindProperty("aimLineMaterial").objectReferenceValue = aimLineMaterial;
                serializedAimLine.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(bossRoot, BossPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(bossRoot);
            }
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
