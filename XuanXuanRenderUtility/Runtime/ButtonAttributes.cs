using System.Reflection;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NBShader
{
    public class InspectorButtonAttribute : PropertyAttribute
    {
        public string Label;
        public string MethodName;

        public InspectorButtonAttribute(string label, string methodName)
        {
            this.Label = label;
            this.MethodName = methodName;
        }
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(InspectorButtonAttribute))]
    public class InspectorButtonPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {

            InspectorButtonAttribute buttonAttribute = (InspectorButtonAttribute)attribute;


            // 绘制按钮
            if (GUI.Button(position, buttonAttribute.Label))
            {
                // 获取包含该方法的对象
                var targetObject = property.serializedObject.targetObject;

                // 获取方法信息
                var methodInfo = targetObject.GetType().GetMethod(buttonAttribute.MethodName,
                    BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                if (methodInfo != null)
                {
                    // 调用方法
                    methodInfo.Invoke(targetObject, null);
                }
                else
                {
                    Debug.LogError($"Method '{buttonAttribute.MethodName}' not found in {targetObject.name}.");
                }
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight; // 返回按钮的高度
        }
    }
#endif
}


