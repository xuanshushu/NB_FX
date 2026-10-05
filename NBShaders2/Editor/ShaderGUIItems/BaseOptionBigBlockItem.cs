using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using NBShader;

namespace NBShaderEditor
{
    public enum RenderFace
    {
        Front = 2,
        Back = 1,
        Both = 0
    }

    public enum ForceZWriteMode
    {
        Default = 0,
        ForceOn = 1,
        ForceOff = 2
    }

    public class BaseOptionBigBlockItem : BigBlockItem
    {
        private readonly NBShaderRootItem _nbRootItem;
        private readonly ShaderGUIFloatItem _baseColorIntensityItem;
        private readonly ShaderGUISliderItem _alphaAllItem;
        private readonly BlockItem _colorAdjustmentBlock;
        private readonly ZTestItem _zTestItem;
        private readonly CullModeItem _cullItem;
        private readonly ToggleItem _backFirstPassItem;
        private readonly ForceZWriteItem _forceZWriteItem;
        private readonly ToggleItem _affectsShadowsItem;
        private readonly ToggleItem _transparentShadowDitherItem;
        private readonly PropertyToggleBlockItem _baseBackColorBlock;
        private readonly ColorItem _baseBackColorItem;
        private readonly PropertyToggleBlockItem _distanceFadeBlock;
        private readonly PropertyToggleBlockItem _softParticlesBlock;
        private readonly ToggleItem _stencilWithoutPlayerItem;
        private readonly ToggleItem _ignoreVertexColorItem;
        private readonly ShaderGUISliderItem _fogIntensityItem;

        public BaseOptionBigBlockItem(NBShaderRootItem rootItem, ShaderGUIItem parentItem) :
            base(
                rootItem,
                parentItem,
                "_BaseOptionBigBlockItemFoldOut",
                () => Content("block.base", "Base Options", "Global controls"))
        {
            _nbRootItem = rootItem;

            _baseColorIntensityItem = CreateBaseIntensityItem(rootItem, this);
            _alphaAllItem = CreateOverallAlphaItem(rootItem, this);

            _colorAdjustmentBlock = CreateColorAdjustmentBlock(rootItem, this);

            _zTestItem = new ZTestItem(rootItem, this);
            _cullItem = new CullModeItem(rootItem, this);

            _backFirstPassItem = CreateBackFirstPassToggle(rootItem, this, OnBackFirstPassChanged, Is3DTransparent);

            _forceZWriteItem = new ForceZWriteItem(rootItem, this);
            _affectsShadowsItem = CreateAffectsShadowsItem(rootItem,this,Is3DMode);
            _transparentShadowDitherItem = CreateTransparentShadowDitherItem(rootItem,this,ShouldDrawTransparentShadowDither);

            _baseBackColorBlock = CreateBaseBackColorBlock(rootItem, this, out _baseBackColorItem, Is3DMode);

            _distanceFadeBlock = CreateDistanceFadeBlock(rootItem, this);

            _softParticlesBlock = CreateSoftParticlesBlock(rootItem, this);

            _stencilWithoutPlayerItem = CreateStencilWithoutPlayerItem(rootItem,this,OnStencilWithoutPlayerChanged,Is3DMode);

            _ignoreVertexColorItem = CreateIgnoreVertexColorItem(rootItem,this,Is3DMode);

            _fogIntensityItem = CreateFogIntensityItem(rootItem, this);

            InitTriggerByChild();
        }

