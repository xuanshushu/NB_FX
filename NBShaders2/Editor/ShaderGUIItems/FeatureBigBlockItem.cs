using System;
using NBShader;
using UnityEditor;
using UnityEngine;

namespace NBShaderEditor
{
    public class FeatureBigBlockItem : BigBlockItem
    {
        private static readonly string[] OnOffNames = { "关闭", "开启" };
        private static readonly string[] TextureGradientNames = { "贴图", "渐变" };
        private static readonly string[] DistortModeNames = { "FlowMap/RG贴图", "折射率" };
        private static readonly string[] ScreenDistortModeNames = { "No Screen Distort", "Deferred Distort", "Camera Opaque Distort" };
        private static readonly string[] RampSourceNames = { "渐变", "贴图" };
        private static readonly string[] BlendModeNames = { "叠加", "相乘" };
        private static readonly string[] FresnelModeNames = { "颜色", "透明" };
        private static readonly string[] DissolveMaskModeNames = { "Process Dissolve", "Dissolve Mask" };
        private static readonly string[] VatModeNames = { "Houdini", "TyFlow" };
        private static readonly string[] HoudiniVatSubModeNames =
        {
            "SoftBody (Deformation)",
            "RigidBody (Pieces)",
            "Dynamic Remeshing (Lookup)",
            "Particle Sprites (Billboard)"
        };
        private static readonly string[] TyFlowVatSubModeNames =
        {
            "Absolute positions",
            "Relative offsets",
            "Skin (R)",
            "Skin (PR)",
            "Skin (PRSAVE)",
            "Skin (PRSXYZ)"
        };

        public FeatureBigBlockItem(NBShaderRootItem rootItem, ShaderGUIItem parentItem)
            : base(
                rootItem,
                parentItem,
                "_FeatureBigBlockItemFoldOut",
                () => NBShaderInspectorLocalization.MakeContent(
                    "inspector.block.feature.label",
                    "特效功能",
                    "inspector.block.feature.tip",
                    "遮罩、扭曲、溶解等特效功能"))
        {
            AddMask(rootItem);
            AddNoiseAndDistort(rootItem);
            AddEmission(rootItem);
            AddColorBlend(rootItem);
            AddRampColor(rootItem);
            AddDissolve(rootItem);
            AddProgramNoise(rootItem);
            AddSharedUV(rootItem);
            AddFresnel(rootItem);
            AddVertexOffset(rootItem);
            AddDepthOutlineAndDecal(rootItem);
            AddParallax(rootItem);
            AddPortal(rootItem);
            AddFlipbook(rootItem);
            AddVat(rootItem);

            InitTriggerByChild();
        }

        public override void DrawBlock()
        {
            for (int i = 0; i < ChildrenItemList.Count; i++)
            {
                ChildrenItemList[i].OnGUI();
            }
        }

        private void AddMask(NBShaderRootItem rootItem)
        {
            PropertyToggleBlockItem maskBlock = ToggleBlock(
                rootItem,
                "_MaskBlockFoldOut",
                "_Mask_Toggle",
                "遮罩",
                keyword: "_MASKMAP_ON",
                bold: true);

            AddToggle(rootItem, maskBlock, "_NB_Debug_Mask", "测试遮罩颜色", enabled => rootItem.SyncService.ApplyToggleKeyword("NB_DEBUG_MASK", enabled));
            AddVectorComponent(rootItem, maskBlock, "_MaskMapVec", 0, "遮罩强度", true);

            PropertyToggleBlockItem refineBlock = ToggleBlock(
                rootItem,
                "_MaskRefineFoldOut",
                "_MaskRefineToggle",
                "遮罩整体调整",
                W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_MASK_REFINE,
                1,
                parent: maskBlock);
            AddVectorComponent(rootItem, refineBlock, "_MaskRefineVec", 0, "范围(Pow)", false);
            AddVectorComponent(rootItem, refineBlock, "_MaskRefineVec", 1, "相乘", false);
            AddVectorComponent(rootItem, refineBlock, "_MaskRefineVec", 2, "偏移(相加)", false);

            new PNoiseBlendModeItem(rootItem, maskBlock, W9ParticleShaderFlags.FLAG_BIT_PNOISE_BLEND_POS_0_MASK, "_MaskPNoiseBlendOpacity", () => Content("遮罩程序噪波混合"),
                () => rootItem.Context.ProgramNoiseEnabled == MixedBool.True);
            AddMaskMap(rootItem, maskBlock, "_MaskMap", "_MaskMapGradientToggle", "_MaskUVModeFoldOut", "遮罩",
                W9ParticleShaderFlags.FLAG_BIT_WRAPMODE_MASKMAP,
                W9ParticleShaderFlags.FLAG_BIT_COLOR_CHANNEL_POS_0_MASKMAP1,
                W9ParticleShaderFlags.FLAG_BIT_UVMODE_POS_0_MASKMAP,
                W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_MASKMAP_GRADIENT,
                1,
                "_MaskMapFoldOut");
            new CustomDataSelectItem(rootItem, maskBlock, W9ParticleShaderFlags.FLAGBIT_POS_0_CUSTOMDATA_MASK_OFFSET_X, 0, () => Content("Mask图X轴偏移自定义曲线"));
            new CustomDataSelectItem(rootItem, maskBlock, W9ParticleShaderFlags.FLAGBIT_POS_0_CUSTOMDATA_MASK_OFFSET_Y, 0, () => Content("Mask图Y轴偏移自定义曲线"));
            new Vector2LineItem(rootItem, maskBlock, "_MaskMapOffsetAnition", true, () => Content("遮罩偏移速度"));
            AddSlider(rootItem, maskBlock, "_MaskMapUVRotation", "遮罩旋转", 0f, 360f);
            PropertyToggleBlockItem rotateBlock = ToggleBlock(
                rootItem,
                "_MaskRotationFoldOut",
                "_Mask_RotationToggle",
                "遮罩旋转速度",
                W9ParticleShaderFlags.FLAG_BIT_PARTILCE_MASKMAPROTATIONANIMATION_ON,
                parent: maskBlock);
            AddFloat(rootItem, rotateBlock, "_MaskMapRotationSpeed", "旋转速度");
            ShaderGUIItem maskNoiseAffect = new NoiseAffectItem(rootItem, maskBlock);
            AddSlider(rootItem, maskNoiseAffect, "_MaskDistortion_intensity", "遮罩扭曲强度", rangePropertyName: "MaskDistortionIntensityRangeVec");

            PropertyToggleBlockItem mask2Block = ToggleBlock(
                rootItem,
                "_Mask2BlockFoldOut",
                "_Mask2_Toggle",
                "遮罩2",
                W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_MASK_MAP2,
                1,
                parent: maskBlock);
            AddMaskMap(rootItem, mask2Block, "_MaskMap2", "_MaskMap2GradientToggle", "_Mask2UVModeFoldOut", "遮罩2",
                W9ParticleShaderFlags.FLAG_BIT_WRAPMODE_MASKMAP2,
                W9ParticleShaderFlags.FLAG_BIT_COLOR_CHANNEL_POS_0_MASKMAP2,
                W9ParticleShaderFlags.FLAG_BIT_UVMODE_POS_0_MASKMAP_2,
                W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_MASKMAP_2_GRADIENT,
                1);
            AddVectorComponent(rootItem, mask2Block, "_MaskMapVec", 1, "遮罩2旋转", false);
            new Vector2LineItem(rootItem, mask2Block, "_MaskMapOffsetAnition", false, () => Content("遮罩2偏移速度"));

            PropertyToggleBlockItem mask3Block = ToggleBlock(
                rootItem,
                "_Mask3BlockFoldOut",
                "_Mask3_Toggle",
                "遮罩3",
                W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_MASK_MAP3,
                1,
                parent: maskBlock);
            AddMaskMap(rootItem, mask3Block, "_MaskMap3", "_MaskMap3GradientToggle", "_Mask3UVModeFoldOut", "遮罩3",
                W9ParticleShaderFlags.FLAG_BIT_WRAPMODE_MASKMAP3,
                W9ParticleShaderFlags.FLAG_BIT_COLOR_CHANNEL_POS_0_MASKMAP3,
                W9ParticleShaderFlags.FLAG_BIT_UVMODE_POS_0_MASKMAP_3,
                W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_MASKMAP_3_GRADIENT,
                1);
            AddVectorComponent(rootItem, mask3Block, "_MaskMapVec", 2, "遮罩3旋转", false);
            new Vector2LineItem(rootItem, mask3Block, "_MaskMap3OffsetAnition", true, () => Content("遮罩3偏移速度"));
        }

