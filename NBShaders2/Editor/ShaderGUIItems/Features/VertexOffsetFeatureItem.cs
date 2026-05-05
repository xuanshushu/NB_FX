using System;
using NBShader;
using UnityEditor;
using UnityEngine;

namespace NBShaderEditor
{
    internal sealed class VertexOffsetFeatureItem : FeatureToggleFoldOutItem
    {
        public VertexOffsetFeatureItem(NBShaderRootItem rootItem, ShaderGUIItem parentItem)
            : base(rootItem, parentItem, "_VertexOffsetBlockFoldOut", "_VertexOffset_Toggle", "顶点偏移", NBShaderFlags.FLAG_BIT_PARTICLE_VERTEX_OFFSET_ON)
        {
            new ToggleItem(rootItem, this, "_NB_Debug_VertexOffset", () => Content("顶点偏移方向测试"), enabled => rootItem.SyncService.ApplyToggleKeyword("NB_DEBUG_VERTEX_OFFSET", enabled));
            AddTextureWithWrap(rootItem, this, "_VertexOffset_Map", "顶点偏移贴图", NBShaderFlags.FLAG_BIT_WRAPMODE_VERTEXOFFSETMAP);
            new UVModeSelectItem(rootItem, this, "_VertexOffsetUVModeFoldOut", NBShaderFlags.FLAG_BIT_UVMODE_POS_0_VERTEX_OFFSET_MAP, 0, () => Content("顶点偏移贴图UV来源"), "_VertexOffset_Map");
            new CustomDataSelectItem(rootItem, this, NBShaderFlags.FLAGBIT_POS_1_CUSTOMDATA_VERTEX_OFFSET_X, 1, () => Content("顶点扰动X轴偏移自定义曲线"));
            new CustomDataSelectItem(rootItem, this, NBShaderFlags.FLAGBIT_POS_1_CUSTOMDATA_VERTEX_OFFSET_Y, 1, () => Content("顶点扰动Y轴偏移自定义曲线"));
            new Vector2LineItem(rootItem, this, "_VertexOffset_Vec", true, () => Content("顶点偏移动画"));
            new VectorComponentItem(rootItem, this, "_VertexOffset_Vec", 2, () => Content("顶点偏移强度"), false);
            new CustomDataSelectItem(rootItem, this, NBShaderFlags.FLAGBIT_POS_1_CUSTOMDATA_VERTEXOFFSET_INTENSITY, 1, () => Content("顶点扰动强度自定义曲线"));
            new ToggleItem(
                rootItem,
                this,
                "_VertexOffset_StartFromZero",
                () => Content("顶点偏移从零开始"),
                enabled => rootItem.SyncService.ApplyToggleFlag(NBShaderFlags.FLAG_BIT_PARTICLE_1_VERTEXOFFSET_START_FROM_ZERO, enabled, 1));
            new ToggleItem(
                rootItem,
                this,
                "_VertexOffset_NormalDir_Toggle",
                () => Content("顶点偏移使用法线方向"),
                enabled => rootItem.SyncService.ApplyToggleFlag(NBShaderFlags.FLAG_BIT_PARTICLE_VERTEX_OFFSET_NORMAL_DIR, enabled));
            Func<bool> showCustomDirection = () => IsPropertyOff(rootItem, "_VertexOffset_NormalDir_Toggle");
            new VectorComponentItem(rootItem, this, "_VertexOffset_CustomDir", 0, () => Content("顶点偏移本地方向X"), false, isVisible: showCustomDirection);
            new VectorComponentItem(rootItem, this, "_VertexOffset_CustomDir", 1, () => Content("顶点偏移本地方向Y"), false, isVisible: showCustomDirection);
            new VectorComponentItem(rootItem, this, "_VertexOffset_CustomDir", 2, () => Content("顶点偏移本地方向Z"), false, isVisible: showCustomDirection);

            PropertyToggleBlockItem maskBlock = ToggleBlock(rootItem, "_VertexOffsetMaskBlockFoldOut", "_VertexOffset_Mask_Toggle", "顶点偏移遮罩",
                NBShaderFlags.FLAG_BIT_PARTICLE_1_VERTEXOFFSET_MASKMAP, 1, parent: this);
            AddTextureWithWrap(rootItem, maskBlock, "_VertexOffset_MaskMap", "顶点偏移遮罩图", NBShaderFlags.FLAG_BIT_WRAPMODE_VERTEXOFFSET_MASKMAP);
            new UVModeSelectItem(rootItem, maskBlock, "_VertexOffsetMaskUVModeFoldOut", NBShaderFlags.FLAG_BIT_UVMODE_POS_0_VERTEX_OFFSET_MASKMAP, 0, () => Content("顶点偏移遮罩图UV来源"), "_VertexOffset_MaskMap");
            new CustomDataSelectItem(rootItem, maskBlock, NBShaderFlags.FLAGBIT_POS_3_CUSTOMDATA_VERTEX_OFFSET_MASK_X, 3, () => Content("顶点扰动遮罩X轴偏移自定义曲线"));
            new CustomDataSelectItem(rootItem, maskBlock, NBShaderFlags.FLAGBIT_POS_3_CUSTOMDATA_VERTEX_OFFSET_MASK_Y, 3, () => Content("顶点扰动遮罩Y轴偏移自定义曲线"));
            new Vector2LineItem(rootItem, maskBlock, "_VertexOffset_MaskMap_Vec", true, () => Content("顶点偏移遮罩动画"));
            new VectorComponentItem(rootItem, maskBlock, "_VertexOffset_MaskMap_Vec", 2, () => Content("顶点偏移遮罩强度"), true);
            InitTriggerByChild();
        }

        private static bool IsPropertyOff(NBShaderRootItem rootItem, string propertyName)
        {
            return rootItem.PropertyInfoDic.TryGetValue(propertyName, out ShaderPropertyInfo info) &&
                   !info.Property.hasMixedValue &&
                   info.Property.floatValue <= 0.5f;
        }
    }
}
