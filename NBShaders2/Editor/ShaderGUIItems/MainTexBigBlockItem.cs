using UnityEditor;

namespace NBShaderEditor
{
    public class MainTexBigBlockItem : BigBlockItem
    {
        private readonly NBShaderRootItem _nbRootItem;
        private readonly TexturePropertyGroupItem _baseMapGroupItem;
        private readonly ColorLineItem _uiColorItem;
        private readonly TextureScaleOffsetItem _uiMainTexScaleOffsetItem;
        private readonly Vector2LineItem _baseMapOffsetSpeedItem;
        private readonly ShaderGUISliderItem _baseMapRotationItem;
        private readonly ShaderGUIFloatItem _baseMapRotationSpeedItem;
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
                isVisible: () => rootItem.Context.UseGraphicMainTex == MixedBool.False);

            _graphicMainTexHelpBox = new HelpBoxItem(
                rootItem,
                this,
                () => NBShaderInspectorLocalization.Get(
                    "inspector.maintex.graphic.tip",
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
                isVisible: () => rootItem.Context.UseGraphicMainTex == MixedBool.True);

            _baseMapOffsetSpeedItem = new Vector2LineItem(
                rootItem,
                this,
                "_BaseMapMaskMapOffset",
                true,
                () => NBShaderInspectorLocalization.MakeContent(
                    "inspector.maintex.offsetspeed.label",
                    "Offset Speed"),
                isVisible: () => rootItem.Context.UseGraphicMainTex == MixedBool.False);

            _baseMapRotationItem = new ShaderGUISliderItem(rootItem, this)
            {
                PropertyName = "_BaseMapUVRotation",
                GuiContent = NBShaderInspectorLocalization.MakeContent(
                    "inspector.maintex.rotation.label",
                    "Rotation"),
                Min = 0f,
                Max = 360f
            };
            _baseMapRotationItem.InitTriggerByChild();

            _baseMapRotationSpeedItem = new ShaderGUIFloatItem(rootItem, this)
            {
                PropertyName = "_BaseMapUVRotationSpeed",
                GuiContent = NBShaderInspectorLocalization.MakeContent(
                    "inspector.maintex.rotationspeed.label",
                    "Rotation Speed")
            };
            _baseMapRotationSpeedItem.InitTriggerByChild();

            InitTriggerByChild();
        }

        public override void DrawBlock()
        {
            if (_nbRootItem.Context.UseGraphicMainTex == MixedBool.Mixed)
            {
                EditorGUILayout.HelpBox("Mixed mesh-source modes will show the matching texture controls per material state.", MessageType.Info);
            }

            _baseMapGroupItem.OnGUI();

            if (_nbRootItem.Context.UseGraphicMainTex == MixedBool.True)
            {
                _graphicMainTexHelpBox.OnGUI();
            }

            _uiColorItem.OnGUI();
            _uiMainTexScaleOffsetItem.OnGUI();
            _baseMapOffsetSpeedItem.OnGUI();
            if (_nbRootItem.Context.UseGraphicMainTex == MixedBool.False)
            {
                _baseMapRotationItem.OnGUI();
                _baseMapRotationSpeedItem.OnGUI();
            }
        }
    }
}
