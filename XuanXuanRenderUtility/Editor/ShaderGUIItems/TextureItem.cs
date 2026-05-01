using System;
using UnityEditor;
using UnityEngine;

namespace NBShaderEditor
{
    public class TextureItem : ShaderGUIItem
    {
        private readonly string _colorPropertyName;
        private readonly bool _drawScaleOffset;
        private readonly Func<GUIContent> _contentProvider;
        private readonly Func<bool> _isVisible;
        private readonly Action<MaterialProperty> _afterDraw;

        public TextureItem(
            ShaderGUIRootItem rootItem,
            ShaderGUIItem parentItem,
            string texturePropertyName,
            Func<GUIContent> contentProvider,
            string colorPropertyName = null,
            bool drawScaleOffset = true,
            Action<MaterialProperty> afterDraw = null,
            Func<bool> isVisible = null) : base(rootItem, parentItem)
        {
            PropertyName = texturePropertyName;
            _colorPropertyName = colorPropertyName;
            _drawScaleOffset = drawScaleOffset;
            _contentProvider = contentProvider ?? (() => GUIContent.none);
            _isVisible = isVisible;
            _afterDraw = afterDraw;
            InitTriggerByChild();
        }

        public override void OnGUI()
        {
            if (_isVisible != null && !_isVisible())
            {
                return;
            }

            MaterialProperty textureProperty = PropertyInfo.Property;
            MaterialProperty colorProperty = null;
            if (!string.IsNullOrEmpty(_colorPropertyName) && RootItem.PropertyInfoDic.ContainsKey(_colorPropertyName))
            {
                colorProperty = RootItem.PropertyInfoDic[_colorPropertyName].Property;
            }

            RootItem.MatEditor.TexturePropertySingleLine(_contentProvider(), textureProperty, colorProperty);
            if (_drawScaleOffset)
            {
                RootItem.MatEditor.TextureScaleOffsetProperty(textureProperty);
            }

            _afterDraw?.Invoke(textureProperty);
        }
    }

    public class TexturePropertyGroupItem : ShaderGUIItem
    {
        private readonly TextureObjectItem _textureItem;
        private readonly ColorLineItem _colorItem;
        private readonly TextureScaleOffsetItem _scaleOffsetItem;
        private readonly Func<bool> _isVisible;

        public TexturePropertyGroupItem(
            ShaderGUIRootItem rootItem,
            ShaderGUIItem parentItem,
            string texturePropertyName,
            string colorPropertyName,
            Func<GUIContent> contentProvider,
            Func<bool> isVisible = null) : base(rootItem, parentItem)
        {
            _isVisible = isVisible;
            _textureItem = new TextureObjectItem(rootItem, this, texturePropertyName, contentProvider);
            _colorItem = string.IsNullOrEmpty(colorPropertyName)
                ? null
                : new ColorLineItem(rootItem, this, colorPropertyName, false, contentProvider);
            _scaleOffsetItem = new TextureScaleOffsetItem(rootItem, this, texturePropertyName, false);
            InitTriggerByChild();
        }

        public override void OnGUI()
        {
            if (_isVisible != null && !_isVisible())
            {
                return;
            }

            float singleLineHeight = EditorGUIUtility.singleLineHeight;
            float rowGap = EditorGUIUtility.standardVerticalSpacing;
            float textureFieldHeight = 3f * singleLineHeight + 2f * rowGap;
            float textureContentGap = EditorGUIUtility.standardVerticalSpacing;

            Rect textureGroupRect = ApplyGlobalRectCompensation(EditorGUILayout.GetControlRect(false, textureFieldHeight));
            Rect textureLabelRow = new Rect(textureGroupRect.x, textureGroupRect.y, textureGroupRect.width, singleLineHeight);
            Rect tillingRow = new Rect(textureGroupRect.x, textureGroupRect.y + singleLineHeight + rowGap, textureGroupRect.width, singleLineHeight);
            Rect offsetRow = new Rect(textureGroupRect.x, tillingRow.y + singleLineHeight + rowGap, textureGroupRect.width, singleLineHeight);
            Rect indentedTextureLabelRow = EditorGUI.IndentedRect(textureLabelRow);

            Rect textureRect = new Rect(indentedTextureLabelRow.x + 2f, textureLabelRow.y, textureFieldHeight, textureFieldHeight);
            textureRect.x -= 2f;
            textureRect.y += 2f;

            float contentX = textureRect.x + textureRect.width + textureContentGap;
            Rect textureLabelRect = MoveRectLeftKeepingRight(textureLabelRow, contentX);

            SplitLineRect(tillingRow, out _, out Rect tillingVec2Rect, out Rect tillingResetRect, false);
            Rect tillingLabelRect = MakeLabelRect(tillingRow, contentX, tillingVec2Rect.x);
            tillingVec2Rect = ApplyControlIndentCompensation(tillingVec2Rect);

            SplitLineRect(offsetRow, out _, out Rect offsetVec2Rect, out Rect offsetResetRect, false);
            Rect offsetLabelRect = MakeLabelRect(offsetRow, contentX, offsetVec2Rect.x);
            offsetVec2Rect = ApplyControlIndentCompensation(offsetVec2Rect);

            _textureItem.Draw(textureRect, textureLabelRect);
            _scaleOffsetItem.Draw(tillingLabelRect, tillingVec2Rect, tillingResetRect, offsetLabelRect, offsetVec2Rect, offsetResetRect);
            _colorItem?.OnGUI();
        }

