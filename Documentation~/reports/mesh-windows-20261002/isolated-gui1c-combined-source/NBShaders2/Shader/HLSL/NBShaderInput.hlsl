#ifndef NBSHADER_INPUT
    #define NBSHADER_INPUT
    
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
    #include "NBShaderFlags.hlsl"
    #include "NBShaderGeometryV1.hlsl"
    #include "NBShaderDistortionV1.hlsl"
    #include "NBShaderUVV1.hlsl"
    #include "NBShaderUVV2.hlsl"
    #include "NBShaderPackedGradientV1.hlsl"
   
    #if defined(_PROGRAM_NOISE) && (defined(_PROGRAM_NOISE_SIMPLE) || defined(_PROGRAM_NOISE_VORONOI))
        #define _PROGRAM_NOISE_ACTIVE
    #endif


  //---------------particleInput-------------------
    CBUFFER_START(UnityPerMaterial)
    float4 _SoftParticleFadeParams;
    float4 _CameraFadeParams;
    // #ifdef _INTERSECT_ON
        float _IntersectRadius;
        half4 _IntersectColor;
    // #endif
    half _AdditiveToPreMultiplyAlphaLerp;
    half _Saturability;
    half _HueShift;
    half _Contrast;
    half3 _ContrastMidColor;
    half4 _BaseMapColorRefine;
    half _AlphaAll;
    float4 _BaseMap_ST;
    float4 _BaseMap_AnimationSheetBlend_ST;//20240826 暂时只是给AnimationSheetHelper用。
    half _AnimationSheetHelperBlendIntensity;
    float4 _MaskMap_ST;
    half4 _BaseColor;
    half4 _BaseBackColor;
    half _BaseColorIntensityForTimeline;
    half4 _EmissionMap_ST;
    half4 _NoiseMap_ST;
    half4 _NoiseMaskMap_ST;
    half4 _DistortionDirection;
    half4 _BaseColorAddSubDiff;
    half _fogintensity;
    half _Emi_Distortion_intensity;
    half _BaseMapUVRotation;
    half _BaseMapUVRotationSpeed;
    float _MaskMapUVRotation;
    float _NoiseMapUVRotation;
    half _ScreenDistortIntensity;
    half _ScreenDistortAlphaPow;
    half _ScreenDistortAlphaAdd;
    half _ScreenDistortAlphaMulti;
    half _NoiseIntensity;
    half _RefractionIOR;
    half _uvRapSoft;
    half4 _EmissionMapColor;
    half _EmissionMapColorIntensity;
    

    //--------------光照部分-------------
    float4 _BumpTex_ST;
    half _BumpScale;
    half4 _MaterialInfo;
    half4 _SpecularColor;
	//-----------SixWayLight----------
    half4 _SixWayInfo;
    half4 _SixWayEmissionColor;

    half4 _MatCapColor;
    half4 _MatCapInfo;

    half _EdgeFade;
    half4 _NoiseOffset;
    half4 _EmissionMapUVOffset;
    half _EmissionMapUVRotation;
    half _EmissionSelfAlphaWeight;
    half _TexDistortion_intensity;
    // //half _RJ_Distortion_intensity;
    // half _XianXingCH_UVRota;
    // half _jingxiangCH_dire;

    half _MaskDistortion_intensity;
    half4 _BaseMapMaskMapOffset;
    half4 _MaskMapOffsetAnition;
    half4 _MaskMap3OffsetAnition;
    half4 _MaskMapVec;
    half4 _MaskRefineVec;

    int _MaskMapGradientCount;
    half4 _MaskMapGradientFloat0;
    half4 _MaskMapGradientFloat1;
    half4 _MaskMapGradientFloat2;
    int _MaskMap2GradientCount;
    half4 _MaskMap2GradientFloat0;
    half4 _MaskMap2GradientFloat1;
    half4 _MaskMap2GradientFloat2;
    int _MaskMap3GradientCount;
    half4 _MaskMap3GradientFloat0;
    half4 _MaskMap3GradientFloat1;
    half4 _MaskMap3GradientFloat2;

    float4 _PCCenter;
    float4 _TWParameter;
    float _TWStrength;
    float4 _Fade;
    float _OverrideZValue;
    float _MaskMapRotationSpeed;
    

    half _FrePower;
    half _FresnelInOutSlider;
    half4 _FresnelRotation;
    half _FresnelSelfAlphaWeight;
    half4 _FresnelUnit;
    half4 _FresnelUnit2;
    half4 _DepthOutline_Vec;
    half4 _DepthOutline_Color;
    half4 _FresnelColor;
    half4 _ColorA;
    float4 _ClipRect;

    float4 _CylinderMatrix0;
    float4 _CylinderMatrix1;
    float4 _CylinderMatrix2;
    float4 _CylinderMatrix3;
   
    half4 _Color;
    float4 _UI_MainTex_ST;//在UI中，RawImage组件的功能和正常的TexST不一致，所以这里使用另外传的方式。
    float4 _MainTex_Reverse_ST;

    half _Cutoff;

    float4 _MaskMap2_ST;
    float4 _MaskMap3_ST;   

    float time;
    half _FresnelFadeDistance;

    half4 LB_RT;

    half4 _Dissolve;
    half4 _DissolveMap_ST;
    half4 _DissolveOffsetRotateDistort;
    half4 _DissolveMaskMap_ST;
    half _DissolveMaskMode;

    half4 _DissolveLineColor;
    half4 _DissolveRampColor;
    float4 _DissolveVoronoi_Vec;
    half4 _DissolveVoronoi_Vec2;
    float4 _DissolveVoronoi_Vec3;
    float4 _DissolveVoronoi_Vec4;
    half4 _DissolveRampMap_ST;
    half4 _Dissolve_Vec2;
    half _ProgramNoise_Rotate;

    half4 _SharedUV_ST;
    half4 _SharedUV_Vec;
    // half _SharedUV_Distort_Intensity;

    half4 _ColorBlendMap_ST;
    float4 _ColorBlendMapOffset;
    half4 _ColorBlendColor;
    half4 _ColorBlendVec;

    half4 _RampColor0;
    half4 _RampColor1;
    half4 _RampColor2;
    half4 _RampColor3;
    half4 _RampColor4;
    half4 _RampColor5;
    half4 _RampColorAlpha0;
    half4 _RampColorAlpha1;
    half4 _RampColorAlpha2;
    uint _RampColorCount;
    half4 _RampColorBlendColor;
    float4 _RampColorMapOffset;
    half4 _RampColorMap_ST;

    half4 _DissolveRampColor0;
    half4 _DissolveRampColor1;
    half4 _DissolveRampColor2;
    half4 _DissolveRampColor3;
    half4 _DissolveRampColor4;
    half4 _DissolveRampColor5;
    half4 _DissolveRampAlpha0;
    half4 _DissolveRampAlpha1;
    half4 _DissolveRampAlpha2;
    uint _DissolveRampCount;


    half3  _VertexOffset_Vec;
    half3 _VertexOffset_CustomDir;
    float _VertexOffset_NormalDir_Toggle;
    float _VertexOffset_DirectionSpace;
    half4 _VertexOffset_Map_ST;

    half4 _VertexOffset_MaskMap_ST;
    half3 _VertexOffset_MaskMap_Vec;

    float _VAT_Toggle;
    float _VATMode;
    float _ImportScale;
    float _TyFlowVATSubMode;
    float _DeformingSkin;
    float _SkinBoneCount;
    float _RGBAEncoded;
    float _RGBAHalf;
    float _LinearToGamma;
    float _VATIncludesNormals;
    float _AffectsShadows;
    float _Frame;
    float _Frames;
    float _FrameInterpolation;
    float _Loop;
    float _InterpolateLoop;
    float _Autoplay;
    float _AutoplaySpeed;

    half _ParallaxMapping_Intensity;
    half4 _ParallaxMapping_Map_ST;
    half4 _ParallaxMapping_Vec;
    half _WorldSpaceUVModeSelector;
    half _ObjectSpaceUVModeSelector;

    uint _W9ParticleShaderFlags;

    uint _W9ParticleShaderFlags1;

    uint _W9ParticleShaderWrapFlags;

    uint _NBShaderForceNoMipFlags;

    uint _W9ParticleCustomDataFlag0;
    uint _W9ParticleCustomDataFlag1;
    uint _W9ParticleCustomDataFlag2;
    uint _W9ParticleCustomDataFlag3;

    uint _UVModeFlag0;
    uint _UVModeFlagType0;
  

    uint _W9ParticleShaderColorChannelFlag;
    uint _W9ParticleShaderPNoiseBlendFlag;
    half _ProgramNoiseBaseBlendOpacity;
    half _MaskPNoiseBlendOpacity;
    half _DissolvePNoiseBlendOpacity;
    half _DistortPNoiseBlendOpacity;
    float4x4 _CustomLocalTransformLocalToWorld;
    float4x4 _CustomLocalTransformWorldToLocal;

    half _EmissionAlphaIntensity;
    half _ColorBlendColorIntensity;

    CBUFFER_END

    #define NB_SHADER_FLAGS _W9ParticleShaderFlags
    #define NB_SHADER_FLAGS1 _W9ParticleShaderFlags1
    #define NB_SHADER_WRAP_FLAGS _W9ParticleShaderWrapFlags
    #define NB_SHADER_FORCE_NO_MIP_FLAGS _NBShaderForceNoMipFlags
    #define NB_SHADER_COLOR_CHANNEL_FLAG _W9ParticleShaderColorChannelFlag
    #define NB_SHADER_PNOISE_BLEND_FLAG _W9ParticleShaderPNoiseBlendFlag
    #define NB_CUSTOM_DATA_FLAG_0 _W9ParticleCustomDataFlag0
    #define NB_CUSTOM_DATA_FLAG_1 _W9ParticleCustomDataFlag1
    #define NB_CUSTOM_DATA_FLAG_2 _W9ParticleCustomDataFlag2
    #define NB_CUSTOM_DATA_FLAG_3 _W9ParticleCustomDataFlag3

    float3x3 GetCustomLocalToWorld3x3()
    {
        return (float3x3)_CustomLocalTransformLocalToWorld;
    }

    float3x3 GetCustomWorldToLocal3x3()
    {
        return (float3x3)_CustomLocalTransformWorldToLocal;
    }

    float3 TransformWorldToObject_NB(float3 positionWS)
    {
        #ifdef _CUSTOM_LOCAL_TRANSFORM
            return mul(_CustomLocalTransformWorldToLocal, float4(positionWS, 1.0)).xyz;
        #else
            return mul(unity_WorldToObject, float4(positionWS, 1.0)).xyz;
        #endif
    }

    float3 TransformObjectToWorld_NB(float3 positionOS)
    {
        #ifdef _CUSTOM_LOCAL_TRANSFORM
            return mul(_CustomLocalTransformLocalToWorld, float4(positionOS, 1.0)).xyz;
        #else
            return mul(unity_ObjectToWorld, float4(positionOS, 1.0)).xyz;
        #endif
    }

    float4 TransformObjectToHClip_NB(float3 positionOS)
    {
        #ifdef _CUSTOM_LOCAL_TRANSFORM
            return TransformWorldToHClip(TransformObjectToWorld_NB(positionOS));
        #else
            return TransformObjectToHClip(positionOS);
        #endif
    }

    float3 TransformWorldToObjectDir_NB(float3 dirWS, bool doNormalize = true)
    {
        #ifdef _CUSTOM_LOCAL_TRANSFORM
            float3 dirOS = mul(GetCustomWorldToLocal3x3(), dirWS);
            return doNormalize ? SafeNormalize(dirOS) : dirOS;
        #else
            float3 dirOS = mul((float3x3)unity_WorldToObject, dirWS);
            return doNormalize ? SafeNormalize(dirOS) : dirOS;
        #endif
    }

    float3 TransformWorldToObjectNormal_NB(float3 normalWS)
    {
        #ifdef _CUSTOM_LOCAL_TRANSFORM
            return SafeNormalize(mul(normalWS, GetCustomLocalToWorld3x3()));
        #else
            return SafeNormalize(mul(normalWS, (float3x3)unity_ObjectToWorld));
        #endif
    }

    float3 TransformObjectToWorldDir_NB(float3 dirOS, bool doNormalize = true)
    {
        #ifdef _CUSTOM_LOCAL_TRANSFORM
            float3 dirWS = mul(GetCustomLocalToWorld3x3(), dirOS);
            return doNormalize ? SafeNormalize(dirWS) : dirWS;
        #else
            return TransformObjectToWorldDir(dirOS, doNormalize);
        #endif
    }

    float3 TransformObjectToWorldNormal_NB(float3 normalOS)
    {
        #ifdef _CUSTOM_LOCAL_TRANSFORM
            return SafeNormalize(mul(normalOS, GetCustomWorldToLocal3x3()));
        #else
            return TransformObjectToWorldNormal(normalOS);
        #endif
    }

    real3 NBTransformWorldToTangentDir(real3 dirWS, real3x3 tangentToWorld, bool doNormalize = false)
    {
        // Unity 2021 / URP 12 does not expose TransformWorldToTangentDir.
        real3 result = mul(tangentToWorld, dirWS);
        return doNormalize ? SafeNormalize(result) : result;
    }

    float3 GetCustomLocalSpaceNormalizeViewDir(float3 positionOS)
    {
        #ifdef _CUSTOM_LOCAL_TRANSFORM
            float3 viewDir = TransformWorldToObject_NB(_WorldSpaceCameraPos) - positionOS;
            return SafeNormalize(viewDir);
        #else
            return GetObjectSpaceNormalizeViewDir(positionOS);
        #endif
    }

    float GetCustomLocalOddNegativeScale()
    {
        #ifdef _CUSTOM_LOCAL_TRANSFORM
            return determinant(GetCustomLocalToWorld3x3()) < 0.0 ? -1.0 : 1.0;
        #else
            return GetOddNegativeScale();
        #endif
    }


    bool CheckLocalFlags(uint bits)
    {
        return (_W9ParticleShaderFlags&bits) != 0;
    }
    bool CheckLocalFlags1(uint bits)
    {
        return (_W9ParticleShaderFlags1&bits) != 0;
    }
    int CheckLocalWrapFlags(uint bits)
    {
        bool bit0 = (_W9ParticleShaderWrapFlags&bits) != 0;
        bool bit1 = (_W9ParticleShaderWrapFlags&(bits<<16)) != 0;
        if(!bit0 && !bit1)
        {
            return 0;
        }
        else if(bit0 && !bit1)
        {
            return 1;
        }
        else if(!bit0 && bit1)
        {
             return 2;
        }
        else if(bit0 && bit1)
        {
            return 3;
        }
        else
        {
            return -1;
        }
    }

    bool CheckForceNoMipFlags(uint bits)
    {
        return (_NBShaderForceNoMipFlags & bits) != 0;
    }


    SamplerState sampler_linear_repeat;
    SamplerState sampler_linear_clamp;
    SamplerState sampler_linear_RepeatU_ClampV;
    SamplerState sampler_linear_ClampU_RepeatV;
    SamplerState sampler_point_clamp;



    half4 SampleTexture2D(Texture2D tex,float2 uv,SAMPLER(textureSampler),bool forceLod0)
    {
        half4 sample;
        UNITY_BRANCH
        if (forceLod0)
        {
            sample = SAMPLE_TEXTURE2D_LOD(tex,textureSampler,uv,0);
        }
        else
        {
            sample = SAMPLE_TEXTURE2D(tex,textureSampler,uv);
        }

        return sample;
    }
    

    half4 SampleTexture2DWithWrapFlags(Texture2D tex,float2 uv,uint bits,bool forceLod0)
    {
        #ifdef _CAMERA_OPAQUE_DISTORT_PASS
        int wrapMode;
        if (bits == FLAG_BIT_WRAPMODE_BASEMAP)
        {
            wrapMode = 1;
        }
        else
        {
            wrapMode = CheckLocalWrapFlags(bits);
        }

        #else
            const int wrapMode = CheckLocalWrapFlags(bits);
        #endif
        
        
        #if defined(SHADER_TARGET_GLSL) ||defined (SHADER_API_GLES)|| defined(SHADER_API_GLES3)
        switch (wrapMode)
        {
            case 0: uv = frac(uv);break;
            case 1: uv = saturate(uv);break;
            case 2: uv = float2(frac(uv.x),saturate(uv.y));break;
            case 3: uv = float2(saturate(uv.x),frac(uv.y));break;
        }
        return SampleTexture2D(tex,uv,sampler_linear_clamp,forceLod0);//SamplerWillIgnore;需要在GLSL相关平台打包时强制设置为Clamp循环。

        #else
        
        switch (wrapMode)
        {
            case 0:
                return SampleTexture2D(tex,uv,sampler_linear_repeat,forceLod0);
                break;
            case 1:
                return SampleTexture2D(tex,uv,sampler_linear_clamp,forceLod0);
            case 2:
                return SampleTexture2D(tex,uv,sampler_linear_RepeatU_ClampV,forceLod0);
                break;
            case 3:
                return SampleTexture2D(tex,uv,sampler_linear_ClampU_RepeatV,forceLod0);
                break;
            default:
                return SampleTexture2D(tex,uv,sampler_linear_repeat,forceLod0);
                break;
        }
        #endif
    }

    #include "NBShaderParallaxV1.hlsl"

    half GetColorChannel(half4 color, int bitPos)
    {
        uint bits = _W9ParticleShaderColorChannelFlag >> bitPos;
        bits = bits & 3;
        if (bits == 0) return color.x;
        if (bits == 1) return color.y;
        if (bits == 2) return color.z;
        return color.w;
    }

    samplerCUBE _FresnelHDRITex;
    sampler2D _MainTex;
    
    
    
    #define SOFT_PARTICLE_NEAR_FADE _SoftParticleFadeParams.x    //�궨��SOFT_PARTICLE_NEAR_FADE ��Ϊ_SoftParticleFadeParams���Ե�x����~
    #define SOFT_PARTICLE_INV_FADE_DISTANCE _SoftParticleFadeParams.y
    
    #define CAMERA_NEAR_FADE  _CameraFadeParams.x
    #define CAMERA_INV_FADE_DISTANCE  _CameraFadeParams.y

    Texture2D _BaseMap;
    Texture2D _NoiseMap;
    Texture2D _NoiseMaskMap;
    Texture2D _EmissionMap;
    Texture2D _MaskMap;
    Texture2D _MaskMap2;
    Texture2D _MaskMap3;
    #ifdef _NORMALMAP
        Texture2D _BumpTex;
    #endif

    #ifdef _FX_LIGHT_MODE_SIX_WAY
        Texture2D _RigRTBk;
        Texture2D _RigLBtF;
        Texture2D _SixWayEmissionRamp;
    #endif

    #ifdef _CAMERA_OPAQUE_DISTORT_PASS
        Texture2D _CameraOpaqueTexture;
    #endif

    #ifdef _MATCAP
    Texture2D _MatCapTex;
    #endif

    // Pre-multiplied alpha helper
    #if defined(_ALPHAPREMULTIPLY_ON)  //if( blend: One OneMinusSrcAlpha)
        #define ALBEDO_MUL albedo
    #else
        #define ALBEDO_MUL albedo.a
    #endif

    
    #ifdef SOFT_UI_FRAME
        sampler2D _SoftUIFrameMask;
        // half4 LB_RT;
    #endif

    #ifdef _DISSOLVE
        Texture2D _DissolveMap;
        Texture2D _DissolveMaskMap;
        Texture2D _DissolveRampMap;
    #endif

    # ifdef  _COLORMAPBLEND
        Texture2D _ColorBlendMap;
    #endif

    #ifdef _COLOR_RAMP
        Texture2D _RampColorMap;
    #endif

    Texture2D _VATTex;
    float4 _VATTex_TexelSize;

    

    half4 SampleTexture2D(sampler2D tex,float2 uv,bool forceLod0)
    {
        half4 sample;
        UNITY_BRANCH
        if (forceLod0)
        {
            sample = tex2Dlod(tex,float4(uv,0,0));
        }
        else
        {
            sample = tex2D(tex,uv);
        }

        return sample;
    }

    half4 tex2D_TryLinearizeWithoutAlphaFX(sampler2D tex, float2 uv, bool forceLod0)
    {
        half4 outColor = 0;
        half4 sample = SampleTexture2D(tex,uv,forceLod0);
            
        #if defined(PARTICLE)//UI下使用ParticleBase不需要做 Gamma2Linear 转换
        UNITY_FLATTEN
        if(CheckLocalFlags(FLAG_BIT_PARTICLE_UIEFFECT_ON))
        {
            outColor = sample;
        }
        else
        {
            outColor = TryLinearizeWithoutAlpha(sample);
        }
        #endif
        return outColor;
    }
    //
    // Color blending fragment function
    float4 MixParticleColor(float4 baseColor, float4 particleColor, float4 colorAddSubDiff)
    {
        #if defined(_COLOROVERLAY_ON) // Overlay blend
            float4 output = baseColor;
            output.rgb = lerp(1 - 2 * (1 - baseColor.rgb) * (1 - particleColor.rgb), 2 * baseColor.rgb * particleColor.rgb, step(baseColor.rgb, 0.5));
            output.a *= particleColor.a;
            return output;
        #elif defined(_COLORCOLOR_ON) // Color blend
            half3 aHSL = RgbToHsv(baseColor.rgb);
            half3 bHSL = RgbToHsv(particleColor.rgb);
            half3 rHSL = half3(bHSL.x, bHSL.y, aHSL.z);
            return half4(HsvToRgb(rHSL), baseColor.a * particleColor.a);
        #elif defined(_COLORADDSUBDIFF_ON) // Additive, Subtractive and Difference blends based on 'colorAddSubDiff'
            float4 output = baseColor;
            output.rgb = baseColor.rgb + particleColor.rgb * colorAddSubDiff.x;
            output.rgb = lerp(output.rgb, abs(output.rgb), colorAddSubDiff.y);
            output.a *= particleColor.a;
            return output;
        #else // Default to Multiply blend
        return baseColor * particleColor;
        #endif
    }

    
    // Camera fade - returns alpha value for fading particles based on camera distance
    half CameraFade(float near, float far, float thisZ)
    {
        return saturate((thisZ - near) * far);
    }
    
    //相交位置渐变功能
    half4 Intersect(float IntersectRadius,half4 IntersectColor,float sceneZ,float thisZ)
    {
        half fade = sceneZ - thisZ;
        fade =1- NB_Remap(fade,0,IntersectRadius,0,1);

        half4 c = 0;
        c.rgb = IntersectColor.rgb;
        c.a  = fade*IntersectColor.a;
        
        return c;        
    }

    //遮挡穿透显示功能。
    half OccludeOpacity(half preAlpha,half _OccludeOpacity,half sceneZ,half thisZ)
    {
        half fakeZtest =  step(thisZ,sceneZ);
        return lerp(preAlpha*_OccludeOpacity,preAlpha,fakeZtest);
    }

    
    // Sample a texture and do blending for texture sheet animation if needed
    half4 BlendTexture(sampler2D _Texture, float2 uv, float3 blendUv, bool forceLod0)
    {
        half4 color = tex2D_TryLinearizeWithoutAlphaFX(_Texture, uv, forceLod0);
    
        half4 color2;
        #ifdef _FLIPBOOKBLENDING_ON
            color2 = tex2D_TryLinearizeWithoutAlphaFX(_Texture, blendUv.xy, forceLod0);
            color = lerp(color, color2, blendUv.z);
        #endif
    
        // if(CheckLocalFlags1(FLAG_BIT_PARTICLE_1_ANIMATION_SHEET_HELPER))
        // {
        //     color2 = tex2D_TryLinearizeWithoutAlphaFX(_Texture, blendUv.xy);
        //     color = lerp(color, color2, blendUv.z);
        // }
        return color;
    }

    half4 BlendTexture(Texture2D _Texture, float2 uv, float3 blendUv,uint bits,bool forceLod0)
    {
        half4 color = SampleTexture2DWithWrapFlags(_Texture,uv,bits,forceLod0);
        half4 color2;
        #ifdef _FLIPBOOKBLENDING_ON
            color2 = SampleTexture2DWithWrapFlags(_Texture,blendUv.xy,bits,forceLod0);
            color = lerp(color, color2, blendUv.z);
        #endif

        // if(CheckLocalFlags1(FLAG_BIT_PARTICLE_1_ANIMATION_SHEET_HELPER))
        // {
        //     color2 = SampleTexture2DWithWrapFlags(_Texture, blendUv.xy,bits);
        //     color = lerp(color, color2, blendUv.z);
        // }
        return color;
    }

    float2 UVOffsetAnimaiton(float2 UV,half2 OffsetSpeed)
    {
        float2 newUV =  float2(OffsetSpeed.x*time+UV.x,OffsetSpeed.y*time+UV.y);
        return newUV;
    }
    
    // 采样噪波
    half4 SampleNoise(half4 NoiseOffset, Texture2D _Texture,float2 UV, half3 wordPos,bool forceLod0)
    {
        
        float2 UV2 = float2(NoiseOffset.x * time + UV.x, NoiseOffset.y * time + UV.y);    //_Time.y

        half4 color = SampleTexture2DWithWrapFlags(_Texture, UV2 ,FLAG_BIT_WRAPMODE_NOISEMAP,forceLod0);
        // color.xy *= color.a;
        
        return color;
    }
    
    
    // // 替换颜色
    // half3 ReplaceColor_float(float3 In, float3 From, float3 To, float Range, float Fuzziness)
    // {
    //     float Distance = distance(From, In);
    //     half3 Out = lerp(To, In, saturate((Distance - Range) / max(Fuzziness, 0.001)));  //e-f? 0.00001
    //     return Out;
    // }

    //Fresnel
    half4 Unity_FresnelEffect(float3 Normal, float3 ViewDir, float Power, float Dire,half fresnelPos)
    {
        // half aa = saturate(dot(normalize(Normal), (ViewDir)));
        half aa = dot(normalize(Normal), (ViewDir));
        aa = (aa+1)*0.5;
        aa = NB_Remap(aa,fresnelPos,1,0,1);
        aa = lerp(aa, (1 - aa), Dire);
        half Out = pow( aa, Power);
        return Out;
    }

    half3 Rotation(half3 normalizedDirection,half3 rotation)
    {
        half4 Dir = half4(normalizedDirection,1);
        float4x4 M_RotationX = float4x4(
        
        1,0,0,0,
        0,cos(rotation.x),sin(rotation.x),0,
        0,-sin(rotation.x),cos(rotation.x),0,
        0,0,0,1

        );
        float4x4 M_RotationY = float4x4(

        cos(rotation.y),0,sin(rotation.y),0,
        0,1,0,0,
        -sin(rotation.y),0,cos(rotation.y),0,
        0,0,0,1
        
        );
        float4x4 M_RotationZ = float4x4(
        
        cos(rotation.z),sin(rotation.z),0,0,
        -sin(rotation.z),cos(rotation.z),0,0,
        0,0,1,0,
        0,0,0,1
        
        );

        return mul(M_RotationZ,mul(M_RotationY,mul(M_RotationX,Dir)));

    }

    //----------------公告板功能-----------------//
    half3 BillBoard(float3 camPos, float3 vertexPos, int billboardType, int _ReverseZ)
    {
        float3 Z = normalize(mul(unity_WorldToObject, float4(_WorldSpaceCameraPos,1)));
        if(_ReverseZ == 1)
        {
            Z *= -1;   
        }
        Z.y *= billboardType;
        
        float3 Y = float3(0,1,0);
        float3 X = normalize(cross(Z,Y));
        Y = normalize(cross(X,Z));
        float4x4 M = float4x4(
            X.x, Y.x, Z.x, 0,
            X.y, Y.y, Z.y, 0,
            X.z, Y.z, Z.z, 0,
            0,0,0,1
            );
        float3 newPos = mul(M, vertexPos);
        return newPos;
    }

    half3 BillBoardNormal(float3 camPos, float3 vertexPos, int billboardType, int _ReverseZ)
    {
        float3 Z = normalize(mul(unity_WorldToObject, float4(_WorldSpaceCameraPos,1)));
        if(_ReverseZ == 1)
        {
            Z *= -1;   
        }
        Z.y *= billboardType;
        
        float3 Y = float3(0,1,0);
        float3 X = normalize(cross(Y,Z));
        Y = normalize(cross(X,Z));
        float4x4 M = float4x4(
            X.x, Y.x, Z.x, 0,
            X.y, Y.y, Z.y, 0,
            X.z, Y.z, Z.z, 0,
            0,0,0,1
            );
        float3 newPos = -mul(M, vertexPos);
        return newPos;
    }


    float2 ParticleUVCommonProcess(float2 originUVAfterTwirlPolar,float4 scaleTilling,float2 offset = float2(0,0),float rotation = 0,float2 rotationCenter = float2(0.5,0.5))
    {
        NBFX_FeatureUVTransformInputV2 input = (NBFX_FeatureUVTransformInputV2)0;
        input.originUV = originUVAfterTwirlPolar;
        input.scaleOffset = scaleTilling;
        input.offsetSpeed = offset;
        input.rotationDegrees = rotation;
        input.rotationCenter = rotationCenter;
        input.timeY = time;
        return NBFX_TransformFeatureUVV2(input);
    }

    struct ParticleUVs
    {
        float2 mainTexUV;
        float2 specUV;
        float2 animBlendUV;
        float2 maskMapUV;
        float2 maskMap2UV;
        float2 maskMap3UV;
        float2 emissionUV;
        float2 dissolve_uv;
        float2 dissolve_mask_uv;
        float2 dissolve_noise1_UV;
        float2 dissolve_noise2_UV;
        float2 colorBlendUV;
        float2 noiseMapUV;
        float2 noiseMaskMapUV;
        float2 bumpTexUV;
        float2 colorRampMapUV;
        float2 sharedUV;
    };

    BaseUVs ProcessBaseUVs(float4 meshTexcoord0, float2 specialUVInTexcoord3, float4 VaryingsP_Custom1, float4 VaryingsP_Custom2, float3 postionOS, float3 positionWS, float2 screenUV)
    {
        NBFX_BaseUVInputV1 input = (NBFX_BaseUVInputV1)0;
        input.meshTexcoord0 = meshTexcoord0;
        input.specialUVInTexcoord3 = specialUVInTexcoord3;
        input.custom1 = VaryingsP_Custom1;
        input.custom2 = VaryingsP_Custom2;
        input.positionOS = postionOS;
        input.positionWS = positionWS;
        input.screenUV = screenUV;

        NBFX_BaseUVParamsV1 parameters = (NBFX_BaseUVParamsV1)0;
        parameters.flags0 = _W9ParticleShaderFlags;
        parameters.flags1 = _W9ParticleShaderFlags1;
        parameters.customDataFlag0 = _W9ParticleCustomDataFlag0;
        parameters.customDataFlag3 = _W9ParticleCustomDataFlag3;
        parameters.uvModeFlag0 = _UVModeFlag0;
        parameters.uvModeFlagType0 = _UVModeFlagType0;
        parameters.mainTexReverseST = _MainTex_Reverse_ST;
        parameters.uiMainTexST = _UI_MainTex_ST;
        parameters.baseMapST = _BaseMap_ST;
        parameters.sharedUVST = _SharedUV_ST;
        parameters.sharedUVVec = _SharedUV_Vec;
        parameters.baseMapMaskMapOffset = _BaseMapMaskMapOffset;
        parameters.twirlParameter = _TWParameter;
        parameters.polarCenter = _PCCenter;
        parameters.cylinderUVMatrix = float4x4(_CylinderMatrix0, _CylinderMatrix1, _CylinderMatrix2, _CylinderMatrix3);
        parameters.twirlStrength = _TWStrength;
        parameters.baseMapUVRotation = _BaseMapUVRotation;
        parameters.baseMapUVRotationSpeed = _BaseMapUVRotationSpeed;
        parameters.worldSpaceUVModeSelector = _WorldSpaceUVModeSelector;
        parameters.objectSpaceUVModeSelector = _ObjectSpaceUVModeSelector;
        parameters.timeY = time;
        return NBFX_BuildBaseUVsV1(input, parameters);
    }


    void ParticleProcessUV(inout ParticleUVs particleUVs,float4 meshTexcoord0,float4 VaryingsP_Custom1,float4 VaryingsP_Custom2,BaseUVs baseUVs)
    {
        particleUVs.specUV = baseUVs.specialUVChannel;
        
        particleUVs.mainTexUV = baseUVs.mainTexUV;
        //-------------------------------------------------
        #ifdef _FLIPBOOKBLENDING_ON //开启序列帧融合
        particleUVs.animBlendUV = NBFX_ResolveFlipbookUVV1(meshTexcoord0,
            _BaseMap_AnimationSheetBlend_ST,
            CheckLocalFlags1(FLAG_BIT_PARTICLE_1_ANIMATION_SHEET_HELPER));
        #endif
       

        #if defined(_NORMALMAP)
            float2 bumpTexUV = GetUVByUVMode(_UVModeFlag0,_UVModeFlagType0,FLAG_BIT_UVMODE_POS_0_BUMPTEX,baseUVs);
            bumpTexUV = TRANSFORM_TEX(bumpTexUV, _BumpTex);
            particleUVs.bumpTexUV = bumpTexUV;
        
        #endif
        
        

        #if defined(_MASKMAP_ON)
        
            float2 MaskMapuv = GetUVByUVMode(_UVModeFlag0,_UVModeFlagType0,FLAG_BIT_UVMODE_POS_0_MASKMAP,baseUVs);

            UNITY_BRANCH
            if(CheckLocalFlags(FLAG_BIT_PARTILCE_MASKMAPROTATIONANIMATION_ON))
            {
                _MaskMapUVRotation += time * _MaskMapRotationSpeed;
            }

            MaskMapuv= Rotate_Radians_float(MaskMapuv, half2(0.5, 0.5), _MaskMapUVRotation);
        
            MaskMapuv = TRANSFORM_TEX(MaskMapuv, _MaskMap);
        
            MaskMapuv.x += GetCustomData(_W9ParticleCustomDataFlag0,FLAGBIT_POS_0_CUSTOMDATA_MASK_OFFSET_X,0,VaryingsP_Custom1,VaryingsP_Custom2);
            MaskMapuv.y += GetCustomData(_W9ParticleCustomDataFlag0,FLAGBIT_POS_0_CUSTOMDATA_MASK_OFFSET_Y,0,VaryingsP_Custom1,VaryingsP_Custom2);

            MaskMapuv = UVOffsetAnimaiton(MaskMapuv,_MaskMapOffsetAnition.xy);
            particleUVs.maskMapUV = MaskMapuv;

            #if defined(_MASKMAP2_ON)
            {
                float2 maskMap2UV = GetUVByUVMode(_UVModeFlag0,_UVModeFlagType0,FLAG_BIT_UVMODE_POS_0_MASKMAP_2,baseUVs);
                maskMap2UV = Rotate_Radians_float(maskMap2UV,half2(0.5,0.5),_MaskMapVec.y);
                maskMap2UV = maskMap2UV * _MaskMap2_ST.xy + _MaskMap2_ST.zw;
                    
                maskMap2UV = UVOffsetAnimaiton(maskMap2UV,_MaskMapOffsetAnition.zw);
                particleUVs.maskMap2UV = maskMap2UV;
            }
            #endif

            #if defined(_MASKMAP3_ON)
            {
                float2 maskMap3UV = GetUVByUVMode(_UVModeFlag0,_UVModeFlagType0,FLAG_BIT_UVMODE_POS_0_MASKMAP_3,baseUVs);
                maskMap3UV = Rotate_Radians_float(maskMap3UV,half2(0.5,0.5),_MaskMapVec.z);
                
                maskMap3UV = maskMap3UV* _MaskMap3_ST.xy + _MaskMap3_ST.zw;
                
                maskMap3UV = UVOffsetAnimaiton(maskMap3UV,_MaskMap3OffsetAnition.xy);
                particleUVs.maskMap3UV = maskMap3UV;
            }
            #endif
        
        #endif

        
        #if defined(_EMISSION)
            float2 emissionUV = GetUVByUVMode(_UVModeFlag0,_UVModeFlagType0,FLAG_BIT_UVMODE_POS_0_EMISSION_MAP,baseUVs);
            emissionUV.x += GetCustomData(_W9ParticleCustomDataFlag3,FLAGBIT_POS_3_CUSTOMDATA_EMISSION_OFFSET_X,0,VaryingsP_Custom1,VaryingsP_Custom2);
            emissionUV.y += GetCustomData(_W9ParticleCustomDataFlag3,FLAGBIT_POS_3_CUSTOMDATA_EMISSION_OFFSET_Y,0,VaryingsP_Custom1,VaryingsP_Custom2);
            particleUVs.emissionUV = ParticleUVCommonProcess(emissionUV,_EmissionMap_ST,_EmissionMapUVOffset.xy,_EmissionMapUVRotation);
        #endif

        #if defined(_DISSOLVE)
            _DissolveMap_ST.z += GetCustomData(_W9ParticleCustomDataFlag1,FLAGBIT_POS_1_CUSTOMDATA_DISSOLVE_OFFSET_X,0,VaryingsP_Custom1,VaryingsP_Custom2);
            _DissolveMap_ST.w += GetCustomData(_W9ParticleCustomDataFlag1,FLAGBIT_POS_1_CUSTOMDATA_DISSOLVE_OFFSET_Y,0,VaryingsP_Custom1,VaryingsP_Custom2);
        
            float2 dissolveUV = GetUVByUVMode(_UVModeFlag0,_UVModeFlagType0,FLAG_BIT_UVMODE_POS_0_DISSOLVE_MAP,baseUVs);
            particleUVs.dissolve_uv = ParticleUVCommonProcess(dissolveUV,_DissolveMap_ST,_DissolveOffsetRotateDistort.xy,_DissolveOffsetRotateDistort.z);
            #if defined(_DISSOLVE_MASK)
            {
                float2 dissolveMaskUV = GetUVByUVMode(_UVModeFlag0,_UVModeFlagType0,FLAG_BIT_UVMODE_POS_0_DISSOLVE_MASK_MAP,baseUVs);
                particleUVs.dissolve_mask_uv = ParticleUVCommonProcess(dissolveMaskUV,_DissolveMaskMap_ST,float2(0,0),_DissolveOffsetRotateDistort.z);
            }
            #endif
        #endif

        #ifdef _PROGRAM_NOISE_ACTIVE
            float2 programNoiseUV = GetUVByUVMode(_UVModeFlag0,_UVModeFlagType0,FLAG_BIT_UVMODE_POS_0_PROGRAM_NOISE,baseUVs);
            programNoiseUV = Rotate_Radians_float(programNoiseUV,half2(0.5,0.5),_ProgramNoise_Rotate);
            #if defined(_PROGRAM_NOISE_SIMPLE)
            {
                _DissolveVoronoi_Vec4.x += GetCustomData(_W9ParticleCustomDataFlag2,FLAGBIT_POS_2_CUSTOMDATA_DISSOLVE_NOISE1_OFFSET_X,0,VaryingsP_Custom1,VaryingsP_Custom2);
                _DissolveVoronoi_Vec4.y += GetCustomData(_W9ParticleCustomDataFlag2,FLAGBIT_POS_2_CUSTOMDATA_DISSOLVE_NOISE1_OFFSET_Y,0,VaryingsP_Custom1,VaryingsP_Custom2);
                particleUVs.dissolve_noise1_UV = programNoiseUV * _DissolveVoronoi_Vec.xy + _DissolveVoronoi_Vec4.xy + time*_DissolveVoronoi_Vec3.xy;
                
            }
            #endif
            #if defined(_PROGRAM_NOISE_VORONOI)
            {
                _DissolveVoronoi_Vec4.z += GetCustomData(_W9ParticleCustomDataFlag2,FLAGBIT_POS_2_CUSTOMDATA_DISSOLVE_NOISE2_OFFSET_X,0,VaryingsP_Custom1,VaryingsP_Custom2);
                _DissolveVoronoi_Vec4.w += GetCustomData(_W9ParticleCustomDataFlag2,FLAGBIT_POS_2_CUSTOMDATA_DISSOLVE_NOISE2_OFFSET_Y,0,VaryingsP_Custom1,VaryingsP_Custom2);
                particleUVs.dissolve_noise2_UV = programNoiseUV * _DissolveVoronoi_Vec.zw + _DissolveVoronoi_Vec4.zw + time*_DissolveVoronoi_Vec3.zw;
            }
            #endif
        #endif
        
        
        #ifdef _COLORMAPBLEND
            float2 colorBlendUV = GetUVByUVMode(_UVModeFlag0,_UVModeFlagType0,FLAG_BIT_UVMODE_POS_0_COLOR_BLEND_MAP,baseUVs);
            colorBlendUV.x += GetCustomData(_W9ParticleCustomDataFlag3,FLAGBIT_POS_3_CUSTOMDATA_COLOR_BLEND_OFFSET_X,0,VaryingsP_Custom1,VaryingsP_Custom2);
            colorBlendUV.y += GetCustomData(_W9ParticleCustomDataFlag3,FLAGBIT_POS_3_CUSTOMDATA_COLOR_BLEND_OFFSET_Y,0,VaryingsP_Custom1,VaryingsP_Custom2);
            particleUVs.colorBlendUV = ParticleUVCommonProcess(colorBlendUV,_ColorBlendMap_ST,_ColorBlendMapOffset.xy,_ColorBlendVec.w);
        
        #endif
        #ifdef _COLOR_RAMP
            float2 colorRampMapUV = GetUVByUVMode(_UVModeFlag0,_UVModeFlagType0,FLAG_BIT_UVMODE_POS_0_RAMP_COLOR_MAP,baseUVs);
            particleUVs.colorRampMapUV = ParticleUVCommonProcess(colorRampMapUV,_RampColorMap_ST,_RampColorMapOffset.xy,_RampColorMapOffset.w);
        #endif
           half cum_noise = 0;

        //TODO
           
        #if defined(_NOISEMAP)
            
            //和ParticleUVCommonProcess相比，此处没有UV动画，NoiseMap的UV流动在最终的SampleNoise中进行
            float2 noiseMapUV = GetUVByUVMode(_UVModeFlag0,_UVModeFlagType0,FLAG_BIT_UVMODE_POS_0_NOISE_MAP,baseUVs);
            particleUVs.noiseMapUV = ParticleUVCommonProcess(noiseMapUV,_NoiseMap_ST,half2(0,0),_NoiseMapUVRotation);

            #if defined(_NOISE_MASKMAP)
            {
                float2 noiseMaskMapUV = GetUVByUVMode(_UVModeFlag0,_UVModeFlagType0,FLAG_BIT_UVMODE_POS_0_NOISE_MASK_MAP,baseUVs);
                particleUVs.noiseMaskMapUV = ParticleUVCommonProcess(noiseMaskMapUV,_NoiseMaskMap_ST,half2(0,0),0);
               
            }
            #endif
        #endif

   
        
    }

    #if defined(_VERTEX_OFFSET)
    Texture2D _VertexOffset_Map;
    #if defined(_VERTEX_OFFSET_MASKMAP)
    Texture2D _VertexOffset_MaskMap;
    #endif
    
    // half3  _VertexOffset_Vec;
    // half3 _VertexOffset_CustomDir;
    // half4 _VertexOffset_Map_ST;

    half3 VetexOffset(half3 positionOS,half2 originUV,half2 originMaskUV,half3 normalOS,half3 vertexColorRGB,out half3 offsetOS)
    {
        // The material remains a Float for serialized 0/1 compatibility; switch on an integer.
        int directionMode = (int)round(_VertexOffset_NormalDir_Toggle);
        half vertexOffsetSample = 1;
        half3 directionSample = vertexColorRGB;
        UNITY_BRANCH
        if (directionMode != 2)
        {
            half2 uv = TRANSFORM_TEX(originUV,_VertexOffset_Map);
            uv = UVOffsetAnimaiton(uv,_VertexOffset_Vec.xy);
            half4 offsetMapSample = SampleTexture2DWithWrapFlags(_VertexOffset_Map,uv,FLAG_BIT_WRAPMODE_VERTEXOFFSETMAP,true);
            vertexOffsetSample = GetColorChannel(offsetMapSample,FLAG_BIT_COLOR_CHANNEL_POS_0_VERTEX_OFFSET_MAP);
            directionSample = offsetMapSample.rgb;
        }

        // Preserve StartFromZero: on = raw data; off = 0.5-centered signed data.
        if (!CheckLocalFlags1(FLAG_BIT_PARTICLE_1_VERTEXOFFSET_START_FROM_ZERO))
        {
            vertexOffsetSample = vertexOffsetSample*2-1;
            directionSample = directionSample*2-1;
        }

        half vertexOffsetMask = 1;
        #if defined(_VERTEX_OFFSET_MASKMAP)
        {
            half2 maskUV = TRANSFORM_TEX(originMaskUV,_VertexOffset_MaskMap);
            maskUV = UVOffsetAnimaiton(maskUV,_VertexOffset_MaskMap_Vec.xy);
            half4 maskMapSample = SampleTexture2DWithWrapFlags(_VertexOffset_MaskMap,maskUV,FLAG_BIT_WRAPMODE_VERTEXOFFSET_MASKMAP,true);
            half vertexOffsetMaskSample = GetColorChannel(maskMapSample,FLAG_BIT_COLOR_CHANNEL_POS_0_VERTEX_OFFSET_MASKMAP);
            vertexOffsetMask = lerp(1,vertexOffsetMaskSample,_VertexOffset_MaskMap_Vec.z);
        }
        #endif
     
        // Keep the space conversion in the ShaderLab host: Graph/VFX hosts may
        // prepare the same direction from different, explicitly supplied inputs.
        if ((directionMode == 2 || directionMode == 3) && _VertexOffset_DirectionSpace > 0.5)
            directionSample = TransformWorldToObjectDir_NB(directionSample, false);

        NBFX_VertexOffsetPreparedV1 prepared = (NBFX_VertexOffsetPreparedV1)0;
        prepared.normalOS = normalOS;
        prepared.directionOS = directionSample;
        prepared.customDirectionOS = _VertexOffset_CustomDir;
        prepared.sampledScalar = vertexOffsetSample;
        prepared.maskWeight = vertexOffsetMask;
        prepared.intensity = _VertexOffset_Vec.z;
        prepared.directionMode = directionMode;
        offsetOS = NBFX_ComputeVertexOffsetOSV1(prepared);

        return positionOS + offsetOS;
        
    }
    #endif

    #include "NBShaderChromaticV1.hlsl"

    //向UV横向两边的色散。
    #if defined(_CHROMATIC_ABERRATION)
    half4 DistortionChoraticaberrat(Texture2D baseTexture,half2 originUV, half2 uvAfterNoise,half ChoraticaberratIntensity,uint bits,bool forceLod0)
    {
        half2 delta = NBFX_ChromaticDeltaV1(originUV, uvAfterNoise,
            ChoraticaberratIntensity,
            CheckLocalFlags(FLAG_BIT_PARTICLE_NOISE_CHORATICABERRAT_WITH_NOISE));
        half2 ra = SampleTexture2DWithWrapFlags(baseTexture,uvAfterNoise,bits,forceLod0).xw;
        half2 ga = SampleTexture2DWithWrapFlags(baseTexture,uvAfterNoise - delta,bits,forceLod0).yw;
        half2 ba = SampleTexture2DWithWrapFlags(baseTexture,uvAfterNoise - delta*2,bits,forceLod0).zw;
        return NBFX_ComposeChromaticV1(ra, ga, ba);
    }
    #endif

    bool needSceneDepth()
    {
        #if defined(_DEPTH_DECAL) || defined(_SOFTPARTICLES_ON) || defined(_DEPTH_OUTLINE)
            return true;
        #endif

        return  false;
    }

    bool needEyeDepth()
    {
        #if defined(_SOFTPARTICLES_ON) || defined(_DEPTH_OUTLINE) || defined(_DISTANCE_FADE)
            return true;
        #endif

        return  false;
    }

    bool needPositionVS()
    {
        #if defined(_MATCAP)
            return true;
        #endif

        if (unity_OrthoParams.w == 1)
        {
            return true;
        }
        return false;
    }

    bool ignoreFresnel()
    {
        #if defined(PARTICLE_BACKFACE_PASS)
        return  true;
        #endif
        return false;
    }

    float3 CustomRefract(float3 incident, float3 normal, float eta)
    {
        return NBFX_RefractDirectionV1(incident, normal, eta);
    }

    Texture2D _ParallaxMapping_Map;

    half2 ParallaxMappingSimple(half2 texCoords, half3 viewDir, bool forceLod0)
    {
         float height = SampleTexture2DWithWrapFlags(_ParallaxMapping_Map,texCoords,FLAG_BIT_WRAPMODE_PARALLAXMAPPINGMAP,forceLod0).r;
         height *= _ParallaxMapping_Intensity;
         viewDir = normalize(viewDir);
         viewDir.xy /= (viewDir.z);
         texCoords -= viewDir.xy * height;
         return  texCoords;
    }

    half2 ParallaxMappingPeelDepth(half2 texCoords, half3 viewDir, bool forceLod0)
    {
        const float minLayers = 2;
        const float maxLayers = 32;
        float numLayers = lerp(maxLayers, minLayers, abs(dot(half3(0.0, 0.0, 1.0), viewDir)));//视线越垂直于表面，层数越少，反之越多。
        float layerDepth = 1/numLayers;
        float currentLayerDepth = 0;
        // viewDir.xy/=viewDir.z;
        half2 p = viewDir.xy*_ParallaxMapping_Intensity;
        half2 deltaTexcoord = p/numLayers;

        half2 currentTexcoords = texCoords;
        float currentMapDepthValue = SampleTexture2DWithWrapFlags(_ParallaxMapping_Map,currentTexcoords,FLAG_BIT_WRAPMODE_PARALLAXMAPPINGMAP,forceLod0).r;

        [loop]
        while (currentLayerDepth < currentMapDepthValue )
        {
            currentTexcoords -= deltaTexcoord;
            currentMapDepthValue = SampleTexture2DWithWrapFlags(_ParallaxMapping_Map,currentTexcoords,FLAG_BIT_WRAPMODE_PARALLAXMAPPINGMAP,forceLod0).r;
            currentLayerDepth += layerDepth;
        }

        return  currentTexcoords;
        
    }
    float2 ParallaxOcclusionMapping(float2 texCoords, float3 viewDir, bool forceLod0)
    {
        return NBFX_ParallaxOcclusionMappingV1(_ParallaxMapping_Map,
            texCoords, viewDir, _ParallaxMapping_Map_ST,
            _ParallaxMapping_Intensity, _ParallaxMapping_Vec,
            CheckLocalWrapFlags(FLAG_BIT_WRAPMODE_PARALLAXMAPPINGMAP), forceLod0);
    }

