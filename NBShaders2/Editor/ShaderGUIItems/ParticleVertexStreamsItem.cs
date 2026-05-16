using System;
using System.Collections.Generic;
using NBShader;
using UnityEditor;
using UnityEngine;

namespace NBShaderEditor
{
    public class ParticleVertexStreamsItem : ShaderGUIItem
    {
        private readonly NBShaderRootItem _nbRootItem;
        private readonly List<ParticleSystemVertexStream> _streams = new List<ParticleSystemVertexStream>();
        private readonly List<string> _streamNames = new List<string>();
        private readonly List<string> _warnings = new List<string>();
        private readonly List<ParticleSystemVertexStream> _rendererStreams = new List<ParticleSystemVertexStream>();
        private readonly List<UnityEngine.Object> _undoRenderers = new List<UnityEngine.Object>();
        private WarningMessageCache _rendererWarningMessageCache;
        private WarningMessageCache _trailWarningMessageCache;

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

            List<ParticleSystemRenderer> renderers = _nbRootItem.ParticleRenderersUsingThisMaterial;
            if (renderers == null || renderers.Count == 0)
            {
                return;
            }

            NBShaderFlags flags = _nbRootItem.ShaderFlags.Count > 0
                ? _nbRootItem.ShaderFlags[0] as NBShaderFlags
                : null;

            if (_nbRootItem.Context.ParticleMode != MixedBool.True)
            {
                return;
            }

            if (flags == null)
            {
                return;
            }

            if (flags.CheckFlagBits(NBShaderFlags.FLAG_BIT_PARTICLE_1_ANIMATION_SHEET_HELPER, index: 1))
            {
                flags.ClearFlagBits(NBShaderFlags.FLAG_BIT_PARTICLE_1_ANIMATION_SHEET_HELPER, index: 1);
            }

            BuildExpectedStreams(material, flags, _streams, _streamNames);

            LayoutSpace();
            EditorGUI.LabelField(
                ApplyGlobalRectCompensation(LayoutRect()),
                NBShaderInspectorLocalization.MakeInspectorContent("vertexStreams.title", "Particle Vertex Streams"),
                EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                for (int i = 0; i < _streamNames.Count; i++)
                {
                    EditorGUI.TextField(ApplyGlobalRectCompensation(LayoutRect()), _streamNames[i]);
                }
            }

            DrawRendererWarnings(renderers, _streams, false);

#if UNITY_2022_3_OR_NEWER && !(UNITY_2022_3_0 || UNITY_2022_3_1 || UNITY_2022_3_2 || UNITY_2022_3_3 || UNITY_2022_3_4 || UNITY_2022_3_5 || UNITY_2022_3_6 || UNITY_2022_3_7 || UNITY_2022_3_8 || UNITY_2022_3_9 || UNITY_2022_3_10)
            DrawRendererWarnings(renderers, _streams, true);
#endif
        }

