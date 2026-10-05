using System;
using NBShader;
using UnityEditor;
using UnityEngine;

namespace NBShaderEditor
{
    internal sealed class VatFeatureItem : FeatureToggleFoldOutItem
    {
        private static readonly string[] VatModeNames = { "Houdini", "TyFlow" };
        private static readonly string[] HoudiniVatSubModeNames =
        {
            "SoftBody (Deformation)",
            "RigidBody (Pieces)",
            "Dynamic Remeshing (Lookup)",
            "Particle Sprites (Billboard)"
        };
        private static readonly string[] TyFlowVatSubModeNames =
        {
            "Absolute positions",
            "Relative offsets",
            "Skin (R)",
            "Skin (PR)",
            "Skin (PRSAVE)",
            "Skin (PRSXYZ)"
        };

        public VatFeatureItem(NBShaderRootItem rootItem, ShaderGUIItem parentItem, bool graphSharedMode=false)
            : base(rootItem, parentItem, "_VATBlockFoldOut", "_VAT_Toggle", "VAT顶点动画图", keyword: graphSharedMode?null:"_VAT", onValueChanged: graphSharedMode?null:(System.Action<bool>)rootItem.SyncService.ApplyVatEnabled, graphVATToggleEdit:graphSharedMode)
        {
            new FeaturePopupItem(rootItem, this, "_VATMode", () => Content("VAT模式"), VatModeNames, _ => {if(graphSharedMode)rootItem.SyncService.TryApplyGraphVATIntentEdit();else rootItem.SyncService.SyncMaterialState();});
            Func<bool> isHoudini = () => IsPropertyMode(rootItem, "_VATMode", (int)VATMode.Houdini);
            Func<bool> isTyflow = () => IsPropertyMode(rootItem, "_VATMode", (int)VATMode.Tyflow);
            Func<bool> hasVatFrameCustomData = () => graphSharedMode?IsGraphVatFrameCustomDataVisible(rootItem):IsVatFrameCustomDataVisible(rootItem);
            ShaderGUIFloatItem floatItem;

            new FeaturePopupItem(rootItem, this, "_HoudiniVATSubMode", () => Content("Houdini VAT Sub Mode"), HoudiniVatSubModeNames,
                _ => {if(graphSharedMode)rootItem.SyncService.TryApplyGraphVATIntentEdit();else rootItem.SyncService.SyncMaterialState();}, isHoudini);
            new HelpBoxItem(rootItem, this, () => Text("feature.vat.houdiniUnsupportedParticle.message", "该 Houdini VAT 类型需要 Mesh 多 UV 数据，不支持 ParticleSystem VertexStream 模式。"), MessageType.Warning,
                () => isHoudini() && HasUnsupportedHoudiniParticleMode(rootItem));
            new SectionLabelItem(rootItem, this, () => Content("Playback"), isHoudini);
            new ToggleItem(rootItem, this, "_B_autoPlayback", () => Content("Auto Playback"), isVisible: isHoudini) {WriteOnlyOnInteractiveChange=graphSharedMode};
            floatItem = new ShaderGUIFloatItem(rootItem, this, () => isHoudini() && ShouldDrawWhenFloatOff(rootItem, "_B_autoPlayback"))
            {
                PropertyName = "_displayFrame",
                GuiContent = Content("Display Frame")
            };
            floatItem.InitTriggerByChild();
            new VatFrameCustomDataItem(rootItem, this, () => Content("VAT Frame CustomData"), () => isHoudini() && hasVatFrameCustomData(), hasVatFrameCustomData,graphSharedMode);
            floatItem = new ShaderGUIFloatItem(rootItem, this, isHoudini)
            {
                PropertyName = "_gameTimeAtFirstFrame",
                GuiContent = Content("Game Time at First Frame")
            };
            floatItem.InitTriggerByChild();
            floatItem = new ShaderGUIFloatItem(rootItem, this, isHoudini)
            {
                PropertyName = "_playbackSpeed",
                GuiContent = Content("Playback Speed")
            };
            floatItem.InitTriggerByChild();
            floatItem = new ShaderGUIFloatItem(rootItem, this, isHoudini)
            {
                PropertyName = "_houdiniFPS",
                GuiContent = Content("Houdini FPS")
            };
            floatItem.InitTriggerByChild();
            new ToggleItem(rootItem, this, "_B_interpolate", () => Content("Interframe Interpolation"), isVisible: isHoudini) {WriteOnlyOnInteractiveChange=graphSharedMode};
            new ToggleItem(rootItem, this, "_animateFirstFrame", () => Content("Animate First Frame"), isVisible: () => isHoudini() && IsPropertyMode(rootItem, "_HoudiniVATSubMode", 1)) {WriteOnlyOnInteractiveChange=graphSharedMode};
            floatItem = new ShaderGUIFloatItem(rootItem, this, isHoudini)
            {
                PropertyName = "_frameCount",
                GuiContent = Content("Frame Count")
            };
            floatItem.InitTriggerByChild();

            new SectionLabelItem(rootItem, this, () => Content("Bounds Metadata"), isHoudini);
            floatItem = new ShaderGUIFloatItem(rootItem, this, isHoudini)
            {
                PropertyName = "_boundMinX",
                GuiContent = Content("Bound Min X")
            };
            floatItem.InitTriggerByChild();
            floatItem = new ShaderGUIFloatItem(rootItem, this, isHoudini)
            {
                PropertyName = "_boundMinY",
                GuiContent = Content("Bound Min Y")
            };
            floatItem.InitTriggerByChild();
            floatItem = new ShaderGUIFloatItem(rootItem, this, isHoudini)
            {
                PropertyName = "_boundMinZ",
                GuiContent = Content("Bound Min Z")
            };
            floatItem.InitTriggerByChild();
            floatItem = new ShaderGUIFloatItem(rootItem, this, isHoudini)
            {
                PropertyName = "_boundMaxX",
                GuiContent = Content("Bound Max X")
            };
            floatItem.InitTriggerByChild();
            floatItem = new ShaderGUIFloatItem(rootItem, this, isHoudini)
            {
                PropertyName = "_boundMaxY",
                GuiContent = Content("Bound Max Y")
            };
            floatItem.InitTriggerByChild();
            floatItem = new ShaderGUIFloatItem(rootItem, this, isHoudini)
            {
                PropertyName = "_boundMaxZ",
                GuiContent = Content("Bound Max Z")
            };
            floatItem.InitTriggerByChild();

            new SectionLabelItem(rootItem, this, () => Content("Textures"), isHoudini);
            new TextureItem(rootItem, this, "_posTexture", () => Content("Position Texture"), drawScaleOffset: false, isVisible: isHoudini);
            new TextureItem(rootItem, this, "_posTexture2", () => Content("Position Texture 2"), drawScaleOffset: false,
                isVisible: () => isHoudini() && ShouldDrawWhenFloatOn(rootItem, "_B_LOAD_POS_TWO_TEX"));
            new TextureItem(rootItem, this, "_rotTexture", () => Content("Rotation Texture"), drawScaleOffset: false,
                isVisible: () => isHoudini() && ShouldDrawHoudiniRotationTexture(rootItem));
            new TextureItem(rootItem, this, "_colTexture", () => Content("Color Texture"), drawScaleOffset: false,
                isVisible: () => isHoudini() && ShouldDrawWhenFloatOn(rootItem, "_B_LOAD_COL_TEX"));
            new TextureItem(rootItem, this, "_lookupTable", () => Content("Lookup Table"), drawScaleOffset: false,
                isVisible: () => isHoudini() && (IsPropertyMode(rootItem, "_HoudiniVATSubMode", 2) || ShouldDrawWhenFloatOn(rootItem, "_B_LOAD_LOOKUP_TABLE")));

            new SectionLabelItem(rootItem, this, () => Content("Scale"), () => isHoudini() && IsPropertyMode(rootItem, "_HoudiniVATSubMode", 1, 3));
            floatItem = new ShaderGUIFloatItem(rootItem, this, () => isHoudini() && IsPropertyMode(rootItem, "_HoudiniVATSubMode", 1, 3))
            {
                PropertyName = "_globalPscaleMul",
                GuiContent = Content("Global Piece Scale Multiplier")
            };
            floatItem.InitTriggerByChild();
            new ToggleItem(rootItem, this, "_B_pscaleAreInPosA", () => Content("Piece Scales in Position Alpha"), isVisible: () => isHoudini() && IsPropertyMode(rootItem, "_HoudiniVATSubMode", 1, 3)) {WriteOnlyOnInteractiveChange=graphSharedMode};

            new SectionLabelItem(rootItem, this, () => Content("Particle Sprite"), () => isHoudini() && IsPropertyMode(rootItem, "_HoudiniVATSubMode", 3));
            floatItem = new ShaderGUIFloatItem(rootItem, this, () => isHoudini() && IsPropertyMode(rootItem, "_HoudiniVATSubMode", 3))
            {
                PropertyName = "_widthBaseScale",
                GuiContent = Content("Width Base Scale")
            };
            floatItem.InitTriggerByChild();
            floatItem = new ShaderGUIFloatItem(rootItem, this, () => isHoudini() && IsPropertyMode(rootItem, "_HoudiniVATSubMode", 3))
            {
                PropertyName = "_heightBaseScale",
                GuiContent = Content("Height Base Scale")
            };
            floatItem.InitTriggerByChild();
            new ToggleItem(rootItem, this, "_B_hideOverlappingOrigin", () => Content("Hide Overlapping Origin"), isVisible: () => isHoudini() && IsPropertyMode(rootItem, "_HoudiniVATSubMode", 3)) {WriteOnlyOnInteractiveChange=graphSharedMode};
            floatItem = new ShaderGUIFloatItem(rootItem, this, () => isHoudini() && IsPropertyMode(rootItem, "_HoudiniVATSubMode", 3) && ShouldDrawWhenFloatOn(rootItem, "_B_hideOverlappingOrigin"))
            {
                PropertyName = "_originRadius",
                GuiContent = Content("Origin Effective Radius")
            };
            floatItem.InitTriggerByChild();
            new ToggleItem(rootItem, this, "_B_CAN_SPIN", () => Content("Particles Can Spin"), isVisible: () => isHoudini() && IsPropertyMode(rootItem, "_HoudiniVATSubMode", 3)) {WriteOnlyOnInteractiveChange=graphSharedMode};
            new ToggleItem(rootItem, this, "_B_spinFromHeading", () => Content("Compute Spin from Heading"), isVisible: () => isHoudini() && IsPropertyMode(rootItem, "_HoudiniVATSubMode", 3) && ShouldDrawWhenFloatOn(rootItem, "_B_CAN_SPIN")) {WriteOnlyOnInteractiveChange=graphSharedMode};
            floatItem = new ShaderGUIFloatItem(rootItem, this, () => isHoudini() && IsPropertyMode(rootItem, "_HoudiniVATSubMode", 3) && ShouldDrawWhenFloatOn(rootItem, "_B_CAN_SPIN") && ShouldDrawWhenFloatOff(rootItem, "_B_spinFromHeading"))
            {
                PropertyName = "_spinPhase",
                GuiContent = Content("Particle Spin Phase")
            };
            floatItem.InitTriggerByChild();
            floatItem = new ShaderGUIFloatItem(rootItem, this, () => isHoudini() && IsPropertyMode(rootItem, "_HoudiniVATSubMode", 3) && ShouldDrawWhenFloatOn(rootItem, "_B_CAN_SPIN"))
            {
                PropertyName = "_scaleByVelAmount",
                GuiContent = Content("Scale by Velocity Amount")
            };
            floatItem.InitTriggerByChild();
            floatItem = new ShaderGUIFloatItem(rootItem, this, () => isHoudini() && IsPropertyMode(rootItem, "_HoudiniVATSubMode", 3))
            {
                PropertyName = "_particleTexUScale",
                GuiContent = Content("Particle Texture U Scale")
            };
            floatItem.InitTriggerByChild();
            floatItem = new ShaderGUIFloatItem(rootItem, this, () => isHoudini() && IsPropertyMode(rootItem, "_HoudiniVATSubMode", 3))
            {
                PropertyName = "_particleTexVScale",
                GuiContent = Content("Particle Texture V Scale")
            };
            floatItem.InitTriggerByChild();

            new SectionLabelItem(rootItem, this, () => Content("Flags"), isHoudini);
            new ToggleItem(rootItem, this, "_B_LOAD_POS_TWO_TEX", () => Content("Positions Require Two Textures"), isVisible: isHoudini) {WriteOnlyOnInteractiveChange=graphSharedMode};
            new ToggleItem(rootItem, this, "_B_UNLOAD_ROT_TEX", () => Content("Use Compressed Normals (no rotTex)"), isVisible: () => isHoudini() && ShouldDrawCompressedNormalsToggle(rootItem)) {WriteOnlyOnInteractiveChange=graphSharedMode};
            new ToggleItem(rootItem, this, "_B_LOAD_COL_TEX", () => Content("Load Color Texture"), isVisible: isHoudini) {WriteOnlyOnInteractiveChange=graphSharedMode};
            new ToggleItem(rootItem, this, "_B_LOAD_LOOKUP_TABLE", () => Content("Load Lookup Table"), isVisible: () => isHoudini() && IsPropertyMode(rootItem, "_HoudiniVATSubMode", 2)) {WriteOnlyOnInteractiveChange=graphSharedMode};

            new TextureItem(rootItem, this, "_VATTex", () => Content("VAT texture"), drawScaleOffset: false, isVisible: isTyflow);
            floatItem = new ShaderGUIFloatItem(rootItem, this, isTyflow)
            {
                PropertyName = "_ImportScale",
                GuiContent = Content("ImportScale")
            };
            floatItem.InitTriggerByChild();
            new FeaturePopupItem(rootItem, this, "_TyFlowVATSubMode", () => Content("TyFlow VAT Sub Mode"), TyFlowVatSubModeNames,
                _ => {if(graphSharedMode)rootItem.SyncService.TryApplyGraphVATIntentEdit();else rootItem.SyncService.SyncMaterialState();}, isTyflow);
            new HelpBoxItem(rootItem, this, () => Text("feature.vat.tyflowUnsupportedParticle.message", "该 TyFlow VAT 类型需要 Mesh 多 UV 数据，不支持 ParticleSystem VertexStream 模式。"), MessageType.Warning,
                () => isTyflow() && HasUnsupportedTyflowParticleMode(rootItem));
            new HelpBoxItem(rootItem, this, () => Text("feature.vat.tyflowUv2Conflict.message", "TyFlow VAT uses UV2 (TEXCOORD0.zw) as vertexIndex / vertexCount in ParticleSystem mode. Flipbook blending or Special UV (UV2) conflicts with it; VAT takes priority."), MessageType.Warning,
                () => isTyflow() && !HasUnsupportedTyflowParticleMode(rootItem) && HasTyflowParticleUV2Conflict(rootItem));
            new ToggleItem(rootItem, this, "_DeformingSkin", () => Content("Deforming skin"), isVisible: isTyflow) {WriteOnlyOnInteractiveChange=graphSharedMode};
            floatItem = new ShaderGUIFloatItem(rootItem, this, isTyflow)
            {
                PropertyName = "_SkinBoneCount",
                GuiContent = Content("Skin bone count")
            };
            floatItem.InitTriggerByChild();
            new ToggleItem(rootItem, this, "_RGBAEncoded", () => Content("RGBA encoded"), isVisible: isTyflow) {WriteOnlyOnInteractiveChange=graphSharedMode};
            new ToggleItem(rootItem, this, "_RGBAHalf", () => Content("RGBA half"), isVisible: isTyflow) {WriteOnlyOnInteractiveChange=graphSharedMode};
            new ToggleItem(rootItem, this, "_LinearToGamma", () => Content("Gamma correction"), isVisible: isTyflow) {WriteOnlyOnInteractiveChange=graphSharedMode};
            new ToggleItem(rootItem, this, "_VATIncludesNormals", () => Content("VAT includes normals"), isVisible: isTyflow) {WriteOnlyOnInteractiveChange=graphSharedMode};
            floatItem = new ShaderGUIFloatItem(rootItem, this, isTyflow)
            {
                PropertyName = "_Frame",
                GuiContent = Content("Frame")
            };
            floatItem.InitTriggerByChild();
            new VatFrameCustomDataItem(rootItem, this, () => Content("VAT Frame CustomData"), () => isTyflow() && hasVatFrameCustomData(), hasVatFrameCustomData,graphSharedMode);
            floatItem = new ShaderGUIFloatItem(rootItem, this, isTyflow)
            {
                PropertyName = "_Frames",
                GuiContent = Content("Frames")
            };
            floatItem.InitTriggerByChild();
            new ToggleItem(rootItem, this, "_FrameInterpolation", () => Content("Frame interpolation"), isVisible: isTyflow) {WriteOnlyOnInteractiveChange=graphSharedMode};
            new ToggleItem(rootItem, this, "_Loop", () => Content("Loop"), isVisible: isTyflow) {WriteOnlyOnInteractiveChange=graphSharedMode};
            new ToggleItem(rootItem, this, "_InterpolateLoop", () => Content("Interpolate loop"), isVisible: isTyflow) {WriteOnlyOnInteractiveChange=graphSharedMode};
            new ToggleItem(rootItem, this, "_Autoplay", () => Content("Autoplay"), isVisible: isTyflow) {WriteOnlyOnInteractiveChange=graphSharedMode};
            floatItem = new ShaderGUIFloatItem(rootItem, this, isTyflow)
            {
                PropertyName = "_AutoplaySpeed",
                GuiContent = Content("AutoplaySpeed")
            };
            floatItem.InitTriggerByChild();
            InitTriggerByChild();
        }

