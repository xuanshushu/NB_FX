using System;
using NBShader;
using UnityEditor;
using UnityEngine;

namespace NBShaderEditor
{
    internal abstract class FeatureToggleFoldOutItem : PropertyToggleBlockItem
    {
        private const string LocalizationTableName = NBShaderInspectorLocalization.TableName;

        protected FeatureToggleFoldOutItem(
            NBShaderRootItem rootItem,
            ShaderGUIItem parentItem,
            string foldOutPropertyName,
            string togglePropertyName,
            string label,
            int flagBits = 0,
            int flagIndex = 0,
            string keyword = null,
            Action<bool> onValueChanged = null,
            Func<bool> isVisible = null,
            bool bold = true)
            : base(
                rootItem,
                parentItem,
                foldOutPropertyName,
                togglePropertyName,
                () => Content(label),
                flagBits,
                flagIndex,
                keyword,
                onValueChanged: onValueChanged,
                isVisible: isVisible,
                bold: bold)
        {
        }

        protected TextureRelatedFoldOutItem AddTextureWithRelatedFoldOut(
            NBShaderRootItem rootItem,
            ShaderGUIItem parent,
            string texturePropertyName,
            string label,
            string foldOutPropertyName,
            int wrapFlag,
            string colorPropertyName = null,
            Func<bool> isVisible = null)
        {
            new TextureItem(
                rootItem,
                parent,
                texturePropertyName,
                () => Content(label),
                colorPropertyName,
                isVisible: isVisible,
                tillingContentProvider: TillingContent,
                offsetContentProvider: OffsetContent);
            TextureRelatedFoldOutItem relatedFoldOut = new TextureRelatedFoldOutItem(
                rootItem,
                parent,
                foldOutPropertyName,
                texturePropertyName,
                () => Content(label + "相关功能"),
                isVisible);
            new WrapModeItem(rootItem, relatedFoldOut, wrapFlag, () => Content(label + " Wrap"), 2);
            return relatedFoldOut;
        }

        protected void AddTextureWithWrap(
            NBShaderRootItem rootItem,
            ShaderGUIItem parent,
            string texturePropertyName,
            string label,
            int wrapFlag,
            string colorPropertyName = null,
            Func<bool> isVisible = null)
        {
            new TextureItem(
                rootItem,
                parent,
                texturePropertyName,
                () => Content(label),
                colorPropertyName,
                isVisible: isVisible,
                tillingContentProvider: TillingContent,
                offsetContentProvider: OffsetContent);
            new WrapModeItem(rootItem, parent, wrapFlag, () => Content(label + " Wrap"), 2, isVisible);
        }

        protected PropertyToggleBlockItem ToggleBlock(
            NBShaderRootItem rootItem,
            string foldOutPropertyName,
            string togglePropertyName,
            string label,
            int flagBits = 0,
            int flagIndex = 0,
            ShaderGUIItem parent = null,
            string keyword = null,
            Action<bool> onValueChanged = null,
            Func<bool> isVisible = null,
            bool bold = false)
        {
            return new PropertyToggleBlockItem(
                rootItem,
                parent ?? this,
                foldOutPropertyName,
                togglePropertyName,
                () => Content(label),
                flagBits,
                flagIndex,
                keyword,
                onValueChanged: onValueChanged,
                isVisible: isVisible,
                bold: bold);
        }

        protected static void AddGradient(
            NBShaderRootItem rootItem,
            ShaderGUIItem parent,
            string label,
            string countPropertyName,
            string colorPrefix,
            string alphaPrefix,
            bool hdr = false,
            Func<bool> isVisible = null)
        {
            new GradientItem(
                rootItem,
                parent,
                countPropertyName,
                6,
                BuildPropertyNames(colorPrefix, 6),
                BuildPropertyNames(alphaPrefix, 3),
                () => Content(label),
                hdr,
                ColorSpace.Gamma,
                isVisible);
        }

        protected static string[] BuildPropertyNames(string prefix, int count)
        {
            string[] names = new string[count];
            for (int i = 0; i < count; i++)
            {
                names[i] = prefix + i;
            }

            return names;
        }

        protected static bool IsPropertyMode(NBShaderRootItem rootItem, string propertyName, int expectedMode)
        {
            return rootItem.PropertyInfoDic.TryGetValue(propertyName, out ShaderPropertyInfo info) &&
                   !info.Property.hasMixedValue &&
                   Mathf.RoundToInt(info.Property.floatValue) == expectedMode;
        }

        protected static bool IsPropertyMode(NBShaderRootItem rootItem, string propertyName, params int[] expectedModes)
        {
            if (!rootItem.PropertyInfoDic.TryGetValue(propertyName, out ShaderPropertyInfo info) ||
                info.Property.hasMixedValue)
            {
                return false;
            }

            int value = Mathf.RoundToInt(info.Property.floatValue);
            for (int i = 0; i < expectedModes.Length; i++)
            {
                if (value == expectedModes[i])
                {
                    return true;
                }
            }

            return false;
        }

        internal static GUIContent Content(string label)
        {
            return LocalizedContent(LocalizationTableName, "inspector.feature." + label + ".label", label);
        }

        protected static GUIContent TillingContent()
        {
            return LocalizedInspectorContent(LocalizationTableName, "common.tilling", "Tilling");
        }

        protected static GUIContent OffsetContent()
        {
            return LocalizedInspectorContent(LocalizationTableName, "common.offset", "Offset");
        }

        internal static string Text(string key, string fallback)
        {
            return LocalizedText(LocalizationTableName, key, fallback);
        }

        internal static string[] PopupOptions(string propertyName, string[] fallback)
        {
            return LocalizedOptions(LocalizationTableName, "feature.popup." + propertyName, fallback);
        }
    }

    internal sealed class FeaturePopupItem : ShaderGUIPopUpItem
    {
        public FeaturePopupItem(
            ShaderGUIRootItem rootItem,
            ShaderGUIItem parentItem,
            string propertyName,
            Func<GUIContent> contentProvider,
            string[] popupNames,
            Action<MaterialProperty> onValueChanged = null,
            Func<bool> isVisible = null)
            : base(
                rootItem,
                parentItem,
                propertyName,
                contentProvider,
                () => FeatureToggleFoldOutItem.PopupOptions(propertyName, popupNames),
                onValueChanged,
                isVisible)
        {
        }
    }
}