        private void AddNoiseAndDistort(NBShaderRootItem rootItem)
        {
            PropertyToggleBlockItem noiseBlock = ToggleBlock(
                rootItem,
                "_NoiseBlockFoldOut",
                "_noisemapEnabled",
                "扭曲",
                keyword: "_NOISEMAP",
                bold: true);
            AddToggle(rootItem, noiseBlock, "_NB_Debug_Distort", "扭曲强度值测试", enabled => rootItem.SyncService.ApplyToggleKeyword("NB_DEBUG_DISTORT", enabled));
            AddSlider(rootItem, noiseBlock, "_NoiseIntensity", "整体扭曲强度", rangePropertyName: "_NoiseIntensityRangeVec");
            new CustomDataSelectItem(rootItem, noiseBlock, W9ParticleShaderFlags.FLAGBIT_POS_1_CUSTOMDATA_NOISE_INTENSITY, 1, () => Content("扭曲强度自定义曲线"));
            new PopupItem(rootItem, noiseBlock, "_ScreenDistortModeToggle", () => Content("屏幕扰动模式"), ScreenDistortModeNames,
                property => rootItem.SyncService.ApplyScreenDistortMode(Mathf.RoundToInt(property.floatValue)),
                () => rootItem.Context.UIEffectEnabled != MixedBool.True);
            AddSlider(rootItem, noiseBlock, "_ScreenDistortIntensity", "屏幕扭曲强度", rangePropertyName: "_ScreenDistortIntensityRangeVec",
                isVisible: () => rootItem.Context.UIEffectEnabled != MixedBool.True && IsPropertyGreater(rootItem, "_ScreenDistortModeToggle", 0.5f));
            AddToggle(rootItem, noiseBlock, "_DisableMainPassToggle", "关闭主材质Pass", enabled =>
            {
                rootItem.SyncService.ApplyScreenDistortMode(GetIntProperty(rootItem, "_ScreenDistortModeToggle"));
            }, () => rootItem.Context.UIEffectEnabled != MixedBool.True && IsPropertyGreater(rootItem, "_ScreenDistortModeToggle", 0.5f));

            PropertyToggleBlockItem screenAlphaBlock = ToggleBlock(
                rootItem,
                "_ScreenDistortAlphaFoldOut",
                "_ScreenDistortAlphaRefineToggle",
                "屏幕扭曲Alpha整体调整",
                W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_SCREEN_DISTORT_ALPHA_REFINE,
                1,
                parent: noiseBlock,
                isVisible: () => rootItem.Context.UIEffectEnabled != MixedBool.True && IsPropertyGreater(rootItem, "_ScreenDistortModeToggle", 0.5f));
            AddFloat(rootItem, screenAlphaBlock, "_ScreenDistortAlphaPow", "范围(Pow)");
            AddFloat(rootItem, screenAlphaBlock, "_ScreenDistortAlphaMulti", "相乘");
            AddFloat(rootItem, screenAlphaBlock, "_ScreenDistortAlphaAdd", "偏移(相加)");

            new PopupItem(rootItem, noiseBlock, "_DistortMode", () => Content("扭曲模式"), DistortModeNames,
                property => rootItem.SyncService.ApplyToggleKeyword("_DISTORT_REFRACTION", property.floatValue > 0.5f));
            TextureRelatedFoldOutItem noiseMapRelatedFoldOut = AddTextureWithRelatedFoldOut(rootItem, noiseBlock, "_NoiseMap", "扭曲贴图", "_NoiseMapFoldOut", W9ParticleShaderFlags.FLAG_BIT_WRAPMODE_NOISEMAP,
                isVisible: () => IsPropertyMode(rootItem, "_DistortMode", 0));
            new UVModeSelectItem(rootItem, noiseMapRelatedFoldOut, "_NoiseUVModeFoldOut", W9ParticleShaderFlags.FLAG_BIT_UVMODE_POS_0_NOISE_MAP, 0, () => Content("扭曲贴图UV来源"), "_NoiseMap",
                isVisible: () => IsPropertyMode(rootItem, "_DistortMode", 0));
            new Vector2LineItem(rootItem, noiseMapRelatedFoldOut, "_DistortionDirection", true, () => Content("扭曲方向强度"), () => IsPropertyMode(rootItem, "_DistortMode", 0));
            new CustomDataSelectItem(rootItem, noiseMapRelatedFoldOut, W9ParticleShaderFlags.FLAGBIT_POS_2_CUSTOMDATA_NOISE_DIRECTION_X, 2, () => Content("扭曲方向强度X自定义曲线"), () => IsPropertyMode(rootItem, "_DistortMode", 0));
            new CustomDataSelectItem(rootItem, noiseMapRelatedFoldOut, W9ParticleShaderFlags.FLAGBIT_POS_2_CUSTOMDATA_NOISE_DIRECTION_Y, 2, () => Content("扭曲方向强度Y自定义曲线"), () => IsPropertyMode(rootItem, "_DistortMode", 0));
            AddSlider(rootItem, noiseMapRelatedFoldOut, "_NoiseMapUVRotation", "扭曲旋转", 0f, 360f, isVisible: () => IsPropertyMode(rootItem, "_DistortMode", 0));
            new Vector2LineItem(rootItem, noiseMapRelatedFoldOut, "_NoiseOffset", true, () => Content("扭曲偏移速度"), () => IsPropertyMode(rootItem, "_DistortMode", 0));
            AddToggle(rootItem, noiseMapRelatedFoldOut, "_DistortionBothDirection_Toggle", "0.5为中值，双向扭曲",
                enabled => rootItem.SyncService.ApplyToggleFlag(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_NOISEMAP_NORMALIZEED_ON, enabled),
                () => IsPropertyMode(rootItem, "_DistortMode", 0));
            AddSlider(rootItem, noiseBlock, "_RefractionIOR", "折射率", 0f, 5f, isVisible: () => IsPropertyMode(rootItem, "_DistortMode", 1));
            new PNoiseBlendModeItem(rootItem, noiseBlock, W9ParticleShaderFlags.FLAG_BIT_PNOISE_BLEND_POS_0_DISTORT, "_DistortPNoiseBlendOpacity", () => Content("扭曲程序噪波混合"),
                () => rootItem.Context.ProgramNoiseEnabled == MixedBool.True);

            PropertyToggleBlockItem noiseMaskBlock = ToggleBlock(
                rootItem,
                "_NoiseMaskBlockFoldOut",
                "_noiseMaskMap_Toggle",
                "扭曲遮罩",
                W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_NOISE_MASKMAP,
                1,
                parent: noiseBlock);
            AddTextureWithWrap(rootItem, noiseMaskBlock, "_NoiseMaskMap", "扭曲遮罩贴图", W9ParticleShaderFlags.FLAG_BIT_WRAPMODE_NOISE_MASKMAP);
            new ColorChannelSelectItem(rootItem, noiseMaskBlock, W9ParticleShaderFlags.FLAG_BIT_COLOR_CHANNEL_POS_0_NOISE_MASK, 0, () => Content("扭曲遮罩图通道选择"));
            new UVModeSelectItem(rootItem, noiseMaskBlock, "_NoiseMaskUVModeFoldOut", W9ParticleShaderFlags.FLAG_BIT_UVMODE_POS_0_NOISE_MASK_MAP, 0, () => Content("扭曲遮罩贴图UV来源"), "_NoiseMaskMap");

            PropertyToggleBlockItem chromaticBlock = ToggleBlock(
                rootItem,
                "_ChromaticAberrationFoldOut",
                "_Distortion_Choraticaberrat_Toggle",
                "扭曲色散",
                W9ParticleShaderFlags.FLAG_BIT_PARTICLE_CHORATICABERRAT,
                parent: noiseBlock,
                bold: true);
            ShaderGUIItem chromaticNoiseAffect = new NoiseAffectItem(rootItem, chromaticBlock);
            AddToggle(rootItem, chromaticNoiseAffect, "_Distortion_Choraticaberrat_WithNoise_Toggle", "色散强度受扭曲强度影响",
                enabled => rootItem.SyncService.ApplyToggleFlag(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_NOISE_CHORATICABERRAT_WITH_NOISE, enabled));
            AddVectorComponent(rootItem, chromaticBlock, "_DistortionDirection", 2, "色散强度", false);
            new CustomDataSelectItem(rootItem, chromaticBlock, W9ParticleShaderFlags.FLAGBIT_POS_0_CUSTOMDATA_CHORATICABERRAT_INTENSITY, 0, () => Content("色散强度自定义曲线"));
        }

        private void AddEmission(NBShaderRootItem rootItem)
        {
            PropertyToggleBlockItem block = ToggleBlock(rootItem, "_EmissionBlockFoldOut", "_EmissionEnabled", "流光(颜色相加)", keyword: "_EMISSION", bold: true);
            AddTextureWithWrap(rootItem, block, "_EmissionMap", "流光贴图", W9ParticleShaderFlags.FLAG_BIT_WRAPMODE_EMISSIONMAP, "_EmissionMapColor");
            new UVModeSelectItem(rootItem, block, "_EmissionUVModeFoldOut", W9ParticleShaderFlags.FLAG_BIT_UVMODE_POS_0_EMISSION_MAP, 0, () => Content("流光贴图UV来源"), "_EmissionMap");
            new CustomDataSelectItem(rootItem, block, W9ParticleShaderFlags.FLAGBIT_POS_3_CUSTOMDATA_EMISSION_OFFSET_X, 3, () => Content("流光贴图X轴偏移自定义曲线"));
            new CustomDataSelectItem(rootItem, block, W9ParticleShaderFlags.FLAGBIT_POS_3_CUSTOMDATA_EMISSION_OFFSET_Y, 3, () => Content("流光贴图Y轴偏移自定义曲线"));
            AddSlider(rootItem, block, "_EmissionMapUVRotation", "流光贴图旋转", 0f, 360f);
            new Vector2LineItem(rootItem, block, "_EmissionMapUVOffset", true, () => Content("流光贴图偏移速度"));
            ShaderGUIItem emissionNoiseAffect = new NoiseAffectItem(rootItem, block);
            AddFloat(rootItem, emissionNoiseAffect, "_Emi_Distortion_intensity", "流光贴图扭曲强度");
            AddFloat(rootItem, block, "_EmissionMapColorIntensity", "流光颜色强度");
        }

        private void AddColorBlend(NBShaderRootItem rootItem)
        {
            PropertyToggleBlockItem block = ToggleBlock(rootItem, "_ColorBlendBlockFoldOut", "_ColorBlendMap_Toggle", "渐变(颜色相乘)", keyword: "_COLORMAPBLEND", bold: true);
            AddTextureWithWrap(rootItem, block, "_ColorBlendMap", "颜色渐变贴图", W9ParticleShaderFlags.FLAG_BIT_WRAPMODE_COLORBLENDMAP, "_ColorBlendColor");
            new UVModeSelectItem(rootItem, block, "_ColorBlendUVModeFoldOut", W9ParticleShaderFlags.FLAG_BIT_UVMODE_POS_0_COLOR_BLEND_MAP, 0, () => Content("颜色渐变贴图UV来源"), "_ColorBlendMap");
            new CustomDataSelectItem(rootItem, block, W9ParticleShaderFlags.FLAGBIT_POS_3_CUSTOMDATA_COLOR_BLEND_OFFSET_X, 3, () => Content("颜色渐变贴图X轴偏移自定义曲线"));
            new CustomDataSelectItem(rootItem, block, W9ParticleShaderFlags.FLAGBIT_POS_3_CUSTOMDATA_COLOR_BLEND_OFFSET_Y, 3, () => Content("颜色渐变贴图Y轴偏移自定义曲线"));
            AddVectorComponent(rootItem, block, "_ColorBlendVec", 3, "颜色渐变贴图旋转", true, 0f, 360f);
            new Vector2LineItem(rootItem, block, "_ColorBlendMapOffset", true, () => Content("颜色渐变贴图偏移速度"));
            ShaderGUIItem colorBlendNoiseAffect = new NoiseAffectItem(rootItem, block);
            AddVectorComponent(rootItem, colorBlendNoiseAffect, "_ColorBlendVec", 0, "颜色渐变扭曲强度", true);
            new PopupItem(rootItem, block, "_ColorBlendAlphaMultiplyMode", () => Content("颜色渐变图Alpha作用"), OnOffNames,
                property => rootItem.SyncService.ApplyToggleFlag(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_COLOR_BLEND_ALPHA_MULTIPLY_MODE, property.floatValue > 0.5f));
            AddVectorComponent(rootItem, block, "_ColorBlendVec", 2, "颜色渐变图Alpha强度", true);
        }

