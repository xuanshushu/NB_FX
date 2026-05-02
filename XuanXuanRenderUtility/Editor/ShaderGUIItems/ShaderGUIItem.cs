using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
namespace NBShaderEditor
{
    public enum MixedBool
    {
        False = 0,
        True = 1,
        Mixed = -1
    }
    public class ShaderGUIItem
    {
        public const float MinLabelWidth = 120f;
        public const float LabelWidthRatio = 0.4f;
        public const float GlobalRectXOffset = -15f;
        public const float GlobalRectWidthExpansion = 15f;
        public const float UnityEditorGUIIndentWidth = 15f;
        public const float EditorGUIIndentWidth = 10f;
        public const float FoldOutArrowWidth = UnityEditorGUIIndentWidth;
        public const float ControlResetGap = 3f;
        public const float ControlIndentCompensation = 10f;
        public ShaderPropertyInfo PropertyInfo;
        public ShaderGUIItem ParentItem;
        public List<ShaderGUIItem> ChildrenItemList = new List<ShaderGUIItem>();
        public ShaderGUIRootItem RootItem;
        public string PropertyName;
        public GUIContent GuiContent;
        private static int _parentControlDisabledDepth;

        public virtual void InitTriggerByChild()//根Child一定要做这个事情
        {
            if ( PropertyInfo == null && PropertyName != null)
            {
                PropertyInfo = RootItem.PropertyInfoDic[PropertyName];
            }
            CheckIsPropertyModified();
        }
        public ShaderGUIItem(ShaderGUIRootItem rtItem,ShaderGUIItem parentItem=null)
        {
            RootItem = rtItem;
            if (parentItem != null)
            {
                ParentItem = parentItem;
                if (!parentItem.ChildrenItemList.Contains(this))
                {
                    ParentItem.ChildrenItemList.Add(this);
                }
            }
        }

        public Rect BaseRect;
        public Rect LabelRect;
        public Rect ControlRect;
        public Rect ResetRect;
        public static float ResetButtonSize => EditorGUIUtility.singleLineHeight;
        public virtual void GetRect(bool applyControlIndentCompensation = true)
        {
            BaseRect = ApplyGlobalRectCompensation(EditorGUILayout.GetControlRect());
            SplitLineRect(BaseRect, out LabelRect, out ControlRect, out ResetRect, applyControlIndentCompensation);
        }

        public static Rect ApplyGlobalRectCompensation(Rect rect)
        {
            rect.x += GlobalRectXOffset;
            rect.width = Mathf.Max(0f, rect.width + GlobalRectWidthExpansion);
            return rect;
        }

        public static Rect ApplyLabelIndentWidth(Rect rect)
        {
            float indentDelta = EditorGUI.indentLevel * (EditorGUIIndentWidth - UnityEditorGUIIndentWidth);
            rect.x += indentDelta;
            rect.width = Mathf.Max(0f, rect.width - indentDelta);
            return rect;
        }

        public static Rect ApplyDirectLabelIndentWidth(Rect rect)
        {
            float indentOffset = EditorGUI.indentLevel * EditorGUIIndentWidth;
            rect.x += indentOffset;
            rect.width = Mathf.Max(0f, rect.width - indentOffset);
            return rect;
        }

        public static float GetEditorLabelTextX(Rect labelRect)
        {
            return labelRect.x + EditorGUI.indentLevel * UnityEditorGUIIndentWidth;
        }

        public static float GetLabelWidth(Rect baseRect)
        {
            return Mathf.Max(MinLabelWidth, baseRect.width * LabelWidthRatio);
        }

        public static void SplitLineRect(
            Rect baseRect,
            out Rect labelRect,
            out Rect controlRect,
            out Rect resetRect,
            bool applyControlIndentCompensation = true)
        {
            float labelWidth = GetLabelWidth(baseRect);
            labelRect = baseRect;
            labelRect.width = labelWidth;
            labelRect = ApplyLabelIndentWidth(labelRect);

            Rect controlAndResetRect = baseRect;
            controlAndResetRect.x += labelWidth;
            controlAndResetRect.width = Mathf.Max(0f, controlAndResetRect.width - labelWidth);
            SplitControlAndResetRect(controlAndResetRect, out controlRect, out resetRect, applyControlIndentCompensation);
        }

