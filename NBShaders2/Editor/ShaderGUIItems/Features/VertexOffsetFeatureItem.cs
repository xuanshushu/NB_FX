using System;
using NBShader;
using UnityEditor;
using UnityEngine;

namespace NBShaderEditor
{
    internal sealed class VertexOffsetFeatureItem : FeatureToggleFoldOutItem
    {
        private const string DirectionModeProperty = "_VertexOffset_NormalDir_Toggle";
        private const string IgnoreVertexColorProperty = "_IgnoreVetexColor_Toggle";
        private static readonly string[] DirectionModeNames = { "自定义方向", "顶点法线方向", "顶点色 RGB", "贴图 RGB" };
        private static readonly string[] DirectionSpaceNames = { "本地空间", "世界空间" };

        public VertexOffsetFeatureItem(NBShaderRootItem rootItem, ShaderGUIItem parentItem)
            : base(rootItem, parentItem, "_VertexOffsetBlockFoldOut", "_VertexOffset_Toggle", "顶点偏移", keyword: "_VERTEX_OFFSET")
        {
            new NBShaderKeywordToggleItem(
                rootItem,
                this,
                "_NB_Debug_VertexOffset",
                "NB_DEBUG_VERTEX_OFFSET",
                () => Content("顶点偏移方向测试"),
                isVisible: null);
            new VertexOffsetPopupItem(rootItem, this, DirectionModeProperty, "顶点偏移方向模式", DirectionModeNames);
            Func<bool> showOffsetMap = () => IsPropertyMode(rootItem, DirectionModeProperty, 0, 1, 3);
            Func<bool> showScalarChannel = () => IsPropertyMode(rootItem, DirectionModeProperty, 0, 1);
            Func<bool> showRGBDirection = () => IsPropertyMode(rootItem, DirectionModeProperty, 2, 3);
            Func<bool> showCustomDirection = () => IsPropertyMode(rootItem, DirectionModeProperty, 0);
            new VertexOffsetPopupItem(rootItem, this, "_VertexOffset_DirectionSpace", "顶点偏移RGB方向空间", DirectionSpaceNames, showRGBDirection);
            new VertexColorWarningItem(rootItem, this);
            new Vector3Item(rootItem, this, "_VertexOffset_CustomDir", () => Content("顶点偏移本地方向"), isVisible: showCustomDirection);
            AddTextureWithWrap(rootItem, this, "_VertexOffset_Map", "顶点偏移贴图", NBShaderFlags.FLAG_BIT_WRAPMODE_VERTEXOFFSETMAP, 0, isVisible: showOffsetMap);
            new ColorChannelSelectItem(rootItem, this, NBShaderFlags.FLAG_BIT_COLOR_CHANNEL_POS_0_VERTEX_OFFSET_MAP, 0, () => Content("顶点偏移贴图通道选择"), showScalarChannel);
            new UVModeSelectItem(rootItem, this, "_VertexOffsetUVModeFoldOut", NBShaderFlags.FLAG_BIT_UVMODE_POS_0_VERTEX_OFFSET_MAP, 0, () => Content("顶点偏移贴图UV来源"), "_VertexOffset_Map", isVisible: showOffsetMap);
            new CustomDataSelectItem(rootItem, this, NBShaderFlags.FLAGBIT_POS_1_CUSTOMDATA_VERTEX_OFFSET_X, 1, () => Content("顶点扰动X轴偏移自定义曲线"), showOffsetMap);
            new CustomDataSelectItem(rootItem, this, NBShaderFlags.FLAGBIT_POS_1_CUSTOMDATA_VERTEX_OFFSET_Y, 1, () => Content("顶点扰动Y轴偏移自定义曲线"), showOffsetMap);
            new Vector2LineItem(rootItem, this, "_VertexOffset_Vec", true, () => Content("顶点偏移动画"), isVisible: showOffsetMap);
            new VectorComponentItem(rootItem, this, "_VertexOffset_Vec", 2, () => Content("顶点偏移强度"), false);
            new CustomDataSelectItem(rootItem, this, NBShaderFlags.FLAGBIT_POS_1_CUSTOMDATA_VERTEXOFFSET_INTENSITY, 1, () => Content("顶点扰动强度自定义曲线"));
            new ToggleItem(
                rootItem,
                this,
                "_VertexOffset_StartFromZero",
                () => Content("顶点偏移从零开始"),
                enabled => rootItem.SyncService.ApplyToggleFlag(NBShaderFlags.FLAG_BIT_PARTICLE_1_VERTEXOFFSET_START_FROM_ZERO, enabled, 1));

            PropertyToggleBlockItem maskBlock = ToggleBlock(rootItem, "_VertexOffsetMaskBlockFoldOut", "_VertexOffset_Mask_Toggle", "顶点偏移遮罩",
                parent: this, keyword: "_VERTEX_OFFSET_MASKMAP");
            AddTextureWithWrap(rootItem, maskBlock, "_VertexOffset_MaskMap", "顶点偏移遮罩图", NBShaderFlags.FLAG_BIT_WRAPMODE_VERTEXOFFSET_MASKMAP, 0);
            new ColorChannelSelectItem(rootItem, maskBlock, NBShaderFlags.FLAG_BIT_COLOR_CHANNEL_POS_0_VERTEX_OFFSET_MASKMAP, 0, () => Content("顶点偏移遮罩图通道选择"));
            new UVModeSelectItem(rootItem, maskBlock, "_VertexOffsetMaskUVModeFoldOut", NBShaderFlags.FLAG_BIT_UVMODE_POS_0_VERTEX_OFFSET_MASKMAP, 0, () => Content("顶点偏移遮罩图UV来源"), "_VertexOffset_MaskMap");
            new CustomDataSelectItem(rootItem, maskBlock, NBShaderFlags.FLAGBIT_POS_3_CUSTOMDATA_VERTEX_OFFSET_MASK_X, 3, () => Content("顶点扰动遮罩X轴偏移自定义曲线"));
            new CustomDataSelectItem(rootItem, maskBlock, NBShaderFlags.FLAGBIT_POS_3_CUSTOMDATA_VERTEX_OFFSET_MASK_Y, 3, () => Content("顶点扰动遮罩Y轴偏移自定义曲线"));
            new Vector2LineItem(rootItem, maskBlock, "_VertexOffset_MaskMap_Vec", true, () => Content("顶点偏移遮罩动画"));
            new VectorComponentItem(rootItem, maskBlock, "_VertexOffset_MaskMap_Vec", 2, () => Content("顶点偏移遮罩强度"), true);
            InitTriggerByChild();
        }