        private void AddRampColor(NBShaderRootItem rootItem)
        {
            PropertyToggleBlockItem block = ToggleBlock(rootItem, "_RampColorBlockFoldOut", "_RampColorToggle", "颜色映射(Ramp)", keyword: "_COLOR_RAMP", bold: true);
            new PopupItem(rootItem, block, "_RampColorSourceMode", () => Content("Ramp来源模式"), RampSourceNames,
                property => rootItem.SyncService.ApplyToggleFlag(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_RAMP_COLOR_MAP_MODE_ON, property.floatValue > 0.5f));
            AddTextureWithWrap(rootItem, block, "_RampColorMap", "颜色映射黑白图", W9ParticleShaderFlags.FLAG_BIT_WRAPMODE_RAMP_COLOR_MAP,
                isVisible: () => IsPropertyMode(rootItem, "_RampColorSourceMode", 1));
            new TextureScaleOffsetItem(rootItem, block, "_RampColorMap", false, () => IsPropertyMode(rootItem, "_RampColorSourceMode", 0), TillingContent, OffsetContent);
            new WrapModeItem(rootItem, block, W9ParticleShaderFlags.FLAG_BIT_WRAPMODE_RAMP_COLOR_MAP, () => Content("颜色映射UV Wrap"), 2,
                () => IsPropertyMode(rootItem, "_RampColorSourceMode", 0));
            new ColorChannelSelectItem(rootItem, block, W9ParticleShaderFlags.FLAG_BIT_COLOR_CHANNEL_POS_0_RAMP_COLOR_MAP, 0, () => Content("颜色映射黑白图通道选择"),
                () => IsPropertyMode(rootItem, "_RampColorSourceMode", 1));
            new UVModeSelectItem(rootItem, block, "_RampColorUVModeFoldOut", W9ParticleShaderFlags.FLAG_BIT_UVMODE_POS_0_RAMP_COLOR_MAP, 0, () => Content("颜色映射黑白图UV来源"), "_RampColorMap", true);
            new Vector2LineItem(rootItem, block, "_RampColorMapOffset", true, () => Content("颜色映射贴图偏移速度"));
            AddVectorComponent(rootItem, block, "_RampColorMapOffset", 3, "颜色映射贴图旋转", true, 0f, 360f);
            AddGradient(rootItem, block, "映射颜色", "_RampColorCount", "_RampColor", "_RampColorAlpha", hdr: true);
            new PopupItem(rootItem, block, "_RampColorBlendMode", () => Content("Ramp颜色混合模式"), BlendModeNames,
                property => rootItem.SyncService.ApplyToggleFlag(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_RAMP_COLOR_BLEND_ADD, property.floatValue > 0.5f));
            new ColorItem(rootItem, block, "_RampColorBlendColor", () => Content("颜色映射叠加颜色"));
        }

        private void AddDissolve(NBShaderRootItem rootItem)
        {
            PropertyToggleBlockItem block = ToggleBlock(rootItem, "_DissolveBlockFoldOut", "_Dissolve_Toggle", "溶解", keyword: "_DISSOLVE", bold: true);
            AddToggle(rootItem, block, "_NB_Debug_Dissolve", "溶解度黑白值测试", enabled => rootItem.SyncService.ApplyToggleKeyword("NB_DEBUG_DISSOLVE", enabled));
            TextureRelatedFoldOutItem dissolveMapRelatedFoldOut = AddTextureWithRelatedFoldOut(rootItem, block, "_DissolveMap", "溶解贴图", "_DissolveMapFoldOut", W9ParticleShaderFlags.FLAG_BIT_WRAPMODE_DISSOLVE_MAP);
            new ColorChannelSelectItem(rootItem, dissolveMapRelatedFoldOut, W9ParticleShaderFlags.FLAG_BIT_COLOR_CHANNEL_POS_0_DISSOLVE_MAP, 0, () => Content("溶解贴图通道选择"));
            new CustomDataSelectItem(rootItem, dissolveMapRelatedFoldOut, W9ParticleShaderFlags.FLAGBIT_POS_1_CUSTOMDATA_DISSOLVE_OFFSET_X, 1, () => Content("溶解贴图X轴偏移自定义曲线"));
            new CustomDataSelectItem(rootItem, dissolveMapRelatedFoldOut, W9ParticleShaderFlags.FLAGBIT_POS_1_CUSTOMDATA_DISSOLVE_OFFSET_Y, 1, () => Content("溶解贴图Y轴偏移自定义曲线"));
            new UVModeSelectItem(rootItem, dissolveMapRelatedFoldOut, "_DissolveUVModeFoldOut", W9ParticleShaderFlags.FLAG_BIT_UVMODE_POS_0_DISSOLVE_MAP, 0, () => Content("溶解贴图UV来源"), "_DissolveMap");
            new Vector2LineItem(rootItem, dissolveMapRelatedFoldOut, "_DissolveOffsetRotateDistort", true, () => Content("溶解贴图偏移速度"));
            AddVectorComponent(rootItem, dissolveMapRelatedFoldOut, "_DissolveOffsetRotateDistort", 2, "溶解贴图旋转", true, 0f, 360f);
            new PNoiseBlendModeItem(rootItem, block, W9ParticleShaderFlags.FLAG_BIT_PNOISE_BLEND_POS_0_DISSOLVE, "_DissolvePNoiseBlendOpacity", () => Content("溶解程序噪波混合"),
                () => rootItem.Context.ProgramNoiseEnabled == MixedBool.True);
            AddVectorComponent(rootItem, block, "_Dissolve", 1, "溶解值Pow", true, 0f, 10f);
            AddSlider(rootItem, block, "_Dissolve", "溶解强度", rangePropertyName: "DissolveXRangeVec", vectorComponentIndex: 0);
            new CustomDataSelectItem(rootItem, block, W9ParticleShaderFlags.FLAGBIT_POS_0_CUSTOMDATA_DISSOLVE_INTENSITY, 0, () => Content("溶解强度自定义曲线"));
            AddVectorComponent(rootItem, block, "_Dissolve", 3, "溶解硬软度", true, 0.001f, 1f);
            ShaderGUIItem dissolveNoiseAffect = new NoiseAffectItem(rootItem, block);
            AddVectorComponent(rootItem, dissolveNoiseAffect, "_DissolveOffsetRotateDistort", 3, "溶解贴图扭曲强度", false);

            PropertyToggleBlockItem lineBlock = ToggleBlock(rootItem, "_DissolveLineFoldOut", "_DissolveLineMaskToggle", "溶解描边",
                W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_DISSOLVE_LINE_MASK, 1, parent: block);
            new ColorItem(rootItem, lineBlock, "_DissolveLineColor", () => Content("溶解描边颜色"));
            AddSlider(rootItem, lineBlock, "_Dissolve_Vec2", "描边位置", rangePropertyName: "Dissolve2XRangeVec", vectorComponentIndex: 0);
            AddSlider(rootItem, lineBlock, "_Dissolve_Vec2", "描边软硬", rangePropertyName: "Dissolve2YRangeVec", vectorComponentIndex: 1);

            PropertyToggleBlockItem rampBlock = ToggleBlock(rootItem, "_DissolveRampFoldOut", "_Dissolve_useRampMap_Toggle", "溶解Ramp图功能",
                W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_DISSOVLE_USE_RAMP, 1, parent: block);
            new PopupItem(rootItem, rampBlock, "_DissolveRampSourceMode", () => Content("溶解Ramp模式"), RampSourceNames,
                property => rootItem.SyncService.ApplyToggleFlag(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_DISSOLVE_RAMP_MAP, property.floatValue > 0.5f));
            AddTextureWithWrap(rootItem, rampBlock, "_DissolveRampMap", "溶解Ramp图", W9ParticleShaderFlags.FLAG_BIT_WRAPMODE_DISSOLVE_RAMPMAP, "_DissolveRampColor",
                () => IsPropertyMode(rootItem, "_DissolveRampSourceMode", 1));
            AddGradient(rootItem, rampBlock, "Ramp颜色", "_DissolveRampCount", "_DissolveRampColor", "_DissolveRampAlpha", hdr: true,
                isVisible: () => IsPropertyMode(rootItem, "_DissolveRampSourceMode", 0));
            new TextureScaleOffsetItem(rootItem, rampBlock, "_DissolveRampMap", false, () => IsPropertyMode(rootItem, "_DissolveRampSourceMode", 0), TillingContent, OffsetContent);
            new ColorItem(rootItem, rampBlock, "_DissolveRampColor", () => Content("Ramp颜色叠加"), () => IsPropertyMode(rootItem, "_DissolveRampSourceMode", 0));
            new WrapModeItem(rootItem, rampBlock, W9ParticleShaderFlags.FLAG_BIT_WRAPMODE_DISSOLVE_RAMPMAP, () => Content("溶解RampUV Wrap"), 2,
                () => IsPropertyMode(rootItem, "_DissolveRampSourceMode", 0));
            new PopupItem(rootItem, rampBlock, "_DissolveRampColorBlendMode", () => Content("溶解Ramp混合模式"), BlendModeNames,
                property => rootItem.SyncService.ApplyToggleFlag(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_DISSOLVE_RAMP_MULITPLY, property.floatValue > 0.5f, 1));

            PropertyToggleBlockItem maskBlock = ToggleBlock(rootItem, "_DissolveMaskFoldOut", "_DissolveMask_Toggle", "溶解遮罩图(过程溶解)",
                W9ParticleShaderFlags.FLAG_BIT_PARTICLE_DISSOLVE_MASK, parent: block);
            new PopupItem(rootItem, maskBlock, "_DissolveMaskMode", () => Content("溶解遮罩模式"), DissolveMaskModeNames);
            AddTextureWithWrap(rootItem, maskBlock, "_DissolveMaskMap", "溶解遮罩图", W9ParticleShaderFlags.FLAG_BIT_WRAPMODE_DISSOLVE_MASKMAP);
            new UVModeSelectItem(rootItem, maskBlock, "_DissolveMaskUVModeFoldOut", W9ParticleShaderFlags.FLAG_BIT_UVMODE_POS_0_DISSOLVE_MASK_MAP, 0, () => Content("溶解遮罩图UV来源"), "_DissolveMaskMap");
            new ColorChannelSelectItem(rootItem, maskBlock, W9ParticleShaderFlags.FLAG_BIT_COLOR_CHANNEL_POS_0_DISSOLVE_MASK_MAP, 0, () => Content("溶解遮罩图通道选择"));
            new VectorComponentItem(rootItem, maskBlock, "_Dissolve", 2, () => Content("溶解遮罩强度"), false, isVisible: () => !IsDissolveMaskStrengthSlider(rootItem));
            new VectorComponentItem(rootItem, maskBlock, "_Dissolve", 2, () => Content("溶解遮罩强度"), true, 0f, 2f, () => IsDissolveMaskStrengthSlider(rootItem));
            new CustomDataSelectItem(rootItem, maskBlock, W9ParticleShaderFlags.FLAGBIT_POS_1_CUSTOMDATA_DISSOLVE_MASK_INTENSITY, 1, () => Content("溶解遮罩图强度自定义曲线"));
        }

