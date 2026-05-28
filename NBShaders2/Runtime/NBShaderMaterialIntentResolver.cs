using System;
using System.Collections.Generic;
using UnityEngine;

namespace NBShader
{
    /// <summary>
    /// Resolves the serialized material feature intent into tier-filtered keywords and pass requirements.
    /// This class is read-only: it never changes material properties, keywords, or shader pass state.
    /// </summary>
    internal static class NBShaderMaterialIntentResolver
    {
        private static readonly KeywordToggleBinding[] ToggleKeywordBindings =
        {
            new KeywordToggleBinding("_SoftParticlesEnabled", "_SOFTPARTICLES_ON"),
            new KeywordToggleBinding("_DistanceFade_Toggle", "_DISTANCE_FADE"),
            new KeywordToggleBinding("_StencilWithoutPlayerToggle", "_STENCIL_WITHOUT_PLAYER"),
            new KeywordToggleBinding("_Mask_Toggle", "_MASKMAP_ON"),
            new KeywordToggleBinding("_noisemapEnabled", "_NOISEMAP"),
            new KeywordToggleBinding("_EmissionEnabled", "_EMISSION"),
            new KeywordToggleBinding("_ColorBlendMap_Toggle", "_COLORMAPBLEND"),
            new KeywordToggleBinding("_RampColorToggle", "_COLOR_RAMP"),
            new KeywordToggleBinding("_Dissolve_Toggle", "_DISSOLVE"),
            new KeywordToggleBinding("_ProgramNoise_Toggle", "_PROGRAM_NOISE"),
            new KeywordToggleBinding("_SharedUVToggle", "_SHARED_UV"),
            new KeywordToggleBinding("_fresnelEnabled", "_FRESNEL"),
            new KeywordToggleBinding("_ParallaxMapping_Toggle", "_PARALLAX_MAPPING"),
            new KeywordToggleBinding("_VertexOffset_Toggle", "_VERTEX_OFFSET"),
            new KeywordToggleBinding("_FlipbookBlending", "_FLIPBOOKBLENDING_ON"),
            new KeywordToggleBinding("_BumpMapToggle", "_NORMALMAP"),
            new KeywordToggleBinding("_MatCapToggle", "_MATCAP"),
            new KeywordToggleBinding("_BlinnPhongSpecularToggle", "_SPECULAR_COLOR"),
            new KeywordToggleBinding("_SixWayColorAbsorptionToggle", "VFX_SIX_WAY_ABSORPTION"),
            new KeywordToggleBinding("_DepthDecal_Toggle", "_DEPTH_DECAL"),
            new KeywordToggleBinding("_DepthOutline_Toggle", "_DEPTH_OUTLINE"),
            new KeywordToggleBinding("_OverrideZ_Toggle", "_OVERRIDE_Z"),
            new KeywordToggleBinding("_Mask2_Toggle", "_MASKMAP2_ON"),
            new KeywordToggleBinding("_Mask3_Toggle", "_MASKMAP3_ON"),
            new KeywordToggleBinding("_noiseMaskMap_Toggle", "_NOISE_MASKMAP"),
            new KeywordToggleBinding("_Distortion_Choraticaberrat_Toggle", "_CHROMATIC_ABERRATION"),
            new KeywordToggleBinding("_DissolveMask_Toggle", "_DISSOLVE_MASK"),
            new KeywordToggleBinding("_Dissolve_useRampMap_Toggle", "_DISSOLVE_RAMP"),
            new KeywordToggleBinding("_ProgramNoise_Simple_Toggle", "_PROGRAM_NOISE_SIMPLE"),
            new KeywordToggleBinding("_ProgramNoise_Voronoi_Toggle", "_PROGRAM_NOISE_VORONOI"),
            new KeywordToggleBinding("_VertexOffset_Mask_Toggle", "_VERTEX_OFFSET_MASKMAP"),
            new KeywordToggleBinding("_NB_Debug_Dissolve", "NB_DEBUG_DISSOLVE"),
            new KeywordToggleBinding("_NB_Debug_Distort", "NB_DEBUG_DISTORT"),
            new KeywordToggleBinding("_NB_Debug_Fresnel", "NB_DEBUG_FRESNEL"),
            new KeywordToggleBinding("_NB_Debug_Mask", "NB_DEBUG_MASK"),
            new KeywordToggleBinding("_NB_Debug_PNoise", "NB_DEBUG_PNOISE"),
            new KeywordToggleBinding("_NB_Debug_VertexOffset", "NB_DEBUG_VERTEX_OFFSET")
        };