        private sealed class VertexOffsetPopupItem : ShaderGUIPopUpItem
        {
            public VertexOffsetPopupItem(NBShaderRootItem rootItem, ShaderGUIItem parentItem,
                string propertyName, string label, string[] options, Func<bool> isVisible = null)
                : base(rootItem, parentItem, propertyName, () => Content(label),
                    () => PopupOptions(propertyName, options), isVisible: isVisible)
            {
            }

            public override void DrawController()
            {
                // Write only on an explicit choice, including a mixed selection choosing the first value.
                EditorGUI.BeginChangeCheck();
                int value = EditorGUI.Popup(ControlRect, Mathf.RoundToInt(PropertyInfo.Property.floatValue), PopUpNames);
                if (EditorGUI.EndChangeCheck())
                {
                    RootItem.MatEditor.RegisterPropertyChangeUndo(GuiContent.text);
                    PropertyInfo.Property.floatValue = value;
                }
            }

            public override void CheckIsPropertyModified(bool isCallByChild = false)
            {
                if (PropertyInfo == null)
                    return;

                float defaultValue = RootItem.Shader.GetPropertyDefaultFloatValue(PropertyInfo.Index);
                PropertyIsDefaultValue = !PropertyInfo.Property.hasMixedValue &&
                                         Mathf.Approximately(PropertyInfo.Property.floatValue, defaultValue);
                HasModified = !PropertyIsDefaultValue;
                ParentItem?.CheckIsPropertyModified(true);
            }
        }

        private sealed class VertexColorWarningItem : ShaderGUIItem
        {
            public VertexColorWarningItem(NBShaderRootItem rootItem, ShaderGUIItem parentItem)
                : base(rootItem, parentItem)
            {
            }

            private static bool NeedsRepair(Material material)
            {
                return material != null && material.HasProperty(DirectionModeProperty) &&
                       material.HasProperty(IgnoreVertexColorProperty) &&
                       material.GetFloat("_VertexOffset_Toggle") > 0.5f &&
                       Mathf.RoundToInt(material.GetFloat(DirectionModeProperty)) == 2 &&
                       material.GetFloat(IgnoreVertexColorProperty) <= 0.5f;
            }

            public override void OnGUI()
            {
                bool needsRepair = false;
                foreach (Material material in RootItem.Mats)
                    needsRepair |= NeedsRepair(material);
                if (!needsRepair)
                    return;

                using (ParentControlDisabledScope())
                {
                    DrawLayoutHelpBox(Text("feature.vertexOffset.vertexColorWarning.message",
                        "顶点色 RGB 正用于偏移方向，同时仍参与染色。可启用全局“忽略顶点色”；该开关也会忽略顶点 Alpha。"), MessageType.Warning);
                    GUIContent repairContent = Content("启用忽略顶点色");
                    if (GUI.Button(ApplyGlobalRectCompensation(LayoutRect()), repairContent))
                    {
                        foreach (Material material in RootItem.Mats)
                        {
                            if (!NeedsRepair(material))
                                continue;

                            Undo.RecordObject(material, repairContent.text);
                            material.SetFloat(IgnoreVertexColorProperty, 1f);
                            NBShaderSyncService.SyncMaterialState(material);
                            EditorUtility.SetDirty(material);
                        }

                        RootItem.MatEditor.PropertiesChanged();
                        RootItem.MatEditor.Repaint();
                        GUI.changed = true;
                    }
                }
            }
        }
    }
}
