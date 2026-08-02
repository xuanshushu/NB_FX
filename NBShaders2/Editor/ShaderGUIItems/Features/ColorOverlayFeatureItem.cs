namespace NBShaderEditor
{
    internal abstract class ColorOverlayFeatureItem : FeatureToggleFoldOutItem
    {
        private static readonly string[] BlendModeNames = { "相加", "相乘" };
        private static readonly string[] OnOffNames = { "关闭", "开启" };

        protected ColorOverlayFeatureItem(
            NBShaderRootItem rootItem,
            ShaderGUIItem parentItem,
            string label,
            string foldOutPropertyName,
            string togglePropertyName,
            string keyword,
            string blendModePropertyName,
            int blendModeFlag,
            int blendModeFlagIndex,
            int blendModeFlagEnabledMode,
            string texturePropertyName,
            string colorPropertyName,
            int wrapFlag,
            int forceNoMipFlag,
            string uvFoldOutPropertyName,
            int uvModeFlag,
            int customDataOffsetXFlag,
            int customDataOffsetYFlag,
            NumericBinding rotation,
            string offsetPropertyName,
            NumericBinding distortion,
            string colorIntensityPropertyName,
            string alphaModePropertyName,
            int alphaModeFlag,
            int alphaModeFlagIndex,
            NumericBinding alphaIntensity)
            : base(
                rootItem,
                parentItem,
                foldOutPropertyName,
                togglePropertyName,
                label,
                keyword: keyword)
        {
            string textureLabel = label + " 贴图";
            string uvSourceLabel = label + " UV来源";
            string offsetXLabel = label + " X轴偏移自定义曲线";
            string offsetYLabel = label + " Y轴偏移自定义曲线";
            string rotationLabel = label + " 旋转";
            string offsetSpeedLabel = label + " 偏移速度";
            string distortionLabel = label + " 扭曲强度";
            string colorIntensityLabel = label + " 颜色强度";
            string alphaModeLabel = label + " Alpha作用";
            string alphaIntensityLabel = label + " Alpha强度";

            new FeaturePopupItem(
                rootItem,
                this,
                blendModePropertyName,
                () => Content("叠加贴图混合方式"),
                BlendModeNames,
                property => rootItem.SyncService.ApplyToggleFlag(
                    blendModeFlag,
                    IsModeEnabled(property.floatValue, blendModeFlagEnabledMode),
                    blendModeFlagIndex));

            AddTextureWithWrap(
                rootItem,
                this,
                texturePropertyName,
                textureLabel,
                wrapFlag,
                forceNoMipFlag,
                colorPropertyName);

            new UVModeSelectItem(
                rootItem,
                this,
                uvFoldOutPropertyName,
                uvModeFlag,
                0,
                () => Content(uvSourceLabel),
                texturePropertyName);
            new CustomDataSelectItem(rootItem, this, customDataOffsetXFlag, 3, () => Content(offsetXLabel));
            new CustomDataSelectItem(rootItem, this, customDataOffsetYFlag, 3, () => Content(offsetYLabel));
            AddNumericItem(rootItem, this, rotation, rotationLabel);
            new Vector2LineItem(rootItem, this, offsetPropertyName, true, () => Content(offsetSpeedLabel));

            ShaderGUIItem noiseAffect = new NoiseAffectItem(rootItem, this);
            AddNumericItem(rootItem, noiseAffect, distortion, distortionLabel);
            AddNumericItem(rootItem, this, NumericBinding.Float(colorIntensityPropertyName), colorIntensityLabel);

            new FeaturePopupItem(
                rootItem,
                this,
                alphaModePropertyName,
                () => Content(alphaModeLabel),
                OnOffNames,
                property => rootItem.SyncService.ApplyToggleFlag(
                    alphaModeFlag,
                    property.floatValue > 0.5f,
                    alphaModeFlagIndex));
            AddNumericItem(rootItem, this, alphaIntensity, alphaIntensityLabel);

            InitTriggerByChild();
        }

        private static bool IsModeEnabled(float propertyValue, int enabledMode)
        {
            return enabledMode == 0 ? propertyValue < 0.5f : propertyValue > 0.5f;
        }

        private static void AddNumericItem(
            NBShaderRootItem rootItem,
            ShaderGUIItem parentItem,
            NumericBinding binding,
            string label)
        {
            if (binding.ComponentIndex >= 0)
            {
                new VectorComponentItem(
                    rootItem,
                    parentItem,
                    binding.PropertyName,
                    binding.ComponentIndex,
                    () => Content(label),
                    binding.IsSlider,
                    binding.Min,
                    binding.Max);
                return;
            }

            if (binding.IsSlider)
            {
                ShaderGUISliderItem sliderItem = new ShaderGUISliderItem(rootItem, parentItem)
                {
                    PropertyName = binding.PropertyName,
                    GuiContent = Content(label),
                    Min = binding.Min,
                    Max = binding.Max
                };
                sliderItem.InitTriggerByChild();
                return;
            }

            ShaderGUIFloatItem floatItem = new ShaderGUIFloatItem(rootItem, parentItem)
            {
                PropertyName = binding.PropertyName,
                GuiContent = Content(label)
            };
            floatItem.InitTriggerByChild();
        }

        protected struct NumericBinding
        {
            private NumericBinding(
                string propertyName,
                int componentIndex,
                bool isSlider,
                float min,
                float max)
            {
                PropertyName = propertyName;
                ComponentIndex = componentIndex;
                IsSlider = isSlider;
                Min = min;
                Max = max;
            }

            public readonly string PropertyName;
            public readonly int ComponentIndex;
            public readonly bool IsSlider;
            public readonly float Min;
            public readonly float Max;

            public static NumericBinding Float(string propertyName)
            {
                return new NumericBinding(propertyName, -1, false, 0f, 0f);
            }

            public static NumericBinding Slider(string propertyName, float min, float max)
            {
                return new NumericBinding(propertyName, -1, true, min, max);
            }

            public static NumericBinding VectorSlider(
                string propertyName,
                int componentIndex,
                float min,
                float max)
            {
                return new NumericBinding(propertyName, componentIndex, true, min, max);
            }
        }
    }
}