        internal static ToggleItem CreateAffectsShadowsItem(NBShaderRootItem rootItem,ShaderGUIItem parent,
            Func<bool> visible=null,bool graphShared=false)
        {
            var item=new ToggleItem(rootItem,parent,"_AffectsShadows",()=>Content("base.affectsShadows","Affects Shadows"),_=>rootItem.SyncService.SyncMaterialState(),visible);
            if(graphShared){item.WriteOnlyOnInteractiveChange=true;item.TryWriteValue=value=>rootItem.SyncService.TryWriteGraphBaseShadow(item.PropertyName,value);}return item;
        }
        internal static ToggleItem CreateTransparentShadowDitherItem(NBShaderRootItem rootItem,ShaderGUIItem parent,
            Func<bool> visible=null,bool graphShared=false)
        {
            if(graphShared)return new GraphBasePackedToggleItem(rootItem,parent,"_TransparentShadowDitherToggle",()=>Content("base.transparentShadowDither","Transparent Dither Shadows"),2,visible);
            return new ToggleItem(rootItem,parent,"_TransparentShadowDitherToggle",()=>Content("base.transparentShadowDither","Transparent Dither Shadows"),_=>rootItem.SyncService.SyncMaterialState(),visible);
        }
        internal static ToggleItem CreateIgnoreVertexColorItem(NBShaderRootItem rootItem,ShaderGUIItem parent,
            Func<bool> visible=null,bool graphShared=false)
        {
            if(graphShared)return new GraphBasePackedToggleItem(rootItem,parent,"_IgnoreVetexColor_Toggle",()=>Content("base.ignoreVertexColor","Ignore Vertex Color"),NBShaderFlags.FLAG_BIT_PARTICLE_1_IGNORE_VERTEX_COLOR,visible);
            return new ToggleItem(rootItem,parent,"_IgnoreVetexColor_Toggle",()=>Content("base.ignoreVertexColor","Ignore Vertex Color"),enabled=>rootItem.SyncService.ApplyToggleFlag(NBShaderFlags.FLAG_BIT_PARTICLE_1_IGNORE_VERTEX_COLOR,enabled,1),visible);
        }
        // Same Toggle primitive, raw flag display, explicit deferred writer.
        private sealed class GraphBasePackedToggleItem:ToggleItem
        {
            readonly NBShaderRootItem root;readonly int bit;
            internal GraphBasePackedToggleItem(NBShaderRootItem root,ShaderGUIItem parent,string name,Func<GUIContent> content,int bit,Func<bool> visible)
                :base(root,parent,name,content,isVisible:visible){this.root=root;this.bit=bit;WriteOnlyOnInteractiveChange=true;TryWriteValue=value=>root.SyncService.TryWriteGraphBaseShadow(name,value);}
            public override void DrawController()
            {
                bool enabled=new NBShaderFlags(root.Mats[0]).CheckFlagBits(bit,index:1);
                bool mixed=false;foreach(Material value in root.Mats)mixed|=new NBShaderFlags(value).CheckFlagBits(bit,index:1)!=enabled;
                bool oldMixed=EditorGUI.showMixedValue;EditorGUI.showMixedValue=mixed;EditorGUI.BeginChangeCheck();
                bool next=EditorGUI.Toggle(ControlRect,enabled);bool changed=EditorGUI.EndChangeCheck();EditorGUI.showMixedValue=oldMixed;
                if(changed&&Event.current!=null&&Event.current.rawType!=EventType.Layout&&Event.current.rawType!=EventType.Repaint)TryWriteValue(next);
            }
        }

