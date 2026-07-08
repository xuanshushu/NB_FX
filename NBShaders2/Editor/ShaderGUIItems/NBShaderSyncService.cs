using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using NBShader;
using NBShaders2.Editor.FeatureLevel;

namespace NBShaderEditor
{
    public class NBShaderSyncService
    {
        private const string FeatureTierPropertyName = "_NBShaderFeatureTier";
        private const string StencilConfigAssetPath = "Packages/com.xuanxuan.nb.fx/XuanXuanRenderUtility/Shader/StencilConfig.asset";
        private readonly NBShaderRootItem _rootItem;
        private StencilValuesConfig _stencilValuesConfig;

        public int KeywordVersion { get; private set; }

        private static readonly FlagToggleBinding[] ToggleFlagBindings =
        {
            new FlagToggleBinding("_ColorAdjustmentOnlyAffectMainTex", NBShaderFlags.FLAG_BIT_PARTICLE_COLOR_ADJUSTMENT_ONLY_AFFECT_MAINTEX, 0),
            new FlagToggleBinding("_HueShift_Toggle", NBShaderFlags.FLAG_BIT_HUESHIFT_ON, 0),
            new FlagToggleBinding("_ChangeSaturability_Toggle", NBShaderFlags.FLAG_BIT_SATURABILITY_ON, 0),
            new FlagToggleBinding("_Contrast_Toggle", NBShaderFlags.FLAG_BIT_PARTICLE_1_MAINTEX_CONTRAST, 1),
            new FlagToggleBinding("_BaseMapColorRefine_Toggle", NBShaderFlags.FLAG_BIT_PARTICLE_1_MAINTEX_COLOR_REFINE, 1),
            new FlagToggleBinding("_ColorMultiAlpha", NBShaderFlags.FLAG_BIT_PARTICLE_COLOR_MULTI_ALPHA, 0),
            new FlagToggleBinding("_BaseBackColor_Toggle", NBShaderFlags.FLAG_BIT_PARTICLE_BACKCOLOR, 0),
            new FlagToggleBinding("_IgnoreVetexColor_Toggle", NBShaderFlags.FLAG_BIT_PARTICLE_1_IGNORE_VERTEX_COLOR, 1),
            new FlagToggleBinding("_BumpMapMaskMode", NBShaderFlags.FLAG_BIT_PARTICLE_NORMALMAP_MASK_MODE, 0),
            new FlagToggleBinding("_DistortionBothDirection_Toggle", NBShaderFlags.FLAG_BIT_PARTICLE_NOISEMAP_NORMALIZEED_ON, 0),
            new FlagToggleBinding("_Distortion_Choraticaberrat_WithNoise_Toggle", NBShaderFlags.FLAG_BIT_PARTICLE_NOISE_CHORATICABERRAT_WITH_NOISE, 0),
            new FlagToggleBinding("_DissolveLineMaskToggle", NBShaderFlags.FLAG_BIT_PARTICLE_1_DISSOLVE_LINE_MASK, 1),
            new FlagToggleBinding("_MaskRefineToggle", NBShaderFlags.FLAG_BIT_PARTICLE_1_MASK_REFINE, 1),
            new FlagToggleBinding("_MaskMapGradientToggle", NBShaderFlags.FLAG_BIT_PARTICLE_1_MASKMAP_GRADIENT, 1),
            new FlagToggleBinding("_MaskMap2GradientToggle", NBShaderFlags.FLAG_BIT_PARTICLE_1_MASKMAP_2_GRADIENT, 1),
            new FlagToggleBinding("_MaskMap3GradientToggle", NBShaderFlags.FLAG_BIT_PARTICLE_1_MASKMAP_3_GRADIENT, 1),
            new FlagToggleBinding("_ScreenDistortAlphaRefineToggle", NBShaderFlags.FLAG_BIT_PARTICLE_1_SCREEN_DISTORT_ALPHA_REFINE, 1),
            new FlagToggleBinding("_InvertFresnel_Toggle", NBShaderFlags.FLAG_BIT_PARTICLE_FRESNEL_INVERT_ON, 0),
            new FlagToggleBinding("_FresnelColorAffectByAlpha", NBShaderFlags.FLAG_BIT_PARTICLE_FRESNEL_COLOR_AFFETCT_BY_ALPHA, 0),
            new FlagToggleBinding("_VertexOffset_StartFromZero", NBShaderFlags.FLAG_BIT_PARTICLE_1_VERTEXOFFSET_START_FROM_ZERO, 1),
            new FlagToggleBinding("_VertexOffset_NormalDir_Toggle", NBShaderFlags.FLAG_BIT_PARTICLE_VERTEX_OFFSET_NORMAL_DIR, 0),
            new FlagToggleBinding("_UTwirlEnabled", NBShaderFlags.FLAG_BIT_PARTICLE_UTWIRL_ON, 0),
            new FlagToggleBinding("_PolarCoordinatesEnabled", NBShaderFlags.FLAG_BIT_PARTICLE_POLARCOORDINATES_ON, 0)
        };

