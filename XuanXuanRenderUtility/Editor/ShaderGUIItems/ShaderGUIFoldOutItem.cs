using UnityEditor;
using UnityEngine;
using UnityEditor.AnimatedValues;

namespace NBShaderEditor
{
    
    public class ShaderGUIFoldOutHelper
    {
	    public ShaderGUIFoldOutHelper(ShaderGUIRootItem rootItem, string foldOutPropertyName)
	    {
		    _rootItem = rootItem;
		    _propertyName = foldOutPropertyName;
		    _propertyInfo = _rootItem.PropertyInfoDic[_propertyName];
		    
		    animBool.valueChanged.AddListener(rootItem.MatEditor.Repaint);
		    animBool.speed = 6f;
	    }
	    
	    private ShaderGUIRootItem _rootItem;
	    private string _propertyName;
	    private AnimBool animBool = new AnimBool();

	    private const float foldOutWidth = 15f;
	    private ShaderPropertyInfo _propertyInfo;
	    
        public bool BeginFadedGroup(Rect baseRect)
        {
	        Rect foldOutRect = baseRect;
	        foldOutRect.x = baseRect.x - foldOutWidth;
	        foldOutRect.width = foldOutWidth + ShaderGUIItem.LabelWidth;//覆盖整个Label
	        animBool.target = _propertyInfo.Property.floatValue>0.5f?true:false;
	        animBool.target = EditorGUI.Foldout(foldOutRect,animBool.target,string.Empty,true);
	        _propertyInfo.Property.floatValue = animBool.target?1f:0f;
	        return EditorGUILayout.BeginFadeGroup(animBool.faded);
        }

        public void EndFadedGroup()
        {
	        EditorGUILayout.EndFadeGroup();
        }
    }

	public class ShaderGUIBigBlockItem:ShaderGUIItem
	{
		ShaderGUIFoldOutHelper _foldOutHelper;
		GUIStyle _boldStyle = new GUIStyle(EditorStyles.boldLabel);
		public string FoldOutPropertyName { get; set; }

		public override void InitTriggerByChild()
		{
			_foldOutHelper = new ShaderGUIFoldOutHelper(RootItem, FoldOutPropertyName);
			base.InitTriggerByChild();
		}

		public ShaderGUIBigBlockItem(ShaderGUIRootItem rootItem, ShaderGUIItem parentItem) :
			base(rootItem, parentItem: parentItem)
		{
			
		}
		
		public override void OnGUI()//完全覆写
        {
	        EditorGUILayout.Space();
	        GetRect();
	        EditorGUI.LabelField(LabelRect, GuiContent,_boldStyle);
	        DrawResetButton();
	        EditorGUI.indentLevel++;
	        bool isOpen = _foldOutHelper.BeginFadedGroup(BaseRect);
	        if (isOpen)
	        {
				DrawBlock();
	        }
	        EditorGUILayout.EndFadeGroup();
	        EditorGUI.indentLevel--;
	        Rect rect = EditorGUILayout.GetControlRect(false, 1);
	        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
        }
	}
}