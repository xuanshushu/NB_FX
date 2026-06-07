using System.Collections.Generic;
using NBShader;
using UnityEditor;
using UnityEngine;

namespace NBShaders2.Editor.FeatureLevel
{
    public static class NBShaderRuntimeSettingsSynchronizer
    {
        public const string DefaultGeneratedRuntimeSettingsAssetPath =
            "Assets/NBShaderGenerated/RuntimeSettings/NBShaderFeatureRuntimeSettings.asset";

        private static bool s_DefaultGeneratedRuntimeSettingsWarningLogged;

        public static bool WriteProjectSettingsToRuntimeAsset(NBShaderFeatureRuntimeSettings asset)
        {
            if (asset == null)
            {
                Debug.LogWarning("NBShader runtime settings asset is null. Pass an explicit NBShaderFeatureRuntimeSettings asset before writing.");
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
                Debug.LogWarning("NBShader runtime settings asset is null. Pass an explicit NBShaderFeatureRuntimeSettings asset before writing.");
                return false;
            }

            ApplyProjectSettingsSnapshotToRuntimeObjectNoSave(asset, NBShaderFeatureLevelProjectSettings.instance);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);
            return true;
        }

        public static NBShaderFeatureRuntimeSettings WriteDefaultGeneratedRuntimeSettingsAsset(out string assetPath)
        {
            assetPath = DefaultGeneratedRuntimeSettingsAssetPath;
            LogDefaultGeneratedRuntimeSettingsWarningOnce(assetPath);

            var asset = LoadOrCreateRuntimeSettingsAsset(assetPath);
            if (asset == null)
                return null;

            return WriteProjectSettingsToRuntimeAsset(asset) ? asset : null;
        }

        public static NBShaderFeatureRuntimeSettings WriteRuntimeSettingsAssetOrDefault(
            NBShaderFeatureRuntimeSettings explicitAsset,
            out string assetPath)
        {
            if (explicitAsset == null)
                return WriteDefaultGeneratedRuntimeSettingsAsset(out assetPath);

            assetPath = AssetDatabase.GetAssetPath(explicitAsset);
            return WriteProjectSettingsToRuntimeAsset(explicitAsset) ? explicitAsset : null;
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

        private static NBShaderFeatureRuntimeSettings LoadOrCreateRuntimeSettingsAsset(string assetPath)
        {
            var existing = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            if (existing == null)
            {
                if (!EnsureAssetFolder(assetPath))
                    return null;

                var asset = ScriptableObject.CreateInstance<NBShaderFeatureRuntimeSettings>();
                AssetDatabase.CreateAsset(asset, assetPath);
                return asset;
            }

            var runtimeSettings = existing as NBShaderFeatureRuntimeSettings;
            if (runtimeSettings != null)
                return runtimeSettings;

            Debug.LogError("NBShader default runtime settings output path is occupied by another asset type: " + assetPath);
            return null;
        }

        private static bool EnsureAssetFolder(string assetPath)
        {
            var slashIndex = assetPath.LastIndexOf('/');
            if (slashIndex <= 0)
                return true;

            var folderPath = assetPath.Substring(0, slashIndex);
            var parts = folderPath.Split('/');
            if (parts.Length == 0 || parts[0] != "Assets")
            {
                Debug.LogError("NBShader default runtime settings asset path must be under Assets: " + assetPath);
                return false;
            }

            var current = "Assets";
            for (var i = 1; i < parts.Length; i++)
            {
                var part = parts[i];
                if (string.IsNullOrEmpty(part))
                    continue;

                var next = current + "/" + part;
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, part);

                if (!AssetDatabase.IsValidFolder(next))
                {
                    Debug.LogError("Failed to create NBShader runtime settings folder: " + next);
                    return false;
                }

                current = next;
            }

            return true;
        }

        private static void LogDefaultGeneratedRuntimeSettingsWarningOnce(string assetPath)
        {
            if (s_DefaultGeneratedRuntimeSettingsWarningLogged)
                return;

            s_DefaultGeneratedRuntimeSettingsWarningLogged = true;
            Debug.LogWarning(
                "NBShader runtime settings asset was not explicitly specified. Generated one from current Project Settings at " +
                assetPath +
                ". Include this asset in the player, Addressables, Resources, or shader AssetBundle before runtime code passes it to NBShaderFeatureRuntime.ApplyTier.");
        }
    }
}
