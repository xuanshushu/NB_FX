using System;
using NBShader;
using UnityEditor;
using UnityEngine;

namespace NBShaderEditor
{
    internal sealed class PortalFeatureItem : FeatureToggleFoldOutItem
    {
        public PortalFeatureItem(NBShaderRootItem rootItem, ShaderGUIItem parentItem)
            : base(rootItem, parentItem, "_PortalBlockFoldOut", "_Portal_Toggle", "模板视差", onValueChanged: _ => rootItem.SyncService.ApplyPortalState(), isVisible: () => rootItem.Context.UIEffectEnabled != MixedBool.True)
        {
            new ToggleItem(rootItem, this, "_Portal_MaskToggle", () => Content("模板视差蒙版"), _ => rootItem.SyncService.ApplyPortalState());
            InitTriggerByChild();
        }
    }
}
