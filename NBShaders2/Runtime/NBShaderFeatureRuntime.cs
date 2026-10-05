using System.Collections.Generic;
using UnityEngine;

namespace NBShader
{
    /// <summary>
    /// Runtime API for applying NBShader feature tiers to Native and complete ordinary Mesh NB Graph materials.
    /// </summary>
    public static class NBShaderFeatureRuntime
    {
        /// <summary>
        /// Applies an NBShader feature tier to one material in place. Native "Effects/NBShader" and complete ordinary Mesh NB Graph materials are processed. Managed Catalog keywords and shader passes are derived from
        /// serialized material intent, then filtered by the target tier.
        /// </summary>
        /// <param name="material">Material to process. Null materials are ignored.</param>
        /// <param name="tier">
        /// Target tier. When null, Ultra is used. This overload does not load runtime settings, so it keeps all
        /// managed Catalog keywords and pass features available.
        /// </param>
        /// <remarks>
        /// If lower-tier shader variants have been stripped from the build, call this API before the material is
        /// used for rendering. Otherwise Unity can request a missing variant and select a similar available variant.
        /// </remarks>
        public static void ApplyTier(Material material, NBShaderFeatureTier? tier = null)
        {
            ApplyTierInternal(material, null, tier);
        }

        /// <summary>
        /// Applies an NBShader feature tier using a user-loaded runtime settings asset.
        /// Pass null for <paramref name="tier"/> to resolve the tier from the settings quality mapping.
        /// </summary>
        public static void ApplyTier(Material material, NBShaderFeatureRuntimeSettings settings, NBShaderFeatureTier? tier)
        {
            ApplyTierInternal(material, settings, tier);
        }

        /// <summary>
        /// Applies an NBShader feature tier to multiple materials in place. Native "Effects/NBShader" and complete ordinary Mesh NB Graph materials are processed. Catalog-external keywords are left unchanged unless they are
        /// explicitly tied to a managed NBShader feature.
        /// </summary>
        /// <param name="materials">Materials to process. Null collections and null entries are ignored.</param>
        /// <param name="tier">
        /// Target tier. When null, Ultra is used. This overload does not load runtime settings, so it keeps all
        /// managed Catalog keywords and pass features available.
        /// </param>
        /// <remarks>
        /// If lower-tier shader variants have been stripped from the build, call this API before the materials are
        /// used for rendering. Otherwise Unity can request a missing variant and select a similar available variant.
        /// </remarks>
        public static void ApplyTier(IEnumerable<Material> materials, NBShaderFeatureTier? tier = null)
        {
            ApplyTierInternal(materials, null, tier);
        }

        /// <summary>
        /// Applies an NBShader feature tier to multiple materials using a user-loaded runtime settings asset.
        /// Pass null for <paramref name="tier"/> to resolve the tier from the settings quality mapping.
        /// </summary>
        public static void ApplyTier(IEnumerable<Material> materials, NBShaderFeatureRuntimeSettings settings, NBShaderFeatureTier? tier)
        {
            ApplyTierInternal(materials, settings, tier);
        }

        private static void ApplyTierInternal(IEnumerable<Material> materials, NBShaderFeatureRuntimeSettings settings, NBShaderFeatureTier? tier)
        {
            if (materials == null)
            {
                return;
            }

            List<Material> graph=null;
            foreach (Material material in materials)
            {
                if(IsGraphProjectionCandidate(material))
                {if(graph==null)graph=new List<Material>();graph.Add(material);}
                else ApplyTierInternal(material, settings, tier);
            }
            // Native remains its original per-item application; only the Graph subset is atomic.
            if(graph!=null){bool ignored;TryApplyGraphRuntimePolicy(graph,settings,tier,out ignored);}
        }

        private static void ApplyTierInternal(Material material, NBShaderFeatureRuntimeSettings settings, NBShaderFeatureTier? tier)
        {
            if(IsGraphProjectionCandidate(material))
            {
                bool ignored;TryApplyGraphRuntimePolicy(new[]{material},settings,tier,out ignored);return;
            }

            if (!IsNBShaderMaterial(material))
            {
                return;
            }

            NBShaderFeatureTier resolvedTier = tier.HasValue ? tier.Value : ResolveTierFromQuality(settings);
            HashSet<string> allowed = settings != null
                ? settings.BuildAllowedSet(resolvedTier)
                : BuildAllowAllCatalogKeywordSet();
            HashSet<string> allowedPassFeatures = settings != null
                ? settings.BuildAllowedPassFeatureSet(resolvedTier)
                : null;
            NBShaderMaterialIntentResult result = NBShaderMaterialIntentResolver.Resolve(material, resolvedTier, allowed, allowedPassFeatures);
            ApplyResolvedIntent(material, result);
        }