//--------------MatCap------------------

//-

    struct AttributesParticle//即URP语境下的appdata
    {
        float4 vertex: POSITION;
        float3 normalOS: NORMAL;
        half4 color: COLOR;
        #if defined(_FLIPBOOKBLENDING_ON)
            float4 texcoords: TEXCOORD0;       //texcoords.zw就是粒子那边新建的UV2
            float3 texcoordBlend: TEXCOORD3;//注意，假如需要UI支持，則Canvas要開放相關Channel
        #else
            float4 texcoords: TEXCOORD0;
            float2 vatTexcoord4: TEXCOORD3;
        #endif

        #if defined(_PARALLAX_MAPPING) || defined(_NORMALMAP) || defined(_FX_LIGHT_MODE_SIX_WAY)
            float4 tangentOS     : TANGENT;
        #endif
        
        float4 Custom1: TEXCOORD1;
        float4 Custom2: TEXCOORD2;
        float2 vatTexcoord5: TEXCOORD4;
        float2 vatTexcoord6: TEXCOORD5;
        float2 vatTexcoord7: TEXCOORD6;
        float2 vatTexcoord8: TEXCOORD7;
        
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };
    
    struct VaryingsParticle//即URP语境下的v2f
    {
        float4 clipPos: SV_POSITION;
        
        half4 color: COLOR;
        float4 texcoord: TEXCOORD0;  // 主帖图 和 mask
        
        #if defined (_EMISSION)   || defined(_COLORMAPBLEND)
            float4 emissionColorBlendTexcoord: TEXCOORD1;  // 流光
        #endif

        #ifdef _NOISEMAP
            float4 noisemapTexcoord:TEXCOORD2;//Noise
        #endif
 
        #if defined(_DISSOLVE) 

            float4 dissolveTexcoord:TEXCOORD15;

        #endif
        #ifdef _PROGRAM_NOISE_ACTIVE
            float4 dissolveNoiseTexcoord: TEXCOORD5;
        #endif
        
        
        float4 positionWS: TEXCOORD3;
        float4 positionOS: TEXCOORD12;
        
        float4 texcoord2AndSpecialUV: TEXCOORD6;  // UV2和SpecialUV

        float4 positionNDC: TEXCOORD7;
        
        
        float4 VaryingsP_Custom1: TEXCOORD8;
        float4 VaryingsP_Custom2: TEXCOORD9;
        

        float4 normalWSAndAnimBlend: TEXCOORD10;
        
        // float3 fresnelViewDir :TEXCOORD11;
        
        // float3 viewDirWS :TEXCOORD13;
        #if defined(_MASKMAP2_ON) || defined(_MASKMAP3_ON)
            float4 texcoordMaskMap2 : TEXCOORD14;
        #endif

        #ifndef _FX_LIGHT_MODE_UNLIT
            #if !defined(_FX_LIGHT_MODE_SIX_WAY)
                half3 vertexSH :TEXCOORD17;
            #endif
            #ifdef _ADDITIONAL_LIGHTS_VERTEX
                half3 vertexLight :TEXCOORD18;
            #endif
        #endif

        #if (defined(_PARALLAX_MAPPING) || defined(_NORMALMAP)) && !defined(_FX_LIGHT_MODE_SIX_WAY)
            half4 tangentWS : TEXCOORD19;
        #endif
        #if defined (_NORMALMAP) || defined(_COLOR_RAMP) 
            float4 bumpTexAndColorRampMapTexcoord : TEXCOORD20;
        #endif

        #ifdef _FX_LIGHT_MODE_SIX_WAY
            half4 bakeDiffuseLighting0 :TEXCOORD21;
            half4 bakeDiffuseLighting1 :TEXCOORD22;
            half4 bakeDiffuseLighting2 :TEXCOORD23;
            half4 backBakeDiffuseLighting0 :TEXCOORD24;
            half4 backBakeDiffuseLighting1 :TEXCOORD25;
            half4 backBakeDiffuseLighting2 :TEXCOORD26;
        #endif
        
        UNITY_VERTEX_INPUT_INSTANCE_ID
        UNITY_VERTEX_OUTPUT_STEREO
    };

    bool isProcessUVInFrag()
    {
        if(CheckLocalFlags(FLAG_BIT_PARTICLE_POLARCOORDINATES_ON) || CheckLocalFlags(FLAG_BIT_PARTICLE_UTWIRL_ON)) 
        {
            return true;
        }
        #if defined(_DEPTH_DECAL) || defined(_PARALLAX_MAPPING) || defined(_SCREEN_DISTORT_MODE) || defined(_CAMERA_OPAQUE_DISTORT_PASS)
            return true;
        #endif
        return false;
    }