        private void AddProgramNoise(NBShaderRootItem rootItem)
        {
            PropertyToggleBlockItem block = ToggleBlock(rootItem, "_ProgramNoiseBlockFoldOut", "_ProgramNoise_Toggle", "程序化噪波", keyword: "_PROGRAM_NOISE", bold: true);
            AddToggle(rootItem, block, "_NB_Debug_PNoise", "程序化噪波测试颜色", enabled => rootItem.SyncService.ApplyToggleKeyword("NB_DEBUG_PNOISE", enabled));
            new UVModeSelectItem(rootItem, block, "_ProgramNoiseUVModeFoldOut", W9ParticleShaderFlags.FLAG_BIT_UVMODE_POS_0_PROGRAM_NOISE, 0, () => Content("程序噪波UV来源"), forceEnable: true);
            AddFloat(rootItem, block, "_ProgramNoise_Rotate", "程序化噪波旋转");

            PropertyToggleBlockItem simpleBlock = ToggleBlock(rootItem, "_ProgramNoiseSimpleFoldOut", "_ProgramNoise_Simple_Toggle", "Perlin噪波",
                W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_PROGRAM_NOISE_SIMPLE, 1, parent: block);
            new Vector2LineItem(rootItem, simpleBlock, "_DissolveVoronoi_Vec", true, () => Content("噪波1缩放"));
            AddVectorComponent(rootItem, simpleBlock, "_DissolveVoronoi_Vec2", 2, "噪波1速度", false);
            new Vector2LineItem(rootItem, simpleBlock, "_DissolveVoronoi_Vec4", true, () => Content("噪波1偏移"));
            new Vector2LineItem(rootItem, simpleBlock, "_DissolveVoronoi_Vec3", true, () => Content("噪波1偏移速度"));
            new CustomDataSelectItem(rootItem, simpleBlock, W9ParticleShaderFlags.FLAGBIT_POS_2_CUSTOMDATA_DISSOLVE_NOISE1_OFFSET_X, 2, () => Content("噪波1偏移速度X自定义曲线"));
            new CustomDataSelectItem(rootItem, simpleBlock, W9ParticleShaderFlags.FLAGBIT_POS_2_CUSTOMDATA_DISSOLVE_NOISE1_OFFSET_Y, 2, () => Content("噪波1偏移速度Y自定义曲线"));

            PropertyToggleBlockItem voronoiBlock = ToggleBlock(rootItem, "_ProgramNoiseVoronoiFoldOut", "_ProgramNoise_Voronoi_Toggle", "Voronoi噪波",
                W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_PROGRAM_NOISE_VORONOI, 1, parent: block);
            new Vector2LineItem(rootItem, voronoiBlock, "_DissolveVoronoi_Vec", false, () => Content("噪波2缩放"));
            AddVectorComponent(rootItem, voronoiBlock, "_DissolveVoronoi_Vec2", 3, "噪波2速度", false);
            new Vector2LineItem(rootItem, voronoiBlock, "_DissolveVoronoi_Vec4", false, () => Content("噪波2偏移"));
            new Vector2LineItem(rootItem, voronoiBlock, "_DissolveVoronoi_Vec3", false, () => Content("噪波2偏移速度"));
            new CustomDataSelectItem(rootItem, voronoiBlock, W9ParticleShaderFlags.FLAGBIT_POS_2_CUSTOMDATA_DISSOLVE_NOISE2_OFFSET_X, 2, () => Content("噪波2偏移速度X自定义曲线"));
            new CustomDataSelectItem(rootItem, voronoiBlock, W9ParticleShaderFlags.FLAGBIT_POS_2_CUSTOMDATA_DISSOLVE_NOISE2_OFFSET_Y, 2, () => Content("噪波2偏移速度Y自定义曲线"));
            AddVectorComponent(rootItem, block, "_DissolveVoronoi_Vec2", 0, "噪波1和噪波2混合系数", true);
            new PNoiseBlendModeItem(
                rootItem,
                block,
                W9ParticleShaderFlags.FLAG_BIT_PNOISE_BLEND_POS_0_BASE_BLEND,
                "_ProgramNoiseBaseBlendOpacity",
                () => Content("两种程序噪波混合"),
                () => rootItem.Context.ProgramNoiseEnabled == MixedBool.True &&
                      rootItem.Context.IsToggleOn("_ProgramNoise_Simple_Toggle") &&
                      rootItem.Context.IsToggleOn("_ProgramNoise_Voronoi_Toggle"));
        }

        private void AddSharedUV(NBShaderRootItem rootItem)
        {
            PropertyToggleBlockItem block = ToggleBlock(rootItem, "_SharedUVBlockFoldOut", "_SharedUVToggle", "公共UV", keyword: "_SHARED_UV", bold: true);
            new UVModeSelectItem(rootItem, block, "_SharedUVModeFoldOut", W9ParticleShaderFlags.FLAG_BIT_UVMODE_POS_0_SHAREDUV, 0, () => Content("公共UV来源"), forceEnable: true);
            new Vector2LineItem(rootItem, block, "_SharedUV_ST", true, () => Content("公共UV Tiling"));
            new Vector2LineItem(rootItem, block, "_SharedUV_ST", false, () => Content("公共UV Offset"));
            new Vector2LineItem(rootItem, block, "_SharedUV_Vec", true, () => Content("公共UV偏移速度"));
            AddVectorComponent(rootItem, block, "_SharedUV_Vec", 2, "旋转", false);
            AddVectorComponent(rootItem, block, "_SharedUV_Vec", 3, "旋转速度", false);
            new CustomDataSelectItem(rootItem, block, W9ParticleShaderFlags.FLAGBIT_POS_3_CUSTOMDATA_SHARED_UV_OFFSET_X, 3, () => Content("公共UV X轴偏移自定义曲线"));
            new CustomDataSelectItem(rootItem, block, W9ParticleShaderFlags.FLAGBIT_POS_3_CUSTOMDATA_SHARED_UV_OFFSET_Y, 3, () => Content("公共UV Y轴偏移自定义曲线"));
        }

        private void AddFresnel(NBShaderRootItem rootItem)
        {
            PropertyToggleBlockItem block = ToggleBlock(rootItem, "_FresnelBlockFoldOut", "_fresnelEnabled", "菲涅尔",
                W9ParticleShaderFlags.FLAG_BIT_PARTICLE_FRESNEL_ON, parent: this, bold: true);
            AddToggle(rootItem, block, "_NB_Debug_Fresnel", "菲涅尔测试颜色", enabled => rootItem.SyncService.ApplyToggleKeyword("NB_DEBUG_FRESNEL", enabled));
            new PopupItem(rootItem, block, "_FresnelMode", () => Content("菲涅尔模式"), FresnelModeNames,
                property => rootItem.SyncService.ApplyToggleFlag(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_FRESNEL_FADE_ON, property.floatValue > 0.5f));
            Func<bool> isFresnelColorMode = () => IsPropertyMode(rootItem, "_FresnelMode", 0);
            new ColorItem(rootItem, block, "_FresnelColor", () => Content("菲涅尔颜色"), isFresnelColorMode);
            AddVectorComponent(rootItem, block, "_FresnelUnit", 2, "菲涅尔强度", true);
            AddVectorComponent(rootItem, block, "_FresnelUnit", 0, "菲涅尔位置", true, -1f, 1f);
            new CustomDataSelectItem(rootItem, block, W9ParticleShaderFlags.FLAGBIT_POS_0_CUSTOMDATA_FRESNEL_OFFSET, 0, () => Content("菲涅尔位置自定义曲线"));
            AddVectorComponent(rootItem, block, "_FresnelUnit", 1, "菲涅尔范围Pow", true, 0f, 10f);
            AddVectorComponent(rootItem, block, "_FresnelUnit", 3, "菲涅尔硬度", true);
            AddToggle(rootItem, block, "_InvertFresnel_Toggle", "翻转菲涅尔",
                enabled => rootItem.SyncService.ApplyToggleFlag(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_FRESNEL_INVERT_ON, enabled));
            AddToggle(rootItem, block, "_FresnelColorAffectByAlpha", "菲涅尔颜色受Alpha影响",
                enabled => rootItem.SyncService.ApplyToggleFlag(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_FRESNEL_COLOR_AFFETCT_BY_ALPHA, enabled),
                isFresnelColorMode);
            AddVectorComponent(rootItem, block, "_FresnelRotation", 0, "菲涅尔方向偏移X", false);
            AddVectorComponent(rootItem, block, "_FresnelRotation", 1, "菲涅尔方向偏移Y", false);
            AddVectorComponent(rootItem, block, "_FresnelRotation", 2, "菲涅尔方向偏移Z", false);
        }

