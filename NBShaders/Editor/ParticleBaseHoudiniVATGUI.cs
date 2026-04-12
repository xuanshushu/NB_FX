using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using NBShader;

namespace NBShaderEditor
{
    internal sealed class ParticleBaseHoudiniVATGUI
    {
        private readonly ShaderGUIHelper _helper;

        private static readonly string[] SubModeNames =
        {
            "SoftBody (Deformation)",
            "RigidBody (Pieces)",
            "Dynamic Remeshing (Lookup)",
            "Particle Sprites (Billboard)"
        };

        private static readonly string[] SubModeKeywords =
        {
            "_HOUDINI_VAT_SOFTBODY",
            "_HOUDINI_VAT_RIGIDBODY",
            "_HOUDINI_VAT_DYNAMIC_REMESH",
            "_HOUDINI_VAT_PARTICLE_SPRITE"
        };

        public ParticleBaseHoudiniVATGUI(ShaderGUIHelper helper)
        {
            _helper = helper;
        }

        public void Draw()
        {
            DrawSubModeSelector();
            DrawPlaybackSection();
            DrawBoundsSection();
            DrawTextureSection();
            DrawScaleSection();
            DrawParticleSpriteSection();
            DrawFlagsSection();
            SyncKeywords();
        }

        public static void AppendRequiredVertexStreams(
            Material material,
            List<ParticleSystemVertexStream> streams,
            List<string> streamList)
        {
            if (material.GetFloat("_VAT_Toggle") <= 0.5f)
            {
                return;
            }

            int subMode = GetMaterialSubMode(material);
            bool isParticleMode = IsHoudiniParticleModeEnabled(material);

            AddStream(streams, streamList, ParticleSystemVertexStream.Position, ParticleBaseGUI.streamPositionText);
            AddStream(streams, streamList, ParticleSystemVertexStream.Normal, ParticleBaseGUI.streamNormalText);

            switch (subMode)
            {
                case 0: // SoftBody needs VAT UV1.
                    AddVatUV1Stream(streams, streamList, isParticleMode);
                    break;

                case 1: // RigidBody needs VAT UV1, UV2, UV3 (Custom2, vatTexcoord5)
                    if (isParticleMode)
                    {
                        break;
                    }

                    AddVatUV1Stream(streams, streamList, isParticleMode);
                    AddStream(streams, streamList, ParticleSystemVertexStream.Custom2XYZW, ParticleBaseGUI.streamCustom2Text);
                    AddStream(streams, streamList, ParticleSystemVertexStream.UV, ParticleBaseGUI.streamUVText);
                    break;

                case 2: // DynamicRemeshing needs UV0 (texcoords)
                    AddStream(streams, streamList, ParticleSystemVertexStream.UV, ParticleBaseGUI.streamUVText);
                    break;

                case 3: // ParticleSprite needs UV0 (corner) + VAT UV1 (particle U/V)
                    AddStream(streams, streamList, ParticleSystemVertexStream.UV, ParticleBaseGUI.streamUVText);
                    AddVatUV1Stream(streams, streamList, isParticleMode);
                    break;
            }
        }

        private List<Material> mats => _helper.mats;

        private bool IsResetToolInitializing()
        {
            return _helper.ResetTool != null && _helper.ResetTool.IsInitResetData;
        }

        private bool TryGetCurrentSubMode(out int subMode)
        {
            MaterialProperty property = _helper.GetProperty("_HoudiniVATSubMode");
            if (property == null || property.hasMixedValue)
            {
                subMode = -1;
                return false;
            }

            subMode = Mathf.Clamp(Mathf.RoundToInt(property.floatValue), 0, SubModeNames.Length - 1);
            return true;
        }

