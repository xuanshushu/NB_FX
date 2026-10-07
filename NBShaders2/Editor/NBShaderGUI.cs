using UnityEditor;
using UnityEngine;
using NBShader;

namespace NBShaderEditor
{
    public class NBShaderGUI : ShaderGUI
    {
        private NBShaderRootItem _rootItem;
        private string _currentLanguage;

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            string currentLanguage = NBShaderInspectorLocalization.CurrentLanguage;
            if (_rootItem == null || !string.Equals(_currentLanguage, currentLanguage, System.StringComparison.OrdinalIgnoreCase))
            {
                _rootItem = new NBShaderRootItem();
                _currentLanguage = currentLanguage;
            }

            _rootItem.OnGUI(materialEditor, properties);
        }

        // Graph and ShaderLab share the original NB section layout and leaf controls.
        protected void OnGraphGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            string language = NBShaderInspectorLocalization.CurrentLanguage;
            if (_rootItem == null || !string.Equals(_currentLanguage, language, System.StringComparison.OrdinalIgnoreCase))
            {
                _rootItem = new NBShaderRootItem();
                _currentLanguage = language;
            }
            _rootItem.OnGUI(materialEditor, properties);
            if (_rootItem.Context.HasMixedMaterialHosts) return;
            // The original six shared sections are the sole visible NB layout.
        }
    }

    public class NBShaderRootItem : ShaderGUIRootItem
    {
        public NBShaderGUIContext Context { get; private set; }
        public NBShaderSyncService SyncService { get; private set; }

        private ModeBigBlockItem _modeBlock;
        private BaseOptionBigBlockItem _baseBlock;
        private MainTexBigBlockItem _mainTexBlock;
        private LightBigBlockItem _lightBlock;
        private FeatureBigBlockItem _featureBlock;
        private TABigBlockItem _taBlock;
        private ParticleVertexStreamsItem _particleVertexStreamsItem;
        private NBShaderGUIToolBar _toolBar;
        ChromaticAberrationFeatureItem _graphChromaticItem;
        bool _sharedGraphChromaticReady;
        ToggleItem _graphBackFirstItem;
        PortalFeatureItem _graphPortalItem;
        bool _sharedGraphPortalReady;
        Rect _graphBackFirstAdoptRect;
        bool _sharedGraphBackFirstHost, _sharedGraphBackFirstReady;

        public override void InitFlags(System.Collections.Generic.List<Material> mats)
        {
            ShaderFlags = new System.Collections.Generic.List<ShaderFlagsBase>();
            foreach (Material mat in mats)
            {
                ShaderFlags.Add(new NBShaderFlags(mat));
            }
        }

        bool _sharedGraphSharedUVReady;
        SharedUVFeatureItem _graphSharedUVItem;
        bool _sharedGraphStencilWithoutPlayerReady;
        ToggleItem _graphStencilWithoutPlayerItem;
        bool _sharedGraphVATReady;
        VatFeatureItem _graphVATItem;
        bool _sharedGraphMainTextureReady;
        bool _sharedGraphFlipbookReady;
        FlipbookFeatureItem _graphFlipbookItem;
        bool _sharedGraphFresnelReady;
        FresnelFeatureItem _graphFresnelItem;
        bool _sharedGraphDissolveReady;
        DissolveFeatureItem _graphDissolveItem;
        VertexOffsetFeatureItem _graphVertexOffsetItem;
        bool _sharedGraphVertexOffsetReady;
        bool _sharedGraphDepthDecalReady;
        ToggleItem _graphDepthDecalItem;
        bool _sharedGraphDepthFeaturesReady;
        PropertyToggleBlockItem _graphDistanceFadeBlock, _graphSoftParticlesBlock;
        DepthOutlineFeatureItem _graphDepthOutlineItem;
        bool _sharedGraphColorAdjustmentReady, _sharedGraphColorRampReady;
        BlockItem _graphColorAdjustmentBlock;
        RampColorFeatureItem _graphColorRampItem;
        bool _sharedGraphMaskProgramReady;
        MaskFeatureItem _graphMaskItem;
        ProgramNoiseFeatureItem _graphProgramNoiseItem;
        bool _sharedGraphTADepthReady;
        bool _sharedGraphQCMReady;
        BigBlockItem _graphTADepthBlock;
        bool _sharedGraphLightModeReady;
        bool _sharedGraphLightSubReady;
        BigBlockItem _graphLightModeBlock;
        bool _sharedGraphBaseBackColorReady;
        PropertyToggleBlockItem _graphBaseBackColorItem;
        BigBlockItem _graphBaseNumericBlock;
        bool _sharedGraphBaseNumericReady;
        bool _sharedGraphBaseShadowReady;
        ToggleItem _graphAffectsShadowsItem,_graphTransparentShadowDitherItem,_graphIgnoreVertexColorItem;
        static readonly string[] SharedGraphLightModeProperties = { "_FxLightMode", "_LightBigBlockItemFoldOut" };

        int _graphGUIReadPass;
        bool _graphGUIReadPassActive;
        internal int GraphGUIReadPass => _graphGUIReadPassActive ? _graphGUIReadPass : 0;
        int _toolbarDisplayReadPass, _toolbarDisplayShaderRevision;
        bool _toolbarDisplayReady;
        internal void InvalidateGraphGUIReadPass()
        {
            // First capability initialization invalidates every scoped pure read,
            // including Sync preflight; clearing only Context would leave old results.
            if(_graphGUIReadPassActive){unchecked{++_graphGUIReadPass;}if(_graphGUIReadPass==0)++_graphGUIReadPass;}
            _toolbarDisplayReadPass=0;
            Context?.InvalidateGUIReadPass();
        }

        public override void OnGUI(MaterialEditor editor, MaterialProperty[] properties)
        {
            // The shared root base updates PropertyInfo references, but does
            // not force block rebuilding if a Graph reimport changes names.
            bool graphSelection = false;
            foreach (UnityEngine.Object target in editor.targets)
                if (target is Material material && NBShaderGUIContext.IsGraphMaterial(material)) graphSelection = true;
            if (graphSelection)
            {
                if (PropertyInfoDic.Count != properties.Length) IsInit = true;
                else foreach (MaterialProperty property in properties)
                    if (!PropertyInfoDic.ContainsKey(property.name)) { IsInit = true; break; }
            }
            if(graphSelection && Shader!=null && Mats!=null && Mats.Count>0 && Mats[0]!=null && Shader!=Mats[0].shader)IsInit=true;
            _graphGUIReadPassActive=graphSelection&&Event.current!=null&&
                (Event.current.rawType==EventType.Layout||Event.current.rawType==EventType.Repaint);
            if(_graphGUIReadPassActive){unchecked{++_graphGUIReadPass;}if(_graphGUIReadPass==0)++_graphGUIReadPass;}
            try{base.OnGUI(editor, properties);}
            finally{_graphGUIReadPassActive=false;}
        }

        GraphOriginalSectionItem _graphOriginalMode, _graphOriginalBase, _graphOriginalLight, _graphOriginalFeature, _graphOriginalTA;
        enum GraphOriginalSection { Mode, Base, Light, Feature, TA }
        sealed class GraphOriginalSectionItem : BigBlockItem
        {
            readonly NBShaderRootItem owner; readonly GraphOriginalSection section; readonly System.Action draw;
            internal GraphOriginalSectionItem(NBShaderRootItem owner,GraphOriginalSection section,string fold,System.Func<GUIContent> content,System.Action draw):base(owner,null,fold,content)
            {this.owner=owner;this.section=section;this.draw=draw;}
            public override void DrawBlock()=>draw();
            public override void ExecuteReset(bool isCallByParent=false)=>owner.ResetGraphOriginalSection(section);
        }
        bool PrepareGraphOriginalSections()
        {
            // The sole missing original section fold is a real Float metadata prerequisite.
            // No EditorPrefs surrogate, packed bit or unknown-property fallback is invented.
            if(!PropertyInfoDic.ContainsKey("_FeatureBigBlockItemFoldOut"))return false;
            foreach(Material material in Mats)if(!HasFloatProperty(material,"_FeatureBigBlockItemFoldOut"))return false;
            _graphOriginalMode??=new GraphOriginalSectionItem(this,GraphOriginalSection.Mode,"_BigBlockModeSettingFoldOut",()=>NBShaderInspectorLocalization.MakeContent("inspector.block.mode.label","模式设置","inspector.block.mode.tip","各种基础模式设置"),DrawGraphOriginalModeBody);
            _graphOriginalBase??=new GraphOriginalSectionItem(this,GraphOriginalSection.Base,"_BaseOptionBigBlockItemFoldOut",()=>NBShaderInspectorLocalization.MakeInspectorContent("block.base","Base Options","Common render and alpha controls"),DrawGraphOriginalBaseBody);
            _graphOriginalLight??=new GraphOriginalSectionItem(this,GraphOriginalSection.Light,"_LightBigBlockItemFoldOut",()=>NBShaderInspectorLocalization.MakeInspectorContent("block.light","Light","Normal, MatCap and light mode controls"),DrawGraphOriginalLightBody);
            _graphOriginalFeature??=new GraphOriginalSectionItem(this,GraphOriginalSection.Feature,"_FeatureBigBlockItemFoldOut",()=>NBShaderInspectorLocalization.MakeContent("inspector.block.feature.label","特效功能","inspector.block.feature.tip","遮罩、扭曲、溶解等特效功能"),DrawGraphOriginalFeatureBody);
            _graphOriginalTA??=new GraphOriginalSectionItem(this,GraphOriginalSection.TA,"_TABigBlockItemFoldOut",()=>NBShaderInspectorLocalization.MakeInspectorContent("block.ta","TA Debug","Technical artist debug and helper controls"),DrawGraphOriginalTABody);
            if(_graphOriginalMode.ChildrenItemList.Count==0)
            {
                System.Collections.Generic.List<ShaderGUIItem> all;
                if(!TryGetGraphResetRootItems(out all))return false;
                AttachGraphOriginalMembers(_graphOriginalMode,GraphOriginalSection.Mode);
                AttachGraphOriginalMembers(_graphOriginalBase,GraphOriginalSection.Base);
                AttachGraphOriginalMembers(_graphOriginalLight,GraphOriginalSection.Light);
                AttachGraphOriginalMembers(_graphOriginalFeature,GraphOriginalSection.Feature);
                AttachGraphOriginalMembers(_graphOriginalTA,GraphOriginalSection.TA);
            }
            return true;
        }
        void AttachGraphOriginalMembers(GraphOriginalSectionItem group,GraphOriginalSection section)
        {
            foreach(ShaderGUIItem item in GraphOriginalSectionMembers(section))
                if(item!=null&&!group.ChildrenItemList.Contains(item)){group.ChildrenItemList.Add(item);item.ParentItem=group;}
            group.CheckIsPropertyModified();
        }
        void DrawGraphOfficialModeInputs()
        {
            // URP surface properties remain the sole state storage. Use the original NB labels/order.
            using(new EditorGUI.DisabledScope(true))EditorGUI.Popup(GetControlRect(),NBShaderInspectorLocalization.MakeInspectorContent("mode.meshSource","Mesh Source").text,1,NBShaderInspectorLocalization.GetInspectorOptions("mode.meshSource",new[]{"粒子系统","模型（非粒子发射）","2D RawImage","2D 精灵","2D 材质贴图","2D UIParticle"}));
            var surface=Context.GetProperty("_Surface");var clip=Context.GetProperty("_AlphaClip");
            int mode=surface.hasMixedValue||clip.hasMixedValue?-1:surface.floatValue>.5f?1:clip.floatValue>.5f?2:0;
            bool previous=EditorGUI.showMixedValue;EditorGUI.showMixedValue=mode<0;EditorGUI.BeginChangeCheck();
            int next=EditorGUI.Popup(GetControlRect(),NBShaderInspectorLocalization.MakeInspectorContent("mode.transparent","Transparent Mode").text,mode,NBShaderInspectorLocalization.GetInspectorOptions("mode.transparent",new[]{"不透明","透明","裁剪"}));
            if(EditorGUI.EndChangeCheck()&&next>=0){MatEditor.RegisterPropertyChangeUndo("NB Transparent Mode");surface.floatValue=next==1?1:0;clip.floatValue=next==2?1:0;Context.Refresh();}
            EditorGUI.showMixedValue=previous;
            if(mode==1&&PropertyInfoDic.ContainsKey("_Blend"))
            {
                _graphOfficialBlend??=new ShaderGUIPopUpItem(this,_graphOriginalMode,"_Blend",
                    ()=>NBShaderInspectorLocalization.MakeInspectorContent("mode.blend","Blend Mode"),
                    ()=>NBShaderInspectorLocalization.GetInspectorOptions("mode.blend",GraphOfficialBlendOptions))
                    {WriteOnlyOnInteractiveChange=true};
                _graphOfficialBlend.OnGUI();
            }
            if(mode==2&&PropertyInfoDic.ContainsKey("_Cutoff"))
            {
                if(_graphOfficialCutoff==null)
                {
                    _graphOfficialCutoff=new ShaderGUISliderItem(this,_graphOriginalMode){PropertyName="_Cutoff",GuiContent=NBShaderInspectorLocalization.MakeInspectorContent("mode.cutoff","Cutoff","0 keeps everything, 1 clips everything."),WriteOnlyOnInteractiveChange=true};
                    _graphOfficialCutoff.InitTriggerByChild();
                }
                _graphOfficialCutoff.OnGUI();
            }
        }
        void DrawGraphOfficialRenderStateInputs()
        {
            if(!PropertyInfoDic.ContainsKey("_ZTest")||!PropertyInfoDic.ContainsKey("_Cull")||!PropertyInfoDic.ContainsKey("_ZWriteControl"))return;
            _graphOfficialZTest??=new ZTestItem(this,_graphOriginalBase){WriteOnlyOnInteractiveChange=true};
            _graphOfficialCull??=new CullModeItem(this,_graphOriginalBase){WriteOnlyOnInteractiveChange=true};
            _graphOfficialZWrite??=new ShaderGUIPopUpItem(this,_graphOriginalBase,"_ZWriteControl",
                ()=>NBShaderInspectorLocalization.MakeInspectorContent("base.forceZWrite","Force ZWrite"),
                ()=>NBShaderInspectorLocalization.GetInspectorOptions("base.forceZWrite",GraphOfficialZWriteOptions))
                {WriteOnlyOnInteractiveChange=true};
            _graphOfficialZTest.OnGUI();_graphOfficialCull.OnGUI();
        }
        static readonly string[] GraphOfficialRenderStateNames={"_ZTest","_Cull","_ZWriteControl"};
        static readonly string[] GraphOfficialModeNames={"_Surface","_AlphaClip","_Blend","_Cutoff"};
        UnityEditor.Rendering.Universal.ShaderGraph.NBGraphUnlitGUIBridge _graphResetURPBridge;
        bool GraphOfficialResetDefaultsCompatible(string[] names)
        {
            foreach(Material value in Mats)foreach(string name in names)
            {
                if(!HasFloatProperty(value,name))return false;
                float compiled=value.shader.GetPropertyDefaultFloatValue(value.shader.FindPropertyIndex(name));
                if(float.IsNaN(compiled)||float.IsInfinity(compiled))return false;
            }
            return true;
        }
        void ResetGraphOfficialDefaults(string[] names)
        {
            foreach(Material value in Mats)foreach(string name in names)
                value.SetFloat(name,value.shader.GetPropertyDefaultFloatValue(value.shader.FindPropertyIndex(name)));
        }
        void ValidateGraphOfficialResetMaterials()
        {
            _graphResetURPBridge??=new UnityEditor.Rendering.Universal.ShaderGraph.NBGraphUnlitGUIBridge();
            foreach(Material value in Mats)_graphResetURPBridge.ValidateMaterial(value);
        }
        ZTestItem _graphOfficialZTest;
        CullModeItem _graphOfficialCull;
        ShaderGUIPopUpItem _graphOfficialZWrite,_graphOfficialBlend;
        ShaderGUISliderItem _graphOfficialCutoff;
        static readonly string[] GraphOfficialZWriteOptions={"Default","Force On","Force Off"};
        static readonly string[] GraphOfficialBlendOptions={"閫忔槑搴︽贩鍚圓lphaBlend","棰勪箻PreMultiply","鍙犲姞Additive","姝ｇ墖鍙犲簳Multiply"};
        void DrawGraphOriginalModeBody()
        {
            DrawGraphOfficialModeInputs();
            // Existing original additive slider business remains unchanged.
            if(InitializeGraphRemainingSharedUI())DrawGraphBareBlock(_graphBlendModeBlock,"Edit NB Mode");
        }
        void DrawGraphBaseNumericLeaf(string property)
        {
            if(!InitializeGraphBaseNumericInputs())return;
            foreach(ShaderGUIItem item in _graphBaseNumericBlock.ChildrenItemList)
                if(item.PropertyName==property){DrawGraphBaseNumericInputs(item);return;}
        }
        void DrawGraphOriginalBaseBody()
        {
            DrawGraphBaseNumericLeaf("_BaseColorIntensityForTimeline");DrawGraphBaseNumericLeaf("_AlphaAll");
            DrawGraphColorAdjustmentInputs();
            DrawGraphOfficialRenderStateInputs();
            DrawGraphBackFirstInputs();_graphOfficialZWrite?.OnGUI();InitializeGraphBaseShadowInputs();
            DrawGraphBaseShadowInputs(_graphAffectsShadowsItem);DrawGraphBaseShadowInputs(_graphTransparentShadowDitherItem);
            DrawGraphBaseBackColorInputs();
            if(InitializeGraphDepthFeaturesInputs()){DrawGraphDepthFeaturesInputs(_graphDistanceFadeBlock);DrawGraphDepthFeaturesInputs(_graphSoftParticlesBlock);}
            DrawGraphStencilWithoutPlayerInputs();DrawGraphBaseShadowInputs(_graphIgnoreVertexColorItem);DrawGraphBaseNumericLeaf("_fogintensity");
        }
        void DrawGraphOriginalLightBody()
        {
            if(InitializeGraphLightModeInputs())DrawGraphBareBlock(_graphLightModeBlock,"Edit NB Light");
            DrawGraphNormalMapInputs();DrawGraphMatCapInputs();
        }
        void DrawGraphOriginalFeatureBody()
        {
            InitializeGraphMaskProgramInputs();DrawGraphMaskProgramInputs(_graphMaskItem);
            DrawGraphNoiseInputs();DrawGraphChromaticInputs();
            if(InitializeGraphOverlayInputs())DrawGraphOverlayLeaf(_graphEmissionItem);
            DrawGraphColorRampInputs();if(InitializeGraphDissolveInputs())DrawGraphDissolveInputs();
            if(InitializeGraphOverlayInputs())DrawGraphOverlayLeaf(_graphColorBlendItem);
            DrawGraphMaskProgramInputs(_graphProgramNoiseItem);DrawGraphSharedUVInputs();
            if(InitializeGraphFresnelInputs())_graphFresnelItem.OnGUI();
            if(InitializeGraphVertexOffsetInputs())DrawGraphVertexOffsetInputs();
            DrawGraphDepthDecalInputs();if(InitializeGraphDepthFeaturesInputs())DrawGraphDepthFeaturesInputs(_graphDepthOutlineItem);
            DrawGraphParallaxInputs();DrawGraphPortalInputs();
            if(InitializeGraphFlipbookInputs())_graphFlipbookItem.OnGUI();DrawGraphVATInputs();
        }
        void DrawGraphOriginalTABody()
        {
            if(InitializeGraphTADepthInputs())DrawGraphBareBlock(_graphTADepthBlock,"Edit NB TA");
            if(InitializeGraphRemainingSharedUI())_graphKeywordListBlock.OnGUI();
        }
        void DrawGraphBareBlock(BigBlockItem block,string undoLabel)
        {
            if(block==null)return;
            if(Event.current!=null&&Event.current.rawType!=EventType.Layout&&Event.current.rawType!=EventType.Repaint)Undo.RecordObjects(MatEditor.targets,undoLabel);
            block.DrawBlock();
        }
        void DrawGraphOverlayLeaf(ShaderGUIItem item)
        {
            if(item==null||!InitializeGraphOverlayInputs())return;
            if(Event.current!=null&&Event.current.rawType!=EventType.Layout&&Event.current.rawType!=EventType.Repaint)Undo.RecordObjects(MatEditor.targets,"Edit NB Overlay Inputs");item.OnGUI();
        }
        System.Collections.Generic.List<ShaderGUIItem> GraphOriginalSectionMembers(GraphOriginalSection section)
        {
            // Populate only on explicit Reset, after the same accepted whole-schema preflight.
            switch(section)
            {
                case GraphOriginalSection.Mode:return new System.Collections.Generic.List<ShaderGUIItem>{_graphBlendModeBlock};
                case GraphOriginalSection.Base:return new System.Collections.Generic.List<ShaderGUIItem>{_graphBaseNumericBlock,_graphColorAdjustmentBlock,_graphBaseBackColorItem,_graphDistanceFadeBlock,_graphSoftParticlesBlock,_graphStencilWithoutPlayerItem,_graphBackFirstItem};
                case GraphOriginalSection.Light:return new System.Collections.Generic.List<ShaderGUIItem>{_graphLightModeBlock,_graphNormalMapBlock,_graphMatCapBlock};
                case GraphOriginalSection.TA:return new System.Collections.Generic.List<ShaderGUIItem>{_graphTADepthBlock,_graphKeywordListBlock};
                default:return new System.Collections.Generic.List<ShaderGUIItem>{_graphMaskItem,_graphNoiseItem,_graphChromaticItem,_graphEmissionItem,_graphColorRampItem,_graphDissolveItem,_graphColorBlendItem,_graphProgramNoiseItem,_graphSharedUVItem,_graphFresnelItem,_graphVertexOffsetItem,_graphDepthDecalItem,_graphDepthOutlineItem,_graphParallaxItem,_graphPortalItem,_graphFlipbookItem,_graphVATItem};
            }
        }
        void ResetGraphOriginalSection(GraphOriginalSection section)
        {
            System.Collections.Generic.List<ShaderGUIItem> all;
            if(!TryGetGraphResetRootItems(out all))return;
            bool officialMode=section==GraphOriginalSection.Mode,officialBase=section==GraphOriginalSection.Base;
            if((officialMode&&!GraphOfficialResetDefaultsCompatible(GraphOfficialModeNames))||
                (officialBase&&!GraphOfficialResetDefaultsCompatible(GraphOfficialRenderStateNames)))return;
            var members=GraphOriginalSectionMembers(section);
            SyncService.TryRunGraphSharedReset(()=>{
                if(officialMode)ResetGraphOfficialDefaults(GraphOfficialModeNames);
                foreach(ShaderGUIItem item in members)if(item!=null)item.ExecuteReset(true);
                if(officialBase)ResetGraphOfficialDefaults(GraphOfficialRenderStateNames);
                if(officialMode||officialBase)ValidateGraphOfficialResetMaterials();
            },true);
        }

        static readonly string[] SharedGraphMainTextureProperties =
        {
            "_BaseMap", "_Color", "_BaseMap_ST", "_BaseMapUVRotation",
            "_BaseMapUVRotationSpeed", "_BaseMapMaskMapOffset", "_TexDistortion_intensity"
        };

        public System.Collections.Generic.IEnumerable<string> GetSharedGraphPropertyNames()
        {
            var names = new System.Collections.Generic.List<string>();
            if(Context!=null&&Context.IsGraphMaterialHost&&PropertyInfoDic.ContainsKey("_FeatureBigBlockItemFoldOut"))names.Add("_FeatureBigBlockItemFoldOut");
            if(Context!=null&&Context.IsGraphMaterialHost&&_sharedGraphRemainingUIReady)names.AddRange(NBShaderSyncService.GraphRemainingSharedUIProperties);
            if(Context!=null&&Context.IsGraphMaterialHost&&_sharedGraphBaseShadowReady)names.AddRange(NBShaderSyncService.GraphBaseShadowProperties);
            if(Context!=null&&Context.IsGraphMaterialHost&&_sharedGraphBaseNumericReady)names.AddRange(NBShaderSyncService.GraphBaseNumericProperties);
            if(Context!=null && Context.IsGraphMaterialHost && _sharedGraphBaseBackColorReady)names.AddRange(NBShaderSyncService.GraphBaseBackColorProperties);
            if (Context != null && Context.IsGraphMaterialHost && _sharedGraphTADepthReady)
                names.AddRange(NBShaderSyncService.GraphTADepthProperties);
            if(Context!=null&&Context.IsGraphMaterialHost&&_sharedGraphQCMReady)names.AddRange(NBShaderSyncService.GraphQCMProperties);
            if (Context != null && Context.IsGraphMaterialHost && _sharedGraphMainTextureReady)
                names.AddRange(SharedGraphMainTextureProperties);
            if (Context != null && Context.IsGraphMaterialHost && _sharedGraphLightModeReady)
                names.AddRange(SharedGraphLightModeProperties);
            if(Context!=null && Context.IsGraphMaterialHost && _sharedGraphLightSubReady) names.AddRange(NBShaderSyncService.GraphLightSubSharedProperties);
            if (Context != null && Context.IsGraphMaterialHost && _sharedGraphFlipbookReady) names.Add("_FlipbookBlending");
            if (Context != null && _sharedGraphMainTextureReady && Context.CanEditGraphMainTexUV) names.AddRange(new[] { "_UTwirlEnabled", "_PolarCoordinatesEnabled", "_TWParameter", "_TWStrength", "_PCCenter", "_CylinderUVRotate", "_CylinderUVPosOffset", "_WorldSpaceUVModeSelector", "_ObjectSpaceUVModeSelector" });
            if (Context != null && Context.IsGraphMaterialHost && _sharedGraphFresnelReady) names.AddRange(SharedGraphFresnelProperties);
            if(Context!=null && Context.IsGraphMaterialHost && _sharedGraphOverlayReady)names.AddRange(SharedGraphOverlayProperties);
            if(Context!=null&&Context.IsGraphMaterialHost&&_sharedGraphDissolveReady)names.AddRange(NBShaderSyncService.GraphDissolveSharedPropertyNames);
            if (Context != null && Context.IsGraphMaterialHost && _sharedGraphParallaxReady) names.AddRange(SharedGraphParallaxProperties);
            if(Context!=null && Context.IsGraphMaterialHost && _sharedGraphNormalMapReady)names.AddRange(SharedGraphNormalMapProperties);
            if(Context!=null&&Context.IsGraphMaterialHost&&_sharedGraphMaskProgramReady)names.AddRange(NBShaderSyncService.GraphMaskProgramSharedPropertyNames);
            if(Context!=null&&Context.IsGraphMaterialHost&&_sharedGraphColorAdjustmentReady)names.AddRange(NBShaderSyncService.GraphColorAdjustmentSharedPropertyNames);
            if(Context!=null&&Context.IsGraphMaterialHost&&_sharedGraphColorRampReady)names.AddRange(NBShaderSyncService.GraphColorRampSharedPropertyNames);
            if(Context!=null&&Context.IsGraphMaterialHost&&_sharedGraphMatCapReady)names.AddRange(NBShaderSyncService.GraphMatCapSharedPropertyNames);
            if(Context!=null&&Context.IsGraphMaterialHost&&_sharedGraphDepthFeaturesReady)names.AddRange(NBShaderSyncService.GraphDepthSharedPropertyNames);
            if(Context!=null&&Context.IsGraphMaterialHost&&_sharedGraphNoiseReady)names.AddRange(NBShaderSyncService.GraphNoiseSharedPropertyNames);
            if(Context!=null&&Context.IsGraphMaterialHost&&_sharedGraphSharedUVReady)names.AddRange(NBShaderSyncService.GraphSharedUVPropertyNames);
            if(Context!=null&&Context.IsGraphMaterialHost&&_sharedGraphVertexOffsetReady)names.AddRange(NBShaderSyncService.GraphVertexOffsetSharedProperties);
            if(Context!=null&&Context.IsGraphMaterialHost&&_sharedGraphStencilWithoutPlayerReady)names.Add("_StencilWithoutPlayerToggle");
            if(Context!=null&&Context.IsGraphMaterialHost&&_sharedGraphDepthDecalReady)names.Add("_DepthDecal_Toggle");
            if (Context != null && Context.IsGraphMaterialHost && _sharedGraphBackFirstHost) names.Add("_BackFirstPassToggle");
            if(Context!=null&&Context.IsGraphMaterialHost&&_sharedGraphVATReady)names.AddRange(NBShaderSyncService.GraphVATSharedPropertyNames);
            if (Context != null && Context.IsGraphMaterialHost && _sharedGraphPortalReady) names.AddRange(NBShaderSyncService.GraphPortalSharedProperties);
            if(Context!=null&&Context.IsGraphMaterialHost&&_sharedGraphChromaticReady)names.AddRange(NBShaderSyncService.GraphChromaticSharedProperties);
            return names;
        }

        sealed class ShaderPropertyTypeMap
        {
            internal int count;
            internal System.Collections.Generic.Dictionary<string,UnityEngine.Rendering.ShaderPropertyType> types;
        }
        static System.Runtime.CompilerServices.ConditionalWeakTable<Shader,ShaderPropertyTypeMap> s_ShaderPropertyTypes=
            new System.Runtime.CompilerServices.ConditionalWeakTable<Shader,ShaderPropertyTypeMap>();
        static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Shader,ShaderPropertyTypeMap>.CreateValueCallback s_BuildShaderPropertyTypes=BuildShaderPropertyTypes;
        internal static int ShaderPropertyTypeCacheRevision { get; private set; }
        static NBShaderRootItem(){EditorApplication.projectChanged+=InvalidateShaderPropertyTypes;}
        internal static void InvalidateShaderPropertyTypes()
        {
            s_ShaderPropertyTypes=new System.Runtime.CompilerServices.ConditionalWeakTable<Shader,ShaderPropertyTypeMap>();
            unchecked{++ShaderPropertyTypeCacheRevision;}
        }
        static ShaderPropertyTypeMap BuildShaderPropertyTypes(Shader shader)
        {
            int count=shader.GetPropertyCount();
            var values=new System.Collections.Generic.Dictionary<string,UnityEngine.Rendering.ShaderPropertyType>(count,System.StringComparer.Ordinal);
            for(int i=0;i<count;++i)
            {
                string name=shader.GetPropertyName(i);
                if(values.ContainsKey(name))
                {
                    int actual=shader.FindPropertyIndex(name);
                    if(actual>=0)values[name]=shader.GetPropertyType(actual);
                }
                else values.Add(name,shader.GetPropertyType(i));
            }
            return new ShaderPropertyTypeMap{count=count,types=values};
        }
        internal static bool HasFloatProperty(Material material, string name)
        {
            if(material==null||material.shader==null||!material.HasProperty(name))return false;
            Shader shader=material.shader;
            var map=s_ShaderPropertyTypes.GetValue(shader,s_BuildShaderPropertyTypes);
            if(map.count!=shader.GetPropertyCount())
            {
                s_ShaderPropertyTypes.Remove(shader);
                map=s_ShaderPropertyTypes.GetValue(shader,s_BuildShaderPropertyTypes);
            }
            UnityEngine.Rendering.ShaderPropertyType type;
            return map.types.TryGetValue(name,out type)&&type==UnityEngine.Rendering.ShaderPropertyType.Float;
        }
        internal bool GraphGUIStateMayInitialize()
        {
            foreach(Material material in Mats)
            {
                if(!material.HasProperty(NBShaderSyncService.GraphGUIStateVersionProperty)||material.GetFloat(NBShaderSyncService.GraphGUIStateVersionProperty)!=2f)return true;
                if(material.HasProperty("_NB_TierVATFamily")&&material.HasProperty("_NB_TierVATSubMode")&&
                    material.GetFloat("_NB_TierVATFamily")==-1f&&material.GetFloat("_NB_TierVATSubMode")==-1f)return true;
            }
            return false;
        }

        // Can be invoked by non-visible backend tests with real MaterialEditor
        // and real MaterialProperties. Does not fabricate missing properties.
        internal bool InitializeGraphMainTextureInputs()
        {
            Context ??= new NBShaderGUIContext(this);
            SyncService ??= new NBShaderSyncService(this);
            Context.Refresh();
            _sharedGraphMainTextureReady = false;
            if (!Context.IsGraphMaterialHost || MatEditor == null) return false;
            foreach (Material material in Mats)
            {
                if (!HasFloatProperty(material, "_MainTexBigBlockItemFoldOut") ||
                    !HasFloatProperty(material, "_BaseMapFoldOut") ||
                    !HasFloatProperty(material, NBShaderSyncService.GraphGUIStateVersionProperty))
                    return false;
            }
            foreach (string name in SharedGraphMainTextureProperties)
                if (!PropertyInfoDic.ContainsKey(name)) return false;
            foreach (Material material in Mats)
                foreach (string prefix in new[] { "_NB_WrapFlags", "_NB_ColorChannel", "_NB_ForceNoMipFlags" })
                    if (!HasFloatProperty(material, prefix + "Lo16") || !HasFloatProperty(material, prefix + "Hi16")) return false;
            // Only the explicit first-schema transaction owns initial gates.
            // Marker2 already ready returns without any paint projection.
            bool hasTierContract = false;
            foreach (Material material in Mats) if (material.HasProperty("_NBShaderFeatureTier")) hasTierContract = true;
            bool mayInitialize=GraphGUIStateMayInitialize();
            if (hasTierContract)
            {
                if (!SyncService.TryInitializeGraphSupportedGateTierState()) return false;
            }
            else SyncService.PrepareGraphGUIState();
            // The first authorized marker/typed projection must not leave a
            // before-initialization Context cached for the rest of this paint.
            if(mayInitialize){InvalidateGraphGUIReadPass();Context.Refresh();}
            _mainTexBlock ??= new MainTexBigBlockItem(this, null);
            _sharedGraphMainTextureReady = true;
            return true;
        }

        internal bool IsGraphLightModeSchemaReady()
        {
            if (MatEditor == null || Mats == null || Mats.Count == 0 || NBShaderGUIContext.HasMixedHosts(Mats)) return false;
            foreach (Material material in Mats)
            {
                if (material == null || !NBShaderGUIContext.IsGraphMaterial(material)) return false;
                foreach (string name in new[] { "_FxLightMode", "_LightBigBlockItemFoldOut", "_SixWayColorAbsorptionToggle", NBShaderSyncService.GraphGUIStateVersionProperty })
                    if (!HasFloatProperty(material, name) || !PropertyInfoDic.ContainsKey(name)) return false;
                bool projectionChange;
                if(!NBShaders2.Editor.FeatureLevel.NBShaderFeatureLevelMaterialApplier.CanApplyGraphSavedSupportedGateTier(material,out projectionChange))return false;
                float mode = material.GetFloat("_FxLightMode");
                if (float.IsNaN(mode) || float.IsInfinity(mode) || mode < 0 || mode > 4 || mode != Mathf.Round(mode) ||
                    material.GetFloat(NBShaderSyncService.GraphGUIStateVersionProperty) != 2f) return false;
                float absorption = material.GetFloat("_SixWayColorAbsorptionToggle");
                if (float.IsNaN(absorption) || float.IsInfinity(absorption)) return false;
            }
            return true;
        }

        internal bool InitializeGraphLightModeInputs()
        {
            Context ??= new NBShaderGUIContext(this);
            SyncService ??= new NBShaderSyncService(this);
            Context.Refresh();
            _sharedGraphLightModeReady = Context.IsGraphMaterialHost && IsGraphLightModeSchemaReady();
            if (!_sharedGraphLightModeReady) return false;
            bool subReady = SyncService.HasGraphLightSubControlsSchema();
            if(_graphLightModeBlock==null || _sharedGraphLightSubReady!=subReady)
                _graphLightModeBlock = subReady ? LightBigBlockItem.CreateGraphSharedLightBlock(this, null) : LightBigBlockItem.CreateGraphModeOnlyBlock(this, null);
            _sharedGraphLightSubReady = subReady;
            return true;
        }

        internal void DrawGraphLightInputs(ShaderGUIItem selectedItem = null)
        {
            bool interactive = Event.current!=null && Event.current.type!=EventType.Layout && Event.current.type!=EventType.Repaint;
            System.Collections.Generic.List<Texture> rampBefore=null;
            if(_sharedGraphLightSubReady && interactive)
            {
                var targets=new UnityEngine.Object[Mats.Count];rampBefore=new System.Collections.Generic.List<Texture>(Mats.Count);
                for(int i=0;i<Mats.Count;++i){targets[i]=Mats[i];rampBefore.Add(Mats[i].GetTexture("_SixWayEmissionRamp"));}
                // Own original direct NoMip/vector/color writes before the actual child event.
                Undo.RecordObjects(targets,"NB Shared Light Inputs");
            }
            if(selectedItem==null)_graphLightModeBlock.OnGUI();else selectedItem.OnGUI();
            if(rampBefore!=null)SyncService.TryFinalizeGraphLightRampEdit(rampBefore);
        }

        internal bool InitializeGraphTADepthInputs()
        {
            Context ??= new NBShaderGUIContext(this);
            SyncService ??= new NBShaderSyncService(this);
            Context.Refresh();
            _sharedGraphTADepthReady = Context.IsGraphMaterialHost && SyncService.HasGraphTADepthEditSchema();
            if (!_sharedGraphTADepthReady) return false;
            bool qcm=SyncService.HasGraphQCMEditSchema();if(qcm!=_sharedGraphQCMReady){_sharedGraphQCMReady=qcm;_graphTADepthBlock=null;}
            _graphTADepthBlock ??= TABigBlockItem.CreateGraphDepthOnlyBlock(this, null,qcm);
            return true;
        }

        internal bool InitializeGraphFlipbookInputs()
        {
            Context ??= new NBShaderGUIContext(this);
            SyncService ??= new NBShaderSyncService(this);
            Context.Refresh();
            _sharedGraphFlipbookReady = Context.IsGraphMaterialHost && SyncService.HasGraphFlipbookEditSchema();
            if (!_sharedGraphFlipbookReady) return false;
            _graphFlipbookItem ??= new FlipbookFeatureItem(this, null, true);
            return true;
        }


        bool _sharedGraphOverlayReady;
        EmissionFeatureItem _graphEmissionItem;
        ColorBlendFeatureItem _graphColorBlendItem;
        internal static readonly string[] SharedGraphOverlayFloatProperties = {
            "_EmissionBlockFoldOut","_EmissionUVModeFoldOut","_ColorBlendBlockFoldOut","_ColorBlendUVModeFoldOut",
            "_EmissionEnabled","_EmissionMapUVRotation","_EmissionMapColorIntensity","_EmissionAlphaIntensity","_Emi_Distortion_intensity",
            "_EmissionBlendMode","_EmissionAlphaMultiplyMode","_ColorBlendMap_Toggle","_ColorBlendColorIntensity","_ColorBlendMode","_ColorBlendAlphaMultiplyMode",
            "_NB_CustomDataFlag3Lo16","_NB_CustomDataFlag3Hi16","_NB_WrapFlagsLo16","_NB_WrapFlagsHi16","_NB_ForceNoMipFlagsLo16","_NB_ForceNoMipFlagsHi16"
        };
        internal static readonly string[] SharedGraphOverlayVectorProperties={"_EmissionMapUVOffset","_ColorBlendMapOffset","_ColorBlendVec"};
        static readonly string[] SharedGraphOverlayProperties={"_EmissionEnabled","_EmissionMap","_EmissionMapColor","_EmissionMapUVRotation","_EmissionMapUVOffset","_EmissionMapColorIntensity","_EmissionAlphaIntensity","_Emi_Distortion_intensity","_EmissionBlendMode","_EmissionAlphaMultiplyMode","_ColorBlendMap_Toggle","_ColorBlendMap","_ColorBlendColor","_ColorBlendColorIntensity","_ColorBlendVec","_ColorBlendMapOffset","_ColorBlendMode","_ColorBlendAlphaMultiplyMode"};
        internal bool InitializeGraphOverlayInputs()
        {
            Context ??= new NBShaderGUIContext(this); SyncService ??= new NBShaderSyncService(this); Context.Refresh();
            _sharedGraphOverlayReady=Context.IsGraphMaterialHost && SyncService.HasGraphOverlayEditSchema();
            if(!_sharedGraphOverlayReady)return false;
            _graphEmissionItem ??= new EmissionFeatureItem(this,null,true); _graphColorBlendItem ??= new ColorBlendFeatureItem(this,null,true);return true;
        }
        void DrawGraphOverlayInputs()
        {
            if(!InitializeGraphOverlayInputs())return;
            // Original widgets keep their exact business. Capture the Material
            // before an interactive event for floats, packed choices and Reset.
            if(Event.current!=null && Event.current.type!=EventType.Layout && Event.current.type!=EventType.Repaint)
                Undo.RecordObjects(MatEditor.targets,"Edit NB Overlay Inputs");
            _graphEmissionItem.OnGUI();_graphColorBlendItem.OnGUI();
        }


        bool _sharedGraphParallaxReady;
        ParallaxFeatureItem _graphParallaxItem;
        internal static readonly string[] SharedGraphParallaxProperties = {
            "_ParallaxMapping_Toggle", "_ParallaxMapping_Map", "_ParallaxMapping_Intensity", "_ParallaxMapping_Vec"
        };
        internal bool InitializeGraphParallaxInputs()
        {
            Context ??= new NBShaderGUIContext(this); SyncService ??= new NBShaderSyncService(this); Context.Refresh();
            _sharedGraphParallaxReady = Context.IsGraphMaterialHost && SyncService.HasGraphParallaxEditSchema();
            if (!_sharedGraphParallaxReady) return false;
            _graphParallaxItem ??= new ParallaxFeatureItem(this, null, true);
            return true;
        }
        void DrawGraphParallaxInputs()
        {
            if (!InitializeGraphParallaxInputs()) return;
            bool input = Event.current != null && Event.current.rawType != EventType.Layout && Event.current.rawType != EventType.Repaint;
            Vector4[] previous = null;
            if (input)
            {
                previous = new Vector4[Mats.Count];
                for (int i=0;i<Mats.Count;++i) previous[i] = Mats[i].GetVector("_ParallaxMapping_Vec");
                Undo.RecordObjects(MatEditor.targets, "Edit NB Parallax Inputs");
            }
            _graphParallaxItem.OnGUI();
            if (input) SyncService.TryFinalizeGraphParallaxLayerEdit(previous);
        }


        bool _sharedGraphMatCapReady;
        PropertyToggleBlockItem _graphMatCapBlock;
        bool _sharedGraphNormalMapReady;
        PropertyToggleBlockItem _graphNormalMapBlock;
        internal static readonly string[] SharedGraphNormalMapProperties={"_BumpMapToggle","_BumpTex","_BumpMapMaskMode","_BumpScale"};
        internal bool InitializeGraphNormalMapInputs()
        {
            Context??=new NBShaderGUIContext(this);SyncService??=new NBShaderSyncService(this);Context.Refresh();
            _sharedGraphNormalMapReady=Context.IsGraphMaterialHost && SyncService.HasGraphNormalMapEditSchema();
            if(!_sharedGraphNormalMapReady)return false;
            _graphNormalMapBlock??=LightBigBlockItem.CreateNormalMapBlock(this,null,true);return true;
        }
        void DrawGraphNormalMapInputs()
        {
            if(!InitializeGraphNormalMapInputs())return;
            if(Event.current!=null && Event.current.rawType!=EventType.Layout && Event.current.rawType!=EventType.Repaint)
                Undo.RecordObjects(MatEditor.targets,"Edit NB Normal Map Inputs");
            _graphNormalMapBlock.OnGUI();
        }

        bool _sharedGraphNoiseReady;
        NoiseAndDistortFeatureItem _graphNoiseItem;
        internal bool InitializeGraphNoiseInputs()
        {
            Context??=new NBShaderGUIContext(this);SyncService??=new NBShaderSyncService(this);Context.Refresh();
            _sharedGraphNoiseReady=Context.IsGraphMaterialHost && SyncService.HasGraphNoiseEditSchema();
            if(!_sharedGraphNoiseReady)return false;
            _graphNoiseItem??=new NoiseAndDistortFeatureItem(this,null,true);return true;
        }
        void DrawGraphNoiseInputs()
        {
            if(!InitializeGraphNoiseInputs())return;
            if(Event.current!=null && Event.current.rawType!=EventType.Layout && Event.current.rawType!=EventType.Repaint)
                Undo.RecordObjects(MatEditor.targets,"Edit NB Noise Inputs");
            _graphNoiseItem.OnGUI();
        }

        public override void OnChildOnGUI()
        {
            if (Context == null)
            {
                Context = new NBShaderGUIContext(this);
                SyncService = new NBShaderSyncService(this);
            }

            Context.Refresh();
            if (Context.HasMixedMaterialHosts)
            {
                EditorGUILayout.HelpBox("NB shared Surface Inputs are read-only for mixed Graph/ShaderLab or different Graph shader selections.", MessageType.Info);
                return;
            }
            if (Context.IsGraphMaterialHost)
            {
                if (IsInit)
                {
                    _graphOriginalMode=_graphOriginalBase=_graphOriginalLight=_graphOriginalFeature=_graphOriginalTA=null;
                    _graphOfficialZTest=null;_graphOfficialCull=null;_graphOfficialZWrite=_graphOfficialBlend=null;_graphOfficialCutoff=null;
                    _mainTexBlock = null;
                    _graphFlipbookItem = null;
                    _graphParallaxItem = null;
                    _graphNoiseItem=null;_sharedGraphNoiseReady=false;
                    _graphVATItem=null;_sharedGraphVATReady=false;
                    _graphStencilWithoutPlayerItem=null;_sharedGraphStencilWithoutPlayerReady=false;
                    _graphSharedUVItem=null;_sharedGraphSharedUVReady=false;
                    _sharedGraphParallaxReady = false;
                    _graphFresnelItem = null;
                    _graphColorAdjustmentBlock=null;_graphColorRampItem=null;
                    _graphDepthDecalItem=null;_sharedGraphDepthDecalReady=false;
                    _graphDistanceFadeBlock=null;_graphSoftParticlesBlock=null;_graphDepthOutlineItem=null;_sharedGraphDepthFeaturesReady=false;
                    _sharedGraphColorAdjustmentReady=false;_sharedGraphColorRampReady=false;
                    _graphDissolveItem = null;
                    _graphVertexOffsetItem=null;_sharedGraphVertexOffsetReady=false;
                    _graphMaskItem=null;_graphProgramNoiseItem=null;_sharedGraphMaskProgramReady=false;
                    _sharedGraphDissolveReady = false;
                    _sharedGraphFresnelReady = false;
                    _graphLightModeBlock = null;
                    _graphBaseBackColorItem=null;_sharedGraphBaseBackColorReady=false;
                    _graphBaseNumericBlock=null;_sharedGraphBaseNumericReady=false;
                    _graphBlendModeBlock=null;_graphKeywordListBlock=null;_sharedGraphRemainingUIReady=false;
                    _sharedGraphBaseShadowReady=false;_graphAffectsShadowsItem=null;_graphTransparentShadowDitherItem=null;_graphIgnoreVertexColorItem=null;
                    _sharedGraphLightSubReady = false;
                    _graphNormalMapBlock = null;
                    _graphMatCapBlock=null;_sharedGraphMatCapReady=false;
                    _sharedGraphNormalMapReady = false;
                    _graphTADepthBlock = null;
                    _toolBar = null;
                    _modeBlock = null;
                    _baseBlock = null;
                    _graphChromaticItem=null;_sharedGraphChromaticReady=false;
                    _graphPortalItem = null; _sharedGraphPortalReady = false;
                    _graphBackFirstItem = null; _sharedGraphBackFirstHost = _sharedGraphBackFirstReady = false;
                    _lightBlock = null;
                    _featureBlock = null;
                    _taBlock = null;
                    _particleVertexStreamsItem = null;
                }
                bool mainReady=InitializeGraphMainTextureInputs();
                _toolBar??=new NBShaderGUIToolBar(this);_toolBar.DrawGraphTierSelector();
                if(!PrepareGraphOriginalSections())
                {
                    EditorGUILayout.HelpBox("Original NB Feature section requires its real Float foldout metadata. No generic Surface Inputs fallback is used.",MessageType.Error);
                    return;
                }
                _graphOriginalMode.OnGUI();_graphOriginalBase.OnGUI();
                if(mainReady)_mainTexBlock.OnGUI();
                else EditorGUILayout.HelpBox("Shared Main Texture schema is incomplete.",MessageType.Error);
                _graphOriginalLight.OnGUI();_graphOriginalFeature.OnGUI();_graphOriginalTA.OnGUI();
                return;
            }

            if (IsInit)
            {
                _toolBar = new NBShaderGUIToolBar(this);
                _modeBlock = new ModeBigBlockItem(this, null);
                _baseBlock = new BaseOptionBigBlockItem(this, null);
                _mainTexBlock = new MainTexBigBlockItem(this, null);
                _lightBlock = new LightBigBlockItem(this, null);
                _featureBlock = new FeatureBigBlockItem(this, null);
                _taBlock = new TABigBlockItem(this, null);
                _particleVertexStreamsItem = new ParticleVertexStreamsItem(this, null);
            }

            bool syncMaterialState = IsInit;
            EditorGUI.BeginChangeCheck();

            _toolBar ??= new NBShaderGUIToolBar(this);
            _toolBar.DrawToolbar();

            _modeBlock.OnGUI();
            _baseBlock.OnGUI();
            _mainTexBlock.OnGUI();
            if (Context.UIEffectEnabled == MixedBool.False)
            {
                _lightBlock.OnGUI();
            }

            _featureBlock.OnGUI();
            _taBlock.OnGUI();

            if (EditorGUI.EndChangeCheck())
            {
                syncMaterialState = true;
            }

            if (syncMaterialState)
            {
                SyncService.SyncMaterialState();
            }

            _particleVertexStreamsItem.OnGUI();
        }

        public void ExecuteResetAllItems()
        {
            if (Context != null && Context.HasMixedMaterialHosts) return;
            if(Context!=null&&Context.IsGraphMaterialHost){TryResetGraphOwnedItems();return;}
            _modeBlock?.ExecuteReset(true);
            _baseBlock?.ExecuteReset(true);
            _mainTexBlock?.ExecuteReset(true);
            _lightBlock?.ExecuteReset(true);
            _featureBlock?.ExecuteReset(true);
            _taBlock?.ExecuteReset(true);
        }

        public System.Collections.Generic.IEnumerable<ShaderGUIItem> GetToolbarResetRootItems()
        {
            if(Context!=null&&Context.IsGraphMaterialHost)
            {
                System.Collections.Generic.List<ShaderGUIItem> items;
                if(TryGetGraphResetRootItems(out items))foreach(ShaderGUIItem item in items)yield return item;
                yield break;
            }
            if (_baseBlock != null)
            {
                yield return _baseBlock;
            }

            if (_mainTexBlock != null)
            {
                yield return _mainTexBlock;
            }

            if (_lightBlock != null)
            {
                yield return _lightBlock;
            }

            if (_featureBlock != null)
            {
                yield return _featureBlock;
            }

            if (_taBlock != null)
            {
                yield return _taBlock;
            }
        }

        static readonly string[] SharedGraphFresnelProperties = {
            "_FresnelBlockFoldOut", "_fresnelEnabled", "_FresnelMode", "_FresnelColor", "_FresnelUnit",
            "_InvertFresnel_Toggle", "_FresnelColorAffectByAlpha", "_FresnelRotation", "_NB_Debug_Fresnel"
        };

        internal bool InitializeGraphFresnelInputs()
        {
            Context ??= new NBShaderGUIContext(this);
            SyncService ??= new NBShaderSyncService(this);
            Context.Refresh();
            _sharedGraphFresnelReady = Context.IsGraphMaterialHost && SyncService.HasGraphFresnelEditSchema();
            if (!_sharedGraphFresnelReady) return false;
            _graphFresnelItem ??= new FresnelFeatureItem(this, null, true);
            return true;
        }

        internal bool InitializeGraphDissolveInputs()
        {
            Context??=new NBShaderGUIContext(this);SyncService??=new NBShaderSyncService(this);Context.Refresh();
            _sharedGraphDissolveReady=Context.IsGraphMaterialHost&&SyncService.HasGraphDissolveEditSchema();
            if(!_sharedGraphDissolveReady)return false;_graphDissolveItem??=new DissolveFeatureItem(this,null,true);return true;
        }
        internal void DrawGraphDissolveInputs(ShaderGUIItem selectedItem=null)
        {
            if(!_sharedGraphDissolveReady)return;
            EventType type=Event.current.type;
            if(type!=EventType.Layout && type!=EventType.Repaint)
                Undo.RecordObjects(MatEditor.targets,"Edit NB Dissolve");
            (selectedItem??_graphDissolveItem).OnGUI();
        }

        internal bool InitializeGraphMaskProgramInputs()
        {
            Context??=new NBShaderGUIContext(this);SyncService??=new NBShaderSyncService(this);Context.Refresh();
            _sharedGraphMaskProgramReady=Context.IsGraphMaterialHost&&SyncService.HasGraphMaskProgramEditSchema();
            if(!_sharedGraphMaskProgramReady)return false;_graphMaskItem??=new MaskFeatureItem(this,null,true);_graphProgramNoiseItem??=new ProgramNoiseFeatureItem(this,null,true);return true;
        }
        internal void DrawGraphMaskProgramInputs(ShaderGUIItem selectedItem=null)
        {
            if(!_sharedGraphMaskProgramReady)return;
            if(Event.current.type!=EventType.Layout&&Event.current.type!=EventType.Repaint)Undo.RecordObjects(MatEditor.targets,"Edit NB Mask / Program Noise");
            if(selectedItem!=null)selectedItem.OnGUI();else{_graphMaskItem.OnGUI();_graphProgramNoiseItem.OnGUI();}
        }

        internal bool InitializeGraphColorAdjustmentInputs()
        {
            Context??=new NBShaderGUIContext(this);SyncService??=new NBShaderSyncService(this);Context.Refresh();
            _sharedGraphColorAdjustmentReady=Context.IsGraphMaterialHost&&SyncService.HasGraphColorAdjustmentEditSchema();
            if(!_sharedGraphColorAdjustmentReady)return false;
            _graphColorAdjustmentBlock??=BaseOptionBigBlockItem.CreateColorAdjustmentBlock(this,null,true);return true;
        }
        internal bool InitializeGraphColorRampInputs()
        {
            Context??=new NBShaderGUIContext(this);SyncService??=new NBShaderSyncService(this);Context.Refresh();
            _sharedGraphColorRampReady=Context.IsGraphMaterialHost&&SyncService.HasGraphColorRampEditSchema();
            if(!_sharedGraphColorRampReady)return false;
            _graphColorRampItem??=new RampColorFeatureItem(this,null,true);return true;
        }
        internal void DrawGraphColorAdjustmentInputs(ShaderGUIItem selectedItem=null)
        {
            if(!InitializeGraphColorAdjustmentInputs())return;
            if(Event.current!=null&&Event.current.rawType!=EventType.Layout&&Event.current.rawType!=EventType.Repaint)
                Undo.RecordObjects(MatEditor.targets,"Edit NB Color Adjustment");
            (selectedItem??_graphColorAdjustmentBlock).OnGUI();
        }
        internal void DrawGraphColorRampInputs(ShaderGUIItem selectedItem=null)
        {
            if(!InitializeGraphColorRampInputs())return;
            if(Event.current!=null&&Event.current.rawType!=EventType.Layout&&Event.current.rawType!=EventType.Repaint)
                Undo.RecordObjects(MatEditor.targets,"Edit NB Color Ramp");
            (selectedItem??_graphColorRampItem).OnGUI();
        }

        internal void DrawGraphTAInputs(ShaderGUIItem selectedItem=null)
        {
            if(!_sharedGraphTADepthReady)return;
            if(Event.current.type!=EventType.Layout&&Event.current.type!=EventType.Repaint)Undo.RecordObjects(MatEditor.targets,"Edit NB TA");
            (selectedItem??_graphTADepthBlock).OnGUI();
        }

        internal bool InitializeGraphMatCapInputs()
        {
            Context??=new NBShaderGUIContext(this);SyncService??=new NBShaderSyncService(this);Context.Refresh();
            _sharedGraphMatCapReady=Context.IsGraphMaterialHost&&SyncService.HasGraphMatCapEditSchema();
            if(!_sharedGraphMatCapReady)return false;
            _graphMatCapBlock??=LightBigBlockItem.CreateMatCapBlock(this,null,true);return true;
        }
        internal void DrawGraphMatCapInputs(ShaderGUIItem selectedItem=null)
        {
            if(!InitializeGraphMatCapInputs())return;
            if(Event.current!=null&&Event.current.rawType!=EventType.Layout&&Event.current.rawType!=EventType.Repaint)
                Undo.RecordObjects(MatEditor.targets,"Edit NB MatCap Inputs");
            (selectedItem??_graphMatCapBlock).OnGUI();
        }

        internal bool InitializeGraphDepthFeaturesInputs()
        {
            Context??=new NBShaderGUIContext(this);SyncService??=new NBShaderSyncService(this);Context.Refresh();
            _sharedGraphDepthFeaturesReady=Context.IsGraphMaterialHost&&SyncService.HasGraphDepthFeaturesEditSchema();
            if(!_sharedGraphDepthFeaturesReady)return false;
            _graphDistanceFadeBlock??=BaseOptionBigBlockItem.CreateDistanceFadeBlock(this,null,true);
            _graphSoftParticlesBlock??=BaseOptionBigBlockItem.CreateSoftParticlesBlock(this,null,true);
            _graphDepthOutlineItem??=new DepthOutlineFeatureItem(this,null,true);return true;
        }
        internal void DrawGraphDepthFeaturesInputs(ShaderGUIItem selectedItem=null)
        {
            if(!InitializeGraphDepthFeaturesInputs())return;
            if(Event.current!=null&&Event.current.rawType!=EventType.Layout&&Event.current.rawType!=EventType.Repaint)
                Undo.RecordObjects(MatEditor.targets,"Edit NB Depth Features");
            if(selectedItem!=null){selectedItem.OnGUI();return;}
            _graphDistanceFadeBlock.OnGUI();_graphSoftParticlesBlock.OnGUI();_graphDepthOutlineItem.OnGUI();
        }
        internal bool InitializeGraphSharedUVInputs()
        {
            Context??=new NBShaderGUIContext(this);SyncService??=new NBShaderSyncService(this);Context.Refresh();
            _sharedGraphSharedUVReady=Context.IsGraphMaterialHost&&SyncService.HasGraphSharedUVEditSchema();
            if(!_sharedGraphSharedUVReady)return false;
            _graphSharedUVItem??=new SharedUVFeatureItem(this,null,true);return true;
        }
        internal void DrawGraphSharedUVInputs(ShaderGUIItem selectedItem=null)
        {
            if(!InitializeGraphSharedUVInputs())return;
            if(Event.current!=null&&Event.current.rawType!=EventType.Layout&&Event.current.rawType!=EventType.Repaint)
                Undo.RecordObjects(MatEditor.targets,"Edit NB Shared UV");
            (selectedItem??_graphSharedUVItem).OnGUI();
        }


        internal bool InitializeGraphVertexOffsetInputs()
        {
            Context??=new NBShaderGUIContext(this);SyncService??=new NBShaderSyncService(this);Context.Refresh();_sharedGraphVertexOffsetReady=Context.IsGraphMaterialHost&&SyncService.HasGraphVertexOffsetEditSchema();
            if(!_sharedGraphVertexOffsetReady)return false;_graphVertexOffsetItem??=new VertexOffsetFeatureItem(this,null,true);return true;
        }
        internal void DrawGraphVertexOffsetInputs(ShaderGUIItem selectedItem=null)
        {
            if(!_sharedGraphVertexOffsetReady)return;if(Event.current.type!=EventType.Layout&&Event.current.type!=EventType.Repaint)Undo.RecordObjects(MatEditor.targets,"Edit NB Vertex Offset");(selectedItem??_graphVertexOffsetItem).OnGUI();
        }
        internal bool InitializeGraphBaseShadowInputs()
        {
            if(!InitializeGraphBaseNumericInputs())return false;
            _sharedGraphBaseShadowReady=SyncService.HasGraphBaseShadowSchema();if(!_sharedGraphBaseShadowReady)return false;
            _graphAffectsShadowsItem??=BaseOptionBigBlockItem.CreateAffectsShadowsItem(this,_graphBaseNumericBlock,graphShared:true);
            _graphTransparentShadowDitherItem??=BaseOptionBigBlockItem.CreateTransparentShadowDitherItem(this,_graphBaseNumericBlock,
                ()=>Context.TransparentMode==TransparentMode.Transparent&&PropertyInfoDic.TryGetValue("_AffectsShadows",out ShaderPropertyInfo info)&&!info.Property.hasMixedValue&&info.Property.floatValue>.5f,true);
            _graphIgnoreVertexColorItem??=BaseOptionBigBlockItem.CreateIgnoreVertexColorItem(this,_graphBaseNumericBlock,graphShared:true);return true;
        }
        internal void DrawGraphBaseShadowInputs(ShaderGUIItem selectedItem)
        {
            if(!InitializeGraphBaseShadowInputs()||selectedItem==null)return;
            if(Event.current!=null&&Event.current.rawType!=EventType.Layout&&Event.current.rawType!=EventType.Repaint)Undo.RecordObjects(MatEditor.targets,"Edit NB Shadow/Vertex Color");selectedItem.OnGUI();
        }

        internal bool InitializeGraphBaseNumericInputs()
        {
            Context??=new NBShaderGUIContext(this);SyncService??=new NBShaderSyncService(this);Context.Refresh();
            _sharedGraphBaseNumericReady=Context.IsGraphMaterialHost&&SyncService.HasGraphBaseNumericSchema();if(!_sharedGraphBaseNumericReady)return false;
            _graphBaseNumericBlock??=BaseOptionBigBlockItem.CreateGraphBaseNumericBlock(this,null);return true;
        }
        internal void DrawGraphBaseNumericInputs(ShaderGUIItem selectedItem=null)
        {
            if(!InitializeGraphBaseNumericInputs())return;
            if(Event.current!=null&&Event.current.rawType!=EventType.Layout&&Event.current.rawType!=EventType.Repaint)Undo.RecordObjects(MatEditor.targets,"Edit NB Base Numeric");
            if(selectedItem==null)_graphBaseNumericBlock.OnGUI();else selectedItem.OnGUI();
        }

        internal bool InitializeGraphBaseBackColorInputs()
        {
            Context??=new NBShaderGUIContext(this);SyncService??=new NBShaderSyncService(this);Context.Refresh();
            _sharedGraphBaseBackColorReady=Context.IsGraphMaterialHost&&SyncService.HasGraphBaseBackColorEditSchema();
            if(!_sharedGraphBaseBackColorReady)return false;
            _graphBaseBackColorItem??=BaseOptionBigBlockItem.CreateBaseBackColorBlock(this,null,out _,
                ()=>Context.UIEffectEnabled==MixedBool.False,true);return true;
        }
        internal void DrawGraphBaseBackColorInputs(ShaderGUIItem selectedItem=null)
        {
            if(!InitializeGraphBaseBackColorInputs())return;
            if(Event.current!=null&&Event.current.rawType!=EventType.Layout&&Event.current.rawType!=EventType.Repaint)
                Undo.RecordObjects(MatEditor.targets,"Edit NB Back Color");
            if(selectedItem==null)_graphBaseBackColorItem.OnGUI();else selectedItem.OnGUI();
        }

        internal bool InitializeGraphStencilWithoutPlayerInputs()
        {
            Context??=new NBShaderGUIContext(this);SyncService??=new NBShaderSyncService(this);Context.Refresh();
            _sharedGraphStencilWithoutPlayerReady=Context.IsGraphMaterialHost&&SyncService.HasGraphStencilWithoutPlayerEditSchema();
            if(!_sharedGraphStencilWithoutPlayerReady)return false;
            _graphStencilWithoutPlayerItem??=BaseOptionBigBlockItem.CreateStencilWithoutPlayerItem(this,null,
                isVisible:()=>Context.UIEffectEnabled==MixedBool.False,graphSharedMode:true);return true;
        }
        internal void DrawGraphStencilWithoutPlayerInputs()
        {
            if(!InitializeGraphStencilWithoutPlayerInputs())return;
            if(Event.current!=null&&Event.current.rawType!=EventType.Layout&&Event.current.rawType!=EventType.Repaint)
                Undo.RecordObjects(MatEditor.targets,"Edit NB Stencil Without Player");
            _graphStencilWithoutPlayerItem.OnGUI();
        }


        internal bool InitializeGraphDepthDecalInputs()
        {
            Context??=new NBShaderGUIContext(this);SyncService??=new NBShaderSyncService(this);Context.Refresh();
            _sharedGraphDepthDecalReady=Context.IsGraphMaterialHost&&SyncService.HasGraphDepthDecalEditSchema();
            if(!_sharedGraphDepthDecalReady)return false;
            _graphDepthDecalItem??=DepthFeatureItem.CreateDepthDecalItem(this,null);return true;
        }
        internal void DrawGraphDepthDecalInputs()
        {
            if(!InitializeGraphDepthDecalInputs())return;
            if(Event.current!=null&&Event.current.rawType!=EventType.Layout&&Event.current.rawType!=EventType.Repaint)
                Undo.RecordObjects(MatEditor.targets,"Edit NB Depth Decal");
            _graphDepthDecalItem.OnGUI();
        }
        internal bool InitializeGraphBackFirstInputs()
        {
            Context ??= new NBShaderGUIContext(this);
            SyncService ??= new NBShaderSyncService(this);
            Context.Refresh();
            _sharedGraphBackFirstHost = Context.IsGraphMaterialHost && SyncService.HasGraphBackFirstUIHost();
            _sharedGraphBackFirstReady = _sharedGraphBackFirstHost && SyncService.HasGraphBackFirstEditSchema();
            if (!_sharedGraphBackFirstReady) return false;
            _graphBackFirstItem ??= BaseOptionBigBlockItem.CreateBackFirstPassToggle(this, null,
                enabled => SyncService.TryApplyGraphBackFirstToggle(enabled),
                () => Context.UIEffectEnabled == MixedBool.False && Context.TransparentMode == TransparentMode.Transparent, true);
            return true;
        }

        internal void DrawGraphBackFirstInputs()
        {
            bool ready = InitializeGraphBackFirstInputs();
            if (!_sharedGraphBackFirstHost) return;
            if (!ready)
            {
                EditorGUILayout.HelpBox("此动作将保留各材质当前主颜色开关，并启用现代 Back First 控制；不会恢复或推测旧材质历史。", MessageType.Info);
                using (new EditorGUI.DisabledScope(!SyncService.HasGraphBackFirstAdoptionSchema()))
                {
                    if (GUILayout.Button("保留当前主颜色并启用 Back First 控制"))
                        SyncService.TryAdoptGraphBackFirstCurrentMain();
                    _graphBackFirstAdoptRect = GUILayoutUtility.GetLastRect();
                }
                return;
            }
            if (Event.current.type != EventType.Layout && Event.current.type != EventType.Repaint)
                Undo.RecordObjects(MatEditor.targets, "Edit NB Back First");
            _graphBackFirstItem.OnGUI();
        }

        internal bool InitializeGraphVATInputs()
        {
            Context??=new NBShaderGUIContext(this);SyncService??=new NBShaderSyncService(this);Context.Refresh();
            _sharedGraphVATReady=Context.IsGraphMaterialHost&&SyncService.HasGraphVATEditSchema();
            if(!_sharedGraphVATReady)return false;
            bool mayInitialize=GraphGUIStateMayInitialize();
            if(!SyncService.TryInitializeGraphSupportedGateTierState()){_sharedGraphVATReady=false;return false;}
            if(mayInitialize){InvalidateGraphGUIReadPass();Context.Refresh();}
            _graphVATItem??=new VatFeatureItem(this,null,true);return true;
        }
        internal void DrawGraphVATInputs(ShaderGUIItem selectedItem=null)
        {
            if(!InitializeGraphVATInputs())return;
            if(Event.current!=null&&Event.current.rawType!=EventType.Layout&&Event.current.rawType!=EventType.Repaint)Undo.RecordObjects(MatEditor.targets,"Edit NB VAT");
            (selectedItem??_graphVATItem).OnGUI();
        }

        internal bool InitializeGraphPortalInputs()
        {
            Context ??= new NBShaderGUIContext(this); SyncService ??= new NBShaderSyncService(this); Context.Refresh();
            _sharedGraphPortalReady = Context.IsGraphMaterialHost && SyncService.HasGraphPortalEditSchema();
            if (!_sharedGraphPortalReady) return false;
            _graphPortalItem ??= new PortalFeatureItem(this, null, true); return true;
        }
        internal void DrawGraphPortalInputs(ShaderGUIItem selectedItem = null)
        {
            if (!InitializeGraphPortalInputs()) return;
            if (Event.current != null && Event.current.rawType != EventType.Layout && Event.current.rawType != EventType.Repaint)
                Undo.RecordObjects(MatEditor.targets, "Apply NB Portal preset");
            (selectedItem ?? _graphPortalItem).OnGUI();
            EditorGUILayout.HelpBox("此 Portal 预设保留当前渲染队列；如需自动队列，可在 URP Advanced 中选择 Auto。", MessageType.Info);
        }

        internal bool InitializeGraphChromaticInputs()
        {
            Context??=new NBShaderGUIContext(this);SyncService??=new NBShaderSyncService(this);Context.Refresh();
            _sharedGraphChromaticReady=Context.IsGraphMaterialHost&&SyncService.HasGraphChromaticEditSchema();
            if(!_sharedGraphChromaticReady)return false;_graphChromaticItem??=new ChromaticAberrationFeatureItem(this,null,true);return true;
        }
        internal void DrawGraphChromaticInputs(ShaderGUIItem selectedItem=null)
        {
            if(!InitializeGraphChromaticInputs())return;
            if(Event.current!=null&&Event.current.rawType!=EventType.Layout&&Event.current.rawType!=EventType.Repaint)Undo.RecordObjects(MatEditor.targets,"Edit NB Chromatic");
            (selectedItem??_graphChromaticItem).OnGUI();
        }

        internal bool TryGetGraphResetRootItems(out System.Collections.Generic.List<ShaderGUIItem> items)
        {
            items=null;Context??=new NBShaderGUIContext(this);SyncService??=new NBShaderSyncService(this);Context.Refresh();
            if(!Context.IsGraphMaterialHost||!SyncService.HasGraphSharedResetSchema())return false;
            // Every existing leaf preflight is complete before factories can initialize.
            if(!InitializeGraphRemainingSharedUI()||!InitializeGraphBaseShadowInputs()||!InitializeGraphBaseBackColorInputs()||!InitializeGraphDepthFeaturesInputs()||
                !InitializeGraphStencilWithoutPlayerInputs()||!InitializeGraphMainTextureInputs()||!InitializeGraphLightModeInputs()||
                !InitializeGraphNormalMapInputs()||!InitializeGraphMatCapInputs()||!InitializeGraphColorAdjustmentInputs()||
                !InitializeGraphMaskProgramInputs()||!InitializeGraphNoiseInputs()||!InitializeGraphChromaticInputs()||
                !InitializeGraphOverlayInputs()||!InitializeGraphColorRampInputs()||!InitializeGraphDissolveInputs()||
                !InitializeGraphSharedUVInputs()||!InitializeGraphFresnelInputs()||!InitializeGraphVertexOffsetInputs()||
                !InitializeGraphDepthDecalInputs()||!InitializeGraphParallaxInputs()||!InitializeGraphPortalInputs()||
                !InitializeGraphFlipbookInputs()||!InitializeGraphVATInputs()||!InitializeGraphTADepthInputs())return false;
            items=new System.Collections.Generic.List<ShaderGUIItem>{
                _graphBlendModeBlock,_graphBaseNumericBlock,_graphColorAdjustmentBlock,_graphBaseBackColorItem,
                _graphDistanceFadeBlock,_graphSoftParticlesBlock,_graphStencilWithoutPlayerItem,
                _mainTexBlock,_graphLightModeBlock,_graphNormalMapBlock,_graphMatCapBlock,
                _graphMaskItem,_graphNoiseItem,_graphChromaticItem,_graphEmissionItem,_graphColorRampItem,
                _graphDissolveItem,_graphColorBlendItem,_graphProgramNoiseItem,_graphSharedUVItem,
                _graphFresnelItem,_graphVertexOffsetItem,_graphDepthOutlineItem,_graphDepthDecalItem,
                _graphParallaxItem,_graphPortalItem,_graphFlipbookItem,_graphVATItem,_graphTADepthBlock,_graphKeywordListBlock};
            if(SyncService.HasGraphBackFirstEditSchema()&&InitializeGraphBackFirstInputs())items.Insert(2,_graphBackFirstItem);
            foreach(ShaderGUIItem item in items)if(!GraphResetDefaultsCompatible(item))return false;
            return true;
        }
        private bool GraphResetDefaultsCompatible(ShaderGUIItem item)
        {
            if(item.PropertyInfo?.Property!=null)
            {
                string name=item.PropertyInfo.Name;int first=Shader.FindPropertyIndex(name);
                foreach(Material value in Mats)
                {
                    int index=value.shader.FindPropertyIndex(name);if(first<0||index<0||value.shader.GetPropertyType(index)!=Shader.GetPropertyType(first))return false;
                    var type=Shader.GetPropertyType(first);
                    if((type==UnityEngine.Rendering.ShaderPropertyType.Float||type==UnityEngine.Rendering.ShaderPropertyType.Range)&&!value.shader.GetPropertyDefaultFloatValue(index).Equals(Shader.GetPropertyDefaultFloatValue(first)))return false;
                    if((type==UnityEngine.Rendering.ShaderPropertyType.Vector||type==UnityEngine.Rendering.ShaderPropertyType.Color)&&!value.shader.GetPropertyDefaultVectorValue(index).Equals(Shader.GetPropertyDefaultVectorValue(first)))return false;
                }
            }
            foreach(ShaderGUIItem child in item.ChildrenItemList)if(!GraphResetDefaultsCompatible(child))return false;
            return true;
        }
        internal bool TryResetGraphOwnedItems(bool disabledChildrenOnly=false)
        {
            System.Collections.Generic.List<ShaderGUIItem> items;
            if(!TryGetGraphResetRootItems(out items))return false;
            if(!disabledChildrenOnly&&(!GraphOfficialResetDefaultsCompatible(GraphOfficialModeNames)||
                !GraphOfficialResetDefaultsCompatible(GraphOfficialRenderStateNames)))return false;
            string[] officialNames=null;
            System.Collections.Generic.Dictionary<Material,float[]> officialBefore=null;
            if(disabledChildrenOnly)
            {
                officialNames=new[]{"_Surface","_AlphaClip","_Blend","_Cutoff","_ZTest","_Cull","_ZWriteControl"};
                officialBefore=new System.Collections.Generic.Dictionary<Material,float[]>();
                foreach(Material value in Mats)
                {
                    var values=new float[officialNames.Length];
                    for(int i=0;i<officialNames.Length;++i)
                    {
                        if(!HasFloatProperty(value,officialNames[i]))return false;
                        values[i]=value.GetFloat(officialNames[i]);
                        if(float.IsNaN(values[i])||float.IsInfinity(values[i]))return false;
                    }
                    officialBefore.Add(value,values);
                }
            }
            return SyncService.TryRunGraphSharedReset(()=>{
                if(!disabledChildrenOnly)ResetGraphOfficialDefaults(GraphOfficialModeNames);
                foreach(ShaderGUIItem item in items)
                    if(disabledChildrenOnly)ResetGraphDisabledChildren(item);else item.ExecuteReset(true);
                if(disabledChildrenOnly)
                {
                    foreach(var original in officialBefore)
                        for(int i=0;i<officialNames.Length;++i)original.Key.SetFloat(officialNames[i],original.Value[i]);
                }
                else ResetGraphOfficialDefaults(GraphOfficialRenderStateNames);
                ValidateGraphOfficialResetMaterials();
            },!disabledChildrenOnly);
        }
        private static void ResetGraphDisabledChildren(ShaderGUIItem item)
        {
            if(item is PropertyToggleBlockItem&&item.PropertyInfo?.Property!=null&&
                !item.PropertyInfo.Property.hasMixedValue&&item.PropertyInfo.Property.floatValue<=.5f)
            {
                // ExecuteReset already traverses its own subtree exactly once.
                foreach(ShaderGUIItem child in item.ChildrenItemList)child.ExecuteReset(true);
                item.CheckIsPropertyModified(true);return;
            }
            foreach(ShaderGUIItem child in item.ChildrenItemList)ResetGraphDisabledChildren(child);
        }

        BigBlockItem _graphBlendModeBlock;BlockItem _graphKeywordListBlock;bool _sharedGraphRemainingUIReady;
        internal bool InitializeGraphRemainingSharedUI()
        {
            Context??=new NBShaderGUIContext(this);SyncService??=new NBShaderSyncService(this);Context.Refresh();
            _sharedGraphRemainingUIReady=Context.IsGraphMaterialHost&&SyncService.HasGraphRemainingSharedUISchema();
            if(!_sharedGraphRemainingUIReady)return false;
            _graphBlendModeBlock??=ModeBigBlockItem.CreateGraphAdditiveBlendBlock(this,null);
            _graphKeywordListBlock??=TABigBlockItem.CreateKeywordsBlock(this,null,true);return true;
        }
        internal void DrawGraphRemainingSharedUI(ShaderGUIItem selectedItem=null)
        {
            if(!InitializeGraphRemainingSharedUI())return;
            if(Event.current!=null&&Event.current.rawType!=EventType.Layout&&Event.current.rawType!=EventType.Repaint)Undo.RecordObjects(MatEditor.targets,"NB Shared Mode/Keywords");
            if(selectedItem!=null){selectedItem.OnGUI();return;}
            _graphBlendModeBlock.OnGUI();if(Mats.Count==1)_graphKeywordListBlock.OnGUI();
        }
        internal bool CanUseGraphSharedToolbar(bool singleTarget=false)
        {
            if(Context==null||!Context.IsGraphMaterialHost||Context.HasMixedMaterialHosts||
                (singleTarget&&Mats.Count!=1)||SyncService==null)return false;
            using(SyncService.BeginGraphFreshPreflightRead())return SyncService.HasGraphRemainingSharedUISchema();
        }
        internal bool CanUseGraphSharedToolbarForDisplay(bool singleTarget=false)
        {
            if(Context==null||!Context.IsGraphMaterialHost||Context.HasMixedMaterialHosts||
                (singleTarget&&Mats.Count!=1)||SyncService==null)return false;
            int pass=GraphGUIReadPass;
            if(pass==0)return SyncService.HasGraphRemainingSharedUISchema(); // All actions/backends fresh.
            if(_toolbarDisplayReadPass!=pass||_toolbarDisplayShaderRevision!=ShaderPropertyTypeCacheRevision)
            {
                _toolbarDisplayReady=SyncService.HasGraphRemainingSharedUISchema();
                _toolbarDisplayReadPass=pass;_toolbarDisplayShaderRevision=ShaderPropertyTypeCacheRevision;
            }
            return _toolbarDisplayReady;
        }
        internal bool TryCollapseGraphOwnedFolds()
        {
            System.Collections.Generic.List<ShaderGUIItem> items;if(!CanUseGraphSharedToolbar()||!TryGetGraphResetRootItems(out items))return false;
            var names=new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);
            foreach(ShaderGUIItem item in items)CollectGraphOwnedFolds(item,names);
            if(PropertyInfoDic.ContainsKey("_FeatureBigBlockItemFoldOut"))names.Add("_FeatureBigBlockItemFoldOut");
            foreach(Material material in Mats)foreach(string name in names)
                if(!HasFloatProperty(material,name)||float.IsNaN(material.GetFloat(name))||float.IsInfinity(material.GetFloat(name)))return false;
            return SyncService.TryRunGraphKnownToolbarEdit(()=>{
                foreach(Material material in Mats)foreach(string name in names)material.SetFloat(name,0f);
            });
        }
        private static void CollectGraphOwnedFolds(ShaderGUIItem item,System.Collections.Generic.HashSet<string> names)
        {
            if(item is BlockItem block&&!string.IsNullOrEmpty(block.FoldOutPropertyName))names.Add(block.FoldOutPropertyName);
            if(item is PropertyToggleBlockItem toggle&&!string.IsNullOrEmpty(toggle.FoldOutPropertyName))names.Add(toggle.FoldOutPropertyName);
            if(item is TextureRelatedFoldOutItem related&&!string.IsNullOrEmpty(related.FoldOutPropertyName))names.Add(related.FoldOutPropertyName);
            if(item is UVModeSelectItem uv&&!string.IsNullOrEmpty(uv.FoldOutPropertyName))names.Add(uv.FoldOutPropertyName);
            if(item is ShaderGUIBigBlockItem oldBlock&&!string.IsNullOrEmpty(oldBlock.FoldOutPropertyName))names.Add(oldBlock.FoldOutPropertyName);
            foreach(ShaderGUIItem child in item.ChildrenItemList)CollectGraphOwnedFolds(child,names);
        }
        internal bool TryClearGraphOwnedClosedTextures()
        {
            System.Collections.Generic.List<ShaderGUIItem> items;if(!CanUseGraphSharedToolbar()||!TryGetGraphResetRootItems(out items))return false;
            var names=new System.Collections.Generic.Dictionary<Material,System.Collections.Generic.HashSet<string>>();
            foreach(Material material in Mats)
            {
                var fields=new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);
                foreach(ShaderGUIItem item in items)CollectGraphClosedTextures(item,material,false,fields);names.Add(material,fields);
            }
            return SyncService.TryRunGraphKnownToolbarEdit(()=>{
                foreach(var target in names)foreach(string name in target.Value)if(target.Key.GetTexture(name)!=null)target.Key.SetTexture(name,null);
            });
        }
        private static void CollectGraphClosedTextures(ShaderGUIItem item,Material material,bool closed,System.Collections.Generic.HashSet<string> fields)
        {
            if(item is PropertyToggleBlockItem&&item.PropertyInfo?.Property!=null)
                closed|=material.GetFloat(item.PropertyName)<=.5f;
            if(closed&&item is TextureObjectItem&&!string.IsNullOrEmpty(item.PropertyName)&&
                material.shader.GetPropertyType(material.shader.FindPropertyIndex(item.PropertyName))==UnityEngine.Rendering.ShaderPropertyType.Texture)fields.Add(item.PropertyName);
            foreach(ShaderGUIItem child in item.ChildrenItemList)CollectGraphClosedTextures(child,material,closed,fields);
        }

    }
}
