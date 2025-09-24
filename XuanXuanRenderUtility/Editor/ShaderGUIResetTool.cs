using System.Collections.Generic;
using System;
using UnityEngine;
using ShaderPropertyPack = NBShaderEditor.ShaderGUIHelper.ShaderPropertyPack;
using UnityEditor;
namespace NBShaderEditor
{
    public class ShaderGUIResetTool
    {
        private ShaderGUIHelper _helper;
        private Shader _shader;
        public bool IsInitResetData = false;
        
        private Stack<(string,string)> _scopeContextStack = new Stack<(string,string)>();

        public void CheckAllModifyOnValueChange()
        {
            foreach (var item in ResetItemDict.Values)
            {
                item.HasModified = item.CheckHasModifyOnValueChange();
            }
        }
        public void Init(ShaderGUIHelper helper)
        {
            _helper = helper;
            _shader = helper.shader;
            IsInitResetData = true;
            ResetItemDict.Clear();
            _scopeContextStack.Clear();
        }

        public void EndInit()
        {
            IsInitResetData = false;
        }

        public void Update()
        {
            if (_needUpdate)
            {
                _needUpdate = false;
                CheckAllModifyOnValueChange();
            }
        }

        private bool _needUpdate = false;
        public void NeedUpdate()
        {
            _needUpdate = true;
        }

        public ShaderGUIResetTool(ShaderGUIHelper helper)
        {
            Init(helper);
        }

        public Dictionary<(string, string), ResetItem> ResetItemDict = new Dictionary<(string, string), ResetItem>();

        public class ResetItem
        {
            public ResetItem Parent;
            public List<ResetItem> ChildResetItems = new List<ResetItem>();
            public Action ResetCallBack;
            public Action OnValueChangedCallBack;
            public Func<bool> CheckHasModifyOnValueChange;
            public Func<bool> CheckHasMixedValueOnValueChange;
            public (string, string) NameTuple;
            public bool HasModified = false;
            public bool HasMixedValue =false; 
            public bool ChildHasModified = false;
            public bool ChildHasMixedValue = false;
            

            public void Init((string, string) nameTuple,Action resetCallBack,Action onValueChangedCallBack,Func<bool> checkHasModifyOnValueChange,Func<bool> checkHasMixedValueOnValueChange)
            {
                NameTuple = nameTuple;
                ResetCallBack = resetCallBack;
                OnValueChangedCallBack = onValueChangedCallBack;
                CheckHasModifyOnValueChange = checkHasModifyOnValueChange;
                CheckHasMixedValueOnValueChange = checkHasMixedValueOnValueChange;
            }

            public void Execute(bool isParentCall = false)
            {
                ResetCallBack?.Invoke();
                OnValueChangedCallBack?.Invoke();
                HasModified = CheckHasModifyOnValueChange();
                HasMixedValue = CheckHasMixedValueOnValueChange();
                CheckOnValueChange(isParentCall);
                foreach (var item in ChildResetItems)
                {
                    item.Execute(true);
                }
            }
            

            public void CheckOnValueChange(bool isParentCall = false)
            {
                HasModified = CheckHasModifyOnValueChange();
                HasMixedValue = CheckHasMixedValueOnValueChange();
                foreach (var childItem in ChildResetItems)
                {
                    HasMixedValue |= childItem.HasMixedValue;
                    HasModified |= childItem.HasModified;
                }

                if (!isParentCall && Parent != null)
                {
                    Parent.CheckOnValueChange();
                }
            }
        }

        public void CheckOnValueChange((string,string) nameTuple)
        {
            if (ResetItemDict.ContainsKey(nameTuple))
            {
                 ResetItemDict[nameTuple].CheckOnValueChange();
                
            }
            else
            {
                Debug.LogError("不包含ResetItemDict:"+nameTuple);
            }
        }

