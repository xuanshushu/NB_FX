using UnityEditor;
using UnityEditor.Rendering.Universal.ShaderGraph;
using UnityEngine;

namespace NBShaderEditor
{
    // Mesh Shader Graph entry. URP owns Surface Options/Advanced and their
    // material validation; NBShaderGUI owns only the Graph-specific inputs.
    public sealed class NBShaderGraphGUI : NBShaderGUI
    {
        readonly NBGraphUnlitGUIBridge _urpGUI = new NBGraphUnlitGUIBridge();

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
            => _urpGUI.OnGUI(materialEditor, properties, OnGraphGUI);

        public override void ValidateMaterial(Material material)
            => _urpGUI.ValidateMaterial(material);

        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
            => _urpGUI.AssignNewShaderToMaterial(material, oldShader, newShader);
    }
}
