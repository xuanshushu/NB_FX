using System;
using System.Collections.Generic;
using UnityEngine;

namespace NBShader
{
    /// <summary>
    /// Runtime settings for NBShader feature tiering. Store one asset named
    /// "NBShaderFeatureRuntimeSettings" in a Resources folder so runtime code can load it.
    /// If the asset cannot be loaded, <see cref="NBShaderFeatureRuntime"/> falls back to Ultra with all
    /// catalog keywords allowed.
    /// </summary>
    [CreateAssetMenu(fileName = NBShaderFeatureCatalog.RuntimeSettingsResourcePath, menuName = "NBShader/Feature Runtime Settings")]
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

        private static string[] CloneCatalogKeywords()
        {
            return (string[])NBShaderFeatureCatalog.RawKeywords.Clone();
        }
    }
}
