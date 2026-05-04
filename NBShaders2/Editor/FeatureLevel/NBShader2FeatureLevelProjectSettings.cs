using System;
using System.Collections.Generic;
using NBShader;
using UnityEditor;
using UnityEngine;

namespace NBShaders2.Editor.FeatureLevel
{
    [FilePath("ProjectSettings/NBShader2FeatureLevels.asset", FilePathAttribute.Location.ProjectFolder)]
    public sealed class NBShader2FeatureLevelProjectSettings : ScriptableSingleton<NBShader2FeatureLevelProjectSettings>
    {
        [SerializeField] private NBShader2FeatureTierKeywordSet[] m_TierKeywordSets;
        [SerializeField] private NBShader2QualityTierMapping[] m_QualityTierMappings;
        [SerializeField] private NBShader2BuildStripPolicy m_BuildStripPolicy = NBShader2BuildStripPolicy.Disabled;
        [SerializeField] private NBShader2FeatureTier m_ExplicitTier = NBShader2FeatureTier.Ultra;

        public NBShader2FeatureTierKeywordSet[] tierKeywordSets { get { EnsureInitialized(); return m_TierKeywordSets; } }
        public NBShader2QualityTierMapping[] qualityTierMappings { get { EnsureInitialized(); return m_QualityTierMappings; } }
        public NBShader2BuildStripPolicy buildStripPolicy { get { return m_BuildStripPolicy; } set { m_BuildStripPolicy = value; } }
        public NBShader2FeatureTier explicitTier { get { return m_ExplicitTier; } set { m_ExplicitTier = value; } }

        public void EnsureInitialized()
        {
            if (m_TierKeywordSets == null || m_TierKeywordSets.Length != 4)
                ResetTierKeywordSetsToDefault();
            else
                NormalizeTierKeywordSets();

            if (m_QualityTierMappings == null || m_QualityTierMappings.Length == 0)
                ResetQualityMappingsToDefault();
            else
                NormalizeQualityMappingsWithCurrentQualityLevels();
        }

        public void ResetTierKeywordSetsToDefault()
        {
            m_TierKeywordSets = new NBShader2FeatureTierKeywordSet[4];
            for (var i = 0; i < 4; i++)
            {
                var tier = (NBShader2FeatureTier)i;
                m_TierKeywordSets[i] = new NBShader2FeatureTierKeywordSet
                {
                    tier = tier,
                    allowedKeywords = NBShader2FeatureLevelCatalog.GetDefaultAllowedKeywords(tier)
                };
            }
        }

        public void ResetQualityMappingsToDefault()
        {
            var names = QualitySettings.names;
            if (names == null || names.Length == 0)
                names = new[] { "Default" };

            m_QualityTierMappings = new NBShader2QualityTierMapping[names.Length];
            for (var i = 0; i < names.Length; i++)
            {
                m_QualityTierMappings[i] = new NBShader2QualityTierMapping
                {
                    qualityName = names[i],
                    tier = GuessTierForQualityIndex(i, names.Length)
                };
            }
        }

        public HashSet<string> GetAllowedKeywordSet(NBShader2FeatureTier tier)
        {
            EnsureInitialized();
            var result = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < m_TierKeywordSets.Length; i++)
            {
                var set = m_TierKeywordSets[i];
                if (set == null || set.tier != tier || set.allowedKeywords == null)
                    continue;

                for (var k = 0; k < set.allowedKeywords.Length; k++)
                {
                    var keyword = set.allowedKeywords[k];
                    if (NBShader2FeatureLevelCatalog.IsManagedKeyword(keyword))
                        result.Add(keyword);
                }
            }
            return result;
        }

        public bool IsKeywordAllowed(NBShader2FeatureTier tier, string keyword)
        {
            return GetAllowedKeywordSet(tier).Contains(keyword);
        }

        public void SetKeywordAllowed(NBShader2FeatureTier tier, string keyword, bool allowed)
        {
            EnsureInitialized();
            if (!NBShader2FeatureLevelCatalog.IsManagedKeyword(keyword))
                return;

            NBShader2FeatureTierKeywordSet target = null;
            for (var i = 0; i < m_TierKeywordSets.Length; i++)
            {
                if (m_TierKeywordSets[i] != null && m_TierKeywordSets[i].tier == tier)
                {
                    target = m_TierKeywordSets[i];
                    break;
                }
            }

            if (target == null)
            {
                target = new NBShader2FeatureTierKeywordSet { tier = tier };
                var index = (int)tier;
                if (index >= 0 && index < m_TierKeywordSets.Length)
                    m_TierKeywordSets[index] = target;
            }

            var keywords = BuildSanitizedAllowedSet(target.allowedKeywords);
            if (allowed)
                keywords.Add(keyword);
            else
                keywords.Remove(keyword);

            target.allowedKeywords = ToCatalogOrderedArray(keywords);
        }

        public bool TryGetTierForQualityName(string qualityName, out NBShader2FeatureTier tier)
        {
            EnsureInitialized();
            tier = NBShader2FeatureTier.Ultra;
            if (string.IsNullOrEmpty(qualityName) || m_QualityTierMappings == null)
                return false;

            for (var i = 0; i < m_QualityTierMappings.Length; i++)
            {
                var mapping = m_QualityTierMappings[i];
                if (mapping == null || !string.Equals(mapping.qualityName, qualityName, StringComparison.Ordinal))
                    continue;

                tier = mapping.tier;
                return true;
            }

            return false;
        }

