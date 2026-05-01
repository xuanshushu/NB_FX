using UnityEditor;

namespace NBShaderEditor
{
    public class TABigBlockItem : BigBlockItem
    {
        private readonly HelpBoxItem _placeholder;

        public TABigBlockItem(NBShaderRootItem rootItem, ShaderGUIItem parentItem)
            : base(
                rootItem,
                parentItem,
                "_TABigBlockItemFoldOut",
                () => NBShaderInspectorLocalization.MakeContent(
                    "inspector.block.ta.label",
                    "TA调试",
                    "inspector.block.ta.tip",
                    "技术美术调试和辅助功能"))
        {
            _placeholder = new HelpBoxItem(
                rootItem,
                this,
                () => NBShaderInspectorLocalization.Get(
                    "inspector.placeholder.ta",
                    "TA block is migrating to the new NBShader2 framework."));
            InitTriggerByChild();
        }

        public override void DrawBlock()
        {
            _placeholder.OnGUI();
        }
    }
}
