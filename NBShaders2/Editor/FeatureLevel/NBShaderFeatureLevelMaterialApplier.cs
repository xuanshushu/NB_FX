using System.Collections.Generic;
using NBShader;
using UnityEngine;

namespace NBShaders2.Editor.FeatureLevel
{
    internal static class NBShaderFeatureLevelMaterialApplier
    {
        private const string FeatureTierPropertyName = "_NBShaderFeatureTier";

        // Existing persisted Tier contract: registered float consumers and declared OVZ keyword.
        internal static readonly string[] GraphSupportedGateProperties = {
            "_NB_TierAllowMask", "_NB_TierAllowMask2", "_NB_TierAllowMask3",
            "_NB_TierAllowNoise", "_NB_TierAllowNoiseMask",
            "_NB_TierAllowProgramNoise", "_NB_TierAllowProgramSimple", "_NB_TierAllowProgramVoronoi", "_NB_TierAllowFresnel",
            "_NB_TierAllowEmission", "_NB_TierAllowColorBlend", "_NB_TierAllowDissolve", "_NB_TierAllowDissolveMask", "_NB_TierAllowDissolveRamp", "_NB_TierAllowDissolveRampMap",
            "_NB_TierAllowParallax",
            "_NB_TierAllowNormalMap",
            "_NB_TierAllowColorRamp", "_NB_TierAllowColorRampMap",
            "_NB_TierAllowMatCap",
            "_NB_TierAllowDistanceFade", "_NB_TierAllowSoftParticles", "_NB_TierAllowDepthOutline",
            "_NB_TierAllowRefraction", "_NB_TierAllowVertexOffset","_NB_TierAllowVertexOffsetMask",
            "_NB_TierAllowDepthDecal",
            "_NB_TierAllowLighting",
            "_NB_TierAllowVAT", "_NB_TierAllowFlipbook"
        };
        static readonly string[] GraphSupportedGateKeywords = {
            "_MASKMAP_ON", "_MASKMAP2_ON", "_MASKMAP3_ON", "_NOISEMAP", "_NOISE_MASKMAP",
            "_PROGRAM_NOISE", "_PROGRAM_NOISE_SIMPLE", "_PROGRAM_NOISE_VORONOI", "_FRESNEL",
            "_EMISSION", "_COLORMAPBLEND", "_DISSOLVE", "_DISSOLVE_MASK", "_DISSOLVE_RAMP", "_DISSOLVE_RAMP_MAP",
            "_PARALLAX_MAPPING",
            "_NORMALMAP",
            "_COLOR_RAMP", "_COLOR_RAMP_MAP",
            "_MATCAP",
            "_DISTANCE_FADE", "_SOFTPARTICLES_ON", "_DEPTH_OUTLINE",
            "_DISTORT_REFRACTION", "_VERTEX_OFFSET","_VERTEX_OFFSET_MASKMAP",
            "_DEPTH_DECAL",
            null, // Selected lighting mode: use the same normalized effective intent.
            "_VAT", "_FLIPBOOKBLENDING_ON"
        };

        internal static readonly string[] GraphSupportedTypedProjectionProperties={"_NB_TierVATFamily","_NB_TierVATSubMode"};
        // One ownership enumeration for preflight, initialization and rollback, also reusable by Runtime.
        internal static readonly string[] GraphSupportedProjectionProperties=CreateGraphProjectionProperties();
        static string[] CreateGraphProjectionProperties()
        {var values=new string[GraphSupportedGateProperties.Length+GraphSupportedTypedProjectionProperties.Length];GraphSupportedGateProperties.CopyTo(values,0);GraphSupportedTypedProjectionProperties.CopyTo(values,GraphSupportedGateProperties.Length);return values;}
        internal static bool HasGraphVATProjectionState(Material material,out bool unprojected)
        {
            unprojected=false;
            foreach(string name in GraphSupportedTypedProjectionProperties)
            {if(!NBShaderMaterialIntentResolver.HasFloatShaderProperty(material,name))return false;float value=material.GetFloat(name);if(float.IsNaN(value)||float.IsInfinity(value)||value!=Mathf.Round(value))return false;}
            float family=material.GetFloat("_NB_TierVATFamily"),mode=material.GetFloat("_NB_TierVATSubMode");
            if(family==-1f&&mode==-1f){unprojected=true;return true;} // Old unprojected schema only.
            if(family==-2f)return mode==0f;
            return (family==0f&&mode>=0f&&mode<=3f)||(family==1f&&mode>=0f&&mode<=5f);
        }

