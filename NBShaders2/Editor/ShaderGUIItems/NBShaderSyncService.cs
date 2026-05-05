using UnityEditor;
using UnityEngine;
using NBShader;

namespace NBShaderEditor
{
    public class NBShaderSyncService
    {
        private const string StencilConfigAssetPath = "Packages/com.xuanxuan.nb.fx/XuanXuanRenderUtility/Shader/StencilConfig.asset";
        private readonly NBShaderRootItem _rootItem;
        private StencilValuesConfig _stencilValuesConfig;

        private static readonly KeywordToggleBinding[] ToggleKeywordBindings =
        {
            new KeywordToggleBinding("_SoftParticlesEnabled", "_SOFTPARTICLES_ON"),
            new KeywordToggleBinding("_StencilWithoutPlayerToggle", "_STENCIL_WITHOUT_PLAYER"),
            new KeywordToggleBinding("_Mask_Toggle", "_MASKMAP_ON"),
            new KeywordToggleBinding("_noisemapEnabled", "_NOISEMAP"),
            new KeywordToggleBinding("_EmissionEnabled", "_EMISSION"),
            new KeywordToggleBinding("_ColorBlendMap_Toggle", "_COLORMAPBLEND"),
            new KeywordToggleBinding("_RampColorToggle", "_COLOR_RAMP"),
            new KeywordToggleBinding("_Dissolve_Toggle", "_DISSOLVE"),
            new KeywordToggleBinding("_ProgramNoise_Toggle", "_PROGRAM_NOISE"),
            new KeywordToggleBinding("_SharedUVToggle", "_SHARED_UV"),
            new KeywordToggleBinding("_fresnelEnabled", "_FRESNEL"),
            new KeywordToggleBinding("_ParallaxMapping_Toggle", "_PARALLAX_MAPPING"),
            new KeywordToggleBinding("_VertexOffset_Toggle", "_VERTEX_OFFSET"),
            new KeywordToggleBinding("_FlipbookBlending", "_FLIPBOOKBLENDING_ON"),
            new KeywordToggleBinding("_BumpMapToggle", "_NORMALMAP"),
            new KeywordToggleBinding("_MatCapToggle", "_MATCAP"),
            new KeywordToggleBinding("_BlinnPhongSpecularToggle", "_SPECULAR_COLOR"),
            new KeywordToggleBinding("_SixWayColorAbsorptionToggle", "VFX_SIX_WAY_ABSORPTION"),
            new KeywordToggleBinding("_DepthDecal_Toggle", "_DEPTH_DECAL"),
            new KeywordToggleBinding("_DepthOutline_Toggle", "_DEPTH_OUTLINE"),
            new KeywordToggleBinding("_Mask2_Toggle", "_MASKMAP2_ON"),
            new KeywordToggleBinding("_Mask3_Toggle", "_MASKMAP3_ON"),
            new KeywordToggleBinding("_noiseMaskMap_Toggle", "_NOISE_MASKMAP"),
            new KeywordToggleBinding("_Distortion_Choraticaberrat_Toggle", "_CHROMATIC_ABERRATION"),
            new KeywordToggleBinding("_DissolveMask_Toggle", "_DISSOLVE_MASK"),
            new KeywordToggleBinding("_Dissolve_useRampMap_Toggle", "_DISSOLVE_RAMP"),
            new KeywordToggleBinding("_ProgramNoise_Simple_Toggle", "_PROGRAM_NOISE_SIMPLE"),
            new KeywordToggleBinding("_ProgramNoise_Voronoi_Toggle", "_PROGRAM_NOISE_VORONOI"),
            new KeywordToggleBinding("_VertexOffset_Mask_Toggle", "_VERTEX_OFFSET_MASKMAP")
        };

        public NBShaderSyncService(NBShaderRootItem rootItem)
        {
            _rootItem = rootItem;
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
                        mat.renderQueue = 2000 + queueBias;
                        SetKeyword(mat, "_ALPHATEST_ON", false);
                    }
                    break;

