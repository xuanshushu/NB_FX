using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace NBShaderEditor
{
    public class TABigBlockItem : BigBlockItem
    {
        private readonly NBShaderRootItem _nbRootItem;
        private readonly PropertyToggleBlockItem _zOffsetBlock;
        private readonly RenderQueueItem _renderQueueItem;
        private readonly PropertyToggleBlockItem _customStencilBlock;
        private readonly BlockItem _keywordBlock;

        public TABigBlockItem(NBShaderRootItem rootItem, ShaderGUIItem parentItem)
            : base(
                rootItem,
                parentItem,
                "_TABigBlockItemFoldOut",
                () => Content("block.ta", "TA Debug", "Technical artist debug and helper controls"))
        {
            _nbRootItem = rootItem;

            _zOffsetBlock = new PropertyToggleBlockItem(
                rootItem,
                this,
                "_ZOffsetBlockFoldOut",
                "_ZOffset_Toggle",
                () => Content("ta.zoffset", "Z Offset"),
                onValueChanged: OnZOffsetChanged,
                isVisible: () => rootItem.Context.UIEffectEnabled != MixedBool.True,
                bold: true);
            AddFloat(rootItem, _zOffsetBlock, "_offsetFactor", "Offset Factor");
            AddFloat(rootItem, _zOffsetBlock, "_offsetUnits", "Offset Units");

            _renderQueueItem = new RenderQueueItem(
                rootItem,
                this,
                "_QueueBias",
                () => Content("ta.renderQueue", "Queue Bias"),
                rootItem.SyncService.SyncMaterialState);

            _customStencilBlock = new PropertyToggleBlockItem(
                rootItem,
                this,
                "_CustomStencilTestFoldOut",
                "_CustomStencilTest",
                () => Content("ta.customStencil", "Custom Stencil Test"),
                onValueChanged: OnCustomStencilChanged,
                bold: true);
            AddFloat(rootItem, _customStencilBlock, "_StencilKeyIndex", "Stencil Config Index");
            AddFloat(rootItem, _customStencilBlock, "_Stencil", "Stencil Value");
            AddPopup(rootItem, _customStencilBlock, "_StencilComp", "Stencil Compare", Enum.GetNames(typeof(CompareFunction)));
            AddPopup(rootItem, _customStencilBlock, "_StencilOp", "Stencil Operation", Enum.GetNames(typeof(StencilOp)));

            _keywordBlock = new BlockItem(
                rootItem,
                this,
                "_ShaderKeywordFoldOut",
                () => Content("ta.keywords", "Enabled Keywords"));
            new KeywordListItem(rootItem, _keywordBlock, () => Content("ta.keywords.list", "Enabled Keywords"));

            InitTriggerByChild();
        }

        public override void DrawBlock()
        {
            _zOffsetBlock.OnGUI();
            _renderQueueItem.OnGUI();
            _customStencilBlock.OnGUI();
            if (_nbRootItem.Mats.Count == 1)
            {
                _keywordBlock.OnGUI();
            }
        }

        private void OnZOffsetChanged(bool enabled)
        {
            if (enabled)
            {
                return;
            }

            SetFloat("_offsetFactor", 0f);
            SetFloat("_offsetUnits", 0f);
        }

        private void OnCustomStencilChanged(bool enabled)
        {
            if (enabled)
            {
                return;
            }

            _nbRootItem.SyncService.ApplyStencilPreset("ParticleBaseDefault");
        }

        private void SetFloat(string propertyName, float value)
        {
            for (int i = 0; i < _nbRootItem.Mats.Count; i++)
            {
                Material mat = _nbRootItem.Mats[i];
                if (mat != null && mat.HasProperty(propertyName))
                {
                    mat.SetFloat(propertyName, value);
                }
            }
        }

        private static void AddFloat(NBShaderRootItem rootItem, ShaderGUIItem parentItem, string propertyName, string label)
        {
            ShaderGUIFloatItem item = new ShaderGUIFloatItem(rootItem, parentItem)
            {
                PropertyName = propertyName,
                GuiContent = NBShaderInspectorLocalization.MakeInspectorContent("ta.property." + propertyName, label)
            };
            item.InitTriggerByChild();
        }

        private static void AddPopup(NBShaderRootItem rootItem, ShaderGUIItem parentItem, string propertyName, string label, string[] options)
        {
            ShaderGUIPopUpItem item = new ShaderGUIPopUpItem(rootItem, parentItem)
            {
                PropertyName = propertyName,
                GuiContent = NBShaderInspectorLocalization.MakeInspectorContent("ta.property." + propertyName, label),
                PopUpNames = options
            };
            item.InitTriggerByChild();
        }

        private static GUIContent Content(string key, string fallback, string tip = "")
        {
            return NBShaderInspectorLocalization.MakeContent("inspector." + key + ".label", fallback, "inspector." + key + ".tip", tip);
        }
    }
}
