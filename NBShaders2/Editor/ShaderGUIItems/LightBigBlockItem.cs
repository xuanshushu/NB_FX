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
        private readonly ToggleItem _bumpToggleItem;
        private readonly TextureItem _bumpTextureItem;
        private readonly ShaderGUISliderItem _bumpScaleItem;
        private readonly ToggleItem _matCapToggleItem;
        private readonly TextureItem _matCapTextureItem;
        private readonly VectorComponentItem _matCapBlendItem;
        private readonly TextureItem _sixWayPositiveItem;
        private readonly TextureItem _sixWayNegativeItem;
        private readonly ToggleItem _sixWayAbsorptionToggleItem;
        private readonly VectorComponentItem _sixWayAbsorptionStrengthItem;
        private readonly TextureItem _sixWayEmissionRampItem;
        private readonly ColorItem _sixWayEmissionColorItem;

        public LightBigBlockItem(NBShaderRootItem rootItem, ShaderGUIItem parentItem)
            : base(
                rootItem,
                parentItem,
                "_LightBigBlockItemFoldOut",
                () => NBShaderInspectorLocalization.MakeContent(
                    "inspector.block.light.label",
                    "光照功能",
                    "inspector.block.light.tip",
                    "法线、MatCap 和光照模式相关功能"))
        {
            _nbRootItem = rootItem;
            _lightModeItem = new FxLightModePopupItem(rootItem, this);

            _specularToggleItem = new ToggleItem(
                rootItem,
                this,
                "_BlinnPhongSpecularToggle",
                () => NBShaderInspectorLocalization.MakeContent(
                    "inspector.light.specular.toggle.label",
                    "高光开关"),
                enabled => rootItem.SyncService.ApplyToggleKeyword("_SPECULAR_COLOR", enabled),
                () => rootItem.Context.FxLightMode == FxLightMode.BlinnPhong || rootItem.Context.FxLightMode == FxLightMode.HalfLambert);

            _specularColorItem = new ColorItem(
                rootItem,
                this,
                "_SpecularColor",
                () => NBShaderInspectorLocalization.MakeContent(
                    "inspector.light.specular.color.label",
                    "高光颜色"),
                () => IsToggleOn(rootItem, "_BlinnPhongSpecularToggle") && IsBlinnOrHalf(rootItem));

            _specularSmoothnessItem = new VectorComponentItem(
                rootItem,
                this,
                "_MaterialInfo",
                1,
                () => NBShaderInspectorLocalization.MakeContent(
                    "inspector.light.specular.smoothness.label",
                    "光滑度"),
                true,
                0f,
                1f,
                () => IsToggleOn(rootItem, "_BlinnPhongSpecularToggle") && IsBlinnOrHalf(rootItem));

            _pbrMetallicItem = new VectorComponentItem(
                rootItem,
                this,
                "_MaterialInfo",
                0,
                () => NBShaderInspectorLocalization.MakeContent(
                    "inspector.light.pbr.metallic.label",
                    "金属度"),
                true,
                0f,
                1f,
                () => rootItem.Context.FxLightMode == FxLightMode.PBR);

            _pbrSmoothnessItem = new VectorComponentItem(
                rootItem,
                this,
                "_MaterialInfo",
                1,
                () => NBShaderInspectorLocalization.MakeContent(
                    "inspector.light.pbr.smoothness.label",
                    "光滑度"),
                true,
                0f,
                1f,
                () => rootItem.Context.FxLightMode == FxLightMode.PBR);

            _bumpToggleItem = new ToggleItem(
                rootItem,
                this,
                "_BumpMapToggle",
                () => NBShaderInspectorLocalization.MakeContent(
                    "inspector.light.bump.toggle.label",
                    "法线贴图开关"),
                enabled => rootItem.SyncService.ApplyToggleKeyword("_NORMALMAP", enabled),
                () => rootItem.Context.FxLightMode != FxLightMode.SixWay);

            _bumpTextureItem = new TextureItem(
                rootItem,
                this,
                "_BumpTex",
                () => NBShaderInspectorLocalization.MakeContent(
                    "inspector.light.bump.texture.label",
                    "法线贴图"),
                drawScaleOffset: false,
                isVisible: () => rootItem.Context.FxLightMode != FxLightMode.SixWay && IsToggleOn(rootItem, "_BumpMapToggle"));

            _bumpScaleItem = new ShaderGUISliderItem(rootItem, this)
            {
                PropertyName = "_BumpScale",
                GuiContent = NBShaderInspectorLocalization.MakeContent(
                    "inspector.light.bump.scale.label",
                    "法线强度"),
                RangePropertyName = "BumpScaleRangeVec"
            };
            _bumpScaleItem.InitTriggerByChild();

            _matCapToggleItem = new ToggleItem(
                rootItem,
                this,
                "_MatCapToggle",
                () => NBShaderInspectorLocalization.MakeContent(
                    "inspector.light.matcap.toggle.label",
                    "MatCap模拟材质"),
                enabled => rootItem.SyncService.ApplyToggleKeyword("_MATCAP", enabled),
                () => rootItem.Context.FxLightMode != FxLightMode.SixWay);

            _matCapTextureItem = new TextureItem(
                rootItem,
                this,
                "_MatCapTex",
                () => NBShaderInspectorLocalization.MakeContent(
                    "inspector.light.matcap.texture.label",
                    "MatCap图"),
                "_MatCapColor",
                drawScaleOffset: false,
                isVisible: () => rootItem.Context.FxLightMode != FxLightMode.SixWay && IsToggleOn(rootItem, "_MatCapToggle"));

            _matCapBlendItem = new VectorComponentItem(
                rootItem,
                this,
                "_MatCapInfo",
                0,
                () => NBShaderInspectorLocalization.MakeContent(
                    "inspector.light.matcap.blend.label",
                    "MatCap相加到相乘过渡"),
                true,
                0f,
                1f,
                () => rootItem.Context.FxLightMode != FxLightMode.SixWay && IsToggleOn(rootItem, "_MatCapToggle"));

            _sixWayPositiveItem = new TextureItem(
                rootItem,
                this,
                "_RigRTBk",
                () => NBShaderInspectorLocalization.MakeContent(
                    "inspector.light.sixway.positive.label",
                    "六路正方向图(P)"),
                drawScaleOffset: false,
                isVisible: () => rootItem.Context.FxLightMode == FxLightMode.SixWay);

            _sixWayNegativeItem = new TextureItem(
                rootItem,
                this,
                "_RigLBtF",
                () => NBShaderInspectorLocalization.MakeContent(
                    "inspector.light.sixway.negative.label",
                    "六路反方向图(N)"),
                drawScaleOffset: false,
                isVisible: () => rootItem.Context.FxLightMode == FxLightMode.SixWay);

            _sixWayAbsorptionToggleItem = new ToggleItem(
                rootItem,
                this,
                "_SixWayColorAbsorptionToggle",
                () => NBShaderInspectorLocalization.MakeContent(
                    "inspector.light.sixway.absorption.toggle.label",
                    "光照颜色吸收"),
                enabled => rootItem.SyncService.ApplyToggleKeyword("VFX_SIX_WAY_ABSORPTION", enabled),
                () => rootItem.Context.FxLightMode == FxLightMode.SixWay);

            _sixWayAbsorptionStrengthItem = new VectorComponentItem(
                rootItem,
                this,
                "_SixWayInfo",
                0,
                () => NBShaderInspectorLocalization.MakeContent(
                    "inspector.light.sixway.absorption.strength.label",
                    "六路吸收强度"),
                true,
                0f,
                1f,
                () => rootItem.Context.FxLightMode == FxLightMode.SixWay && IsToggleOn(rootItem, "_SixWayColorAbsorptionToggle"));

            _sixWayEmissionRampItem = new TextureItem(
                rootItem,
                this,
                "_SixWayEmissionRamp",
                () => NBShaderInspectorLocalization.MakeContent(
                    "inspector.light.sixway.ramp.label",
                    "六路自发光Ramp"),
                drawScaleOffset: false,
                isVisible: () => rootItem.Context.FxLightMode == FxLightMode.SixWay);

            _sixWayEmissionColorItem = new ColorItem(
                rootItem,
                this,
                "_SixWayEmissionColor",
                () => NBShaderInspectorLocalization.MakeContent(
                    "inspector.light.sixway.color.label",
                    "六路自发光颜色"),
                () => rootItem.Context.FxLightMode == FxLightMode.SixWay);

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
            if (_nbRootItem.Context.FxLightMode != FxLightMode.SixWay && IsToggleOn(_nbRootItem, "_BumpMapToggle"))
            {
                _bumpScaleItem.OnGUI();
            }

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

        private static bool IsBlinnOrHalf(NBShaderRootItem rootItem)
        {
            return rootItem.Context.FxLightMode == FxLightMode.BlinnPhong || rootItem.Context.FxLightMode == FxLightMode.HalfLambert;
        }
    }

    public class FxLightModePopupItem : ShaderGUIPopUpItem
    {
        private readonly NBShaderRootItem _nbRootItem;

        public FxLightModePopupItem(NBShaderRootItem rootItem, ShaderGUIItem parentItem) : base(rootItem, parentItem)
        {
            _nbRootItem = rootItem;
            PropertyName = "_FxLightMode";
            GuiContent = NBShaderInspectorLocalization.MakeContent(
                "inspector.light.mode.label",
                "光照类型");
            PopUpNames = new[]
            {
                "默认无光(Unlit)",
                "简单光照(BlinnPhong)",
                "简单光照过渡(HalfLambert)",
                "高级光照(PBR)",
                "六路光照(SixWay)"
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
