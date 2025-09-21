using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
namespace NBShaderEditor
{
    public class ShaderGUIItem
    {
        private const float labelWidth = 100f;
        public MaterialProperty Property;
        public ShaderGUIItem ParentItem;
        public List<ShaderGUIItem> ChildrenItemList;
        public Rect rect;
        public ShaderGUIRootItem rootItem;
        public string propertyName;
        public GUIContent guiContent;

        public virtual void Init()
        {
            
        }

        public ShaderGUIItem(ShaderGUIRootItem rtItem,ShaderGUIItem parentItem=null)
        {
            rootItem = rtItem;
            if (parentItem != null)
            {
                parentItem = parentItem;
            }
            Init();
        }

        public void InitProperty()
        {
            if (propertyName != null)
            {
                Property = rootItem.propertyDic[propertyName];
            }
        }

        public Rect baseRect;
        public Rect labelRect;
        public Rect controlRect;
        public Rect resetRect;
        static float resetButtonSize => EditorGUIUtility.singleLineHeight;
        public virtual void GetRect()
        {
            baseRect = EditorGUILayout.GetControlRect();
            labelRect = baseRect;
            labelRect.width = labelWidth;
            // EditorGUI.DrawRect(labelRect,Color.red);
            controlRect = baseRect;
            controlRect.x += labelWidth;
            controlRect.width -= labelWidth;
            controlRect.width -= resetButtonSize;
            // EditorGUI.DrawRect(controlRect,Color.green);
            resetRect = baseRect;
            resetRect.x = baseRect.x + baseRect.width -resetButtonSize;
            resetRect.width = resetButtonSize;
            // EditorGUI.DrawRect(resetRect,Color.blue);
        }

        public virtual void OnGUI()
        {
            if (rootItem.isInit)
            {
                InitProperty();
            }
            GetRect();
        }
    }
}