        public static void SplitControlAndResetRect(
            Rect baseRect,
            out Rect controlRect,
            out Rect resetRect,
            bool applyControlIndentCompensation = true)
        {
            resetRect = baseRect;
            resetRect.x = baseRect.xMax - ResetButtonSize;
            resetRect.width = ResetButtonSize;

            float controlIndentCompensation = applyControlIndentCompensation ? ControlIndentCompensation : 0f;
            controlRect = baseRect;
            controlRect.x -= controlIndentCompensation;
            controlRect.width = Mathf.Max(0f, baseRect.width - ResetButtonSize - ControlResetGap + controlIndentCompensation);
        }

        
        public virtual void OnGUI()
        {
            GetRect();
            using (ParentControlDisabledScope())
            {
                EditorGUI.LabelField(LabelRect, GuiContent);
            }

            using (ParentControlDisabledScope())
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = PropertyInfo.Property.hasMixedValue;
                bool animatedPropertyScope = BeginAnimatedPropertyBackground(BaseRect, PropertyInfo.Property);
                using (new EditorGUIIndentLevelScope(0))
                {
                    DrawController();
                }
                EndAnimatedPropertyBackground(animatedPropertyScope);
                EditorGUI.showMixedValue = false;
                if (EditorGUI.EndChangeCheck())
                {
                    OnEndChange();
                }
            }

            DrawResetButton();
            DrawBlock();
        }

        public void DrawResetButton()//如果重写OnGUI，一定要记得调用DrawResetButton
        {
            CheckIsPropertyModified();
            _resetButtonContent.text = HasModified ? "R" : "";
            _resetButtonStyle = HasModified ? GUI.skin.button : GUI.skin.label;
            using (ParentControlDisabledScope())
            {
                if (GUI.Button(ResetRect, _resetButtonContent, _resetButtonStyle))
                {
                    ExecuteReset();
                }
            }
        }

        public virtual void DrawController()
        {
            
        }

        protected readonly struct EditorGUIIndentLevelScope : System.IDisposable
        {
            private readonly int _indentLevel;

            public EditorGUIIndentLevelScope(int indentLevel)
            {
                _indentLevel = EditorGUI.indentLevel;
                EditorGUI.indentLevel = indentLevel;
            }

            public void Dispose()
            {
                EditorGUI.indentLevel = _indentLevel;
            }
        }

        protected bool IsParentControlDisabled => _parentControlDisabledDepth > 0;

        protected EditorGUI.DisabledScope ParentControlDisabledScope(bool disabled = false)
        {
            return new EditorGUI.DisabledScope(IsParentControlDisabled || disabled);
        }

        protected readonly struct InheritedControlDisabledScope : System.IDisposable
        {
            private readonly bool _disabled;

            public InheritedControlDisabledScope(bool disabled)
            {
                _disabled = disabled;
                if (_disabled)
                {
                    _parentControlDisabledDepth++;
                }
            }

            public void Dispose()
            {
                if (_disabled)
                {
                    _parentControlDisabledDepth = Mathf.Max(0, _parentControlDisabledDepth - 1);
                }
            }
        }

        public virtual void DrawBlock()
        {
            
        }
        
        public virtual void OnEndChange()
        {
            CheckIsPropertyModified();
        }

        private readonly Dictionary<string, string> _animationPropertyPathDic = new Dictionary<string, string>();

        protected bool BeginAnimatedPropertyBackground(Rect totalPosition, MaterialProperty property)
        {
            if (property == null || RootItem?.MatEditor == null)
            {
                return false;
            }

            RootItem.MatEditor.BeginAnimatedCheck(totalPosition, property);
            return true;
        }

        protected void EndAnimatedPropertyBackground(bool scopeActive)
        {
            if (scopeActive && RootItem?.MatEditor != null)
            {
                RootItem.MatEditor.EndAnimatedCheck();
            }
        }