        private void AddVertexOffset(NBShaderRootItem rootItem)
        {
            PropertyToggleBlockItem block = ToggleBlock(rootItem, "_VertexOffsetBlockFoldOut", "_VertexOffset_Toggle", "顶点偏移",
                W9ParticleShaderFlags.FLAG_BIT_PARTICLE_VERTEX_OFFSET_ON, parent: this, bold: true);
            AddToggle(rootItem, block, "_NB_Debug_VertexOffset", "顶点偏移方向测试", enabled => rootItem.SyncService.ApplyToggleKeyword("NB_DEBUG_VERTEX_OFFSET", enabled));
            AddTextureWithWrap(rootItem, block, "_VertexOffset_Map", "顶点偏移贴图", W9ParticleShaderFlags.FLAG_BIT_WRAPMODE_VERTEXOFFSETMAP);
            new UVModeSelectItem(rootItem, block, "_VertexOffsetUVModeFoldOut", W9ParticleShaderFlags.FLAG_BIT_UVMODE_POS_0_VERTEX_OFFSET_MAP, 0, () => Content("顶点偏移贴图UV来源"), "_VertexOffset_Map");
            new CustomDataSelectItem(rootItem, block, W9ParticleShaderFlags.FLAGBIT_POS_1_CUSTOMDATA_VERTEX_OFFSET_X, 1, () => Content("顶点扰动X轴偏移自定义曲线"));
            new CustomDataSelectItem(rootItem, block, W9ParticleShaderFlags.FLAGBIT_POS_1_CUSTOMDATA_VERTEX_OFFSET_Y, 1, () => Content("顶点扰动Y轴偏移自定义曲线"));
            new Vector2LineItem(rootItem, block, "_VertexOffset_Vec", true, () => Content("顶点偏移动画"));
            AddVectorComponent(rootItem, block, "_VertexOffset_Vec", 2, "顶点偏移强度", false);
            new CustomDataSelectItem(rootItem, block, W9ParticleShaderFlags.FLAGBIT_POS_1_CUSTOMDATA_VERTEXOFFSET_INTENSITY, 1, () => Content("顶点扰动强度自定义曲线"));
            AddToggle(rootItem, block, "_VertexOffset_StartFromZero", "顶点偏移从零开始",
                enabled => rootItem.SyncService.ApplyToggleFlag(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_VERTEXOFFSET_START_FROM_ZERO, enabled, 1));
            AddToggle(rootItem, block, "_VertexOffset_NormalDir_Toggle", "顶点偏移使用法线方向",
                enabled => rootItem.SyncService.ApplyToggleFlag(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_VERTEX_OFFSET_NORMAL_DIR, enabled));
            Func<bool> showCustomDirection = () => IsPropertyOff(rootItem, "_VertexOffset_NormalDir_Toggle");
            AddVectorComponent(rootItem, block, "_VertexOffset_CustomDir", 0, "顶点偏移本地方向X", false, isVisible: showCustomDirection);
            AddVectorComponent(rootItem, block, "_VertexOffset_CustomDir", 1, "顶点偏移本地方向Y", false, isVisible: showCustomDirection);
            AddVectorComponent(rootItem, block, "_VertexOffset_CustomDir", 2, "顶点偏移本地方向Z", false, isVisible: showCustomDirection);

            PropertyToggleBlockItem maskBlock = ToggleBlock(rootItem, "_VertexOffsetMaskBlockFoldOut", "_VertexOffset_Mask_Toggle", "顶点偏移遮罩",
                W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_VERTEXOFFSET_MASKMAP, 1, parent: block);
            AddTextureWithWrap(rootItem, maskBlock, "_VertexOffset_MaskMap", "顶点偏移遮罩图", W9ParticleShaderFlags.FLAG_BIT_WRAPMODE_VERTEXOFFSET_MASKMAP);
            new UVModeSelectItem(rootItem, maskBlock, "_VertexOffsetMaskUVModeFoldOut", W9ParticleShaderFlags.FLAG_BIT_UVMODE_POS_0_VERTEX_OFFSET_MASKMAP, 0, () => Content("顶点偏移遮罩图UV来源"), "_VertexOffset_MaskMap");
            new CustomDataSelectItem(rootItem, maskBlock, W9ParticleShaderFlags.FLAGBIT_POS_3_CUSTOMDATA_VERTEX_OFFSET_MASK_X, 3, () => Content("顶点扰动遮罩X轴偏移自定义曲线"));
            new CustomDataSelectItem(rootItem, maskBlock, W9ParticleShaderFlags.FLAGBIT_POS_3_CUSTOMDATA_VERTEX_OFFSET_MASK_Y, 3, () => Content("顶点扰动遮罩Y轴偏移自定义曲线"));
            new Vector2LineItem(rootItem, maskBlock, "_VertexOffset_MaskMap_Vec", true, () => Content("顶点偏移遮罩动画"));
            AddVectorComponent(rootItem, maskBlock, "_VertexOffset_MaskMap_Vec", 2, "顶点偏移遮罩强度", true);
        }

        private void AddDepthOutlineAndDecal(NBShaderRootItem rootItem)
        {
            PropertyToggleBlockItem outlineBlock = ToggleBlock(rootItem, "_DepthOutlineBlockFoldOut", "_DepthOutline_Toggle", "深度描边",
                W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_DEPTH_OUTLINE, 1, parent: this, bold: true,
                isVisible: () => rootItem.Context.UIEffectEnabled != MixedBool.True);
            new ColorItem(rootItem, outlineBlock, "_DepthOutline_Color", () => Content("深度描边颜色"));
            new Vector2LineItem(rootItem, outlineBlock, "_DepthOutline_Vec", true, () => Content("深度描边距离"));

            AddToggle(rootItem, this, "_DepthDecal_Toggle", "深度贴花",
                rootItem.SyncService.ApplyDepthDecalEnabled,
                () => rootItem.Context.UIEffectEnabled != MixedBool.True);
        }

        private void AddParallax(NBShaderRootItem rootItem)
        {
            PropertyToggleBlockItem block = ToggleBlock(rootItem, "_ParallaxBlockFoldOut", "_ParallaxMapping_Toggle", "遮蔽视差",
                W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_PARALLAX_MAPPING, 1, parent: this, keyword: "_PARALLAX_MAPPING", bold: true,
                isVisible: () => rootItem.Context.UIEffectEnabled != MixedBool.True);
            AddTextureWithWrap(rootItem, block, "_ParallaxMapping_Map", "视差贴图", W9ParticleShaderFlags.FLAG_BIT_WRAPMODE_PARALLAXMAPPINGMAP);
            AddSlider(rootItem, block, "_ParallaxMapping_Intensity", "视差", rangePropertyName: "_ParallaxMapping_IntensityRangeVec");
            AddVectorComponent(rootItem, block, "_ParallaxMapping_Vec", 0, "遮蔽视差最小层数", true, 0f, 100f);
            AddVectorComponent(rootItem, block, "_ParallaxMapping_Vec", 1, "遮蔽视差最大层数", true, 0f, 100f);
            new HelpBoxItem(rootItem, block, () => Text("feature.parallax.layerWarning.message", "遮蔽视差层数过高将影响性能"), MessageType.Warning,
                () => IsParallaxMaxLayerHigh(rootItem));
        }

        private void AddPortal(NBShaderRootItem rootItem)
        {
            PropertyToggleBlockItem block = ToggleBlock(rootItem, "_PortalBlockFoldOut", "_Portal_Toggle", "模板视差",
                parent: this, bold: true, onValueChanged: _ => rootItem.SyncService.ApplyPortalState(), isVisible: () => rootItem.Context.UIEffectEnabled != MixedBool.True);
            AddToggle(rootItem, block, "_Portal_MaskToggle", "模板视差蒙版", _ => rootItem.SyncService.ApplyPortalState());
        }

        private void AddFlipbook(NBShaderRootItem rootItem)
        {
            new FlipbookToggleItem(rootItem, this);
        }

