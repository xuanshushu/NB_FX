using System.Collections.Generic;
using UnityEngine;

namespace NBShader
{
    /// <summary>
    /// Runtime API for applying NBShader2 feature tiers to materials.
    /// </summary>
    public static class NBShader2FeatureRuntime
    {
        private static NBShader2FeatureRuntimeSettings s_Settings;
        private static bool s_SettingsLoaded;

        /// <summary>
        /// Applies an NBShader2 feature tier to one material in place. Only materials whose shader name is
        /// "Effects/NBShader2" are processed. The method disables Catalog keywords that are not allowed by
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
        public static void ApplyTier(Material material, NBShader2FeatureTier? tier = null)
        {
            if (!IsNBShader2Material(material))
            {
                return;
            }

            NBShader2FeatureRuntimeSettings settings = GetSettings();
            NBShader2FeatureTier resolvedTier = tier.HasValue ? tier.Value : ResolveTierFromQuality(settings);
            HashSet<string> allowed = settings != null
                ? settings.BuildAllowedSet(resolvedTier)
                : NBShader2FeatureCatalog.BuildDefaultAllowedSet(resolvedTier);

            string[] materialKeywords = material.shaderKeywords;
            if (materialKeywords == null)
            {
                return;
            }

            for (int i = 0; i < materialKeywords.Length; i++)
            {
                string keyword = materialKeywords[i];
                if (NBShader2FeatureCatalog.IsManagedKeyword(keyword) && !allowed.Contains(keyword))
                {
                    material.DisableKeyword(keyword);
                }
            }
        }

        /// <summary>
        /// Applies an NBShader2 feature tier to multiple materials in place. Only materials whose shader name is
        /// "Effects/NBShader2" are processed. Catalog-external keywords are left unchanged.
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
        public static void ApplyTier(IEnumerable<Material> materials, NBShader2FeatureTier? tier = null)
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

        private static bool IsNBShader2Material(Material material)
        {
            return material != null
                && material.shader != null
                && material.shader.name == NBShader2FeatureCatalog.ShaderName;
        }

        private static NBShader2FeatureRuntimeSettings GetSettings()
        {
            if (!s_SettingsLoaded)
            {
                s_Settings = Resources.Load<NBShader2FeatureRuntimeSettings>(NBShader2FeatureCatalog.RuntimeSettingsResourcePath);
                s_SettingsLoaded = true;
            }

            return s_Settings;
        }

        private static NBShader2FeatureTier ResolveTierFromQuality(NBShader2FeatureRuntimeSettings settings)
        {
            if (settings == null)
            {
                return NBShader2FeatureTier.Ultra;
            }

            string qualityName = GetCurrentQualityName();
            NBShader2FeatureTier tier;
            return settings.TryGetTierForQualityName(qualityName, out tier) ? tier : NBShader2FeatureTier.Ultra;
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
    }
}