        private static readonly FlagModeBinding[] ModeFlagBindings =
        {
            new FlagModeBinding("_ColorBlendAlphaMultiplyMode", NBShaderFlags.FLAG_BIT_PARTICLE_COLOR_BLEND_ALPHA_MULTIPLY_MODE, 0, 1),
            new FlagModeBinding("_RampColorBlendMode", NBShaderFlags.FLAG_BIT_PARTICLE_RAMP_COLOR_BLEND_ADD, 0, 1),
            new FlagModeBinding("_DissolveRampColorBlendMode", NBShaderFlags.FLAG_BIT_PARTICLE_1_DISSOLVE_RAMP_MULITPLY, 1, 1),
            new FlagModeBinding("_FresnelMode", NBShaderFlags.FLAG_BIT_PARTICLE_FRESNEL_FADE_ON, 0, 1)
        };

        private static readonly string[] HoudiniVatKeywords =
        {
            "_HOUDINI_VAT_SOFTBODY",
            "_HOUDINI_VAT_RIGIDBODY",
            "_HOUDINI_VAT_DYNAMIC_REMESH",
            "_HOUDINI_VAT_PARTICLE_SPRITE"
        };

        private static readonly string[] TyflowVatKeywords =
        {
            "_TYFLOW_VAT_ABSOLUTE",
            "_TYFLOW_VAT_RELATIVE",
            "_TYFLOW_VAT_SKIN_R",
            "_TYFLOW_VAT_SKIN_PR",
            "_TYFLOW_VAT_SKIN_PRSAVE",
            "_TYFLOW_VAT_SKIN_PRSXYZ"
        };

        public NBShaderSyncService(NBShaderRootItem rootItem)
        {
            _rootItem = rootItem;
        }

        public static void SyncMaterialState(Material material)
        {
            if (material == null)
            {
                return;
            }

            SyncMaterialState(new List<Material> { material });
        }

        public static void SyncMaterialState(IList<Material> materials)
        {
            if (materials == null || materials.Count == 0)
            {
                return;
            }

            var validMaterials = new List<Material>();
            for (int i = 0; i < materials.Count; i++)
            {
                if (materials[i] != null)
                {
                    validMaterials.Add(materials[i]);
                }
            }

            if (validMaterials.Count == 0)
            {
                return;
            }

            var rootItem = new NBShaderRootItem
            {
                Mats = validMaterials,
                Shader = validMaterials[0].shader
            };
            rootItem.InitFlags(validMaterials);
            new NBShaderSyncService(rootItem).SyncMaterialState();
        }

        public void NotifyKeywordsMayHaveChanged()
        {
            KeywordVersion++;
        }

        public void ApplyTransparentMode(TransparentMode mode)
        {
            if (!_rootItem.PropertyInfoDic.ContainsKey("_ZWrite") || !_rootItem.PropertyInfoDic.ContainsKey("_QueueBias"))
            {
                return;
            }

            MaterialProperty zWriteProperty = _rootItem.PropertyInfoDic["_ZWrite"].Property;
            MaterialProperty queueBiasProperty = _rootItem.PropertyInfoDic["_QueueBias"].Property;
            int queueBias = Mathf.RoundToInt(queueBiasProperty.floatValue);

            switch (mode)
            {
                case TransparentMode.Opaque:
                    zWriteProperty.floatValue = 1;
                    foreach (Material mat in _rootItem.Mats)
                    {
                        SetRenderQueueIfNeeded(mat, 2000 + queueBias);
                        SetKeyword(mat, "_ALPHATEST_ON", false);
                        SyncResolvedIntentStateIfNBShader(mat);
                    }
                    break;

                case TransparentMode.Transparent:
                    zWriteProperty.floatValue = 0;
                    foreach (Material mat in _rootItem.Mats)
                    {
                        SetRenderQueueIfNeeded(mat, 3000 + queueBias);
                        SetKeyword(mat, "_ALPHATEST_ON", false);
                        SyncResolvedIntentStateIfNBShader(mat);
                    }
                    break;

                case TransparentMode.CutOff:
                    zWriteProperty.floatValue = 1;
                    foreach (Material mat in _rootItem.Mats)
                    {
                        SetRenderQueueIfNeeded(mat, 2450 + queueBias);
                        SetKeyword(mat, "_ALPHATEST_ON", true);
                        SyncResolvedIntentStateIfNBShader(mat);
                    }
                    break;
            }
        }

        public void SyncMaterialState()
        {
            for (int i = 0; i < _rootItem.Mats.Count; i++)
            {
                Material mat = _rootItem.Mats[i];
                if (mat == null)
                {
                    continue;
                }

                NBShaderFlags flags = GetFlags(i);
                SyncMeshSourceMode(mat, flags);
                SyncCustomData(mat, flags);
                SyncUVDerivedFlags(flags);
                SyncTransparentMode(mat);
                SyncTransparentShadowFlags(mat, flags);
                SyncBlendMode(mat);
                SyncTimeMode(mat, flags);
                SyncTogglePropertyFlags(mat, flags);
                SyncParallaxLayerCount(mat);
                SyncResolvedIntentState(mat);
            }
        }

        public void ApplyToggleFlag(int flagBits, bool enabled, int flagIndex = 0)
        {
            foreach (ShaderFlagsBase flagBase in _rootItem.ShaderFlags)
            {
                if (enabled)
                {
                    SetFlag(flagBase, flagBits, true, flagIndex);
                }
                else
                {
                    SetFlag(flagBase, flagBits, false, flagIndex);
                }
            }
        }

        public void ApplyShaderPass(string passName, bool enabled)
        {
            foreach (Material mat in _rootItem.Mats)
            {
                if (IsResolvedIntentPass(passName) && NBShaderMaterialIntentResolver.IsNBShaderMaterial(mat))
                    SyncResolvedIntentState(mat);
                else
                    SetShaderPassEnabledIfNeeded(mat, passName, enabled);
            }
        }