        private void AddVat(NBShaderRootItem rootItem)
        {
            PropertyToggleBlockItem block = ToggleBlock(rootItem, "_VATBlockFoldOut", "_VAT_Toggle", "VAT顶点动画图",
                parent: this, bold: true, onValueChanged: rootItem.SyncService.ApplyVatEnabled);
            new PopupItem(rootItem, block, "_VATMode", () => Content("VAT模式"), VatModeNames, _ => rootItem.SyncService.SyncMaterialState());
            Func<bool> isHoudini = () => IsPropertyMode(rootItem, "_VATMode", (int)VATMode.Houdini);
            Func<bool> isTyflow = () => IsPropertyMode(rootItem, "_VATMode", (int)VATMode.Tyflow);
            Func<bool> hasVatFrameCustomData = () => IsVatFrameCustomDataVisible(rootItem);

            new PopupItem(rootItem, block, "_HoudiniVATSubMode", () => Content("Houdini VAT Sub Mode"), HoudiniVatSubModeNames,
                _ => rootItem.SyncService.SyncMaterialState(), isHoudini);
            new HelpBoxItem(rootItem, block, () => Text("feature.vat.houdiniUnsupportedParticle.message", "该 Houdini VAT 类型需要 Mesh 多 UV 数据，不支持 ParticleSystem VertexStream 模式。"), MessageType.Warning,
                () => isHoudini() && HasUnsupportedHoudiniParticleMode(rootItem));
            AddSectionLabel(rootItem, block, "Playback", isHoudini);
            AddToggle(rootItem, block, "_B_autoPlayback", "Auto Playback", isVisible: isHoudini);
            AddFloat(rootItem, block, "_displayFrame", "Display Frame", () => isHoudini() && ShouldDrawWhenFloatOff(rootItem, "_B_autoPlayback"));
            new VatFrameCustomDataItem(rootItem, block, () => Content("VAT Frame CustomData"), () => isHoudini() && hasVatFrameCustomData(), hasVatFrameCustomData);
            AddFloat(rootItem, block, "_gameTimeAtFirstFrame", "Game Time at First Frame", isHoudini);
            AddFloat(rootItem, block, "_playbackSpeed", "Playback Speed", isHoudini);
            AddFloat(rootItem, block, "_houdiniFPS", "Houdini FPS", isHoudini);
            AddToggle(rootItem, block, "_B_interpolate", "Interframe Interpolation", isVisible: isHoudini);
            AddToggle(rootItem, block, "_animateFirstFrame", "Animate First Frame", isVisible: () => isHoudini() && IsPropertyMode(rootItem, "_HoudiniVATSubMode", 1));
            AddFloat(rootItem, block, "_frameCount", "Frame Count", isHoudini);

            AddSectionLabel(rootItem, block, "Bounds Metadata", isHoudini);
            AddFloat(rootItem, block, "_boundMinX", "Bound Min X", isHoudini);
            AddFloat(rootItem, block, "_boundMinY", "Bound Min Y", isHoudini);
            AddFloat(rootItem, block, "_boundMinZ", "Bound Min Z", isHoudini);
            AddFloat(rootItem, block, "_boundMaxX", "Bound Max X", isHoudini);
            AddFloat(rootItem, block, "_boundMaxY", "Bound Max Y", isHoudini);
            AddFloat(rootItem, block, "_boundMaxZ", "Bound Max Z", isHoudini);

            AddSectionLabel(rootItem, block, "Textures", isHoudini);
            new TextureItem(rootItem, block, "_posTexture", () => Content("Position Texture"), drawScaleOffset: false, isVisible: isHoudini);
            new TextureItem(rootItem, block, "_posTexture2", () => Content("Position Texture 2"), drawScaleOffset: false,
                isVisible: () => isHoudini() && ShouldDrawWhenFloatOn(rootItem, "_B_LOAD_POS_TWO_TEX"));
            new TextureItem(rootItem, block, "_rotTexture", () => Content("Rotation Texture"), drawScaleOffset: false,
                isVisible: () => isHoudini() && ShouldDrawHoudiniRotationTexture(rootItem));
            new TextureItem(rootItem, block, "_colTexture", () => Content("Color Texture"), drawScaleOffset: false,
                isVisible: () => isHoudini() && ShouldDrawWhenFloatOn(rootItem, "_B_LOAD_COL_TEX"));
            new TextureItem(rootItem, block, "_lookupTable", () => Content("Lookup Table"), drawScaleOffset: false,
                isVisible: () => isHoudini() && (IsPropertyMode(rootItem, "_HoudiniVATSubMode", 2) || ShouldDrawWhenFloatOn(rootItem, "_B_LOAD_LOOKUP_TABLE")));

            AddSectionLabel(rootItem, block, "Scale", () => isHoudini() && IsPropertyMode(rootItem, "_HoudiniVATSubMode", 1, 3));
            AddFloat(rootItem, block, "_globalPscaleMul", "Global Piece Scale Multiplier", () => isHoudini() && IsPropertyMode(rootItem, "_HoudiniVATSubMode", 1, 3));
            AddToggle(rootItem, block, "_B_pscaleAreInPosA", "Piece Scales in Position Alpha", isVisible: () => isHoudini() && IsPropertyMode(rootItem, "_HoudiniVATSubMode", 1, 3));

            AddSectionLabel(rootItem, block, "Particle Sprite", () => isHoudini() && IsPropertyMode(rootItem, "_HoudiniVATSubMode", 3));
            AddFloat(rootItem, block, "_widthBaseScale", "Width Base Scale", () => isHoudini() && IsPropertyMode(rootItem, "_HoudiniVATSubMode", 3));
            AddFloat(rootItem, block, "_heightBaseScale", "Height Base Scale", () => isHoudini() && IsPropertyMode(rootItem, "_HoudiniVATSubMode", 3));
            AddToggle(rootItem, block, "_B_hideOverlappingOrigin", "Hide Overlapping Origin", isVisible: () => isHoudini() && IsPropertyMode(rootItem, "_HoudiniVATSubMode", 3));
            AddFloat(rootItem, block, "_originRadius", "Origin Effective Radius", () => isHoudini() && IsPropertyMode(rootItem, "_HoudiniVATSubMode", 3) && ShouldDrawWhenFloatOn(rootItem, "_B_hideOverlappingOrigin"));
            AddToggle(rootItem, block, "_B_CAN_SPIN", "Particles Can Spin", isVisible: () => isHoudini() && IsPropertyMode(rootItem, "_HoudiniVATSubMode", 3));
            AddToggle(rootItem, block, "_B_spinFromHeading", "Compute Spin from Heading", isVisible: () => isHoudini() && IsPropertyMode(rootItem, "_HoudiniVATSubMode", 3) && ShouldDrawWhenFloatOn(rootItem, "_B_CAN_SPIN"));
            AddFloat(rootItem, block, "_spinPhase", "Particle Spin Phase", () => isHoudini() && IsPropertyMode(rootItem, "_HoudiniVATSubMode", 3) && ShouldDrawWhenFloatOn(rootItem, "_B_CAN_SPIN") && ShouldDrawWhenFloatOff(rootItem, "_B_spinFromHeading"));
            AddFloat(rootItem, block, "_scaleByVelAmount", "Scale by Velocity Amount", () => isHoudini() && IsPropertyMode(rootItem, "_HoudiniVATSubMode", 3) && ShouldDrawWhenFloatOn(rootItem, "_B_CAN_SPIN"));
            AddFloat(rootItem, block, "_particleTexUScale", "Particle Texture U Scale", () => isHoudini() && IsPropertyMode(rootItem, "_HoudiniVATSubMode", 3));
            AddFloat(rootItem, block, "_particleTexVScale", "Particle Texture V Scale", () => isHoudini() && IsPropertyMode(rootItem, "_HoudiniVATSubMode", 3));

            AddSectionLabel(rootItem, block, "Flags", isHoudini);
            AddToggle(rootItem, block, "_B_LOAD_POS_TWO_TEX", "Positions Require Two Textures", isVisible: isHoudini);
            AddToggle(rootItem, block, "_B_UNLOAD_ROT_TEX", "Use Compressed Normals (no rotTex)", isVisible: () => isHoudini() && ShouldDrawCompressedNormalsToggle(rootItem));
            AddToggle(rootItem, block, "_B_LOAD_COL_TEX", "Load Color Texture", isVisible: isHoudini);
            AddToggle(rootItem, block, "_B_LOAD_LOOKUP_TABLE", "Load Lookup Table", isVisible: () => isHoudini() && IsPropertyMode(rootItem, "_HoudiniVATSubMode", 2));

            new TextureItem(rootItem, block, "_VATTex", () => Content("VAT texture"), drawScaleOffset: false, isVisible: isTyflow);
            AddFloat(rootItem, block, "_ImportScale", "ImportScale", isTyflow);
            new PopupItem(rootItem, block, "_TyFlowVATSubMode", () => Content("TyFlow VAT Sub Mode"), TyFlowVatSubModeNames,
                _ => rootItem.SyncService.SyncMaterialState(), isTyflow);
            new HelpBoxItem(rootItem, block, () => Text("feature.vat.tyflowUnsupportedParticle.message", "该 TyFlow VAT 类型需要 Mesh 多 UV 数据，不支持 ParticleSystem VertexStream 模式。"), MessageType.Warning,
                () => isTyflow() && HasUnsupportedTyflowParticleMode(rootItem));
            new HelpBoxItem(rootItem, block, () => Text("feature.vat.tyflowUv2Conflict.message", "TyFlow VAT uses UV2 (TEXCOORD0.zw) as vertexIndex / vertexCount in ParticleSystem mode. Flipbook blending or Special UV (UV2) conflicts with it; VAT takes priority."), MessageType.Warning,
                () => isTyflow() && !HasUnsupportedTyflowParticleMode(rootItem) && HasTyflowParticleUV2Conflict(rootItem));
            AddToggle(rootItem, block, "_DeformingSkin", "Deforming skin", isVisible: isTyflow);
            AddFloat(rootItem, block, "_SkinBoneCount", "Skin bone count", isTyflow);
            AddToggle(rootItem, block, "_RGBAEncoded", "RGBA encoded", isVisible: isTyflow);
            AddToggle(rootItem, block, "_RGBAHalf", "RGBA half", isVisible: isTyflow);
            AddToggle(rootItem, block, "_LinearToGamma", "Gamma correction", isVisible: isTyflow);
            AddToggle(rootItem, block, "_VATIncludesNormals", "VAT includes normals", isVisible: isTyflow);
            AddToggle(rootItem, block, "_AffectsShadows", "Affects shadows", isVisible: isTyflow);
            AddFloat(rootItem, block, "_Frame", "Frame", isTyflow);
            new VatFrameCustomDataItem(rootItem, block, () => Content("VAT Frame CustomData"), () => isTyflow() && hasVatFrameCustomData(), hasVatFrameCustomData);
            AddFloat(rootItem, block, "_Frames", "Frames", isTyflow);
            AddToggle(rootItem, block, "_FrameInterpolation", "Frame interpolation", isVisible: isTyflow);
            AddToggle(rootItem, block, "_Loop", "Loop", isVisible: isTyflow);
            AddToggle(rootItem, block, "_InterpolateLoop", "Interpolate loop", isVisible: isTyflow);
            AddToggle(rootItem, block, "_Autoplay", "Autoplay", isVisible: isTyflow);
            AddFloat(rootItem, block, "_AutoplaySpeed", "AutoplaySpeed", isTyflow);
        }

        private void AddMaskMap(
            NBShaderRootItem rootItem,
            ShaderGUIItem parent,
            string texturePropertyName,
            string modePropertyName,
            string uvFoldOutPropertyName,
            string label,
            int wrapFlag,
            int colorChannelFlagPos,
            int uvModeFlagPos,
            int gradientFlag,
            int gradientFlagIndex,
            string textureFoldOutPropertyName = null)
        {
            new PopupItem(rootItem, parent, modePropertyName, () => Content(label + "模式"), TextureGradientNames,
                property => rootItem.SyncService.ApplyToggleFlag(gradientFlag, property.floatValue > 0.5f, gradientFlagIndex));
            ShaderGUIItem textureParent = parent;
            if (string.IsNullOrEmpty(textureFoldOutPropertyName))
            {
                AddTextureWithWrap(rootItem, parent, texturePropertyName, label + "贴图", wrapFlag,
                    isVisible: () => IsPropertyMode(rootItem, modePropertyName, 0));
            }
            else
            {
                textureParent = AddTextureWithRelatedFoldOut(rootItem, parent, texturePropertyName, label + "贴图", textureFoldOutPropertyName, wrapFlag,
                    isVisible: () => IsPropertyMode(rootItem, modePropertyName, 0));
            }

            new ColorChannelSelectItem(rootItem, textureParent, colorChannelFlagPos, 0, () => Content(label + "通道选择"),
                () => IsPropertyMode(rootItem, modePropertyName, 0));
            AddAlphaGradient(rootItem, parent, label + "渐变", texturePropertyName + "GradientCount", texturePropertyName + "GradientFloat",
                () => IsPropertyMode(rootItem, modePropertyName, 1));
            new TextureScaleOffsetItem(rootItem, parent, texturePropertyName, false, () => IsPropertyMode(rootItem, modePropertyName, 1), TillingContent, OffsetContent);
            new WrapModeItem(rootItem, parent, wrapFlag, () => Content(label + "UV Wrap"), 2,
                () => IsPropertyMode(rootItem, modePropertyName, 1));
            new UVModeSelectItem(rootItem, parent, uvFoldOutPropertyName, uvModeFlagPos, 0, () => Content(label + "UV来源"), texturePropertyName);
        }

