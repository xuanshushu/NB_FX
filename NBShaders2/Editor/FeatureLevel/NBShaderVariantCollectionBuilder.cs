using System;
using System.Collections.Generic;
using System.IO;
using NBShader;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace NBShaders2.Editor.FeatureLevel
{
    public static class NBShaderVariantCollectionBuilder
    {
        public const string DefaultOutputFolder = "Assets/NBShader/ShaderVariantCollections";

        private static readonly NBShaderFeatureTier[] Tiers =
        {
            NBShaderFeatureTier.Low,
            NBShaderFeatureTier.Medium,
            NBShaderFeatureTier.High,
            NBShaderFeatureTier.Ultra
        };

        public static NBShaderVariantCollectionBuildResult Preview(IEnumerable<string> searchFolders, string outputFolder)
        {
            return Preview(searchFolders, outputFolder, null);
        }

        public static NBShaderVariantCollectionBuildResult Preview(
            IEnumerable<string> searchFolders,
            string outputFolder,
            IEnumerable<NBShaderFeatureTier> tiers)
        {
            var validFolders = GetValidSearchFolders(searchFolders);
            var materials = CollectMaterialsInValidFolders(validFolders);
            return BuildResult(validFolders, materials, NormalizeOutputFolder(outputFolder), NormalizeTiers(tiers), false);
        }

        public static NBShaderVariantCollectionBuildResult Generate(IEnumerable<string> searchFolders, string outputFolder)
        {
            return Generate(searchFolders, outputFolder, null);
        }

        public static NBShaderVariantCollectionBuildResult Generate(
            IEnumerable<string> searchFolders,
            string outputFolder,
            IEnumerable<NBShaderFeatureTier> tiers)
        {
            var validFolders = GetValidSearchFolders(searchFolders);
            var materials = CollectMaterialsInValidFolders(validFolders);
            var normalizedOutputFolder = NormalizeOutputFolder(outputFolder);
            var normalizedTiers = NormalizeTiers(tiers);

            if (normalizedTiers.Length == 0)
                return BuildResult(validFolders, materials, normalizedOutputFolder, normalizedTiers, false, "No NBShader feature tiers were selected.");

            if (materials.Length == 0)
                return BuildResult(validFolders, materials, normalizedOutputFolder, normalizedTiers, false, "No NBShader materials were found in the selected folders.");

            string errorMessage;
            if (!EnsureOutputFolder(normalizedOutputFolder, out errorMessage))
                return BuildResult(validFolders, materials, normalizedOutputFolder, normalizedTiers, false, errorMessage);

            return BuildResult(validFolders, materials, normalizedOutputFolder, normalizedTiers, true);
        }

        public static Material[] CollectMaterials(IEnumerable<string> searchFolders)
        {
            var folders = GetValidSearchFolders(searchFolders);
            return CollectMaterialsInValidFolders(folders);
        }

        private static Material[] CollectMaterialsInValidFolders(string[] folders)
        {
            if (folders.Length == 0)
                return new Material[0];

            var guids = AssetDatabase.FindAssets("t:Material", folders);
            var result = new List<Material>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrEmpty(path) || !seen.Add(path))
                    continue;

                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (NBShaderMaterialIntentResolver.IsNBShaderMaterial(material))
                    result.Add(material);
            }

            return result.ToArray();
        }

        public static string[] GetValidSearchFolders(IEnumerable<string> searchFolders)
        {
            if (searchFolders == null)
                return new string[0];

            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var searchFolder in searchFolders)
            {
                var path = NormalizeAssetPath(searchFolder);
                if (string.IsNullOrEmpty(path) || !AssetDatabase.IsValidFolder(path) || !seen.Add(path))
                    continue;

                result.Add(path);
            }

            return result.ToArray();
        }

        internal static string NormalizeAssetPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;

            path = path.Replace('\\', '/').Trim();
            if (path.StartsWith("Assets", StringComparison.Ordinal) ||
                path.StartsWith("Packages", StringComparison.Ordinal))
            {
                return path.TrimEnd('/');
            }

            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..")).Replace('\\', '/').TrimEnd('/');
            var fullPath = Path.GetFullPath(path).Replace('\\', '/').TrimEnd('/');
            if (fullPath.StartsWith(projectRoot + "/", StringComparison.OrdinalIgnoreCase))
                return fullPath.Substring(projectRoot.Length + 1);

            return string.Empty;
        }

        internal static string ToAbsolutePath(string assetPath)
        {
            assetPath = NormalizeAssetPath(assetPath);
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..")).Replace('\\', '/').TrimEnd('/');
            if (string.IsNullOrEmpty(assetPath))
                return projectRoot;

            return Path.Combine(projectRoot, assetPath).Replace('\\', '/');
        }

        public static string GetOutputPath(string outputFolder, NBShaderFeatureTier tier)
        {
            outputFolder = NormalizeOutputFolder(outputFolder);
            if (string.IsNullOrEmpty(outputFolder))
                outputFolder = DefaultOutputFolder;
            return outputFolder.TrimEnd('/') + "/NBShader_" + tier + ".shadervariants";
        }

        private static NBShaderVariantCollectionBuildResult BuildResult(
            string[] validFolders,
            Material[] materials,
            string outputFolder,
            NBShaderFeatureTier[] tiers,
            bool writeAssets,
            string errorMessage = null)
        {
            if (tiers == null)
                tiers = CloneDefaultTiers();

            var tierResults = new NBShaderVariantCollectionTierResult[tiers.Length];
            for (var i = 0; i < tiers.Length; i++)
            {
                var tier = tiers[i];
                var buildInfo = NBShaderFeatureLevelEditorAPI.GetBuildInfo(
                    tier,
                    materials,
                    NBShaderBuildInfoMode.ExactMaterialVariants);
                var variants = BuildSvcVariants(buildInfo);
                var path = GetOutputPath(outputFolder, tier);
                var generated = false;
                string tierError = null;

                if (writeAssets)
                    generated = WriteCollection(path, variants, out tierError);

                tierResults[i] = new NBShaderVariantCollectionTierResult(
                    tier,
                    buildInfo != null ? buildInfo.materials.Length : 0,
                    buildInfo != null ? buildInfo.includedPassNames.Length : 0,
                    variants.Length,
                    path,
                    generated,
                    tierError);
            }

            return new NBShaderVariantCollectionBuildResult(
                validFolders,
                materials,
                outputFolder,
                tierResults,
                errorMessage);
        }

        private static SvcVariant[] BuildSvcVariants(NBShaderBuildInfoSet buildInfo)
        {
            if (buildInfo == null)
                return new SvcVariant[0];

            var result = new List<SvcVariant>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var variants = buildInfo.variants;
            for (var i = 0; i < variants.Length; i++)
            {
                var variant = variants[i];
                if (variant == null || variant.shader == null)
                    continue;

                var keywords = variant.keywords;
                var key = variant.shader.name + "|" + variant.passType + "|" + string.Join(";", keywords);
                if (!seen.Add(key))
                    continue;

                result.Add(new SvcVariant(variant.shader, variant.passType, keywords));
            }

            return result.ToArray();
        }

        private static bool WriteCollection(string path, SvcVariant[] variants, out string errorMessage)
        {
            errorMessage = null;

            var existing = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            var collection = existing as ShaderVariantCollection;
            if (existing != null && collection == null)
            {
                errorMessage = "Output path already exists and is not a ShaderVariantCollection:\n" + path;
                return false;
            }

            if (collection == null)
            {
                collection = new ShaderVariantCollection();
                AssetDatabase.CreateAsset(collection, path);
            }
            else
            {
                collection.Clear();
            }

            for (var i = 0; i < variants.Length; i++)
            {
                var variant = variants[i];
                try
                {
                    collection.Add(new ShaderVariantCollection.ShaderVariant(
                        variant.shader,
                        variant.passType,
                        variant.keywords));
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("Failed to add NBShader SVC variant: " + ex.Message);
                }
            }

            EditorUtility.SetDirty(collection);
            AssetDatabase.SaveAssetIfDirty(collection);
            return true;
        }

        private static bool EnsureOutputFolder(string outputFolder, out string errorMessage)
        {
            errorMessage = null;
            if (string.IsNullOrEmpty(outputFolder))
            {
                errorMessage = "Output folder is empty.";
                return false;
            }

            if (AssetDatabase.IsValidFolder(outputFolder))
                return true;

            if (!outputFolder.StartsWith("Assets/", StringComparison.Ordinal) &&
                !string.Equals(outputFolder, "Assets", StringComparison.Ordinal))
            {
                errorMessage = "Output folder must already exist unless it is under Assets:\n" + outputFolder;
                return false;
            }

            var parts = outputFolder.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }

            if (AssetDatabase.IsValidFolder(outputFolder))
                return true;

            errorMessage = "Could not create output folder:\n" + outputFolder;
            return false;
        }

        private static string NormalizeOutputFolder(string outputFolder)
        {
            if (string.IsNullOrEmpty(outputFolder))
                return DefaultOutputFolder;

            return NormalizeAssetPath(outputFolder);
        }

        private static NBShaderFeatureTier[] NormalizeTiers(IEnumerable<NBShaderFeatureTier> tiers)
        {
            if (tiers == null)
                return CloneDefaultTiers();

            var selected = new HashSet<NBShaderFeatureTier>();
            foreach (var tier in tiers)
            {
                if (IsValidTier(tier))
                    selected.Add(tier);
            }

            var result = new List<NBShaderFeatureTier>();
            for (var i = 0; i < Tiers.Length; i++)
            {
                var tier = Tiers[i];
                if (selected.Contains(tier))
                    result.Add(tier);
            }

            return result.ToArray();
        }

        private static bool IsValidTier(NBShaderFeatureTier tier)
        {
            return tier == NBShaderFeatureTier.Low ||
                   tier == NBShaderFeatureTier.Medium ||
                   tier == NBShaderFeatureTier.High ||
                   tier == NBShaderFeatureTier.Ultra;
        }

        private static NBShaderFeatureTier[] CloneDefaultTiers()
        {
            return (NBShaderFeatureTier[])Tiers.Clone();
        }

        private sealed class SvcVariant
        {
            public readonly Shader shader;
            public readonly PassType passType;
            public readonly string[] keywords;

            public SvcVariant(Shader shader, PassType passType, string[] keywords)
            {
                this.shader = shader;
                this.passType = passType;
                this.keywords = keywords != null ? (string[])keywords.Clone() : new string[0];
            }
        }
    }

    public sealed class NBShaderVariantCollectionBuildResult
    {
        private readonly string[] m_ValidSearchFolders;
        private readonly Material[] m_Materials;
        private readonly NBShaderVariantCollectionTierResult[] m_Tiers;

        public readonly string outputFolder;
        public readonly string errorMessage;

        public string[] validSearchFolders { get { return (string[])m_ValidSearchFolders.Clone(); } }
        public Material[] materials { get { return (Material[])m_Materials.Clone(); } }
        public NBShaderVariantCollectionTierResult[] tiers { get { return (NBShaderVariantCollectionTierResult[])m_Tiers.Clone(); } }
        public int materialCount { get { return m_Materials.Length; } }
        public int validSearchFolderCount { get { return m_ValidSearchFolders.Length; } }
        public int generatedCount { get { return CountGenerated(m_Tiers); } }
        public bool hasError { get { return !string.IsNullOrEmpty(firstErrorMessage); } }
        public bool hasTierError { get { return HasTierError(m_Tiers); } }
        public bool allTiersGenerated { get { return m_Tiers.Length > 0 && generatedCount == m_Tiers.Length; } }
        public bool succeeded { get { return !hasError && allTiersGenerated; } }
        public string firstErrorMessage { get { return GetFirstErrorMessage(errorMessage, m_Tiers); } }

        public NBShaderVariantCollectionBuildResult(
            string[] validSearchFolders,
            Material[] materials,
            string outputFolder,
            NBShaderVariantCollectionTierResult[] tiers,
            string errorMessage)
        {
            m_ValidSearchFolders = validSearchFolders != null ? (string[])validSearchFolders.Clone() : new string[0];
            m_Materials = materials != null ? (Material[])materials.Clone() : new Material[0];
            this.outputFolder = outputFolder;
            m_Tiers = tiers != null ? (NBShaderVariantCollectionTierResult[])tiers.Clone() : new NBShaderVariantCollectionTierResult[0];
            this.errorMessage = errorMessage;
        }

        private static int CountGenerated(NBShaderVariantCollectionTierResult[] tiers)
        {
            if (tiers == null || tiers.Length == 0)
                return 0;

            var count = 0;
            for (var i = 0; i < tiers.Length; i++)
            {
                var tier = tiers[i];
                if (tier != null && tier.generated)
                    count++;
            }

            return count;
        }

        private static bool HasTierError(NBShaderVariantCollectionTierResult[] tiers)
        {
            if (tiers == null || tiers.Length == 0)
                return false;

            for (var i = 0; i < tiers.Length; i++)
            {
                var tier = tiers[i];
                if (tier != null && tier.hasError)
                    return true;
            }

            return false;
        }

        private static string GetFirstErrorMessage(string resultError, NBShaderVariantCollectionTierResult[] tiers)
        {
            if (!string.IsNullOrEmpty(resultError))
                return resultError;

            if (tiers == null || tiers.Length == 0)
                return null;

            for (var i = 0; i < tiers.Length; i++)
            {
                var tier = tiers[i];
                if (tier != null && tier.hasError)
                    return tier.errorMessage;
            }

            return null;
        }
    }

    public sealed class NBShaderVariantCollectionTierResult
    {
        public readonly NBShaderFeatureTier tier;
        public readonly int materialCount;
        public readonly int passCount;
        public readonly int variantCount;
        public readonly string outputAssetPath;
        public readonly bool generated;
        public readonly string errorMessage;

        public bool hasError { get { return !string.IsNullOrEmpty(errorMessage); } }

        public NBShaderVariantCollectionTierResult(
            NBShaderFeatureTier tier,
            int materialCount,
            int passCount,
            int variantCount,
            string outputAssetPath,
            bool generated,
            string errorMessage)
        {
            this.tier = tier;
            this.materialCount = materialCount;
            this.passCount = passCount;
            this.variantCount = variantCount;
            this.outputAssetPath = outputAssetPath;
            this.generated = generated;
            this.errorMessage = errorMessage;
        }
    }
}
