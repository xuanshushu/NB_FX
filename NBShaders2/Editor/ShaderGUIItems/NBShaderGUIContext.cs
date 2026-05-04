using System;
using NBShader;
using NBShaders2.Editor.FeatureLevel;
using UnityEditor;
using UnityEngine;

namespace NBShaderEditor
{
    public class NBShaderGUIContext
    {
        private const string FeatureTierPropertyName = "_NBShader2FeatureTier";

        private readonly NBShaderRootItem _rootItem;

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
        public NBShader2FeatureTier CurrentTier { get; private set; } = NBShader2FeatureTier.Ultra;
        public bool CurrentTierMixed { get; private set; }

        public bool IsKeywordAllowed(string keyword)
        {
            if (!NBShader2FeatureCatalog.IsManagedKeyword(keyword))
            {
                return true;
            }

            if (CurrentTierMixed)
            {
                return true;
            }

            return NBShader2FeatureLevelProjectSettings.instance.GetAllowedKeywordSet(CurrentTier).Contains(keyword);
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

        public static bool IsCatalogKeyword(string keyword)
        {
            return NBShader2FeatureCatalog.IsManagedKeyword(keyword);
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
                CurrentTier = NBShader2FeatureTier.Ultra;
                CurrentTierMixed = false;
                return;
            }

            MaterialProperty tierProperty = GetProperty(FeatureTierPropertyName);
            CurrentTierMixed = tierProperty.hasMixedValue;
            CurrentTier = CurrentTierMixed
                ? NBShader2FeatureTier.Ultra
                : ToFeatureTier(Mathf.RoundToInt(tierProperty.floatValue));
        }

        private static NBShader2FeatureTier ToFeatureTier(int value)
        {
            return Enum.IsDefined(typeof(NBShader2FeatureTier), value) && value >= 0
                ? (NBShader2FeatureTier)value
                : NBShader2FeatureTier.Ultra;
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
