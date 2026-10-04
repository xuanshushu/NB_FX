using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using NBShader;
using NBShaders2.Editor.FeatureLevel;

namespace NBShaderEditor
{
    public class NBShaderSyncService
    {
        private const string FeatureTierPropertyName = "_NBShaderFeatureTier";
        private const string StencilConfigAssetPath = "Packages/com.xuanxuan.nb.fx/XuanXuanRenderUtility/Shader/StencilConfig.asset";
        private readonly NBShaderRootItem _rootItem;
        private StencilValuesConfig _stencilValuesConfig;

        public int KeywordVersion { get; private set; }

        private static readonly FlagToggleBinding[] ToggleFlagBindings =
        {
            new FlagToggleBinding("_ColorAdjustmentOnlyAffectMainTex", NBShaderFlags.FLAG_BIT_PARTICLE_COLOR_ADJUSTMENT_ONLY_AFFECT_MAINTEX, 0),
            new FlagToggleBinding("_HueShift_Toggle", NBShaderFlags.FLAG_BIT_HUESHIFT_ON, 0),
            new FlagToggleBinding("_ChangeSaturability_Toggle", NBShaderFlags.FLAG_BIT_SATURABILITY_ON, 0),
            new FlagToggleBinding("_Contrast_Toggle", NBShaderFlags.FLAG_BIT_PARTICLE_1_MAINTEX_CONTRAST, 1),
            new FlagToggleBinding("_BaseMapColorRefine_Toggle", NBShaderFlags.FLAG_BIT_PARTICLE_1_MAINTEX_COLOR_REFINE, 1),
            new FlagToggleBinding("_ColorMultiAlpha", NBShaderFlags.FLAG_BIT_PARTICLE_COLOR_MULTI_ALPHA, 0),
            new FlagToggleBinding("_BaseBackColor_Toggle", NBShaderFlags.FLAG_BIT_PARTICLE_BACKCOLOR, 0),
            new FlagToggleBinding("_IgnoreVetexColor_Toggle", NBShaderFlags.FLAG_BIT_PARTICLE_1_IGNORE_VERTEX_COLOR, 1),
            new FlagToggleBinding("_BumpMapMaskMode", NBShaderFlags.FLAG_BIT_PARTICLE_NORMALMAP_MASK_MODE, 0),
            new FlagToggleBinding("_DistortionBothDirection_Toggle", NBShaderFlags.FLAG_BIT_PARTICLE_NOISEMAP_NORMALIZEED_ON, 0),
            new FlagToggleBinding("_Distortion_Choraticaberrat_WithNoise_Toggle", NBShaderFlags.FLAG_BIT_PARTICLE_NOISE_CHORATICABERRAT_WITH_NOISE, 0),
            new FlagToggleBinding("_DissolveLineMaskToggle", NBShaderFlags.FLAG_BIT_PARTICLE_1_DISSOLVE_LINE_MASK, 1),
            new FlagToggleBinding("_MaskRefineToggle", NBShaderFlags.FLAG_BIT_PARTICLE_1_MASK_REFINE, 1),
            new FlagToggleBinding("_MaskMapGradientToggle", NBShaderFlags.FLAG_BIT_PARTICLE_1_MASKMAP_GRADIENT, 1),
            new FlagToggleBinding("_MaskMap2GradientToggle", NBShaderFlags.FLAG_BIT_PARTICLE_1_MASKMAP_2_GRADIENT, 1),
            new FlagToggleBinding("_MaskMap3GradientToggle", NBShaderFlags.FLAG_BIT_PARTICLE_1_MASKMAP_3_GRADIENT, 1),
            new FlagToggleBinding("_ScreenDistortAlphaRefineToggle", NBShaderFlags.FLAG_BIT_PARTICLE_1_SCREEN_DISTORT_ALPHA_REFINE, 1),
            new FlagToggleBinding("_InvertFresnel_Toggle", NBShaderFlags.FLAG_BIT_PARTICLE_FRESNEL_INVERT_ON, 0),
            new FlagToggleBinding("_FresnelColorAffectByAlpha", NBShaderFlags.FLAG_BIT_PARTICLE_FRESNEL_COLOR_AFFETCT_BY_ALPHA, 0),
            new FlagToggleBinding("_VertexOffset_StartFromZero", NBShaderFlags.FLAG_BIT_PARTICLE_1_VERTEXOFFSET_START_FROM_ZERO, 1),
            new FlagToggleBinding("_UTwirlEnabled", NBShaderFlags.FLAG_BIT_PARTICLE_UTWIRL_ON, 0),
            new FlagToggleBinding("_PolarCoordinatesEnabled", NBShaderFlags.FLAG_BIT_PARTICLE_POLARCOORDINATES_ON, 0)
        };

        private static readonly FlagModeBinding[] ModeFlagBindings =
        {
            new FlagModeBinding("_EmissionBlendMode", NBShaderFlags.FLAG_BIT_PARTICLE_COLOR_OVERLAY_1_MULTIPLY, 0, 1),
            new FlagModeBinding("_ColorBlendMode", NBShaderFlags.FLAG_BIT_PARTICLE_1_COLOR_OVERLAY_2_ADD, 1, 0),
            new FlagModeBinding("_EmissionAlphaMultiplyMode", NBShaderFlags.FLAG_BIT_PARTICLE_1_COLOR_OVERLAY_1_ALPHA_MULTIPLY, 1, 1),
            new FlagModeBinding("_ColorBlendAlphaMultiplyMode", NBShaderFlags.FLAG_BIT_PARTICLE_COLOR_BLEND_ALPHA_MULTIPLY_MODE, 0, 1),
            new FlagModeBinding("_RampColorBlendMode", NBShaderFlags.FLAG_BIT_PARTICLE_RAMP_COLOR_BLEND_ADD, 0, 1),
            new FlagModeBinding("_DissolveRampColorBlendMode", NBShaderFlags.FLAG_BIT_PARTICLE_1_DISSOLVE_RAMP_MULITPLY, 1, 1),
            new FlagModeBinding("_FresnelMode", NBShaderFlags.FLAG_BIT_PARTICLE_FRESNEL_FADE_ON, 0, 1)
        };

