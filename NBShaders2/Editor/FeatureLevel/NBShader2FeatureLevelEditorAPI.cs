using System;
using System.Collections.Generic;
using NBShader;

namespace NBShaders2.Editor.FeatureLevel
{
    /// <summary>
    /// Public Editor API for CI/custom build scripts. Override scopes are process-local and never saved
    /// into ProjectSettings/NBShader2FeatureLevels.asset.
    /// </summary>
    public static class NBShader2FeatureLevelEditorAPI
    {
        public static IDisposable OverrideBuildStripPolicy(NBShader2BuildStripPolicy policy, NBShader2FeatureTier explicitTier)
        {
            return NBShader2FeatureLevelBuildStripOverride.Push(policy, explicitTier);
        }

        public static IDisposable OverrideBuildStripExplicitTier(NBShader2FeatureTier tier)
        {
            return NBShader2FeatureLevelBuildStripOverride.PushExplicitTier(tier);
        }

        public static IDisposable DisableBuildStripping()
        {
            return NBShader2FeatureLevelBuildStripOverride.PushDisabled();
        }

        public static void ClearBuildStripOverrides()
        {
            NBShader2FeatureLevelBuildStripOverride.ClearAll();
        }

        public static HashSet<string> GetAllowedManagedKeywords(NBShader2FeatureTier tier)
        {
            return NBShader2FeatureLevelProjectSettings.instance.GetAllowedKeywordSet(tier);
        }

        public static bool SyncRuntimeSettingsAsset()
        {
            return NBShader2RuntimeSettingsSynchronizer.SyncFromProjectSettings();
        }
    }
}
