using System;
using System.Collections.Generic;
using NBShader;
using UnityEditor;
using UnityEngine;

namespace NBShaders2.Editor.FeatureLevel
{
    [FilePath("ProjectSettings/NBShaderFeatureLevels.asset", FilePathAttribute.Location.ProjectFolder)]
    public sealed class NBShaderFeatureLevelProjectSettings : ScriptableSingleton<NBShaderFeatureLevelProjectSettings>
    {
        private static readonly string[] DefaultQualityNames = { "Default" };

        [SerializeField] private NBShaderFeatureTierKeywordSet[] m_TierKeywordSets;
        [SerializeField] private NBShaderFeatureTierPassSet[] m_TierPassSets;
        [SerializeField] private NBShaderQualityTierMapping[] m_QualityTierMappings;
        [SerializeField] private NBShaderBuildStripPolicy m_BuildStripPolicy = NBShaderBuildStripPolicy.Disabled;
        [SerializeField] private NBShaderFeatureTier m_ExplicitTier = NBShaderFeatureTier.Ultra;
        [SerializeField] private bool m_EnableDebugSymbols;
        [SerializeField] private NBShaderFeatureRuntimeSettings m_RuntimeSettingsAsset;

        [NonSerialized] private bool m_Initialized;
        [NonSerialized] private HashSet<string>[] m_AllowedKeywordSetCache;
        [NonSerialized] private bool m_AllowedKeywordSetCacheValid;
        [NonSerialized] private HashSet<string>[] m_AllowedPassFeatureSetCache;
        [NonSerialized] private bool m_AllowedPassFeatureSetCacheValid;

        public NBShaderFeatureTierKeywordSet[] tierKeywordSets { get { EnsureInitialized(); InvalidateAllowedKeywordSetCache(); return m_TierKeywordSets; } }
        public NBShaderFeatureTierPassSet[] tierPassSets { get { EnsureInitialized(); InvalidateAllowedPassFeatureSetCache(); return m_TierPassSets; } }
        public NBShaderQualityTierMapping[] qualityTierMappings { get { EnsureInitialized(); return m_QualityTierMappings; } }
        public NBShaderBuildStripPolicy buildStripPolicy { get { return m_BuildStripPolicy; } set { m_BuildStripPolicy = value; } }
        public NBShaderFeatureTier explicitTier { get { return m_ExplicitTier; } set { m_ExplicitTier = value; } }
        public bool enableDebugSymbols { get { return m_EnableDebugSymbols; } }
        public NBShaderFeatureRuntimeSettings runtimeSettingsAsset { get { return m_RuntimeSettingsAsset; } set { m_RuntimeSettingsAsset = value; } }

        public void EnsureInitialized()
        {
            if (m_Initialized &&
                HasValidTierKeywordSets() &&
                HasValidTierPassSets() &&
                HasValidQualityMappings() &&
                AreQualityMappingsCurrent())
            {
                return;
            }

            var changed = false;
            if (m_TierKeywordSets == null || m_TierKeywordSets.Length != 4)
            {
                ResetTierKeywordSetsToDefault();
                changed = true;
            }
            else
            {
                changed |= NormalizeTierKeywordSets();
            }

            if (m_TierPassSets == null || m_TierPassSets.Length != 4)
            {
                ResetTierPassSetsToDefault();
                changed = true;
            }
            else
            {
                changed |= NormalizeTierPassSets();
            }

            if (m_QualityTierMappings == null || m_QualityTierMappings.Length == 0)
            {
                ResetQualityMappingsToDefault();
                changed = true;
            }
            else
            {
                changed |= NormalizeQualityMappingsWithCurrentQualityLevels();
            }

            if (changed)
            {
                Save(true);
            }

            m_Initialized = true;
        }

        public void ResetTierKeywordSetsToDefault()
        {
            m_TierKeywordSets = NBShaderFeatureLevelPresetLoader.LoadDefaultTierKeywordSets();
            InvalidateAllowedKeywordSetCache();
        }

        public void ResetTierPassSetsToDefault()
        {
            m_TierPassSets = NBShaderFeatureLevelPresetLoader.LoadDefaultTierPassSets();
            InvalidateAllowedPassFeatureSetCache();
        }

