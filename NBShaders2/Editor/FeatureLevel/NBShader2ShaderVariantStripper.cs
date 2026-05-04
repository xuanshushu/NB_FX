using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace NBShaders2.Editor.FeatureLevel
{
    public sealed class NBShader2ShaderVariantStripper : IPreprocessShaders
    {
        public int callbackOrder { get { return 0; } }

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            if (shader == null || shader.name != NBShader2FeatureLevelCatalog.ShaderName || data == null || data.Count == 0)
                return;

            var buildSettings = NBShader2FeatureLevelBuildStripOverride.current;
            if (buildSettings.policy == NBShader2BuildStripPolicy.Disabled)
                return;

            var projectSettings = NBShader2FeatureLevelProjectSettings.instance;
            projectSettings.EnsureInitialized();

            HashSet<string> allowed;
            if (buildSettings.policy == NBShader2BuildStripPolicy.QualityMappedUnion)
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
                if (!NBShader2FeatureLevelCatalog.IsManagedKeyword(keywordName))
                    continue;

                if (allowed == null || !allowed.Contains(keywordName))
                    return true;
            }
            return false;
        }
    }
}