        internal const string GraphOverrideDepthKeyword = "_OVERRIDE_Z";

        internal static bool HasGraphOverrideDepthKeyword(Material material)
            => material != null && material.shader != null &&
                material.shader.keywordSpace.FindKeyword(GraphOverrideDepthKeyword).isValid;

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
            changed = SetKeyword(material, GraphOverrideDepthKeyword, effective);
            return true;
        }

        static bool TryReadGraphSupportedGateTier(Material material, NBShaderFeatureTier tier,
            IEnumerable<string> allowedManagedKeywords, out NBShaderMaterialIntentResult intent)
        {
            intent = null;
            if ((int)tier < 0 || (int)tier > 3 ||
                !NBShaderMaterialIntentResolver.HasFloatShaderProperty(material, FeatureTierPropertyName)) return false;
            float saved = material.GetFloat(FeatureTierPropertyName);
            if (float.IsNaN(saved) || float.IsInfinity(saved) || saved < 0 || saved > 3 || saved != Mathf.Round(saved)) return false;
            foreach (string property in GraphSupportedProjectionProperties)
            {
                if (!NBShaderMaterialIntentResolver.HasFloatShaderProperty(material, property)) return false;
                float value = material.GetFloat(property);
                if (float.IsNaN(value) || float.IsInfinity(value)) return false;
            }
            bool oldProjection;if(!HasGraphVATProjectionState(material,out oldProjection))return false;
            var allowed = allowedManagedKeywords ?? NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForBuildInfoNoSave(tier);
            string[] unavailable;
            return NBShaderMaterialIntentResolver.TryResolveGraphSupportedKeywordIntent(material, tier, allowed, out intent, out unavailable);
        }

        internal static bool CanApplyGraphSupportedGateTier(Material material, NBShaderFeatureTier tier,
            IEnumerable<string> allowedManagedKeywords, out bool wouldChange)
        {
            wouldChange = false;
            NBShaderMaterialIntentResult intent;
            if (!TryReadGraphSupportedGateTier(material, tier, allowedManagedKeywords, out intent)) return false;
            var effective = new HashSet<string>(intent.effectiveKeywords);
            for (int i = 0; i < GraphSupportedGateProperties.Length; ++i)
                wouldChange |= material.GetFloat(GraphSupportedGateProperties[i]) != (GraphGateEnabled(i,effective) ? 1f : 0f);
            bool parentAllowed,flipbookAllowed;float family,subMode;
            var vatAllowed=allowedManagedKeywords??NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForBuildInfoNoSave(tier);
            if(!NBShaderMaterialIntentResolver.TryResolveGraphVATProjection(material,tier,vatAllowed,out parentAllowed,out family,out subMode,out flipbookAllowed))return false;
            wouldChange|=material.GetFloat("_NB_TierVATFamily")!=family||material.GetFloat("_NB_TierVATSubMode")!=subMode;
            // Older graphs may omit the declared keyword; never invent one.
            if (HasGraphOverrideDepthKeyword(material))
            {
                bool overrideDepth;
                var allowed = allowedManagedKeywords ?? NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForBuildInfoNoSave(tier);
                if (!NBShaderMaterialIntentResolver.TryResolveGraphOverrideDepthIntent(material, allowed, out overrideDepth)) return false;
                wouldChange |= material.IsKeywordEnabled(GraphOverrideDepthKeyword) != overrideDepth;
            }
            bool declaredChange;
            if(!CanApplyGraphDeclaredKeywordState(material,effective,out declaredChange))return false;
            wouldChange|=declaredChange;
            return true;
        }

