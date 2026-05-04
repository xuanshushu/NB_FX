using System;
using NBShader;

namespace NBShaders2.Editor.FeatureLevel
{
    public enum NBShader2BuildStripPolicy
    {
        Disabled = 0,
        ExplicitTier = 1,
        QualityMappedUnion = 2,
    }

    [Serializable]
    public sealed class NBShader2FeatureTierKeywordSet
    {
        public NBShader2FeatureTier tier;
        public string[] allowedKeywords = new string[0];
    }

    [Serializable]
    public sealed class NBShader2QualityTierMapping
    {
        public string qualityName;
        public NBShader2FeatureTier tier;
    }
}