        private static Rect MoveRectLeftKeepingRight(Rect rect, float x)
        {
            float xMax = rect.xMax;
            rect.x = Mathf.Min(x, xMax);
            rect.width = xMax - rect.x;
            return rect;
        }

        private static Rect MakeLabelRect(Rect rowRect, float labelX, float controlX)
        {
            Rect labelRect = rowRect;
            labelRect.x = Mathf.Min(labelX, controlX);
            labelRect.width = Mathf.Max(0f, controlX - labelRect.x);
            return labelRect;
        }

        private static Rect ApplyControlIndentCompensation(Rect rect)
        {
            rect.x -= ControlIndentCompensation;
            rect.width += ControlIndentCompensation;
            return rect;
        }
    }

    public class TextureObjectItem : ShaderGUIItem
    {
        private readonly Func<GUIContent> _contentProvider;
        private readonly Func<bool> _isVisible;

        public TextureObjectItem(
            ShaderGUIRootItem rootItem,
            ShaderGUIItem parentItem,
            string texturePropertyName,
            Func<GUIContent> contentProvider,
            Func<bool> isVisible = null) : base(rootItem, parentItem)
        {
            PropertyName = texturePropertyName;
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
            Draw(ControlRect, LabelRect, ResetRect);
            DrawBlock();
        }

        public void Draw(Rect textureRect, Rect labelAndResetRect)
        {
            SplitControlAndResetRect(labelAndResetRect, out Rect labelRect, out Rect resetRect, false);
            Draw(textureRect, labelRect, resetRect);
        }

        public void Draw(Rect textureRect, Rect labelRect, Rect resetRect)
        {
            ControlRect = textureRect;
            LabelRect = labelRect;
            ResetRect = resetRect;

            MaterialProperty property = PropertyInfo.Property;
            GUI.Label(LabelRect, _contentProvider(), EditorStyles.boldLabel);

            EditorGUI.showMixedValue = property.hasMixedValue;
            EditorGUI.BeginChangeCheck();
            bool animatedScope = BeginAnimatedPropertyBackground(ControlRect, property);
            Texture texture = (Texture)EditorGUI.ObjectField(ControlRect, property.textureValue, typeof(Texture2D));
            EndAnimatedPropertyBackground(animatedScope);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                property.textureValue = texture;
                OnEndChange();
            }

            DrawResetButton();
        }

        public override void CheckIsPropertyModified(bool isCallByChild = false)
        {
            if (!isCallByChild)
            {
                bool isDefaultValue = PropertyInfo == null ||
                                      (!PropertyInfo.Property.hasMixedValue && PropertyInfo.Property.textureValue == null);
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
            if (PropertyInfo != null)
            {
                PropertyInfo.Property.textureValue = null;
                PropertyIsDefaultValue = true;
            }

            HasModified = false;
            if (!isCallByParent)
            {
                ParentItem?.CheckIsPropertyModified(true);
            }
        }
    }

    public class TextureScaleOffsetItem : ShaderGUIItem
    {
        private static readonly GUIContent TillingContent = new GUIContent("Tilling");
        private static readonly GUIContent OffsetContent = new GUIContent("Offset");
        private static readonly Vector4 DefaultScaleOffset = new Vector4(1f, 1f, 0f, 0f);

        private readonly bool _isVectorProperty;
        private readonly Func<bool> _isVisible;

