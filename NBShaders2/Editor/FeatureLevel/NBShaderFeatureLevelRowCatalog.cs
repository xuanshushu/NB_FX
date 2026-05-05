using System.Collections.Generic;

namespace NBShaders2.Editor.FeatureLevel
{
    internal enum NBShaderFeatureLevelRowKind
    {
        Group,
        Keyword
    }

    internal sealed class NBShaderFeatureLevelRow
    {
        public readonly NBShaderFeatureLevelRowKind kind;
        public readonly string key;
        public readonly string parentKey;
        public readonly string keyword;
        public readonly string labelFallback;
        public readonly int depth;

        public NBShaderFeatureLevelRow(
            NBShaderFeatureLevelRowKind kind,
            string key,
            string parentKey,
            string keyword,
            string labelFallback,
            int depth)
        {
            this.kind = kind;
            this.key = key;
            this.parentKey = parentKey;
            this.keyword = keyword;
            this.labelFallback = labelFallback;
            this.depth = depth;
        }

        public bool isKeyword
        {
            get { return kind == NBShaderFeatureLevelRowKind.Keyword; }
        }
    }

    internal static class NBShaderFeatureLevelRowCatalog
    {
        public static readonly NBShaderFeatureLevelRow[] Rows =
        {
            Group("mode", null, "Mode", 0),
            Group("transparent", "mode", "Transparent Mode", 1),
            Keyword("_ALPHATEST_ON", "transparent", "Alpha Test", 2),
            Keyword("_ALPHAPREMULTIPLY_ON", "transparent", "Premultiply Alpha", 2),
            Keyword("_ALPHAMODULATE_ON", "transparent", "Alpha Modulate", 2),
            Group("time", "mode", "Time Mode", 1),
            Keyword("_UNSCALETIME", "time", "Unscaled Time", 2),
            Keyword("_SCRIPTABLETIME", "time", "Scriptable Time", 2),

            Group("base", null, "Base", 0),
            Keyword("_DISTANCE_FADE", "base", "Distance Fade", 1),
            Keyword("_SOFTPARTICLES_ON", "base", "Soft Particles", 1),
            Keyword("_STENCIL_WITHOUT_PLAYER", "base", "Stencil Without Player", 1),
            Group("particle", "base", "Particle Data", 1),
            Keyword("_PARCUSTOMDATA_ON", "particle", "Particle Custom Data", 2),

            Group("light", null, "Lighting", 0),
            Group("lightMode", "light", "Light Mode", 1),
            Keyword("_FX_LIGHT_MODE_UNLIT", "lightMode", "Unlit Lighting", 2),
            Keyword("_FX_LIGHT_MODE_BLINN_PHONG", "lightMode", "Blinn-Phong Lighting", 2),
            Keyword("_FX_LIGHT_MODE_HALF_LAMBERT", "lightMode", "Half Lambert Lighting", 2),
            Keyword("_FX_LIGHT_MODE_PBR", "lightMode", "PBR Lighting", 2),
            Keyword("_FX_LIGHT_MODE_SIX_WAY", "lightMode", "Six Way Lighting", 2),
            Keyword("_SPECULAR_COLOR", "light", "Specular Color", 1),
            Keyword("_NORMALMAP", "light", "Normal Map", 1),
            Keyword("_MATCAP", "light", "MatCap", 1),
            Keyword("VFX_SIX_WAY_ABSORPTION", "light", "Six Way Absorption", 1),

            Group("feature", null, "Feature", 0),
            Keyword("_MASKMAP_ON", "feature", "Mask Map", 1),
            Keyword("_MASKMAP2_ON", "_MASKMAP_ON", "Mask Map 2", 2),
            Keyword("_MASKMAP3_ON", "_MASKMAP_ON", "Mask Map 3", 2),
            Keyword("NB_DEBUG_MASK", "_MASKMAP_ON", "Debug Mask", 2),
            Keyword("_NOISEMAP", "feature", "Noise Map", 1),
            Keyword("_NOISE_MASKMAP", "_NOISEMAP", "Noise Mask Map", 2),
            Keyword("_CHROMATIC_ABERRATION", "_NOISEMAP", "Chromatic Aberration", 2),
            Keyword("NB_DEBUG_DISTORT", "_NOISEMAP", "Debug Distort", 2),
            Keyword("_SCREEN_DISTORT_MODE", "_NOISEMAP", "Screen Distort", 2),
            Keyword("_DISTORT_REFRACTION", "_NOISEMAP", "Distort Refraction", 2),
            Keyword("_EMISSION", "feature", "Emission", 1),
            Keyword("_COLORMAPBLEND", "feature", "Color Map Blend", 1),
            Keyword("_COLOR_RAMP", "feature", "Color Ramp", 1),
            Keyword("_COLOR_RAMP_MAP", "_COLOR_RAMP", "Color Ramp Map", 2),
            Keyword("_DISSOLVE", "feature", "Dissolve", 1),
            Keyword("_DISSOLVE_MASK", "_DISSOLVE", "Dissolve Mask", 2),
            Keyword("_DISSOLVE_RAMP_MAP", "_DISSOLVE", "Dissolve Ramp Map", 2),
            Keyword("NB_DEBUG_DISSOLVE", "_DISSOLVE", "Debug Dissolve", 2),
            Keyword("_DISSOLVE_EDITOR_TEST", "_DISSOLVE", "Editor Dissolve Test", 2),
            Keyword("_PROGRAM_NOISE", "feature", "Program Noise", 1),
            Keyword("_PROGRAM_NOISE_SIMPLE", "_PROGRAM_NOISE", "Program Noise Simple", 2),
            Keyword("_PROGRAM_NOISE_VORONOI", "_PROGRAM_NOISE", "Program Noise Voronoi", 2),
            Keyword("NB_DEBUG_PNOISE", "_PROGRAM_NOISE", "Debug Program Noise", 2),
            Keyword("_SHARED_UV", "feature", "Shared UV", 1),
            Group("fresnel", "feature", "Fresnel", 1),
            Keyword("_FRESNEL", "fresnel", "Fresnel", 2),
            Keyword("NB_DEBUG_FRESNEL", "fresnel", "Debug Fresnel", 2),
            Keyword("FRESNEL_CUBEMAP", "fresnel", "Fresnel Cubemap", 2),
            Keyword("FRESNEL_REFLECTIONPROBE", "fresnel", "Fresnel Reflection Probe", 2),
            Group("vertexOffset", "feature", "Vertex Offset", 1),
            Keyword("_VERTEX_OFFSET", "vertexOffset", "Vertex Offset", 2),
            Keyword("_VERTEX_OFFSET_MASKMAP", "_VERTEX_OFFSET", "Vertex Offset Mask Map", 3),
            Keyword("NB_DEBUG_VERTEX_OFFSET", "vertexOffset", "Debug Vertex Offset", 2),
            Keyword("_CUSTOM_LOCAL_TRANSFORM", "vertexOffset", "Custom Local Transform", 2),
            Group("depth", "feature", "Depth", 1),
            Keyword("_DEPTH_DECAL", "depth", "Depth Decal", 2),
            Keyword("_DEPTH_OUTLINE", "depth", "Depth Outline", 2),
            Keyword("_PARALLAX_MAPPING", "feature", "Parallax Mapping", 1),
            Keyword("_FLIPBOOKBLENDING_ON", "feature", "Flipbook Blending", 1),
            Keyword("_VAT", "feature", "VAT", 1),
            Group("vatMode", "_VAT", "VAT Mode", 2),
            Keyword("_VAT_HOUDINI", "vatMode", "Houdini VAT", 3),
            Keyword("_VAT_TYFLOW", "vatMode", "TyFlow VAT", 3),
            Group("houdiniVat", "_VAT", "Houdini VAT", 2),
            Keyword("_HOUDINI_VAT_SOFTBODY", "houdiniVat", "Houdini VAT Soft Body", 3),
            Keyword("_HOUDINI_VAT_RIGIDBODY", "houdiniVat", "Houdini VAT Rigid Body", 3),
            Keyword("_HOUDINI_VAT_DYNAMIC_REMESH", "houdiniVat", "Houdini VAT Dynamic Remesh", 3),
            Keyword("_HOUDINI_VAT_PARTICLE_SPRITE", "houdiniVat", "Houdini VAT Particle Sprite", 3),
            Group("tyflowVat", "_VAT", "TyFlow VAT", 2),
            Keyword("_TYFLOW_VAT_ABSOLUTE", "tyflowVat", "TyFlow VAT Absolute", 3),
            Keyword("_TYFLOW_VAT_RELATIVE", "tyflowVat", "TyFlow VAT Relative", 3),
            Keyword("_TYFLOW_VAT_SKIN_R", "tyflowVat", "TyFlow VAT Skin R", 3),
            Keyword("_TYFLOW_VAT_SKIN_PR", "tyflowVat", "TyFlow VAT Skin PR", 3),
            Keyword("_TYFLOW_VAT_SKIN_PRSAVE", "tyflowVat", "TyFlow VAT Skin PR Save", 3),
            Keyword("_TYFLOW_VAT_SKIN_PRSXYZ", "tyflowVat", "TyFlow VAT Skin PRSXYZ", 3)
        };

