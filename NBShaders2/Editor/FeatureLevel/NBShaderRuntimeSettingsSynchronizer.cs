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
