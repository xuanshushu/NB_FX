using System.Collections.Generic;

namespace NBShader
{
    /// <summary>
    /// Hard-coded catalog of raw shader feature keywords managed by the NBShader2 runtime tier system.
    /// Keywords not listed here are never changed by <see cref="NBShader2FeatureRuntime"/>.
    /// </summary>
    public static class NBShader2FeatureCatalog
    {
        /// <summary>
        /// Shader name used by NBShader2 materials.
        /// </summary>
        public const string ShaderName = "Effects/NBShader2";

        /// <summary>
        /// Resources path used to load <see cref="NBShader2FeatureRuntimeSettings"/>.
        /// Place the asset at any Resources folder as "NBShader2FeatureRuntimeSettings.asset".
        /// </summary>
        public const string RuntimeSettingsResourcePath = "NBShader2FeatureRuntimeSettings";

        /// <summary>
        /// Raw shader_feature keywords currently managed by NBShader2 tiering. The '_' placeholder and
        /// multi_compile keywords are intentionally excluded.
        /// </summary>
        public static readonly string[] RawKeywords =
        {
            "FRESNEL_CUBEMAP",
            "FRESNEL_REFLECTIONPROBE",
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
            "_COLORMAPBLEND",
            "_COLOR_RAMP",
            "_CUSTOM_LOCAL_TRANSFORM",
            "_DEPTH_DECAL",
            "_DISSOLVE",
            "_DISSOLVE_EDITOR_TEST",
            "_DISTORT_REFRACTION",
            "_EMISSION",
            "_FLIPBOOKBLENDING_ON",
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
            "_MATCAP",
            "_NOISEMAP",
            "_NOISEMAP_NORMALIZEED",
            "_NORMALMAP",
            "_PARALLAX_MAPPING",
            "_PARCUSTOMDATA_ON",
            "_PROGRAM_NOISE",
            "_SCREEN_DISTORT_MODE",
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
            "_VAT_TYFLOW"
        };

        internal static readonly HashSet<string> RawKeywordSet = new HashSet<string>(RawKeywords);

        /// <summary>
        /// Returns true when the keyword is explicitly managed by the NBShader2 feature tier system.
        /// Catalog-external keywords must be ignored by tier application and build stripping.
        /// </summary>
        public static bool IsManagedKeyword(string keyword)
        {
            return !string.IsNullOrEmpty(keyword) && RawKeywordSet.Contains(keyword);
        }

        /// <summary>
        /// Built-in fallback whitelist used when the project runtime settings asset is missing.
        /// Project Settings can override these defaults per project.
        /// </summary>
        public static string[] GetDefaultAllowedKeywords(NBShader2FeatureTier tier)
        {
            switch (tier)
            {
                case NBShader2FeatureTier.Low:
                    return new[]
                    {
                        "_ALPHAPREMULTIPLY_ON",
                        "_ALPHAMODULATE_ON",
                        "_ALPHATEST_ON",
                        "_SOFTPARTICLES_ON",
                        "_UNSCALETIME",
                        "_SCRIPTABLETIME",
                        "_FX_LIGHT_MODE_UNLIT"
                    };

                case NBShader2FeatureTier.Medium:
                    return new[]
                    {
                        "_MASKMAP_ON",
                        "_NOISEMAP",
                        "_DISSOLVE",
                        "_SHARED_UV",
                        "FRESNEL_CUBEMAP",
                        "FRESNEL_REFLECTIONPROBE",
                        "_ALPHAPREMULTIPLY_ON",
                        "_ALPHAMODULATE_ON",
                        "_ALPHATEST_ON",
                        "_SOFTPARTICLES_ON",
                        "_UNSCALETIME",
                        "_SCRIPTABLETIME",
                        "_NOISEMAP_NORMALIZEED",
                        "_DEPTH_DECAL",
                        "_FX_LIGHT_MODE_UNLIT",
                        "_NORMALMAP"
                    };

                case NBShader2FeatureTier.High:
                    return new[]
                    {
                        "_DISTORT_REFRACTION",
                        "_SCREEN_DISTORT_MODE",
                        "_MASKMAP_ON",
                        "_NOISEMAP",
                        "_EMISSION",
                        "_DISSOLVE",
                        "_PROGRAM_NOISE",
                        "_COLORMAPBLEND",
                        "_COLOR_RAMP",
                        "_SHARED_UV",
                        "_CUSTOM_LOCAL_TRANSFORM",
                        "FRESNEL_CUBEMAP",
                        "FRESNEL_REFLECTIONPROBE",
                        "_ALPHAPREMULTIPLY_ON",
                        "_ALPHAMODULATE_ON",
                        "_ALPHATEST_ON",
                        "_SOFTPARTICLES_ON",
                        "_UNSCALETIME",
                        "_SCRIPTABLETIME",
                        "_NOISEMAP_NORMALIZEED",
                        "_DEPTH_DECAL",
                        "_PARALLAX_MAPPING",
                        "_STENCIL_WITHOUT_PLAYER",
                        "_FX_LIGHT_MODE_UNLIT",
                        "_FX_LIGHT_MODE_BLINN_PHONG",
                        "_FX_LIGHT_MODE_HALF_LAMBERT",
                        "_NORMALMAP",
                        "_MATCAP",
                        "_SPECULAR_COLOR"
                    };

                default:
                    return RawKeywords;
            }
        }

        internal static HashSet<string> BuildDefaultAllowedSet(NBShader2FeatureTier tier)
        {
            return new HashSet<string>(GetDefaultAllowedKeywords(tier));
        }
    }
}
