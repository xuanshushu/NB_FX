using UnityEditor;
using UnityEditor.Rendering.Universal.ShaderGraph;
using UnityEngine;

namespace NBShaderEditor
{
    // Graph materials use the same NB sections as the native inspector.
    // URP remains the authority for material validation and shader assignment.
    public sealed class NBShaderGraphGUI : NBShaderGUI
    {
        readonly NBGraphUnlitGUIBridge _urpGUI = new NBGraphUnlitGUIBridge();

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            // EditorPrefs foldouts can set GUI.changed without a Material edit.
            // Read the existing normalized intent before interactive events;
            // Passive Layout/Repaint/MouseMove must never normalize the saved gate values.
            System.Collections.Generic.Dictionary<Material, NBShader.NBShaderMaterialIntentResult> before = null;
            System.Collections.Generic.Dictionary<Material, NBShader.NBShaderPassIntent> beforeBack = null;
            System.Collections.Generic.Dictionary<Material,float> beforeSurface=null;
            System.Collections.Generic.Dictionary<Material,float> beforeBlend=null;
            bool interactive = Event.current != null && !NBShaderRootItem.IsGraphPassiveGUIEvent(Event.current);
            if (interactive)
            {
                beforeBlend=new System.Collections.Generic.Dictionary<Material,float>();
                beforeSurface=new System.Collections.Generic.Dictionary<Material,float>();
                before = new System.Collections.Generic.Dictionary<Material, NBShader.NBShaderMaterialIntentResult>();
                beforeBack = new System.Collections.Generic.Dictionary<Material, NBShader.NBShaderPassIntent>();
                foreach (UnityEngine.Object target in materialEditor.targets)
                    if (target is Material selected)
                    {
                        if(selected.HasProperty("_Blend"))beforeBlend[selected]=selected.GetFloat("_Blend");
                        if(selected.HasProperty("_TransparentShadowDitherToggle"))beforeSurface[selected]=selected.GetFloat("_Surface");
                        NBShader.NBShaderMaterialIntentResult intent;
                        if (NBShaders2.Editor.FeatureLevel.NBShaderFeatureLevelMaterialApplier.TryReadGraphSavedSupportedGateTier(selected, out intent))
                            before[selected] = intent;
                        NBShader.NBShaderPassIntent backIntent;
                        if (NBShaderSyncService.TryReadGraphSavedOwnedBackFirstPassIntent(selected, out backIntent))
                            beforeBack[selected] = backIntent;
                    }
            }
            EditorGUI.BeginChangeCheck();
            OnGraphGUI(materialEditor, properties);
            bool graphEdited = EditorGUI.EndChangeCheck();
            if (graphEdited && interactive)
                foreach (UnityEngine.Object target in materialEditor.targets)
                    if (target is Material changedMaterial) _urpGUI.ValidateMaterial(changedMaterial);
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
            if(graphEdited&&beforeSurface!=null)
            {
                var selection=new System.Collections.Generic.List<Material>();bool surfaceChanged=false;
                foreach(UnityEngine.Object target in materialEditor.targets)if(target is Material selected)
                {
                    selection.Add(selected);
                    surfaceChanged|=beforeSurface.TryGetValue(selected,out float oldSurface)&&oldSurface!=selected.GetFloat("_Surface");
                }
                bool coverageChanged;
                if(surfaceChanged&&NBShaderSyncService.TryApplyGraphShadowCoverageForSurface(selection,out coverageChanged)&&coverageChanged)
                    foreach(Material selected in selection)EditorUtility.SetDirty(selected);
            }
            if(graphEdited&&beforeBlend!=null)
            {
                var selection=new System.Collections.Generic.List<Material>();foreach(UnityEngine.Object target in materialEditor.targets)if(target is Material material)selection.Add(material);
                bool changed;if(NBShaderSyncService.TryApplyGraphBlendPresetForOfficialEdit(selection,beforeBlend,out changed)&&changed)foreach(Material material in selection)EditorUtility.SetDirty(material);
            }
            // URP's nested GUI owns surface state; this outer GUI owns only
            // the original NB SixWay keywords. Sync immediately on edits.
            bool passiveSyncChanged=false;
            foreach (UnityEngine.Object target in materialEditor.targets)
                if (target is Material material)
                {
                    if(interactive||graphEdited||!GraphPassiveInputsUnchanged)passiveSyncChanged|=SyncSixWayKeywordsWithChange(material);
                    NBShader.NBShaderPassIntent oldBack, newBack;
                    if (graphEdited && beforeBack != null && beforeBack.TryGetValue(material, out oldBack) &&
                        NBShaderSyncService.TryReadGraphSavedOwnedBackFirstPassIntent(material, out newBack) &&
                        (oldBack.enabledByMaterial != newBack.enabledByMaterial || oldBack.included != newBack.included))
                    {
                        bool backChanged;
                        if (NBShaderSyncService.ApplyGraphSavedOwnedBackFirstPassState(material, out backChanged) && backChanged)
                            EditorUtility.SetDirty(material);
                    }
                    if (graphEdited && before != null && intentChanged && allProjectionReady)
                    {
                        bool changed;
                        NBShaders2.Editor.FeatureLevel.NBShaderFeatureLevelMaterialApplier.ApplyGraphSavedSupportedGateTier(material, out changed);
                        if (changed) EditorUtility.SetDirty(material);
                    }
                }
            CompleteGraphGUIInputWitness(passiveSyncChanged);
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
            // Validate owns only the explicitly migrated modern back pass.
            bool backChanged;
            if (NBShaderSyncService.ApplyGraphSavedOwnedBackFirstPassState(material, out backChanged) && backChanged)
                EditorUtility.SetDirty(material);
            // Zero/absent screen migration owns no raw pass; never adopt during Validate.
            if(material.HasProperty(NBShaderEditor.NBShaderSyncService.GraphScreenMigration) && material.GetFloat(NBShaderEditor.NBShaderSyncService.GraphScreenMigration)==1f)
            {
                var tier=(NBShader.NBShaderFeatureTier)(int)material.GetFloat("_NBShaderFeatureTier");bool screenChanged;
                if(NBShaderEditor.NBShaderSyncService.ApplyGraphOwnedScreenPassState(material,tier,
                    NBShaders2.Editor.FeatureLevel.NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForBuildInfoNoSave(tier),
                    NBShaders2.Editor.FeatureLevel.NBShaderFeatureLevelProjectSettings.instance.GetAllowedPassFeatureSetForBuildInfoNoSave(tier),out screenChanged)&&screenChanged)EditorUtility.SetDirty(material);
            }
        }

        internal static void SyncSixWayKeywords(Material material)=>SyncSixWayKeywordsWithChange(material);
        static bool SyncSixWayKeywordsWithChange(Material material)
        {
            if (material == null) return false;
            // Same authority as Tier transactions: final sync cannot reopen a filtered SV_Depth variant.
            bool overrideDepthChanged;
            NBShaders2.Editor.FeatureLevel.NBShaderFeatureLevelMaterialApplier.ApplyGraphSavedOverrideDepth(material, out overrideDepthChanged);
            // Saved Tier authority for the existing declared keywords. This
            // passive final sync never writes derived Float gates or raw intent.
            bool declaredChanged;
            NBShaders2.Editor.FeatureLevel.NBShaderFeatureLevelMaterialApplier.ApplyGraphSavedDeclaredKeywords(material,out declaredChanged);
            return overrideDepthChanged||declaredChanged;
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
