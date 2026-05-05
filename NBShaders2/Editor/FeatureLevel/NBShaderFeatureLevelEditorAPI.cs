using System;
using System.Collections.Generic;
using NBShader;

namespace NBShaders2.Editor.FeatureLevel
{
    /// <summary>
    /// Public Editor API for CI/custom build scripts. Override scopes are process-local and never saved
    /// into ProjectSettings/NBShaderFeatureLevels.asset.
    /// </summary>
    public static class NBShaderFeatureLevelEditorAPI
    {
        public static IDisposable OverrideBuildStripPolicy(NBShaderBuildStripPolicy policy, NBShaderFeatureTier explicitTier)
        {
            return NBShaderFeatureLevelBuildStripOverride.Push(policy, explicitTier);
        }

        public static IDisposable OverrideBuildStripExplicitTier(NBShaderFeatureTier tier)
        {
            return NBShaderFeatureLevelBuildStripOverride.PushExplicitTier(tier);
        }

        public static IDisposable DisableBuildStripping()
        {
            return NBShaderFeatureLevelBuildStripOverride.PushDisabled();
        }

        public static void ClearBuildStripOverrides()
        {
            NBShaderFeatureLevelBuildStripOverride.ClearAll();
        }

        public static HashSet<string> GetAllowedManagedKeywords(NBShaderFeatureTier tier)
        {
            return NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSet(tier);
        }

        public static bool SyncRuntimeSettingsAsset()
        {
            return NBShaderRuntimeSettingsSynchronizer.SyncFromProjectSettings();
        }
    }
}
