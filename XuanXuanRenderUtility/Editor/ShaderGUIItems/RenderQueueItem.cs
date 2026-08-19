using System;
using UnityEditor;
using UnityEngine;

namespace NBShaderEditor
{
    public class RenderQueueItem : ShaderGUIFloatItem
    {
        private readonly string _label;
        private readonly Func<Material, int> _baseQueueProvider;
        private readonly Func<bool> _isVisible;
        private int _displayedRenderQueue;
        private bool _displayedMixedQueue;
        private bool _queueLabelInitialized;

        public RenderQueueItem(
            ShaderGUIRootItem rootItem,
            ShaderGUIItem parentItem,
            string propertyName,
            Func<GUIContent> contentProvider,
            Func<Material, int> baseQueueProvider,
            Func<bool> isVisible = null) : base(rootItem, parentItem)
        {
            PropertyName = propertyName;
            GUIContent content = contentProvider?.Invoke() ?? GUIContent.none;
            _label = content.text;
            _baseQueueProvider = baseQueueProvider ?? (mat => mat != null && mat.shader != null ? mat.shader.renderQueue : 0);
            _isVisible = isVisible;
            GuiContent = new GUIContent(content);
            InitTriggerByChild();
            InitializeQueueBiasFromRenderQueue();
            CheckIsPropertyModified();
        }

        public override void OnGUI()
        {
            if (_isVisible != null && !_isVisible())
            {
                return;
            }

            UpdateQueueLabel();
            base.OnGUI();
        }

        public override void DrawController()
        {
            GetActualQueueBias(out float queueBias, out bool mixedQueueBias);

            bool previousMixedValue = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = mixedQueueBias;
            EditorGUI.BeginChangeCheck();
            float value = EditorGUI.FloatField(ControlRect, queueBias);
            if (EditorGUI.EndChangeCheck())
            {
                PropertyInfo.Property.floatValue = value;
            }

            EditorGUI.showMixedValue = previousMixedValue;
        }

        public override void OnEndChange()
        {
            base.OnEndChange();
            ApplyQueueBias();
        }

        public override void ExecuteReset(bool isCallByParent = false)
        {
            base.ExecuteReset(isCallByParent);
            ApplyQueueBias();
        }

        private void InitializeQueueBiasFromRenderQueue()
        {
            if (RootItem.Mats == null)
            {
                return;
            }

            for (int i = 0; i < RootItem.Mats.Count; i++)
            {
                Material mat = RootItem.Mats[i];
                if (mat != null && mat.HasProperty(PropertyName))
                {
                    mat.SetFloat(PropertyName, mat.renderQueue - _baseQueueProvider(mat));
                }
            }

            RootItem.MatEditor?.Repaint();
        }

        private void ApplyQueueBias()
        {
            if (RootItem.Mats == null)
            {
                return;
            }

            int queueBias = Mathf.RoundToInt(PropertyInfo.Property.floatValue);
            for (int i = 0; i < RootItem.Mats.Count; i++)
            {
                Material mat = RootItem.Mats[i];
                if (mat != null)
                {
                    mat.renderQueue = _baseQueueProvider(mat) + queueBias;
                }
            }
        }

        private void UpdateQueueLabel()
        {
            GetActualRenderQueue(out int renderQueue, out bool mixedQueue);
            if (_queueLabelInitialized &&
                _displayedRenderQueue == renderQueue &&
                _displayedMixedQueue == mixedQueue)
            {
                return;
            }

            _displayedRenderQueue = renderQueue;
            _displayedMixedQueue = mixedQueue;
            _queueLabelInitialized = true;
            GuiContent.text = _label + ":" + (mixedQueue ? "-" : renderQueue.ToString());
        }

        private void GetActualRenderQueue(out int renderQueue, out bool mixedQueue)
        {
            renderQueue = 0;
            mixedQueue = false;
            if (RootItem.Mats == null)
            {
                return;
            }

            bool hasQueue = false;
            for (int i = 0; i < RootItem.Mats.Count; i++)
            {
                Material mat = RootItem.Mats[i];
                if (mat == null)
                {
                    continue;
                }

                if (!hasQueue)
                {
                    renderQueue = mat.renderQueue;
                    hasQueue = true;
                }
                else if (renderQueue != mat.renderQueue)
                {
                    mixedQueue = true;
                    return;
                }
            }
        }

        private void GetActualQueueBias(out float queueBias, out bool mixedQueueBias)
        {
            queueBias = 0f;
            mixedQueueBias = false;
            if (RootItem.Mats == null)
            {
                return;
            }

            bool hasQueueBias = false;
            for (int i = 0; i < RootItem.Mats.Count; i++)
            {
                Material mat = RootItem.Mats[i];
                if (mat == null)
                {
                    continue;
                }

                float currentQueueBias = mat.renderQueue - _baseQueueProvider(mat);
                if (!hasQueueBias)
                {
                    queueBias = currentQueueBias;
                    hasQueueBias = true;
                }
                else if (!Mathf.Approximately(queueBias, currentQueueBias))
                {
                    mixedQueueBias = true;
                    return;
                }
            }
        }
    }
}
