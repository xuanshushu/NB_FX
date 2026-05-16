using UnityEditor;
using UnityEngine;
using NBShader;

namespace NBShaderEditor
{
    public class MainTexBigBlockItem : BigBlockItem
    {
        private readonly NBShaderRootItem _nbRootItem;
        private readonly TexturePropertyGroupItem _baseMapGroupItem;
        private readonly TextureRelatedFoldOutItem _baseMapRelatedFoldOutItem;
        private readonly ColorLineItem _uiColorItem;
        private readonly TextureScaleOffsetItem _uiMainTexScaleOffsetItem;
        private readonly ColorChannelSelectItem _alphaChannelItem;
        private readonly WrapModeItem _baseMapWrapModeItem;
        private readonly UVModeSelectItem _uvModeItem;
        private readonly CustomDataSelectItem _offsetXCustomDataItem;
        private readonly CustomDataSelectItem _offsetYCustomDataItem;
        private readonly Vector2LineItem _baseMapOffsetSpeedItem;
        private readonly ShaderGUISliderItem _baseMapRotationItem;
        private readonly ShaderGUIFloatItem _baseMapRotationSpeedItem;
        private readonly ShaderGUISliderItem _texDistortionIntensityItem;
        private readonly PNoiseBlendModeItem _pNoiseBlendModeItem;
        private readonly HelpBoxItem _graphicMainTexHelpBox;

        public MainTexBigBlockItem(NBShaderRootItem rootItem, ShaderGUIItem parentItem)
            : base(
                rootItem,
                parentItem,
                "_MainTexBigBlockItemFoldOut",
                () => NBShaderInspectorLocalization.MakeContent(
                    "inspector.block.maintex.label",
                    "Main Texture",
                    "inspector.block.maintex.tip",
                    "Main texture and base color controls"))
        {
            _nbRootItem = rootItem;
            _baseMapGroupItem = new TexturePropertyGroupItem(
                rootItem,
                this,
                "_BaseMap",
                "_BaseColor",
                () => NBShaderInspectorLocalization.MakeContent(
                    "inspector.maintex.basemap.label",
                    "Main Texture"),
                isVisible: () => rootItem.Context.UseGraphicMainTex == MixedBool.False,
                tillingContentProvider: TillingContent,
                offsetContentProvider: OffsetContent);

            _baseMapRelatedFoldOutItem = new TextureRelatedFoldOutItem(
                rootItem,
                this,
                "_BaseMapFoldOut",
                "_BaseMap",
                () => NBShaderInspectorLocalization.MakeContent(
                    "inspector.maintex.basemap.related.label",
                    "Main Texture Related"),
                () => rootItem.Context.UseGraphicMainTex == MixedBool.False);

            _graphicMainTexHelpBox = new HelpBoxItem(
                rootItem,
                this,
                () => NBShaderInspectorLocalization.Get(
                    "inspector.maintex.graphic.message",
                    "Current mode uses Graphic texture. Only color and ST remain editable."),
                MessageType.Info);

            _uiColorItem = new ColorLineItem(
                rootItem,
                this,
                "_Color",
                showLabel: false,
                contentProvider: () => NBShaderInspectorLocalization.MakeContent(
                    "inspector.maintex.uicolor.label",
                    "Graphic Color"),
                isVisible: () => rootItem.Context.UseGraphicMainTex == MixedBool.True);

            _uiMainTexScaleOffsetItem = new TextureScaleOffsetItem(
                rootItem,
                this,
                "_UI_MainTex_ST",
                isVectorProperty: true,
                isVisible: () => rootItem.Context.UseGraphicMainTex == MixedBool.True,
                tillingContentProvider: TillingContent,
                offsetContentProvider: OffsetContent);

            _alphaChannelItem = new ColorChannelSelectItem(
                rootItem,
                _baseMapRelatedFoldOutItem,
                NBShaderFlags.FLAG_BIT_COLOR_CHANNEL_POS_0_MAINTEX_ALPHA,
                3,
                () => NBShaderInspectorLocalization.MakeContent(
                    "inspector.maintex.alphaChannel.label",
                    "Alpha Channel"));

            _baseMapWrapModeItem = new WrapModeItem(
                rootItem,
                _baseMapRelatedFoldOutItem,
                NBShaderFlags.FLAG_BIT_WRAPMODE_BASEMAP,
                () => NBShaderInspectorLocalization.MakeContent(
                    "inspector.maintex.wrap.label",
                    "Main Texture Wrap"),
                2,
                () => rootItem.Context.UseGraphicMainTex == MixedBool.False);

            _uvModeItem = new UVModeSelectItem(
                rootItem,
                _baseMapRelatedFoldOutItem,
                "_MainTexUVModeFoldOut",
                NBShaderFlags.FLAG_BIT_UVMODE_POS_0_MAINTEX,
                0,
                () => NBShaderInspectorLocalization.MakeContent(
                    "inspector.maintex.uvmode.label",
                    "Main Texture UV Source"),
                forceEnable: true,
                isVisible: () => rootItem.Context.MeshSourceMode != MeshSourceMode.UIEffectSprite);

            _offsetXCustomDataItem = new CustomDataSelectItem(
                rootItem,
                _baseMapRelatedFoldOutItem,
                NBShaderFlags.FLAGBIT_POS_0_CUSTOMDATA_MAINTEX_OFFSET_X,
                0,
                () => NBShaderInspectorLocalization.MakeContent(
                    "inspector.maintex.customdata.offsetx.label",
                    "Main Texture Offset X Custom Data"),
                () => rootItem.Context.ParticleMode == MixedBool.True);

            _offsetYCustomDataItem = new CustomDataSelectItem(
                rootItem,
                _baseMapRelatedFoldOutItem,
                NBShaderFlags.FLAGBIT_POS_0_CUSTOMDATA_MAINTEX_OFFSET_Y,
                0,
                () => NBShaderInspectorLocalization.MakeContent(
                    "inspector.maintex.customdata.offsety.label",
                    "Main Texture Offset Y Custom Data"),
                () => rootItem.Context.ParticleMode == MixedBool.True);

            _baseMapOffsetSpeedItem = new Vector2LineItem(
                rootItem,
                _baseMapRelatedFoldOutItem,
                "_BaseMapMaskMapOffset",
                true,
                () => NBShaderInspectorLocalization.MakeContent(
                    "inspector.maintex.offsetspeed.label",
                    "Offset Speed"),
                isVisible: () => rootItem.Context.UseGraphicMainTex == MixedBool.False);

            _baseMapRotationItem = new ShaderGUISliderItem(rootItem, _baseMapRelatedFoldOutItem)
            {
                PropertyName = "_BaseMapUVRotation",
                GuiContent = NBShaderInspectorLocalization.MakeContent(
                    "inspector.maintex.rotation.label",
                    "Rotation"),
                Min = 0f,
                Max = 360f
            };
            _baseMapRotationItem.InitTriggerByChild();

            _baseMapRotationSpeedItem = new ShaderGUIFloatItem(rootItem, _baseMapRelatedFoldOutItem)
            {
                PropertyName = "_BaseMapUVRotationSpeed",
                GuiContent = NBShaderInspectorLocalization.MakeContent(
                    "inspector.maintex.rotationspeed.label",
                    "Rotation Speed")
            };
            _baseMapRotationSpeedItem.InitTriggerByChild();

            _texDistortionIntensityItem = new ShaderGUISliderItem(rootItem, _baseMapRelatedFoldOutItem)
            {
                PropertyName = "_TexDistortion_intensity",
                GuiContent = NBShaderInspectorLocalization.MakeContent(
                    "inspector.maintex.distortionIntensity.label",
                    "Main Texture Distortion"),
                RangePropertyName = "TexDistortionintensityRangeVec"
            };
            _texDistortionIntensityItem.InitTriggerByChild();

            _pNoiseBlendModeItem = new PNoiseBlendModeItem(
                rootItem,
                _baseMapRelatedFoldOutItem,
                NBShaderFlags.FLAG_BIT_PNOISE_BLEND_POS_0_MAINTEX,
                "_MainTexPNoiseBlendOpacity",
                () => NBShaderInspectorLocalization.MakeContent(
                    "inspector.maintex.pnoiseBlend.label",
                    "Main Texture Program Noise Blend"),
                () => rootItem.Context.ProgramNoiseEnabled == MixedBool.True);

            InitTriggerByChild();
        }

