using System;
using System.Collections.Generic;
using UnityEngine;
namespace NBShaderEditor
{
     public class ModeBigBlockItem : BigBlockItem
    {
        private readonly NBShaderRootItem _nbRootItem;

        public ModeBigBlockItem(NBShaderRootItem rootItem, ShaderGUIItem parentItem)
            : base(
                rootItem,
                parentItem,
                "_BigBlockModeSettingFoldOut",
                () => NBShaderInspectorLocalization.MakeContent(
                    "inspector.block.mode.label",
                    "模式设置",
                    "inspector.block.mode.tip",
                    "各种基础模式设置"))
        {
            _nbRootItem = rootItem;
            _meshModePopUp = new MeshModePopUp(rootItem, this);
            _transparentMode = new TransparentModePopUp(rootItem, this);
            base.InitTriggerByChild();
        }

        private MeshModePopUp _meshModePopUp;
        private TransparentModePopUp _transparentMode;

        public override void DrawBlock()
        {
            _meshModePopUp.OnGUI();
            _transparentMode.OnGUI();
        }
    }
    public enum MeshSourceMode
    {
        Particle,
        Mesh,
        UIEffectRawImage,
        UIEffectSprite,
        UIEffectBaseMap,
        UIParticle,
        UnKnowOrMixed = -1
    }

    public class MeshModePopUp : ShaderGUIPopUpItem,IDisposable
    {
        public MeshSourceMode MeshSourceMode; 
        public static Dictionary<ShaderGUIRootItem,MeshModePopUp> MeshSourceModeDic = new Dictionary<ShaderGUIRootItem,MeshModePopUp>();
        public MeshModePopUp(ShaderGUIRootItem rootItem, ShaderGUIItem parentItem) :
            base(rootItem, parentItem: parentItem)
        {
            PopUpNames = NBShaderInspectorLocalization.GetInspectorOptions("mode.meshSource", MeshSourceOptions);
            PropertyName = "_MeshSourceMode";
            GuiContent = NBShaderInspectorLocalization.MakeInspectorContent("mode.meshSource", "Mesh Source Mode", "Matches the current object type.");
            MeshSourceModeDic[rootItem] = this;
            InitTriggerByChild();
        }

        public MixedBool UIEffectEnabled()
        {
            if ((int)MeshSourceMode >= 2)
            {
                return MixedBool.True;
            }
            else if (MeshSourceMode == MeshSourceMode.UnKnowOrMixed)
            {
                return MixedBool.Mixed;
            }
            else
            {
                return MixedBool.False;
            }
        }

        public override void OnGUI()
        {
            PopUpNames = NBShaderInspectorLocalization.GetInspectorOptions("mode.meshSource", MeshSourceOptions);
            GuiContent = NBShaderInspectorLocalization.MakeInspectorContent("mode.meshSource", "Mesh Source Mode", "Matches the current object type.");
            base.OnGUI();
            if (PropertyInfo.Property.hasMixedValue)
            {
                MeshSourceMode = MeshSourceMode.UnKnowOrMixed;
            }
            else
            {
                MeshSourceMode = (MeshSourceMode)PropertyInfo.Property.floatValue;
            }
        }

        public override void OnEndChange()
        {
            base.OnEndChange();
            if (RootItem is NBShaderRootItem nbRootItem)
            {
                nbRootItem.Context.Refresh();
            }
        }

        public void Dispose()
        {
            MeshSourceModeDic.Remove(RootItem);
        }

        private static readonly string[] MeshSourceOptions =
        {
            "粒子系统",
            "模型（非粒子发射）",
            "2D RawImage",
            "2D 精灵",
            "2D 材质贴图",
            "2D UIParticle"
        };
    }
    
    public enum TransparentMode
    {
        Opaque = 0,
        Transparent = 1,
        CutOff = 2,
        UnKnowOrMixed = -1
    }

    public class TransparentModePopUp : ShaderGUIPopUpItem,IDisposable
    {

