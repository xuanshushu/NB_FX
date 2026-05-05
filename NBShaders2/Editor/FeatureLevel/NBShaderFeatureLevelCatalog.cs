using System;
using System.Collections.Generic;
using NBShader;

namespace NBShaders2.Editor.FeatureLevel
{
    public static class NBShaderFeatureLevelCatalog
    {
        public const string ShaderName = NBShaderFeatureCatalog.ShaderName;

        public static string[] ManagedKeywords
        {
            get { return (string[])NBShaderFeatureCatalog.RawKeywords.Clone(); }
        }

        public static bool IsManagedKeyword(string keyword)
        {
            return NBShaderFeatureCatalog.IsManagedKeyword(keyword);
        }
    }
}