        private TextureRelatedFoldOutItem AddTextureWithRelatedFoldOut(
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

        private void AddTextureWithWrap(
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

        private PropertyToggleBlockItem ToggleBlock(
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

        private static void AddToggle(
            NBShaderRootItem rootItem,
            ShaderGUIItem parent,
            string propertyName,
            string label,
            Action<bool> onValueChanged = null,
            Func<bool> isVisible = null)
        {
            new ToggleItem(rootItem, parent, propertyName, () => Content(label), onValueChanged, isVisible);
        }

        private static void AddFloat(NBShaderRootItem rootItem, ShaderGUIItem parent, string propertyName, string label, Func<bool> isVisible = null)
        {
            FloatItem item = new FloatItem(rootItem, parent, isVisible)
            {
                PropertyName = propertyName,
                GuiContent = Content(label)
            };
            item.InitTriggerByChild();
        }

        private static void AddSlider(
            NBShaderRootItem rootItem,
            ShaderGUIItem parent,
            string propertyName,
            string label,
            float min = 0f,
            float max = 1f,
            string rangePropertyName = null,
            Func<bool> isVisible = null,
            int vectorComponentIndex = -1)
        {
            if (vectorComponentIndex >= 0)
            {
                if (!string.IsNullOrEmpty(rangePropertyName))
                {
                    new VectorComponentRangeSliderItem(rootItem, parent, propertyName, vectorComponentIndex, rangePropertyName, () => Content(label), isVisible);
                    return;
                }

                new VectorComponentItem(rootItem, parent, propertyName, vectorComponentIndex, () => Content(label), true, min, max, isVisible);
                return;
            }

            SliderItem item = new SliderItem(rootItem, parent, isVisible)
            {
                PropertyName = propertyName,
                GuiContent = Content(label),
                Min = min,
                Max = max,
                RangePropertyName = rangePropertyName
            };
            item.InitTriggerByChild();
        }

        private static void AddVectorComponent(
            NBShaderRootItem rootItem,
            ShaderGUIItem parent,
            string propertyName,
            int componentIndex,
            string label,
            bool isSlider,
            float min = 0f,
            float max = 1f,
            Func<bool> isVisible = null)
        {
            new VectorComponentItem(rootItem, parent, propertyName, componentIndex, () => Content(label), isSlider, min, max, isVisible);
        }

        private static void AddSectionLabel(NBShaderRootItem rootItem, ShaderGUIItem parent, string label, Func<bool> isVisible = null)
        {
            new SectionLabelItem(rootItem, parent, () => Content(label), isVisible);
        }

        private static bool IsDissolveMaskStrengthSlider(NBShaderRootItem rootItem)
        {
            return rootItem.PropertyInfoDic.TryGetValue("_DissolveMaskMode", out ShaderPropertyInfo info) &&
                   !info.Property.hasMixedValue &&
                   info.Property.floatValue > 0.5f;
        }

        private static void AddAlphaGradient(
            NBShaderRootItem rootItem,
            ShaderGUIItem parent,
            string label,
            string countPropertyName,
            string alphaPrefix,
            Func<bool> isVisible = null)
        {
            new GradientItem(
                rootItem,
                parent,
                countPropertyName,
                6,
                Array.Empty<string>(),
                BuildPropertyNames(alphaPrefix, 3),
                () => Content(label),
                false,
                ColorSpace.Gamma,
                isVisible);
        }

        private static void AddGradient(
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

        private static string[] BuildPropertyNames(string prefix, int count)
        {
            string[] names = new string[count];
            for (int i = 0; i < count; i++)
            {
                names[i] = prefix + i;
            }

            return names;
        }

        private static bool IsPropertyMode(NBShaderRootItem rootItem, string propertyName, int expectedMode)
        {
            return rootItem.PropertyInfoDic.TryGetValue(propertyName, out ShaderPropertyInfo info) &&
                   !info.Property.hasMixedValue &&
                   Mathf.RoundToInt(info.Property.floatValue) == expectedMode;
        }

        private static bool IsPropertyMode(NBShaderRootItem rootItem, string propertyName, params int[] expectedModes)
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

        private static bool IsPropertyGreater(NBShaderRootItem rootItem, string propertyName, float threshold)
        {
            return rootItem.PropertyInfoDic.TryGetValue(propertyName, out ShaderPropertyInfo info) &&
                   !info.Property.hasMixedValue &&
                   info.Property.floatValue > threshold;
        }

        private static bool IsPropertyOn(NBShaderRootItem rootItem, string propertyName)
        {
            return IsPropertyGreater(rootItem, propertyName, 0.5f);
        }

        private static bool IsPropertyOff(NBShaderRootItem rootItem, string propertyName)
        {
            return rootItem.PropertyInfoDic.TryGetValue(propertyName, out ShaderPropertyInfo info) &&
                   !info.Property.hasMixedValue &&
                   info.Property.floatValue <= 0.5f;
        }

        private static bool IsParallaxMaxLayerHigh(NBShaderRootItem rootItem)
        {
            return rootItem.PropertyInfoDic.TryGetValue("_ParallaxMapping_Vec", out ShaderPropertyInfo info) &&
                   !info.Property.hasMixedValue &&
                   info.Property.vectorValue.y >= 20f;
        }

        private static bool ShouldDrawWhenFloatOn(NBShaderRootItem rootItem, string propertyName)
        {
            if (!rootItem.PropertyInfoDic.TryGetValue(propertyName, out ShaderPropertyInfo info))
            {
                return false;
            }

            return info.Property.hasMixedValue || info.Property.floatValue > 0.5f;
        }

        private static bool ShouldDrawWhenFloatOff(NBShaderRootItem rootItem, string propertyName)
        {
            if (!rootItem.PropertyInfoDic.TryGetValue(propertyName, out ShaderPropertyInfo info))
            {
                return false;
            }

            return info.Property.hasMixedValue || info.Property.floatValue <= 0.5f;
        }

        private static bool TryGetPropertyMode(NBShaderRootItem rootItem, string propertyName, out int mode)
        {
            mode = 0;
            if (!rootItem.PropertyInfoDic.TryGetValue(propertyName, out ShaderPropertyInfo info) ||
                info.Property.hasMixedValue)
            {
                return false;
            }

            mode = Mathf.RoundToInt(info.Property.floatValue);
            return true;
        }

        private static bool ShouldDrawHoudiniRotationTexture(NBShaderRootItem rootItem)
        {
            return TryGetPropertyMode(rootItem, "_HoudiniVATSubMode", out int subMode) &&
                   subMode != 3 &&
                   ShouldDrawWhenFloatOff(rootItem, "_B_UNLOAD_ROT_TEX");
        }

        private static bool ShouldDrawCompressedNormalsToggle(NBShaderRootItem rootItem)
        {
            return TryGetPropertyMode(rootItem, "_HoudiniVATSubMode", out int subMode) && subMode != 3;
        }

        private static int GetIntProperty(NBShaderRootItem rootItem, string propertyName)
        {
            return rootItem.PropertyInfoDic.TryGetValue(propertyName, out ShaderPropertyInfo info) && !info.Property.hasMixedValue
                ? Mathf.RoundToInt(info.Property.floatValue)
                : 0;
        }

        private static bool IsVatFrameCustomDataVisible(NBShaderRootItem rootItem)
        {
            return IsAnyHoudiniParticleModeEnabled(rootItem) || IsAnyTyflowParticleModeEnabled(rootItem);
        }

        private static bool HasTyflowParticleUV2Conflict(NBShaderRootItem rootItem)
        {
            if (rootItem.Mats == null || rootItem.ShaderFlags == null)
            {
                return false;
            }

            int count = Mathf.Min(rootItem.Mats.Count, rootItem.ShaderFlags.Count);
            for (int i = 0; i < count; i++)
            {
                Material mat = rootItem.Mats[i];
                if (!IsTyflowParticleModeEnabled(mat) ||
                    !(rootItem.ShaderFlags[i] is W9ParticleShaderFlags flags))
                {
                    continue;
                }

                bool flipbook = mat.IsKeywordEnabled("_FLIPBOOKBLENDING_ON");
                bool specialUVUsesUV2 = flags.CheckIsUVModeOn(W9ParticleShaderFlags.UVMode.SpecialUVChannel) &&
                                         !flags.CheckFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_USE_TEXCOORD2, index: 1);
                if (flipbook || specialUVUsesUV2)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsAnyHoudiniParticleModeEnabled(NBShaderRootItem rootItem)
        {
            if (rootItem.Mats == null)
            {
                return false;
            }

            for (int i = 0; i < rootItem.Mats.Count; i++)
            {
                if (IsHoudiniParticleModeEnabled(rootItem.Mats[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsAnyTyflowParticleModeEnabled(NBShaderRootItem rootItem)
        {
            if (rootItem.Mats == null)
            {
                return false;
            }

            for (int i = 0; i < rootItem.Mats.Count; i++)
            {
                if (IsTyflowParticleModeEnabled(rootItem.Mats[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasUnsupportedHoudiniParticleMode(NBShaderRootItem rootItem)
        {
            if (rootItem.Mats == null)
            {
                return false;
            }

            for (int i = 0; i < rootItem.Mats.Count; i++)
            {
                Material mat = rootItem.Mats[i];
                if (IsHoudiniParticleModeEnabled(mat) &&
                    GetMaterialInt(mat, "_HoudiniVATSubMode", 0) == 1)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasUnsupportedTyflowParticleMode(NBShaderRootItem rootItem)
        {
            if (rootItem.Mats == null)
            {
                return false;
            }

            for (int i = 0; i < rootItem.Mats.Count; i++)
            {
                Material mat = rootItem.Mats[i];
                if (IsVatParticleMode(mat, (int)VATMode.Tyflow) &&
                    GetMaterialInt(mat, "_TyFlowVATSubMode", 0) >= 2)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsHoudiniParticleModeEnabled(Material mat)
        {
            return IsVatParticleMode(mat, (int)VATMode.Houdini);
        }

        private static bool IsTyflowParticleModeEnabled(Material mat)
        {
            return IsVatParticleMode(mat, (int)VATMode.Tyflow) &&
                   (!mat.HasProperty("_TyFlowVATSubMode") || Mathf.RoundToInt(mat.GetFloat("_TyFlowVATSubMode")) <= 1);
        }

        private static bool IsVatParticleMode(Material mat, int vatMode)
        {
            if (mat == null ||
                !mat.HasProperty("_VAT_Toggle") ||
                !mat.HasProperty("_VATMode") ||
                !mat.HasProperty("_MeshSourceMode") ||
                mat.GetFloat("_VAT_Toggle") <= 0.5f ||
                Mathf.RoundToInt(mat.GetFloat("_VATMode")) != vatMode)
            {
                return false;
            }

            MeshSourceMode meshSourceMode = (MeshSourceMode)Mathf.RoundToInt(mat.GetFloat("_MeshSourceMode"));
            return meshSourceMode == MeshSourceMode.Particle ||
                   meshSourceMode == MeshSourceMode.UIParticle;
        }

        private static int GetMaterialInt(Material mat, string propertyName, int defaultValue)
        {
            return mat != null && mat.HasProperty(propertyName)
                ? Mathf.RoundToInt(mat.GetFloat(propertyName))
                : defaultValue;
        }

        private static GUIContent Content(string label)
        {
            return NBShaderInspectorLocalization.MakeContent("inspector.feature." + label + ".label", label);
        }

        private static GUIContent TillingContent()
        {
            return NBShaderInspectorLocalization.MakeInspectorContent("common.tilling", "Tilling");
        }

        private static GUIContent OffsetContent()
        {
            return NBShaderInspectorLocalization.MakeInspectorContent("common.offset", "Offset");
        }

        private static string Text(string key, string fallback)
        {
            return NBShaderInspectorLocalization.GetInspectorText(key, fallback);
        }

        private static string[] PopupOptions(string propertyName, string[] fallback)
        {
            return NBShaderInspectorLocalization.GetInspectorOptions("feature.popup." + propertyName, fallback);
        }

        private class FloatItem : ShaderGUIFloatItem
        {
            private readonly Func<bool> _isVisible;

            public FloatItem(ShaderGUIRootItem rootItem, ShaderGUIItem parentItem, Func<bool> isVisible)
                : base(rootItem, parentItem)
            {
                _isVisible = isVisible;
            }

            public override void OnGUI()
            {
                if (_isVisible != null && !_isVisible())
                {
                    return;
                }

                base.OnGUI();
            }
        }

        private class SliderItem : ShaderGUISliderItem
        {
            private readonly Func<bool> _isVisible;

            public SliderItem(ShaderGUIRootItem rootItem, ShaderGUIItem parentItem, Func<bool> isVisible)
                : base(rootItem, parentItem)
            {
                _isVisible = isVisible;
            }

            public override void OnGUI()
            {
                if (_isVisible != null && !_isVisible())
                {
                    return;
                }

                base.OnGUI();
            }
        }

        private class SectionLabelItem : ShaderGUIItem
        {
            private readonly Func<GUIContent> _contentProvider;
            private readonly Func<bool> _isVisible;

            public SectionLabelItem(ShaderGUIRootItem rootItem, ShaderGUIItem parentItem, Func<GUIContent> contentProvider, Func<bool> isVisible)
                : base(rootItem, parentItem)
            {
                _contentProvider = contentProvider ?? (() => GUIContent.none);
                _isVisible = isVisible;
            }

            public override void OnGUI()
            {
                if (_isVisible != null && !_isVisible())
                {
                    return;
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField(_contentProvider(), EditorStyles.boldLabel);
            }
        }

        private class HelpBoxItem : ShaderGUIItem
        {
            private readonly Func<string> _messageProvider;
            private readonly MessageType _messageType;
            private readonly Func<bool> _isVisible;

            public HelpBoxItem(ShaderGUIRootItem rootItem, ShaderGUIItem parentItem, Func<string> messageProvider, MessageType messageType, Func<bool> isVisible)
                : base(rootItem, parentItem)
            {
                _messageProvider = messageProvider ?? (() => string.Empty);
                _messageType = messageType;
                _isVisible = isVisible;
            }

            public override void OnGUI()
            {
                if (_isVisible != null && !_isVisible())
                {
                    return;
                }

                EditorGUILayout.HelpBox(_messageProvider(), _messageType);
            }
        }

        private class NoiseAffectItem : ShaderGUIItem
        {
            private readonly NBShaderRootItem _nbRootItem;

            public NoiseAffectItem(NBShaderRootItem rootItem, ShaderGUIItem parentItem)
                : base(rootItem, parentItem)
            {
                _nbRootItem = rootItem;
            }

            public override void OnGUI()
            {
                bool previousMixedValue = EditorGUI.showMixedValue;
                bool noiseEnabledHasMixedValue = _nbRootItem.Context.NoiseEnabled == MixedBool.Mixed;
                using (new EditorGUI.DisabledScope(_nbRootItem.Context.NoiseEnabled == MixedBool.False))
                {
                    for (int i = 0; i < ChildrenItemList.Count; i++)
                    {
                        EditorGUI.showMixedValue = noiseEnabledHasMixedValue;
                        ChildrenItemList[i].OnGUI();
                    }
                }

                EditorGUI.showMixedValue = previousMixedValue;
            }

            public override void CheckIsPropertyModified(bool isCallByChild = false)
            {
                HasModified = false;
                for (int i = 0; i < ChildrenItemList.Count; i++)
                {
                    HasModified |= ChildrenItemList[i].HasModified;
                }

                ParentItem?.CheckIsPropertyModified(true);
            }
        }

        private class FlipbookToggleItem : ToggleItem
        {
            private readonly NBShaderRootItem _nbRootItem;

            public FlipbookToggleItem(NBShaderRootItem rootItem, ShaderGUIItem parentItem)
                : base(
                    rootItem,
                    parentItem,
                    "_FlipbookBlending",
                    () => Content("序列帧融帧(丝滑)"),
                    rootItem.SyncService.ApplyFlipbookEnabled)
            {
                _nbRootItem = rootItem;
            }

            public override void DrawBlock()
            {
                if (PropertyInfo.Property.hasMixedValue || PropertyInfo.Property.floatValue <= 0.5f)
                {
                    return;
                }

                if (_nbRootItem.Context.MeshSourceMode == MeshSourceMode.Particle ||
                    _nbRootItem.Context.MeshSourceMode == MeshSourceMode.UIParticle)
                {
                    if (HasSpecialUVChannel())
                    {
                        EditorGUILayout.HelpBox(
                            Text(
                                "feature.flipbook.specialUvWarning.message",
                                "序列帧融帧和特殊UV通道同时开启，粒子序列帧应该影响UV0和UV1两个通道，特殊通道只能使用UV3（原始UV）"),
                            MessageType.Warning);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox(
                            Text(
                                "feature.flipbook.particleInfo.message",
                                "AnimationSheet的AffectUVChannel需要有UV0和UV1"),
                            MessageType.Info);
                    }

                    return;
                }

                if (_nbRootItem.Context.MeshSourceMode == MeshSourceMode.Mesh)
                {
                    EditorGUILayout.HelpBox(
                        Text(
                            "feature.flipbook.meshInfo.message",
                            "需要添加AnimationSheetHelper脚本"),
                        MessageType.Info);
                }
            }

            private bool HasSpecialUVChannel()
            {
                for (int i = 0; i < _nbRootItem.ShaderFlags.Count; i++)
                {
                    if (_nbRootItem.ShaderFlags[i] is W9ParticleShaderFlags flags &&
                        flags.CheckIsUVModeOn(W9ParticleShaderFlags.UVMode.SpecialUVChannel))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        private class VatFrameCustomDataItem : CustomDataSelectItem
        {
            private readonly Func<bool> _isVisible;
            private readonly Func<bool> _anyVatFrameCustomDataVisible;

            public VatFrameCustomDataItem(
                NBShaderRootItem rootItem,
                ShaderGUIItem parentItem,
                Func<GUIContent> contentProvider,
                Func<bool> isVisible,
                Func<bool> anyVatFrameCustomDataVisible)
                : base(rootItem, parentItem, W9ParticleShaderFlags.FLAGBIT_POS_2_CUSTOMDATA_VAT_FRAME, 2, contentProvider)
            {
                _isVisible = isVisible;
                _anyVatFrameCustomDataVisible = anyVatFrameCustomDataVisible;
            }

            public override void OnGUI()
            {
                if (_isVisible != null && !_isVisible())
                {
                    if (_anyVatFrameCustomDataVisible == null || !_anyVatFrameCustomDataVisible())
                    {
                        ClearVatFrameCustomData();
                    }

                    return;
                }

                base.OnGUI();
            }

            private void ClearVatFrameCustomData()
            {
                for (int i = 0; i < RootItem.ShaderFlags.Count; i++)
                {
                    if (RootItem.ShaderFlags[i] is W9ParticleShaderFlags flags)
                    {
                        flags.SetCustomDataFlag(
                            W9ParticleShaderFlags.CutomDataComponent.Off,
                            W9ParticleShaderFlags.FLAGBIT_POS_2_CUSTOMDATA_VAT_FRAME,
                            2);
                    }
                }
            }
        }

        private class PopupItem : ShaderGUIPopUpItem
        {
            private readonly Func<GUIContent> _contentProvider;
            private readonly Func<string[]> _popupNamesProvider;
            private readonly Action<MaterialProperty> _onValueChanged;
            private readonly Func<bool> _isVisible;

            public PopupItem(
                ShaderGUIRootItem rootItem,
                ShaderGUIItem parentItem,
                string propertyName,
                Func<GUIContent> contentProvider,
                string[] popupNames,
                Action<MaterialProperty> onValueChanged = null,
                Func<bool> isVisible = null) : base(rootItem, parentItem)
            {
                PropertyName = propertyName;
                _contentProvider = contentProvider;
                _popupNamesProvider = () => PopupOptions(propertyName, popupNames);
                _onValueChanged = onValueChanged;
                _isVisible = isVisible;
                PopUpNames = _popupNamesProvider();
                InitTriggerByChild();
            }

            public override void OnGUI()
            {
                if (_isVisible != null && !_isVisible())
                {
                    return;
                }

                GuiContent = _contentProvider();
                PopUpNames = _popupNamesProvider();
                base.OnGUI();
            }

            public override void OnEndChange()
            {
                base.OnEndChange();
                _onValueChanged?.Invoke(PropertyInfo.Property);
            }
        }
    }
}
