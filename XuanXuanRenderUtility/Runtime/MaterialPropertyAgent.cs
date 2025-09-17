using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


//TODO增加一键去重功能。测试排查BUG
[ExecuteInEditMode]
public class MaterialPropertyAgent : MonoBehaviour,IMaterialModifier
{
    [System.Serializable]
    public struct PropertyData
    {
        [HideInInspector] public int dataIndexInAgent;
        [HideInInspector] public MaterialPropertyAgent agent;
        [HideInInspector] public int id;


        public int index;
        [HideInInspector] public string propName;

        public shaderPropertyType type;


        public string descripName;


        public Color colorValue;

        public Vector4 vecValue;

        public float floatValue;

        [SerializeField] public IEnumerable propNameList;
        [HideInInspector] public bool isActive;
        [HideInInspector] public Shader shader;
        [HideInInspector] public Material mat;

#if UNITY_EDITOR
        public float rangMin { get; set; }
        public float rangMax { get; set; }

        public void setValueByPropChange()
        {
            agent.refreshShderPropNameList();
            if (!agent.isCanUsedIndex(index))
            {
                index = agent.getCanUsedIndex();
            }
            
            string propertyName = UnityEditor.ShaderUtil.GetPropertyName(shader, index);
            id = Shader.PropertyToID(propertyName);
            descripName = UnityEditor.ShaderUtil.GetPropertyDescription(shader, index);
            type = (shaderPropertyType) UnityEditor.ShaderUtil.GetPropertyType(shader, index);
            if (type == shaderPropertyType.TexEnv)
            {
                propName = UnityEditor.ShaderUtil.GetPropertyName(shader, index) + "_ST";
            }
            else
            {
                propName = UnityEditor.ShaderUtil.GetPropertyName(shader, index);
            }

            switch (type)
            {
                case shaderPropertyType.Color:
                    colorValue = mat.GetColor(id);
                    break;
                case shaderPropertyType.Float:
                    floatValue = mat.GetFloat(id);
                    break;
                case shaderPropertyType.Range:
                    rangMin = UnityEditor.ShaderUtil.GetRangeLimits(shader, index, 1);
                    rangMax = UnityEditor.ShaderUtil.GetRangeLimits(shader, index, 2);
                    floatValue = mat.GetFloat(id);
                    break;
                case shaderPropertyType.Vector:
                    vecValue = mat.GetVector(id);
                    break;
                case shaderPropertyType.TexEnv:
                    string stName = UnityEditor.ShaderUtil.GetPropertyName(shader, index) + "_ST";
                    vecValue = mat.GetVector(stName);
                    break;
            }
        }

        public void inActivateThis()
        {
            isActive = false;
        }
#endif
    }

    //因为ShaderUtil只是用于Editor，所以复制一个枚举对属性类型进行识别。

    public enum shaderPropertyType
    {
        //
        // 摘要:
        //     Color Property.
        Color = 0,

        //
        // 摘要:
        //     Vector Property.
        Vector = 1,

        //
        // 摘要:
        //     Float Property.
        Float = 2,

        //
        // 摘要:
        //     Range Property.
        Range = 3,

        //
        // 摘要:
        //     Texture Property.
        TexEnv = 4
    }


    public PropertyData data0 = new PropertyData();

    public PropertyData data1 = new PropertyData();

    public PropertyData data2 = new PropertyData();

    public PropertyData data3 = new PropertyData();

    public PropertyData data4 = new PropertyData();

    public PropertyData data5 = new PropertyData();

    public Shader shader;
    public Material mat;


    public int materialIndex = 0;
    public Renderer customRenderer;

    void initMatAndShader(bool initMat = false)
    {
        if (!initMat)
        {
            if (mat != null) return;
        }

        if (customRenderer || GetComponent<Renderer>())
        {
            Material[] materials;
            Renderer r;
            if (customRenderer)
            {
                r = customRenderer;
            }
            else
            {
                r = GetComponent<Renderer>();
            }
            if (Application.isPlaying)
            {
                materials = r.materials;
            }
            else
            {
                materials = r.sharedMaterials;
            }
          
            mat = materials[materialIndex];
            shader = mat.shader;
        }
        else if (this.GetComponent<Graphic>())
        {
            Graphic graphic = this.GetComponent<Graphic>();
   
//测试，要不要用IMaterialModifier来处理
            // if (Application.isPlaying)
            // {
            //     graphic.material = Material.Instantiate(graphic.material);
            // }

            mat = graphic.material;


            shader = mat.shader;
        }
        else
        {
            Debug.LogError("MaterialPropertyAgent未找到材质", this.gameObject);
        }
    }

    private void Start()
    {
        if (Application.isPlaying)
        {
            initMatAndShader(true);
        }
        else
        {
            initMatAndShader(false);
        }
    }

