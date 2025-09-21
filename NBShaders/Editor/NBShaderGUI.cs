using UnityEditor;

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
}