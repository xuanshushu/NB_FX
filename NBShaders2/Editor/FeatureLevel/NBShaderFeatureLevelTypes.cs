using System;
using NBShader;

namespace NBShaders2.Editor.FeatureLevel
{
    public enum NBShaderBuildStripPolicy
    {
        Disabled = 0,
        ExplicitTier = 1,
        QualityMappedUnion = 2,
    }

    [Serializable]
    public sealed class NBShaderFeatureTierKeywordSet
    {
        public NBShaderFeatureTier tier;
        public string[] allowedKeywords = new string[0];
    }

    [Serializable]
    public sealed class NBShaderQualityTierMapping
    {
        public string qualityName;
        public NBShaderFeatureTier tier;
    }
}
