using UnityEditor;

namespace NBShaderEditor
{
    public class ShaderGUIPopUpItem:ShaderGUIItem
    {
        public ShaderGUIPopUpItem(ShaderGUIRootItem rootItem, ShaderGUIItem parentItem) :
            base(rootItem, parentItem: parentItem)
        {
            // base.InitTriggerByChild();
        }

        public string[] PopUpNames;

        public override void DrawController()
        {
            PropertyInfo.Property.floatValue = EditorGUI.Popup(ControlRect,(int)PropertyInfo.Property.floatValue,PopUpNames);
        }
    }
}