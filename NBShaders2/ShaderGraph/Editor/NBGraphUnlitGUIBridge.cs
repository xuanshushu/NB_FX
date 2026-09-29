// Compiled by NBGraphSubTarget.asmref into the current URP Editor assembly.
// ShaderGraphUnlitGUI is internal, so the NBShaders2 Editor assembly uses this
// public, dependency-neutral facade rather than reflection or copied URP code.
using System;
using UnityEngine;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    public sealed class NBGraphUnlitGUIBridge
    {
        readonly GraphGUI _gui = new GraphGUI();

        public void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties,
            Action<MaterialEditor, MaterialProperty[]> drawSurfaceInputs)
        {
            _gui.DrawSurfaceInputsCallback = drawSurfaceInputs;
            _gui.OnGUI(materialEditor, properties);
        }

        public void ValidateMaterial(Material material) => _gui.ValidateMaterial(material);

        public void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
            => _gui.AssignNewShaderToMaterial(material, oldShader, newShader);

        sealed class GraphGUI : ShaderGraphUnlitGUI
        {
            MaterialProperty[] _properties;
            internal Action<MaterialEditor, MaterialProperty[]> DrawSurfaceInputsCallback;

            public override void FindProperties(MaterialProperty[] properties)
            {
                _properties = properties;
                base.FindProperties(properties);
            }

            public override void DrawSurfaceInputs(Material material)
            {
                if (DrawSurfaceInputsCallback == null)
                    base.DrawSurfaceInputs(material);
                else
                    DrawSurfaceInputsCallback(materialEditor, _properties);
            }
        }
    }
}