        // Complete Graph owner distinguishes candidate identity from schema validity.
        // Keep both global Native classifiers and the existing editor asset scan unchanged.
        private static bool IsGraphProjectionCandidate(Material material)
            => material != null && material.shader != null && !IsNBShaderMaterial(material) && material.HasProperty("_NB_DistortionMode");

        private static bool TryApplyGraphRuntimePolicy(IEnumerable<Material> materials,
            NBShaderFeatureRuntimeSettings settings, NBShaderFeatureTier? tier, out bool changed)
        {
            changed=false;NBShaderFeatureTier resolved=tier.HasValue?tier.Value:ResolveTierFromQuality(settings);
            if(resolved<NBShaderFeatureTier.Low||resolved>NBShaderFeatureTier.Ultra)return false;
            IEnumerable<string> keywords=settings!=null?settings.BuildAllowedSet(resolved):NBShaderFeatureCatalog.RawKeywords;
            IEnumerable<string> passes=settings!=null?settings.BuildAllowedPassFeatureSet(resolved):NBShaderPassFeatureCatalog.RawPassFeatureIds;
            return TryApplyGraphOwnedProjection(materials,resolved,keywords,passes,out changed);
        }

        private static bool IsNBShaderMaterial(Material material)
        {
            return material != null
                && material.shader != null
                && material.shader.name == NBShaderFeatureCatalog.ShaderName;
        }

        private static void ApplyResolvedIntent(Material material, NBShaderMaterialIntentResult result)
        {
            if (material == null || result == null)
            {
                return;
            }

            var effectiveKeywords = new HashSet<string>(result.effectiveKeywords);
            for (int i = 0; i < NBShaderFeatureCatalog.RawKeywords.Length; i++)
            {
                string keyword = NBShaderFeatureCatalog.RawKeywords[i];
                bool shouldEnable = effectiveKeywords.Contains(keyword);
                if (material.IsKeywordEnabled(keyword) != shouldEnable)
                {
                    if (shouldEnable)
                    {
                        material.EnableKeyword(keyword);
                    }
                    else
                    {
                        material.DisableKeyword(keyword);
                    }
                }
            }

            bool evaluateShVertex = effectiveKeywords.Contains("_FX_LIGHT_MODE_SIX_WAY");
            if (material.IsKeywordEnabled("EVALUATE_SH_VERTEX") != evaluateShVertex)
            {
                if (evaluateShVertex)
                {
                    material.EnableKeyword("EVALUATE_SH_VERTEX");
                }
                else
                {
                    material.DisableKeyword("EVALUATE_SH_VERTEX");
                }
            }

            for (int i = 0; i < result.passes.Length; i++)
            {
                NBShaderPassIntent pass = result.passes[i];
                if (!string.IsNullOrEmpty(pass.passName) &&
                    material.GetShaderPassEnabled(pass.passName) != pass.included)
                {
                    material.SetShaderPassEnabled(pass.passName, pass.included);
                }
            }
        }

        private static NBShaderFeatureTier ResolveTierFromQuality(NBShaderFeatureRuntimeSettings settings)
        {
            if (settings == null)
            {
                return NBShaderFeatureTier.Ultra;
            }

            string qualityName = GetCurrentQualityName();
            NBShaderFeatureTier tier;
            return settings.TryGetTierForQualityName(qualityName, out tier) ? tier : NBShaderFeatureTier.Ultra;
        }

        private static string GetCurrentQualityName()
        {
            string[] names = QualitySettings.names;
            int index = QualitySettings.GetQualityLevel();
            if (names != null && index >= 0 && index < names.Length)
            {
                return names[index];
            }

            return string.Empty;
        }

        private static HashSet<string> BuildAllowAllCatalogKeywordSet()
        {
            return new HashSet<string>(NBShaderFeatureCatalog.RawKeywords);
        }