        private static readonly string[] HoudiniVatKeywords =
        {
            "_HOUDINI_VAT_SOFTBODY",
            "_HOUDINI_VAT_RIGIDBODY",
            "_HOUDINI_VAT_DYNAMIC_REMESH",
            "_HOUDINI_VAT_PARTICLE_SPRITE"
        };

        private static readonly string[] TyflowVatKeywords =
        {
            "_TYFLOW_VAT_ABSOLUTE",
            "_TYFLOW_VAT_RELATIVE",
            "_TYFLOW_VAT_SKIN_R",
            "_TYFLOW_VAT_SKIN_PR",
            "_TYFLOW_VAT_SKIN_PRSAVE",
            "_TYFLOW_VAT_SKIN_PRSXYZ"
        };

        public NBShaderSyncService(NBShaderRootItem rootItem)
        {
            _rootItem = rootItem;
        }

        public static void SyncMaterialState(Material material)
        {
            if (material == null)
            {
                return;
            }

            SyncMaterialState(new List<Material> { material });
        }

        public static void SyncMaterialState(IList<Material> materials)
        {
            if (materials == null || materials.Count == 0)
            {
                return;
            }

            var validMaterials = new List<Material>();
            for (int i = 0; i < materials.Count; i++)
            {
                if (materials[i] != null)
                {
                    validMaterials.Add(materials[i]);
                }
            }

            if (validMaterials.Count == 0)
            {
                return;
            }

            var rootItem = new NBShaderRootItem
            {
                Mats = validMaterials,
                Shader = validMaterials[0].shader
            };
            rootItem.InitFlags(validMaterials);
            new NBShaderSyncService(rootItem).SyncMaterialState();
        }

        bool HasGraphTargets()
        {
            if (_rootItem.Mats == null) return false;
            foreach (Material material in _rootItem.Mats)
                if (NBShaderGUIContext.IsGraphMaterial(material)) return true;
            return false;
        }

        public void NotifyKeywordsMayHaveChanged()
        {
            KeywordVersion++;
        }

        public void ApplyTransparentMode(TransparentMode mode)
        {
            if (HasGraphTargets()) return; // Graph1A uses native fallback; no legacy projection.
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
                        SetRenderQueueIfNeeded(mat, 2000 + queueBias);
                        SetKeyword(mat, "_ALPHATEST_ON", false);
                        SyncResolvedIntentStateIfNBShader(mat);
                    }
                    break;

                case TransparentMode.Transparent:
                    zWriteProperty.floatValue = 0;
                    foreach (Material mat in _rootItem.Mats)
                    {
                        SetRenderQueueIfNeeded(mat, 3000 + queueBias);
                        SetKeyword(mat, "_ALPHATEST_ON", false);
                        SyncResolvedIntentStateIfNBShader(mat);
                    }
                    break;

