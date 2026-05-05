using System;
using System.Collections.Generic;
using NBShader;

namespace NBShaders2.Editor.FeatureLevel
{
    public static class NBShader2FeatureLevelCatalog
    {
        public const string ShaderName = NBShader2FeatureCatalog.ShaderName;

        public static string[] ManagedKeywords
        {
            get { return (string[])NBShader2FeatureCatalog.RawKeywords.Clone(); }
        }

        public static bool IsManagedKeyword(string keyword)
        {
            return NBShader2FeatureCatalog.IsManagedKeyword(keyword);
        }
    }
}
