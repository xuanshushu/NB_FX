using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
namespace NBShaderEditor
{
    public class ShaderGUIItem
    {
        public MaterialEditor MatEditor;
        public ShaderGUI GUI;
        public MaterialProperty Property;
        public MaterialProperty[] Properties;
        public ShaderGUIItem ParentItem;
        public List<ShaderGUIItem> ChildrenItemList;
        public Material[] Mats;
        public Rect rect;
        public RootItem RootItem;

        public ShaderGUIItem(ShaderGUIItem parentItem=null,string propertyname = null)
        {
            if (parentItem != null)
            {
                Mats = parentItem.Mats;
                MatEditor = parentItem.MatEditor;
                GUI = parentItem.GUI;
                RootItem = parentItem.RootItem;
            }
            
            ParentItem = parentItem;
            Mats = MatEditor.targets as Material[];
            
        }

        public virtual void OnGUI()
        {
            
        }
    }
}