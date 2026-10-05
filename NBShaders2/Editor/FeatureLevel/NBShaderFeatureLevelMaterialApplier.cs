using System.Collections.Generic;
using NBShader;
using UnityEngine;

namespace NBShaders2.Editor.FeatureLevel
{
    internal static class NBShaderFeatureLevelMaterialApplier
    {
        private const string FeatureTierPropertyName = "_NBShaderFeatureTier";

        // Existing persisted Tier contract: registered float consumers and declared OVZ keyword.
        internal static readonly string[] GraphSupportedGateProperties = NBShaderFeatureRuntime.GraphSupportedGateProperties;
        internal static readonly string[] GraphSupportedGateKeywords = NBShaderFeatureRuntime.GraphSupportedGateKeywords;

        internal static readonly string[] GraphSupportedTypedProjectionProperties = NBShaderFeatureRuntime.GraphSupportedTypedProjectionProperties;
        // One ownership enumeration for preflight, initialization and rollback, also reusable by Runtime.
        internal static readonly string[] GraphSupportedProjectionProperties=NBShaderFeatureRuntime.GraphSupportedProjectionProperties;
        static string[] CreateGraphProjectionProperties()
        {
            return NBShaderFeatureRuntime.CreateGraphProjectionProperties();
        }
        internal static bool HasGraphVATProjectionState(Material material,out bool unprojected)
        {
            return NBShaderFeatureRuntime.HasGraphVATProjectionState(material, out unprojected);
        }

        internal const string GraphOverrideDepthKeyword = "_OVERRIDE_Z";

        internal static bool HasGraphOverrideDepthKeyword(Material material)
        {
            return NBShaderFeatureRuntime.HasGraphOverrideDepthKeyword(material);
        }

        // Missing Tier is the old unfiltered capability, not an invented saved Tier.
        // A present but non-Float/noncanonical Tier grants no write permission.
        internal static bool TryReadGraphOverrideDepthState(Material material,
            out bool effective, out bool allowedByTier)
        {
            effective = allowedByTier = false;
            if (!HasGraphOverrideDepthKeyword(material)) return false;
            IEnumerable<string> allowed = NBShaderFeatureCatalog.RawKeywords;
            if (material.HasProperty(FeatureTierPropertyName))
            {
                if (!NBShaderMaterialIntentResolver.HasFloatShaderProperty(material, FeatureTierPropertyName)) return false;
                float saved = material.GetFloat(FeatureTierPropertyName);
                if (float.IsNaN(saved) || float.IsInfinity(saved) || saved < 0 || saved > 3 || saved != Mathf.Round(saved)) return false;
                allowed = NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForBuildInfoNoSave((NBShaderFeatureTier)(int)saved);
            }
            if (!NBShaderMaterialIntentResolver.TryResolveGraphOverrideDepthIntent(material, allowed, out effective)) return false;
            foreach (string keyword in allowed) if (keyword == GraphOverrideDepthKeyword) { allowedByTier = true; break; }
            return true;
        }

        internal static bool ApplyGraphSavedOverrideDepth(Material material, out bool changed)
        {
            changed = false;
            bool effective, allowed;
            if (!TryReadGraphOverrideDepthState(material, out effective, out allowed)) return false;
            changed = NBShaderFeatureRuntime.SetGraphOwnedKeyword(material, GraphOverrideDepthKeyword, effective);
            return true;
        }

        static bool TryReadGraphSupportedGateTier(Material material, NBShaderFeatureTier tier,
            IEnumerable<string> allowedManagedKeywords, out NBShaderMaterialIntentResult intent)
        {
            return NBShaderFeatureRuntime.TryReadGraphSupportedGateTier(material, tier, allowedManagedKeywords ?? NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForBuildInfoNoSave(tier), out intent);
        }

        internal static bool CanApplyGraphSupportedGateTier(Material material, NBShaderFeatureTier tier,
            IEnumerable<string> allowedManagedKeywords, out bool wouldChange)
        {
            return NBShaderFeatureRuntime.CanApplyGraphSupportedGateTier(material, tier, allowedManagedKeywords ?? NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForBuildInfoNoSave(tier), out wouldChange);
        }

        internal static bool ApplyGraphSupportedGateTier(Material material, NBShaderFeatureTier tier,
            IEnumerable<string> allowedManagedKeywords, out bool changed)
        {
            return NBShaderFeatureRuntime.ApplyGraphSupportedGateTier(material, tier, allowedManagedKeywords ?? NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForBuildInfoNoSave(tier), out changed);
        }

