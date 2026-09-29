using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace NBShaderEditor
{
    // A capability-specific Root: Graph properties are metadata-driven and do
    // not contain ShaderLab's folds, keywords, or single-float packed flags.
    // MaterialEditor handles property drawers, multi-edit and Undo; this root
    // only participates in the existing NBShader GUI layout/lifecycle.
    public sealed class NBShaderGraphRootItem : ShaderGUIRootItem
    {
        MaterialProperty[] _properties;

        public override void OnGUI(MaterialEditor editor, MaterialProperty[] properties)
        {
            _properties = properties;
            base.OnGUI(editor, properties);
        }

        public override void OnChildOnGUI()
        {
            foreach (MaterialProperty property in _properties)
            {
                if ((property.propertyFlags &
                     (ShaderPropertyFlags.HideInInspector | ShaderPropertyFlags.PerRendererData)) != 0)
                    continue;

                float height = MatEditor.GetPropertyHeight(property, property.displayName);
                Rect rect = GetControlRect(height);
                MatEditor.ShaderProperty(rect, property, property.displayName);
            }
        }
    }
}
