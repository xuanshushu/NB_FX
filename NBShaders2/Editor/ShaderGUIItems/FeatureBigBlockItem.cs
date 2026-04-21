using UnityEditor;

namespace NBShaderEditor
{
    public class FeatureBigBlockItem : NBShaderBlockItem
    {
        private readonly NBShaderHelpBoxItem _placeholder;

        public FeatureBigBlockItem(NBShaderRootItem rootItem, ShaderGUIItem parentItem)
            : base(
                rootItem,
                parentItem,
                "_FeatureBigBlockItemFoldOut",
                "inspector.block.feature.label",
                "特别功能",
                "inspector.block.feature.tip",
                "遮罩、扭曲、溶解等特效功能")
        {
            _placeholder = new NBShaderHelpBoxItem(
                rootItem,
                this,
                "inspector.placeholder.feature",
                "Feature block is migrating to the new NBShader2 framework.");
            InitTriggerByChild();
        }

        public override void DrawBlock()
        {
            _placeholder.OnGUI();
        }
    }
}
