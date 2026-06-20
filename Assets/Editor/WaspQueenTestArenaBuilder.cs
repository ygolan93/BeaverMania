using System;
using System.Collections.Generic;
using System.IO;
using Beavermania.Data.NPC;
using Beavermania.NPC;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Beavermania.EditorTools
{
    /// <summary>
    /// Builds Wasp Queen boss placeholder prefabs, config asset, animator controller, and stages the boss
    /// into the shared ShadowRevenantTestArena scene. Mirrors ShadowRevenantTestArenaBuilder conventions.
    /// Batch: Unity -executeMethod Beavermania.EditorTools.WaspQueenTestArenaBuilder.ExecuteBatch
    /// </summary>
    public static class WaspQueenTestArenaBuilder
    {
        const string ScenePath = "Assets/Scenes/ShadowRevenantTestArena.unity";
        const string ConfigFolder = "Assets/Data/NPC/WaspQueen";
        const string ConfigPath = ConfigFolder + "/WaspQueenConfig.asset";
        const string PrefabFolder = "Assets/Prefabs/NPC/WaspQueen";
        const string BossPrefabPath = PrefabFolder + "/PF_WaspQueen_Boss.prefab";
        const string ProjectilePrefabPath = PrefabFolder + "/PF_WaspQueen_PoisonProjectile.prefab";
        const string ZonePrefabPath = PrefabFolder + "/PF_WaspQueen_PoisonZone.prefab";
        const string AnimatorControllerPath = PrefabFolder + "/WaspQueen.controller";
        const string PoisonMaterialPath = PrefabFolder + "/WaspQueenPoison.mat";
        const string TelegraphMaterialPath = PrefabFolder + "/WaspQueenPoisonTelegraph.mat";

        const string WaspSourcePrefabPath = "Assets/Prefabs/Wasp/LVL1 Wasp.prefab";
        const string ExplosionPrefabPath = "Assets/Prefabs/ProjectEffects/GreatExplosion.prefab";
        const string WaspIdleClipFbxPath = "Assets/Prefabs/Wasp/Animations/Wasp.fbx";
        const string BeatenClipFbxPath = "Assets/Prefabs/Wasp/Animations/Beaten.fbx";
        const string PoisonCloudVfxPath = "Assets/Eden/VFX/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Misc/CFXR2 Poison Cloud.prefab";

        static readonly string[] FragmentPrefabPaths =
        {
            "Assets/Prefabs/Wasp/WaspRemains/WaspBody Variant.prefab",
            "Assets/Prefabs/Wasp/WaspRemains/WaspHead Variant.prefab",
            "Assets/Prefabs/Wasp/WaspRemains/WaspWing1 Variant.prefab",
            "Assets/Prefabs/Wasp/WaspRemains/WaspLeg Variant.prefab"
        };

        static readonly string[] AnimatorTriggers =
        {
            "Intro",
            "RangedAttack",
            "PoisonAoE",
            "Charge",
            "Summon",
            "PhaseTransition",
            "Die",
            "Sting",
            "Hit"
        };

        const float VisualScale = 2.6f;
        static readonly int EnemyLayer = LayerMask.NameToLayer("Enemy");

        public static void ExecuteBatch()
        {
            try
            {
                RunInternal();
                Debug.Log("[WaspQueenTestArenaBuilder] Completed successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError("[WaspQueenTestArenaBuilder] Failed: " + ex);
                EditorApplication.Exit(1);
                return;
            }

            EditorApplication.Exit(0);
        }

        [MenuItem("Beavermania/Build/Wasp Queen Test Arena")]
        public static void BuildFromMenu()
        {
            RunInternal();
            Debug.Log("[WaspQueenTestArenaBuilder] Build finished. Check Console for details.");
        }

        [MenuItem("Beavermania/Build/Wasp Queen Sting Setup")]
        public static void EnsureStingAnimatorTrigger()
        {
            LoadOrCreateAnimatorController();
            AssetDatabase.SaveAssets();
            Debug.Log("[WaspQueenTestArenaBuilder] Sting animator trigger ensured on " + AnimatorControllerPath);
        }

        [MenuItem("Beavermania/Build/Wasp Queen Hit Reaction Setup")]
        public static void EnsureHitReactionState()
        {
            AnimatorController controller = LoadOrCreateAnimatorController();
            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;

            AnimatorState hitState = FindAnimatorState(stateMachine, "Hit");
            if (hitState == null)
            {
                hitState = stateMachine.AddState("Hit");
                hitState.motion = LoadHitReactionClip();

                AnimatorStateTransition toHit = stateMachine.AddAnyStateTransition(hitState);
                toHit.AddCondition(AnimatorConditionMode.If, 0f, "Hit");
                toHit.hasExitTime = false;
                toHit.duration = 0.05f;
                toHit.canTransitionToSelf = false;

                AnimatorState defaultState = stateMachine.defaultState;
                if (defaultState != null)
                {
                    AnimatorStateTransition backToDefault = hitState.AddTransition(defaultState);
                    backToDefault.hasExitTime = true;
                    backToDefault.exitTime = 0.85f;
                    backToDefault.duration = 0.1f;
                }
            }
            else if (hitState.motion == null)
            {
                hitState.motion = LoadHitReactionClip();
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Debug.Log("[WaspQueenTestArenaBuilder] Hit reaction state ensured on " + AnimatorControllerPath);
        }

        static AnimatorState FindAnimatorState(AnimatorStateMachine stateMachine, string stateName)
        {
            ChildAnimatorState[] states = stateMachine.states;
            for (int i = 0; i < states.Length; i++)
            {
                if (states[i].state != null && states[i].state.name == stateName)
                    return states[i].state;
            }

            return null;
        }

        static AnimationClip LoadHitReactionClip()
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(BeatenClipFbxPath);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                    return clip;
            }

            return null;
        }

        [MenuItem("Beavermania/Build/Wasp Queen Poison Fog")]
        public static void RebuildPoisonZoneFog()
        {
            GameObject vfxSource = AssetDatabase.LoadAssetAtPath<GameObject>(PoisonCloudVfxPath);
            if (vfxSource == null)
                throw new InvalidOperationException("Missing CFXR poison cloud at " + PoisonCloudVfxPath);

            GameObject root = PrefabUtility.LoadPrefabContents(ZonePrefabPath);
            try
            {
                Transform activeRoot = root.transform.Find("ActiveCloud");
                if (activeRoot == null)
                    throw new InvalidOperationException("ActiveCloud child not found on poison zone prefab.");

                MeshRenderer meshRenderer = activeRoot.GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                    UnityEngine.Object.DestroyImmediate(meshRenderer, true);

                MeshFilter meshFilter = activeRoot.GetComponent<MeshFilter>();
                if (meshFilter != null)
                    UnityEngine.Object.DestroyImmediate(meshFilter, true);

                activeRoot.localScale = Vector3.one;

                for (int i = activeRoot.childCount - 1; i >= 0; i--)
                    UnityEngine.Object.DestroyImmediate(activeRoot.GetChild(i).gameObject);

                GameObject fog = (GameObject)PrefabUtility.InstantiatePrefab(vfxSource, root.scene);
                PrefabUtility.UnpackPrefabInstance(fog, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                fog.name = "PoisonCloudFX";
                fog.transform.SetParent(activeRoot, false);
                fog.transform.localPosition = new Vector3(0f, 0.2f, 0f);
                fog.transform.localRotation = Quaternion.identity;
                fog.transform.localScale = Vector3.one;

                ParticleSystem[] systems = fog.GetComponentsInChildren<ParticleSystem>(true);
                for (int i = 0; i < systems.Length; i++)
                {
                    ParticleSystem ps = systems[i];
                    if (ps == null)
                        continue;

                    ParticleSystem.MainModule main = ps.main;
                    main.loop = true;
                    main.playOnAwake = true;
                }

                WaspQueenPoisonZone zone = root.GetComponent<WaspQueenPoisonZone>();
                if (zone != null)
                {
                    var serialized = new SerializedObject(zone);
                    SerializedProperty scaleProperty = serialized.FindProperty("scaleVisualsToRadius");
                    if (scaleProperty != null)
                        scaleProperty.boolValue = true;

                    SerializedProperty baseRadiusProperty = serialized.FindProperty("activeVisualBaseRadius");
                    if (baseRadiusProperty != null && baseRadiusProperty.floatValue <= 0f)
                        baseRadiusProperty.floatValue = 3f;

                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }

                PrefabUtility.SaveAsPrefabAsset(root, ZonePrefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log("[WaspQueenTestArenaBuilder] Poison fog rebuilt on " + ZonePrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static void RunInternal()
        {
            EnsureFolder(ConfigFolder);
            EnsureFolder(PrefabFolder);

            Material poisonMaterial = LoadOrCreateMaterial(PoisonMaterialPath, new Color(0.45f, 0.95f, 0.15f, 0.85f), true);
            Material telegraphMaterial = LoadOrCreateMaterial(TelegraphMaterialPath, new Color(0.95f, 0.85f, 0.1f, 0.55f), true);
            AnimatorController animatorController = LoadOrCreateAnimatorController();

            WaspQueenConfig config = LoadOrCreateConfig();
            GameObject projectilePrefab = BuildProjectilePrefab(poisonMaterial);
            GameObject zonePrefab = BuildPoisonZonePrefab(poisonMaterial, telegraphMaterial);
            AssignConfigContent(config, projectilePrefab, zonePrefab);

            GameObject bossPrefab = BuildBossPrefab(config, animatorController);
            WireScene(bossPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string folderName = Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(folderName))
                throw new InvalidOperationException("Invalid folder path: " + path);

            if (!AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, folderName);
        }

        static Material LoadOrCreateMaterial(string path, Color color, bool emissive)
        {
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
                return existing;

            Shader shader = Shader.Find("Standard");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Lit");

            Material material = new Material(shader) { color = color };
            if (emissive)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", new Color(color.r, color.g, color.b) * 0.65f);
            }

            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static AnimatorController LoadOrCreateAnimatorController()
        {
            AnimatorController existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimatorControllerPath);
            AnimatorController controller = existing != null
                ? existing
                : AnimatorController.CreateAnimatorControllerAtPath(AnimatorControllerPath);

            foreach (string trigger in AnimatorTriggers)
                AddParameterIfMissing(controller, trigger, AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            if (stateMachine.defaultState == null)
            {
                AnimationClip idleClip = LoadWaspIdleClip();
                AnimatorState idleState = stateMachine.AddState("Idle");
                idleState.motion = idleClip;
                stateMachine.defaultState = idleState;
            }

            EditorUtility.SetDirty(controller);
            return controller;
        }

        static void AddParameterIfMissing(AnimatorController controller, string name, AnimatorControllerParameterType type)
        {
            foreach (AnimatorControllerParameter parameter in controller.parameters)
            {
                if (parameter.name == name)
                    return;
            }

            controller.AddParameter(name, type);
        }

        static AnimationClip LoadWaspIdleClip()
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(WaspIdleClipFbxPath);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                    return clip;
            }

            return null;
        }

        static WaspQueenConfig LoadOrCreateConfig()
        {
            WaspQueenConfig config = AssetDatabase.LoadAssetAtPath<WaspQueenConfig>(ConfigPath);
            if (config != null)
                return config;

            config = ScriptableObject.CreateInstance<WaspQueenConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            return config;
        }

        static void AssignConfigContent(WaspQueenConfig config, GameObject projectilePrefab, GameObject zonePrefab)
        {
            var serialized = new SerializedObject(config);

            serialized.FindProperty("waspPrefab").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>(WaspSourcePrefabPath);
            serialized.FindProperty("poisonProjectilePrefab").objectReferenceValue =
                projectilePrefab.GetComponent<WaspQueenProjectile>();
            serialized.FindProperty("poisonZonePrefab").objectReferenceValue =
                zonePrefab.GetComponent<WaspQueenPoisonZone>();
            serialized.FindProperty("deathExplosionPrefab").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>(ExplosionPrefabPath);

            SerializedProperty fragments = serialized.FindProperty("fragmentPrefabs");
            fragments.arraySize = FragmentPrefabPaths.Length;
            for (int i = 0; i < FragmentPrefabPaths.Length; i++)
            {
                fragments.GetArrayElementAtIndex(i).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<GameObject>(FragmentPrefabPaths[i]);
            }

            // Ground raycast must not detect the boss (Enemy) or Ignore Raycast layers.
            int groundMask = ~0;
            if (EnemyLayer >= 0)
                groundMask &= ~(1 << EnemyLayer);
            groundMask &= ~(1 << LayerMask.NameToLayer("Ignore Raycast"));
            serialized.FindProperty("groundMask").intValue = groundMask;
            serialized.FindProperty("chargeObstructionMask").intValue = 0;

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
        }

        static GameObject BuildProjectilePrefab(Material poisonMaterial)
        {
            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            root.name = "PF_WaspQueen_PoisonProjectile";
            root.transform.localScale = Vector3.one * 0.5f;

            MeshCollider meshCollider = root.GetComponent<MeshCollider>();
            if (meshCollider != null)
                UnityEngine.Object.DestroyImmediate(meshCollider);

            SphereCollider sphereCollider = root.AddComponent<SphereCollider>();
            sphereCollider.isTrigger = true;
            sphereCollider.radius = 0.5f;

            Rigidbody rigidbody = root.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            rigidbody.isKinematic = false;

            ApplyMaterial(root, poisonMaterial);
            root.AddComponent<WaspQueenProjectile>();

            return SavePrefab(root, ProjectilePrefabPath);
        }

        static GameObject BuildPoisonZonePrefab(Material activeMaterial, Material telegraphMaterial)
        {
            GameObject root = new GameObject("PF_WaspQueen_PoisonZone");
            SphereCollider zoneCollider = root.AddComponent<SphereCollider>();
            zoneCollider.isTrigger = true;
            zoneCollider.radius = 4f;
            zoneCollider.enabled = false;

            Transform telegraphRoot = BuildZoneDisc(root.transform, "TelegraphRing", telegraphMaterial, 8f, 0.04f);
            Transform activeRoot = BuildZoneDisc(root.transform, "ActiveCloud", activeMaterial, 8f, 0.08f);
            telegraphRoot.gameObject.SetActive(false);
            activeRoot.gameObject.SetActive(false);

            WaspQueenPoisonZone zone = root.AddComponent<WaspQueenPoisonZone>();
            SetSerializedReference(zone, "zoneCollider", zoneCollider);
            SetSerializedReference(zone, "telegraphRoot", telegraphRoot);
            SetSerializedReference(zone, "activeRoot", activeRoot);

            return SavePrefab(root, ZonePrefabPath);
        }

        static Transform BuildZoneDisc(Transform parent, string name, Material material, float diameter, float height)
        {
            GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = name;
            disc.transform.SetParent(parent, false);
            disc.transform.localPosition = Vector3.zero;
            disc.transform.localScale = new Vector3(diameter, height, diameter);

            Collider collider = disc.GetComponent<Collider>();
            if (collider != null)
                UnityEngine.Object.DestroyImmediate(collider);

            ApplyMaterial(disc, material);
            return disc.transform;
        }

        static GameObject BuildBossPrefab(WaspQueenConfig config, AnimatorController animatorController)
        {
            GameObject root = new GameObject("PF_WaspQueen_Boss");
            root.tag = "Boss";
            if (EnemyLayer >= 0)
                root.layer = EnemyLayer;

            Rigidbody body = root.AddComponent<Rigidbody>();
            body.mass = 60f;
            body.useGravity = false;
            body.isKinematic = true;
            body.constraints = RigidbodyConstraints.FreezeRotation;

            CapsuleCollider bodyCollider = root.AddComponent<CapsuleCollider>();
            bodyCollider.height = 4f;
            bodyCollider.radius = 1.2f;
            bodyCollider.center = new Vector3(0f, 2f, 0f);

            Animator animator = BuildVisual(root.transform, animatorController);

            Transform projectileSpawn = CreateChild(root.transform, "ProjectileSpawnPoint", new Vector3(0f, 2.3f, 2.0f));
            Transform aoeOrigin = CreateChild(root.transform, "AoEOrigin", new Vector3(0f, 0.05f, 0f));
            Transform chargeAnchor = CreateChild(root.transform, "ChargeDirectionAnchor", new Vector3(0f, 1.6f, 1.6f));
            Transform waspSpawn1 = CreateChild(root.transform, "WaspSpawnPoint_01", new Vector3(2.6f, 1.0f, -1.0f));
            Transform waspSpawn2 = CreateChild(root.transform, "WaspSpawnPoint_02", new Vector3(-2.6f, 1.0f, -1.0f));
            Transform waspSpawn3 = CreateChild(root.transform, "WaspSpawnPoint_03", new Vector3(0f, 2.6f, -2.0f));
            Transform waspSpawn4 = CreateChild(root.transform, "WaspSpawnPoint_04", new Vector3(0f, 0.6f, -2.6f));
            CreateChild(root.transform, "VFX_ChestOrCore", new Vector3(0f, 2.0f, 0.55f));
            CreateChild(root.transform, "VFX_Stinger", new Vector3(0f, 1.0f, -1.2f));
            CreateChild(root.transform, "VFX_Wings", new Vector3(0f, 2.6f, -0.4f));

            GameObject chargeHitboxObject = new GameObject("ChargeHitbox");
            chargeHitboxObject.transform.SetParent(root.transform, false);
            chargeHitboxObject.transform.localPosition = new Vector3(0f, 1.6f, 1.6f);
            if (EnemyLayer >= 0)
                chargeHitboxObject.layer = EnemyLayer;
            SphereCollider chargeHitbox = chargeHitboxObject.AddComponent<SphereCollider>();
            chargeHitbox.isTrigger = true;
            chargeHitbox.radius = 1.5f;
            chargeHitbox.enabled = false;

            NPC_Health npcHealth = root.AddComponent<NPC_Health>();
            EnemyHealthBarVisibility healthBarVisibility = root.AddComponent<EnemyHealthBarVisibility>();
            AudioSource audioSource = root.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;

            WaspQueenChargeAttack chargeAttack = root.AddComponent<WaspQueenChargeAttack>();
            SetSerializedReference(chargeAttack, "chargeOrigin", chargeAnchor);

            WaspQueenBoss boss = root.AddComponent<WaspQueenBoss>();

            (Slider healthSlider, Canvas healthCanvas) = CreateBossHealthBar(root.transform);
            npcHealth.NPCslider = healthSlider;
            SetSerializedReference(healthBarVisibility, "healthBarCanvas", healthCanvas);

            boss.Config = config;
            boss.Body = body;
            boss.Animator = animator;
            boss.HealthBar = npcHealth;
            boss.ProjectileSpawnPoint = projectileSpawn;
            boss.AoeOrigin = aoeOrigin;
            boss.WaspSpawnPoints = new[] { waspSpawn1, waspSpawn2, waspSpawn3, waspSpawn4 };
            boss.ChargeAttack = chargeAttack;
            boss.AudioSource = audioSource;
            boss.ChargeHitbox = chargeHitbox;
            boss.ActivateOnStart = false;

            return SavePrefab(root, BossPrefabPath);
        }

        static Animator BuildVisual(Transform parent, AnimatorController animatorController)
        {
            GameObject waspSource = AssetDatabase.LoadAssetAtPath<GameObject>(WaspSourcePrefabPath);
            if (waspSource == null)
                throw new InvalidOperationException("Missing LVL1 Wasp prefab at " + WaspSourcePrefabPath);

            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(waspSource);
            PrefabUtility.UnpackPrefabInstance(visual, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            visual.name = "Visual";
            visual.transform.SetParent(parent, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = waspSource.transform.localScale * VisualScale;

            StripGameplayComponents(visual);

            Animator animator = visual.GetComponentInChildren<Animator>();
            if (animator == null)
                animator = visual.AddComponent<Animator>();

            animator.runtimeAnimatorController = animatorController;
            return animator;
        }

        static void StripGameplayComponents(GameObject visual)
        {
            RemoveComponents<NPC_Basic>(visual);
            RemoveComponents<NPC_Audio>(visual);
            RemoveComponents<NPC_Health>(visual);
            RemoveComponents<EnemyHealthBarVisibility>(visual);
            RemoveComponents<Rigidbody>(visual);
            RemoveComponents<Collider>(visual);
        }

        static void RemoveComponents<T>(GameObject root) where T : Component
        {
            T[] components = root.GetComponentsInChildren<T>(true);
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == null)
                    continue;

                try
                {
                    UnityEngine.Object.DestroyImmediate(components[i], true);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[WaspQueenTestArenaBuilder] Could not remove {typeof(T).Name}: {ex.Message}");
                }
            }
        }

        static (Slider slider, Canvas canvas) CreateBossHealthBar(Transform bossRoot)
        {
            Transform anchor = CreateChild(bossRoot, "HealthBarAnchor", new Vector3(0f, 4.6f, 0f));

            GameObject pivot = new GameObject("HealthBarPivot");
            pivot.transform.SetParent(anchor, false);
            ApplyLayerRecursively(pivot, EnemyLayer);

            GameObject canvasObject = new GameObject(
                "Canvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(pivot.transform, false);
            canvasObject.transform.localScale = Vector3.one * 0.01f;

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(400f, 60f);

            CanvasScaler canvasScaler = canvasObject.GetComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            canvasScaler.dynamicPixelsPerUnit = 1f;

            GameObject sliderObject = DefaultControls.CreateSlider(new DefaultControls.Resources());
            sliderObject.name = "WaspQueenHPBar";
            sliderObject.transform.SetParent(canvasObject.transform, false);

            RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0.5f, 0.5f);
            sliderRect.anchorMax = new Vector2(0.5f, 0.5f);
            sliderRect.pivot = new Vector2(0.5f, 0.5f);
            sliderRect.anchoredPosition = Vector2.zero;
            sliderRect.sizeDelta = new Vector2(380f, 40f);

            Slider slider = sliderObject.GetComponent<Slider>();
            slider.interactable = false;
            slider.transition = Selectable.Transition.None;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;

            Image fillImage = slider.fillRect != null ? slider.fillRect.GetComponent<Image>() : null;
            if (fillImage != null)
                fillImage.color = new Color(0.9f, 1f, 0f, 1f);

            ApplyLayerRecursively(canvasObject, EnemyLayer);
            canvas.enabled = false;

            return (slider, canvas);
        }

        static void ApplyLayerRecursively(GameObject target, int layer)
        {
            if (target == null || layer < 0)
                return;

            target.layer = layer;
            for (int i = 0; i < target.transform.childCount; i++)
                ApplyLayerRecursively(target.transform.GetChild(i).gameObject, layer);
        }

        static Transform CreateChild(Transform parent, string name, Vector3 localPosition)
        {
            Transform child = new GameObject(name).transform;
            child.SetParent(parent, false);
            child.localPosition = localPosition;
            child.localRotation = Quaternion.identity;
            child.localScale = Vector3.one;
            return child;
        }

        static void WireScene(GameObject bossPrefab)
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            DeactivateExistingShadowRevenant();
            RemoveExistingWaspQueen();

            GameObject bossInstance = PrefabUtility.InstantiatePrefab(bossPrefab, scene) as GameObject;
            if (bossInstance == null)
                throw new InvalidOperationException("Failed to instantiate Wasp Queen prefab into scene.");

            Vector3 spawnPosition = ResolveArenaSpawnPosition(new Vector3(0f, 0f, 12f));
            bossInstance.transform.position = spawnPosition;
            bossInstance.transform.rotation = Quaternion.LookRotation(Vector3.back, Vector3.up);

            EnsureSceneBinder();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        static Vector3 ResolveArenaSpawnPosition(Vector3 desired)
        {
            Vector3 origin = new Vector3(desired.x, desired.y + 50f, desired.z);
            int mask = ~0;
            if (EnemyLayer >= 0)
                mask &= ~(1 << EnemyLayer);

            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 200f, mask, QueryTriggerInteraction.Ignore))
                return new Vector3(desired.x, hit.point.y + 1.5f, desired.z);

            return new Vector3(desired.x, Mathf.Max(desired.y, 1.5f), desired.z);
        }

        static void DeactivateExistingShadowRevenant()
        {
            ShadowRevenantController[] controllers = UnityEngine.Object.FindObjectsOfType<ShadowRevenantController>(true);
            for (int i = 0; i < controllers.Length; i++)
            {
                if (controllers[i] != null)
                    controllers[i].gameObject.SetActive(false);
            }
        }

        static void RemoveExistingWaspQueen()
        {
            WaspQueenBoss[] bosses = UnityEngine.Object.FindObjectsOfType<WaspQueenBoss>(true);
            for (int i = 0; i < bosses.Length; i++)
            {
                if (bosses[i] != null)
                    UnityEngine.Object.DestroyImmediate(bosses[i].gameObject);
            }
        }

        static void EnsureSceneBinder()
        {
            if (UnityEngine.Object.FindObjectOfType<ShadowRevenantTestSceneBinder>(true) != null)
                return;

            GameObject binderObject = new GameObject("ShadowRevenantTestSceneBinder");
            binderObject.AddComponent<ShadowRevenantTestSceneBinder>();
        }

        static GameObject SavePrefab(GameObject temporaryRoot, string path)
        {
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(temporaryRoot, path);
            UnityEngine.Object.DestroyImmediate(temporaryRoot);
            return saved;
        }

        static void ApplyMaterial(GameObject target, Material material)
        {
            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer != null && material != null)
                renderer.sharedMaterial = material;
        }

        static void SetSerializedReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            var serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
                throw new InvalidOperationException($"Missing serialized property '{propertyName}' on {target.GetType().Name}.");

            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
