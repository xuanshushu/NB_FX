using UnityEditor;
using UnityEngine;
using System;

namespace NBShaderEditor
{
    public class ShaderGUIFloatItem:ShaderGUIItem
    {
        public ShaderGUIFloatItem(ShaderGUIRootItem rootItem, ShaderGUIItem parentItem) :
            base(rootItem, parentItem: parentItem)
        {
            base.InitTriggerByChild();
        }

        public override void DrawController()
        {
            PropertyInfo.Property.floatValue = DraggableLabelFloat.Handle(LabelRect, PropertyInfo.Property.floatValue, sensitivity: -1f);//拖动Label控件可以操作Float参数
            PropertyInfo.Property.floatValue = EditorGUI.FloatField(ControlRect, PropertyInfo.Property.floatValue);
        }
        
    }
    
    public class ShaderGUISliderItem:ShaderGUIItem
    {
        public float Min = 0;
        public float Max = 1;
        public string RangePropertyName;
        ShaderPropertyInfo _rangePropertyInfo;
        private const float RangeWidth = 50f;

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

        public ShaderGUISliderItem(ShaderGUIRootItem rootItem, ShaderGUIItem parentItem) :
            base(rootItem, parentItem: parentItem) { }

        public override void DrawController()
        {
            if (_rangePropertyInfo != null)
            {
                Rect minRect = ControlRect;
                minRect.width = RangeWidth;
                Rect maxRect = ControlRect;
                maxRect.x += maxRect.width;
                maxRect.x -= RangeWidth;
                maxRect.width = RangeWidth;
                ControlRect.x += RangeWidth;
                ControlRect.width -= 2 * RangeWidth;
                
                RangeVecHasMixedValue(out bool minValueHasMixed,out bool maxValueHasMixed);
                
                Vector4 rangeVector = _rangePropertyInfo.Property.vectorValue;
                float min = rangeVector.x;
                float max = rangeVector.y;
                EditorGUI.showMixedValue = minValueHasMixed;

                GUI.backgroundColor = RootItem.DefaultBackgroundColor;
                min = EditorGUI.FloatField(minRect, min);
                EditorGUI.showMixedValue = maxValueHasMixed;
                max = EditorGUI.FloatField(maxRect, max);
                rangeVector.x = min;
                rangeVector.y = max;
                _rangePropertyInfo.Property.vectorValue = rangeVector;
                EditorGUI.showMixedValue = PropertyInfo.Property.hasMixedValue;
                if (IsPropertyAnimated(PropertyName)) GUI.backgroundColor = RootItem.AnimatedBackgroundColor;
                PropertyInfo.Property.floatValue = EditorGUI.Slider(ControlRect, PropertyInfo.Property.floatValue, min, max);
                GUI.backgroundColor = RootItem.DefaultBackgroundColor;
                EditorGUI.showMixedValue = false;

            }
            else
            {
                PropertyInfo.Property.floatValue = EditorGUI.Slider(ControlRect, PropertyInfo.Property.floatValue,Min,Max);
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

    }

}