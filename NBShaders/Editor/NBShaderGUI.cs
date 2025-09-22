using UnityEditor;
using System.Collections.Generic;
using UnityEngine;

namespace NBShaderEditor
{
    public class NBShaderGUI:ShaderGUI
    {
        NBShaderRootItem NBRootItem = new NBShaderRootItem();
   
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            NBRootItem.OnGUI(materialEditor, properties);
            
        }
    }
    
    public class NBShaderRootItem:ShaderGUIRootItem
    {
        public override void InitFlags(List<Material> mats)
        {
            ShaderFlags = new List<ShaderFlagsBase>();
            foreach (Material mat in mats)
            {
                W9ParticleShaderFlags flag = new W9ParticleShaderFlags(mat);
                ShaderFlags.Add(flag);
            }
        }

        public ModeBigBlockItem modeBigBlock;
        public BaseOptionBigBlockItem baseOptionBigBlock;
        public override void OnChildOnGUI()
        {
            if (IsInit)
            {
                modeBigBlock = new ModeBigBlockItem(this,null);
                baseOptionBigBlock = new BaseOptionBigBlockItem(this,null);
                
            }
            modeBigBlock.OnGUI();
            baseOptionBigBlock.OnGUI();
        }
        
    }
}