using System;
using System.Collections.Generic;
using NBShader;
using NBShaders2.Editor.FeatureLevel;
using UnityEditor;
using UnityEngine;

namespace NBShaderEditor
{
    public class NBShaderGUIContext
    {
        private const string FeatureTierPropertyName = "_NBShaderFeatureTier";

        private readonly NBShaderRootItem _rootItem;
        private HashSet<string> _currentTierAllowedKeywords;

        public NBShaderGUIContext(NBShaderRootItem rootItem)
        {
            _rootItem = rootItem;
        }

        public MeshSourceMode MeshSourceMode { get; private set; } = MeshSourceMode.UnKnowOrMixed;
        public TransparentMode TransparentMode { get; private set; } = TransparentMode.UnKnowOrMixed;
        public MixedBool UIEffectEnabled { get; private set; } = MixedBool.Mixed;
        public MixedBool UseGraphicMainTex { get; private set; } = MixedBool.Mixed;
        public MixedBool ParticleMode { get; private set; } = MixedBool.Mixed;
        public MixedBool NoiseEnabled { get; private set; } = MixedBool.Mixed;
        public MixedBool ProgramNoiseEnabled { get; private set; } = MixedBool.Mixed;
        public MixedBool VatEnabled { get; private set; } = MixedBool.Mixed;
        public MixedBool FlipbookEnabled { get; private set; } = MixedBool.Mixed;
        public FxLightMode FxLightMode { get; private set; } = FxLightMode.UnKnownOrMixedValue;
        public NBShaderFeatureTier CurrentTier { get; private set; } = NBShaderFeatureTier.Ultra;
        public bool CurrentTierMixed { get; private set; }

        public bool IsKeywordAllowed(string keyword)
        {
            if (!NBShaderFeatureCatalog.IsManagedKeyword(keyword))
            {
                return true;
            }

            if (CurrentTierMixed)
            {
                return true;
            }

            return _currentTierAllowedKeywords != null && _currentTierAllowedKeywords.Contains(keyword);
        }

        public bool AreKeywordsAllowed(params string[] keywords)
        {
            if (keywords == null)
            {
                return true;
            }

            for (int i = 0; i < keywords.Length; i++)
            {
                if (!IsKeywordAllowed(keywords[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public bool IsAnyKeywordAllowed(params string[] keywords)
        {
            if (keywords == null || keywords.Length == 0)
            {
                return true;
            }

            for (int i = 0; i < keywords.Length; i++)
            {
                if (IsKeywordAllowed(keywords[i]))
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsAnyKeywordAllowed(string keyword0, string keyword1)
        {
            return IsKeywordAllowed(keyword0) || IsKeywordAllowed(keyword1);
        }

        public bool IsAnyKeywordAllowed(
            string keyword0,
            string keyword1,
            string keyword2,
            string keyword3,
            string keyword4)
        {
            return IsKeywordAllowed(keyword0) ||
                   IsKeywordAllowed(keyword1) ||
                   IsKeywordAllowed(keyword2) ||
                   IsKeywordAllowed(keyword3) ||
                   IsKeywordAllowed(keyword4);
        }

        public static bool IsCatalogKeyword(string keyword)
        {
            return NBShaderFeatureCatalog.IsManagedKeyword(keyword);
        }

        public bool HasProperty(string propertyName)
        {
            return _rootItem.PropertyInfoDic.ContainsKey(propertyName);
        }

        public MaterialProperty GetProperty(string propertyName)
        {
            return _rootItem.PropertyInfoDic[propertyName].Property;
        }

        public void Refresh()
        {
            RefreshFeatureTier();

            if (HasProperty("_MeshSourceMode"))
            {
                MaterialProperty meshSourceModeProperty = GetProperty("_MeshSourceMode");
                MeshSourceMode = meshSourceModeProperty.hasMixedValue
                    ? MeshSourceMode.UnKnowOrMixed
                    : (MeshSourceMode)meshSourceModeProperty.floatValue;
            }

            if (HasProperty("_TransparentMode"))
            {
                MaterialProperty transparentModeProperty = GetProperty("_TransparentMode");
                TransparentMode = transparentModeProperty.hasMixedValue
                    ? TransparentMode.UnKnowOrMixed
                    : (TransparentMode)transparentModeProperty.floatValue;
            }

            if (MeshSourceMode == MeshSourceMode.UnKnowOrMixed)
            {
                UIEffectEnabled = MixedBool.Mixed;
                UseGraphicMainTex = MixedBool.Mixed;
                ParticleMode = MixedBool.Mixed;
            }
            else
            {
                UIEffectEnabled = (int)MeshSourceMode >= 2 ? MixedBool.True : MixedBool.False;
                UseGraphicMainTex = MeshSourceMode == MeshSourceMode.UIEffectRawImage || MeshSourceMode == MeshSourceMode.UIEffectSprite
                    ? MixedBool.True
                    : MixedBool.False;
                ParticleMode = MeshSourceMode == MeshSourceMode.Particle || MeshSourceMode == MeshSourceMode.UIParticle
                    ? MixedBool.True
                    : MixedBool.False;
            }

            if (HasProperty("_FxLightMode"))
            {
                MaterialProperty lightModeProperty = GetProperty("_FxLightMode");
                FxLightMode = lightModeProperty.hasMixedValue
                    ? FxLightMode.UnKnownOrMixedValue
                    : (FxLightMode)lightModeProperty.floatValue;
            }

            NoiseEnabled = IsKeywordAllowed("_NOISEMAP") ? GetToggleState("_noisemapEnabled") : MixedBool.False;
            ProgramNoiseEnabled = IsKeywordAllowed("_PROGRAM_NOISE") ? GetToggleState("_ProgramNoise_Toggle") : MixedBool.False;
            VatEnabled = IsKeywordAllowed("_VAT") ? GetToggleState("_VAT_Toggle") : MixedBool.False;
            FlipbookEnabled = IsKeywordAllowed("_FLIPBOOKBLENDING_ON") ? GetToggleState("_FlipbookBlending") : MixedBool.False;
        }

        private void RefreshFeatureTier()
        {
            if (!HasProperty(FeatureTierPropertyName))
            {
                CurrentTier = NBShaderFeatureTier.Ultra;
                CurrentTierMixed = false;
                _currentTierAllowedKeywords =
                    NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForReadOnlyUse(CurrentTier);
                return;
            }

            MaterialProperty tierProperty = GetProperty(FeatureTierPropertyName);
            CurrentTierMixed = tierProperty.hasMixedValue;
            CurrentTier = CurrentTierMixed
                ? NBShaderFeatureTier.Ultra
                : ToFeatureTier(Mathf.RoundToInt(tierProperty.floatValue));
            _currentTierAllowedKeywords = CurrentTierMixed
                ? null
                : NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForReadOnlyUse(CurrentTier);
        }

        private static NBShaderFeatureTier ToFeatureTier(int value)
        {
            return Enum.IsDefined(typeof(NBShaderFeatureTier), value) && value >= 0
                ? (NBShaderFeatureTier)value
                : NBShaderFeatureTier.Ultra;
        }

        public MixedBool GetToggleState(string propertyName)
        {
            if (!HasProperty(propertyName))
            {
                return MixedBool.False;
            }

            MaterialProperty property = GetProperty(propertyName);
            if (property.hasMixedValue)
            {
                return MixedBool.Mixed;
            }

            return property.floatValue > 0.5f ? MixedBool.True : MixedBool.False;
        }

        public bool IsToggleOn(string propertyName)
        {
            return GetToggleState(propertyName) == MixedBool.True;
        }
    }
}
