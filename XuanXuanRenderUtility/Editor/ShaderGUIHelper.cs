using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.AnimatedValues;
// using UnityEditor.Rendering.Universal.ShaderGUI;
using UnityEngine;
using Random = UnityEngine.Random;
using System.Reflection;
using UnityEngine.UIElements;
using UnityEditor;
namespace NBShaderEditor
{
    /*
        多选材质面板的原则记录：
        0、多选后 同时只会有1个matEditor。每个属性只会有一个MaterialProperty。但会有多个Mat以及Mat属性值。会有多个ShaderFlag以及ShaderFlag值。
        1、当多选后，属性处于Mixed状态时。对propety.Value进行设置是不合法的。只有非Mixed状态才会完全设置。包括这样对property.Value值进行判断也是非法的。如果在不确定Mixed的情况下，应该通过遍历Mats[i].Get或Set进行操作。Flag亦同理。
        2、drawBlock应该都传Property，让DrawBlock内能知道Mixed状态。然后在Block内需要判断状态是否明确才进行相关的操作。
        3、drawBlock一般放需要绘制的行为。对数值修改的行为统一放到OnValueChangeCheckBlock。
        4、所有在GUI上存储，并需要后续判断的状态，需要可以标记mixed状态。如果是枚举，需要有一个指定为-1的UnKnownOrMixed枚举。如果是bool，应该改为int值，并规定-1为UnKnownOrMixed状态。
        5、对于属性值的更改设定，都应该在OnValueChange的情况下进行。
        6、对于Toggle作用的方法，均应该有EditorOnly的Property进行标记Toggle储存。Keywords或者Flag应该是设定结果，而非Toggle标记。
        7、DrawVectorComponent这种多个GUI公用一个property属性的情况，需要有手动的提供各个Component是否是Mixed的方案。
    */
    public class ShaderGUIHelper
    {
        public ShaderGUIHelper()
        {
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }
        private void OnUndoRedoPerformed()//定义一个Undo回调
        {
            if(!shader) return;//会有一些空的Helper对象
            DrawGradientUndoPerformed();
            ResetTool?.NeedUpdate();
        }
        
        public class ShaderPropertyPack
        {
            public MaterialProperty property;
            public string name;
            public int index;
        }

        public List<Material> mats;
        public MaterialEditor matEditor;
        public Shader shader;
        // public List<ShaderPropertyPack> ShaderPropertyPacks = new List<ShaderPropertyPack>();
        public Dictionary<string, ShaderPropertyPack> ShaderPropertyPacksDic =
            new Dictionary<string, ShaderPropertyPack>();
        public ShaderFlagsBase[] shaderFlags = null;

        public bool isClearUnUsedTexture = false;

        Color defaultBackgroundColor;
        Color animatedBackgroundColor=>Color.red;

        public void Init(MaterialEditor materialEditor, MaterialProperty[] properties,
            ShaderFlagsBase[] shaderFlags_in = null, List<Material> mats_in = null)
        {
            defaultBackgroundColor = GUI.backgroundColor;
            shaderFlags = shaderFlags_in;
            // ShaderPropertyPacks.Clear();
            matEditor = materialEditor;
            mats = mats_in;
            if (mats[0].shader != shader)
            {
                shader = mats[0].shader;
                ShaderPropertyPacksDic.Clear();
                for (int i = 0; i < ShaderUtil.GetPropertyCount(shader); i++)
                {
                    ShaderPropertyPack pack = new ShaderPropertyPack();
                    pack.name = ShaderUtil.GetPropertyName(shader, i);
                    for (int index = 0; index < properties.Length; ++index)
                    {
                        if (properties[index] != null && properties[index].name == pack.name)
                        {
                            pack.property = properties[index];
                            pack.index = index;
                            break;
                        }
                        else
                        {
                            if (index == properties.Length - 1)
                            {
                                Debug.LogError(pack.name + "找不到Properties");
                            }
                        }
                    }

                    // ShaderPropertyPacks.Add(pack);
                    ShaderPropertyPacksDic.Add(pack.name,pack);
                }
            }
            else
            {
                for (int i = 0; i < ShaderUtil.GetPropertyCount(shader); i++)
                {
                    string propertyName = ShaderUtil.GetPropertyName(shader, i);
                    ShaderPropertyPacksDic[propertyName].property = properties[i];
                }

            }
           

            if (ResetTool == null)
            {
                ResetTool = new ShaderGUIResetTool(this);
            }
            else
            {
                ResetTool.EndInit();
                ResetTool.Update();
            }
            
            isClearUnUsedTexture = false;

        }
  
        private ShaderGUIToolBar _toolBar;
    
        public ShaderGUIResetTool ResetTool;
        public List<Renderer> renderersUsingThisMaterial = new List<Renderer>();
        
        Dictionary<(string,string),string> propertyPathDic = new Dictionary<(string,string), string>();

        public void InitRenderers(List<Renderer> rendererList)
        {
            renderersUsingThisMaterial = rendererList;
            propertyPathDic.Clear();
        }