        public void DrawResetModifyButton(Rect rect,(string, string) nameTuple, Action resetCallBack,
            Action onValueChangedCallBack,Func<bool> checkHasModifyOnValueChange,Func<bool> checkHasMixedValueOnValueChange,bool isSharedGlobalParent = false)
        {
            ConstructResetItem(nameTuple,resetCallBack,onValueChangedCallBack,checkHasModifyOnValueChange,checkHasMixedValueOnValueChange,isSharedGlobalParent);
            DrawResetModifyButtonFinal(rect,nameTuple);
        }
        public void DrawResetModifyButton(Rect rect,(string,string)nameTuple,ShaderPropertyPack pack, Action resetAction,Action onValueChangedCallBack,VectorValeType vectorValeType = VectorValeType.Undefine,bool isSharedGlobalParent = false)
        {
            
            // (string, string) nameTuple = (label, pack.property.name);
            ConstructResetItem(nameTuple,
                resetAction: ()=>{
                    SetPropertyToDefaultValue(pack,vectorValeType); 
                    resetAction?.Invoke();
                },onValueChangedCallBack:onValueChangedCallBack,
                checkHasModifyOnValueChange: () => IsPropertyModified(pack,vectorValeType),
                checkHasMixedValueOnValueChange: () =>  pack.property.hasMixedValue,
                isSharedGlobalParent: isSharedGlobalParent
            );
            if (ResetItemDict.ContainsKey(nameTuple))
            {
                DrawResetModifyButtonFinal(rect,nameTuple);
            }
        }

        public void DrawResetModifyButton(Rect rect, string label)
        {
            //大部分功能都是触发子类
            ConstructResetItem((label, ""), resetAction: () => { },onValueChangedCallBack: () => { }, () => false, () => false);
            DrawResetModifyButtonFinal(rect,(label,""));
        }
        
        //isSharedGlobalParent==>有些组件是公用的，比如极坐标一类。这些是不会设置父Item的。
        public void ConstructResetItem((string,string) nameTuple, Action resetAction,
            Action onValueChangedCallBack,Func<bool> checkHasModifyOnValueChange,Func<bool> checkHasMixedValueOnValueChange,bool isSharedGlobalParent = false)
        {
            if(!IsInitResetData) return;
            if (!ResetItemDict.ContainsKey(nameTuple))
            {
                ResetItem item = new ResetItem();
                item.Init(nameTuple,resetAction,onValueChangedCallBack,checkHasModifyOnValueChange,checkHasMixedValueOnValueChange);
                ResetItemDict.Add(nameTuple,item);
                
                if (_scopeContextStack.Count > 0 && !isSharedGlobalParent)
                {
                    var contextNameTuple = _scopeContextStack.Peek();
                    ResetItem parentItem = ResetItemDict[contextNameTuple];
                    parentItem.ChildResetItems.Add(item);
                    item.Parent = parentItem;
                }
                onValueChangedCallBack?.Invoke();//属性值初始化
                item.CheckOnValueChange();//Init
            }
            else
            {
                // Debug.LogError("ResetItem已经存在:"+nameTuple.ToString());
            }
            //就算是已经ContainsKey了，也Push和Pop一下。没有作用，但让写法更简单。
            _scopeContextStack.Push(nameTuple);
        }
        
        
        public void EndResetModifyButtonScope()
        {
            if(!IsInitResetData) return;
            if(_scopeContextStack.Count == 0) return;
            _scopeContextStack.Pop();
        }

        public float ResetButtonSize => EditorGUIUtility.singleLineHeight;
        
        private GUIContent resetIconContent = new GUIContent();
        //仅仅只是Drawer
        private void DrawResetModifyButtonFinal(Rect position, (string, string) nameTuple)
        {
            ResetItem item;
            // GUILayout.FlexibleSpace();
            if (ResetItemDict.ContainsKey(nameTuple))
            {
                item = ResetItemDict[nameTuple];
            }
            else
            {
                return;
            }
            float btnSize = ResetButtonSize;
            string iconText;
            bool isDisabled = true;
            GUIStyle iconStyle;
            if (item.HasModified || item.HasMixedValue)
            {
                isDisabled = false;
                iconText = "R";
                iconStyle = GUI.skin.button;
            }
            else
            {
                isDisabled = true;
                iconText = "";
                iconStyle = GUI.skin.label;
            }

            resetIconContent.text = iconText;
            EditorGUI.BeginDisabledGroup(isDisabled);
            // if (GUILayout.Button(iconTexture, GUILayout.Width(btnSize), GUILayout.Height(btnSize)))
            if (position.width <= 0)
            {
                position = GUILayoutUtility.GetRect(resetIconContent, GUI.skin.button, GUILayout.Width(btnSize),
                    GUILayout.Height(btnSize));
            }
            if(GUI.Button(position,resetIconContent,iconStyle))
            {
                item.Execute();
            }
            EditorGUI.EndDisabledGroup();
        }
     
