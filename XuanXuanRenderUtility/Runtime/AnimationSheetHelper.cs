using System;
using UnityEngine;
// using Sirenix.OdinInspector;
// using Unity.Mathematics;
using UnityEngine.UI;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class AnimationSheetHelper : MonoBehaviour,IMaterialModifier
{

    public bool isParticleBaseShader = true;
    
    public string propertyName = "_BaseMap_ST";

    private int _propertyID;
    private int _particleBaseAniBlendStPropertyID = Shader.PropertyToID("_BaseMap_AnimationSheetBlend_ST");
    private int _particleBaseAniBlendIntensityPropertyID = Shader.PropertyToID("_AnimationSheetHelperBlendIntensity");
    private static readonly int ParticleShaderFlagsId = Shader.PropertyToID("_W9ParticleShaderFlags");
    private static readonly int ParticleShaderFlags1Id = Shader.PropertyToID("_W9ParticleShaderFlags1");
    private static readonly int NBShaderFlagsId = Shader.PropertyToID("_NBShaderFlags");
    private static readonly int NBShaderFlags1Id = Shader.PropertyToID("_NBShaderFlags1");
    private const int FlagBitUIEffectOn = 1 << 14;
    private const int FlagBitAnimationSheetHelper = 1 << 15;
    private const int FlagBitUIEffectBaseMapMode = 1 << 22;
    


    // [LabelText("序列帧图横向帧数量")]
    // [OnValueChanged("Init")]
    public int xSize = 4;

    // [LabelText("序列帧图纵向帧数量")]
    // [OnValueChanged("Init")]
    public int ySize = 4;

    // [LabelText("手动控制播放")] 
    public bool manualPlay = false;

    // [ShowIf("manualPlay")]
    // [LabelText("手动播放位置")] [Range(0, 1)] 
    public float manualPlayePos = 0;
    
    // [LabelText("播放速度fps")]
    // [HideIf("manualPlay")]
    public float speed = 16;

    // [ReadOnly]
    // [LabelText("TillingOffset")]
    public Vector4 scaleOffset;
    // [ReadOnly]
    public Material mat;

    // [ReadOnly]
    public int frameIndex;
    // [ReadOnly]
    public int frameCount;

    private float _time;

    private float _xScale;
    private float _yScale;

    private static List<Material> usedMaterialList = new List<Material>();
    
    
    // // Start is called before the first frame update
    // void Start()
    // {
    //     Debug.Log( "ASUpadate_Start");
    //     Init();
    // }
    private void OnEnable()
    {
        // Debug.Log( "ASUpadate_OnEnable");
        Init();
        #if UNITY_EDITOR
            EditorApplication.update += EditorUpdate;
        #endif
    }

    private void OnDisable()
    {
        Init();

        //清掉，避免参数保留。
        if (isParticleBaseShader)
        {
            if (mat)
            {
                GetShaderFlagIds(mat, out _, out int flags1Id);
                ClearFlagBits(mat, flags1Id, FlagBitAnimationSheetHelper);
            }
        }

        if (usedMaterialList.Contains(mat))
        {
            usedMaterialList.Remove(mat);
        }
        #if UNITY_EDITOR
                EditorApplication.update -= EditorUpdate;
        #endif
    }

    // [Button("初始化")]
    public void Init()
    {
        _time = 0;
        if (xSize <= 0)
        {
            xSize = 1;
        }

        if (ySize <= 0)
        {
            ySize = 1;
        }
        frameCount = xSize * ySize;

        _xScale = 1 / (float)xSize;
        _yScale = 1 / (float)ySize;
        // if (frameCount <= 0)
        // {
        //     frameCount = 1;
        // }
        // scaleOffset = new Vector4(1f/(float)xSize,1f/ (float)ySize, 0, 0);

        if (gameObject.TryGetComponent(out Graphic g))
        {
            if (Application.isPlaying)
            {
                mat = g.materialForRendering;
            }
            else
            {
                mat = g.material;
            }
        }
        else if (gameObject.TryGetComponent(out Renderer r))
        {
            if (Application.isPlaying)
            {
                mat = r.material;
            }
            else
            {
                mat = r.sharedMaterial;
            }
            // Debug.Log("AS_GetRenderer:Mat--"+mat.name);
            
        }
        else
        {
            mat = null;
        }

        InitParticleBaseShaderToggle();
        // InitPostProcessToggle();
        

        _propertyID = Shader.PropertyToID(propertyName);
        if (mat != null)
        {
            mat.SetVector(_propertyID,CalSt(0));
        }

    }
    
    

    public void InitParticleBaseShaderToggle()
    {
        if (isParticleBaseShader)
        {
            GetShaderFlagIds(mat, out int flagsId, out int flags1Id);
            // Debug.Log(CheckFlagBits(mat, ParticleShaderFlags1Id, FlagBitUIEffectBaseMapMode));
            if(CheckFlagBits(mat, flagsId, FlagBitUIEffectOn) && !CheckFlagBits(mat, flags1Id, FlagBitUIEffectBaseMapMode))
            {
                propertyName = "_UI_MainTex_ST";
            }
            else
            {
                propertyName = "_BaseMap_ST";
            }
            if (mat)
            {
                SetFlagBits(mat, flags1Id, FlagBitAnimationSheetHelper);
            }
        }
        // else
        // {
        //     if (mat)
        //     {
        //         if (mat.shader.name == "Mh2/Effects/Particle_NiuBi")
        //         {
        //             _flags.SetMaterial(mat);
        //             _flags.ClearFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_ANIMATION_SHEET_HELPER,index:1);
        //         }
        //     }
        // }
    }

    // public void InitPostProcessToggle()
    // {
    //     if (isPostProcessShader)
    //     {
    //         TryGetComponent<PostProcessingController>(out postProcessingController);
    //     }
    //     else
    //     {
    //         postProcessingController = null;
    //     }
    // }


    // Update is called once per frame
    private int _lastIndex;
    private int _nextIndex = 1;
    private float _blendLerp;
    private void Update()
    {
        // if (!isPostProcessShader)//后处理控制不通过才知。
        // {
        //     if(!mat || _propertyID==0) return;
        // }

  

        float frameIndexFloat;
        if (manualPlay)
        {
            float playPos = Mathf.Repeat(manualPlayePos, 1f);
            frameIndexFloat = playPos * frameCount;
        }
        else
        {
            if (!Application.isPlaying)
            {
                #if UNITY_EDITOR
                    _time += (float)editorDeltaTime * speed;
                #endif
            }
            else
            {
                _time += Time.deltaTime * speed;
            }
            frameIndexFloat = _time % frameCount;
        }
        frameIndex = (int) frameIndexFloat;
        if (frameIndex == frameCount)
        {
            frameIndex = frameCount - 1;
        }
        
        
        if (_lastIndex != frameIndex)
        {
            // if (isPostProcessShader)
            // {
            //     postProcessingController.distortSpeedTexSt = CalSt(frameIndex);
            // }
            // else
            // {
                mat.SetVector(_propertyID,CalSt(frameIndex));
            // }
            _lastIndex = frameIndex;
            _nextIndex = frameIndex + 1;
            if (isParticleBaseShader)
            {
                mat.SetVector(_particleBaseAniBlendStPropertyID,CalSt(_nextIndex));
            }
     
        }

        if (isParticleBaseShader)
        {
            _blendLerp =  Mathf.Repeat(frameIndexFloat,1f);
            mat.SetFloat(_particleBaseAniBlendIntensityPropertyID,_blendLerp);
        }

    }

    public Material GetModifiedMaterial(Material baseMaterial)
    {
        if (usedMaterialList.Contains(baseMaterial))
        {
            //有可能打断UI合批，烦请客户端判断
            Material newMat = Instantiate(baseMaterial);
            mat = newMat;
            usedMaterialList.Add(newMat);
        }
        else
        {
            mat = baseMaterial;
            usedMaterialList.Add(baseMaterial);
        }
        return mat;
    }
    
    
    

    private Vector4 CalSt(int index)
    {
        float xOffset = (index % xSize)*_xScale;
        float yOffset = (ySize - index / xSize -1)*_yScale;
        return new Vector4(_xScale, _yScale, xOffset, yOffset);
    }

    private static void SetFlagBits(Material material, int propertyId, int bits)
    {
        if (!material)
        {
            return;
        }

        material.SetInteger(propertyId, material.GetInteger(propertyId) | bits);
    }

    private static void ClearFlagBits(Material material, int propertyId, int bits)
    {
        if (!material)
        {
            return;
        }

        material.SetInteger(propertyId, material.GetInteger(propertyId) & ~bits);
    }

    private static bool CheckFlagBits(Material material, int propertyId, int bits)
    {
        return material && (material.GetInteger(propertyId) & bits) != 0;
    }

    private static void GetShaderFlagIds(Material material, out int flagsId, out int flags1Id)
    {
        if (material && (material.HasProperty(NBShaderFlagsId) || material.HasProperty(NBShaderFlags1Id)))
        {
            flagsId = NBShaderFlagsId;
            flags1Id = NBShaderFlags1Id;
            return;
        }

        flagsId = ParticleShaderFlagsId;
        flags1Id = ParticleShaderFlags1Id;
    }

#if UNITY_EDITOR
    private double editorDeltaTime = 0;
    private double lastEditorTime = 0;
    void EditorUpdate()
    {
        
        if (lastEditorTime == 0)
        {
            lastEditorTime = EditorApplication.timeSinceStartup;
        }
        editorDeltaTime = EditorApplication.timeSinceStartup - lastEditorTime;
        lastEditorTime = EditorApplication.timeSinceStartup;
        if (!Application.isPlaying)
        {
            Update();
        }
    }
#endif
    
}
