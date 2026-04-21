using UnityEditor;

namespace NBShaderEditor
{
    public class TABigBlockItem : NBShaderBlockItem
    {
        private readonly NBShaderHelpBoxItem _placeholder;

        public TABigBlockItem(NBShaderRootItem rootItem, ShaderGUIItem parentItem)
            : base(
                rootItem,
                parentItem,
                "_TABigBlockItemFoldOut",
                "inspector.block.ta.label",
                "TA调试",
                "inspector.block.ta.tip",
                "技术美术调试和辅助功能")
        {
            _placeholder = new NBShaderHelpBoxItem(
                rootItem,
                this,
                "inspector.placeholder.ta",
                "TA block is migrating to the new NBShader2 framework.");
            InitTriggerByChild();
        }

        public override void DrawBlock()
        {
            _placeholder.OnGUI();
        }
    }
}
