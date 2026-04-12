using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using NBShader;

namespace NBShaderEditor
{
    internal sealed class ParticleBaseTyflowVATGUI
    {
        private const string TyflowSubModeProperty = "_TyFlowVATSubMode";
        private const int TyflowSkinModeStart = 2;
        private const float TyflowParticleHighestNonSkinMode = 1.5f;

        private readonly ShaderGUIHelper _helper;

        private static readonly string[] TyflowAnimationModeNames =
        {
            "Absolute positions",
            "Relative offsets",
            "Skin (R)",
            "Skin (PR)",
            "Skin (PRSAVE)",
            "Skin (PRSXYZ)"
        };

        private static readonly string[] TyflowModeKeywords =
        {
            "_TYFLOW_VAT_ABSOLUTE",
            "_TYFLOW_VAT_RELATIVE",
            "_TYFLOW_VAT_SKIN_R",
            "_TYFLOW_VAT_SKIN_PR",
            "_TYFLOW_VAT_SKIN_PRSAVE",
            "_TYFLOW_VAT_SKIN_PRSXYZ"
        };

        public ParticleBaseTyflowVATGUI(ShaderGUIHelper helper)
        {
            _helper = helper;
        }

        public void Draw()
        {
            DrawTyflowSettings();
            SyncKeywords();
        }

        public static void AppendRequiredVertexStreams(
            Material material,
            List<ParticleSystemVertexStream> streams,
            List<string> streamList)
        {
            if (!IsTyflowParticleModeEnabled(material))
            {
                return;
            }

            AddStream(streams, streamList, ParticleSystemVertexStream.UV2, ParticleBaseGUI.streamUV2Text);
        }

        private void DrawTyflowSettings()
        {
            _helper.DrawTexture("VAT texture", "_VATTex", drawScaleOffset: false, drawWrapMode: false);
            _helper.DrawFloat("ImportScale", "_ImportScale");
            _helper.DrawPopUp("TyFlow VAT Sub Mode", TyflowSubModeProperty, TyflowAnimationModeNames);

            if (HasUnsupportedParticleMode())
            {
                EditorGUILayout.HelpBox("该 TyFlow VAT 类型需要 Mesh 多 UV 数据，不支持 ParticleSystem VertexStream 模式。", MessageType.Warning);
            }
            else if (HasParticleUV2Conflict())
            {
                EditorGUILayout.HelpBox("TyFlow VAT uses UV2 (TEXCOORD0.zw) as vertexIndex / vertexCount in ParticleSystem mode. Flipbook blending or Special UV (UV2) conflicts with it; VAT takes priority.", MessageType.Warning);
            }

            _helper.DrawToggle("Deforming skin", "_DeformingSkin");
            _helper.DrawFloat("Skin bone count", "_SkinBoneCount");
            _helper.DrawToggle("RGBA encoded", "_RGBAEncoded");
            _helper.DrawToggle("RGBA half", "_RGBAHalf");
            _helper.DrawToggle("Gamma correction", "_LinearToGamma");
            _helper.DrawToggle("VAT includes normals", "_VATIncludesNormals");
            _helper.DrawToggle("Affects shadows", "_AffectsShadows");
            _helper.DrawFloat("Frame", "_Frame");
            DrawFrameCustomDataSelect();
            _helper.DrawFloat("Frames", "_Frames");
            _helper.DrawToggle("Frame interpolation", "_FrameInterpolation");
            _helper.DrawToggle("Loop", "_Loop");
            _helper.DrawToggle("Interpolate loop", "_InterpolateLoop");
            _helper.DrawToggle("Autoplay", "_Autoplay");
            _helper.DrawFloat("AutoplaySpeed", "_AutoplaySpeed");
        }

