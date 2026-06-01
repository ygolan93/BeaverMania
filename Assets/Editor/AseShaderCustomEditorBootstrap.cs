using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Beavermania.EditorTools
{
    /// <summary>
    /// Reimports ASE-exported shaders once so Unity binds CustomEditor "ASEMaterialInspector"
    /// after the fallback inspector assembly is available (avoids import-order console errors).
    /// </summary>
    [InitializeOnLoad]
    public static class AseShaderCustomEditorBootstrap
    {
        const string EditorPrefKey = "Beavermania.AseMaterialInspectorStub.Initialized";

        static readonly string[] AseShaderPaths =
        {
            "Assets/Eden/Assets/PolygonNatureBiomes/PNB_Core/Shaders/SyntyStudios_WaterShader.shader",
            "Assets/Eden/Assets/PolygonNatureBiomes/PNB_Core/Shaders/SyntyStudios_CloudShader.shader",
            "Assets/Eden/Assets/PolygonNatureBiomes/PNB_Core/Shaders/SyntyStudios_CloudShader_NoFog.shader",
            "Assets/Eden/Assets/PolygonNatureBiomes/PNB_Core/Shaders/SyntyStudios_Skybox.shader",
            "Assets/Eden/Assets/PolygonNatureBiomes/PNB_Core/Shaders/SyntyStudios_Skybox_NoFog.shader",
            "Assets/Eden/Assets/PolygonNatureBiomes/PNB_Core/Shaders/SyntyStudios_Triplanar_Basic.shader",
            "Assets/Eden/Assets/PolygonNatureBiomes/PNB_Core/Shaders/SyntyStudios_VegitationShader.shader",
            "Assets/Eden/Assets/PolygonNatureBiomes/PNB_Core/Shaders/SyntyStudios_Basic_LOD_Shader.shader",
            "Assets/External Packages/Fantasy Forest Environment Free Sample/StandardNoCulling.shader",
        };

        static AseShaderCustomEditorBootstrap()
        {
            EditorApplication.delayCall += RunOnceAfterCompile;
        }

        [MenuItem("Beavermania/Fix/Refresh ASE material inspectors")]
        public static void RefreshFromMenu()
        {
            EditorPrefs.DeleteKey(EditorPrefKey);
            ReimportAseShaders(forceLog: true);
        }

        static void RunOnceAfterCompile()
        {
            if (EditorPrefs.GetBool(EditorPrefKey, false))
                return;

            ReimportAseShaders(forceLog: false);
            EditorPrefs.SetBool(EditorPrefKey, true);
        }

        static void ReimportAseShaders(bool forceLog)
        {
            var reimported = new List<string>();

            foreach (var path in AseShaderPaths)
            {
                if (AssetDatabase.LoadAssetAtPath<Shader>(path) == null)
                    continue;

                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                reimported.Add(path);
            }

            if (forceLog || reimported.Count > 0)
            {
                Debug.Log("[AseShaderCustomEditorBootstrap] Reimported "
                    + reimported.Count
                    + " ASE shader(s) so CustomEditor ASEMaterialInspector can bind.");
            }
        }
    }
}