#ifndef _FX_LIGHT_MODE_UNLIT
	#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"



    void InitializeInputData(VaryingsParticle input,half3x3 tangentToWorld, half3 viewDirWS, out InputData inputData)
    {
        inputData = (InputData)0;

    #if defined(REQUIRES_WORLD_SPACE_POS_INTERPOLATOR)
        inputData.positionWS = input.positionWS;
    #endif
    //Normal转到外面执行
    //     half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
    // #if defined(_NORMALMAP) || defined(_DETAIL)
    //     float sgn = input.tangentWS.w;      // should be either +1 or -1
    //     float3 bitangent = sgn * cross(input.normalWSAndAnimBlend.xyz, input.tangentWS.xyz);
    //     half3x3 tangentToWorld = half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWSAndAnimBlend.xyz);
    //
    //     #if defined(_NORMALMAP)
    //     inputData.tangentToWorld = tangentToWorld;
    //     #endif
    //     inputData.normalWS = TransformTangentToWorld(normalTS, tangentToWorld);
    // #else
    //     inputData.normalWS = input.normalWSAndAnimBlend.xyz;
    // #endif
    //
    //     inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
    inputData.tangentToWorld = tangentToWorld;
    inputData.normalWS = input.normalWSAndAnimBlend.xyz;
    inputData.viewDirectionWS = viewDirWS;

    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
        inputData.shadowCoord = input.shadowCoord;
    #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
        inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
    #else
        inputData.shadowCoord = float4(0, 0, 0, 0);
    #endif
    #ifdef _ADDITIONAL_LIGHTS_VERTEX
        inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactorAndVertexLight.x);
        inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
    #else
        inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS.xyz, 1.0), input.positionWS.w);
    #endif

    // #if defined(DYNAMICLIGHTMAP_ON)
    //     inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.dynamicLightmapUV, input.vertexSH, inputData.normalWS);
    // #else
    //     inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, inputData.normalWS);
    // #endif

        #if defined(_FX_LIGHT_MODE_SIX_WAY)
            inputData.bakedGI = 0;
        #else
            inputData.bakedGI = SampleSHPixel(input.vertexSH, inputData.normalWS);
        #endif

        inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.clipPos);
        inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);

        #if defined(DEBUG_DISPLAY)
            #if defined(DYNAMICLIGHTMAP_ON)
            inputData.dynamicLightmapUV = input.dynamicLightmapUV;
            #endif
            #if defined(LIGHTMAP_ON)
            inputData.staticLightmapUV = input.staticLightmapUV;
            #else
                #if !defined(_FX_LIGHT_MODE_SIX_WAY)
                    inputData.vertexSH = input.vertexSH;
                #endif
            #endif
        #endif
    }
