using System.Collections.Generic;
using NBShader;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace NBShaders2.Editor.FeatureLevel
{
    public sealed class NBShaderVariantStripper : IPreprocessShaders
    {
        public int callbackOrder { get { return 0; } }

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            if (shader == null || shader.name != NBShaderFeatureLevelCatalog.ShaderName || data == null || data.Count == 0)
                return;

            var buildSettings = NBShaderFeatureLevelBuildStripOverride.current;
            if (buildSettings.policy == NBShaderBuildStripPolicy.Disabled)
                return;

            HashSet<string> allowedKeywords;
            HashSet<string> allowedPassFeatures;
            var projectSettings = NBShaderFeatureLevelProjectSettings.instance;
            if (buildSettings.policy == NBShaderBuildStripPolicy.QualityMappedUnion)
            {
                allowedKeywords = projectSettings.GetQualityMappedUnionAllowedKeywordSetForBuildInfoNoSave();
                allowedPassFeatures = projectSettings.GetQualityMappedUnionAllowedPassFeatureSetForBuildInfoNoSave();
            }
            else
            {
                allowedKeywords = projectSettings.GetAllowedKeywordSetForBuildInfoNoSave(buildSettings.explicitTier);
                allowedPassFeatures = projectSettings.GetAllowedPassFeatureSetForBuildInfoNoSave(buildSettings.explicitTier);
            }

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
                #pragma warning disable 0618 // Unity 2021-compatible keyword name API.
                var keywordName = ShaderKeyword.GetKeywordName(shader, keywords[i]);
                #pragma warning restore 0618
                if (!NBShaderFeatureLevelCatalog.IsManagedKeyword(keywordName))
                    continue;

                if (allowed != null && !allowed.Contains(keywordName))
                    return true;
            }
            return false;
        }
    }
}
