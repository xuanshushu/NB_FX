using System;
using NBShader;

namespace NBShaderEditor
{
    internal sealed class ChromaticAberrationFeatureItem : FeatureToggleFoldOutItem
    {
        public ChromaticAberrationFeatureItem(NBShaderRootItem rootItem, ShaderGUIItem parentItem, bool graphSharedMode=false)
            : base(
                rootItem,
                parentItem,
                "_ChromaticAberrationFoldOut",
                "_Distortion_Choraticaberrat_Toggle",
                "色散",
                keyword: graphSharedMode?null:"_CHROMATIC_ABERRATION", graphChromaticEdit:graphSharedMode)
        {
            ShaderGUIItem chromaticNoiseAffect = new NoiseAffectItem(rootItem, this);
            new ToggleItem(
                rootItem,
                chromaticNoiseAffect,
                "_Distortion_Choraticaberrat_WithNoise_Toggle",
                () => Content("色散强度受扭曲强度影响"),
                graphSharedMode?null:(Action<bool>)(enabled => rootItem.SyncService.ApplyToggleFlag(NBShaderFlags.FLAG_BIT_PARTICLE_NOISE_CHORATICABERRAT_WITH_NOISE, enabled)))
            {WriteOnlyOnInteractiveChange=graphSharedMode,TryWriteValue=graphSharedMode?(Func<bool,bool>)(enabled=>rootItem.SyncService.TryApplyGraphChromaticNoiseFlag(enabled)):null};
            new VectorComponentItem(rootItem, this, "_DistortionDirection", 2, () => Content("色散强度"), false) {TryWriteComponent=graphSharedMode?(Func<float,bool>)(v=>rootItem.SyncService.TryApplyGraphChromaticIntensity(v)):null};
            new CustomDataSelectItem(rootItem, this, NBShaderFlags.FLAGBIT_POS_0_CUSTOMDATA_CHORATICABERRAT_INTENSITY, 0, () => Content("色散强度自定义曲线"),graphChromaticData:graphSharedMode);
            InitTriggerByChild();
        }
    }
}