        internal static bool TryReadGraphSavedSupportedGateTier(Material material, out NBShaderMaterialIntentResult intent)
        {
            intent = null;
            if (!NBShaderMaterialIntentResolver.HasFloatShaderProperty(material, FeatureTierPropertyName)) return false;
            float saved = material.GetFloat(FeatureTierPropertyName);
            if (float.IsNaN(saved) || float.IsInfinity(saved) || saved < 0 || saved > 3 || saved != Mathf.Round(saved)) return false;
            return TryReadGraphSupportedGateTier(material, (NBShaderFeatureTier)(int)saved, null, out intent);
        }

        internal static bool CanApplyGraphSavedSupportedGateTier(Material material, out bool wouldChange)
        {
            wouldChange = false;
            if (!NBShaderMaterialIntentResolver.HasFloatShaderProperty(material, FeatureTierPropertyName)) return false;
            float saved = material.GetFloat(FeatureTierPropertyName);
            if (float.IsNaN(saved) || float.IsInfinity(saved) || saved < 0 || saved > 3 || saved != Mathf.Round(saved)) return false;
            return CanApplyGraphSupportedGateTier(material, (NBShaderFeatureTier)(int)saved, null, out wouldChange);
        }

        internal static bool ApplyGraphSavedSupportedGateTier(Material material, out bool changed)
        {
            changed = false;
            if (!NBShaderMaterialIntentResolver.HasFloatShaderProperty(material, FeatureTierPropertyName)) return false;
            float saved = material.GetFloat(FeatureTierPropertyName);
            if (float.IsNaN(saved) || float.IsInfinity(saved) || saved < 0 || saved > 3 || saved != Mathf.Round(saved)) return false;
            return ApplyGraphSupportedGateTier(material, (NBShaderFeatureTier)(int)saved, null, out changed);
        }


        // Explicit ProgramNoise three-feature capability. Same saved intent/dependencies.
        public static bool ApplyGraphProgramNoiseGroup(Material material, NBShaderFeatureTier tier,
            IEnumerable<string> allowedManagedKeywords, out bool changed)
        {
            return NBShaderFeatureRuntime.ApplyGraphProgramNoiseGroup(material, tier, allowedManagedKeywords ?? NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForBuildInfoNoSave(tier), out changed);
        }


        // Explicit Noise pair only; normalized reader owns dependencies.
        public static bool ApplyGraphNoisePair(Material material, NBShaderFeatureTier tier,
            IEnumerable<string> allowedManagedKeywords, out bool changed)
        {
            return NBShaderFeatureRuntime.ApplyGraphNoisePair(material, tier, allowedManagedKeywords ?? NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForBuildInfoNoSave(tier), out changed);
        }


        public static bool ApplyGraphMaskGroup(Material material, NBShaderFeatureTier tier,
            IEnumerable<string> allowedManagedKeywords, out bool changed)
        {
            return NBShaderFeatureRuntime.ApplyGraphMaskGroup(material, tier, allowedManagedKeywords ?? NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForBuildInfoNoSave(tier), out changed);
        }


        internal static bool ApplyGraphOverlayPair(Material material, NBShaderFeatureTier tier,
            IEnumerable<string> allowedManagedKeywords, out bool changed)
        {
            return NBShaderFeatureRuntime.ApplyGraphOverlayPair(material, tier, allowedManagedKeywords ?? NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForBuildInfoNoSave(tier), out changed);
        }


        internal static bool ApplyGraphParallaxGroup(Material material, NBShaderFeatureTier tier,
            IEnumerable<string> allowedManagedKeywords, out bool changed)
        {
            return NBShaderFeatureRuntime.ApplyGraphParallaxGroup(material, tier, allowedManagedKeywords ?? NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForBuildInfoNoSave(tier), out changed);
        }


        internal static bool ApplyGraphNormalMapGroup(Material material, NBShaderFeatureTier tier,
            IEnumerable<string> allowedManagedKeywords, out bool changed)
        {
            return NBShaderFeatureRuntime.ApplyGraphNormalMapGroup(material, tier, allowedManagedKeywords ?? NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForBuildInfoNoSave(tier), out changed);
        }


        internal static bool ApplyGraphRefractionGroup(Material material,NBShaderFeatureTier tier,IEnumerable<string> allowedManagedKeywords,out bool changed)
        {
            return NBShaderFeatureRuntime.ApplyGraphRefractionGroup(material, tier, allowedManagedKeywords ?? NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForBuildInfoNoSave(tier), out changed);
        }

        internal static bool ApplyGraphVATProjectionGroup(Material material,NBShaderFeatureTier tier,
            IEnumerable<string> allowedManagedKeywords,out bool changed)
        {
            return NBShaderFeatureRuntime.ApplyGraphVATProjectionGroup(material, tier, allowedManagedKeywords ?? NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForBuildInfoNoSave(tier), out changed);
        }

        static bool SetGraphAllowFloat(Material material, string name, bool enabled)
        {
            return NBShaderFeatureRuntime.SetGraphAllowFloat(material, name, enabled);
        }



