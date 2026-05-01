using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace NBShaderEditor
{
    public class BaseOptionBigBlockItem : BigBlockItem
    {
        public BaseOptionBigBlockItem(NBShaderRootItem rootItem, ShaderGUIItem parentItem) :
            base(
                rootItem,
                parentItem,
                "_BaseOptionBigBlockItemFoldOut",
                () => NBShaderInspectorLocalization.MakeContent(
                    "inspector.block.base.label",
                    "基本全局功能",
                    "inspector.block.base.tip",
                    "全局控制功能"))
        {
            _baseColorIntensityItem = new ShaderGUIFloatItem(rootItem, this)
            {
                PropertyName = "_BaseColorIntensityForTimeline",
                GuiContent = new GUIContent("整体颜色强度", "")
            };
            _baseColorIntensityItem.InitTriggerByChild();

            _alphaAllItem = new ShaderGUISliderItem(rootItem, this)
            {
                PropertyName = "_AlphaAll",
                GuiContent = new GUIContent("整体透明度"),
                RangePropertyName = "AlphaAllRangeVec"
            };
            _alphaAllItem.InitTriggerByChild();

            _zTestItem = new ZTestItem(rootItem, this);
            base.InitTriggerByChild();
            
        }
        
        private readonly ShaderGUIFloatItem _baseColorIntensityItem;
        private readonly ShaderGUISliderItem _alphaAllItem;
        private readonly ZTestItem _zTestItem;

        public override void DrawBlock()
        {
            _baseColorIntensityItem.OnGUI();
            _alphaAllItem.OnGUI();
            _zTestItem.OnGUI();
        }
    }

    public class ZTestItem : ShaderGUIPopUpItem
    {
        public ZTestItem(ShaderGUIRootItem rootItem, ShaderGUIItem parentItem) : base(rootItem, parentItem: parentItem)
        {
            PropertyName = "_ZTest";
            GuiContent = new GUIContent("深度测试");
            PopUpNames = Enum.GetNames(typeof(CompareFunction));
            base.InitTriggerByChild();
        }

        public override void OnGUI()
        {
            MixedBool uiEffectEnabled = MeshModePopUp.MeshSourceModeDic[RootItem].UIEffectEnabled();
            if (uiEffectEnabled == MixedBool.True)
            {
                if (!Mathf.Approximately(PropertyInfo.Property.floatValue,(float)CompareFunction.LessEqual))
                {
                    PropertyInfo.Property.floatValue = (float)CompareFunction.LessEqual;
                }
                return; //UIEffeect不应比较深度
            }
            else if (uiEffectEnabled == MixedBool.Mixed)
            {
                return;
            }
            base.OnGUI();
        }
        
    }
        
}
