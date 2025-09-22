using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
namespace NBShaderEditor
{
    public enum MixedBool
    {
        False = 0,
        True = 1,
        Mixed = -1
    }
    public class ShaderGUIItem
    {
        public const float LabelWidth = 100f;
        public ShaderPropertyInfo PropertyInfo;
        public ShaderGUIItem ParentItem;
        public List<ShaderGUIItem> ChildrenItemList = new List<ShaderGUIItem>();
        public ShaderGUIRootItem RootItem;
        public string PropertyName;
        public GUIContent GuiContent;

        public virtual void InitTriggerByChild()//根Child一定要做这个事情
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
                if (!parentItem.ChildrenItemList.Contains(this))
                {
                    ParentItem.ChildrenItemList.Add(this);
                }
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
            DrawResetButton();
            DrawBlock();
        }

        public void DrawResetButton()//如果重写OnGUI，一定要记得调用DrawResetButton
        {
            _resetButtonContent.text = HasModified ? "R" : "";
            _resetButtonStyle = HasModified ? GUI.skin.button : GUI.skin.label;
            if (GUI.Button(ResetRect, _resetButtonContent, _resetButtonStyle))
            {
                ExecuteReset();
            }
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
        public bool PropertyIsDefaultValue = true;
        public virtual void CheckIsPropertyModified(bool isCallByChild = false)
        {
            if (!isCallByChild)//只有自身Reset了，才需要查询自己的状态是否正确
            {
                bool isDefaultValue = true;
                if (PropertyInfo != null)
                {
                    switch (PropertyInfo.Property.type)
                    {
                        case MaterialProperty.PropType.Float:
                        
                            float defaultValue = RootItem.Shader.GetPropertyDefaultFloatValue(PropertyInfo.Index);
                            isDefaultValue = Mathf.Approximately(PropertyInfo.Property.floatValue, defaultValue);
                        break;
                    }
                }
                else
                {
                    isDefaultValue = true;//如果没有Property，则是看子集的情况
                }

                if (isDefaultValue == PropertyIsDefaultValue)
                {
                    return;//如果状态没有改变，就不需要做任何操作
                }
                else
                {
                    PropertyIsDefaultValue = isDefaultValue;
                }
            }

            HasModified = !PropertyIsDefaultValue;

            foreach (var childItem in ChildrenItemList)
            {
                HasModified |= childItem.HasModified;
            }
            ParentItem?.CheckIsPropertyModified(true);
        }
        
        public virtual void ExecuteReset(bool isCallByParent = false)
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
                PropertyIsDefaultValue = true;
            }
            
            foreach (var childItem in ChildrenItemList)
            {
                childItem.ExecuteReset(true);
            }
            HasModified = false;
            if (!isCallByParent)//直接由用户触发重置
            {
                ParentItem?.CheckIsPropertyModified(true);
            }
        }
        
        #endregion
        
        
    }
}