using System.Collections.Generic;
using UnityEditor;
namespace NBShaderEditor
{
    public class RootItem:ShaderGUIItem
    {
        public Dictionary<string,MaterialProperty> PropertyDic = new Dictionary<string,MaterialProperty>();
        public RootItem(MaterialEditor editor, MaterialProperty[] props):base()
        {
            
        }

        public void OnGUI(MaterialEditor editor,MaterialProperty[] props)
        {
            PropertyDic.Clear();
            foreach (MaterialProperty prop in props)
            {
                PropertyDic.Add(prop.name, prop);
            }
        }
    }
}