        private bool IsSubMode(params int[] subModes)
        {
            if (IsResetToolInitializing())
            {
                return true;
            }

            if (!TryGetCurrentSubMode(out int currentSubMode))
            {
                return false;
            }

            for (int i = 0; i < subModes.Length; i++)
            {
                if (currentSubMode == subModes[i])
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryGetFloatProperty(string propertyName, out MaterialProperty property)
        {
            property = _helper.GetProperty(propertyName);
            return property != null && !property.hasMixedValue;
        }

        private bool IsFloatPropertyOn(string propertyName)
        {
            return TryGetFloatProperty(propertyName, out MaterialProperty property) && property.floatValue > 0.5f;
        }

        private bool IsFloatPropertyOff(string propertyName)
        {
            return TryGetFloatProperty(propertyName, out MaterialProperty property) && property.floatValue <= 0.5f;
        }

        private bool IsFloatPropertyMixed(string propertyName)
        {
            MaterialProperty property = _helper.GetProperty(propertyName);
            return property != null && property.hasMixedValue;
        }

        private bool ShouldDrawWhenFloatOn(string propertyName)
        {
            return IsResetToolInitializing() || IsFloatPropertyMixed(propertyName) || IsFloatPropertyOn(propertyName);
        }

        private bool ShouldDrawWhenFloatOff(string propertyName)
        {
            return IsResetToolInitializing() || IsFloatPropertyMixed(propertyName) || IsFloatPropertyOff(propertyName);
        }

        private static int GetMaterialSubMode(Material material)
        {
            if (!material.HasProperty("_HoudiniVATSubMode"))
            {
                return 0;
            }

            return Mathf.Clamp(Mathf.RoundToInt(material.GetFloat("_HoudiniVATSubMode")), 0, SubModeNames.Length - 1);
        }

        private static bool IsHoudiniParticleModeEnabled(Material material)
        {
            if (material == null ||
                !material.HasProperty("_VAT_Toggle") ||
                !material.HasProperty("_VATMode") ||
                !material.HasProperty("_MeshSourceMode") ||
                material.GetFloat("_VAT_Toggle") <= 0.5f ||
                Mathf.RoundToInt(material.GetFloat("_VATMode")) != (int)ParticleBaseGUI.VATMode.Houdini)
            {
                return false;
            }

            ParticleBaseGUI.MeshSourceMode meshSourceMode = (ParticleBaseGUI.MeshSourceMode)Mathf.RoundToInt(material.GetFloat("_MeshSourceMode"));
            return meshSourceMode == ParticleBaseGUI.MeshSourceMode.Particle ||
                   meshSourceMode == ParticleBaseGUI.MeshSourceMode.UIParticle;
        }

        private void DrawSubModeSelector()
        {
            _helper.DrawPopUp("Houdini VAT Sub Mode", "_HoudiniVATSubMode", SubModeNames);
            if (HasUnsupportedParticleMode())
            {
                EditorGUILayout.HelpBox("该 Houdini VAT 类型需要 Mesh 多 UV 数据，不支持 ParticleSystem VertexStream 模式。", MessageType.Warning);
            }
        }

        private void DrawPlaybackSection()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Playback", EditorStyles.boldLabel);

            _helper.DrawToggle("Auto Playback", "_B_autoPlayback");

            if (ShouldDrawWhenFloatOff("_B_autoPlayback"))
            {
                _helper.DrawFloat("Display Frame", "_displayFrame");
            }

            DrawFrameCustomDataSelect();

            _helper.DrawFloat("Game Time at First Frame", "_gameTimeAtFirstFrame");
            _helper.DrawFloat("Playback Speed", "_playbackSpeed");
            _helper.DrawFloat("Houdini FPS", "_houdiniFPS");
            _helper.DrawToggle("Interframe Interpolation", "_B_interpolate");

            if (IsSubMode(1)) // RigidBody
            {
                _helper.DrawToggle("Animate First Frame", "_animateFirstFrame");
            }
        }

        private void DrawBoundsSection()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Bounds Metadata", EditorStyles.boldLabel);

            _helper.DrawFloat("Bound Min X", "_boundMinX");
            _helper.DrawFloat("Bound Min Y", "_boundMinY");
            _helper.DrawFloat("Bound Min Z", "_boundMinZ");
            _helper.DrawFloat("Bound Max X", "_boundMaxX");
            _helper.DrawFloat("Bound Max Y", "_boundMaxY");
            _helper.DrawFloat("Bound Max Z", "_boundMaxZ");
        }

