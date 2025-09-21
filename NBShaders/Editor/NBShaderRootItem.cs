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
            shaderFlags = new List<ShaderFlagsBase>();
            foreach (Material mat in mats)
            {
                W9ParticleShaderFlags flag = new W9ParticleShaderFlags(mat);
                shaderFlags.Add(flag);
            }
        }

        public override void OnChildOnGUI()
        {
            if (isInit)
            {
                modeBigBlock = new ShaderGUIBigBlockItem(this,null);
            }
            modeBigBlock.OnGUI();
        }
        
    }
    public class ModeBigBlockItem:ShaderGUIBigBlockItem
    {
        public ModeBigBlockItem(ShaderGUIRootItem rootItem, ShaderGUIItem parentItem) :
            base(rootItem, parentItem: parentItem)
        {
            guiContent = new GUIContent("模式设置", "根据Mesh来源指定相应模式");
        }
    }
    
    
}