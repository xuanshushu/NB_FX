using UnityEditor;
using UnityEngine;
using NBShader;

namespace NBShaderEditor
{
    public class NBShaderGUI : ShaderGUI
    {
        private NBShaderRootItem _rootItem;
        private NBShaderGraphRootItem _graphRootItem;
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

        // URP retains Surface Options/Advanced. Shared blocks incrementally own
        // Surface Inputs; the existing Graph root temporarily draws remaining
        // native inputs so PNoise/SixWay/etc never lose their editing entry.
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
            _graphRootItem ??= new NBShaderGraphRootItem();
            _graphRootItem.OnGUI(materialEditor, properties, _rootItem.GetSharedGraphPropertyNames());
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
        static readonly string[] SharedGraphLightModeProperties = { "_FxLightMode", "_LightBigBlockItemFoldOut" };

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
            base.OnGUI(editor, properties);
        }

        static readonly string[] SharedGraphMainTextureProperties =
        {
            "_BaseMap", "_Color", "_BaseMap_ST", "_BaseMapUVRotation",
            "_BaseMapUVRotationSpeed", "_BaseMapMaskMapOffset", "_TexDistortion_intensity"
        };

        public System.Collections.Generic.IEnumerable<string> GetSharedGraphPropertyNames()
        {
            var names = new System.Collections.Generic.List<string>();
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

        internal static bool HasFloatProperty(Material material, string name)
        {
            if (!material.HasProperty(name)) return false;
            Shader shader = material.shader;
            for (int i = 0; i < shader.GetPropertyCount(); i++)
                if (shader.GetPropertyName(i) == name)
                    return shader.GetPropertyType(i) == UnityEngine.Rendering.ShaderPropertyType.Float;
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
            if (hasTierContract)
            {
                if (!SyncService.TryInitializeGraphSupportedGateTierState()) return false;
            }
            else SyncService.PrepareGraphGUIState();
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
                if (InitializeGraphMainTextureInputs())
                    _mainTexBlock.OnGUI();
                else
                    EditorGUILayout.HelpBox("Shared Main Texture needs its real Float foldouts/schema. Existing Graph native inputs remain available below.", MessageType.Info);
                if (InitializeGraphLightModeInputs())
                    DrawGraphLightInputs();
                DrawGraphNormalMapInputs();
                DrawGraphMatCapInputs();
                DrawGraphColorAdjustmentInputs();
                DrawGraphColorRampInputs();
                DrawGraphDepthFeaturesInputs();
                DrawGraphDepthDecalInputs();
                if (InitializeGraphFlipbookInputs()) _graphFlipbookItem.OnGUI();
                if (InitializeGraphTADepthInputs())DrawGraphTAInputs();
                if (InitializeGraphFresnelInputs()) _graphFresnelItem.OnGUI();
                if(InitializeGraphDissolveInputs())DrawGraphDissolveInputs();
                if(InitializeGraphVertexOffsetInputs())DrawGraphVertexOffsetInputs();
                if(InitializeGraphMaskProgramInputs())DrawGraphMaskProgramInputs();
                DrawGraphOverlayInputs();
                DrawGraphParallaxInputs();
                DrawGraphSharedUVInputs();
                DrawGraphNoiseInputs();
                DrawGraphChromaticInputs();
                DrawGraphVATInputs();
                DrawGraphBackFirstInputs();
                DrawGraphPortalInputs();
                DrawGraphStencilWithoutPlayerInputs();
                DrawGraphBaseBackColorInputs();
                _toolBar ??= new NBShaderGUIToolBar(this);
                _toolBar.DrawGraphTierSelector();
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
            _modeBlock?.ExecuteReset(true);
            _baseBlock?.ExecuteReset(true);
            _mainTexBlock?.ExecuteReset(true);
            _lightBlock?.ExecuteReset(true);
            _featureBlock?.ExecuteReset(true);
            _taBlock?.ExecuteReset(true);
        }

        public System.Collections.Generic.IEnumerable<ShaderGUIItem> GetToolbarResetRootItems()
        {
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
            if(!SyncService.TryInitializeGraphSupportedGateTierState()){_sharedGraphVATReady=false;return false;}
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

    }
}