        private static void BuildExpectedStreams(
            Material material,
            NBShaderFlags flags,
            List<ParticleSystemVertexStream> streams,
            List<string> streamNames)
        {
            streams.Clear();
            streamNames.Clear();

            AddStream(streams, streamNames, ParticleSystemVertexStream.Position, "POSITION.xyz");

            bool useFlipbookBlending = material.IsKeywordEnabled("_FLIPBOOKBLENDING_ON") ||
                                       GetFloat(material, "_FlipbookBlending") > 0.5f;
            bool useSpecialUVChannel = flags.CheckIsUVModeOn(NBShaderFlags.UVMode.SpecialUVChannel);
            bool useUV3ForSpecialUV = flags.CheckFlagBits(NBShaderFlags.FLAG_BIT_PARTICLE_1_USE_TEXCOORD2, index: 1);
            bool customData1 = flags.CheckFlagBits(NBShaderFlags.FLAG_BIT_PARTICLE_CUSTOMDATA1_ON);
            bool customData2 = flags.CheckFlagBits(NBShaderFlags.FLAG_BIT_PARTICLE_CUSTOMDATA2_ON);

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

        private void DrawRendererWarnings(List<ParticleSystemRenderer> renderers, List<ParticleSystemVertexStream> streams, bool trail)
        {
            _warnings.Clear();
            foreach (ParticleSystemRenderer renderer in renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                _rendererStreams.Clear();
                if (trail)
                {
#if UNITY_2022_3_OR_NEWER && !(UNITY_2022_3_0 || UNITY_2022_3_1 || UNITY_2022_3_2 || UNITY_2022_3_3 || UNITY_2022_3_4 || UNITY_2022_3_5 || UNITY_2022_3_6 || UNITY_2022_3_7 || UNITY_2022_3_8 || UNITY_2022_3_9 || UNITY_2022_3_10)
                    renderer.GetActiveTrailVertexStreams(_rendererStreams);
#endif
                }
                else
                {
                    renderer.GetActiveVertexStreams(_rendererStreams);
                }

                if (!StreamsEqual(_rendererStreams, streams))
                {
                    _warnings.Add(renderer.name);
                }
            }

            if (_warnings.Count == 0)
            {
                return;
            }

            string mismatchText = trail
                ? NBShaderInspectorLocalization.GetInspectorText("vertexStreams.trailMismatch", "Particle trail renderers with mismatched vertex streams:")
                : NBShaderInspectorLocalization.GetInspectorText("vertexStreams.mismatch", "Particle renderers with mismatched vertex streams:");
            DrawLayoutHelpBox(GetWarningMessage(trail, mismatchText), MessageType.Error);
            GUIContent buttonContent = trail
                ? NBShaderInspectorLocalization.MakeContent("inspector.vertexStreams.applyTrail.button", "Apply Trail Vertex Streams")
                : NBShaderInspectorLocalization.MakeContent("inspector.vertexStreams.apply.button", "Apply Vertex Streams");
            if (GUI.Button(ApplyGlobalRectCompensation(LayoutRect()), buttonContent, EditorStyles.miniButton))
            {
                _undoRenderers.Clear();
                for (int i = 0; i < renderers.Count; i++)
                {
                    if (renderers[i] != null)
                    {
                        _undoRenderers.Add(renderers[i]);
                    }
                }

                Undo.RecordObjects(
                    _undoRenderers.ToArray(),
                    NBShaderInspectorLocalization.GetInspectorText("vertexStreams.apply.undo", "Apply custom vertex streams from material"));
                foreach (ParticleSystemRenderer renderer in renderers)
                {
                    if (renderer == null)
                    {
                        continue;
                    }

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

        private static bool StreamsEqual(List<ParticleSystemVertexStream> lhs, List<ParticleSystemVertexStream> rhs)
        {
            if (lhs == null || rhs == null || lhs.Count != rhs.Count)
            {
                return false;
            }

            for (int i = 0; i < lhs.Count; i++)
            {
                if (lhs[i] != rhs[i])
                {
                    return false;
                }
            }

            return true;
        }

        private string GetWarningMessage(bool trail, string mismatchText)
        {
            return trail
                ? GetWarningMessage(ref _trailWarningMessageCache, mismatchText)
                : GetWarningMessage(ref _rendererWarningMessageCache, mismatchText);
        }

        private string GetWarningMessage(ref WarningMessageCache cache, string mismatchText)
        {
            int hash = ComputeWarningsHash();
            if (cache.Message != null &&
                cache.Count == _warnings.Count &&
                cache.Hash == hash)
            {
                return cache.Message;
            }

            cache.Count = _warnings.Count;
            cache.Hash = hash;
            cache.Message = mismatchText + "\n-" + string.Join("\n-", _warnings);
            return cache.Message;
        }

        private int ComputeWarningsHash()
        {
            unchecked
            {
                int hash = 17;
                for (int i = 0; i < _warnings.Count; i++)
                {
                    hash = hash * 31 + (_warnings[i] == null ? 0 : StringComparer.Ordinal.GetHashCode(_warnings[i]));
                }

                return hash;
            }
        }

        private struct WarningMessageCache
        {
            public int Count;
            public int Hash;
            public string Message;
        }
    }
}