        private static readonly string[] HoudiniVatKeywords =
        {
            "_HOUDINI_VAT_SOFTBODY",
            "_HOUDINI_VAT_RIGIDBODY",
            "_HOUDINI_VAT_DYNAMIC_REMESH",
            "_HOUDINI_VAT_PARTICLE_SPRITE"
        };

        private static readonly string[] TyflowVatKeywords =
        {
            "_TYFLOW_VAT_ABSOLUTE",
            "_TYFLOW_VAT_RELATIVE",
            "_TYFLOW_VAT_SKIN_R",
            "_TYFLOW_VAT_SKIN_PR",
            "_TYFLOW_VAT_SKIN_PRSAVE",
            "_TYFLOW_VAT_SKIN_PRSXYZ"
        };

        public static bool IsNBShaderMaterial(Material material)
        {
            return material != null &&
                   material.shader != null &&
                   material.shader.name == NBShaderFeatureCatalog.ShaderName;
        }

        public static NBShaderMaterialIntentResult Resolve(Material material, NBShaderFeatureTier tier)
        {
            return Resolve(
                material,
                tier,
                NBShaderFeatureCatalog.RawKeywords,
                NBShaderPassFeatureCatalog.GetDefaultAllowedPassFeatures(tier));
        }

        public static NBShaderMaterialIntentResult Resolve(
            Material material,
            NBShaderFeatureTier tier,
            IEnumerable<string> allowedManagedKeywords,
            IEnumerable<string> allowedPassFeatureIds)
        {
            var allowedKeywords = BuildAllowedKeywordSet(allowedManagedKeywords);
            var allowedPassFeatures = BuildAllowedPassFeatureSet(allowedPassFeatureIds);
            var intendedKeywords = ResolveIntendedManagedKeywords(material);
            var effectiveKeywords = FilterAllowedKeywords(intendedKeywords, allowedKeywords);
            ApplyKeywordDependencies(effectiveKeywords);
            var strippedKeywords = BuildDifference(intendedKeywords, effectiveKeywords);
            var passIntents = ResolvePassIntents(material, effectiveKeywords, allowedPassFeatures);

            return new NBShaderMaterialIntentResult(
                material,
                tier,
                ToCatalogOrderedArray(intendedKeywords),
                ToCatalogOrderedArray(effectiveKeywords),
                ToCatalogOrderedArray(strippedKeywords),
                passIntents);
        }

