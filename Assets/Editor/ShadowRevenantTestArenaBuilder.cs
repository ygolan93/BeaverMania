using System;
using System.IO;
using Beavermania.Data.NPC;
using Beavermania.NPC;
using Beavermania.Objects;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Beavermania.EditorTools
{
    /// <summary>
    /// Builds Shadow Revenant placeholder prefabs, config asset, and wires the test arena scene.
    /// Batch: Unity -executeMethod Beavermania.EditorTools.ShadowRevenantTestArenaBuilder.ExecuteBatch
    /// </summary>
    public static class ShadowRevenantTestArenaBuilder
    {
        const string ScenePath = "Assets/Scenes/ShadowRevenantTestArena.unity";
        const string ConfigPath = "Assets/Data/NPC/ShadowRevenant/ShadowRevenantConfig.asset";
        const string PrefabFolder = "Assets/Prefabs/NPC/ShadowRevenant";
        const string BossPrefabPath = PrefabFolder + "/ShadowRevenant.prefab";
        const string ProjectilePrefabPath = PrefabFolder + "/ShadowRevenantProjectile.prefab";
        const string FogPrefabPath = PrefabFolder + "/ShadowRevenantDreadFogZone.prefab";
        const string ShadePrefabPath = PrefabFolder + "/ShadowRevenantShadeMinion.prefab";
        const string AnimatorControllerPath = PrefabFolder + "/ShadowRevenant.controller";
        const string PlaceholderMaterialPath = PrefabFolder + "/ShadowRevenantPlaceholder.mat";

        static readonly int EnemyLayer = LayerMask.NameToLayer("Enemy");

        public static void ExecuteBatch()
        {
            try
            {
                RunInternal();
                Debug.Log("[ShadowRevenantTestArenaBuilder] Completed successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError("[ShadowRevenantTestArenaBuilder] Failed: " + ex);
                EditorApplication.Exit(1);
                return;
            }

            EditorApplication.Exit(0);
        }

        public static void ExecuteBatchHealthBarOnly()
        {
            try
            {
                RebuildBossHealthBarOnly();
                Debug.Log("[ShadowRevenantTestArenaBuilder] Boss health bar rebuild completed.");
            }
            catch (Exception ex)
            {
                Debug.LogError("[ShadowRevenantTestArenaBuilder] Health bar rebuild failed: " + ex);
                EditorApplication.Exit(1);
                return;
            }

            EditorApplication.Exit(0);
        }

        [MenuItem("Beavermania/Build/Shadow Revenant Boss Health Bar")]
        public static void RebuildBossHealthBarOnly()
        {
            if (!AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath))
                throw new InvalidOperationException("Missing boss prefab at " + BossPrefabPath);

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(BossPrefabPath);
            try
            {
                Transform existingAnchor = prefabRoot.transform.Find("HealthBarAnchor");
                if (existingAnchor != null)
                    UnityEngine.Object.DestroyImmediate(existingAnchor.gameObject);

                ShadowRevenantController controller = prefabRoot.GetComponent<ShadowRevenantController>();
                if (controller == null)
                    throw new InvalidOperationException("ShadowRevenantController missing on boss prefab.");

                NPC_Health npcHealth = prefabRoot.GetComponent<NPC_Health>();
                if (npcHealth == null)
                    npcHealth = prefabRoot.AddComponent<NPC_Health>();

                EnemyHealthBarVisibility healthBarVisibility = prefabRoot.GetComponent<EnemyHealthBarVisibility>();
                if (healthBarVisibility == null)
                    healthBarVisibility = prefabRoot.AddComponent<EnemyHealthBarVisibility>();

                (Slider healthSlider, Canvas healthCanvas) = CreateBossHealthBar(prefabRoot.transform, EnemyLayer);
                npcHealth.NPCslider = healthSlider;
                SetSerializedReference(healthBarVisibility, "healthBarCanvas", healthCanvas);
                SetSerializedReference(healthBarVisibility, "shadowRevenant", controller);
                SetSerializedReference(controller, "healthBar", npcHealth);

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, BossPrefabPath);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        [MenuItem("Beavermania/Build/Shadow Revenant Test Arena")]
        public static void ExecuteFromMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "Build Shadow Revenant Test Arena",
                    "Create/update Shadow Revenant prefabs, config, and place boss in ShadowRevenantTestArena?",
                    "Build",
                    "Cancel"))
                return;

            RunInternal();
            EditorUtility.DisplayDialog("Shadow Revenant Test Arena", "Build finished. Check Console for details.", "OK");
        }

        static void RunInternal()
        {
            EnsureFolder("Assets/Data/NPC/ShadowRevenant");
            EnsureFolder(PrefabFolder);

            Material placeholderMaterial = LoadOrCreatePlaceholderMaterial();
            AnimatorController animatorController = LoadOrCreateAnimatorController();

            ShadowRevenantConfig config = LoadOrCreateConfig();
            GameObject projectilePrefab = BuildProjectilePrefab(placeholderMaterial);
            GameObject fogPrefab = BuildFogPrefab(placeholderMaterial);
            GameObject shadePrefab = BuildShadePrefab(placeholderMaterial);
            AssignConfigPrefabs(config, projectilePrefab, fogPrefab, shadePrefab);

            GameObject bossPrefab = BuildBossPrefab(config, animatorController, placeholderMaterial);
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

        static AnimatorController LoadOrCreateAnimatorController()
        {
            var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimatorControllerPath);
            if (existing != null)
                return existing;

            var controller = AnimatorController.CreateAnimatorControllerAtPath(AnimatorControllerPath);
            AddParameterIfMissing(controller, "Phased", AnimatorControllerParameterType.Bool);
            AddParameterIfMissing(controller, "Attack", AnimatorControllerParameterType.Trigger);
            AddParameterIfMissing(controller, "Stagger", AnimatorControllerParameterType.Trigger);
            AddParameterIfMissing(controller, "Summon", AnimatorControllerParameterType.Trigger);
            AddParameterIfMissing(controller, "Dead", AnimatorControllerParameterType.Trigger);
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

        static ShadowRevenantConfig LoadOrCreateConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<ShadowRevenantConfig>(ConfigPath);
            if (config != null)
                return config;

            config = ScriptableObject.CreateInstance<ShadowRevenantConfig>();
            config.teleportGroundMask = LayerMask.GetMask("Default");
            config.teleportObstructionMask = 0;
            AssetDatabase.CreateAsset(config, ConfigPath);
            return config;
        }

        static void AssignConfigPrefabs(
            ShadowRevenantConfig config,
            GameObject projectilePrefab,
            GameObject fogPrefab,
            GameObject shadePrefab)
        {
            config.projectilePrefab = projectilePrefab.GetComponent<ShadowRevenantProjectile>();
            config.fogPrefab = fogPrefab.GetComponent<ShadowRevenantDreadFogZone>();
            config.shadeMinionPrefab = shadePrefab.GetComponent<ShadowRevenantShadeMinion>();
            EditorUtility.SetDirty(config);
        }

        static GameObject BuildProjectilePrefab(Material material)
        {
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

        static GameObject BuildFogPrefab(Material material)
        {
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
            SetSerializedReference(fog, "fogCollider", fogCollider);
            SetSerializedReference(fog, "visualRoot", visualRoot.transform);
            return SavePrefab(root, FogPrefabPath);
        }

        static GameObject BuildShadePrefab(Material material)
        {
            var root = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            root.name = "ShadowRevenantShadeMinion";
            root.transform.localScale = new Vector3(0.6f, 0.8f, 0.6f);
            UnityEngine.Object.DestroyImmediate(root.GetComponent<MeshCollider>());

            var capsuleCollider = root.AddComponent<CapsuleCollider>();
            capsuleCollider.isTrigger = true;
            capsuleCollider.height = 2f;
            capsuleCollider.radius = 0.5f;

            var rigidbody = root.AddComponent<Rigidbody>();
            rigidbody.useGravity = false;
            rigidbody.isKinematic = false;
            rigidbody.constraints = RigidbodyConstraints.FreezeRotation;

            ApplyMaterial(root, material);
            root.AddComponent<ShadowRevenantShadeMinion>();
            return SavePrefab(root, ShadePrefabPath);
        }

        static GameObject BuildBossPrefab(
            ShadowRevenantConfig config,
            AnimatorController animatorController,
            Material material)
        {
            var root = new GameObject("ShadowRevenant");
            root.tag = "Boss";
            if (EnemyLayer >= 0)
                root.layer = EnemyLayer;

            var body = root.AddComponent<Rigidbody>();
            body.mass = 80f;
            body.useGravity = false;
            body.isKinematic = true;
            body.constraints = RigidbodyConstraints.FreezeRotation;

            var bodyCollider = root.AddComponent<CapsuleCollider>();
            bodyCollider.height = 4f;
            bodyCollider.radius = 1.4f;
            bodyCollider.center = new Vector3(0f, 2f, 0f);

            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = new Vector3(0f, 2f, 0f);
            visual.transform.localScale = new Vector3(1.8f, 2f, 1.8f);
            UnityEngine.Object.DestroyImmediate(visual.GetComponent<Collider>());
            ApplyMaterial(visual, material);

            var animator = visual.AddComponent<Animator>();
            animator.runtimeAnimatorController = animatorController;

            var projectileMuzzle = CreateChild(root.transform, "ProjectileMuzzle", new Vector3(0f, 2.5f, 1.6f));
            var fogSpawnAnchor = CreateChild(root.transform, "FogSpawnAnchor", new Vector3(0f, 0.2f, 0f));
            var shadeSpawn0 = CreateChild(root.transform, "ShadeSpawnPoint_0", new Vector3(2.5f, 0f, 0f));
            var shadeSpawn1 = CreateChild(root.transform, "ShadeSpawnPoint_1", new Vector3(-1.25f, 0f, 2.2f));
            var shadeSpawn2 = CreateChild(root.transform, "ShadeSpawnPoint_2", new Vector3(-1.25f, 0f, -2.2f));
            var poolRoot = CreateChild(root.transform, "PoolRoot", Vector3.zero);

            var poolHub = root.AddComponent<ShadowRevenantPoolHub>();
            var controller = root.AddComponent<ShadowRevenantController>();
            var npcHealth = root.AddComponent<NPC_Health>();
            var healthBarVisibility = root.AddComponent<EnemyHealthBarVisibility>();

            (Slider healthSlider, Canvas healthCanvas) = CreateBossHealthBar(root.transform, EnemyLayer);
            npcHealth.NPCslider = healthSlider;
            SetSerializedReference(healthBarVisibility, "healthBarCanvas", healthCanvas);
            SetSerializedReference(healthBarVisibility, "shadowRevenant", controller);

            SetSerializedReference(poolHub, "config", config);
            SetSerializedReference(poolHub, "poolRoot", poolRoot);

            SetSerializedReference(controller, "config", config);
            SetSerializedReference(controller, "projectileMuzzle", projectileMuzzle);
            SetSerializedReference(controller, "fogSpawnAnchor", fogSpawnAnchor);
            SetSerializedReference(controller, "body", body);
            SetSerializedReference(controller, "animator", animator);
            SetSerializedReference(controller, "healthBar", npcHealth);
            SetSerializedReference(controller, "poolHub", poolHub);
            SetSerializedArray(controller, "shadeSpawnPoints", new[] { shadeSpawn0, shadeSpawn1, shadeSpawn2 });
            SetSerializedArray(controller, "phaseDisabledColliders", new Collider[] { bodyCollider });

            return SavePrefab(root, BossPrefabPath);
        }

        static (Slider slider, Canvas canvas) CreateBossHealthBar(Transform bossRoot, int enemyLayer)
        {
            Transform anchor = CreateChild(bossRoot, "HealthBarAnchor", new Vector3(0f, 4f, 0f));

            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "Sphere";
            sphere.transform.SetParent(anchor, false);
            sphere.transform.localPosition = Vector3.zero;
            sphere.transform.localRotation = Quaternion.identity;
            sphere.transform.localScale = Vector3.one;
            UnityEngine.Object.DestroyImmediate(sphere.GetComponent<SphereCollider>());

            var sphereRenderer = sphere.GetComponent<MeshRenderer>();
            if (sphereRenderer != null)
                sphereRenderer.enabled = false;

            sphere.AddComponent<RotateUI>();
            ApplyLayerRecursively(sphere, enemyLayer);

            var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(sphere.transform, false);
            canvasObject.transform.localPosition = new Vector3(0f, 0f, -0.10415292f);
            canvasObject.transform.localRotation = Quaternion.identity;
            canvasObject.transform.localScale = new Vector3(0.0013639355f, 0.0013639358f, 0.0013639355f);

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            var canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(1255f, 505f);
            canvasRect.anchoredPosition = new Vector2(-0.12442511f, 0.95286345f);

            var canvasScaler = canvasObject.GetComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            canvasScaler.dynamicPixelsPerUnit = 1f;

            ApplyLayerRecursively(canvasObject, enemyLayer);

            GameObject sliderObject = DefaultControls.CreateSlider(new DefaultControls.Resources());
            sliderObject.name = "ShadowRevenantHPBar";
            sliderObject.transform.SetParent(canvasObject.transform, false);

            var sliderRect = sliderObject.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0.5f, 0.5f);
            sliderRect.anchorMax = new Vector2(0.5f, 0.5f);
            sliderRect.pivot = new Vector2(0.5f, 0.5f);
            sliderRect.anchoredPosition = new Vector2(0f, 130.99771f);
            sliderRect.sizeDelta = new Vector2(160f, 20f);
            sliderRect.localScale = new Vector3(4.867748f, 5.2391567f, 4.867748f);

            var slider = sliderObject.GetComponent<Slider>();
            slider.interactable = false;
            slider.transition = Selectable.Transition.None;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;

            Image fillImage = slider.fillRect != null ? slider.fillRect.GetComponent<Image>() : null;
            if (fillImage != null)
                fillImage.color = new Color(0.90128887f, 1f, 0f, 1f);

            ApplyLayerRecursively(sliderObject, enemyLayer);
            canvas.enabled = false;

            return (slider, canvas);
        }

        static void ApplyLayerRecursively(GameObject target, int layer)
        {
            if (target == null || layer < 0)
                return;

            target.layer = layer;
            Transform transform = target.transform;
            for (int i = 0; i < transform.childCount; i++)
                ApplyLayerRecursively(transform.GetChild(i).gameObject, layer);
        }

        static Transform CreateChild(Transform parent, string name, Vector3 localPosition)
        {
            var child = new GameObject(name).transform;
            child.SetParent(parent, false);
            child.localPosition = localPosition;
            child.localRotation = Quaternion.identity;
            child.localScale = Vector3.one;
            return child;
        }

        static void WireScene(GameObject bossPrefab)
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            RemoveExistingBossRoots();

            GameObject bossInstance = PrefabUtility.InstantiatePrefab(bossPrefab, scene) as GameObject;
            if (bossInstance == null)
                throw new InvalidOperationException("Failed to instantiate Shadow Revenant prefab in scene.");

            bossInstance.transform.position = new Vector3(0f, 0f, 12f);
            bossInstance.transform.rotation = Quaternion.LookRotation(Vector3.back, Vector3.up);

            EnsureSceneBinder();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        static void RemoveExistingBossRoots()
        {
            var controllers = UnityEngine.Object.FindObjectsOfType<ShadowRevenantController>(true);
            for (int i = 0; i < controllers.Length; i++)
            {
                if (controllers[i] != null)
                    UnityEngine.Object.DestroyImmediate(controllers[i].gameObject);
            }

            GameObject named = GameObject.Find("ShadowRevenant");
            if (named != null && named.GetComponent<ShadowRevenantController>() == null)
                UnityEngine.Object.DestroyImmediate(named);
        }

        static void EnsureSceneBinder()
        {
            if (UnityEngine.Object.FindObjectOfType<ShadowRevenantTestSceneBinder>(true) != null)
                return;

            var binderObject = new GameObject("ShadowRevenantTestSceneBinder");
            binderObject.AddComponent<ShadowRevenantTestSceneBinder>();
        }

        static GameObject SavePrefab(GameObject temporaryRoot, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                var saved = PrefabUtility.SaveAsPrefabAsset(temporaryRoot, path);
                UnityEngine.Object.DestroyImmediate(temporaryRoot);
                return saved;
            }

            var created = PrefabUtility.SaveAsPrefabAsset(temporaryRoot, path);
            UnityEngine.Object.DestroyImmediate(temporaryRoot);
            return created;
        }

        static void ApplyMaterial(GameObject target, Material material)
        {
            var renderer = target.GetComponent<Renderer>();
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

        static void SetSerializedArray<T>(UnityEngine.Object target, string propertyName, T[] values) where T : UnityEngine.Object
        {
            var serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null || !property.isArray)
                throw new InvalidOperationException($"Missing array property '{propertyName}' on {target.GetType().Name}.");

            property.arraySize = values != null ? values.Length : 0;
            for (int i = 0; i < property.arraySize; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
