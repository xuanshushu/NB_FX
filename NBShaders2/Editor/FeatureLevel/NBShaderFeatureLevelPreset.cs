using System;
using System.Collections.Generic;
using NBShader;
using UnityEditor;
using UnityEngine;

namespace NBShaders2.Editor.FeatureLevel
{
    [CreateAssetMenu(fileName = "NBShaderDefaultFeatureLevels", menuName = "NBShader/Feature Level Preset")]
    public sealed class NBShaderFeatureLevelPreset : ScriptableObject
    {
        [SerializeField] private NBShaderFeatureTierKeywordSet[] m_TierKeywordSets = new NBShaderFeatureTierKeywordSet[0];

        public NBShaderFeatureTierKeywordSet[] CreateTierKeywordSets()
        {
            var result = new NBShaderFeatureTierKeywordSet[4];
            for (var i = 0; i < result.Length; i++)
            {
                var tier = (NBShaderFeatureTier)i;
                result[i] = new NBShaderFeatureTierKeywordSet
                {
                    tier = tier,
                    allowedKeywords = GetAllowedKeywords(tier)
                };
            }

            return result;
        }

        public string[] GetAllowedKeywords(NBShaderFeatureTier tier)
        {
            if (m_TierKeywordSets == null)
                return new string[0];

            for (var i = 0; i < m_TierKeywordSets.Length; i++)
            {
                var set = m_TierKeywordSets[i];
                if (set != null && set.tier == tier)
                    return SanitizeKeywords(set.allowedKeywords);
            }

            return new string[0];
        }

        private static string[] SanitizeKeywords(string[] keywords)
        {
            if (keywords == null || keywords.Length == 0)
                return new string[0];

            var allowed = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < keywords.Length; i++)
            {
                var keyword = keywords[i];
                if (NBShaderFeatureLevelCatalog.IsManagedKeyword(keyword))
                    allowed.Add(keyword);
            }

            var result = new List<string>();
            var catalog = NBShaderFeatureLevelCatalog.ManagedKeywords;
            for (var i = 0; i < catalog.Length; i++)
            {
                if (allowed.Contains(catalog[i]))
                    result.Add(catalog[i]);
            }

            return result.ToArray();
        }
    }

    internal static class NBShaderFeatureLevelPresetLoader
    {
        public const string DefaultPresetAssetPath =
            "Packages/com.xuanxuan.nb.fx/NBShaders2/Editor/FeatureLevel/LevelAssets/NBShaderDefaultFeatureLevels.asset";

        public static NBShaderFeatureTierKeywordSet[] LoadDefaultTierKeywordSets()
        {
            var preset = AssetDatabase.LoadAssetAtPath<NBShaderFeatureLevelPreset>(DefaultPresetAssetPath);
            if (preset != null)
                return preset.CreateTierKeywordSets();

            Debug.LogWarning(
                "NBShader default feature level preset was not found at " +
                DefaultPresetAssetPath +
                ". Falling back to allowing all Catalog keywords for every tier.");
            return CreateAllowAllTierKeywordSets();
        }

        public static string[] LoadDefaultAllowedKeywords(NBShaderFeatureTier tier)
        {
            var sets = LoadDefaultTierKeywordSets();
            for (var i = 0; i < sets.Length; i++)
            {
                var set = sets[i];
                if (set != null && set.tier == tier)
                    return set.allowedKeywords ?? new string[0];
            }

            return new string[0];
        }

        private static NBShaderFeatureTierKeywordSet[] CreateAllowAllTierKeywordSets()
        {
            var result = new NBShaderFeatureTierKeywordSet[4];
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = new NBShaderFeatureTierKeywordSet
                {
                    tier = (NBShaderFeatureTier)i,
                    allowedKeywords = (string[])NBShaderFeatureLevelCatalog.ManagedKeywords.Clone()
                };
            }

            return result;
        }
    }
}