        private static bool IsGraphVatFrameCustomDataVisible(NBShaderRootItem root)
        {
            if(root.Mats==null)return false;
            foreach(Material material in root.Mats)
            {
                if(material==null||!material.HasProperty("_VAT_Toggle")||material.GetFloat("_VAT_Toggle")<=.5f)continue;
                if(Mathf.RoundToInt(material.GetFloat("_VATMode"))==1)return true; // Tyflow's actual FrameCD consumer also runs for Mesh/skin.
                var flags=new NBShaderFlags(material);
                if(flags.CheckFlagBits(NBShaderFlags.FLAG_BIT_PARTICLE_1_IS_PARTICLE_SYSTEM,index:1))return true;
            }
            return false;
        }

        private static bool ShouldDrawWhenFloatOn(NBShaderRootItem rootItem, string propertyName)
        {
            if (!rootItem.PropertyInfoDic.TryGetValue(propertyName, out ShaderPropertyInfo info))
            {
                return false;
            }

            return info.Property.hasMixedValue || info.Property.floatValue > 0.5f;
        }

        private static bool ShouldDrawWhenFloatOff(NBShaderRootItem rootItem, string propertyName)
        {
            if (!rootItem.PropertyInfoDic.TryGetValue(propertyName, out ShaderPropertyInfo info))
            {
                return false;
            }

            return info.Property.hasMixedValue || info.Property.floatValue <= 0.5f;
        }

