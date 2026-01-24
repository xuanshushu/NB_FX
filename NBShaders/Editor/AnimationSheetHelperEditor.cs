using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AnimationSheetHelper))]
public class AnimationSheetHelperEditor : Editor
{
    SerializedProperty isParticleBaseShader;
    SerializedProperty isPostProcessShader;
    SerializedProperty postProcessingController;
    SerializedProperty propertyName;

    SerializedProperty xSize;
    SerializedProperty ySize;

    SerializedProperty manualPlay;
    SerializedProperty manualPlayePos;
    SerializedProperty speed;

    SerializedProperty scaleOffset;
    SerializedProperty mat;
    SerializedProperty frameIndex;
    SerializedProperty frameCount;

    private AnimationSheetHelper _target;

    void OnEnable()
    {
        _target = (AnimationSheetHelper)target;

        isParticleBaseShader = serializedObject.FindProperty("isParticleBaseShader");
        // isPostProcessShader  = serializedObject.FindProperty("isPostProcessShader");
        // postProcessingController = serializedObject.FindProperty("postProcessingController");
        propertyName = serializedObject.FindProperty("propertyName");

        xSize = serializedObject.FindProperty("xSize");
        ySize = serializedObject.FindProperty("ySize");

        manualPlay = serializedObject.FindProperty("manualPlay");
        manualPlayePos = serializedObject.FindProperty("manualPlayePos");
        speed = serializedObject.FindProperty("speed");

        scaleOffset = serializedObject.FindProperty("scaleOffset");
        mat = serializedObject.FindProperty("mat");
        frameIndex = serializedObject.FindProperty("frameIndex");
        frameCount = serializedObject.FindProperty("frameCount");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawShaderSection();
        EditorGUILayout.Space(5);
        DrawAnimationSection();
        EditorGUILayout.Space(5);
        DrawRuntimeInfo();
        EditorGUILayout.Space(8);
        DrawInitButton();

        serializedObject.ApplyModifiedProperties();
    }

    #region Sections

    void DrawShaderSection()
    {
        EditorGUILayout.LabelField("Shader 设置", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(isParticleBaseShader, new GUIContent("使用 NB Shader"));
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
            _target.InitParticleBaseShaderToggle();
            EditorUtility.SetDirty(_target);
        }

        // EditorGUI.BeginChangeCheck();
        // EditorGUILayout.PropertyField(isPostProcessShader, new GUIContent("使用 后处理扭曲"));
        // if (EditorGUI.EndChangeCheck())
        // {
        //     serializedObject.ApplyModifiedProperties();
        //     // _target.InitPostProcessToggle();
        //     EditorUtility.SetDirty(_target);
        // }

        // if (isPostProcessShader.boolValue)
        // {
        //     GUI.enabled = false;
        //     EditorGUILayout.PropertyField(postProcessingController);
        //     GUI.enabled = true;
        // }

        EditorGUILayout.PropertyField(propertyName, new GUIContent("Shader 属性名"));
    }

    void DrawAnimationSection()
    {
        EditorGUILayout.LabelField("序列帧设置", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(xSize, new GUIContent("横向帧数量"));
        EditorGUILayout.PropertyField(ySize, new GUIContent("纵向帧数量"));
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
            _target.Init();
            EditorUtility.SetDirty(_target);
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.PropertyField(manualPlay, new GUIContent("手动控制播放"));

        if (manualPlay.boolValue)
        {
            EditorGUILayout.Slider(manualPlayePos, 0f, 1f, new GUIContent("手动播放位置"));
        }
        else
        {
            EditorGUILayout.PropertyField(speed, new GUIContent("播放速度 fps"));
        }
    }

    void DrawRuntimeInfo()
    {
        EditorGUILayout.LabelField("运行时信息", EditorStyles.boldLabel);

        GUI.enabled = false;
        EditorGUILayout.PropertyField(scaleOffset, new GUIContent("Tiling Offset"));
        EditorGUILayout.PropertyField(mat, new GUIContent("Material"));
        EditorGUILayout.PropertyField(frameIndex);
        EditorGUILayout.PropertyField(frameCount);
        GUI.enabled = true;
    }

    void DrawInitButton()
    {
        if (GUILayout.Button("初始化", GUILayout.Height(28)))
        {
            _target.Init();
            EditorUtility.SetDirty(_target);
        }
    }

    #endregion
}
