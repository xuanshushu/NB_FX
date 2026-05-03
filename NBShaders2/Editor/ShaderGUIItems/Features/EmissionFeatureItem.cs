using System;
using NBShader;
using UnityEditor;
using UnityEngine;

namespace NBShaderEditor
{
    internal sealed class EmissionFeatureItem : FeatureToggleFoldOutItem
    {
        public EmissionFeatureItem(NBShaderRootItem rootItem, ShaderGUIItem parentItem)
            : base(rootItem, parentItem, "_EmissionBlockFoldOut", "_EmissionEnabled", "流光(颜色相加)", keyword: "_EMISSION")
        {
            AddTextureWithWrap(rootItem, this, "_EmissionMap", "流光贴图", W9ParticleShaderFlags.FLAG_BIT_WRAPMODE_EMISSIONMAP, "_EmissionMapColor");
            new UVModeSelectItem(rootItem, this, "_EmissionUVModeFoldOut", W9ParticleShaderFlags.FLAG_BIT_UVMODE_POS_0_EMISSION_MAP, 0, () => Content("流光贴图UV来源"), "_EmissionMap");
            new CustomDataSelectItem(rootItem, this, W9ParticleShaderFlags.FLAGBIT_POS_3_CUSTOMDATA_EMISSION_OFFSET_X, 3, () => Content("流光贴图X轴偏移自定义曲线"));
            new CustomDataSelectItem(rootItem, this, W9ParticleShaderFlags.FLAGBIT_POS_3_CUSTOMDATA_EMISSION_OFFSET_Y, 3, () => Content("流光贴图Y轴偏移自定义曲线"));
            ShaderGUISliderItem emissionMapUVRotationItem = new ShaderGUISliderItem(rootItem, this)
            {
                PropertyName = "_EmissionMapUVRotation",
                GuiContent = Content("流光贴图旋转"),
                Min = 0f,
                Max = 360f
            };
            emissionMapUVRotationItem.InitTriggerByChild();
            new Vector2LineItem(rootItem, this, "_EmissionMapUVOffset", true, () => Content("流光贴图偏移速度"));
            ShaderGUIItem emissionNoiseAffect = new NoiseAffectItem(rootItem, this);
            ShaderGUIFloatItem emissionDistortionIntensityItem = new ShaderGUIFloatItem(rootItem, emissionNoiseAffect)
            {
                PropertyName = "_Emi_Distortion_intensity",
                GuiContent = Content("流光贴图扭曲强度")
            };
            emissionDistortionIntensityItem.InitTriggerByChild();
            ShaderGUIFloatItem emissionMapColorIntensityItem = new ShaderGUIFloatItem(rootItem, this)
            {
                PropertyName = "_EmissionMapColorIntensity",
                GuiContent = Content("流光颜色强度")
            };
            emissionMapColorIntensityItem.InitTriggerByChild();
            InitTriggerByChild();
        }
    }
}
