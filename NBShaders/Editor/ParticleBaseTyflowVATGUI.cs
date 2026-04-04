using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using NBShader;

namespace NBShaderEditor
{
    internal sealed class ParticleBaseTyflowVATGUI
    {
        private const int MeshSourceModeParticle = 0;
        private const int MeshSourceModeUIParticle = 5;
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

        private static readonly string[] CustomDataOptions =
        {
            "**OFF**",
            "CustomData1_X",
            "CustomData1_Y",
            "CustomData1_Z",
            "CustomData1_W",
            "CustomData2_X",
            "CustomData2_Y",
            "CustomData2_Z",
            "CustomData2_W"
        };

        public ParticleBaseTyflowVATGUI(ShaderGUIHelper helper)
        {
            _helper = helper;
        }

        public void Draw()
        {
            DrawTyflowSettings();
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
            _helper.DrawPopUp("Mode", "_Mode", TyflowAnimationModeNames);

            if (IsTyflowSkinModeSelected())
            {
                EditorGUILayout.HelpBox("TyFlow VAT skin modes are TODO and are currently skipped.", MessageType.Warning);
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
            if (!IsAnyTyflowParticleModeEnabled() || _helper.shaderFlags == null || _helper.shaderFlags.Length == 0)
            {
                return;
            }

            const int dataBitPos = W9ParticleShaderFlags.FLAGBIT_POS_2_CUSTOMDATA_TYFLOW_VAT_FRAME;
            const int dataIndex = 2;
            (string, string) nameTuple = ("TyflowVATFrameCustomData", "");

            EditorGUI.showMixedValue = CustomDataHasMixedValue(dataBitPos, dataIndex);
            W9ParticleShaderFlags.CutomDataComponent component = GetCurrentCustomDataComponent(dataBitPos, dataIndex);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginHorizontal();
            component = (W9ParticleShaderFlags.CutomDataComponent)EditorGUILayout.Popup(new GUIContent("Frame CustomData"), (int)component, CustomDataOptions);
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

        private bool IsTyflowSkinModeSelected()
        {
            MaterialProperty modeProperty = _helper.GetProperty("_Mode");
            return modeProperty != null &&
                   !modeProperty.hasMixedValue &&
                   modeProperty.floatValue >= TyflowSkinModeStart;
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
            if (material.GetFloat("_VAT_Toggle") <= 0.5f ||
                Mathf.RoundToInt(material.GetFloat("_VATMode")) != (int)ParticleBaseGUI.VATMode.Tyflow)
            {
                return false;
            }

            int meshSourceMode = Mathf.RoundToInt(material.GetFloat("_MeshSourceMode"));
            if (meshSourceMode != MeshSourceModeParticle &&
                meshSourceMode != MeshSourceModeUIParticle)
            {
                return false;
            }

            return material.GetFloat("_Mode") <= TyflowParticleHighestNonSkinMode;
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