                case TransparentMode.Transparent:
                    zWriteProperty.floatValue = 0;
                    foreach (Material mat in _rootItem.Mats)
                    {
                        bool uiEffect = IsUIEffectMode(mat);
                        mat.renderQueue = 3000 + queueBias;
                        SetKeyword(mat, "_ALPHATEST_ON", false);
                    }
                    break;

                case TransparentMode.CutOff:
                    zWriteProperty.floatValue = 1;
                    foreach (Material mat in _rootItem.Mats)
                    {
                        mat.renderQueue = 2450 + queueBias;
                        SetKeyword(mat, "_ALPHATEST_ON", true);
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
                SyncBlendMode(mat);
                SyncLightMode(mat);
                SyncTimeMode(mat, flags);
                SyncTogglePropertyKeywords(mat);
                SyncModePropertyKeywords(mat, flags);
                SyncFlagBackedKeywords(mat, flags);
                SyncScreenDistortPasses(mat);
                SyncVatKeywords(mat);
                SyncParallaxLayerCount(mat);
            }
        }

        public void ApplyToggleFlag(int flagBits, bool enabled, int flagIndex = 0)
        {
            foreach (ShaderFlagsBase flagBase in _rootItem.ShaderFlags)
            {
                if (enabled)
                {
                    flagBase.SetFlagBits(flagBits, index: flagIndex);
                }
                else
                {
                    flagBase.ClearFlagBits(flagBits, index: flagIndex);
                }
            }
        }

        public void ApplyToggleFlagAndKeyword(int flagBits, int flagIndex, string keyword, bool enabled)
        {
            ApplyToggleFlag(flagBits, enabled, flagIndex);
            ApplyToggleKeyword(keyword, enabled);
        }

        public void ApplyShaderPass(string passName, bool enabled)
        {
            foreach (Material mat in _rootItem.Mats)
            {
                mat.SetShaderPassEnabled(passName, enabled);
            }
        }

        public void ApplyScreenDistortMode(int mode)
        {
            foreach (Material mat in _rootItem.Mats)
            {
                bool deferred = mode == 1;
                bool cameraOpaque = mode == 2;
                bool disableMainPass = mat.HasProperty("_DisableMainPassToggle") &&
                                       mat.GetFloat("_DisableMainPassToggle") > 0.5f;

                mat.SetShaderPassEnabled("NBCameraOpaqueDistortPass", cameraOpaque);
                mat.SetShaderPassEnabled("NBDeferredDistortPass", deferred);
                mat.SetShaderPassEnabled("UniversalForward", mode == 0 || !disableMainPass);

                if (mode == 0 && mat.HasProperty("_DisableMainPassToggle"))
                {
                    mat.SetFloat("_DisableMainPassToggle", 0f);
                }
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
                        mat.SetFloat("_TransparentMode", (float)TransparentMode.CutOff);
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
                if (mat.HasProperty("_VAT_Toggle"))
                {
                    mat.SetFloat("_VAT_Toggle", enabled ? 1f : 0f);
                }

                SyncVatKeywords(mat);
            }
        }

        public void ApplyFlipbookEnabled(bool enabled)
        {
            foreach (Material mat in _rootItem.Mats)
            {
                if (mat.HasProperty("_FlipbookBlending"))
                {
                    mat.SetFloat("_FlipbookBlending", enabled ? 1f : 0f);
                }

                if (enabled)
                {
                    DisableVat(mat);
                    SetKeyword(mat, "_FLIPBOOKBLENDING_ON", true);
                }
                else
                {
                    SetKeyword(mat, "_FLIPBOOKBLENDING_ON", false);
                }
            }
        }

