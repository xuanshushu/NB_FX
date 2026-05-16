using System;
using UnityEditor;

namespace NBShaderEditor
{
    public class HelpBoxItem : ShaderGUIItem
    {
        private readonly MessageType _messageType;
        private readonly Func<string> _messageProvider;
        private readonly Func<bool> _isVisible;

        public HelpBoxItem(
            ShaderGUIRootItem rootItem,
            ShaderGUIItem parentItem,
            Func<string> messageProvider,
            MessageType messageType = MessageType.Info,
            Func<bool> isVisible = null) : base(rootItem, parentItem)
        {
            _messageProvider = messageProvider ?? (() => string.Empty);
            _messageType = messageType;
            _isVisible = isVisible;
        }

        public override void OnGUI()
        {
            if (_isVisible != null && !_isVisible())
            {
                return;
            }

            using (ParentControlDisabledScope())
            {
                DrawLayoutHelpBox(_messageProvider(), _messageType);
            }
        }
    }
}