        public bool IsPropertyAnimated(string propertyName,params string[] componentNames)
        {
            if (AnimationMode.InAnimationMode())
            {
                
                // string propertyPath = "material." + propertyName;
                
                foreach (var r in renderersUsingThisMaterial)
                {
                    if (componentNames.Length > 0)
                    {
                        foreach (var component in componentNames)
                        {

                            string propertyPath;
                            if (propertyPathDic.ContainsKey((propertyName, component)))
                            {
                                propertyPath = propertyPathDic[(propertyName, component)];
                            }
                            else
                            {
                                propertyPath = "material." + propertyName +"." +component;
                                propertyPathDic.Add((propertyName, component), propertyPath);
                            }
                            if (AnimationMode.IsPropertyAnimated(r, propertyPath))
                            {
                                return true;
                            }
                        }
                    }
                    else
                    {
                        string propertyPath;
                        if (propertyPathDic.ContainsKey((propertyName,"")))
                        {
                            propertyPath = propertyPathDic[(propertyName, "")];
                        }
                        else
                        {
                            propertyPath = "material." + propertyName;
                            propertyPathDic.Add((propertyName,""), propertyPath);
                        }
                        if (AnimationMode.IsPropertyAnimated(r, propertyPath))
                        {
                            // Debug.Log(propertyName);
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public void DrawToolBar()
        {
            if (_toolBar == null) _toolBar = new ShaderGUIToolBar(this);
            _toolBar.DrawToolbar();
        }
        
        public void DrawToggleFoldOut(int foldOutFlagBit,int foldOutFlagIndex, int animBoolIndex,string label, string propertyName,
            int flagBitsName = 0,
            int flagIndex = 0, string shaderKeyword = null, string shaderPassName = null, bool isIndentBlock = true, FontStyle fontStyle = FontStyle.Normal,
            Action<MaterialProperty> drawBlock = null,Action<MaterialProperty> drawEndChangeCheck = null,bool isSharedGlobalParent = false)
        {
            bool foldOutState = shaderFlags[0].CheckFlagBits(foldOutFlagBit, index: foldOutFlagIndex);
            AnimBool animBool = GetAnimBool(foldOutFlagBit, animBoolIndex, foldOutFlagIndex); //foldOut里的第一组。
            animBool.target = foldOutState;
            DrawToggleFoldOut(ref animBool, label, propertyName, flagBitsName, flagIndex, shaderKeyword,
                shaderPassName, isIndentBlock, fontStyle, drawBlock, drawEndChangeCheck,isSharedGlobalParent:isSharedGlobalParent);
            foldOutState = animBool.target;
            if (foldOutState)
            {
                shaderFlags[0].SetFlagBits(foldOutFlagBit, index: foldOutFlagIndex);
            }
            else
            {
                shaderFlags[0].ClearFlagBits(foldOutFlagBit, index: foldOutFlagIndex);
            }
        }

        public void ColorProperty(string label, string propertyName,bool showAlpha = true)
        {
            EditorGUILayout.BeginHorizontal();
            ShaderPropertyPack shaderPropertyPack = ShaderPropertyPacksDic[propertyName];
            MaterialProperty prop = shaderPropertyPack.property;
            Color color = shaderPropertyPack.property.colorValue;
            Rect position = EditorGUILayout.GetControlRect();
            // MaterialEditor.BeginProperty(position, prop);
           
            EditorGUI.BeginChangeCheck();
            bool hdr = (prop.flags & MaterialProperty.PropFlags.HDR) != 0;
            if (IsPropertyAnimated(propertyName,"r","g","b","a"))
            {
                GUI.backgroundColor = animatedBackgroundColor;
            }
            color = EditorGUI.ColorField(position, new GUIContent(label), prop.colorValue, true, showAlpha, hdr);
            GUI.backgroundColor = defaultBackgroundColor;
            Action onEndChaneChange = () =>
            {
                prop.colorValue = color;
                ResetTool.CheckOnValueChange((label,propertyName));
            };
            if (EditorGUI.EndChangeCheck())
            {
                onEndChaneChange();
            }
            ResetTool.DrawResetModifyButton(new Rect(),(label,shaderPropertyPack.name),shaderPropertyPack,resetAction: () =>
            {
                color = prop.colorValue;
            },onValueChangedCallBack: onEndChaneChange);
            // MaterialEditor.EndProperty();
            // matEditor.ColorProperty(GetProperty(propertyName), label);
            
            EditorGUILayout.EndHorizontal();
            ResetTool.EndResetModifyButtonScope();
        }

        public void DrawBigBlockFoldOut(int foldOutFlagBit,int foldOutFlagIndex,int animBoolIndex ,string label, Action drawBlock,bool isResetButtonBias = true)
        {
            EditorGUILayout.Space();
            DrawFoldOut(foldOutFlagBit,foldOutFlagIndex,animBoolIndex, label,FontStyle.Bold, drawBlock,isResetButtonBias);
            GuiLine();
        }

        private AnimBool[] animBoolArr = new AnimBool[96];//先假定有3组。和存好的bit一一对应。
        
        public AnimBool GetAnimBool(int flagBit, int AnimBoolIndex,int flagIndex)
        {
            int bitPos = 0;
            for (int i = 0; i < 32; i++)
            {
                if ((flagBit & (1 << i)) > 0)
                {
                    bitPos = i;
                    break;
                }
            }
            int arrIndex = AnimBoolIndex * 32 + bitPos;
            // Debug.Log(arrIndex.ToString() +"---"+ animBoolArr[arrIndex]);
            if (animBoolArr[arrIndex] == null)
            {
                animBoolArr[arrIndex] = new AnimBool(shaderFlags[0].CheckFlagBits(flagBit,index:flagIndex));
            }

            animBoolArr[arrIndex].speed = 10f;
            
            return animBoolArr[arrIndex];
        }

        public void DrawFoldOut(int foldOutFlagBit,int foldOutFlagIndex,int animBoolIndex, String label,FontStyle fontStyle = FontStyle.Normal, Action drawBlock = null,bool isResetButtonBias = true)
        {
            bool foldOutState = shaderFlags[0].CheckFlagBits(foldOutFlagBit, index: foldOutFlagIndex);
            AnimBool animBool = GetAnimBool(foldOutFlagBit, animBoolIndex, foldOutFlagIndex);
            animBool.target = foldOutState;
            
            EditorGUILayout.BeginHorizontal();
            var rect = EditorGUILayout.GetControlRect();
            var foldoutRect = new Rect(rect.x, rect.y, rect.width, rect.height);
            foldoutRect.width -= 2 * ResetTool.ResetButtonSize;
            var labelRect = new Rect(rect.x + 2f, rect.y, rect.width - 2f, rect.height);
            var resetRect = rect;
            resetRect.x = rect.x + rect.width - ResetTool.ResetButtonSize ;
            if (isResetButtonBias) resetRect.x -= 3f;
            resetRect.width = ResetTool.ResetButtonSize;
            
            animBool.target = EditorGUI.Foldout(foldoutRect, animBool.target, string.Empty, true);
            
            FontStyle origFontStyle = EditorStyles.label.fontStyle;
            EditorStyles.label.fontStyle = fontStyle;
            EditorGUI.LabelField(labelRect, label);
            EditorStyles.label.fontStyle = origFontStyle;
            
            ResetTool.DrawResetModifyButton(resetRect,label);
            EditorGUILayout.EndHorizontal();
            
            float faded = animBool.faded;
            // if (faded == 0) faded = 0.00001f;
            if (faded == 0 && ResetTool.IsInitResetData) faded = 0.00001f;
            if (EditorGUILayout.BeginFadeGroup(faded))
            {
                EditorGUI.indentLevel++;
                drawBlock?.Invoke();
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFadeGroup();
            foldOutState = animBool.target;
            if (foldOutState)
            {
                shaderFlags[0].SetFlagBits(foldOutFlagBit, index: foldOutFlagIndex);
            }
            else
            {
                shaderFlags[0].ClearFlagBits(foldOutFlagBit, index: foldOutFlagIndex);
            }
            ResetTool.EndResetModifyButtonScope();
        }

        public void DrawBigBlockWithToggle(String label, string propertyName, int flagBitsName = 0, int flagIndex = 0,
            string shaderKeyword = null, string shaderPassName = null, string shaderPassName2 = null,
            Action<MaterialProperty> drawBlock = null)
        {

            DrawToggle(label, propertyName, flagBitsName, flagIndex, shaderKeyword, shaderPassName, shaderPassName2,
                isIndentBlock: true, FontStyle.Bold, drawBlock: drawBlock);
            GuiLine();

        }

        public void DrawToggleFoldOut(ref AnimBool foldOutAnimBool, String label, string propertyName,
            int flagBitsName = 0,
            int flagIndex = 0, string shaderKeyword = null, string shaderPassName = null, bool isIndentBlock = true,
            FontStyle fontStyle = FontStyle.Normal,
            Action<MaterialProperty> drawBlock = null, Action<MaterialProperty> drawEndChangeCheck = null,bool isSharedGlobalParent = false)
        {
            ShaderPropertyPack propertyPack = ShaderPropertyPacksDic[propertyName];
            MaterialProperty toggleProp = GetProperty(propertyName);
            
            if (fontStyle == FontStyle.Bold)
            {
                EditorGUILayout.Space();
            }

            EditorGUILayout.BeginHorizontal();
                var rect = EditorGUILayout.GetControlRect();
                var toggleRect = rect;
                toggleRect.x += EditorGUIUtility.labelWidth;
                toggleRect.width -= EditorGUIUtility.labelWidth;

                var foldoutRect = new Rect(rect.x, rect.y, rect.width, rect.height);
                foldoutRect.width = toggleRect.x - foldoutRect.x;
                var labelRect = new Rect(rect.x , rect.y, rect.width , rect.height);

                // bool isToggle = false;
                // 必须先画Toggle，不然按钮会被FoldOut和Label覆盖。
                DrawToggle(string.Empty, propertyName, flagBitsName, flagIndex, shaderKeyword, shaderPassName, isIndentBlock: false, fontStyle: FontStyle.Normal, rect: toggleRect, drawEndChangeCheck: drawEndChangeCheck,isSharedGlobalParent:isSharedGlobalParent);
                
                foldOutAnimBool.target = EditorGUI.Foldout(foldoutRect, foldOutAnimBool.target, string.Empty, true);
                var origFontStyle = EditorStyles.label.fontStyle;
                EditorStyles.label.fontStyle = fontStyle;

                EditorGUI.LabelField(labelRect, label);
                EditorStyles.label.fontStyle = origFontStyle;
            EditorGUILayout.EndHorizontal();
            if (isIndentBlock) EditorGUI.indentLevel++;
            float faded = foldOutAnimBool.faded;
            if (faded == 0 && ResetTool.IsInitResetData) faded = 0.0001f; //用于欺骗FadeGroup，不要让他真的关闭了。这样会藏不住相关的GUI。我们的目的是，GUI藏住，但是逻辑还是在跑。drawBlock要执行。
            if (EditorGUILayout.BeginFadeGroup(faded))
            {
                bool isDisabledGroup = toggleProp.hasMixedValue || toggleProp.floatValue < 0.5f;
                EditorGUI.BeginDisabledGroup(isDisabledGroup);
                    
                drawBlock?.Invoke(toggleProp);
                EditorGUI.EndDisabledGroup();
            }
            EditorGUILayout.EndFadeGroup();
            if (isIndentBlock) EditorGUI.indentLevel--;
            
            ResetTool.EndResetModifyButtonScope();//开始是在DrawToggle里开始的
            
        }

        public void DrawToggle(String label, string propertyName, int flagBitsName = 0, int flagIndex = 0,
            string shaderKeyword = null, string shaderPassName = null, string shaderPassName2 = null,
            bool isIndentBlock = true, FontStyle fontStyle = FontStyle.Normal, Rect rect = new Rect(),
            Action<MaterialProperty> drawBlock = null, Action<MaterialProperty> drawEndChangeCheck = null,bool isSharedGlobalParent = false)
        {
            if (GetProperty(propertyName) == null)
                return;
            
            if (fontStyle == FontStyle.Bold)
            {
                EditorGUILayout.Space();
            }
            
            MaterialProperty toggleProperty = GetProperty(propertyName);
            ShaderPropertyPack propertyPack = ShaderPropertyPacksDic[propertyName];
            EditorGUI.showMixedValue = toggleProperty.hasMixedValue;
            
            EditorGUI.BeginChangeCheck();
            bool isToggle = toggleProperty.floatValue > 0.5f ? true : false;
            var origFontStyle = EditorStyles.label.fontStyle;
            EditorStyles.label.fontStyle = fontStyle;
            Rect resetButtonRect = rect;
            if (label.Length <= 0) //给FoldOut功能使用。
            {
                rect.x += 2f;
                rect.width -= 24f;//避免覆盖ResetButton
                isToggle = EditorGUI.Toggle(rect, isToggle, EditorStyles.toggle);
                resetButtonRect.x = resetButtonRect.x + resetButtonRect.width - ResetTool.ResetButtonSize;
                resetButtonRect.width = ResetTool.ResetButtonSize;
            }
            else
            {
                Rect newRect = EditorGUILayout.GetControlRect();
                Rect newToggleRect = newRect;
                Rect newLabelRect = newRect;
                // newLabelRect.x += indent;
                newLabelRect.width = EditorGUIUtility.labelWidth;
                newToggleRect.x += EditorGUIUtility.labelWidth +2f;
                newToggleRect.width -= EditorGUIUtility.labelWidth +2f;
                resetButtonRect = newToggleRect;
                resetButtonRect.x = resetButtonRect.x + resetButtonRect.width - ResetTool.ResetButtonSize;
                resetButtonRect.width = ResetTool.ResetButtonSize;
                EditorGUI.LabelField(newLabelRect, label);
                isToggle = EditorGUI.Toggle(newToggleRect, isToggle, EditorStyles.toggle);
            }
            
            EditorStyles.label.fontStyle = origFontStyle;
            Action onEndChangeCheck = () =>
            {
                toggleProperty.floatValue = isToggle ? 1.0f : 0.0f;
                ResetTool.CheckOnValueChange((label,propertyName));
                if (!toggleProperty.hasMixedValue)
                {
                    for (int i = 0; i < mats.Count; i++)
                    {
                        if (isToggle)
                        {
                            mats[i].SetFloat(propertyName,1);
                            
                            if (flagBitsName != 0 && shaderFlags[i] != null)
                            {
                                shaderFlags[i].SetFlagBits(flagBitsName, index: flagIndex);
                            }
            
                            if (shaderKeyword != null)
                            {
                                mats[i].EnableKeyword(shaderKeyword);
                            }
            
                            if (shaderPassName != null)
                            {
                                mats[i].SetShaderPassEnabled(shaderPassName, true);
                            }
            
                            if (shaderPassName2 != null)
                            {
                                mats[i].SetShaderPassEnabled(shaderPassName2, true);
                            }
                        }
                        else
                        {
                            
                            mats[i].SetFloat(propertyName,0);
                            
            
                            if (flagBitsName != 0 && shaderFlags[i] != null)
                            {
                                shaderFlags[i].ClearFlagBits(flagBitsName, index: flagIndex);
                            }
            
                            if (shaderKeyword != null)
                            {
                                mats[i].DisableKeyword(shaderKeyword);
                            }
            
                            if (shaderPassName != null)
                            {
                                mats[i].SetShaderPassEnabled(shaderPassName, false);
                            }
            
                            if (shaderPassName2 != null)
                            {
                                mats[i].SetShaderPassEnabled(shaderPassName2, false);
                            }
                        }
                    }
                }
                drawEndChangeCheck?.Invoke(toggleProperty);
            };
            if (EditorGUI.EndChangeCheck())
            {
                onEndChangeCheck();
            }
            
            
            ResetTool.DrawResetModifyButton(resetButtonRect,(label,propertyPack.name),propertyPack,resetAction: () =>
            {
                isToggle = propertyPack.property.floatValue > 0.5f ? true : false;
            },onValueChangedCallBack:onEndChangeCheck,isSharedGlobalParent:isSharedGlobalParent);
            
            
            if (isIndentBlock) EditorGUI.indentLevel++;
            drawBlock?.Invoke(toggleProperty);
            if (isIndentBlock) EditorGUI.indentLevel--;
            
            EditorGUI.showMixedValue = false;
            if (rect.width <= 0)
            {
                ResetTool.EndResetModifyButtonScope();//如果是DrawFoldOut，需要在DrawFoldOut里去结束。
            }

            if (ResetTool.IsInitResetData)
            {
                ResetTool.CheckOnValueChange((label,propertyPack.name));//为什么是在这里验证而不是再ResetTool里。
            }
        }

        void RangeVecHasMixedValue(string rangePropertyName, out bool minValueHasMixed, out bool maxValueHasMixed)
        { 
            minValueHasMixed = false;
            maxValueHasMixed = false;
            if (mats.Count > 1)
            {
                MaterialProperty rangeProperty = GetProperty(rangePropertyName);
                float minValue = 0;
                float maxValue = 0;
                for (int i = 0; i < mats.Count; i++)
                {
                    Vector4 rangeVec = mats[i].GetVector(rangePropertyName);
                    if (i == 0)
                    {
                        minValue = rangeVec.x;
                        maxValue = rangeVec.y;
                    }
                    else
                    {
                        if (!Mathf.Approximately(minValue, rangeVec.x))
                        {
                            minValueHasMixed = true;
                        }

                        if (!Mathf.Approximately(maxValue, rangeVec.y))
                        {
                            maxValueHasMixed = true;
                        }
                    }
                }
            }
        }

        bool RangePropIsDefaultValue(string rangePropertyName)
        {
            MaterialProperty rangeProperty = GetProperty(rangePropertyName);
            return mats[0].GetVector(rangePropertyName) == shader.GetPropertyDefaultVectorValue(ShaderPropertyPacksDic[rangePropertyName].index) && !rangeProperty.hasMixedValue;
        }

        void DrawSlider(string label, ref float value,ref float min,ref float max,bool isValueMixed,string rangePropertyName = null,bool propIsAnimated = false)
        {
            if (rangePropertyName == null)
            {
                EditorGUI.showMixedValue = isValueMixed;
                if (propIsAnimated) GUI.backgroundColor = animatedBackgroundColor;
                value = EditorGUILayout.Slider(label,value,min,max);
                GUI.backgroundColor = defaultBackgroundColor;
                EditorGUI.showMixedValue = false;
            }
            else
            {
                Rect rect = EditorGUILayout.GetControlRect();
                Rect labelRect = rect;
                labelRect.width = EditorGUIUtility.labelWidth;
                Rect valueRect = rect;
                valueRect.x += EditorGUIUtility.labelWidth;
                valueRect.width -= EditorGUIUtility.labelWidth;
                // EditorGUI.DrawRect(valueRect,Color.red);
                float rangeWidth = 50f;
                Rect minRect = valueRect;
                minRect.width = rangeWidth;
                minRect.x -= indent;
                minRect.width += indent;
                // EditorGUI.DrawRect(minRect,Color.green);
                Rect maxRect = valueRect;
                maxRect.x += valueRect.width;
                maxRect.x -= rangeWidth;
                maxRect.width = rangeWidth;
                maxRect.x -= indent;
                maxRect.width += indent;
                valueRect.x += rangeWidth;
                valueRect.width -= 2*rangeWidth;
                valueRect.x -= indent;
                valueRect.width += indent;
                valueRect.x += 4f;
                valueRect.width -= 8f;
                EditorGUI.LabelField(labelRect, label);
                
                RangeVecHasMixedValue(rangePropertyName,out bool minValueHasMixed,out bool maxValueHasMixed);

                EditorGUI.showMixedValue = minValueHasMixed;
                min = EditorGUI.FloatField(minRect, GUIContent.none, min);
                EditorGUI.showMixedValue = maxValueHasMixed;
                max = EditorGUI.FloatField(maxRect, GUIContent.none, max);
                EditorGUI.showMixedValue = isValueMixed;
                if (propIsAnimated)
                {
                    GUI.backgroundColor = animatedBackgroundColor;
                }
                value = EditorGUI.Slider(valueRect, value, min, max);
                GUI.backgroundColor = defaultBackgroundColor;
                EditorGUI.showMixedValue = false;
            }
        }

        public void DrawSlider(string label, string propertyName,float min = 0,float max = 1,string rangePropertyName = null, Action<float> drawBlock = null)
        {
            bool hasMixedValue = GetProperty(propertyName).hasMixedValue;
            float f = GetProperty(propertyName).floatValue;
            bool customedRange = rangePropertyName != null;
            Vector4 rangeVec = Vector4.zero;
            if (customedRange)
            {
                rangeVec = GetProperty(rangePropertyName).vectorValue;
            }
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            if (IsPropertyAnimated(propertyName))
            {
                GUI.backgroundColor = animatedBackgroundColor;
            }

            bool isPropAnimated = IsPropertyAnimated(propertyName);
            if (customedRange)
            {
                DrawSlider(label, ref f, ref rangeVec.x,ref rangeVec.y,hasMixedValue,rangePropertyName,isPropAnimated);
            }
            else
            {
                DrawSlider(label, ref f, ref min,ref max,hasMixedValue,propIsAnimated:isPropAnimated);
            }

            GUI.backgroundColor = defaultBackgroundColor;
            Action endChangCallBack= () =>
            {
                GetProperty(propertyName).floatValue = f;
                if (customedRange)
                {
                    GetProperty(rangePropertyName).vectorValue = rangeVec;
                }
                ResetTool.CheckOnValueChange((label,propertyName));
            };
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                endChangCallBack.Invoke();
            }
            ResetTool.DrawResetModifyButton(new Rect(),(label,propertyName),resetCallBack: () =>
            {
                f = shader.GetPropertyDefaultFloatValue(ShaderPropertyPacksDic[propertyName].index);
                if (customedRange)
                {
                    rangeVec = shader.GetPropertyDefaultVectorValue(ShaderPropertyPacksDic[rangePropertyName].index);
                }
            },onValueChangedCallBack: () =>
            {
                endChangCallBack();
            }
            ,checkHasModifyOnValueChange:
            () =>
            {
                bool isModify = !Mathf.Approximately(mats[0].GetFloat(propertyName),shader.GetPropertyDefaultFloatValue(ShaderPropertyPacksDic[propertyName].index));
                if (customedRange)
                {
                    isModify |= !RangePropIsDefaultValue(rangePropertyName);
                }
                return isModify;
            },checkHasMixedValueOnValueChange:
            () =>
            {
                bool hasMixedValue = false;
                hasMixedValue |= GetProperty(propertyName).hasMixedValue;
                if (customedRange)
                {
                    RangeVecHasMixedValue(rangePropertyName,out bool minValueHasMixed,out bool maxValueHasMixed);
                    hasMixedValue |= minValueHasMixed;
                    hasMixedValue |= maxValueHasMixed;
                }
                return hasMixedValue;
            });
            EditorGUILayout.EndHorizontal();
            drawBlock?.Invoke(f);
            ResetTool.EndResetModifyButtonScope();
        }


        public void DrawFloat(string label, string propertyName, bool isReciprocal = false,
            Action<MaterialProperty> drawBlock = null)
        {
            EditorGUI.showMixedValue = GetProperty(propertyName).hasMixedValue;
            MaterialProperty floatProperty = GetProperty(propertyName);
            float f = floatProperty.floatValue;
            if (isReciprocal) f = 1 / f;
            EditorGUI.BeginChangeCheck();
            Action endChangeCallback = () =>
            {
                floatProperty.floatValue = f;
                ResetTool.CheckOnValueChange((label,propertyName));
            };
            
            EditorGUILayout.BeginHorizontal();
            if (IsPropertyAnimated(propertyName))
            {
                GUI.backgroundColor = animatedBackgroundColor;
            }
            f = EditorGUILayout.FloatField(label, f);
            GUI.backgroundColor = defaultBackgroundColor;
  
            if (isReciprocal) f = 1 / f;
            if (EditorGUI.EndChangeCheck())
            {
                endChangeCallback.Invoke();
            }
            
            ResetTool.DrawResetModifyButton(new Rect(),(label,propertyName),ShaderPropertyPacksDic[propertyName],resetAction: () =>
            {
                f = floatProperty.floatValue;
            },onValueChangedCallBack:endChangeCallback);
            EditorGUILayout.EndHorizontal();

            drawBlock?.Invoke(floatProperty);
            EditorGUI.showMixedValue = false;
            ResetTool.EndResetModifyButtonScope();
        }

        Vector2 GetVec2InVec4(Vector4 vec4,bool isFirstLine)
        {
            if (isFirstLine)
            {
                return new Vector2(vec4.x, vec4.y);
            }
            else
            {
                return new Vector2(vec4.z, vec4.w);
            }
        }
        
        Vector4 SetVec2InVec4(Vector4 vec4,bool isFirstLine,Vector2 vec2Value)
        {
            if (isFirstLine)
            {
                vec4.x = vec2Value.x;
                vec4.y = vec2Value.y;
                return vec4;
            }
            else
            {
                 vec4.z = vec2Value.x;
                 vec4.w = vec2Value.y;
                 return vec4;
            }
        }

        Vector2 GetVecInTwoLineDefaultValue(string propertyName, bool isFirstLine)
        {
            ShaderPropertyPack propertyPack = ShaderPropertyPacksDic[propertyName];
            Vector4 defaultVector = shader.GetPropertyDefaultVectorValue(propertyPack.index);
            if (isFirstLine)
            {
                return new Vector2(defaultVector.x, defaultVector.y);
            }
            else
            {
                return new Vector2(defaultVector.z, defaultVector.w);
            }
        }

        bool Vector4In2LineHasMixedValue(string propertyName, bool isFirstLine)
        {
            MaterialProperty matProp = GetProperty(propertyName);
            if (matProp.hasMixedValue)
            {
                Vector2 val = Vector2.zero;
                for (int i = 0; i < mats.Count; i++)
                {
                    Vector2 matValue =  GetVec2InVec4(mats[i].GetVector(propertyName), isFirstLine);
                    if (i == 0)
                    {
                        val = matValue;
                    }
                    else
                    {
                        if (!val.Equals(matValue) )
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public void DrawVector4In2Line(string propertyName, string label , bool isFirstLine,
            Action drawBlock = null)
        {
            EditorGUI.showMixedValue = Vector4In2LineHasMixedValue(propertyName, isFirstLine);
            MaterialProperty property = GetProperty(propertyName);
            (string,string) nameTuple = (label,propertyName);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginHorizontal();

            Vector2 vec2 = GetVec2InVec4(property.vectorValue, isFirstLine);
            // vec2 = EditorGUILayout.Vector2Field(label, vec2);
            Rect rect = EditorGUILayout.GetControlRect(true);
            Rect labelRect = rect;
            labelRect.width = EditorGUIUtility.labelWidth - indent;
            EditorGUI.LabelField(labelRect, label);
            Rect vec2Rect = rect;
            vec2Rect.x += labelRect.width;
            vec2Rect.width -= labelRect.width;
            if (isFirstLine)
            {
                if (IsPropertyAnimated(propertyName, "x", "y"))
                {
                    GUI.backgroundColor = animatedBackgroundColor;
                }
            }
            else
            {
                if (IsPropertyAnimated(propertyName, "z", "w"))
                {
                    GUI.backgroundColor = animatedBackgroundColor;
                }
            }
            vec2 = EditorGUI.Vector2Field(vec2Rect,"", vec2);
            GUI.backgroundColor = defaultBackgroundColor;
            
            Action vec2OnEndChangeCheck = () =>
            {
                int shaderID = Shader.PropertyToID(propertyName);
                for (int i = 0; i < mats.Count; i++)
                {
                    Vector4 vec4 = mats[i].GetVector(shaderID);
                    vec4 = SetVec2InVec4(vec4, isFirstLine, vec2);
                    if (mats.Count == 1)
                    {
                        GetProperty(propertyName).vectorValue = vec4; //为了K动画，多选不能K动画。   
                    }
                    else
                    {
                        mats[i].SetVector(shaderID, vec4);
                    }
                }
                ResetTool.CheckOnValueChange(nameTuple);
            };


            if (EditorGUI.EndChangeCheck())
            {
                vec2OnEndChangeCheck();
            }
            EditorGUI.showMixedValue = false;
            
            ResetTool.DrawResetModifyButton(new Rect(),nameTuple,
                resetCallBack:()=>
                {
                    vec2 = GetVecInTwoLineDefaultValue(propertyName, isFirstLine);
                    vec2OnEndChangeCheck();
                },
                onValueChangedCallBack:vec2OnEndChangeCheck,
                checkHasModifyOnValueChange: () => GetVec2InVec4(mats[0].GetVector(propertyName), isFirstLine) != GetVecInTwoLineDefaultValue(propertyName, isFirstLine),
                checkHasMixedValueOnValueChange:()=>Vector4In2LineHasMixedValue(propertyName, isFirstLine));

            drawBlock?.Invoke();
            ResetTool.EndResetModifyButtonScope();
            EditorGUILayout.EndHorizontal();

        }

        float GetCompInVec4(Vector4 vec, string comp)
        {
            float f = 0;
            switch (comp)
            {
                case "x":
                    f = vec.x;
                    break;
                case "y":
                    f = vec.y;
                    break;
                case "z":
                    f = vec.z;
                    break;
                case "w":
                    f = vec.w;
                    break;
            }

            return f;
        }

        float GetCompDefaultValueInVec4(string propertyName, string comp)
        {
            ShaderPropertyPack propertyPack = ShaderPropertyPacksDic[propertyName];
            Vector4 defaultValue = shader.GetPropertyDefaultVectorValue(propertyPack.index);
            return GetCompInVec4(defaultValue, comp);
        }

        Vector4 SetCompInVec4(Vector4 vec, string comp, float value)
        {
            switch (comp)
            {
                case "x":
                    vec.x = value;
                    break;
                case "y":
                    vec.y = value;
                    break;
                case "z":
                    vec.z = value;
                    break;
                case "w":
                    vec.w = value;
                    break;
            }

            return vec;
        }

        bool Vector4ComponentHasMixedValue(string propertyName, string channel)
        {
            MaterialProperty property = GetProperty(propertyName);
            if (property.hasMixedValue)
            {
                float val = 0;
                for (int i = 0; i < mats.Count; i++)
                {
                    if (i == 0)
                    {
                        val = GetCompInVec4(mats[i].GetVector(propertyName),channel) ;
                    }
                    else
                    {
                        if (!val.Equals(GetCompInVec4(mats[i].GetVector(propertyName),channel)) )
                        {
                            return true;
                        }
                    }
                }
            }
            
            return false;
            
        }
        public void DrawVector4Component(string label, string propertyName, string channel, bool isSlider,float minValue = 0,float maxValue = 1,
            string rangeVecPropName = null, float powerSlider = 1, float multiplier = 1,
            bool isReciprocal = false, Action<float,bool> drawBlock = null, Action<float,bool> drawEndChangeCheckBlock = null)
        {
            bool hasMixedValue = Vector4ComponentHasMixedValue(propertyName, channel);
            (string, string) nameTuple = (label, propertyName);
            Vector4 vec = GetProperty(propertyName).vectorValue;
            float f = GetCompInVec4(vec, channel);
            Vector4 rangeVec = Vector4.zero;
            bool isCustomedRange = rangeVecPropName != null;
            if (isCustomedRange)
            {
                rangeVec = GetProperty(rangeVecPropName).vectorValue;
            }
            f *= multiplier;
            if (isReciprocal) f = 1 / f;
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            bool propIsAnimated = IsPropertyAnimated(propertyName, channel);
            if (isSlider)
            {
                if (powerSlider > 1)
                {
                    if (propIsAnimated) GUI.backgroundColor = animatedBackgroundColor;
                     f = PowerSlider(EditorGUILayout.GetControlRect(new GUILayoutOption[] { GUILayout.Height(18) }),
                         new GUIContent(label), f, minValue, maxValue, powerSlider);
                     GUI.backgroundColor = defaultBackgroundColor;
                }
                else
                {
                    if (isCustomedRange)
                    {
                        DrawSlider(label,ref f,ref rangeVec.x,ref rangeVec.y,hasMixedValue,rangeVecPropName,propIsAnimated);
                    }
                    else
                    {
                        DrawSlider(label,ref f,ref minValue,ref maxValue,hasMixedValue,propIsAnimated:propIsAnimated);
                    }
                }
            }
            else
            {
                EditorGUI.showMixedValue = hasMixedValue;
                if(propIsAnimated) GUI.backgroundColor = animatedBackgroundColor;
                f = EditorGUILayout.FloatField(label, f);
                GUI.backgroundColor = defaultBackgroundColor;
                EditorGUI.showMixedValue = false;
            }

            if (isReciprocal) f = 1 / f;
            f /= multiplier;

            Action floatVecEndChangeCheck = () =>
            {
                int id= Shader.PropertyToID(propertyName);
                for (int i = 0; i < mats.Count; i++)
                {
                    Vector4 val = mats[i].GetVector(id);
                    val = SetCompInVec4(val, channel, f);
                    if (mats.Count == 1)
                    {
                        GetProperty(propertyName).vectorValue = val;//为了K动画，多选不能K动画。
                    }
                    else
                    {
                        mats[i].SetVector(id, val);
                    }
                    if (isCustomedRange)
                    {
                        mats[i].SetVector(rangeVecPropName, rangeVec);
                    }
                }

                drawEndChangeCheckBlock?.Invoke(f, hasMixedValue); 
                ResetTool.CheckOnValueChange(nameTuple);
            };

            if (EditorGUI.EndChangeCheck())
            {
                floatVecEndChangeCheck();
            }
            
            ResetTool.DrawResetModifyButton(new Rect(),nameTuple,
                resetCallBack:()=>
                {
                    f = GetCompDefaultValueInVec4(propertyName, channel);
                    if (isCustomedRange)
                    {
                        rangeVec = shader.GetPropertyDefaultVectorValue(ShaderPropertyPacksDic[rangeVecPropName].index);
                    }
                    floatVecEndChangeCheck();
                },onValueChangedCallBack:floatVecEndChangeCheck,
                checkHasModifyOnValueChange: () =>
                {
                    bool isEqual = Mathf.Approximately(GetCompInVec4(mats[0].GetVector(propertyName), channel), GetCompDefaultValueInVec4(propertyName, channel));
                    if (isCustomedRange)
                    {
                        isEqual &= RangePropIsDefaultValue(rangeVecPropName);
                    }
                    return !isEqual;
                },
                checkHasMixedValueOnValueChange: () =>
                {
                    bool hasMixedValue = Vector4ComponentHasMixedValue(propertyName, channel);
                    if (isCustomedRange)
                    {
                        RangeVecHasMixedValue(rangeVecPropName, out bool minValueHasMixed, out bool maxValueHasMixed);
                        hasMixedValue |= minValueHasMixed;
                        hasMixedValue |= maxValueHasMixed;
                    }

                    return hasMixedValue;
                });
            EditorGUILayout.EndHorizontal();

            drawBlock?.Invoke(f,hasMixedValue);
            ResetTool.EndResetModifyButtonScope();
            EditorGUI.showMixedValue = false;
        }

        public void DrawVector4XYZComponet(string label, string propertyName, Action<Vector3> drawBlock = null)
        {
            EditorGUI.showMixedValue = GetProperty(propertyName).hasMixedValue;
            (string, string) nameTuple = (label, propertyName);
            Vector4 originVec = GetProperty(propertyName).vectorValue;
            Vector3 vec = originVec;
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginHorizontal();
            vec = EditorGUILayout.Vector3Field(label, vec);
            EditorGUI.showMixedValue = false;
            Action drawEndChangeCheck = () =>
            {
                GetProperty(propertyName).vectorValue = new Vector4(vec.x, vec.y, vec.z, originVec.w);
                ResetTool.CheckOnValueChange(nameTuple);
                
            };
            if (EditorGUI.EndChangeCheck())
            {
                drawEndChangeCheck();
            }
            
            ResetTool.DrawResetModifyButton(new Rect(),nameTuple,ShaderPropertyPacksDic[propertyName],resetAction: () =>
            {
                vec = GetProperty(propertyName).vectorValue;
            },onValueChangedCallBack:drawEndChangeCheck,vectorValeType:VectorValeType.XYZ);
            EditorGUILayout.EndHorizontal();
            drawBlock?.Invoke(vec);
            ResetTool.EndResetModifyButtonScope();
        }

        public enum SamplerWarpMode
        {
            Repeat,
            Clamp,
            RepeatX_ClampY,
            ClampX_RepeatY
        }

        public Rect GetRectAfterLabelWidth(Rect rect, bool ignoreIndent = false)
        {
            Rect rectAfterLabelWidth = MaterialEditor.GetRectAfterLabelWidth(rect); //右边缘是准的。
            Rect leftAlignedFieldRect = MaterialEditor.GetLeftAlignedFieldRect(rect); //左边缘是准的，实际有2f空隙。
            float x = leftAlignedFieldRect.x + 2f;
            float width = rectAfterLabelWidth.x + rectAfterLabelWidth.width - x;

            var newRec = new Rect(x, rectAfterLabelWidth.y, width, rectAfterLabelWidth.height);

            if (ignoreIndent)
            {
                float indent = (float)EditorGUI.indentLevel * 15f;
                newRec.x -= indent;
                newRec.width += indent;
            }
            return newRec;
        }
        public void DrawTextureFoldOut(int foldOutFlagBit,int foldOutFlagIndex,int animBoolIndex,string label, string texturePropertyName,
            string colorPropertyName = null, bool drawScaleOffset = true, bool drawWrapMode = false,
            int flagBitsName = 0, int flagIndex = 2, Action<MaterialProperty> drawBlock = null)
        {
           
            bool foldOutState = shaderFlags[0].CheckFlagBits(foldOutFlagBit, index: foldOutFlagIndex);
            AnimBool animBool = GetAnimBool(foldOutFlagBit, animBoolIndex, foldOutFlagIndex);
            animBool.target = foldOutState;
            DrawTextureFoldOut(ref animBool, label, texturePropertyName, colorPropertyName, drawScaleOffset,
                drawWrapMode, flagBitsName, flagIndex, drawBlock);
            foldOutState = animBool.target;
            if (foldOutState)
            {
                shaderFlags[0].SetFlagBits(foldOutFlagBit, index: foldOutFlagIndex);
            }
            else
            {
                shaderFlags[0].ClearFlagBits(foldOutFlagBit, index: foldOutFlagIndex);
            }

        }


        public void DrawTextureFoldOut(ref AnimBool foldOutAnimBool, string label, string texturePropertyName,
            string colorPropertyName = null, bool drawScaleOffset = true, bool drawWrapMode = false,
            int wrapModeFlagBitsName = 0, int flagIndex = 2, Action<MaterialProperty> drawBlock = null)
        {
            // EditorGUILayout.BeginHorizontal();
            // var rect = EditorGUILayout.GetControlRect(false,68f);//MaterialEditor.GetTextureFieldHeight() => 64f;
            // var foldoutRect = new Rect(rect.x, rect.y, rect.width , rect.height);
            // var textureThumbnialRect = new Rect(rect.x , rect.y, rect.width, rect.height);
            // Texture texture = matEditor.TextureProperty(textureThumbnialRect,GetProperty(texturePropertyName), label, drawScaleOffset);
            Texture texture = TextureProperty(GetProperty(texturePropertyName), label, drawScaleOffset);
            // EditorGUILayout.EndHorizontal();

            if (colorPropertyName != null)
            {
                // Rect colorPropRect = GetRectAfterLabelWidth(rect, true);
                // colorPropRect.x -= EditorGUI.indentLevel
                // EditorGUI.indentLevel++;
                // Rect colorPropRect = EditorGUILayout.GetControlRect(false);
                // Color color = matEditor.ColorProperty(colorPropRect, GetProperty(colorPropertyName), "");
                ColorProperty("",colorPropertyName,true);
                // EditorGUI.indentLevel--;
            }
            var foldoutRect = EditorGUILayout.GetControlRect(false);//MaterialEditor.GetTextureFieldHeight() => 64f;
            Rect labelRect = foldoutRect;
            labelRect.width = EditorGUIUtility.labelWidth;
            EditorGUI.LabelField(labelRect, label+"相关功能", EditorStyles.boldLabel);
            foldOutAnimBool.target = EditorGUI.Foldout(foldoutRect, foldOutAnimBool.target, "", true);
            float faded = foldOutAnimBool.faded;
            if (faded == 0 && ResetTool.IsInitResetData) faded = 0.00001f;
            
            if(EditorGUILayout.BeginFadeGroup(faded))
            {
                EditorGUI.BeginDisabledGroup(texture == null);
                DrawAfterTexture(true, label, texturePropertyName, drawWrapMode, wrapModeFlagBitsName, flagIndex, drawBlock);
                EditorGUI.EndDisabledGroup();
            }
            EditorGUILayout.EndFadeGroup();
        }

        public void DrawTexture(string label, string texturePropertyName, string colorPropertyName = null,
            bool drawScaleOffset = true, bool drawWrapMode = false, int wrapModeFlagBitsName = 0, int flagIndex = 2,
            Action<MaterialProperty> drawBlock = null)
        {
            bool hasTexture = mats[0].GetTexture(texturePropertyName) != null;
            Texture texture = TextureProperty(GetProperty(texturePropertyName), label, drawScaleOffset);
            if (colorPropertyName != null)
            {
                ColorProperty("",colorPropertyName,true);
            }
                
            DrawAfterTexture(hasTexture, label, texturePropertyName, drawWrapMode, wrapModeFlagBitsName,
                flagIndex, drawBlock);
        }
        
        Texture TextureProperty(MaterialProperty textureProperty, string label, bool drawScaleOffset)
        {
            ShaderPropertyPack texturePropertyPack = ShaderPropertyPacksDic[textureProperty.name];
            if (!GUI.enabled && isClearUnUsedTexture&&textureProperty.textureValue)
            {
                Debug.Log("清除掉贴图："+textureProperty.name,textureProperty.textureValue);
                texturePropertyPack.property.textureValue = null;
            }
            float indentWidth = 15f;
            float currentIndentWidth = EditorGUI.indentLevel * indentWidth;
            float singleLineHeight = EditorGUIUtility.singleLineHeight;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.GetControlRect(GUILayout.Width(EditorGUI.indentLevel * indentWidth));
            float textureFieldHeight = 3*singleLineHeight;
            var textureRect = EditorGUILayout.GetControlRect(GUILayout.Height(textureFieldHeight),GUILayout.Width(textureFieldHeight));
            
            EditorGUILayout.BeginVertical();
            var textureLabelRect = EditorGUILayout.GetControlRect();
            textureLabelRect.x -= currentIndentWidth;
            textureLabelRect.width += currentIndentWidth;
            EditorGUI.LabelField(textureLabelRect,label,EditorStyles.boldLabel);
       
            var textureResetButtonRect = textureLabelRect;
            textureResetButtonRect.x += textureResetButtonRect.width;
            textureResetButtonRect.x -= ResetTool.ResetButtonSize;
            textureResetButtonRect.width = ResetTool.ResetButtonSize;

            float tillingOffsetLabelWidth = 30f;
            Rect tillingRect = EditorGUILayout.GetControlRect();
            Rect tillingVec2Rect = tillingRect;
            tillingVec2Rect.x += tillingOffsetLabelWidth;
            tillingVec2Rect.width -= tillingOffsetLabelWidth;
            tillingVec2Rect.width -= ResetTool.ResetButtonSize;
            tillingVec2Rect.width -= 2f;
            Rect tillingResetButtonRect = tillingRect;
            tillingResetButtonRect.x = tillingResetButtonRect.x + tillingResetButtonRect.width - ResetTool.ResetButtonSize;
            tillingResetButtonRect.width = ResetTool.ResetButtonSize;
            Rect offsetRect = EditorGUILayout.GetControlRect();
            Rect offsetVec2Rect = offsetRect;
            offsetVec2Rect.x += tillingOffsetLabelWidth;
            offsetVec2Rect.width -= tillingOffsetLabelWidth;
            offsetVec2Rect.width -= ResetTool.ResetButtonSize;
            offsetVec2Rect.width -= 2f;
            Rect offsetResetButtonRect = offsetRect;
            offsetResetButtonRect.x = offsetResetButtonRect.x + offsetResetButtonRect.width - ResetTool.ResetButtonSize;
            offsetResetButtonRect.width = ResetTool.ResetButtonSize;
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            
            Texture texture = textureProperty.textureValue;
            Action drawTextureEndChangeCheck = () =>
            {
                textureProperty.textureValue = texture;
                ResetTool.CheckOnValueChange((label,texturePropertyPack.property.name));
            };
            EditorGUI.BeginChangeCheck();
            texture = (Texture)EditorGUI.ObjectField(textureRect,texture,typeof(Texture2D));
            if (EditorGUI.EndChangeCheck())
            {
                drawTextureEndChangeCheck();
            }
            ResetTool.DrawResetModifyButton(textureResetButtonRect,(label,texturePropertyPack.name),texturePropertyPack,resetAction: () =>
            {
                texture = null;
            },onValueChangedCallBack:drawTextureEndChangeCheck);
            ResetTool.EndResetModifyButtonScope();

            if (drawScaleOffset)
            {
                DrawScaleOffset(texturePropertyPack, tillingRect, tillingVec2Rect, tillingResetButtonRect, offsetRect,
                    offsetVec2Rect, offsetResetButtonRect);
            }
            return texture;
        }

        static float indent => (float) EditorGUI.indentLevel * 15f;
        public void TextureScaleOffsetProperty(string texturePropertyName)
        {
            ShaderPropertyPack texturePropertyPack = ShaderPropertyPacksDic[texturePropertyName];
            // EditorGUILayout.BeginHorizontal();
            Rect tillingRect = EditorGUILayout.GetControlRect();
            // EditorGUILayout.EndHorizontal();
            Rect tillingLabelRect  = new Rect(tillingRect.x + indent, tillingRect.y, EditorGUIUtility.labelWidth - indent, tillingRect.height);
            tillingLabelRect.width = EditorGUIUtility.labelWidth;
            Rect tillingVec2Rect = tillingRect;
            tillingVec2Rect.x += tillingLabelRect.width;
            tillingVec2Rect.x -= indent;
            tillingVec2Rect.width -= 3f;
            tillingVec2Rect.width -= tillingLabelRect.width;
            tillingVec2Rect.width += indent;
            tillingVec2Rect.width -= ResetTool.ResetButtonSize;
            Rect tillingResetButtonRect = tillingRect;
            tillingResetButtonRect.x = tillingResetButtonRect.x + tillingResetButtonRect.width - ResetTool.ResetButtonSize;
            tillingResetButtonRect.width = ResetTool.ResetButtonSize;
            
          
            Rect offsetRect = EditorGUILayout.GetControlRect();
            Rect offsetLabelRect  = new Rect(offsetRect.x + indent, offsetRect.y, EditorGUIUtility.labelWidth - indent, offsetRect.height);
            offsetLabelRect.width = EditorGUIUtility.labelWidth;
            Rect offsetVec2Rect = offsetRect;
            offsetVec2Rect.x += offsetLabelRect.width;
            offsetVec2Rect.x -= indent;
            offsetVec2Rect.width -= 3f;
            offsetVec2Rect.width -= offsetLabelRect.width;
            offsetVec2Rect.width += indent;
            offsetVec2Rect.width -= ResetTool.ResetButtonSize;
            Rect offsetResetButtonRect = offsetRect;
            offsetResetButtonRect.x = offsetResetButtonRect.x + offsetResetButtonRect.width - ResetTool.ResetButtonSize;
            offsetResetButtonRect.width = ResetTool.ResetButtonSize;
            
            DrawScaleOffset(texturePropertyPack,tillingLabelRect,tillingVec2Rect,tillingResetButtonRect,offsetLabelRect,offsetVec2Rect,offsetResetButtonRect);

        }

        public void DrawScaleOffset(ShaderPropertyPack texturePropertyPack, Rect tillingRect, Rect tillingVec2Rect,
            Rect tillingResetButtonRect, Rect offsetRect, Rect offsetVec2Rect, Rect offsetResetButtonRect)
        {
            MaterialProperty textureProperty = texturePropertyPack.property;
            Vector4 tillingOffset = textureProperty.textureScaleAndOffset;
            string tillingLabel = "Tilling";
            var tillingTuple = (tillingLabel, textureProperty.name + "_ST");
            GUI.Label(tillingRect,tillingLabel);
            Vector2 tilling = new Vector2(tillingOffset.x, tillingOffset.y);
            Action drawTillingEndChangeCheck = () =>
            {
                tillingOffset.x = tilling.x;
                tillingOffset.y = tilling.y;
                textureProperty.textureScaleAndOffset = tillingOffset;
                ResetTool.CheckOnValueChange(tillingTuple);

            };
            EditorGUI.BeginChangeCheck();
            if (IsPropertyAnimated(tillingTuple.Item2, "x","y") )
            {
                GUI.backgroundColor = animatedBackgroundColor;
            }
            tilling = EditorGUI.Vector2Field(tillingVec2Rect, "", tilling);
            GUI.backgroundColor = defaultBackgroundColor;
            if (EditorGUI.EndChangeCheck())
            {
                drawTillingEndChangeCheck();
            }
            
            ResetTool.DrawResetModifyButton(tillingResetButtonRect,tillingTuple,texturePropertyPack,resetAction: () =>
            {
                tilling = Vector2.one;
            },onValueChangedCallBack:drawTillingEndChangeCheck,VectorValeType.Tilling);
            ResetTool.EndResetModifyButtonScope();
            
            string offsetLabel = "Offset";
            var offsetTuple = (offsetLabel, textureProperty.name + "_ST");
            GUI.Label(offsetRect,offsetLabel);
            Vector2 offset = new Vector2(tillingOffset.z, tillingOffset.w);
            Action drawOffsetEndChangeCheck = () =>
            {
                tillingOffset.z = offset.x;
                tillingOffset.w = offset.y;
                textureProperty.textureScaleAndOffset = tillingOffset;
                ResetTool.CheckOnValueChange(offsetTuple);

            };
            EditorGUI.BeginChangeCheck();
            if (IsPropertyAnimated(offsetTuple.Item2, "z","w"))
            {
                GUI.backgroundColor = animatedBackgroundColor;
            }
            offset = EditorGUI.Vector2Field(offsetVec2Rect, "", offset);
            GUI.backgroundColor = defaultBackgroundColor;
            if (EditorGUI.EndChangeCheck())
            {
                drawOffsetEndChangeCheck();
            }
            
            ResetTool.DrawResetModifyButton(offsetResetButtonRect,offsetTuple,texturePropertyPack,resetAction: () =>
            {
                offset = Vector2.zero;
            },onValueChangedCallBack:drawOffsetEndChangeCheck,VectorValeType.Offset);
            ResetTool.EndResetModifyButtonScope();
        }
        bool WrapModeFlagHasMixedValue(int wrapModeFlagBitsName, int flagIndex)
        {
            int tmpWrapMode = 0;
            for (int i = 0; i < shaderFlags.Length; i++)
            {
                if (i == 0)
                {
                    tmpWrapMode = GetWrapModeFlagValue(wrapModeFlagBitsName,flagIndex,shaderFlags[i]);
                }
                else
                {
                    if (!tmpWrapMode.Equals(GetWrapModeFlagValue(wrapModeFlagBitsName, flagIndex, shaderFlags[i])))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        int GetWrapModeFlagValue(int wrapModeFlagBitsName, int flagIndex,ShaderFlagsBase shaderFlag)
        {
            int tmpWrapMode = shaderFlag.CheckFlagBits(wrapModeFlagBitsName, index: flagIndex) ? 1 : 0;
            tmpWrapMode = shaderFlag.CheckFlagBits(wrapModeFlagBitsName << 16, index: flagIndex)
                ? tmpWrapMode + 2
                : tmpWrapMode;
            return tmpWrapMode;
        }

        void SetWrapModeFlagValue(int wrapModeFlagBitsName, int flagIndex,int wrapModeValue)
        {
            for (int i = 0; i < shaderFlags.Length; i++)
            {
                switch (wrapModeValue)
                {
                    case 0:
                        shaderFlags[i].ClearFlagBits(wrapModeFlagBitsName, index: flagIndex);
                        shaderFlags[i].ClearFlagBits(wrapModeFlagBitsName << 16, index: flagIndex);
                        break;
                    case 1:
                        shaderFlags[i].SetFlagBits(wrapModeFlagBitsName, index: flagIndex);
                        shaderFlags[i].ClearFlagBits(wrapModeFlagBitsName << 16, index: flagIndex);
                        break;
                    case 2:
                        shaderFlags[i].ClearFlagBits(wrapModeFlagBitsName, index: flagIndex);
                        shaderFlags[i].SetFlagBits(wrapModeFlagBitsName << 16, index: flagIndex);
                        break;
                    case 3:
                        shaderFlags[i].SetFlagBits(wrapModeFlagBitsName, index: flagIndex);
                        shaderFlags[i].SetFlagBits(wrapModeFlagBitsName << 16, index: flagIndex);
                        break;
                }
            }
        }

        public void DrawWrapMode(string texturelabel,int wrapModeFlagBitsName = 0, int flagIndex = 2)
        {
            bool hasMixedValue = WrapModeFlagHasMixedValue(wrapModeFlagBitsName, flagIndex);
            EditorGUI.showMixedValue = hasMixedValue;
            string wrapLabel = texturelabel + "循环模式";
            (string, string) wrapNameTuple = (wrapLabel, "");
                
            int tmpWrapMode = GetWrapModeFlagValue(wrapModeFlagBitsName, flagIndex,shaderFlags[0]);
            Action onWrapModeEndChangeCheck = () =>
            {
                SetWrapModeFlagValue(wrapModeFlagBitsName, flagIndex, tmpWrapMode);
                hasMixedValue = WrapModeFlagHasMixedValue(wrapModeFlagBitsName, flagIndex);
                ResetTool.CheckOnValueChange(wrapNameTuple);
            };
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginHorizontal();
            tmpWrapMode = EditorGUILayout.Popup(new GUIContent(wrapLabel), tmpWrapMode,
                Enum.GetNames(typeof(SamplerWarpMode)));
            if (EditorGUI.EndChangeCheck())
            {
                onWrapModeEndChangeCheck();
            }
            ResetTool.DrawResetModifyButton(new Rect(),wrapNameTuple,resetCallBack: () =>
            {
                tmpWrapMode = 0;
            },onValueChangedCallBack:onWrapModeEndChangeCheck,() =>
            {
                bool hasModified = GetWrapModeFlagValue(wrapModeFlagBitsName, flagIndex,shaderFlags[0]) != 0 ? true : false;
                return hasModified;
            }, () => WrapModeFlagHasMixedValue(wrapModeFlagBitsName, flagIndex));
            EditorGUILayout.EndHorizontal();
            ResetTool.EndResetModifyButtonScope();
        }
        public void DrawAfterTexture(bool hasTexture, string label, string texturePropertyName, bool drawWrapMode = false, int wrapModeFlagBitsName = 0, int flagIndex = 2,
            Action<MaterialProperty> drawBlock = null)
        {
            EditorGUI.indentLevel++;
            EditorGUI.BeginDisabledGroup(!hasTexture);
            if (drawWrapMode)
            {
                DrawWrapMode(label, wrapModeFlagBitsName,flagIndex);
            }

            drawBlock?.Invoke(GetProperty(texturePropertyName));
            EditorGUI.indentLevel--;
            EditorGUI.EndDisabledGroup();
        }


        public void DrawPopUp(string label, string propertyName, string[] options, string[] toolTips = null,
            Action<MaterialProperty> drawBlock = null,Action<MaterialProperty> drawOnValueChangedBlock = null,bool isSharedGlobalParent = false)
        {
            MaterialProperty property = GetProperty(propertyName);
            if (property == null) return;
            EditorGUI.showMixedValue = property.hasMixedValue;

            float mode = property.floatValue;
            EditorGUI.BeginChangeCheck();
            GUIContent[] optionGUIContents = new GUIContent[options.Length];
            for (int i = 0; i < optionGUIContents.Length; i++)
            {
                if (toolTips != null && toolTips.Length == options.Length)
                {
                    optionGUIContents[i] = new GUIContent(options[i], toolTips[i]);
                }
                else
                {
                    optionGUIContents[i] = new GUIContent(options[i]);
                }
            }
            
            Action drawOnValueChanged = () =>
            {
                property.floatValue = mode;
                drawOnValueChangedBlock?.Invoke(property);
                ResetTool.CheckOnValueChange((label,propertyName));
            };
           

            EditorGUILayout.BeginHorizontal();
            mode = EditorGUILayout.Popup(new GUIContent(label), (int)mode, optionGUIContents);
            if (EditorGUI.EndChangeCheck())
            {
                drawOnValueChanged.Invoke();
            }
            ResetTool.DrawResetModifyButton(new Rect(),(label,propertyName),ShaderPropertyPacksDic[propertyName],resetAction: () =>
            {
                mode = property.floatValue;
            },onValueChangedCallBack:drawOnValueChanged);
            EditorGUILayout.EndHorizontal();

            drawBlock?.Invoke(property);
            ResetTool.EndResetModifyButtonScope();
            EditorGUI.showMixedValue = false;
            
            // if (ResetTool.IsInitResetData)
            // {
            //     ResetTool.CheckOnValueChange((label,propertyName));//为什么是在这里验证而不是再ResetTool里。
            // }
        }

        public MaterialProperty GetProperty(string propertyName)
        {
            // foreach (ShaderPropertyPack pack in ShaderPropertyPacks)
            // {
            //     if (pack.name == propertyName)
            //     {
            //         return pack.property;
            //     }
            // }
            if (ShaderPropertyPacksDic.ContainsKey(propertyName))
            {
                return ShaderPropertyPacksDic[propertyName].property;
            }

            // Debug.LogError("材质球" + mat.name + "找不到属性" + propertyName, mat);
            return null;
        }

        bool RenderQueueHasMixedValue()
        {
            int queue = 0;
            for (int i = 0; i < mats.Count; i++)
            {
                if (i == 0)
                {
                    queue = mats[i].renderQueue;
                }
                else
                {
                    if (queue != mats[i].renderQueue)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public void DrawRenderQueue(MaterialProperty queueBiasProp)
        {
            Rect rect = EditorGUILayout.GetControlRect();
            Rect labelRect = new Rect(rect.x, rect.y, EditorGUIUtility.labelWidth, rect.height);
            int queueLabelWidth = 55;
            Rect queueLabelRect = new Rect(labelRect.x , labelRect.y, queueLabelWidth, rect.height);
            Rect queueNumberRect = new Rect(queueLabelRect.x + queueLabelWidth , queueLabelRect.y,EditorGUIUtility.labelWidth - queueLabelWidth, rect.height);
            EditorGUI.LabelField(queueLabelRect, "Queue:" );
            EditorGUI.showMixedValue = RenderQueueHasMixedValue();
            EditorGUI.BeginDisabledGroup(true);
            EditorGUI.TextField(queueNumberRect, mats[0].renderQueue.ToString());
            EditorGUI.EndDisabledGroup();
            EditorGUI.showMixedValue = false;
            Rect afterLabelRect = GetRectAfterLabelWidth(rect);
            int QueueBias = (int)queueBiasProp.floatValue;
            EditorGUI.showMixedValue = queueBiasProp.hasMixedValue;
            EditorGUI.BeginChangeCheck();
            QueueBias = EditorGUI.IntField(afterLabelRect, "QueueBias:", QueueBias);
            if (EditorGUI.EndChangeCheck())
            {
                queueBiasProp.floatValue = QueueBias;
            }
            EditorGUI.showMixedValue = false;
        }

        void GuiLine(int i_height = 1)
        {

            Rect rect = EditorGUILayout.GetControlRect(false, i_height);
            rect.height = i_height;
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));

        }

        public static float PowerSlider(Rect position, GUIContent label, float value, float leftValue, float rightValue,
            float power)
        {
            var editorGuiType = typeof(EditorGUI);
            var methodInfo = editorGuiType.GetMethod(
                "PowerSlider",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static,
                null,
                new[] { typeof(Rect), typeof(GUIContent), typeof(float), typeof(float), typeof(float), typeof(float) },
                null);
            if (methodInfo != null)
            {
                return (float)methodInfo.Invoke(null,
                    new object[] { position, label, value, leftValue, rightValue, power });
            }

            return leftValue;
        }


        private GradientAlphaKey[] defaultGradientAlphaKey = new GradientAlphaKey[]
            { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) };


        float RGBLuminance(Color color)
        {
            return 0.2126f* color.r + 0.7152f * color.g + 0.0722f * color.b;
        }

        private bool isUpateGradientPickerCache = false;

        public void DrawGradientUndoPerformed()
        {
            Debug.Log("UndoGradient");
            isUpateGradientPickerCache = true;
        }
        Dictionary<(string,string),bool> gradientsUpdateDic = new Dictionary<(string,string),bool>();
        Dictionary<(string,string),Gradient> gradientsDic = new Dictionary<(string,string),Gradient>();

        void GetGradientKeyCount(MaterialProperty countProperty,
            MaterialProperty[] colorProperties, MaterialProperty[] alphaProperties,out int countPropertyIntValue,out int colorKeysCount,out int alphaKeysCount)
        {

            countPropertyIntValue = countProperty.intValue;
            if (colorProperties != null && alphaProperties != null)
            {
                colorKeysCount = countPropertyIntValue & 0xFFFF;
                alphaKeysCount = countPropertyIntValue >> 16;
            }
            else
            {
                colorKeysCount = countPropertyIntValue;
                alphaKeysCount = 2;
            }
        }
        bool GradientPropertyHasMixedValue(MaterialProperty countProperty,
            MaterialProperty[] colorProperties = null, MaterialProperty[] alphaProperties = null)
        {
          
            
            GetGradientKeyCount(countProperty, colorProperties, alphaProperties,out int countPropertyIntValue, out int colorKeysCount,out int alphaKeysCount);
            
            bool hasMixedValue = false;
            hasMixedValue |= countProperty.hasMixedValue;
            if (colorProperties != null)
            {
                for(int i = 0; i < colorKeysCount; i++)
                {
                    hasMixedValue |= colorProperties[i].hasMixedValue;
                }
            }
            if (alphaProperties != null)
            {
                for (int i = 0; i < Mathf.CeilToInt(alphaKeysCount/2f); i++)
                {
                    hasMixedValue |= alphaProperties[i].hasMixedValue;
                }
            }
            return hasMixedValue;

            
        }

        void GetGradientConditionBool(MaterialProperty[] colorProperties,
            MaterialProperty[] alphaProperties, out bool isBlackAndWhiteGradient,
            out bool isNoAlphaColorGradient)
        {
            isBlackAndWhiteGradient = colorProperties == null && alphaProperties != null;
            isNoAlphaColorGradient =  colorProperties != null && alphaProperties == null;
        }

        void SetGradientByProperty(Gradient gradient, MaterialProperty countProperty,
            MaterialProperty[] colorProperties = null, MaterialProperty[] alphaProperties = null)
        {
            GetGradientConditionBool(colorProperties,alphaProperties,out bool isBlackAndWhiteGradient,out bool isNoAlphaColorGradient);
            GetGradientKeyCount(countProperty, colorProperties, alphaProperties,out int countPropertyIntValue, out int colorKeysCount,out int alphaKeysCount);
            
            if (colorProperties != null || isBlackAndWhiteGradient)
            {
                GradientColorKey[] colorKeys;
                if (gradient.colorKeys.Length != colorKeysCount)
                {
                    colorKeys = new GradientColorKey[colorKeysCount];
                }
                else
                {
                    colorKeys = gradient.colorKeys;
                }
                for (int i = 0; i < colorKeysCount; i++)
                {
                    if (isBlackAndWhiteGradient)
                    {
                        Vector4 vec = alphaProperties[i / 2].vectorValue;
                        Color c = Color.white;
                        if (i % 2 == 0)
                        {
                            c.r = vec.x;
                            c.g = vec.x;
                            c.b = vec.x;
                        }
                        else
                        {
                            c.r = vec.z;
                            c.g = vec.z;
                            c.b = vec.z;
                        }
                        // Debug.Log(i);
                        // Debug.Log(c);
                        colorKeys[i].color = c;
                        colorKeys[i].time = i % 2 == 0 ? vec.y : vec.w;
                    }
                    else
                    {
                        Color c = colorProperties[i].colorValue; 
                        colorKeys[i].color = c;
                        colorKeys[i].time = c.a;
                    }
                }
                gradient.colorKeys = colorKeys;
            }
      
            if (isBlackAndWhiteGradient || isNoAlphaColorGradient || alphaProperties == null)
            {
                gradient.alphaKeys = defaultGradientAlphaKey;
            }
            else
            {

                GradientAlphaKey[] alphaKeys;
                if (gradient.alphaKeys.Length != alphaKeysCount)
                {
                    alphaKeys = new GradientAlphaKey[alphaKeysCount];
                }
                else
                {
                    alphaKeys = gradient.alphaKeys;
                }

                for (int i = 0; i < alphaKeysCount ; i++)
                {
                    Vector4 vec = alphaProperties[i / 2].vectorValue;
                    if (i % 2 == 0)
                    {
                        alphaKeys[i].alpha = vec.x;
                        alphaKeys[i].time = vec.y;
                    }
                    else
                    {
                        alphaKeys[i].alpha = vec.z;
                        alphaKeys[i].time = vec.w;
                    }
                }
                gradient.alphaKeys = alphaKeys;
                
            }
        }
        //如果是黑白Gradient，则取Gradient的颜色的黑白值（这样在面板上可视化比较好）
        //如果既有颜色，也有Alpha。则在CountProperty上采取前16位和后16位编码。
        //原则：gradient对象只是一个操作中介。取值应该直接在MatProperty上去，Set值也应该在验证合法后才能Set，不合法应该提出警告。
        public void DrawGradient(bool hdr,ColorSpace colorSpace,string label,int maxCount,string countPropertyName,MaterialProperty[] colorProperties = null,MaterialProperty[] alphaProperties = null)
        {
            (string,string) nameTuple = (label, countPropertyName);

        
            if (isUpateGradientPickerCache)
            {
                foreach (var keys in gradientsUpdateDic.Keys.ToList())
                {
                    gradientsUpdateDic[keys] = true;
                }

                isUpateGradientPickerCache = false;
            }
            MaterialProperty countProperty = GetProperty(countPropertyName);
            Rect rect = EditorGUILayout.GetControlRect();

            // var labelRect = new Rect(rect.x + 2f, rect.y, rect.width - 2f, rect.height);
            // EditorGUI.LabelField(labelRect,label);
            var gradientRect = rect;
            gradientRect.width -= ResetTool.ResetButtonSize;
            gradientRect.width -= 2f;
            var gradientResetButtonRect = rect;
            gradientResetButtonRect.x = gradientResetButtonRect.x + gradientResetButtonRect.width - ResetTool.ResetButtonSize;
            gradientResetButtonRect.width = ResetTool.ResetButtonSize;

            Gradient gradient;
            if (!gradientsDic.ContainsKey(nameTuple)||gradientsUpdateDic[nameTuple])
            {
                if (!gradientsDic.ContainsKey(nameTuple))
                {
                    gradient = new Gradient();
                    // gradient.colorSpace = ColorSpace.Gamma;
                    gradientsDic.Add(nameTuple,gradient);
                    gradientsUpdateDic.Add(nameTuple,false);
                }
                else
                {
                    gradient = gradientsDic[nameTuple];
                    gradientsUpdateDic[nameTuple] = false;
                }
                
                SetGradientByProperty(gradient,countProperty, colorProperties, alphaProperties);
                
                GradientReflectionHelper.RefreshGradientData();
                // Debug.Log("----------------SetCurrentGradient------------------");
            }
            else
            {
                gradient = gradientsDic[nameTuple];
            }
            
            EditorGUI.showMixedValue = GradientPropertyHasMixedValue(countProperty, colorProperties, alphaProperties);
            GetGradientKeyCount(countProperty, colorProperties, alphaProperties,out int countPropertyIntValue, out int colorKeysCount,out int alphaKeysCount);
            GetGradientConditionBool(colorProperties, alphaProperties,out bool isBlackAndWhiteGradient,out bool isNoAlphaColorGradient);
            
            EditorGUI.BeginChangeCheck();
            gradient = EditorGUI.GradientField(gradientRect, new GUIContent(label),gradient,hdr,colorSpace);

            Action onGradientEndChangeCheck = () =>
            {
                gradientsUpdateDic[nameTuple] = true;
                int countPropertyValue = countPropertyIntValue;

                if (colorProperties != null || isBlackAndWhiteGradient)
                {
                    int finalColorKeysCount = gradient.colorKeys.Length;
                    if (finalColorKeysCount <= maxCount)
                    {
                        if (isBlackAndWhiteGradient)
                        {
                            int floatPropertyCount = Mathf.CeilToInt((float)finalColorKeysCount / (2f));
                            for (int i = 0; i < floatPropertyCount; i++)
                            {
                                Vector4 vec = Vector4.zero;
                                vec.x = gradient.colorKeys[i*2].color.r;
                                vec.y = gradient.colorKeys[i*2].time;
                                if (i * 2 + 1 < gradient.colorKeys.Length)
                                {
                                    vec.z = gradient.colorKeys[i * 2 + 1].color.r;
                                    vec.w = gradient.colorKeys[i * 2 + 1].time;
                                }

                                alphaProperties[i].vectorValue = vec;
                            }
                        }
                        else
                        {
                            for (int i = 0; i < finalColorKeysCount; i++)
                            {
                                
                                Color c = gradient.colorKeys[i].color;
                                c.a = gradient.colorKeys[i].time;
                                colorProperties[i].colorValue = c;
                            }
                        }

                        countPropertyValue &= (0xFFFF <<16);
                        countPropertyValue |= finalColorKeysCount;
                    }
                }
                
                if (!(isBlackAndWhiteGradient || isNoAlphaColorGradient || alphaProperties == null))
                {
                    int finalAlphaKeysCount = gradient.alphaKeys.Length;
                    if (finalAlphaKeysCount <= maxCount)
                    {
                        int floatPropertyCount = Mathf.CeilToInt((float)finalAlphaKeysCount / (2f));
                        for (int i = 0; i < floatPropertyCount; i++)
                        {
                            Vector4 vec = Vector4.zero;
                            vec.x = gradient.alphaKeys[i*2].alpha;
                            vec.y = gradient.alphaKeys[i*2].time;
                            if (i * 2 + 1 < gradient.alphaKeys.Length)
                            {
                                vec.z = gradient.alphaKeys[i * 2 + 1].alpha;
                                vec.w = gradient.alphaKeys[i * 2 + 1].time;
                            }
                            alphaProperties[i].vectorValue = vec;
                        }

                        countPropertyValue &= (0xFFFF);
                        int alphaCount = finalAlphaKeysCount << 16;
                        countPropertyValue |= alphaCount;
                    }
                }

                countProperty.intValue = countPropertyValue;
                ResetTool.CheckOnValueChange(nameTuple);
                
            };

            if (EditorGUI.EndChangeCheck())
            {
                onGradientEndChangeCheck();
            }
            
      
            ResetTool.DrawResetModifyButton(gradientResetButtonRect,nameTuple,resetCallBack: () =>
                {
                    ShaderPropertyPack countPropPack = ShaderPropertyPacksDic[countPropertyName];
                    
                    countPropPack.property.intValue = 2;
                    
                    if (colorProperties != null)
                    {
                        foreach (var colorProp in colorProperties)
                        {
                            ShaderPropertyPack colorPropPack = ShaderPropertyPacksDic[colorProp.name];
                            colorPropPack.property.colorValue =
                                shader.GetPropertyDefaultVectorValue(colorPropPack.index);
                        }
                    }

                    if (alphaProperties != null)
                    {
                        foreach (var alphaProps in alphaProperties)
                        {
                            ShaderPropertyPack alphaPropPack = ShaderPropertyPacksDic[alphaProps.name];
                            alphaPropPack.property.vectorValue = shader.GetPropertyDefaultVectorValue(alphaPropPack.index);
                        }
                    }
                    SetGradientByProperty(gradient,countProperty, colorProperties, alphaProperties);

                },onValueChangedCallBack:onGradientEndChangeCheck,
                checkHasMixedValueOnValueChange: () =>
                GradientPropertyHasMixedValue(countProperty, colorProperties, alphaProperties),
                checkHasModifyOnValueChange: () =>
                {
                    bool hasModified = false;
                    ShaderPropertyPack countPropPack = ShaderPropertyPacksDic[countPropertyName];

                    bool hasCountModified = !Mathf.Approximately(mats[0].GetInteger(countPropPack.name),2);
                    hasModified |= hasCountModified;
                    if (colorProperties != null)
                    {
                        foreach (var colorProp in colorProperties)
                        {
                            ShaderPropertyPack colorPropPack = ShaderPropertyPacksDic[colorProp.name];
                            hasModified |= mats[0].GetVector(colorPropPack.name) !=
                                           shader.GetPropertyDefaultVectorValue(colorPropPack.index);
                        }
                    }

                    if (alphaProperties != null)
                    {
                        foreach (var alphaProps in alphaProperties)
                        {
                            ShaderPropertyPack alphaPropPack = ShaderPropertyPacksDic[alphaProps.name];
                            Vector4 vec = mats[0].GetVector(alphaPropPack.name);
                            Vector4 defaultVec = shader.GetPropertyDefaultVectorValue(alphaPropPack.index);
                            hasModified |= vec != defaultVec;
                        }
                    }
                    return hasModified;
                });
            
            ResetTool.EndResetModifyButtonScope();
        }

        
       

    }

    public enum VectorValeType
    {
        X,Y,Z,W,XY,ZW,XYZ,XYZW,Tilling,Offset,Undefine
    }
}