        internal static bool ApplyGraphSupportedGateTier(Material material, NBShaderFeatureTier tier,
            IEnumerable<string> allowedManagedKeywords, out bool changed)
        {
            changed = false;
            var allowed = new HashSet<string>(allowedManagedKeywords ?? NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForBuildInfoNoSave(tier));
            bool wouldChange;
            if (!CanApplyGraphSupportedGateTier(material, tier, allowed, out wouldChange)) return false;
            if (!wouldChange) return true;
            // All registered groups have been preflighted before the first write.
            bool groupChanged;
            if (!ApplyGraphMaskGroup(material, tier, allowed, out groupChanged)) return false;
            changed |= groupChanged;
            if (!ApplyGraphNoisePair(material, tier, allowed, out groupChanged)) return false;
            changed |= groupChanged;
            if (!ApplyGraphProgramNoiseGroup(material, tier, allowed, out groupChanged)) return false;
            changed |= groupChanged;
            if (!ApplyGraphFresnelGroup(material, tier, allowed, out groupChanged)) return false;
            changed |= groupChanged;
            if(!ApplyGraphDissolveGroup(material,tier,allowed,out groupChanged))return false;
            changed|=groupChanged;
            if (HasGraphOverrideDepthKeyword(material))
            {
                bool effective;
                if (!NBShaderMaterialIntentResolver.TryResolveGraphOverrideDepthIntent(material, allowed, out effective)) return false;
                changed |= SetKeyword(material, GraphOverrideDepthKeyword, effective);
            }
            if (!ApplyGraphOverlayPair(material, tier, allowed, out groupChanged)) return false;
            changed |= groupChanged;
            if (!ApplyGraphParallaxGroup(material, tier, allowed, out groupChanged)) return false;
            changed |= groupChanged;
            if (!ApplyGraphNormalMapGroup(material,tier,allowed,out groupChanged)) return false;
            changed |= groupChanged;
            if(!ApplyGraphColorRampGroup(material,tier,allowed,out groupChanged))return false;
            changed|=groupChanged;
            if(!ApplyGraphDepthFeaturesGroup(material,tier,allowed,out groupChanged))return false;
            changed|=groupChanged;
            if(!ApplyGraphDepthDecalGroup(material,tier,allowed,out groupChanged))return false;
            changed|=groupChanged;
            if(!ApplyGraphVertexOffsetGroup(material,tier,allowed,out groupChanged))return false;changed|=groupChanged;
            if(!ApplyGraphMatCapGroup(material,tier,allowed,out groupChanged))return false;
            changed|=groupChanged;
            if(!ApplyGraphRefractionGroup(material,tier,allowed,out groupChanged))return false;
            changed|=groupChanged;
            if(!ApplyGraphLightingGroup(material,tier,allowed,out groupChanged))return false;
            changed|=groupChanged;
            if(!ApplyGraphVATProjectionGroup(material,tier,allowed,out groupChanged))return false;
            changed|=groupChanged;
            return true;
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
            changed = false;
            foreach (string name in new[] { "_NB_TierAllowProgramNoise", "_NB_TierAllowProgramSimple", "_NB_TierAllowProgramVoronoi" })
                if (!NBShaderMaterialIntentResolver.HasFloatShaderProperty(material, name)) return false;
            var allowed = allowedManagedKeywords ?? NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForBuildInfoNoSave(tier);
            NBShaderMaterialIntentResult intent; string[] unavailable;
            if (!NBShaderMaterialIntentResolver.TryResolveGraphSupportedKeywordIntent(material, tier, allowed, out intent, out unavailable)) return false;
            var effective = new HashSet<string>(intent.effectiveKeywords);
            changed |= SetGraphAllowFloat(material, "_NB_TierAllowProgramNoise", effective.Contains("_PROGRAM_NOISE"));
            changed |= SetGraphAllowFloat(material, "_NB_TierAllowProgramSimple", effective.Contains("_PROGRAM_NOISE_SIMPLE"));
            changed |= SetGraphAllowFloat(material, "_NB_TierAllowProgramVoronoi", effective.Contains("_PROGRAM_NOISE_VORONOI"));
            return true;
        }


