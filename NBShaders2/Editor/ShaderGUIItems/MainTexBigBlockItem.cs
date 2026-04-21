using UnityEditor;

namespace NBShaderEditor
{
    public class MainTexBigBlockItem : NBShaderBlockItem
    {
        private readonly NBShaderTexturePropertyItem _baseMapItem;
        private readonly NBShaderColorPropertyItem _uiColorItem;
        private readonly NBShaderVector2LinePropertyItem _uiMainTexTilingItem;
        private readonly NBShaderVector2LinePropertyItem _uiMainTexOffsetItem;
        private readonly NBShaderVector2LinePropertyItem _baseMapOffsetSpeedItem;
        private readonly NBShaderSliderPropertyItem _baseMapRotationItem;
        private readonly NBShaderFloatPropertyItem _baseMapRotationSpeedItem;
        private readonly NBShaderHelpBoxItem _graphicMainTexHelpBox;

        public MainTexBigBlockItem(NBShaderRootItem rootItem, ShaderGUIItem parentItem)
            : base(
                rootItem,
                parentItem,
                "_MainTexBigBlockItemFoldOut",
                "inspector.block.maintex.label",
                "主贴图功能",
                "inspector.block.maintex.tip",
                "主贴图和主颜色相关功能")
        {
            _baseMapItem = new NBShaderTexturePropertyItem(
                rootItem,
                this,
                "_BaseMap",
                "inspector.maintex.basemap.label",
                "主贴图",
                "_BaseColor",
                drawScaleOffset: true,
                isVisible: () => rootItem.Context.UseGraphicMainTex == MixedBool.False);

            _graphicMainTexHelpBox = new NBShaderHelpBoxItem(
                rootItem,
                this,
                "inspector.maintex.graphic.tip",
                "当前模式下主贴图来自 Graphic，Inspector 只提供颜色和 ST 调整。",
                MessageType.Info);

            _uiColorItem = new NBShaderColorPropertyItem(
                rootItem,
                this,
                "_Color",
                "inspector.maintex.uicolor.label",
                "贴图颜色叠加",
                isVisible: () => rootItem.Context.UseGraphicMainTex == MixedBool.True);

            _uiMainTexTilingItem = new NBShaderVector2LinePropertyItem(
                rootItem,
                this,
                "_UI_MainTex_ST",
                true,
                "inspector.maintex.uitiling.label",
                "Tiling",
                isVisible: () => rootItem.Context.UseGraphicMainTex == MixedBool.True);

            _uiMainTexOffsetItem = new NBShaderVector2LinePropertyItem(
                rootItem,
                this,
                "_UI_MainTex_ST",
                false,
                "inspector.maintex.uioffset.label",
                "Offset",
                isVisible: () => rootItem.Context.UseGraphicMainTex == MixedBool.True);

            _baseMapOffsetSpeedItem = new NBShaderVector2LinePropertyItem(
                rootItem,
                this,
                "_BaseMapMaskMapOffset",
                true,
                "inspector.maintex.offsetspeed.label",
                "偏移速度",
                isVisible: () => rootItem.Context.UseGraphicMainTex == MixedBool.False);

            _baseMapRotationItem = new NBShaderSliderPropertyItem(
                rootItem,
                this,
                "_BaseMapUVRotation",
                "inspector.maintex.rotation.label",
                "主贴图旋转",
                0f,
                360f,
                isVisible: () => rootItem.Context.UseGraphicMainTex == MixedBool.False);

            _baseMapRotationSpeedItem = new NBShaderFloatPropertyItem(
                rootItem,
                this,
                "_BaseMapUVRotationSpeed",
                "inspector.maintex.rotationspeed.label",
                "主贴图旋转速度",
                isVisible: () => rootItem.Context.UseGraphicMainTex == MixedBool.False);

            InitTriggerByChild();
        }

        public override void DrawBlock()
        {
            if (NBRootItem.Context.UseGraphicMainTex == MixedBool.Mixed)
            {
                EditorGUILayout.HelpBox("Mesh 来源混合时，主贴图编辑入口会根据材质模式分别显示。", MessageType.Info);
            }

            if (NBRootItem.Context.UseGraphicMainTex == MixedBool.True)
            {
                _graphicMainTexHelpBox.OnGUI();
            }

            _baseMapItem.OnGUI();
            _uiColorItem.OnGUI();
            _uiMainTexTilingItem.OnGUI();
            _uiMainTexOffsetItem.OnGUI();
            _baseMapOffsetSpeedItem.OnGUI();
            _baseMapRotationItem.OnGUI();
            _baseMapRotationSpeedItem.OnGUI();
        }
    }
}
