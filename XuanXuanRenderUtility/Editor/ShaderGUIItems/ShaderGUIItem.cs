using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
namespace NBShaderEditor
{
    public class ShaderGUIItem
    {
        public const float LabelWidth = 100f;
        public ShaderPropertyInfo PropertyInfo;
        public ShaderGUIItem ParentItem;
        public List<ShaderGUIItem> ChildrenItemList = new List<ShaderGUIItem>();
        public ShaderGUIRootItem RootItem;
        public string PropertyName;
        public GUIContent GuiContent;
        public int shaderPropertyIndex;

        public virtual void InitTriggerByChild()
        {
            if ( PropertyInfo == null && PropertyName != null)
            {
                PropertyInfo = RootItem.PropertyInfoDic[PropertyName];
            }
            CheckIsPropertyModified();
        }
        public ShaderGUIItem(ShaderGUIRootItem rtItem,ShaderGUIItem parentItem=null)
        {
            RootItem = rtItem;
            if (parentItem != null)
            {
                ParentItem = parentItem;
            }
        }

        public Rect BaseRect;
        public Rect LabelRect;
        public Rect ControlRect;
        public Rect ResetRect;
        private static float ResetButtonSize => EditorGUIUtility.singleLineHeight;
        public virtual void GetRect()
        {
            BaseRect = EditorGUILayout.GetControlRect();
            LabelRect = BaseRect;
            LabelRect.width = LabelWidth;
            ControlRect = BaseRect;
            ControlRect.x += LabelWidth;
            ControlRect.width -= LabelWidth;
            ControlRect.width -= ResetButtonSize;
            ResetRect = BaseRect;
            ResetRect.x = BaseRect.x + BaseRect.width -ResetButtonSize;
            ResetRect.width = ResetButtonSize;
        }

        
        public virtual void OnGUI()
        {
           
            GetRect();
            EditorGUI.LabelField(LabelRect, GuiContent);
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = PropertyInfo.Property.hasMixedValue;
            DrawController();
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                OnEndChange();
            }
            _resetButtonContent.text = HasModified ? "R" : "";
            _resetButtonStyle = HasModified ? GUI.skin.button : GUI.skin.label;
            if (GUI.Button(ResetRect, _resetButtonContent, _resetButtonStyle))
            {
                ExecuteReset();
            }
            DrawBlock();
        }

        public virtual void DrawController()
        {
            
        }

        public virtual void DrawBlock()
        {
            
        }
        
        public virtual void OnEndChange()
        {
            CheckIsPropertyModified();
        }

        #region  ResetLogic


        private GUIContent _resetButtonContent = new GUIContent("R","重置当前属性及子集属性(如有)");
        private GUIStyle _resetButtonStyle;
        public bool HasModified = false;
        public virtual void CheckIsPropertyModified()
        {
            if (PropertyInfo != null)
            {
                switch (PropertyInfo.Property.type)
                {
                    case MaterialProperty.PropType.Float:
                    
                        float defaultValue = RootItem.Shader.GetPropertyDefaultFloatValue(PropertyInfo.Index);
                        HasModified = !Mathf.Approximately(PropertyInfo.Property.floatValue, defaultValue);
                    break;
                }
            }
            else
            {
                HasModified = false;//如果没有Property，则是看子集的情况
            }

            foreach (var childItem in ChildrenItemList)
            {
                HasModified |= childItem.HasModified;
            }
            ParentItem?.CheckIsPropertyModified();
        }
        
        public virtual void ExecuteReset()
        {
            if (PropertyInfo != null)
            {
                switch (PropertyInfo.Property.type)
                {
                    case MaterialProperty.PropType.Float:
                    
                        float defaultValue = RootItem.Shader.GetPropertyDefaultFloatValue(PropertyInfo.Index);
                        PropertyInfo.Property.floatValue = defaultValue;
                        break;
                }
            }
            
            foreach (var childItem in ChildrenItemList)
            {
                childItem.ExecuteReset();
            }
            CheckIsPropertyModified();
        }
        
        #endregion
        
        
    }
}