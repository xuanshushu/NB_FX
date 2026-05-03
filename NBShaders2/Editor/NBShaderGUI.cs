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
                ShaderFlags.Add(new W9ParticleShaderFlags(mat));
            }
        }

        public override void OnChildOnGUI()
        {
            if (Context == null)
            {
                Context = new NBShaderGUIContext(this);
                SyncService = new NBShaderSyncService(this);
            }

            Context.Refresh();

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
            SyncService.SyncMaterialState();
            _particleVertexStreamsItem.OnGUI();
        }

        public void ExecuteResetAllItems()
        {
            _modeBlock?.ExecuteReset(true);
            _baseBlock?.ExecuteReset(true);
            _mainTexBlock?.ExecuteReset(true);
            _lightBlock?.ExecuteReset(true);
            _featureBlock?.ExecuteReset(true);
            _taBlock?.ExecuteReset(true);
        }
    }
}
