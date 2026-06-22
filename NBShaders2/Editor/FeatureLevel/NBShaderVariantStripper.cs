using System.Collections.Generic;
using NBShader;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace NBShaders2.Editor.FeatureLevel
{
    public sealed class NBShaderVariantStripper : IPreprocessShaders, IPreprocessBuildWithReport
    {
        private static bool s_MissingExplicitTierWarningLogged;

        public int callbackOrder { get { return 0; } }

        public void OnPreprocessBuild(BuildReport report)
        {
            ResetMissingExplicitTierWarning();
        }

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            if (shader == null || shader.name != NBShaderFeatureLevelCatalog.ShaderName || data == null || data.Count == 0)
                return;

            NBShaderFeatureTier tier;
            if (!NBShaderFeatureLevelBuildStripOverride.TryGetCurrentTier(out tier))
                LogMissingExplicitTierWarning();

            var projectSettings = NBShaderFeatureLevelProjectSettings.instance;
            HashSet<string> allowedKeywords = projectSettings.GetAllowedKeywordSetForBuildInfoNoSave(tier);
            HashSet<string> allowedPassFeatures = projectSettings.GetAllowedPassFeatureSetForBuildInfoNoSave(tier);

            if (IsDisallowedManagedPass(snippet, allowedPassFeatures))
            {
                data.Clear();
                return;
            }

            for (var i = data.Count - 1; i >= 0; i--)
            {
                if (ContainsDisallowedManagedKeyword(shader, data[i], allowedKeywords))
                    data.RemoveAt(i);
            }
        }

        private static void LogMissingExplicitTierWarning()
        {
            if (s_MissingExplicitTierWarningLogged)
                return;

            s_MissingExplicitTierWarningLogged = true;
            Debug.LogWarning(
                "NBShader2 build stripping tier was not specified. Defaulting to Ultra. " +
                "Wrap BuildPipeline.BuildPlayer or BuildPipeline.BuildAssetBundles in " +
                "NBShaderFeatureLevelEditorAPI.OverrideBuildStripExplicitTier(tier) to build lower-tier NBShader variants intentionally.");
        }

        internal static void ResetMissingExplicitTierWarning()
        {
            s_MissingExplicitTierWarningLogged = false;
        }

        private static bool IsDisallowedManagedPass(ShaderSnippetData snippet, HashSet<string> allowedPassFeatures)
        {
            if (allowedPassFeatures == null)
                return false;

            string passFeatureId;
            if (!TryResolveManagedPassFeature(snippet, out passFeatureId))
                return false;

            return !allowedPassFeatures.Contains(passFeatureId);
        }

        private static bool TryResolveManagedPassFeature(ShaderSnippetData snippet, out string passFeatureId)
        {
            if (!string.IsNullOrEmpty(snippet.passName) &&
                NBShaderFeatureLevelCatalog.TryGetManagedPassFeatureByPassName(snippet.passName, out passFeatureId))
            {
                return true;
            }

            switch (snippet.passType)
            {
                case PassType.ScriptableRenderPipelineDefaultUnlit:
                    passFeatureId = NBShaderPassFeatureCatalog.BackFirstPassId;
                    return true;
                case PassType.ShadowCaster:
                    passFeatureId = NBShaderPassFeatureCatalog.ShadowCasterPassId;
                    return true;
                default:
                    passFeatureId = string.Empty;
                    return false;
            }
        }

        private static bool ContainsDisallowedManagedKeyword(Shader shader, ShaderCompilerData compilerData, HashSet<string> allowed)
        {
            var keywords = compilerData.shaderKeywordSet.GetShaderKeywords();
            for (var i = 0; i < keywords.Length; i++)
            {
                var keywordName = keywords[i].name;
                if (!NBShaderFeatureLevelCatalog.IsManagedKeyword(keywordName))
                    continue;

                if (allowed != null && !allowed.Contains(keywordName))
                    return true;
            }
            return false;
        }
    }
}
