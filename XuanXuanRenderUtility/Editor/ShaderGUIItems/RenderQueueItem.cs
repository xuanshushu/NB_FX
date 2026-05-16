using System;
using UnityEditor;
using UnityEngine;

namespace NBShaderEditor
{
    public class RenderQueueItem : ShaderGUIFloatItem
    {
        private readonly Func<GUIContent> _contentProvider;
        private readonly Action _onQueueChanged;
        private readonly Func<bool> _isVisible;

        public RenderQueueItem(
            ShaderGUIRootItem rootItem,
            ShaderGUIItem parentItem,
            string propertyName,
            Func<GUIContent> contentProvider,
            Action onQueueChanged = null,
            Func<bool> isVisible = null) : base(rootItem, parentItem)
        {
            PropertyName = propertyName;
            _contentProvider = contentProvider ?? (() => GUIContent.none);
            _onQueueChanged = onQueueChanged;
            _isVisible = isVisible;
            GuiContent = _contentProvider();
            InitTriggerByChild();
        }

        public override void OnGUI()
        {
            if (_isVisible != null && !_isVisible())
            {
                return;
            }

            base.OnGUI();
        }

        public override void OnEndChange()
        {
            base.OnEndChange();
            _onQueueChanged?.Invoke();
        }

        public override void ExecuteReset(bool isCallByParent = false)
        {
            base.ExecuteReset(isCallByParent);
            _onQueueChanged?.Invoke();
        }
    }
}
