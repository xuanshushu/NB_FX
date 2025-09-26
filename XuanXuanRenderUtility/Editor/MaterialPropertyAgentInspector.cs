using UnityEditor;
using UnityEngine;
using UnityEditor.Experimental.GraphView;
using System;
using NBShader;

namespace NBShaderEditor
{
// #if UNITY_EDITOR
    [CustomEditor(typeof(MaterialPropertyAgent))]
    public class MaterialPropertyAgentInspector : UnityEditor.Editor
    {
        private MaterialPropertyAgent agent;

        private void OnEnable()
        {
            agent = (MaterialPropertyAgent)target;
        }

        private GUIContent materialIndexContent = new GUIContent("材质序号:", "只有模型模式下需要使用，谨慎修改");

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            if (!agent.isGetByComponet)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("customRenderer"),
                    new GUIContent("指定Renderer"));
            }

            if (agent.isRendererMode)
            {
                EditorGUILayout.BeginHorizontal();
                int lastIndex = agent.materialIndex;
                agent.materialIndex = EditorGUILayout.IntField(materialIndexContent, agent.materialIndex);
                if (lastIndex != agent.materialIndex)
                {
                    agent.initMatAndShaderByMaterialIndexChange();
                }

                EditorGUILayout.LabelField("材质名：" + agent.mat.name + "\t" + "Shader名：" + agent.shader.name);
                EditorGUILayout.EndHorizontal();
            }

            DrawPropertyData(ref agent.data0, "Data0", serializedObject.FindProperty("data0"));
            DrawPropertyData(ref agent.data1, "Data1", serializedObject.FindProperty("data1"));
            DrawPropertyData(ref agent.data2, "Data2", serializedObject.FindProperty("data2"));
            DrawPropertyData(ref agent.data3, "Data3", serializedObject.FindProperty("data3"));
            DrawPropertyData(ref agent.data4, "Data4", serializedObject.FindProperty("data4"));
            DrawPropertyData(ref agent.data5, "Data5", serializedObject.FindProperty("data5"));
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("添加"))
            {
                agent.addProperteData();
            }

            if (GUILayout.Button("全部删除"))
            {
                agent.removeAllProperty();
            }

            EditorGUILayout.EndHorizontal();
            serializedObject.ApplyModifiedProperties();
            if (EditorGUI.EndChangeCheck())
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(agent);
                if (!agent.isGetByComponet && agent.mat)
                {
                    if (matEditor) DestroyImmediate(matEditor);
                    matEditor = (MaterialEditor)CreateEditor(agent.mat);
                }
            }

            if (!agent.isGetByComponet && agent.mat)
            {
                DrawMaterialInspector(matEditor, agent.mat);
            }
        }

        void DrawPropertyData(ref MaterialPropertyAgent.PropertyData data, string dataLabel,
            SerializedProperty property)
        {
            EditorGUI.BeginProperty(EditorGUILayout.GetControlRect(false, 0f), GUIContent.none, property);
            if (data.isActive)
            {
                EditorGUILayout.LabelField(dataLabel, EditorStyles.boldLabel);

                EditorGUI.indentLevel++;

                EditorGUILayout.BeginHorizontal();
                float originLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 80;
                // data.index = EditorGUILayout.Popup("属性名:", data.index, agent.shaderPropNameArr);
                // data.index = EditorGUILayout.Popup("属性名:", data.index, agent.shaderPropDescripArr);
                // string dataDesript = data.descripName;
                if (GUILayout.Button(data.descripName, EditorStyles.popup))
                {
                    int dataIndexInAgent = data.dataIndexInAgent;

                    StringListSerchProvider provider = ScriptableObject.CreateInstance<StringListSerchProvider>();
                    provider.Initialize(agent.shaderPropDescripsForSerch, (x) =>
                    {
                        if (x != null)
                        {
                            AfterShaderPropSerch(dataIndexInAgent, x);
                        }
                    });
                    SearchWindow.Open(new SearchWindowContext(GUIUtility.GUIToScreenPoint(Event.current.mousePosition)),
                        provider);
                    // SearchWindow.Open(new SearchWindowContext(GUIUtility.GUIToScreenPoint(Event.current.mousePosition)),
                    //     new StringListSerchProvider(agent.shaderPropDescripsForSerch, (x) =>
                    //     {
                    //         if (x != null)
                    //         {
                    //             AfterShaderPropSerch(dataIndexInAgent,x);
                    //         }
                    //     }));
                }



                EditorGUILayout.LabelField("属性类型:", data.type.ToString());

                EditorGUILayout.EndHorizontal();
                EditorGUIUtility.labelWidth = originLabelWidth;
                switch (data.type)
                {
                    case MaterialPropertyAgent.shaderPropertyType.Color:
                        SerializedProperty colorProp = property.FindPropertyRelative("colorValue");
                        if (data.descripName.ToLower().Contains("hdr"))
                        {
                            colorProp.colorValue = EditorGUILayout.ColorField(new GUIContent(data.propName + " :"),
                                colorProp.colorValue, true, true, true);
                        }
                        else
                        {
                            colorProp.colorValue =
                                EditorGUILayout.ColorField(data.propName + " :", colorProp.colorValue);
                        }

                        break;
                    case MaterialPropertyAgent.shaderPropertyType.Vector:
                    case MaterialPropertyAgent.shaderPropertyType.TexEnv:
                        SerializedProperty vecProp = property.FindPropertyRelative("vecValue");
                        if (data.type == MaterialPropertyAgent.shaderPropertyType.Vector)
                        {
                            vecProp.vector4Value =
                                EditorGUILayout.Vector4Field(data.propName + " :", vecProp.vector4Value);
                        }
                        else if (data.type == MaterialPropertyAgent.shaderPropertyType.TexEnv)
                        {
                            vecProp.vector4Value =
                                EditorGUILayout.Vector4Field(data.propName + ":", vecProp.vector4Value);

                        }

                        break;
                    case MaterialPropertyAgent.shaderPropertyType.Float:
                    case MaterialPropertyAgent.shaderPropertyType.Range:
                        SerializedProperty floatProp = property.FindPropertyRelative("floatValue");
                        if (data.type == MaterialPropertyAgent.shaderPropertyType.Float)
                        {
                            floatProp.floatValue =
                                EditorGUILayout.FloatField(data.propName + ":", floatProp.floatValue);
                        }
                        else if (data.type == MaterialPropertyAgent.shaderPropertyType.Range)
                        {
                            floatProp.floatValue = EditorGUILayout.Slider(data.propName + ":", floatProp.floatValue,
                                data.rangMin, data.rangMax);

                        }

                        break;

                }

                if (GUILayout.Button("删除", new[] { GUILayout.Width(200) }))
                {
                    data.isActive = false;
                }

                EditorGUI.indentLevel--;
                // EditorGUILayout.Space();
            }

            EditorGUI.EndProperty();
        }

        //因为Action不能有Ref。所以有了这个丑陋的HardCode
        public void AfterShaderPropSerch(int dataIndexInAgent, string propertyDesrpt)
        {
            switch (dataIndexInAgent)
            {
                case 0:
                    AfterShaderPropSerch(ref agent.data0, propertyDesrpt);
                    break;
                case 1:
                    AfterShaderPropSerch(ref agent.data1, propertyDesrpt);
                    break;
                case 2:
                    AfterShaderPropSerch(ref agent.data2, propertyDesrpt);
                    break;
                case 3:
                    AfterShaderPropSerch(ref agent.data3, propertyDesrpt);
                    break;
                case 4:
                    AfterShaderPropSerch(ref agent.data4, propertyDesrpt);
                    break;
                case 5:
                    AfterShaderPropSerch(ref agent.data5, propertyDesrpt);
                    break;


            }
        }

        public void AfterShaderPropSerch(ref MaterialPropertyAgent.PropertyData data, string propertyDesrpt)
        {
            int preservedIndex = data.index;
            // string propname = data.propName;
            data.index = Array.FindIndex(agent.shaderPropDescripArr, x => x.Equals(propertyDesrpt));
            // Debug.Log();
            if (preservedIndex != data.index) //证明用户进行了更改
            {
                if (!agent.isCanUsedIndex(data.index))
                {
                    //TODO给一个报错提示
                    Debug.LogError("材质属性已经存在：" + ShaderUtil.GetPropertyDescription(agent.shader, data.index));
                    data.index = agent.getCanUsedIndex();
                }

                //此处进行内容刷新
                data.setValueByPropChange();
            }
        }

        private MaterialEditor matEditor;

        void DrawMaterialInspector(MaterialEditor editor, Material mat)
        {
            if (editor != null && mat != null)
            {
                // Draw the material's foldout and the material shader field
                // Required to call _materialEditor.OnInspectorGUI ();
                editor.DrawHeader();
                //  We need to prevent the user to edit Unity default materials
                bool isDefaultMaterial = !AssetDatabase.GetAssetPath(mat).StartsWith("Assets");
                using (new EditorGUI.DisabledGroupScope(isDefaultMaterial))
                {

                    // Draw the material properties
                    // Works only if the foldout of _materialEditor.DrawHeader () is open
                    editor.OnInspectorGUI();
                }
            }
        }

    }
}
// #endif
