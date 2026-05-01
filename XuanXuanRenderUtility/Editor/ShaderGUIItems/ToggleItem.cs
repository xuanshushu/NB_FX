using System;
using UnityEditor;
using UnityEngine;

namespace NBShaderEditor
{
    public class ToggleItem : ShaderGUIItem
    {
        private readonly Func<GUIContent> _contentProvider;
        private readonly Func<bool> _isVisible;
        private readonly Action<bool> _onValueChanged;

        public ToggleItem(
            ShaderGUIRootItem rootItem,
            ShaderGUIItem parentItem,
            string propertyName,
            Func<GUIContent> contentProvider,
            Action<bool> onValueChanged = null,
            Func<bool> isVisible = null) : base(rootItem, parentItem)
        {
            PropertyName = propertyName;
            _contentProvider = contentProvider ?? (() => GUIContent.none);
            _onValueChanged = onValueChanged;
            _isVisible = isVisible;
            InitTriggerByChild();
        }

        public override void OnGUI()
        {
            if (_isVisible != null && !_isVisible())
            {
                return;
            }

            GuiContent = _contentProvider();
            base.OnGUI();
        }

        public override void DrawController()
        {
            bool value = PropertyInfo.Property.floatValue > 0.5f;
            value = EditorGUI.Toggle(ControlRect, value);
            PropertyInfo.Property.floatValue = value ? 1f : 0f;
        }

        public override void OnEndChange()
        {
            base.OnEndChange();
            _onValueChanged?.Invoke(PropertyInfo.Property.floatValue > 0.5f);
        }
    }
}
