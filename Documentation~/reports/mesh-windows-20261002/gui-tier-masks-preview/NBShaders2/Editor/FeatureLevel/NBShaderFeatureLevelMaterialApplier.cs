using System.Collections.Generic;
using NBShader;
using UnityEngine;

namespace NBShaders2.Editor.FeatureLevel
{
    internal static class NBShaderFeatureLevelMaterialApplier
    {
        private const string FeatureTierPropertyName = "_NBShaderFeatureTier";

        // Explicit Mask1/2/3 capability only. The serialized toggles and raw
        // flags remain intent; the normalized reader owns parent dependencies.
        // General Apply, Toolbar and Validate remain protected for Graph.
        public static bool ApplyGraphMaskGroup(Material material, NBShaderFeatureTier tier,
            IEnumerable<string> allowedManagedKeywords, out bool changed)
        {
            changed = false;
            foreach (string property in new[] { "_NB_TierAllowMask", "_NB_TierAllowMask2", "_NB_TierAllowMask3" })
                if (!NBShaderMaterialIntentResolver.HasFloatShaderProperty(material, property)) return false;
            var allowed = allowedManagedKeywords ?? NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForBuildInfoNoSave(tier);
            NBShaderMaterialIntentResult result;
            string[] unavailable;
            if (!NBShaderMaterialIntentResolver.TryResolveGraphSupportedKeywordIntent(material, tier, allowed,
                out result, out unavailable)) return false;
            var effective = new HashSet<string>(result.effectiveKeywords);
            changed |= SetGraphAllowFloat(material, "_NB_TierAllowMask", effective.Contains("_MASKMAP_ON"));
            changed |= SetGraphAllowFloat(material, "_NB_TierAllowMask2", effective.Contains("_MASKMAP2_ON"));
            changed |= SetGraphAllowFloat(material, "_NB_TierAllowMask3", effective.Contains("_MASKMAP3_ON"));
            return true;
        }

        // Explicit first-pair experiment only. Ordinary Apply/toolbar/Validate
        // remain guarded until every Graph feature and pass has a real consumer.
        // Serialized feature inputs, packed flags and URP state stay intent.
        public static bool ApplyGraphNoisePair(Material material, NBShaderFeatureTier tier,
            IEnumerable<string> allowedManagedKeywords, out bool changed)
        {
            changed = false;
            if (!NBShaderMaterialIntentResolver.HasFloatShaderProperty(material, "_NB_TierAllowNoise") ||
                !NBShaderMaterialIntentResolver.HasFloatShaderProperty(material, "_NB_TierAllowNoiseMask")) return false;
            var allowed = allowedManagedKeywords ?? NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForBuildInfoNoSave(tier);
            bool noise, mask;
            if (!NBShaderMaterialIntentResolver.TryResolveGraphNoisePair(material, allowed, out noise, out mask)) return false;
            changed |= SetGraphAllowFloat(material, "_NB_TierAllowNoise", noise);
            changed |= SetGraphAllowFloat(material, "_NB_TierAllowNoiseMask", mask);
            return true;
        }

        static bool SetGraphAllowFloat(Material material, string name, bool enabled)
        {
            float value = enabled ? 1f : 0f;
            if (material.GetFloat(name) == value) return false;
            material.SetFloat(name, value);
            return true;
        }

        public static bool Apply(Material material, NBShaderFeatureTier tier, bool writeTierProperty, bool applyResolvedState)
        {
            bool changed;
            return Apply(material, tier, writeTierProperty, applyResolvedState, out changed);
        }

        public static bool Apply(
            Material material,
            NBShaderFeatureTier tier,
            bool writeTierProperty,
            bool applyResolvedState,
            out bool changed)
        {
            changed = false;
            if (!NBShaderMaterialIntentResolver.IsNBShaderMaterial(material))
                return false;

            if (writeTierProperty &&
                material.HasProperty(FeatureTierPropertyName) &&
                !Mathf.Approximately(material.GetFloat(FeatureTierPropertyName), (float)tier))
            {
                material.SetFloat(FeatureTierPropertyName, (float)tier);
                changed = true;
            }

            if (!applyResolvedState)
                return true;

            var allowedKeywords = NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForBuildInfoNoSave(tier);
            var allowedPassFeatures = NBShaderFeatureLevelProjectSettings.instance.GetAllowedPassFeatureSetForBuildInfoNoSave(tier);
            var result = NBShaderMaterialIntentResolver.Resolve(material, tier, allowedKeywords, allowedPassFeatures);
            changed |= ApplyResolvedIntent(material, result);
            return true;
        }

        private static bool ApplyResolvedIntent(Material material, NBShaderMaterialIntentResult result)
        {
            if (material == null || result == null)
                return false;

            var changed = false;
            var effectiveKeywords = new HashSet<string>(result.effectiveKeywords);
            for (int i = 0; i < NBShaderFeatureCatalog.RawKeywords.Length; i++)
            {
                string keyword = NBShaderFeatureCatalog.RawKeywords[i];
                changed |= SetKeyword(material, keyword, effectiveKeywords.Contains(keyword));
            }

            changed |= SetKeyword(material, "EVALUATE_SH_VERTEX", effectiveKeywords.Contains("_FX_LIGHT_MODE_SIX_WAY"));

            for (int i = 0; i < result.passes.Length; i++)
            {
                NBShaderPassIntent pass = result.passes[i];
                if (!string.IsNullOrEmpty(pass.passName) &&
                    material.GetShaderPassEnabled(pass.passName) != pass.included)
                {
                    material.SetShaderPassEnabled(pass.passName, pass.included);
                    changed = true;
                }
            }

            return changed;
        }

        private static bool SetKeyword(Material material, string keyword, bool enabled)
        {
            if (material == null || string.IsNullOrEmpty(keyword) || material.IsKeywordEnabled(keyword) == enabled)
                return false;

            if (enabled)
                material.EnableKeyword(keyword);
            else
                material.DisableKeyword(keyword);

            return true;
        }
    }
}
