using System.Collections.Generic;

namespace NBShader
{
    /// <summary>
    /// Hard-coded catalog of raw shader feature keywords managed by the NBShader runtime tier system.
    /// Keywords not listed here are never changed by <see cref="NBShaderFeatureRuntime"/>.
    /// </summary>
    public static class NBShaderFeatureCatalog
    {
        /// <summary>
        /// Shader name used by NBShader materials.
        /// </summary>
        public const string ShaderName = "Effects/NBShader";

        /// <summary>
        /// Resources path used to load <see cref="NBShaderFeatureRuntimeSettings"/>.
        /// Place the asset at any Resources folder as "NBShaderFeatureRuntimeSettings.asset".
        /// </summary>
        public const string RuntimeSettingsResourcePath = "NBShaderFeatureRuntimeSettings";

        /// <summary>
        /// Raw shader_feature keywords currently managed by NBShader tiering. The '_' placeholder and
        /// multi_compile keywords are intentionally excluded.
        /// </summary>
        public static readonly string[] RawKeywords =
        {
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
            "_PARCUSTOMDATA_ON",
            "_PROGRAM_NOISE",
            "_PROGRAM_NOISE_SIMPLE",
            "_PROGRAM_NOISE_VORONOI",
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
            "_VAT_TYFLOW",
            "_VERTEX_OFFSET",
            "_VERTEX_OFFSET_MASKMAP"
        };

        internal static readonly HashSet<string> RawKeywordSet = new HashSet<string>(RawKeywords);

        /// <summary>
        /// Returns true when the keyword is explicitly managed by the NBShader feature tier system.
        /// Catalog-external keywords must be ignored by tier application and build stripping.
        /// </summary>
        public static bool IsManagedKeyword(string keyword)
        {
            return !string.IsNullOrEmpty(keyword) && RawKeywordSet.Contains(keyword);
        }
    }
}