        public void SetPropertyToDefaultValue(ShaderPropertyPack pack,VectorValeType vectorValeType = VectorValeType.Undefine)
        {
            MaterialProperty property = pack.property;
            MaterialProperty.PropType propertyType = property.type;
            if (pack.property.type == MaterialProperty.PropType.Texture && vectorValeType != VectorValeType.Undefine)
            {
                propertyType = MaterialProperty.PropType.Vector;//Tilling or Offset
            }
            switch (propertyType)
            {
                case MaterialProperty.PropType.Color:
                    Vector4 colorValue = _shader.GetPropertyDefaultVectorValue(pack.index);
                    property.colorValue = new Color(colorValue.x, colorValue.y, colorValue.z, colorValue.x);
                    break;

                case MaterialProperty.PropType.Vector:
                    Vector4 defaultVecValue;
                    Vector4 vecValue;
                    if (vectorValeType == VectorValeType.Tilling || vectorValeType == VectorValeType.Offset)
                    {
                        defaultVecValue = new Vector4(1f, 1f, 0f, 0f);
                        vecValue = property.textureScaleAndOffset;
                        
                    }
                    else
                    {
                        defaultVecValue = _shader.GetPropertyDefaultVectorValue(pack.index);
                        vecValue = property.vectorValue;
                    }
                    switch (vectorValeType)
                    {
                        case VectorValeType.Undefine: Debug.LogError("VectorValeType is undefined"); break;
                        case VectorValeType.X: vecValue.x = defaultVecValue.x;property.vectorValue = vecValue;break;
                        case VectorValeType.Y: vecValue.y = defaultVecValue.y;property.vectorValue = vecValue;break;
                        case VectorValeType.Z: vecValue.z = defaultVecValue.z;property.vectorValue = vecValue;break;
                        case VectorValeType.W: vecValue.w = defaultVecValue.w;property.vectorValue = vecValue;break;
                        case VectorValeType.XY:vecValue.x = defaultVecValue.x; vecValue.y = defaultVecValue.y;
                            property.vectorValue = vecValue;break;
                        case VectorValeType.Tilling:vecValue.x = defaultVecValue.x; vecValue.y = defaultVecValue.y;
                            property.textureScaleAndOffset = vecValue;break;
                        case VectorValeType.ZW:vecValue.z = defaultVecValue.z; vecValue.w = defaultVecValue.w;
                            property.vectorValue = vecValue;break;
                        case VectorValeType.Offset:vecValue.z = defaultVecValue.z; vecValue.w = defaultVecValue.w;
                            property.textureScaleAndOffset = vecValue;break;
                        case VectorValeType.XYZ:vecValue.x = defaultVecValue.x; vecValue.y = defaultVecValue.y;
                            vecValue.z = defaultVecValue.z; property.vectorValue = vecValue;break;
                        case VectorValeType.XYZW: property.vectorValue = defaultVecValue;break;
                    }
                    break;

                case MaterialProperty.PropType.Float or MaterialProperty.PropType.Range:
                    float value = _shader.GetPropertyDefaultFloatValue(pack.index);
                    property.floatValue = value;
                    break;

                case MaterialProperty.PropType.Texture:
                    if (property.textureValue == null)
                    {
                        break;
                    }
                    else
                    {
                        property.textureValue = null;
                        break;
                    }
                // return property.textureValue.name == shader.GetPropertyTextureDefaultName(pack.index) ? false : true;

                default:
                    // 如果不属于上述类型，输出提示信息
                    Debug.Log($"{property.displayName} has no default value or unsupported type");
                    break;
            }
        }