        internal static ShaderGUIFloatItem CreateBaseIntensityItem(NBShaderRootItem rootItem, ShaderGUIItem parentItem, bool graphShared=false)
        {
            var item=new ShaderGUIFloatItem(rootItem,parentItem){PropertyName="_BaseColorIntensityForTimeline",GuiContent=Content("base.colorIntensity","Base Color Intensity")};
            if(graphShared){item.TryWriteValue=value=>rootItem.SyncService.TryWriteGraphBaseNumeric(item.PropertyName,value);item.TryReset=()=>rootItem.SyncService.TryResetGraphBaseNumeric(item.PropertyName);}
            item.InitTriggerByChild();return item;
        }
        internal static ShaderGUISliderItem CreateOverallAlphaItem(NBShaderRootItem rootItem, ShaderGUIItem parentItem, bool graphShared=false)
        {
            var item=new ShaderGUISliderItem(rootItem,parentItem){PropertyName="_AlphaAll",GuiContent=Content("base.alphaAll","Overall Alpha"),RangePropertyName="AlphaAllRangeVec",WriteOnlyOnInteractiveChange=graphShared};
            if(graphShared){item.TryWriteValue=value=>rootItem.SyncService.TryWriteGraphBaseNumeric(item.PropertyName,value);item.TryWriteRange=(min,max,minEdited,maxEdited)=>rootItem.SyncService.TryWriteGraphAlphaRange(min,max,minEdited,maxEdited);item.TryReset=()=>rootItem.SyncService.TryResetGraphBaseNumeric(item.PropertyName);}
            item.InitTriggerByChild();return item;
        }
        internal static ShaderGUISliderItem CreateFogIntensityItem(NBShaderRootItem rootItem, ShaderGUIItem parentItem, bool graphShared=false)
        {
            var item=new ShaderGUISliderItem(rootItem,parentItem){PropertyName="_fogintensity",GuiContent=Content("base.fogIntensity","Fog Intensity"),Min=0f,Max=1f,WriteOnlyOnInteractiveChange=graphShared};
            if(graphShared){item.TryWriteValue=value=>rootItem.SyncService.TryWriteGraphBaseNumeric(item.PropertyName,value);item.TryReset=()=>rootItem.SyncService.TryResetGraphBaseNumeric(item.PropertyName);}
            item.InitTriggerByChild();return item;
        }
        internal static BigBlockItem CreateGraphBaseNumericBlock(NBShaderRootItem rootItem, ShaderGUIItem parentItem)
        {
            var block=new BigBlockItem(rootItem,parentItem,"_BaseOptionBigBlockItemFoldOut",()=>Content("block.base","Base Options","Global controls"));
            CreateBaseIntensityItem(rootItem,block,true);CreateOverallAlphaItem(rootItem,block,true);CreateFogIntensityItem(rootItem,block,true);return block;
        }

        internal static PropertyToggleBlockItem CreateBaseBackColorBlock(NBShaderRootItem rootItem,
            ShaderGUIItem parentItem, out ColorItem colorItem, System.Func<bool> isVisible=null, bool graphSharedMode=false)
        {
            var block = new PropertyToggleBlockItem(rootItem,parentItem,
                "_BaseBackColorFoldOut","_BaseBackColor_Toggle",() => Content("base.backColor", "Back Color"),
                NBShaderFlags.FLAG_BIT_PARTICLE_BACKCOLOR,0,
                isVisible:isVisible,graphBaseBackColorEdit:graphSharedMode);
            colorItem = new ColorItem(rootItem,block,"_BaseBackColor",() => Content("base.backColor.color", "Back Color"));
            return block;
        }

        internal static ToggleItem CreateStencilWithoutPlayerItem(NBShaderRootItem rootItem,ShaderGUIItem parentItem,
            Action<bool> onValueChanged=null,Func<bool> isVisible=null,bool graphSharedMode=false)
        {
            if(graphSharedMode)return new GraphStencilWithoutPlayerToggleItem(rootItem,parentItem,isVisible);
            return new NBShaderKeywordToggleItem(
                rootItem,
                parentItem,
                "_StencilWithoutPlayerToggle",
                "_STENCIL_WITHOUT_PLAYER",
                () => Content("base.stencilWithoutPlayer", "Stencil Without Player"),
                onValueChanged,
                isVisible);
        }
        // Same original control; Graph owns a deferred explicit preset transaction only.
        private sealed class GraphStencilWithoutPlayerToggleItem : NBShaderKeywordToggleItem
        {
            readonly NBShaderRootItem root;
            public GraphStencilWithoutPlayerToggleItem(NBShaderRootItem root,ShaderGUIItem parent,Func<bool> visible)
                :base(root,parent,"_StencilWithoutPlayerToggle","_STENCIL_WITHOUT_PLAYER",
                    ()=>Content("base.stencilWithoutPlayer","Stencil Without Player"),isVisible:visible){this.root=root;}
            public override void DrawController()
            {
                EditorGUI.BeginChangeCheck();bool enabled=EditorGUI.Toggle(ControlRect,PropertyInfo.Property.floatValue>.5f);
                if(EditorGUI.EndChangeCheck())root.SyncService.TryApplyGraphStencilWithoutPlayer(enabled);
            }
            public override void ExecuteReset(bool isCallByParent=false)
            {
                if(!root.SyncService.TryApplyGraphStencilWithoutPlayer(false))return;
                CheckIsPropertyModified();if(!isCallByParent)ParentItem?.CheckIsPropertyModified(true);
            }
        }