        // Pure Graph projection authority. Editor owns policy, persistence, Undo and adoption.
        private const string FeatureTierPropertyName = "_NBShaderFeatureTier";
        internal const string GraphOverrideDepthKeyword = "_OVERRIDE_Z";
        internal const string GraphScreenMigration = "_NB_GraphScreenPassMigrationComplete";


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
            "_NB_TierAllowVAT", "_NB_TierAllowFlipbook",
            "_NB_TierAllowChromaticAberration"
        };

        internal static readonly string[] GraphSupportedGateKeywords = {
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
            "_VAT", "_FLIPBOOKBLENDING_ON",
            "_CHROMATIC_ABERRATION"
        };

        internal static readonly string[] GraphDeclaredKeywordNames={
            "_SPECULAR_COLOR","VFX_SIX_WAY_ABSORPTION","EVALUATE_SH_VERTEX",
            "NB_DEBUG_MASK","NB_DEBUG_PNOISE","NB_DEBUG_DISSOLVE","NB_DEBUG_DISTORT","NB_DEBUG_FRESNEL","NB_DEBUG_VERTEX_OFFSET"};

        internal static readonly string[] GraphSupportedTypedProjectionProperties={"_NB_TierVATFamily","_NB_TierVATSubMode"};

        internal static readonly string[] GraphSupportedProjectionProperties=CreateGraphProjectionProperties();

        internal static bool HasGraphOverrideDepthKeyword(Material material)
            => material != null && material.shader != null &&
                material.shader.keywordSpace.FindKeyword(GraphOverrideDepthKeyword).isValid;