        public void ResetQualityMappingsToDefault()
        {
            var names = QualitySettings.names;
            if (names == null || names.Length == 0)
                names = DefaultQualityNames;

            m_QualityTierMappings = new NBShaderQualityTierMapping[names.Length];
            for (var i = 0; i < names.Length; i++)
            {
                m_QualityTierMappings[i] = new NBShaderQualityTierMapping
                {
                    qualityName = names[i],
                    tier = GuessTierForQualityIndex(i, names.Length)
                };
            }
        }

        public HashSet<string> GetAllowedKeywordSet(NBShaderFeatureTier tier)
        {
            return new HashSet<string>(GetAllowedKeywordSetForReadOnlyUse(tier), StringComparer.Ordinal);
        }

        public HashSet<string> GetAllowedPassFeatureSet(NBShaderFeatureTier tier)
        {
            return new HashSet<string>(GetAllowedPassFeatureSetForReadOnlyUse(tier), StringComparer.Ordinal);
        }

        public bool IsKeywordAllowed(NBShaderFeatureTier tier, string keyword)
        {
            return GetAllowedKeywordSetForReadOnlyUse(tier).Contains(keyword);
        }

        public bool IsPassFeatureAllowed(NBShaderFeatureTier tier, string passFeatureId)
        {
            return GetAllowedPassFeatureSetForReadOnlyUse(tier).Contains(passFeatureId);
        }

        internal HashSet<string> GetAllowedKeywordSetForReadOnlyUse(NBShaderFeatureTier tier)
        {
            EnsureInitialized();
            EnsureAllowedKeywordSetCache();
            return m_AllowedKeywordSetCache[ToTierIndex(tier)];
        }

        internal HashSet<string> GetAllowedPassFeatureSetForReadOnlyUse(NBShaderFeatureTier tier)
        {
            EnsureInitialized();
            EnsureAllowedPassFeatureSetCache();
            return m_AllowedPassFeatureSetCache[ToTierIndex(tier)];
        }

        internal HashSet<string> GetAllowedKeywordSetForBuildInfoNoSave(NBShaderFeatureTier tier)
        {
            var existing = FindTierKeywordSet(tier);
            if (existing != null && existing.allowedKeywords != null)
                return BuildSanitizedAllowedSet(existing.allowedKeywords);

            return BuildSanitizedAllowedSet(NBShaderFeatureLevelCatalog.ManagedKeywords);
        }

        internal HashSet<string> GetAllowedPassFeatureSetForBuildInfoNoSave(NBShaderFeatureTier tier)
        {
            var existing = FindTierPassSet(tier);
            if (existing != null && existing.allowedPassFeatures != null)
                return BuildSanitizedAllowedPassFeatureSet(existing.allowedPassFeatures);

            return BuildSanitizedAllowedPassFeatureSet(NBShaderFeatureLevelCatalog.ManagedPassFeatures);
        }

        public void SetKeywordAllowed(NBShaderFeatureTier tier, string keyword, bool allowed)
        {
            EnsureInitialized();
            if (!NBShaderFeatureLevelCatalog.IsManagedKeyword(keyword))
                return;

            NBShaderFeatureTierKeywordSet target = null;
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
                target = new NBShaderFeatureTierKeywordSet { tier = tier };
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
            InvalidateAllowedKeywordSetCache();
        }

        public void SetPassFeatureAllowed(NBShaderFeatureTier tier, string passFeatureId, bool allowed)
        {
            EnsureInitialized();
            if (!NBShaderFeatureLevelCatalog.IsManagedPassFeature(passFeatureId))
                return;

            NBShaderFeatureTierPassSet target = null;
            for (var i = 0; i < m_TierPassSets.Length; i++)
            {
                if (m_TierPassSets[i] != null && m_TierPassSets[i].tier == tier)
                {
                    target = m_TierPassSets[i];
                    break;
                }
            }

            if (target == null)
            {
                target = new NBShaderFeatureTierPassSet { tier = tier };
                var index = (int)tier;
                if (index >= 0 && index < m_TierPassSets.Length)
                    m_TierPassSets[index] = target;
            }

            var passFeatures = BuildSanitizedAllowedPassFeatureSet(target.allowedPassFeatures);
            if (allowed)
                passFeatures.Add(passFeatureId);
            else
                passFeatures.Remove(passFeatureId);

            target.allowedPassFeatures = ToCatalogOrderedPassFeatureArray(passFeatures);
            InvalidateAllowedPassFeatureSetCache();
        }