        public TransparentMode TransparentMode;
        public static Dictionary<ShaderGUIRootItem,TransparentModePopUp> TransparentModeDic =
            new Dictionary<ShaderGUIRootItem, TransparentModePopUp>();
        public TransparentModePopUp(ShaderGUIRootItem rootItem, ShaderGUIItem parentItem) :
            base(rootItem, parentItem: parentItem)
        {
            PopUpNames = NBShaderInspectorLocalization.GetInspectorOptions("mode.transparent", TransparentOptions);
            PropertyName = "_TransparentMode";
            GuiContent = NBShaderInspectorLocalization.MakeInspectorContent("mode.transparent", "Transparent Mode", "Controls opaque, transparent, and cutoff rendering.");
            TransparentModeDic[RootItem] = this;
            _cutOffSlider = new ShaderGUISliderItem(RootItem, this)
            {
                PropertyName = "_Cutoff",
                GuiContent = NBShaderInspectorLocalization.MakeInspectorContent("mode.cutoff", "Cutoff", "0 keeps everything, 1 clips everything.")
            };
            _cutOffSlider.InitTriggerByChild();
            _blendPopUp = new BlendPopUp(RootItem, this);
            PropertyInfo = RootItem.PropertyInfoDic[PropertyName];
            TransparentMode = (TransparentMode)PropertyInfo.Property.floatValue;
            InitTriggerByChild();
        }


        public override void OnGUI()
        {
            PopUpNames = NBShaderInspectorLocalization.GetInspectorOptions("mode.transparent", TransparentOptions);
            GuiContent = NBShaderInspectorLocalization.MakeInspectorContent("mode.transparent", "Transparent Mode", "Controls opaque, transparent, and cutoff rendering.");
            _cutOffSlider.GuiContent = NBShaderInspectorLocalization.MakeInspectorContent("mode.cutoff", "Cutoff", "0 keeps everything, 1 clips everything.");
            base.OnGUI();
            if (PropertyInfo.Property.hasMixedValue)
            {
                TransparentMode = TransparentMode.UnKnowOrMixed;
            }
            else
            {
                TransparentMode = (TransparentMode)PropertyInfo.Property.floatValue;
            }
        }

        private ShaderGUISliderItem _cutOffSlider;
        private BlendPopUp _blendPopUp;
        public override void DrawBlock()
        {
            if (TransparentMode== TransparentMode.CutOff)
            {
                _cutOffSlider.OnGUI();
            }

            if (TransparentMode == TransparentMode.Transparent)
            {
                _blendPopUp.OnGUI();
            }
            
        }

        public override void OnEndChange()
        {
            TransparentMode = (TransparentMode)PropertyInfo.Property.floatValue;
            switch (TransparentMode)
            {
                case TransparentMode.Opaque:
                    if (RootItem is NBShaderRootItem opaqueRootItem)
                    {
                        opaqueRootItem.SyncService.ApplyTransparentMode(TransparentMode);
                    }
                    _blendPopUp.PropertyInfo.Property.floatValue = (float)BlendMode.Opaque;
                    _blendPopUp.OnEndChange();
                    break;
                case TransparentMode.Transparent:
                    if (RootItem is NBShaderRootItem transparentRootItem)
                    {
                        transparentRootItem.SyncService.ApplyTransparentMode(TransparentMode);
                    }

                    if (_blendPopUp.BlendMode == BlendMode.Opaque)
                    {
                        _blendPopUp.PropertyInfo.Property.floatValue = (float)BlendMode.Alpha;//如果设置错误则强制设置。
                        _blendPopUp.OnEndChange();
                    }

                    break;
                case TransparentMode.CutOff:
                    if (RootItem is NBShaderRootItem cutOffRootItem)
                    {
                        cutOffRootItem.SyncService.ApplyTransparentMode(TransparentMode);
                    }
                    _blendPopUp.PropertyInfo.Property.floatValue = (float)BlendMode.Opaque;
                    _blendPopUp.OnEndChange();
                    break;
            }

            base.OnEndChange();
        }

        public void Dispose()
        {
            TransparentModeDic.Remove(RootItem);//回收时清掉相关引用。？会不会有时序问题
        }

        private static readonly string[] TransparentOptions =
        {
            "不透明Opaque",
            "半透明Transparent",
            "不透明裁剪CutOff"
        };
        
        
    }

    public enum BlendMode
    {
        Alpha, // Old school alpha-blending mode, fresnel does not affect amount of transparency
        Premultiply, // Physically plausible transparency mode, implemented as alpha pre-multiply
        Additive,
        Multiply,
        Opaque,
        UnKnowOrMixed = -1
    }

