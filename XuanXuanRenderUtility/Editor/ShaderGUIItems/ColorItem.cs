using System;
using UnityEditor;
using UnityEngine;

namespace NBShaderEditor
{
    public class ColorItem : ShaderGUIItem
    {
        private const float ColorFieldLeftInset = EditorGUIIndentWidth;

        private readonly Func<GUIContent> _contentProvider;
        private readonly Func<bool> _isVisible;

        public ColorItem(
            ShaderGUIRootItem rootItem,
            ShaderGUIItem parentItem,
            string propertyName,
            Func<GUIContent> contentProvider,
            Func<bool> isVisible = null) : base(rootItem, parentItem)
        {
            PropertyName = propertyName;
            _contentProvider = contentProvider ?? (() => GUIContent.none);
            _isVisible = isVisible;
            GuiContent = _contentProvider();
            InitTriggerByChild();
        }

        public override void OnGUI()
        {
            if (_isVisible != null && !_isVisible())
            {
                return;
            }

            GetRect(false);
            using (ParentControlDisabledScope())
            {
                EditorGUI.LabelField(LabelRect, GuiContent);
            }

            Color color = PropertyInfo.Property.colorValue;
            bool hdr = ShaderGUIUnityCompat.HasHdrFlag(PropertyInfo.Property);
            using (ParentControlDisabledScope())
            {
                EditorGUI.showMixedValue = PropertyInfo.Property.hasMixedValue;
                EditorGUI.BeginChangeCheck();
                Rect colorRect = GetLabeledColorFieldRect(ControlRect);
                bool animatedScope = BeginAnimatedPropertyBackground(colorRect, PropertyInfo.Property);
                using (new EditorGUIIndentLevelScope(0))
                {
                    color = EditorGUI.ColorField(colorRect, GUIContent.none, color, true, true, hdr);
                }
                EndAnimatedPropertyBackground(animatedScope);
                EditorGUI.showMixedValue = false;
                if (EditorGUI.EndChangeCheck())
                {
                    PropertyInfo.Property.colorValue = color;
                    OnEndChange();
                }
            }

            DrawResetButton();
            DrawBlock();
        }

        internal static Rect GetNoLabelColorFieldRect(Rect rect)
        {
            rect.x += ColorFieldLeftInset;
            rect.width = Mathf.Max(0f, rect.width - ColorFieldLeftInset);
            return rect;
        }

        internal static Rect GetLabeledColorFieldRect(Rect rect)
        {
            float leftPadding = EditorStyles.colorField.padding.left + ColorFieldLeftInset +1f;
            rect.x -= leftPadding;
            rect.width += leftPadding;
            return rect;
        }
    }

    public class ColorLineItem : ShaderGUIItem
    {
        private readonly Func<GUIContent> _contentProvider;
        private readonly bool _showLabel;
        private readonly Func<bool> _isVisible;

        public ColorLineItem(
            ShaderGUIRootItem rootItem,
            ShaderGUIItem parentItem,
            string propertyName,
            bool showLabel,
            Func<GUIContent> contentProvider = null,
            Func<bool> isVisible = null) : base(rootItem, parentItem)
        {
            PropertyName = propertyName;
            _showLabel = showLabel;
            _contentProvider = contentProvider ?? (() => GUIContent.none);
            _isVisible = isVisible;
            GuiContent = _showLabel ? _contentProvider() : GUIContent.none;
            InitTriggerByChild();
        }

        public override void OnGUI()
        {
            if (_isVisible != null && !_isVisible())
            {
                return;
            }

            Draw(ApplyGlobalRectCompensation(LayoutRect()));
            DrawBlock();
        }

        public void Draw(Rect rect)
        {
            BaseRect = rect;

            if (_showLabel)
            {
                SplitLineRect(rect, out LabelRect, out ControlRect, out ResetRect, false);
                using (ParentControlDisabledScope())
                {
                    EditorGUI.LabelField(LabelRect, GuiContent);
                }
            }
            else
            {
                LabelRect = new Rect(rect.x, rect.y, 0f, rect.height);
                SplitControlAndResetRect(rect, out ControlRect, out ResetRect, false);
            }

            MaterialProperty property = PropertyInfo.Property;
            Color color = property.colorValue;
            bool hdr = ShaderGUIUnityCompat.HasHdrFlag(property);
            using (ParentControlDisabledScope())
            {
                EditorGUI.showMixedValue = property.hasMixedValue;
                EditorGUI.BeginChangeCheck();
                Rect colorRect = _showLabel
                    ? ColorItem.GetLabeledColorFieldRect(ControlRect)
                    : ColorItem.GetNoLabelColorFieldRect(ControlRect);
                bool animatedScope = BeginAnimatedPropertyBackground(colorRect, property);
                using (new EditorGUIIndentLevelScope(0))
                {
                    color = EditorGUI.ColorField(colorRect, GUIContent.none, color, true, true, hdr);
                }
                EndAnimatedPropertyBackground(animatedScope);
                EditorGUI.showMixedValue = false;
                if (EditorGUI.EndChangeCheck())
                {
                    property.colorValue = color;
                    OnEndChange();
                }
            }

            DrawResetButton();
        }

        public override void CheckIsPropertyModified(bool isCallByChild = false)
        {
            if (!isCallByChild)
            {
                bool isDefaultValue = true;
                if (PropertyInfo != null)
                {
                    Vector4 defaultColor = RootItem.Shader.GetPropertyDefaultVectorValue(PropertyInfo.Index);
                    isDefaultValue = !PropertyInfo.Property.hasMixedValue &&
                                     Approximately(PropertyInfo.Property.colorValue, defaultColor);
                }

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

    }
}
