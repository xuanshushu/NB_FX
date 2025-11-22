Shader "XuanXuan/Postprocess/NBPostProcessUber"
{
        Properties
        { 
//            [MainTexture] _BaseMap("Base Map", 2D) = "white"
            _SpeedDistortMap("速度径向扭曲贴图", 2D) = "white"
            _SpeedDistortVec("速度径向 x强度y位置z范围w速度",Vector) = (0,0,0,0)
            _SpeedDistortVec2("速度径向 x:uvX速度 y:uvY速度",Vector) = (0,0,0,0)
            
            _TextureOverlay("肌理附加图",2D) = "white"
            _TextureOverlayIntensity("肌理附加强度",Float) = 0
            _TextureOverlayAnim("机理图动画",Vector) = (0,0,0,0)
            _TextureOverlayMask("肌理图蒙板",2D) = "white"
            
            _InvertIntensity("反向强度",Float) = 0
            _DeSaturateIntensity("饱和度强度",Float) = 0
            _Contrast("对比度",Float) = 1
            _FlashIntensity("反闪效果强度",Float) = 1
            _FlashColor("闪颜色", Vector) = (1, 1, 1, 1)
            _BlackFlashColor("闪黑颜色", Vector) = (0, 0, 0, 1)
            _FlashTexture("黑白闪细节图",2D) = "white"
            _FlashTextureIntensity("黑白细节图混合强度",Float) = 0.5
            _FlashGradientRange("黑白闪过渡范围",Float) = 1
            _FlashVec("xy:FlashTextureOffsetAnim",Vector) = (0,0,0,0)


            [HideInInspector] _NBPostProcessFlags("_NBPostProcessFlags", Integer) = 0
            _ChromaticAberrationVector("色散矢量",Vector) = (1,0,0,0)
            
            _CustomScreenCenter("自定义屏幕中心",Vector) = (0.5,0.5,0,0)
            _RadialBlurVec("径向模糊矢量 x强度",Vector) = (1,0,0,0)
            _VignetteVec("暗角矢量 x强度,y圆度,z光滑度",Vector) = (1,0,0,0)
            
            
            //不知道为什么，这里写成Color老是出错。
            _VignetteColor("暗角颜色",Vector) = (0.0,0.0,0.0,1.0)

        }
    
        // The SubShader block containing the Shader code.
        SubShader
        {
            // SubShader Tags define when and under which conditions a SubShader block or
            // a pass is executed.
            Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
    
            Pass
            {
                Blend SrcAlpha OneMinusSrcAlpha
                ZTest Off
                HLSLPROGRAM
                // This line defines the name of the vertex shader.
                #pragma vertex vert
                // This line defines the name of the fragment shader.
                #pragma fragment frag
                #define CUSTOM_POSTPROCESS
                // #define _POLARCOORDINATES
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
         
                #include "HLSL/PostProcessingFlags.hlsl"
			    #include "../../XuanXuanRenderUtility/Shader/HLSL/XuanXuan_Utility.hlsl"

                
                // This example uses the Attributes structure as an input structure in
                // the vertex shader.

         
                struct Attributes
                {
                    float4 positionOS   : POSITION;
                };
    
                struct Varyings
                {
                    // The positions in this struct must have the SV_POSITION semantic.
                    float4 positionHCS  : SV_POSITION;
                    half2 uv :TEXCOORD0;
                };
    
                TEXTURE2D(_ScreenColorCopy1);
                SAMPLER(_linear_clamp);
                SAMPLER(sampler_ScreenColorCopy1);
                
                TEXTURE2D(_DisturbanceMaskTex);
                SAMPLER(sampler_DisturbanceMaskTex);

                Texture2D _SpeedDistortMap;
                SAMPLER(sampler_SpeedDistortMap);

                TEXTURE2D(_TextureOverlay);
                SAMPLER(sampler_TextureOverlay);
                TEXTURE2D(_TextureOverlayMask);
                SAMPLER(sampler_TextureOverlayMask);

                TEXTURE2D(_FlashTexture);
                SAMPLER(sampler_FlashTexture);

                #if UNITY_VERSION < 202220
                SAMPLER(sampler_LinearClamp);
                #endif
                
                CBUFFER_START(UnityPerMaterial)
                
                    float4 _SpeedDistortMap_ST;
                    half4 _SpeedDistortVec;
                    float4 _SpeedDistortVec2;

                    half4 _TextureOverlay_ST;
                    half _TextureOverlayIntensity;
                    half4 _TextureOverlayMask_ST;
                    half4 _TextureOverlayAnim;

                    half _FlashIntensity;
                    half _InvertIntensity;
                    half _DeSaturateIntensity;
                    half _Contrast;
                    half _FlashGradientRange;
                    half _FlashTextureIntensity;
                    float4 _FlashTexture_ST;
                    float2 _FlashVec;
                    half3 _FlashColor;
                    half3 _BlackFlashColor;
                
                    half4 _ChromaticAberrationVec;
                    half4 _CustomScreenCenter;
                    half4 _RadialBlurVec;
                    half4 _VignetteVec;
                    half4 _VignetteColor;
                
                CBUFFER_END

                half4 SAMPLE_TEXTURE2D_CHORATICABERRAT(half2 screenUV,half2 distortUV,half2 blurVec,half distToCenter)
                {
                    if(CheckLocalFlags(FLAG_BIT_CHORATICABERRAT_BY_DISTORT))
                    {
                        blurVec = blurVec*0.25*_ChromaticAberrationVec.x;
                    }
                    else
                    {
                        half intensity = 1;
                        half range = _ChromaticAberrationVec.z*0.5f;
                        intensity = NB_Remap(distToCenter,_ChromaticAberrationVec.y-range,_ChromaticAberrationVec.y+range,0,1);
                        blurVec = blurVec*0.25*_ChromaticAberrationVec.x*intensity;
                    }
                    half r = SAMPLE_TEXTURE2D_X(_ScreenColorCopy1, sampler_LinearClamp, screenUV + distortUV             ).x;
                    half g = SAMPLE_TEXTURE2D_X(_ScreenColorCopy1, sampler_LinearClamp, blurVec + screenUV + distortUV     ).y;
                    half b = SAMPLE_TEXTURE2D_X(_ScreenColorCopy1, sampler_LinearClamp, blurVec * 2.0 + screenUV + distortUV).z;

                    half a = dot(blurVec,blurVec)*100000;
                    return half4(r,g,b,a);
                }

         
    
                Varyings vert(Attributes IN)
                {
                    // Declaring the output object (OUT) with the Varyings struct.
                    Varyings OUT;
                    OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                    half4 clipVertex = OUT.positionHCS/OUT.positionHCS.w;
                    OUT.uv = ComputeScreenPos(clipVertex);
                    // Returning the output.
                    return OUT;
                }
    
                half4 frag(Varyings IN) : SV_Target
                {
                    half2 screenUV = IN.uv;
                    half2 distortUV = 0;
                    half2 distortUVWithoutIntensity=0;

                    half4 color = 0;

                    half2 polarCoordinates = 0;

                    //half disturbanceMask = (SAMPLE_TEXTURE2D(_DisturbanceMaskTex, _linear_clamp, screenUV)) * 2 - 1 + 0.00392;  //8位0.5校准
                    //disturbanceMask *= 0.4;
                    //half2 disturbanceMask = (SAMPLE_TEXTURE2D(_DisturbanceMaskTex, _linear_clamp, screenUV).xy) * 0.8 - 0.398432;  //上面两行合并
                    
                    half2 disturbanceMask = (SAMPLE_TEXTURE2D(_DisturbanceMaskTex, _linear_clamp, screenUV).xy);
                    // return half4(disturbanceMask,0,1);//上面两行合并
  
                    
                    color.a = SimpleSmoothstep(0,0.01,abs(disturbanceMask.x + disturbanceMask.y));
                    screenUV += disturbanceMask;
                 
                    UNITY_BRANCH
                    if (!CheckLocalFlags(FLAG_BIT_NB_POSTPROCESS_ON))
                    {
                        color.rgb = SAMPLE_TEXTURE2D(_ScreenColorCopy1, _linear_clamp, screenUV).rgb;
                    }
                    
                    UNITY_BRANCH
                    if((CheckLocalFlags(FLAG_BIT_DISTORT_SPEED)& (!CheckLocalFlags(FLAG_BIT_POST_DISTORT_SCREEN_UV)))|CheckLocalFlags(FLAG_BIT_OVERLAYTEXTURE_POLLARCOORD)|CheckLocalFlags(FLAG_BIT_FLASHTEXTURE_POLLARCOORD))
                    {
                        polarCoordinates= PolarCoordinates(screenUV,_CustomScreenCenter.xy);
                    }
                    
                    
                    UNITY_BRANCH
                    if(CheckLocalFlags(FLAG_BIT_DISTORT_SPEED))
                    {
                        _SpeedDistortMap_ST.zw += _SpeedDistortVec2.xy * _Time.y;
                        // return half4(polarCoordinates,0,1);
                        half2 distortSpeedUV;
                        if(CheckLocalFlags(FLAG_BIT_POST_DISTORT_SCREEN_UV))
                        {
                            distortSpeedUV = screenUV* _SpeedDistortMap_ST.xy+_SpeedDistortMap_ST.zw;
                            half2 noise = SAMPLE_TEXTURE2D(_SpeedDistortMap,sampler_SpeedDistortMap,distortSpeedUV);
                            noise = noise * 2-1+_SpeedDistortVec.w*0.1;
                            half distortStrength = _SpeedDistortVec.x * 0.2;
                            distortUVWithoutIntensity = noise;
                            distortUV = noise * distortStrength;
                            
                        }
                        else
                        {
                            distortSpeedUV =  polarCoordinates * _SpeedDistortMap_ST.xy + _SpeedDistortMap_ST.zw;
                            half noise = SAMPLE_TEXTURE2D(_SpeedDistortMap,sampler_SpeedDistortMap,distortSpeedUV);
                            noise *= SimpleSmoothstep(_SpeedDistortVec.y,_SpeedDistortVec.y+_SpeedDistortVec.z,(distortSpeedUV.y - _SpeedDistortMap_ST.w)/_SpeedDistortMap_ST.y);
                            half distortStrength =  - _SpeedDistortVec.x * 0.2;
                            distortUV = normalize(screenUV-_CustomScreenCenter.xy)*noise;
                            distortUVWithoutIntensity = distortUV;
                            distortUV *= distortStrength;
                        }


                        color.a += dot(distortUV,distortUV)*100000;
                    }
                    else
                    {
                        distortUVWithoutIntensity = disturbanceMask;
                    }
                  
                    // return half4(((dot(distortUV,distortUV)*100000)).rrr,1);
                    
                    UNITY_BRANCH
                    if(CheckLocalFlags(FLAG_BIT_CHORATICABERRAT) || CheckLocalFlags(FLAG_BIT_RADIALBLUR))
                    {
                        float2 blurVec = 0;
                        half dist = 0;

                        if(!CheckLocalFlags(FLAG_BIT_CHORATICABERRAT_BY_DISTORT)|!CheckLocalFlags(FLAG_BIT_RADIALBLUR_BY_DISTORT))
                        {
                            blurVec = _CustomScreenCenter.xy - screenUV;
                            dist = dot(blurVec,blurVec)*4;
                        }

                        float2 choraticaBerratBlurVec;
                        if(CheckLocalFlags(FLAG_BIT_CHORATICABERRAT))
                        {
                            if(CheckLocalFlags(FLAG_BIT_CHORATICABERRAT_BY_DISTORT))
                            {
                                choraticaBerratBlurVec = distortUVWithoutIntensity;
                            }
                            else
                            {
                                choraticaBerratBlurVec = blurVec;
                            }
                        }
                        
                            

                        
                        if(CheckLocalFlags(FLAG_BIT_RADIALBLUR))
                        {

                            float2 radialblurVec; 
                            if(CheckLocalFlags(FLAG_BIT_RADIALBLUR_BY_DISTORT))
                            {
                                radialblurVec =  distortUVWithoutIntensity*_RadialBlurVec.x;
                            }
                            else
                            {
                                _RadialBlurVec.z *= 0.5;
                                half rangeIntensity = NB_Remap(dist,_RadialBlurVec.y-_RadialBlurVec.z,_RadialBlurVec.y+_RadialBlurVec.z,0,1);
                                rangeIntensity = saturate(rangeIntensity);
                                radialblurVec  =  blurVec*_RadialBlurVec.x*rangeIntensity;
                            }
                            color.a += dot(blurVec,blurVec)*100;

                            half3 acumulateColor = half3(0, 0, 0);

                            int iteration = _RadialBlurVec.w;
                            [unroll(12)]
                            for(int i = 0;i<iteration;i++)
                            {
                                if(CheckLocalFlags(FLAG_BIT_CHORATICABERRAT))
                                {
                                    //sample *3
                                    acumulateColor += SAMPLE_TEXTURE2D_CHORATICABERRAT(screenUV,distortUV,choraticaBerratBlurVec,dist);
                                }
                                else
                                {
                                    acumulateColor += SAMPLE_TEXTURE2D(_ScreenColorCopy1,_linear_clamp,screenUV + distortUV);
                                }
                                screenUV += radialblurVec;
                            }
                            color.rgb = acumulateColor / iteration;
                        }
                        else
                        {
                            half4 choraticaBerratColor = SAMPLE_TEXTURE2D_CHORATICABERRAT(screenUV,distortUV,choraticaBerratBlurVec,dist);
                            color.rgb  = choraticaBerratColor.rgb;
                            color.a += choraticaBerratColor.a;
                        }
                        
                    }
                    else
                    {
                        color.rgb = SAMPLE_TEXTURE2D(_ScreenColorCopy1,_linear_clamp,screenUV + distortUV);
                    }

                    // return  half4(color.aaa,1);

                 

                    UNITY_BRANCH
                    if(CheckLocalFlags(FLAG_BIT_OVERLAYTEXTURE))
                    {
                        half2 overlayTexUV;
                        if(CheckLocalFlags(FLAG_BIT_OVERLAYTEXTURE_POLLARCOORD))
                        {
                            overlayTexUV = polarCoordinates;
                        }
                        else
                        {
                            
                            overlayTexUV = screenUV;
                        }

                        float2 overlayMainTexUV = TRANSFORM_TEX(overlayTexUV,_TextureOverlay);
                        overlayMainTexUV = UVOffsetAnimaiton(overlayMainTexUV,_TextureOverlayAnim.xy,_Time.y);
                        half4 overlayTexSample = SAMPLE_TEXTURE2D(_TextureOverlay,sampler_TextureOverlay,overlayMainTexUV);

                        half overlayTexMask = 1;
                        if (CheckLocalFlags(FLAG_BIT_OVERLAYTEXTURE_MASKMAP))
                        {
                            float2 overlayTexMaskUV = TRANSFORM_TEX(overlayTexUV,_TextureOverlayMask);
                            overlayTexMask = SAMPLE_TEXTURE2D(_TextureOverlayMask,sampler_TextureOverlayMask,overlayTexMaskUV);
                        }
                        color.rgb *= lerp(1,overlayTexSample.rgb,overlayTexSample.a*_TextureOverlayIntensity*overlayTexMask);
                        // return half4( lerp(overlayTexSample.rgb,1,overlayTexSample.a*_TextureOverlayIntensity),1);
                        color.a += _TextureOverlayIntensity;
                    }

                    UNITY_BRANCH
                    if(CheckLocalFlags(FLAG_BIT_FLASH))
                    {

                        // //因为颜色空间的特殊原因，这里的转换运算可能会造成性能热点，后续考虑优化。
                        // half3 invertColor = SRGBToLinear(1- LinearToSRGB(color.xyz));
                        // color.rgb = lerp(color.rgb,invertColor,_InvertIntensity);

                        half flashLuminace = luminance(color.rgb);

                        half2 flashTexUV ;
                        if(CheckLocalFlags(FLAG_BIT_FLASHTEXTURE_POLLARCOORD))
                        {
                            flashTexUV = polarCoordinates;
                        }
                        else
                        {
                            
                            flashTexUV = screenUV;
                        }

                        flashTexUV = TRANSFORM_TEX(flashTexUV,_FlashTexture);
                        flashTexUV = UVOffsetAnimaiton(flashTexUV,_FlashVec.xy,_Time.y);

                        half flashTexColor = SAMPLE_TEXTURE2D(_FlashTexture,sampler_FlashTexture,flashTexUV).r;
                        flashTexColor = pow(flashTexColor,_DeSaturateIntensity);

                        flashLuminace = lerp(flashLuminace,flashTexColor,_FlashTextureIntensity);
                        half RangeMin = _FlashGradientRange;
                        half RangeMax = RangeMin + _Contrast;
                        flashLuminace = SimpleSmoothstep(RangeMin,RangeMax,flashLuminace);
                        half3 finalBlackFlashColor = lerp(_FlashColor,_BlackFlashColor,_InvertIntensity);
                        half3 finalFlashColor =  lerp(_BlackFlashColor,_FlashColor,_InvertIntensity);
                        
                        half3 endColor = lerp(finalBlackFlashColor,finalFlashColor,flashLuminace);
                        color.rgb = lerp(color.rgb,endColor,_FlashIntensity);
                        color.a = 1;
                        

                        // half3 endColor = lerp(_BlackFlashColor,_FlashColor,luminance(color.rgb));
                        // color.rgb = lerp(color.rgb,endColor,_InvertIntensity);
                        //
                        // color.xyz = RgbToHsv(color.rgb);
                        // half3 colorHSV = color.xyz;
                        // color.y *= _DeSaturateIntensity;
                        // color.rgb = HsvToRgb(color.xyz);
                        //
                        // color.rgb = lerp(half3(0.5,0.5,0.5),color.rgb,_Contrast);
                        //
                        // color.a = 1;
                    }

                    UNITY_BRANCH
                    if(CheckLocalFlags(FLAG_BIT_VIGNETTE))
                    {
                        _VignetteVec.x *= _VignetteColor.a;
                        half2 screenVec = (_CustomScreenCenter.xy - screenUV)*_VignetteVec.x;
                        screenVec.x *= _VignetteVec.y;
                        half2 dist = dot(screenVec,screenVec);
                        dist = saturate(dist);
                        half factor = pow(1-dist,_VignetteVec.z) ;
                        // color.rgb *= lerp(_VignetteColor,(1.0).xxx,factor);
                        factor = 1- factor;
                        factor = lerp(factor,1,_VignetteVec.w);
                        // _VignetteColor *= factor;
                        color.rgb = lerp(color,_VignetteColor,factor);
                        color.a =1;
                    }

                    // return half4(_TextureOverlayIntensity.rrr,1);
                    // float finalIntensity = (_SpeedDistortVec.x+ _TextureOverlayIntensity +_InvertIntensity +(1- _DeSaturateIntensity)+_Contrast)*2;
                    // finalIntensity = saturate(finalIntensity);

                    // return half4(saturate(color.a).rrr,1);
                    
                    return half4(color.rgb,saturate(color.a));
                }
                ENDHLSL
            }
        }

}