        // Explicit Noise pair only; normalized reader owns dependencies.
        public static bool ApplyGraphNoisePair(Material material, NBShaderFeatureTier tier,
            IEnumerable<string> allowedManagedKeywords, out bool changed)
        {
            changed = false;
            if (!NBShaderMaterialIntentResolver.HasFloatShaderProperty(material, "_NB_TierAllowNoise") ||
                !NBShaderMaterialIntentResolver.HasFloatShaderProperty(material, "_NB_TierAllowNoiseMask")) return false;
            var allowed = allowedManagedKeywords ?? NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForBuildInfoNoSave(tier);
            NBShaderMaterialIntentResult result; string[] unavailable;
            if (!NBShaderMaterialIntentResolver.TryResolveGraphSupportedKeywordIntent(material, tier, allowed, out result, out unavailable)) return false;
            var effective = new HashSet<string>(result.effectiveKeywords);
            changed |= SetGraphAllowFloat(material, "_NB_TierAllowNoise", effective.Contains("_NOISEMAP"));
            changed |= SetGraphAllowFloat(material, "_NB_TierAllowNoiseMask", effective.Contains("_NOISE_MASKMAP"));
            return true;
        }


        public static bool ApplyGraphMaskGroup(Material material, NBShaderFeatureTier tier,
            IEnumerable<string> allowedManagedKeywords, out bool changed)
        {
            changed = false;
            foreach (string property in new[] { "_NB_TierAllowMask", "_NB_TierAllowMask2", "_NB_TierAllowMask3" })
                if (!NBShaderMaterialIntentResolver.HasFloatShaderProperty(material, property)) return false;
            var allowed = allowedManagedKeywords ?? NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForBuildInfoNoSave(tier);
            NBShaderMaterialIntentResult result;
            string[] unavailable;
            if (!NBShaderMaterialIntentResolver.TryResolveGraphSupportedKeywordIntent(material, tier, allowed,
                out result, out unavailable)) return false;
            var effective = new HashSet<string>(result.effectiveKeywords);
            changed |= SetGraphAllowFloat(material, "_NB_TierAllowMask", effective.Contains("_MASKMAP_ON"));
            changed |= SetGraphAllowFloat(material, "_NB_TierAllowMask2", effective.Contains("_MASKMAP2_ON"));
            changed |= SetGraphAllowFloat(material, "_NB_TierAllowMask3", effective.Contains("_MASKMAP3_ON"));
            return true;
        }


        internal static bool ApplyGraphOverlayPair(Material material, NBShaderFeatureTier tier,
            IEnumerable<string> allowedManagedKeywords, out bool changed)
        {
            changed = false;
            foreach (string name in new[] { "_NB_TierAllowEmission", "_NB_TierAllowColorBlend" })
                if (!NBShaderMaterialIntentResolver.HasFloatShaderProperty(material, name)) return false;
            var allowed = allowedManagedKeywords ?? NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForBuildInfoNoSave(tier);
            NBShaderMaterialIntentResult intent; string[] unavailable;
            if (!NBShaderMaterialIntentResolver.TryResolveGraphSupportedKeywordIntent(material, tier, allowed, out intent, out unavailable)) return false;
            var effective = new HashSet<string>(intent.effectiveKeywords);
            changed |= SetGraphAllowFloat(material, "_NB_TierAllowEmission", effective.Contains("_EMISSION"));
            changed |= SetGraphAllowFloat(material, "_NB_TierAllowColorBlend", effective.Contains("_COLORMAPBLEND"));
            return true;
        }


        internal static bool ApplyGraphParallaxGroup(Material material, NBShaderFeatureTier tier,
            IEnumerable<string> allowedManagedKeywords, out bool changed)
        {
            changed = false;
            if (!NBShaderMaterialIntentResolver.HasFloatShaderProperty(material, "_NB_TierAllowParallax")) return false;
            var allowed = allowedManagedKeywords ?? NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForBuildInfoNoSave(tier);
            NBShaderMaterialIntentResult intent; string[] unavailable;
            if (!NBShaderMaterialIntentResolver.TryResolveGraphSupportedKeywordIntent(material, tier, allowed, out intent, out unavailable)) return false;
            changed = SetGraphAllowFloat(material, "_NB_TierAllowParallax", new HashSet<string>(intent.effectiveKeywords).Contains("_PARALLAX_MAPPING"));
            return true;
        }