        private static bool TryGetPropertyMode(NBShaderRootItem rootItem, string propertyName, out int mode)
        {
            mode = 0;
            if (!rootItem.PropertyInfoDic.TryGetValue(propertyName, out ShaderPropertyInfo info) ||
                info.Property.hasMixedValue)
            {
                return false;
            }

            mode = Mathf.RoundToInt(info.Property.floatValue);
            return true;
        }

        private static bool ShouldDrawHoudiniRotationTexture(NBShaderRootItem rootItem)
        {
            return TryGetPropertyMode(rootItem, "_HoudiniVATSubMode", out int subMode) &&
                   subMode != 3 &&
                   ShouldDrawWhenFloatOff(rootItem, "_B_UNLOAD_ROT_TEX");
        }

        private static bool ShouldDrawCompressedNormalsToggle(NBShaderRootItem rootItem)
        {
            return TryGetPropertyMode(rootItem, "_HoudiniVATSubMode", out int subMode) && subMode != 3;
        }

        private static bool IsVatFrameCustomDataVisible(NBShaderRootItem rootItem)
        {
            return IsAnyHoudiniParticleModeEnabled(rootItem) || IsAnyTyflowParticleModeEnabled(rootItem);
        }

        private static bool HasTyflowParticleUV2Conflict(NBShaderRootItem rootItem)
        {
            if (rootItem.Mats == null || rootItem.ShaderFlags == null)
            {
                return false;
            }

            int count = Mathf.Min(rootItem.Mats.Count, rootItem.ShaderFlags.Count);
            for (int i = 0; i < count; i++)
            {
                Material mat = rootItem.Mats[i];
                if (!IsTyflowParticleModeEnabled(mat) ||
                    !(rootItem.ShaderFlags[i] is NBShaderFlags flags))
                {
                    continue;
                }

                bool flipbook = mat.IsKeywordEnabled("_FLIPBOOKBLENDING_ON");
                bool specialUVUsesUV2 = flags.CheckIsUVModeOn(NBShaderFlags.UVMode.SpecialUVChannel) &&
                                         !flags.CheckFlagBits(NBShaderFlags.FLAG_BIT_PARTICLE_1_USE_TEXCOORD2, index: 1);
                if (flipbook || specialUVUsesUV2)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsAnyHoudiniParticleModeEnabled(NBShaderRootItem rootItem)
        {
            if (rootItem.Mats == null)
            {
                return false;
            }

            for (int i = 0; i < rootItem.Mats.Count; i++)
            {
                if (IsHoudiniParticleModeEnabled(rootItem.Mats[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsAnyTyflowParticleModeEnabled(NBShaderRootItem rootItem)
        {
            if (rootItem.Mats == null)
            {
                return false;
            }

            for (int i = 0; i < rootItem.Mats.Count; i++)
            {
                if (IsTyflowParticleModeEnabled(rootItem.Mats[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasUnsupportedHoudiniParticleMode(NBShaderRootItem rootItem)
        {
            if (rootItem.Mats == null)
            {
                return false;
            }

            for (int i = 0; i < rootItem.Mats.Count; i++)
            {
                Material mat = rootItem.Mats[i];
                if (IsHoudiniParticleModeEnabled(mat) &&
                    GetMaterialInt(mat, "_HoudiniVATSubMode", 0) == 1)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasUnsupportedTyflowParticleMode(NBShaderRootItem rootItem)
        {
            if (rootItem.Mats == null)
            {
                return false;
            }

            for (int i = 0; i < rootItem.Mats.Count; i++)
            {
                Material mat = rootItem.Mats[i];
                if (IsVatParticleMode(mat, (int)VATMode.Tyflow) &&
                    GetMaterialInt(mat, "_TyFlowVATSubMode", 0) >= 2)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsHoudiniParticleModeEnabled(Material mat)
        {
            return IsVatParticleMode(mat, (int)VATMode.Houdini);
        }

        private static bool IsTyflowParticleModeEnabled(Material mat)
        {
            return IsVatParticleMode(mat, (int)VATMode.Tyflow) &&
                   (!mat.HasProperty("_TyFlowVATSubMode") || Mathf.RoundToInt(mat.GetFloat("_TyFlowVATSubMode")) <= 1);
        }

        private static bool IsVatParticleMode(Material mat, int vatMode)
        {
            if (mat == null ||
                !mat.HasProperty("_VAT_Toggle") ||
                !mat.HasProperty("_VATMode") ||
                !mat.HasProperty("_MeshSourceMode") ||
                mat.GetFloat("_VAT_Toggle") <= 0.5f ||
                Mathf.RoundToInt(mat.GetFloat("_VATMode")) != vatMode)
            {
                return false;
            }

            MeshSourceMode meshSourceMode = (MeshSourceMode)Mathf.RoundToInt(mat.GetFloat("_MeshSourceMode"));
            return meshSourceMode == MeshSourceMode.Particle ||
                   meshSourceMode == MeshSourceMode.UIParticle;
        }

        private static int GetMaterialInt(Material mat, string propertyName, int defaultValue)
        {
            return mat != null && mat.HasProperty(propertyName)
                ? Mathf.RoundToInt(mat.GetFloat(propertyName))
                : defaultValue;
        }
    }
}
