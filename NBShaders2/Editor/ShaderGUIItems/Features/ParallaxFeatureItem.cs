using System;
using NBShader;
using UnityEditor;
using UnityEngine;

namespace NBShaderEditor
{
    internal sealed class ParallaxFeatureItem : FeatureToggleFoldOutItem
    {
        public ParallaxFeatureItem(NBShaderRootItem rootItem, ShaderGUIItem parentItem)
            : base(rootItem, parentItem, "_ParallaxBlockFoldOut", "_ParallaxMapping_Toggle", "遮蔽视差", NBShaderFlags.FLAG_BIT_PARTICLE_1_PARALLAX_MAPPING, 1, keyword: "_PARALLAX_MAPPING", isVisible: () => rootItem.Context.UIEffectEnabled != MixedBool.True)
        {
            AddTextureWithWrap(rootItem, this, "_ParallaxMapping_Map", "视差贴图", NBShaderFlags.FLAG_BIT_WRAPMODE_PARALLAXMAPPINGMAP);
            ShaderGUISliderItem parallaxMappingIntensityItem = new ShaderGUISliderItem(rootItem, this)
            {
                PropertyName = "_ParallaxMapping_Intensity",
                GuiContent = Content("视差"),
                RangePropertyName = "_ParallaxMapping_IntensityRangeVec"
            };
            parallaxMappingIntensityItem.InitTriggerByChild();
            new VectorComponentItem(rootItem, this, "_ParallaxMapping_Vec", 0, () => Content("遮蔽视差最小层数"), true, 0f, 100f);
            new VectorComponentItem(rootItem, this, "_ParallaxMapping_Vec", 1, () => Content("遮蔽视差最大层数"), true, 0f, 100f);
            new HelpBoxItem(rootItem, this, () => Text("feature.parallax.layerWarning.message", "遮蔽视差层数过高将影响性能"), MessageType.Warning,
                () => IsParallaxMaxLayerHigh(rootItem));
            InitTriggerByChild();
        }

        private static bool IsParallaxMaxLayerHigh(NBShaderRootItem rootItem)
        {
            return rootItem.PropertyInfoDic.TryGetValue("_ParallaxMapping_Vec", out ShaderPropertyInfo info) &&
                   !info.Property.hasMixedValue &&
                   info.Property.vectorValue.y >= 20f;
        }
    }
}