        internal static bool ApplyGraphNormalMapGroup(Material material, NBShaderFeatureTier tier,
            IEnumerable<string> allowedManagedKeywords, out bool changed)
        {
            changed=false;
            if(!NBShaderMaterialIntentResolver.HasFloatShaderProperty(material,"_NB_TierAllowNormalMap"))return false;
            var allowed=allowedManagedKeywords??NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForBuildInfoNoSave(tier);
            NBShaderMaterialIntentResult intent;string[] unavailable;
            if(!NBShaderMaterialIntentResolver.TryResolveGraphSupportedKeywordIntent(material,tier,allowed,out intent,out unavailable))return false;
            changed=SetGraphAllowFloat(material,"_NB_TierAllowNormalMap",new HashSet<string>(intent.effectiveKeywords).Contains("_NORMALMAP"));
            return true;
        }


        internal static bool ApplyGraphRefractionGroup(Material material,NBShaderFeatureTier tier,IEnumerable<string> allowedManagedKeywords,out bool changed)
        {
            changed=false;if(!NBShaderMaterialIntentResolver.HasFloatShaderProperty(material,"_NB_TierAllowRefraction"))return false;
            var allowed=allowedManagedKeywords??NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForBuildInfoNoSave(tier);
            NBShaderMaterialIntentResult intent;string[] unavailable;if(!NBShaderMaterialIntentResolver.TryResolveGraphSupportedKeywordIntent(material,tier,allowed,out intent,out unavailable))return false;
            changed=SetGraphAllowFloat(material,"_NB_TierAllowRefraction",new HashSet<string>(intent.effectiveKeywords).Contains("_DISTORT_REFRACTION"));return true;
        }

        internal static bool ApplyGraphVATProjectionGroup(Material material,NBShaderFeatureTier tier,
            IEnumerable<string> allowedManagedKeywords,out bool changed)
        {
            changed=false;bool oldProjection;if(!HasGraphVATProjectionState(material,out oldProjection))return false;
            foreach(string name in new[]{"_NB_TierAllowVAT","_NB_TierAllowFlipbook"})
            {if(!NBShaderMaterialIntentResolver.HasFloatShaderProperty(material,name))return false;float value=material.GetFloat(name);if(float.IsNaN(value)||float.IsInfinity(value))return false;}
            bool parentAllowed,flipbookAllowed;float family,subMode;
            var vatAllowed=allowedManagedKeywords??NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForBuildInfoNoSave(tier);
            if(!NBShaderMaterialIntentResolver.TryResolveGraphVATProjection(material,tier,vatAllowed,out parentAllowed,out family,out subMode,out flipbookAllowed))return false;
            changed|=SetGraphAllowFloat(material,"_NB_TierAllowVAT",parentAllowed);changed|=SetGraphAllowFloat(material,"_NB_TierAllowFlipbook",flipbookAllowed);
            if(material.GetFloat("_NB_TierVATFamily")!=family){material.SetFloat("_NB_TierVATFamily",family);changed=true;}
            if(material.GetFloat("_NB_TierVATSubMode")!=subMode){material.SetFloat("_NB_TierVATSubMode",subMode);changed=true;}
            return true;
        }

        static bool SetGraphAllowFloat(Material material, string name, bool enabled)
        {
            float value = enabled ? 1f : 0f;
            if (material.GetFloat(name) == value) return false;
            material.SetFloat(name, value);
            return true;
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
            changed = false;
            if (!NBShaderMaterialIntentResolver.HasFloatShaderProperty(material, "_NB_TierAllowFresnel")) return false;
            var allowed = allowedManagedKeywords ?? NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForBuildInfoNoSave(tier);
            NBShaderMaterialIntentResult intent; string[] unavailable;
            if (!NBShaderMaterialIntentResolver.TryResolveGraphSupportedKeywordIntent(material, tier, allowed, out intent, out unavailable)) return false;
            changed = SetGraphAllowFloat(material, "_NB_TierAllowFresnel", new HashSet<string>(intent.effectiveKeywords).Contains("_FRESNEL"));
            return true;
        }

