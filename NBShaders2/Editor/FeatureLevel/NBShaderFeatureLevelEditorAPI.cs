using System;
using System.Collections.Generic;
using NBShader;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace NBShaders2.Editor.FeatureLevel
{
    /// <summary>
    /// Public Editor API for CI/custom build scripts. Override scopes are process-local and never saved
    /// into ProjectSettings/NBShaderFeatureLevels.asset.
    /// </summary>
    public static class NBShaderFeatureLevelEditorAPI
    {
        public static IDisposable OverrideBuildStripPolicy(NBShaderBuildStripPolicy policy, NBShaderFeatureTier explicitTier)
        {
            return NBShaderFeatureLevelBuildStripOverride.Push(policy, explicitTier);
        }

        public static IDisposable OverrideBuildStripExplicitTier(NBShaderFeatureTier tier)
        {
            return NBShaderFeatureLevelBuildStripOverride.PushExplicitTier(tier);
        }

        public static IDisposable DisableBuildStripping()
        {
            return NBShaderFeatureLevelBuildStripOverride.PushDisabled();
        }

        public static void ClearBuildStripOverrides()
        {
            NBShaderFeatureLevelBuildStripOverride.ClearAll();
        }

        public static HashSet<string> GetAllowedManagedKeywords(NBShaderFeatureTier tier)
        {
            return NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForBuildInfoNoSave(tier);
        }

        public static HashSet<string> GetAllowedManagedPassFeatures(NBShaderFeatureTier tier)
        {
            return NBShaderFeatureLevelProjectSettings.instance.GetAllowedPassFeatureSetForBuildInfoNoSave(tier);
        }

        public static bool TryGetMaterialBuildInfo(
            Material material,
            NBShaderFeatureTier tier,
            out NBShaderMaterialBuildInfo buildInfo,
            NBShaderBuildInfoMode mode = NBShaderBuildInfoMode.ExactMaterialVariants)
        {
            buildInfo = null;
            if (!NBShaderMaterialIntentResolver.IsNBShaderMaterial(material))
                return false;

            buildInfo = GetBuildInfo(material, tier, mode);
            return buildInfo != null;
        }

        public static NBShaderMaterialBuildInfo GetBuildInfo(
            Material material,
            NBShaderFeatureTier tier,
            NBShaderBuildInfoMode mode = NBShaderBuildInfoMode.ExactMaterialVariants)
        {
            if (!NBShaderMaterialIntentResolver.IsNBShaderMaterial(material))
                return null;

            var allowedKeywords = NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForBuildInfoNoSave(tier);
            var allowedPassFeatures = NBShaderFeatureLevelProjectSettings.instance.GetAllowedPassFeatureSetForBuildInfoNoSave(tier);
            var intent = NBShaderMaterialIntentResolver.Resolve(material, tier, allowedKeywords, allowedPassFeatures);
            return new NBShaderMaterialBuildInfo(
                material,
                tier,
                mode,
                NBShaderBuildInfoUtility.ToCatalogOrderedManagedKeywords(allowedKeywords),
                intent.effectiveKeywords,
                intent.strippedManagedKeywords,
                BuildPassInfo(intent.passes),
                NBShaderBuildInfoUtility.ToCatalogOrderedPassFeatures(allowedPassFeatures));
        }

        public static NBShaderBuildInfoSet GetBuildInfo(
            NBShaderFeatureTier tier,
            IEnumerable<Material> materials,
            NBShaderBuildInfoMode mode = NBShaderBuildInfoMode.ExactMaterialVariants)
        {
            var result = new List<NBShaderMaterialBuildInfo>();
            if (materials != null)
            {
                foreach (var material in materials)
                {
                    NBShaderMaterialBuildInfo buildInfo;
                    if (TryGetMaterialBuildInfo(material, tier, out buildInfo, mode))
                        result.Add(buildInfo);
                }
            }

            if (result.Count == 0)
                return null;

            return new NBShaderBuildInfoSet(
                tier,
                mode,
                result.ToArray(),
                NBShaderBuildInfoUtility.ToCatalogOrderedManagedKeywords(
                    NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForBuildInfoNoSave(tier)),
                NBShaderBuildInfoUtility.ToCatalogOrderedPassFeatures(
                    NBShaderFeatureLevelProjectSettings.instance.GetAllowedPassFeatureSetForBuildInfoNoSave(tier)));
        }

        public static bool ShouldKeepVariant(
            Shader shader,
            ShaderSnippetData snippet,
            ShaderCompilerData compilerData,
            NBShaderMaterialBuildInfo buildInfo)
        {
            if (buildInfo == null)
                return true;

            return ShouldKeepVariant(
                shader,
                snippet.passName,
                snippet.passType,
                ExtractManagedKeywords(shader, compilerData),
                buildInfo);
        }

        public static bool ShouldKeepVariant(
            Shader shader,
            ShaderSnippetData snippet,
            ShaderCompilerData compilerData,
            NBShaderBuildInfoSet buildInfo)
        {
            if (buildInfo == null)
                return true;

            return ShouldKeepVariant(
                shader,
                snippet.passName,
                snippet.passType,
                ExtractManagedKeywords(shader, compilerData),
                buildInfo);
        }

        public static bool ShouldKeepVariant(
            Shader shader,
            string passName,
            PassType passType,
            IEnumerable<string> keywords,
            NBShaderMaterialBuildInfo buildInfo)
        {
            if (buildInfo == null)
                return true;

            var set = new NBShaderBuildInfoSet(
                buildInfo.tier,
                buildInfo.mode,
                new[] { buildInfo },
                buildInfo.allowedManagedKeywords,
                buildInfo.allowedPassFeatures);
            return ShouldKeepVariant(shader, passName, passType, keywords, set);
        }

        public static bool ShouldKeepVariant(
            Shader shader,
            string passName,
            PassType passType,
            IEnumerable<string> keywords,
            NBShaderBuildInfoSet buildInfo)
        {
            if (shader == null || shader.name != NBShaderFeatureLevelCatalog.ShaderName || buildInfo == null)
                return true;

            var managedKeywords = NBShaderBuildInfoUtility.ToCatalogOrderedManagedKeywords(keywords);
            if (!buildInfo.hasMaterials)
                return true;

            if (buildInfo.mode == NBShaderBuildInfoMode.TierAndPass)
                return IsPassIncluded(passName, passType, buildInfo) &&
                       NBShaderBuildInfoUtility.IsSubsetOf(managedKeywords, buildInfo.allowedManagedKeywords);

            var variants = buildInfo.variants;
            var declaredCompilerKeywords = NBShaderBuildInfoUtility.FilterKeywordsForPass(passName, passType, managedKeywords);
            for (var i = 0; i < variants.Length; i++)
            {
                var variant = variants[i];
                if (variant == null || !IsSameShader(shader, variant.shader))
                    continue;

                if (!IsPassMatch(passName, passType, variant.passName, variant.passType))
                    continue;

                if (NBShaderBuildInfoUtility.AreKeywordSetsEqual(declaredCompilerKeywords, variant.keywords))
                    return true;
            }

            return false;
        }

        public static bool WriteConfiguredRuntimeSettingsAsset()
        {
            return NBShaderRuntimeSettingsSynchronizer.WriteConfiguredRuntimeSettingsAsset();
        }

        public static bool WriteRuntimeSettingsAsset(NBShaderFeatureRuntimeSettings asset)
        {
            return NBShaderRuntimeSettingsSynchronizer.WriteProjectSettingsToRuntimeAsset(asset);
        }

        private static NBShaderPassBuildInfo[] BuildPassInfo(NBShaderPassIntent[] passes)
        {
            if (passes == null || passes.Length == 0)
                return new NBShaderPassBuildInfo[0];

            var result = new NBShaderPassBuildInfo[passes.Length];
            for (var i = 0; i < passes.Length; i++)
            {
                var pass = passes[i];
                result[i] = new NBShaderPassBuildInfo(
                    pass.passName,
                    NBShaderBuildInfoUtility.GetPassType(pass.passName),
                    pass.enabledByMaterial,
                    pass.allowedByTier,
                    pass.included,
                    pass.reason);
            }

            return result;
        }

        private static string[] ExtractManagedKeywords(Shader shader, ShaderCompilerData compilerData)
        {
            if (shader == null)
                return new string[0];

            var result = new List<string>();
            var keywords = compilerData.shaderKeywordSet.GetShaderKeywords();
            for (var i = 0; i < keywords.Length; i++)
            {
                #pragma warning disable 0618 // Unity 2021-compatible keyword name API.
                var keywordName = ShaderKeyword.GetKeywordName(shader, keywords[i]);
                #pragma warning restore 0618
                if (NBShaderFeatureLevelCatalog.IsManagedKeyword(keywordName))
                    result.Add(keywordName);
            }

            return NBShaderBuildInfoUtility.ToCatalogOrderedManagedKeywords(result);
        }

        private static bool IsPassIncluded(string passName, PassType passType, NBShaderBuildInfoSet buildInfo)
        {
            if (buildInfo == null)
                return true;

            if (!string.IsNullOrEmpty(passName))
                return NBShaderBuildInfoUtility.ContainsPassName(buildInfo.includedPassNames, passName);

            var variants = buildInfo.variants;
            for (var i = 0; i < variants.Length; i++)
            {
                var variant = variants[i];
                if (variant != null && variant.passType == passType)
                    return true;
            }

            return false;
        }

        private static bool IsPassMatch(string passName, PassType passType, string targetPassName, PassType targetPassType)
        {
            if (!string.IsNullOrEmpty(passName) && !string.IsNullOrEmpty(targetPassName))
                return string.Equals(passName, targetPassName, StringComparison.Ordinal);

            return passType == targetPassType;
        }

        private static bool IsSameShader(Shader a, Shader b)
        {
            if (a == null || b == null)
                return false;

            return ReferenceEquals(a, b) || string.Equals(a.name, b.name, StringComparison.Ordinal);
        }
    }
}
