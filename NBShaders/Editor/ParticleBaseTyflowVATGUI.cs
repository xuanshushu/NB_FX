using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using NBShader;

namespace NBShaderEditor
{
    internal sealed class ParticleBaseTyflowVATGUI
    {
        private const int TyflowSkinModeStart = 2;

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
            if (material.GetFloat("_VAT_Toggle") <= 0.5f ||
                Mathf.RoundToInt(material.GetFloat("_VATMode")) != (int)ParticleBaseGUI.VATMode.Tyflow)
            {
                return;
            }

            AddStream(streams, streamList, ParticleSystemVertexStream.Custom1XYZW, ParticleBaseGUI.streamCustom1Text);
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