        public TextureScaleOffsetItem(
            ShaderGUIRootItem rootItem,
            ShaderGUIItem parentItem,
            string propertyName,
            bool isVectorProperty,
            Func<bool> isVisible = null) : base(rootItem, parentItem)
        {
            PropertyName = propertyName;
            _isVectorProperty = isVectorProperty;
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
            Rect tillingLabelRect = LabelRect;
            Rect tillingVec2Rect = ControlRect;
            Rect tillingResetRect = ResetRect;

            GetRect();
            Rect offsetLabelRect = LabelRect;
            Rect offsetVec2Rect = ControlRect;
            Rect offsetResetRect = ResetRect;

            Draw(tillingLabelRect, tillingVec2Rect, tillingResetRect, offsetLabelRect, offsetVec2Rect, offsetResetRect);
            DrawBlock();
        }

        public void Draw(
            Rect tillingLabelRect,
            Rect tillingVec2Rect,
            Rect tillingResetRect,
            Rect offsetLabelRect,
            Rect offsetVec2Rect,
            Rect offsetResetRect)
        {
            MaterialProperty property = PropertyInfo.Property;
            Vector4 scaleOffset = GetScaleOffset(property);

            GUI.Label(tillingLabelRect, TillingContent);
            Vector2 tilling = new Vector2(scaleOffset.x, scaleOffset.y);
            EditorGUI.showMixedValue = property.hasMixedValue;
            EditorGUI.BeginChangeCheck();
            bool tillingAnimatedScope = BeginAnimatedPropertyBackground(tillingVec2Rect, property);
            tilling = EditorGUI.Vector2Field(tillingVec2Rect, GUIContent.none, tilling);
            EndAnimatedPropertyBackground(tillingAnimatedScope);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                scaleOffset.x = tilling.x;
                scaleOffset.y = tilling.y;
                Apply(scaleOffset);
            }

            bool tillingModified = property.hasMixedValue || tilling != Vector2.one;
            if (GUI.Button(tillingResetRect, tillingModified ? "R" : string.Empty, tillingModified ? GUI.skin.button : GUI.skin.label))
            {
                scaleOffset.x = 1f;
                scaleOffset.y = 1f;
                Apply(scaleOffset);
            }

            GUI.Label(offsetLabelRect, OffsetContent);
            Vector2 offset = new Vector2(scaleOffset.z, scaleOffset.w);
            EditorGUI.showMixedValue = property.hasMixedValue;
            EditorGUI.BeginChangeCheck();
            bool offsetAnimatedScope = BeginAnimatedPropertyBackground(offsetVec2Rect, property);
            offset = EditorGUI.Vector2Field(offsetVec2Rect, GUIContent.none, offset);
            EndAnimatedPropertyBackground(offsetAnimatedScope);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                scaleOffset.z = offset.x;
                scaleOffset.w = offset.y;
                Apply(scaleOffset);
            }

            bool offsetModified = property.hasMixedValue || offset != Vector2.zero;
            if (GUI.Button(offsetResetRect, offsetModified ? "R" : string.Empty, offsetModified ? GUI.skin.button : GUI.skin.label))
            {
                scaleOffset.z = 0f;
                scaleOffset.w = 0f;
                Apply(scaleOffset);
            }
        }

        public override void CheckIsPropertyModified(bool isCallByChild = false)
        {
            if (!isCallByChild)
            {
                bool isDefaultValue = true;
                if (PropertyInfo != null)
                {
                    MaterialProperty property = PropertyInfo.Property;
                    isDefaultValue = !property.hasMixedValue && Approximately(GetScaleOffset(property), DefaultScaleOffset);
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

        public override void ExecuteReset(bool isCallByParent = false)
        {
            if (PropertyInfo != null)
            {
                SetScaleOffset(PropertyInfo.Property, DefaultScaleOffset);
                PropertyIsDefaultValue = true;
            }

            HasModified = false;
            if (!isCallByParent)
            {
                ParentItem?.CheckIsPropertyModified(true);
            }
        }

        private Vector4 GetScaleOffset(MaterialProperty property)
        {
            return _isVectorProperty ? property.vectorValue : property.textureScaleAndOffset;
        }

        private void Apply(Vector4 scaleOffset)
        {
            SetScaleOffset(PropertyInfo.Property, scaleOffset);
            OnEndChange();
        }

        private void SetScaleOffset(MaterialProperty property, Vector4 scaleOffset)
        {
            if (_isVectorProperty)
            {
                property.vectorValue = scaleOffset;
            }
            else
            {
                property.textureScaleAndOffset = scaleOffset;
            }
        }

        private static bool Approximately(Vector4 a, Vector4 b)
        {
            return Mathf.Approximately(a.x, b.x) &&
                   Mathf.Approximately(a.y, b.y) &&
                   Mathf.Approximately(a.z, b.z) &&
                   Mathf.Approximately(a.w, b.w);
        }
    }
}
