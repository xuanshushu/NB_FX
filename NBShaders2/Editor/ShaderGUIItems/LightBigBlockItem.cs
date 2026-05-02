using NBShader;
using UnityEditor;
using UnityEngine;

namespace NBShaderEditor
{
    public enum FxLightMode
    {
        UnLit = 0,
        BlinnPhong = 1,
        HalfLambert = 2,
        PBR = 3,
        SixWay = 4,
        UnKnownOrMixedValue = -1
    }

    public class LightBigBlockItem : BigBlockItem
    {
        private readonly NBShaderRootItem _nbRootItem;
        private readonly FxLightModePopupItem _lightModeItem;
        private readonly ToggleItem _specularToggleItem;
        private readonly ColorItem _specularColorItem;
        private readonly VectorComponentItem _specularSmoothnessItem;
        private readonly VectorComponentItem _pbrMetallicItem;
        private readonly VectorComponentItem _pbrSmoothnessItem;
        private readonly PropertyToggleBlockItem _bumpBlock;
        private readonly PropertyToggleBlockItem _matCapBlock;
        private readonly TextureItem _sixWayPositiveItem;
        private readonly TextureItem _sixWayNegativeItem;
        private readonly ToggleItem _sixWayAbsorptionToggleItem;
        private readonly VectorComponentItem _sixWayAbsorptionStrengthItem;
        private readonly TextureItem _sixWayEmissionRampItem;
        private readonly VectorComponentItem _sixWayEmissionPowItem;
        private readonly ColorItem _sixWayEmissionColorItem;

        public LightBigBlockItem(NBShaderRootItem rootItem, ShaderGUIItem parentItem)
            : base(
                rootItem,
                parentItem,
                "_LightBigBlockItemFoldOut",
                () => Content("block.light", "Light", "Normal, MatCap and light mode controls"))
        {
            _nbRootItem = rootItem;
            _lightModeItem = new FxLightModePopupItem(rootItem, this);

            _specularToggleItem = new ToggleItem(
                rootItem,
                this,
                "_BlinnPhongSpecularToggle",
                () => Content("light.specular.toggle", "Specular"),
                enabled => rootItem.SyncService.ApplyToggleKeyword("_SPECULAR_COLOR", enabled),
                () => IsBlinnOrHalf(rootItem));

            _specularColorItem = new ColorItem(
                rootItem,
                this,
                "_SpecularColor",
                () => Content("light.specular.color", "Specular Color"),
                () => IsToggleOn(rootItem, "_BlinnPhongSpecularToggle") && IsBlinnOrHalf(rootItem));

            _specularSmoothnessItem = new VectorComponentItem(
                rootItem,
                this,
                "_MaterialInfo",
                1,
                () => Content("light.specular.smoothness", "Smoothness"),
                true,
                0f,
                1f,
                () => IsToggleOn(rootItem, "_BlinnPhongSpecularToggle") && IsBlinnOrHalf(rootItem));

            _pbrMetallicItem = new VectorComponentItem(
                rootItem,
                this,
                "_MaterialInfo",
                0,
                () => Content("light.pbr.metallic", "Metallic"),
                true,
                0f,
                1f,
                () => rootItem.Context.FxLightMode == FxLightMode.PBR);

            _pbrSmoothnessItem = new VectorComponentItem(
                rootItem,
                this,
                "_MaterialInfo",
                1,
                () => Content("light.pbr.smoothness", "Smoothness"),
                true,
                0f,
                1f,
                () => rootItem.Context.FxLightMode == FxLightMode.PBR);

            _bumpBlock = new PropertyToggleBlockItem(
                rootItem,
                this,
                "_BumpToggleFoldOut",
                "_BumpMapToggle",
                () => Content("light.bump.toggle", "Normal Map"),
                keyword: "_NORMALMAP",
                isVisible: () => rootItem.Context.FxLightMode != FxLightMode.SixWay);

            new TextureItem(
                rootItem,
                _bumpBlock,
                "_BumpTex",
                () => Content("light.bump.texture", "Normal Map"),
                drawScaleOffset: false);
            new WrapModeItem(rootItem, _bumpBlock, W9ParticleShaderFlags.FLAG_BIT_WRAPMODE_BUMPTEX, () => Content("light.bump.wrap", "Normal Map Wrap"));
            new UVModeSelectItem(
                rootItem,
                _bumpBlock,
                "_BumpUVModeFoldOut",
                W9ParticleShaderFlags.FLAG_BIT_UVMODE_POS_0_BUMPMAP,
                0,
                () => Content("light.bump.uvmode", "Normal Map UV Source"),
                "_BumpTex");
            new ToggleItem(
                rootItem,
                _bumpBlock,
                "_BumpMapMaskMode",
                () => Content("light.bump.maskMode", "Normal Map Multi Channel"),
                enabled => rootItem.SyncService.ApplyToggleFlag(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_NORMALMAP_MASK_MODE, enabled));
            ShaderGUISliderItem bumpScaleItem = new ShaderGUISliderItem(rootItem, _bumpBlock)
            {
                PropertyName = "_BumpScale",
                GuiContent = Content("light.bump.scale", "Normal Strength"),
                RangePropertyName = "BumpScaleRangeVec"
            };
            bumpScaleItem.InitTriggerByChild();

            _matCapBlock = new PropertyToggleBlockItem(
                rootItem,
                this,
                "_MatCapFoldOut",
                "_MatCapToggle",
                () => Content("light.matcap.toggle", "MatCap"),
                keyword: "_MATCAP",
                isVisible: () => rootItem.Context.FxLightMode != FxLightMode.SixWay);
            new TextureItem(rootItem, _matCapBlock, "_MatCapTex", () => Content("light.matcap.texture", "MatCap Texture"), "_MatCapColor", false);
            new VectorComponentItem(rootItem, _matCapBlock, "_MatCapInfo", 0, () => Content("light.matcap.blend", "Add/Multiply Blend"), true, 0f, 1f);

            _sixWayPositiveItem = new TextureItem(
                rootItem,
                this,
                "_RigRTBk",
                () => Content("light.sixway.positive", "SixWay Positive"),
                drawScaleOffset: false,
                isVisible: IsSixWay);

            _sixWayNegativeItem = new TextureItem(
                rootItem,
                this,
                "_RigLBtF",
                () => Content("light.sixway.negative", "SixWay Negative"),
                drawScaleOffset: false,
                isVisible: IsSixWay);

            _sixWayAbsorptionToggleItem = new ToggleItem(
                rootItem,
                this,
                "_SixWayColorAbsorptionToggle",
                () => Content("light.sixway.absorption.toggle", "Light Color Absorption"),
                enabled => rootItem.SyncService.ApplyToggleKeyword("VFX_SIX_WAY_ABSORPTION", enabled),
                IsSixWay);

            _sixWayAbsorptionStrengthItem = new VectorComponentItem(
                rootItem,
                this,
                "_SixWayInfo",
                0,
                () => Content("light.sixway.absorption.strength", "Absorption Strength"),
                true,
                0f,
                1f,
                () => IsSixWay() && IsToggleOn(rootItem, "_SixWayColorAbsorptionToggle"));

            _sixWayEmissionRampItem = new TextureItem(
                rootItem,
                this,
                "_SixWayEmissionRamp",
                () => Content("light.sixway.ramp", "SixWay Emission Ramp"),
                drawScaleOffset: false,
                afterDraw: SyncSixWayRampFlag,
                isVisible: IsSixWay);

            _sixWayEmissionPowItem = new VectorComponentItem(
                rootItem,
                this,
                "_SixWayInfo",
                1,
                () => Content("light.sixway.emissionPow", "SixWay Emission Pow"),
                false,
                isVisible: IsSixWay);

            _sixWayEmissionColorItem = new ColorItem(
                rootItem,
                this,
                "_SixWayEmissionColor",
                () => Content("light.sixway.color", "SixWay Emission Color"),
                IsSixWay);

            InitTriggerByChild();
        }

