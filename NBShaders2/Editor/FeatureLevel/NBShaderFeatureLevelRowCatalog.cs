using System.Collections.Generic;
using NBShader;

namespace NBShaders2.Editor.FeatureLevel
{
    internal enum NBShaderFeatureLevelRowKind
    {
        Group,
        Keyword,
        Pass
    }

    internal enum NBShaderFeaturePerformanceCost
    {
        Low,
        Medium,
        High,
        Ultra
    }

    internal sealed class NBShaderFeatureLevelRow
    {
        public readonly NBShaderFeatureLevelRowKind kind;
        public readonly string key;
        public readonly string parentKey;
        public readonly string keyword;
        public readonly string passFeatureId;
        public readonly string labelFallback;
        public readonly NBShaderFeaturePerformanceCost performanceCost;
        public readonly string effectFallback;
        public readonly int depth;

        public NBShaderFeatureLevelRow(
            NBShaderFeatureLevelRowKind kind,
            string key,
            string parentKey,
            string keyword,
            string passFeatureId,
            string labelFallback,
            NBShaderFeaturePerformanceCost performanceCost,
            string effectFallback,
            int depth)
        {
            this.kind = kind;
            this.key = key;
            this.parentKey = parentKey;
            this.keyword = keyword;
            this.passFeatureId = passFeatureId;
            this.labelFallback = labelFallback;
            this.performanceCost = performanceCost;
            this.effectFallback = effectFallback;
            this.depth = depth;
        }

        public bool isKeyword
        {
            get { return kind == NBShaderFeatureLevelRowKind.Keyword; }
        }

        public bool isPass
        {
            get { return kind == NBShaderFeatureLevelRowKind.Pass; }
        }
    }

