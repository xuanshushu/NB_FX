using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using NBShader;

namespace NBShaderEditor
{
    public enum RenderFace
    {
        Front = 2,
        Back = 1,
        Both = 0
    }

    public enum ForceZWriteMode
    {
        Default = 0,
        ForceOn = 1,
        ForceOff = 2
    }

    public class BaseOptionBigBlockItem : BigBlockItem
    {
        private readonly NBShaderRootItem _nbRootItem;
        private readonly ShaderGUIFloatItem _baseColorIntensityItem;
        private readonly ShaderGUISliderItem _alphaAllItem;
        private readonly BlockItem _colorAdjustmentBlock;
        private readonly ToggleItem _colorAdjustmentOnlyMainTexItem;
        private readonly PropertyToggleBlockItem _hueShiftBlock;
        private readonly ShaderGUISliderItem _hueShiftSlider;
        private readonly CustomDataSelectItem _hueShiftCustomDataItem;
        private readonly PropertyToggleBlockItem _saturabilityBlock;
        private readonly ShaderGUISliderItem _saturabilitySlider;
        private readonly CustomDataSelectItem _saturabilityCustomDataItem;
        private readonly PropertyToggleBlockItem _contrastBlock;
        private readonly ColorItem _contrastMidColorItem;
        private readonly ShaderGUISliderItem _contrastSlider;
        private readonly CustomDataSelectItem _contrastCustomDataItem;
        private readonly PropertyToggleBlockItem _baseMapColorRefineBlock;
        private readonly VectorComponentItem _baseMapColorRefineA;
        private readonly VectorComponentItem _baseMapColorRefineBPower;
        private readonly VectorComponentItem _baseMapColorRefineBMultiply;
        private readonly VectorComponentItem _baseMapColorRefineLerp;
        private readonly ToggleItem _colorMultiAlphaItem;
        private readonly ZTestItem _zTestItem;
        private readonly CullModeItem _cullItem;
        private readonly ToggleItem _backFirstPassItem;
        private readonly ForceZWriteItem _forceZWriteItem;
        private readonly PropertyToggleBlockItem _baseBackColorBlock;
        private readonly ColorItem _baseBackColorItem;
        private readonly PropertyToggleBlockItem _distanceFadeBlock;
        private readonly Vector2LineItem _fadeRangeItem;
        private readonly PropertyToggleBlockItem _softParticlesBlock;
        private readonly Vector2LineItem _softParticleFadeItem;
        private readonly ToggleItem _stencilWithoutPlayerItem;
        private readonly ToggleItem _ignoreVertexColorItem;
        private readonly ShaderGUISliderItem _fogIntensityItem;

