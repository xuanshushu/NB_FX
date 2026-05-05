using System.Collections.Generic;
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

            var projectSettings = NBShaderFeatureLevelProjectSettings.instance;
            projectSettings.EnsureInitialized();

            HashSet<string> allowed;
            if (buildSettings.policy == NBShaderBuildStripPolicy.QualityMappedUnion)
                allowed = projectSettings.GetQualityMappedUnionAllowedKeywordSet();
            else
                allowed = projectSettings.GetAllowedKeywordSet(buildSettings.explicitTier);

            for (var i = data.Count - 1; i >= 0; i--)
            {
                if (ContainsDisallowedManagedKeyword(shader, data[i], allowed))
                    data.RemoveAt(i);
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

                if (allowed == null || !allowed.Contains(keywordName))
                    return true;
            }
            return false;
        }
    }
}
