using System;
using UnityEditor;
using UnityEngine;

namespace NBShaderEditor
{
    public class ShaderGUIFloatItem:ShaderGUIItem
    {
        private readonly Func<bool> _isVisible;

        public ShaderGUIFloatItem(ShaderGUIRootItem rootItem, ShaderGUIItem parentItem, Func<bool> isVisible = null) :
            base(rootItem, parentItem: parentItem)
        {
            _isVisible = isVisible;
            base.InitTriggerByChild();
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
            float value = DraggableLabelFloat.Handle(LabelRect, PropertyInfo.Property.floatValue, sensitivity: -1f);//拖动Label控件可以操作Float参数
            value = EditorGUI.FloatField(ControlRect, value);
            SetFloatIfDifferent(PropertyInfo.Property, value);
        }
        
    }
    
    public class ShaderGUISliderItem:ShaderGUIItem
    {
        public float Min = 0;
        public float Max = 1;
        public string RangePropertyName;
        ShaderPropertyInfo _rangePropertyInfo;
        private readonly Func<bool> _isVisible;
        private const float SliderFrontGap = 5f;
        private const float SliderBackGap = 2f;
        private const float RangeFieldWidth = 30f;

        public override void InitTriggerByChild()
        {
            if (RangePropertyName != null)
            {
                _rangePropertyInfo = RootItem.PropertyInfoDic[RangePropertyName];
                Min = _rangePropertyInfo.Property.vectorValue.x;
                Max = _rangePropertyInfo.Property.vectorValue.y;
            }
            base.InitTriggerByChild();
        }

        public ShaderGUISliderItem(ShaderGUIRootItem rootItem, ShaderGUIItem parentItem, Func<bool> isVisible = null) :
            base(rootItem, parentItem: parentItem)
        {
            _isVisible = isVisible;
        }

