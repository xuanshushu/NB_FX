using System;
using UnityEditor;
using UnityEngine;

namespace NBShaderEditor
{
    public class ShaderGUIPopUpItem:ShaderGUIItem
    {
        private readonly Func<GUIContent> _contentProvider;
        private readonly Func<string[]> _popupNamesProvider;
        private readonly Action<MaterialProperty> _onValueChanged;
        private readonly Func<bool> _isVisible;

        public ShaderGUIPopUpItem(ShaderGUIRootItem rootItem, ShaderGUIItem parentItem) :
            base(rootItem, parentItem: parentItem)
        {
            // base.InitTriggerByChild();
        }

        public ShaderGUIPopUpItem(
            ShaderGUIRootItem rootItem,
            ShaderGUIItem parentItem,
            string propertyName,
            Func<GUIContent> contentProvider,
            Func<string[]> popupNamesProvider,
            Action<MaterialProperty> onValueChanged = null,
            Func<bool> isVisible = null) : base(rootItem, parentItem)
        {
            PropertyName = propertyName;
            _contentProvider = contentProvider ?? (() => GUIContent.none);
            _popupNamesProvider = popupNamesProvider;
            _onValueChanged = onValueChanged;
            _isVisible = isVisible;
            GuiContent = _contentProvider();
            PopUpNames = _popupNamesProvider?.Invoke();
            InitTriggerByChild();
        }

        public string[] PopUpNames;

        public override void OnGUI()
        {
            if (_isVisible != null && !_isVisible())
            {
                return;
            }

            base.OnGUI();
        }

        public override void DrawController()
        {
            int value = EditorGUI.Popup(ControlRect, (int)PropertyInfo.Property.floatValue, PopUpNames);
            SetFloatIfDifferent(PropertyInfo.Property, value);
        }

        public override void OnEndChange()
        {
            base.OnEndChange();
            _onValueChanged?.Invoke(PropertyInfo.Property);
        }

        public override void ExecuteReset(bool isCallByParent = false)
        {
            base.ExecuteReset(isCallByParent);
            OnEndChange();
        }
    }
}