        private void DrawFrameCustomDataSelect()
        {
            const int dataBitPos = W9ParticleShaderFlags.FLAGBIT_POS_2_CUSTOMDATA_VAT_FRAME;
            const int dataIndex = 2;
            W9ParticleShaderFlags.CutomDataComponent component = GetCurrentCustomDataComponent(dataBitPos, dataIndex);
            if (!IsAnyHoudiniParticleModeEnabled() || _helper.shaderFlags == null || _helper.shaderFlags.Length == 0)
            {
                ClearFrameCustomData(dataBitPos, dataIndex);
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

        private void ClearFrameCustomData(int dataBitPos, int dataIndex)
        {
            if (_helper.shaderFlags == null)
            {
                return;
            }

            for (int i = 0; i < _helper.shaderFlags.Length; i++)
            {
                if (_helper.shaderFlags[i] is W9ParticleShaderFlags flags)
                {
                    flags.SetCustomDataFlag(W9ParticleShaderFlags.CutomDataComponent.Off, dataBitPos, dataIndex);
                }
            }
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

        private bool IsAnyHoudiniParticleModeEnabled()
        {
            if (_helper.mats == null)
            {
                return false;
            }

            for (int i = 0; i < _helper.mats.Count; i++)
            {
                if (IsHoudiniParticleModeEnabled(_helper.mats[i]))
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

        private static bool IsUnsupportedParticleMode(Material material)
        {
            return IsHoudiniParticleModeEnabled(material) && GetMaterialSubMode(material) == 1;
        }

        private void DrawTextureSection()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Textures", EditorStyles.boldLabel);

            _helper.DrawTexture("Position Texture", "_posTexture", drawScaleOffset: false, drawWrapMode: false);

            if (ShouldDrawWhenFloatOn("_B_LOAD_POS_TWO_TEX"))
            {
                _helper.DrawTexture("Position Texture 2", "_posTexture2", drawScaleOffset: false, drawWrapMode: false);
            }

            if (ShouldDrawRotationTexture())
            {
                _helper.DrawTexture("Rotation Texture", "_rotTexture", drawScaleOffset: false, drawWrapMode: false);
            }

            if (ShouldDrawWhenFloatOn("_B_LOAD_COL_TEX"))
            {
                _helper.DrawTexture("Color Texture", "_colTexture", drawScaleOffset: false, drawWrapMode: false);
            }

            if (IsSubMode(2) || ShouldDrawWhenFloatOn("_B_LOAD_LOOKUP_TABLE"))
            {
                _helper.DrawTexture("Lookup Table", "_lookupTable", drawScaleOffset: false, drawWrapMode: false);
            }
        }

        private void DrawScaleSection()
        {
            if (IsSubMode(1, 3))
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Scale", EditorStyles.boldLabel);

                _helper.DrawFloat("Global Piece Scale Multiplier", "_globalPscaleMul");
                _helper.DrawToggle("Piece Scales in Position Alpha", "_B_pscaleAreInPosA");
            }
        }

        private void DrawParticleSpriteSection()
        {
            if (!IsSubMode(3))
            {
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Particle Sprite", EditorStyles.boldLabel);

            _helper.DrawFloat("Width Base Scale", "_widthBaseScale");
            _helper.DrawFloat("Height Base Scale", "_heightBaseScale");

            _helper.DrawToggle("Hide Overlapping Origin", "_B_hideOverlappingOrigin");
            if (ShouldDrawWhenFloatOn("_B_hideOverlappingOrigin"))
            {
                _helper.DrawFloat("Origin Effective Radius", "_originRadius");
            }

            _helper.DrawToggle("Particles Can Spin", "_B_CAN_SPIN");

            if (ShouldDrawWhenFloatOn("_B_CAN_SPIN"))
            {
                _helper.DrawToggle("Compute Spin from Heading", "_B_spinFromHeading");

                if (ShouldDrawWhenFloatOff("_B_spinFromHeading"))
                {
                    _helper.DrawFloat("Particle Spin Phase", "_spinPhase");
                }

                _helper.DrawFloat("Scale by Velocity Amount", "_scaleByVelAmount");
            }

            _helper.DrawFloat("Particle Texture U Scale", "_particleTexUScale");
            _helper.DrawFloat("Particle Texture V Scale", "_particleTexVScale");
        }

        private void DrawFlagsSection()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Flags", EditorStyles.boldLabel);

            _helper.DrawToggle("Positions Require Two Textures", "_B_LOAD_POS_TWO_TEX");

            if (ShouldDrawCompressedNormalsToggle())
            {
                _helper.DrawToggle("Use Compressed Normals (no rotTex)", "_B_UNLOAD_ROT_TEX");
            }

            _helper.DrawToggle("Load Color Texture", "_B_LOAD_COL_TEX");

            if (IsSubMode(2))
            {
                _helper.DrawToggle("Load Lookup Table", "_B_LOAD_LOOKUP_TABLE");
            }
        }

        private bool ShouldDrawRotationTexture()
        {
            if (IsResetToolInitializing())
            {
                return true;
            }

            if (!TryGetCurrentSubMode(out int subMode) || subMode == 3)
            {
                return false;
            }

            return IsFloatPropertyMixed("_B_UNLOAD_ROT_TEX") || IsFloatPropertyOff("_B_UNLOAD_ROT_TEX");
        }

        private bool ShouldDrawCompressedNormalsToggle()
        {
            if (IsResetToolInitializing())
            {
                return true;
            }

            return TryGetCurrentSubMode(out int subMode) && subMode != 3;
        }

        private void SyncKeywords()
        {
            if (mats == null)
            {
                return;
            }

            foreach (Material mat in mats)
            {
                if (mat == null)
                {
                    continue;
                }

                SyncKeywords(mat);
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
            for (int i = 0; i < SubModeKeywords.Length; i++)
            {
                mat.DisableKeyword(SubModeKeywords[i]);
            }

            if (enabledIndex >= 0 && enabledIndex < SubModeKeywords.Length)
            {
                mat.EnableKeyword(SubModeKeywords[enabledIndex]);
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

        private static void AddVatUV1Stream(
            List<ParticleSystemVertexStream> streams,
            List<string> streamList,
            bool isParticleMode)
        {
            if (isParticleMode)
            {
                AddStream(streams, streamList, ParticleSystemVertexStream.UV2, ParticleBaseGUI.streamUV2Text);
                return;
            }

            AddStream(streams, streamList, ParticleSystemVertexStream.Custom1XYZW, ParticleBaseGUI.streamCustom1Text);
        }
    }
}