        public BaseOptionBigBlockItem(NBShaderRootItem rootItem, ShaderGUIItem parentItem) :
            base(
                rootItem,
                parentItem,
                "_BaseOptionBigBlockItemFoldOut",
                () => Content("block.base", "Base Options", "Global controls"))
        {
            _nbRootItem = rootItem;

            _baseColorIntensityItem = new ShaderGUIFloatItem(rootItem, this)
            {
                PropertyName = "_BaseColorIntensityForTimeline",
                GuiContent = Content("base.colorIntensity", "Base Color Intensity")
            };
            _baseColorIntensityItem.InitTriggerByChild();

            _alphaAllItem = new ShaderGUISliderItem(rootItem, this)
            {
                PropertyName = "_AlphaAll",
                GuiContent = Content("base.alphaAll", "Overall Alpha"),
                RangePropertyName = "AlphaAllRangeVec"
            };
            _alphaAllItem.InitTriggerByChild();

            _colorAdjustmentBlock = new BlockItem(
                rootItem,
                this,
                "_BaseColorAdjustmentFoldOut",
                () => Content("base.colorAdjustment", "Color Adjustment"));

            _colorAdjustmentOnlyMainTexItem = new ToggleItem(
                rootItem,
                _colorAdjustmentBlock,
                "_ColorAdjustmentOnlyAffectMainTex",
                () => Content("base.colorAdjustment.onlyMainTex", "Only Affect Main Texture"),
                enabled => rootItem.SyncService.ApplyToggleFlag(
                    W9ParticleShaderFlags.FLAG_BIT_PARTICLE_COLOR_ADJUSTMENT_ONLY_AFFECT_MAINTEX,
                    enabled));

            _hueShiftBlock = new PropertyToggleBlockItem(
                rootItem,
                _colorAdjustmentBlock,
                "_HueShiftFoldOut",
                "_HueShift_Toggle",
                () => Content("base.hueShift", "Hue Shift"),
                W9ParticleShaderFlags.FLAG_BIT_HUESHIFT_ON);
            _hueShiftSlider = new ShaderGUISliderItem(rootItem, _hueShiftBlock)
            {
                PropertyName = "_HueShift",
                GuiContent = Content("base.hueShift.value", "Hue"),
                Min = 0f,
                Max = 1f
            };
            _hueShiftSlider.InitTriggerByChild();
            _hueShiftCustomDataItem = new CustomDataSelectItem(
                rootItem,
                _hueShiftBlock,
                W9ParticleShaderFlags.FLAGBIT_POS_0_CUSTOMDATA_HUESHIFT,
                0,
                () => Content("base.hueShift.customData", "Hue Custom Data"),
                IsParticleMode);

            _saturabilityBlock = new PropertyToggleBlockItem(
                rootItem,
                _colorAdjustmentBlock,
                "_SaturabilityFoldOut",
                "_ChangeSaturability_Toggle",
                () => Content("base.saturability", "Saturation"),
                W9ParticleShaderFlags.FLAG_BIT_SATURABILITY_ON);
            _saturabilitySlider = new ShaderGUISliderItem(rootItem, _saturabilityBlock)
            {
                PropertyName = "_Saturability",
                GuiContent = Content("base.saturability.value", "Saturation"),
                RangePropertyName = "SaturabilityRangeVec"
            };
            _saturabilitySlider.InitTriggerByChild();
            _saturabilityCustomDataItem = new CustomDataSelectItem(
                rootItem,
                _saturabilityBlock,
                W9ParticleShaderFlags.FLAGBIT_POS_1_CUSTOMDATA_SATURATE,
                1,
                () => Content("base.saturability.customData", "Saturation Custom Data"),
                IsParticleMode);

            _contrastBlock = new PropertyToggleBlockItem(
                rootItem,
                _colorAdjustmentBlock,
                "_ContrastFoldOut",
                "_Contrast_Toggle",
                () => Content("base.contrast", "Contrast"),
                W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_MAINTEX_CONTRAST,
                1);
            _contrastMidColorItem = new ColorItem(rootItem, _contrastBlock, "_ContrastMidColor", () => Content("base.contrast.mid", "Contrast Mid Color"));
            _contrastSlider = new ShaderGUISliderItem(rootItem, _contrastBlock)
            {
                PropertyName = "_Contrast",
                GuiContent = Content("base.contrast.value", "Contrast"),
                Min = 0f,
                Max = 5f
            };
            _contrastSlider.InitTriggerByChild();
            _contrastCustomDataItem = new CustomDataSelectItem(
                rootItem,
                _contrastBlock,
                W9ParticleShaderFlags.FLAGBIT_POS_2_CUSTOMDATA_MAINTEX_CONTRAST,
                2,
                () => Content("base.contrast.customData", "Contrast Custom Data"),
                IsParticleMode);

            _baseMapColorRefineBlock = new PropertyToggleBlockItem(
                rootItem,
                _colorAdjustmentBlock,
                "_BaseMapColorRefineFoldOut",
                "_BaseMapColorRefine_Toggle",
                () => Content("base.colorRefine", "Color Refine"),
                W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_MAINTEX_COLOR_REFINE,
                1);
            _baseMapColorRefineA = new VectorComponentItem(rootItem, _baseMapColorRefineBlock, "_BaseMapColorRefine", 0, () => Content("base.colorRefine.a", "A Main Color Multiply"), false);
            _baseMapColorRefineBPower = new VectorComponentItem(rootItem, _baseMapColorRefineBlock, "_BaseMapColorRefine", 1, () => Content("base.colorRefine.bPower", "B Main Color Power"), false);
            _baseMapColorRefineBMultiply = new VectorComponentItem(rootItem, _baseMapColorRefineBlock, "_BaseMapColorRefine", 2, () => Content("base.colorRefine.bMultiply", "B After Power Multiply"), false);
            _baseMapColorRefineLerp = new VectorComponentItem(rootItem, _baseMapColorRefineBlock, "_BaseMapColorRefine", 3, () => Content("base.colorRefine.lerp", "A/B Lerp"), true, 0f, 1f);

            _colorMultiAlphaItem = new ToggleItem(
                rootItem,
                _colorAdjustmentBlock,
                "_ColorMultiAlpha",
                () => Content("base.colorMultiAlpha", "Color Multiply Alpha"),
                enabled => rootItem.SyncService.ApplyToggleFlag(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_COLOR_MULTI_ALPHA, enabled));

            _zTestItem = new ZTestItem(rootItem, this);
            _cullItem = new CullModeItem(rootItem, this);

            _backFirstPassItem = new ToggleItem(
                rootItem,
                this,
                "_BackFirstPassToggle",
                () => Content("base.backFirstPass", "Back First Pass"),
                OnBackFirstPassChanged,
                () => Is3DTransparent());

            _forceZWriteItem = new ForceZWriteItem(rootItem, this);

            _baseBackColorBlock = new PropertyToggleBlockItem(
                rootItem,
                this,
                "_BaseBackColorFoldOut",
                "_BaseBackColor_Toggle",
                () => Content("base.backColor", "Back Color"),
                W9ParticleShaderFlags.FLAG_BIT_PARTICLE_BACKCOLOR,
                0,
                isVisible: Is3DMode);
            _baseBackColorItem = new ColorItem(rootItem, _baseBackColorBlock, "_BaseBackColor", () => Content("base.backColor.color", "Back Color"));

            _distanceFadeBlock = new PropertyToggleBlockItem(
                rootItem,
                this,
                "_DistanceFadeFoldOut",
                "_DistanceFade_Toggle",
                () => Content("base.distanceFade", "Distance Fade"),
                W9ParticleShaderFlags.FLAG_BIT_PARTICLE_DISTANCEFADE_ON,
                0,
                isVisible: Is3DMode);
            _fadeRangeItem = new Vector2LineItem(rootItem, _distanceFadeBlock, "_Fade", true, () => Content("base.distanceFade.range", "Fade Range"));

            _softParticlesBlock = new PropertyToggleBlockItem(
                rootItem,
                this,
                "_SoftParticlesFoldOut",
                "_SoftParticlesEnabled",
                () => Content("base.softParticles", "Soft Particles"),
                keyword: "_SOFTPARTICLES_ON",
                isVisible: Is3DMode);
            _softParticleFadeItem = new Vector2LineItem(rootItem, _softParticlesBlock, "_SoftParticleFadeParams", true, () => Content("base.softParticles.range", "Near/Far Fade"));

            _stencilWithoutPlayerItem = new ToggleItem(
                rootItem,
                this,
                "_StencilWithoutPlayerToggle",
                () => Content("base.stencilWithoutPlayer", "Stencil Without Player"),
                OnStencilWithoutPlayerChanged,
                Is3DMode);

            _ignoreVertexColorItem = new ToggleItem(
                rootItem,
                this,
                "_IgnoreVetexColor_Toggle",
                () => Content("base.ignoreVertexColor", "Ignore Vertex Color"),
                enabled => rootItem.SyncService.ApplyToggleFlag(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_IGNORE_VERTEX_COLOR, enabled, 1),
                Is3DMode);

            _fogIntensityItem = new ShaderGUISliderItem(rootItem, this)
            {
                PropertyName = "_fogintensity",
                GuiContent = Content("base.fogIntensity", "Fog Intensity"),
                Min = 0f,
                Max = 1f
            };
            _fogIntensityItem.InitTriggerByChild();

            InitTriggerByChild();
        }