        public void ApplyScreenDistortMode(int mode)
        {
            foreach (Material mat in _rootItem.Mats)
            {
                if (mat == null)
                    continue;

                if (mode == 0 && mat.HasProperty("_DisableMainPassToggle"))
                {
                    SetFloatIfExists(mat, "_DisableMainPassToggle", 0f);
                }

                if (NBShaderMaterialIntentResolver.IsNBShaderMaterial(mat))
                    SyncResolvedIntentState(mat);
            }
        }

        public void ApplyDepthDecalEnabled(bool enabled)
        {
            ApplyToggleKeyword("_DEPTH_DECAL", enabled);
            foreach (Material mat in _rootItem.Mats)
            {
                if (mat == null)
                {
                    continue;
                }

                ApplyStencilPresetToMaterial(mat, enabled ? "ParticleBaseDecal" : "ParticleBaseDefault");
                SetFloatIfExists(mat, "_CustomStencilTest", enabled ? 1f : 0f);
                SetFloatIfExists(mat, "_Cull", enabled ? (float)RenderFace.Back : (float)RenderFace.Front);
                SetFloatIfExists(mat, "_ZTest", enabled
                    ? (float)UnityEngine.Rendering.CompareFunction.GreaterEqual
                    : (float)UnityEngine.Rendering.CompareFunction.LessEqual);
            }
        }

