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
            // EditorPrefs foldouts can set GUI.changed without a Material edit.
            // Read the existing normalized intent before interactive events;
            // Layout/Repaint must never normalize the saved gate values.
            System.Collections.Generic.Dictionary<Material, NBShader.NBShaderMaterialIntentResult> before = null;
            bool interactive = Event.current != null && Event.current.type != EventType.Layout && Event.current.type != EventType.Repaint;
            if (interactive)
            {
                before = new System.Collections.Generic.Dictionary<Material, NBShader.NBShaderMaterialIntentResult>();
                foreach (UnityEngine.Object target in materialEditor.targets)
                    if (target is Material selected)
                    {
                        NBShader.NBShaderMaterialIntentResult intent;
                        if (NBShaders2.Editor.FeatureLevel.NBShaderFeatureLevelMaterialApplier.TryReadGraphSavedSupportedGateTier(selected, out intent))
                            before[selected] = intent;
                    }
            }
            EditorGUI.BeginChangeCheck();
            _urpGUI.OnGUI(materialEditor, properties, OnGraphGUI);
            bool graphEdited = EditorGUI.EndChangeCheck();
            bool allProjectionReady = true;
            bool intentChanged = false;
            if (graphEdited && before != null)
                foreach (UnityEngine.Object target in materialEditor.targets)
                {
                    NBShader.NBShaderMaterialIntentResult oldIntent, newIntent;
                    if (!(target is Material selected) ||
                        !before.TryGetValue(selected, out oldIntent) ||
                        !NBShaders2.Editor.FeatureLevel.NBShaderFeatureLevelMaterialApplier.TryReadGraphSavedSupportedGateTier(selected, out newIntent))
                        allProjectionReady = false;
                    else
                        intentChanged |= oldIntent.tier != newIntent.tier ||
                            !SameIntentKeywords(oldIntent.intendedManagedKeywords, newIntent.intendedManagedKeywords) ||
                            !SameIntentKeywords(oldIntent.effectiveKeywords, newIntent.effectiveKeywords);
                }
            // URP's nested GUI owns surface state; this outer GUI owns only
            // the original NB SixWay keywords. Sync immediately on edits.
            foreach (UnityEngine.Object target in materialEditor.targets)
                if (target is Material material)
                {
                    SyncSixWayKeywords(material);
                    if (graphEdited && before != null && intentChanged && allProjectionReady)
                    {
                        bool changed;
                        NBShaders2.Editor.FeatureLevel.NBShaderFeatureLevelMaterialApplier.ApplyGraphSavedSupportedGateTier(material, out changed);
                        if (changed) EditorUtility.SetDirty(material);
                    }
                }
        }

        static bool SameIntentKeywords(string[] before, string[] after)
        {
            if (before.Length != after.Length) return false;
            for (int i = 0; i < before.Length; ++i) if (before[i] != after[i]) return false;
            return true;
        }

        public override void ValidateMaterial(Material material)
        {
            _urpGUI.ValidateMaterial(material);
            SyncSixWayKeywords(material);
            bool changed;
            NBShaders2.Editor.FeatureLevel.NBShaderFeatureLevelMaterialApplier.ApplyGraphSavedSupportedGateTier(material, out changed);
            if (changed) EditorUtility.SetDirty(material);
        }

        internal static void SyncSixWayKeywords(Material material)
        {
            if (material == null) return;
            if (material.HasProperty("_OverrideZ_Toggle"))
                SetExistingKeyword(material, "_OVERRIDE_Z", material.GetFloat("_OverrideZ_Toggle") > 0.5f);
            SetExistingKeyword(material, "_SPECULAR_COLOR",
                material.HasProperty("_BlinnPhongSpecularToggle") && material.GetFloat("_BlinnPhongSpecularToggle") > 0.5f);
            NBShader.NBShaderMaterialIntentResult debugIntent;
            string[] unavailable;
            if (NBShader.NBShaderMaterialIntentResolver.TryResolveGraphSupportedKeywordIntent(
                material, NBShader.NBShaderFeatureTier.Ultra, NBShader.NBShaderFeatureCatalog.RawKeywords,
                out debugIntent, out unavailable))
            {
                SetExistingKeyword(material, "NB_DEBUG_MASK", System.Array.IndexOf(debugIntent.effectiveKeywords, "NB_DEBUG_MASK") >= 0);
                SetExistingKeyword(material, "NB_DEBUG_PNOISE", System.Array.IndexOf(debugIntent.effectiveKeywords, "NB_DEBUG_PNOISE") >= 0);
                SetExistingKeyword(material, "NB_DEBUG_DISSOLVE", System.Array.IndexOf(debugIntent.effectiveKeywords, "NB_DEBUG_DISSOLVE") >= 0);
                SetExistingKeyword(material, "NB_DEBUG_DISTORT", System.Array.IndexOf(debugIntent.effectiveKeywords, "NB_DEBUG_DISTORT") >= 0);
                SetExistingKeyword(material, "NB_DEBUG_FRESNEL", System.Array.IndexOf(debugIntent.effectiveKeywords, "NB_DEBUG_FRESNEL") >= 0);
                SetExistingKeyword(material, "NB_DEBUG_VERTEX_OFFSET", System.Array.IndexOf(debugIntent.effectiveKeywords, "NB_DEBUG_VERTEX_OFFSET") >= 0);
            }
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
        {
            _urpGUI.AssignNewShaderToMaterial(material, oldShader, newShader);
            // Shader assignment is the existing authorized write transaction.
            NBShaderSyncService.TryInitializeGraphSupportedGateTierOnAssign(material);
            SyncSixWayKeywords(material);
        }
    }
}