        private static HashSet<string> ResolveIntendedManagedKeywords(Material material)
        {
            var keywords = new HashSet<string>(StringComparer.Ordinal);
            if (!IsNBShaderMaterial(material))
                return keywords;

            for (var i = 0; i < ToggleKeywordBindings.Length; i++)
            {
                var binding = ToggleKeywordBindings[i];
                if (GetFloat(material, binding.propertyName, 0f) > 0.5f)
                    AddManagedKeyword(keywords, binding.keyword);
            }

            int meshMode = GetInt(material, "_MeshSourceMode", 0);
            int transparentMode = GetInt(material, "_TransparentMode", NBShaderMaterialIntentProtocol.TransparentTransparent);
            if (transparentMode == NBShaderMaterialIntentProtocol.TransparentCutOff)
                AddManagedKeyword(keywords, "_ALPHATEST_ON");

            int blendMode = ResolveBlendMode(material, transparentMode);
            switch (blendMode)
            {
                case NBShaderMaterialIntentProtocol.BlendPremultiply:
                case NBShaderMaterialIntentProtocol.BlendAdditive:
                    AddManagedKeyword(keywords, "_ALPHAPREMULTIPLY_ON");
                    break;
                case NBShaderMaterialIntentProtocol.BlendMultiply:
                    AddManagedKeyword(keywords, "_ALPHAMODULATE_ON");
                    break;
            }

            if (IsParticleMeshSource(meshMode))
                AddManagedKeyword(keywords, "_PARCUSTOMDATA_ON");

            switch (GetInt(material, "_TimeMode", 0))
            {
                case NBShaderMaterialIntentProtocol.TimeUnscaled:
                    AddManagedKeyword(keywords, "_UNSCALETIME");
                    break;
                case NBShaderMaterialIntentProtocol.TimeScriptable:
                    AddManagedKeyword(keywords, "_SCRIPTABLETIME");
                    break;
            }

            switch (GetInt(material, "_FxLightMode", NBShaderMaterialIntentProtocol.FxLightUnlit))
            {
                case NBShaderMaterialIntentProtocol.FxLightUnlit:
                    AddManagedKeyword(keywords, "_FX_LIGHT_MODE_UNLIT");
                    break;
                case NBShaderMaterialIntentProtocol.FxLightBlinnPhong:
                    AddManagedKeyword(keywords, "_FX_LIGHT_MODE_BLINN_PHONG");
                    break;
                case NBShaderMaterialIntentProtocol.FxLightHalfLambert:
                    AddManagedKeyword(keywords, "_FX_LIGHT_MODE_HALF_LAMBERT");
                    break;
                case NBShaderMaterialIntentProtocol.FxLightPbr:
                    AddManagedKeyword(keywords, "_FX_LIGHT_MODE_PBR");
                    break;
                case NBShaderMaterialIntentProtocol.FxLightSixWay:
                    AddManagedKeyword(keywords, "_FX_LIGHT_MODE_SIX_WAY");
                    break;
            }

            if (GetInt(material, "_DistortMode", 0) == 1)
                AddManagedKeyword(keywords, "_DISTORT_REFRACTION");

            if (GetInt(material, "_RampColorSourceMode", 0) == 1)
                AddManagedKeyword(keywords, "_COLOR_RAMP_MAP");

            if (GetInt(material, "_DissolveRampSourceMode", 0) == 1 &&
                GetFloat(material, "_Dissolve_useRampMap_Toggle", 0f) > 0.5f)
            {
                AddManagedKeyword(keywords, "_DISSOLVE_RAMP_MAP");
            }

            if (!IsUIEffectMeshSource(meshMode) && GetInt(material, "_ScreenDistortModeToggle", 0) != 0)
                AddManagedKeyword(keywords, "_SCREEN_DISTORT_MODE");

            ResolveVatKeywords(material, keywords);

            return keywords;
        }

        private static void ResolveVatKeywords(Material material, HashSet<string> keywords)
        {
            if (GetFloat(material, "_VAT_Toggle", 0f) <= 0.5f)
                return;

            keywords.Remove("_FLIPBOOKBLENDING_ON");
            AddManagedKeyword(keywords, "_VAT");

            if (GetInt(material, "_VATMode", 0) == NBShaderMaterialIntentProtocol.VatTyflow)
            {
                AddManagedKeyword(keywords, "_VAT_TYFLOW");
                AddIndexedKeyword(keywords, TyflowVatKeywords, GetInt(material, "_TyFlowVATSubMode", 0));
            }
            else
            {
                AddManagedKeyword(keywords, "_VAT_HOUDINI");
                AddIndexedKeyword(keywords, HoudiniVatKeywords, GetInt(material, "_HoudiniVATSubMode", 0));
            }
        }

