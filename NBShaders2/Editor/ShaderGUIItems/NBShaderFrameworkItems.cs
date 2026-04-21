using UnityEditor;
using UnityEngine;

namespace NBShaderEditor
{
    public class NBShaderBlockItem : ShaderGUIBigBlockItem
    {
        protected readonly NBShaderRootItem NBRootItem;
        private readonly string _labelKey;
        private readonly string _labelFallback;
        private readonly string _tipKey;
        private readonly string _tipFallback;

        public NBShaderBlockItem(
            NBShaderRootItem rootItem,
            ShaderGUIItem parentItem,
            string foldOutPropertyName,
            string labelKey,
            string labelFallback,
            string tipKey,
            string tipFallback) : base(rootItem, parentItem)
        {
            NBRootItem = rootItem;
            FoldOutPropertyName = foldOutPropertyName;
            _labelKey = labelKey;
            _labelFallback = labelFallback;
            _tipKey = tipKey;
            _tipFallback = tipFallback;
        }

        public override void OnGUI()
        {
            GuiContent = NBShaderInspectorLocalization.MakeContent(_labelKey, _labelFallback, _tipKey, _tipFallback);
            base.OnGUI();
        }
    }

    public class NBShaderFeatureItem : ShaderGUIItem
    {
        public string FeatureId { get; set; }

        public NBShaderFeatureItem(ShaderGUIRootItem rootItem, ShaderGUIItem parentItem) : base(rootItem, parentItem)
        {
        }
    }

    public class NBShaderControlItem : ShaderGUIItem
    {
        protected readonly NBShaderRootItem NBRootItem;

        public NBShaderControlItem(NBShaderRootItem rootItem, ShaderGUIItem parentItem) : base(rootItem, parentItem)
        {
            NBRootItem = rootItem;
        }

        protected void SetLocalizedContent(string labelKey, string labelFallback, string tipKey = null, string tipFallback = "")
        {
            GuiContent = NBShaderInspectorLocalization.MakeContent(labelKey, labelFallback, tipKey, tipFallback);
        }
    }

    public class NBShaderHelpBoxItem : NBShaderControlItem
    {
        private readonly MessageType _messageType;
        private readonly string _labelKey;
        private readonly string _labelFallback;

        public NBShaderHelpBoxItem(
            NBShaderRootItem rootItem,
            ShaderGUIItem parentItem,
            string labelKey,
            string labelFallback,
            MessageType messageType = MessageType.Info) : base(rootItem, parentItem)
        {
            _labelKey = labelKey;
            _labelFallback = labelFallback;
            _messageType = messageType;
        }

        public override void OnGUI()
        {
            EditorGUILayout.HelpBox(NBShaderInspectorLocalization.Get(_labelKey, _labelFallback), _messageType);
        }
    }
}