        public static bool Apply(Material material, NBShaderFeatureTier tier, bool writeTierProperty, bool applyResolvedState)
        {
            bool changed;
            return Apply(material, tier, writeTierProperty, applyResolvedState, out changed);
        }

        public static bool Apply(
            Material material,
            NBShaderFeatureTier tier,
            bool writeTierProperty,
            bool applyResolvedState,
            out bool changed)
        {
            changed = false;
            if (!NBShaderMaterialIntentResolver.IsNBShaderMaterial(material))
                return false;

            if (writeTierProperty &&
                material.HasProperty(FeatureTierPropertyName) &&
                !Mathf.Approximately(material.GetFloat(FeatureTierPropertyName), (float)tier))
            {
                material.SetFloat(FeatureTierPropertyName, (float)tier);
                changed = true;
            }

            if (!applyResolvedState)
                return true;

            var allowedKeywords = NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForBuildInfoNoSave(tier);
            var allowedPassFeatures = NBShaderFeatureLevelProjectSettings.instance.GetAllowedPassFeatureSetForBuildInfoNoSave(tier);
            var result = NBShaderMaterialIntentResolver.Resolve(material, tier, allowedKeywords, allowedPassFeatures);
            changed |= ApplyResolvedIntent(material, result);
            return true;
        }

        private static bool ApplyResolvedIntent(Material material, NBShaderMaterialIntentResult result)
        {
            if (material == null || result == null)
                return false;

            var changed = false;
            var effectiveKeywords = new HashSet<string>(result.effectiveKeywords);
            for (int i = 0; i < NBShaderFeatureCatalog.RawKeywords.Length; i++)
            {
                string keyword = NBShaderFeatureCatalog.RawKeywords[i];
                changed |= SetKeyword(material, keyword, effectiveKeywords.Contains(keyword));
            }

            changed |= SetKeyword(material, "EVALUATE_SH_VERTEX", effectiveKeywords.Contains("_FX_LIGHT_MODE_SIX_WAY"));

            for (int i = 0; i < result.passes.Length; i++)
            {
                NBShaderPassIntent pass = result.passes[i];
                if (!string.IsNullOrEmpty(pass.passName) &&
                    material.GetShaderPassEnabled(pass.passName) != pass.included)
                {
                    material.SetShaderPassEnabled(pass.passName, pass.included);
                    changed = true;
                }
            }

            return changed;
        }

        private static bool SetKeyword(Material material, string keyword, bool enabled)
        {
            if (material == null || string.IsNullOrEmpty(keyword) || material.IsKeywordEnabled(keyword) == enabled)
                return false;

            if (enabled)
                material.EnableKeyword(keyword);
            else
                material.DisableKeyword(keyword);

            return true;
        }

        public static bool ApplyGraphFresnelGroup(Material material, NBShaderFeatureTier tier,
            IEnumerable<string> allowedManagedKeywords, out bool changed)
        {
            return NBShaderFeatureRuntime.ApplyGraphFresnelGroup(material, tier, allowedManagedKeywords ?? NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForBuildInfoNoSave(tier), out changed);
        }

        public static bool ApplyGraphDissolveGroup(Material material,NBShaderFeatureTier tier,
            IEnumerable<string> allowedManagedKeywords,out bool changed)
        {
            return NBShaderFeatureRuntime.ApplyGraphDissolveGroup(material, tier, allowedManagedKeywords ?? NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForBuildInfoNoSave(tier), out changed);
        }

        internal static bool ApplyGraphColorRampGroup(Material material,NBShaderFeatureTier tier,
            IEnumerable<string> allowedManagedKeywords,out bool changed)
        {
            return NBShaderFeatureRuntime.ApplyGraphColorRampGroup(material, tier, allowedManagedKeywords ?? NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForBuildInfoNoSave(tier), out changed);
        }

        internal static bool ApplyGraphMatCapGroup(Material material,NBShaderFeatureTier tier,
            IEnumerable<string> allowedManagedKeywords,out bool changed)
        {
            return NBShaderFeatureRuntime.ApplyGraphMatCapGroup(material, tier, allowedManagedKeywords ?? NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForBuildInfoNoSave(tier), out changed);
        }

        internal static bool ApplyGraphDepthFeaturesGroup(Material material,NBShaderFeatureTier tier,
            IEnumerable<string> allowedManagedKeywords,out bool changed)
        {
            return NBShaderFeatureRuntime.ApplyGraphDepthFeaturesGroup(material, tier, allowedManagedKeywords ?? NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForBuildInfoNoSave(tier), out changed);
        }

