using System;
using NBShader;
using UnityEditor;
using UnityEngine;

namespace NBShaderEditor
{
    internal sealed class DepthFeatureItem : ShaderGUIItem
    {
        public DepthFeatureItem(NBShaderRootItem rootItem, ShaderGUIItem parentItem)
            : base(rootItem, parentItem)
        {
            new DepthOutlineFeatureItem(rootItem, this);
            new ToggleItem(
                rootItem,
                this,
                "_DepthDecal_Toggle",
                () => FeatureToggleFoldOutItem.Content("深度贴花"),
                rootItem.SyncService.ApplyDepthDecalEnabled,
                FeatureToggleFoldOutItem.TierVisible(
                    rootItem,
                    "_DEPTH_DECAL",
                    () => rootItem.Context.UIEffectEnabled != MixedBool.True));
            InitTriggerByChild();
        }

        public override void OnGUI()
        {
            for (int i = 0; i < ChildrenItemList.Count; i++)
            {
                ChildrenItemList[i].OnGUI();
            }
        }

        public override void CheckIsPropertyModified(bool isCallByChild = false)
        {
            HasModified = false;
            for (int i = 0; i < ChildrenItemList.Count; i++)
            {
                HasModified |= ChildrenItemList[i].HasModified;
            }

            ParentItem?.CheckIsPropertyModified(true);
        }
    }

    internal sealed class DepthOutlineFeatureItem : FeatureToggleFoldOutItem
    {
        public DepthOutlineFeatureItem(NBShaderRootItem rootItem, ShaderGUIItem parentItem)
            : base(
                rootItem,
                parentItem,
                "_DepthOutlineBlockFoldOut",
                "_DepthOutline_Toggle",
                "深度描边",
                keyword: "_DEPTH_OUTLINE",
                isVisible: () => rootItem.Context.UIEffectEnabled != MixedBool.True)
        {
            new ColorItem(rootItem, this, "_DepthOutline_Color", () => Content("深度描边颜色"));
            new Vector2LineItem(rootItem, this, "_DepthOutline_Vec", true, () => Content("深度描边距离"));
            InitTriggerByChild();
        }
    }
}
