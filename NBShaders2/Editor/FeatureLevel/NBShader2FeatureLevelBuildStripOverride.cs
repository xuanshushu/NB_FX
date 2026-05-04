using System;
using System.Collections.Generic;
using NBShader;

namespace NBShaders2.Editor.FeatureLevel
{
    public struct NBShader2BuildStripSettings
    {
        public NBShader2BuildStripPolicy policy;
        public NBShader2FeatureTier explicitTier;

        public NBShader2BuildStripSettings(NBShader2BuildStripPolicy policy, NBShader2FeatureTier explicitTier)
        {
            this.policy = policy;
            this.explicitTier = explicitTier;
        }
    }

    public static class NBShader2FeatureLevelBuildStripOverride
    {
        private static readonly Stack<NBShader2BuildStripSettings> s_OverrideStack = new Stack<NBShader2BuildStripSettings>();

        public static bool hasOverride { get { return s_OverrideStack.Count > 0; } }

        public static NBShader2BuildStripSettings current
        {
            get
            {
                if (s_OverrideStack.Count > 0)
                    return s_OverrideStack.Peek();

                var settings = NBShader2FeatureLevelProjectSettings.instance;
                settings.EnsureInitialized();
                return new NBShader2BuildStripSettings(settings.buildStripPolicy, settings.explicitTier);
            }
        }

        public static IDisposable Push(NBShader2BuildStripPolicy policy, NBShader2FeatureTier explicitTier)
        {
            s_OverrideStack.Push(new NBShader2BuildStripSettings(policy, explicitTier));
            return new Scope();
        }

        public static IDisposable PushExplicitTier(NBShader2FeatureTier tier)
        {
            return Push(NBShader2BuildStripPolicy.ExplicitTier, tier);
        }

        public static IDisposable PushDisabled()
        {
            return Push(NBShader2BuildStripPolicy.Disabled, NBShader2FeatureLevelProjectSettings.instance.explicitTier);
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