        internal static bool TryReadGraphSupportedGateTier(Material material, NBShaderFeatureTier tier,
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
            var allowed = allowedManagedKeywords ?? NBShaderFeatureCatalog.RawKeywords;
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
            var vatAllowed=allowedManagedKeywords??NBShaderFeatureCatalog.RawKeywords;
            if(!NBShaderMaterialIntentResolver.TryResolveGraphVATProjection(material,tier,vatAllowed,out parentAllowed,out family,out subMode,out flipbookAllowed))return false;
            wouldChange|=material.GetFloat("_NB_TierVATFamily")!=family||material.GetFloat("_NB_TierVATSubMode")!=subMode;
            // Older graphs may omit the declared keyword; never invent one.
            if (HasGraphOverrideDepthKeyword(material))
            {
                bool overrideDepth;
                var allowed = allowedManagedKeywords ?? NBShaderFeatureCatalog.RawKeywords;
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
            var allowed = new HashSet<string>(allowedManagedKeywords ?? NBShaderFeatureCatalog.RawKeywords);
            bool wouldChange;
            if (!CanApplyGraphSupportedGateTier(material, tier, allowed, out wouldChange)) return false;
            if (!wouldChange) return true;
            // All registered groups have been preflighted before the first write.
            bool chromaticChanged;if(!ApplyGraphChromaticGroup(material,tier,allowed,out chromaticChanged))return false;changed|=chromaticChanged;
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
                changed |= SetGraphOwnedKeyword(material, GraphOverrideDepthKeyword, effective);
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

        internal static bool ApplyGraphProgramNoiseGroup(Material material, NBShaderFeatureTier tier,
            IEnumerable<string> allowedManagedKeywords, out bool changed)
        {
            changed = false;
            foreach (string name in new[] { "_NB_TierAllowProgramNoise", "_NB_TierAllowProgramSimple", "_NB_TierAllowProgramVoronoi" })
                if (!NBShaderMaterialIntentResolver.HasFloatShaderProperty(material, name)) return false;
            var allowed = allowedManagedKeywords ?? NBShaderFeatureCatalog.RawKeywords;
            NBShaderMaterialIntentResult intent; string[] unavailable;
            if (!NBShaderMaterialIntentResolver.TryResolveGraphSupportedKeywordIntent(material, tier, allowed, out intent, out unavailable)) return false;
            var effective = new HashSet<string>(intent.effectiveKeywords);
            changed |= SetGraphAllowFloat(material, "_NB_TierAllowProgramNoise", effective.Contains("_PROGRAM_NOISE"));
            changed |= SetGraphAllowFloat(material, "_NB_TierAllowProgramSimple", effective.Contains("_PROGRAM_NOISE_SIMPLE"));
            changed |= SetGraphAllowFloat(material, "_NB_TierAllowProgramVoronoi", effective.Contains("_PROGRAM_NOISE_VORONOI"));
            return true;
        }

        internal static bool ApplyGraphNoisePair(Material material, NBShaderFeatureTier tier,
            IEnumerable<string> allowedManagedKeywords, out bool changed)
        {
            changed = false;
            if (!NBShaderMaterialIntentResolver.HasFloatShaderProperty(material, "_NB_TierAllowNoise") ||
                !NBShaderMaterialIntentResolver.HasFloatShaderProperty(material, "_NB_TierAllowNoiseMask")) return false;
            var allowed = allowedManagedKeywords ?? NBShaderFeatureCatalog.RawKeywords;
            NBShaderMaterialIntentResult result; string[] unavailable;
            if (!NBShaderMaterialIntentResolver.TryResolveGraphSupportedKeywordIntent(material, tier, allowed, out result, out unavailable)) return false;
            var effective = new HashSet<string>(result.effectiveKeywords);
            changed |= SetGraphAllowFloat(material, "_NB_TierAllowNoise", effective.Contains("_NOISEMAP"));
            changed |= SetGraphAllowFloat(material, "_NB_TierAllowNoiseMask", effective.Contains("_NOISE_MASKMAP"));
            return true;
        }

        internal static bool ApplyGraphMaskGroup(Material material, NBShaderFeatureTier tier,
            IEnumerable<string> allowedManagedKeywords, out bool changed)
        {
            changed = false;
            foreach (string property in new[] { "_NB_TierAllowMask", "_NB_TierAllowMask2", "_NB_TierAllowMask3" })
                if (!NBShaderMaterialIntentResolver.HasFloatShaderProperty(material, property)) return false;
            var allowed = allowedManagedKeywords ?? NBShaderFeatureCatalog.RawKeywords;
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
            var allowed = allowedManagedKeywords ?? NBShaderFeatureCatalog.RawKeywords;
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
            var allowed = allowedManagedKeywords ?? NBShaderFeatureCatalog.RawKeywords;
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
            var allowed=allowedManagedKeywords??NBShaderFeatureCatalog.RawKeywords;
            NBShaderMaterialIntentResult intent;string[] unavailable;
            if(!NBShaderMaterialIntentResolver.TryResolveGraphSupportedKeywordIntent(material,tier,allowed,out intent,out unavailable))return false;
            changed=SetGraphAllowFloat(material,"_NB_TierAllowNormalMap",new HashSet<string>(intent.effectiveKeywords).Contains("_NORMALMAP"));
            return true;
        }

        internal static bool ApplyGraphRefractionGroup(Material material,NBShaderFeatureTier tier,IEnumerable<string> allowedManagedKeywords,out bool changed)
        {
            changed=false;if(!NBShaderMaterialIntentResolver.HasFloatShaderProperty(material,"_NB_TierAllowRefraction"))return false;
            var allowed=allowedManagedKeywords??NBShaderFeatureCatalog.RawKeywords;
            NBShaderMaterialIntentResult intent;string[] unavailable;if(!NBShaderMaterialIntentResolver.TryResolveGraphSupportedKeywordIntent(material,tier,allowed,out intent,out unavailable))return false;
            changed=SetGraphAllowFloat(material,"_NB_TierAllowRefraction",new HashSet<string>(intent.effectiveKeywords).Contains("_DISTORT_REFRACTION"));return true;
        }

        internal static bool ApplyGraphFresnelGroup(Material material, NBShaderFeatureTier tier,
            IEnumerable<string> allowedManagedKeywords, out bool changed)
        {
            changed = false;
            if (!NBShaderMaterialIntentResolver.HasFloatShaderProperty(material, "_NB_TierAllowFresnel")) return false;
            var allowed = allowedManagedKeywords ?? NBShaderFeatureCatalog.RawKeywords;
            NBShaderMaterialIntentResult intent; string[] unavailable;
            if (!NBShaderMaterialIntentResolver.TryResolveGraphSupportedKeywordIntent(material, tier, allowed, out intent, out unavailable)) return false;
            changed = SetGraphAllowFloat(material, "_NB_TierAllowFresnel", new HashSet<string>(intent.effectiveKeywords).Contains("_FRESNEL"));
            return true;
        }

        internal static bool ApplyGraphDissolveGroup(Material material,NBShaderFeatureTier tier,
            IEnumerable<string> allowedManagedKeywords,out bool changed)
        {
            changed=false;
            foreach(string name in new[]{"_NB_TierAllowDissolve","_NB_TierAllowDissolveMask","_NB_TierAllowDissolveRamp","_NB_TierAllowDissolveRampMap"})
                if(!NBShaderMaterialIntentResolver.HasFloatShaderProperty(material,name))return false;
            var allowed=allowedManagedKeywords??NBShaderFeatureCatalog.RawKeywords;
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
            var allowed=allowedManagedKeywords??NBShaderFeatureCatalog.RawKeywords;
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
            var allowed=allowedManagedKeywords??NBShaderFeatureCatalog.RawKeywords;
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
            var allowed=allowedManagedKeywords??NBShaderFeatureCatalog.RawKeywords;
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
            var allowed=allowedManagedKeywords??NBShaderFeatureCatalog.RawKeywords;NBShaderMaterialIntentResult intent;string[] unavailable;if(!NBShaderMaterialIntentResolver.TryResolveGraphSupportedKeywordIntent(material,tier,allowed,out intent,out unavailable))return false;
            var effective=new System.Collections.Generic.HashSet<string>(intent.effectiveKeywords);changed|=SetGraphAllowFloat(material,"_NB_TierAllowVertexOffset",effective.Contains("_VERTEX_OFFSET"));changed|=SetGraphAllowFloat(material,"_NB_TierAllowVertexOffsetMask",effective.Contains("_VERTEX_OFFSET_MASKMAP"));return true;
        }

        internal static bool ApplyGraphDepthDecalGroup(Material material,NBShaderFeatureTier tier,
            IEnumerable<string> allowedManagedKeywords,out bool changed)
        {
            changed=false;
            if(!NBShaderMaterialIntentResolver.HasFloatShaderProperty(material,"_NB_TierAllowDepthDecal"))return false;
            var allowed=allowedManagedKeywords??NBShaderFeatureCatalog.RawKeywords;
            NBShaderMaterialIntentResult intent;string[] unavailable;
            if(!NBShaderMaterialIntentResolver.TryResolveGraphSupportedKeywordIntent(material,tier,allowed,out intent,out unavailable))return false;
            changed=SetGraphAllowFloat(material,"_NB_TierAllowDepthDecal",new HashSet<string>(intent.effectiveKeywords).Contains("_DEPTH_DECAL"));return true;
        }

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

        internal static bool ApplyGraphVATProjectionGroup(Material material,NBShaderFeatureTier tier,
            IEnumerable<string> allowedManagedKeywords,out bool changed)
        {
            changed=false;bool oldProjection;if(!HasGraphVATProjectionState(material,out oldProjection))return false;
            foreach(string name in new[]{"_NB_TierAllowVAT","_NB_TierAllowFlipbook"})
            {if(!NBShaderMaterialIntentResolver.HasFloatShaderProperty(material,name))return false;float value=material.GetFloat(name);if(float.IsNaN(value)||float.IsInfinity(value))return false;}
            bool parentAllowed,flipbookAllowed;float family,subMode;
            var vatAllowed=allowedManagedKeywords??NBShaderFeatureCatalog.RawKeywords;
            if(!NBShaderMaterialIntentResolver.TryResolveGraphVATProjection(material,tier,vatAllowed,out parentAllowed,out family,out subMode,out flipbookAllowed))return false;
            changed|=SetGraphAllowFloat(material,"_NB_TierAllowVAT",parentAllowed);changed|=SetGraphAllowFloat(material,"_NB_TierAllowFlipbook",flipbookAllowed);
            if(material.GetFloat("_NB_TierVATFamily")!=family){material.SetFloat("_NB_TierVATFamily",family);changed=true;}
            if(material.GetFloat("_NB_TierVATSubMode")!=subMode){material.SetFloat("_NB_TierVATSubMode",subMode);changed=true;}
            return true;
        }

        internal static bool ApplyGraphChromaticGroup(Material material,NBShaderFeatureTier tier,IEnumerable<string> allowedKeywords,out bool changed)
        {
            changed=false;if(!NBShaderMaterialIntentResolver.HasFloatShaderProperty(material,"_NB_TierAllowChromaticAberration"))return false;
            NBShaderMaterialIntentResult intent;
            if(!TryReadGraphSupportedGateTier(material,tier,allowedKeywords,out intent))return false;
            changed=SetGraphAllowFloat(material,"_NB_TierAllowChromaticAberration",new HashSet<string>(intent.effectiveKeywords).Contains("_CHROMATIC_ABERRATION"));return true;
        }

        internal static bool SetGraphAllowFloat(Material material, string name, bool enabled)
        {
            float value = enabled ? 1f : 0f;
            if (material.GetFloat(name) == value) return false;
            material.SetFloat(name, value);
            return true;
        }

        internal static bool GraphDeclaredKeywordWanted(string name,HashSet<string> effective)
            => effective.Contains(name=="EVALUATE_SH_VERTEX"?"_FX_LIGHT_MODE_SIX_WAY":name);

        internal static bool CanApplyGraphDeclaredKeywordState(Material material,HashSet<string> effective,out bool wouldChange)
        {
            wouldChange=false;if(material==null||material.shader==null)return false;
            foreach(string name in GraphDeclaredKeywordNames)
            {
                if(!material.shader.keywordSpace.FindKeyword(name).isValid)return false;
                wouldChange|=material.IsKeywordEnabled(name)!=GraphDeclaredKeywordWanted(name,effective);
            }
            return true;
        }

        internal static bool ApplyGraphDeclaredKeywordState(Material material,HashSet<string> effective,out bool changed)
        {
            changed=false;bool wouldChange;
            if(!CanApplyGraphDeclaredKeywordState(material,effective,out wouldChange))return false;
            if(!wouldChange)return true;
            foreach(string name in GraphDeclaredKeywordNames)changed|=SetGraphOwnedKeyword(material,name,GraphDeclaredKeywordWanted(name,effective));
            return true;
        }

        internal static bool ApplyGraphDeclaredKeywords(Material material,NBShaderFeatureTier tier,
            IEnumerable<string> allowedManagedKeywords,out bool changed)
        {
            changed=false;NBShaderMaterialIntentResult intent;
            if(!TryReadGraphSupportedGateTier(material,tier,allowedManagedKeywords,out intent))return false;
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
            for(int i=0;i<states.Length;++i)SetGraphOwnedKeyword(material,GraphDeclaredKeywordNames[i],states[i]);
        }

        internal static bool GraphLightingAllowed(HashSet<string> effective)
        {
            foreach(string keyword in effective)
                if(keyword.StartsWith("_FX_LIGHT_MODE_",System.StringComparison.Ordinal))return true;
            return false;
        }

        internal static bool GraphGateEnabled(int index,HashSet<string> effective)
            => GraphSupportedGateKeywords[index]==null ? GraphLightingAllowed(effective) : effective.Contains(GraphSupportedGateKeywords[index]);

        internal static string[] CreateGraphProjectionProperties()
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

        internal static bool SetGraphOwnedKeyword(Material material, string keyword, bool enabled)
        {
            if (material == null || string.IsNullOrEmpty(keyword) || material.IsKeywordEnabled(keyword) == enabled)
                return false;

            if (enabled)
                material.EnableKeyword(keyword);
            else
                material.DisableKeyword(keyword);

            return true;
        }

        internal static bool CanApplyGraphOwnedScreenPassState(Material material,NBShaderFeatureTier tier,IEnumerable<string> allowedKeywords,IEnumerable<string> allowedPassFeatures,out NBShaderPassIntent[] intent)
        {
            intent=null;
            if(!material.HasProperty(GraphScreenMigration))return true; // Old capabilities own no screen pass.
            if(!NBShaderMaterialIntentResolver.HasFloatShaderProperty(material,GraphScreenMigration))return false;
            float state=material.GetFloat(GraphScreenMigration);if(state==0)return true;if(state!=1)return false;
            return NBShaderMaterialIntentResolver.TryResolveGraphScreenPassIntent(material,tier,allowedKeywords,allowedPassFeatures,out intent);
        }

        internal static bool ApplyGraphOwnedScreenPassState(Material material,NBShaderFeatureTier tier,IEnumerable<string> allowedKeywords,IEnumerable<string> allowedPassFeatures,out bool changed)
        {
            changed=false;NBShaderPassIntent[] intent;
            if(!CanApplyGraphOwnedScreenPassState(material,tier,allowedKeywords,allowedPassFeatures,out intent))return false;
            if(intent==null)return true;
            foreach(var pass in intent)if(material.GetShaderPassEnabled(pass.passName)!=pass.included){material.SetShaderPassEnabled(pass.passName,pass.included);changed=true;}
            return true;
        }

        internal static bool CanApplyGraphOwnedBackFirstPassState(Material material, NBShaderFeatureTier tier,
            IEnumerable<string> allowedKeywords, IEnumerable<string> allowedPasses, out NBShaderPassIntent intent)
        {
            intent = null;
            int route;
            if (!NBShaderPassFeatureCatalog.TryGetGraphColorRouting(material, out route)) return false;
            if (route == 0) return true;
            if (!NBShaderMaterialIntentResolver.HasFloatShaderProperty(material, "_NB_GraphPassMigrationComplete")) return false;
            float migration = material.GetFloat("_NB_GraphPassMigrationComplete");
            if (migration == 0f) return true;
            if (migration != 1f) return false;
            return NBShaderMaterialIntentResolver.TryResolveGraphBackFirstPassIntent(material, tier,
                allowedKeywords, allowedPasses, out intent);
        }

        internal static bool ApplyGraphOwnedBackFirstPassState(Material material, NBShaderFeatureTier tier,
            IEnumerable<string> allowedKeywords, IEnumerable<string> allowedPasses, out bool changed)
        {
            changed = false;
            NBShaderPassIntent intent;
            if (!CanApplyGraphOwnedBackFirstPassState(material, tier, allowedKeywords, allowedPasses, out intent)) return false;
            if (intent == null) return true;
            // The existing method returns changed, rather than accepted.
            changed = TryApplyGraphBackFirstPassIntent(material, tier, allowedKeywords, allowedPasses);
            return true;
        }

        internal static bool TryApplyGraphBackFirstPassIntent(Material material, NBShaderFeatureTier tier,
            IEnumerable<string> allowedKeywords, IEnumerable<string> allowedPassFeatureIds)
        {
            NBShaderPassIntent intent;
            if (!NBShaderMaterialIntentResolver.TryResolveGraphBackFirstPassIntent(material, tier, allowedKeywords, allowedPassFeatureIds, out intent)) return false;
            bool changed = false;
            if (material.GetShaderPassEnabled(intent.passName) != intent.included)
            { material.SetShaderPassEnabled(intent.passName, intent.included); changed = true; }
            float effective = intent.included ? 1f : 0f;
            if (material.GetFloat("_NB_BackFirstEffective") != effective)
            { material.SetFloat("_NB_BackFirstEffective", effective); changed = true; }
            return changed;
        }

        // One explicit pure composite. Slice1 intentionally leaves public ApplyTier
        // Native-only; Editor can continue using its existing narrow Group surfaces.
        internal static bool CanApplyGraphOwnedProjection(Material material, NBShaderFeatureTier tier,
            IEnumerable<string> allowedKeywords, IEnumerable<string> allowedPassFeatures, out bool wouldChange)
        {
            wouldChange = false;
            var keywords = new HashSet<string>(allowedKeywords ?? NBShaderFeatureCatalog.RawKeywords);
            var passes = new HashSet<string>(allowedPassFeatures ?? NBShaderPassFeatureCatalog.RawPassFeatureIds);
            if (!CanApplyGraphSupportedGateTier(material, tier, keywords, out wouldChange)) return false;
            NBShaderPassIntent[] screen; NBShaderPassIntent back;
            if (!CanApplyGraphOwnedScreenPassState(material, tier, keywords, passes, out screen) ||
                !CanApplyGraphOwnedBackFirstPassState(material, tier, keywords, passes, out back)) return false;
            if (screen != null)
                foreach (var pass in screen) wouldChange |= material.GetShaderPassEnabled(pass.passName) != pass.included;
            if (back != null)
                wouldChange |= material.GetShaderPassEnabled(back.passName) != back.included ||
                    material.GetFloat("_NB_BackFirstEffective") != (back.included ? 1f : 0f);
            return true;
        }

        private sealed class GraphOwnedProjectionBeforeImage
        {
            readonly Material material;
            readonly float[] projections;
            readonly bool[] declared;
            readonly bool hasOverrideDepth, overrideDepth;
            readonly Dictionary<string, bool> passes = new Dictionary<string, bool>();
            readonly bool hasBack; readonly float backEffective;
            internal GraphOwnedProjectionBeforeImage(Material value, NBShaderFeatureTier tier,
                IEnumerable<string> keywords, IEnumerable<string> passFeatures)
            {
                material = value; projections = new float[GraphSupportedProjectionProperties.Length];
                for (int i = 0; i < projections.Length; ++i) projections[i] = value.GetFloat(GraphSupportedProjectionProperties[i]);
                declared = CaptureGraphDeclaredKeywordState(value);
                hasOverrideDepth = HasGraphOverrideDepthKeyword(value);
                overrideDepth = hasOverrideDepth && value.IsKeywordEnabled(GraphOverrideDepthKeyword);
                NBShaderPassIntent[] screen; NBShaderPassIntent back;
                CanApplyGraphOwnedScreenPassState(value, tier, keywords, passFeatures, out screen);
                CanApplyGraphOwnedBackFirstPassState(value, tier, keywords, passFeatures, out back);
                if (screen != null) foreach (var pass in screen) passes[pass.passName] = value.GetShaderPassEnabled(pass.passName);
                hasBack = back != null;
                if (hasBack) { passes[back.passName] = value.GetShaderPassEnabled(back.passName); backEffective = value.GetFloat("_NB_BackFirstEffective"); }
            }
            internal void Restore()
            {
                for (int i = 0; i < projections.Length; ++i) material.SetFloat(GraphSupportedProjectionProperties[i], projections[i]);
                RestoreGraphDeclaredKeywordState(material, declared);
                if (hasOverrideDepth) SetGraphOwnedKeyword(material, GraphOverrideDepthKeyword, overrideDepth);
                foreach (var pass in passes) material.SetShaderPassEnabled(pass.Key, pass.Value);
                if (hasBack) material.SetFloat("_NB_BackFirstEffective", backEffective);
            }
        }

        internal static bool TryApplyGraphOwnedProjection(Material material, NBShaderFeatureTier tier,
            IEnumerable<string> allowedKeywords, IEnumerable<string> allowedPassFeatures, out bool changed)
        {
            return TryApplyGraphOwnedProjection(new[] { material }, tier, allowedKeywords, allowedPassFeatures, out changed);
        }

        // Explicit Graph selection is atomic. Null/unknown/invalid last targets
        // refuse before the first write. This does not alter Native collection semantics.
        internal static bool TryApplyGraphOwnedProjection(IEnumerable<Material> materials, NBShaderFeatureTier tier,
            IEnumerable<string> allowedKeywords, IEnumerable<string> allowedPassFeatures, out bool changed)
        {
            changed = false; if (materials == null) return false;
            var keywords = new HashSet<string>(allowedKeywords ?? NBShaderFeatureCatalog.RawKeywords);
            var passes = new HashSet<string>(allowedPassFeatures ?? NBShaderPassFeatureCatalog.RawPassFeatureIds);
            var targets = new List<Material>(); var seen = new HashSet<Material>();
            foreach (Material material in materials)
            {
                bool ignored;
                if (!CanApplyGraphOwnedProjection(material, tier, keywords, passes, out ignored)) return false;
                if (seen.Add(material)) targets.Add(material);
            }
            if (targets.Count == 0) return false;
            var before = new List<GraphOwnedProjectionBeforeImage>();
            foreach (Material material in targets) before.Add(new GraphOwnedProjectionBeforeImage(material, tier, keywords, passes));
            try
            {
                foreach (Material material in targets)
                {
                    bool gateChanged, screenChanged, backChanged;
                    if (!ApplyGraphSupportedGateTier(material, tier, keywords, out gateChanged) ||
                        !ApplyGraphOwnedScreenPassState(material, tier, keywords, passes, out screenChanged) ||
                        !ApplyGraphOwnedBackFirstPassState(material, tier, keywords, passes, out backChanged))
                    { foreach (var image in before) image.Restore(); changed = false; return false; }
                    changed |= gateChanged || screenChanged || backChanged;
                }
                return true;
            }
            catch
            {
                foreach (var image in before) image.Restore();
                changed = false; throw;
            }
        }

    }
}
