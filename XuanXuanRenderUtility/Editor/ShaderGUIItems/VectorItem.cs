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
            using (ParentControlDisabledScope())
            {
                EditorGUI.LabelField(LabelRect, _contentProvider());
            }

            using (ParentControlDisabledScope())
            {
                EditorGUI.showMixedValue = property.hasMixedValue;
                EditorGUI.BeginChangeCheck();
                Vector4 vector = property.vectorValue;
                Vector2 value = _firstLine ? new Vector2(vector.x, vector.y) : new Vector2(vector.z, vector.w);
                bool animatedScope = BeginAnimatedPropertyBackground(ControlRect, property);
                using (new EditorGUIIndentLevelScope(0))
                {
                    value = EditorGUI.Vector2Field(ControlRect, GUIContent.none, value);
                }
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
            using (ParentControlDisabledScope())
            {
                EditorGUI.LabelField(LabelRect, _contentProvider());
            }

            using (ParentControlDisabledScope())
            {
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
                    using (new EditorGUIIndentLevelScope(0))
                    {
                        value = EditorGUI.Slider(ControlRect, value, _min, _max);
                    }
                    EndAnimatedPropertyBackground(animatedScope);
                }
                else
                {
                    bool animatedScope = BeginAnimatedPropertyBackground(ControlRect, property);
                    using (new EditorGUIIndentLevelScope(0))
                    {
                        value = EditorGUI.FloatField(ControlRect, value);
                    }
                    EndAnimatedPropertyBackground(animatedScope);
                }
                EditorGUI.showMixedValue = false;
                if (EditorGUI.EndChangeCheck())
                {
                    SetValue(ref vector, value);
                    property.vectorValue = vector;
                    OnEndChange();
                }
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

    public class VectorComponentRangeSliderItem : ShaderGUIItem
    {
        private readonly int _componentIndex;
        private readonly Func<GUIContent> _contentProvider;
        private readonly string _rangePropertyName;
        private readonly Func<bool> _isVisible;
        private ShaderPropertyInfo _rangePropertyInfo;

        private const float SliderFrontGap = 5f;
        private const float SliderBackGap = 2f;

        public VectorComponentRangeSliderItem(
            ShaderGUIRootItem rootItem,
            ShaderGUIItem parentItem,
            string propertyName,
            int componentIndex,
            string rangePropertyName,
            Func<GUIContent> contentProvider,
            Func<bool> isVisible = null) : base(rootItem, parentItem)
        {
            PropertyName = propertyName;
            _componentIndex = componentIndex;
            _rangePropertyName = rangePropertyName;
            _contentProvider = contentProvider ?? (() => GUIContent.none);
            _isVisible = isVisible;
            InitTriggerByChild();
        }

        public override void InitTriggerByChild()
        {
            if (!string.IsNullOrEmpty(_rangePropertyName))
            {
                _rangePropertyInfo = RootItem.PropertyInfoDic[_rangePropertyName];
            }

            base.InitTriggerByChild();
        }

        public override void OnGUI()
        {
            if (_isVisible != null && !_isVisible())
            {
                return;
            }

            GetRect();
            using (ParentControlDisabledScope())
            {
                EditorGUI.LabelField(LabelRect, _contentProvider());
            }

            using (ParentControlDisabledScope())
            {
                EditorGUI.BeginChangeCheck();
                using (new EditorGUIIndentLevelScope(0))
                {
                    DrawController();
                }
                EditorGUI.showMixedValue = false;
                if (EditorGUI.EndChangeCheck())
                {
                    OnEndChange();
                }
            }

            DrawResetButton();
        }

        public override void DrawController()
        {
            Vector4 vector = PropertyInfo.Property.vectorValue;
            Vector4 range = _rangePropertyInfo.Property.vectorValue;
            float min = range.x;
            float max = range.y;
            float sliderMin = Mathf.Min(min, max);
            float sliderMax = Mathf.Max(min, max);
            float value = GetValue(vector);

            float rangeFieldWidth = EditorGUIUtility.fieldWidth;
            Rect minRect = ControlRect;
            minRect.width = rangeFieldWidth;
            Rect maxRect = ControlRect;
            maxRect.x = ControlRect.xMax - rangeFieldWidth;
            maxRect.width = rangeFieldWidth;
            Rect sliderRect = ControlRect;
            sliderRect.x = minRect.xMax + SliderFrontGap;
            sliderRect.width = Mathf.Max(0f, maxRect.x - sliderRect.x - SliderBackGap + EditorGUI.indentLevel * UnityEditorGUIIndentWidth);

            RangeVecHasMixedValue(out bool minValueHasMixed, out bool maxValueHasMixed);

            EditorGUI.showMixedValue = minValueHasMixed;
            bool minAnimatedScope = BeginAnimatedPropertyBackground(minRect, _rangePropertyInfo.Property);
            min = EditorGUI.FloatField(minRect, min);
            EndAnimatedPropertyBackground(minAnimatedScope);

            EditorGUI.showMixedValue = maxValueHasMixed;
            bool maxAnimatedScope = BeginAnimatedPropertyBackground(maxRect, _rangePropertyInfo.Property);
            max = EditorGUI.FloatField(maxRect, max);
            EndAnimatedPropertyBackground(maxAnimatedScope);

            range.x = min;
            range.y = max;
            _rangePropertyInfo.Property.vectorValue = range;

            sliderMin = Mathf.Min(min, max);
            sliderMax = Mathf.Max(min, max);
            value = DraggableLabelFloat.Handle(
                LabelRect,
                value,
                DraggableLabelFloat.GetSensitivityByRange(sliderMin, sliderMax),
                sliderMin,
                sliderMax);
            EditorGUI.showMixedValue = PropertyInfo.Property.hasMixedValue;
            bool sliderAnimatedScope = BeginAnimatedPropertyBackground(sliderRect, PropertyInfo.Property);
            value = SliderNoIndent(sliderRect, value, sliderMin, sliderMax);
            EndAnimatedPropertyBackground(sliderAnimatedScope);

            SetValue(ref vector, Mathf.Clamp(value, sliderMin, sliderMax));
            PropertyInfo.Property.vectorValue = vector;
            EditorGUI.showMixedValue = false;
        }

        private static float SliderNoIndent(Rect rect, float value, float min, float max)
        {
            int indentLevel = EditorGUI.indentLevel;
            try
            {
                EditorGUI.indentLevel = 0;
                return EditorGUI.Slider(rect, value, min, max);
            }
            finally
            {
                EditorGUI.indentLevel = indentLevel;
            }
        }

        private void RangeVecHasMixedValue(out bool minValueHasMixed, out bool maxValueHasMixed)
        {
            minValueHasMixed = false;
            maxValueHasMixed = false;
            if (RootItem.Mats.Count <= 1)
            {
                return;
            }

            float firstMin = 0f;
            float firstMax = 0f;
            for (int i = 0; i < RootItem.Mats.Count; i++)
            {
                Vector4 range = RootItem.Mats[i].GetVector(_rangePropertyName);
                if (i == 0)
                {
                    firstMin = range.x;
                    firstMax = range.y;
                    continue;
                }

                if (!Mathf.Approximately(firstMin, range.x))
                {
                    minValueHasMixed = true;
                }

                if (!Mathf.Approximately(firstMax, range.y))
                {
                    maxValueHasMixed = true;
                }
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

        public override void CheckIsPropertyModified(bool isCallByChild = false)
        {
            if (!isCallByChild)
            {
                Vector4 defaultValue = RootItem.Shader.GetPropertyDefaultVectorValue(PropertyInfo.Index);
                Vector4 defaultRange = RootItem.Shader.GetPropertyDefaultVectorValue(_rangePropertyInfo.Index);
                bool isDefaultValue = Mathf.Approximately(GetValue(PropertyInfo.Property.vectorValue), GetValue(defaultValue)) &&
                                      _rangePropertyInfo.Property.vectorValue == defaultRange;
                if (isDefaultValue == PropertyIsDefaultValue)
                {
                    return;
                }

                PropertyIsDefaultValue = isDefaultValue;
            }

            HasModified = !PropertyIsDefaultValue;
            foreach (ShaderGUIItem childItem in ChildrenItemList)
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
            _rangePropertyInfo.Property.vectorValue = RootItem.Shader.GetPropertyDefaultVectorValue(_rangePropertyInfo.Index);

            PropertyIsDefaultValue = true;
            HasModified = false;
            if (!isCallByParent)
            {
                ParentItem?.CheckIsPropertyModified(true);
            }
        }
    }

    public class Vector3Item : ShaderGUIItem
    {
        private readonly Func<GUIContent> _contentProvider;
        private readonly Func<bool> _isVisible;
        private readonly Action<Vector3> _onValueChanged;

        public Vector3Item(
            ShaderGUIRootItem rootItem,
            ShaderGUIItem parentItem,
            string propertyName,
            Func<GUIContent> contentProvider,
            Action<Vector3> onValueChanged = null,
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

            GetRect();
            MaterialProperty property = PropertyInfo.Property;
            using (ParentControlDisabledScope())
            {
                EditorGUI.LabelField(LabelRect, _contentProvider());
            }

            using (ParentControlDisabledScope())
            {
                EditorGUI.showMixedValue = property.hasMixedValue;
                EditorGUI.BeginChangeCheck();
                Vector4 vector = property.vectorValue;
                Vector3 value = new Vector3(vector.x, vector.y, vector.z);
                bool animatedScope = BeginAnimatedPropertyBackground(ControlRect, property);
                using (new EditorGUIIndentLevelScope(0))
                {
                    value = EditorGUI.Vector3Field(ControlRect, GUIContent.none, value);
                }
                EndAnimatedPropertyBackground(animatedScope);
                EditorGUI.showMixedValue = false;
                if (EditorGUI.EndChangeCheck())
                {
                    vector.x = value.x;
                    vector.y = value.y;
                    vector.z = value.z;
                    property.vectorValue = vector;
                    OnEndChange();
                    _onValueChanged?.Invoke(value);
                }
            }

            DrawResetButton();
        }

        public override void ExecuteReset(bool isCallByParent = false)
        {
            base.ExecuteReset(isCallByParent);
            Vector4 vector = PropertyInfo.Property.vectorValue;
            _onValueChanged?.Invoke(new Vector3(vector.x, vector.y, vector.z));
        }
    }
}