        public override void DrawBlock()
        {
            _baseColorIntensityItem.OnGUI();
            _alphaAllItem.OnGUI();
            _colorAdjustmentBlock.OnGUI();
            _zTestItem.OnGUI();
            _cullItem.OnGUI();
            _backFirstPassItem.OnGUI();
            _forceZWriteItem.OnGUI();
            _baseBackColorBlock.OnGUI();
            _distanceFadeBlock.OnGUI();
            _softParticlesBlock.OnGUI();
            _stencilWithoutPlayerItem.OnGUI();
            _ignoreVertexColorItem.OnGUI();

            if (Is3DMode())
            {
                _fogIntensityItem.OnGUI();
            }
            else if (_nbRootItem.PropertyInfoDic.ContainsKey("_fogintensity"))
            {
                _nbRootItem.PropertyInfoDic["_fogintensity"].Property.floatValue = 0f;
            }
        }

        private bool Is3DMode()
        {
            return _nbRootItem.Context.UIEffectEnabled == MixedBool.False;
        }

        private bool Is3DTransparent()
        {
            return Is3DMode() && _nbRootItem.Context.TransparentMode == TransparentMode.Transparent;
        }

        private bool IsParticleMode()
        {
            return _nbRootItem.Context.ParticleMode == MixedBool.True;
        }