        public bool TryGetTierForQualityName(string qualityName, out NBShaderFeatureTier tier)
        {
            EnsureInitialized();
            tier = NBShaderFeatureTier.Ultra;
            return TryGetTierForQualityNameNoSave(qualityName, out tier);
        }

        internal bool TryGetTierForQualityNameNoSave(string qualityName, out NBShaderFeatureTier tier)
        {
            tier = NBShaderFeatureTier.Ultra;
            if (string.IsNullOrEmpty(qualityName))
                return false;

            if (m_QualityTierMappings != null)
            {
                for (var i = 0; i < m_QualityTierMappings.Length; i++)
                {
                    var mapping = m_QualityTierMappings[i];
                    if (mapping == null || !string.Equals(mapping.qualityName, qualityName, StringComparison.Ordinal))
                        continue;

                    tier = mapping.tier;
                    return true;
                }
            }

            var names = QualitySettings.names;
            if (names == null || names.Length == 0)
                names = DefaultQualityNames;

            for (var i = 0; i < names.Length; i++)
            {
                if (!string.Equals(names[i], qualityName, StringComparison.Ordinal))
                    continue;

                tier = GuessTierForQualityIndex(i, names.Length);
                return true;
            }

            return false;
        }

        public string[] GetQualityNamesForTier(NBShaderFeatureTier tier)
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

        public void MoveQualityToTier(string qualityName, NBShaderFeatureTier tier)
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

