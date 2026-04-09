using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

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
                return;

            int subMode = Mathf.RoundToInt(material.GetFloat("_HoudiniVATSubMode"));

            AddStream(streams, streamList, ParticleSystemVertexStream.Position, ParticleBaseGUI.streamPositionText);
            AddStream(streams, streamList, ParticleSystemVertexStream.Normal, ParticleBaseGUI.streamNormalText);

            switch (subMode)
            {
                case 0: // SoftBody — needs UV1 (Custom1)
                    AddStream(streams, streamList, ParticleSystemVertexStream.Custom1XYZW, ParticleBaseGUI.streamCustom1Text);
                    break;

                case 1: // RigidBody — needs UV1, UV2, UV3 (Custom1, Custom2, vatTexcoord5)
                    AddStream(streams, streamList, ParticleSystemVertexStream.Custom1XYZW, ParticleBaseGUI.streamCustom1Text);
                    AddStream(streams, streamList, ParticleSystemVertexStream.Custom2XYZW, ParticleBaseGUI.streamCustom2Text);
                    AddStream(streams, streamList, ParticleSystemVertexStream.UV, ParticleBaseGUI.streamUVText);
                    break;

                case 2: // DynamicRemeshing — needs UV0 (texcoords)
                    AddStream(streams, streamList, ParticleSystemVertexStream.UV, ParticleBaseGUI.streamUVText);
                    break;

                case 3: // ParticleSprite — needs UV0 (corner) + UV1 (particle U/V)
                    AddStream(streams, streamList, ParticleSystemVertexStream.UV, ParticleBaseGUI.streamUVText);
                    AddStream(streams, streamList, ParticleSystemVertexStream.Custom1XYZW, ParticleBaseGUI.streamCustom1Text);
                    break;
            }
        }

        // ─────────────────────────────────────────────────────────────
        // Material property helpers (bypass ShaderGUIHelper.GetProperty)
        // ─────────────────────────────────────────────────────────────

        private MaterialEditor matEditor => _helper.matEditor;
        private List<Material> mats => _helper.mats;

        private float GetFloat(string name, float defaultValue = 0)
        {
            if (mats == null || mats.Count == 0) return defaultValue;
            var mat = mats[0];
            if (mat.HasProperty(name))
                return mat.GetFloat(name);
            return defaultValue;
        }

        private void SetFloat(string name, float value)
        {
            if (mats == null) return;
            foreach (var mat in mats)
            {
                if (mat != null && mat.HasProperty(name))
                    mat.SetFloat(name, value);
            }
        }

        private Texture GetTexture(string name)
        {
            if (mats == null || mats.Count == 0) return null;
            var mat = mats[0];
            if (mat.HasProperty(name))
                return mat.GetTexture(name);
            return null;
        }

        private void SetTexture(string name, Texture value)
        {
            if (mats == null) return;
            foreach (var mat in mats)
            {
                if (mat != null)
                    mat.SetTexture(name, value);
            }
        }

        private void DrawFloatField(string label, string propName, float defaultValue = 0)
        {
            float val = GetFloat(propName, defaultValue);
            EditorGUI.BeginChangeCheck();
            val = EditorGUILayout.FloatField(label, val);
            if (EditorGUI.EndChangeCheck())
                SetFloat(propName, val);
        }

        private void DrawToggleField(string label, string propName, float defaultValue = 0)
        {
            bool val = GetFloat(propName, defaultValue) > 0.5f;
            EditorGUI.BeginChangeCheck();
            val = EditorGUILayout.Toggle(label, val);
            if (EditorGUI.EndChangeCheck())
                SetFloat(propName, val ? 1 : 0);
        }

        private void DrawTextureField(string label, string propName)
        {
            Texture tex = GetTexture(propName);
            EditorGUI.BeginChangeCheck();
            tex = EditorGUILayout.ObjectField(label, tex, typeof(Texture2D), false) as Texture;
            if (EditorGUI.EndChangeCheck())
                SetTexture(propName, tex);
        }

        // ─────────────────────────────────────────────────────────────
        // Drawing sections
        // ─────────────────────────────────────────────────────────────

        private int GetCurrentSubMode()
        {
            return Mathf.RoundToInt(GetFloat("_HoudiniVATSubMode", 0));
        }

        private void DrawSubModeSelector()
        {
            int current = GetCurrentSubMode();
            EditorGUI.BeginChangeCheck();
            int newVal = EditorGUILayout.Popup("Houdini VAT Sub Mode", current, SubModeNames);
            if (EditorGUI.EndChangeCheck())
                SetFloat("_HoudiniVATSubMode", newVal);
        }

        private void DrawPlaybackSection()
        {
            int subMode = GetCurrentSubMode();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Playback", EditorStyles.boldLabel);

            DrawToggleField("Auto Playback", "_B_autoPlayback", 1);

            bool autoPlayback = GetFloat("_B_autoPlayback", 1) > 0.5f;
            if (!autoPlayback)
                DrawFloatField("Display Frame", "_displayFrame", 0);

            DrawFloatField("Game Time at First Frame", "_gameTimeAtFirstFrame", 0);
            DrawFloatField("Playback Speed", "_playbackSpeed", 1);
            DrawFloatField("Houdini FPS", "_houdiniFPS", 24);
            DrawToggleField("Interframe Interpolation", "_B_interpolate", 1);

            if (subMode == 1) // RigidBody
                DrawToggleField("Animate First Frame", "_animateFirstFrame", 0);
        }

        private void DrawBoundsSection()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Bounds Metadata", EditorStyles.boldLabel);

            DrawFloatField("Bound Min X", "_boundMinX", -1);
            DrawFloatField("Bound Min Y", "_boundMinY", -1);
            DrawFloatField("Bound Min Z", "_boundMinZ", -1);
            DrawFloatField("Bound Max X", "_boundMaxX", 1);
            DrawFloatField("Bound Max Y", "_boundMaxY", 1);
            DrawFloatField("Bound Max Z", "_boundMaxZ", 1);
        }

        private void DrawTextureSection()
        {
            int subMode = GetCurrentSubMode();
            float loadLookupTable = GetFloat("_B_LOAD_LOOKUP_TABLE", 0);
            float loadPosTwoTex   = GetFloat("_B_LOAD_POS_TWO_TEX", 0);
            float loadColTex      = GetFloat("_B_LOAD_COL_TEX", 0);
            float unloadRotTex    = GetFloat("_B_UNLOAD_ROT_TEX", 0);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Textures", EditorStyles.boldLabel);

            DrawTextureField("Position Texture", "_posTexture");

            if (loadPosTwoTex > 0.5f)
                DrawTextureField("Position Texture 2", "_posTexture2");

            // Rotation texture: hide for ParticleSprite mode or when compressed normals are used
            if (subMode != 3 && unloadRotTex <= 0.5f)
                DrawTextureField("Rotation Texture", "_rotTexture");

            if (loadColTex > 0.5f)
                DrawTextureField("Color Texture", "_colTexture");

            // Lookup table: only for DynamicRemeshing or when flag is set
            if (subMode == 2 || loadLookupTable > 0.5f)
                DrawTextureField("Lookup Table", "_lookupTable");
        }

        private void DrawScaleSection()
        {
            int subMode = GetCurrentSubMode();

            if (subMode == 1 || subMode == 3)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Scale", EditorStyles.boldLabel);

                DrawFloatField("Global Piece Scale Multiplier", "_globalPscaleMul", 1);
                DrawToggleField("Piece Scales in Position Alpha", "_B_pscaleAreInPosA", 1);
            }
        }

        private void DrawParticleSpriteSection()
        {
            int subMode = GetCurrentSubMode();
            if (subMode != 3)
                return;

            float canSpin = GetFloat("_B_CAN_SPIN", 0);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Particle Sprite", EditorStyles.boldLabel);

            DrawFloatField("Width Base Scale", "_widthBaseScale", 0.2f);
            DrawFloatField("Height Base Scale", "_heightBaseScale", 0.2f);

            DrawToggleField("Hide Overlapping Origin", "_B_hideOverlappingOrigin", 1);
            float hideOrigin = GetFloat("_B_hideOverlappingOrigin", 1);
            if (hideOrigin > 0.5f)
                DrawFloatField("Origin Effective Radius", "_originRadius", 0.02f);

            DrawToggleField("Particles Can Spin", "_B_CAN_SPIN", 0);

            if (canSpin > 0.5f)
            {
                DrawToggleField("Compute Spin from Heading", "_B_spinFromHeading", 0);

                float spinFromHeading = GetFloat("_B_spinFromHeading", 0);
                if (spinFromHeading <= 0.5f)
                    DrawFloatField("Particle Spin Phase", "_spinPhase", 0);

                DrawFloatField("Scale by Velocity Amount", "_scaleByVelAmount", 1);
            }

            DrawFloatField("Particle Texture U Scale", "_particleTexUScale", 1);
            DrawFloatField("Particle Texture V Scale", "_particleTexVScale", 1);
        }

        private void DrawFlagsSection()
        {
            int subMode = GetCurrentSubMode();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Flags", EditorStyles.boldLabel);

            DrawToggleField("Positions Require Two Textures", "_B_LOAD_POS_TWO_TEX", 0);

            if (subMode != 3)
                DrawToggleField("Use Compressed Normals (no rotTex)", "_B_UNLOAD_ROT_TEX", 0);

            DrawToggleField("Load Color Texture", "_B_LOAD_COL_TEX", 0);

            if (subMode == 2)
                DrawToggleField("Load Lookup Table", "_B_LOAD_LOOKUP_TABLE", 0);
        }

        private void SyncKeywords()
        {
            if (mats == null)
                return;

            int subMode = GetCurrentSubMode();

            foreach (Material mat in mats)
            {
                if (mat == null) continue;

                // Sync sub-mode keywords
                SetKeyword(mat, "_HOUDINI_VAT_SOFTBODY",         subMode == 0);
                SetKeyword(mat, "_HOUDINI_VAT_RIGIDBODY",        subMode == 1);
                SetKeyword(mat, "_HOUDINI_VAT_DYNAMIC_REMESH",   subMode == 2);
                SetKeyword(mat, "_HOUDINI_VAT_PARTICLE_SPRITE",  subMode == 3);

                // Sync feature keywords based on flags
                SetKeyword(mat, "_B_LOAD_POS_TWO_TEX",   mat.GetFloat("_B_LOAD_POS_TWO_TEX")   > 0.5f);
                SetKeyword(mat, "_B_UNLOAD_ROT_TEX",     mat.GetFloat("_B_UNLOAD_ROT_TEX")     > 0.5f);
                SetKeyword(mat, "_B_LOAD_COL_TEX",       mat.GetFloat("_B_LOAD_COL_TEX")       > 0.5f);
                SetKeyword(mat, "_B_CAN_SPIN",           mat.GetFloat("_B_CAN_SPIN")           > 0.5f);
                SetKeyword(mat, "_B_LOAD_LOOKUP_TABLE",  mat.GetFloat("_B_LOAD_LOOKUP_TABLE")  > 0.5f);
            }
        }

        private static void SetKeyword(Material mat, string keyword, bool state)
        {
            if (state)
                mat.EnableKeyword(keyword);
            else
                mat.DisableKeyword(keyword);
        }

        private static void AddStream(
            List<ParticleSystemVertexStream> streams,
            List<string> streamList,
            ParticleSystemVertexStream stream,
            string streamLabel)
        {
            if (streams.Contains(stream))
                return;

            streams.Add(stream);
            streamList.Add(streamLabel);
        }
    }
}