        private void DrawFrameCustomDataSelect()
        {
            const int dataBitPos = W9ParticleShaderFlags.FLAGBIT_POS_2_CUSTOMDATA_VAT_FRAME;
            const int dataIndex = 2;
            W9ParticleShaderFlags.CutomDataComponent component = GetCurrentCustomDataComponent(dataBitPos, dataIndex);
            if (!IsAnyTyflowParticleModeEnabled() || _helper.shaderFlags == null || _helper.shaderFlags.Length == 0)
            {
                if (component != W9ParticleShaderFlags.CutomDataComponent.Off)
                {
                    component = W9ParticleShaderFlags.CutomDataComponent.Off;
                    for (int i = 0; i < _helper.shaderFlags.Length; i++)
                    {
                        if (_helper.shaderFlags[i] is W9ParticleShaderFlags flags)
                        {
                            flags.SetCustomDataFlag(component, dataBitPos, dataIndex);
                        }
                    }
                }
                
                return;
            }

            (string, string) nameTuple = ("VATFrameCustomData", "");

            EditorGUI.showMixedValue = CustomDataHasMixedValue(dataBitPos, dataIndex);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginHorizontal();
            component = (W9ParticleShaderFlags.CutomDataComponent)EditorGUILayout.Popup(new GUIContent("VAT Frame CustomData"), (int)component, ParticleBaseGUI.CustomDataOptions);
            EditorGUI.showMixedValue = false;

            Action applySelection = () =>
            {
                for (int i = 0; i < _helper.shaderFlags.Length; i++)
                {
                    if (_helper.shaderFlags[i] is W9ParticleShaderFlags flags)
                    {
                        flags.SetCustomDataFlag(component, dataBitPos, dataIndex);
                    }
                }

                _helper.ResetTool.CheckOnValueChange(nameTuple);
            };

            if (EditorGUI.EndChangeCheck())
            {
                applySelection();
            }

            _helper.ResetTool.DrawResetModifyButton(
                new Rect(),
                nameTuple,
                resetCallBack: () =>
                {
                    component = W9ParticleShaderFlags.CutomDataComponent.Off;
                    applySelection();
                },
                onValueChangedCallBack: applySelection,
                checkHasModifyOnValueChange: () => GetCurrentCustomDataComponent(dataBitPos, dataIndex) != W9ParticleShaderFlags.CutomDataComponent.Off,
                checkHasMixedValueOnValueChange: () => CustomDataHasMixedValue(dataBitPos, dataIndex));
            EditorGUILayout.EndHorizontal();
            _helper.ResetTool.EndResetModifyButtonScope();
        }

        private W9ParticleShaderFlags.CutomDataComponent GetCurrentCustomDataComponent(int dataBitPos, int dataIndex)
        {
            if (_helper.shaderFlags == null || _helper.shaderFlags.Length == 0)
            {
                return W9ParticleShaderFlags.CutomDataComponent.Off;
            }

            if (_helper.shaderFlags[0] is W9ParticleShaderFlags flags)
            {
                return flags.GetCustomDataFlag(dataBitPos, dataIndex);
            }

            return W9ParticleShaderFlags.CutomDataComponent.Off;
        }

