using System;
using UnityEditor;
using UnityEngine;

namespace NBShaderEditor
{
    public class Vector2LineItem : ShaderGUIItem
    {
        private readonly bool _firstLine;
        private readonly Func<GUIContent> _contentProvider;
        private readonly Func<bool> _isVisible;

        public Vector2LineItem(
            ShaderGUIRootItem rootItem,
            ShaderGUIItem parentItem,
            string propertyName,
            bool firstLine,
            Func<GUIContent> contentProvider,
            Func<bool> isVisible = null) : base(rootItem, parentItem)
        {
            PropertyName = propertyName;
            _firstLine = firstLine;
            _contentProvider = contentProvider ?? (() => GUIContent.none);
            _isVisible = isVisible;
            InitTriggerByChild();
        }

        public override void OnGUI()
        {
            if (_isVisible != null && !_isVisible())
            {
                return;
            }

            GetRect();
            MaterialProperty property = PropertyInfo.Property;
            EditorGUI.LabelField(LabelRect, _contentProvider());
            EditorGUI.showMixedValue = property.hasMixedValue;
            EditorGUI.BeginChangeCheck();
            Vector4 vector = property.vectorValue;
            Vector2 value = _firstLine ? new Vector2(vector.x, vector.y) : new Vector2(vector.z, vector.w);
            bool animatedScope = BeginAnimatedPropertyBackground(ControlRect, property);
            value = EditorGUI.Vector2Field(ControlRect, GUIContent.none, value);
            EndAnimatedPropertyBackground(animatedScope);
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
                OnEndChange();
            }

            DrawResetButton();
        }

        public override void CheckIsPropertyModified(bool isCallByChild = false)
        {
            if (PropertyInfo == null)
            {
                base.CheckIsPropertyModified(isCallByChild);
                return;
            }

            if (!isCallByChild)
            {
                Vector4 defaultValue = RootItem.Shader.GetPropertyDefaultVectorValue(PropertyInfo.Index);
                Vector4 currentValue = PropertyInfo.Property.vectorValue;
                bool isDefaultValue = _firstLine
                    ? Mathf.Approximately(currentValue.x, defaultValue.x) && Mathf.Approximately(currentValue.y, defaultValue.y)
                    : Mathf.Approximately(currentValue.z, defaultValue.z) && Mathf.Approximately(currentValue.w, defaultValue.w);

                if (isDefaultValue == PropertyIsDefaultValue)
                {
                    return;
                }

                PropertyIsDefaultValue = isDefaultValue;
            }

            HasModified = !PropertyIsDefaultValue;
            foreach (var childItem in ChildrenItemList)
            {
                HasModified |= childItem.HasModified;
            }
            ParentItem?.CheckIsPropertyModified(true);
        }

        public override void ExecuteReset(bool isCallByParent = false)
        {
            Vector4 defaultValue = RootItem.Shader.GetPropertyDefaultVectorValue(PropertyInfo.Index);
            Vector4 currentValue = PropertyInfo.Property.vectorValue;
            if (_firstLine)
            {
                currentValue.x = defaultValue.x;
                currentValue.y = defaultValue.y;
            }
            else
            {
                currentValue.z = defaultValue.z;
                currentValue.w = defaultValue.w;
            }

            PropertyInfo.Property.vectorValue = currentValue;
            PropertyIsDefaultValue = true;
            HasModified = false;
            if (!isCallByParent)
            {
                ParentItem?.CheckIsPropertyModified(true);
            }
        }
    }

    public class VectorComponentItem : ShaderGUIItem
    {
        private readonly int _componentIndex;
        private readonly bool _isSlider;
        private readonly float _min;
        private readonly float _max;
        private readonly Func<GUIContent> _contentProvider;
        private readonly Func<bool> _isVisible;

        public VectorComponentItem(
            ShaderGUIRootItem rootItem,
            ShaderGUIItem parentItem,
            string propertyName,
            int componentIndex,
            Func<GUIContent> contentProvider,
            bool isSlider,
            float min = 0f,
            float max = 1f,
            Func<bool> isVisible = null) : base(rootItem, parentItem)
        {
            PropertyName = propertyName;
            _componentIndex = componentIndex;
            _contentProvider = contentProvider ?? (() => GUIContent.none);
            _isSlider = isSlider;
            _min = min;
            _max = max;
            _isVisible = isVisible;
            InitTriggerByChild();
        }

        public override void OnGUI()
        {
            if (_isVisible != null && !_isVisible())
            {
                return;
            }

            GetRect();
            MaterialProperty property = PropertyInfo.Property;
            EditorGUI.LabelField(LabelRect, _contentProvider());
            EditorGUI.showMixedValue = property.hasMixedValue;
            EditorGUI.BeginChangeCheck();
            Vector4 vector = property.vectorValue;
            float value = GetValue(vector);
            if (_isSlider)
            {
                value = DraggableLabelFloat.Handle(
                    LabelRect,
                    value,
                    DraggableLabelFloat.GetSensitivityByRange(_min, _max),
                    _min,
                    _max);
                bool animatedScope = BeginAnimatedPropertyBackground(ControlRect, property);
                value = EditorGUI.Slider(ControlRect, value, _min, _max);
                EndAnimatedPropertyBackground(animatedScope);
            }
            else
            {
                bool animatedScope = BeginAnimatedPropertyBackground(ControlRect, property);
                value = EditorGUI.FloatField(ControlRect, value);
                EndAnimatedPropertyBackground(animatedScope);
            }
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                SetValue(ref vector, value);
                property.vectorValue = vector;
                OnEndChange();
            }

            DrawResetButton();
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

        public override void CheckIsPropertyModified(bool isCallByChild = false)
        {
            if (PropertyInfo == null)
            {
                base.CheckIsPropertyModified(isCallByChild);
                return;
            }

            if (!isCallByChild)
            {
                Vector4 defaultValue = RootItem.Shader.GetPropertyDefaultVectorValue(PropertyInfo.Index);
                bool isDefaultValue = Mathf.Approximately(GetValue(PropertyInfo.Property.vectorValue), GetValue(defaultValue));
                if (isDefaultValue == PropertyIsDefaultValue)
                {
                    return;
                }

                PropertyIsDefaultValue = isDefaultValue;
            }

            HasModified = !PropertyIsDefaultValue;
            foreach (var childItem in ChildrenItemList)
            {
                HasModified |= childItem.HasModified;
            }
            ParentItem?.CheckIsPropertyModified(true);
        }

        public override void ExecuteReset(bool isCallByParent = false)
        {
            Vector4 vector = PropertyInfo.Property.vectorValue;
            Vector4 defaultValue = RootItem.Shader.GetPropertyDefaultVectorValue(PropertyInfo.Index);
            SetValue(ref vector, GetValue(defaultValue));
            PropertyInfo.Property.vectorValue = vector;
            PropertyIsDefaultValue = true;
            HasModified = false;
            if (!isCallByParent)
            {
                ParentItem?.CheckIsPropertyModified(true);
            }
        }
    }
}
