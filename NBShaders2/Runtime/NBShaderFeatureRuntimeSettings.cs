using System;
using System.Collections.Generic;
using UnityEngine;

namespace NBShader
{
    /// <summary>
    /// Optional runtime settings for NBShader feature tiering. User projects own asset creation and
    /// runtime loading; pass the loaded asset to <see cref="NBShaderFeatureRuntime"/> when tier gating is required.
    /// </summary>
    [CreateAssetMenu(fileName = NBShaderFeatureCatalog.RuntimeSettingsAssetName, menuName = "NBShader/Feature Runtime Settings")]
    public sealed class NBShaderFeatureRuntimeSettings : ScriptableObject
    {
        [Serializable]
        public sealed class QualityTierMapping
        {
            public string qualityName;
            public NBShaderFeatureTier tier = NBShaderFeatureTier.Ultra;
        }

        [Header("Allowed Raw Shader Feature Keywords")]
        public string[] lowAllowedKeywords = CloneCatalogKeywords();
        public string[] mediumAllowedKeywords = CloneCatalogKeywords();
        public string[] highAllowedKeywords = CloneCatalogKeywords();
        public string[] ultraAllowedKeywords = CloneCatalogKeywords();

        [Header("Allowed Shader Pass Features")]
        public string[] lowAllowedPassFeatures = CloneCatalogPassFeatures();
        public string[] mediumAllowedPassFeatures = CloneCatalogPassFeatures();
        public string[] highAllowedPassFeatures = CloneCatalogPassFeatures();
        public string[] ultraAllowedPassFeatures = CloneCatalogPassFeatures();

        [Header("QualitySettings Name To Tier")]
        public QualityTierMapping[] qualityTierMappings = new QualityTierMapping[0];

        public string[] GetAllowedKeywords(NBShaderFeatureTier tier)
        {
            switch (tier)
            {
                case NBShaderFeatureTier.Low:
                    return lowAllowedKeywords ?? NBShaderFeatureCatalog.RawKeywords;
                case NBShaderFeatureTier.Medium:
                    return mediumAllowedKeywords ?? NBShaderFeatureCatalog.RawKeywords;
                case NBShaderFeatureTier.High:
                    return highAllowedKeywords ?? NBShaderFeatureCatalog.RawKeywords;
                default:
                    return ultraAllowedKeywords ?? NBShaderFeatureCatalog.RawKeywords;
            }
        }

        public string[] GetAllowedPassFeatures(NBShaderFeatureTier tier)
        {
            switch (tier)
            {
                case NBShaderFeatureTier.Low:
                    return lowAllowedPassFeatures ?? NBShaderPassFeatureCatalog.RawPassFeatureIds;
                case NBShaderFeatureTier.Medium:
                    return mediumAllowedPassFeatures ?? NBShaderPassFeatureCatalog.RawPassFeatureIds;
                case NBShaderFeatureTier.High:
                    return highAllowedPassFeatures ?? NBShaderPassFeatureCatalog.RawPassFeatureIds;
                default:
                    return ultraAllowedPassFeatures ?? NBShaderPassFeatureCatalog.RawPassFeatureIds;
            }
        }

        public bool TryGetTierForQualityName(string qualityName, out NBShaderFeatureTier tier)
        {
            tier = NBShaderFeatureTier.Ultra;
            if (string.IsNullOrEmpty(qualityName) || qualityTierMappings == null)
            {
                return false;
            }

            for (int i = 0; i < qualityTierMappings.Length; i++)
            {
                QualityTierMapping mapping = qualityTierMappings[i];
                if (mapping != null && string.Equals(mapping.qualityName, qualityName, StringComparison.OrdinalIgnoreCase))
                {
                    tier = mapping.tier;
                    return true;
                }
            }

            return false;
        }

        internal HashSet<string> BuildAllowedSet(NBShaderFeatureTier tier)
        {
            string[] keywords = GetAllowedKeywords(tier);
            HashSet<string> allowed = new HashSet<string>();
            for (int i = 0; i < keywords.Length; i++)
            {
                string keyword = keywords[i];
                if (NBShaderFeatureCatalog.IsManagedKeyword(keyword))
                {
                    allowed.Add(keyword);
                }
            }

            return allowed;
        }

        internal HashSet<string> BuildAllowedPassFeatureSet(NBShaderFeatureTier tier)
        {
            string[] passFeatures = GetAllowedPassFeatures(tier);
            HashSet<string> allowed = new HashSet<string>();
            for (int i = 0; i < passFeatures.Length; i++)
            {
                string passFeature = passFeatures[i];
                if (NBShaderPassFeatureCatalog.IsManagedPassFeature(passFeature))
                {
                    allowed.Add(passFeature);
                }
            }

            return allowed;
        }

        private static string[] CloneCatalogKeywords()
        {
            return (string[])NBShaderFeatureCatalog.RawKeywords.Clone();
        }

        private static string[] CloneCatalogPassFeatures()
        {
            return (string[])NBShaderPassFeatureCatalog.RawPassFeatureIds.Clone();
        }
    }
}
