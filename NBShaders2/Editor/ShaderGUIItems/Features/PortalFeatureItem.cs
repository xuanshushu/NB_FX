using System;
using NBShader;
using UnityEditor;
using UnityEngine;

namespace NBShaderEditor
{
    internal sealed class PortalFeatureItem : FeatureToggleFoldOutItem
    {
        public PortalFeatureItem(NBShaderRootItem rootItem, ShaderGUIItem parentItem, bool graphSharedMode = false)
            : base(rootItem, parentItem, "_PortalBlockFoldOut", "_Portal_Toggle", "模板视差", onValueChanged: _ => { if (graphSharedMode) rootItem.SyncService.TryApplyGraphPortalState(); else rootItem.SyncService.ApplyPortalState(); }, isVisible: () => rootItem.Context.UIEffectEnabled != MixedBool.True, graphPortalEdit:graphSharedMode)
        {
            new ToggleItem(rootItem, this, "_Portal_MaskToggle", () => Content("模板视差蒙版"), graphSharedMode ? null : (Action<bool>)(_ => rootItem.SyncService.ApplyPortalState()))
            { WriteOnlyOnInteractiveChange = graphSharedMode,
                TryWriteValue = graphSharedMode ? (Func<bool,bool>)(value => rootItem.SyncService.TryApplyGraphPortalToggle("_Portal_MaskToggle",value)) : null };
            InitTriggerByChild();
        }
    }
}
