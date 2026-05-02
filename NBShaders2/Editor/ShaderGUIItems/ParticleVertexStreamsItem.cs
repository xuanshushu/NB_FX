using System.Collections.Generic;
using System.Linq;
using NBShader;
using UnityEditor;
using UnityEngine;

namespace NBShaderEditor
{
    public class ParticleVertexStreamsItem : ShaderGUIItem
    {
        private readonly NBShaderRootItem _nbRootItem;

        public ParticleVertexStreamsItem(NBShaderRootItem rootItem, ShaderGUIItem parentItem) : base(rootItem, parentItem)
        {
            _nbRootItem = rootItem;
        }

        public override void OnGUI()
        {
            if (_nbRootItem.Mats == null ||
                _nbRootItem.Mats.Count != 1)
            {
                return;
            }

            Material material = _nbRootItem.Mats[0];
            if (material == null)
            {
                return;
            }

            List<ParticleSystemRenderer> renderers = FindParticleRenderers(material);
            if (renderers.Count == 0)
            {
                return;
            }

            W9ParticleShaderFlags flags = _nbRootItem.ShaderFlags.Count > 0
                ? _nbRootItem.ShaderFlags[0] as W9ParticleShaderFlags
                : null;
            flags?.ClearFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_ANIMATION_SHEET_HELPER, index: 1);

            if (_nbRootItem.Context.ParticleMode != MixedBool.True)
            {
                return;
            }

            if (flags == null)
            {
                return;
            }

            BuildExpectedStreams(material, flags, out List<ParticleSystemVertexStream> streams, out List<string> streamNames);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(
                NBShaderInspectorLocalization.MakeInspectorContent("vertexStreams.title", "Particle Vertex Streams"),
                EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                for (int i = 0; i < streamNames.Count; i++)
                {
                    EditorGUILayout.TextField(streamNames[i]);
                }
            }

            DrawRendererWarnings(renderers, streams, false);

#if UNITY_2022_3_OR_NEWER && !(UNITY_2022_3_0 || UNITY_2022_3_1 || UNITY_2022_3_2 || UNITY_2022_3_3 || UNITY_2022_3_4 || UNITY_2022_3_5 || UNITY_2022_3_6 || UNITY_2022_3_7 || UNITY_2022_3_8 || UNITY_2022_3_9 || UNITY_2022_3_10)
            DrawRendererWarnings(renderers, streams, true);
#endif
        }

        private static List<ParticleSystemRenderer> FindParticleRenderers(Material material)
        {
            Renderer[] renderers = Object.FindObjectsOfType(typeof(Renderer)) as Renderer[];
            List<ParticleSystemRenderer> result = new List<ParticleSystemRenderer>();
            if (renderers == null)
            {
                return result;
            }

            foreach (Renderer renderer in renderers)
            {
                if (renderer is ParticleSystemRenderer psr &&
                    (psr.sharedMaterial == material || psr.trailMaterial == material))
                {
                    result.Add(psr);
                }
            }

            return result;
        }

