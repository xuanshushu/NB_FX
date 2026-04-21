using System;
using UnityEditor;
using UnityEngine;

namespace NBShaderEditor
{
    public class NBShaderFloatPropertyItem : ShaderGUIFloatItem
    {
        private readonly Func<bool> _isVisible;

        public NBShaderFloatPropertyItem(
            NBShaderRootItem rootItem,
            ShaderGUIItem parentItem,
            string propertyName,
            string labelKey,
            string labelFallback,
            string tipKey = null,
            string tipFallback = "",
            Func<bool> isVisible = null) : base(rootItem, parentItem)
        {
            PropertyName = propertyName;
            GuiContent = NBShaderInspectorLocalization.MakeContent(labelKey, labelFallback, tipKey, tipFallback);
            _isVisible = isVisible;
            InitTriggerByChild();
        }

        public override void OnGUI()
        {
            if (_isVisible != null && !_isVisible())
            {
                return;
            }

            base.OnGUI();
        }
    }

    public class NBShaderSliderPropertyItem : ShaderGUISliderItem
    {
        private readonly Func<bool> _isVisible;

        public NBShaderSliderPropertyItem(
            NBShaderRootItem rootItem,
            ShaderGUIItem parentItem,
            string propertyName,
            string labelKey,
            string labelFallback,
            float min,
            float max,
            string rangePropertyName = null,
            string tipKey = null,
            string tipFallback = "",
            Func<bool> isVisible = null) : base(rootItem, parentItem)
        {
            PropertyName = propertyName;
            RangePropertyName = rangePropertyName;
            Min = min;
            Max = max;
            GuiContent = NBShaderInspectorLocalization.MakeContent(labelKey, labelFallback, tipKey, tipFallback);
            _isVisible = isVisible;
            InitTriggerByChild();
        }

        public override void OnGUI()
        {
            if (_isVisible != null && !_isVisible())
            {
                return;
            }

            base.OnGUI();
        }
    }

    public class NBShaderPopupPropertyItem : ShaderGUIPopUpItem
    {
        private readonly Func<bool> _isVisible;
        private readonly Action _onValueChanged;

        public NBShaderPopupPropertyItem(
            NBShaderRootItem rootItem,
            ShaderGUIItem parentItem,
            string propertyName,
            string labelKey,
            string labelFallback,
            string[] popupNames,
            Action onValueChanged = null,
            string tipKey = null,
            string tipFallback = "",
            Func<bool> isVisible = null) : base(rootItem, parentItem)
        {
            PropertyName = propertyName;
            GuiContent = NBShaderInspectorLocalization.MakeContent(labelKey, labelFallback, tipKey, tipFallback);
            PopUpNames = popupNames;
            _isVisible = isVisible;
            _onValueChanged = onValueChanged;
            InitTriggerByChild();
        }

        public override void OnGUI()
        {
            if (_isVisible != null && !_isVisible())
            {
                return;
            }

            base.OnGUI();
        }

        public override void OnEndChange()
        {
            base.OnEndChange();
            _onValueChanged?.Invoke();
        }
    }

    public class NBShaderTogglePropertyItem : ShaderGUIItem
    {
        private readonly Func<bool> _isVisible;
        private readonly Action<bool> _onValueChanged;

        public NBShaderTogglePropertyItem(
            NBShaderRootItem rootItem,
            ShaderGUIItem parentItem,
            string propertyName,
            string labelKey,
            string labelFallback,
            Action<bool> onValueChanged = null,
            string tipKey = null,
            string tipFallback = "",
            Func<bool> isVisible = null) : base(rootItem, parentItem)
        {
            PropertyName = propertyName;
            GuiContent = NBShaderInspectorLocalization.MakeContent(labelKey, labelFallback, tipKey, tipFallback);
            _isVisible = isVisible;
            _onValueChanged = onValueChanged;
            InitTriggerByChild();
        }

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