        public static bool ApplyGraphDissolveGroup(Material material,NBShaderFeatureTier tier,
            IEnumerable<string> allowedManagedKeywords,out bool changed)
        {
            changed=false;
            foreach(string name in new[]{"_NB_TierAllowDissolve","_NB_TierAllowDissolveMask","_NB_TierAllowDissolveRamp","_NB_TierAllowDissolveRampMap"})
                if(!NBShaderMaterialIntentResolver.HasFloatShaderProperty(material,name))return false;
            var allowed=allowedManagedKeywords??NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForBuildInfoNoSave(tier);
            NBShaderMaterialIntentResult intent;string[] unavailable;
            if(!NBShaderMaterialIntentResolver.TryResolveGraphSupportedKeywordIntent(material,tier,allowed,out intent,out unavailable))return false;
            var effective=new HashSet<string>(intent.effectiveKeywords);
            changed|=SetGraphAllowFloat(material,"_NB_TierAllowDissolve",effective.Contains("_DISSOLVE"));
            changed|=SetGraphAllowFloat(material,"_NB_TierAllowDissolveMask",effective.Contains("_DISSOLVE_MASK"));
            changed|=SetGraphAllowFloat(material,"_NB_TierAllowDissolveRamp",effective.Contains("_DISSOLVE_RAMP"));
            changed|=SetGraphAllowFloat(material,"_NB_TierAllowDissolveRampMap",effective.Contains("_DISSOLVE_RAMP_MAP"));return true;
        }

        internal static bool ApplyGraphColorRampGroup(Material material,NBShaderFeatureTier tier,
            IEnumerable<string> allowedManagedKeywords,out bool changed)
        {
            changed=false;
            foreach(string name in new[]{"_NB_TierAllowColorRamp","_NB_TierAllowColorRampMap"})
                if(!NBShaderMaterialIntentResolver.HasFloatShaderProperty(material,name))return false;
            var allowed=allowedManagedKeywords??NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForBuildInfoNoSave(tier);
            NBShaderMaterialIntentResult intent;string[] unavailable;
            if(!NBShaderMaterialIntentResolver.TryResolveGraphSupportedKeywordIntent(material,tier,allowed,out intent,out unavailable))return false;
            var effective=new HashSet<string>(intent.effectiveKeywords);
            changed|=SetGraphAllowFloat(material,"_NB_TierAllowColorRamp",effective.Contains("_COLOR_RAMP"));
            changed|=SetGraphAllowFloat(material,"_NB_TierAllowColorRampMap",effective.Contains("_COLOR_RAMP_MAP"));return true;
        }

        internal static bool ApplyGraphMatCapGroup(Material material,NBShaderFeatureTier tier,
            IEnumerable<string> allowedManagedKeywords,out bool changed)
        {
            changed=false;
            if(!NBShaderMaterialIntentResolver.HasFloatShaderProperty(material,"_NB_TierAllowMatCap"))return false;
            var allowed=allowedManagedKeywords??NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForBuildInfoNoSave(tier);
            NBShaderMaterialIntentResult intent;string[] unavailable;
            if(!NBShaderMaterialIntentResolver.TryResolveGraphSupportedKeywordIntent(material,tier,allowed,out intent,out unavailable))return false;
            changed=SetGraphAllowFloat(material,"_NB_TierAllowMatCap",new HashSet<string>(intent.effectiveKeywords).Contains("_MATCAP"));return true;
        }