        private void OnBackFirstPassChanged(bool enabled)
        {
            _nbRootItem.SyncService.ApplyShaderPass("SRPDefaultUnlit", enabled);
            if (enabled && _nbRootItem.PropertyInfoDic.ContainsKey("_Cull"))
            {
                _nbRootItem.PropertyInfoDic["_Cull"].Property.floatValue = (float)RenderFace.Front;
            }
        }

        private void OnStencilWithoutPlayerChanged(bool enabled)
        {
            _nbRootItem.SyncService.ApplyToggleKeyword("_STENCIL_WITHOUT_PLAYER", enabled);
            _nbRootItem.SyncService.ApplyStencilPreset(enabled ? "ParticleWithoutPlayer" : "ParticleBaseDefault");
            if (_nbRootItem.PropertyInfoDic.ContainsKey("_CustomStencilTest"))
            {
                _nbRootItem.PropertyInfoDic["_CustomStencilTest"].Property.floatValue = enabled ? 1f : 0f;
            }
        }

        private static GUIContent Content(string key, string fallback, string tip = "")
        {
            return NBShaderInspectorLocalization.MakeContent("inspector." + key + ".label", fallback, "inspector." + key + ".tip", tip);
        }
    }

    public class ZTestItem : ShaderGUIPopUpItem
    {
        public ZTestItem(ShaderGUIRootItem rootItem, ShaderGUIItem parentItem) : base(rootItem, parentItem: parentItem)
        {
            PropertyName = "_ZTest";
            GuiContent = NBShaderInspectorLocalization.MakeInspectorContent("base.ztest", "ZTest");
            PopUpNames = Enum.GetNames(typeof(CompareFunction));
            InitTriggerByChild();
        }

        public override void OnGUI()
        {
            GuiContent = NBShaderInspectorLocalization.MakeInspectorContent("base.ztest", "ZTest");
            if (RootItem is NBShaderRootItem nbRootItem)
            {
                if (nbRootItem.Context.UIEffectEnabled == MixedBool.True)
                {
                    if (!Mathf.Approximately(PropertyInfo.Property.floatValue, (float)CompareFunction.LessEqual))
                    {
                        PropertyInfo.Property.floatValue = (float)CompareFunction.LessEqual;
                    }

                    return;
                }

                if (nbRootItem.Context.UIEffectEnabled == MixedBool.Mixed)
                {
                    return;
                }
            }

            base.OnGUI();
        }
    }

    public class CullModeItem : ShaderGUIPopUpItem
    {
        public CullModeItem(ShaderGUIRootItem rootItem, ShaderGUIItem parentItem) : base(rootItem, parentItem)
        {
            PropertyName = "_Cull";
            GuiContent = NBShaderInspectorLocalization.MakeInspectorContent("base.cull", "Cull");
            PopUpNames = Enum.GetNames(typeof(RenderFace));
            InitTriggerByChild();
        }

        public override void OnGUI()
        {
            GuiContent = NBShaderInspectorLocalization.MakeInspectorContent("base.cull", "Cull");
            base.OnGUI();
        }
    }

    public class ForceZWriteItem : ShaderGUIPopUpItem
    {
        private static readonly string[] Options = { "Default", "Force On", "Force Off" };

        public ForceZWriteItem(ShaderGUIRootItem rootItem, ShaderGUIItem parentItem) : base(rootItem, parentItem)
        {
            PropertyName = "_ForceZWriteToggle";
            GuiContent = NBShaderInspectorLocalization.MakeInspectorContent("base.forceZWrite", "Force ZWrite");
            PopUpNames = NBShaderInspectorLocalization.GetInspectorOptions("base.forceZWrite", Options);
            InitTriggerByChild();
        }

        public override void OnGUI()
        {
            GuiContent = NBShaderInspectorLocalization.MakeInspectorContent("base.forceZWrite", "Force ZWrite");
            PopUpNames = NBShaderInspectorLocalization.GetInspectorOptions("base.forceZWrite", Options);
            base.OnGUI();
        }

        public override void OnEndChange()
        {
            base.OnEndChange();
            if (RootItem is NBShaderRootItem nbRootItem)
            {
                nbRootItem.SyncService.SyncMaterialState();
            }
        }
    }
}
