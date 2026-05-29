using System.Collections.Generic;
using NBShader;
using UnityEditor;
using UnityEngine;

namespace NBShaders2.Editor.FeatureLevel
{
    public static class NBShaderRuntimeSettingsSynchronizer
    {
        public static bool WriteConfiguredRuntimeSettingsAsset()
        {
            return WriteProjectSettingsToRuntimeAsset(NBShaderFeatureLevelProjectSettings.instance.runtimeSettingsAsset);
        }

        public static bool WriteProjectSettingsToRuntimeAsset(NBShaderFeatureRuntimeSettings asset)
        {
            if (asset == null)
            {
                Debug.LogWarning("NBShader runtime settings asset is not configured. Assign a Runtime Settings Asset before writing.");
                return false;
            }

            var settings = NBShaderFeatureLevelProjectSettings.instance;
            settings.EnsureInitialized();
            ApplyProjectSettingsToRuntimeObject(asset, settings);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);
            return true;
        }

        public static bool WriteProjectSettingsSnapshotToRuntimeAssetNoSave(NBShaderFeatureRuntimeSettings asset)
        {
            if (asset == null)
            {
                Debug.LogWarning("NBShader runtime settings asset is not configured. Assign a Runtime Settings Asset before writing.");
                return false;
            }

            ApplyProjectSettingsSnapshotToRuntimeObjectNoSave(asset, NBShaderFeatureLevelProjectSettings.instance);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);
            return true;
        }

        private static void ApplyProjectSettingsToRuntimeObject(
            NBShaderFeatureRuntimeSettings asset,
            NBShaderFeatureLevelProjectSettings settings)
        {
            asset.lowAllowedKeywords = ToCatalogOrderedKeywords(settings.GetAllowedKeywordSet(NBShaderFeatureTier.Low));
            asset.mediumAllowedKeywords = ToCatalogOrderedKeywords(settings.GetAllowedKeywordSet(NBShaderFeatureTier.Medium));
            asset.highAllowedKeywords = ToCatalogOrderedKeywords(settings.GetAllowedKeywordSet(NBShaderFeatureTier.High));
            asset.ultraAllowedKeywords = ToCatalogOrderedKeywords(settings.GetAllowedKeywordSet(NBShaderFeatureTier.Ultra));

            asset.lowAllowedPassFeatures = ToCatalogOrderedPassFeatures(settings.GetAllowedPassFeatureSet(NBShaderFeatureTier.Low));
            asset.mediumAllowedPassFeatures = ToCatalogOrderedPassFeatures(settings.GetAllowedPassFeatureSet(NBShaderFeatureTier.Medium));
            asset.highAllowedPassFeatures = ToCatalogOrderedPassFeatures(settings.GetAllowedPassFeatureSet(NBShaderFeatureTier.High));
            asset.ultraAllowedPassFeatures = ToCatalogOrderedPassFeatures(settings.GetAllowedPassFeatureSet(NBShaderFeatureTier.Ultra));

            asset.qualityTierMappings = ConvertQualityMappings(settings.qualityTierMappings);
        }

        private static void ApplyProjectSettingsSnapshotToRuntimeObjectNoSave(
            NBShaderFeatureRuntimeSettings asset,
            NBShaderFeatureLevelProjectSettings settings)
        {
            asset.lowAllowedKeywords = ToCatalogOrderedKeywords(settings.GetAllowedKeywordSetForBuildInfoNoSave(NBShaderFeatureTier.Low));
            asset.mediumAllowedKeywords = ToCatalogOrderedKeywords(settings.GetAllowedKeywordSetForBuildInfoNoSave(NBShaderFeatureTier.Medium));
            asset.highAllowedKeywords = ToCatalogOrderedKeywords(settings.GetAllowedKeywordSetForBuildInfoNoSave(NBShaderFeatureTier.High));
            asset.ultraAllowedKeywords = ToCatalogOrderedKeywords(settings.GetAllowedKeywordSetForBuildInfoNoSave(NBShaderFeatureTier.Ultra));

            asset.lowAllowedPassFeatures = ToCatalogOrderedPassFeatures(settings.GetAllowedPassFeatureSetForBuildInfoNoSave(NBShaderFeatureTier.Low));
            asset.mediumAllowedPassFeatures = ToCatalogOrderedPassFeatures(settings.GetAllowedPassFeatureSetForBuildInfoNoSave(NBShaderFeatureTier.Medium));
            asset.highAllowedPassFeatures = ToCatalogOrderedPassFeatures(settings.GetAllowedPassFeatureSetForBuildInfoNoSave(NBShaderFeatureTier.High));
            asset.ultraAllowedPassFeatures = ToCatalogOrderedPassFeatures(settings.GetAllowedPassFeatureSetForBuildInfoNoSave(NBShaderFeatureTier.Ultra));

            asset.qualityTierMappings = ConvertQualityMappingsNoSave(settings);
        }

        private static NBShaderFeatureRuntimeSettings.QualityTierMapping[] ConvertQualityMappings(NBShaderQualityTierMapping[] source)
        {
            if (source == null || source.Length == 0)
                return new NBShaderFeatureRuntimeSettings.QualityTierMapping[0];

            var result = new NBShaderFeatureRuntimeSettings.QualityTierMapping[source.Length];
            for (var i = 0; i < source.Length; i++)
            {
                var item = source[i];
                result[i] = new NBShaderFeatureRuntimeSettings.QualityTierMapping
                {
                    qualityName = item != null ? item.qualityName : string.Empty,
                    tier = item != null ? item.tier : NBShaderFeatureTier.Ultra
                };
            }

            return result;
        }

        private static NBShaderFeatureRuntimeSettings.QualityTierMapping[] ConvertQualityMappingsNoSave(NBShaderFeatureLevelProjectSettings settings)
        {
            var qualityNames = QualitySettings.names;
            if (qualityNames == null || qualityNames.Length == 0)
                qualityNames = new[] { "Default" };

            var result = new NBShaderFeatureRuntimeSettings.QualityTierMapping[qualityNames.Length];
            for (var i = 0; i < qualityNames.Length; i++)
            {
                var qualityName = qualityNames[i];
                NBShaderFeatureTier tier;
                if (!settings.TryGetTierForQualityNameNoSave(qualityName, out tier))
                    tier = NBShaderFeatureTier.Ultra;

                result[i] = new NBShaderFeatureRuntimeSettings.QualityTierMapping
                {
                    qualityName = qualityName,
                    tier = tier
                };
            }

            return result;
        }

        private static string[] ToCatalogOrderedKeywords(HashSet<string> allowed)
        {
            return ToCatalogOrderedArray(allowed, NBShaderFeatureCatalog.RawKeywords);
        }

        private static string[] ToCatalogOrderedPassFeatures(HashSet<string> allowed)
        {
            return ToCatalogOrderedArray(allowed, NBShaderPassFeatureCatalog.RawPassFeatureIds);
        }

        private static string[] ToCatalogOrderedArray(HashSet<string> allowed, string[] catalogOrder)
        {
            if (allowed == null || catalogOrder == null)
                return new string[0];

            var result = new List<string>();
            for (var i = 0; i < catalogOrder.Length; i++)
            {
                var value = catalogOrder[i];
                if (allowed.Contains(value))
                    result.Add(value);
            }

            return result.ToArray();
        }
    }
}
