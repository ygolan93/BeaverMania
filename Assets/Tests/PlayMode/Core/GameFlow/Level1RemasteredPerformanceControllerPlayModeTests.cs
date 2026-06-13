using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Beavermania.Tests.PlayMode.Display
{
    public sealed class Level1RemasteredPerformanceControllerPlayModeTests
    {
        readonly Dictionary<string, Type> cachedRuntimeTypes = new(StringComparer.Ordinal);
        readonly List<UnityEngine.Object> spawnedObjects = new();

        int originalQualityLevel;
        float originalShadowDistance;

        [SetUp]
        public void SetUp()
        {
            originalQualityLevel = QualitySettings.GetQualityLevel();
            originalShadowDistance = QualitySettings.shadowDistance;
        }

        [TearDown]
        public void TearDown()
        {
            QualitySettings.SetQualityLevel(originalQualityLevel, true);
            QualitySettings.shadowDistance = originalShadowDistance;

            for (int index = spawnedObjects.Count - 1; index >= 0; index--)
            {
                if (spawnedObjects[index] != null)
                    UnityEngine.Object.DestroyImmediate(spawnedObjects[index]);
            }

            spawnedObjects.Clear();
        }

        [UnityTest]
        public IEnumerator OnEnable_AppliesBalancedTier_AndOnDisable_RestoresSnapshots()
        {
            int ultraQuality = FindQualityLevel("Ultra");
            QualitySettings.SetQualityLevel(ultraQuality, true);
            QualitySettings.shadowDistance = 91f;

            Terrain firstTerrain = CreateTerrain("Terrain_A");
            ConfigureTerrain(firstTerrain, 333f, 44f, 99, 22f, 0.88f, 2f, 777f);

            Terrain secondTerrain = CreateTerrain("Terrain_B");
            ConfigureTerrain(secondTerrain, 287f, 40f, 88, 18f, 0.5f, 4f, 650f);

            GameObject highCluster = CreateTrackedGameObject("HighCluster");
            highCluster.SetActive(true);
            GameObject proxyCluster = CreateTrackedGameObject("ProxyCluster");
            proxyCluster.SetActive(false);

            ScriptableObject profile = CreateProfile();
            GameObject controllerObject = CreateTrackedGameObject("PerformanceController");
            controllerObject.SetActive(false);

            Component controller = controllerObject.AddComponent(ResolveRuntimeType("Beavermania.Display.Level1RemasteredPerformanceController"));
            AssignControllerFields(
                controller,
                profile,
                new[] { firstTerrain, secondTerrain },
                highCluster,
                proxyCluster);

            controllerObject.SetActive(true);
            yield return null;

            Assert.That(QualitySettings.names[QualitySettings.GetQualityLevel()], Is.EqualTo("Medium"));
            Assert.That(QualitySettings.shadowDistance, Is.EqualTo(30f).Within(0.001f));
            Assert.That(highCluster.activeSelf, Is.False);
            Assert.That(proxyCluster.activeSelf, Is.True);

            AssertTerrain(firstTerrain, 180f, 28f, 16, 12f, 0.75f, 8f, 500f);
            AssertTerrain(secondTerrain, 180f, 28f, 16, 12f, 0.75f, 8f, 500f);

            ((Behaviour)controller).enabled = false;
            yield return null;

            Assert.That(QualitySettings.GetQualityLevel(), Is.EqualTo(ultraQuality));
            Assert.That(QualitySettings.shadowDistance, Is.EqualTo(91f).Within(0.001f));
            Assert.That(highCluster.activeSelf, Is.True);
            Assert.That(proxyCluster.activeSelf, Is.False);

            AssertTerrain(firstTerrain, 333f, 44f, 99, 22f, 0.88f, 2f, 777f);
            AssertTerrain(secondTerrain, 287f, 40f, 88, 18f, 0.5f, 4f, 650f);
        }

        [UnityTest]
        public IEnumerator OnDestroy_RestoresGlobalQualitySettings()
        {
            int ultraQuality = FindQualityLevel("Ultra");
            QualitySettings.SetQualityLevel(ultraQuality, true);
            QualitySettings.shadowDistance = 82f;

            Terrain terrain = CreateTerrain("Terrain_Destroy");
            ConfigureTerrain(terrain, 260f, 38f, 64, 14f, 0.42f, 6f, 540f);

            GameObject highCluster = CreateTrackedGameObject("HighCluster_Destroy");
            GameObject proxyCluster = CreateTrackedGameObject("ProxyCluster_Destroy");

            ScriptableObject profile = CreateProfile();
            GameObject controllerObject = CreateTrackedGameObject("PerformanceController_Destroy");
            controllerObject.SetActive(false);

            Component controller = controllerObject.AddComponent(ResolveRuntimeType("Beavermania.Display.Level1RemasteredPerformanceController"));
            AssignControllerFields(
                controller,
                profile,
                new[] { terrain },
                highCluster,
                proxyCluster);

            controllerObject.SetActive(true);
            yield return null;

            UnityEngine.Object.Destroy(controllerObject);
            yield return null;

            Assert.That(QualitySettings.GetQualityLevel(), Is.EqualTo(ultraQuality));
            Assert.That(QualitySettings.shadowDistance, Is.EqualTo(82f).Within(0.001f));
            AssertTerrain(terrain, 260f, 38f, 64, 14f, 0.42f, 6f, 540f);
        }

        [UnityTest]
        public IEnumerator MissingReferences_LogOnceAndLeaveCurrentSettingsUntouched()
        {
            int fastQuality = FindQualityLevel("Fast");
            QualitySettings.SetQualityLevel(fastQuality, true);
            QualitySettings.shadowDistance = 19f;

            LogAssert.Expect(LogType.Warning, new Regex("Level1RemasteredPerformanceController disabled:"));

            GameObject controllerObject = CreateTrackedGameObject("PerformanceController_Missing");
            controllerObject.SetActive(false);
            controllerObject.AddComponent(ResolveRuntimeType("Beavermania.Display.Level1RemasteredPerformanceController"));

            controllerObject.SetActive(true);
            yield return null;
            yield return null;

            Assert.That(QualitySettings.GetQualityLevel(), Is.EqualTo(fastQuality));
            Assert.That(QualitySettings.shadowDistance, Is.EqualTo(19f).Within(0.001f));
            LogAssert.NoUnexpectedReceived();
        }

        GameObject CreateTrackedGameObject(string name)
        {
            var go = new GameObject(name);
            spawnedObjects.Add(go);
            return go;
        }

        Terrain CreateTerrain(string name)
        {
            GameObject go = CreateTrackedGameObject(name);
            var terrainData = new TerrainData();
            spawnedObjects.Add(terrainData);

            Terrain terrain = go.AddComponent<Terrain>();
            terrain.terrainData = terrainData;
            return terrain;
        }

        ScriptableObject CreateProfile()
        {
            Type profileType = ResolveRuntimeType("Beavermania.Data.Display.PerformanceBudgetProfile");
            Type tierDefinitionType = ResolveRuntimeType("Beavermania.Data.Display.PerformanceBudgetProfile+TierDefinition");

            var profile = ScriptableObject.CreateInstance(profileType);
            spawnedObjects.Add(profile);

            object high = Activator.CreateInstance(tierDefinitionType);
            object balanced = Activator.CreateInstance(tierDefinitionType);
            object fast = Activator.CreateInstance(tierDefinitionType);
            object canopySafe = Activator.CreateInstance(tierDefinitionType);

            ConfigureTier(high, "High", "Medium", true, false, 200f, 30f, 25, 20f, 1f, 5f, 600f, 40f);
            ConfigureTier(balanced, "Balanced", "Medium", false, true, 180f, 28f, 16, 12f, 0.75f, 8f, 500f, 30f);
            ConfigureTier(fast, "Fast", "Fast", false, true, 140f, 22f, 10, 4f, 0.5f, 12f, 400f, 18f);
            ConfigureTier(canopySafe, "CanopySafe", "Fast", false, false, 110f, 18f, 6, 0f, 0.25f, 15f, 300f, 12f);

            Array tiers = Array.CreateInstance(tierDefinitionType, 4);
            tiers.SetValue(high, 0);
            tiers.SetValue(balanced, 1);
            tiers.SetValue(fast, 2);
            tiers.SetValue(canopySafe, 3);

            SetFieldValue(profile, "startingTierIndex", 1);
            SetFieldValue(profile, "rollingWindowSize", 60);
            SetFieldValue(profile, "degradeAverageFrameTimeMs", 17.2f);
            SetFieldValue(profile, "degradeConsecutiveFrames", 45);
            SetFieldValue(profile, "recoverAverageFrameTimeMs", 14.9f);
            SetFieldValue(profile, "recoverConsecutiveFrames", 300);
            SetFieldValue(profile, "tierChangeCooldownSeconds", 1f);
            SetFieldValue(profile, "tiers", tiers);

            return profile;
        }

        Type ResolveRuntimeType(string fullName)
        {
            if (cachedRuntimeTypes.TryGetValue(fullName, out Type cachedType))
                return cachedType;

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type resolvedType = assembly.GetType(fullName, throwOnError: false);
                if (resolvedType != null)
                {
                    cachedRuntimeTypes[fullName] = resolvedType;
                    return resolvedType;
                }
            }

            throw new InvalidOperationException($"Failed to resolve runtime type '{fullName}'.");
        }

        static void ConfigureTerrain(
            Terrain terrain,
            float treeDistance,
            float billboardDistance,
            int treeMaximumFullLodCount,
            float detailObjectDistance,
            float detailObjectDensity,
            float heightmapPixelError,
            float splatMapDistance)
        {
            terrain.treeDistance = treeDistance;
            terrain.treeBillboardDistance = billboardDistance;
            terrain.treeMaximumFullLODCount = treeMaximumFullLodCount;
            terrain.detailObjectDistance = detailObjectDistance;
            terrain.detailObjectDensity = detailObjectDensity;
            terrain.heightmapPixelError = heightmapPixelError;
            terrain.basemapDistance = splatMapDistance;
        }

        static void AssertTerrain(
            Terrain terrain,
            float treeDistance,
            float billboardDistance,
            int treeMaximumFullLodCount,
            float detailObjectDistance,
            float detailObjectDensity,
            float heightmapPixelError,
            float splatMapDistance)
        {
            Assert.That(terrain.treeDistance, Is.EqualTo(treeDistance).Within(0.001f));
            Assert.That(terrain.treeBillboardDistance, Is.EqualTo(billboardDistance).Within(0.001f));
            Assert.That(terrain.treeMaximumFullLODCount, Is.EqualTo(treeMaximumFullLodCount));
            Assert.That(terrain.detailObjectDistance, Is.EqualTo(detailObjectDistance).Within(0.001f));
            Assert.That(terrain.detailObjectDensity, Is.EqualTo(detailObjectDensity).Within(0.001f));
            Assert.That(terrain.heightmapPixelError, Is.EqualTo(heightmapPixelError).Within(0.001f));
            Assert.That(terrain.basemapDistance, Is.EqualTo(splatMapDistance).Within(0.001f));
        }

        static void ConfigureTier(
            object boxedTier,
            string label,
            string qualityLevelName,
            bool highClusterEnabled,
            bool proxyClusterEnabled,
            float treeDistance,
            float billboardDistance,
            int treeMaximumFullLodCount,
            float detailObjectDistance,
            float detailObjectDensity,
            float heightmapPixelError,
            float splatMapDistance,
            float shadowDistance)
        {
            SetFieldValue(boxedTier, "label", label);
            SetFieldValue(boxedTier, "qualityLevelName", qualityLevelName);
            SetFieldValue(boxedTier, "highClusterEnabled", highClusterEnabled);
            SetFieldValue(boxedTier, "proxyClusterEnabled", proxyClusterEnabled);
            SetFieldValue(boxedTier, "treeDistance", treeDistance);
            SetFieldValue(boxedTier, "billboardDistance", billboardDistance);
            SetFieldValue(boxedTier, "treeMaximumFullLodCount", treeMaximumFullLodCount);
            SetFieldValue(boxedTier, "detailObjectDistance", detailObjectDistance);
            SetFieldValue(boxedTier, "detailObjectDensity", detailObjectDensity);
            SetFieldValue(boxedTier, "heightmapPixelError", heightmapPixelError);
            SetFieldValue(boxedTier, "splatMapDistance", splatMapDistance);
            SetFieldValue(boxedTier, "shadowDistance", shadowDistance);
        }

        static void AssignControllerFields(
            Component controller,
            ScriptableObject profile,
            Terrain[] terrains,
            GameObject highCluster,
            GameObject proxyCluster)
        {
            SetFieldValue(controller, "profile", profile);
            SetFieldValue(controller, "cachedTerrains", terrains);
            SetFieldValue(controller, "perfCanopyClusterHigh", highCluster);
            SetFieldValue(controller, "perfCanopyClusterProxy", proxyCluster);
        }

        static int FindQualityLevel(string qualityLevelName)
        {
            string[] names = QualitySettings.names;
            for (int index = 0; index < names.Length; index++)
            {
                if (names[index] == qualityLevelName)
                    return index;
            }

            throw new AssertionException($"Missing quality level '{qualityLevelName}'.");
        }

        static void SetFieldValue(object target, string fieldName, object value)
        {
            FieldInfo fieldInfo = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (fieldInfo == null)
                throw new MissingFieldException(target.GetType().FullName, fieldName);

            fieldInfo.SetValue(target, value);
        }
    }
}