        internal static bool ApplyGraphDepthFeaturesGroup(Material material,NBShaderFeatureTier tier,
            IEnumerable<string> allowedManagedKeywords,out bool changed)
        {
            changed=false;
            foreach(string name in new[]{"_NB_TierAllowDistanceFade","_NB_TierAllowSoftParticles","_NB_TierAllowDepthOutline"})
                if(!NBShaderMaterialIntentResolver.HasFloatShaderProperty(material,name))return false;
            var allowed=allowedManagedKeywords??NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForBuildInfoNoSave(tier);
            NBShaderMaterialIntentResult intent;string[] unavailable;
            if(!NBShaderMaterialIntentResolver.TryResolveGraphSupportedKeywordIntent(material,tier,allowed,out intent,out unavailable))return false;
            var effective=new HashSet<string>(intent.effectiveKeywords);
            changed|=SetGraphAllowFloat(material,"_NB_TierAllowDistanceFade",effective.Contains("_DISTANCE_FADE"));
            changed|=SetGraphAllowFloat(material,"_NB_TierAllowSoftParticles",effective.Contains("_SOFTPARTICLES_ON"));
            changed|=SetGraphAllowFloat(material,"_NB_TierAllowDepthOutline",effective.Contains("_DEPTH_OUTLINE"));
            return true;
        }

        internal static bool ApplyGraphVertexOffsetGroup(Material material,NBShaderFeatureTier tier,System.Collections.Generic.IEnumerable<string> allowedManagedKeywords,out bool changed)
        {
            changed=false;foreach(string name in new[]{"_NB_TierAllowVertexOffset","_NB_TierAllowVertexOffsetMask"})if(!NBShaderMaterialIntentResolver.HasFloatShaderProperty(material,name))return false;
            var allowed=allowedManagedKeywords??NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForBuildInfoNoSave(tier);NBShaderMaterialIntentResult intent;string[] unavailable;if(!NBShaderMaterialIntentResolver.TryResolveGraphSupportedKeywordIntent(material,tier,allowed,out intent,out unavailable))return false;
            var effective=new System.Collections.Generic.HashSet<string>(intent.effectiveKeywords);changed|=SetGraphAllowFloat(material,"_NB_TierAllowVertexOffset",effective.Contains("_VERTEX_OFFSET"));changed|=SetGraphAllowFloat(material,"_NB_TierAllowVertexOffsetMask",effective.Contains("_VERTEX_OFFSET_MASKMAP"));return true;
        }

        internal static bool ApplyGraphDepthDecalGroup(Material material,NBShaderFeatureTier tier,
            IEnumerable<string> allowedManagedKeywords,out bool changed)
        {
            changed=false;
            if(!NBShaderMaterialIntentResolver.HasFloatShaderProperty(material,"_NB_TierAllowDepthDecal"))return false;
            var allowed=allowedManagedKeywords??NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForBuildInfoNoSave(tier);
            NBShaderMaterialIntentResult intent;string[] unavailable;
            if(!NBShaderMaterialIntentResolver.TryResolveGraphSupportedKeywordIntent(material,tier,allowed,out intent,out unavailable))return false;
            changed=SetGraphAllowFloat(material,"_NB_TierAllowDepthDecal",new HashSet<string>(intent.effectiveKeywords).Contains("_DEPTH_DECAL"));return true;
        }