        public override void DrawBlock()
        {
            _lightModeItem.OnGUI();
            _specularToggleItem.OnGUI();
            _specularColorItem.OnGUI();
            _specularSmoothnessItem.OnGUI();
            _pbrMetallicItem.OnGUI();
            _pbrSmoothnessItem.OnGUI();
            _bumpBlock.OnGUI();
            _matCapBlock.OnGUI();
            _sixWayPositiveItem.OnGUI();
            _sixWayNegativeItem.OnGUI();
            _sixWayAbsorptionToggleItem.OnGUI();
            _sixWayAbsorptionStrengthItem.OnGUI();
            _sixWayEmissionRampItem.OnGUI();
            _sixWayEmissionPowItem.OnGUI();
            _sixWayEmissionColorItem.OnGUI();
        }

        private bool IsSixWay()
        {
            return _nbRootItem.Context.FxLightMode == FxLightMode.SixWay;
        }

        private void SyncSixWayRampFlag(MaterialProperty rampProperty)
        {
            if (rampProperty.hasMixedValue)
            {
                return;
            }

            _nbRootItem.SyncService.ApplyToggleFlag(
                W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_SIXWAY_RAMPMAP,
                rampProperty.textureValue != null,
                1);
        }

        private static bool IsToggleOn(NBShaderRootItem rootItem, string propertyName)
        {
            return rootItem.Context.IsToggleOn(propertyName);
        }

        private static bool IsBlinnOrHalf(NBShaderRootItem rootItem)
        {
            return rootItem.Context.FxLightMode == FxLightMode.BlinnPhong ||
                   rootItem.Context.FxLightMode == FxLightMode.HalfLambert;
        }

        private static GUIContent Content(string key, string fallback, string tip = "")
        {
            return NBShaderInspectorLocalization.MakeContent("inspector." + key + ".label", fallback, "inspector." + key + ".tip", tip);
        }
    }

    public class FxLightModePopupItem : ShaderGUIPopUpItem
    {
        private readonly NBShaderRootItem _nbRootItem;

        public FxLightModePopupItem(NBShaderRootItem rootItem, ShaderGUIItem parentItem) : base(rootItem, parentItem)
        {
            _nbRootItem = rootItem;
            PropertyName = "_FxLightMode";
            GuiContent = NBShaderInspectorLocalization.MakeContent("inspector.light.mode.label", "Light Mode");
            PopUpNames = new[]
            {
                "Unlit",
                "BlinnPhong",
                "HalfLambert",
                "PBR",
                "SixWay"
            };
            InitTriggerByChild();
        }

        public override void OnEndChange()
        {
            base.OnEndChange();
            _nbRootItem.SyncService.ApplyLightMode((FxLightMode)PropertyInfo.Property.floatValue);
            _nbRootItem.Context.Refresh();
        }
    }
}
