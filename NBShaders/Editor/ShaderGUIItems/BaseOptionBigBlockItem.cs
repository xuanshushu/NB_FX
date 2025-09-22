using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace NBShaderEditor
{
    public class BaseOptionBigBlockItem:ShaderGUIBigBlockItem
    {
        public BaseOptionBigBlockItem(ShaderGUIRootItem rootItem, ShaderGUIItem parentItem) :
            base(rootItem, parentItem: parentItem)
        {
            GuiContent = new GUIContent("基本全局功能", "全局控制功能");
            FoldOutPropertyName = "_BaseOptionBigBlockItemFoldOut";
            _baseColorIntensityItem = new BaseColorIntensityItem(rootItem, this);
            _alphaAllItem = new AlphaAllItem(rootItem, this);
            _zTestItem = new ZTestItem(rootItem, this);
            base.InitTriggerByChild();
            
        }
        
        BaseColorIntensityItem _baseColorIntensityItem;
        AlphaAllItem _alphaAllItem;
        ZTestItem _zTestItem;

        public override void DrawBlock()
        {
            _baseColorIntensityItem.OnGUI();
            _alphaAllItem.OnGUI();
            _zTestItem.OnGUI();
        }
    }
    public class BaseColorIntensityItem:ShaderGUIFloatItem
    {
        public BaseColorIntensityItem(ShaderGUIRootItem rootItem, ShaderGUIItem parentItem) : base(rootItem, parentItem: parentItem)
        {
            PropertyName = "_BaseColorIntensityForTimeline";
            GuiContent = new GUIContent("整体颜色强度", "");
            base.InitTriggerByChild();
        }
    }
    
    public class AlphaAllItem:ShaderGUISliderItem
    {
        public AlphaAllItem(ShaderGUIRootItem rootItem, ShaderGUIItem parentItem) : base(rootItem, parentItem: parentItem)
        {
            PropertyName = "_AlphaAll";
            GuiContent = new GUIContent("整体透明度");
            RangePropertyName = "AlphaAllRangeVec";
            base.InitTriggerByChild();
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
