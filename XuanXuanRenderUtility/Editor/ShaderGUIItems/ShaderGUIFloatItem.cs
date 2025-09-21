using UnityEditor;
using UnityEditor.Graphs;

namespace NBShaderEditor
{
    public class ShaderGUIFloatItem:ShaderGUIItem
    {
        public ShaderGUIFloatItem(ShaderGUIRootItem rootItem, ShaderGUIItem parentItem) :
            base(rootItem, parentItem: parentItem)
        {
            base.InitTriggerByChild();
        }

        public override void DrawController()
        {
            PropertyInfo.Property.floatValue = EditorGUI.FloatField(ControlRect, PropertyInfo.Property.floatValue);
        }
    }
    
    public class ShaderGUISliderItem:ShaderGUIItem
    {
        public float Min = 0;
        public float Max = 1;
        public string RangePropertyName;
        ShaderPropertyInfo _rangePropertyInfo; 

        public override void InitTriggerByChild()
        {
            if (RangePropertyName != null)
            {
                _rangePropertyInfo = RootItem.PropertyInfoDic[RangePropertyName];
                Min = _rangePropertyInfo.Property.vectorValue.x;
                Max = _rangePropertyInfo.Property.vectorValue.y;
            }
            base.InitTriggerByChild();
        }

        public ShaderGUISliderItem(ShaderGUIRootItem rootItem, ShaderGUIItem parentItem) :
            base(rootItem, parentItem: parentItem) { }

        public override void DrawController()
        {
            PropertyInfo.Property.floatValue = EditorGUI.Slider(ControlRect, PropertyInfo.Property.floatValue,Min,Max);
        }
    }
}