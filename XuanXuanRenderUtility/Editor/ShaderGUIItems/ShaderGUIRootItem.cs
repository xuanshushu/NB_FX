using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace NBShaderEditor
{
    public class ShaderGUIRootItem
    {
        public MaterialEditor matEditor;
        public List<Material> mats;
        public Dictionary<string,MaterialProperty> propertyDic = new Dictionary<string,MaterialProperty>();
        public List<ShaderFlagsBase> shaderFlags;//各个继承类各自初始化
        public bool isInit = true;
        public virtual void InitFlags(List<Material> mats) { } //各个子类各自实现 
        
        public virtual void OnGUI(MaterialEditor editor,MaterialProperty[] props)
        {
            propertyDic.Clear();
            foreach (MaterialProperty prop in props)
            {
                propertyDic.Add(prop.name, prop);
            }
            matEditor = editor;

            if (isInit)
            {
                mats = new List<Material>();
                foreach (var obj in editor.targets)
                {
                    mats.Add(obj as Material);
                }
                InitFlags(mats);
            }
            OnChildOnGUI();
            isInit = false;
        }

        public virtual void OnChildOnGUI()
        {
            
        }
    }
}