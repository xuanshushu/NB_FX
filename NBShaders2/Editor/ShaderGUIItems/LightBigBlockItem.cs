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

    public class LightBigBlockItem : NBShaderBlockItem
    {
        private readonly NBShaderPopupPropertyItem _lightModeItem;
        private readonly NBShaderTogglePropertyItem _specularToggleItem;
        private readonly NBShaderColorPropertyItem _specularColorItem;
        private readonly NBShaderVectorComponentPropertyItem _specularSmoothnessItem;
        private readonly NBShaderVectorComponentPropertyItem _pbrMetallicItem;
        private readonly NBShaderVectorComponentPropertyItem _pbrSmoothnessItem;
        private readonly NBShaderTogglePropertyItem _bumpToggleItem;
        private readonly NBShaderTexturePropertyItem _bumpTextureItem;
        private readonly NBShaderSliderPropertyItem _bumpScaleItem;
        private readonly NBShaderTogglePropertyItem _matCapToggleItem;
        private readonly NBShaderTexturePropertyItem _matCapTextureItem;
        private readonly NBShaderVectorComponentPropertyItem _matCapBlendItem;
        private readonly NBShaderTexturePropertyItem _sixWayPositiveItem;
        private readonly NBShaderTexturePropertyItem _sixWayNegativeItem;
        private readonly NBShaderTogglePropertyItem _sixWayAbsorptionToggleItem;
        private readonly NBShaderVectorComponentPropertyItem _sixWayAbsorptionStrengthItem;
        private readonly NBShaderTexturePropertyItem _sixWayEmissionRampItem;
        private readonly NBShaderColorPropertyItem _sixWayEmissionColorItem;

        public LightBigBlockItem(NBShaderRootItem rootItem, ShaderGUIItem parentItem)
            : base(
                rootItem,
                parentItem,
                "_LightBigBlockItemFoldOut",
                "inspector.block.light.label",
                "光照功能",
                "inspector.block.light.tip",
                "法线、MatCap 和光照模式相关功能")
        {
            _lightModeItem = new NBShaderPopupPropertyItem(
                rootItem,
                this,
                "_FxLightMode",
                "inspector.light.mode.label",
                "光照类型",
                new[]
                {
                    "默认无光(Unlit)",
                    "简单光照(BlinnPhong)",
                    "简单光照通透(HalfLambert)",
                    "高级光照(PBR)",
                    "六路光照(SixWay)"
                },
                onValueChanged: () =>
                {
                    rootItem.SyncService.ApplyLightMode((FxLightMode)rootItem.PropertyInfoDic["_FxLightMode"].Property.floatValue);
                    rootItem.Context.Refresh();
                });

            _specularToggleItem = new NBShaderTogglePropertyItem(
                rootItem,
                this,
                "_BlinnPhongSpecularToggle",
                "inspector.light.specular.toggle.label",
                "高光开关",
                enabled => rootItem.SyncService.ApplyToggleKeyword("_SPECULAR_COLOR", enabled),
                isVisible: () => rootItem.Context.FxLightMode == FxLightMode.BlinnPhong || rootItem.Context.FxLightMode == FxLightMode.HalfLambert);

            _specularColorItem = new NBShaderColorPropertyItem(
                rootItem,
                this,
                "_SpecularColor",
                "inspector.light.specular.color.label",
                "高光颜色",
                isVisible: () => IsToggleOn(rootItem, "_BlinnPhongSpecularToggle") && (_IsBlinnOrHalf(rootItem)));

            _specularSmoothnessItem = new NBShaderVectorComponentPropertyItem(
                rootItem,
                this,
                "_MaterialInfo",
                1,
                "inspector.light.specular.smoothness.label",
                "光滑度",
                true,
                0f,
                1f,
                isVisible: () => IsToggleOn(rootItem, "_BlinnPhongSpecularToggle") && (_IsBlinnOrHalf(rootItem)));

            _pbrMetallicItem = new NBShaderVectorComponentPropertyItem(
                rootItem,
                this,
                "_MaterialInfo",
                0,
                "inspector.light.pbr.metallic.label",
                "金属度",
                true,
                0f,
                1f,
                isVisible: () => rootItem.Context.FxLightMode == FxLightMode.PBR);

            _pbrSmoothnessItem = new NBShaderVectorComponentPropertyItem(
                rootItem,
                this,
                "_MaterialInfo",
                1,
                "inspector.light.pbr.smoothness.label",
                "光滑度",
                true,
                0f,
                1f,
                isVisible: () => rootItem.Context.FxLightMode == FxLightMode.PBR);

            _bumpToggleItem = new NBShaderTogglePropertyItem(
                rootItem,
                this,
                "_BumpMapToggle",
                "inspector.light.bump.toggle.label",
                "法线贴图开关",
                enabled => rootItem.SyncService.ApplyToggleKeyword("_NORMALMAP", enabled),
                isVisible: () => rootItem.Context.FxLightMode != FxLightMode.SixWay);

            _bumpTextureItem = new NBShaderTexturePropertyItem(
                rootItem,
                this,
                "_BumpTex",
                "inspector.light.bump.texture.label",
                "法线贴图",
                drawScaleOffset: false,
                isVisible: () => rootItem.Context.FxLightMode != FxLightMode.SixWay && IsToggleOn(rootItem, "_BumpMapToggle"));

            _bumpScaleItem = new NBShaderSliderPropertyItem(
                rootItem,
                this,
                "_BumpScale",
                "inspector.light.bump.scale.label",
                "法线强度",
                0f,
                1f,
                "BumpScaleRangeVec",
                isVisible: () => rootItem.Context.FxLightMode != FxLightMode.SixWay && IsToggleOn(rootItem, "_BumpMapToggle"));

            _matCapToggleItem = new NBShaderTogglePropertyItem(
                rootItem,
                this,
                "_MatCapToggle",
                "inspector.light.matcap.toggle.label",
                "MatCap模拟材质",
                enabled => rootItem.SyncService.ApplyToggleKeyword("_MATCAP", enabled),
                isVisible: () => rootItem.Context.FxLightMode != FxLightMode.SixWay);

            _matCapTextureItem = new NBShaderTexturePropertyItem(
                rootItem,
                this,
                "_MatCapTex",
                "inspector.light.matcap.texture.label",
                "MatCap图",
                "_MatCapColor",
                drawScaleOffset: false,
                isVisible: () => rootItem.Context.FxLightMode != FxLightMode.SixWay && IsToggleOn(rootItem, "_MatCapToggle"));

            _matCapBlendItem = new NBShaderVectorComponentPropertyItem(
                rootItem,
                this,
                "_MatCapInfo",
                0,
                "inspector.light.matcap.blend.label",
                "MatCap相加到相乘过渡",
                true,
                0f,
                1f,
                isVisible: () => rootItem.Context.FxLightMode != FxLightMode.SixWay && IsToggleOn(rootItem, "_MatCapToggle"));

            _sixWayPositiveItem = new NBShaderTexturePropertyItem(
                rootItem,
                this,
                "_RigRTBk",
                "inspector.light.sixway.positive.label",
                "六路正方向图(P)",
                drawScaleOffset: false,
                isVisible: () => rootItem.Context.FxLightMode == FxLightMode.SixWay);

            _sixWayNegativeItem = new NBShaderTexturePropertyItem(
                rootItem,
                this,
                "_RigLBtF",
                "inspector.light.sixway.negative.label",
                "六路反方向图(N)",
                drawScaleOffset: false,
                isVisible: () => rootItem.Context.FxLightMode == FxLightMode.SixWay);

            _sixWayAbsorptionToggleItem = new NBShaderTogglePropertyItem(
                rootItem,
                this,
                "_SixWayColorAbsorptionToggle",
                "inspector.light.sixway.absorption.toggle.label",
                "光照颜色吸收",
                enabled => rootItem.SyncService.ApplyToggleKeyword("VFX_SIX_WAY_ABSORPTION", enabled),
                isVisible: () => rootItem.Context.FxLightMode == FxLightMode.SixWay);

            _sixWayAbsorptionStrengthItem = new NBShaderVectorComponentPropertyItem(
                rootItem,
                this,
                "_SixWayInfo",
                0,
                "inspector.light.sixway.absorption.strength.label",
                "六路吸收强度",
                true,
                0f,
                1f,
                isVisible: () => rootItem.Context.FxLightMode == FxLightMode.SixWay && IsToggleOn(rootItem, "_SixWayColorAbsorptionToggle"));

            _sixWayEmissionRampItem = new NBShaderTexturePropertyItem(
                rootItem,
                this,
                "_SixWayEmissionRamp",
                "inspector.light.sixway.ramp.label",
                "六路自发光Ramp",
                drawScaleOffset: false,
                isVisible: () => rootItem.Context.FxLightMode == FxLightMode.SixWay);

            _sixWayEmissionColorItem = new NBShaderColorPropertyItem(
                rootItem,
                this,
                "_SixWayEmissionColor",
                "inspector.light.sixway.color.label",
                "六路自发光颜色",
                isVisible: () => rootItem.Context.FxLightMode == FxLightMode.SixWay);

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
            _bumpToggleItem.OnGUI();
            _bumpTextureItem.OnGUI();
            _bumpScaleItem.OnGUI();
            _matCapToggleItem.OnGUI();
            _matCapTextureItem.OnGUI();
            _matCapBlendItem.OnGUI();
            _sixWayPositiveItem.OnGUI();
            _sixWayNegativeItem.OnGUI();
            _sixWayAbsorptionToggleItem.OnGUI();
            _sixWayAbsorptionStrengthItem.OnGUI();
            _sixWayEmissionRampItem.OnGUI();
            _sixWayEmissionColorItem.OnGUI();
        }

        private static bool IsToggleOn(NBShaderRootItem rootItem, string propertyName)
        {
            return rootItem.PropertyInfoDic.ContainsKey(propertyName) &&
                   rootItem.PropertyInfoDic[propertyName].Property.floatValue > 0.5f;
        }

        private static bool _IsBlinnOrHalf(NBShaderRootItem rootItem)
        {
            return rootItem.Context.FxLightMode == FxLightMode.BlinnPhong || rootItem.Context.FxLightMode == FxLightMode.HalfLambert;
        }
    }
}
