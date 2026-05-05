using System;
using NBShader;
using UnityEditor;
using UnityEngine;

namespace NBShaderEditor
{
    internal sealed class SharedUVFeatureItem : FeatureToggleFoldOutItem
    {
        public SharedUVFeatureItem(NBShaderRootItem rootItem, ShaderGUIItem parentItem)
            : base(rootItem, parentItem, "_SharedUVBlockFoldOut", "_SharedUVToggle", "公共UV", keyword: "_SHARED_UV")
        {
            new UVModeSelectItem(rootItem, this, "_SharedUVModeFoldOut", NBShaderFlags.FLAG_BIT_UVMODE_POS_0_SHAREDUV, 0, () => Content("公共UV来源"), forceEnable: true);
            new Vector2LineItem(rootItem, this, "_SharedUV_ST", true, () => Content("公共UV Tiling"));
            new Vector2LineItem(rootItem, this, "_SharedUV_ST", false, () => Content("公共UV Offset"));
            new Vector2LineItem(rootItem, this, "_SharedUV_Vec", true, () => Content("公共UV偏移速度"));
            new VectorComponentItem(rootItem, this, "_SharedUV_Vec", 2, () => Content("旋转"), false);
            new VectorComponentItem(rootItem, this, "_SharedUV_Vec", 3, () => Content("旋转速度"), false);
            new CustomDataSelectItem(rootItem, this, NBShaderFlags.FLAGBIT_POS_3_CUSTOMDATA_SHARED_UV_OFFSET_X, 3, () => Content("公共UV X轴偏移自定义曲线"));
            new CustomDataSelectItem(rootItem, this, NBShaderFlags.FLAGBIT_POS_3_CUSTOMDATA_SHARED_UV_OFFSET_Y, 3, () => Content("公共UV Y轴偏移自定义曲线"));
            InitTriggerByChild();
        }
    }
}
