using System;
using NBShader;
using UnityEditor;
using UnityEngine;

namespace NBShaderEditor
{
    internal sealed class ProgramNoiseFeatureItem : FeatureToggleFoldOutItem
    {
        public ProgramNoiseFeatureItem(NBShaderRootItem rootItem, ShaderGUIItem parentItem)
            : base(rootItem, parentItem, "_ProgramNoiseBlockFoldOut", "_ProgramNoise_Toggle", "程序化噪波", keyword: "_PROGRAM_NOISE")
        {
            new NBShaderKeywordToggleItem(
                rootItem,
                this,
                "_NB_Debug_PNoise",
                "NB_DEBUG_PNOISE",
                () => Content("程序化噪波测试颜色"),
                isVisible: null);
            new UVModeSelectItem(rootItem, this, "_ProgramNoiseUVModeFoldOut", NBShaderFlags.FLAG_BIT_UVMODE_POS_0_PROGRAM_NOISE, 0, () => Content("程序噪波UV来源"), forceEnable: true);
            ShaderGUIFloatItem programNoiseRotateItem = new ShaderGUIFloatItem(rootItem, this)
            {
                PropertyName = "_ProgramNoise_Rotate",
                GuiContent = Content("程序化噪波旋转")
            };
            programNoiseRotateItem.InitTriggerByChild();

            PropertyToggleBlockItem simpleBlock = ToggleBlock(rootItem, "_ProgramNoiseSimpleFoldOut", "_ProgramNoise_Simple_Toggle", "Perlin噪波",
                parent: this, keyword: "_PROGRAM_NOISE_SIMPLE");
            new Vector2LineItem(rootItem, simpleBlock, "_DissolveVoronoi_Vec", true, () => Content("噪波1缩放"));
            new VectorComponentItem(rootItem, simpleBlock, "_DissolveVoronoi_Vec2", 2, () => Content("噪波1速度"), false);
            new Vector2LineItem(rootItem, simpleBlock, "_DissolveVoronoi_Vec4", true, () => Content("噪波1偏移"));
            new Vector2LineItem(rootItem, simpleBlock, "_DissolveVoronoi_Vec3", true, () => Content("噪波1偏移速度"));
            new CustomDataSelectItem(rootItem, simpleBlock, NBShaderFlags.FLAGBIT_POS_2_CUSTOMDATA_DISSOLVE_NOISE1_OFFSET_X, 2, () => Content("噪波1偏移速度X自定义曲线"));
            new CustomDataSelectItem(rootItem, simpleBlock, NBShaderFlags.FLAGBIT_POS_2_CUSTOMDATA_DISSOLVE_NOISE1_OFFSET_Y, 2, () => Content("噪波1偏移速度Y自定义曲线"));

            PropertyToggleBlockItem voronoiBlock = ToggleBlock(rootItem, "_ProgramNoiseVoronoiFoldOut", "_ProgramNoise_Voronoi_Toggle", "Voronoi噪波",
                parent: this, keyword: "_PROGRAM_NOISE_VORONOI");
            new Vector2LineItem(rootItem, voronoiBlock, "_DissolveVoronoi_Vec", false, () => Content("噪波2缩放"));
            new VectorComponentItem(rootItem, voronoiBlock, "_DissolveVoronoi_Vec2", 3, () => Content("噪波2速度"), false);
            new Vector2LineItem(rootItem, voronoiBlock, "_DissolveVoronoi_Vec4", false, () => Content("噪波2偏移"));
            new Vector2LineItem(rootItem, voronoiBlock, "_DissolveVoronoi_Vec3", false, () => Content("噪波2偏移速度"));
            new CustomDataSelectItem(rootItem, voronoiBlock, NBShaderFlags.FLAGBIT_POS_2_CUSTOMDATA_DISSOLVE_NOISE2_OFFSET_X, 2, () => Content("噪波2偏移速度X自定义曲线"));
            new CustomDataSelectItem(rootItem, voronoiBlock, NBShaderFlags.FLAGBIT_POS_2_CUSTOMDATA_DISSOLVE_NOISE2_OFFSET_Y, 2, () => Content("噪波2偏移速度Y自定义曲线"));
            new VectorComponentItem(rootItem, this, "_DissolveVoronoi_Vec2", 0, () => Content("噪波1和噪波2混合系数"), true);
            new PNoiseBlendModeItem(
                rootItem,
                this,
                NBShaderFlags.FLAG_BIT_PNOISE_BLEND_POS_0_BASE_BLEND,
                "_ProgramNoiseBaseBlendOpacity",
                () => Content("两种程序噪波混合"),
                () => rootItem.Context.ProgramNoiseEnabled == MixedBool.True &&
                      rootItem.Context.IsToggleOn("_ProgramNoise_Simple_Toggle") &&
                      rootItem.Context.IsToggleOn("_ProgramNoise_Voronoi_Toggle"));
            InitTriggerByChild();
        }
    }
}