        public override void DrawBlock()
        {
            _baseColorIntensityItem.OnGUI();
            _alphaAllItem.OnGUI();
            _colorAdjustmentBlock.OnGUI();
            _zTestItem.OnGUI();
            _cullItem.OnGUI();
            _backFirstPassItem.OnGUI();
            DrawBackFirstPassWarning();
            _forceZWriteItem.OnGUI();
            _affectsShadowsItem.OnGUI();
            _transparentShadowDitherItem.OnGUI();
            _baseBackColorBlock.OnGUI();
            _distanceFadeBlock.OnGUI();
            _softParticlesBlock.OnGUI();
            _stencilWithoutPlayerItem.OnGUI();
            _ignoreVertexColorItem.OnGUI();

            if (Is3DMode())
            {
                _fogIntensityItem.OnGUI();
            }
            else if (_nbRootItem.PropertyInfoDic.ContainsKey("_fogintensity"))
            {
                _nbRootItem.PropertyInfoDic["_fogintensity"].Property.floatValue = 0f;
            }
        }

        private bool Is3DMode()
        {
            return _nbRootItem.Context.UIEffectEnabled == MixedBool.False;
        }

        private bool Is3DTransparent()
        {
            return Is3DMode() && _nbRootItem.Context.TransparentMode == TransparentMode.Transparent;
        }

        private bool IsParticleMode()
        {
            return _nbRootItem.Context.ParticleMode == MixedBool.True;
        }

        private bool ShouldDrawTransparentShadowDither()
        {
            return Is3DTransparent() &&
                   _nbRootItem.PropertyInfoDic.TryGetValue("_AffectsShadows", out ShaderPropertyInfo info) &&
                   !info.Property.hasMixedValue &&
                   info.Property.floatValue > 0.5f;
        }

        private void DrawBackFirstPassWarning()
        {
            if (!Is3DTransparent() ||
                !_nbRootItem.PropertyInfoDic.ContainsKey("_BackFirstPassToggle") ||
                _nbRootItem.PropertyInfoDic["_BackFirstPassToggle"].Property.hasMixedValue ||
                _nbRootItem.PropertyInfoDic["_BackFirstPassToggle"].Property.floatValue <= 0.5f)
            {
                return;
            }

            DrawLayoutHelpBox(
                NBShaderInspectorLocalization.GetInspectorText(
                    "base.backFirstPass.warning",
                    "预渲染反面会导致打断动态合批，请谨慎使用。"),
                MessageType.Warning);
        }

        internal static ToggleItem CreateBackFirstPassToggle(NBShaderRootItem rootItem, ShaderGUIItem parentItem,
            Action<bool> onChanged, Func<bool> isVisible, bool graphInteractiveOnly = false)
        {
            return new ToggleItem(rootItem, parentItem, "_BackFirstPassToggle",
                () => Content("base.backFirstPass", "Back First Pass"), onChanged, isVisible)
            { WriteOnlyOnInteractiveChange = graphInteractiveOnly };
        }

        private void OnBackFirstPassChanged(bool enabled)
        {
            _nbRootItem.SyncService.ApplyShaderPass("SRPDefaultUnlit", enabled);
            if (enabled && _nbRootItem.PropertyInfoDic.ContainsKey("_Cull"))
            {
                _nbRootItem.PropertyInfoDic["_Cull"].Property.floatValue = (float)RenderFace.Front;
            }
        }

        private void OnStencilWithoutPlayerChanged(bool enabled)
        {
            _nbRootItem.SyncService.ApplyStencilPreset(enabled ? "ParticleWithoutPlayer" : "ParticleBaseDefault");
            if (_nbRootItem.PropertyInfoDic.ContainsKey("_CustomStencilTest"))
            {
                _nbRootItem.PropertyInfoDic["_CustomStencilTest"].Property.floatValue = enabled ? 1f : 0f;
            }
        }

        private static GUIContent Content(string key, string fallback, string tip = "")
        {
            return NBShaderInspectorLocalization.MakeInspectorContent(key, fallback, tip);
        }

