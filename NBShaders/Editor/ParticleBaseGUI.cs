using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor.AnimatedValues;
using UnityEditor;
using NBShader;
using UVMode = NBShader.W9ParticleShaderFlags.UVMode;

namespace NBShaderEditor
{
    public class ParticleBaseGUI : ShaderGUI
    {
        private ShaderGUIHelper _helper = new ShaderGUIHelper();
        public List<Material> mats = new List<Material>();
        private Shader shader;
        private MaterialEditor matEditor;
        public List<W9ParticleShaderFlags> shaderFlags = new List<W9ParticleShaderFlags>();

        private int lastFlagBit;

        // private bool isCustomedStencil = false;//isCustomStencil应该各个材质各自控制。
        private readonly int _isCustomedStencilPropID = Shader.PropertyToID("_CustomStencilTest");
        private readonly string _defaultStencilKey = "ParticleBaseDefault";

        private StencilValuesConfig _stencilValuesConfig;

        private bool isInit = true;

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            //一定要初始化在第一行
            mats.Clear();
            shaderFlags.Clear();
            for (int i = 0; i < materialEditor.targets.Length; i++)
            {
                var targetMat = materialEditor.targets[i] as Material;

                mats.Add(targetMat);
                shaderFlags.Add(new W9ParticleShaderFlags(mats[i]));

            }

            if (!_stencilValuesConfig)
            {
                _stencilValuesConfig =
                    AssetDatabase.LoadAssetAtPath<StencilValuesConfig>(
                        "Packages/com.xuanxuan.nb.fx/NBShaders/Shader/StencilConfig.asset");
            }

            matEditor = materialEditor;
            EditorGUIUtility.labelWidth = 180f;
            _helper.Init(materialEditor, props, shaderFlags.ToArray(), mats);

            if (isInit)
            {
                CacheRenderersUsingThisMaterial(mats[0],0);//应该只Cache这一次就可以了。
                isInit = false;
            }

            _helper.DrawToolBar();
            EditorGUI.BeginChangeCheck();

            _helper.DrawBigBlockFoldOut(W9ParticleShaderFlags.foldOutBitMeshOption, 3, GetAnimBoolIndex(3), "模式设置",
                () => DrawMeshOptions(),false);
                
            _helper.DrawBigBlockFoldOut(W9ParticleShaderFlags.foldOutBitBaseOption, 3, GetAnimBoolIndex(3), "基本全局功能",
               () => DrawBaseOptions(),false);
            _helper.DrawBigBlockFoldOut(W9ParticleShaderFlags.foldOutBitMainTexOption, 3, GetAnimBoolIndex(3), "主贴图功能",
               () => DrawMainTexOptions());

            if (_uiEffectEnabled == 0 || _helper.ResetTool.IsInitResetData)
            {
               _helper.DrawBigBlockFoldOut(W9ParticleShaderFlags.foldOutBit1LightOption, 4, GetAnimBoolIndex(4),
                   "光照功能", () => DrawLightOptions());
            }

            _helper.DrawBigBlockFoldOut(W9ParticleShaderFlags.foldOutBitFeatureOption, 3, GetAnimBoolIndex(3), "特别功能",
               () => DrawFeatureOptions());
            _helper.DrawBigBlockFoldOut(W9ParticleShaderFlags.foldOutBit1TaOption, 4, GetAnimBoolIndex(4), "TA调试",
               () => DrawTaOptions());

            EditorGUIUtility.labelWidth = 0f;

            if (mats.Count == 1)
            {
               if (_meshSourceMode == MeshSourceMode.Particle || _meshSourceMode == MeshSourceMode.UIParticle)
               {
                   DoVertexStreamsArea(mats[0], m_ParticleRenderersUsingThisMaterial, 0); //填充stream和stremList
                   mats[0].EnableKeyword("_CUSTOMDATA");
               }
               else
               {
                   mats[0].DisableKeyword("_CUSTOMDATA");
               }
            }

            if (EditorGUI.EndChangeCheck())
            {
               DoAfterDraw();

               //多选状态下同步ShaderFlag
               if (mats.Count > 1)
               {
                   for (int i = 1; i < mats.Count; i++)
                   {
                       mats[i].SetInteger(W9ParticleShaderFlags.foldOutFlagId,
                           mats[0].GetInteger(W9ParticleShaderFlags.foldOutFlagId));
                       mats[i].SetInteger(W9ParticleShaderFlags.foldOutFlagId1,
                           mats[0].GetInteger(W9ParticleShaderFlags.foldOutFlagId1));
                       mats[i].SetInteger(W9ParticleShaderFlags.foldOutFlagId2,
                           mats[0].GetInteger(W9ParticleShaderFlags.foldOutFlagId2));
                   }
               }
            }
        }

        int _uiEffectEnabled = -1; //0 false,1 true,-1 unKnow | MixedValue
        int _meshSourceModeIsParticle = -1; //Particle Or UI Particle;
        private int _useGraphicMainTex = -1; //UI Sprite/UI RawImage
        int _noiseEnabled = -1; //扭曲
        private int _pNoiseEnabled = -1;//程序化噪波

        private MeshSourceMode _meshSourceMode = MeshSourceMode.UnKnowOrMixed;
        private TransparentMode _transparentMode = TransparentMode.UnKnowOrMixed;

        public void DrawMeshOptions()
        {

            _helper.DrawPopUp("Mesh来源模式", "_MeshSourceMode", _meshSourceModeNames, drawBlock: modeProp =>
            {

                if (!modeProp.hasMixedValue)
                {
                    _meshSourceMode = (MeshSourceMode)modeProp.floatValue;
                }
                else
                {
                    _meshSourceMode = MeshSourceMode.UnKnowOrMixed;
                }

                for (int i = 0; i < mats.Count; i++)
                {
                    MeshSourceMode mode = (MeshSourceMode)mats[i].GetFloat("_MeshSourceMode");

                    int uiEffectEnabled;
                    int meshSourceModeIsParticle;
                    int useGraphicMainTex;
                    if (mode == MeshSourceMode.UIEffectRawImage || mode == MeshSourceMode.UIEffectSprite ||
                        mode == MeshSourceMode.UIEffectBaseMap || mode == MeshSourceMode.UIParticle)
                    {
                        uiEffectEnabled = 1;
                    }
                    else
                    {
                        uiEffectEnabled = 0;
                    }

                    if (mode == MeshSourceMode.Particle || mode == MeshSourceMode.UIParticle)
                    {
                        meshSourceModeIsParticle = 1;
                    }
                    else
                    {
                        meshSourceModeIsParticle = 0;
                    }

                    if (mode == MeshSourceMode.UIEffectSprite || mode == MeshSourceMode.UIEffectRawImage)
                    {
                        useGraphicMainTex = 1;
                    }
                    else
                    {
                        useGraphicMainTex = 0;
                    }

                    if (i == 0)
                    {
                        _uiEffectEnabled = uiEffectEnabled;
                        _meshSourceModeIsParticle = meshSourceModeIsParticle;
                        _useGraphicMainTex = useGraphicMainTex;
                    }
                    else
                    {
                        if (_uiEffectEnabled != uiEffectEnabled)
                        {
                            _uiEffectEnabled = -1;
                        }

                        if (_meshSourceModeIsParticle != meshSourceModeIsParticle)
                        {
                            _meshSourceModeIsParticle = -1;
                        }

                        if (_useGraphicMainTex != useGraphicMainTex)
                        {
                            _useGraphicMainTex = -1;
                        }
                    }
                }

                if (checkIsParicleSystem)
                {
                    if (_meshSourceModeIsParticle <= 0)
                    {
                        EditorGUILayout.HelpBox("检测到材质用在粒子系统上，和设置不匹配", MessageType.Error);
                    }
                }
                else
                {
                    //这个不能Log，因为在Project面板下打开是不知道在不在粒子系统里的。
                    // if (_meshSourceMode == MeshSourceMode.Particle)
                    // {
                    //     EditorGUILayout.HelpBox("检测到材质没有用在粒子系统上，和设置不匹配",MessageType.Error);
                    // }
                }
            });

            _helper.DrawPopUp("透明模式", "_TransparentMode", transparentModeNames, drawBlock: transModeProp =>
            {
                if (!transModeProp.hasMixedValue)
                {
                    _transparentMode = (TransparentMode)mats[0].GetFloat("_TransparentMode");
                    if (_transparentMode == TransparentMode.CutOff||_helper.ResetTool.IsInitResetData)
                    {
                        // matEditor.ShaderProperty(_helper.GetProperty("_Cutoff"), "裁剪位置");
                        _helper.DrawSlider("裁剪位置","_Cutoff");
                    }

                    if (_transparentMode == TransparentMode.Transparent||_helper.ResetTool.IsInitResetData)
                    {
                        _helper.DrawPopUp("混合模式", "_Blend", blendModeNames, drawBlock: blendProp =>
                        {
                            if (!blendProp.hasMixedValue)
                            {
                                BlendMode blendMode = (BlendMode)blendProp.floatValue;
                                if (blendMode == BlendMode.Premultiply || blendMode == BlendMode.Additive||_helper.ResetTool.IsInitResetData)
                                {
                                    _helper.DrawSlider("叠加到预乘混合","_AdditiveToPreMultiplyAlphaLerp",0,1);
                                }
                            }

                        },drawOnValueChangedBlock: blendProp => {
                            if (!_helper.ResetTool.IsInitResetData)
                            {
                                BlendMode blendMode = (BlendMode)blendProp.floatValue;
                                MaterialProperty addToPreMultiplyAlphaLerpProp =
                                    _helper.GetProperty("_AdditiveToPreMultiplyAlphaLerp");
                                if (blendMode == BlendMode.Premultiply)
                                {
                                    addToPreMultiplyAlphaLerpProp.floatValue = 1;
                                }
                                else if (blendMode == BlendMode.Additive)
                                {
                                    addToPreMultiplyAlphaLerpProp.floatValue = 0;
                                }
                            }
                        });
                    }
                }
                else
                {
                    _transparentMode = TransparentMode.UnKnowOrMixed;
                }

            });
        }

        public void DrawBaseOptions()
        {
            _helper.DrawFloat("整体颜色强度", "_BaseColorIntensityForTimeline");
            _helper.DrawSlider("整体透明度", "_AlphaAll", rangePropertyName:"AlphaAllRangeVec");
            _helper.DrawFoldOut(W9ParticleShaderFlags.foldOutBit2ColorAdjustment, 5,
                GetAnimBoolIndex(5),"颜色调整",drawBlock: () =>
                {
                    _helper.DrawToggle("颜色调整仅影响主贴图","_ColorAdjustmentOnlyAffectMainTex",W9ParticleShaderFlags.FLAG_BIT_PARTICLE_COLOR_ADJUSTMENT_ONLY_AFFECT_MAINTEX);
                    _helper.DrawToggleFoldOut(W9ParticleShaderFlags.foldOutBitHueShift, 3, GetAnimBoolIndex(3), "色相偏移",
                    "_HueShift_Toggle", W9ParticleShaderFlags.FLAG_BIT_HUESHIFT_ON, isIndentBlock: true,
                    drawBlock: (isToggle) =>
                    {
                        _helper.DrawSlider("色相", "_HueShift", 0, 1);
                        DrawCustomDataSelect("色相自定义曲线", W9ParticleShaderFlags.FLAGBIT_POS_0_CUSTOMDATA_HUESHIFT, 0);
                    });


                _helper.DrawToggleFoldOut(W9ParticleShaderFlags.foldOutBitSaturability, 3, GetAnimBoolIndex(3),
                    "饱和度", "_ChangeSaturability_Toggle", W9ParticleShaderFlags.FLAG_BIT_SATURABILITY_ON,
                    isIndentBlock: true, drawBlock: (isToggle) =>
                    {
                        _helper.DrawSlider("饱和度", "_Saturability", rangePropertyName:"SaturabilityRangeVec");
                        DrawCustomDataSelect("饱和度强度自定义曲线", W9ParticleShaderFlags.FLAGBIT_POS_1_CUSTOMDATA_SATURATE, 1);
                    });

                _helper.DrawToggleFoldOut(W9ParticleShaderFlags.foldOutBit1MianTexContrast, 4, GetAnimBoolIndex(4),
                    "对比度", "_Contrast_Toggle", W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_MAINTEX_CONTRAST, 1,
                    isIndentBlock: true, drawBlock: (isToggle) =>
                    {
                        matEditor.ShaderProperty(_helper.GetProperty("_ContrastMidColor"), "对比度中值颜色");
                        _helper.DrawSlider("对比度", "_Contrast", 0, 5);
                        DrawCustomDataSelect("对比度自定义曲线",
                            W9ParticleShaderFlags.FLAGBIT_POS_2_CUSTOMDATA_MAINTEX_CONTRAST, 2);
                    });

                _helper.DrawToggleFoldOut(W9ParticleShaderFlags.foldOutBit1MainTexColorRefine, 4, GetAnimBoolIndex(4),
                    "颜色修正", "_BaseMapColorRefine_Toggle",
                    W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_MAINTEX_COLOR_REFINE, 1, isIndentBlock: true, drawBlock:
                    (isToggle) =>
                    {
                        _helper.DrawVector4Component("A:主颜色相乘", "_BaseMapColorRefine", "x", false);
                        _helper.DrawVector4Component("B:主颜色Power", "_BaseMapColorRefine", "y", false);
                        _helper.DrawVector4Component("B:主颜色Power后相乘", "_BaseMapColorRefine", "z", false);
                        _helper.DrawVector4Component("A/B线性差值", "_BaseMapColorRefine", "w", true, 0f, 1f);
                    });
                });
            if (_uiEffectEnabled == 0)
            {
                _helper.DrawPopUp("深度测试", "_ZTest", Enum.GetNames(typeof(CompareFunction)));
            }
            else if (_uiEffectEnabled == 1)
            {
                _helper.GetProperty("_ZTest").floatValue = 4.0f; //UI层使用默认值LessEqual
            }

            // _helper.DrawPopUp("时间模式","_TimeMode",Enum.GetNames(typeof(TimeMode)));
            _helper.DrawPopUp("渲染面向", "_Cull", Enum.GetNames(typeof(RenderFace)));



            if (_uiEffectEnabled == 0)
            {

                if (_transparentMode == TransparentMode.Transparent)
                {
                    _helper.DrawToggle("预渲染反面", "_BackFirstPassToggle", drawBlock: (isToggle) =>
                    {
                        if (!isToggle.hasMixedValue)
                        {
                            bool isBackFirstPass = isToggle.floatValue > 0.5f;
                            for (int i = 0; i < mats.Count; i++)
                            {
                                mats[i].SetShaderPassEnabled("SRPDefaultUnlit", isToggle.floatValue > 0.5f);
                                if (isBackFirstPass)
                                {
                                    mats[i].SetFloat("_Cull", (float)RenderFace.Front);
                                }
                            }

                            if (isBackFirstPass)
                            {
                                EditorGUILayout.HelpBox("预渲染反面会导致打断动态合批，请谨慎使用。", MessageType.Warning);
                            }
                        }
                    });


                }
                _helper.DrawPopUp("深度写入强制控制", "_ForceZWriteToggle",_ForceZWriteToggleNames);


                
                _helper.DrawToggleFoldOut(W9ParticleShaderFlags.foldOutBit2BaseBackColor,5,GetAnimBoolIndex(5),"背面颜色", "_BaseBackColor_Toggle", W9ParticleShaderFlags.FLAG_BIT_PARTICLE_BACKCOLOR,
                    drawBlock:
                    (isToggle) =>
                    {
                        // matEditor.ColorProperty(_helper.GetProperty("_BaseBackColor"), "");
                        _helper.ColorProperty("背面颜色","_BaseBackColor");
                    });


            }

            if (_uiEffectEnabled == 0)
            {
                _helper.DrawToggleFoldOut(W9ParticleShaderFlags.foldOutBitDistanceFade, 3, GetAnimBoolIndex(3), "近距离透明",
                    "_DistanceFade_Toggle", W9ParticleShaderFlags.FLAG_BIT_PARTICLE_DISTANCEFADE_ON,
                    isIndentBlock: true, drawBlock: (isToggle) =>
                    {
                        _helper.DrawVector4In2Line("_Fade", "透明过度范围", true);
                    });
            }
            else
            {
                for (int i = 0; i < mats.Count; i++)
                {
                    shaderFlags[i].ClearFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_DISTANCEFADE_ON);
                }
            }

