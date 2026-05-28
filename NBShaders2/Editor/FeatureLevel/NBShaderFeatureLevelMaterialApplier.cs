using System.Collections.Generic;
using NBShader;
using UnityEngine;

namespace NBShaders2.Editor.FeatureLevel
{
    internal static class NBShaderFeatureLevelMaterialApplier
    {
        private const string FeatureTierPropertyName = "_NBShaderFeatureTier";

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
