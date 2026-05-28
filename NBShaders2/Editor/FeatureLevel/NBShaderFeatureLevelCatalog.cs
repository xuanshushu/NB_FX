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

        public static string[] ManagedPassFeatures
        {
            get { return (string[])NBShaderPassFeatureCatalog.RawPassFeatureIds.Clone(); }
        }

        public static bool IsManagedKeyword(string keyword)
        {
            return NBShaderFeatureCatalog.IsManagedKeyword(keyword);
        }

        public static bool IsManagedPassFeature(string passFeatureId)
        {
            return NBShaderPassFeatureCatalog.IsManagedPassFeature(passFeatureId);
        }

        public static bool TryGetManagedPassFeatureInfo(
            string passFeatureId,
            out string passName,
            out string displayName)
        {
            NBShaderPassFeatureInfo feature;
            if (NBShaderPassFeatureCatalog.TryGetPassFeature(passFeatureId, out feature))
            {
                passName = feature.passName;
                displayName = feature.displayName;
                return true;
            }

            passName = string.Empty;
            displayName = string.Empty;
            return false;
        }

        public static bool TryGetManagedPassFeatureByPassName(
            string passName,
            out string passFeatureId)
        {
            NBShaderPassFeatureInfo feature;
            if (NBShaderPassFeatureCatalog.TryGetPassFeatureByPassName(passName, out feature))
            {
                passFeatureId = feature.id;
                return true;
            }

            passFeatureId = string.Empty;
            return false;
        }
    }
}