    internal static class NBShaderFeatureLevelRowCatalog
    {
        public static readonly NBShaderFeatureLevelRow[] Rows =
        {
            Group("mode", null, "Mode", 0),
            Group("transparent", "mode", "Transparent Mode", 1),
            Keyword("_ALPHATEST_ON", "transparent", "Alpha Test", 2, NBShaderFeaturePerformanceCost.Medium, "Cuts pixels by alpha threshold."),
            Keyword("_ALPHAPREMULTIPLY_ON", "transparent", "Premultiply Alpha", 2, NBShaderFeaturePerformanceCost.Low, "Uses premultiplied alpha blending."),
            Keyword("_ALPHAMODULATE_ON", "transparent", "Alpha Modulate", 2, NBShaderFeaturePerformanceCost.Low, "Uses alpha modulation transparent blending."),
            Group("time", "mode", "Time Mode", 1),
            Keyword("_UNSCALETIME", "time", "Unscaled Time", 2, NBShaderFeaturePerformanceCost.Low, "Uses unscaled time."),
            Keyword("_SCRIPTABLETIME", "time", "Scriptable Time", 2, NBShaderFeaturePerformanceCost.Low, "Uses script provided time value."),

            Group("base", null, "Base", 0),
            Keyword("_DISTANCE_FADE", "base", "Distance Fade", 1, NBShaderFeaturePerformanceCost.Low, "Fades material by camera distance."),
            Keyword("_SOFTPARTICLES_ON", "base", "Soft Particles", 1, NBShaderFeaturePerformanceCost.High, "Softens particles using scene depth."),
            Keyword("_STENCIL_WITHOUT_PLAYER", "base", "Stencil Without Player", 1, NBShaderFeaturePerformanceCost.Low, "Uses stencil without player mask."),
            Group("particle", "base", "Particle Data", 1),
            Keyword("_PARCUSTOMDATA_ON", "particle", "Particle Custom Data", 2, NBShaderFeaturePerformanceCost.Low, "Reads particle custom data channels."),

            Group("light", null, "Lighting", 0),
            Group("lightMode", "light", "Light Mode", 1),
            Keyword("_FX_LIGHT_MODE_UNLIT", "lightMode", "Unlit Lighting", 2, NBShaderFeaturePerformanceCost.Low, "Uses unlit shading."),
            Keyword("_FX_LIGHT_MODE_BLINN_PHONG", "lightMode", "Blinn-Phong Lighting", 2, NBShaderFeaturePerformanceCost.Medium, "Uses Blinn Phong lighting."),
            Keyword("_FX_LIGHT_MODE_HALF_LAMBERT", "lightMode", "Half Lambert Lighting", 2, NBShaderFeaturePerformanceCost.Medium, "Uses half Lambert lighting."),
            Keyword("_FX_LIGHT_MODE_PBR", "lightMode", "PBR Lighting", 2, NBShaderFeaturePerformanceCost.High, "Uses PBR lighting calculations."),
            Keyword("_FX_LIGHT_MODE_SIX_WAY", "lightMode", "Six Way Lighting", 2, NBShaderFeaturePerformanceCost.Ultra, "Uses six way lighting data."),
            Keyword("_SPECULAR_COLOR", "light", "Specular Color", 1, NBShaderFeaturePerformanceCost.Medium, "Adds specular color highlight."),
            Keyword("_NORMALMAP", "light", "Normal Map", 1, NBShaderFeaturePerformanceCost.High, "Uses normal map lighting detail."),
            Keyword("_MATCAP", "light", "MatCap", 1, NBShaderFeaturePerformanceCost.Medium, "Adds matcap lighting texture."),
            Keyword("VFX_SIX_WAY_ABSORPTION", "light", "Six Way Absorption", 1, NBShaderFeaturePerformanceCost.High, "Adds absorption response for six way lighting."),

            Group("feature", null, "Feature", 0),
            Keyword("_MASKMAP_ON", "feature", "Mask Map", 1, NBShaderFeaturePerformanceCost.Medium, "Uses the first mask map."),
            Keyword("_MASKMAP2_ON", "_MASKMAP_ON", "Mask Map 2", 2, NBShaderFeaturePerformanceCost.Medium, "Uses the second mask map."),
            Keyword("_MASKMAP3_ON", "_MASKMAP_ON", "Mask Map 3", 2, NBShaderFeaturePerformanceCost.Medium, "Uses the third mask map."),
            Keyword("NB_DEBUG_MASK", "_MASKMAP_ON", "Debug Mask", 2, NBShaderFeaturePerformanceCost.Low, "Shows mask values for debugging."),
            Keyword("_NOISEMAP", "feature", "Noise Map", 1, NBShaderFeaturePerformanceCost.High, "Uses a noise map for distortion."),
            Keyword("_NOISE_MASKMAP", "_NOISEMAP", "Noise Mask Map", 2, NBShaderFeaturePerformanceCost.Medium, "Masks the distortion effect."),
            Keyword("NB_DEBUG_DISTORT", "_NOISEMAP", "Debug Distort", 2, NBShaderFeaturePerformanceCost.Low, "Shows distortion strength for debugging."),
            Keyword("_SCREEN_DISTORT_MODE", "_NOISEMAP", "Screen Distort", 2, NBShaderFeaturePerformanceCost.Ultra, "Samples screen texture for distortion."),
            Keyword("_DISTORT_REFRACTION", "_NOISEMAP", "Distort Refraction", 2, NBShaderFeaturePerformanceCost.High, "Uses refraction style distortion."),
            Keyword("_CHROMATIC_ABERRATION", "feature", "Chromatic Aberration", 1, NBShaderFeaturePerformanceCost.High, "Splits color channels for distortion fringe."),
            Keyword("_EMISSION", "feature", "Emission", 1, NBShaderFeaturePerformanceCost.Medium, "Adds emissive color or texture contribution."),
            Keyword("_COLORMAPBLEND", "feature", "Color Map Blend", 1, NBShaderFeaturePerformanceCost.Medium, "Blends an extra color gradient texture."),
            Keyword("_COLOR_RAMP", "feature", "Color Ramp", 1, NBShaderFeaturePerformanceCost.Medium, "Remaps color through a ramp."),
            Keyword("_COLOR_RAMP_MAP", "_COLOR_RAMP", "Color Ramp Map", 2, NBShaderFeaturePerformanceCost.Medium, "Uses a texture as the color ramp source."),
            Keyword("_DISSOLVE", "feature", "Dissolve", 1, NBShaderFeaturePerformanceCost.High, "Clips and blends pixels for dissolve."),
            Keyword("_DISSOLVE_MASK", "_DISSOLVE", "Dissolve Mask", 2, NBShaderFeaturePerformanceCost.High, "Uses a process mask for dissolve."),
            Keyword("_DISSOLVE_RAMP", "_DISSOLVE", "Dissolve Ramp", 2, NBShaderFeaturePerformanceCost.Medium, "Adds ramp coloring to dissolve edges."),
            Keyword("_DISSOLVE_RAMP_MAP", "_DISSOLVE_RAMP", "Dissolve Ramp Map", 3, NBShaderFeaturePerformanceCost.Medium, "Uses a texture for dissolve ramp color."),
            Keyword("NB_DEBUG_DISSOLVE", "_DISSOLVE", "Debug Dissolve", 2, NBShaderFeaturePerformanceCost.Low, "Shows dissolve values for debugging."),
            Keyword("_PROGRAM_NOISE", "feature", "Program Noise", 1, NBShaderFeaturePerformanceCost.High, "Generates procedural noise in shader."),
            Keyword("_PROGRAM_NOISE_SIMPLE", "_PROGRAM_NOISE", "Program Noise Simple", 2, NBShaderFeaturePerformanceCost.High, "Enables simple procedural noise."),
            Keyword("_PROGRAM_NOISE_VORONOI", "_PROGRAM_NOISE", "Program Noise Voronoi", 2, NBShaderFeaturePerformanceCost.High, "Enables Voronoi procedural noise."),
            Keyword("NB_DEBUG_PNOISE", "_PROGRAM_NOISE", "Debug Program Noise", 2, NBShaderFeaturePerformanceCost.Low, "Shows procedural noise values for debugging."),
            Keyword("_SHARED_UV", "feature", "Shared UV", 1, NBShaderFeaturePerformanceCost.Low, "Shares a common UV transform."),
            Keyword("_FRESNEL", "feature", "Fresnel", 1, NBShaderFeaturePerformanceCost.Medium, "Adds view angle based rim lighting."),
            Keyword("NB_DEBUG_FRESNEL", "_FRESNEL", "Debug Fresnel", 2, NBShaderFeaturePerformanceCost.Low, "Shows fresnel values for debugging."),
            Keyword("_VERTEX_OFFSET", "feature", "Vertex Offset", 1, NBShaderFeaturePerformanceCost.High, "Offsets vertices in the shader."),
            Keyword("_VERTEX_OFFSET_MASKMAP", "_VERTEX_OFFSET", "Vertex Offset Mask Map", 2, NBShaderFeaturePerformanceCost.High, "Masks vertex offset by texture."),
            Keyword("NB_DEBUG_VERTEX_OFFSET", "_VERTEX_OFFSET", "Debug Vertex Offset", 2, NBShaderFeaturePerformanceCost.Low, "Shows vertex offset values for debugging."),
            Group("depth", "feature", "Depth", 1),
            Keyword("_DEPTH_DECAL", "depth", "Depth Decal", 2, NBShaderFeaturePerformanceCost.High, "Projects decal effect using scene depth."),
            Keyword("_DEPTH_OUTLINE", "depth", "Depth Outline", 2, NBShaderFeaturePerformanceCost.High, "Creates outline effect using depth difference."),
            Keyword("_OVERRIDE_Z", "depth", "Override Z", 2, NBShaderFeaturePerformanceCost.Medium, "Writes a fixed camera eye depth through SV_Depth."),
            Keyword("_PARALLAX_MAPPING", "feature", "Parallax Mapping", 1, NBShaderFeaturePerformanceCost.High, "Offsets UVs for parallax depth."),
            Keyword("_FLIPBOOKBLENDING_ON", "feature", "Flipbook Blending", 1, NBShaderFeaturePerformanceCost.Medium, "Blends between flipbook frames."),
            Keyword("_VAT", "feature", "VAT", 1, NBShaderFeaturePerformanceCost.Ultra, "Enables vertex animation texture playback."),
            Group("vatMode", "_VAT", "VAT Mode", 2),
            Keyword("_VAT_HOUDINI", "vatMode", "Houdini VAT", 3, NBShaderFeaturePerformanceCost.Ultra, "Uses Houdini VAT data layout."),
            Keyword("_VAT_TYFLOW", "vatMode", "TyFlow VAT", 3, NBShaderFeaturePerformanceCost.Ultra, "Uses TyFlow VAT data layout."),
            Group("houdiniVat", "_VAT", "Houdini VAT", 2),
            Keyword("_HOUDINI_VAT_SOFTBODY", "houdiniVat", "Houdini VAT Soft Body", 3, NBShaderFeaturePerformanceCost.Ultra, "Uses Houdini soft body VAT mode."),
            Keyword("_HOUDINI_VAT_RIGIDBODY", "houdiniVat", "Houdini VAT Rigid Body", 3, NBShaderFeaturePerformanceCost.Ultra, "Uses Houdini rigid body VAT mode."),
            Keyword("_HOUDINI_VAT_DYNAMIC_REMESH", "houdiniVat", "Houdini VAT Dynamic Remesh", 3, NBShaderFeaturePerformanceCost.Ultra, "Uses Houdini dynamic remesh VAT mode."),
            Keyword("_HOUDINI_VAT_PARTICLE_SPRITE", "houdiniVat", "Houdini VAT Particle Sprite", 3, NBShaderFeaturePerformanceCost.Ultra, "Uses Houdini particle sprite VAT mode."),
            Group("tyflowVat", "_VAT", "TyFlow VAT", 2),
            Keyword("_TYFLOW_VAT_ABSOLUTE", "tyflowVat", "TyFlow VAT Absolute", 3, NBShaderFeaturePerformanceCost.Ultra, "Uses TyFlow absolute VAT mode."),
            Keyword("_TYFLOW_VAT_RELATIVE", "tyflowVat", "TyFlow VAT Relative", 3, NBShaderFeaturePerformanceCost.Ultra, "Uses TyFlow relative VAT mode."),
            Keyword("_TYFLOW_VAT_SKIN_R", "tyflowVat", "TyFlow VAT Skin R", 3, NBShaderFeaturePerformanceCost.Ultra, "Uses TyFlow skin R VAT mode."),
            Keyword("_TYFLOW_VAT_SKIN_PR", "tyflowVat", "TyFlow VAT Skin PR", 3, NBShaderFeaturePerformanceCost.Ultra, "Uses TyFlow skin PR VAT mode."),
            Keyword("_TYFLOW_VAT_SKIN_PRSAVE", "tyflowVat", "TyFlow VAT Skin PR Save", 3, NBShaderFeaturePerformanceCost.Ultra, "Uses TyFlow skin PR save VAT mode."),
            Keyword("_TYFLOW_VAT_SKIN_PRSXYZ", "tyflowVat", "TyFlow VAT Skin PRSXYZ", 3, NBShaderFeaturePerformanceCost.Ultra, "Uses TyFlow skin PRSXYZ VAT mode."),

            Group("passes", null, "Passes", 0),
            Pass(NBShaderPassFeatureCatalog.BackFirstPassId, "passes", "Back First Pass", 1, NBShaderFeaturePerformanceCost.Medium, "Renders the optional back-face pre-pass."),
            Pass(NBShaderPassFeatureCatalog.CameraOpaqueDistortPassId, "passes", "Camera Opaque Distort Pass", 1, NBShaderFeaturePerformanceCost.Ultra, "Renders the camera opaque texture screen distort pass."),
            Pass(NBShaderPassFeatureCatalog.DeferredDistortPassId, "passes", "Deferred Distort Pass", 1, NBShaderFeaturePerformanceCost.Ultra, "Renders the deferred screen distort pass."),
            Pass(NBShaderPassFeatureCatalog.DepthOnlyPassId, "passes", "Depth Only Pass", 1, NBShaderFeaturePerformanceCost.Medium, "Writes material depth for URP depth prepass usage."),
            Pass(NBShaderPassFeatureCatalog.ShadowCasterPassId, "passes", "Shadow Caster Pass", 1, NBShaderFeaturePerformanceCost.High, "Renders the material into shadow maps."),
            Pass(NBShaderPassFeatureCatalog.Universal2DPassId, "passes", "Universal 2D Pass", 1, NBShaderFeaturePerformanceCost.Medium, "Allows the material to render through URP 2D renderer.")
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
            return new NBShaderFeatureLevelRow(
                NBShaderFeatureLevelRowKind.Group,
                key,
                parentKey,
                null,
                null,
                labelFallback,
                NBShaderFeaturePerformanceCost.Low,
                null,
                depth);
        }

        private static NBShaderFeatureLevelRow Keyword(
            string keyword,
            string parentKey,
            string labelFallback,
            int depth,
            NBShaderFeaturePerformanceCost performanceCost,
            string effectFallback)
        {
            return new NBShaderFeatureLevelRow(
                NBShaderFeatureLevelRowKind.Keyword,
                keyword,
                parentKey,
                keyword,
                null,
                labelFallback,
                performanceCost,
                effectFallback,
                depth);
        }

        private static NBShaderFeatureLevelRow Pass(
            string passFeatureId,
            string parentKey,
            string labelFallback,
            int depth,
            NBShaderFeaturePerformanceCost performanceCost,
            string effectFallback)
        {
            return new NBShaderFeatureLevelRow(
                NBShaderFeatureLevelRowKind.Pass,
                passFeatureId,
                parentKey,
                null,
                passFeatureId,
                labelFallback,
                performanceCost,
                effectFallback,
                depth);
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
