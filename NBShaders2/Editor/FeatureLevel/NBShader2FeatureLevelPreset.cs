using System;
using System.Collections.Generic;
using NBShader;
using UnityEditor;
using UnityEngine;

namespace NBShaders2.Editor.FeatureLevel
{
    [CreateAssetMenu(fileName = "NBShader2DefaultFeatureLevels", menuName = "NBShader2/Feature Level Preset")]
    public sealed class NBShader2FeatureLevelPreset : ScriptableObject
    {
        [SerializeField] private NBShader2FeatureTierKeywordSet[] m_TierKeywordSets = new NBShader2FeatureTierKeywordSet[0];

        public NBShader2FeatureTierKeywordSet[] CreateTierKeywordSets()
        {
            var result = new NBShader2FeatureTierKeywordSet[4];
            for (var i = 0; i < result.Length; i++)
            {
                var tier = (NBShader2FeatureTier)i;
                result[i] = new NBShader2FeatureTierKeywordSet
                {
                    tier = tier,
                    allowedKeywords = GetAllowedKeywords(tier)
                };
            }

            return result;
        }

        public string[] GetAllowedKeywords(NBShader2FeatureTier tier)
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
                if (NBShader2FeatureLevelCatalog.IsManagedKeyword(keyword))
                    allowed.Add(keyword);
            }

            var result = new List<string>();
            var catalog = NBShader2FeatureLevelCatalog.ManagedKeywords;
            for (var i = 0; i < catalog.Length; i++)
            {
                if (allowed.Contains(catalog[i]))
                    result.Add(catalog[i]);
            }

            return result.ToArray();
        }
    }

    internal static class NBShader2FeatureLevelPresetLoader
    {
        public const string DefaultPresetAssetPath =
            "Packages/com.xuanxuan.nb.fx/NBShaders2/Editor/FeatureLevel/LevelAssets/NBShader2DefaultFeatureLevels.asset";

        public static NBShader2FeatureTierKeywordSet[] LoadDefaultTierKeywordSets()
        {
            var preset = AssetDatabase.LoadAssetAtPath<NBShader2FeatureLevelPreset>(DefaultPresetAssetPath);
            if (preset != null)
                return preset.CreateTierKeywordSets();

            Debug.LogWarning(
                "NBShader2 default feature level preset was not found at " +
                DefaultPresetAssetPath +
                ". Falling back to allowing all Catalog keywords for every tier.");
            return CreateAllowAllTierKeywordSets();
        }

        public static string[] LoadDefaultAllowedKeywords(NBShader2FeatureTier tier)
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

        private static NBShader2FeatureTierKeywordSet[] CreateAllowAllTierKeywordSets()
        {
            var result = new NBShader2FeatureTierKeywordSet[4];
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = new NBShader2FeatureTierKeywordSet
                {
                    tier = (NBShader2FeatureTier)i,
                    allowedKeywords = (string[])NBShader2FeatureLevelCatalog.ManagedKeywords.Clone()
                };
            }

            return result;
        }
    }
}
