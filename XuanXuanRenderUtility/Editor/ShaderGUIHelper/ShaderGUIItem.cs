using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
namespace NBShaderEditor
{
    public abstract class ShaderGUIItem
    {
        public MaterialEditor MatEditor;
        public MaterialProperty Property;
        public ShaderGUIItem ParentItem;
        public List<ShaderGUIItem> ChildrenItemList;
        public Material[] Mats;

        public ShaderGUIItem(MaterialEditor editor,MaterialProperty[] props,ShaderGUIItem parentItem,string propertyname = null)
        {
            if (propertyname != null)
            {
                foreach (var prop in props)
                {
                    if (prop.name == propertyname)
                    {
                        Property = prop;
                        return;
                    }
                }
            }

            MatEditor = editor;
            ParentItem = parentItem;
            Mats = MatEditor.targets as Material[];
            
        }

        public virtual void OnGUI()
        {
            
        }
    }
}