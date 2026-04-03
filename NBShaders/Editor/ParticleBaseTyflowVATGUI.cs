using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using NBShader;

namespace NBShaderEditor
{
    internal sealed class ParticleBaseTyflowVATGUI
    {
        private const int HoudiniMode = 0;
        private const int TyflowMode = 1;
        private const int TyflowSkinModeStart = 2;

        private readonly ShaderGUIHelper _helper;
        private readonly List<Material> _mats;
        private readonly Func<int, int> _getAnimBoolIndex;
        private readonly Action<Material> _syncVatKeywords;

        private static readonly string[] VatModeNames =
        {
            "Houdini",
            "Tyflow"
        };

        private static readonly string[] TyflowAnimationModeNames =
        {
            "Absolute positions",
            "Relative offsets",
            "Skin (R)",
            "Skin (PR)",
            "Skin (PRSAVE)",
            "Skin (PRSXYZ)"
        };

        public ParticleBaseTyflowVATGUI(
            ShaderGUIHelper helper,
            List<Material> mats,
            Func<int, int> getAnimBoolIndex,
            Action<Material> syncVatKeywords)
        {
            _helper = helper;
            _mats = mats;
            _getAnimBoolIndex = getAnimBoolIndex;
            _syncVatKeywords = syncVatKeywords;
        }

        public void Draw()
        {
            _helper.DrawToggleFoldOut(
                W9ParticleShaderFlags.foldOutBit2VAT,
                5,
                _getAnimBoolIndex(5),
                "VAT\u9876\u70B9\u52A8\u753B\u56FE",
                "_VAT_Toggle",
                shaderKeyword: "_VAT",
                fontStyle: FontStyle.Bold,
                drawBlock: isToggle =>
                {
                    DrawVatMode();

                    if (IsTyflowSelected())
                    {
                        DrawTyflowSettings();
                    }
                },
                drawEndChangeCheck: isToggle =>
                {
                    if (isToggle.hasMixedValue)
                    {
                        return;
                    }

                    foreach (Material mat in _mats)
                    {
                        if (isToggle.floatValue > 0.5f)
                        {
                            float vatMode = mat.GetFloat("_VATMode");
                            if (vatMode < HoudiniMode || vatMode > TyflowMode)
                            {
                                mat.SetFloat("_VATMode", HoudiniMode);
                            }
                        }

                        _syncVatKeywords(mat);
                    }
                });
        }

        public static void AppendRequiredVertexStreams(
            Material material,
            List<ParticleSystemVertexStream> streams,
            List<string> streamList)
        {
            if (material.GetFloat("_VAT_Toggle") <= 0.5f ||
                Mathf.RoundToInt(material.GetFloat("_VATMode")) != TyflowMode)
            {
                return;
            }

            AddStream(streams, streamList, ParticleSystemVertexStream.Custom1XYZW, ParticleBaseGUI.streamCustom1Text);
        }

        private void DrawVatMode()
        {
            _helper.DrawPopUp(
                "VAT\u6A21\u5F0F",
                "_VATMode",
                VatModeNames,
                drawOnValueChangedBlock: modeProp =>
                {
                    if (modeProp.hasMixedValue)
                    {
                        return;
                    }

                    foreach (Material mat in _mats)
                    {
                        mat.SetFloat("_VATMode", modeProp.floatValue);
                        _syncVatKeywords(mat);
                    }
                });
        }

        private bool IsTyflowSelected()
        {
            MaterialProperty vatModeProperty = _helper.GetProperty("_VATMode");
            return vatModeProperty != null &&
                   !vatModeProperty.hasMixedValue &&
                   Mathf.RoundToInt(vatModeProperty.floatValue) == TyflowMode;
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
            _helper.DrawToggle("Deforming skin", "_DeformingSkin");
            _helper.DrawFloat("Skin bone count", "_SkinBoneCount");
            _helper.DrawToggle("RGBA encoded", "_RGBAEncoded");
            _helper.DrawToggle("RGBA half", "_RGBAHalf");
            _helper.DrawToggle("Gamma correction", "_LinearToGamma");
            _helper.DrawToggle("VAT includes normals", "_VATIncludesNormals");
            _helper.DrawToggle("Affects shadows", "_AffectsShadows");
            _helper.DrawFloat("Frame", "_Frame");
            _helper.DrawFloat("Frames", "_Frames");
            _helper.DrawToggle("Frame interpolation", "_FrameInterpolation");
            _helper.DrawToggle("Loop", "_Loop");
            _helper.DrawToggle("Interpolate loop", "_InterpolateLoop");
            _helper.DrawToggle("Autoplay", "_Autoplay");
            _helper.DrawFloat("AutoplaySpeed", "_AutoplaySpeed");
        }

        private bool IsTyflowSkinModeSelected()
        {
            MaterialProperty modeProperty = _helper.GetProperty("_Mode");
            return modeProperty != null &&
                   !modeProperty.hasMixedValue &&
                   modeProperty.floatValue >= TyflowSkinModeStart;
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