        public string[] GetQualityNamesForTier(NBShader2FeatureTier tier)
        {
            EnsureInitialized();
            if (m_QualityTierMappings == null || m_QualityTierMappings.Length == 0)
                return new string[0];

            var names = new List<string>();
            for (var i = 0; i < m_QualityTierMappings.Length; i++)
            {
                var mapping = m_QualityTierMappings[i];
                if (mapping != null && mapping.tier == tier && !string.IsNullOrEmpty(mapping.qualityName))
                    names.Add(mapping.qualityName);
            }

            return names.ToArray();
        }

        public void MoveQualityToTier(string qualityName, NBShader2FeatureTier tier)
        {
            EnsureInitialized();
            if (string.IsNullOrEmpty(qualityName))
                return;

            for (var i = 0; i < m_QualityTierMappings.Length; i++)
            {
                var mapping = m_QualityTierMappings[i];
                if (mapping == null || !string.Equals(mapping.qualityName, qualityName, StringComparison.Ordinal))
                    continue;

                mapping.tier = tier;
                return;
            }

            var newMappings = new NBShader2QualityTierMapping[m_QualityTierMappings.Length + 1];
            Array.Copy(m_QualityTierMappings, newMappings, m_QualityTierMappings.Length);
            newMappings[newMappings.Length - 1] = new NBShader2QualityTierMapping
            {
                qualityName = qualityName,
                tier = tier
            };
            m_QualityTierMappings = newMappings;
        }

        public HashSet<string> GetQualityMappedUnionAllowedKeywordSet()
        {
            EnsureInitialized();
            var result = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < m_QualityTierMappings.Length; i++)
            {
                if (m_QualityTierMappings[i] == null)
                    continue;
                result.UnionWith(GetAllowedKeywordSet(m_QualityTierMappings[i].tier));
            }
            return result;
        }

        public void SaveProjectSettings()
        {
            EnsureInitialized();
            Save(true);
            NBShader2RuntimeSettingsSynchronizer.SyncFromProjectSettings();
        }

        private static NBShader2FeatureTier GuessTierForQualityIndex(int index, int count)
        {
            if (count <= 1)
                return NBShader2FeatureTier.Ultra;
            var normalized = index / (float)(count - 1);
            if (normalized < 0.25f) return NBShader2FeatureTier.Low;
            if (normalized < 0.50f) return NBShader2FeatureTier.Medium;
            if (normalized < 0.75f) return NBShader2FeatureTier.High;
            return NBShader2FeatureTier.Ultra;
        }

        private void NormalizeTierKeywordSets()
        {
            var normalized = new NBShader2FeatureTierKeywordSet[4];
            for (var tierIndex = 0; tierIndex < normalized.Length; tierIndex++)
            {
                var tier = (NBShader2FeatureTier)tierIndex;
                var existing = FindTierKeywordSet(tier);
                normalized[tierIndex] = new NBShader2FeatureTierKeywordSet
                {
                    tier = tier,
                    allowedKeywords = existing != null
                        ? ToCatalogOrderedArray(BuildSanitizedAllowedSet(existing.allowedKeywords))
                        : NBShader2FeatureLevelCatalog.GetDefaultAllowedKeywords(tier)
                };
            }

            m_TierKeywordSets = normalized;
        }

        private NBShader2FeatureTierKeywordSet FindTierKeywordSet(NBShader2FeatureTier tier)
        {
            if (m_TierKeywordSets == null)
                return null;

            for (var i = 0; i < m_TierKeywordSets.Length; i++)
            {
                var set = m_TierKeywordSets[i];
                if (set != null && set.tier == tier)
                    return set;
            }

            return null;
        }

        private void NormalizeQualityMappingsWithCurrentQualityLevels()
        {
            var names = QualitySettings.names;
            if (names == null || names.Length == 0)
                names = new[] { "Default" };

            var previous = new Dictionary<string, NBShader2FeatureTier>(StringComparer.Ordinal);
            if (m_QualityTierMappings != null)
            {
                for (var i = 0; i < m_QualityTierMappings.Length; i++)
                {
                    var mapping = m_QualityTierMappings[i];
                    if (mapping == null || string.IsNullOrEmpty(mapping.qualityName) || previous.ContainsKey(mapping.qualityName))
                        continue;

                    previous.Add(mapping.qualityName, mapping.tier);
                }
            }

            m_QualityTierMappings = new NBShader2QualityTierMapping[names.Length];
            for (var i = 0; i < names.Length; i++)
            {
                NBShader2FeatureTier tier;
                if (!previous.TryGetValue(names[i], out tier))
                    tier = GuessTierForQualityIndex(i, names.Length);

                m_QualityTierMappings[i] = new NBShader2QualityTierMapping
                {
                    qualityName = names[i],
                    tier = tier
                };
            }
        }

        private static HashSet<string> BuildSanitizedAllowedSet(string[] keywords)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            if (keywords == null)
                return result;

            for (var i = 0; i < keywords.Length; i++)
            {
                var keyword = keywords[i];
                if (NBShader2FeatureLevelCatalog.IsManagedKeyword(keyword))
                    result.Add(keyword);
            }

            return result;
        }

        private static string[] ToCatalogOrderedArray(HashSet<string> keywords)
        {
            if (keywords == null || keywords.Count == 0)
                return new string[0];

            var result = new List<string>();
            var catalog = NBShader2FeatureLevelCatalog.ManagedKeywords;
            for (var i = 0; i < catalog.Length; i++)
            {
                if (keywords.Contains(catalog[i]))
                    result.Add(catalog[i]);
            }

            return result.ToArray();
        }
    }
}