        public bool IsPropertyAnimated(string propertyName, params string[] componentNames)
         {
             if (propertyName == null) return false;
            if (AnimationMode.InAnimationMode())
            {
                foreach (var r in RootItem.RenderersUsingThisMaterial)
                {
                    if (componentNames != null && componentNames.Length > 0)
                    {
                        foreach (string componentName in componentNames)
                        {
                            if (AnimationMode.IsPropertyAnimated(r, GetAnimationPropertyPath(propertyName, componentName)))
                            {
                                return true;
                            }
                        }
                    }
                    else if (AnimationMode.IsPropertyAnimated(r, GetAnimationPropertyPath(propertyName, string.Empty)))
                    {
                        return true;
                    }
                }
            }
        
            return false;
        }

        private string GetAnimationPropertyPath(string propertyName, string componentName)
        {
            string key = string.IsNullOrEmpty(componentName) ? propertyName : propertyName + "." + componentName;
            if (!_animationPropertyPathDic.TryGetValue(key, out string propertyPath))
            {
                propertyPath = string.IsNullOrEmpty(componentName)
                    ? "material." + propertyName
                    : "material." + propertyName + "." + componentName;
                _animationPropertyPathDic.Add(key, propertyPath);
            }

            return propertyPath;
        }


        #region  ResetLogic


        private GUIContent _resetButtonContent = new GUIContent("R","重置当前属性及子集属性(如有)");
        private GUIStyle _resetButtonStyle;
        public bool HasModified = false;
        public bool PropertyIsDefaultValue = true;
        public virtual void CheckIsPropertyModified(bool isCallByChild = false)
        {
            if (!isCallByChild)//只有自身Reset了，才需要查询自己的状态是否正确
            {
                bool isDefaultValue = true;
                if (PropertyInfo != null)
                {
                    switch (PropertyInfo.Property.type)
                    {
                        case MaterialProperty.PropType.Float:
                        case MaterialProperty.PropType.Range:
                            float defaultValue = RootItem.Shader.GetPropertyDefaultFloatValue(PropertyInfo.Index);
                            isDefaultValue = Mathf.Approximately(PropertyInfo.Property.floatValue, defaultValue);
                        break;
                        case MaterialProperty.PropType.Color:
                            Vector4 defaultColor = RootItem.Shader.GetPropertyDefaultVectorValue(PropertyInfo.Index);
                            isDefaultValue = Approximately(PropertyInfo.Property.colorValue, defaultColor);
                            break;
                        case MaterialProperty.PropType.Vector:
                            Vector4 defaultVector = RootItem.Shader.GetPropertyDefaultVectorValue(PropertyInfo.Index);
                            isDefaultValue = Approximately(PropertyInfo.Property.vectorValue, defaultVector);
                            break;
                    }
                }
                else
                {
                    isDefaultValue = true;//如果没有Property，则是看子集的情况
                }

                if (isDefaultValue == PropertyIsDefaultValue)
                {
                    return;//如果状态没有改变，就不需要做任何操作
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
        
        public virtual void ExecuteReset(bool isCallByParent = false)
        {
            if (PropertyInfo != null)
            {
                switch (PropertyInfo.Property.type)
                {
                    case MaterialProperty.PropType.Float:
                    case MaterialProperty.PropType.Range:
                        float defaultValue = RootItem.Shader.GetPropertyDefaultFloatValue(PropertyInfo.Index);
                        PropertyInfo.Property.floatValue = defaultValue;
                        break;
                    case MaterialProperty.PropType.Color:
                        PropertyInfo.Property.colorValue = RootItem.Shader.GetPropertyDefaultVectorValue(PropertyInfo.Index);
                        break;
                    case MaterialProperty.PropType.Vector:
                        PropertyInfo.Property.vectorValue = RootItem.Shader.GetPropertyDefaultVectorValue(PropertyInfo.Index);
                        break;
                }
                PropertyIsDefaultValue = true;
            }
            
            foreach (var childItem in ChildrenItemList)
            {
                childItem.ExecuteReset(true);
            }
            HasModified = false;
            if (!isCallByParent)//直接由用户触发重置
            {
                ParentItem?.CheckIsPropertyModified(true);
            }
        }

        private static bool Approximately(Vector4 a, Vector4 b)
        {
            return Mathf.Approximately(a.x, b.x) &&
                   Mathf.Approximately(a.y, b.y) &&
                   Mathf.Approximately(a.z, b.z) &&
                   Mathf.Approximately(a.w, b.w);
        }
        
        #endregion
        
        
    }
}
