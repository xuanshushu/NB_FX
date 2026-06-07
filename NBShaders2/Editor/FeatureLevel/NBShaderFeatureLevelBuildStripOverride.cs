using System;
using System.Collections.Generic;
using NBShader;

namespace NBShaders2.Editor.FeatureLevel
{
    public static class NBShaderFeatureLevelBuildStripOverride
    {
        private static readonly Stack<NBShaderFeatureTier> s_OverrideStack = new Stack<NBShaderFeatureTier>();

        public static bool hasOverride { get { return s_OverrideStack.Count > 0; } }

        internal static bool TryGetCurrentTier(out NBShaderFeatureTier tier)
        {
            if (s_OverrideStack.Count > 0)
            {
                tier = s_OverrideStack.Peek();
                return true;
            }

            tier = NBShaderFeatureTier.Ultra;
            return false;
        }

        public static IDisposable PushExplicitTier(NBShaderFeatureTier tier)
        {
            NBShaderVariantStripper.ResetMissingExplicitTierWarning();
            s_OverrideStack.Push(tier);
            return new Scope();
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
