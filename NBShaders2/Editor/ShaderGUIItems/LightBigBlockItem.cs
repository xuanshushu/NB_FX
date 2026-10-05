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
        private readonly ForceNoMipItem _sixWayPositiveForceNoMipItem;
        private readonly TextureItem _sixWayNegativeItem;
        private readonly ForceNoMipItem _sixWayNegativeForceNoMipItem;
        private readonly ToggleItem _sixWayAbsorptionToggleItem;
        private readonly VectorComponentItem _sixWayAbsorptionStrengthItem;
        private readonly TextureItem _sixWayEmissionRampItem;
        private readonly ForceNoMipItem _sixWayEmissionRampForceNoMipItem;
        private readonly VectorComponentItem _sixWayEmissionPowItem;
        private readonly ColorItem _sixWayEmissionColorItem;

        internal static FxLightModePopupItem CreateModeItem(NBShaderRootItem rootItem, ShaderGUIItem parentItem, bool graphSharedMode = false)
            => new FxLightModePopupItem(rootItem, parentItem, graphSharedMode);

        internal static BigBlockItem CreateGraphModeOnlyBlock(NBShaderRootItem rootItem, ShaderGUIItem parentItem)
        {
            var block = new BigBlockItem(rootItem, parentItem, "_LightBigBlockItemFoldOut",
                () => NBShaderInspectorLocalization.MakeInspectorContent("block.light", "Light", "Normal, MatCap and light mode controls"));
            CreateModeItem(rootItem, block, true);
            return block;
        }


        // Both Native and Graph construct the same original Bump subtree.
        // Graph opts into existing scoped writers; Native arguments/visibility stay unchanged.
        internal static PropertyToggleBlockItem CreateNormalMapBlock(NBShaderRootItem rootItem,
            ShaderGUIItem parentItem, bool graphSharedMode = false)
        {
            var block = new PropertyToggleBlockItem(
                rootItem,
                parentItem,
                "_BumpToggleFoldOut",
                "_BumpMapToggle",
                () => Content("light.bump.toggle", "Normal Map"),
                keyword: graphSharedMode ? null : "_NORMALMAP",
                onValueChanged: graphSharedMode ? (System.Action<bool>)(enabled => rootItem.SyncService.TryApplyGraphNormalMapEdit(enabled)) : null,
                isVisible: () => rootItem.Context.FxLightMode != FxLightMode.SixWay);

            new TextureItem(
                rootItem,
                block,
                "_BumpTex",
                () => Content("light.bump.texture", "Normal Map"),
                drawScaleOffset: true);
            TextureRelatedFoldOutItem bumpTexRelatedFoldOut = new TextureRelatedFoldOutItem(
                rootItem,
                block,
                "_BumpTexFoldOut",
                "_BumpTex",
                () => Content("light.bump.related", "Normal Map Related"));
            new WrapModeItem(rootItem, bumpTexRelatedFoldOut, NBShaderFlags.FLAG_BIT_WRAPMODE_BUMPTEX, () => Content("light.bump.wrap", "Normal Map Wrap"));
            new ForceNoMipItem(rootItem, bumpTexRelatedFoldOut, NBShaderFlags.FLAG_BIT_FORCE_NO_MIP_BUMPTEX);
            new UVModeSelectItem(
                rootItem,
                bumpTexRelatedFoldOut,
                "_BumpUVModeFoldOut",
                NBShaderFlags.FLAG_BIT_UVMODE_POS_0_BUMPMAP,
                0,
                () => Content("light.bump.uvmode", "Normal Map UV Source"),
                "_BumpTex", graphFeatureProtocolEdit: graphSharedMode);
            new ToggleItem(
                rootItem,
                bumpTexRelatedFoldOut,
                "_BumpMapMaskMode",
                () => Content("light.bump.maskMode", "Normal Map Multi Channel"),
                enabled => { if(graphSharedMode)rootItem.SyncService.TryApplyGraphNormalMapMaskEdit(enabled);
                    else rootItem.SyncService.ApplyToggleFlag(NBShaderFlags.FLAG_BIT_PARTICLE_NORMALMAP_MASK_MODE, enabled); });
            ShaderGUISliderItem bumpScaleItem = new ShaderGUISliderItem(rootItem, bumpTexRelatedFoldOut)
            {
                PropertyName = "_BumpScale",
                GuiContent = Content("light.bump.scale", "Normal Strength"),
                RangePropertyName = "BumpScaleRangeVec",
                WriteOnlyOnInteractiveChange = graphSharedMode
            };
            bumpScaleItem.InitTriggerByChild();

            return block;
        }

        // Both hosts construct the same original atomic controls. Native defaults stay unchanged.
        internal sealed class SharedLightSubItems
        {
            internal ToggleItem SpecularToggleItem;
            internal ColorItem SpecularColorItem;
            internal VectorComponentItem SpecularSmoothnessItem;
            internal VectorComponentItem PbrMetallicItem;
            internal VectorComponentItem PbrSmoothnessItem;
            internal TextureItem SixWayPositiveItem;
            internal ForceNoMipItem SixWayPositiveForceNoMipItem;
            internal TextureItem SixWayNegativeItem;
            internal ForceNoMipItem SixWayNegativeForceNoMipItem;
            internal ToggleItem SixWayAbsorptionToggleItem;
            internal VectorComponentItem SixWayAbsorptionStrengthItem;
            internal TextureItem SixWayEmissionRampItem;
            internal ForceNoMipItem SixWayEmissionRampForceNoMipItem;
            internal VectorComponentItem SixWayEmissionPowItem;
            internal ColorItem SixWayEmissionColorItem;
        }

        internal static SharedLightSubItems CreateSpecularPBRSubItems(NBShaderRootItem rootItem,
            ShaderGUIItem parentItem, bool graphSharedMode = false)
        {
            var items = new SharedLightSubItems();
            items.SpecularToggleItem = new NBShaderKeywordToggleItem(
                rootItem,
                parentItem,
                "_BlinnPhongSpecularToggle",
                "_SPECULAR_COLOR",
                () => Content("light.specular.toggle", "Specular"),
                isVisible: () => IsBlinnOrHalf(rootItem), graphLightingEdit: graphSharedMode) { WriteOnlyOnInteractiveChange = graphSharedMode };

            items.SpecularColorItem = new ColorItem(
                rootItem,
                parentItem,
                "_SpecularColor",
                () => Content("light.specular.color", "Specular Color"),
                () => IsToggleOn(rootItem, "_BlinnPhongSpecularToggle") && IsBlinnOrHalf(rootItem));

            items.SpecularSmoothnessItem = new VectorComponentItem(
                rootItem,
                parentItem,
                "_MaterialInfo",
                1,
                () => Content("light.specular.smoothness", "Smoothness"),
                true,
                0f,
                1f,
                () => IsToggleOn(rootItem, "_BlinnPhongSpecularToggle") && IsBlinnOrHalf(rootItem));

            items.PbrMetallicItem = new VectorComponentItem(
                rootItem,
                parentItem,
                "_MaterialInfo",
                0,
                () => Content("light.pbr.metallic", "Metallic"),
                true,
                0f,
                1f,
                () => rootItem.Context.FxLightMode == FxLightMode.PBR);

            items.PbrSmoothnessItem = new VectorComponentItem(
                rootItem,
                parentItem,
                "_MaterialInfo",
                1,
                () => Content("light.pbr.smoothness", "Smoothness"),
                true,
                0f,
                1f,
                () => rootItem.Context.FxLightMode == FxLightMode.PBR);

            return items;
        }

        internal static void AddSixWaySubItems(NBShaderRootItem rootItem, ShaderGUIItem parentItem,
            SharedLightSubItems items, System.Action<MaterialProperty> syncSixWayRampFlag = null,
            bool graphSharedMode = false)
        {
            System.Func<bool> isSixWay = () => rootItem.Context.FxLightMode == FxLightMode.SixWay;
            items.SixWayPositiveItem = new TextureItem(
                rootItem,
                parentItem,
                "_RigRTBk",
                () => Content("light.sixway.positive", "SixWay Positive"),
                drawScaleOffset: false,
                isVisible: isSixWay);
            items.SixWayPositiveForceNoMipItem = new ForceNoMipItem(
                rootItem,
                parentItem,
                NBShaderFlags.FLAG_BIT_FORCE_NO_MIP_RIG_RTBK,
                isSixWay);

            items.SixWayNegativeItem = new TextureItem(
                rootItem,
                parentItem,
                "_RigLBtF",
                () => Content("light.sixway.negative", "SixWay Negative"),
                drawScaleOffset: false,
                isVisible: isSixWay);
            items.SixWayNegativeForceNoMipItem = new ForceNoMipItem(
                rootItem,
                parentItem,
                NBShaderFlags.FLAG_BIT_FORCE_NO_MIP_RIG_LBTF,
                isSixWay);

            items.SixWayAbsorptionToggleItem = new NBShaderKeywordToggleItem(
                rootItem,
                parentItem,
                "_SixWayColorAbsorptionToggle",
                "VFX_SIX_WAY_ABSORPTION",
                () => Content("light.sixway.absorption.toggle", "Light Color Absorption"),
                isVisible: isSixWay, graphLightingEdit: graphSharedMode) { WriteOnlyOnInteractiveChange = graphSharedMode };

            items.SixWayAbsorptionStrengthItem = new VectorComponentItem(
                rootItem,
                parentItem,
                "_SixWayInfo",
                0,
                () => Content("light.sixway.absorption.strength", "Absorption Strength"),
                true,
                0f,
                1f,
                () => isSixWay() && IsToggleOn(rootItem, "_SixWayColorAbsorptionToggle"));

            items.SixWayEmissionRampItem = new TextureItem(
                rootItem,
                parentItem,
                "_SixWayEmissionRamp",
                () => Content("light.sixway.ramp", "SixWay Emission Ramp"),
                drawScaleOffset: false,
                afterDraw: syncSixWayRampFlag,
                isVisible: isSixWay);
            items.SixWayEmissionRampForceNoMipItem = new ForceNoMipItem(
                rootItem,
                parentItem,
                NBShaderFlags.FLAG_BIT_FORCE_NO_MIP_SIX_WAY_EMISSION_RAMP,
                isSixWay);

            items.SixWayEmissionPowItem = new VectorComponentItem(
                rootItem,
                parentItem,
                "_SixWayInfo",
                1,
                () => Content("light.sixway.emissionPow", "SixWay Emission Pow"),
                false,
                isVisible: isSixWay);

            items.SixWayEmissionColorItem = new ColorItem(
                rootItem,
                parentItem,
                "_SixWayEmissionColor",
                () => Content("light.sixway.color", "SixWay Emission Color"),
                isSixWay);

        }

        internal static BigBlockItem CreateGraphSharedLightBlock(NBShaderRootItem rootItem, ShaderGUIItem parentItem)
        {
            var block = CreateGraphModeOnlyBlock(rootItem, parentItem);
            var items = CreateSpecularPBRSubItems(rootItem, block, true);
            // Native's afterDraw callback remains Native-only: Graph paint never derives the Ramp bit.
            AddSixWaySubItems(rootItem, block, items, graphSharedMode: true);
            new HelpBoxItem(rootItem, block,
                () => NBShaderInspectorLocalization.GetInspectorText("light.sixway.uvWarning.message", "六路UV跟随主贴图UV及颜色"),
                MessageType.Warning, () => rootItem.Context.FxLightMode == FxLightMode.SixWay);
            return block;
        }

        public LightBigBlockItem(NBShaderRootItem rootItem, ShaderGUIItem parentItem)
            : base(
                rootItem,
                parentItem,
                "_LightBigBlockItemFoldOut",
                () => Content("block.light", "Light", "Normal, MatCap and light mode controls"))
        {
            _nbRootItem = rootItem;
            _lightModeItem = CreateModeItem(rootItem, this);

            var sharedSubItems = CreateSpecularPBRSubItems(rootItem, this);
            _specularToggleItem = sharedSubItems.SpecularToggleItem;
            _specularColorItem = sharedSubItems.SpecularColorItem;
            _specularSmoothnessItem = sharedSubItems.SpecularSmoothnessItem;
            _pbrMetallicItem = sharedSubItems.PbrMetallicItem;
            _pbrSmoothnessItem = sharedSubItems.PbrSmoothnessItem;

            _bumpBlock = CreateNormalMapBlock(rootItem, this);

            _matCapBlock = CreateMatCapBlock(rootItem, this);

            AddSixWaySubItems(rootItem, this, sharedSubItems, SyncSixWayRampFlag);
            _sixWayPositiveItem = sharedSubItems.SixWayPositiveItem;
            _sixWayPositiveForceNoMipItem = sharedSubItems.SixWayPositiveForceNoMipItem;
            _sixWayNegativeItem = sharedSubItems.SixWayNegativeItem;
            _sixWayNegativeForceNoMipItem = sharedSubItems.SixWayNegativeForceNoMipItem;
            _sixWayAbsorptionToggleItem = sharedSubItems.SixWayAbsorptionToggleItem;
            _sixWayAbsorptionStrengthItem = sharedSubItems.SixWayAbsorptionStrengthItem;
            _sixWayEmissionRampItem = sharedSubItems.SixWayEmissionRampItem;
            _sixWayEmissionRampForceNoMipItem = sharedSubItems.SixWayEmissionRampForceNoMipItem;
            _sixWayEmissionPowItem = sharedSubItems.SixWayEmissionPowItem;
            _sixWayEmissionColorItem = sharedSubItems.SixWayEmissionColorItem;

            InitTriggerByChild();
        }

        public override void DrawBlock()
        {
            if (IsAnyLightModeAllowed())
            {
                _lightModeItem.OnGUI();
            }

            if (IsTierAllowed("_SPECULAR_COLOR"))
            {
                _specularToggleItem.OnGUI();
                _specularColorItem.OnGUI();
                _specularSmoothnessItem.OnGUI();
            }

            if (IsTierAllowed("_FX_LIGHT_MODE_PBR"))
            {
                _pbrMetallicItem.OnGUI();
                _pbrSmoothnessItem.OnGUI();
            }

            if (IsTierAllowed("_NORMALMAP"))
            {
                _bumpBlock.OnGUI();
            }

            if (IsTierAllowed("_MATCAP"))
            {
                _matCapBlock.OnGUI();
            }

            if (IsTierAllowed("_FX_LIGHT_MODE_SIX_WAY"))
            {
                _sixWayPositiveItem.OnGUI();
                _sixWayPositiveForceNoMipItem.OnGUI();
                _sixWayNegativeItem.OnGUI();
                _sixWayNegativeForceNoMipItem.OnGUI();
                DrawSixWayWarning();
                _sixWayEmissionRampItem.OnGUI();
                _sixWayEmissionRampForceNoMipItem.OnGUI();
                _sixWayEmissionPowItem.OnGUI();
                _sixWayEmissionColorItem.OnGUI();
            }

            if (IsTierAllowed("VFX_SIX_WAY_ABSORPTION"))
            {
                _sixWayAbsorptionToggleItem.OnGUI();
                _sixWayAbsorptionStrengthItem.OnGUI();
            }
        }

        private bool IsTierAllowed(string keyword)
        {
            return _nbRootItem.Context == null || _nbRootItem.Context.IsKeywordAllowed(keyword);
        }

        private bool IsAnyLightModeAllowed()
        {
            return _nbRootItem.Context == null ||
                   _nbRootItem.Context.IsAnyKeywordAllowed(
                       "_FX_LIGHT_MODE_UNLIT",
                       "_FX_LIGHT_MODE_BLINN_PHONG",
                       "_FX_LIGHT_MODE_HALF_LAMBERT",
                       "_FX_LIGHT_MODE_PBR",
                       "_FX_LIGHT_MODE_SIX_WAY");
        }

        private bool IsSixWay()
        {
            return _nbRootItem.Context.FxLightMode == FxLightMode.SixWay;
        }

        private void DrawSixWayWarning()
        {
            if (!IsSixWay())
            {
                return;
            }

            DrawLayoutHelpBox(
                NBShaderInspectorLocalization.GetInspectorText(
                    "light.sixway.uvWarning.message",
                    "六路UV跟随主贴图UV及颜色"),
                MessageType.Warning);
        }

        private void SyncSixWayRampFlag(MaterialProperty rampProperty)
        {
            if (rampProperty.hasMixedValue)
            {
                return;
            }

            _nbRootItem.SyncService.ApplyToggleFlag(
                NBShaderFlags.FLAG_BIT_PARTICLE_1_SIXWAY_RAMPMAP,
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
            return NBShaderInspectorLocalization.MakeInspectorContent(key, fallback, tip);
        }

        // Both hosts draw the original MatCap subtree. Its sampler has no ST/UV/Wrap controls.
        internal static PropertyToggleBlockItem CreateMatCapBlock(NBShaderRootItem rootItem,
            ShaderGUIItem parentItem, bool graphSharedMode=false)
        {
            var block = new PropertyToggleBlockItem(
                rootItem,
                parentItem,
                "_MatCapFoldOut",
                "_MatCapToggle",
                () => Content("light.matcap.toggle", "MatCap"),
                keyword: graphSharedMode ? null : "_MATCAP",
                onValueChanged: graphSharedMode ? (System.Action<bool>)(enabled => rootItem.SyncService.TryApplyGraphMatCapEdit(enabled)) : null,
                isVisible: () => rootItem.Context.FxLightMode != FxLightMode.SixWay);
            new TextureItem(rootItem, block, "_MatCapTex", () => Content("light.matcap.texture", "MatCap Texture"), "_MatCapColor", false);
            new ForceNoMipItem(rootItem, block, NBShaderFlags.FLAG_BIT_FORCE_NO_MIP_MATCAP);
            new VectorComponentItem(rootItem, block, "_MatCapInfo", 0, () => Content("light.matcap.blend", "Add/Multiply Blend"), true, 0f, 1f);

            return block;
        }
    }

    public class FxLightModePopupItem : ShaderGUIPopUpItem
    {
        private static readonly string[] LightModeOptions =
        {
            "Unlit",
            "BlinnPhong",
            "HalfLambert",
            "PBR",
            "SixWay"
        };

        private readonly NBShaderRootItem _nbRootItem;
        private readonly bool _graphSharedMode;

        public FxLightModePopupItem(NBShaderRootItem rootItem, ShaderGUIItem parentItem, bool graphSharedMode = false) : base(rootItem, parentItem)
        {
            _nbRootItem = rootItem;
            _graphSharedMode = graphSharedMode;
            PropertyName = "_FxLightMode";
            GuiContent = NBShaderInspectorLocalization.MakeContent("inspector.light.mode.label", "Light Mode");
            PopUpNames = NBShaderInspectorLocalization.GetInspectorOptions("light.mode", LightModeOptions);
            InitTriggerByChild();
        }

        public override void OnGUI()
        {
            base.OnGUI();
        }

        internal bool CommitSelectedMode(int value)
        {
            if (value < 0 || value >= LightModeOptions.Length || PropertyInfo == null) return false;
            MaterialProperty property = PropertyInfo.Property;
            if (!property.hasMixedValue && Mathf.Approximately(property.floatValue, value)) return false;
            if (_graphSharedMode)
            {
                if (!_nbRootItem.IsGraphLightModeSchemaReady()) return false;
                _nbRootItem.MatEditor.RegisterPropertyChangeUndo("NB Light Mode");
            }
            property.floatValue = value;
            return true;
        }

        public override void DrawController()
        {
            if (!_graphSharedMode) { base.DrawController(); return; }
            EditorGUI.BeginChangeCheck();
            int selected = EditorGUI.Popup(ControlRect, (int)PropertyInfo.Property.floatValue, PopUpNames);
            if (EditorGUI.EndChangeCheck()) CommitSelectedMode(selected);
        }

        public override void ExecuteReset(bool isCallByParent = false)
        {
            if (_graphSharedMode)
            {
                if (!_nbRootItem.IsGraphLightModeSchemaReady()) return;
                _nbRootItem.MatEditor.RegisterPropertyChangeUndo("Reset NB Light Mode");
            }
            base.ExecuteReset(isCallByParent);
        }

        public override void OnEndChange()
        {
            base.OnEndChange();
            if (_graphSharedMode)
            {
                if (_nbRootItem.IsGraphLightModeSchemaReady())
                    foreach (Material material in _nbRootItem.Mats)
                    {
                        bool changed;
                        NBShaders2.Editor.FeatureLevel.NBShaderFeatureLevelMaterialApplier.ApplyGraphSavedLightingGroup(material,out changed);
                        NBShaderGraphGUI.SyncSixWayKeywords(material);
                    }
            }
            else _nbRootItem.SyncService.ApplyLightMode((FxLightMode)PropertyInfo.Property.floatValue);
            _nbRootItem.Context.Refresh();
        }
    }
}