        internal static BlockItem CreateColorAdjustmentBlock(NBShaderRootItem rootItem, ShaderGUIItem parentItem, bool graphSharedMode=false)
        {
            var _colorAdjustmentBlock = new BlockItem(
                rootItem,
                parentItem,
                "_BaseColorAdjustmentFoldOut",
                () => Content("base.colorAdjustment", "Color Adjustment"));

            var _colorAdjustmentOnlyMainTexItem = new ToggleItem(
                rootItem,
                _colorAdjustmentBlock,
                "_ColorAdjustmentOnlyAffectMainTex",
                () => Content("base.colorAdjustment.onlyMainTex", "Only Affect Main Texture"),
                enabled => { if(graphSharedMode)rootItem.SyncService.TryApplyGraphColorAdjustmentFlagEdit(NBShaderFlags.FLAG_BIT_PARTICLE_COLOR_ADJUSTMENT_ONLY_AFFECT_MAINTEX,0,enabled);
                    else rootItem.SyncService.ApplyToggleFlag(NBShaderFlags.FLAG_BIT_PARTICLE_COLOR_ADJUSTMENT_ONLY_AFFECT_MAINTEX,enabled); });

            var _hueShiftBlock = new PropertyToggleBlockItem(
                rootItem,
                _colorAdjustmentBlock,
                "_HueShiftFoldOut",
                "_HueShift_Toggle",
                () => Content("base.hueShift", "Hue Shift"),
                graphSharedMode ? 0 : NBShaderFlags.FLAG_BIT_HUESHIFT_ON,
                onValueChanged: graphSharedMode ? (Action<bool>)(enabled => rootItem.SyncService.TryApplyGraphColorAdjustmentFlagEdit(NBShaderFlags.FLAG_BIT_HUESHIFT_ON,0,enabled)) : null);
            var _hueShiftSlider = new ShaderGUISliderItem(rootItem, _hueShiftBlock)
            {
                PropertyName = "_HueShift",
                GuiContent = Content("base.hueShift.value", "Hue"),
                Min = 0f,
                Max = 1f
            };
            _hueShiftSlider.WriteOnlyOnInteractiveChange = graphSharedMode;
            _hueShiftSlider.InitTriggerByChild();
            var _hueShiftCustomDataItem = new CustomDataSelectItem(
                rootItem,
                _hueShiftBlock,
                NBShaderFlags.FLAGBIT_POS_0_CUSTOMDATA_HUESHIFT,
                0,
                () => Content("base.hueShift.customData", "Hue Custom Data"),
                () => rootItem.Context.ParticleMode == MixedBool.True);

            var _saturabilityBlock = new PropertyToggleBlockItem(
                rootItem,
                _colorAdjustmentBlock,
                "_SaturabilityFoldOut",
                "_ChangeSaturability_Toggle",
                () => Content("base.saturability", "Saturation"),
                graphSharedMode ? 0 : NBShaderFlags.FLAG_BIT_SATURABILITY_ON,
                onValueChanged: graphSharedMode ? (Action<bool>)(enabled => rootItem.SyncService.TryApplyGraphColorAdjustmentFlagEdit(NBShaderFlags.FLAG_BIT_SATURABILITY_ON,0,enabled)) : null);
            var _saturabilitySlider = new ShaderGUISliderItem(rootItem, _saturabilityBlock)
            {
                PropertyName = "_Saturability",
                GuiContent = Content("base.saturability.value", "Saturation"),
                RangePropertyName = "SaturabilityRangeVec"
            };
            _saturabilitySlider.WriteOnlyOnInteractiveChange = graphSharedMode;
            _saturabilitySlider.InitTriggerByChild();
            var _saturabilityCustomDataItem = new CustomDataSelectItem(
                rootItem,
                _saturabilityBlock,
                NBShaderFlags.FLAGBIT_POS_1_CUSTOMDATA_SATURATE,
                1,
                () => Content("base.saturability.customData", "Saturation Custom Data"),
                () => rootItem.Context.ParticleMode == MixedBool.True);

            var _contrastBlock = new PropertyToggleBlockItem(
                rootItem,
                _colorAdjustmentBlock,
                "_ContrastFoldOut",
                "_Contrast_Toggle",
                () => Content("base.contrast", "Contrast"),
                graphSharedMode ? 0 : NBShaderFlags.FLAG_BIT_PARTICLE_1_MAINTEX_CONTRAST,
                1, onValueChanged: graphSharedMode ? (Action<bool>)(enabled => rootItem.SyncService.TryApplyGraphColorAdjustmentFlagEdit(NBShaderFlags.FLAG_BIT_PARTICLE_1_MAINTEX_CONTRAST,1,enabled)) : null);
            var _contrastMidColorItem = new ColorItem(rootItem, _contrastBlock, "_ContrastMidColor", () => Content("base.contrast.mid", "Contrast Mid Color"));
            var _contrastSlider = new ShaderGUISliderItem(rootItem, _contrastBlock)
            {
                PropertyName = "_Contrast",
                GuiContent = Content("base.contrast.value", "Contrast"),
                Min = 0f,
                Max = 5f
            };
            _contrastSlider.WriteOnlyOnInteractiveChange = graphSharedMode;
            _contrastSlider.InitTriggerByChild();
            var _contrastCustomDataItem = new CustomDataSelectItem(
                rootItem,
                _contrastBlock,
                NBShaderFlags.FLAGBIT_POS_2_CUSTOMDATA_MAINTEX_CONTRAST,
                2,
                () => Content("base.contrast.customData", "Contrast Custom Data"),
                () => rootItem.Context.ParticleMode == MixedBool.True);

            var _baseMapColorRefineBlock = new PropertyToggleBlockItem(
                rootItem,
                _colorAdjustmentBlock,
                "_BaseMapColorRefineFoldOut",
                "_BaseMapColorRefine_Toggle",
                () => Content("base.colorRefine", "Color Refine"),
                graphSharedMode ? 0 : NBShaderFlags.FLAG_BIT_PARTICLE_1_MAINTEX_COLOR_REFINE,
                1, onValueChanged: graphSharedMode ? (Action<bool>)(enabled => rootItem.SyncService.TryApplyGraphColorAdjustmentFlagEdit(NBShaderFlags.FLAG_BIT_PARTICLE_1_MAINTEX_COLOR_REFINE,1,enabled)) : null);
            var _baseMapColorRefineA = new VectorComponentItem(rootItem, _baseMapColorRefineBlock, "_BaseMapColorRefine", 0, () => Content("base.colorRefine.a", "A Main Color Multiply"), false);
            var _baseMapColorRefineBPower = new VectorComponentItem(rootItem, _baseMapColorRefineBlock, "_BaseMapColorRefine", 1, () => Content("base.colorRefine.bPower", "B Main Color Power"), false);
            var _baseMapColorRefineBMultiply = new VectorComponentItem(rootItem, _baseMapColorRefineBlock, "_BaseMapColorRefine", 2, () => Content("base.colorRefine.bMultiply", "B After Power Multiply"), false);
            var _baseMapColorRefineLerp = new VectorComponentItem(rootItem, _baseMapColorRefineBlock, "_BaseMapColorRefine", 3, () => Content("base.colorRefine.lerp", "A/B Lerp"), true, 0f, 1f);

            var _colorMultiAlphaItem = new ToggleItem(
                rootItem,
                _colorAdjustmentBlock,
                "_ColorMultiAlpha",
                () => Content("base.colorMultiAlpha", "Color Multiply Alpha"),
                enabled => { if(graphSharedMode)rootItem.SyncService.TryApplyGraphColorAdjustmentFlagEdit(NBShaderFlags.FLAG_BIT_PARTICLE_COLOR_MULTI_ALPHA,0,enabled);
                    else rootItem.SyncService.ApplyToggleFlag(NBShaderFlags.FLAG_BIT_PARTICLE_COLOR_MULTI_ALPHA,enabled); });

            return _colorAdjustmentBlock;
        }