        private static readonly HashSet<string> ParentKeys = BuildParentKeys();
        private static readonly Dictionary<string, NBShaderFeatureLevelRow> RowsByKey = BuildRowsByKey();

        public static bool HasChildren(NBShaderFeatureLevelRow row)
        {
            return row != null && ParentKeys.Contains(row.key);
        }

        public static bool TryGetRow(string key, out NBShaderFeatureLevelRow row)
        {
            return RowsByKey.TryGetValue(key, out row);
        }

        private static NBShaderFeatureLevelRow Group(string key, string parentKey, string labelFallback, int depth)
        {
            return new NBShaderFeatureLevelRow(NBShaderFeatureLevelRowKind.Group, key, parentKey, null, labelFallback, depth);
        }

        private static NBShaderFeatureLevelRow Keyword(string keyword, string parentKey, string labelFallback, int depth)
        {
            return new NBShaderFeatureLevelRow(NBShaderFeatureLevelRowKind.Keyword, keyword, parentKey, keyword, labelFallback, depth);
        }

        private static HashSet<string> BuildParentKeys()
        {
            var result = new HashSet<string>();
            for (var i = 0; i < Rows.Length; i++)
            {
                if (!string.IsNullOrEmpty(Rows[i].parentKey))
                    result.Add(Rows[i].parentKey);
            }

            return result;
        }

        private static Dictionary<string, NBShaderFeatureLevelRow> BuildRowsByKey()
        {
            var result = new Dictionary<string, NBShaderFeatureLevelRow>();
            for (var i = 0; i < Rows.Length; i++)
                result[Rows[i].key] = Rows[i];
            return result;
        }
    }
}
