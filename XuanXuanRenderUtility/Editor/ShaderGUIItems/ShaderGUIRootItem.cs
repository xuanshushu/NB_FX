using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using NBShader;

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
        public Color DefaultBackgroundColor;
        public readonly Color AnimatedBackgroundColor = Color.red;
        public List<Renderer> RenderersUsingThisMaterial = new List<Renderer>();
    
        
        public virtual void OnGUI(MaterialEditor editor,MaterialProperty[] props)
        {
            MatEditor = editor;
            if (IsInit)
            {
                DefaultBackgroundColor = GUI.backgroundColor;
                Mats = new List<Material>();
                foreach (var obj in editor.targets)
                {
                    Mats.Add(obj as Material);
                }
                InitFlags(Mats);
                Shader = Mats[0].shader;
                CacheRenderersUsingThisMaterial(Mats[0]);
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

        void CacheRenderersUsingThisMaterial(Material material)
        {
            Renderer[] renderers = UnityEngine.Object.FindObjectsOfType(typeof(Renderer)) as Renderer[];//为了兼容性使用较慢版本
            RenderersUsingThisMaterial.Clear();
            foreach (Renderer renderer in renderers)
            {
                foreach (var mat in renderer.sharedMaterials)
                {
                    if (mat == material)
                    {
                        RenderersUsingThisMaterial.Add(renderer);
                        break;
                    }
                }
            }
        }
        
        public virtual void InitFlags(List<Material> mats) { } //各个子类各自实现 

        public virtual void OnChildOnGUI()
        {
            
        }
    }
}