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

    public class ShaderGUIBitMaskItem : ShaderGUIItem
    {
        private static readonly string[] EmptyMaskNames = new string[0];

        private readonly Func<GUIContent> _contentProvider;
        private readonly Func<string[]> _maskNamesProvider;
        private readonly Action<MaterialProperty> _onValueChanged;
        private readonly Func<bool> _isVisible;

        public ShaderGUIBitMaskItem(ShaderGUIRootItem rootItem, ShaderGUIItem parentItem)
            : base(rootItem, parentItem: parentItem)
        {
        }

        public ShaderGUIBitMaskItem(
            ShaderGUIRootItem rootItem,
            ShaderGUIItem parentItem,
            string propertyName,
            Func<GUIContent> contentProvider,
            Func<string[]> maskNamesProvider,
            Action<MaterialProperty> onValueChanged = null,
            Func<bool> isVisible = null) : base(rootItem, parentItem)
        {
            PropertyName = propertyName;
            _contentProvider = contentProvider ?? (() => GUIContent.none);
            _maskNamesProvider = maskNamesProvider;
            _onValueChanged = onValueChanged;
            _isVisible = isVisible;
            GuiContent = _contentProvider();
            MaskNames = _maskNamesProvider?.Invoke();
            InitTriggerByChild();
        }

        public string[] MaskNames;
        public int ValidMask = -1;

        public override void OnGUI()
        {
            if (_isVisible != null && !_isVisible())
            {
                return;
            }

            if (_contentProvider != null)
            {
                GuiContent = _contentProvider();
            }

            if (_maskNamesProvider != null)
            {
                MaskNames = _maskNamesProvider();
            }

            base.OnGUI();
        }

        public override void DrawController()
        {
            string[] maskNames = MaskNames ?? EmptyMaskNames;
            int validMask = GetValidMask(maskNames);
            int mask = Mathf.RoundToInt(PropertyInfo.Property.floatValue) & validMask;
            int value = EditorGUI.MaskField(ControlRect, mask, maskNames) & validMask;
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

        private int GetValidMask(string[] maskNames)
        {
            if (ValidMask >= 0)
            {
                return ValidMask;
            }

            int optionCount = Mathf.Clamp(maskNames.Length, 0, 30);
            return optionCount == 0 ? 0 : (1 << optionCount) - 1;
        }
    }
}