        private static void BuildExpectedStreams(
            Material material,
            W9ParticleShaderFlags flags,
            out List<ParticleSystemVertexStream> streams,
            out List<string> streamNames)
        {
            streams = new List<ParticleSystemVertexStream>();
            streamNames = new List<string>();

            AddStream(streams, streamNames, ParticleSystemVertexStream.Position, "POSITION.xyz");

            bool useFlipbookBlending = material.IsKeywordEnabled("_FLIPBOOKBLENDING_ON") ||
                                       GetFloat(material, "_FlipbookBlending") > 0.5f;
            bool useSpecialUVChannel = flags.CheckIsUVModeOn(W9ParticleShaderFlags.UVMode.SpecialUVChannel);
            bool useUV3ForSpecialUV = flags.CheckFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_USE_TEXCOORD2, index: 1);
            bool customData1 = flags.CheckFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_CUSTOMDATA1_ON);
            bool customData2 = flags.CheckFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_CUSTOMDATA2_ON);

            bool needNormal = GetFloat(material, "_VertexOffset_NormalDir_Toggle") > 0.5f ||
                              GetFloat(material, "_fresnelEnabled") > 0.5f ||
                              GetFloat(material, "_ParallaxMapping_Toggle") > 0.5f ||
                              GetFloat(material, "_FxLightMode") > (float)FxLightMode.UnLit ||
                              GetFloat(material, "_BumpMapToggle") > 0.5f;
            bool needTangent = needNormal;

            if (needTangent)
            {
                AddStream(streams, streamNames, ParticleSystemVertexStream.Tangent, "TANGENT.xyzw");
            }

            if (needNormal)
            {
                AddStream(streams, streamNames, ParticleSystemVertexStream.Normal, "NORMAL.xyz");
            }

            AddStream(streams, streamNames, ParticleSystemVertexStream.Color, "COLOR.xyzw");
            AddStream(streams, streamNames, ParticleSystemVertexStream.UV, "TEXCOORD0.xy");

            if (useFlipbookBlending && useSpecialUVChannel)
            {
                AddStream(streams, streamNames, ParticleSystemVertexStream.UV2, "TEXCOORD0.zw");
            }
            else if (useSpecialUVChannel)
            {
                AddStream(streams, streamNames, useUV3ForSpecialUV ? ParticleSystemVertexStream.UV3 : ParticleSystemVertexStream.UV2, useUV3ForSpecialUV ? "TEXCOORD3.xy" : "TEXCOORD0.zw");
            }
            else if (useFlipbookBlending || customData1 || customData2)
            {
                AddStream(streams, streamNames, ParticleSystemVertexStream.UV2, "TEXCOORD0.zw");
            }

            bool fillSkipUV2 = false;
            if (customData1 || customData2 || useFlipbookBlending)
            {
                AddStream(streams, streamNames, ParticleSystemVertexStream.Custom1XYZW, "TEXCOORD1.xyzw");
            }
            else if (useSpecialUVChannel && useUV3ForSpecialUV)
            {
                AddStream(streams, streamNames, ParticleSystemVertexStream.UV2, "TEXCOORD1.xy");
                fillSkipUV2 = true;
            }

            if (customData2 || useFlipbookBlending)
            {
                AddStream(streams, streamNames, ParticleSystemVertexStream.Custom2XYZW, "TEXCOORD2.xyzw");
            }
            else if (useSpecialUVChannel && useUV3ForSpecialUV && !fillSkipUV2)
            {
                AddStream(streams, streamNames, ParticleSystemVertexStream.UV2, "TEXCOORD2.xy");
                fillSkipUV2 = true;
            }

            if (useFlipbookBlending)
            {
                AddStream(streams, streamNames, ParticleSystemVertexStream.AnimBlend, "TEXCOORD3.x");
                if (useSpecialUVChannel && useUV3ForSpecialUV)
                {
                    AddStream(streams, streamNames, ParticleSystemVertexStream.UV3, "TEXCOORD3.yz");
                }
            }
            else if (useSpecialUVChannel && useUV3ForSpecialUV && !fillSkipUV2)
            {
                AddStream(streams, streamNames, ParticleSystemVertexStream.UV2, "TEXCOORD3.xy");
            }

            AppendVatRequiredStreams(material, streams, streamNames);
        }

        private static void AppendVatRequiredStreams(Material material, List<ParticleSystemVertexStream> streams, List<string> streamNames)
        {
            if (GetFloat(material, "_VAT_Toggle") <= 0.5f)
            {
                return;
            }

            AddStreamUnique(streams, streamNames, ParticleSystemVertexStream.Position, "VAT POSITION.xyz");
            AddStreamUnique(streams, streamNames, ParticleSystemVertexStream.Normal, "VAT NORMAL.xyz");

            int vatMode = Mathf.RoundToInt(GetFloat(material, "_VATMode"));
            if (vatMode == (int)VATMode.Tyflow)
            {
                if (IsParticleMode(material) && Mathf.RoundToInt(GetFloat(material, "_TyFlowVATSubMode")) <= 1)
                {
                    AddStreamUnique(streams, streamNames, ParticleSystemVertexStream.UV2, "VAT TEXCOORD0.zw");
                }

                return;
            }

            int houdiniSubMode = Mathf.RoundToInt(GetFloat(material, "_HoudiniVATSubMode"));
            bool isParticleMode = IsParticleMode(material);
            switch (houdiniSubMode)
            {
                case 0:
                    AddHoudiniVatUV1Stream(streams, streamNames, isParticleMode);
                    break;
                case 1:
                    if (isParticleMode)
                    {
                        break;
                    }

                    AddHoudiniVatUV1Stream(streams, streamNames, isParticleMode);
                    AddStreamUnique(streams, streamNames, ParticleSystemVertexStream.Custom2XYZW, "VAT TEXCOORD2.xyzw");
                    AddStreamUnique(streams, streamNames, ParticleSystemVertexStream.UV, "VAT TEXCOORD0.xy");
                    break;
                case 2:
                    AddStreamUnique(streams, streamNames, ParticleSystemVertexStream.UV, "VAT TEXCOORD0.xy");
                    break;
                case 3:
                    AddStreamUnique(streams, streamNames, ParticleSystemVertexStream.UV, "VAT TEXCOORD0.xy");
                    AddHoudiniVatUV1Stream(streams, streamNames, isParticleMode);
                    break;
            }
        }

        private static void AddHoudiniVatUV1Stream(List<ParticleSystemVertexStream> streams, List<string> streamNames, bool isParticleMode)
        {
            AddStreamUnique(
                streams,
                streamNames,
                isParticleMode ? ParticleSystemVertexStream.UV2 : ParticleSystemVertexStream.Custom1XYZW,
                isParticleMode ? "VAT TEXCOORD0.zw" : "VAT TEXCOORD1.xyzw");
        }

        private static void AddStream(List<ParticleSystemVertexStream> streams, List<string> streamNames, ParticleSystemVertexStream stream, string label)
        {
            streams.Add(stream);
            streamNames.Add(label);
        }

        private static void AddStreamUnique(List<ParticleSystemVertexStream> streams, List<string> streamNames, ParticleSystemVertexStream stream, string label)
        {
            if (streams.Contains(stream))
            {
                return;
            }

            streams.Add(stream);
            streamNames.Add(label);
        }

        private static float GetFloat(Material material, string propertyName)
        {
            return material != null && material.HasProperty(propertyName) ? material.GetFloat(propertyName) : 0f;
        }

        private static bool IsParticleMode(Material material)
        {
            if (material == null || !material.HasProperty("_MeshSourceMode"))
            {
                return false;
            }

            MeshSourceMode meshSourceMode = (MeshSourceMode)Mathf.RoundToInt(material.GetFloat("_MeshSourceMode"));
            return meshSourceMode == MeshSourceMode.Particle || meshSourceMode == MeshSourceMode.UIParticle;
        }

        private static void DrawRendererWarnings(List<ParticleSystemRenderer> renderers, List<ParticleSystemVertexStream> streams, bool trail)
        {
            List<string> warnings = new List<string>();
            List<ParticleSystemVertexStream> rendererStreams = new List<ParticleSystemVertexStream>();
            foreach (ParticleSystemRenderer renderer in renderers)
            {
                rendererStreams.Clear();
                if (trail)
                {
#if UNITY_2022_3_OR_NEWER && !(UNITY_2022_3_0 || UNITY_2022_3_1 || UNITY_2022_3_2 || UNITY_2022_3_3 || UNITY_2022_3_4 || UNITY_2022_3_5 || UNITY_2022_3_6 || UNITY_2022_3_7 || UNITY_2022_3_8 || UNITY_2022_3_9 || UNITY_2022_3_10)
                    renderer.GetActiveTrailVertexStreams(rendererStreams);
#endif
                }
                else
                {
                    renderer.GetActiveVertexStreams(rendererStreams);
                }

                if (!rendererStreams.SequenceEqual(streams))
                {
                    warnings.Add(renderer.name);
                }
            }

            if (warnings.Count == 0)
            {
                return;
            }

            string mismatchText = trail
                ? NBShaderInspectorLocalization.GetInspectorText("vertexStreams.trailMismatch", "Particle trail renderers with mismatched vertex streams:")
                : NBShaderInspectorLocalization.GetInspectorText("vertexStreams.mismatch", "Particle renderers with mismatched vertex streams:");
            EditorGUILayout.HelpBox(mismatchText + "\n-" + string.Join("\n-", warnings), MessageType.Error, true);
            GUIContent buttonContent = trail
                ? NBShaderInspectorLocalization.MakeContent("inspector.vertexStreams.applyTrail.button", "Apply Trail Vertex Streams")
                : NBShaderInspectorLocalization.MakeContent("inspector.vertexStreams.apply.button", "Apply Vertex Streams");
            if (GUILayout.Button(buttonContent, EditorStyles.miniButton))
            {
                Undo.RecordObjects(
                    renderers.Where(r => r != null).ToArray(),
                    NBShaderInspectorLocalization.GetInspectorText("vertexStreams.apply.undo", "Apply custom vertex streams from material"));
                foreach (ParticleSystemRenderer renderer in renderers)
                {
                    if (trail)
                    {
#if UNITY_2022_3_OR_NEWER && !(UNITY_2022_3_0 || UNITY_2022_3_1 || UNITY_2022_3_2 || UNITY_2022_3_3 || UNITY_2022_3_4 || UNITY_2022_3_5 || UNITY_2022_3_6 || UNITY_2022_3_7 || UNITY_2022_3_8 || UNITY_2022_3_9 || UNITY_2022_3_10)
                        renderer.SetActiveTrailVertexStreams(streams);
#endif
                    }
                    else
                    {
                        renderer.SetActiveVertexStreams(streams);
                    }
                }
            }
        }
    }
}
