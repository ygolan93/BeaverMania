using System.Globalization;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace BeavermaniaEditor
{
    // One-shot: verify the WaspQueen.fbx reimport after the WQ_Sting re-author. Forces a synchronous import
    // first so clip sub-assets are present, then reports clip list + fileIDs, WQ_Sting curve/key counts,
    // avatar validity, and skin bone count. Safe to delete afterward.
    [InitializeOnLoad]
    public static class WQStingVerify
    {
        const string Fbx = "Assets/Prefabs/WaspQueen/Animations/WaspQueen.fbx";
        const string Result = "Temp/wq_sting_verify.txt";

        static WQStingVerify() { EditorApplication.delayCall += Run; }

        static void Run()
        {
            var sb = new StringBuilder();
            try
            {
                AssetDatabase.ImportAsset(Fbx, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                var reps = AssetDatabase.LoadAllAssetRepresentationsAtPath(Fbx);
                int clipCount = 0;
                foreach (var o in reps)
                {
                    var clip = o as AnimationClip;
                    if (clip == null) continue;
                    clipCount++;
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(clip, out string guid, out long fid);
                    if (clip.name == "WQ_Sting")
                    {
                        var binds = AnimationUtility.GetCurveBindings(clip);
                        int totalKeys = 0;
                        bool hasThigh = false, hasWing = false, hasStinger = false;
                        foreach (var b in binds)
                        {
                            var c = AnimationUtility.GetEditorCurve(clip, b);
                            if (c != null) totalKeys += c.length;
                            if (b.path.Contains("thigh_fk")) hasThigh = true;
                            if (b.path.Contains("Wing_")) hasWing = true;
                            if (b.path.Contains("Stinger")) hasStinger = true;
                        }
                        sb.AppendLine("WQ_Sting fileID=" + fid + " len=" + clip.length.ToString("0.###", CultureInfo.InvariantCulture) + "s bindings=" + binds.Length + " totalKeys=" + totalKeys + " hasThigh=" + hasThigh + " hasWing=" + hasWing + " hasStinger=" + hasStinger);
                    }
                    else
                    {
                        sb.AppendLine("clip " + clip.name + " fileID=" + fid);
                    }
                }
                sb.AppendLine("clipCount=" + clipCount);

                var objs = AssetDatabase.LoadAllAssetsAtPath(Fbx);
                Avatar avatar = null;
                foreach (var o in objs) { avatar = o as Avatar; if (avatar != null) break; }
                sb.AppendLine(avatar != null ? ("avatar=" + avatar.name + " isValid=" + avatar.isValid) : "avatar=NONE");
                var root = AssetDatabase.LoadAssetAtPath<GameObject>(Fbx);
                if (root != null)
                {
                    var smr = root.GetComponentInChildren<SkinnedMeshRenderer>();
                    if (smr != null) sb.AppendLine("skin bones=" + (smr.bones != null ? smr.bones.Length : 0));
                }
            }
            catch (System.Exception e) { sb.AppendLine("EXCEPTION: " + e.Message); }
            try { System.IO.File.WriteAllText(Result, sb.ToString()); } catch { }
            Debug.Log("[WQStingVerify] done -> " + Result);
        }
    }
}
