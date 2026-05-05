using System;
using System.Collections.Generic;
using NBShader;

namespace NBShaders2.Editor.FeatureLevel
{
    public struct NBShaderBuildStripSettings
    {
        public NBShaderBuildStripPolicy policy;
        public NBShaderFeatureTier explicitTier;

        public NBShaderBuildStripSettings(NBShaderBuildStripPolicy policy, NBShaderFeatureTier explicitTier)
        {
            this.policy = policy;
            this.explicitTier = explicitTier;
        }
    }

    public static class NBShaderFeatureLevelBuildStripOverride
    {
        private static readonly Stack<NBShaderBuildStripSettings> s_OverrideStack = new Stack<NBShaderBuildStripSettings>();

        public static bool hasOverride { get { return s_OverrideStack.Count > 0; } }

        public static NBShaderBuildStripSettings current
        {
            get
            {
                if (s_OverrideStack.Count > 0)
                    return s_OverrideStack.Peek();

                var settings = NBShaderFeatureLevelProjectSettings.instance;
                settings.EnsureInitialized();
                return new NBShaderBuildStripSettings(settings.buildStripPolicy, settings.explicitTier);
            }
        }

        public static IDisposable Push(NBShaderBuildStripPolicy policy, NBShaderFeatureTier explicitTier)
        {
            s_OverrideStack.Push(new NBShaderBuildStripSettings(policy, explicitTier));
            return new Scope();
        }

        public static IDisposable PushExplicitTier(NBShaderFeatureTier tier)
        {
            return Push(NBShaderBuildStripPolicy.ExplicitTier, tier);
        }

        public static IDisposable PushDisabled()
        {
            return Push(NBShaderBuildStripPolicy.Disabled, NBShaderFeatureLevelProjectSettings.instance.explicitTier);
        }

        public static void ClearAll()
        {
            s_OverrideStack.Clear();
        }

        private sealed class Scope : IDisposable
        {
            private bool m_Disposed;

            public void Dispose()
            {
                if (m_Disposed)
                    return;
                m_Disposed = true;
                if (s_OverrideStack.Count > 0)
                    s_OverrideStack.Pop();
            }
        }
    }
}