        internal static bool ApplyGraphVertexOffsetGroup(Material material,NBShaderFeatureTier tier,System.Collections.Generic.IEnumerable<string> allowedManagedKeywords,out bool changed)
        {
            return NBShaderFeatureRuntime.ApplyGraphVertexOffsetGroup(material, tier, allowedManagedKeywords ?? NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForBuildInfoNoSave(tier), out changed);
        }

        internal static bool ApplyGraphDepthDecalGroup(Material material,NBShaderFeatureTier tier,
            IEnumerable<string> allowedManagedKeywords,out bool changed)
        {
            return NBShaderFeatureRuntime.ApplyGraphDepthDecalGroup(material, tier, allowedManagedKeywords ?? NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForBuildInfoNoSave(tier), out changed);
        }

        // Existing declared keywords only. No new mode/flags protocol and no
        // change to unrecognized/raw-new schema until the existing GUI adopts it.
        internal static readonly string[] GraphDeclaredKeywordNames = NBShaderFeatureRuntime.GraphDeclaredKeywordNames;
        static bool GraphDeclaredKeywordWanted(string name,HashSet<string> effective)
        {
            return NBShaderFeatureRuntime.GraphDeclaredKeywordWanted(name, effective);
        }
        static bool CanApplyGraphDeclaredKeywordState(Material material,HashSet<string> effective,out bool wouldChange)
        {
            return NBShaderFeatureRuntime.CanApplyGraphDeclaredKeywordState(material, effective, out wouldChange);
        }
        static bool ApplyGraphDeclaredKeywordState(Material material,HashSet<string> effective,out bool changed)
        {
            return NBShaderFeatureRuntime.ApplyGraphDeclaredKeywordState(material, effective, out changed);
        }
        internal static bool ApplyGraphDeclaredKeywords(Material material,NBShaderFeatureTier tier,
            IEnumerable<string> allowedManagedKeywords,out bool changed)
        {
            return NBShaderFeatureRuntime.ApplyGraphDeclaredKeywords(material, tier, allowedManagedKeywords ?? NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForBuildInfoNoSave(tier), out changed);
        }
        internal static bool ApplyGraphSavedDeclaredKeywords(Material material,out bool changed)
        {
            changed=false;NBShaderMaterialIntentResult intent;
            if(!TryReadGraphSavedSupportedGateTier(material,out intent))return false;
            return ApplyGraphDeclaredKeywordState(material,new HashSet<string>(intent.effectiveKeywords),out changed);
        }
        internal static bool[] CaptureGraphDeclaredKeywordState(Material material)
        {
            return NBShaderFeatureRuntime.CaptureGraphDeclaredKeywordState(material);
        }
        internal static void RestoreGraphDeclaredKeywordState(Material material,bool[] states)
        {
            NBShaderFeatureRuntime.RestoreGraphDeclaredKeywordState(material, states);
        }

        // The normalized resolver already selected exactly one existing mode.
        // A denied selected mode uses the proven mode-0 local shader fallback;
        // the saved _FxLightMode and intended keywords are never rewritten.
        static bool GraphLightingAllowed(HashSet<string> effective)
        {
            return NBShaderFeatureRuntime.GraphLightingAllowed(effective);
        }
        static bool GraphGateEnabled(int index,HashSet<string> effective)
        {
            return NBShaderFeatureRuntime.GraphGateEnabled(index, effective);
        }

        internal static bool ApplyGraphLightingGroup(Material material,NBShaderFeatureTier tier,
            IEnumerable<string> allowedManagedKeywords,out bool changed)
        {
            return NBShaderFeatureRuntime.ApplyGraphLightingGroup(material, tier, allowedManagedKeywords ?? NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForBuildInfoNoSave(tier), out changed);
        }
        internal static bool ApplyGraphSavedLightingGroup(Material material,out bool changed)
        {
            changed=false;NBShaderMaterialIntentResult intent;
            if(!TryReadGraphSavedSupportedGateTier(material,out intent))return false;
            return ApplyGraphLightingGroup(material,intent.tier,null,out changed);
        }
        internal static bool ApplyGraphChromaticGroup(Material material,NBShaderFeatureTier tier,IEnumerable<string> allowedKeywords,out bool changed)
        {
            return NBShaderFeatureRuntime.ApplyGraphChromaticGroup(material, tier, allowedKeywords ?? NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForBuildInfoNoSave(tier), out changed);
        }
        internal static bool ApplyGraphSavedChromaticGroup(Material material,out bool changed)
        {
            changed=false;if(!NBShaderMaterialIntentResolver.HasFloatShaderProperty(material,FeatureTierPropertyName))return false;
            float saved=material.GetFloat(FeatureTierPropertyName);
            if(float.IsNaN(saved)||float.IsInfinity(saved)||saved<0||saved>3||saved!=Mathf.Round(saved))return false;
            return ApplyGraphChromaticGroup(material,(NBShaderFeatureTier)(int)saved,null,out changed);
        }
    }
}
