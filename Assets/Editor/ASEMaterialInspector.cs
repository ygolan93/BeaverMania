using UnityEditor;
using UnityEngine;

/// <summary>
/// Fallback ShaderGUI for Amplify Shader Editor exported shaders when ASE is not installed.
/// Synty and other ASE-exported shaders declare CustomEditor "ASEMaterialInspector".
/// </summary>
public class ASEMaterialInspector : ShaderGUI
{
    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        materialEditor.PropertiesDefaultGUI(properties);
    }
}