    public class NBShaderColorPropertyItem : NBShaderControlItem
    {
        private readonly string _propertyName;
        private readonly string _labelKey;
        private readonly string _labelFallback;
        private readonly string _tipKey;
        private readonly string _tipFallback;
        private readonly bool _showAlpha;
        private readonly Func<bool> _isVisible;

        public NBShaderColorPropertyItem(
            NBShaderRootItem rootItem,
            ShaderGUIItem parentItem,
            string propertyName,
            string labelKey,
            string labelFallback,
            bool showAlpha = true,
            string tipKey = null,
            string tipFallback = "",
            Func<bool> isVisible = null) : base(rootItem, parentItem)
        {
            _propertyName = propertyName;
            _labelKey = labelKey;
            _labelFallback = labelFallback;
            _tipKey = tipKey;
            _tipFallback = tipFallback;
            _showAlpha = showAlpha;
            _isVisible = isVisible;
        }

        public override void OnGUI()
        {
            if (_isVisible != null && !_isVisible())
            {
                return;
            }

            GUIContent content = NBShaderInspectorLocalization.MakeContent(_labelKey, _labelFallback, _tipKey, _tipFallback);
            NBRootItem.MatEditor.ColorProperty(NBRootItem.PropertyInfoDic[_propertyName].Property, content.text);
        }
    }

    public class NBShaderTexturePropertyItem : NBShaderControlItem
    {
        private readonly string _texturePropertyName;
        private readonly string _colorPropertyName;
        private readonly string _labelKey;
        private readonly string _labelFallback;
        private readonly string _tipKey;
        private readonly string _tipFallback;
        private readonly bool _drawScaleOffset;
        private readonly Func<bool> _isVisible;
        private readonly Action<MaterialProperty> _afterDraw;

        public NBShaderTexturePropertyItem(
            NBShaderRootItem rootItem,
            ShaderGUIItem parentItem,
            string texturePropertyName,
            string labelKey,
            string labelFallback,
            string colorPropertyName = null,
            bool drawScaleOffset = true,
            Action<MaterialProperty> afterDraw = null,
            string tipKey = null,
            string tipFallback = "",
            Func<bool> isVisible = null) : base(rootItem, parentItem)
        {
            _texturePropertyName = texturePropertyName;
            _colorPropertyName = colorPropertyName;
            _labelKey = labelKey;
            _labelFallback = labelFallback;
            _tipKey = tipKey;
            _tipFallback = tipFallback;
            _drawScaleOffset = drawScaleOffset;
            _isVisible = isVisible;
            _afterDraw = afterDraw;
        }

        public override void OnGUI()
        {
            if (_isVisible != null && !_isVisible())
            {
                return;
            }

            MaterialProperty textureProperty = NBRootItem.PropertyInfoDic[_texturePropertyName].Property;
            MaterialProperty colorProperty = null;
            if (!string.IsNullOrEmpty(_colorPropertyName) && NBRootItem.PropertyInfoDic.ContainsKey(_colorPropertyName))
            {
                colorProperty = NBRootItem.PropertyInfoDic[_colorPropertyName].Property;
            }

            GUIContent content = NBShaderInspectorLocalization.MakeContent(_labelKey, _labelFallback, _tipKey, _tipFallback);
            NBRootItem.MatEditor.TexturePropertySingleLine(content, textureProperty, colorProperty);
            if (_drawScaleOffset)
            {
                NBRootItem.MatEditor.TextureScaleOffsetProperty(textureProperty);
            }

            _afterDraw?.Invoke(textureProperty);
        }
    }

    public class NBShaderVector2LinePropertyItem : NBShaderControlItem
    {
        private readonly string _propertyName;
        private readonly bool _firstLine;
        private readonly string _labelKey;
        private readonly string _labelFallback;
        private readonly string _tipKey;
        private readonly string _tipFallback;
        private readonly Func<bool> _isVisible;