        public void ApplyPortalState()
        {
            foreach (Material mat in _rootItem.Mats)
            {
                if (mat == null)
                {
                    continue;
                }

                bool portal = mat.HasProperty("_Portal_Toggle") && mat.GetFloat("_Portal_Toggle") > 0.5f;
                bool mask = mat.HasProperty("_Portal_MaskToggle") && mat.GetFloat("_Portal_MaskToggle") > 0.5f;
                if (!portal)
                {
                    ApplyStencilPresetToMaterial(mat, "ParticleBaseDefault");
                    SetFloatIfExists(mat, "_CustomStencilTest", 0f);
                    SetFloatIfExists(mat, "_TransparentMode", (float)TransparentMode.Transparent);
                    SetFloatIfExists(mat, "_ZTest", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
                    SetFloatIfExists(mat, "_ForceZWriteToggle", 0f);
                }
                else if (mask)
                {
                    ApplyStencilPresetToMaterial(mat, "ParticalBasePortalMask");
                    SetFloatIfExists(mat, "_CustomStencilTest", 1f);
                    if (mat.HasProperty("_TransparentMode") &&
                        Mathf.RoundToInt(mat.GetFloat("_TransparentMode")) == (int)TransparentMode.Transparent)
                    {
                        SetFloatIfExists(mat, "_TransparentMode", (float)TransparentMode.CutOff);
                    }

                    SetFloatIfExists(mat, "_ZTest", (float)UnityEngine.Rendering.CompareFunction.LessEqual);
                    SetFloatIfExists(mat, "_ForceZWriteToggle", 2f);
                }
                else
                {
                    ApplyStencilPresetToMaterial(mat, "ParticalBasePortal");
                    SetFloatIfExists(mat, "_CustomStencilTest", 1f);
                }
            }

            SyncMaterialState();
        }

        public void ApplyVatEnabled(bool enabled)
        {
            foreach (Material mat in _rootItem.Mats)
            {
                SetFloatIfExists(mat, "_VAT_Toggle", enabled ? 1f : 0f);

                if (enabled)
                    DisableFlipbook(mat);

                if (NBShaderMaterialIntentResolver.IsNBShaderMaterial(mat))
                    SyncResolvedIntentState(mat);
                else
                    SyncVatKeywords(mat);
            }
        }

        public void ApplyFlipbookEnabled(bool enabled)
        {
            foreach (Material mat in _rootItem.Mats)
            {
                SetFloatIfExists(mat, "_FlipbookBlending", enabled ? 1f : 0f);

                if (enabled)
                {
                    DisableVat(mat);
                }

                if (NBShaderMaterialIntentResolver.IsNBShaderMaterial(mat))
                    SyncResolvedIntentState(mat);
                else
                    SetKeyword(mat, "_FLIPBOOKBLENDING_ON", enabled);
            }
        }

        public void ApplyBlendMode(BlendMode mode)
        {
            foreach (Material mat in _rootItem.Mats)
            {
                bool nbShaderMaterial = NBShaderMaterialIntentResolver.IsNBShaderMaterial(mat);
                switch (mode)
                {
                    case BlendMode.Alpha:
                        SetIntIfExists(mat, "_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        SetIntIfExists(mat, "_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        if (!nbShaderMaterial)
                        {
                            SetKeyword(mat, "_ALPHAPREMULTIPLY_ON", false);
                            SetKeyword(mat, "_ALPHAMODULATE_ON", false);
                        }
                        break;
                    case BlendMode.Premultiply:
                        SetIntIfExists(mat, "_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        SetIntIfExists(mat, "_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        if (!nbShaderMaterial)
                        {
                            SetKeyword(mat, "_ALPHAPREMULTIPLY_ON", true);
                            SetKeyword(mat, "_ALPHAMODULATE_ON", false);
                        }
                        break;
                    case BlendMode.Additive:
                        SetIntIfExists(mat, "_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        SetIntIfExists(mat, "_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        if (!nbShaderMaterial)
                        {
                            SetKeyword(mat, "_ALPHAPREMULTIPLY_ON", true);
                            SetKeyword(mat, "_ALPHAMODULATE_ON", false);
                        }
                        break;
                    case BlendMode.Multiply:
                        SetIntIfExists(mat, "_SrcBlend", (int)UnityEngine.Rendering.BlendMode.DstColor);
                        SetIntIfExists(mat, "_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        if (!nbShaderMaterial)
                        {
                            SetKeyword(mat, "_ALPHAPREMULTIPLY_ON", false);
                            SetKeyword(mat, "_ALPHAMODULATE_ON", true);
                        }
                        break;
                    case BlendMode.Opaque:
                        SetIntIfExists(mat, "_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        SetIntIfExists(mat, "_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                        if (!nbShaderMaterial)
                        {
                            SetKeyword(mat, "_ALPHAPREMULTIPLY_ON", false);
                            SetKeyword(mat, "_ALPHAMODULATE_ON", false);
                        }
                        break;
                }

                if (nbShaderMaterial)
                    SyncResolvedIntentState(mat);
            }
        }

        public void ApplyLightMode(FxLightMode mode)
        {
            foreach (Material mat in _rootItem.Mats)
            {
                if (NBShaderMaterialIntentResolver.IsNBShaderMaterial(mat))
                    SyncResolvedIntentState(mat);
                else
                    SetLightModeKeyword(mat, mode);
            }
        }

        public void ApplyToggleKeyword(string keyword, bool enabled)
        {
            foreach (Material mat in _rootItem.Mats)
            {
                if (NBShaderFeatureCatalog.IsManagedKeyword(keyword) &&
                    NBShaderMaterialIntentResolver.IsNBShaderMaterial(mat))
                    SyncResolvedIntentState(mat);
                else
                    SetKeyword(mat, keyword, enabled);
            }
        }

        public void ApplyStencilPreset(string key)
        {
            foreach (Material mat in _rootItem.Mats)
            {
                if (mat == null)
                {
                    continue;
                }

                ApplyStencilPresetToMaterial(mat, key);
            }
        }

        public bool AnyProgramNoiseEnabled()
        {
            foreach (Material mat in _rootItem.Mats)
            {
                if (mat.HasProperty("_ProgramNoise_Toggle") && mat.GetFloat("_ProgramNoise_Toggle") > 0.5f)
                {
                    return true;
                }
            }

            return false;
        }

        private NBShaderFlags GetFlags(int index)
        {
            return index >= 0 &&
                   index < _rootItem.ShaderFlags.Count &&
                   _rootItem.ShaderFlags[index] is NBShaderFlags flags
                ? flags
                : null;
        }

        private StencilValuesConfig GetStencilValuesConfig()
        {
            if (_stencilValuesConfig == null)
            {
                _stencilValuesConfig = AssetDatabase.LoadAssetAtPath<StencilValuesConfig>(StencilConfigAssetPath);
            }

            return _stencilValuesConfig;
        }

        private void ApplyStencilPresetToMaterial(Material mat, string key)
        {
            StencilValuesConfig config = GetStencilValuesConfig();
            if (config != null)
            {
                StencilTestHelper.SetMaterialStencil(mat, key, config, out _);
            }
            else
            {
                ApplyFallbackStencilPreset(mat, key);
            }
        }

        private static void ApplyFallbackStencilPreset(Material mat, string key)
        {
            int stencil = 0;
            int comp = (int)UnityEngine.Rendering.CompareFunction.Always;
            int pass = (int)UnityEngine.Rendering.StencilOp.Keep;
            int keyIndex = 0;

            switch (key)
            {
                case "ParticalBasePortal":
                    stencil = 200;
                    comp = (int)UnityEngine.Rendering.CompareFunction.Equal;
                    keyIndex = 2;
                    break;
                case "ParticalBasePortalMask":
                    stencil = 200;
                    pass = (int)UnityEngine.Rendering.StencilOp.Replace;
                    keyIndex = 3;
                    break;
                case "ParticleBaseDecal":
                    stencil = 2;
                    comp = (int)UnityEngine.Rendering.CompareFunction.GreaterEqual;
                    keyIndex = 4;
                    break;
                case "ParticleWithoutPlayer":
                    stencil = 5;
                    comp = (int)UnityEngine.Rendering.CompareFunction.Greater;
                    keyIndex = 5;
                    break;
            }

            SetFloatIfExists(mat, "_Stencil", stencil);
            SetFloatIfExists(mat, "_StencilComp", comp);
            SetFloatIfExists(mat, "_StencilOp", pass);
            SetFloatIfExists(mat, "_StencilFail", (int)UnityEngine.Rendering.StencilOp.Keep);
            SetFloatIfExists(mat, "_StencilZFail", (int)UnityEngine.Rendering.StencilOp.Keep);
            SetFloatIfExists(mat, "_StencilReadMask", 255f);
            SetFloatIfExists(mat, "_StencilWriteMask", 255f);
            SetFloatIfExists(mat, "_StencilKeyIndex", keyIndex);
        }

        private static void SetFloatIfExists(Material mat, string propertyName, float value)
        {
            if (mat != null &&
                mat.HasProperty(propertyName) &&
                !Mathf.Approximately(mat.GetFloat(propertyName), value))
            {
                mat.SetFloat(propertyName, value);
            }
        }

        private static void SetIntIfExists(Material mat, string propertyName, int value)
        {
            if (mat != null &&
                mat.HasProperty(propertyName) &&
                Mathf.RoundToInt(mat.GetFloat(propertyName)) != value)
            {
                mat.SetInt(propertyName, value);
            }
        }

        private static void SetVectorIfExists(Material mat, string propertyName, Vector4 value)
        {
            if (mat != null &&
                mat.HasProperty(propertyName) &&
                mat.GetVector(propertyName) != value)
            {
                mat.SetVector(propertyName, value);
            }
        }

        private static void SetRenderQueueIfNeeded(Material mat, int renderQueue)
        {
            if (mat != null && mat.renderQueue != renderQueue)
            {
                mat.renderQueue = renderQueue;
            }
        }

        private static void SetShaderPassEnabledIfNeeded(Material mat, string passName, bool enabled)
        {
            if (mat != null && !string.IsNullOrEmpty(passName) && mat.GetShaderPassEnabled(passName) != enabled)
            {
                mat.SetShaderPassEnabled(passName, enabled);
            }
        }

        private void SyncMeshSourceMode(Material mat, NBShaderFlags flags)
        {
            if (flags == null || !mat.HasProperty("_MeshSourceMode"))
            {
                return;
            }

            MeshSourceMode mode = (MeshSourceMode)Mathf.RoundToInt(mat.GetFloat("_MeshSourceMode"));
            bool isParticle = mode == MeshSourceMode.Particle || mode == MeshSourceMode.UIParticle;
            bool isUIEffect = mode == MeshSourceMode.UIEffectRawImage ||
                              mode == MeshSourceMode.UIEffectSprite ||
                              mode == MeshSourceMode.UIEffectBaseMap ||
                              mode == MeshSourceMode.UIParticle;
            bool useBaseMapTexture = mode == MeshSourceMode.UIEffectBaseMap ||
                                     mode == MeshSourceMode.UIParticle;

            SetFlag(flags, NBShaderFlags.FLAG_BIT_PARTICLE_1_IS_PARTICLE_SYSTEM, isParticle, 1);
            SetFlag(flags, NBShaderFlags.FLAG_BIT_PARTICLE_1_UV_FROM_MESH, mode == MeshSourceMode.Mesh, 1);
            SetFlag(flags, NBShaderFlags.FLAG_BIT_PARTICLE_UIEFFECT_ON, isUIEffect, 0);
            SetFlag(flags, NBShaderFlags.FLAG_BIT_PARTICLE_1_UIEFFECT_SPRITE_MODE, mode == MeshSourceMode.UIEffectSprite, 1);
            SetFlag(flags, NBShaderFlags.FLAG_BIT_PARTICLE_1_UIEFFECT_BASEMAP_MODE, useBaseMapTexture, 1);
            if (mode == MeshSourceMode.Particle)
            {
                SetFlag(flags, NBShaderFlags.FLAG_BIT_PARTICLE_1_ANIMATION_SHEET_HELPER, false, 1);
            }

            if (mat.HasProperty("_CustomData"))
            {
                SetFloatIfExists(mat, "_CustomData", isParticle ? 1f : 0f);
            }

            if (isParticle)
            {
                SetKeyword(mat, "_CUSTOMDATA", true);
            }
            else
            {
                SetKeyword(mat, "_CUSTOMDATA", false);
                SetFlag(flags, NBShaderFlags.FLAG_BIT_PARTICLE_CUSTOMDATA1_ON, false, 0);
                SetFlag(flags, NBShaderFlags.FLAG_BIT_PARTICLE_CUSTOMDATA2_ON, false, 0);
            }
        }

        private void SyncTimeMode(Material mat, NBShaderFlags flags)
        {
            if (flags == null || !mat.HasProperty("_TimeMode"))
            {
                return;
            }

            TimeMode mode = (TimeMode)Mathf.RoundToInt(mat.GetFloat("_TimeMode"));
            SetFlag(flags, NBShaderFlags.FLAG_BIT_PARTICLE_UNSCALETIME_ON, mode == TimeMode.UnScaleTime, 0);
            SetFlag(flags, NBShaderFlags.FLAG_BIT_PARTICLE_SCRIPTABLETIME_ON, mode == TimeMode.ScriptableTime, 0);
        }

        private void SyncTogglePropertyFlags(Material mat, NBShaderFlags flags)
        {
            if (flags == null || mat == null)
            {
                return;
            }

            for (int i = 0; i < ToggleFlagBindings.Length; i++)
            {
                var binding = ToggleFlagBindings[i];
                if (!mat.HasProperty(binding.propertyName))
                {
                    continue;
                }

                SetFlag(flags, binding.flagBits, mat.GetFloat(binding.propertyName) > 0.5f, binding.flagIndex);
            }

            for (int i = 0; i < ModeFlagBindings.Length; i++)
            {
                var binding = ModeFlagBindings[i];
                if (!mat.HasProperty(binding.propertyName))
                {
                    continue;
                }

                SetFlag(flags, binding.flagBits, Mathf.RoundToInt(mat.GetFloat(binding.propertyName)) == binding.enabledMode, binding.flagIndex);
            }
        }

        private bool IsKeywordAllowed(string keyword)
        {
            return _rootItem.Context == null || _rootItem.Context.IsKeywordAllowed(keyword);
        }

        private void SetKeyword(Material mat, string keyword, bool enabled)
        {
            if (mat == null || string.IsNullOrEmpty(keyword))
            {
                return;
            }

            bool shouldEnable = enabled && IsKeywordAllowed(keyword);
            if (mat.IsKeywordEnabled(keyword) == shouldEnable)
            {
                return;
            }

            if (shouldEnable)
            {
                mat.EnableKeyword(keyword);
            }
            else
            {
                mat.DisableKeyword(keyword);
            }

            KeywordVersion++;
        }

        private void SyncResolvedIntentState(Material mat)
        {
            var tier = ResolveMaterialTier(mat);
            var allowedKeywords = NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForReadOnlyUse(tier);
            var allowedPassFeatures = NBShaderFeatureLevelProjectSettings.instance.GetAllowedPassFeatureSetForReadOnlyUse(tier);
            var result = NBShaderMaterialIntentResolver.Resolve(mat, tier, allowedKeywords, allowedPassFeatures);
            var effectiveKeywords = new HashSet<string>(result.effectiveKeywords);

            for (int i = 0; i < NBShaderFeatureCatalog.RawKeywords.Length; i++)
            {
                string keyword = NBShaderFeatureCatalog.RawKeywords[i];
                SetKeyword(mat, keyword, effectiveKeywords.Contains(keyword));
            }

            for (int i = 0; i < result.passes.Length; i++)
            {
                NBShaderPassIntent pass = result.passes[i];
                if (!string.IsNullOrEmpty(pass.passName))
                {
                    SetShaderPassEnabledIfNeeded(mat, pass.passName, pass.included);
                }
            }

            SetKeyword(mat, "EVALUATE_SH_VERTEX", effectiveKeywords.Contains("_FX_LIGHT_MODE_SIX_WAY"));
        }

        private void SyncResolvedIntentStateIfNBShader(Material mat)
        {
            if (NBShaderMaterialIntentResolver.IsNBShaderMaterial(mat))
                SyncResolvedIntentState(mat);
        }

        private NBShaderFeatureTier ResolveMaterialTier(Material mat)
        {
            if (mat != null && mat.HasProperty(FeatureTierPropertyName))
            {
                int value = Mathf.RoundToInt(mat.GetFloat(FeatureTierPropertyName));
                if (value >= (int)NBShaderFeatureTier.Low && value <= (int)NBShaderFeatureTier.Ultra)
                {
                    return (NBShaderFeatureTier)value;
                }
            }

            if (_rootItem.Context != null && !_rootItem.Context.CurrentTierMixed)
            {
                return _rootItem.Context.CurrentTier;
            }

            return NBShaderFeatureTier.Ultra;
        }

        private static bool IsResolvedIntentPass(string passName)
        {
            if (string.IsNullOrEmpty(passName))
                return false;

            if (string.Equals(passName, NBShaderPassFeatureCatalog.MainForwardPassName, StringComparison.Ordinal))
                return true;

            string passFeatureId;
            return NBShaderFeatureLevelCatalog.TryGetManagedPassFeatureByPassName(passName, out passFeatureId);
        }

        private void SyncCustomData(Material mat, NBShaderFlags flags)
        {
            if (flags == null || !mat.HasProperty("_MeshSourceMode"))
            {
                return;
            }

            MeshSourceMode mode = (MeshSourceMode)Mathf.RoundToInt(mat.GetFloat("_MeshSourceMode"));
            bool isParticle = mode == MeshSourceMode.Particle || mode == MeshSourceMode.UIParticle;
            if (!isParticle)
            {
                return;
            }

            SetFlag(flags, NBShaderFlags.FLAG_BIT_PARTICLE_CUSTOMDATA1_ON, flags.IsCustomData1On(), 0);
            SetFlag(flags, NBShaderFlags.FLAG_BIT_PARTICLE_CUSTOMDATA2_ON, flags.IsCustomData2On(), 0);
        }

        private static void SyncUVDerivedFlags(NBShaderFlags flags)
        {
            if (flags == null)
            {
                return;
            }

            if (!flags.CheckIsUVModeOn(NBShaderFlags.UVMode.SpecialUVChannel))
            {
                SetFlag(flags, NBShaderFlags.FLAG_BIT_PARTICLE_1_USE_TEXCOORD1, false, 1);
                SetFlag(flags, NBShaderFlags.FLAG_BIT_PARTICLE_1_USE_TEXCOORD2, false, 1);
            }

            SetFlag(flags, NBShaderFlags.FLAG_BIT_PARTICLE_1_CYLINDER_CORDINATE, flags.CheckIsUVModeOn(NBShaderFlags.UVMode.Cylinder), 1);
        }

        private void SyncTransparentMode(Material mat)
        {
            if (!mat.HasProperty("_TransparentMode"))
            {
                return;
            }

            TransparentMode mode = (TransparentMode)Mathf.RoundToInt(mat.GetFloat("_TransparentMode"));
            int queueBias = mat.HasProperty("_QueueBias") ? Mathf.RoundToInt(mat.GetFloat("_QueueBias")) : 0;
            bool uiEffect = mat.HasProperty("_MeshSourceMode") &&
                            ((MeshSourceMode)Mathf.RoundToInt(mat.GetFloat("_MeshSourceMode")) == MeshSourceMode.UIEffectRawImage ||
                             (MeshSourceMode)Mathf.RoundToInt(mat.GetFloat("_MeshSourceMode")) == MeshSourceMode.UIEffectSprite ||
                             (MeshSourceMode)Mathf.RoundToInt(mat.GetFloat("_MeshSourceMode")) == MeshSourceMode.UIEffectBaseMap ||
                             (MeshSourceMode)Mathf.RoundToInt(mat.GetFloat("_MeshSourceMode")) == MeshSourceMode.UIParticle);

            if (mode != TransparentMode.Transparent)
            {
                SetFloatIfExists(mat, "_TransparentShadowDitherToggle", 0f);
            }

            switch (mode)
            {
                case TransparentMode.Opaque:
                    SetIntIfExists(mat, "_ZWrite", 1);
                    SetRenderQueueIfNeeded(mat, 2100 + queueBias);
                    SetFloatIfExists(mat, "_Blend", (float)BlendMode.Opaque);
                    break;
                case TransparentMode.Transparent:
                    SetIntIfExists(mat, "_ZWrite", 0);
                    SetRenderQueueIfNeeded(mat, (uiEffect ? 3000 : 3100) + queueBias);
                    if (mat.HasProperty("_Blend") && (BlendMode)Mathf.RoundToInt(mat.GetFloat("_Blend")) == BlendMode.Opaque)
                    {
                        SetFloatIfExists(mat, "_Blend", (float)BlendMode.Alpha);
                    }

                    break;
                case TransparentMode.CutOff:
                    SetIntIfExists(mat, "_ZWrite", 1);
                    SetRenderQueueIfNeeded(mat, 2450 + queueBias);
                    SetFloatIfExists(mat, "_Blend", (float)BlendMode.Opaque);
                    break;
            }

            if (mat.HasProperty("_ForceZWriteToggle"))
            {
                float forceZWrite = mat.GetFloat("_ForceZWriteToggle");
                if (forceZWrite > 0.5f && forceZWrite < 1.5f)
                {
                    SetIntIfExists(mat, "_ZWrite", 1);
                }
                else if (forceZWrite > 1.5f)
                {
                    SetIntIfExists(mat, "_ZWrite", 0);
                }
            }
        }

        private static void SyncTransparentShadowFlags(Material mat, NBShaderFlags flags)
        {
            TransparentMode mode = mat != null && mat.HasProperty("_TransparentMode")
                ? (TransparentMode)Mathf.RoundToInt(mat.GetFloat("_TransparentMode"))
                : TransparentMode.UnKnowOrMixed;
            bool isTransparent = mode == TransparentMode.Transparent;
            bool useTransparentShadowDither = isTransparent &&
                                              mat.HasProperty("_TransparentShadowDitherToggle") &&
                                              mat.GetFloat("_TransparentShadowDitherToggle") > 0.5f;

            SetFlag(flags, NBShaderFlags.FLAG_BIT_PARTICLE_1_TRANSPARENT_MODE, isTransparent, 1);
            SetFlag(flags, NBShaderFlags.FLAG_BIT_PARTICLE_1_TRANSPARENT_SHADOW_DITHER, useTransparentShadowDither, 1);
        }

        private static bool IsUIEffectMode(Material mat)
        {
            if (mat == null || !mat.HasProperty("_MeshSourceMode"))
            {
                return false;
            }

            MeshSourceMode meshSourceMode = (MeshSourceMode)Mathf.RoundToInt(mat.GetFloat("_MeshSourceMode"));
            return meshSourceMode == MeshSourceMode.UIEffectRawImage ||
                   meshSourceMode == MeshSourceMode.UIEffectSprite ||
                   meshSourceMode == MeshSourceMode.UIEffectBaseMap ||
                   meshSourceMode == MeshSourceMode.UIParticle;
        }

        private static void SyncParallaxLayerCount(Material mat)
        {
            if (mat == null ||
                !mat.HasProperty("_ParallaxMapping_Toggle") ||
                !mat.HasProperty("_ParallaxMapping_Vec") ||
                mat.GetFloat("_ParallaxMapping_Toggle") <= 0.5f)
            {
                return;
            }

            Vector4 value = mat.GetVector("_ParallaxMapping_Vec");
            if (value.y < value.x + 1f)
            {
                value.y = value.x + 1f;
                SetVectorIfExists(mat, "_ParallaxMapping_Vec", value);
            }
        }

        private void SyncBlendMode(Material mat)
        {
            if (!mat.HasProperty("_Blend"))
            {
                return;
            }

            switch ((BlendMode)Mathf.RoundToInt(mat.GetFloat("_Blend")))
            {
                case BlendMode.Alpha:
                    SetIntIfExists(mat, "_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    SetIntIfExists(mat, "_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    break;
                case BlendMode.Premultiply:
                    SetIntIfExists(mat, "_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    SetIntIfExists(mat, "_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    break;
                case BlendMode.Additive:
                    SetIntIfExists(mat, "_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    SetIntIfExists(mat, "_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    break;
                case BlendMode.Multiply:
                    SetIntIfExists(mat, "_SrcBlend", (int)UnityEngine.Rendering.BlendMode.DstColor);
                    SetIntIfExists(mat, "_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    break;
                case BlendMode.Opaque:
                    SetIntIfExists(mat, "_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    SetIntIfExists(mat, "_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    break;
            }
        }

        private void SetLightModeKeyword(Material mat, FxLightMode mode)
        {
            SetKeyword(mat, "_FX_LIGHT_MODE_UNLIT", false);
            SetKeyword(mat, "_FX_LIGHT_MODE_BLINN_PHONG", false);
            SetKeyword(mat, "_FX_LIGHT_MODE_HALF_LAMBERT", false);
            SetKeyword(mat, "_FX_LIGHT_MODE_PBR", false);
            SetKeyword(mat, "_FX_LIGHT_MODE_SIX_WAY", false);
            SetKeyword(mat, "EVALUATE_SH_VERTEX", false);

            switch (mode)
            {
                case FxLightMode.UnLit:
                    SetKeyword(mat, "_FX_LIGHT_MODE_UNLIT", true);
                    break;
                case FxLightMode.BlinnPhong:
                    SetKeyword(mat, "_FX_LIGHT_MODE_BLINN_PHONG", true);
                    break;
                case FxLightMode.HalfLambert:
                    SetKeyword(mat, "_FX_LIGHT_MODE_HALF_LAMBERT", true);
                    break;
                case FxLightMode.PBR:
                    SetKeyword(mat, "_FX_LIGHT_MODE_PBR", true);
                    break;
                case FxLightMode.SixWay:
                    SetKeyword(mat, "_FX_LIGHT_MODE_SIX_WAY", true);
                    SetKeyword(mat, "EVALUATE_SH_VERTEX", true);
                    break;
            }
        }

        private void SyncVatKeywords(Material mat)
        {
            if (!mat.HasProperty("_VAT_Toggle") || mat.GetFloat("_VAT_Toggle") <= 0.5f)
            {
                ClearVatKeywords(mat);
                return;
            }

            DisableFlipbook(mat);
            SetKeyword(mat, "_VAT", true);
            int vatMode = mat.HasProperty("_VATMode") ? Mathf.RoundToInt(mat.GetFloat("_VATMode")) : 0;
            if (vatMode == (int)VATMode.Tyflow)
            {
                SetKeyword(mat, "_VAT_HOUDINI", false);
                SetKeyword(mat, "_VAT_TYFLOW", true);
                SetHoudiniVATKeyword(mat, -1);
                SetTyflowVATKeyword(mat, mat.HasProperty("_TyFlowVATSubMode") ? Mathf.RoundToInt(mat.GetFloat("_TyFlowVATSubMode")) : 0);
            }
            else
            {
                SetKeyword(mat, "_VAT_HOUDINI", true);
                SetKeyword(mat, "_VAT_TYFLOW", false);
                SetHoudiniVATKeyword(mat, mat.HasProperty("_HoudiniVATSubMode") ? Mathf.RoundToInt(mat.GetFloat("_HoudiniVATSubMode")) : 0);
                SetTyflowVATKeyword(mat, -1);
            }
        }

        private void DisableVat(Material mat)
        {
            SetFloatIfExists(mat, "_VAT_Toggle", 0f);

            ClearVatKeywords(mat);
        }

        private void DisableFlipbook(Material mat)
        {
            SetFloatIfExists(mat, "_FlipbookBlending", 0f);

            SetKeyword(mat, "_FLIPBOOKBLENDING_ON", false);
        }

        private void ClearVatKeywords(Material mat)
        {
            SetKeyword(mat, "_VAT", false);
            SetKeyword(mat, "_VAT_HOUDINI", false);
            SetKeyword(mat, "_VAT_TYFLOW", false);
            SetHoudiniVATKeyword(mat, -1);
            SetTyflowVATKeyword(mat, -1);
        }

        private void SetHoudiniVATKeyword(Material mat, int enabledIndex)
        {
            SetExclusiveKeyword(mat, HoudiniVatKeywords, enabledIndex);
        }

        private void SetTyflowVATKeyword(Material mat, int enabledIndex)
        {
            SetExclusiveKeyword(mat, TyflowVatKeywords, enabledIndex);
        }

        private void SetExclusiveKeyword(Material mat, string[] keywords, int enabledIndex)
        {
            for (int i = 0; i < keywords.Length; i++)
            {
                SetKeyword(mat, keywords[i], false);
            }

            if (enabledIndex >= 0 && enabledIndex < keywords.Length)
            {
                SetKeyword(mat, keywords[enabledIndex], true);
            }
        }

        private static void SetFlag(ShaderFlagsBase flags, int flagBits, bool enabled, int index)
        {
            if (flags == null ||
                flags.material == null ||
                flags.CheckFlagBits(flagBits, index: index) == enabled)
            {
                return;
            }

            if (enabled)
            {
                flags.SetFlagBits(flagBits, index: index);
            }
            else
            {
                flags.ClearFlagBits(flagBits, index: index);
            }
        }

        private struct FlagToggleBinding
        {
            public readonly string propertyName;
            public readonly int flagBits;
            public readonly int flagIndex;

            public FlagToggleBinding(string propertyName, int flagBits, int flagIndex)
            {
                this.propertyName = propertyName;
                this.flagBits = flagBits;
                this.flagIndex = flagIndex;
            }
        }

        private struct FlagModeBinding
        {
            public readonly string propertyName;
            public readonly int flagBits;
            public readonly int flagIndex;
            public readonly int enabledMode;

            public FlagModeBinding(string propertyName, int flagBits, int flagIndex, int enabledMode)
            {
                this.propertyName = propertyName;
                this.flagBits = flagBits;
                this.flagIndex = flagIndex;
                this.enabledMode = enabledMode;
            }
        }
    }

    public enum VATMode
    {
        Houdini = 0,
        Tyflow = 1,
        UnKnownOrMixed = -1
    }

    public enum TimeMode
    {
        Default = 0,
        UnScaleTime = 1,
        ScriptableTime = 2
    }
}
