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

        public override void InitFlags(System.Collections.Generic.List<Material> mats)
        {
            ShaderFlags = new System.Collections.Generic.List<ShaderFlagsBase>();
            foreach (Material mat in mats)
            {
                ShaderFlags.Add(new NBShaderFlags(mat));
            }
        }

        bool _sharedGraphMainTextureReady;
        bool _sharedGraphFlipbookReady;
        FlipbookFeatureItem _graphFlipbookItem;
        bool _sharedGraphFresnelReady;
        FresnelFeatureItem _graphFresnelItem;
        bool _sharedGraphTADepthReady;
        BigBlockItem _graphTADepthBlock;
        bool _sharedGraphLightModeReady;
        BigBlockItem _graphLightModeBlock;
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
            if (Context != null && Context.IsGraphMaterialHost && _sharedGraphTADepthReady)
                names.AddRange(NBShaderSyncService.GraphTADepthProperties);
            if (Context != null && Context.IsGraphMaterialHost && _sharedGraphMainTextureReady)
                names.AddRange(SharedGraphMainTextureProperties);
            if (Context != null && Context.IsGraphMaterialHost && _sharedGraphLightModeReady)
                names.AddRange(SharedGraphLightModeProperties);
            if (Context != null && Context.IsGraphMaterialHost && _sharedGraphFlipbookReady) names.Add("_FlipbookBlending");
            if (Context != null && _sharedGraphMainTextureReady && Context.CanEditGraphMainTexUV) names.AddRange(new[] { "_UTwirlEnabled", "_PolarCoordinatesEnabled", "_TWParameter", "_TWStrength", "_PCCenter", "_CylinderUVRotate", "_CylinderUVPosOffset", "_WorldSpaceUVModeSelector", "_ObjectSpaceUVModeSelector" });
            if (Context != null && Context.IsGraphMaterialHost && _sharedGraphFresnelReady) names.AddRange(SharedGraphFresnelProperties);
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
            _graphLightModeBlock ??= LightBigBlockItem.CreateGraphModeOnlyBlock(this, null);
            return true;
        }

        internal bool InitializeGraphTADepthInputs()
        {
            Context ??= new NBShaderGUIContext(this);
            SyncService ??= new NBShaderSyncService(this);
            Context.Refresh();
            _sharedGraphTADepthReady = Context.IsGraphMaterialHost && SyncService.HasGraphTADepthEditSchema();
            if (!_sharedGraphTADepthReady) return false;
            _graphTADepthBlock ??= TABigBlockItem.CreateGraphDepthOnlyBlock(this, null);
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
                    _graphFresnelItem = null;
                    _sharedGraphFresnelReady = false;
                    _graphLightModeBlock = null;
                    _graphTADepthBlock = null;
                    _toolBar = null;
                    _modeBlock = null;
                    _baseBlock = null;
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
                    _graphLightModeBlock.OnGUI();
                if (InitializeGraphFlipbookInputs()) _graphFlipbookItem.OnGUI();
                if (InitializeGraphTADepthInputs()) _graphTADepthBlock.OnGUI();
                if (InitializeGraphFresnelInputs()) _graphFresnelItem.OnGUI();
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
    }
}
