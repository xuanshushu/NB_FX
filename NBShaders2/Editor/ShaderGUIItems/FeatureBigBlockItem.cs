namespace NBShaderEditor
{
    public class FeatureBigBlockItem : BigBlockItem
    {
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
            new MaskFeatureItem(rootItem, this);
            new NoiseAndDistortFeatureItem(rootItem, this);
            new EmissionFeatureItem(rootItem, this);
            new ColorBlendFeatureItem(rootItem, this);
            new RampColorFeatureItem(rootItem, this);
            new DissolveFeatureItem(rootItem, this);
            new ProgramNoiseFeatureItem(rootItem, this);
            new SharedUVFeatureItem(rootItem, this);
            new FresnelFeatureItem(rootItem, this);
            new VertexOffsetFeatureItem(rootItem, this);
            new DepthFeatureItem(rootItem, this);
            new ParallaxFeatureItem(rootItem, this);
            new PortalFeatureItem(rootItem, this);
            new FlipbookFeatureItem(rootItem, this);
            new VatFeatureItem(rootItem, this);

            InitTriggerByChild();
        }

        public override void DrawBlock()
        {
            for (int i = 0; i < ChildrenItemList.Count; i++)
            {
                ShaderGUIItem child = ChildrenItemList[i];
                if (IsFeatureItemAllowed(child))
                {
                    child.OnGUI();
                }
            }
        }

        private bool IsFeatureItemAllowed(ShaderGUIItem item)
        {
            string keyword = GetFeatureKeyword(item);
            return string.IsNullOrEmpty(keyword) || ((NBShaderRootItem)RootItem).Context.IsKeywordAllowed(keyword);
        }

        private static string GetFeatureKeyword(ShaderGUIItem item)
        {
            if (item is MaskFeatureItem)
            {
                return "_MASKMAP_ON";
            }

            if (item is NoiseAndDistortFeatureItem)
            {
                return "_NOISEMAP";
            }

            if (item is EmissionFeatureItem)
            {
                return "_EMISSION";
            }

            if (item is ColorBlendFeatureItem)
            {
                return "_COLORMAPBLEND";
            }

            if (item is RampColorFeatureItem)
            {
                return "_COLOR_RAMP";
            }

            if (item is DissolveFeatureItem)
            {
                return "_DISSOLVE";
            }

            if (item is ProgramNoiseFeatureItem)
            {
                return "_PROGRAM_NOISE";
            }

            if (item is SharedUVFeatureItem)
            {
                return "_SHARED_UV";
            }

            if (item is FresnelFeatureItem)
            {
                return "_FRESNEL";
            }

            if (item is ParallaxFeatureItem)
            {
                return "_PARALLAX_MAPPING";
            }

            if (item is VatFeatureItem)
            {
                return "_VAT";
            }

            if (item is FlipbookFeatureItem)
            {
                return "_FLIPBOOKBLENDING_ON";
            }

            if (item is VertexOffsetFeatureItem)
            {
                return "_VERTEX_OFFSET";
            }

            return null;
        }
    }
}