    private void Update()
    {
        if (mat == null)
        {
            // initMatAndShader();
            return;
        }

        updateData(data0);
        updateData(data1);
        updateData(data2);
        updateData(data3);
        updateData(data4);
        updateData(data5);
    }

    void updateData(PropertyData data)
    {
        if (!data.isActive) return;
        if (mat == null) return;
        switch (data.type)
        {
            case shaderPropertyType.Color:
                mat.SetColor(data.propName, data.colorValue);
                break;
            case shaderPropertyType.Vector:
            case shaderPropertyType.TexEnv:
                mat.SetVector(data.propName, data.vecValue);
                break;
            case shaderPropertyType.Float:
            case shaderPropertyType.Range:
                mat.SetFloat(data.propName, data.floatValue);
                break;
        }
    }

    //实际上是修改了graphic.materialForRendering
    public Material GetModifiedMaterial(Material baseMaterial)
    {
        if (mat) 
        {
            return mat;
        }
        else
        {
            return baseMaterial;
        }
    }

#if UNITY_EDITOR
    
    private void OnRenderObject()
    {
        if (!UnityEditor.EditorApplication.isPlaying)
        {
            Update();
        }
    }

    public void addProperteData()
    {
        refreshShderPropNameList();

        if (!data0.isActive)
        {
            initData(ref data0, 0);
        }
        else if (!data1.isActive)
        {
            initData(ref data1, 1);
        }
        else if (!data2.isActive)
        {
            initData(ref data2, 2);
        }
        else if (!data3.isActive)
        {
            initData(ref data3, 3);
        }
        else if (!data4.isActive)
        {
            initData(ref data4, 4);
        }
        else if (!data5.isActive)
        {
            initData(ref data5, 5);
        }
        else
        {
            Debug.Log("已用掉可用的6个属性");
        }
    }

    public void removeAllProperty()
    {
        data0.isActive = false;
        data1.isActive = false;
        data2.isActive = false;
        data3.isActive = false;
        data4.isActive = false;
        data5.isActive = false;
    }

    public void initData(ref PropertyData data, int dataIndexInAgent)
    {
        data.dataIndexInAgent = dataIndexInAgent;
        data.agent = this;
        data.shader = shader;
        data.mat = mat;
        data.isActive = true;

        data.index = getCanUsedIndex();
        data.setValueByPropChange();
    }

    #region TODO自动排除已用Property

    List<string> usedPropertyName = new List<string>();

    void collectUsedPropName()
    {
        usedPropertyName.Clear();
        if (data0.isActive)
        {
            usedPropertyName.Add(data0.propName);
        }

        if (data1.isActive)
        {
            usedPropertyName.Add(data1.propName);
        }

        if (data2.isActive)
        {
            usedPropertyName.Add(data2.propName);
        }

        if (data3.isActive)
        {
            usedPropertyName.Add(data3.propName);
        }

        if (data4.isActive)
        {
            usedPropertyName.Add(data4.propName);
        }

        if (data5.isActive)
        {
            usedPropertyName.Add(data5.propName);
        }
    }

    public int getCanUsedIndex()
    {
        int index = -1;
        collectUsedPropName();
        for (int i = 0; i < shaderPropNameArr.Length; i++)
        {
            string propertyName;
            if (UnityEditor.ShaderUtil.GetPropertyType(shader, i) == UnityEditor.ShaderUtil.ShaderPropertyType.TexEnv)
            {
                propertyName = shaderPropNameArr[i] + "_ST";
            }
            else
            {
                propertyName = shaderPropNameArr[i];
            }

            if (usedPropertyName.Contains(propertyName)) continue;
            index = i;
            break;
        }

        return index;
    }