        private static NBShaderPassIntent[] ResolvePassIntents(
            Material material,
            HashSet<string> effectiveKeywords,
            HashSet<string> allowedPassFeatures)
        {
            var result = new List<NBShaderPassIntent>(7);
            bool isNBShaderMaterial = IsNBShaderMaterial(material);
            int meshMode = GetInt(material, "_MeshSourceMode", 0);
            int transparentMode = GetInt(material, "_TransparentMode", NBShaderMaterialIntentProtocol.TransparentTransparent);
            bool uiEffect = IsUIEffectMeshSource(meshMode);
            int screenDistortMode = isNBShaderMaterial ? GetInt(material, "_ScreenDistortModeToggle", 0) : 0;
            bool screenDistortKeywordEnabled = effectiveKeywords.Contains("_SCREEN_DISTORT_MODE");
            bool screenDistortEnabled = !uiEffect && screenDistortMode != 0 && screenDistortKeywordEnabled;
            bool disableMainPass = GetFloat(material, "_DisableMainPassToggle", 0f) > 0.5f;
            bool mainPassEnabled = isNBShaderMaterial && (!screenDistortEnabled || !disableMainPass);

            result.Add(NBShaderPassIntent.CreateCore(
                NBShaderPassFeatureCatalog.MainForwardPassName,
                mainPassEnabled,
                mainPassEnabled ? "main pass" : "disabled by screen distort material intent"));

            AddManagedPass(
                result,
                NBShaderPassFeatureCatalog.BackFirstPassId,
                !uiEffect &&
                transparentMode == NBShaderMaterialIntentProtocol.TransparentTransparent &&
                GetFloat(material, "_BackFirstPassToggle", 0f) > 0.5f,
                allowedPassFeatures,
                "back first material toggle");

            AddManagedPass(
                result,
                NBShaderPassFeatureCatalog.CameraOpaqueDistortPassId,
                screenDistortEnabled && screenDistortMode == 2,
                allowedPassFeatures,
                "camera opaque screen distort material mode");

            AddManagedPass(
                result,
                NBShaderPassFeatureCatalog.DeferredDistortPassId,
                screenDistortEnabled && screenDistortMode == 1,
                allowedPassFeatures,
                "deferred screen distort material mode");

            AddManagedPass(
                result,
                NBShaderPassFeatureCatalog.DepthOnlyPassId,
                !uiEffect && ResolveZWrite(material, transparentMode),
                allowedPassFeatures,
                "depth write material state");

            AddManagedPass(
                result,
                NBShaderPassFeatureCatalog.ShadowCasterPassId,
                !uiEffect &&
                GetFloat(material, "_AffectsShadows", 0f) > 0.5f &&
                IsKnownTransparentMode(GetInt(material, "_TransparentMode", NBShaderMaterialIntentProtocol.TransparentTransparent)),
                allowedPassFeatures,
                "shadow material state");

            AddManagedPass(
                result,
                NBShaderPassFeatureCatalog.Universal2DPassId,
                isNBShaderMaterial,
                allowedPassFeatures,
                "URP 2D renderer pass");

            return result.ToArray();
        }

        private static void AddManagedPass(
            List<NBShaderPassIntent> result,
            string featureId,
            bool enabledByMaterial,
            HashSet<string> allowedPassFeatures,
            string reason)
        {
            NBShaderPassFeatureInfo feature;
            if (!NBShaderPassFeatureCatalog.TryGetPassFeature(featureId, out feature))
                return;

            bool allowedByTier = allowedPassFeatures == null || allowedPassFeatures.Contains(featureId);
            result.Add(new NBShaderPassIntent(
                featureId,
                feature.passName,
                enabledByMaterial,
                allowedByTier,
                enabledByMaterial && allowedByTier,
                reason));
        }

        private static HashSet<string> BuildAllowedKeywordSet(IEnumerable<string> allowedManagedKeywords)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            if (allowedManagedKeywords == null)
            {
                for (var i = 0; i < NBShaderFeatureCatalog.RawKeywords.Length; i++)
                    result.Add(NBShaderFeatureCatalog.RawKeywords[i]);
                return result;
            }

            foreach (var keyword in allowedManagedKeywords)
            {
                if (NBShaderFeatureCatalog.IsManagedKeyword(keyword))
                    result.Add(keyword);
            }

            return result;
        }

        private static HashSet<string> BuildAllowedPassFeatureSet(IEnumerable<string> allowedPassFeatureIds)
        {
            if (allowedPassFeatureIds == null)
                return null;

            var result = new HashSet<string>(StringComparer.Ordinal);
            foreach (var featureId in allowedPassFeatureIds)
            {
                if (NBShaderPassFeatureCatalog.IsManagedPassFeature(featureId))
                    result.Add(featureId);
            }

            return result;
        }

        private static HashSet<string> FilterAllowedKeywords(HashSet<string> source, HashSet<string> allowed)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            foreach (var keyword in source)
            {
                if (allowed.Contains(keyword))
                    result.Add(keyword);
            }