        public void ApplyBlendMode(BlendMode mode)
        {
            foreach (Material mat in _rootItem.Mats)
            {
                switch (mode)
                {
                    case BlendMode.Alpha:
                        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        SetKeyword(mat, "_ALPHAPREMULTIPLY_ON", false);
                        SetKeyword(mat, "_ALPHAMODULATE_ON", false);
                        break;
                    case BlendMode.Premultiply:
                        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        SetKeyword(mat, "_ALPHAPREMULTIPLY_ON", true);
                        SetKeyword(mat, "_ALPHAMODULATE_ON", false);
                        break;
                    case BlendMode.Additive:
                        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        SetKeyword(mat, "_ALPHAPREMULTIPLY_ON", true);
                        SetKeyword(mat, "_ALPHAMODULATE_ON", false);
                        break;
                    case BlendMode.Multiply:
                        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.DstColor);
                        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        SetKeyword(mat, "_ALPHAPREMULTIPLY_ON", false);
                        SetKeyword(mat, "_ALPHAMODULATE_ON", true);
                        break;
                    case BlendMode.Opaque:
                        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                        SetKeyword(mat, "_ALPHAPREMULTIPLY_ON", false);
                        SetKeyword(mat, "_ALPHAMODULATE_ON", false);
                        break;
                }
            }
        }

        public void ApplyLightMode(FxLightMode mode)
        {
            foreach (Material mat in _rootItem.Mats)
            {
                SetLightModeKeyword(mat, mode);
            }
        }

        public void ApplyToggleKeyword(string keyword, bool enabled)
        {
            foreach (Material mat in _rootItem.Mats)
            {
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
            SetFloatIfExists(mat, "_StencilReadMask", 255f);
            SetFloatIfExists(mat, "_StencilWriteMask", 255f);
            SetFloatIfExists(mat, "_StencilKeyIndex", keyIndex);
        }

        private static void SetFloatIfExists(Material mat, string propertyName, float value)
        {
            if (mat.HasProperty(propertyName))
            {
                mat.SetFloat(propertyName, value);
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

            SetFlag(flags, NBShaderFlags.FLAG_BIT_PARTICLE_1_IS_PARTICLE_SYSTEM, isParticle, 1);
            SetFlag(flags, NBShaderFlags.FLAG_BIT_PARTICLE_1_UV_FROM_MESH, mode == MeshSourceMode.Mesh, 1);
            SetFlag(flags, NBShaderFlags.FLAG_BIT_PARTICLE_UIEFFECT_ON, isUIEffect, 0);
            SetFlag(flags, NBShaderFlags.FLAG_BIT_PARTICLE_1_UIEFFECT_SPRITE_MODE, mode == MeshSourceMode.UIEffectSprite, 1);
            SetFlag(flags, NBShaderFlags.FLAG_BIT_PARTICLE_1_UIEFFECT_BASEMAP_MODE, mode == MeshSourceMode.UIEffectBaseMap, 1);
            if (mode == MeshSourceMode.Particle)
            {
                SetFlag(flags, NBShaderFlags.FLAG_BIT_PARTICLE_1_ANIMATION_SHEET_HELPER, false, 1);
            }

            if (mat.HasProperty("_CustomData"))
            {
                mat.SetFloat("_CustomData", isParticle ? 1f : 0f);
            }

            if (isParticle)
            {
                SetKeyword(mat, "_CUSTOMDATA", true);
                SetKeyword(mat, "_PARCUSTOMDATA_ON", true);
            }
            else
            {
                SetKeyword(mat, "_CUSTOMDATA", false);
                SetKeyword(mat, "_PARCUSTOMDATA_ON", false);
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
            SetKeyword(mat, "_UNSCALETIME", mode == TimeMode.UnScaleTime);
            SetKeyword(mat, "_SCRIPTABLETIME", mode == TimeMode.ScriptableTime);
        }

        private void SyncTogglePropertyKeywords(Material mat)
        {
            for (int i = 0; i < ToggleKeywordBindings.Length; i++)
            {
                var binding = ToggleKeywordBindings[i];
                if (!mat.HasProperty(binding.propertyName))
                {
                    continue;
                }

                SetKeyword(mat, binding.keyword, mat.GetFloat(binding.propertyName) > 0.5f);
            }
        }

        private void SyncModePropertyKeywords(Material mat, NBShaderFlags flags)
        {
            if (mat.HasProperty("_DistortMode"))
            {
                SetKeyword(mat, "_DISTORT_REFRACTION", Mathf.RoundToInt(mat.GetFloat("_DistortMode")) == 1);
            }

            if (mat.HasProperty("_RampColorSourceMode"))
            {
                bool useRampMap = Mathf.RoundToInt(mat.GetFloat("_RampColorSourceMode")) == 1;
                if (flags != null)
                {
                    SetFlag(flags, NBShaderFlags.FLAG_BIT_PARTICLE_RAMP_COLOR_MAP_MODE_ON, useRampMap, 0);
                }

                SetKeyword(mat, "_COLOR_RAMP_MAP", useRampMap);
            }

            if (mat.HasProperty("_DissolveRampSourceMode"))
            {
                bool useDissolveRampMap = Mathf.RoundToInt(mat.GetFloat("_DissolveRampSourceMode")) == 1;
                bool dissolveRampEnabled = IsDissolveRampEnabled(mat, flags);
                if (flags != null)
                {
                    SetFlag(flags, NBShaderFlags.FLAG_BIT_PARTICLE_DISSOLVE_RAMP_MAP, useDissolveRampMap, 0);
                }

                SetKeyword(mat, "_DISSOLVE_RAMP_MAP", dissolveRampEnabled && useDissolveRampMap);
            }

            if (mat.HasProperty("_ScreenDistortModeToggle"))
            {
                SetKeyword(mat, "_SCREEN_DISTORT_MODE", Mathf.RoundToInt(mat.GetFloat("_ScreenDistortModeToggle")) != 0);
            }
        }

        private void SyncFlagBackedKeywords(Material mat, NBShaderFlags flags)
        {
            if (flags == null || mat == null)
            {
                return;
            }

            SetFlagBackedKeyword(mat, flags, "_DISTANCE_FADE", NBShaderFlags.FLAG_BIT_PARTICLE_DISTANCEFADE_ON, 0);
            SetFlagBackedKeyword(mat, flags, "_FRESNEL", NBShaderFlags.FLAG_BIT_PARTICLE_FRESNEL_ON, 0);
            SetFlagBackedKeyword(mat, flags, "_CHROMATIC_ABERRATION", NBShaderFlags.FLAG_BIT_PARTICLE_CHORATICABERRAT, 0);
            bool dissolveRampEnabled = flags.CheckFlagBits(NBShaderFlags.FLAG_BIT_PARTICLE_1_DISSOVLE_USE_RAMP, index: 1);
            SetKeyword(mat, "_DISSOLVE_RAMP", dissolveRampEnabled);
            SetKeyword(mat, "_DISSOLVE_RAMP_MAP", dissolveRampEnabled && flags.CheckFlagBits(NBShaderFlags.FLAG_BIT_PARTICLE_DISSOLVE_RAMP_MAP, index: 0));
            SetFlagBackedKeyword(mat, flags, "_DISSOLVE_MASK", NBShaderFlags.FLAG_BIT_PARTICLE_DISSOLVE_MASK, 0);
            SetFlagBackedKeyword(mat, flags, "_COLOR_RAMP_MAP", NBShaderFlags.FLAG_BIT_PARTICLE_RAMP_COLOR_MAP_MODE_ON, 0);
            SetFlagBackedKeyword(mat, flags, "_VERTEX_OFFSET", NBShaderFlags.FLAG_BIT_PARTICLE_VERTEX_OFFSET_ON, 0);

            SetFlagBackedKeyword(mat, flags, "_DEPTH_OUTLINE", NBShaderFlags.FLAG_BIT_PARTICLE_1_DEPTH_OUTLINE, 1);
            SetFlagBackedKeyword(mat, flags, "_MASKMAP2_ON", NBShaderFlags.FLAG_BIT_PARTICLE_1_MASK_MAP2, 1);
            SetFlagBackedKeyword(mat, flags, "_MASKMAP3_ON", NBShaderFlags.FLAG_BIT_PARTICLE_1_MASK_MAP3, 1);
            SetFlagBackedKeyword(mat, flags, "_NOISE_MASKMAP", NBShaderFlags.FLAG_BIT_PARTICLE_1_NOISE_MASKMAP, 1);
            SetFlagBackedKeyword(mat, flags, "_PROGRAM_NOISE_SIMPLE", NBShaderFlags.FLAG_BIT_PARTICLE_1_PROGRAM_NOISE_SIMPLE, 1);
            SetFlagBackedKeyword(mat, flags, "_PROGRAM_NOISE_VORONOI", NBShaderFlags.FLAG_BIT_PARTICLE_1_PROGRAM_NOISE_VORONOI, 1);
            SetFlagBackedKeyword(mat, flags, "_VERTEX_OFFSET_MASKMAP", NBShaderFlags.FLAG_BIT_PARTICLE_1_VERTEXOFFSET_MASKMAP, 1);
        }

        private static bool IsDissolveRampEnabled(Material mat, NBShaderFlags flags)
        {
            if (mat != null && mat.HasProperty("_Dissolve_useRampMap_Toggle"))
            {
                return mat.GetFloat("_Dissolve_useRampMap_Toggle") > 0.5f;
            }

            return flags != null && flags.CheckFlagBits(NBShaderFlags.FLAG_BIT_PARTICLE_1_DISSOVLE_USE_RAMP, index: 1);
        }

        private void SetFlagBackedKeyword(Material mat, NBShaderFlags flags, string keyword, int flagBits, int flagIndex)
        {
            SetKeyword(mat, keyword, flags.CheckFlagBits(flagBits, index: flagIndex));
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

            if (enabled && IsKeywordAllowed(keyword))
            {
                mat.EnableKeyword(keyword);
            }
            else
            {
                mat.DisableKeyword(keyword);
            }
        }

        private static void SyncScreenDistortPasses(Material mat)
        {
            if (!mat.HasProperty("_ScreenDistortModeToggle"))
            {
                return;
            }

            int mode = Mathf.RoundToInt(mat.GetFloat("_ScreenDistortModeToggle"));
            bool deferred = mode == 1;
            bool cameraOpaque = mode == 2;
            bool disableMainPass = mat.HasProperty("_DisableMainPassToggle") &&
                                   mat.GetFloat("_DisableMainPassToggle") > 0.5f;

            mat.SetShaderPassEnabled("NBCameraOpaqueDistortPass", cameraOpaque);
            mat.SetShaderPassEnabled("NBDeferredDistortPass", deferred);
            mat.SetShaderPassEnabled("UniversalForward", mode == 0 || !disableMainPass);

            if (mode == 0 && mat.HasProperty("_DisableMainPassToggle"))
            {
                mat.SetFloat("_DisableMainPassToggle", 0f);
            }
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
                flags.ClearFlagBits(NBShaderFlags.FLAG_BIT_PARTICLE_1_USE_TEXCOORD1, index: 1);
                flags.ClearFlagBits(NBShaderFlags.FLAG_BIT_PARTICLE_1_USE_TEXCOORD2, index: 1);
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

            switch (mode)
            {
                case TransparentMode.Opaque:
                    mat.SetInt("_ZWrite", 1);
                    mat.renderQueue = 2100 + queueBias;
                    mat.SetFloat("_Blend", (float)BlendMode.Opaque);
                    SetKeyword(mat, "_ALPHATEST_ON", false);
                    break;
                case TransparentMode.Transparent:
                    mat.SetInt("_ZWrite", 0);
                    mat.renderQueue = (uiEffect ? 3000 : 3100) + queueBias;
                    if (mat.HasProperty("_Blend") && (BlendMode)Mathf.RoundToInt(mat.GetFloat("_Blend")) == BlendMode.Opaque)
                    {
                        mat.SetFloat("_Blend", (float)BlendMode.Alpha);
                    }

                    SetKeyword(mat, "_ALPHATEST_ON", false);
                    break;
                case TransparentMode.CutOff:
                    mat.SetInt("_ZWrite", 1);
                    mat.renderQueue = 2450 + queueBias;
                    mat.SetFloat("_Blend", (float)BlendMode.Opaque);
                    SetKeyword(mat, "_ALPHATEST_ON", true);
                    break;
            }

            if (mat.HasProperty("_ForceZWriteToggle"))
            {
                float forceZWrite = mat.GetFloat("_ForceZWriteToggle");
                if (forceZWrite > 0.5f && forceZWrite < 1.5f)
                {
                    mat.SetInt("_ZWrite", 1);
                }
                else if (forceZWrite > 1.5f)
                {
                    mat.SetInt("_ZWrite", 0);
                }
            }
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
                mat.SetVector("_ParallaxMapping_Vec", value);
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
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    SetKeyword(mat, "_ALPHAPREMULTIPLY_ON", false);
                    SetKeyword(mat, "_ALPHAMODULATE_ON", false);
                    break;
                case BlendMode.Premultiply:
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    SetKeyword(mat, "_ALPHAPREMULTIPLY_ON", true);
                    SetKeyword(mat, "_ALPHAMODULATE_ON", false);
                    break;
                case BlendMode.Additive:
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    SetKeyword(mat, "_ALPHAPREMULTIPLY_ON", true);
                    SetKeyword(mat, "_ALPHAMODULATE_ON", false);
                    break;
                case BlendMode.Multiply:
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.DstColor);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    SetKeyword(mat, "_ALPHAPREMULTIPLY_ON", false);
                    SetKeyword(mat, "_ALPHAMODULATE_ON", true);
                    break;
                case BlendMode.Opaque:
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    SetKeyword(mat, "_ALPHAPREMULTIPLY_ON", false);
                    SetKeyword(mat, "_ALPHAMODULATE_ON", false);
                    break;
            }
        }

        private void SyncLightMode(Material mat)
        {
            if (!mat.HasProperty("_FxLightMode"))
            {
                return;
            }

            SetLightModeKeyword(mat, (FxLightMode)Mathf.RoundToInt(mat.GetFloat("_FxLightMode")));
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
            if (mat.HasProperty("_VAT_Toggle"))
            {
                mat.SetFloat("_VAT_Toggle", 0f);
            }

            ClearVatKeywords(mat);
        }

        private void DisableFlipbook(Material mat)
        {
            if (mat.HasProperty("_FlipbookBlending"))
            {
                mat.SetFloat("_FlipbookBlending", 0f);
            }

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
            string[] keywords =
            {
                "_HOUDINI_VAT_SOFTBODY",
                "_HOUDINI_VAT_RIGIDBODY",
                "_HOUDINI_VAT_DYNAMIC_REMESH",
                "_HOUDINI_VAT_PARTICLE_SPRITE"
            };

            SetExclusiveKeyword(mat, keywords, enabledIndex);
        }

        private void SetTyflowVATKeyword(Material mat, int enabledIndex)
        {
            string[] keywords =
            {
                "_TYFLOW_VAT_ABSOLUTE",
                "_TYFLOW_VAT_RELATIVE",
                "_TYFLOW_VAT_SKIN_R",
                "_TYFLOW_VAT_SKIN_PR",
                "_TYFLOW_VAT_SKIN_PRSAVE",
                "_TYFLOW_VAT_SKIN_PRSXYZ"
            };

            SetExclusiveKeyword(mat, keywords, enabledIndex);
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
            if (enabled)
            {
                flags.SetFlagBits(flagBits, index: index);
            }
            else
            {
                flags.ClearFlagBits(flagBits, index: index);
            }
        }

        private struct KeywordToggleBinding
        {
            public readonly string propertyName;
            public readonly string keyword;

            public KeywordToggleBinding(string propertyName, string keyword)
            {
                this.propertyName = propertyName;
                this.keyword = keyword;
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
