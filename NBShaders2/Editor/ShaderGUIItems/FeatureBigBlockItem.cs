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
                ChildrenItemList[i].OnGUI();
            }
        }
    }
}
