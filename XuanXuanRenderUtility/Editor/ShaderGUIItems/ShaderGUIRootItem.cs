using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace NBShaderEditor
{
    public class ShaderPropertyInfo
    {
        public MaterialProperty Property;
        public string Name;
        public int Index;
    }
    public class ShaderGUIRootItem
    {
        public Shader Shader;
        public MaterialEditor MatEditor;
        public List<Material> Mats;
        public Dictionary<string,ShaderPropertyInfo> PropertyInfoDic = new Dictionary<string,ShaderPropertyInfo>();
        public List<ShaderFlagsBase> ShaderFlags;//各个继承类各自初始化
        public bool IsInit = true;
        public virtual void InitFlags(List<Material> mats) { } //各个子类各自实现 
        
        public virtual void OnGUI(MaterialEditor editor,MaterialProperty[] props)
        {
            MatEditor = editor;
            if (IsInit)
            {
                Mats = new List<Material>();
                foreach (var obj in editor.targets)
                {
                    Mats.Add(obj as Material);
                }
                InitFlags(Mats);
                Shader = Mats[0].shader;
            }
            if (PropertyInfoDic.Count != props.Length)
            {
                PropertyInfoDic.Clear();
                for (int i = 0; i < props.Length; i++)
                {
                    ShaderPropertyInfo propInfo = new ShaderPropertyInfo();
                    propInfo.Property = props[i];
                    propInfo.Name = props[i].name;
                    propInfo.Index = i;
                    PropertyInfoDic.Add(propInfo.Name, propInfo);
                }
            }
            else
            {
                for (int i = 0; i < props.Length; i++)
                {
                    PropertyInfoDic[props[i].name].Property = props[i];
                }
            }
          
            OnChildOnGUI();
            IsInit = false;
        }

        public virtual void OnChildOnGUI()
        {
            
        }
    }
}