            if (_uiEffectEnabled == 0)
            {
                _helper.DrawToggleFoldOut(W9ParticleShaderFlags.foldOutBitSoftParticles, 3, GetAnimBoolIndex(3), "软粒子",
                    "_SoftParticlesEnabled", shaderKeyword: "_SOFTPARTICLES_ON", isIndentBlock: true, drawBlock:
                    (isToggle) =>
                    {
                        _helper.DrawVector4In2Line("_SoftParticleFadeParams", "远近裁剪面", true);
                    });



                _helper.DrawToggle("剔除主角色", "_StencilWithoutPlayerToggle", shaderKeyword: "_STENCIL_WITHOUT_PLAYER",
                    drawEndChangeCheck: isToggle =>
                    {
                        if (!isToggle.hasMixedValue)
                        {
                            for (int i = 0; i < mats.Count; i++)
                            {
                                if (isToggle.floatValue > 0.5f)
                                {
                                    StencilTestHelper.SetMaterialStencil(mats[i], "ParticleWithoutPlayer",
                                        _stencilValuesConfig, out int queue);
                                    mats[i].SetFloat(_isCustomedStencilPropID, 1.0f);
                                }
                                else
                                {
                                    StencilTestHelper.SetMaterialStencil(mats[i], _defaultStencilKey,
                                        _stencilValuesConfig, out int queue);
                                    mats[i].SetFloat(_isCustomedStencilPropID, 0.0f);
                                }
                            }
                        }
                    });
                _helper.DrawToggle("忽略顶点色", "_IgnoreVetexColor_Toggle",
                    W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_IGNORE_VERTEX_COLOR, flagIndex: 1);
                _helper.DrawSlider("雾影响强度", "_fogintensity", 0f, 1f);
            }
            else if (_uiEffectEnabled == 1)
            {
                _helper.GetProperty("_fogintensity").floatValue = 0;
            }
        }

        public void DrawMainTexOptions()
        {
            Action drawAfterMainTex = () =>
            {
                DrawColorChannelSelect("主贴图透明度通道", W9ParticleShaderFlags.FLAG_BIT_COLOR_CHANNEL_POS_0_MAINTEX_ALPHA,3);
                if (_meshSourceMode != MeshSourceMode.UIEffectSprite)
                {
                    MaterialProperty textureProp = null;
                    if (_meshSourceMode == MeshSourceMode.UIEffectRawImage)
                    {
                        textureProp = _helper.GetProperty("_MainTex");
                    }
                    else
                    {
                        textureProp = _helper.GetProperty("_BaseMap");
                    }

                    DrawUVModeSelect(W9ParticleShaderFlags.foldOutBit1UVModeMainTex, 4, "主贴图UV来源",
                        W9ParticleShaderFlags.FLAG_BIT_UVMODE_POS_0_MAINTEX, 0, textureProp: textureProp);
                }

                DrawCustomDataSelect("主贴图X轴偏移自定义曲线", W9ParticleShaderFlags.FLAGBIT_POS_0_CUSTOMDATA_MAINTEX_OFFSET_X,
                    0);
                DrawCustomDataSelect("主贴图Y轴偏移自定义曲线", W9ParticleShaderFlags.FLAGBIT_POS_0_CUSTOMDATA_MAINTEX_OFFSET_Y,
                    0);

                if (_meshSourceMode != MeshSourceMode.UIEffectSprite)
                {

                    _helper.DrawVector4In2Line("_BaseMapMaskMapOffset", "偏移速度", true);
                    _helper.DrawSlider("主贴图旋转", "_BaseMapUVRotation", 0f, 360f);
                    _helper.DrawFloat("主贴图旋转速度","_BaseMapUVRotationSpeed");
                }

                DrawNoiseAffectBlock(() =>
                {
                    _helper.DrawSlider("主贴图扭曲强度控制", "_TexDistortion_intensity",rangePropertyName: "TexDistortionintensityRangeVec");

                });


          
            };

            Action drawBaseMapFoldOut = () => { 
                _helper.DrawTextureFoldOut(W9ParticleShaderFlags.foldOutBitBaseMap, 3, GetAnimBoolIndex(3), "主贴图",
                "_BaseMap", "_BaseColor", drawWrapMode: true,
                flagBitsName: W9ParticleShaderFlags.FLAG_BIT_WRAPMODE_BASEMAP, flagIndex: 2, drawBlock:
                theBaseMap =>
                {
                    drawAfterMainTex();
                });};
            Action drawUITextureModify = () =>
            {
                _helper.ColorProperty( "贴图颜色叠加","_Color");
                _helper.DrawVector4In2Line("_UI_MainTex_ST", "Tilling", true);
                _helper.DrawVector4In2Line("_UI_MainTex_ST", "Offset", false);
                drawAfterMainTex();
            };

            if (_helper.ResetTool.IsInitResetData)
            {
                drawBaseMapFoldOut();
                drawUITextureModify();
            }
            else
            {
                if (_useGraphicMainTex <= 0)
                {
                    drawBaseMapFoldOut();
                    // _helper.DrawTexture("主贴图","_BaseMap","_BaseColor",drawWrapMode:true,flagBitsName:W9ParticleShaderFlags.FLAG_BIT_WRAPMODE_BASEMAP,flagIndex:2);
                }
                else
                {
                    drawUITextureModify();
                    //实际上贴图来自_MainTex
                }
            }


        }
        
        private MaterialProperty[] rampColorPropArr = new MaterialProperty[6];
        private MaterialProperty[] rampColorAlphaPropArr = new MaterialProperty[3];

        private MaterialProperty[] maskMapGradientPropArr = new MaterialProperty[3];
        private MaterialProperty[] maskMap2GradientPropArr = new MaterialProperty[3];
        private MaterialProperty[] maskMap3GradientPropArr = new MaterialProperty[3];
        
        private MaterialProperty[] dissolveRampColorPropArr = new MaterialProperty[6];
        private MaterialProperty[] dissolveRampAlphaPropArr = new MaterialProperty[3];

        private FxLightMode _fxLightMode;

        public void DrawLightOptions()
        {
            _helper.DrawPopUp("光照类型", "_FxLightMode", _fxLightModeNames, drawBlock: mode =>
            {
                if (!mode.hasMixedValue)
                {
                    _fxLightMode = (FxLightMode)mode.floatValue;
                    if (_fxLightMode == FxLightMode.BlinnPhong || _fxLightMode == FxLightMode.PBR || _fxLightMode == FxLightMode.HalfLambert||_helper.ResetTool.IsInitResetData)
                    {
                        if (_fxLightMode == FxLightMode.BlinnPhong || _fxLightMode == FxLightMode.HalfLambert||_helper.ResetTool.IsInitResetData)
                        {
                            _helper.DrawToggle("高光开关", "_BlinnPhongSpecularToggle", shaderKeyword: "_SPECULAR_COLOR",
                                drawBlock:
                                isToggle =>
                                {
                                    if ((!isToggle.hasMixedValue && isToggle.floatValue > 0.5f)||_helper.ResetTool.IsInitResetData)
                                    {
                                        _helper.ColorProperty("高光颜色","_SpecularColor");
                                        _helper.DrawVector4Component("光滑度", "_MaterialInfo", "y", true, 0, 1);

                                    }
                                });
                        }

                        if (_fxLightMode == FxLightMode.PBR||_helper.ResetTool.IsInitResetData)
                        {
                            _helper.DrawVector4Component("金属度", "_MaterialInfo", "x", true, 0, 1);
                            _helper.DrawVector4Component("光滑度", "_MaterialInfo", "y", true, 0, 1);
                        }
                    }
                    if (_fxLightMode == FxLightMode.SixWay||_helper.ResetTool.IsInitResetData)
                    {
                        _helper.DrawTexture("六路正方向图(P)", "_RigRTBk", drawScaleOffset: false);
                        _helper.DrawTexture("六路反方向图(N)", "_RigLBtF", drawScaleOffset: false);

                        EditorGUILayout.HelpBox("六路UV跟随主贴图UV及颜色", MessageType.Warning);

                        _helper.DrawToggle("光照颜色吸收", "_SixWayColorAbsorptionToggle",
                            shaderKeyword: "VFX_SIX_WAY_ABSORPTION", drawBlock:
                            isAbsorption =>
                            {
                                _helper.DrawVector4Component("六路吸收强度", "_SixWayInfo", "x", true, 0, 1);
                            });

                        _helper.DrawTexture("六路自发光Ramp", "_SixWayEmissionRamp", drawScaleOffset: false,
                            drawBlock: rampMap =>
                            {
                                if (!rampMap.hasMixedValue)
                                {
                                    for (int i = 0; i < shaderFlags.Count; i++)
                                    {
                                        if (rampMap.textureValue)
                                        {
                                            shaderFlags[i]
                                                .SetFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_SIXWAY_RAMPMAP,
                                                    index: 1);
                                        }
                                        else
                                        {
                                            shaderFlags[i]
                                                .ClearFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_SIXWAY_RAMPMAP,
                                                    index: 1);
                                        }
                                    }
                                }
                            });
                        _helper.DrawVector4Component("六路自发光Pow", "_SixWayInfo", "y", false);
                        _helper.ColorProperty("六路自发光颜色","_SixWayEmissionColor");
                    }
                }
                else
                {
                    _fxLightMode = FxLightMode.UnKnownOrMixedValue;
                }

            });
           

            if (_fxLightMode != FxLightMode.SixWay||_helper.ResetTool.IsInitResetData)
            {
                //--------------法线-----------------
                _helper.DrawToggleFoldOut(W9ParticleShaderFlags.foldOutBit2BumpTexToggle, 5, GetAnimBoolIndex(5),
                    "法线贴图开关", "_BumpMapToggle", shaderKeyword: "_NORMALMAP", drawBlock: isBumpMapToggle =>
                    {
                        MaterialProperty bumpTexFollowMainTexUVToggle = _helper.GetProperty("_BumpTexFollowMainTexUVToggle");
                        _helper.DrawTextureFoldOut(W9ParticleShaderFlags.foldOutBit1BumpTex, 4, GetAnimBoolIndex(4),
                            "法线贴图", "_BumpTex",
                            flagBitsName: W9ParticleShaderFlags.FLAG_BIT_WRAPMODE_BUMPTEX, 
                            drawBlock: theBumpmap =>
                            {
                               
                                DrawUVModeSelect(W9ParticleShaderFlags.foldOutBit1UVModeBumpTex, 4, "法线贴图UV来源", W9ParticleShaderFlags.FLAG_BIT_UVMODE_POS_0_BUMPMAP, 0, theBumpmap);

                                //在DoAfterDraw会执行SetKeyword的逻辑。
                                _helper.DrawToggle("法线贴图多通道模式", "_BumpMapMaskMode", W9ParticleShaderFlags.FLAG_BIT_PARTICLE_NORMALMAP_MASK_MODE);
                                _helper.DrawSlider("法线强度", "_BumpScale", rangePropertyName:"BumpScaleRangeVec");
                            });
                    });
                _helper.DrawToggleFoldOut(W9ParticleShaderFlags.foldOutBit2MatCapToggle, 5, GetAnimBoolIndex(5),
                    "MatCap模拟材质", "_MatCapToggle", shaderKeyword: "_MATCAP", drawBlock: isMatCapToggle =>
                    {
                        _helper.DrawTexture("MatCap图", "_MatCapTex",colorPropertyName:"_MatCapColor", drawScaleOffset: false);
                        // matEditor.ColorProperty(_helper.GetProperty("_MatCapColor"), "MatCap颜色");
                        _helper.DrawVector4Component("MatCap相加到相乘过渡", "_MatCapInfo", "x", true);
                    });
              
            }
            else if (_fxLightMode == FxLightMode.SixWay)
            {
                //这里应该关掉法线和Matcap的Keyword
            }
        }

        public void DrawFeatureOptions()
        {
            _helper.DrawToggleFoldOut(W9ParticleShaderFlags.foldOutBitMask, 3, GetAnimBoolIndex(3), "遮罩",
                "_Mask_Toggle", shaderKeyword: "_MASKMAP_ON", fontStyle: FontStyle.Bold, drawBlock: (isToggle) =>
                {
                    _helper.DrawToggle("测试遮罩颜色","_NB_Debug_Mask",shaderKeyword:"NB_DEBUG_MASK");
                    _helper.DrawVector4Component("遮罩强度", "_MaskMapVec", "x", true);
                    _helper.DrawToggleFoldOut(W9ParticleShaderFlags.foldOutBit2MaskRefine,5,GetAnimBoolIndex(5),"遮罩整体调整","_MaskRefineToggle",W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_MASK_REFINE,1,drawBlock:
                        maskRefineProp =>
                        {
                            _helper.DrawVector4Component("范围(Pow)","_MaskRefineVec","x",false);
                            _helper.DrawVector4Component("相乘","_MaskRefineVec","y",false);
                            _helper.DrawVector4Component("偏移(相加)","_MaskRefineVec","z",false);
                        });
                    DrawPNoiseBlendModeSelect("遮罩",W9ParticleShaderFlags.FLAG_BIT_PNOISE_BLEND_POS_0_MASK,"_MaskPNoiseBlendOpacity");
                    _helper.DrawPopUp("遮罩模式", "_MaskMapGradientToggle", _maskMapModeNames,
                        drawBlock: maskMapModeProp =>
                        {
                            Action drawMaskTexturePart = () =>
                            {
                                _helper.DrawTextureFoldOut(W9ParticleShaderFlags.foldOutBitMaskMap, 3,
                                    GetAnimBoolIndex(3), "遮罩", "_MaskMap", drawWrapMode: true,
                                    flagBitsName: W9ParticleShaderFlags.FLAG_BIT_WRAPMODE_MASKMAP, flagIndex: 2,
                                    drawBlock: theMaskMap =>
                                    {
                                        DrawColorChannelSelect("遮罩通道选择", W9ParticleShaderFlags.FLAG_BIT_COLOR_CHANNEL_POS_0_MASKMAP1);
                                    });
                            };
                            Action drawMaskGradientPart = () =>
                            {
                                maskMapGradientPropArr[0] = _helper.GetProperty("_MaskMapGradientFloat0");
                                maskMapGradientPropArr[1] = _helper.GetProperty("_MaskMapGradientFloat1");
                                maskMapGradientPropArr[2] = _helper.GetProperty("_MaskMapGradientFloat2");
                                _helper.DrawGradient(false, ColorSpace.Gamma, "遮罩渐变", 6,
                                    "_MaskMapGradientCount", alphaProperties: maskMapGradientPropArr);
                                _helper.TextureScaleOffsetProperty("_MaskMap");
                                _helper.DrawWrapMode("遮罩", W9ParticleShaderFlags.FLAG_BIT_WRAPMODE_MASKMAP,
                                    flagIndex: 2);
                            };
                            if (_helper.ResetTool.IsInitResetData)
                            {
                                drawMaskTexturePart();
                                drawMaskGradientPart();
                            }
                            else
                            {
                                if (!maskMapModeProp.hasMixedValue)
                                {
                                    //绘制贴图
                                    if (maskMapModeProp.floatValue < 0.5f)
                                    {
                                        drawMaskTexturePart();
                                    }
                                    else
                                    {
                                        drawMaskGradientPart();
                                    }
                                }
                            }

                     

                        }, drawOnValueChangedBlock: maskMapModeProp =>
                        {
                            for (int i = 0; i < shaderFlags.Count; i++)
                            {
                                if (maskMapModeProp.floatValue < 0.5f)
                                {
                                    shaderFlags[i]
                                        .ClearFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_MASKMAP_GRADIENT,
                                            index: 1);
                                }
                                else
                                {
                                    shaderFlags[i]
                                        .SetFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_MASKMAP_GRADIENT,
                                            index: 1);
                                }
                            }
                        });
                    
                        DrawUVModeSelect(W9ParticleShaderFlags.foldOutBit1UVModeMaskMap, 4, "遮罩UV来源",
                            W9ParticleShaderFlags.FLAG_BIT_UVMODE_POS_0_MASKMAP, 0);
                        DrawCustomDataSelect("Mask图X轴偏移自定义曲线",
                            W9ParticleShaderFlags.FLAGBIT_POS_0_CUSTOMDATA_MASK_OFFSET_X, 0);
                        DrawCustomDataSelect("Mask图Y轴偏移自定义曲线",
                            W9ParticleShaderFlags.FLAGBIT_POS_0_CUSTOMDATA_MASK_OFFSET_Y, 0);
                        _helper.DrawVector4In2Line("_MaskMapOffsetAnition", "遮罩偏移速度", true);
                        _helper.DrawFloat("遮罩旋转", "_MaskMapUVRotation");
                        _helper.DrawToggleFoldOut(W9ParticleShaderFlags.foldOutBitMaskRotate, 3,
                            GetAnimBoolIndex(3), "遮罩旋转速度", "_Mask_RotationToggle", W9ParticleShaderFlags
                                .FLAG_BIT_PARTILCE_MASKMAPROTATIONANIMATION_ON, isIndentBlock: false,
                            drawBlock: (isToggle2) =>
                            {
                                _helper.DrawFloat("旋转速度", "_MaskMapRotationSpeed");
                            });

                        DrawNoiseAffectBlock(() =>
                        {
                            _helper.DrawSlider("遮罩扭曲强度", "_MaskDistortion_intensity", rangePropertyName:"MaskDistortionIntensityRangeVec");
                        });
                        
                       
                        

                    _helper.DrawToggleFoldOut(W9ParticleShaderFlags.foldOutBitMask2, 3, GetAnimBoolIndex(3), "遮罩2",
                        "_Mask2_Toggle", flagBitsName: W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_MASK_MAP2,
                        flagIndex: 1, isIndentBlock: true, drawBlock:
                        (isToggle) =>
                        {
                            _helper.DrawPopUp("遮罩2模式", "_MaskMap2GradientToggle", _maskMapModeNames,
                                drawBlock: maskMap2GradientModeProp =>
                                {
                                    Action drawMask2TexturePart = () =>
                                    {
                                        _helper.DrawTexture("遮罩2贴图", "_MaskMap2", drawWrapMode: true,
                                            wrapModeFlagBitsName: W9ParticleShaderFlags.FLAG_BIT_WRAPMODE_MASKMAP2,
                                            flagIndex: 2,
                                            drawBlock: theMaskMap2Texture =>
                                            {
                                                DrawColorChannelSelect("遮罩2通道选择",
                                                    W9ParticleShaderFlags.FLAG_BIT_COLOR_CHANNEL_POS_0_MASKMAP2);
                                            });
                                    };
                                    Action drawMask2GradientPart = () =>
                                    {
                                        maskMap2GradientPropArr[0] = _helper.GetProperty("_MaskMap2GradientFloat0");
                                        maskMap2GradientPropArr[1] = _helper.GetProperty("_MaskMap2GradientFloat1");
                                        maskMap2GradientPropArr[2] = _helper.GetProperty("_MaskMap2GradientFloat2");
                                        _helper.DrawGradient(false, ColorSpace.Gamma,
                                            "遮罩2渐变(UV纵向)", 6, "_MaskMap2GradientCount",
                                            alphaProperties: maskMap2GradientPropArr);
                                        matEditor.TextureScaleOffsetProperty(_helper.GetProperty("_MaskMap2"));
                                        _helper.DrawWrapMode("遮罩2UV",
                                            W9ParticleShaderFlags.FLAG_BIT_WRAPMODE_MASKMAP2, flagIndex: 2);
                                    };
                                    if (_helper.ResetTool.IsInitResetData)
                                    {
                                        drawMask2TexturePart();
                                        drawMask2GradientPart();
                                    }
                                    else
                                    {
                                        if (!maskMap2GradientModeProp.hasMixedValue)
                                        {
                                            if (maskMap2GradientModeProp.floatValue < 0.5f)
                                            {
                                                drawMask2TexturePart();
                                            }
                                            else
                                            {
                                                drawMask2GradientPart();
                                            }
                                        }
                                    }

                                    DrawUVModeSelect(W9ParticleShaderFlags.foldOutBit1UVModeMaskMap2, 4, "遮罩2UV来源",
                                        W9ParticleShaderFlags.FLAG_BIT_UVMODE_POS_0_MASKMAP_2, 0);
                                    _helper.DrawVector4Component("遮罩2旋转","_MaskMapVec","y",false);
                                    _helper.DrawVector4In2Line("_MaskMapOffsetAnition", "遮罩2偏移速度", false);
                                }, drawOnValueChangedBlock: maskMap2GradientModeProp =>
                                {
                                    for (int i = 0; i < shaderFlags.Count; i++)
                                    {
                                        if (maskMap2GradientModeProp.floatValue < 0.5f)
                                        {
                                            shaderFlags[i]
                                                .ClearFlagBits(
                                                    W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_MASKMAP_2_GRADIENT,
                                                    index: 1);
                                        }
                                        else
                                        {
                                            shaderFlags[i]
                                                .SetFlagBits(
                                                    W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_MASKMAP_2_GRADIENT,
                                                    index: 1);
                                        }
                                    }

                                });

                        });
                    _helper.DrawToggleFoldOut(W9ParticleShaderFlags.foldOutBitMask3, 3, GetAnimBoolIndex(3), "遮罩3",
                        "_Mask3_Toggle", flagBitsName: W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_MASK_MAP3,
                        flagIndex: 1, isIndentBlock: true, drawBlock:
                        (isToggle) =>
                        {
                            _helper.DrawPopUp("遮罩3模式", "_MaskMap3GradientToggle", _maskMapModeNames,
                                drawBlock: maskMap3GradientModeProp =>
                                {
                                    Action drawMask3TexturePart = () =>
                                    {
                                        _helper.DrawTexture("遮罩3贴图", "_MaskMap3", drawWrapMode: true,
                                            wrapModeFlagBitsName: W9ParticleShaderFlags.FLAG_BIT_WRAPMODE_MASKMAP3,
                                            flagIndex: 2,
                                            drawBlock: theMaskMap2Texture =>
                                            {
                                                DrawColorChannelSelect("遮罩3通道选择",
                                                    W9ParticleShaderFlags.FLAG_BIT_COLOR_CHANNEL_POS_0_MASKMAP3);
                                            });
                                    };
                                    Action drawMask3GradientPart = () =>
                                    {
                                        maskMap3GradientPropArr[0] = _helper.GetProperty("_MaskMap3GradientFloat0");
                                        maskMap3GradientPropArr[1] = _helper.GetProperty("_MaskMap3GradientFloat1");
                                        maskMap3GradientPropArr[2] = _helper.GetProperty("_MaskMap3GradientFloat2");
                                        _helper.DrawGradient(false, ColorSpace.Gamma,
                                            "遮罩3渐变(UV横向)", 6, "_MaskMap3GradientCount",
                                            alphaProperties: maskMap3GradientPropArr);
                                        matEditor.TextureScaleOffsetProperty(_helper.GetProperty("_MaskMap3"));
                                        _helper.DrawWrapMode("遮罩3UV",
                                            W9ParticleShaderFlags.FLAG_BIT_WRAPMODE_MASKMAP3, flagIndex: 2);
                                    };
                                    if (_helper.ResetTool.IsInitResetData)
                                    {
                                        drawMask3TexturePart();
                                        drawMask3GradientPart();
                                    }
                                    else
                                    {
                                        if (!maskMap3GradientModeProp.hasMixedValue)
                                        {
                                            if (maskMap3GradientModeProp.floatValue < 0.5f)
                                            {
                                                drawMask3TexturePart();
                                            }
                                            else
                                            {
                                                drawMask3GradientPart();
                                            }
                                        }
                                    }

                                    DrawUVModeSelect(W9ParticleShaderFlags.foldOutBit1UVModeMaskMap3, 4, "遮罩3UV来源",
                                        W9ParticleShaderFlags.FLAG_BIT_UVMODE_POS_0_MASKMAP_3, 0);
                                    _helper.DrawVector4Component("遮罩3旋转","_MaskMapVec","z",false);
                                    
                                    _helper.DrawVector4In2Line("_MaskMap3OffsetAnition", "遮罩3偏移速度", true);
                                }, drawOnValueChangedBlock: maskMap3GradientModeProp =>
                                {
                                    for (int i = 0; i < shaderFlags.Count; i++)
                                    {
                                        if (maskMap3GradientModeProp.floatValue < 0.5f)
                                        {
                                            shaderFlags[i]
                                                .ClearFlagBits(
                                                    W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_MASKMAP_3_GRADIENT,
                                                    index: 1);
                                        }
                                        else
                                        {
                                            shaderFlags[i]
                                                .SetFlagBits(
                                                    W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_MASKMAP_3_GRADIENT,
                                                    index: 1);
                                        }
                                    }

                                });
                        });
                });


            MaterialProperty noiseToggleProperty = _helper.GetProperty("_noisemapEnabled");
            if (noiseToggleProperty.hasMixedValue)
            {
                _noiseEnabled = -1;
            }
            else
            {
                _noiseEnabled = noiseToggleProperty.floatValue > 0.5f ? 1 : 0;
            }
            _helper.DrawToggleFoldOut(W9ParticleShaderFlags.foldOutBitNoise, 3, GetAnimBoolIndex(3), "扭曲",
                "_noisemapEnabled", shaderKeyword: "_NOISEMAP", fontStyle: FontStyle.Bold,
                drawBlock: (isToggle) =>
                {
                    _helper.DrawToggle("扭曲强度值测试","_NB_Debug_Distort",shaderKeyword:"NB_DEBUG_DISTORT");
                    _helper.DrawSlider("整体扭曲强度", "_NoiseIntensity", rangePropertyName:"_NoiseIntensityRangeVec");
                    DrawCustomDataSelect("扭曲强度自定义曲线", W9ParticleShaderFlags.FLAGBIT_POS_1_CUSTOMDATA_NOISE_INTENSITY, 1);
                    if (!(_uiEffectEnabled>0.5f) || _helper.ResetTool.IsInitResetData)
                    {
                        _helper.DrawPopUp("屏幕扰动模式","_ScreenDistortModeToggle",screenDistortModeNames,drawBlock:
                        scrDistortModeProp =>
                        {
                            if (scrDistortModeProp.floatValue > 0.5f || _helper.ResetTool.IsInitResetData)
                            {
                                EditorGUI.indentLevel++;
                                _helper.DrawSlider("屏幕扭曲强度", "_ScreenDistortIntensity", rangePropertyName:"_ScreenDistortIntensityRangeVec");
                                _helper.DrawToggle("关闭主材质Pass","_DisableMainPassToggle",drawEndChangeCheck:
                                    disableMainPassToggleProp =>
                                    {
                                        for (int i = 0; i < mats.Count; i++)
                                        {
                                            mats[i].SetShaderPassEnabled("UniversalForward",disableMainPassToggleProp.floatValue < 0.5f);
                                        }
                                    });
                                _helper.DrawToggleFoldOut(W9ParticleShaderFlags.foldOutBit2ScreenDistortAlphaRefine,5,GetAnimBoolIndex(5),"屏幕扭曲Alpha整体调整","_ScreenDistortAlphaRefineToggle",W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_SCREEN_DISTORT_ALPHA_REFINE,1,drawBlock:
                                    AlphaRefineProp =>
                                    {
                                        _helper.DrawFloat("范围(Pow)", "_ScreenDistortAlphaPow");
                                        _helper.DrawFloat("相乘", "_ScreenDistortAlphaMulti");
                                        _helper.DrawFloat("偏移(相加)", "_ScreenDistortAlphaAdd");
                                    });
                             
                                EditorGUI.indentLevel--;
                            }
                            
                        },drawOnValueChangedBlock:
                        screenDistortModeProp =>
                        {
                            ScreenDistortMode screenDistortMode = (ScreenDistortMode)screenDistortModeProp.floatValue;
                            for (int i = 0; i < mats.Count; i++)
                            {
                                switch (screenDistortMode)
                                {
                                    case ScreenDistortMode.None:
                                        mats[i].SetShaderPassEnabled("NBCameraOpaqueDistortPass", false);
                                        mats[i].SetShaderPassEnabled("NBDeferredDistortPass", false);
                                        //打开主材质Pass
                                        mats[i].SetShaderPassEnabled("UniversalForward",true);
                                        if (i == 0) _helper.GetProperty("_DisableMainPassToggle").floatValue = 0;
                                        
                                        break;
                                    case ScreenDistortMode.CameraOpaqueDistort:
                                        mats[i].SetShaderPassEnabled("NBCameraOpaqueDistortPass", true);
                                        mats[i].SetShaderPassEnabled("NBDeferredDistortPass", false);
                                        break;
                                    case ScreenDistortMode.DeferredDistort:
                                        mats[i].SetShaderPassEnabled("NBCameraOpaqueDistortPass", false);
                                        mats[i].SetShaderPassEnabled("NBDeferredDistortPass", true);
                                        break;
                                    //在OnvalueChange后不会有Mixed的情况
                                }
                            }
                        });
                    }
               

                    _helper.DrawPopUp("扭曲模式","_DistortMode",distortModeNames,drawBlock: modeProp =>
                    {
                        if((!modeProp.hasMixedValue && Mathf.Approximately(modeProp.floatValue,0))||_helper.ResetTool.IsInitResetData)
                        {
                            EditorGUILayout.LabelField("扭曲贴图RG双通道则为FlowMap,FlowMap贴图设置应该去掉sRGB勾选");
                            _helper.DrawTextureFoldOut(W9ParticleShaderFlags.foldOutBitNoiseMap, 3, GetAnimBoolIndex(3), "扭曲贴图",
                                "_NoiseMap", drawWrapMode: true, flagBitsName: W9ParticleShaderFlags.FLAG_BIT_WRAPMODE_NOISEMAP,
                                flagIndex: 2, drawBlock:
                                theNoiseMap =>
                                {
                                    DrawUVModeSelect(W9ParticleShaderFlags.foldOutBit1UVModeNoiseMap, 4, "扭曲贴图UV来源",
                                        W9ParticleShaderFlags.FLAG_BIT_UVMODE_POS_0_NOISE_MAP, 0, theNoiseMap);
              
                                    _helper.DrawVector4In2Line("_DistortionDirection", "扭曲方向强度", true);
                                    DrawCustomDataSelect("扭曲方向强度X自定义曲线",
                                        W9ParticleShaderFlags.FLAGBIT_POS_2_CUSTOMDATA_NOISE_DIRECTION_X, 2);
                                    DrawCustomDataSelect("扭曲方向强度Y自定义曲线",
                                        W9ParticleShaderFlags.FLAGBIT_POS_2_CUSTOMDATA_NOISE_DIRECTION_Y, 2);

                                    _helper.DrawSlider("扭曲旋转", "_NoiseMapUVRotation", 0f, 360f);
                                    _helper.DrawVector4In2Line("_NoiseOffset", "扭曲偏移速度", true);
                                    _helper.DrawToggle("0.5为中值，双向扭曲", "_DistortionBothDirection_Toggle",
                                        flagBitsName: W9ParticleShaderFlags.FLAG_BIT_PARTICLE_NOISEMAP_NORMALIZEED_ON,
                                        isIndentBlock: false);
                                });
                        }
                        
                        if((!modeProp.hasMixedValue && Mathf.Approximately(modeProp.floatValue,1))||_helper.ResetTool.IsInitResetData)
                        { 
                            _helper.DrawSlider("折射率", "_RefractionIOR", 0f, 5f);
                            
                        }
                        
                    },drawOnValueChangedBlock: modePropOnValueChanged =>
                    {
                        for (int i = 0; i < mats.Count; i++)
                        {
                            if (Mathf.Approximately(modePropOnValueChanged.floatValue, 1))
                            {
                                mats[i].EnableKeyword("_DISTORT_REFRACTION");
                            }
                            else
                            {
                                mats[i].DisableKeyword("_DISTORT_REFRACTION");
                            }
                        }
                    });
                    
                    DrawPNoiseBlendModeSelect("扭曲",W9ParticleShaderFlags.FLAG_BIT_PNOISE_BLEND_POS_0_DISTORT,"_DistortPNoiseBlendOpacity");

                  

                    _helper.DrawToggleFoldOut(W9ParticleShaderFlags.foldOutBitNoiseMaskToggle, 3, GetAnimBoolIndex(3),
                        "扭曲遮罩", "_noiseMaskMap_Toggle",
                        flagBitsName: W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_NOISE_MASKMAP, flagIndex: 1,
                        drawBlock: isNoiseMaskToggle =>
                        {
                            _helper.DrawTexture("扭曲遮罩贴图", "_NoiseMaskMap", drawWrapMode: true,
                                wrapModeFlagBitsName: W9ParticleShaderFlags.FLAG_BIT_WRAPMODE_NOISE_MASKMAP, drawBlock:
                                theNoiseMaskMap =>
                                {
                                    DrawColorChannelSelect("扭曲遮罩图通道选择",
                                        W9ParticleShaderFlags.FLAG_BIT_COLOR_CHANNEL_POS_0_NOISE_MASK);
                                    DrawUVModeSelect(W9ParticleShaderFlags.foldOutBit1UVModeNoiseMaskMap, 4,
                                        "扭曲遮罩贴图UV来源", W9ParticleShaderFlags.FLAG_BIT_UVMODE_POS_0_NOISE_MASK_MAP, 0,
                                        theNoiseMaskMap);
                                });
                        });
                });

            _helper.DrawToggleFoldOut(W9ParticleShaderFlags.foldOutBitDistortionChoraticaberrat, 3, GetAnimBoolIndex(3),
                "扭曲色散", "_Distortion_Choraticaberrat_Toggle", W9ParticleShaderFlags.FLAG_BIT_PARTICLE_CHORATICABERRAT,
                isIndentBlock: true, fontStyle: FontStyle.Bold, drawBlock:
                (is_Choraticaberrat_Toggle) =>
                {
                    DrawNoiseAffectBlock(() =>
                    {
                        _helper.DrawToggle("色散强度受扭曲强度影响", "_Distortion_Choraticaberrat_WithNoise_Toggle",
                            W9ParticleShaderFlags.FLAG_BIT_PARTICLE_NOISE_CHORATICABERRAT_WITH_NOISE);
                    });
                    _helper.DrawVector4Component("色散强度", "_DistortionDirection", "z", false);
                    DrawCustomDataSelect("色散强度自定义曲线",
                        W9ParticleShaderFlags.FLAGBIT_POS_0_CUSTOMDATA_CHORATICABERRAT_INTENSITY, 0);
                });

            _helper.DrawToggleFoldOut(W9ParticleShaderFlags.foldOutBitEmission, 3, GetAnimBoolIndex(3), "流光(颜色相加)",
                "_EmissionEnabled", shaderKeyword: "_EMISSION", isIndentBlock: true, fontStyle: FontStyle.Bold,
                drawBlock: (isToggle) =>
                {
                    _helper.DrawTexture("流光贴图", "_EmissionMap", "_EmissionMapColor",
                        wrapModeFlagBitsName: W9ParticleShaderFlags.FLAG_BIT_WRAPMODE_EMISSIONMAP, flagIndex: 2,
                        drawBlock: theEmissionMap =>
                        {
                            DrawUVModeSelect(W9ParticleShaderFlags.foldOutBit1UVModeEmissionMap, 4, "流光贴图UV来源", W9ParticleShaderFlags.FLAG_BIT_UVMODE_POS_0_EMISSION_MAP, 0, theEmissionMap);
                            _helper.DrawSlider("流光贴图旋转", "_EmissionMapUVRotation", 0f, 360f);
                            _helper.DrawVector4In2Line("_EmissionMapUVOffset", "流光贴图偏移速度", true);
                            DrawNoiseAffectBlock(() => { _helper.DrawSlider("流光贴图扭曲强度", "_Emi_Distortion_intensity",rangePropertyName:"EmiDistortionIntensityRangeVec"); });
                        
                        });
                    _helper.DrawFloat("流光颜色强度", "_EmissionMapColorIntensity");
                });

            _helper.DrawToggleFoldOut(W9ParticleShaderFlags.foldOutColorBlend, 3, GetAnimBoolIndex(3), "渐变(颜色相乘)",
                "_ColorBlendMap_Toggle", shaderKeyword: "_COLORMAPBLEND", isIndentBlock: true,
                fontStyle: FontStyle.Bold,
                drawBlock: (isToggle) =>
                {
                    _helper.DrawTexture("颜色渐变贴图", "_ColorBlendMap",colorPropertyName:"_ColorBlendColor" ,
                        wrapModeFlagBitsName: W9ParticleShaderFlags.FLAG_BIT_WRAPMODE_COLORBLENDMAP, flagIndex: 2,
                        drawBlock: texProp =>
                        {
                            DrawUVModeSelect(W9ParticleShaderFlags.foldOutBit1UVModeColorBlendMap, 4, "颜色渐变贴图UV来源", W9ParticleShaderFlags.FLAG_BIT_UVMODE_POS_0_COLOR_BLEND_MAP, 0, texProp);
                            _helper.DrawVector4Component("颜色渐变贴图旋转", "_ColorBlendVec", "w", true, 0f, 360f);
                            _helper.DrawVector4In2Line("_ColorBlendMapOffset", "颜色渐变贴图偏移速度", true);
                            DrawNoiseAffectBlock(() => { _helper.DrawVector4Component("颜色渐变扭曲强度","_ColorBlendVec","x",true,0f,1f); });
                        
                        });
                    _helper.DrawPopUp("颜色渐变图Alpha作用","_ColorBlendAlphaMultiplyMode",colorBlendAlphaMode,drawOnValueChangedBlock:
                        alphaModeProp =>
                        {
                            for (int i = 0; i < shaderFlags.Count; i++)
                            {
                                if (alphaModeProp.floatValue > 0.5)
                                {
                                    shaderFlags[i].SetFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_COLOR_BLEND_ALPHA_MULTIPLY_MODE);
                                }
                                else
                                {
                                    shaderFlags[i].ClearFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_COLOR_BLEND_ALPHA_MULTIPLY_MODE);
                                }
                                
                            }
                        });
                    _helper.DrawVector4Component("颜色渐变图Alpha强度","_ColorBlendVec","z",true,0f,1f);
                    
                });

            _helper.DrawToggleFoldOut(W9ParticleShaderFlags.foldOutBit2RampColor, 5, GetAnimBoolIndex(5), "颜色映射(Ramp)",
                "_RampColorToggle", shaderKeyword: "_COLOR_RAMP", isIndentBlock: true, fontStyle: FontStyle.Bold,
                drawBlock:
                isToggleProp =>
                {
                    _helper.DrawPopUp("Ramp来源模式", "_RampColorSourceMode", rampColorSourceMode,
                        drawBlock: modeProp =>
                        {
                            Action drawRampTexture = () =>
                            {
                                _helper.DrawTexture("颜色映射黑白图", "_RampColorMap", drawWrapMode: true,
                                    wrapModeFlagBitsName: W9ParticleShaderFlags.FLAG_BIT_WRAPMODE_RAMP_COLOR_MAP,
                                    flagIndex: 2,
                                    drawBlock: texProp =>
                                    {
                                        DrawColorChannelSelect("颜色映射黑白图通道选择",
                                            W9ParticleShaderFlags.FLAG_BIT_COLOR_CHANNEL_POS_0_RAMP_COLOR_MAP);
                                    }); 
                            };
                            Action drawNoRampTexture = () =>
                            {
                                _helper.TextureScaleOffsetProperty("_RampColorMap");
                                _helper.DrawWrapMode("颜色映射UV",
                                    W9ParticleShaderFlags.FLAG_BIT_WRAPMODE_RAMP_COLOR_MAP, 2);
                            };
                            if (_helper.ResetTool.IsInitResetData)
                            {
                                drawRampTexture();
                                drawNoRampTexture();
                            }
                            else
                            {
                                if (!modeProp.hasMixedValue)
                                {
                                    if (modeProp.floatValue >= 0.5f)
                                    {
                                        drawRampTexture();
                                    }
                                    else
                                    {
                                        drawNoRampTexture();
                                    }
                                }
                            }
                        }, drawOnValueChangedBlock: modeProp =>
                        {
                            for (int i = 0; i < shaderFlags.Count; i++)
                            {
                                if (modeProp.floatValue > 0.5f)
                                {
                                    shaderFlags[i]
                                        .SetFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_RAMP_COLOR_MAP_MODE_ON);
                                }
                                else
                                {
                                    shaderFlags[i]
                                        .ClearFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_RAMP_COLOR_MAP_MODE_ON);
                                }
                            }
                        });
                    DrawUVModeSelect(W9ParticleShaderFlags.foldOutBit1UVModeRampColorMap, 4, "颜色映射黑白图UV来源",
                        W9ParticleShaderFlags.FLAG_BIT_UVMODE_POS_0_RAMP_COLOR_MAP, 0,
                        _helper.GetProperty("_RampColorMap"),forceEnable:true);
                    _helper.DrawVector4In2Line("_RampColorMapOffset", "颜色映射贴图偏移速度", true);
                    _helper.DrawVector4Component("颜色映射贴图旋转", "_RampColorMapOffset", "w", true, 0f, 360f);
                    rampColorPropArr[0] = _helper.GetProperty("_RampColor0");
                    rampColorPropArr[1] = _helper.GetProperty("_RampColor1");
                    rampColorPropArr[2] = _helper.GetProperty("_RampColor2");
                    rampColorPropArr[3] = _helper.GetProperty("_RampColor3");
                    rampColorPropArr[4] = _helper.GetProperty("_RampColor4");
                    rampColorPropArr[5] = _helper.GetProperty("_RampColor5");
                    rampColorAlphaPropArr[0] = _helper.GetProperty("_RampColorAlpha0");
                    rampColorAlphaPropArr[1] = _helper.GetProperty("_RampColorAlpha1");
                    rampColorAlphaPropArr[2] = _helper.GetProperty("_RampColorAlpha2");
                    _helper.DrawGradient(true, ColorSpace.Gamma, "映射颜色", 6, "_RampColorCount",
                        rampColorPropArr, rampColorAlphaPropArr);
                    _helper.DrawPopUp("Ramp颜色混合模式", "_RampColorBlendMode", rampColorBlendMode, drawOnValueChangedBlock:
                        modeProp =>
                        {
                            for (int i = 0; i < shaderFlags.Count; i++)
                            {
                                if (modeProp.floatValue > 0.5f)
                                {
                                    shaderFlags[i].SetFlagBits(W9ParticleShaderFlags
                                        .FLAG_BIT_PARTICLE_RAMP_COLOR_BLEND_ADD);
                                }
                                else
                                {
                                    shaderFlags[i].ClearFlagBits(W9ParticleShaderFlags
                                        .FLAG_BIT_PARTICLE_RAMP_COLOR_BLEND_ADD);
                                }
                            }
                        });
                    matEditor.ShaderProperty(_helper.GetProperty("_RampColorBlendColor"), "颜色映射叠加颜色_hdr");


                });
            _helper.DrawToggleFoldOut(W9ParticleShaderFlags.foldOutDissolve, 3, GetAnimBoolIndex(3), "溶解",
                "_Dissolve_Toggle", shaderKeyword: "_DISSOLVE", isIndentBlock: true, fontStyle: FontStyle.Bold,
                drawBlock: (isToggle) =>
                {
                    
                    _helper.DrawToggle("溶解度黑白值测试", "_NB_Debug_Dissolve", shaderKeyword: "NB_DEBUG_DISSOLVE");
                    _helper.DrawTextureFoldOut(W9ParticleShaderFlags.foldOutDissolveMap, 3, GetAnimBoolIndex(3), "溶解贴图",
                        "_DissolveMap", drawScaleOffset: true, drawWrapMode: true,
                        flagBitsName: W9ParticleShaderFlags.FLAG_BIT_WRAPMODE_DISSOLVE_MAP, flagIndex: 2,
                        drawBlock: (dissolveTex) =>
                        {
                            DrawColorChannelSelect("溶解贴图通道选择",
                                W9ParticleShaderFlags.FLAG_BIT_COLOR_CHANNEL_POS_0_DISSOLVE_MAP);
                            // matEditor.TextureScaleOffsetProperty(_helper.GetProperty("_DissolveMap"));
                            DrawCustomDataSelect("溶解贴图X轴偏移自定义曲线",
                                W9ParticleShaderFlags.FLAGBIT_POS_1_CUSTOMDATA_DISSOLVE_OFFSET_X, 1);
                            DrawCustomDataSelect("溶解贴图Y轴偏移自定义曲线",
                                W9ParticleShaderFlags.FLAGBIT_POS_1_CUSTOMDATA_DISSOLVE_OFFSET_Y, 1);
                            DrawUVModeSelect(W9ParticleShaderFlags.foldOutBit1UVModeDissolveMap, 4, "溶解贴图UV来源",
                                W9ParticleShaderFlags.FLAG_BIT_UVMODE_POS_0_DISSOLVE_MAP, 0, dissolveTex);
                            _helper.DrawVector4In2Line("_DissolveOffsetRotateDistort", "溶解贴图偏移速度", true);
                            _helper.DrawVector4Component("溶解贴图旋转", "_DissolveOffsetRotateDistort", "z", true, 0f, 360f);
                        });
                    DrawPNoiseBlendModeSelect("溶解",W9ParticleShaderFlags.FLAG_BIT_PNOISE_BLEND_POS_0_DISSOLVE,"_DissolvePNoiseBlendOpacity");
                    _helper.DrawVector4Component("溶解值Pow", "_Dissolve","y",true,0f,10f);
                    _helper.DrawVector4Component("溶解强度", "_Dissolve", "x", true, rangeVecPropName:"DissolveXRangeVec");
                    DrawCustomDataSelect("溶解强度自定义曲线", W9ParticleShaderFlags.FLAGBIT_POS_0_CUSTOMDATA_DISSOLVE_INTENSITY,
                        0);
                    _helper.DrawVector4Component("溶解硬软度", "_Dissolve", "w", true, 0.001f, 1f);
                    DrawNoiseAffectBlock(() =>
                    {
                        _helper.DrawVector4Component("溶解贴图扭曲强度", "_DissolveOffsetRotateDistort", "w", false);
                    });

                    _helper.DrawToggleFoldOut(W9ParticleShaderFlags.foldOutBit2DissolveLine,5,GetAnimBoolIndex(5),"溶解描边","_DissolveLineMaskToggle",W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_DISSOLVE_LINE_MASK,1,drawBlock:
                        isDissolveLineMask =>
                        {
                             // matEditor.ColorProperty(_helper.GetProperty("_DissolveLineColor"), "溶解描边颜色");
                             _helper.ColorProperty("溶解描边颜色","_DissolveLineColor");
                            _helper.DrawVector4Component("描边位置","_Dissolve_Vec2","x",true,rangeVecPropName:"Dissolve2XRangeVec");
                            _helper.DrawVector4Component("描边软硬","_Dissolve_Vec2","y",true,rangeVecPropName:"Dissolve2YRangeVec");

                        });

           
                    _helper.DrawToggleFoldOut(W9ParticleShaderFlags.foldOutDissolveRampMap, 3, GetAnimBoolIndex(3),
                        "溶解Ramp图功能", "_Dissolve_useRampMap_Toggle",
                        W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_DISSOVLE_USE_RAMP, flagIndex: 1, isIndentBlock: true,
                        drawBlock:
                        isDissolveUseRampToggle =>
                        {
                            _helper.DrawPopUp("溶解Ramp模式", "_DissolveRampSourceMode", dissolveRampSourceMode,
                                drawBlock: dissolveRampModeProp =>
                                {
                                    Action drawRampTexture = () =>
                                    {
                                        _helper.DrawTexture("溶解Ramp图", "_DissolveRampMap", "_DissolveRampColor",
                                            drawScaleOffset: true, drawWrapMode: true,
                                            wrapModeFlagBitsName: W9ParticleShaderFlags
                                                .FLAG_BIT_WRAPMODE_DISSOLVE_RAMPMAP, flagIndex: 2);
                                    };
                                    Action drawRampGradient = () =>
                                    {
                                        dissolveRampColorPropArr[0] = _helper.GetProperty("_DissolveRampColor0");
                                        dissolveRampColorPropArr[1] = _helper.GetProperty("_DissolveRampColor1");
                                        dissolveRampColorPropArr[2] = _helper.GetProperty("_DissolveRampColor2");
                                        dissolveRampColorPropArr[3] = _helper.GetProperty("_DissolveRampColor3");
                                        dissolveRampColorPropArr[4] = _helper.GetProperty("_DissolveRampColor4");
                                        dissolveRampColorPropArr[5] = _helper.GetProperty("_DissolveRampColor5");
                                        dissolveRampAlphaPropArr[0] = _helper.GetProperty("_DissolveRampAlpha0");
                                        dissolveRampAlphaPropArr[1] = _helper.GetProperty("_DissolveRampAlpha1");
                                        dissolveRampAlphaPropArr[2] = _helper.GetProperty("_DissolveRampAlpha2");
                                        _helper.DrawGradient(true, ColorSpace.Gamma,
                                            "Ramp颜色", 6, "_DissolveRampCount", dissolveRampColorPropArr,
                                            dissolveRampAlphaPropArr);
                                        _helper.TextureScaleOffsetProperty("_DissolveRampMap");
                                        _helper.ColorProperty("Ramp颜色叠加","_DissolveRampColor");
                                        _helper.DrawWrapMode("溶解RampUV",
                                            W9ParticleShaderFlags.FLAG_BIT_WRAPMODE_DISSOLVE_RAMPMAP, 2);
                                        
                                    };
                                    if (_helper.ResetTool.IsInitResetData)
                                    {
                                        drawRampTexture();
                                        drawRampGradient();
                                    }
                                    else
                                    {
                                        if (!dissolveRampModeProp.hasMixedValue)
                                        {
                                            if (dissolveRampModeProp.floatValue > 0.5f)
                                            {
                                                drawRampTexture();
                                            }
                                            else
                                            {
                                                drawRampGradient();
                                            }
                                        }
                                    }
                                    
                                    
                                    _helper.DrawPopUp("溶解Ramp混合模式","_DissolveRampColorBlendMode",dissolveRampBlendModeNames,drawOnValueChangedBlock:
                                        rampColorBend =>
                                        {
                                            for (int i = 0; i < shaderFlags.Count; i++)
                                            {
                                                if (rampColorBend.floatValue > 0.5f)
                                                {
                                                    shaderFlags[i].SetFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_DISSOLVE_RAMP_MULITPLY,index:1);
                                                }
                                                else
                                                {
                                                    shaderFlags[i].ClearFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_DISSOLVE_RAMP_MULITPLY,index:1);
                                                }
                                            }
                                        });
                                }, drawOnValueChangedBlock: dissolveRampModeProp =>
                                {
                                    for (int i = 0; i < shaderFlags.Count; i++)
                                    {
                                        if (dissolveRampModeProp.floatValue > 0.5f)
                                        {
                                            shaderFlags[i].SetFlagBits(W9ParticleShaderFlags
                                                .FLAG_BIT_PARTICLE_DISSOLVE_RAMP_MAP);
                                        }
                                        else
                                        {
                                            shaderFlags[i].ClearFlagBits(W9ParticleShaderFlags
                                                .FLAG_BIT_PARTICLE_DISSOLVE_RAMP_MAP);
                                        }
                                    }
                                });
                        });


                    _helper.DrawToggleFoldOut(W9ParticleShaderFlags.foldOutDissolveMask, 3, GetAnimBoolIndex(3), "溶解遮罩图(过程溶解)",
                        "_DissolveMask_Toggle", W9ParticleShaderFlags.FLAG_BIT_PARTICLE_DISSOLVE_MASK, drawBlock:
                        (isToggle) =>
                        {
                            _helper.DrawTexture("溶解遮罩图", "_DissolveMaskMap", drawWrapMode: true,
                                wrapModeFlagBitsName: W9ParticleShaderFlags.FLAG_BIT_WRAPMODE_DISSOLVE_MASKMAP,
                                flagIndex: 2, drawBlock:
                                texProp =>
                                {
                                    DrawUVModeSelect(W9ParticleShaderFlags.foldOutBit1UVModeDissolveMaskMap, 4,
                                        "溶解遮罩图UV来源", W9ParticleShaderFlags.FLAG_BIT_UVMODE_POS_0_DISSOLVE_MASK_MAP, 0,
                                        texProp);
                                });
                            DrawColorChannelSelect("溶解遮罩图通道选择",
                                W9ParticleShaderFlags.FLAG_BIT_COLOR_CHANNEL_POS_0_DISSOLVE_MASK_MAP);
                            _helper.DrawVector4Component("溶解遮罩强度", "_Dissolve", "z", false);
                            DrawCustomDataSelect("溶解遮罩图强度自定义曲线",
                                W9ParticleShaderFlags.FLAGBIT_POS_1_CUSTOMDATA_DISSOLVE_MASK_INTENSITY, 1);
                        });
                    
                    DrawPNoiseBlendModeSelect("溶解",W9ParticleShaderFlags.FLAG_BIT_PNOISE_BLEND_POS_0_DISSOLVE,"_DissolvePNoiseBlendOpacity");

                });

            MaterialProperty pNoiseToggle = _helper.GetProperty("_ProgramNoise_Toggle");
            if (pNoiseToggle.hasMixedValue)
            {
                _pNoiseEnabled = -1;
            }
            else
            {
                _pNoiseEnabled = pNoiseToggle.floatValue > 0.5f ? 1 : 0;
            }
            _helper.DrawToggleFoldOut(W9ParticleShaderFlags.foldOutProgramNoise, 3, GetAnimBoolIndex(3), "程序化噪波",
                "_ProgramNoise_Toggle", shaderKeyword: "_PROGRAM_NOISE", fontStyle: FontStyle.Bold,
                drawBlock: (isToggle) =>
                {
                    _helper.DrawToggle("程序化噪波测试颜色","_NB_Debug_PNoise",shaderKeyword:"NB_DEBUG_PNOISE");
                    
                    DrawUVModeSelect(W9ParticleShaderFlags.foldOutBit1UVModeProgramNoise, 4, "程序噪波UV来源", W9ParticleShaderFlags.FLAG_BIT_UVMODE_POS_0_PROGRAM_NOISE, 0);
                    _helper.DrawFloat("程序化噪波旋转","_ProgramNoise_Rotate");
                    _helper.DrawToggleFoldOut(W9ParticleShaderFlags.foldOutBit2ProgramNoiseSimple,5,GetAnimBoolIndex(5),"Perlin噪波",propertyName:"_ProgramNoise_Simple_Toggle",flagBitsName:W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_PROGRAM_NOISE_SIMPLE,flagIndex:1,drawBlock:
                        isToggle =>
                        {
                            _helper.DrawVector4In2Line("_DissolveVoronoi_Vec", "噪波1缩放", true);
                            _helper.DrawVector4Component("噪波1速度", "_DissolveVoronoi_Vec2", "z", false);
                            _helper.DrawVector4In2Line("_DissolveVoronoi_Vec4", "噪波1偏移", true);
                            DrawCustomDataSelect("噪波1偏移速度X自定义曲线",
                                W9ParticleShaderFlags.FLAGBIT_POS_2_CUSTOMDATA_DISSOLVE_NOISE1_OFFSET_X, 2);
                            DrawCustomDataSelect("噪波1偏移速度Y自定义曲线",
                                W9ParticleShaderFlags.FLAGBIT_POS_2_CUSTOMDATA_DISSOLVE_NOISE1_OFFSET_Y, 2);
                            _helper.DrawVector4In2Line("_DissolveVoronoi_Vec3", "噪波1偏移速度", true);
                        });
                    _helper.DrawToggleFoldOut(W9ParticleShaderFlags.foldOutBit2ProgramNoiseVoronoi, 5,
                        GetAnimBoolIndex(5), "Voronoi噪波", propertyName: "_ProgramNoise_Voronoi_Toggle",
                        flagBitsName: W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_PROGRAM_NOISE_VORONOI, flagIndex: 1,
                        drawBlock:
                        isToggle =>
                        {
                            _helper.DrawVector4In2Line("_DissolveVoronoi_Vec", "噪波2缩放", false);
                            _helper.DrawVector4Component("噪波2速度", "_DissolveVoronoi_Vec2", "w", false);
                            _helper.DrawVector4In2Line("_DissolveVoronoi_Vec4", "噪波2偏移", false);
                            DrawCustomDataSelect("噪波2偏移速度X自定义曲线",
                                W9ParticleShaderFlags.FLAGBIT_POS_2_CUSTOMDATA_DISSOLVE_NOISE2_OFFSET_X, 2);
                            DrawCustomDataSelect("噪波2偏移速度Y自定义曲线",
                                W9ParticleShaderFlags.FLAGBIT_POS_2_CUSTOMDATA_DISSOLVE_NOISE2_OFFSET_Y, 2);
                            _helper.DrawVector4In2Line("_DissolveVoronoi_Vec3", "噪波2偏移速度", false);
                        });

                    if ((_helper.GetProperty("_ProgramNoise_Simple_Toggle").floatValue > 0.5 &&
                         _helper.GetProperty("_ProgramNoise_Voronoi_Toggle").floatValue > 0.5) ||
                        _helper.ResetTool.IsInitResetData)
                    {
                        DrawPNoiseBlendModeSelect("两种",W9ParticleShaderFlags.FLAG_BIT_PNOISE_BLEND_POS_0_BASE_BLEND,"_ProgramNoiseBaseBlendOpacity");
                    }
                    // _helper.DrawVector4Component("噪波整体和溶解贴图混合系数", "_DissolveVoronoi_Vec2", "y", true);
                    EditorGUILayout.Space();
                });
            
            _helper.DrawToggleFoldOut(W9ParticleShaderFlags.foldOutBit2SharedUV,5,GetAnimBoolIndex(5),"公共UV","_SharedUVToggle",shaderKeyword:"_SHARED_UV",fontStyle:FontStyle.Bold,drawBlock:
                (isToggle) =>
                {
                    _helper.TextureScaleOffsetProperty("_SharedUV_ST",true);
                    _helper.DrawWrapMode("公共UV",W9ParticleShaderFlags.FLAG_BIT_WRAPMODE2_SHAREDUV,8);
                    DrawUVModeSelect(W9ParticleShaderFlags.foldOutBit2SharedUVMode,5,"公共UV来源",W9ParticleShaderFlags.FLAG_BIT_UVMODE_POS_0_SHAREDUV,0);
                });

            _helper.DrawToggleFoldOut(W9ParticleShaderFlags.foldOutFresnel, 3, GetAnimBoolIndex(3), "菲涅尔",
                "_fresnelEnabled", W9ParticleShaderFlags.FLAG_BIT_PARTICLE_FRESNEL_ON, isIndentBlock: true,
                fontStyle: FontStyle.Bold,
                drawBlock: (isToggle) =>
                {
                    _helper.DrawToggle("菲涅尔值测试","_NB_Debug_Distort",shaderKeyword:"NB_DEBUG_FRESNEL");
                    
                    Action drawFresnelColorMode = () =>
                    {
                        // matEditor.ColorProperty(_helper.GetProperty("_FresnelColor"), "菲涅尔颜色");
                        _helper.ColorProperty("菲涅尔颜色","_FresnelColor");
                        _helper.DrawToggle("菲涅尔颜色受Alpha影响", "_FresnelColorAffectByAlpha",
                            W9ParticleShaderFlags.FLAG_BIT_PARTICLE_FRESNEL_COLOR_AFFETCT_BY_ALPHA);
                    };
                    _helper.DrawPopUp("菲涅尔模式", "_FresnelMode", _fresnelModeNames, drawBlock:
                        fresnelModeProp =>
                        {
                            if (_helper.ResetTool.IsInitResetData)
                            {
                                drawFresnelColorMode();
                            }
                            else
                            {
                                if ((!fresnelModeProp.hasMixedValue) && fresnelModeProp.floatValue.Equals((float)FresnelMode.Color))
                                {
                                    drawFresnelColorMode();
                                }
                            }
                        },
                        drawOnValueChangedBlock: fresnelModeProp =>
                        {
                            FresnelMode fresnelMode = (FresnelMode)fresnelModeProp.floatValue;
                            for (int i = 0; i < shaderFlags.Count; i++)
                            {
                                switch (fresnelMode)
                                {
                                    case FresnelMode.Color:
                                        shaderFlags[i].ClearFlagBits(W9ParticleShaderFlags
                                            .FLAG_BIT_PARTICLE_FRESNEL_FADE_ON);
                                        break;
                                    case FresnelMode.Fade:
                                        shaderFlags[i].SetFlagBits(W9ParticleShaderFlags
                                            .FLAG_BIT_PARTICLE_FRESNEL_FADE_ON);
                                        break;
                                }
                            }
                        });
                    _helper.DrawVector4Component("菲涅尔强度", "_FresnelUnit", "z", true);


                    if (mats.Count == 1)
                    {
                        FresnelMode fresnelMode = (FresnelMode)mats[0].GetFloat("_FresnelMode");

                    }

                    _helper.DrawVector4Component("菲涅尔位置", "_FresnelUnit", "x", true, -1f, 1f);
                    DrawCustomDataSelect("菲尼尔位置自定义曲线", W9ParticleShaderFlags.FLAGBIT_POS_0_CUSTOMDATA_FRESNEL_OFFSET,
                        0);
                    _helper.DrawVector4Component("菲涅尔范围Pow", "_FresnelUnit", "y", true, 0f, 10f);
                    _helper.DrawVector4Component("菲涅尔硬度", "_FresnelUnit", "w", true, 0f, 1f);
                    _helper.DrawToggle("翻转菲涅尔", "_InvertFresnel_Toggle",
                        W9ParticleShaderFlags.FLAG_BIT_PARTICLE_FRESNEL_INVERT_ON);
                    _helper.DrawVector4XYZComponet("菲涅尔方向偏移","_FresnelRotation");
                });


            _helper.DrawToggleFoldOut(W9ParticleShaderFlags.foldOutVertexOffset, 3, GetAnimBoolIndex(3), "顶点偏移",
                "_VertexOffset_Toggle", W9ParticleShaderFlags.FLAG_BIT_PARTICLE_VERTEX_OFFSET_ON, isIndentBlock: true,
                fontStyle: FontStyle.Bold,
                drawBlock: isToggle =>
                {
                    _helper.DrawTexture("顶点偏移贴图", "_VertexOffset_Map", drawScaleOffset: true, drawWrapMode: true,
                        wrapModeFlagBitsName: W9ParticleShaderFlags.FLAG_BIT_WRAPMODE_VERTEXOFFSETMAP, flagIndex: 2,
                        drawBlock: texProp =>
                        {
                            DrawUVModeSelect(W9ParticleShaderFlags.foldOutBit1UVModeVertexOffsetMap, 4, "顶点偏移贴图UV来源",
                                W9ParticleShaderFlags.FLAG_BIT_UVMODE_POS_0_VERTEX_OFFSET_MAP, 0, texProp);
                        });
                    DrawCustomDataSelect("顶点扰动X轴偏移自定义曲线",
                        W9ParticleShaderFlags.FLAGBIT_POS_1_CUSTOMDATA_VERTEX_OFFSET_X, 1);
                    DrawCustomDataSelect("顶点扰动Y轴偏移自定义曲线",
                        W9ParticleShaderFlags.FLAGBIT_POS_1_CUSTOMDATA_VERTEX_OFFSET_Y, 1);

                    _helper.DrawVector4In2Line("_VertexOffset_Vec", "顶点偏移动画", true);
                    _helper.DrawVector4Component("顶点偏移强度", "_VertexOffset_Vec", "z", false);
                    DrawCustomDataSelect("顶点扰动强度自定义曲线",
                        W9ParticleShaderFlags.FLAGBIT_POS_1_CUSTOMDATA_VERTEXOFFSET_INTENSITY, 1);
                    _helper.DrawToggle("顶点偏移从零开始", "_VertexOffset_StartFromZero",
                        W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_VERTEXOFFSET_START_FROM_ZERO, 1);
                    _helper.DrawToggle("顶点偏移使用法线方向", "_VertexOffset_NormalDir_Toggle",
                        W9ParticleShaderFlags.FLAG_BIT_PARTICLE_VERTEX_OFFSET_NORMAL_DIR, isIndentBlock: false,
                        drawBlock:
                        isToggle =>
                        {
                            if (!isToggle.hasMixedValue && isToggle.floatValue < 0.5f)
                            {
                                // matEditor.ShaderProperty(_helper.GetProperty("_VertexOffset_CustomDir"), "顶点偏移本地方向");
                                _helper.DrawVector4XYZComponet("顶点偏移本地方向","_VertexOffset_CustomDir");
                            }
                        });
                    _helper.DrawToggleFoldOut(W9ParticleShaderFlags.foldOutBit1VertexOffsetMask, 4, GetAnimBoolIndex(4),
                        "顶点偏移遮罩", "_VertexOffset_Mask_Toggle",
                        W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_VERTEXOFFSET_MASKMAP, 1,
                        drawBlock: isMaskToggle =>
                        {
                            _helper.DrawTexture("顶点偏移遮罩图", "_VertexOffset_MaskMap", drawScaleOffset: true,
                                drawWrapMode: true,
                                wrapModeFlagBitsName: W9ParticleShaderFlags.FLAG_BIT_WRAPMODE_VERTEXOFFSET_MASKMAP,
                                flagIndex: 2,
                                drawBlock: texProp =>
                                {
                                    DrawUVModeSelect(W9ParticleShaderFlags.foldOutBit1UVModeVertexOffsetMaskMap, 4,
                                        "顶点偏移遮罩图UV来源",
                                        W9ParticleShaderFlags.FLAG_BIT_UVMODE_POS_0_VERTEX_OFFSET_MASKMAP, 0, texProp);
                                });
                            DrawCustomDataSelect("顶点扰动遮罩X轴偏移自定义曲线",
                                W9ParticleShaderFlags.FLAGBIT_POS_3_CUSTOMDATA_VERTEX_OFFSET_MASK_X, 3);
                            DrawCustomDataSelect("顶点扰动遮罩Y轴偏移自定义曲线",
                                W9ParticleShaderFlags.FLAGBIT_POS_3_CUSTOMDATA_VERTEX_OFFSET_MASK_Y, 3);
                            _helper.DrawVector4In2Line("_VertexOffset_MaskMap_Vec", "顶点偏移遮罩动画", true);
                            _helper.DrawVector4Component("顶点偏移遮罩强度", "_VertexOffset_MaskMap_Vec", "z", true);
                        });
                });

            if (_uiEffectEnabled == 0)
            {
                _helper.DrawToggleFoldOut(W9ParticleShaderFlags.foldOutDepthOutline, 3, GetAnimBoolIndex(3), "深度描边",
                    "_DepthOutline_Toggle",
                    flagBitsName: W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_DEPTH_OUTLINE, fontStyle: FontStyle.Bold,
                    flagIndex: 1, isIndentBlock: true,
                    drawBlock: (isToggle) =>
                    {
                        _helper.ColorProperty("深度描边颜色","_DepthOutline_Color");
                        _helper.DrawVector4In2Line("_DepthOutline_Vec", "深度描边距离", true);
                    });

                _helper.DrawToggle("深度贴花", "_DepthDecal_Toggle", shaderKeyword: "_DEPTH_DECAL",
                    fontStyle: FontStyle.Bold,
                    drawEndChangeCheck: (isToggle) =>
                    {
                        if (!isToggle.hasMixedValue)
                        {
                            for (int i = 0; i < mats.Count; i++)
                            {
                                if(_helper.ResetTool.IsInitResetData) break;//避免初始化时重设
                                if (isToggle.floatValue > 0.5f)
                                {
                                    StencilTestHelper.SetMaterialStencil(mats[i], "ParticleBaseDecal",
                                        _stencilValuesConfig, out int ignore);
                                    mats[i].SetFloat(_isCustomedStencilPropID, 1f);
                                    mats[i].SetFloat("_Cull", (float)RenderFace.Back);
                                    mats[i].SetFloat("_ZTest", (float)CompareFunction.GreaterEqual);
                                }
                                else
                                {
                                    StencilTestHelper.SetMaterialStencil(mats[i], _defaultStencilKey,
                                        _stencilValuesConfig, out int ignore);
                                    mats[i].SetFloat(_isCustomedStencilPropID, 0f);
                                    mats[i].SetFloat("_Cull", (float)RenderFace.Front);
                                    mats[i].SetFloat("_ZTest", (float)CompareFunction.LessEqual);
                                }
                            }

                        }
                    });

                _helper.DrawToggleFoldOut(W9ParticleShaderFlags.foldOutParallexMapping, 3, GetAnimBoolIndex(3), "遮蔽视差",
                    "_ParallaxMapping_Toggle", shaderKeyword: "_PARALLAX_MAPPING",
                    isIndentBlock: true, fontStyle: FontStyle.Bold,
                    drawBlock: isTogggle =>
                    {
                        _helper.DrawTexture("视差贴图", "_ParallaxMapping_Map", drawWrapMode: true,
                            wrapModeFlagBitsName: W9ParticleShaderFlags.FLAG_BIT_WRAPMODE_PARALLAXMAPPINGMAP,
                            flagIndex: 2);
                        _helper.DrawSlider("视差", "_ParallaxMapping_Intensity", rangePropertyName:"_ParallaxMapping_IntensityRangeVec");

                        Action<float, bool> OnPomLayerCountChange = (f, isMixedValue) =>
                        {
                            int shaderID = Shader.PropertyToID("_ParallaxMapping_Vec");
                            for (int i = 0; i < mats.Count; i++)
                            {
                                Vector4 vecValue = mats[i].GetVector(shaderID);
                                if (vecValue.y < vecValue.x + 1)
                                {
                                    vecValue.y = vecValue.x + 1;
                                }

                                mats[i].SetVector(shaderID, vecValue);
                            }
                        };

                        _helper.DrawVector4Component("遮蔽视差最小层数", "_ParallaxMapping_Vec", "x", true, 0f, 100f,
                            drawEndChangeCheckBlock: OnPomLayerCountChange);
                        _helper.DrawVector4Component("遮蔽视差最大层数", "_ParallaxMapping_Vec", "y", true, 0f, 100f,
                            drawEndChangeCheckBlock: OnPomLayerCountChange,
                            drawBlock: (f, hasMixedValue) =>
                            {
                                if (!hasMixedValue && f >= 20f)
                                {
                                    EditorGUILayout.HelpBox("遮蔽视差层数过高将影响性能", MessageType.Warning);
                                }
                            });
                    });

                Action<Material> SetPortal = (mat) =>
                {
                    StencilTestHelper.SetMaterialStencil(mat, "ParticalBasePortal", _stencilValuesConfig,
                        out int Ignore);
                    mat.SetFloat(_isCustomedStencilPropID, 1f);
                };

                Action<Material> SetPortalMask = (mat) =>
                {
                    StencilTestHelper.SetMaterialStencil(mat, "ParticalBasePortalMask", _stencilValuesConfig,
                        out int Ignore);
                    if (mat.GetFloat("_TransparentMode") == (float)TransparentMode.Transparent)
                    {
                        mat.SetFloat("_TransparentMode", (float)TransparentMode.CutOff);
                    }

                    mat.SetFloat("_ZTest", (float)CompareFunction.LessEqual);
                    mat.SetFloat("_ForceZWriteToggle",2);
                };
                Action<Material> RestPortal = (mat) =>
                {
                    StencilTestHelper.SetMaterialStencil(mat, _defaultStencilKey,
                        _stencilValuesConfig, out int ignore);
                    mat.SetFloat(_isCustomedStencilPropID, 0f);
                    mat.SetFloat("_TransparentMode", (float)TransparentMode.Transparent);
                    mat.SetFloat("_ZTest", (float)CompareFunction.LessEqual);
                    mat.SetFloat("_ForceZWriteToggle", 0f);
                };

                _helper.DrawToggleFoldOut(W9ParticleShaderFlags.foldOutBit1Portal, 4, GetAnimBoolIndex(4), "模板视差",
                    "_Portal_Toggle", fontStyle: FontStyle.Bold,
                    drawBlock: isPortalToggle =>
                    {
                        _helper.DrawToggle("模板视差蒙版", "_Portal_MaskToggle", drawEndChangeCheck: isPortalMaskToggle =>
                        {
                            if (!isPortalMaskToggle.hasMixedValue)
                            {

                                for (int i = 0; i < mats.Count; i++)
                                {
                                    
                                    if (isPortalMaskToggle.floatValue > 0.5f)
                                    {
                                        SetPortalMask(mats[i]);
                                    }
                                    else if(isPortalToggle.floatValue >0.5f) 
                                    {
                                        SetPortal(mats[i]);
                                    }
                                    else
                                    {
                                        if(_helper.ResetTool.IsInitResetData) continue;//避免初始化时重设
                                        RestPortal(mats[i]);
                                    }

                                }
                            }
                        });
                    }, drawEndChangeCheck: (isPortalToggle) =>
                    {
                        
                        for (int i = 0; i < mats.Count; i++)
                        {
                            if (isPortalToggle.floatValue > 0.5f)
                            {
                                if (mats[i].GetFloat("_Portal_MaskToggle") < 0.5f)
                                {
                                    SetPortal(mats[i]);
                                }
                                else
                                {
                                    SetPortalMask(mats[i]);
                                }
                            }
                            else
                            {
                                if(_helper.ResetTool.IsInitResetData )continue;//避免初始化时重设
                                RestPortal(mats[i]);
                            }
                        }
                    });
            }

            //粒子序列帧融帧的逻辑，是将UV0为第一格，UV1234推到第二格，中间用AnimBlend融合）。所以多UV是必然和这个矛盾的。
            _helper.DrawToggle("序列帧融帧(丝滑)", "_FlipbookBlending", shaderKeyword: "_FLIPBOOKBLENDING_ON",
                fontStyle: FontStyle.Bold, drawBlock: (isToggle) =>
                {
                    if (!isToggle.hasMixedValue && isToggle.floatValue > 0.5f)
                    {
                        if (_meshSourceMode == MeshSourceMode.Particle)
                        {
                            if (shaderFlags[0].CheckIsUVModeOn(UVMode.SpecialUVChannel))
                            {
                                EditorGUILayout.HelpBox("序列帧融帧和特殊UV通道同时开启，粒子序列帧应该影响UV0和UV1两个通道，特殊通道只能使用UV3（原始UV）",
                                    MessageType.Warning);
                            }
                            else
                            {
                                EditorGUILayout.HelpBox("AnimationSheet的AffectUVChannel需要有UV0和UV1",
                                    MessageType.Info);
                            }
                        }
                        else if (_meshSourceMode == MeshSourceMode.Mesh)
                        {
                            EditorGUILayout.HelpBox("需要添加AnimationSheetHelper脚本", MessageType.Info);
                        }
                    }
                });

        }

        public void DrawTaOptions()
        {
            if (_uiEffectEnabled == 0)
            {
                _helper.DrawToggleFoldOut(W9ParticleShaderFlags.foldOutBit1ZOffset, 4, GetAnimBoolIndex(4), "深度偏移",
                    "_ZOffset_Toggle", fontStyle: FontStyle.Bold, drawBlock:
                    (isToggle) =>
                    {

                        matEditor.ShaderProperty(_helper.GetProperty("_offsetFactor"), "OffsetFactor");
                        matEditor.ShaderProperty(_helper.GetProperty("_offsetUnits"), "Offset单位");
                    },
                    drawEndChangeCheck: (isToggle) =>
                    {
                        if (!isToggle.hasMixedValue && isToggle.floatValue < 0.5f)
                        {
                            for (int i = 0; i < mats.Count; i++)
                            {
                                mats[i].SetFloat("_offsetFactor", 0f);
                                mats[i].SetFloat("_offsetUnits", 0f);
                            }
                        }
                    });
            }


            _helper.DrawRenderQueue(_helper.GetProperty("_QueueBias"));

            _helper.DrawToggleFoldOut(W9ParticleShaderFlags.foldOutBit1CustomStencilTest, 4, GetAnimBoolIndex(4),
                "模板手动调试开关", "_CustomStencilTest",
                drawBlock: isToggle =>
                {
                    bool hasMixedKeyValue = false;
                    int stencilKeyIndexID = Shader.PropertyToID("_StencilKeyIndex");
                    string originKey = "";
                    for (int i = 0; i < mats.Count; i++)
                    {
                        string key = _stencilValuesConfig.GetKeyByIndex(mats[i].GetInt(stencilKeyIndexID));
                        if (i == 0)
                        {
                            originKey = key;
                        }
                        else
                        {
                            if (originKey != key) hasMixedKeyValue = true;
                        }

                        hasMixedKeyValue = false;
                    }

                    EditorGUI.showMixedValue = hasMixedKeyValue;
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.TextField("当前Config:", originKey);
                    EditorGUI.EndDisabledGroup();
                    EditorGUI.showMixedValue = false;

                    matEditor.ShaderProperty(_helper.GetProperty("_Stencil"), "模板值");
                    matEditor.ShaderProperty(_helper.GetProperty("_StencilComp"), "模板比较方式");
                    matEditor.ShaderProperty(_helper.GetProperty("_StencilOp"), "模板处理方式");

                },
                drawEndChangeCheck: isToggle =>
                {
                    if (!isToggle.hasMixedValue)
                    {
                        for (int i = 0; i < mats.Count; i++)
                        {
                            if (isToggle.floatValue > 0.5f)
                            {
                                mats[i].SetFloat(_isCustomedStencilPropID, 1f);
                            }
                            else
                            {
                                StencilTestHelper.SetMaterialStencil(mats[i], _defaultStencilKey, _stencilValuesConfig,
                                    out int ignore);
                            }

                        }
                    }
                });


            if (mats.Count == 1)
            {
                _helper.DrawFoldOut(W9ParticleShaderFlags.foldOutBit1ShaderKeyword, 4, GetAnimBoolIndex(4),
                    "已开启Keyword:", drawBlock:
                    () =>
                    {
                        List<string> shaderKeywords = new List<string>();
                        foreach (var localKeyword in mats[0].enabledKeywords)
                        {
                            shaderKeywords.Add(localKeyword.name);
                        }

                        if (shaderKeywords != null && shaderKeywords.Count > 0)
                        {
                            float height = EditorGUIUtility.singleLineHeight * shaderKeywords.Count;
                            Rect labelRect = EditorGUILayout.GetControlRect(false, height);
                            string label = "";
                            for (int i = 0; i < shaderKeywords.Count; i++)
                            {
                                label += shaderKeywords[i];
                                label += "\n";
                            }

                            EditorGUI.LabelField(labelRect, label);
                        }

                    });
            }
        }

        void DrawNoiseAffectBlock(Action drawBlock)
        {
            
            EditorGUI.BeginDisabledGroup(_noiseEnabled == 0);
            EditorGUI.showMixedValue = _noiseEnabled < 0;
            drawBlock();
            EditorGUI.showMixedValue = false;
            EditorGUI.EndDisabledGroup();
        }

        public string[] blendModeNames =
        {
            "透明度混合AlphaBlend",
            "预乘PreMultiply",
            "叠加Additive",
            "正片叠底Multiply"
        };

        public enum BlendMode
        {
            Alpha, // Old school alpha-blending mode, fresnel does not affect amount of transparency
            Premultiply, // Physically plausible transparency mode, implemented as alpha pre-multiply
            Additive,
            Multiply,
            Opaque
        }

        public enum TimeMode
        {
            Default,
            UnScaleTime,
            ScriptableTime
        }

        public enum RenderFace
        {
            Front = 2,
            Back = 1,
            Both = 0
        }

        public enum FresnelMode
        {
            Color = 0,
            Fade = 1,
            UnkownOrMixed = -1
        }

        private string[] _fresnelModeNames =
        {
            "颜色|边缘光",
            "半透明|渐隐"
        };

        private string[] _maskMapModeNames =
        {
            "遮罩贴图",
            "渐变控件"
        };

        private string[] _ForceZWriteToggleNames =
        {
            "默认",
            "强制开启",
            "强制关闭"
    };
        
        
        public enum FxLightMode
        {
            UnLit,
            BlinnPhong,
            HalfLambert,
            PBR,
            SixWay,
            UnKnownOrMixedValue = -1
        }
        private string[] _fxLightModeNames =
        {
            "默认无光(Unlit)",
            "简单光照(BlinnPhong)",
            "简单光照通透(HalfLambert)",
            "高级光照(PBR)",
            "六路光照(SixWay)"
        };

        public string[] transparentModeNames =
        {
            "不透明Opaque",
            "半透明Transparent",
            "不透明裁剪CutOff"
        };
        
        public enum TransparentMode
        {
            Opaque = 0,
            Transparent = 1,
            CutOff = 2,
            UnKnowOrMixed = -1
        }

        private string[] matCapBlendModeNames =
        {
            "相加Add",
            "相乘Multiply",
        };
        
        private string[] dissolveRampBlendModeNames =
        {
            "线性差值Lerp",
            "相乘Multiply",
        };

        private string[] colorBlendAlphaMode = new[]
        {
            "颜色渐变强度",
            "遮罩(乘以主贴图Alpha)"
        };

        private string[] rampColorSourceMode =
        {
            "UV",
            "映射贴图"
        };
        
        private string[] dissolveRampSourceMode =
        {
            "渐变控件",
            "Ramp贴图"
        };
        
        private string[] rampColorBlendMode =
        {
            "相乘Multiply",
            "相加Add"
        };

        public enum ScreenDistortMode
        {
            None = 0,
            DeferredDistort = 1,
            CameraOpaqueDistort = 2,
            Mixed = -1
        }
        
        private string[] screenDistortModeNames =
        {
            "不扰动屏幕",
            "全部扰动(后处理扰动)",
            "仅影响场景(半透明前扰动)"
        };

        private string[] distortModeNames =
        {
            "扰动贴图",
            "折射"
        };
        
        void DoAfterDraw()
        {
            // Debug.Log(mats[0].name + " MaterialEditorDoAfterDraw!");
            for (int i = 0; i < mats.Count; i++)
            {
               
                switch (_meshSourceMode)
                {
                    case MeshSourceMode.Particle:
                        shaderFlags[i].SetFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_IS_PARTICLE_SYSTEM, index: 1);
                        shaderFlags[i].ClearFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_UV_FROM_MESH, index: 1);
                        shaderFlags[i].ClearFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_UIEFFECT_ON, index: 0);
                        shaderFlags[i].ClearFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_UIEFFECT_SPRITE_MODE,
                            index: 1);
                        shaderFlags[i].ClearFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_UIEFFECT_BASEMAP_MODE,
                            index: 1);

                        //如果是粒子系统，则不需要走AnimationSheetHelper
                        shaderFlags[i].ClearFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_ANIMATION_SHEET_HELPER);
                        break;
                    case MeshSourceMode.Mesh:
                        shaderFlags[i].ClearFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_IS_PARTICLE_SYSTEM,
                            index: 1);
                        shaderFlags[i].SetFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_UV_FROM_MESH, index: 1);
                        shaderFlags[i].ClearFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_UIEFFECT_ON, index: 0);
                        shaderFlags[i].ClearFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_UIEFFECT_SPRITE_MODE,
                            index: 1);
                        shaderFlags[i].ClearFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_UIEFFECT_BASEMAP_MODE,
                            index: 1);
                        break;
                    case MeshSourceMode.UIEffectRawImage:
                        shaderFlags[i].ClearFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_IS_PARTICLE_SYSTEM,
                            index: 1);
                        shaderFlags[i].ClearFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_UV_FROM_MESH, index: 1);
                        shaderFlags[i].SetFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_UIEFFECT_ON, index: 0);
                        shaderFlags[i].ClearFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_UIEFFECT_SPRITE_MODE,
                            index: 1);
                        shaderFlags[i].ClearFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_UIEFFECT_BASEMAP_MODE,
                            index: 1);
                        break;
                    case MeshSourceMode.UIEffectSprite:
                        shaderFlags[i].ClearFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_IS_PARTICLE_SYSTEM,
                            index: 1);
                        shaderFlags[i].ClearFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_UV_FROM_MESH, index: 1);
                        shaderFlags[i].SetFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_UIEFFECT_ON, index: 0);
                        shaderFlags[i].SetFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_UIEFFECT_SPRITE_MODE,
                            index: 1);
                        shaderFlags[i].ClearFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_UIEFFECT_BASEMAP_MODE,
                            index: 1);
                        break;
                    case MeshSourceMode.UIEffectBaseMap:
                        shaderFlags[i].ClearFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_IS_PARTICLE_SYSTEM,
                            index: 1);
                        shaderFlags[i].ClearFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_UV_FROM_MESH, index: 1);
                        shaderFlags[i].SetFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_UIEFFECT_ON, index: 0);
                        shaderFlags[i].ClearFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_UIEFFECT_SPRITE_MODE,
                            index: 1);
                        shaderFlags[i].SetFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_UIEFFECT_BASEMAP_MODE,
                            index: 1);
                        break;
                }
            
                
                if (_meshSourceModeIsParticle > 0.5f)
                {

                    if (shaderFlags[i].IsCustomData1On())
                    {
                        shaderFlags[i].SetFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_CUSTOMDATA1_ON);
                    }
                    else
                    {
                        shaderFlags[i].ClearFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_CUSTOMDATA1_ON);
                    }

                    if (shaderFlags[i].IsCustomData2On())
                    {
                        shaderFlags[i].SetFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_CUSTOMDATA2_ON);
                    }
                    else
                    {
                        shaderFlags[i].ClearFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_CUSTOMDATA2_ON);
                    }

                }
                
                switch (_fxLightMode)
                {
                    case FxLightMode.UnLit:
                        mats[i].EnableKeyword("_FX_LIGHT_MODE_UNLIT");
                        mats[i].DisableKeyword("_FX_LIGHT_MODE_BLINN_PHONG");
                        mats[i].DisableKeyword("_FX_LIGHT_MODE_HALF_LAMBERT");
                        mats[i].DisableKeyword("_FX_LIGHT_MODE_PBR");
                        mats[i].DisableKeyword("_FX_LIGHT_MODE_SIX_WAY");
                        mats[i].DisableKeyword("EVALUATE_SH_VERTEX");
                        break;
                    case FxLightMode.BlinnPhong:
                        mats[i].DisableKeyword("_FX_LIGHT_MODE_UNLIT");
                        mats[i].EnableKeyword("_FX_LIGHT_MODE_BLINN_PHONG");
                        mats[i].DisableKeyword("_FX_LIGHT_MODE_HALF_LAMBERT");
                        mats[i].DisableKeyword("_FX_LIGHT_MODE_PBR");
                        mats[i].DisableKeyword("_FX_LIGHT_MODE_SIX_WAY");
                        mats[i].DisableKeyword("EVALUATE_SH_VERTEX");
                        break;
                    case FxLightMode.HalfLambert:
                        mats[i].DisableKeyword("_FX_LIGHT_MODE_UNLIT");
                        mats[i].DisableKeyword("_FX_LIGHT_MODE_BLINN_PHONG");
                        mats[i].EnableKeyword("_FX_LIGHT_MODE_HALF_LAMBERT");
                        mats[i].DisableKeyword("_FX_LIGHT_MODE_PBR");
                        mats[i].DisableKeyword("_FX_LIGHT_MODE_SIX_WAY");
                        mats[i].DisableKeyword("EVALUATE_SH_VERTEX");
                        break;
                        
                    case FxLightMode.PBR:
                        mats[i].DisableKeyword("_FX_LIGHT_MODE_UNLIT");
                        mats[i].DisableKeyword("_FX_LIGHT_MODE_BLINN_PHONG");
                        mats[i].DisableKeyword("_FX_LIGHT_MODE_HALF_LAMBERT");
                        mats[i].EnableKeyword("_FX_LIGHT_MODE_PBR");
                        mats[i].DisableKeyword("_FX_LIGHT_MODE_SIX_WAY");
                        mats[i].DisableKeyword("EVALUATE_SH_VERTEX");
                        break;
                    case FxLightMode.SixWay:
                        mats[i].DisableKeyword("_FX_LIGHT_MODE_UNLIT");
                        mats[i].DisableKeyword("_FX_LIGHT_MODE_BLINN_PHONG");
                        mats[i].DisableKeyword("_FX_LIGHT_MODE_HALF_LAMBERT");
                        mats[i].DisableKeyword("_FX_LIGHT_MODE_PBR");
                        mats[i].EnableKeyword("_FX_LIGHT_MODE_SIX_WAY");
                        mats[i].EnableKeyword("EVALUATE_SH_VERTEX");//强制六面体使用顶点SH。
                        break;
                }

                if (!shaderFlags[i].CheckIsUVModeOn(UVMode.SpecialUVChannel))
                {
                    shaderFlags[i].ClearFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_USE_TEXCOORD1, index: 1);
                    shaderFlags[i].ClearFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_USE_TEXCOORD2, index: 1);
                }

                if (!shaderFlags[i].CheckIsUVModeOn(UVMode.Cylinder))
                {
                    shaderFlags[i].ClearFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_CYLINDER_CORDINATE, index: 1);
                }


                TransparentMode transparentMode = (TransparentMode)mats[i].GetFloat("_TransparentMode");
                int queueBias = (int)mats[i].GetFloat("_QueueBias");
                switch (transparentMode)
                {
                    case TransparentMode.Opaque:
                        mats[i].SetInt("_ZWrite", (int)1);
                        mats[i].renderQueue = 2100 + queueBias; //3D粒子永远最前显示
                        mats[i].SetInt("_Blend", (int)BlendMode.Opaque);
                        break;
                    case TransparentMode.Transparent:
                        mats[i].SetInt("_ZWrite", (int)0);
                        int defaultQueue = 3100;
                        if (_uiEffectEnabled == 1)
                        {
                            defaultQueue = 3000;
                        }

                        mats[i].renderQueue = defaultQueue + queueBias; //3D粒子永远最前显示

                        BlendMode bm = (BlendMode)mats[i].GetFloat("_Blend");
                        if (bm == BlendMode.Opaque)
                        {
                            mats[i].SetFloat("_Blend", (float)BlendMode.Alpha); //如果设置错误则强制设置。
                        }

                        break;
                    case TransparentMode.CutOff:
                        mats[i].SetInt("_ZWrite", (int)1);
                        mats[i].renderQueue = 2450 + queueBias; //3D粒子永远最前显示
                        mats[i].SetInt("_Blend", (int)BlendMode.Opaque);
                        break;
                }

                float forceZWriteToggle = mats[i].GetFloat("_ForceZWriteToggle");
                if ( forceZWriteToggle > 0.5f&& forceZWriteToggle<1.5f)
                {
                    mats[i].SetInt("_ZWrite", (int)1);
                }
                else if(forceZWriteToggle > 1.5f)
                {
                    mats[i].SetInt("_ZWrite", (int)0);
                }

                if (_transparentMode == TransparentMode.CutOff)
                {
                    mats[i].EnableKeyword("_ALPHATEST_ON");
                }
                else
                {
                    mats[i].DisableKeyword("_ALPHATEST_ON");
                }



                // blendMode
                BlendMode blendMode = (BlendMode)mats[i].GetFloat("_Blend");

                switch (blendMode)
                {
                    case BlendMode.Alpha:
                        mats[i].SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        mats[i].SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        mats[i].DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        mats[i].DisableKeyword("_ALPHAMODULATE_ON");
                        break;
                    case BlendMode.Premultiply:
                        mats[i].SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        mats[i].SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        mats[i].EnableKeyword("_ALPHAPREMULTIPLY_ON");
                        mats[i].DisableKeyword("_ALPHAMODULATE_ON");
                        break;
                    case BlendMode.Additive:
                        mats[i].SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        mats[i].SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        mats[i].EnableKeyword("_ALPHAPREMULTIPLY_ON");
                        mats[i].DisableKeyword("_ALPHAMODULATE_ON");
                        break;
                    case BlendMode.Multiply:
                        mats[i].SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.DstColor);
                        mats[i].SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        mats[i].DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        mats[i].EnableKeyword("_ALPHAMODULATE_ON");
                        break;
                    case BlendMode.Opaque:
                        mats[i].SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        mats[i].SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                        mats[i].DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        break;
                }

                TimeMode timeMode = (TimeMode)mats[i].GetFloat("_TimeMode");

                switch (timeMode)
                {
                    case TimeMode.Default:
                        // setMaterialFlags(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_UNSCALETIME_ON, false);
                        // setMaterialFlags(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_SCRIPTABLETIME_ON, false);
                        shaderFlags[i].ClearFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_UNSCALETIME_ON);
                        shaderFlags[i].CheckFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_SCRIPTABLETIME_ON);
                        break;
                    case TimeMode.UnScaleTime:
                        // setMaterialFlags(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_UNSCALETIME_ON, true);
                        // setMaterialFlags(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_SCRIPTABLETIME_ON, false);
                        shaderFlags[i].SetFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_UNSCALETIME_ON);
                        shaderFlags[i].CheckFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_SCRIPTABLETIME_ON);
                        break;
                    case TimeMode.ScriptableTime:
                        // setMaterialFlags(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_UNSCALETIME_ON, false);
                        // setMaterialFlags(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_SCRIPTABLETIME_ON, true);
                        shaderFlags[i].CheckFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_UNSCALETIME_ON);
                        shaderFlags[i].SetFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_SCRIPTABLETIME_ON);
                        break;
                }
            }
        }
        
        public static GUIContent VertexStreams = new GUIContent("顶点流统计",
            "The vertex streams needed for this Material to function properly.");
  
        public static string streamPositionText = "Position (POSITION.xyz)";
        public static string streamNormalText = "Normal (NORMAL.xyz)";
        public static string streamColorText = "Color (COLOR.xyzw)";
        public static string streamUVText = "UV (TEXCOORD0.xy)";
        public static string streamUV3Text = "UV3 (TEXCOORD0.zw)";
        public static string streamUV2Text = "UV2 (TEXCOORD0.zw)";
        public static string streamUV2AndAnimBlendText = "UV2 (TEXCOORD3.zw)";
        public static string streamUV3AndAnimBlendText = "UV3 (TEXCOORD3.zw)";
        public static string streamAnimBlendText = "AnimBlend (TEXCOORD3.x)";
        public static string streamTangentText = "Tangent (TANGENT.xyzw)";
        public static string streamCustom1Text = "Custom1.xyzw(TEXCOORD1.xyzw)";
        public static string streamCustom2Text = "Custom2.xyzw(TEXCOORD2.xyzw)";


        public static GUIContent streamApplyToAllSystemsText = new GUIContent("使粒子与材质顶点流相同",
            "Apply the vertex stream layout to all Particle Systems using this material");

        public static string undoApplyCustomVertexStreams = L10n.Tr("Apply custom vertex streams from material");
        
        List<ParticleSystemRenderer> m_ParticleRenderersUsingThisMaterial = new List<ParticleSystemRenderer>();
        List<Renderer> m_RenderersUsingThisMaterial = new List<Renderer>();

        private bool checkIsParicleSystem = false;
        void CacheRenderersUsingThisMaterial(Material material, int matID)
        {
            checkIsParicleSystem = false;
            m_ParticleRenderersUsingThisMaterial.Clear();
            m_RenderersUsingThisMaterial.Clear();
            // #if UNITY_2022_1_OR_NEWER
            // ParticleSystemRenderer[] renderers =
            //     UnityEngine.Object.FindObjectsByType(typeof(ParticleSystemRenderer),FindObjectsSortMode.None) as ParticleSystemRenderer[];
            // #else
            Renderer[] renderers =
                UnityEngine.Object.FindObjectsOfType(typeof(Renderer)) as Renderer[];//为了兼容性使用较慢版本
            m_RenderersUsingThisMaterial.Clear();
            // #endif
            foreach (Renderer renderer in renderers)
            {
                if (renderer is ParticleSystemRenderer)
                {
                    ParticleSystemRenderer psr = renderer as ParticleSystemRenderer;
                    if (psr.sharedMaterial == material || psr.trailMaterial == material)
                    {
                        checkIsParicleSystem = true;
                        shaderFlags[matID].ClearFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_ANIMATION_SHEET_HELPER,index:1);//如果是粒子系统用，就主动关掉Helper的类型。
                        m_ParticleRenderersUsingThisMaterial.Add(psr);
                    }
                }

                foreach (var mat in renderer.sharedMaterials)
                {
                    if (mat == material)
                    {
                        m_RenderersUsingThisMaterial.Add(renderer);
                        break;
                    }
                }
                
            }
            _helper.InitRenderers(m_RenderersUsingThisMaterial);
        }
        
        //雨轩：UnityEditorInternal命名空间下提供 一个类ReorderableList可以实现通过拖曳来达到列表元素的重新排序。
        private static ReorderableList vertexStreamList;
        //构建粒子系统顶点流界面
        public void DoVertexStreamsArea(Material material, List<ParticleSystemRenderer> renderers,
            int matID, bool useLighting = false)
        {
            EditorGUILayout.Space();
     
            // bool useFlipbookBlending = (material.GetFloat("_FlipbookBlending") > 0.0f);
            bool useFlipbookBlending = material.IsKeywordEnabled("_FLIPBOOKBLENDING_ON");
            bool useSpecialUVChannel = shaderFlags[matID].CheckIsUVModeOn(UVMode.SpecialUVChannel);
            bool isUseUV3ForSpecialUV =
                shaderFlags[matID].CheckFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_USE_TEXCOORD2, index:1);
            // bool CustomDataEnabled = (material.GetFloat("_CustomData") > 0.0f);
            bool isCustomData1 = shaderFlags[matID].CheckFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_CUSTOMDATA1_ON);
            bool isCustomData2 = shaderFlags[matID].CheckFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_CUSTOMDATA2_ON);
            

            // Build the list of expected vertex streams
            List<ParticleSystemVertexStream> streams = new List<ParticleSystemVertexStream>();
            List<string> streamList = new List<string>();
            
            streams.Add(ParticleSystemVertexStream.Position); //必然会传递有顶点位置信息
            streamList.Add(streamPositionText); //记录顶点位置信息，给GUI面板用

            bool needTangent = false;
            bool needNormal = false;

            needNormal = (material.GetFloat("_VertexOffset_NormalDir_Toggle") > 0.5f);
            
            //如果有灯光，必有法线信息。如果有法线贴图，必有顶点切线法线信息。
            //菲涅尔效果需要用到法线内容。
            if (material.GetFloat("_fresnelEnabled") > 0.5f)
            {
                needNormal = true;
                needTangent = true;
            }

            if (material.GetFloat("_ParallaxMapping_Toggle") > 0.5f)
            {
                needNormal = true;
                needTangent = true;
            }

            if (_fxLightMode != FxLightMode.UnLit || material.GetFloat("_BumpMapToggle") > 0.5f)
            {
                needNormal = true;
                needTangent = true;
            }

            bool useUV3AsMainUV = shaderFlags[matID].CheckFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_USETEXCOORD2);

            if (needTangent)
            {
                streams.Add(ParticleSystemVertexStream.Tangent);
                streamList.Add(streamTangentText);
            }

            if (needNormal)
            {
                streams.Add(ParticleSystemVertexStream.Normal);
                streamList.Add(streamNormalText);
            }

            //粒子着色器，必有顶点颜色信息。
            streams.Add(ParticleSystemVertexStream.Color);
            streamList.Add(streamColorText);

            //TEXCOORD0填充
            //必有顶点第一套UV信息。
            streams.Add(ParticleSystemVertexStream.UV);
            streamList.Add(streamUVText);
            //在做动画序列帧时，需要:TEXCOORD1(xy为正常uv，zw为Blend用的第二套uv)，:TEXCOORD2(x为Blend混合值)
            if (useFlipbookBlending && useSpecialUVChannel)
            {
                streams.Add(ParticleSystemVertexStream.UV2);
                streamList.Add(streamUV2Text);
            }
            else if (useSpecialUVChannel & !useFlipbookBlending)
            {
                 
                if (isUseUV3ForSpecialUV)
                {
                    streams.Add(ParticleSystemVertexStream.UV3);
                    streamList.Add(streamUV3Text);
                }
                else
                {
                    streams.Add(ParticleSystemVertexStream.UV2);
                    streamList.Add(streamUV2Text);
                }
                
            }
            else if (useFlipbookBlending & !useSpecialUVChannel)
            {
                if (!streams.Contains(ParticleSystemVertexStream.UV2))
                {
                    streams.Add(ParticleSystemVertexStream.UV2);
                    streamList.Add(streamUV2Text);
                }
            }
            else if(isCustomData1 || isCustomData2)
            {
                streams.Add(ParticleSystemVertexStream.UV2);
                streamList.Add(streamUV2Text);
            }


            //填充TEXCOORD1
            bool isFillSkipUV2 = false;//因为如果要使用UV3，粒子系统必须填充UV2才能激活
            if (isCustomData1 || isCustomData2 || useFlipbookBlending)
            {
                streams.Add(ParticleSystemVertexStream.Custom1XYZW);
                streamList.Add(streamCustom1Text);
            }
            else if(useSpecialUVChannel & isUseUV3ForSpecialUV)
            {
                streams.Add(ParticleSystemVertexStream.UV2);
                streamList.Add("TEXCOORD1.xy");
                isFillSkipUV2 = true;
            }

            //填充TEXCOORD2
            if (isCustomData2 || useFlipbookBlending)
            {
                streams.Add(ParticleSystemVertexStream.Custom2XYZW);
                streamList.Add(streamCustom2Text);
            }
            else if(useSpecialUVChannel & isUseUV3ForSpecialUV & !isFillSkipUV2)
            {
                streams.Add(ParticleSystemVertexStream.UV2);
                streamList.Add("TEXCOORD2.xy");
                isFillSkipUV2 = true;
            }

            //填充TEXCOORD3
            if (useFlipbookBlending)
            {
                streams.Add(ParticleSystemVertexStream.AnimBlend);
                streamList.Add(streamAnimBlendText);
                if (useSpecialUVChannel)
                {
                    if (isUseUV3ForSpecialUV)
                    {
                        streams.Add(ParticleSystemVertexStream.UV3);
                        streamList.Add(streamUV3AndAnimBlendText);
                    }
                }
            }
            else if(useSpecialUVChannel & isUseUV3ForSpecialUV & !isFillSkipUV2)
            {
                streams.Add(ParticleSystemVertexStream.UV2);
                streamList.Add("TEXCOORD3.xy");
            }


            //可排序列表绘制。
            //创建一个可排序列表
            vertexStreamList = new ReorderableList(streamList, typeof(string), false, true, false, false);

            //创建表头。ReorderableList下面还有很多回调。可以按需选择。
            vertexStreamList.drawHeaderCallback = (Rect rect) => { EditorGUI.LabelField(rect, "Vertex Streams"); };

            vertexStreamList.DoLayoutList(); //执行表格绘制。

            // Display a warning if any renderers have incorrect vertex streams
            string Warnings = "";
            List<ParticleSystemVertexStream> rendererStreams = new List<ParticleSystemVertexStream>();
            foreach (ParticleSystemRenderer renderer in renderers) //每个使用该材质的粒子系统都会进行比较
            {
                renderer.GetActiveVertexStreams(rendererStreams); //获得ParticleSystemRenderer的顶点流
                if (!rendererStreams.SequenceEqual(streams)) //重点！是否和我们拼装的顶点流一致。
                    Warnings += "-" + renderer.name + "\n";
            }

            //
            if (!string.IsNullOrEmpty(Warnings))
            {
                //如果有Warning
                EditorGUILayout.HelpBox(
                    "下面的粒子系统Renderer顶点流不正确:\n" +
                    Warnings, MessageType.Error, true);
                // Set the streams on all systems using this materialz
                if (GUILayout.Button("使粒子与材质顶点流相同", EditorStyles.miniButton,
                        GUILayout.ExpandWidth(true)))
                {
                    //做一个撤回记录。
                    Undo.RecordObjects(renderers.Where(r => r != null).ToArray(), "Apply custom vertex streams from material");

                    //重点！直接赋值我们拼装好的顶点流。
                    foreach (ParticleSystemRenderer renderer in renderers)
                    {
                        renderer.SetActiveVertexStreams(streams);
                        
                    }
                }
            }
            
            //从2022.3.11开始添加这个功能。
            #if UNITY_2022_3_OR_NEWER && !(UNITY_2022_3_0 ||UNITY_2022_3_1||UNITY_2022_3_2||UNITY_2022_3_3||UNITY_2022_3_4||UNITY_2022_3_5||UNITY_2022_3_6||UNITY_2022_3_7||UNITY_2022_3_8||UNITY_2022_3_9||UNITY_2022_3_10)
            // Display a warning if any renderers have incorrect vertex streams
            string trailWarnings = "";
            List<ParticleSystemVertexStream> trailRendererStreams = new List<ParticleSystemVertexStream>();
            foreach (ParticleSystemRenderer renderer in renderers) //每个使用该材质的粒子系统都会进行比较
            {
                renderer.GetActiveTrailVertexStreams(trailRendererStreams); //获得ParticleSystemRenderer的顶点流
                if (!trailRendererStreams.SequenceEqual(streams)) //重点！是否和我们拼装的顶点流一致。
                    trailWarnings += "-" + renderer.name + "\n";
            }
            
            if (!string.IsNullOrEmpty(trailWarnings))
            {
                //如果有Warning
                EditorGUILayout.HelpBox(
                    "下面的粒子系统Renderer拖尾顶点流不正确:\n" +
                    trailWarnings, MessageType.Error, true);
                // Set the streams on all systems using this material
                if (GUILayout.Button("使粒子拖尾与材质顶点流相同", EditorStyles.miniButton,
                        GUILayout.ExpandWidth(true)))
                {
                    //做一个撤回记录。
                    Undo.RecordObjects(renderers.Where(r => r != null).ToArray(), "Apply custom vertex streams from material");

                    //重点！直接赋值我们拼装好的顶点流。
                    foreach (ParticleSystemRenderer renderer in renderers)
                    {
                        renderer.SetActiveTrailVertexStreams(streams);
                    }
                }
            }
            #endif
            
        }

        private string[] _customDataOptions =
        {
            "**不使用**",
            "CustomData1_X",
            "CustomData1_Y",
            "CustomData1_Z",
            "CustomData1_W",
            "CustomData2_X",
            "CustomData2_Y",
            "CustomData2_Z",
            "CustomData2_W"
        };


        bool CustomDataHasMixedValue(int dataBitPos, int dataIndex)
        {
            W9ParticleShaderFlags.CutomDataComponent
                component = W9ParticleShaderFlags.CutomDataComponent.UnKnownOrMixed;
            for (int i = 0; i < shaderFlags.Count; i++)
            {
                W9ParticleShaderFlags.CutomDataComponent curComponent =
                    shaderFlags[i].GetCustomDataFlag(dataBitPos, dataIndex);
                if (i == 0)
                {
                    component = curComponent;
                }
                else
                {
                    if (component != curComponent) return true;
                }
            }

            return false;
        }
        public void DrawCustomDataSelect(string label, int dataBitPos, int dataIndex)
        {
            // if(!_isUseParticleSystem)return;//只有粒子系统才会处理相关内容。
            // if (mats.Count != 1) return; //仅单选触发
            
            if(_meshSourceModeIsParticle <=0 ) return;
            (string, string) nameTuple = (label, "");
            //-------------这里需要处理多选情况--------------
            EditorGUI.showMixedValue = CustomDataHasMixedValue(dataBitPos, dataIndex);
            W9ParticleShaderFlags.CutomDataComponent component = shaderFlags[0].GetCustomDataFlag(dataBitPos, dataIndex);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginHorizontal();
            component = (W9ParticleShaderFlags.CutomDataComponent)EditorGUILayout.Popup(new GUIContent(label), (int)component, _customDataOptions);
            EditorGUI.showMixedValue = false;
            Action customDataDrawEndChangeCheck = () =>
            {
                for (int i = 0; i < shaderFlags.Count; i++)
                {
                    shaderFlags[i].SetCustomDataFlag(component,dataBitPos,dataIndex);
                }
                _helper.ResetTool.CheckOnValueChange(nameTuple);
            };
            if (EditorGUI.EndChangeCheck())
            {
                customDataDrawEndChangeCheck();
            }
            _helper.ResetTool.DrawResetModifyButton(new Rect(),nameTuple,
                resetCallBack:()=>
                {
                    component = 0;
                    customDataDrawEndChangeCheck();
                },onValueChangedCallBack:customDataDrawEndChangeCheck,
                checkHasModifyOnValueChange: () => shaderFlags[0].GetCustomDataFlag(dataBitPos, dataIndex)!= 0 ,
                checkHasMixedValueOnValueChange:()=>CustomDataHasMixedValue(dataBitPos, dataIndex));
            EditorGUILayout.EndHorizontal();
            _helper.ResetTool.EndResetModifyButtonScope();
            
        }

    
        private string[] _uvModeNames =
        {
            "默认UV通道",
            "特殊UV通道",
            "极坐标|旋转",
            "圆柱无缝",
            "主贴图",
            "屏幕UV",
            "世界坐标",
            "局部本地坐标",
            "公共UV"
        };
        
        private string[] _posUVModeNames =
        {
            "xy平面",
            "xz平面",
            "yz平面",
        };
        
        enum SpecialUVChannelMode
        {
            UV2_Texcoord1,
            UV3_Texcoord2
        }

        bool UvModeHasMixedValue(int uvModeBitPos, int uvModeFlagIndex)
        {
            UVMode uvMode = UVMode.UnknownOrMixed;
            for (int i = 0; i < shaderFlags.Count; i++)
            {
                if (i == 0)
                {
                    uvMode = shaderFlags[i].GetUVMode(uvModeBitPos, uvModeFlagIndex);
                }
                else
                {
                    if (uvMode != shaderFlags[i].GetUVMode(uvModeBitPos, uvModeFlagIndex))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public void DrawUVModeSelect(int foldOutFlagBit, int foldOutFlagIndex,string label, int uvModeBitPos, int uvModeFlagIndex,MaterialProperty textureProp = null,bool forceEnable = false)
        {
            // if(textureProp.hasMixedValue) return;
            if(forceEnable)
            {
                EditorGUI.BeginDisabledGroup(false);
            }
            else if (textureProp != null)
            {
                EditorGUI.BeginDisabledGroup(!textureProp.textureValue);
            }
            else
            {
                EditorGUI.BeginDisabledGroup(false);
            }
            bool uvModeHasMixedValue = UvModeHasMixedValue(uvModeBitPos, uvModeFlagIndex);
            EditorGUI.showMixedValue = uvModeHasMixedValue;
            (string, string) wrapModeNameTuple = (label, "");
            
            EditorGUILayout.BeginHorizontal();
            

            Rect rect = EditorGUILayout.GetControlRect();
            var labelRect = new Rect(rect.x , rect.y, rect.width, rect.height);
            var popUpRect = _helper.GetRectAfterLabelWidth(rect,true);
            
            bool isChangeUVMode = false;
            EditorGUI.BeginChangeCheck();
            UVMode uvMode = shaderFlags[0].GetUVMode(uvModeBitPos, uvModeFlagIndex);

            Action drawUVModeEndChangeCheck = () =>
            {
                isChangeUVMode = true;
                for (int i = 0; i < shaderFlags.Count; i++)
                {
                    shaderFlags[i].SetUVMode(uvMode, uvModeBitPos, uvModeFlagIndex);
                }
                _helper.ResetTool.CheckOnValueChange(wrapModeNameTuple);
            };
            uvMode = (UVMode) EditorGUI.Popup(popUpRect, (int)uvMode, _uvModeNames);
            if (EditorGUI.EndChangeCheck())
            {
                drawUVModeEndChangeCheck();
            }

            bool needFoldOut = uvMode != UVMode.DefaultUVChannel && uvMode != UVMode.CommonUV
                                    && uvMode != UVMode.ScreenUV && uvMode != UVMode.MainTex;
            bool foldOutState = shaderFlags[0].CheckFlagBits(foldOutFlagBit, index: foldOutFlagIndex);
            AnimBool animBool = _helper.GetAnimBool(foldOutFlagBit, foldOutFlagIndex-3, foldOutFlagIndex);
            animBool.target = foldOutState;
            if (!uvModeHasMixedValue && !needFoldOut)
            {
                animBool.target = false;
            }
            else
            {
                animBool.target =  EditorGUI.Foldout(rect, animBool.target, string.Empty, true);
                if (isChangeUVMode)
                {
                    animBool.target = true;
                }
            }
            foldOutState = animBool.target;
            if (foldOutState)
            {
                shaderFlags[0].SetFlagBits(foldOutFlagBit, index: foldOutFlagIndex);
            }
            else
            {
                shaderFlags[0].ClearFlagBits(foldOutFlagBit, index: foldOutFlagIndex);
            }
            EditorGUI.LabelField(labelRect,label);
            
            _helper.ResetTool.DrawResetModifyButton(new Rect(),wrapModeNameTuple,
            resetCallBack: () =>
            {
                uvMode = 0;
                drawUVModeEndChangeCheck();
            },onValueChangedCallBack: drawUVModeEndChangeCheck,checkHasModifyOnValueChange: () =>
            {
                return shaderFlags[0].GetUVMode(uvModeBitPos, uvModeFlagIndex) != 0;
            },checkHasMixedValueOnValueChange:()=>UvModeHasMixedValue(uvModeBitPos, uvModeFlagIndex));
            EditorGUILayout.EndHorizontal();
            _helper.ResetTool.EndResetModifyButtonScope();
            
          

            EditorGUI.showMixedValue = false;

            if (needFoldOut||_helper.ResetTool.IsInitResetData)
            {
                if (!uvModeHasMixedValue)
                {
                    EditorGUI.indentLevel++;
                   
                    float faded = animBool.faded;
                    if (faded == 0) faded = 0.0001f;
                    EditorGUILayout.BeginFadeGroup(faded);
                    EditorGUILayout.LabelField("以下设置材质内通用:",EditorStyles.boldLabel);
                    

                    Action drawSpecialUVChannel = () =>
                    {
                        _helper.DrawPopUp("特殊UV通道选择","_SpecialUVChannelMode",  Enum.GetNames(typeof(SpecialUVChannelMode)),
                            drawOnValueChangedBlock:
                            specialUVChannelMode =>
                            {
                                //在OnValueChange的时候。就已经是一起Set了。
                                SpecialUVChannelMode spUVMode = (SpecialUVChannelMode)specialUVChannelMode.floatValue;
                                for (int i = 0; i < shaderFlags.Count; i++)
                                {
                                    switch (spUVMode)
                                    {
                                        case SpecialUVChannelMode.UV2_Texcoord1:
                                            shaderFlags[i].SetFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_USE_TEXCOORD1,index:1);
                                            shaderFlags[i].ClearFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_USE_TEXCOORD2,index:1);
                                            break;
                                        case SpecialUVChannelMode.UV3_Texcoord2:
                                            shaderFlags[i].ClearFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_USE_TEXCOORD1,index:1);
                                            shaderFlags[i].SetFlagBits(W9ParticleShaderFlags.FLAG_BIT_PARTICLE_1_USE_TEXCOORD2,index:1);
                                            break;
                                        //TODO:如果所有UVMode都没有开启，需要都Clear。
                                    }
                                }
                            },isSharedGlobalParent:true);
                    };

                    Action drawPolarOrTwirl = () =>
                    {
                        _helper.DrawToggleFoldOut(W9ParticleShaderFlags.foldOutBitTwril,3,GetAnimBoolIndex(3),"旋转扭曲","_UTwirlEnabled",flagBitsName:W9ParticleShaderFlags.FLAG_BIT_PARTICLE_UTWIRL_ON,isSharedGlobalParent:true,drawBlock:(isToggle) =>{
                            _helper.DrawVector4In2Line("_TWParameter","旋转扭曲中心",true);
                            _helper.DrawFloat("旋转扭曲强度","_TWStrength");
                        });

                        _helper.DrawToggleFoldOut(W9ParticleShaderFlags.foldOutBitPolar,3,GetAnimBoolIndex(3),"极坐标", "_PolarCoordinatesEnabled",W9ParticleShaderFlags.FLAG_BIT_PARTICLE_POLARCOORDINATES_ON,isSharedGlobalParent:true,drawBlock:(isToggle) =>{
                            // _helper.DrawToggle("极坐标只影响特殊功能","_PolarCordinateOnlySpecialFunciton_Toggle",W9ParticleShaderFlags.FLAG_BIT_PARTICLE_PC_ONLYSPECIALFUNC);
                            _helper.DrawVector4In2Line("_PCCenter","极坐标中心",true);
                            _helper.DrawVector4Component("极坐标强度","_PCCenter","z",true,0f,1f);
                        });
                    };
                    
                    Action drawCylinderUV = () =>
                    {
                        // EditorGUILayout.LabelField("圆柱坐标模式尚未开发完成！");
                        EditorGUILayout.LabelField("圆柱模式消耗比较大，慎用");
                        _helper.DrawVector4XYZComponet("圆柱坐标旋转","_CylinderUVRotate");
                        _helper.DrawVector4XYZComponet("圆柱坐标偏移","_CylinderUVPosOffset");
                        Matrix4x4 cylinderMatrix =
                            Matrix4x4.Translate(_helper.GetProperty("_CylinderUVPosOffset").vectorValue) *
                            Matrix4x4.Rotate(Quaternion.Euler(_helper.GetProperty("_CylinderUVRotate").vectorValue));
                        _helper.GetProperty("_CylinderMatrix0").vectorValue =cylinderMatrix.GetRow(0);
                        _helper.GetProperty("_CylinderMatrix1").vectorValue =cylinderMatrix.GetRow(1);
                        _helper.GetProperty("_CylinderMatrix2").vectorValue =cylinderMatrix.GetRow(2);
                        _helper.GetProperty("_CylinderMatrix3").vectorValue =cylinderMatrix.GetRow(3);
                    };

                    Action drawWorldPosUV = () =>
                    {
                        _helper.DrawPopUp("坐标平面", "_WorldSpaceUVModeSelector", _posUVModeNames);
                    };
                    
                    Action drawObjectPosUV = () =>
                    {
                        _helper.DrawPopUp("坐标平面", "_ObjectSpaceUVModeSelector", _posUVModeNames);
                    };

                    if (_helper.ResetTool.IsInitResetData)
                    {
                        drawSpecialUVChannel();
                        drawPolarOrTwirl();
                        drawWorldPosUV();
                        drawObjectPosUV();
                    }
                    else
                    {
                        switch (uvMode)
                        {
                            case UVMode.SpecialUVChannel:
                                drawSpecialUVChannel();
                                break;
                            case UVMode.PolarOrTwirl:
                                drawPolarOrTwirl();
                                break;
                            case UVMode.Cylinder:
                                drawCylinderUV();
                                break;
                            case UVMode.WorldPos:
                                drawWorldPosUV();
                                break;
                            case UVMode.ObjectPos:
                                drawObjectPosUV();
                                break;
                        }
                    }

                    EditorGUILayout.EndFadeGroup();
                }
                EditorGUI.indentLevel--;
            }
            EditorGUI.EndDisabledGroup();

        }

        private string[] _meshSourceModeNames =
        {
            "粒子系统",
            "模型（非粒子发射）",
            "2D RawImage",
            "2D 精灵",
            "2D 材质贴图",
            "2D UIParticle"
        };

        enum MeshSourceMode
        {
            Particle,
            Mesh,
            UIEffectRawImage,
            UIEffectSprite,
            UIEffectBaseMap,
            UIParticle,
            UnKnowOrMixed = -1
        }

        bool ColorChannelHasMixedValue(int colorChannelBitPos)
        {
            W9ParticleShaderFlags.ColorChannel colorChannel = W9ParticleShaderFlags.ColorChannel.UnKnownOrMixedValue;
            for (int i = 0; i < shaderFlags.Count; i++)
            {
                W9ParticleShaderFlags.ColorChannel curChannel = shaderFlags[i].GetColorChanel(colorChannelBitPos);
                if (i == 0)
                {
                    colorChannel = curChannel;
                }
                else
                {
                    if(colorChannel != curChannel) return true;
                }
            }

            return false;
        }

        private string[] _colorChannelNames = { "R", "G", "B", "A" };

        public void DrawColorChannelSelect(string label, int colorChannelBitPos,int defaultChannel = 0)
        {
            bool hasMixedValue = ColorChannelHasMixedValue(colorChannelBitPos);
            EditorGUI.showMixedValue = hasMixedValue;
            (string, string) nameTuple = (label, "");

            W9ParticleShaderFlags.ColorChannel chanel = shaderFlags[0].GetColorChanel(colorChannelBitPos);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginHorizontal();
            int index = EditorGUILayout.Popup(label, (int)chanel,
                _colorChannelNames);
            Action colorChannelOnEndChangeCheck = () =>
            {
                for (int i = 0; i < shaderFlags.Count; i++)
                {
                    shaderFlags[i].SetColorChanel((W9ParticleShaderFlags.ColorChannel)index,colorChannelBitPos);
                }
                _helper.ResetTool.CheckOnValueChange(nameTuple);
            };
            if (EditorGUI.EndChangeCheck())
            {
                colorChannelOnEndChangeCheck();
            }
            EditorGUI.showMixedValue = false;
            _helper.ResetTool.DrawResetModifyButton(new Rect(),nameTuple,
                resetCallBack: () => { index = defaultChannel; }, 
                onValueChangedCallBack: colorChannelOnEndChangeCheck,
                checkHasModifyOnValueChange: () => { return shaderFlags[0].GetColorChanel(colorChannelBitPos) != (W9ParticleShaderFlags.ColorChannel)defaultChannel;},
                checkHasMixedValueOnValueChange:()=>ColorChannelHasMixedValue(colorChannelBitPos));
            EditorGUILayout.EndHorizontal();
            _helper.ResetTool.EndResetModifyButtonScope();
        }
        
        bool PNoiseBlendModeHasMixedValue(int pNoiseBlendModeBitPos)
        {
            W9ParticleShaderFlags.PNoiseBlendMode pNoiseBlendMode = W9ParticleShaderFlags.PNoiseBlendMode.UnKnownOrMixedValue;
            for (int i = 0; i < shaderFlags.Count; i++)
            {
                W9ParticleShaderFlags.PNoiseBlendMode curMode = shaderFlags[i].GetPNoiseBlendMode(pNoiseBlendModeBitPos);
                if (i == 0)
                {
                    pNoiseBlendMode = curMode;
                }
                else
                {
                    if(pNoiseBlendMode != curMode) return true;
                }
            }

            return false;
        }
        
        private string[] _pNoiseBlendModeNames = 
            {   
                "不使用" ,
                "Multiply" ,
                "Min" ,
                "HardLight"
            };

        void DrawPNoiseBlendModeSelect(string label, int pNoiseBlendModeBitPos,string opacityPropertyName)
        {
            if(_pNoiseEnabled < 0.5 && !_helper.ResetTool.IsInitResetData) return;
            bool hasMixedValue = PNoiseBlendModeHasMixedValue(pNoiseBlendModeBitPos);
            EditorGUI.showMixedValue = hasMixedValue;
            (string, string) nameTuple = (label, "");
            
            W9ParticleShaderFlags.PNoiseBlendMode blendMode = shaderFlags[0].GetPNoiseBlendMode(pNoiseBlendModeBitPos);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginHorizontal();
            int index = EditorGUILayout.Popup(label+"程序噪波混合", (int)blendMode, _pNoiseBlendModeNames);
            Action pNoiseBlendModeOnEndChangeCheck = () =>
            {
                for (int i = 0; i < shaderFlags.Count; i++)
                {
                    shaderFlags[i].SetPNoiseBlendMode((W9ParticleShaderFlags.PNoiseBlendMode)index,pNoiseBlendModeBitPos);
                }
                _helper.ResetTool.CheckOnValueChange(nameTuple);
            };
            if (EditorGUI.EndChangeCheck())
            {
                pNoiseBlendModeOnEndChangeCheck();
            }
            EditorGUI.showMixedValue = false;
            _helper.ResetTool.DrawResetModifyButton(new Rect(),nameTuple,
                resetCallBack: () => { index = (int)W9ParticleShaderFlags.PNoiseBlendMode.NotUse; }, 
                onValueChangedCallBack: pNoiseBlendModeOnEndChangeCheck,
                checkHasModifyOnValueChange: () => { return shaderFlags[0].GetPNoiseBlendMode(pNoiseBlendModeBitPos) != W9ParticleShaderFlags.PNoiseBlendMode.NotUse;},
                checkHasMixedValueOnValueChange:()=>PNoiseBlendModeHasMixedValue(pNoiseBlendModeBitPos));
            EditorGUILayout.EndHorizontal();
            _helper.ResetTool.EndResetModifyButtonScope();
            
            _helper.DrawSlider("程序噪波混合强度",opacityPropertyName);
            
        }

        int GetAnimBoolIndex(int foldOutFlagIndex)
        {
            return foldOutFlagIndex - 3;
        }
        
        // private static readonly FieldInfo _validKeywordsField = typeof(Material)
        //     .GetField("m_ValidKeywords", BindingFlags.NonPublic | BindingFlags.Instance);
        //
        // public static string[] GetValidKeywordsDirect(Material material)
        // {
        //     if (_validKeywordsField == null) 
        //     {
        //         Debug.LogError("m_ValidKeywords field not found!");
        //         return null;
        //     }
        //
        //     var keywords = _validKeywordsField.GetValue(material) as string[];
        //     return keywords;
        // }
    }
}