    public class BlendPopUp : ShaderGUIPopUpItem,IDisposable
    {
        public BlendMode BlendMode;
        public static Dictionary<ShaderGUIRootItem,BlendPopUp> BlendModeDic = new Dictionary<ShaderGUIRootItem,BlendPopUp>();
        public BlendPopUp(ShaderGUIRootItem rootItem, ShaderGUIItem parentItem) : base(rootItem, parentItem)
        {
            PropertyName = "_Blend";
            PopUpNames = NBShaderInspectorLocalization.GetInspectorOptions("mode.blend", BlendOptions);
            GuiContent = NBShaderInspectorLocalization.MakeInspectorContent("mode.blend", "Blend Mode");
            BlendModeDic[rootItem] = this;
            _addToPreMultiplySlider = new AddToPreMultiplySlider(rootItem, this);
            InitTriggerByChild();
        }

        public override void OnGUI()
        {
            PopUpNames = NBShaderInspectorLocalization.GetInspectorOptions("mode.blend", BlendOptions);
            GuiContent = NBShaderInspectorLocalization.MakeInspectorContent("mode.blend", "Blend Mode");
            base.OnGUI();
            if (PropertyInfo.Property.hasMixedValue)
            {
                BlendMode = BlendMode.UnKnowOrMixed;
            }
            else
            {
                BlendMode = (BlendMode)PropertyInfo.Property.floatValue;
            }
        }

        private AddToPreMultiplySlider _addToPreMultiplySlider;
        public override void DrawBlock()
        {
            if (BlendMode == BlendMode.Additive || BlendMode == BlendMode.Premultiply)
            {
                _addToPreMultiplySlider.OnGUI();
            }
        }

        public override void OnEndChange()
        {
            BlendMode = (BlendMode)PropertyInfo.Property.floatValue;
            if (BlendMode == BlendMode.Additive)
            {
                _addToPreMultiplySlider.PropertyInfo.Property.floatValue = 0;
                _addToPreMultiplySlider.OnEndChange();
            }

            if (BlendMode == BlendMode.Premultiply)
            {
                _addToPreMultiplySlider.PropertyInfo.Property.floatValue = 1;
                _addToPreMultiplySlider.OnEndChange();
            }

            if (RootItem is NBShaderRootItem nbRootItem)
            {
                nbRootItem.SyncService.ApplyBlendMode(BlendMode);
            }
            
            base.OnEndChange();
        }

        public void Dispose()
        {
            BlendModeDic.Remove(RootItem);
        }

        private static readonly string[] BlendOptions =
        {
            "透明度混合AlphaBlend",
            "预乘PreMultiply",
            "叠加Additive",
            "正片叠底Multiply"
        };
    }

    public class AddToPreMultiplySlider : ShaderGUISliderItem
    {
        public AddToPreMultiplySlider(ShaderGUIRootItem rootItem, ShaderGUIItem parentItem) : base(rootItem, parentItem)
        {
            PropertyName = "_AdditiveToPreMultiplyAlphaLerp";
            GuiContent = NBShaderInspectorLocalization.MakeInspectorContent("mode.additiveToPremultiply", "Additive To Premultiply", "0 is additive, 1 is premultiply.");
            base.InitTriggerByChild();
        }

        public override void OnGUI()
        {
            GuiContent = NBShaderInspectorLocalization.MakeInspectorContent("mode.additiveToPremultiply", "Additive To Premultiply", "0 is additive, 1 is premultiply.");
            base.OnGUI();
        }

        public override void CheckIsPropertyModified(bool isCallByChild = false)
        {
            float defaultValue = 0;
            BlendPopUp blendPopUp = BlendPopUp.BlendModeDic[RootItem];
            if (blendPopUp.BlendMode == BlendMode.Premultiply)
            {
                defaultValue = 1;
            }
            
            HasModified = !Mathf.Approximately(defaultValue,PropertyInfo.Property.floatValue);
            ParentItem?.CheckIsPropertyModified(true);
            
        }

        public override void ExecuteReset(bool isCallByParent = false)
        {
            float defaultValue = 0;
            BlendPopUp blendPopUp = BlendPopUp.BlendModeDic[RootItem];
            if (blendPopUp.BlendMode == BlendMode.Premultiply)
            {
                defaultValue = 1;
            }

            PropertyInfo.Property.floatValue = defaultValue;
        }
    }

}