                case TransparentMode.CutOff:
                    zWriteProperty.floatValue = 1;
                    foreach (Material mat in _rootItem.Mats)
                    {
                        SetRenderQueueIfNeeded(mat, 2450 + queueBias);
                        SetKeyword(mat, "_ALPHATEST_ON", true);
                        SyncResolvedIntentStateIfNBShader(mat);
                    }
                    break;
            }
        }

        // Explicit ordinary-Graph MainTex protocol edits; general projection guards stay intact.
        private bool HasGraphMainTexTargets()
        {
            if (_rootItem.Mats == null || _rootItem.Mats.Count == 0 || NBShaderGUIContext.HasMixedHosts(_rootItem.Mats)) return false;
            foreach (Material material in _rootItem.Mats)
            {
                if (!NBShaderGUIContext.IsGraphMaterial(material)) return false;
                foreach (string name in new[] { "_NB_DistortionMode", "_NB_Flags0Lo16", "_NB_Flags0Hi16", "_NB_Flags1Lo16", "_NB_Flags1Hi16" })
                {
                    if (!NBShaderRootItem.HasFloatProperty(material, name)) return false;
                    float value = material.GetFloat(name);
                    if (float.IsNaN(value) || float.IsInfinity(value)) return false;
                }
            }
            return true;
        }

        private bool HasGraphMainTexFields(string[] floats, string[] vectors)
        {
            if (!HasGraphMainTexTargets()) return false;
            foreach (Material material in _rootItem.Mats)
            {
                foreach (string name in floats)
                {
                    if (!NBShaderRootItem.HasFloatProperty(material, name) || !_rootItem.PropertyInfoDic.ContainsKey(name)) return false;
                    float value = material.GetFloat(name);
                    if (float.IsNaN(value) || float.IsInfinity(value)) return false;
                }
                foreach (string name in vectors)
                {
                    if (!_rootItem.PropertyInfoDic.ContainsKey(name)) return false;
                    int index = material.shader.FindPropertyIndex(name);
                    if (index < 0 || material.shader.GetPropertyType(index) != UnityEngine.Rendering.ShaderPropertyType.Vector) return false;
                    Vector4 value = material.GetVector(name);
                    for (int channel = 0; channel < 4; ++channel)
                        if (float.IsNaN(value[channel]) || float.IsInfinity(value[channel])) return false;
                }
            }
            return true;
        }

        internal bool HasGraphMainTexCustomDataEditSchema()
            => HasGraphMainTexFields(new[] { "_NB_CustomDataFlag0Lo16", "_NB_CustomDataFlag0Hi16" }, Array.Empty<string>());

        internal bool HasGraphMainTexUVEditSchema()
            => HasGraphMainTexFields(new[] {
                "_MainTexUVModeFoldOut", "_GlobalTwirlFoldOut", "_GlobalPolarFoldOut",
                "_NB_UVModeFlag0Lo16", "_NB_UVModeFlag0Hi16", "_NB_UVModeFlagType0Lo16", "_NB_UVModeFlagType0Hi16",
                "_NB_Flags0Lo16", "_NB_Flags0Hi16", "_NB_Flags1Lo16", "_NB_Flags1Hi16",
                "_UTwirlEnabled", "_PolarCoordinatesEnabled", "_TWStrength", "_WorldSpaceUVModeSelector", "_ObjectSpaceUVModeSelector"
            }, new[] { "_TWParameter", "_PCCenter", "_CylinderUVRotate", "_CylinderUVPosOffset",
                "_CylinderMatrix0", "_CylinderMatrix1", "_CylinderMatrix2", "_CylinderMatrix3" });

        private static int ReadGraphHalf(Material material, string name)
            => Mathf.RoundToInt(Mathf.Clamp(material.GetFloat(name), 0f, 65535f));

        private static bool WriteGraphHalfSlice(Material material, string name, int mask, int value)
        {
            int before = ReadGraphHalf(material, name);
            int after = (before & ~mask) | (value & mask);
            if (before == after) return false; // Preserve a finite noncanonical raw half on a decoded no-op.
            material.SetFloat(name, after);
            NotifyGraphPackedFlagsEdited(material, name, mask);
            return true;
        }

        private void RefreshGraphMainTexPropertyReferences()
        {
            if (_rootItem.MatEditor == null) return;
            foreach (MaterialProperty property in MaterialEditor.GetMaterialProperties(_rootItem.MatEditor.targets))
                if (_rootItem.PropertyInfoDic.TryGetValue(property.name, out ShaderPropertyInfo info)) info.Property = property;
        }

        private bool RunGraphMainTexEdit(string label, Func<Material, bool> edit)
        {
            if (!HasGraphMainTexTargets()) return false;
            var objects = new List<UnityEngine.Object>();
            foreach (Material material in _rootItem.Mats) objects.Add(material);
            Undo.RecordObjects(objects.ToArray(), label);
            foreach (Material material in _rootItem.Mats)
                if (edit(material)) EditorUtility.SetDirty(material);
            RefreshGraphMainTexPropertyReferences();
            return true;
        }

        private static bool UpdateGraphMainTexUVDerived(Material material)
        {
            var flags = new NBShaderFlags(material);
            bool changed = WriteGraphHalfSlice(material, "_NB_Flags1Hi16", 16,
                flags.CheckIsUVModeOn(NBShaderFlags.UVMode.Cylinder) ? 16 : 0);
            if (!flags.CheckIsUVModeOn(NBShaderFlags.UVMode.SpecialUVChannel))
                changed |= WriteGraphHalfSlice(material, "_NB_Flags1Hi16", 12, 0);
            return changed;
        }

        internal bool TryApplyGraphMainTexUVMode(NBShaderFlags.UVMode mode, bool setFoldFromPopup = false)
        {
            if (!HasGraphMainTexUVEditSchema() || (int)mode < 0 || (int)mode > 8) return false;
            return RunGraphMainTexEdit("Main Texture UV Source", material => {
                bool changed = WriteGraphHalfSlice(material, "_NB_UVModeFlag0Lo16", 3, (int)mode & 3);
                changed |= WriteGraphHalfSlice(material, "_NB_UVModeFlagType0Lo16", 3, (int)mode / 4);
                if (setFoldFromPopup)
                {
                    float fold = mode == NBShaderFlags.UVMode.DefaultUVChannel || mode == NBShaderFlags.UVMode.CommonUV ||
                        mode == NBShaderFlags.UVMode.ScreenUV || mode == NBShaderFlags.UVMode.MainTex ? 0f : 1f;
                    if (material.GetFloat("_MainTexUVModeFoldOut") != fold) { material.SetFloat("_MainTexUVModeFoldOut", fold); changed = true; }
                }
                return UpdateGraphMainTexUVDerived(material) | changed;
            });
        }

        internal bool TryApplyGraphMainTexCustomData(int position, NBShaderFlags.CutomDataComponent component)
        {
            if (!HasGraphMainTexCustomDataEditSchema() || (position != 0 && position != 4) || (int)component < 0 || (int)component > 8) return false;
            // Same public protocol constants/enum as NBShaderFlags.SetCustomDataFlag; no new meaning.
            int[] values = { 0, NBShaderFlags.CustomData1XBit, NBShaderFlags.CustomData1YBit,
                NBShaderFlags.CustomData1ZBit, NBShaderFlags.CustomData1WBit, NBShaderFlags.CustomData2XBit,
                NBShaderFlags.CustomData2YBit, NBShaderFlags.CustomData2ZBit, NBShaderFlags.CustomData2WBit };
            return RunGraphMainTexEdit("Main Texture Offset Custom Data", material =>
                WriteGraphHalfSlice(material, "_NB_CustomDataFlag0Lo16", 15 << position, values[(int)component] << position));
        }

        internal bool TryApplyGraphMainTexSpecialUV(int channel)
        {
            if (!HasGraphMainTexUVEditSchema() || channel < 0 || channel > 1) return false;
            return RunGraphMainTexEdit("Special UV Channel", material =>
                WriteGraphHalfSlice(material, "_NB_Flags1Hi16", 12, channel == 0 ? 4 : 8));
        }

        internal bool TryApplyGraphMainTexUVToggle(int bit, bool enabled)
        {
            if (!HasGraphMainTexUVEditSchema() || (bit != NBShaderFlags.FLAG_BIT_PARTICLE_UTWIRL_ON && bit != NBShaderFlags.FLAG_BIT_PARTICLE_POLARCOORDINATES_ON)) return false;
            string property = bit == NBShaderFlags.FLAG_BIT_PARTICLE_UTWIRL_ON ? "_UTwirlEnabled" : "_PolarCoordinatesEnabled";
            return RunGraphMainTexEdit("Main Texture UV Transform", material => {
                bool changed = material.GetFloat(property) != (enabled ? 1f : 0f);
                if (changed) material.SetFloat(property, enabled ? 1f : 0f);
                return WriteGraphHalfSlice(material, "_NB_Flags0Lo16", bit, enabled ? bit : 0) | changed;
            });
        }

        internal bool TryApplyGraphMainTexPlane(string property, int plane)
        {
            if (!HasGraphMainTexUVEditSchema() || plane < 0 || plane > 2 ||
                (property != "_WorldSpaceUVModeSelector" && property != "_ObjectSpaceUVModeSelector")) return false;
            return RunGraphMainTexEdit("Coordinate Plane", material => {
                if (material.GetFloat(property) == plane) return false;
                material.SetFloat(property, plane); return true;
            });
        }

        internal bool TryApplyGraphMainTexCylinderComponent(string property, int component, float value)
        {
            if (!HasGraphMainTexUVEditSchema() || component < 0 || component > 2 || float.IsNaN(value) || float.IsInfinity(value) ||
                (property != "_CylinderUVRotate" && property != "_CylinderUVPosOffset")) return false;
            return RunGraphMainTexEdit("Cylinder UV", material => {
                Vector4 vector = material.GetVector(property);
                if (vector[component] == value) return false;
                vector[component] = value; material.SetVector(property, vector);
                Vector4 rotation = material.GetVector("_CylinderUVRotate"), offset = material.GetVector("_CylinderUVPosOffset");
                Matrix4x4 matrix = Matrix4x4.Translate(new Vector3(offset.x, offset.y, offset.z)) *
                    Matrix4x4.Rotate(Quaternion.Euler(new Vector3(rotation.x, rotation.y, rotation.z)));
                for (int row = 0; row < 4; ++row) material.SetVector("_CylinderMatrix" + row, matrix.GetRow(row));
                return true;
            });
        }

        // Wrap original reset, including descendants, before any MaterialProperty setter runs.
        // Restore unowned raw halves and unowned decoded bits rather than normalize the whole word.
        internal bool TryRunGraphMainTexReset(Action reset, bool wholeBlock, int flags0Bits = 0)
        {
            if (!HasGraphMainTexTargets() || (!wholeBlock && !HasGraphMainTexUVEditSchema())) return false;
            string[] names = { "_NB_Flags0Lo16", "_NB_Flags0Hi16", "_NB_Flags1Lo16", "_NB_Flags1Hi16",
                "_NB_UVModeFlag0Lo16", "_NB_UVModeFlag0Hi16", "_NB_UVModeFlagType0Lo16", "_NB_UVModeFlagType0Hi16",
                "_NB_CustomDataFlag0Lo16", "_NB_CustomDataFlag0Hi16", "_NB_WrapFlagsLo16", "_NB_WrapFlagsHi16",
                "_NB_ColorChannelLo16", "_NB_ColorChannelHi16", "_NB_ForceNoMipFlagsLo16", "_NB_ForceNoMipFlagsHi16" };
            var before = new List<Dictionary<string, float>>(); var snapshots = new List<string>();
            var objects = new List<UnityEngine.Object>();
            foreach (Material material in _rootItem.Mats)
            {
                var values = new Dictionary<string, float>();
                foreach (string name in names) if (material.HasProperty(name)) values.Add(name, material.GetFloat(name));
                before.Add(values); snapshots.Add(EditorJsonUtility.ToJson(material)); objects.Add(material);
            }
            Undo.RecordObjects(objects.ToArray(), "Reset Main Texture");
            reset();
            for (int index = 0; index < _rootItem.Mats.Count; ++index)
            {
                Material material = _rootItem.Mats[index];
                if (wholeBlock && HasGraphMainTexUVEditSchema()) UpdateGraphMainTexUVDerived(material);
                if (!wholeBlock && flags0Bits != 0)
                {
                    string property = flags0Bits == NBShaderFlags.FLAG_BIT_PARTICLE_UTWIRL_ON ? "_UTwirlEnabled" : "_PolarCoordinatesEnabled";
                    WriteGraphHalfSlice(material, "_NB_Flags0Lo16", flags0Bits, material.GetFloat(property) > .5f ? flags0Bits : 0);
                }
                foreach (var pair in before[index])
                {
                    int mask = 0;
                    if (pair.Key == "_NB_Flags0Lo16") mask = flags0Bits;
                    if (wholeBlock)
                    {
                        if (pair.Key == "_NB_Flags1Hi16") mask = 28;
                        else if (pair.Key == "_NB_UVModeFlag0Lo16" || pair.Key == "_NB_UVModeFlagType0Lo16" || pair.Key == "_NB_ColorChannelLo16") mask = 3;
                        else if (pair.Key == "_NB_CustomDataFlag0Lo16") mask = 255;
                        else if (pair.Key == "_NB_WrapFlagsLo16" || pair.Key == "_NB_WrapFlagsHi16" || pair.Key == "_NB_ForceNoMipFlagsLo16") mask = 1;
                    }
                    int previous = Mathf.RoundToInt(Mathf.Clamp(pair.Value, 0f, 65535f));
                    int merged = (ReadGraphHalf(material, pair.Key) & mask) | (previous & ~mask);
                    float value = merged == previous ? pair.Value : merged;
                    if (material.GetFloat(pair.Key) != value) material.SetFloat(pair.Key, value);
                }
                if (EditorJsonUtility.ToJson(material) != snapshots[index]) EditorUtility.SetDirty(material);
            }
            RefreshGraphMainTexPropertyReferences(); return true;
        }

        internal const string GraphGUIStateVersionProperty = "_NB_GraphGUIStateVersion";

        // GUI1B candidate: serialized UI mirrors of the EXISTING flag protocol.
        // No Graph Tier/effective projection, no periodic mirror->flags authority switch.
        internal static bool GraphFlagIntentSchemaAvailable(Material material)
        {
            if (!NBShaderGUIContext.IsGraphMaterial(material)) return false;
            for (int i = 0; i < ToggleFlagBindings.Length; ++i)
                if (!NBShaderRootItem.HasFloatProperty(material, ToggleFlagBindings[i].propertyName)) return false;
            for (int i = 0; i < ModeFlagBindings.Length; ++i)
                if (!NBShaderRootItem.HasFloatProperty(material, ModeFlagBindings[i].propertyName)) return false;
            return true;
        }

        internal static bool IsGraphIntentPackedHalf(string propertyName)
            => propertyName == "_NB_Flags0Lo16" || propertyName == "_NB_Flags0Hi16" ||
                propertyName == "_NB_Flags1Lo16" || propertyName == "_NB_Flags1Hi16";

        // Explicit native editor callback only. The caller owns the existing
        // Undo transaction and has already changed this raw slice. Ordinary
        // Validate/Sync still never guesses whether an unowned mirror is newer.
        internal static void NotifyGraphPackedFlagsEdited(Material material,
            string propertyName, int editedSliceBits = 65535)
        {
            if (!IsGraphIntentPackedHalf(propertyName) ||
                !GraphFlagIntentSchemaAvailable(material) ||
                !NBShaderRootItem.HasFloatProperty(material, GraphGUIStateVersionProperty) ||
                material.GetFloat(GraphGUIStateVersionProperty) != 2f) return;
            int word = propertyName.StartsWith("_NB_Flags0", StringComparison.Ordinal) ? 0 : 1;
            uint mask = unchecked((uint)editedSliceBits) & 65535u;
            if (propertyName.EndsWith("Hi16", StringComparison.Ordinal)) mask <<= 16;
            SeedGraphFlagIntents(material, word, mask);
        }

        private static void SeedGraphFlagIntents(Material material,
            int editedWord = -1, uint editedBits = uint.MaxValue)
        {
            var flags = new NBShaderFlags(material); // Shared Material halfword read hooks; NEVER write words here.
            for (int i = 0; i < ToggleFlagBindings.Length; ++i)
            {
                var binding = ToggleFlagBindings[i];
                if (editedWord >= 0 && (binding.flagIndex != editedWord ||
                    (unchecked((uint)binding.flagBits) & editedBits) == 0u)) continue;
                material.SetFloat(binding.propertyName,
                    flags.CheckFlagBits(binding.flagBits, index: binding.flagIndex) ? 1f : 0f);
            }
            for (int i = 0; i < ModeFlagBindings.Length; ++i)
            {
                var binding = ModeFlagBindings[i];
                if (editedWord >= 0 && (binding.flagIndex != editedWord ||
                    (unchecked((uint)binding.flagBits) & editedBits) == 0u)) continue;
                int disabledMode = binding.enabledMode == 0 ? 1 : 0;
                material.SetFloat(binding.propertyName,
                    flags.CheckFlagBits(binding.flagBits, index: binding.flagIndex)
                        ? binding.enabledMode : disabledMode);
            }
        }

        internal void PrepareGraphGUIState()
        {
            if (_rootItem.Mats == null || NBShaderGUIContext.HasMixedHosts(_rootItem.Mats)) return;
            var uninitialized = new List<UnityEngine.Object>();
            foreach (Material material in _rootItem.Mats)
            {
                if (!NBShaderGUIContext.IsGraphMaterial(material) ||
                    !NBShaderRootItem.HasFloatProperty(material, GraphGUIStateVersionProperty) ||
                    !NBShaderRootItem.HasFloatProperty(material, "_MainTexBigBlockItemFoldOut") ||
                    !NBShaderRootItem.HasFloatProperty(material, "_BaseMapFoldOut")) continue;
                float targetVersion = GraphFlagIntentSchemaAvailable(material) ? 2f : 1f;
                if (material.GetFloat(GraphGUIStateVersionProperty) < targetVersion) uninitialized.Add(material);
            }
            if (uninitialized.Count == 0) return;
            Undo.RecordObjects(uninitialized.ToArray(), "Initialize NB Graph GUI state");
            foreach (UnityEngine.Object target in uninitialized)
            {
                var material = (Material)target;
                bool seedFlagIntents = GraphFlagIntentSchemaAvailable(material);
                if (seedFlagIntents) SeedGraphFlagIntents(material);
                // Commit version LAST. Existing Graph functional properties, raw
                // halfwords (including noncanonical finite values), URP surface,
                // keywords, passes, queue, textures and prior foldouts are untouched.
                material.SetFloat(GraphGUIStateVersionProperty, seedFlagIntents ? 2f : 1f);
                EditorUtility.SetDirty(material);
            }
        }

        public void SyncMaterialState()
        {
            // Static Material/list entry must also be protected without Context.
            if (NBShaderGUIContext.HasMixedHosts(_rootItem.Mats)) return;
            PrepareGraphGUIState();
            for (int i = 0; i < _rootItem.Mats.Count; i++)
            {
                Material mat = _rootItem.Mats[i];
                if (mat == null)
                {
                    continue;
                }

                // Graph GUI1A's shared controls write their own supported words.
                // Never run legacy Mesh/Surface/Time/Tier/Pass projection here.
                if (NBShaderGUIContext.IsGraphMaterial(mat)) continue;

                NBShaderFlags flags = GetFlags(i);
                SyncMeshSourceMode(mat, flags);
                SyncCustomData(mat, flags);
                SyncUVDerivedFlags(flags);
                SyncTransparentMode(mat);
                SyncTransparentShadowFlags(mat, flags);
                SyncBlendMode(mat);
                SyncTimeMode(mat, flags);
                SyncTogglePropertyFlags(mat, flags);
                SyncParallaxLayerCount(mat);
                SyncResolvedIntentState(mat);
            }
        }

        public void ApplyToggleFlag(int flagBits, bool enabled, int flagIndex = 0)
        {
            if (NBShaderGUIContext.HasMixedHosts(_rootItem.Mats)) return;
            foreach (ShaderFlagsBase flagBase in _rootItem.ShaderFlags)
            {
                if (enabled)
                {
                    SetFlag(flagBase, flagBits, true, flagIndex);
                }
                else
                {
                    SetFlag(flagBase, flagBits, false, flagIndex);
                }
            }
        }

        public void ApplyShaderPass(string passName, bool enabled)
        {
            if (HasGraphTargets()) return; // Graph1A uses native fallback; no legacy projection.
            foreach (Material mat in _rootItem.Mats)
            {
                if (IsResolvedIntentPass(passName) && NBShaderMaterialIntentResolver.IsNBShaderMaterial(mat))
                    SyncResolvedIntentState(mat);
                else
                    SetShaderPassEnabledIfNeeded(mat, passName, enabled);
            }
        }

        public void ApplyScreenDistortMode(int mode)
        {
            if (HasGraphTargets()) return; // Graph1A uses native fallback; no legacy projection.
            foreach (Material mat in _rootItem.Mats)
            {
                if (mat == null)
                    continue;

                if (mode == 0 && mat.HasProperty("_DisableMainPassToggle"))
                {
                    SetFloatIfExists(mat, "_DisableMainPassToggle", 0f);
                }

                if (NBShaderMaterialIntentResolver.IsNBShaderMaterial(mat))
                    SyncResolvedIntentState(mat);
            }
        }

        public void ApplyDepthDecalEnabled(bool enabled)
        {
            if (HasGraphTargets()) return; // Graph1A uses native fallback; no legacy projection.
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
            if (HasGraphTargets()) return; // Graph1A uses native fallback; no legacy projection.
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
                        SetFloatIfExists(mat, "_TransparentMode", (float)TransparentMode.CutOff);
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
            if (HasGraphTargets()) return; // Graph1A uses native fallback; no legacy projection.
            foreach (Material mat in _rootItem.Mats)
            {
                SetFloatIfExists(mat, "_VAT_Toggle", enabled ? 1f : 0f);

                if (enabled)
                    DisableFlipbook(mat);

                if (NBShaderMaterialIntentResolver.IsNBShaderMaterial(mat))
                    SyncResolvedIntentState(mat);
                else
                    SyncVatKeywords(mat);
            }
        }

        // Explicit shared Flipbook editor capability. General Graph Sync/Apply guards remain.
        internal bool HasGraphFlipbookEditSchema()
        {
            if (_rootItem.Mats == null || _rootItem.Mats.Count == 0 || NBShaderGUIContext.HasMixedHosts(_rootItem.Mats)) return false;
            foreach (Material material in _rootItem.Mats)
            {
                if (material == null || !NBShaderGUIContext.IsGraphMaterial(material)) return false;
                NBShaderMaterialIntentResult intent; string[] unavailable;
                if (!NBShaderMaterialIntentResolver.TryResolveGraphSupportedKeywordIntent(material,
                    NBShaderFeatureTier.Ultra, NBShaderFeatureCatalog.RawKeywords, out intent, out unavailable)) return false;
                foreach (string name in new[] { "_FlipbookBlending", "_VAT_Toggle", "_AnimationSheetHelperBlendIntensity" })
                {
                    if (!NBShaderRootItem.HasFloatProperty(material, name) || !_rootItem.PropertyInfoDic.ContainsKey(name)) return false;
                    float value = material.GetFloat(name); if (float.IsNaN(value) || float.IsInfinity(value)) return false;
                }
                foreach (string name in new[] { "_BaseMap_ST", "_BaseMap_AnimationSheetBlend_ST" })
                {
                    int index = material.shader.FindPropertyIndex(name);
                    if (index < 0 || material.shader.GetPropertyType(index) != UnityEngine.Rendering.ShaderPropertyType.Vector || !_rootItem.PropertyInfoDic.ContainsKey(name)) return false;
                    Vector4 value = material.GetVector(name);
                    for (int channel = 0; channel < 4; ++channel) if (float.IsNaN(value[channel]) || float.IsInfinity(value[channel])) return false;
                }
            }
            return true;
        }

        internal bool TryApplyGraphFlipbookEdit(bool enabled)
        {
            if (!HasGraphFlipbookEditSchema()) return false;
            foreach (Material material in _rootItem.Mats)
            {
                SetFloatIfExists(material, "_FlipbookBlending", enabled ? 1f : 0f);
                if (enabled) DisableVat(material); // Reuse original enable-Flipbook mutual-exclusion contract.
                // Graph consumes the real Float; no fictitious local Flipbook axis.
            }
            _rootItem.Context?.Refresh();
            return true;
        }

        public void ApplyFlipbookEnabled(bool enabled)
        {
            if (HasGraphTargets()) return; // Graph1A uses native fallback; no legacy projection.
            foreach (Material mat in _rootItem.Mats)
            {
                SetFloatIfExists(mat, "_FlipbookBlending", enabled ? 1f : 0f);

                if (enabled)
                {
                    DisableVat(mat);
                }

                if (NBShaderMaterialIntentResolver.IsNBShaderMaterial(mat))
                    SyncResolvedIntentState(mat);
                else
                    SetKeyword(mat, "_FLIPBOOKBLENDING_ON", enabled);
            }
        }

        public void ApplyBlendMode(BlendMode mode)
        {
            if (HasGraphTargets()) return; // Graph1A uses native fallback; no legacy projection.
            foreach (Material mat in _rootItem.Mats)
            {
                bool nbShaderMaterial = NBShaderMaterialIntentResolver.IsNBShaderMaterial(mat);
                switch (mode)
                {
                    case BlendMode.Alpha:
                        SetIntIfExists(mat, "_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        SetIntIfExists(mat, "_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        if (!nbShaderMaterial)
                        {
                            SetKeyword(mat, "_ALPHAPREMULTIPLY_ON", false);
                            SetKeyword(mat, "_ALPHAMODULATE_ON", false);
                        }
                        break;
                    case BlendMode.Premultiply:
                        SetIntIfExists(mat, "_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        SetIntIfExists(mat, "_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        if (!nbShaderMaterial)
                        {
                            SetKeyword(mat, "_ALPHAPREMULTIPLY_ON", true);
                            SetKeyword(mat, "_ALPHAMODULATE_ON", false);
                        }
                        break;
                    case BlendMode.Additive:
                        SetIntIfExists(mat, "_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        SetIntIfExists(mat, "_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        if (!nbShaderMaterial)
                        {
                            SetKeyword(mat, "_ALPHAPREMULTIPLY_ON", true);
                            SetKeyword(mat, "_ALPHAMODULATE_ON", false);
                        }
                        break;
                    case BlendMode.Multiply:
                        SetIntIfExists(mat, "_SrcBlend", (int)UnityEngine.Rendering.BlendMode.DstColor);
                        SetIntIfExists(mat, "_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        if (!nbShaderMaterial)
                        {
                            SetKeyword(mat, "_ALPHAPREMULTIPLY_ON", false);
                            SetKeyword(mat, "_ALPHAMODULATE_ON", true);
                        }
                        break;
                    case BlendMode.Opaque:
                        SetIntIfExists(mat, "_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        SetIntIfExists(mat, "_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                        if (!nbShaderMaterial)
                        {
                            SetKeyword(mat, "_ALPHAPREMULTIPLY_ON", false);
                            SetKeyword(mat, "_ALPHAMODULATE_ON", false);
                        }
                        break;
                }

                if (nbShaderMaterial)
                    SyncResolvedIntentState(mat);
            }
        }

        public void ApplyLightMode(FxLightMode mode)
        {
            if (HasGraphTargets()) return; // Graph1A uses native fallback; no legacy projection.
            foreach (Material mat in _rootItem.Mats)
            {
                if (NBShaderMaterialIntentResolver.IsNBShaderMaterial(mat))
                    SyncResolvedIntentState(mat);
                else
                    SetLightModeKeyword(mat, mode);
            }
        }

        public void ApplyToggleKeyword(string keyword, bool enabled)
        {
            if (HasGraphTargets()) return; // Graph1A uses native fallback; no legacy projection.
            foreach (Material mat in _rootItem.Mats)
            {
                if (NBShaderFeatureCatalog.IsManagedKeyword(keyword) &&
                    NBShaderMaterialIntentResolver.IsNBShaderMaterial(mat))
                    SyncResolvedIntentState(mat);
                else
                    SetKeyword(mat, keyword, enabled);
            }
        }

        public void ApplyStencilPreset(string key)
        {
            if (HasGraphTargets()) return; // Graph1A uses native fallback; no legacy projection.
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
            SetFloatIfExists(mat, "_StencilFail", (int)UnityEngine.Rendering.StencilOp.Keep);
            SetFloatIfExists(mat, "_StencilZFail", (int)UnityEngine.Rendering.StencilOp.Keep);
            SetFloatIfExists(mat, "_StencilReadMask", 255f);
            SetFloatIfExists(mat, "_StencilWriteMask", 255f);
            SetFloatIfExists(mat, "_StencilKeyIndex", keyIndex);
        }

        private static void SetFloatIfExists(Material mat, string propertyName, float value)
        {
            if (mat != null &&
                mat.HasProperty(propertyName) &&
                !Mathf.Approximately(mat.GetFloat(propertyName), value))
            {
                mat.SetFloat(propertyName, value);
            }
        }

        private static void SetIntIfExists(Material mat, string propertyName, int value)
        {
            if (mat != null &&
                mat.HasProperty(propertyName) &&
                Mathf.RoundToInt(mat.GetFloat(propertyName)) != value)
            {
                mat.SetInt(propertyName, value);
            }
        }

        private static void SetVectorIfExists(Material mat, string propertyName, Vector4 value)
        {
            if (mat != null &&
                mat.HasProperty(propertyName) &&
                mat.GetVector(propertyName) != value)
            {
                mat.SetVector(propertyName, value);
            }
        }

        private static void SetRenderQueueIfNeeded(Material mat, int renderQueue)
        {
            if (mat != null && mat.renderQueue != renderQueue)
            {
                mat.renderQueue = renderQueue;
            }
        }

        private static void SetShaderPassEnabledIfNeeded(Material mat, string passName, bool enabled)
        {
            if (mat != null && !string.IsNullOrEmpty(passName) && mat.GetShaderPassEnabled(passName) != enabled)
            {
                mat.SetShaderPassEnabled(passName, enabled);
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
            bool useBaseMapTexture = mode == MeshSourceMode.UIEffectBaseMap ||
                                     mode == MeshSourceMode.UIParticle;

            SetFlag(flags, NBShaderFlags.FLAG_BIT_PARTICLE_1_IS_PARTICLE_SYSTEM, isParticle, 1);
            SetFlag(flags, NBShaderFlags.FLAG_BIT_PARTICLE_1_UV_FROM_MESH, mode == MeshSourceMode.Mesh, 1);
            SetFlag(flags, NBShaderFlags.FLAG_BIT_PARTICLE_UIEFFECT_ON, isUIEffect, 0);
            SetFlag(flags, NBShaderFlags.FLAG_BIT_PARTICLE_1_UIEFFECT_SPRITE_MODE, mode == MeshSourceMode.UIEffectSprite, 1);
            SetFlag(flags, NBShaderFlags.FLAG_BIT_PARTICLE_1_UIEFFECT_BASEMAP_MODE, useBaseMapTexture, 1);
            if (mode == MeshSourceMode.Particle)
            {
                SetFlag(flags, NBShaderFlags.FLAG_BIT_PARTICLE_1_ANIMATION_SHEET_HELPER, false, 1);
            }

            if (mat.HasProperty("_CustomData"))
            {
                SetFloatIfExists(mat, "_CustomData", isParticle ? 1f : 0f);
            }

            if (isParticle)
            {
                SetKeyword(mat, "_CUSTOMDATA", true);
            }
            else
            {
                SetKeyword(mat, "_CUSTOMDATA", false);
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

        private void SyncTogglePropertyFlags(Material mat, NBShaderFlags flags)
        {
            if (flags == null || mat == null)
            {
                return;
            }

            for (int i = 0; i < ToggleFlagBindings.Length; i++)
            {
                var binding = ToggleFlagBindings[i];
                if (!mat.HasProperty(binding.propertyName))
                {
                    continue;
                }

                SetFlag(flags, binding.flagBits, mat.GetFloat(binding.propertyName) > 0.5f, binding.flagIndex);
            }

            for (int i = 0; i < ModeFlagBindings.Length; i++)
            {
                var binding = ModeFlagBindings[i];
                if (!mat.HasProperty(binding.propertyName))
                {
                    continue;
                }

                SetFlag(flags, binding.flagBits, Mathf.RoundToInt(mat.GetFloat(binding.propertyName)) == binding.enabledMode, binding.flagIndex);
            }
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

            bool shouldEnable = enabled && IsKeywordAllowed(keyword);
            if (mat.IsKeywordEnabled(keyword) == shouldEnable)
            {
                return;
            }

            if (shouldEnable)
            {
                mat.EnableKeyword(keyword);
            }
            else
            {
                mat.DisableKeyword(keyword);
            }

            KeywordVersion++;
        }

        private void SyncResolvedIntentState(Material mat)
        {
            var tier = ResolveMaterialTier(mat);
            var allowedKeywords = NBShaderFeatureLevelProjectSettings.instance.GetAllowedKeywordSetForReadOnlyUse(tier);
            var allowedPassFeatures = NBShaderFeatureLevelProjectSettings.instance.GetAllowedPassFeatureSetForReadOnlyUse(tier);
            var result = NBShaderMaterialIntentResolver.Resolve(mat, tier, allowedKeywords, allowedPassFeatures);
            var effectiveKeywords = new HashSet<string>(result.effectiveKeywords);

            for (int i = 0; i < NBShaderFeatureCatalog.RawKeywords.Length; i++)
            {
                string keyword = NBShaderFeatureCatalog.RawKeywords[i];
                SetKeyword(mat, keyword, effectiveKeywords.Contains(keyword));
            }

            for (int i = 0; i < result.passes.Length; i++)
            {
                NBShaderPassIntent pass = result.passes[i];
                if (!string.IsNullOrEmpty(pass.passName))
                {
                    SetShaderPassEnabledIfNeeded(mat, pass.passName, pass.included);
                }
            }

            SetKeyword(mat, "EVALUATE_SH_VERTEX", effectiveKeywords.Contains("_FX_LIGHT_MODE_SIX_WAY"));
        }

        private void SyncResolvedIntentStateIfNBShader(Material mat)
        {
            if (NBShaderMaterialIntentResolver.IsNBShaderMaterial(mat))
                SyncResolvedIntentState(mat);
        }

        private NBShaderFeatureTier ResolveMaterialTier(Material mat)
        {
            if (mat != null && mat.HasProperty(FeatureTierPropertyName))
            {
                int value = Mathf.RoundToInt(mat.GetFloat(FeatureTierPropertyName));
                if (value >= (int)NBShaderFeatureTier.Low && value <= (int)NBShaderFeatureTier.Ultra)
                {
                    return (NBShaderFeatureTier)value;
                }
            }

            if (_rootItem.Context != null && !_rootItem.Context.CurrentTierMixed)
            {
                return _rootItem.Context.CurrentTier;
            }

            return NBShaderFeatureTier.Ultra;
        }

        private static bool IsResolvedIntentPass(string passName)
        {
            if (string.IsNullOrEmpty(passName))
                return false;

            if (string.Equals(passName, NBShaderPassFeatureCatalog.MainForwardPassName, StringComparison.Ordinal))
                return true;

            string passFeatureId;
            return NBShaderFeatureLevelCatalog.TryGetManagedPassFeatureByPassName(passName, out passFeatureId);
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
                SetFlag(flags, NBShaderFlags.FLAG_BIT_PARTICLE_1_USE_TEXCOORD1, false, 1);
                SetFlag(flags, NBShaderFlags.FLAG_BIT_PARTICLE_1_USE_TEXCOORD2, false, 1);
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

            if (mode != TransparentMode.Transparent)
            {
                SetFloatIfExists(mat, "_TransparentShadowDitherToggle", 0f);
            }

            switch (mode)
            {
                case TransparentMode.Opaque:
                    SetIntIfExists(mat, "_ZWrite", 1);
                    SetFloatIfExists(mat, "_Blend", (float)BlendMode.Opaque);
                    break;
                case TransparentMode.Transparent:
                    SetIntIfExists(mat, "_ZWrite", 0);
                    if (mat.HasProperty("_Blend") && (BlendMode)Mathf.RoundToInt(mat.GetFloat("_Blend")) == BlendMode.Opaque)
                    {
                        SetFloatIfExists(mat, "_Blend", (float)BlendMode.Alpha);
                    }

                    break;
                case TransparentMode.CutOff:
                    SetIntIfExists(mat, "_ZWrite", 1);
                    SetFloatIfExists(mat, "_Blend", (float)BlendMode.Opaque);
                    break;
            }

            if (mat.HasProperty("_ForceZWriteToggle"))
            {
                float forceZWrite = mat.GetFloat("_ForceZWriteToggle");
                if (forceZWrite > 0.5f && forceZWrite < 1.5f)
                {
                    SetIntIfExists(mat, "_ZWrite", 1);
                }
                else if (forceZWrite > 1.5f)
                {
                    SetIntIfExists(mat, "_ZWrite", 0);
                }
            }
        }

        private static void SyncTransparentShadowFlags(Material mat, NBShaderFlags flags)
        {
            TransparentMode mode = mat != null && mat.HasProperty("_TransparentMode")
                ? (TransparentMode)Mathf.RoundToInt(mat.GetFloat("_TransparentMode"))
                : TransparentMode.UnKnowOrMixed;
            bool isTransparent = mode == TransparentMode.Transparent;
            bool useTransparentShadowDither = isTransparent &&
                                              mat.HasProperty("_TransparentShadowDitherToggle") &&
                                              mat.GetFloat("_TransparentShadowDitherToggle") > 0.5f;

            SetFlag(flags, NBShaderFlags.FLAG_BIT_PARTICLE_1_TRANSPARENT_MODE, isTransparent, 1);
            SetFlag(flags, NBShaderFlags.FLAG_BIT_PARTICLE_1_TRANSPARENT_SHADOW_DITHER, useTransparentShadowDither, 1);
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
                SetVectorIfExists(mat, "_ParallaxMapping_Vec", value);
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
                    SetIntIfExists(mat, "_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    SetIntIfExists(mat, "_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    break;
                case BlendMode.Premultiply:
                    SetIntIfExists(mat, "_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    SetIntIfExists(mat, "_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    break;
                case BlendMode.Additive:
                    SetIntIfExists(mat, "_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    SetIntIfExists(mat, "_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    break;
                case BlendMode.Multiply:
                    SetIntIfExists(mat, "_SrcBlend", (int)UnityEngine.Rendering.BlendMode.DstColor);
                    SetIntIfExists(mat, "_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    break;
                case BlendMode.Opaque:
                    SetIntIfExists(mat, "_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    SetIntIfExists(mat, "_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    break;
            }
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
            SetFloatIfExists(mat, "_VAT_Toggle", 0f);

            ClearVatKeywords(mat);
        }

        private void DisableFlipbook(Material mat)
        {
            SetFloatIfExists(mat, "_FlipbookBlending", 0f);

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
            SetExclusiveKeyword(mat, HoudiniVatKeywords, enabledIndex);
        }

        private void SetTyflowVATKeyword(Material mat, int enabledIndex)
        {
            SetExclusiveKeyword(mat, TyflowVatKeywords, enabledIndex);
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
            if (flags == null ||
                flags.material == null ||
                flags.CheckFlagBits(flagBits, index: index) == enabled)
            {
                return;
            }

            if (enabled)
            {
                flags.SetFlagBits(flagBits, index: index);
            }
            else
            {
                flags.ClearFlagBits(flagBits, index: index);
            }
        }

        private struct FlagToggleBinding
        {
            public readonly string propertyName;
            public readonly int flagBits;
            public readonly int flagIndex;

            public FlagToggleBinding(string propertyName, int flagBits, int flagIndex)
            {
                this.propertyName = propertyName;
                this.flagBits = flagBits;
                this.flagIndex = flagIndex;
            }
        }

        private struct FlagModeBinding
        {
            public readonly string propertyName;
            public readonly int flagBits;
            public readonly int flagIndex;
            public readonly int enabledMode;

            public FlagModeBinding(string propertyName, int flagBits, int flagIndex, int enabledMode)
            {
                this.propertyName = propertyName;
                this.flagBits = flagBits;
                this.flagIndex = flagIndex;
                this.enabledMode = enabledMode;
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
