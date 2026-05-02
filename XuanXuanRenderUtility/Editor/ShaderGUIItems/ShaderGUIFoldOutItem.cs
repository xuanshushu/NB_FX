using UnityEditor;
using UnityEngine;
using UnityEditor.AnimatedValues;

namespace NBShaderEditor
{
    
    public class ShaderGUIFoldOutHelper
    {
	    private const float FoldOutAnimationSpeed = 10f;

	    public ShaderGUIFoldOutHelper(ShaderGUIRootItem rootItem, string foldOutPropertyName)
	    {
		    _rootItem = rootItem;
		    _propertyName = foldOutPropertyName;
		    _propertyInfo = _rootItem.PropertyInfoDic[_propertyName];

		    bool isOpen = _propertyInfo.Property.floatValue > 0.5f;
		    animBool.value = isOpen;
		    animBool.target = isOpen;
		    animBool.valueChanged.AddListener(rootItem.MatEditor.Repaint);
		    animBool.speed = FoldOutAnimationSpeed;
	    }
	    
	    private ShaderGUIRootItem _rootItem;
	    private string _propertyName;
	    private AnimBool animBool = new AnimBool();

	    private ShaderPropertyInfo _propertyInfo;
	    
        public bool BeginFadedGroup(Rect labelRect)
        {
	        DrawFoldOut(labelRect);
	        return BeginFadeGroup();
        }

        public bool BeginFadeGroup()
        {
	        return EditorGUILayout.BeginFadeGroup(animBool.faded);
        }

        public bool DrawFoldOut(Rect labelRect)
        {
	        Rect foldOutRect = MakeFoldOutRect(labelRect);
	        bool isOpen = _propertyInfo.Property.floatValue > 0.5f;
	        isOpen = GUI.Toggle(foldOutRect, isOpen, GUIContent.none, EditorStyles.foldout);
	        SetOpen(isOpen);
	        return isOpen;
        }

        public bool DrawFoldOutLabel(Rect labelRect, GUIContent content, GUIStyle style)
        {
	        Rect foldOutRect = MakeFoldOutRect(labelRect);
	        bool isOpen = _propertyInfo.Property.floatValue > 0.5f;
	        isOpen = GUI.Toggle(foldOutRect, isOpen, content, style ?? EditorStyles.foldout);
	        SetOpen(isOpen);
	        return isOpen;
        }

        private void SetOpen(bool isOpen)
        {
	        animBool.target = isOpen;
	        _propertyInfo.Property.floatValue = isOpen ? 1f : 0f;
        }

        private static Rect MakeFoldOutRect(Rect labelRect)
        {
	        Rect foldOutRect = labelRect;
	        foldOutRect.x = ShaderGUIItem.GetEditorLabelTextX(labelRect) - ShaderGUIItem.FoldOutArrowWidth;
	        foldOutRect.width = Mathf.Max(0f, labelRect.xMax - foldOutRect.x);
	        return foldOutRect;
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
	        bool isOpen = _foldOutHelper.BeginFadedGroup(LabelRect);
	        EditorGUI.indentLevel++;
	        if (isOpen)
	        {
				DrawBlock();
	        }
	        EditorGUILayout.EndFadeGroup();
	        EditorGUI.indentLevel--;
	        Rect rect = ApplyGlobalRectCompensation(EditorGUILayout.GetControlRect(false, 1));
	        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
        }
	}
}
