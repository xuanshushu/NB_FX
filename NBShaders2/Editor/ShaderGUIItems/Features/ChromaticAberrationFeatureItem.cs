using NBShader;

namespace NBShaderEditor
{
    internal sealed class ChromaticAberrationFeatureItem : FeatureToggleFoldOutItem
    {
        public ChromaticAberrationFeatureItem(NBShaderRootItem rootItem, ShaderGUIItem parentItem)
            : base(
                rootItem,
                parentItem,
                "_ChromaticAberrationFoldOut",
                "_Distortion_Choraticaberrat_Toggle",
                "色散",
                keyword: "_CHROMATIC_ABERRATION")
        {
            ShaderGUIItem chromaticNoiseAffect = new NoiseAffectItem(rootItem, this);
            new ToggleItem(
                rootItem,
                chromaticNoiseAffect,
                "_Distortion_Choraticaberrat_WithNoise_Toggle",
                () => Content("色散强度受扭曲强度影响"),
                enabled => rootItem.SyncService.ApplyToggleFlag(NBShaderFlags.FLAG_BIT_PARTICLE_NOISE_CHORATICABERRAT_WITH_NOISE, enabled));
            new VectorComponentItem(rootItem, this, "_DistortionDirection", 2, () => Content("色散强度"), false);
            new CustomDataSelectItem(rootItem, this, NBShaderFlags.FLAGBIT_POS_0_CUSTOMDATA_CHORATICABERRAT_INTENSITY, 0, () => Content("色散强度自定义曲线"));
            InitTriggerByChild();
        }
    }
}