        internal static PropertyToggleBlockItem CreateDistanceFadeBlock(NBShaderRootItem rootItem,
            ShaderGUIItem parentItem, bool graphSharedMode=false)
        {
            var block = new PropertyToggleBlockItem(
                rootItem,
                parentItem,
                "_DistanceFadeFoldOut",
                "_DistanceFade_Toggle",
                () => Content("base.distanceFade", "Distance Fade"),
                keyword: graphSharedMode ? null : "_DISTANCE_FADE",
                onValueChanged: graphSharedMode ? (System.Action<bool>)(_ => rootItem.SyncService.TryApplyGraphDepthFeaturesIntentEdit()) : null,
                isVisible: () => rootItem.Context.UIEffectEnabled == MixedBool.False);
            new Vector2LineItem(rootItem, block, "_Fade", true, () => Content("base.distanceFade.range", "Fade Range"));

            return block;
        }

        internal static PropertyToggleBlockItem CreateSoftParticlesBlock(NBShaderRootItem rootItem,
            ShaderGUIItem parentItem, bool graphSharedMode=false)
        {
            var block = new PropertyToggleBlockItem(
                rootItem,
                parentItem,
                "_SoftParticlesFoldOut",
                "_SoftParticlesEnabled",
                () => Content("base.softParticles", "Soft Particles"),
                keyword: graphSharedMode ? null : "_SOFTPARTICLES_ON",
                onValueChanged: graphSharedMode ? (System.Action<bool>)(_ => rootItem.SyncService.TryApplyGraphDepthFeaturesIntentEdit()) : null,
                isVisible: () => rootItem.Context.UIEffectEnabled == MixedBool.False);
            new Vector2LineItem(rootItem, block, "_SoftParticleFadeParams", true, () => Content("base.softParticles.range", "Near/Far Fade"));

            return block;
        }
    }