        public override void OnGUI()
        {
            if (_isVisible != null && !_isVisible())
            {
                return;
            }

            if (_rangePropertyInfo == null)
            {
                base.OnGUI();
                return;
            }

            GetRect();
            using (ParentControlDisabledScope())
            {
                EditorGUI.LabelField(LabelRect, GuiContent);
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
            DrawBlock();
        }

        public override void DrawController()
        {
            if (_rangePropertyInfo != null)
            {
                Rect minRect = ControlRect;
                minRect.width = RangeFieldWidth;
                Rect maxRect = ControlRect;
                maxRect.x = ControlRect.xMax - RangeFieldWidth;
                maxRect.width = RangeFieldWidth;
                Rect sliderRect = ControlRect;
                sliderRect.x = minRect.xMax + SliderFrontGap;
                sliderRect.width = Mathf.Max(0f, maxRect.x - sliderRect.x - SliderBackGap + EditorGUI.indentLevel * UnityEditorGUIIndentWidth);
                
                RangeVecHasMixedValue(out bool minValueHasMixed,out bool maxValueHasMixed);
                
                Vector4 rangeVector = _rangePropertyInfo.Property.vectorValue;
                float min = rangeVector.x;
                float max = rangeVector.y;

                EditorGUI.showMixedValue = minValueHasMixed;
                bool minAnimatedScope = BeginAnimatedPropertyBackground(minRect, _rangePropertyInfo.Property);
                min = EditorGUI.FloatField(minRect, min);
                EndAnimatedPropertyBackground(minAnimatedScope);
                EditorGUI.showMixedValue = maxValueHasMixed;
                bool maxAnimatedScope = BeginAnimatedPropertyBackground(maxRect, _rangePropertyInfo.Property);
                max = EditorGUI.FloatField(maxRect, max);
                EndAnimatedPropertyBackground(maxAnimatedScope);
                rangeVector.x = min;
                rangeVector.y = max;
                SetVectorIfDifferent(_rangePropertyInfo.Property, rangeVector);

                float sliderMin = Mathf.Min(min, max);
                float sliderMax = Mathf.Max(min, max);
                float value = DraggableLabelFloat.Handle(
                    LabelRect,
                    PropertyInfo.Property.floatValue,
                    DraggableLabelFloat.GetSensitivityByRange(sliderMin, sliderMax),
                    sliderMin,
                    sliderMax);
                EditorGUI.showMixedValue = PropertyInfo.Property.hasMixedValue;
                bool sliderAnimatedScope = BeginAnimatedPropertyBackground(sliderRect, PropertyInfo.Property);
                value = SliderNoIndent(sliderRect, value, sliderMin, sliderMax);
                EndAnimatedPropertyBackground(sliderAnimatedScope);

                SetFloatIfDifferent(PropertyInfo.Property, Mathf.Clamp(value, sliderMin, sliderMax));
                EditorGUI.showMixedValue = false;

            }
            else
            {
                float value = DraggableLabelFloat.Handle(
                    LabelRect,
                    PropertyInfo.Property.floatValue,
                    DraggableLabelFloat.GetSensitivityByRange(Min, Max),
                    Min,
                    Max);
                value = EditorGUI.Slider(ControlRect, value,Min,Max);
                SetFloatIfDifferent(PropertyInfo.Property, value);
            }
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

        void RangeVecHasMixedValue( out bool minValueHasMixed, out bool maxValueHasMixed)
        { 
            minValueHasMixed = false;
            maxValueHasMixed = false;
            if (RootItem.Mats.Count > 1)
            {
                MaterialProperty rangeProperty = _rangePropertyInfo.Property;
                float minValue = 0;
                float maxValue = 0;
                for (int i = 0; i < RootItem.Mats.Count; i++)
                {
                    Vector4 rangeVec = RootItem.Mats[i].GetVector(RangePropertyName);
                    if (i == 0)
                    {
                        minValue = rangeVec.x;
                        maxValue = rangeVec.y;
                    }
                    else
                    {
                        if (!Mathf.Approximately(minValue, rangeVec.x))
                        {
                            minValueHasMixed = true;
                        }

                        if (!Mathf.Approximately(maxValue, rangeVec.y))
                        {
                            maxValueHasMixed = true;
                        }
                    }
                }
            }
        }
        
        bool RangePropIsDefaultValue()
        {
            MaterialProperty rangeProperty = _rangePropertyInfo.Property;
            return rangeProperty.vectorValue == RootItem.Shader.GetPropertyDefaultVectorValue(_rangePropertyInfo.Index) && !rangeProperty.hasMixedValue;
        }

        public override void CheckIsPropertyModified(bool isCallByChild = false)
        {
            if (RangePropertyName != null)
            {
                if (!isCallByChild) //只有自身Reset了，才需要查询自己的状态是否正确
                {
                    bool isDefaultValue = true;

                    float defaultValue = RootItem.Shader.GetPropertyDefaultFloatValue(PropertyInfo.Index);
                    isDefaultValue = Mathf.Approximately(PropertyInfo.Property.floatValue, defaultValue);
                    Vector4 defaultRangeVec = RootItem.Shader.GetPropertyDefaultVectorValue(_rangePropertyInfo.Index);
                    isDefaultValue &= _rangePropertyInfo.Property.vectorValue == defaultRangeVec;
                    base.CheckIsPropertyModified(isCallByChild);
                    if (isDefaultValue == PropertyIsDefaultValue)
                    {
                        return; //如果状态没有改变，就不需要做任何操作
                    }
                    else
                    {
                        PropertyIsDefaultValue = isDefaultValue;
                    }
                }

                HasModified = !PropertyIsDefaultValue;
                foreach (var childItem in ChildrenItemList)
                {
                    HasModified |= childItem.HasModified;
                }

                ParentItem?.CheckIsPropertyModified(true);
            }
            else
            {
                base.CheckIsPropertyModified(isCallByChild);
            }

        }

        public override void ExecuteReset(bool isCallByParent = false)
        {
            if (RangePropertyName != null)
            {
                PropertyInfo.Property.floatValue = RootItem.Shader.GetPropertyDefaultFloatValue(PropertyInfo.Index);
                _rangePropertyInfo.Property.vectorValue =
                    RootItem.Shader.GetPropertyDefaultVectorValue(_rangePropertyInfo.Index);
                PropertyIsDefaultValue = true;

                foreach (var childItem in ChildrenItemList)
                {
                    childItem.ExecuteReset(true);
                }

                HasModified = false;
                if (!isCallByParent) //直接由用户触发重置
                {
                    ParentItem?.CheckIsPropertyModified(true);
                }
            }
            else
            {
                base.ExecuteReset(isCallByParent);
            }
        }
    }
     
    //用于滑动控制Label的方案。
    // DraggableLabelFloat.cs
    public static class DraggableLabelFloat
    {
        // 为每个控件实例缓存一次拖拽状态
        private static readonly System.Collections.Generic.Dictionary<int, DragState> s_Drag =
            new System.Collections.Generic.Dictionary<int, DragState>();
        private class DragState
        {
            public float startValue;
            public float dragX;        // 累计水平位移（单位：像素）
        }

        public static float Handle(Rect labelRect, float value, float sensitivity = -1f, float? min = null, float? max = null)
        {
            if (!GUI.enabled)
            {
                return value;
            }

            EditorGUIUtility.AddCursorRect(labelRect, MouseCursor.SlideArrow);

            int id = GUIUtility.GetControlID(FocusType.Passive, labelRect);
            Event e = Event.current;

            switch (e.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (labelRect.Contains(e.mousePosition) && e.button == 0)
                    {
                        GUIUtility.hotControl = id;
                        e.Use();
                        EditorGUIUtility.SetWantsMouseJumping(1);

                        s_Drag[id] = new DragState
                        {
                            startValue = value,
                            dragX = 0f
                        };
                    }
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id && s_Drag.TryGetValue(id, out var st))
                    {
                        // 关键：使用 Event.delta 累加位移，兼容越界/跳回
                        st.dragX += e.delta.x;

                        float baseSens = sensitivity > 0f
                            ? sensitivity
                            : 0.003f * Mathf.Max(1f, Mathf.Abs(st.startValue));

                        float modifier = 1f;
                        if (e.shift) modifier *= 0.1f;
                        if (e.control || e.command) modifier *= 10f;

                        float accel = 1f + 0.15f * Mathf.Clamp01(Mathf.Abs(st.dragX) / 50f);

                        float newValue = st.startValue + st.dragX * baseSens * modifier * accel;

                        if (min.HasValue) newValue = Mathf.Max(min.Value, newValue);
                        if (max.HasValue) newValue = Mathf.Min(max.Value, newValue);

                        if (!float.IsNaN(newValue) && !float.IsInfinity(newValue) && !Mathf.Approximately(newValue, value))
                        {
                            GUI.changed = true;
                            value = newValue;
                        }

                        e.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        GUIUtility.hotControl = 0;
                        EditorGUIUtility.SetWantsMouseJumping(0);
                        s_Drag.Remove(id);
                        e.Use();
                    }
                    break;

                case EventType.KeyDown:
                    // 可选：ESC 取消拖拽并还原
                    if (GUIUtility.hotControl == id && e.keyCode == KeyCode.Escape && s_Drag.TryGetValue(id, out var st2))
                    {
                        value = st2.startValue;
                        GUI.changed = true;

                        GUIUtility.hotControl = 0;
                        EditorGUIUtility.SetWantsMouseJumping(0);
                        s_Drag.Remove(id);
                        e.Use();
                    }
                    break;
            }

            return value;
        }

        public static float GetSensitivityByRange(float min, float max)
        {
            float range = Mathf.Abs(max - min);
            if (float.IsNaN(range) || float.IsInfinity(range) || Mathf.Approximately(range, 0f))
            {
                return -1f;
            }

            return range * 0.003f;
        }

    }

}
