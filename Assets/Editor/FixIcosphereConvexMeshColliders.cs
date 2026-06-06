using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Beavermania.EditorTools
{
    /// <summary>
    /// Fixes Play Mode warnings:
    /// "Couldn't create a Convex Mesh from source mesh Icosphere within the maximum polygons limit (256)."
    /// </summary>
    public static class FixIcosphereConvexMeshColliders
    {
        const string BuiltinIcosphereMeshName = "Icosphere";

        const string Level1ScenePath = "Assets/Scenes/Level 1.unity";

        [MenuItem("Beavermania/Fix/Convex Icosphere MeshColliders — Level 1.unity")]
        public static void FixLevel1SceneFromMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "Fix convex Icosphere MeshColliders",
                    "Open Level 1.unity and fix convex MeshColliders on builtin Icosphere meshes?\n\n"
                    + "Unsaved changes in the current scene will be prompted to save.",
                    "Run",
                    "Cancel"))
                return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var scene = EditorSceneManager.OpenScene(Level1ScenePath, OpenSceneMode.Single);
            var log = new StringBuilder();
            int fixedCount = FixScene(scene, log);
            if (fixedCount > 0)
            {
                EditorSceneManager.SaveScene(scene);
                Debug.Log(log.ToString());
            }
            else
            {
                Debug.Log("[FixIcosphereConvexMeshColliders] No convex Icosphere MeshColliders found in " + Level1ScenePath);
            }
        }

        [MenuItem("Beavermania/Fix/Convex Icosphere MeshColliders (active scene, silent)")]
        public static void FixActiveSceneSilent()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogWarning("[FixIcosphereConvexMeshColliders] No active loaded scene.");
                return;
            }

            var log = new StringBuilder();
            int fixedCount = FixScene(scene, log);
            if (fixedCount > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                Debug.Log(log.ToString());
            }
            else
            {
                Debug.Log("[FixIcosphereConvexMeshColliders] No convex Icosphere MeshColliders found in " + scene.path);
            }
        }

        [MenuItem("Beavermania/Fix/Convex Icosphere MeshColliders (active scene)")]
        public static void FixActiveSceneFromMenu()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogWarning("[FixIcosphereConvexMeshColliders] No active loaded scene.");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "Fix convex Icosphere MeshColliders",
                    "Fix convex MeshColliders on Unity's builtin Icosphere mesh in the active scene?\n\n"
                    + "Static objects: MeshCollider.convex = false.\n"
                    + "Objects under a Rigidbody: MeshCollider replaced with SphereCollider.\n\n"
                    + "Scene: " + scene.path,
                    "Run",
                    "Cancel"))
                return;

            var log = new StringBuilder();
            int fixedCount = FixScene(scene, log);
            if (fixedCount > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                Debug.Log(log.ToString());
            }
            else
            {
                Debug.Log("[FixIcosphereConvexMeshColliders] No convex Icosphere MeshColliders found in " + scene.path);
            }
        }

        public static int FixScene(Scene scene, StringBuilder log)
        {
            int fixedCount = 0;
            var roots = scene.GetRootGameObjects();
            for (int r = 0; r < roots.Length; r++)
            {
                MeshCollider[] meshColliders = roots[r].GetComponentsInChildren<MeshCollider>(true);
                for (int i = 0; i < meshColliders.Length; i++)
                {
                    MeshCollider meshCollider = meshColliders[i];
                    if (meshCollider == null || !meshCollider.convex)
                        continue;

                    Mesh mesh = meshCollider.sharedMesh;
                    if (mesh == null || mesh.name != BuiltinIcosphereMeshName)
                        continue;

                    GameObject target = meshCollider.gameObject;
                    Rigidbody body = target.GetComponentInParent<Rigidbody>();
                    string path = BuildPath(target.transform);

                    if (body == null)
                    {
                        meshCollider.convex = false;
                        log.AppendLine("[FixIcosphereConvexMeshColliders] Set non-convex: " + path);
                    }
                    else
                    {
                        SphereCollider sphereCollider = target.GetComponent<SphereCollider>();
                        if (sphereCollider == null)
                            sphereCollider = target.AddComponent<SphereCollider>();

                        sphereCollider.isTrigger = meshCollider.isTrigger;
                        sphereCollider.material = meshCollider.material;
                        sphereCollider.center = Vector3.zero;
                        sphereCollider.radius = 0.5f;
                        Object.DestroyImmediate(meshCollider, true);
                        log.AppendLine("[FixIcosphereConvexMeshColliders] Replaced with SphereCollider: " + path);
                    }

                    fixedCount++;
                }
            }

            log.Insert(0, "[FixIcosphereConvexMeshColliders] Fixed " + fixedCount + " collider(s) in " + scene.path + ".\n");
            return fixedCount;
        }

        static string BuildPath(Transform transform)
        {
            var path = new StringBuilder(transform.name);
            Transform current = transform.parent;
            while (current != null)
            {
                path.Insert(0, current.name + "/");
                current = current.parent;
            }

            return path.ToString();
        }
    }
}