    public bool isCanUsedIndex(int i)
    {
        string propertyName;
        if (UnityEditor.ShaderUtil.GetPropertyType(shader, i) == UnityEditor.ShaderUtil.ShaderPropertyType.TexEnv)
        {
            propertyName = shaderPropNameArr[i] + "_ST";
        }
        else
        {
            propertyName = shaderPropNameArr[i];
        }

        collectUsedPropName();
        if (usedPropertyName.Contains(propertyName))
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    #endregion

    public string[] shaderPropNameArr;
    public string[] shaderPropDescripArr;
    public string[] shaderPropDescripsForSerch;

    private void OnValidate()
    {
        // if (XLuaManager.Instance != null)
        // {
        //     if (XLuaManager.Instance.HasGameStart)//判断是游戏运行状态才进行实例化
        //     {
        //         return;//游戏进行中不允许编辑
        //     }
        // }
        // Debug.Log("MaterialPropertyAgent : " + "OnValidate");
        refreshShderPropNameList();
        if (TryGetComponent<Renderer>(out Renderer r)||customRenderer)
        {
            isRendererMode = true;
        }
        else
        {
            isRendererMode = false;
        }

        if (TryGetComponent<Renderer>(out Renderer r2) || TryGetComponent<Graphic>(out Graphic g))
        {
            isGetByComponet = true;
        }
        else
        {
            isGetByComponet = false;
        }
    }
    public bool isRendererMode = false;
    public bool isGetByComponet = false;
    public void initMatAndShaderByMaterialIndexChange()
    {
        initMatAndShader(true);
        refreshShderPropNameList();
        if (data0.isActive && data0.shader != mat.shader) {data0.shader = mat.shader;data0.mat = mat;}
        if (data1.isActive && data1.shader != mat.shader) {data1.shader = mat.shader;data1.mat = mat;}
        if (data2.isActive && data2.shader != mat.shader) {data2.shader = mat.shader;data2.mat = mat;}
        if (data3.isActive && data3.shader != mat.shader) {data3.shader = mat.shader;data3.mat = mat;}
        if (data4.isActive && data4.shader != mat.shader) {data4.shader = mat.shader;data4.mat = mat;}
        if (data5.isActive && data5.shader != mat.shader) {data5.shader = mat.shader;data5.mat = mat;}

    }


    List<string> shaderPropNameList = new List<string>();
    private List<string> shaderPropDescripList = new List<string>();
    private List<string> shaderPropDescripListForSerch = new List<string>();
    public void refreshShderPropNameList()
    {
        initMatAndShader();
        if (shader == null) return;

        shaderPropNameList.Clear();
        shaderPropDescripList.Clear();
        shaderPropDescripListForSerch.Clear();
        for (int i = 0; i < UnityEditor.ShaderUtil.GetPropertyCount(shader); i++)
        {
            shaderPropNameList.Add(UnityEditor.ShaderUtil.GetPropertyName(shader, i));
            string descript = UnityEditor.ShaderUtil.GetPropertyDescription(shader, i);
            shaderPropDescripList.Add(descript);
            string lowerDesc = descript.ToLower();
            if (!(lowerDesc.Contains("ignore") || lowerDesc.Contains("mode") || lowerDesc.Contains("toggle") ||
                  lowerDesc.Contains("enable") || lowerDesc.Contains("flag")))
            {
                shaderPropDescripListForSerch.Add(descript);
            }
        }

        shaderPropNameArr = shaderPropNameList.ToArray();
        shaderPropDescripArr = shaderPropDescripList.ToArray();
        shaderPropDescripsForSerch = shaderPropDescripListForSerch.ToArray();
    }
#endif
}


/*
[CustomPropertyDrawer(typeof(MaterialPropertyAgent.PropertyData))]
public class PropertyAgentPropertyDataDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property); 

        var isActive = property.FindPropertyRelative("isActive");
        if (isActive.boolValue)
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

            EditorGUI.indentLevel++;

            EditorGUILayout.BeginHorizontal();
            var index = property.FindPropertyRelative("index"); 
            MaterialPropertyAgent agent = property.FindPropertyRelative("agent").objectReferenceValue as MaterialPropertyAgent;
            int preservedIndex = index.intValue;
            float originLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 80;
           index.intValue = EditorGUILayout.Popup("属性名:", index.intValue, agent.shaderPropNameArr);
            if (preservedIndex != index.intValue)//证明用户进行了更改
            {
                if (!agent.isCanUsedIndex(index.intValue))
                {
                    //TODO给一个报错提示
                    Debug.Log(ShaderUtil.GetPropertyDescription(agent.shader, index.intValue));
                    index.intValue = agent.getCanUsedIndex();
                }
                //此处进行内容刷新
                data.setValueByPropChange();
            }


            EditorGUILayout.LabelField("属性类型:", data.type.ToString());

            EditorGUILayout.EndHorizontal();
            EditorGUIUtility.labelWidth = originLabelWidth;
            switch (data.type)
            {
                case MaterialPropertyAgent.shaderPropertyType.Color:
                    data.colorValue = EditorGUILayout.ColorField(data.descripName + " :", data.colorValue);
                    break;
                case MaterialPropertyAgent.shaderPropertyType.Vector:
                    data.vecValue = EditorGUILayout.Vector4Field(data.descripName + " :", data.vecValue);
                    break;
                case MaterialPropertyAgent.shaderPropertyType.Float:
                    data.floatValue = EditorGUILayout.FloatField(data.descripName + ":", data.floatValue);
                    break;
                case MaterialPropertyAgent.shaderPropertyType.Range:
                    data.floatValue = EditorGUILayout.Slider(data.descripName + ":", data.floatValue, data.rangMin, data.rangMax);
                    break;
                case MaterialPropertyAgent.shaderPropertyType.TexEnv:
                    data.vecValue = EditorGUILayout.Vector4Field(data.propName + "_ST:", data.vecValue);
                    break;
            }

            if (GUILayout.Button("删除", new[] { GUILayout.Width(200) }))
            {
                data.isActive = false;
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }
    }

    EditorGUI.EndProperty();
    }

}
*/