        public NBShaderVector2LinePropertyItem(
            NBShaderRootItem rootItem,
            ShaderGUIItem parentItem,
            string propertyName,
            bool firstLine,
            string labelKey,
            string labelFallback,
            string tipKey = null,
            string tipFallback = "",
            Func<bool> isVisible = null) : base(rootItem, parentItem)
        {
            _propertyName = propertyName;
            _firstLine = firstLine;
            _labelKey = labelKey;
            _labelFallback = labelFallback;
            _tipKey = tipKey;
            _tipFallback = tipFallback;
            _isVisible = isVisible;
        }

        public override void OnGUI()
        {
            if (_isVisible != null && !_isVisible())
            {
                return;
            }

            GetRect();
            MaterialProperty property = NBRootItem.PropertyInfoDic[_propertyName].Property;
            GUIContent content = NBShaderInspectorLocalization.MakeContent(_labelKey, _labelFallback, _tipKey, _tipFallback);
            EditorGUI.LabelField(LabelRect, content);
            EditorGUI.showMixedValue = property.hasMixedValue;
            EditorGUI.BeginChangeCheck();
            Vector4 vector = property.vectorValue;
            Vector2 value = _firstLine ? new Vector2(vector.x, vector.y) : new Vector2(vector.z, vector.w);
            value = EditorGUI.Vector2Field(ControlRect, GUIContent.none, value);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                if (_firstLine)
                {
                    vector.x = value.x;
                    vector.y = value.y;
                }
                else
                {
                    vector.z = value.x;
                    vector.w = value.y;
                }

                property.vectorValue = vector;
            }
        }
    }

    public class NBShaderVectorComponentPropertyItem : NBShaderControlItem
    {
        private readonly string _propertyName;
        private readonly int _componentIndex;
        private readonly bool _isSlider;
        private readonly float _min;
        private readonly float _max;
        private readonly string _labelKey;
        private readonly string _labelFallback;
        private readonly string _tipKey;
        private readonly string _tipFallback;
        private readonly Func<bool> _isVisible;

        public NBShaderVectorComponentPropertyItem(
            NBShaderRootItem rootItem,
            ShaderGUIItem parentItem,
            string propertyName,
            int componentIndex,
            string labelKey,
            string labelFallback,
            bool isSlider,
            float min = 0f,
            float max = 1f,
            string tipKey = null,
            string tipFallback = "",
            Func<bool> isVisible = null) : base(rootItem, parentItem)
        {
            _propertyName = propertyName;
            _componentIndex = componentIndex;
            _labelKey = labelKey;
            _labelFallback = labelFallback;
            _isSlider = isSlider;
            _min = min;
            _max = max;
            _tipKey = tipKey;
            _tipFallback = tipFallback;
            _isVisible = isVisible;
        }

        public override void OnGUI()
        {
            if (_isVisible != null && !_isVisible())
            {
                return;
            }

            GetRect();
            MaterialProperty property = NBRootItem.PropertyInfoDic[_propertyName].Property;
            GUIContent content = NBShaderInspectorLocalization.MakeContent(_labelKey, _labelFallback, _tipKey, _tipFallback);
            EditorGUI.LabelField(LabelRect, content);
            EditorGUI.showMixedValue = property.hasMixedValue;
            EditorGUI.BeginChangeCheck();
            Vector4 vector = property.vectorValue;
            float value = GetValue(vector);
            value = _isSlider ? EditorGUI.Slider(ControlRect, value, _min, _max) : EditorGUI.FloatField(ControlRect, value);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                SetValue(ref vector, value);
                property.vectorValue = vector;
            }
        }

        private float GetValue(Vector4 vector)
        {
            switch (_componentIndex)
            {
                case 0: return vector.x;
                case 1: return vector.y;
                case 2: return vector.z;
                case 3: return vector.w;
                default: return vector.x;
            }
        }

        private void SetValue(ref Vector4 vector, float value)
        {
            switch (_componentIndex)
            {
                case 0:
                    vector.x = value;
                    break;
                case 1:
                    vector.y = value;
                    break;
                case 2:
                    vector.z = value;
                    break;
                case 3:
                    vector.w = value;
                    break;
            }
        }
    }
}
