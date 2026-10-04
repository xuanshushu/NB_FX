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

        // Only an editor host distinction, not a GraphMPB/VFX capability claim.
        public bool IsGraphMaterialHost { get; private set; }
        public bool HasMixedMaterialHosts { get; private set; }

        public static bool IsGraphMaterial(Material material)
        {
            // Matches the existing Material halfword-store signature. Do not
            // guess a host from the first material or require a legacy name.
            return material != null && material.shader != null &&
                material.HasProperty("_NB_DistortionMode") &&
                material.HasProperty("_NB_Flags0Lo16") && material.HasProperty("_NB_Flags0Hi16") &&
                material.HasProperty("_NB_Flags1Lo16") && material.HasProperty("_NB_Flags1Hi16");
        }

        public static bool HasMixedHosts(IList<Material> materials)
        {
            if (materials == null || materials.Count < 2) return false;
            bool first = false, graph = false;
            Shader graphShader = null;
            foreach (Material material in materials)
            {
                if (material == null) continue;
                bool current = IsGraphMaterial(material);
                if (!first) { first = true; graph = current; graphShader = material.shader; }
                else if (current != graph || (graph && material.shader != graphShader)) return true;
            }
            return false;
        }

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
            // GUI1A has no effective Graph Tier projection. Do not falsely
            // gate existing native Graph inputs via an unimplemented Tier.
            if (IsGraphMaterialHost) return true;
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
            HasMixedMaterialHosts = HasMixedHosts(_rootItem.Mats);
            IsGraphMaterialHost = !HasMixedMaterialHosts && _rootItem.Mats != null &&
                _rootItem.Mats.Count > 0 && IsGraphMaterial(_rootItem.Mats[0]);
            RefreshFeatureTier();

            if (IsGraphMaterialHost)
            {
                MeshSourceMode = MeshSourceMode.Mesh;
                UIEffectEnabled = UseGraphicMainTex = ParticleMode = MixedBool.False;
                // Official URP properties are read-only here. No shadow
                // _TransparentMode or second surface-state writer.
                TransparentMode = TransparentMode.UnKnowOrMixed;
                if (HasProperty("_Surface") && !GetProperty("_Surface").hasMixedValue &&
                    HasProperty("_AlphaClip") && !GetProperty("_AlphaClip").hasMixedValue)
                {
                    TransparentMode = GetProperty("_Surface").floatValue > 0.5f
                        ? TransparentMode.Transparent
                        : GetProperty("_AlphaClip").floatValue > 0.5f
                            ? TransparentMode.CutOff : TransparentMode.Opaque;
                }
                if (HasProperty("_FxLightMode"))
                {
                    MaterialProperty light = GetProperty("_FxLightMode");
                    FxLightMode = light.hasMixedValue ? FxLightMode.UnKnownOrMixedValue : (FxLightMode)light.floatValue;
                }
                NoiseEnabled = GetToggleState("_noisemapEnabled");
                ProgramNoiseEnabled = GetToggleState("_ProgramNoise_Toggle");
                // Real saved Graph intent, not legacy mode/pass projection.
                VatEnabled = GetGraphToggleState("_VAT_Toggle");
                FlipbookEnabled = GetGraphToggleState("_FlipbookBlending");
                return;
            }

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

        private MixedBool GetGraphToggleState(string propertyName)
        {
            if (_rootItem.Mats == null || _rootItem.Mats.Count == 0 || !HasProperty(propertyName)) return MixedBool.Mixed;
            if (GetProperty(propertyName).hasMixedValue) return MixedBool.Mixed;
            bool first = true, value = false;
            foreach (Material material in _rootItem.Mats)
            {
                if (material == null || !NBShaderRootItem.HasFloatProperty(material, propertyName)) return MixedBool.Mixed;
                float raw = material.GetFloat(propertyName);
                if (float.IsNaN(raw) || float.IsInfinity(raw)) return MixedBool.Mixed;
                bool next = raw > 0.5f;
                if (!first && next != value) return MixedBool.Mixed;
                first = false; value = next;
            }
            return value ? MixedBool.True : MixedBool.False;
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