            return result;
        }

        private static void ApplyKeywordDependencies(HashSet<string> keywords)
        {
            RemoveIfParentMissing(keywords, "_MASKMAP_ON", "_MASKMAP2_ON", "_MASKMAP3_ON", "NB_DEBUG_MASK");
            RemoveIfParentMissing(keywords, "_NOISEMAP", "_NOISE_MASKMAP", "NB_DEBUG_DISTORT", "_SCREEN_DISTORT_MODE", "_DISTORT_REFRACTION");
            RemoveIfParentMissing(keywords, "_COLOR_RAMP", "_COLOR_RAMP_MAP");
            RemoveIfParentMissing(keywords, "_DISSOLVE", "_DISSOLVE_MASK", "_DISSOLVE_RAMP", "NB_DEBUG_DISSOLVE");
            RemoveIfParentMissing(keywords, "_DISSOLVE_RAMP", "_DISSOLVE_RAMP_MAP");
            RemoveIfParentMissing(keywords, "_PROGRAM_NOISE", "_PROGRAM_NOISE_SIMPLE", "_PROGRAM_NOISE_VORONOI", "NB_DEBUG_PNOISE");
            RemoveIfParentMissing(keywords, "_FRESNEL", "NB_DEBUG_FRESNEL");
            RemoveIfParentMissing(keywords, "_VERTEX_OFFSET", "_VERTEX_OFFSET_MASKMAP", "NB_DEBUG_VERTEX_OFFSET");
            RemoveIfParentMissing(keywords, "_VAT", "_VAT_HOUDINI", "_VAT_TYFLOW");
            RemoveIfParentMissing(keywords, "_VAT_HOUDINI", HoudiniVatKeywords);
            RemoveIfParentMissing(keywords, "_VAT_TYFLOW", TyflowVatKeywords);
            RemoveIfParentMissing(keywords, "_FX_LIGHT_MODE_SIX_WAY", "VFX_SIX_WAY_ABSORPTION");
        }

        private static void RemoveIfParentMissing(HashSet<string> keywords, string parentKeyword, params string[] dependentKeywords)
        {
            if (keywords.Contains(parentKeyword) || dependentKeywords == null)
                return;

            for (var i = 0; i < dependentKeywords.Length; i++)
                keywords.Remove(dependentKeywords[i]);
        }

        private static HashSet<string> BuildDifference(HashSet<string> source, HashSet<string> retained)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            foreach (var keyword in source)
            {
                if (!retained.Contains(keyword))
                    result.Add(keyword);
            }

            return result;
        }

        private static void AddManagedKeyword(HashSet<string> keywords, string keyword)
        {
            if (NBShaderFeatureCatalog.IsManagedKeyword(keyword))
                keywords.Add(keyword);
        }

        private static void AddIndexedKeyword(HashSet<string> keywords, string[] source, int index)
        {
            if (source == null || index < 0 || index >= source.Length)
                return;

            AddManagedKeyword(keywords, source[index]);
        }

        private static string[] ToCatalogOrderedArray(HashSet<string> keywords)
        {
            if (keywords == null || keywords.Count == 0)
                return new string[0];

            var result = new List<string>();
            for (var i = 0; i < NBShaderFeatureCatalog.RawKeywords.Length; i++)
            {
                var keyword = NBShaderFeatureCatalog.RawKeywords[i];
                if (keywords.Contains(keyword))
                    result.Add(keyword);
            }

            return result.ToArray();
        }

        private static bool IsParticleMeshSource(int mode)
        {
            return mode == NBShaderMaterialIntentProtocol.MeshSourceParticle ||
                   mode == NBShaderMaterialIntentProtocol.MeshSourceUIParticle;
        }

        private static bool IsUIEffectMeshSource(int mode)
        {
            return mode == NBShaderMaterialIntentProtocol.MeshSourceUIEffectRawImage ||
                   mode == NBShaderMaterialIntentProtocol.MeshSourceUIEffectSprite ||
                   mode == NBShaderMaterialIntentProtocol.MeshSourceUIEffectBaseMap ||
                   mode == NBShaderMaterialIntentProtocol.MeshSourceUIParticle;
        }

        private static bool IsKnownTransparentMode(int mode)
        {
            return mode == NBShaderMaterialIntentProtocol.TransparentOpaque ||
                   mode == NBShaderMaterialIntentProtocol.TransparentTransparent ||
                   mode == NBShaderMaterialIntentProtocol.TransparentCutOff;
        }

        private static int ResolveBlendMode(Material material, int transparentMode)
        {
            if (!IsKnownTransparentMode(transparentMode))
                return GetInt(material, "_Blend", 0);

            if (transparentMode == NBShaderMaterialIntentProtocol.TransparentOpaque ||
                transparentMode == NBShaderMaterialIntentProtocol.TransparentCutOff)
                return NBShaderMaterialIntentProtocol.BlendOpaque;

            int blendMode = GetInt(material, "_Blend", 0);
            return blendMode == NBShaderMaterialIntentProtocol.BlendOpaque
                ? NBShaderMaterialIntentProtocol.BlendAlpha
                : blendMode;
        }

        private static bool ResolveZWrite(Material material, int transparentMode)
        {
            bool zWrite;
            if (transparentMode == NBShaderMaterialIntentProtocol.TransparentOpaque ||
                transparentMode == NBShaderMaterialIntentProtocol.TransparentCutOff)
            {
                zWrite = true;
            }
            else if (transparentMode == NBShaderMaterialIntentProtocol.TransparentTransparent)
            {
                zWrite = false;
            }
            else
            {
                zWrite = GetInt(material, "_ZWrite", 0) == 1;
            }

            float forceZWrite = GetFloat(material, "_ForceZWriteToggle", 0f);
            if (forceZWrite > 0.5f && forceZWrite < 1.5f)
                return true;
            if (forceZWrite > 1.5f)
                return false;
            return zWrite;
        }

        private static float GetFloat(Material material, string propertyName, float fallback)
        {
            return material != null && material.HasProperty(propertyName) ? material.GetFloat(propertyName) : fallback;
        }

        private static int GetInt(Material material, string propertyName, int fallback)
        {
            return Mathf.RoundToInt(GetFloat(material, propertyName, fallback));
        }

        private struct KeywordToggleBinding
        {
            public readonly string propertyName;
            public readonly string keyword;

            public KeywordToggleBinding(string propertyName, string keyword)
            {
                this.propertyName = propertyName;
                this.keyword = keyword;
            }
        }
    }

    internal sealed class NBShaderMaterialIntentResult
    {
        public readonly Material material;
        public readonly NBShaderFeatureTier tier;
        public readonly string[] intendedManagedKeywords;
        public readonly string[] effectiveKeywords;
        public readonly string[] strippedManagedKeywords;
        public readonly NBShaderPassIntent[] passes;
        public readonly string[] includedPassNames;
        public readonly string[] strippedPassNames;

        public NBShaderMaterialIntentResult(
            Material material,
            NBShaderFeatureTier tier,
            string[] intendedManagedKeywords,
            string[] effectiveKeywords,
            string[] strippedManagedKeywords,
            NBShaderPassIntent[] passes)
        {
            this.material = material;
            this.tier = tier;
            this.intendedManagedKeywords = intendedManagedKeywords ?? new string[0];
            this.effectiveKeywords = effectiveKeywords ?? new string[0];
            this.strippedManagedKeywords = strippedManagedKeywords ?? new string[0];
            this.passes = passes ?? new NBShaderPassIntent[0];
            includedPassNames = BuildPassNameArray(this.passes, true);
            strippedPassNames = BuildPassNameArray(this.passes, false);
        }

        private static string[] BuildPassNameArray(NBShaderPassIntent[] passes, bool included)
        {
            var result = new List<string>();
            for (var i = 0; i < passes.Length; i++)
            {
                var pass = passes[i];
                if (pass.included == included && !string.IsNullOrEmpty(pass.passName))
                    result.Add(pass.passName);
            }

            return result.ToArray();
        }
    }

    internal sealed class NBShaderPassIntent
    {
        public readonly string featureId;
        public readonly string passName;
        public readonly bool enabledByMaterial;
        public readonly bool allowedByTier;
        public readonly bool included;
        public readonly string reason;

        public NBShaderPassIntent(
            string featureId,
            string passName,
            bool enabledByMaterial,
            bool allowedByTier,
            bool included,
            string reason)
        {
            this.featureId = featureId;
            this.passName = passName;
            this.enabledByMaterial = enabledByMaterial;
            this.allowedByTier = allowedByTier;
            this.included = included;
            this.reason = reason;
        }

        public static NBShaderPassIntent CreateCore(string passName, bool included, string reason)
        {
            return new NBShaderPassIntent(null, passName, included, true, included, reason);
        }
    }
}