#endif


    int2 GetGradientIndex(half timeArr[6], int arrCount, half gradientTime)
    {
        
        // // 边界情况处理
        // if (gradientTime < timeArr[0]) {
        //     return int2(-1, 0); // 小于最小值
        // }
        // if (gradientTime >= timeArr[arrCount - 1]) {
        //     return int2(arrCount - 1, arrCount); // 大于等于最大值
        // }

        // 顺序查找第一个大于X的元素索引
        [unroll] 
        for (int i = 0; i < arrCount; i++) {
            if (timeArr[i] > gradientTime) {
                return int2(i - 1, i); // 返回区间索引
            }
        }
        return int2(-1, 0); // 理论上不会执行此处
    }
    half GetGradientIndexInterval(half timeArr[6],int arrCount,int2 indexes,half gradientTime)
    {
        //超出范围的直接在外面就判断好。
        // half smallVal = indexes.x < 0 ? 0:timeArr[indexes.x]; 
        half smallVal = timeArr[indexes.x]; 
        // half bigVal = indexes.y >= arrCount ? 1 : timeArr[indexes.y]; 
        half bigVal = timeArr[indexes.y]; 
        return (gradientTime - smallVal) / (bigVal - smallVal);
    }

    half3 GetGradientColorValue(half3 colorArr[6],half timeArr[6], int arrCount,half gradientTime)
    {
        if (gradientTime <= timeArr[0])
        {
            return colorArr[0];
        }
        else if (gradientTime >= timeArr[arrCount - 1] )
        {
            return colorArr[arrCount - 1];
        }
        else
        {
            int2 indexes = GetGradientIndex(timeArr, arrCount, gradientTime);
            half interval = GetGradientIndexInterval(timeArr, arrCount, indexes, gradientTime);
            interval = SmoothStep01(interval);
            return lerp(colorArr[indexes.x], colorArr[indexes.y], interval);
        }
    }

    half GetGradientAlphaValue(half alphaArr[6],half timeArr[6], int arrCount,half gradientTime)
    {
        if (gradientTime <= timeArr[0])
        {
            return alphaArr[0];
        }
        else if (gradientTime >= timeArr[arrCount - 1] )
        {
            return alphaArr[arrCount - 1];
        }
        else
        {
            int2 indexes = GetGradientIndex(timeArr, arrCount, gradientTime);
            half interval = GetGradientIndexInterval(timeArr, arrCount, indexes, gradientTime);
            interval = SmoothStep01(interval);//TODO:消耗很大，如何避免？
            half alpha  = lerp(alphaArr[indexes.x], alphaArr[indexes.y], interval);
            alpha *= alpha;//Make Alpha Smoother
            return alpha ;
        }
    }

    void ColorAdjustment(inout half3 result,inout half alpha,half4 customData1,half4 customData2)
    {
        bool hueOn = CheckLocalFlags(FLAG_BIT_HUESHIFT_ON);
        bool contrastOn = CheckLocalFlags1(FLAG_BIT_PARTICLE_1_MAINTEX_CONTRAST);
        bool saturationOn = CheckLocalFlags(FLAG_BIT_SATURABILITY_ON);
        if (hueOn)
            _HueShift = GetCustomData(_W9ParticleCustomDataFlag0,
                FLAGBIT_POS_0_CUSTOMDATA_HUESHIFT,_HueShift,customData1,customData2);
        if (contrastOn)
            _Contrast = GetCustomData(_W9ParticleCustomDataFlag2,
                FLAGBIT_POS_2_CUSTOMDATA_MAINTEX_CONTRAST,_Contrast,customData1,customData2);
        if (saturationOn)
            _Saturability = GetCustomData(_W9ParticleCustomDataFlag1,
                FLAGBIT_POS_1_CUSTOMDATA_SATURATE,_Saturability,customData1,customData2);
        NBFX_ApplyColorAdjustmentV1(result, alpha,
            hueOn, _HueShift, contrastOn, _Contrast, _ContrastMidColor,
            saturationOn, _Saturability,
            CheckLocalFlags1(FLAG_BIT_PARTICLE_1_MAINTEX_COLOR_REFINE),
            _BaseMapColorRefine,
            CheckLocalFlags(FLAG_BIT_PARTICLE_COLOR_MULTI_ALPHA));
    }

#endif
