using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NBShaderEditor
{
    public class NBShaderRootItem:ShaderGUIRootItem
    {
        public ShaderGUIBigBlockItem modeBigBlock;
        public override void InitFlags(List<Material> mats)
        {
            ShaderFlags = new List<ShaderFlagsBase>();
            foreach (Material mat in mats)
            {
                W9ParticleShaderFlags flag = new W9ParticleShaderFlags(mat);
                ShaderFlags.Add(flag);
            }
        }

        public override void OnChildOnGUI()
        {
            if (IsInit)
            {
                modeBigBlock = new ModeBigBlockItem(this,null);
            }
            modeBigBlock.OnGUI();
        }
        
    }
   
}