using System;
using NBShader;

namespace NBShaders2.Editor.FeatureLevel
{
    [Serializable]
    public sealed class NBShaderFeatureTierKeywordSet
    {
        public NBShaderFeatureTier tier;
        public string[] allowedKeywords = new string[0];
    }

    [Serializable]
    public sealed class NBShaderFeatureTierPassSet
    {
        public NBShaderFeatureTier tier;
        public string[] allowedPassFeatures = new string[0];
    }

    [Serializable]
    public sealed class NBShaderQualityTierMapping
    {
        public string qualityName;
        public NBShaderFeatureTier tier;
    }
}