        public override void DrawBlock()
        {
            if (_nbRootItem.Context.UseGraphicMainTex == MixedBool.Mixed)
            {
                DrawLayoutHelpBox(
                    NBShaderInspectorLocalization.GetInspectorText(
                        "maintex.mixedMeshSource.message",
                        "Mixed mesh-source modes will show the matching texture controls per material state."),
                    MessageType.Info);
            }

            _baseMapGroupItem.OnGUI();

            if (_nbRootItem.Context.UseGraphicMainTex == MixedBool.True)
            {
                _graphicMainTexHelpBox.OnGUI();
            }

            _uiColorItem.OnGUI();
            _uiMainTexScaleOffsetItem.OnGUI();
            if (_nbRootItem.Context.UseGraphicMainTex == MixedBool.False)
            {
                _baseMapRelatedFoldOutItem.OnGUI();
            }
            else
            {
                DrawMainTexRelatedItems();
            }
        }

        private void DrawMainTexRelatedItems()
        {
            _alphaChannelItem.OnGUI();
            _baseMapWrapModeItem.OnGUI();
            _uvModeItem.OnGUI();
            _offsetXCustomDataItem.OnGUI();
            _offsetYCustomDataItem.OnGUI();
            _baseMapOffsetSpeedItem.OnGUI();
            if (_nbRootItem.Context.UseGraphicMainTex == MixedBool.False)
            {
                _baseMapRotationItem.OnGUI();
                _baseMapRotationSpeedItem.OnGUI();
            }

            if (_nbRootItem.Context.UseGraphicMainTex == MixedBool.False)
            {
                bool previousMixedValue = EditorGUI.showMixedValue;
                EditorGUI.showMixedValue = _nbRootItem.Context.NoiseEnabled == MixedBool.Mixed;
                using (new InheritedControlDisabledScope(_nbRootItem.Context.NoiseEnabled == MixedBool.False))
                {
                    _texDistortionIntensityItem.OnGUI();
                }

                EditorGUI.showMixedValue = previousMixedValue;
            }

            _pNoiseBlendModeItem.OnGUI();
        }

        private static GUIContent TillingContent()
        {
            return NBShaderInspectorLocalization.MakeInspectorContent("common.tilling", "Tilling");
        }

        private static GUIContent OffsetContent()
        {
            return NBShaderInspectorLocalization.MakeInspectorContent("common.offset", "Offset");
        }
    }
}
