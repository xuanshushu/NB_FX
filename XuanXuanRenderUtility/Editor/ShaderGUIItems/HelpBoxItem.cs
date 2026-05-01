using System;
using UnityEditor;

namespace NBShaderEditor
{
    public class HelpBoxItem : ShaderGUIItem
    {
        private readonly MessageType _messageType;
        private readonly Func<string> _messageProvider;

        public HelpBoxItem(
            ShaderGUIRootItem rootItem,
            ShaderGUIItem parentItem,
            Func<string> messageProvider,
            MessageType messageType = MessageType.Info) : base(rootItem, parentItem)
        {
            _messageProvider = messageProvider ?? (() => string.Empty);
            _messageType = messageType;
        }

        public override void OnGUI()
        {
            EditorGUILayout.HelpBox(_messageProvider(), _messageType);
        }
    }
}
