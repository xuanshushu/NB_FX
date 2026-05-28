using System;
using System.Collections.Generic;
using NBShader;
using UnityEngine;
using UnityEngine.Rendering;

namespace NBShaders2.Editor.FeatureLevel
{
    public enum NBShaderBuildInfoMode
    {
        TierAndPass = 0,
        ExactMaterialVariants = 1
    }

    public sealed class NBShaderBuildInfoSet
    {
        private readonly NBShaderMaterialBuildInfo[] m_Materials;
        private readonly NBShaderVariantBuildInfo[] m_Variants;
        private readonly string[] m_AllowedManagedKeywords;
        private readonly string[] m_AllowedPassFeatures;
        private readonly string[] m_IncludedPassNames;

        public readonly NBShaderFeatureTier tier;
        public readonly NBShaderBuildInfoMode mode;

        public NBShaderMaterialBuildInfo[] materials { get { return (NBShaderMaterialBuildInfo[])m_Materials.Clone(); } }
        public NBShaderVariantBuildInfo[] variants { get { return (NBShaderVariantBuildInfo[])m_Variants.Clone(); } }
        public string[] allowedManagedKeywords { get { return (string[])m_AllowedManagedKeywords.Clone(); } }
        public string[] allowedPassFeatures { get { return (string[])m_AllowedPassFeatures.Clone(); } }
        public string[] includedPassNames { get { return (string[])m_IncludedPassNames.Clone(); } }
        public bool hasMaterials { get { return m_Materials.Length > 0; } }

        public NBShaderBuildInfoSet(
            NBShaderFeatureTier tier,
            NBShaderBuildInfoMode mode,
            NBShaderMaterialBuildInfo[] materials,
            string[] allowedManagedKeywords,
            string[] allowedPassFeatures = null)
        {
            this.tier = tier;
            this.mode = mode;
            m_Materials = materials != null ? (NBShaderMaterialBuildInfo[])materials.Clone() : new NBShaderMaterialBuildInfo[0];
            m_AllowedManagedKeywords = allowedManagedKeywords != null ? (string[])allowedManagedKeywords.Clone() : new string[0];
            m_AllowedPassFeatures = NBShaderBuildInfoUtility.ToCatalogOrderedPassFeatures(allowedPassFeatures);
            m_Variants = BuildUniqueVariants(m_Materials);
            m_IncludedPassNames = BuildIncludedPassNames(m_Materials);
        }

        private static NBShaderVariantBuildInfo[] BuildUniqueVariants(NBShaderMaterialBuildInfo[] materials)
        {
            var result = new List<NBShaderVariantBuildInfo>();
            var keys = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < materials.Length; i++)
            {
                var material = materials[i];
                if (material == null)
                    continue;

                var materialVariants = material.variants;
                for (var v = 0; v < materialVariants.Length; v++)
                {
                    var variant = materialVariants[v];
                    if (variant == null)
                        continue;

                    var key = variant.GetStableKey();
                    if (keys.Add(key))
                        result.Add(variant);
                }
            }

            return result.ToArray();
        }

        private static string[] BuildIncludedPassNames(NBShaderMaterialBuildInfo[] materials)
        {
            var result = new List<string>();
            var set = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < materials.Length; i++)
            {
                var material = materials[i];
                if (material == null)
                    continue;

                var passNames = material.includedPassNames;
                for (var p = 0; p < passNames.Length; p++)
                {
                    var passName = passNames[p];
                    if (!string.IsNullOrEmpty(passName) && set.Add(passName))
                        result.Add(passName);
                }
            }