        public bool IsPropertyModified(ShaderPropertyPack pack,VectorValeType vectorValeType = VectorValeType.Undefine)
        {
            MaterialProperty property = pack.property;
            MaterialProperty.PropType propertyType = property.type;
            if (pack.property.type == MaterialProperty.PropType.Texture && vectorValeType != VectorValeType.Undefine)
            {
                propertyType = MaterialProperty.PropType.Vector;//Tilling or Offset
            }
            switch (propertyType)
            {
                case MaterialProperty.PropType.Color:
                    Vector4 colorValue = _shader.GetPropertyDefaultVectorValue(pack.index);
                    Color color = new Color(colorValue.x, colorValue.y, colorValue.z, colorValue.w);
                    return property.colorValue == color ? false : true;

                case MaterialProperty.PropType.Vector:

                    Vector4 defaultVecValue;
                    Vector4 vecValue;
                    if (vectorValeType == VectorValeType.Tilling || vectorValeType == VectorValeType.Offset)
                    {
                        defaultVecValue = new Vector4(1f, 1f, 0f, 0f);
                        vecValue = property.textureScaleAndOffset;
                        
                    }
                    else
                    {
                        defaultVecValue = _shader.GetPropertyDefaultVectorValue(pack.index);
                        vecValue = property.vectorValue;
                    }

                
                    Vector2 defaultVecXYValue = new Vector2(defaultVecValue.x, defaultVecValue.y);
                    Vector2 defaultVecZWValue = new Vector2(defaultVecValue.z, defaultVecValue.w);
                    Vector2 vecXYValue = new Vector2(vecValue.x, vecValue.y);
                    Vector2 vecZWValue = new Vector2(vecValue.z, vecValue.w);
                    Vector2 defaultVecXYZValue = new Vector3(defaultVecValue.x, defaultVecValue.y,defaultVecValue.z);
                    Vector2 vecXYZValue = new Vector3(vecValue.x, vecValue.y,vecValue.z);
                    

                    bool isVecModified = false;
                    switch (vectorValeType)
                    {
                        case VectorValeType.Undefine: Debug.LogError("VectorValeType is undefined"); break;
                        case VectorValeType.X: isVecModified = Mathf.Approximately(vecValue.x,defaultVecValue.x) ? false : true;break;
                        case VectorValeType.Y: isVecModified = Mathf.Approximately(vecValue.y,defaultVecValue.y) ? false : true;break;
                        case VectorValeType.Z: isVecModified = Mathf.Approximately(vecValue.z,defaultVecValue.z) ? false : true;break;
                        case VectorValeType.W: isVecModified = Mathf.Approximately(vecValue.w,defaultVecValue.w) ? false : true;break;
                        case VectorValeType.XY:case VectorValeType.Tilling:
                            isVecModified = vecXYValue == defaultVecXYValue ? false : true;break;
                        case VectorValeType.ZW:case VectorValeType.Offset:
                            isVecModified = vecZWValue == defaultVecZWValue ? false : true;break;
                        case VectorValeType.XYZ:isVecModified = vecXYZValue == defaultVecXYZValue ? false : true ; break;
                        case VectorValeType.XYZW:isVecModified=  vecValue == defaultVecValue? false : true ; break;
                    }
                    return isVecModified;

                case MaterialProperty.PropType.Float or MaterialProperty.PropType.Range:
                    return Mathf.Approximately(property.floatValue, _shader.GetPropertyDefaultFloatValue(pack.index)) ? false : true;

                case MaterialProperty.PropType.Texture:
                    if (property.textureValue == null)
                    {
                        return false;
                    }
                    else
                    {
                        return property.textureValue.name == "textureExternal" ? false : true;
                    }
                // return property.textureValue.name == shader.GetPropertyTextureDefaultName(pack.index) ? false : true;

                default:
                    // 如果不属于上述类型，输出提示信息
                    return false;
                    // Debug.Log($"{property.displayName} has no default value or unsupported type");
                    // break;
            }
        }
    }

}