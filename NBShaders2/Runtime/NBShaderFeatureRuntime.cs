using System.Collections.Generic;
using UnityEngine;

namespace NBShader
{
    /// <summary>
    /// Runtime API for applying NBShader feature tiers to materials.
    /// </summary>
    public static class NBShaderFeatureRuntime
    {
        private static NBShaderFeatureRuntimeSettings s_Settings;
        private static bool s_SettingsLoaded;

        /// <summary>
        /// Applies an NBShader feature tier to one material in place. Only materials whose shader name is
        /// "Effects/NBShader" are processed. The method disables Catalog keywords that are not allowed by
        /// the target tier; Catalog-external keywords are left unchanged.
        /// </summary>
        /// <param name="material">Material to process. Null materials are ignored.</param>
        /// <param name="tier">
        /// Target tier. When null, the tier is resolved from the current QualitySettings quality name and the
        /// Runtime Settings mapping; missing mappings fall back to the Ultra tier, and missing settings allow all Catalog keywords.
        /// </param>
        /// <remarks>
        /// If lower-tier shader variants have been stripped from the build, call this API before the material is
        /// used for rendering. Otherwise Unity can request a missing variant and select a similar available variant.
        /// </remarks>
        public static void ApplyTier(Material material, NBShaderFeatureTier? tier = null)
        {
            if (!IsNBShaderMaterial(material))
            {
                return;
            }

            NBShaderFeatureRuntimeSettings settings = GetSettings();
            NBShaderFeatureTier resolvedTier = tier.HasValue ? tier.Value : ResolveTierFromQuality(settings);
            HashSet<string> allowed = settings != null
                ? settings.BuildAllowedSet(resolvedTier)
                : BuildAllowAllCatalogKeywordSet();

            string[] materialKeywords = material.shaderKeywords;
            if (materialKeywords == null)
            {
                return;
            }

            for (int i = 0; i < materialKeywords.Length; i++)
            {
                string keyword = materialKeywords[i];
                if (NBShaderFeatureCatalog.IsManagedKeyword(keyword) && !allowed.Contains(keyword))
                {
                    material.DisableKeyword(keyword);
                }
            }
        }

        /// <summary>
        /// Applies an NBShader feature tier to multiple materials in place. Only materials whose shader name is
        /// "Effects/NBShader" are processed. Catalog-external keywords are left unchanged.
        /// </summary>
        /// <param name="materials">Materials to process. Null collections and null entries are ignored.</param>
        /// <param name="tier">
        /// Target tier. When null, the tier is resolved from the current QualitySettings quality name and the
        /// Runtime Settings mapping; missing mappings fall back to the Ultra tier, and missing settings allow all Catalog keywords.
        /// </param>
        /// <remarks>
        /// If lower-tier shader variants have been stripped from the build, call this API before the materials are
        /// used for rendering. Otherwise Unity can request a missing variant and select a similar available variant.
        /// </remarks>
        public static void ApplyTier(IEnumerable<Material> materials, NBShaderFeatureTier? tier = null)
        {
            if (materials == null)
            {
                return;
            }

            foreach (Material material in materials)
            {
                ApplyTier(material, tier);
            }
        }

        private static bool IsNBShaderMaterial(Material material)
        {
            return material != null
                && material.shader != null
                && material.shader.name == NBShaderFeatureCatalog.ShaderName;
        }

        private static NBShaderFeatureRuntimeSettings GetSettings()
        {
            if (!s_SettingsLoaded)
            {
                s_Settings = Resources.Load<NBShaderFeatureRuntimeSettings>(NBShaderFeatureCatalog.RuntimeSettingsResourcePath);
                s_SettingsLoaded = true;
            }

            return s_Settings;
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