        // Existing declared keywords only. No new mode/flags protocol and no
        // change to unrecognized/raw-new schema until the existing GUI adopts it.
        internal static readonly string[] GraphDeclaredKeywordNames={
            "_SPECULAR_COLOR","VFX_SIX_WAY_ABSORPTION","EVALUATE_SH_VERTEX",
            "NB_DEBUG_MASK","NB_DEBUG_PNOISE","NB_DEBUG_DISSOLVE","NB_DEBUG_DISTORT","NB_DEBUG_FRESNEL","NB_DEBUG_VERTEX_OFFSET"};
        static bool GraphDeclaredKeywordWanted(string name,HashSet<string> effective)
            => effective.Contains(name=="EVALUATE_SH_VERTEX"?"_FX_LIGHT_MODE_SIX_WAY":name);
        static bool CanApplyGraphDeclaredKeywordState(Material material,HashSet<string> effective,out bool wouldChange)
        {
            wouldChange=false;if(material==null||material.shader==null)return false;
            foreach(string name in GraphDeclaredKeywordNames)
            {
                if(!material.shader.keywordSpace.FindKeyword(name).isValid)return false;
                wouldChange|=material.IsKeywordEnabled(name)!=GraphDeclaredKeywordWanted(name,effective);
            }
            return true;
        }
        static bool ApplyGraphDeclaredKeywordState(Material material,HashSet<string> effective,out bool changed)
        {
            changed=false;bool wouldChange;
            if(!CanApplyGraphDeclaredKeywordState(material,effective,out wouldChange))return false;
            if(!wouldChange)return true;
            foreach(string name in GraphDeclaredKeywordNames)changed|=SetKeyword(material,name,GraphDeclaredKeywordWanted(name,effective));
            return true;
        }
        internal static bool ApplyGraphDeclaredKeywords(Material material,NBShaderFeatureTier tier,
            IEnumerable<string> allowedManagedKeywords,out bool changed)
        {
            changed=false;NBShaderMaterialIntentResult intent;
            if(!TryReadGraphSupportedGateTier(material,tier,allowedManagedKeywords,out intent))return false;
            return ApplyGraphDeclaredKeywordState(material,new HashSet<string>(intent.effectiveKeywords),out changed);
        }
        internal static bool ApplyGraphSavedDeclaredKeywords(Material material,out bool changed)
        {
            changed=false;NBShaderMaterialIntentResult intent;
            if(!TryReadGraphSavedSupportedGateTier(material,out intent))return false;
            return ApplyGraphDeclaredKeywordState(material,new HashSet<string>(intent.effectiveKeywords),out changed);
        }
        internal static bool[] CaptureGraphDeclaredKeywordState(Material material)
        {
            var states=new bool[GraphDeclaredKeywordNames.Length];
            for(int i=0;i<states.Length;++i)states[i]=material.IsKeywordEnabled(GraphDeclaredKeywordNames[i]);return states;
        }
        internal static void RestoreGraphDeclaredKeywordState(Material material,bool[] states)
        {
            if(states==null||states.Length!=GraphDeclaredKeywordNames.Length)return;
            for(int i=0;i<states.Length;++i)SetKeyword(material,GraphDeclaredKeywordNames[i],states[i]);
        }

        // The normalized resolver already selected exactly one existing mode.
        // A denied selected mode uses the proven mode-0 local shader fallback;
        // the saved _FxLightMode and intended keywords are never rewritten.
        static bool GraphLightingAllowed(HashSet<string> effective)
        {
            foreach(string keyword in effective)
                if(keyword.StartsWith("_FX_LIGHT_MODE_",System.StringComparison.Ordinal))return true;
            return false;
        }
        static bool GraphGateEnabled(int index,HashSet<string> effective)
            => GraphSupportedGateKeywords[index]==null ? GraphLightingAllowed(effective) : effective.Contains(GraphSupportedGateKeywords[index]);

        internal static bool ApplyGraphLightingGroup(Material material,NBShaderFeatureTier tier,
            IEnumerable<string> allowedManagedKeywords,out bool changed)
        {
            changed=false;NBShaderMaterialIntentResult intent;
            if(!TryReadGraphSupportedGateTier(material,tier,allowedManagedKeywords,out intent))return false;
            var effective=new HashSet<string>(intent.effectiveKeywords);bool keywordChange;
            if(!CanApplyGraphDeclaredKeywordState(material,effective,out keywordChange))return false;
            float value=GraphLightingAllowed(effective)?1f:0f;
            if(material.GetFloat("_NB_TierAllowLighting")!=value){material.SetFloat("_NB_TierAllowLighting",value);changed=true;}
            bool projected;if(!ApplyGraphDeclaredKeywordState(material,effective,out projected))return false;
            changed|=projected;return true;
        }
        internal static bool ApplyGraphSavedLightingGroup(Material material,out bool changed)
        {
            changed=false;NBShaderMaterialIntentResult intent;
            if(!TryReadGraphSavedSupportedGateTier(material,out intent))return false;
            return ApplyGraphLightingGroup(material,intent.tier,null,out changed);
        }
    }
}
