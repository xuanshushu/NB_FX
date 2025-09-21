using UnityEditor;
using UnityEngine;
using UnityEditor.AnimatedValues;

namespace NBShaderEditor
{
    
    public interface IShaderGUIFoldOut
    {
	    public AnimBool animBool { get; set; }
	    public FontStyle fontStyle { get; set; }
	    MaterialProperty foldOutProperty;
		public string foldOutPropertyName;

		public ShaderGUIFoldOutItem(ShaderGUIRootItem rootItem, ShaderGUIItem parentItem) : base(rootItem, parentItem: parentItem)
		{
		}
        public override void OnGUI()
        {
            base.OnGUI();
            FontStyle origFontStyle = EditorStyles.label.fontStyle;
            EditorStyles.label.fontStyle = fontStyle;
            EditorGUI.LabelField(labelRect,guiContent);
            EditorStyles.label.fontStyle = origFontStyle;
        }
    }

	public class ShaderGUIBigBlockItem:ShaderGUIFoldOutItem
	{
		public ShaderGUIBigBlockItem(ShaderGUIRootItem rootItem, ShaderGUIItem parentItem) :
			base(rootItem, parentItem: parentItem)
		{
			fontStyle = FontStyle.Bold;
		}

        public override void OnGUI()
        {
	        base.OnGUI();
        }
	}
}