            var newMappings = new NBShaderQualityTierMapping[m_QualityTierMappings.Length + 1];
            Array.Copy(m_QualityTierMappings, newMappings, m_QualityTierMappings.Length);
            newMappings[newMappings.Length - 1] = new NBShaderQualityTierMapping
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
                result.UnionWith(GetAllowedKeywordSetForReadOnlyUse(m_QualityTierMappings[i].tier));
            }
            return result;
        }

        public HashSet<string> GetQualityMappedUnionAllowedPassFeatureSet()
        {
            EnsureInitialized();
            var result = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < m_QualityTierMappings.Length; i++)
            {
                if (m_QualityTierMappings[i] == null)
                    continue;
                result.UnionWith(GetAllowedPassFeatureSetForReadOnlyUse(m_QualityTierMappings[i].tier));
            }
            return result;
        }

        internal HashSet<string> GetQualityMappedUnionAllowedKeywordSetForBuildInfoNoSave()
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            AddQualityMappedUnionNoSave(
                delegate(NBShaderFeatureTier tier)
                {
                    result.UnionWith(GetAllowedKeywordSetForBuildInfoNoSave(tier));
                });
            return result;
        }

        internal HashSet<string> GetQualityMappedUnionAllowedPassFeatureSetForBuildInfoNoSave()
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            AddQualityMappedUnionNoSave(
                delegate(NBShaderFeatureTier tier)
                {
                    result.UnionWith(GetAllowedPassFeatureSetForBuildInfoNoSave(tier));
                });
            return result;
        }

        public void SaveProjectSettings()
        {
            EnsureInitialized();
            Save(true);
        }

        public void SetDebugSymbolsEnabled(bool enabled)
        {
            m_EnableDebugSymbols = enabled;
        }

        public void SaveDebugSymbolsProjectSettings()
        {
            Save(true);
        }

        private static NBShaderFeatureTier GuessTierForQualityIndex(int index, int count)
        {
            if (count <= 1)
                return NBShaderFeatureTier.Ultra;
            var normalized = index / (float)(count - 1);
            if (normalized < 0.25f) return NBShaderFeatureTier.Low;
            if (normalized < 0.50f) return NBShaderFeatureTier.Medium;
            if (normalized < 0.75f) return NBShaderFeatureTier.High;
            return NBShaderFeatureTier.Ultra;
        }

        private bool NormalizeTierKeywordSets()
        {
            var changed = false;
            NBShaderFeatureTierKeywordSet[] defaults = null;
            var normalized = new NBShaderFeatureTierKeywordSet[4];
            for (var tierIndex = 0; tierIndex < normalized.Length; tierIndex++)
            {
                var tier = (NBShaderFeatureTier)tierIndex;
                var existing = FindTierKeywordSet(tier);
                if (existing == null)
                {
                    if (defaults == null)
                        defaults = NBShaderFeatureLevelPresetLoader.LoadDefaultTierKeywordSets();
                    changed = true;
                }

                normalized[tierIndex] = new NBShaderFeatureTierKeywordSet
                {
                    tier = tier,
                    allowedKeywords = existing != null
                        ? ToCatalogOrderedArray(BuildSanitizedAllowedSet(existing.allowedKeywords))
                        : GetAllowedKeywords(defaults, tier)
                };

                if (existing != null && !AreKeywordArraysEquivalent(existing.allowedKeywords, normalized[tierIndex].allowedKeywords))
                    changed = true;
            }

            m_TierKeywordSets = normalized;
            if (changed)
                InvalidateAllowedKeywordSetCache();
            return changed;
        }

        private bool NormalizeTierPassSets()
        {
            var changed = false;
            NBShaderFeatureTierPassSet[] defaults = null;
            var normalized = new NBShaderFeatureTierPassSet[4];
            for (var tierIndex = 0; tierIndex < normalized.Length; tierIndex++)
            {
                var tier = (NBShaderFeatureTier)tierIndex;
                var existing = FindTierPassSet(tier);
                if (existing == null)
                {
                    if (defaults == null)
                        defaults = NBShaderFeatureLevelPresetLoader.LoadDefaultTierPassSets();
                    changed = true;
                }

                normalized[tierIndex] = new NBShaderFeatureTierPassSet
                {
                    tier = tier,
                    allowedPassFeatures = existing != null
                        ? ToCatalogOrderedPassFeatureArray(BuildSanitizedAllowedPassFeatureSet(existing.allowedPassFeatures))
                        : GetAllowedPassFeatures(defaults, tier)
                };

                if (existing != null && !ArePassFeatureArraysEquivalent(existing.allowedPassFeatures, normalized[tierIndex].allowedPassFeatures))
                    changed = true;
            }

            m_TierPassSets = normalized;
            if (changed)
                InvalidateAllowedPassFeatureSetCache();
            return changed;
        }

        private bool HasValidTierKeywordSets()
        {
            if (m_TierKeywordSets == null || m_TierKeywordSets.Length != 4)
                return false;

            for (var i = 0; i < m_TierKeywordSets.Length; i++)
            {
                var set = m_TierKeywordSets[i];
                if (set == null || set.tier != (NBShaderFeatureTier)i || set.allowedKeywords == null)
                    return false;
            }

            return true;
        }

        private bool HasValidTierPassSets()
        {
            if (m_TierPassSets == null || m_TierPassSets.Length != 4)
                return false;

            for (var i = 0; i < m_TierPassSets.Length; i++)
            {
                var set = m_TierPassSets[i];
                if (set == null || set.tier != (NBShaderFeatureTier)i || set.allowedPassFeatures == null)
                    return false;
            }

            return true;
        }

        private bool HasValidQualityMappings()
        {
            return m_QualityTierMappings != null && m_QualityTierMappings.Length > 0;
        }

        private bool AreQualityMappingsCurrent()
        {
            var names = QualitySettings.names;
            if (names == null || names.Length == 0)
                names = DefaultQualityNames;

            if (m_QualityTierMappings == null || m_QualityTierMappings.Length != names.Length)
                return false;

            for (var i = 0; i < names.Length; i++)
            {
                var mapping = m_QualityTierMappings[i];
                if (mapping == null || !string.Equals(mapping.qualityName, names[i], StringComparison.Ordinal))
                    return false;
            }

            return true;
        }

        private void EnsureAllowedKeywordSetCache()
        {
            if (m_AllowedKeywordSetCacheValid &&
                m_AllowedKeywordSetCache != null &&
                m_AllowedKeywordSetCache.Length == 4)
            {
                return;
            }

            if (m_AllowedKeywordSetCache == null || m_AllowedKeywordSetCache.Length != 4)
                m_AllowedKeywordSetCache = new HashSet<string>[4];

            for (var i = 0; i < m_AllowedKeywordSetCache.Length; i++)
                m_AllowedKeywordSetCache[i] = BuildAllowedKeywordSet((NBShaderFeatureTier)i);

            m_AllowedKeywordSetCacheValid = true;
        }

        private void EnsureAllowedPassFeatureSetCache()
        {
            if (m_AllowedPassFeatureSetCacheValid &&
                m_AllowedPassFeatureSetCache != null &&
                m_AllowedPassFeatureSetCache.Length == 4)
            {
                return;
            }

            if (m_AllowedPassFeatureSetCache == null || m_AllowedPassFeatureSetCache.Length != 4)
                m_AllowedPassFeatureSetCache = new HashSet<string>[4];

            for (var i = 0; i < m_AllowedPassFeatureSetCache.Length; i++)
                m_AllowedPassFeatureSetCache[i] = BuildAllowedPassFeatureSet((NBShaderFeatureTier)i);

            m_AllowedPassFeatureSetCacheValid = true;
        }

        private void InvalidateAllowedKeywordSetCache()
        {
            m_AllowedKeywordSetCacheValid = false;
        }

        private void InvalidateAllowedPassFeatureSetCache()
        {
            m_AllowedPassFeatureSetCacheValid = false;
        }

        private HashSet<string> BuildAllowedKeywordSet(NBShaderFeatureTier tier)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            if (m_TierKeywordSets == null)
                return result;

            for (var i = 0; i < m_TierKeywordSets.Length; i++)
            {
                var set = m_TierKeywordSets[i];
                if (set == null || set.tier != tier || set.allowedKeywords == null)
                    continue;

                for (var k = 0; k < set.allowedKeywords.Length; k++)
                {
                    var keyword = set.allowedKeywords[k];
                    if (NBShaderFeatureLevelCatalog.IsManagedKeyword(keyword))
                        result.Add(keyword);
                }
            }

            return result;
        }

        private HashSet<string> BuildAllowedPassFeatureSet(NBShaderFeatureTier tier)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            if (m_TierPassSets == null)
                return result;

            for (var i = 0; i < m_TierPassSets.Length; i++)
            {
                var set = m_TierPassSets[i];
                if (set == null || set.tier != tier || set.allowedPassFeatures == null)
                    continue;

                for (var k = 0; k < set.allowedPassFeatures.Length; k++)
                {
                    var passFeature = set.allowedPassFeatures[k];
                    if (NBShaderFeatureLevelCatalog.IsManagedPassFeature(passFeature))
                        result.Add(passFeature);
                }
            }

            return result;
        }

        private static int ToTierIndex(NBShaderFeatureTier tier)
        {
            var index = (int)tier;
            return index >= 0 && index < 4 ? index : (int)NBShaderFeatureTier.Ultra;
        }

        private NBShaderFeatureTierKeywordSet FindTierKeywordSet(NBShaderFeatureTier tier)
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

        private NBShaderFeatureTierPassSet FindTierPassSet(NBShaderFeatureTier tier)
        {
            if (m_TierPassSets == null)
                return null;

            for (var i = 0; i < m_TierPassSets.Length; i++)
            {
                var set = m_TierPassSets[i];
                if (set != null && set.tier == tier)
                    return set;
            }

            return null;
        }

        private bool NormalizeQualityMappingsWithCurrentQualityLevels()
        {
            var names = QualitySettings.names;
            if (names == null || names.Length == 0)
                names = DefaultQualityNames;

            var previous = new Dictionary<string, NBShaderFeatureTier>(StringComparer.Ordinal);
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

            var changed = m_QualityTierMappings == null || m_QualityTierMappings.Length != names.Length;
            m_QualityTierMappings = new NBShaderQualityTierMapping[names.Length];
            for (var i = 0; i < names.Length; i++)
            {
                NBShaderFeatureTier tier;
                if (!previous.TryGetValue(names[i], out tier))
                {
                    tier = GuessTierForQualityIndex(i, names.Length);
                    changed = true;
                }

                m_QualityTierMappings[i] = new NBShaderQualityTierMapping
                {
                    qualityName = names[i],
                    tier = tier
                };
            }

            return changed;
        }

        private delegate void TierAccumulator(NBShaderFeatureTier tier);

        private void AddQualityMappedUnionNoSave(TierAccumulator accumulator)
        {
            if (accumulator == null)
                return;

            if (m_QualityTierMappings != null && m_QualityTierMappings.Length > 0)
            {
                for (var i = 0; i < m_QualityTierMappings.Length; i++)
                {
                    var mapping = m_QualityTierMappings[i];
                    if (mapping != null)
                        accumulator(mapping.tier);
                }
                return;
            }

            var names = QualitySettings.names;
            if (names == null || names.Length == 0)
                names = DefaultQualityNames;

            for (var i = 0; i < names.Length; i++)
                accumulator(GuessTierForQualityIndex(i, names.Length));
        }

        private static HashSet<string> BuildSanitizedAllowedSet(string[] keywords)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            if (keywords == null)
                return result;

            for (var i = 0; i < keywords.Length; i++)
            {
                var keyword = keywords[i];
                if (NBShaderFeatureLevelCatalog.IsManagedKeyword(keyword))
                    result.Add(keyword);
            }

            return result;
        }

        private static HashSet<string> BuildSanitizedAllowedPassFeatureSet(string[] passFeatures)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            if (passFeatures == null)
                return result;

            for (var i = 0; i < passFeatures.Length; i++)
            {
                var passFeature = passFeatures[i];
                if (NBShaderFeatureLevelCatalog.IsManagedPassFeature(passFeature))
                    result.Add(passFeature);
            }

            return result;
        }

        private static string[] ToCatalogOrderedArray(HashSet<string> keywords)
        {
            if (keywords == null || keywords.Count == 0)
                return new string[0];

            var result = new List<string>();
            var catalog = NBShaderFeatureLevelCatalog.ManagedKeywords;
            for (var i = 0; i < catalog.Length; i++)
            {
                if (keywords.Contains(catalog[i]))
                    result.Add(catalog[i]);
            }

            return result.ToArray();
        }

        private static string[] ToCatalogOrderedPassFeatureArray(HashSet<string> passFeatures)
        {
            if (passFeatures == null || passFeatures.Count == 0)
                return new string[0];

            var result = new List<string>();
            var catalog = NBShaderFeatureLevelCatalog.ManagedPassFeatures;
            for (var i = 0; i < catalog.Length; i++)
            {
                if (passFeatures.Contains(catalog[i]))
                    result.Add(catalog[i]);
            }

            return result.ToArray();
        }

        private static string[] GetAllowedKeywords(NBShaderFeatureTierKeywordSet[] sets, NBShaderFeatureTier tier)
        {
            if (sets == null)
                return new string[0];

            for (var i = 0; i < sets.Length; i++)
            {
                var set = sets[i];
                if (set != null && set.tier == tier)
                    return set.allowedKeywords ?? new string[0];
            }

            return new string[0];
        }

        private static string[] GetAllowedPassFeatures(NBShaderFeatureTierPassSet[] sets, NBShaderFeatureTier tier)
        {
            if (sets == null)
                return new string[0];

            for (var i = 0; i < sets.Length; i++)
            {
                var set = sets[i];
                if (set != null && set.tier == tier)
                    return set.allowedPassFeatures ?? new string[0];
            }

            return new string[0];
        }

        private static bool AreKeywordArraysEquivalent(string[] a, string[] b)
        {
            var setA = BuildSanitizedAllowedSet(a);
            var setB = BuildSanitizedAllowedSet(b);
            return setA.SetEquals(setB);
        }

        private static bool ArePassFeatureArraysEquivalent(string[] a, string[] b)
        {
            var setA = BuildSanitizedAllowedPassFeatureSet(a);
            var setB = BuildSanitizedAllowedPassFeatureSet(b);
            return setA.SetEquals(setB);
        }
    }
}
