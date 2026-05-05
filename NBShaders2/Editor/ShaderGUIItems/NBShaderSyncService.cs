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
                        mat.DisableKeyword("_ALPHATEST_ON");
                    }
                    break;

                case TransparentMode.Transparent:
                    zWriteProperty.floatValue = 0;
                    foreach (Material mat in _rootItem.Mats)
                    {
                        bool uiEffect = IsUIEffectMode(mat);
                        mat.renderQueue = 3000 + queueBias;
                        mat.DisableKeyword("_ALPHATEST_ON");
                    }
                    break;

                case TransparentMode.CutOff:
                    zWriteProperty.floatValue = 1;
                    foreach (Material mat in _rootItem.Mats)
                    {
                        mat.renderQueue = 2450 + queueBias;
                        mat.EnableKeyword("_ALPHATEST_ON");
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
                    mat.EnableKeyword("_FLIPBOOKBLENDING_ON");
                }
                else
                {
                    mat.DisableKeyword("_FLIPBOOKBLENDING_ON");
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
                        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        mat.DisableKeyword("_ALPHAMODULATE_ON");
                        break;
                    case BlendMode.Premultiply:
                        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                        mat.DisableKeyword("_ALPHAMODULATE_ON");
                        break;
                    case BlendMode.Additive:
                        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                        mat.DisableKeyword("_ALPHAMODULATE_ON");
                        break;
                    case BlendMode.Multiply:
                        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.DstColor);
                        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        mat.EnableKeyword("_ALPHAMODULATE_ON");
                        break;
                    case BlendMode.Opaque:
                        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        mat.DisableKeyword("_ALPHAMODULATE_ON");
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
                if (enabled)
                {
                    mat.EnableKeyword(keyword);
                }
                else
                {
                    mat.DisableKeyword(keyword);
                }
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
                mat.EnableKeyword("_CUSTOMDATA");
            }
            else
            {
                mat.DisableKeyword("_CUSTOMDATA");
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
                    mat.DisableKeyword("_ALPHATEST_ON");
                    break;
                case TransparentMode.Transparent:
                    mat.SetInt("_ZWrite", 0);
                    mat.renderQueue = (uiEffect ? 3000 : 3100) + queueBias;
                    if (mat.HasProperty("_Blend") && (BlendMode)Mathf.RoundToInt(mat.GetFloat("_Blend")) == BlendMode.Opaque)
                    {
                        mat.SetFloat("_Blend", (float)BlendMode.Alpha);
                    }

                    mat.DisableKeyword("_ALPHATEST_ON");
                    break;
                case TransparentMode.CutOff:
                    mat.SetInt("_ZWrite", 1);
                    mat.renderQueue = 2450 + queueBias;
                    mat.SetFloat("_Blend", (float)BlendMode.Opaque);
                    mat.EnableKeyword("_ALPHATEST_ON");
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
                    mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    mat.DisableKeyword("_ALPHAMODULATE_ON");
                    break;
                case BlendMode.Premultiply:
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                    mat.DisableKeyword("_ALPHAMODULATE_ON");
                    break;
                case BlendMode.Additive:
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                    mat.DisableKeyword("_ALPHAMODULATE_ON");
                    break;
                case BlendMode.Multiply:
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.DstColor);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    mat.EnableKeyword("_ALPHAMODULATE_ON");
                    break;
                case BlendMode.Opaque:
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    mat.DisableKeyword("_ALPHAMODULATE_ON");
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

        private static void SetLightModeKeyword(Material mat, FxLightMode mode)
        {
            mat.DisableKeyword("_FX_LIGHT_MODE_UNLIT");
            mat.DisableKeyword("_FX_LIGHT_MODE_BLINN_PHONG");
            mat.DisableKeyword("_FX_LIGHT_MODE_HALF_LAMBERT");
            mat.DisableKeyword("_FX_LIGHT_MODE_PBR");
            mat.DisableKeyword("_FX_LIGHT_MODE_SIX_WAY");
            mat.DisableKeyword("EVALUATE_SH_VERTEX");

            switch (mode)
            {
                case FxLightMode.UnLit:
                    mat.EnableKeyword("_FX_LIGHT_MODE_UNLIT");
                    break;
                case FxLightMode.BlinnPhong:
                    mat.EnableKeyword("_FX_LIGHT_MODE_BLINN_PHONG");
                    break;
                case FxLightMode.HalfLambert:
                    mat.EnableKeyword("_FX_LIGHT_MODE_HALF_LAMBERT");
                    break;
                case FxLightMode.PBR:
                    mat.EnableKeyword("_FX_LIGHT_MODE_PBR");
                    break;
                case FxLightMode.SixWay:
                    mat.EnableKeyword("_FX_LIGHT_MODE_SIX_WAY");
                    mat.EnableKeyword("EVALUATE_SH_VERTEX");
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
            mat.EnableKeyword("_VAT");
            int vatMode = mat.HasProperty("_VATMode") ? Mathf.RoundToInt(mat.GetFloat("_VATMode")) : 0;
            if (vatMode == (int)VATMode.Tyflow)
            {
                mat.DisableKeyword("_VAT_HOUDINI");
                mat.EnableKeyword("_VAT_TYFLOW");
                SetHoudiniVATKeyword(mat, -1);
                SetTyflowVATKeyword(mat, mat.HasProperty("_TyFlowVATSubMode") ? Mathf.RoundToInt(mat.GetFloat("_TyFlowVATSubMode")) : 0);
            }
            else
            {
                mat.EnableKeyword("_VAT_HOUDINI");
                mat.DisableKeyword("_VAT_TYFLOW");
                SetHoudiniVATKeyword(mat, mat.HasProperty("_HoudiniVATSubMode") ? Mathf.RoundToInt(mat.GetFloat("_HoudiniVATSubMode")) : 0);
                SetTyflowVATKeyword(mat, -1);
            }
        }

        private static void DisableVat(Material mat)
        {
            if (mat.HasProperty("_VAT_Toggle"))
            {
                mat.SetFloat("_VAT_Toggle", 0f);
            }

            ClearVatKeywords(mat);
        }

        private static void DisableFlipbook(Material mat)
        {
            if (mat.HasProperty("_FlipbookBlending"))
            {
                mat.SetFloat("_FlipbookBlending", 0f);
            }

            mat.DisableKeyword("_FLIPBOOKBLENDING_ON");
        }

        private static void ClearVatKeywords(Material mat)
        {
            mat.DisableKeyword("_VAT");
            mat.DisableKeyword("_VAT_HOUDINI");
            mat.DisableKeyword("_VAT_TYFLOW");
            SetHoudiniVATKeyword(mat, -1);
            SetTyflowVATKeyword(mat, -1);
        }

        private static void SetHoudiniVATKeyword(Material mat, int enabledIndex)
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

        private static void SetTyflowVATKeyword(Material mat, int enabledIndex)
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

        private static void SetExclusiveKeyword(Material mat, string[] keywords, int enabledIndex)
        {
            for (int i = 0; i < keywords.Length; i++)
            {
                mat.DisableKeyword(keywords[i]);
            }

            if (enabledIndex >= 0 && enabledIndex < keywords.Length)
            {
                mat.EnableKeyword(keywords[enabledIndex]);
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