            return result.ToArray();
        }
    }

    public sealed class NBShaderMaterialBuildInfo
    {
        private readonly string[] m_EffectiveKeywords;
        private readonly string[] m_StrippedManagedKeywords;
        private readonly string[] m_AllowedManagedKeywords;
        private readonly string[] m_AllowedPassFeatures;
        private readonly NBShaderPassBuildInfo[] m_Passes;
        private readonly NBShaderVariantBuildInfo[] m_Variants;
        private readonly string[] m_IncludedPassNames;

        public readonly Material material;
        public readonly Shader shader;
        public readonly NBShaderFeatureTier tier;
        public readonly NBShaderBuildInfoMode mode;

        public string[] effectiveKeywords { get { return (string[])m_EffectiveKeywords.Clone(); } }
        public string[] strippedManagedKeywords { get { return (string[])m_StrippedManagedKeywords.Clone(); } }
        public string[] allowedManagedKeywords { get { return (string[])m_AllowedManagedKeywords.Clone(); } }
        public string[] allowedPassFeatures { get { return (string[])m_AllowedPassFeatures.Clone(); } }
        public NBShaderPassBuildInfo[] passes { get { return (NBShaderPassBuildInfo[])m_Passes.Clone(); } }
        public NBShaderVariantBuildInfo[] variants { get { return (NBShaderVariantBuildInfo[])m_Variants.Clone(); } }
        public string[] includedPassNames { get { return (string[])m_IncludedPassNames.Clone(); } }

        public NBShaderMaterialBuildInfo(
            Material material,
            NBShaderFeatureTier tier,
            NBShaderBuildInfoMode mode,
            string[] allowedManagedKeywords,
            string[] effectiveKeywords,
            string[] strippedManagedKeywords,
            NBShaderPassBuildInfo[] passes,
            string[] allowedPassFeatures = null)
        {
            this.material = material;
            shader = material != null ? material.shader : null;
            this.tier = tier;
            this.mode = mode;
            m_AllowedManagedKeywords = NBShaderBuildInfoUtility.ToCatalogOrderedManagedKeywords(allowedManagedKeywords);
            m_AllowedPassFeatures = NBShaderBuildInfoUtility.ToCatalogOrderedPassFeatures(allowedPassFeatures);
            m_EffectiveKeywords = NBShaderBuildInfoUtility.ToCatalogOrderedManagedKeywords(effectiveKeywords);
            m_StrippedManagedKeywords = NBShaderBuildInfoUtility.ToCatalogOrderedManagedKeywords(strippedManagedKeywords);
            m_Passes = passes != null ? (NBShaderPassBuildInfo[])passes.Clone() : new NBShaderPassBuildInfo[0];
            m_IncludedPassNames = BuildIncludedPassNames(m_Passes);
            m_Variants = BuildVariants();
        }

        private NBShaderVariantBuildInfo[] BuildVariants()
        {
            if (shader == null)
                return new NBShaderVariantBuildInfo[0];

            var result = new List<NBShaderVariantBuildInfo>();
            var materialKeywords = NBShaderBuildInfoUtility.GetCatalogExternalMaterialKeywords(material);
            for (var i = 0; i < m_Passes.Length; i++)
            {
                var pass = m_Passes[i];
                if (pass == null || !pass.included)
                    continue;

                result.Add(new NBShaderVariantBuildInfo(
                    shader,
                    pass.passName,
                    pass.passType,
                    NBShaderBuildInfoUtility.BuildShaderVariantKeywordsForPass(
                        pass.passName,
                        pass.passType,
                        m_EffectiveKeywords,
                        materialKeywords)));
            }

            return result.ToArray();
        }

        private static string[] BuildIncludedPassNames(NBShaderPassBuildInfo[] passes)
        {
            var result = new List<string>();
            for (var i = 0; i < passes.Length; i++)
            {
                var pass = passes[i];
                if (pass != null && pass.included && !string.IsNullOrEmpty(pass.passName))
                    result.Add(pass.passName);
            }

            return result.ToArray();
        }
    }

    public sealed class NBShaderPassBuildInfo
    {
        public readonly string passName;
        public readonly PassType passType;
        public readonly bool enabledByMaterial;
        public readonly bool allowedByTier;
        public readonly bool included;
        public readonly string reason;

        public NBShaderPassBuildInfo(
            string passName,
            PassType passType,
            bool enabledByMaterial,
            bool allowedByTier,
            bool included,
            string reason)
        {
            this.passName = passName;
            this.passType = passType;
            this.enabledByMaterial = enabledByMaterial;
            this.allowedByTier = allowedByTier;
            this.included = included;
            this.reason = reason;
        }
    }

    public sealed class NBShaderVariantBuildInfo
    {
        private readonly string[] m_Keywords;

        public readonly Shader shader;
        public readonly string passName;
        public readonly PassType passType;
        public string[] keywords { get { return (string[])m_Keywords.Clone(); } }

        public NBShaderVariantBuildInfo(Shader shader, string passName, PassType passType, string[] keywords)
        {
            this.shader = shader;
            this.passName = passName;
            this.passType = passType;
            m_Keywords = NBShaderBuildInfoUtility.ToShaderVariantOrderedKeywords(keywords);
        }

        public string GetStableKey()
        {
            string shaderName = shader != null ? shader.name : string.Empty;
            return shaderName + "|" + passName + "|" + passType + "|" + string.Join(";", m_Keywords);
        }
    }

    internal static class NBShaderBuildInfoUtility
    {
        public static PassType GetPassType(string passName)
        {
            if (passName == "SRPDefaultUnlit")
                return PassType.ScriptableRenderPipelineDefaultUnlit;
            if (passName == "ShadowCaster")
                return PassType.ShadowCaster;
            return PassType.ScriptableRenderPipeline;
        }

        public static string[] FilterKeywordsForPass(string passName, PassType passType, IEnumerable<string> keywords)
        {
            var declaredKeywords = GetDeclaredKeywords(passName, passType);
            var result = new List<string>();
            var source = ToCatalogOrderedManagedKeywords(keywords);
            for (var i = 0; i < source.Length; i++)
            {
                var keyword = source[i];
                if (declaredKeywords.Contains(keyword))
                    result.Add(keyword);
            }

            return result.ToArray();
        }

        public static string[] BuildShaderVariantKeywordsForPass(
            string passName,
            PassType passType,
            IEnumerable<string> managedKeywords,
            IEnumerable<string> externalKeywords)
        {
            var declaredKeywords = GetDeclaredShaderVariantKeywords(passName, passType);
            var source = new HashSet<string>(StringComparer.Ordinal);

            var managed = ToCatalogOrderedManagedKeywords(managedKeywords);
            for (var i = 0; i < managed.Length; i++)
            {
                var keyword = managed[i];
                source.Add(keyword);
                if (string.Equals(keyword, "_FX_LIGHT_MODE_SIX_WAY", StringComparison.Ordinal))
                    source.Add("EVALUATE_SH_VERTEX");
            }

            if (externalKeywords != null)
            {
                foreach (var keyword in externalKeywords)
                {
                    if (IsCatalogExternalShaderKeyword(keyword))
                        source.Add(keyword);
                }
            }

            var result = new List<string>();
            for (var i = 0; i < NBShaderFeatureCatalog.RawKeywords.Length; i++)
            {
                var keyword = NBShaderFeatureCatalog.RawKeywords[i];
                if (source.Contains(keyword) && declaredKeywords.Contains(keyword))
                    result.Add(keyword);
            }

            for (var i = 0; i < CatalogExternalShaderKeywordOrder.Length; i++)
            {
                var keyword = CatalogExternalShaderKeywordOrder[i];
                if (source.Contains(keyword) && declaredKeywords.Contains(keyword))
                    result.Add(keyword);
            }

            return result.ToArray();
        }

        public static string[] GetCatalogExternalMaterialKeywords(Material material)
        {
            if (material == null || material.shaderKeywords == null || material.shaderKeywords.Length == 0)
                return new string[0];

            var result = new List<string>();
            var source = new HashSet<string>(material.shaderKeywords, StringComparer.Ordinal);
            for (var i = 0; i < CatalogExternalShaderKeywordOrder.Length; i++)
            {
                var keyword = CatalogExternalShaderKeywordOrder[i];
                if (source.Contains(keyword))
                    result.Add(keyword);
            }

            return result.ToArray();
        }

        public static string[] ToCatalogOrderedManagedKeywords(IEnumerable<string> keywords)
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            if (keywords != null)
            {
                foreach (var keyword in keywords)
                {
                    if (NBShaderFeatureCatalog.IsManagedKeyword(keyword))
                        set.Add(keyword);
                }
            }

            if (set.Count == 0)
                return new string[0];

            var result = new List<string>();
            for (var i = 0; i < NBShaderFeatureCatalog.RawKeywords.Length; i++)
            {
                var keyword = NBShaderFeatureCatalog.RawKeywords[i];
                if (set.Contains(keyword))
                    result.Add(keyword);
            }

            return result.ToArray();
        }

        public static string[] ToShaderVariantOrderedKeywords(IEnumerable<string> keywords)
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            if (keywords != null)
            {
                foreach (var keyword in keywords)
                {
                    if (NBShaderFeatureCatalog.IsManagedKeyword(keyword) ||
                        IsCatalogExternalShaderKeyword(keyword))
                    {
                        set.Add(keyword);
                    }
                }
            }

            if (set.Count == 0)
                return new string[0];

            var result = new List<string>();
            for (var i = 0; i < NBShaderFeatureCatalog.RawKeywords.Length; i++)
            {
                var keyword = NBShaderFeatureCatalog.RawKeywords[i];
                if (set.Contains(keyword))
                    result.Add(keyword);
            }

            for (var i = 0; i < CatalogExternalShaderKeywordOrder.Length; i++)
            {
                var keyword = CatalogExternalShaderKeywordOrder[i];
                if (set.Contains(keyword))
                    result.Add(keyword);
            }

            return result.ToArray();
        }

        public static string[] ToCatalogOrderedPassFeatures(IEnumerable<string> passFeatures)
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            if (passFeatures != null)
            {
                foreach (var passFeature in passFeatures)
                {
                    if (NBShaderPassFeatureCatalog.IsManagedPassFeature(passFeature))
                        set.Add(passFeature);
                }
            }

            if (set.Count == 0)
                return new string[0];

            var result = new List<string>();
            for (var i = 0; i < NBShaderPassFeatureCatalog.RawPassFeatureIds.Length; i++)
            {
                var passFeature = NBShaderPassFeatureCatalog.RawPassFeatureIds[i];
                if (set.Contains(passFeature))
                    result.Add(passFeature);
            }

            return result.ToArray();
        }

        public static bool ContainsPassName(string[] passNames, string passName)
        {
            if (passNames == null || string.IsNullOrEmpty(passName))
                return false;

            for (var i = 0; i < passNames.Length; i++)
            {
                if (string.Equals(passNames[i], passName, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        public static bool IsSubsetOf(string[] subset, string[] superset)
        {
            var set = new HashSet<string>(superset ?? new string[0], StringComparer.Ordinal);
            if (subset == null)
                return true;

            for (var i = 0; i < subset.Length; i++)
            {
                if (!set.Contains(subset[i]))
                    return false;
            }

            return true;
        }

        public static bool AreKeywordSetsEqual(string[] a, string[] b)
        {
            var normalizedA = ToCatalogOrderedManagedKeywords(a);
            var normalizedB = ToCatalogOrderedManagedKeywords(b);
            if (normalizedA.Length != normalizedB.Length)
                return false;

            for (var i = 0; i < normalizedA.Length; i++)
            {
                if (!string.Equals(normalizedA[i], normalizedB[i], StringComparison.Ordinal))
                    return false;
            }

            return true;
        }

        private static HashSet<string> GetDeclaredKeywords(string passName, PassType passType)
        {
            if (passType == PassType.ShadowCaster)
                return DeclaredDepthShadowKeywords;

            if (string.Equals(passName, "DepthOnly", StringComparison.Ordinal))
                return DeclaredDepthShadowKeywords;

            if (string.Equals(passName, "NBCameraOpaqueDistortPass", StringComparison.Ordinal) ||
                string.Equals(passName, "NBDeferredDistortPass", StringComparison.Ordinal))
            {
                return DeclaredDistortKeywords;
            }

            if (string.Equals(passName, "Universal2D", StringComparison.Ordinal))
                return DeclaredUniversal2DKeywords;

            return DeclaredForwardKeywords;
        }

        private static HashSet<string> GetDeclaredShaderVariantKeywords(string passName, PassType passType)
        {
            if (passType == PassType.ShadowCaster)
                return DeclaredShadowCasterShaderVariantKeywords;

            if (string.Equals(passName, "DepthOnly", StringComparison.Ordinal))
                return DeclaredDepthOnlyShaderVariantKeywords;

            if (string.Equals(passName, "NBCameraOpaqueDistortPass", StringComparison.Ordinal) ||
                string.Equals(passName, "NBDeferredDistortPass", StringComparison.Ordinal))
            {
                return DeclaredDistortShaderVariantKeywords;
            }

            if (string.Equals(passName, "Universal2D", StringComparison.Ordinal))
                return DeclaredUniversal2DShaderVariantKeywords;

            return DeclaredForwardShaderVariantKeywords;
        }

        private static bool IsCatalogExternalShaderKeyword(string keyword)
        {
            return !string.IsNullOrEmpty(keyword) && CatalogExternalShaderKeywordSet.Contains(keyword);
        }

        private static readonly string[] CatalogExternalShaderKeywordOrder =
        {
            "EVALUATE_SH_VERTEX",
            "EVALUATE_SH_MIXED",
            "SOFT_UI_FRAME",
            "UNITY_UI_CLIP_RECT",
            "_ADDITIONAL_LIGHTS_VERTEX",
            "_ADDITIONAL_LIGHTS",
            "_CASTING_PUNCTUAL_LIGHT_SHADOW"
        };

        private static readonly HashSet<string> CatalogExternalShaderKeywordSet =
            new HashSet<string>(CatalogExternalShaderKeywordOrder, StringComparer.Ordinal);

        private static readonly HashSet<string> DeclaredForwardKeywords = BuildDeclaredKeywordSet(
            "NB_DEBUG_DISSOLVE",
            "NB_DEBUG_DISTORT",
            "NB_DEBUG_FRESNEL",
            "NB_DEBUG_MASK",
            "NB_DEBUG_PNOISE",
            "NB_DEBUG_VERTEX_OFFSET",
            "VFX_SIX_WAY_ABSORPTION",
            "_ALPHAMODULATE_ON",
            "_ALPHAPREMULTIPLY_ON",
            "_ALPHATEST_ON",
            "_CHROMATIC_ABERRATION",
            "_COLORMAPBLEND",
            "_COLOR_RAMP",
            "_COLOR_RAMP_MAP",
            "_DEPTH_DECAL",
            "_DEPTH_OUTLINE",
            "_DISSOLVE",
            "_DISSOLVE_MASK",
            "_DISSOLVE_RAMP",
            "_DISSOLVE_RAMP_MAP",
            "_DISTANCE_FADE",
            "_DISTORT_REFRACTION",
            "_EMISSION",
            "_FLIPBOOKBLENDING_ON",
            "_FRESNEL",
            "_FX_LIGHT_MODE_BLINN_PHONG",
            "_FX_LIGHT_MODE_HALF_LAMBERT",
            "_FX_LIGHT_MODE_PBR",
            "_FX_LIGHT_MODE_SIX_WAY",
            "_FX_LIGHT_MODE_UNLIT",
            "_HOUDINI_VAT_DYNAMIC_REMESH",
            "_HOUDINI_VAT_PARTICLE_SPRITE",
            "_HOUDINI_VAT_RIGIDBODY",
            "_HOUDINI_VAT_SOFTBODY",
            "_MASKMAP_ON",
            "_MASKMAP2_ON",
            "_MASKMAP3_ON",
            "_MATCAP",
            "_NOISEMAP",
            "_NOISE_MASKMAP",
            "_NORMALMAP",
            "_OVERRIDE_Z",
            "_PARALLAX_MAPPING",
            "_PROGRAM_NOISE",
            "_PROGRAM_NOISE_SIMPLE",
            "_PROGRAM_NOISE_VORONOI",
            "_SCRIPTABLETIME",
            "_SHARED_UV",
            "_SOFTPARTICLES_ON",
            "_SPECULAR_COLOR",
            "_STENCIL_WITHOUT_PLAYER",
            "_TYFLOW_VAT_ABSOLUTE",
            "_TYFLOW_VAT_RELATIVE",
            "_TYFLOW_VAT_SKIN_PR",
            "_TYFLOW_VAT_SKIN_PRSAVE",
            "_TYFLOW_VAT_SKIN_PRSXYZ",
            "_TYFLOW_VAT_SKIN_R",
            "_UNSCALETIME",
            "_VAT",
            "_VAT_HOUDINI",
            "_VAT_TYFLOW",
            "_VERTEX_OFFSET",
            "_VERTEX_OFFSET_MASKMAP");

        private static readonly HashSet<string> DeclaredDistortKeywords = BuildDeclaredKeywordSet(
            "_ALPHAMODULATE_ON",
            "_ALPHAPREMULTIPLY_ON",
            "_ALPHATEST_ON",
            "_CHROMATIC_ABERRATION",
            "_DEPTH_DECAL",
            "_DEPTH_OUTLINE",
            "_DISSOLVE",
            "_DISSOLVE_MASK",
            "_DISSOLVE_RAMP",
            "_DISSOLVE_RAMP_MAP",
            "_DISTANCE_FADE",
            "_DISTORT_REFRACTION",
            "_FLIPBOOKBLENDING_ON",
            "_FRESNEL",
            "_FX_LIGHT_MODE_UNLIT",
            "_MASKMAP_ON",
            "_MASKMAP2_ON",
            "_MASKMAP3_ON",
            "_NOISEMAP",
            "_NOISE_MASKMAP",
            "_NORMALMAP",
            "_PROGRAM_NOISE",
            "_PROGRAM_NOISE_SIMPLE",
            "_PROGRAM_NOISE_VORONOI",
            "_SCRIPTABLETIME",
            "_SHARED_UV",
            "_SOFTPARTICLES_ON",
            "_STENCIL_WITHOUT_PLAYER",
            "_UNSCALETIME",
            "_VERTEX_OFFSET",
            "_VERTEX_OFFSET_MASKMAP");

        private static readonly HashSet<string> DeclaredDepthShadowKeywords = BuildDeclaredKeywordSet(
            "_ALPHATEST_ON",
            "_DISSOLVE",
            "_DISSOLVE_MASK",
            "_FLIPBOOKBLENDING_ON",
            "_HOUDINI_VAT_DYNAMIC_REMESH",
            "_HOUDINI_VAT_PARTICLE_SPRITE",
            "_HOUDINI_VAT_RIGIDBODY",
            "_HOUDINI_VAT_SOFTBODY",
            "_MASKMAP_ON",
            "_MASKMAP2_ON",
            "_MASKMAP3_ON",
            "_NOISEMAP",
            "_NOISE_MASKMAP",
            "_PROGRAM_NOISE",
            "_PROGRAM_NOISE_SIMPLE",
            "_PROGRAM_NOISE_VORONOI",
            "_SCRIPTABLETIME",
            "_SHARED_UV",
            "_TYFLOW_VAT_ABSOLUTE",
            "_TYFLOW_VAT_RELATIVE",
            "_TYFLOW_VAT_SKIN_PR",
            "_TYFLOW_VAT_SKIN_PRSAVE",
            "_TYFLOW_VAT_SKIN_PRSXYZ",
            "_TYFLOW_VAT_SKIN_R",
            "_UNSCALETIME",
            "_VAT",
            "_VAT_HOUDINI",
            "_VAT_TYFLOW",
            "_VERTEX_OFFSET",
            "_VERTEX_OFFSET_MASKMAP");

        private static readonly HashSet<string> DeclaredUniversal2DKeywords = BuildDeclaredKeywordSet(
            "_ALPHAMODULATE_ON",
            "_ALPHAPREMULTIPLY_ON",
            "_ALPHATEST_ON",
            "_CHROMATIC_ABERRATION",
            "_COLORMAPBLEND",
            "_COLOR_RAMP",
            "_COLOR_RAMP_MAP",
            "_DISSOLVE",
            "_DISSOLVE_MASK",
            "_DISSOLVE_RAMP",
            "_DISSOLVE_RAMP_MAP",
            "_EMISSION",
            "_FLIPBOOKBLENDING_ON",
            "_FRESNEL",
            "_FX_LIGHT_MODE_UNLIT",
            "_MASKMAP_ON",
            "_MASKMAP2_ON",
            "_MASKMAP3_ON",
            "_NOISEMAP",
            "_NOISE_MASKMAP",
            "_NORMALMAP",
            "_PARCUSTOMDATA_ON",
            "_PROGRAM_NOISE",
            "_PROGRAM_NOISE_SIMPLE",
            "_PROGRAM_NOISE_VORONOI",
            "_SCREEN_DISTORT_MODE",
            "_SCRIPTABLETIME",
            "_SHARED_UV",
            "_STENCIL_WITHOUT_PLAYER",
            "_UNSCALETIME",
            "_VERTEX_OFFSET",
            "_VERTEX_OFFSET_MASKMAP");

        private static readonly HashSet<string> DeclaredForwardShaderVariantKeywords =
            BuildDeclaredShaderVariantKeywordSet(
                DeclaredForwardKeywords,
                "SOFT_UI_FRAME",
                "EVALUATE_SH_MIXED",
                "EVALUATE_SH_VERTEX",
                "UNITY_UI_CLIP_RECT",
                "_ADDITIONAL_LIGHTS_VERTEX",
                "_ADDITIONAL_LIGHTS");

        private static readonly HashSet<string> DeclaredDistortShaderVariantKeywords =
            BuildDeclaredShaderVariantKeywordSet(
                DeclaredDistortKeywords,
                "SOFT_UI_FRAME",
                "EVALUATE_SH_MIXED",
                "EVALUATE_SH_VERTEX");

        private static readonly HashSet<string> DeclaredDepthOnlyShaderVariantKeywords =
            BuildDeclaredShaderVariantKeywordSet(
                DeclaredDepthShadowKeywords);

        private static readonly HashSet<string> DeclaredShadowCasterShaderVariantKeywords =
            BuildDeclaredShaderVariantKeywordSet(
                DeclaredDepthShadowKeywords,
                "_CASTING_PUNCTUAL_LIGHT_SHADOW");

        private static readonly HashSet<string> DeclaredUniversal2DShaderVariantKeywords =
            BuildDeclaredShaderVariantKeywordSet(
                DeclaredUniversal2DKeywords,
                "SOFT_UI_FRAME",
                "EVALUATE_SH_MIXED",
                "EVALUATE_SH_VERTEX",
                "UNITY_UI_CLIP_RECT",
                "_ADDITIONAL_LIGHTS_VERTEX",
                "_ADDITIONAL_LIGHTS");

        private static HashSet<string> BuildDeclaredKeywordSet(params string[] keywords)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            if (keywords == null)
                return result;

            for (var i = 0; i < keywords.Length; i++)
            {
                var keyword = keywords[i];
                if (NBShaderFeatureCatalog.IsManagedKeyword(keyword))
                    result.Add(keyword);
            }

            return result;
        }

        private static HashSet<string> BuildDeclaredShaderVariantKeywordSet(
            HashSet<string> managedKeywords,
            params string[] externalKeywords)
        {
            var result = new HashSet<string>(managedKeywords ?? new HashSet<string>(), StringComparer.Ordinal);
            if (externalKeywords == null)
                return result;

            for (var i = 0; i < externalKeywords.Length; i++)
            {
                var keyword = externalKeywords[i];
                if (IsCatalogExternalShaderKeyword(keyword))
                    result.Add(keyword);
            }

            return result;
        }
    }
}
