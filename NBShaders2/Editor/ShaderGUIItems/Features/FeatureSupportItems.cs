using System;
using NBShader;
using UnityEditor;
using UnityEngine;

namespace NBShaderEditor
{
    internal sealed class NoiseAffectItem : ShaderGUIItem
    {
        private readonly NBShaderRootItem _nbRootItem;

        public NoiseAffectItem(NBShaderRootItem rootItem, ShaderGUIItem parentItem)
            : base(rootItem, parentItem)
        {
            _nbRootItem = rootItem;
        }

        public override void OnGUI()
        {
            bool previousMixedValue = EditorGUI.showMixedValue;
            bool noiseEnabledHasMixedValue = _nbRootItem.Context.NoiseEnabled == MixedBool.Mixed;
            using (new InheritedControlDisabledScope(_nbRootItem.Context.NoiseEnabled == MixedBool.False))
            {
                for (int i = 0; i < ChildrenItemList.Count; i++)
                {
                    EditorGUI.showMixedValue = noiseEnabledHasMixedValue;
                    ChildrenItemList[i].OnGUI();
                }
            }

            EditorGUI.showMixedValue = previousMixedValue;
        }

        public override void CheckIsPropertyModified(bool isCallByChild = false)
        {
            HasModified = false;
            for (int i = 0; i < ChildrenItemList.Count; i++)
            {
                HasModified |= ChildrenItemList[i].HasModified;
            }

            ParentItem?.CheckIsPropertyModified(true);
        }
    }

    internal sealed class FlipbookFeatureItem : ToggleItem
    {
        private readonly NBShaderRootItem _nbRootItem;

        public FlipbookFeatureItem(NBShaderRootItem rootItem, ShaderGUIItem parentItem)
            : base(
                rootItem,
                parentItem,
                "_FlipbookBlending",
                () => FeatureToggleFoldOutItem.Content("序列帧融帧(丝滑)"),
                rootItem.SyncService.ApplyFlipbookEnabled)
        {
            _nbRootItem = rootItem;
        }

        public override void DrawBlock()
        {
            if (PropertyInfo.Property.hasMixedValue || PropertyInfo.Property.floatValue <= 0.5f)
            {
                return;
            }

            if (_nbRootItem.Context.MeshSourceMode == MeshSourceMode.Particle ||
                _nbRootItem.Context.MeshSourceMode == MeshSourceMode.UIParticle)
            {
                if (HasSpecialUVChannel())
                {
                    using (ParentControlDisabledScope())
                    {
                        EditorGUILayout.HelpBox(
                            FeatureToggleFoldOutItem.Text(
                                "feature.flipbook.specialUvWarning.message",
                                "序列帧融帧和特殊UV通道同时开启，粒子序列帧应该影响UV0和UV1两个通道，特殊通道只能使用UV3（原始UV）"),
                            MessageType.Warning);
                    }
                }
                else
                {
                    using (ParentControlDisabledScope())
                    {
                        EditorGUILayout.HelpBox(
                            FeatureToggleFoldOutItem.Text(
                                "feature.flipbook.particleInfo.message",
                                "AnimationSheet的AffectUVChannel需要有UV0和UV1"),
                            MessageType.Info);
                    }
                }

                return;
            }

            if (_nbRootItem.Context.MeshSourceMode == MeshSourceMode.Mesh)
            {
                using (ParentControlDisabledScope())
                {
                    EditorGUILayout.HelpBox(
                        FeatureToggleFoldOutItem.Text(
                            "feature.flipbook.meshInfo.message",
                            "需要添加AnimationSheetHelper脚本"),
                        MessageType.Info);
                }
            }
        }

        private bool HasSpecialUVChannel()
        {
            for (int i = 0; i < _nbRootItem.ShaderFlags.Count; i++)
            {
                if (_nbRootItem.ShaderFlags[i] is W9ParticleShaderFlags flags &&
                    flags.CheckIsUVModeOn(W9ParticleShaderFlags.UVMode.SpecialUVChannel))
                {
                    return true;
                }
            }

            return false;
        }
    }

    internal sealed class VatFrameCustomDataItem : CustomDataSelectItem
    {
        private readonly Func<bool> _isVisible;
        private readonly Func<bool> _anyVatFrameCustomDataVisible;

        public VatFrameCustomDataItem(
            NBShaderRootItem rootItem,
            ShaderGUIItem parentItem,
            Func<GUIContent> contentProvider,
            Func<bool> isVisible,
            Func<bool> anyVatFrameCustomDataVisible)
            : base(rootItem, parentItem, W9ParticleShaderFlags.FLAGBIT_POS_2_CUSTOMDATA_VAT_FRAME, 2, contentProvider)
        {
            _isVisible = isVisible;
            _anyVatFrameCustomDataVisible = anyVatFrameCustomDataVisible;
        }

        public override void OnGUI()
        {
            if (_isVisible != null && !_isVisible())
            {
                if (_anyVatFrameCustomDataVisible == null || !_anyVatFrameCustomDataVisible())
                {
                    ClearVatFrameCustomData();
                }

                return;
            }

            base.OnGUI();
        }

        private void ClearVatFrameCustomData()
        {
            for (int i = 0; i < RootItem.ShaderFlags.Count; i++)
            {
                if (RootItem.ShaderFlags[i] is W9ParticleShaderFlags flags)
                {
                    flags.SetCustomDataFlag(
                        W9ParticleShaderFlags.CutomDataComponent.Off,
                        W9ParticleShaderFlags.FLAGBIT_POS_2_CUSTOMDATA_VAT_FRAME,
                        2);
                }
            }
        }
    }
}