        private bool CustomDataHasMixedValue(int dataBitPos, int dataIndex)
        {
            if (_helper.shaderFlags == null || _helper.shaderFlags.Length == 0)
            {
                return false;
            }

            W9ParticleShaderFlags.CutomDataComponent component = W9ParticleShaderFlags.CutomDataComponent.UnKnownOrMixed;
            for (int i = 0; i < _helper.shaderFlags.Length; i++)
            {
                if (!(_helper.shaderFlags[i] is W9ParticleShaderFlags flags))
                {
                    continue;
                }

                W9ParticleShaderFlags.CutomDataComponent current = flags.GetCustomDataFlag(dataBitPos, dataIndex);
                if (component == W9ParticleShaderFlags.CutomDataComponent.UnKnownOrMixed)
                {
                    component = current;
                    continue;
                }

                if (component != current)
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasUnsupportedParticleMode()
        {
            if (_helper.mats == null)
            {
                return false;
            }

            for (int i = 0; i < _helper.mats.Count; i++)
            {
                if (IsUnsupportedParticleMode(_helper.mats[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsAnyTyflowParticleModeEnabled()
        {
            if (_helper.mats == null)
            {
                return false;
            }

            for (int i = 0; i < _helper.mats.Count; i++)
            {
                if (IsTyflowParticleModeEnabled(_helper.mats[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasParticleUV2Conflict()
        {
            if (_helper.mats == null || _helper.mats.Count == 0 || _helper.shaderFlags == null || _helper.shaderFlags.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < _helper.mats.Count; i++)
            {
                Material mat = _helper.mats[i];
                if (!IsTyflowParticleModeEnabled(mat))
                {
                    continue;
                }

                W9ParticleShaderFlags flags = _helper.shaderFlags[i] as W9ParticleShaderFlags;
                if (flags == null)
                {
                    continue;
                }

                bool usesFlipbookBlending = mat.IsKeywordEnabled("_FLIPBOOKBLENDING_ON");
                bool usesSpecialUVOnUV2 = flags.CheckIsUVModeOn(W9ParticleShaderFlags.UVMode.SpecialUVChannel) &&
                                          !flags.CheckFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_USE_TEXCOORD2, index: 1);
                if (usesFlipbookBlending || usesSpecialUVOnUV2)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsTyflowParticleModeEnabled(Material material)
        {
            if (material == null ||
                !material.HasProperty("_VAT_Toggle") ||
                !material.HasProperty("_VATMode") ||
                !material.HasProperty("_MeshSourceMode") ||
                material.GetFloat("_VAT_Toggle") <= 0.5f ||
                Mathf.RoundToInt(material.GetFloat("_VATMode")) != (int)ParticleBaseGUI.VATMode.Tyflow)
            {
                return false;
            }

            ParticleBaseGUI.MeshSourceMode meshSourceMode = (ParticleBaseGUI.MeshSourceMode)Mathf.RoundToInt(material.GetFloat("_MeshSourceMode"));
            if (meshSourceMode != ParticleBaseGUI.MeshSourceMode.Particle &&
                meshSourceMode != ParticleBaseGUI.MeshSourceMode.UIParticle)
            {
                return false;
            }

            return GetMaterialSubMode(material) <= TyflowParticleHighestNonSkinMode;
        }

        private static bool IsUnsupportedParticleMode(Material material)
        {
            if (material == null ||
                !material.HasProperty("_VAT_Toggle") ||
                !material.HasProperty("_VATMode") ||
                !material.HasProperty("_MeshSourceMode") ||
                material.GetFloat("_VAT_Toggle") <= 0.5f ||
                Mathf.RoundToInt(material.GetFloat("_VATMode")) != (int)ParticleBaseGUI.VATMode.Tyflow)
            {
                return false;
            }

            ParticleBaseGUI.MeshSourceMode meshSourceMode = (ParticleBaseGUI.MeshSourceMode)Mathf.RoundToInt(material.GetFloat("_MeshSourceMode"));
            if (meshSourceMode != ParticleBaseGUI.MeshSourceMode.Particle &&
                meshSourceMode != ParticleBaseGUI.MeshSourceMode.UIParticle)
            {
                return false;
            }

            return GetMaterialSubMode(material) >= TyflowSkinModeStart;
        }

        private static int GetMaterialSubMode(Material material)
        {
            if (!material.HasProperty(TyflowSubModeProperty))
            {
                return 0;
            }

            return Mathf.Clamp(Mathf.RoundToInt(material.GetFloat(TyflowSubModeProperty)), 0, TyflowModeKeywords.Length - 1);
        }

        private void SyncKeywords()
        {
            if (_helper.mats == null)
            {
                return;
            }

            for (int i = 0; i < _helper.mats.Count; i++)
            {
                SyncKeywords(_helper.mats[i]);
            }
        }

        internal static void SyncKeywords(Material mat)
        {
            if (mat == null)
            {
                return;
            }

            SetSubModeKeyword(mat, GetMaterialSubMode(mat));
        }

        internal static void ClearKeywords(Material mat)
        {
            if (mat == null)
            {
                return;
            }

            SetSubModeKeyword(mat, -1);
        }

        private static void SetSubModeKeyword(Material mat, int enabledIndex)
        {
            for (int i = 0; i < TyflowModeKeywords.Length; i++)
            {
                mat.DisableKeyword(TyflowModeKeywords[i]);
            }

            if (enabledIndex >= 0 && enabledIndex < TyflowModeKeywords.Length)
            {
                mat.EnableKeyword(TyflowModeKeywords[enabledIndex]);
            }
        }

        private static void AddStream(
            List<ParticleSystemVertexStream> streams,
            List<string> streamList,
            ParticleSystemVertexStream stream,
            string streamLabel)
        {
            if (streams.Contains(stream))
            {
                return;
            }

            streams.Add(stream);
            streamList.Add(streamLabel);
        }
    }
}
