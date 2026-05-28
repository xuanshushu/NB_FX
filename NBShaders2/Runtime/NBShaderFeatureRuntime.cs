using System.Collections.Generic;
using UnityEngine;

namespace NBShader
{
    /// <summary>
    /// Runtime API for applying NBShader feature tiers to materials.
    /// </summary>
    public static class NBShaderFeatureRuntime
    {
        /// <summary>
        /// Applies an NBShader feature tier to one material in place. Only materials whose shader name is
        /// "Effects/NBShader" are processed. Managed Catalog keywords and shader passes are derived from
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
        /// Applies an NBShader feature tier to multiple materials in place. Only materials whose shader name is
        /// "Effects/NBShader" are processed. Catalog-external keywords are left unchanged unless they are
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

            foreach (Material material in materials)
            {
                ApplyTierInternal(material, settings, tier);
            }
        }

        private static void ApplyTierInternal(Material material, NBShaderFeatureRuntimeSettings settings, NBShaderFeatureTier? tier)
        {
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
    }
}