    public class ZTestItem : ShaderGUIPopUpItem
    {
        private static readonly string[] Options = Enum.GetNames(typeof(CompareFunction));

        public ZTestItem(ShaderGUIRootItem rootItem, ShaderGUIItem parentItem) : base(rootItem, parentItem: parentItem)
        {
            PropertyName = "_ZTest";
            GuiContent = NBShaderInspectorLocalization.MakeInspectorContent("base.ztest", "ZTest");
            PopUpNames = NBShaderInspectorLocalization.GetInspectorOptions("base.ztest", Options);
            InitTriggerByChild();
        }

        public override void OnGUI()
        {
            if (RootItem is NBShaderRootItem nbRootItem)
            {
                if (nbRootItem.Context.UIEffectEnabled == MixedBool.True)
                {
                    if (!Mathf.Approximately(PropertyInfo.Property.floatValue, (float)CompareFunction.LessEqual))
                    {
                        PropertyInfo.Property.floatValue = (float)CompareFunction.LessEqual;
                    }

                    return;
                }

                if (nbRootItem.Context.UIEffectEnabled == MixedBool.Mixed)
                {
                    return;
                }
            }

            base.OnGUI();
        }
    }

    public class CullModeItem : ShaderGUIPopUpItem
    {
        private static readonly string[] Options = { "Both", "Back", "Front" };

        public CullModeItem(ShaderGUIRootItem rootItem, ShaderGUIItem parentItem) : base(rootItem, parentItem)
        {
            PropertyName = "_Cull";
            GuiContent = NBShaderInspectorLocalization.MakeInspectorContent("base.cull", "Cull");
            PopUpNames = NBShaderInspectorLocalization.GetInspectorOptions("base.cull", Options);
            InitTriggerByChild();
        }

        public override void OnGUI()
        {
            base.OnGUI();
        }
    }

    public class ForceZWriteItem : ShaderGUIPopUpItem
    {
        private static readonly string[] Options = { "Default", "Force On", "Force Off" };

        public ForceZWriteItem(ShaderGUIRootItem rootItem, ShaderGUIItem parentItem) : base(rootItem, parentItem)
        {
            PropertyName = "_ForceZWriteToggle";
            GuiContent = NBShaderInspectorLocalization.MakeInspectorContent("base.forceZWrite", "Force ZWrite");
            PopUpNames = NBShaderInspectorLocalization.GetInspectorOptions("base.forceZWrite", Options);
            InitTriggerByChild();
        }

        public override void OnGUI()
        {
            base.OnGUI();
        }

        public override void OnEndChange()
        {
            base.OnEndChange();
            if (RootItem is NBShaderRootItem nbRootItem)
            {
                nbRootItem.SyncService.SyncMaterialState();
            }
        }
    }
}
