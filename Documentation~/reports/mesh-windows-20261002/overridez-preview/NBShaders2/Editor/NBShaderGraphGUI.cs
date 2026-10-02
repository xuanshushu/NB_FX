using UnityEditor;
using UnityEditor.Rendering.Universal.ShaderGraph;
using UnityEngine;

namespace NBShaderEditor
{
    // Mesh Shader Graph entry. URP owns Surface Options/Advanced and their
    // material validation; NBShaderGUI owns only the Graph-specific inputs.
    public sealed class NBShaderGraphGUI : NBShaderGUI
    {
        readonly NBGraphUnlitGUIBridge _urpGUI = new NBGraphUnlitGUIBridge();

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            _urpGUI.OnGUI(materialEditor, properties, OnGraphGUI);
            // URP's nested GUI owns surface state; this outer GUI owns only
            // the original NB SixWay keywords. Sync immediately on edits.
            foreach (UnityEngine.Object target in materialEditor.targets)
                if (target is Material material) SyncSixWayKeywords(material);
        }

        public override void ValidateMaterial(Material material)
        {
            _urpGUI.ValidateMaterial(material);
            SyncSixWayKeywords(material);
        }

        static void SyncSixWayKeywords(Material material)
        {
            if (material == null) return;
            if(material.HasProperty("_OverrideZ_Toggle"))
                SetExistingKeyword(material,"_OVERRIDE_Z",material.GetFloat("_OverrideZ_Toggle")>0.5f);
            bool sixWay = material.HasProperty("_FxLightMode") &&
                Mathf.RoundToInt(material.GetFloat("_FxLightMode")) == 4;
            SetExistingKeyword(material, "EVALUATE_SH_VERTEX", sixWay);
            SetExistingKeyword(material, "VFX_SIX_WAY_ABSORPTION", sixWay &&
                material.HasProperty("_SixWayColorAbsorptionToggle") &&
                material.GetFloat("_SixWayColorAbsorptionToggle") > 0.5f);
        }

        static void SetExistingKeyword(Material material, string keyword, bool enabled)
        {
            if (material.IsKeywordEnabled(keyword) == enabled) return;
            if (enabled) material.EnableKeyword(keyword);
            else material.DisableKeyword(keyword);
        }

        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
            => _urpGUI.AssignNewShaderToMaterial(material, oldShader, newShader);
    }
}
