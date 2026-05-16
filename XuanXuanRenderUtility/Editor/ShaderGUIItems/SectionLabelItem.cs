using System;
using UnityEditor;
using UnityEngine;

namespace NBShaderEditor
{
    public class SectionLabelItem : ShaderGUIItem
    {
        private readonly Func<GUIContent> _contentProvider;
        private readonly Func<bool> _isVisible;

        public SectionLabelItem(
            ShaderGUIRootItem rootItem,
            ShaderGUIItem parentItem,
            Func<GUIContent> contentProvider,
            Func<bool> isVisible = null) : base(rootItem, parentItem)
        {
            _contentProvider = contentProvider ?? (() => GUIContent.none);
            _isVisible = isVisible;
            GuiContent = _contentProvider();
        }

        public override void OnGUI()
        {
            if (_isVisible != null && !_isVisible())
            {
                return;
            }

            LayoutSpace();
            using (ParentControlDisabledScope())
            {
                EditorGUI.LabelField(ApplyGlobalRectCompensation(LayoutRect()), GuiContent, EditorStyles.boldLabel);
            }
        }
    }
}
