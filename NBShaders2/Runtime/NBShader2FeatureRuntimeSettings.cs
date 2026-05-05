using System;
using System.Collections.Generic;
using UnityEngine;

namespace NBShader
{
    /// <summary>
    /// Runtime settings for NBShader2 feature tiering. Store one asset named
    /// "NBShader2FeatureRuntimeSettings" in a Resources folder so runtime code can load it.
    /// If the asset cannot be loaded, <see cref="NBShader2FeatureRuntime"/> falls back to Ultra with all
    /// catalog keywords allowed.
    /// </summary>
    [CreateAssetMenu(fileName = NBShader2FeatureCatalog.RuntimeSettingsResourcePath, menuName = "NBShader2/Feature Runtime Settings")]
    public sealed class NBShader2FeatureRuntimeSettings : ScriptableObject
    {
        [Serializable]
        public sealed class QualityTierMapping
        {
            public string qualityName;
            public NBShader2FeatureTier tier = NBShader2FeatureTier.Ultra;
        }

        [Header("Allowed Raw Shader Feature Keywords")]
        public string[] lowAllowedKeywords = CloneCatalogKeywords();
        public string[] mediumAllowedKeywords = CloneCatalogKeywords();
        public string[] highAllowedKeywords = CloneCatalogKeywords();
        public string[] ultraAllowedKeywords = CloneCatalogKeywords();

        [Header("QualitySettings Name To Tier")]
        public QualityTierMapping[] qualityTierMappings = new QualityTierMapping[0];

        public string[] GetAllowedKeywords(NBShader2FeatureTier tier)
        {
            switch (tier)
            {
                case NBShader2FeatureTier.Low:
                    return lowAllowedKeywords ?? NBShader2FeatureCatalog.RawKeywords;
                case NBShader2FeatureTier.Medium:
                    return mediumAllowedKeywords ?? NBShader2FeatureCatalog.RawKeywords;
                case NBShader2FeatureTier.High:
                    return highAllowedKeywords ?? NBShader2FeatureCatalog.RawKeywords;
                default:
                    return ultraAllowedKeywords ?? NBShader2FeatureCatalog.RawKeywords;
            }
        }

        public bool TryGetTierForQualityName(string qualityName, out NBShader2FeatureTier tier)
        {
            tier = NBShader2FeatureTier.Ultra;
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

        internal HashSet<string> BuildAllowedSet(NBShader2FeatureTier tier)
        {
            string[] keywords = GetAllowedKeywords(tier);
            HashSet<string> allowed = new HashSet<string>();
            for (int i = 0; i < keywords.Length; i++)
            {
                string keyword = keywords[i];
                if (NBShader2FeatureCatalog.IsManagedKeyword(keyword))
                {
                    allowed.Add(keyword);
                }
            }

            return allowed;
        }

        private static string[] CloneCatalogKeywords()
        {
            return (string[])NBShader2FeatureCatalog.RawKeywords.Clone();
        }
    }
}
