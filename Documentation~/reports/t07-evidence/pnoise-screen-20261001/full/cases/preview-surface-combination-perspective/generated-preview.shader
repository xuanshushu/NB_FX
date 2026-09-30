Shader "NB FX/Shader Graph/NBFXMeshMasterPreview"
    {
        Properties
        {
            [NoScaleOffset]_BaseMap("BaseMap", 2D) = "white" {}
            [HDR]_Color("Color", Color) = (1, 1, 1, 1)
            _NB_DistortionNoise("NB_DistortionNoise", Vector, 2) = (0.25, 0, 0, 0)
            _NB_DistortionIntensity("NB_DistortionIntensity", Float) = 0.25
            _NB_DistortionMode("NB_DistortionMode", Float) = 0
            _NB_Flags0Lo16("NB_Flags0Lo16", Float) = 0
            _NB_Flags0Hi16("NB_Flags0Hi16", Float) = 0
            _NB_Flags1Lo16("NB_Flags1Lo16", Float) = 0
            _NB_Flags1Hi16("NB_Flags1Hi16", Float) = 0
            _NB_DistortionAlphaPow("NB_DistortionAlphaPow", Float) = 1
            _NB_DistortionAlphaMultiplier("NB_DistortionAlphaMultiplier", Float) = 1
            _NB_DistortionAlphaAdd("NB_DistortionAlphaAdd", Float) = 0
            _MaskMap("MaskMap", 2D) = "white" {}
            _Mask_Toggle("Mask_Toggle", Float) = 0
            _MaskMapVec("MaskMapVec", Vector, 4) = (1, 0, 0, 0)
            _MaskRefineVec("MaskRefineVec", Vector, 4) = (1, 1, 0, 0)
            _NB_ColorChannelLo16("NB_ColorChannelLo16", Float) = 3
            _BaseMap_ST("BaseMap ST", Vector, 4) = (1, 1, 0, 0)
            _BaseMapUVRotation("BaseMap UV Rotation", Float) = 0
            _BaseMapUVRotationSpeed("BaseMap UV Rotation Speed", Float) = 0
            _BaseMapMaskMapOffset("BaseMap MaskMap Offset", Vector, 4) = (0, 0, 0, 0)
            _MaskMapUVRotation("MaskMap UV Rotation", Float) = 0
            _MaskMapRotationSpeed("MaskMap Rotation Speed", Float) = 0
            _MaskMapOffsetAnition("MaskMap Offset Anition", Vector, 4) = (0, 0, 0, 0)
            _DissolveOffsetRotateDistort("Dissolve Offset Rotate Distort", Vector, 4) = (0, 0, 0, 0)
            _DissolveMaskMap("DissolveMaskMap", 2D) = "white" {}
            _DissolveMask_Toggle("DissolveMask_Toggle", Float) = 0
            [Enum(ProcessDissolve, 0, DissolveMask, 1)]_DissolveMaskMode("DissolveMaskMode", Float) = 0
            _MaskMap2("MaskMap2", 2D) = "white" {}
            _Mask2_Toggle("Mask2_Toggle", Float) = 0
            _MaskMap3("MaskMap3", 2D) = "white" {}
            _Mask3_Toggle("Mask3_Toggle", Float) = 0
            _MaskMap3OffsetAnition("MaskMap3OffsetAnition", Vector, 4) = (0, 0, 0, 0)
            _NB_WrapFlagsLo16("NB_WrapFlagsLo16", Float) = 0
            _NB_WrapFlagsHi16("NB_WrapFlagsHi16", Float) = 0
            _MaskMapGradientCount("MaskMapGradientCount", Float) = 2
            _MaskMapGradientFloat0("MaskMapGradientFloat0", Vector, 4) = (0, 0, 1, 1)
            _MaskMapGradientFloat1("MaskMapGradientFloat1", Vector, 4) = (1, 0, 1, 1)
            _MaskMapGradientFloat2("MaskMapGradientFloat2", Vector, 4) = (1, 0, 1, 1)
            _MaskMap2GradientCount("MaskMap2GradientCount", Float) = 2
            _MaskMap2GradientFloat0("MaskMap2GradientFloat0", Vector, 4) = (0, 0, 1, 1)
            _MaskMap2GradientFloat1("MaskMap2GradientFloat1", Vector, 4) = (1, 0, 1, 1)
            _MaskMap2GradientFloat2("MaskMap2GradientFloat2", Vector, 4) = (1, 0, 1, 1)
            _MaskMap3GradientCount("MaskMap3GradientCount", Float) = 2
            _MaskMap3GradientFloat0("MaskMap3GradientFloat0", Vector, 4) = (0, 0, 1, 1)
            _MaskMap3GradientFloat1("MaskMap3GradientFloat1", Vector, 4) = (1, 0, 1, 1)
            _MaskMap3GradientFloat2("MaskMap3GradientFloat2", Vector, 4) = (1, 0, 1, 1)
            _AlphaAll("AlphaAll", Float) = 1
            _ColorA("ColorA", Vector, 4) = (1, 1, 1, 1)
            _BaseColorIntensityForTimeline("BaseColorIntensityForTimeline", Range(0, 10)) = 1
            _fresnelEnabled("FresnelEnabled", Float) = 0
            _FresnelUnit("FresnelUnit", Vector, 4) = (0, 0.5, 1, 0.5)
            [HDR]_FresnelColor("FresnelColor", Color) = (1, 1, 1, 1)
            _FresnelRotation("FresnelRotation", Vector, 4) = (0, 0, 0, 0.5)
            _Dissolve_Vec2("DissolveLineRange", Vector, 4) = (0.2, 0.1, 0, 0)
            [HDR]_DissolveLineColor("DissolveLineColor", Color) = (1, 0, 0, 1)
            _Dissolve_useRampMap_Toggle("DissolveRampToggle", Float) = 0
            _DissolveRampMap("DissolveRampMap", 2D) = "white" {}
            _DissolveRampSourceMode("DissolveRampSourceMode", Float) = 0
            [HDR]_DissolveRampColor("DissolveRampColor", Color) = (1, 1, 1, 1)
            _DissolveRampCount("DissolveRampCount", Float) = 131074
            _DissolveRampColor0("DissolveRampColor0", Color) = (1, 0, 0, 0)
            _DissolveRampColor1("DissolveRampColor1", Color) = (0, 0, 0, 1)
            _DissolveRampColor2("DissolveRampColor2", Color) = (1, 1, 1, 1)
            _DissolveRampColor3("DissolveRampColor3", Color) = (1, 1, 1, 1)
            _DissolveRampColor4("DissolveRampColor4", Color) = (1, 1, 1, 1)
            _DissolveRampColor5("DissolveRampColor5", Color) = (1, 1, 1, 1)
            _DissolveRampAlpha0("DissolveRampAlpha0", Vector, 4) = (1, 0, 1, 1)
            _DissolveRampAlpha1("DissolveRampAlpha1", Vector, 4) = (1, 0, 1, 1)
            _DissolveRampAlpha2("DissolveRampAlpha2", Vector, 4) = (1, 0, 1, 1)
            _NB_ForceNoMipFlagsLo16("NB_ForceNoMipFlagsLo16", Float) = 0
            _NB_ForceNoMipFlagsHi16("NB_ForceNoMipFlagsHi16", Float) = 0
            _NB_DissolveRampSTOverrideEnabled("NB_DissolveRampSTOverrideEnabled", Float) = 0
            _NB_DissolveRampSTOverride("NB_DissolveRampSTOverride", Vector, 4) = (1, 1, 0, 0)
            _NoiseMap("NoiseMap", 2D) = "white" {}
            _noisemapEnabled("NoiseEnabled", Float) = 0
            _NoiseMapUVRotation("NoiseMapUVRotation", Float) = 0
            _NoiseOffset("NoiseOffset", Vector, 4) = (0, 0, 0, 0)
            _NoiseIntensity("NoiseIntensity", Float) = 1
            _DistortionDirection("DistortionDirection", Vector, 4) = (1, 1, 0, 0)
            _NoiseMaskMap("NoiseMaskMap", 2D) = "white" {}
            _noiseMaskMap_Toggle("NoiseMaskToggle", Float) = 0
            _TexDistortion_intensity("TexDistortionIntensity", Float) = 0.5
            _Emi_Distortion_intensity("EmiDistortionIntensity", Float) = 0
            _MaskDistortion_intensity("MaskDistortionIntensity", Float) = 0
            _Cutoff("Cutoff", Float) = 0.5
            _AdditiveToPreMultiplyAlphaLerp("AdditiveToPreMultiplyAlphaLerp", Float) = 0
            _VertexOffset_Map("VertexOffset_Map", 2D) = "white" {}
            _VertexOffset_Vec("VertexOffset_Vec", Vector, 4) = (0, 0, 1, 0)
            _VertexOffset_CustomDir("VertexOffset_CustomDir", Vector, 4) = (1, 1, 1, 0)
            _VertexOffset_NormalDir_Toggle("VertexOffset_NormalDir_Toggle", Float) = 0
            _VertexOffset_DirectionSpace("VertexOffset_DirectionSpace", Float) = 0
            _VertexOffset_Toggle("VertexOffset_Toggle", Float) = 0
            _VertexOffset_MaskMap("VertexOffset_MaskMap", 2D) = "white" {}
            _VertexOffset_MaskMap_Vec("VertexOffset_MaskMap_Vec", Vector, 4) = (0, 0, 1, 0)
            _VertexOffset_Mask_Toggle("VertexOffset_Mask_Toggle", Float) = 0
            _NB_ColorChannelHi16("NB_ColorChannelHi16", Float) = 0
            _MatCapToggle("MatCapToggle", Float) = 0
            [NoScaleOffset]_MatCapTex("MatCapTex", 2D) = "white" {}
            [HDR]_MatCapColor("MatCapColor", Color) = (1, 1, 1, 1)
            _MatCapInfo("MatCapInfo", Vector, 4) = (1, 0, 0, 0)
            _BumpMapToggle("BumpMapToggle", Float) = 0
            [Normal]_BumpTex("BumpTex", 2D) = "bump" {}
            _BumpScale("BumpScale", Float) = 1
            _FxLightMode("FxLightMode", Float) = 0
            _MaterialInfo("MaterialInfo", Vector, 4) = (1, 1, 0, 0)
            [HDR]_SpecularColor("SpecularColor", Color) = (1, 1, 1, 1)
            _ProgramNoise_Toggle("ProgramNoiseToggle", Float) = 0
            _ProgramNoise_Simple_Toggle("ProgramSimpleToggle", Float) = 0
            _ProgramNoise_Voronoi_Toggle("ProgramVoronoiToggle", Float) = 0
            _ProgramNoise_Rotate("ProgramNoiseRotate", Float) = 0
            _DissolveVoronoi_Vec("PNoiseVec", Vector, 4) = (1, 1, 2, 2)
            _DissolveVoronoi_Vec2("PNoiseVec2", Vector, 4) = (1, 1, 2, 2)
            _DissolveVoronoi_Vec3("PNoiseVec3", Vector, 4) = (0, 0, 0, 0)
            _DissolveVoronoi_Vec4("PNoiseVec4", Vector, 4) = (0, 0, 0, 0)
            _ProgramNoiseBaseBlendOpacity("PNoiseBaseBlendOpacity", Float) = 0
            _MaskPNoiseBlendOpacity("PNoiseMaskBlendOpacity", Float) = 1
            _DissolvePNoiseBlendOpacity("PNoiseDissolveBlendOpacity", Float) = 1
            _NB_PNoiseBlendLo16("PNoiseBlendLo16", Float) = 0
            _NB_PNoiseBlendHi16("PNoiseBlendHi16", Float) = 0
            [NoScaleOffset]_RigRTBk("RigRTBk", 2D) = "white" {}
            [NoScaleOffset]_RigLBtF("RigLBtF", 2D) = "white" {}
            [NoScaleOffset]_SixWayEmissionRamp("SixWayEmissionRamp", 2D) = "white" {}
            _SixWayInfo("SixWayInfo", Vector, 4) = (0.5, 0, 0, 0)
            [HDR]_SixWayEmissionColor("SixWayEmissionColor", Color) = (1, 0.5, 0, 1)
            _SixWayColorAbsorptionToggle("SixWayColorAbsorptionToggle", Float) = 0
            _DistortPNoiseBlendOpacity("PNoiseDistortBlendOpacity", Float) = 1
            _DissolveMap("DissolveMap", 2D) = "grey" {}
            _Dissolve_Toggle("Dissolve_Toggle", Float) = 0
            _Dissolve("Dissolve", Vector, 4) = (0.5, 1, 1, 0.1)
            [HDR]_BaseBackColor("BaseBackColor", Color) = (1, 1, 1, 1)
            _EmissionMap("EmissionMap", 2D) = "white" {}
            _EmissionEnabled("EmissionEnabled", Float) = 0
            _EmissionMapUVOffset("EmissionMapUVOffset", Vector, 4) = (0, 0, 0, 0)
            _EmissionMapUVRotation("EmissionMapUVRotation", Range(0, 360)) = 0
            [HDR]_EmissionMapColor("EmissionMapColor", Color) = (1, 1, 1, 1)
            _EmissionMapColorIntensity("EmissionMapColorIntensity", Float) = 1
            _EmissionAlphaIntensity("EmissionAlphaIntensity", Range(0, 1)) = 1
            _ColorBlendMap("ColorBlendMap", 2D) = "white" {}
            _ColorBlendMap_Toggle("ColorBlendMapToggle", Float) = 0
            _ColorBlendMapOffset("ColorBlendMapOffset", Vector, 4) = (0, 0, 0, 0)
            _ColorBlendVec("ColorBlendVec", Vector, 4) = (0, 0, 1, 0)
            [HDR]_ColorBlendColor("ColorBlendColor", Color) = (1, 1, 1, 1)
            _ColorBlendColorIntensity("ColorBlendColorIntensity", Float) = 1
            _RampColorMap("RampColorMap", 2D) = "white" {}
            _RampColorToggle("RampColorToggle", Float) = 0
            _RampColorSourceMode("RampColorSourceMode", Float) = 0
            _RampColorMapOffset("RampColorMapOffset", Vector, 4) = (0, 0, 0, 0)
            _RampColor0("RampColor0", Color) = (0, 0, 0, 0)
            _RampColor1("RampColor1", Color) = (1, 0, 0, 1)
            _RampColor2("RampColor2", Color) = (1, 1, 1, 1)
            _RampColor3("RampColor3", Color) = (1, 1, 1, 1)
            _RampColor4("RampColor4", Color) = (1, 1, 1, 1)
            _RampColor5("RampColor5", Color) = (1, 1, 1, 1)
            _RampColorAlpha0("RampColorAlpha0", Vector, 4) = (1, 0, 1, 1)
            _RampColorAlpha1("RampColorAlpha1", Vector, 4) = (1, 0, 1, 1)
            _RampColorAlpha2("RampColorAlpha2", Vector, 4) = (1, 0, 1, 1)
            _RampColorCount("RampColorCount", Float) = 131074
            [HDR]_RampColorBlendColor("RampColorBlendColor", Color) = (1, 1, 1, 1)
            _HueShift("HueShift", Float) = 0
            _Contrast("Contrast", Float) = 1
            _ContrastMidColor("ContrastMidColor", Color) = (0.5, 0.5, 0.5, 1)
            _Saturability("Saturability", Range(0, 1)) = 0
            _BaseMapColorRefine("BaseMapColorRefine", Vector, 4) = (1, 1, 2, 0.5)
            _DistanceFade_Toggle("DistanceFadeToggle", Float) = 0
            _Fade("Fade", Vector, 4) = (2, 4, 0, 0)
            _SoftParticlesEnabled("SoftParticlesEnabled", Float) = 0
            _SoftParticleFadeParams("SoftParticleFadeParams", Vector, 4) = (0, 0.5, 0, 0)
            _DepthOutline_Toggle("DepthOutlineToggle", Float) = 0
            [HDR]_DepthOutline_Color("DepthOutlineColor", Color) = (1, 1, 1, 1)
            _DepthOutline_Vec("DepthOutlineVec", Vector, 4) = (0, 0.5, 0, 0)
            _NB_UVModeFlag0Lo16("NB UV Mode 0 Lo16", Float) = 0
            _NB_UVModeFlag0Hi16("NB UV Mode 0 Hi16", Float) = 0
            _NB_UVModeFlagType0Lo16("NB UV Mode Type 0 Lo16", Float) = 0
            _NB_UVModeFlagType0Hi16("NB UV Mode Type 0 Hi16", Float) = 0
            _SharedUV_ST("Shared UV ST", Vector, 4) = (1, 1, 0, 0)
            _SharedUV_Vec("Shared UV Vec", Vector, 4) = (0, 0, 0, 0)
            _TWParameter("Twirl Center", Vector, 4) = (0.5, 0.5, 0, 0)
            _TWStrength("Twirl Strength", Float) = 1
            _PCCenter("Polar Center And Strength", Vector, 4) = (0.5, 0.5, 1, 0)
            [HideInInspector]_CastShadows("_CastShadows", Float) = 0
            [HideInInspector]_Surface("_Surface", Float) = 1
            [HideInInspector]_Blend("_Blend", Float) = 0
            [HideInInspector]_AlphaClip("_AlphaClip", Float) = 0
            [HideInInspector]_SrcBlend("_SrcBlend", Float) = 1
            [HideInInspector]_DstBlend("_DstBlend", Float) = 0
            [HideInInspector]_SrcBlendAlpha("_SrcBlendAlpha", Float) = 1
            [HideInInspector]_DstBlendAlpha("_DstBlendAlpha", Float) = 0
            [HideInInspector][ToggleUI]_ZWrite("_ZWrite", Float) = 0
            [HideInInspector]_ZWriteControl("_ZWriteControl", Float) = 0
            [HideInInspector]_ZTest("_ZTest", Float) = 4
            [HideInInspector]_Cull("_Cull", Float) = 0
            [HideInInspector]_AlphaToMask("_AlphaToMask", Float) = 0
            [HideInInspector]_QueueOffset("_QueueOffset", Float) = 0
            [HideInInspector]_QueueControl("_QueueControl", Float) = -1
            [HideInInspector]_ColorMask("_ColorMask", Float) = 15
            [HideInInspector]_Stencil("_Stencil", Float) = 0
            [HideInInspector]_StencilComp("_StencilComp", Float) = 8
            [HideInInspector]_StencilOp("_StencilOp", Float) = 0
            [HideInInspector]_StencilFail("_StencilFail", Float) = 0
            [HideInInspector]_StencilZFail("_StencilZFail", Float) = 0
            [HideInInspector]_StencilReadMask("_StencilReadMask", Float) = 255
            [HideInInspector]_StencilWriteMask("_StencilWriteMask", Float) = 255
            [HideInInspector][NoScaleOffset]unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
            [HideInInspector][NoScaleOffset]unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
            [HideInInspector][NoScaleOffset]unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
        }
        SubShader
        {
            Tags
            {
                "RenderPipeline"="UniversalPipeline"
                "RenderType"="Transparent"
                "UniversalMaterialType" = "Unlit"
                "Queue"="Transparent"
                "DisableBatching"="False"
                "ShaderGraphShader"="true"
                "ShaderGraphTargetId"="NBGraphUnlitSubTarget"
            }
            Pass
            {
                Name "Universal Forward"
                Tags
                {
                    // LightMode: <None>
                }
            
            // Render State
            Cull [_Cull]
                Blend [_SrcBlend] [_DstBlend], [_SrcBlendAlpha] [_DstBlendAlpha]
                ZTest [_ZTest]
                ZWrite [_ZWrite]
                ColorMask [_ColorMask]
                Stencil
                {
                ReadMask [_StencilReadMask]
                WriteMask [_StencilWriteMask]
                Ref [_Stencil]
                CompFront [_StencilComp]
                ZFailFront [_StencilZFail]
                FailFront [_StencilFail]
                PassFront [_StencilOp]
                CompBack [_StencilComp]
                ZFailBack [_StencilZFail]
                FailBack [_StencilFail]
                PassBack [_StencilOp]
                }
                AlphaToMask [_AlphaToMask]
            
            // Debug
            // <None>
            
            // --------------------------------------------------
            // Pass
            
            HLSLPROGRAM
            
            // Pragmas
            #pragma target 2.0
                #pragma multi_compile_instancing
                #pragma instancing_options renderinglayer
                #pragma vertex vert
                #pragma fragment frag
            
            // Keywords
            #pragma multi_compile _ LIGHTMAP_ON
                #pragma multi_compile _ DIRLIGHTMAP_COMBINED
                #pragma multi_compile _ USE_LEGACY_LIGHTMAPS
                #pragma multi_compile _ LIGHTMAP_BICUBIC_SAMPLING
                #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
                #pragma multi_compile_fragment _ DEBUG_DISPLAY
                #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
                #pragma shader_feature_fragment _ _SURFACE_TYPE_TRANSPARENT
                #pragma shader_feature_local_fragment _ _ALPHAPREMULTIPLY_ON
                #pragma shader_feature_local_fragment _ _ALPHAMODULATE_ON
                #pragma shader_feature_local_fragment _ _ALPHATEST_ON
                #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
                #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
                #pragma shader_feature_local_fragment _ VFX_SIX_WAY_ABSORPTION
            // GraphKeywords: <None>
            
            // Defines
            
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define ATTRIBUTES_NEED_TEXCOORD1
            #define ATTRIBUTES_NEED_TEXCOORD2
            #define ATTRIBUTES_NEED_COLOR
            #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
            #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
            #define VARYINGS_NEED_POSITION_WS
            #define VARYINGS_NEED_NORMAL_WS
            #define VARYINGS_NEED_TANGENT_WS
            #define VARYINGS_NEED_TEXCOORD0
            #define VARYINGS_NEED_TEXCOORD1
            #define VARYINGS_NEED_TEXCOORD2
            #define VARYINGS_NEED_COLOR
            #define VARYINGS_NEED_CULLFACE
            #define FEATURES_GRAPH_VERTEX
            /* WARNING: $splice Could not find named fragment 'PassInstancing' */
            #define SHADERPASS SHADERPASS_UNLIT
                #define SHADERGRAPH_PREVIEW_MAIN
                #define _FOG_FRAGMENT 1
            
            
            // custom interpolator pre-include
            /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
            
            // Includes
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
            
            // --------------------------------------------------
            // Structs and Packing
            
            // custom interpolators pre packing
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
            
            struct Attributes
                {
                     float3 positionOS : POSITION;
                     float3 normalOS : NORMAL;
                     float4 tangentOS : TANGENT;
                     float4 uv0 : TEXCOORD0;
                     float4 uv1 : TEXCOORD1;
                     float4 uv2 : TEXCOORD2;
                     float4 color : COLOR;
                    #if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
                     uint instanceID : INSTANCEID_SEMANTIC;
                    #endif
                };
                struct Varyings
                {
                     float4 positionCS : SV_POSITION;
                     float3 positionWS;
                     float3 normalWS;
                     float4 tangentWS;
                     float4 texCoord0;
                     float4 texCoord1;
                     float4 texCoord2;
                     float4 color;
                    #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
                     uint instanceID : CUSTOM_INSTANCE_ID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                    #endif
                     float3 SixBake0;
                     float3 SixBake1;
                     float3 SixBake2;
                     float3 SixBack0;
                     float3 SixBack1;
                     float3 SixBack2;
                     float4 SixTangentSigned;
                };
                struct SurfaceDescriptionInputs
                {
                     float3 WorldSpaceNormal;
                     float3 WorldSpaceTangent;
                     float3 WorldSpaceBiTangent;
                     float3 WorldSpaceViewDirection;
                     float3 ViewSpacePosition;
                     float3 WorldSpacePosition;
                     float2 NDCPosition;
                     float2 PixelPosition;
                     float4 uv0;
                     float4 uv1;
                     float4 uv2;
                     float4 VertexColor;
                     float FaceSign;
                     float3 SixBake0;
                     float3 SixBake1;
                     float3 SixBake2;
                     float3 SixBack0;
                     float3 SixBack1;
                     float3 SixBack2;
                     float4 SixTangentSigned;
                };
                struct VertexDescriptionInputs
                {
                     float3 ObjectSpaceNormal;
                     float3 WorldSpaceNormal;
                     float3 ObjectSpaceTangent;
                     float3 WorldSpaceTangent;
                     float3 ObjectSpaceBiTangent;
                     float3 WorldSpaceBiTangent;
                     float3 ObjectSpacePosition;
                     float4 uv0;
                     float4 uv1;
                     float4 uv2;
                     float4 VertexColor;
                };
                struct PackedVaryings
                {
                     float4 positionCS : SV_POSITION;
                     float4 tangentWS : INTERP0;
                     float4 texCoord0 : INTERP1;
                     float4 texCoord1 : INTERP2;
                     float4 texCoord2 : INTERP3;
                     float4 color : INTERP4;
                     float4 SixTangentSigned : INTERP5;
                     float4 packed_positionWS_SixBack1x : INTERP6;
                     float4 packed_normalWS_SixBack1y : INTERP7;
                     float4 packed_SixBake0_SixBack1z : INTERP8;
                     float4 packed_SixBake1_SixBack2x : INTERP9;
                     float4 packed_SixBake2_SixBack2y : INTERP10;
                     float4 packed_SixBack0_SixBack2z : INTERP11;
                    #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
                     uint instanceID : CUSTOM_INSTANCE_ID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                    #endif
                };
            
            PackedVaryings PackVaryings (Varyings input)
                {
                    PackedVaryings output;
                    ZERO_INITIALIZE(PackedVaryings, output);
                    output.positionCS = input.positionCS;
                    output.tangentWS.xyzw = input.tangentWS;
                    output.texCoord0.xyzw = input.texCoord0;
                    output.texCoord1.xyzw = input.texCoord1;
                    output.texCoord2.xyzw = input.texCoord2;
                    output.color.xyzw = input.color;
                    output.SixTangentSigned.xyzw = input.SixTangentSigned;
                    output.packed_positionWS_SixBack1x.xyz = input.positionWS;
                    output.packed_positionWS_SixBack1x.w = input.SixBack1.x;
                    output.packed_normalWS_SixBack1y.xyz = input.normalWS;
                    output.packed_normalWS_SixBack1y.w = input.SixBack1.y;
                    output.packed_SixBake0_SixBack1z.xyz = input.SixBake0;
                    output.packed_SixBake0_SixBack1z.w = input.SixBack1.z;
                    output.packed_SixBake1_SixBack2x.xyz = input.SixBake1;
                    output.packed_SixBake1_SixBack2x.w = input.SixBack2.x;
                    output.packed_SixBake2_SixBack2y.xyz = input.SixBake2;
                    output.packed_SixBake2_SixBack2y.w = input.SixBack2.y;
                    output.packed_SixBack0_SixBack2z.xyz = input.SixBack0;
                    output.packed_SixBack0_SixBack2z.w = input.SixBack2.z;
                    #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
                    output.instanceID = input.instanceID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
                    #endif
                    return output;
                }
                
                Varyings UnpackVaryings (PackedVaryings input)
                {
                    Varyings output;
                    output.positionCS = input.positionCS;
                    output.tangentWS = input.tangentWS.xyzw;
                    output.texCoord0 = input.texCoord0.xyzw;
                    output.texCoord1 = input.texCoord1.xyzw;
                    output.texCoord2 = input.texCoord2.xyzw;
                    output.color = input.color.xyzw;
                    output.SixTangentSigned = input.SixTangentSigned.xyzw;
                    output.positionWS = input.packed_positionWS_SixBack1x.xyz;
                    output.SixBack1.x = input.packed_positionWS_SixBack1x.w;
                    output.normalWS = input.packed_normalWS_SixBack1y.xyz;
                    output.SixBack1.y = input.packed_normalWS_SixBack1y.w;
                    output.SixBake0 = input.packed_SixBake0_SixBack1z.xyz;
                    output.SixBack1.z = input.packed_SixBake0_SixBack1z.w;
                    output.SixBake1 = input.packed_SixBake1_SixBack2x.xyz;
                    output.SixBack2.x = input.packed_SixBake1_SixBack2x.w;
                    output.SixBake2 = input.packed_SixBake2_SixBack2y.xyz;
                    output.SixBack2.y = input.packed_SixBake2_SixBack2y.w;
                    output.SixBack0 = input.packed_SixBack0_SixBack2z.xyz;
                    output.SixBack2.z = input.packed_SixBack0_SixBack2z.w;
                    #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
                    output.instanceID = input.instanceID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
                    #endif
                    return output;
                }
                
            
            // --------------------------------------------------
            // Graph
            
            // Graph Properties
            CBUFFER_START(UnityPerMaterial)
                float4 _NBGraphBaseColorCustomFunction_f712bc264a1c40848edc2dd476a4cada_SampledAlbedo_0_Vector4;
                float _NBGraphBaseColorCustomFunction_f712bc264a1c40848edc2dd476a4cada_SelectedAlpha_1_Float;
                TEXTURE2D(_BaseMap);
                SAMPLER(sampler_BaseMap);
                float4 _BaseMap_TexelSize;
                float4 _Color;
                float2 _NB_DistortionNoise;
                float _NB_DistortionIntensity;
                float _NB_DistortionMode;
                float _NB_Flags0Lo16;
                float _NB_Flags0Hi16;
                float _NB_Flags1Lo16;
                float _NB_Flags1Hi16;
                float _NB_DistortionAlphaPow;
                float _NB_DistortionAlphaMultiplier;
                float _NB_DistortionAlphaAdd;
                TEXTURE2D(_MaskMap);
                SAMPLER(sampler_MaskMap);
                float4 _MaskMap_TexelSize;
                float4 _MaskMap_ST;
                float _Mask_Toggle;
                float4 _MaskMapVec;
                float4 _MaskRefineVec;
                float _NB_ColorChannelLo16;
                TEXTURE2D(_DissolveMap);
                SAMPLER(sampler_DissolveMap);
                float4 _DissolveMap_TexelSize;
                float4 _DissolveMap_ST;
                float _Dissolve_Toggle;
                float4 _Dissolve;
                float4 _BaseMap_ST;
                float _BaseMapUVRotation;
                float _BaseMapUVRotationSpeed;
                float4 _BaseMapMaskMapOffset;
                float _MaskMapUVRotation;
                float _MaskMapRotationSpeed;
                float4 _MaskMapOffsetAnition;
                float4 _DissolveOffsetRotateDistort;
                TEXTURE2D(_DissolveMaskMap);
                SAMPLER(sampler_DissolveMaskMap);
                float4 _DissolveMaskMap_TexelSize;
                float4 _DissolveMaskMap_ST;
                float _DissolveMask_Toggle;
                float _DissolveMaskMode;
                TEXTURE2D(_MaskMap2);
                SAMPLER(sampler_MaskMap2);
                float4 _MaskMap2_TexelSize;
                float4 _MaskMap2_ST;
                float _Mask2_Toggle;
                TEXTURE2D(_MaskMap3);
                SAMPLER(sampler_MaskMap3);
                float4 _MaskMap3_TexelSize;
                float4 _MaskMap3_ST;
                float _Mask3_Toggle;
                float4 _MaskMap3OffsetAnition;
                float _NB_WrapFlagsLo16;
                float _NB_WrapFlagsHi16;
                float _MaskMapGradientCount;
                float4 _MaskMapGradientFloat0;
                float4 _MaskMapGradientFloat1;
                float4 _MaskMapGradientFloat2;
                float _MaskMap2GradientCount;
                float4 _MaskMap2GradientFloat0;
                float4 _MaskMap2GradientFloat1;
                float4 _MaskMap2GradientFloat2;
                float _MaskMap3GradientCount;
                float4 _MaskMap3GradientFloat0;
                float4 _MaskMap3GradientFloat1;
                float4 _MaskMap3GradientFloat2;
                float _AlphaAll;
                float4 _ColorA;
                float _BaseColorIntensityForTimeline;
                float4 _BaseBackColor;
                TEXTURE2D(_EmissionMap);
                SAMPLER(sampler_EmissionMap);
                float4 _EmissionMap_TexelSize;
                float4 _EmissionMap_ST;
                float _EmissionEnabled;
                float4 _EmissionMapUVOffset;
                float _EmissionMapUVRotation;
                float4 _EmissionMapColor;
                float _EmissionMapColorIntensity;
                float _EmissionAlphaIntensity;
                TEXTURE2D(_ColorBlendMap);
                SAMPLER(sampler_ColorBlendMap);
                float4 _ColorBlendMap_TexelSize;
                float4 _ColorBlendMap_ST;
                float _ColorBlendMap_Toggle;
                float4 _ColorBlendMapOffset;
                float4 _ColorBlendVec;
                float4 _ColorBlendColor;
                float _ColorBlendColorIntensity;
                TEXTURE2D(_RampColorMap);
                SAMPLER(sampler_RampColorMap);
                float4 _RampColorMap_TexelSize;
                float4 _RampColorMap_ST;
                float _RampColorToggle;
                float _RampColorSourceMode;
                float4 _RampColorMapOffset;
                float4 _RampColor0;
                float4 _RampColor1;
                float4 _RampColor2;
                float4 _RampColor3;
                float4 _RampColor4;
                float4 _RampColor5;
                float4 _RampColorAlpha0;
                float4 _RampColorAlpha1;
                float4 _RampColorAlpha2;
                float _RampColorCount;
                float4 _RampColorBlendColor;
                float _HueShift;
                float _Contrast;
                float4 _ContrastMidColor;
                float _Saturability;
                float4 _BaseMapColorRefine;
                float _fresnelEnabled;
                float4 _FresnelUnit;
                float4 _FresnelColor;
                float4 _FresnelRotation;
                float4 _Dissolve_Vec2;
                float4 _DissolveLineColor;
                float _Dissolve_useRampMap_Toggle;
                TEXTURE2D(_DissolveRampMap);
                SAMPLER(sampler_DissolveRampMap);
                float4 _DissolveRampMap_TexelSize;
                float4 _DissolveRampMap_ST;
                float _DissolveRampSourceMode;
                float4 _DissolveRampColor;
                float _DissolveRampCount;
                float4 _DissolveRampColor0;
                float4 _DissolveRampColor1;
                float4 _DissolveRampColor2;
                float4 _DissolveRampColor3;
                float4 _DissolveRampColor4;
                float4 _DissolveRampColor5;
                float4 _DissolveRampAlpha0;
                float4 _DissolveRampAlpha1;
                float4 _DissolveRampAlpha2;
                float _NB_ForceNoMipFlagsLo16;
                float _NB_ForceNoMipFlagsHi16;
                float _NB_DissolveRampSTOverrideEnabled;
                float4 _NB_DissolveRampSTOverride;
                float _DistanceFade_Toggle;
                float4 _Fade;
                float _SoftParticlesEnabled;
                float4 _SoftParticleFadeParams;
                float _DepthOutline_Toggle;
                float4 _DepthOutline_Color;
                float4 _DepthOutline_Vec;
                float _NB_UVModeFlag0Lo16;
                float _NB_UVModeFlag0Hi16;
                float _NB_UVModeFlagType0Lo16;
                float _NB_UVModeFlagType0Hi16;
                float4 _SharedUV_ST;
                float4 _SharedUV_Vec;
                float4 _TWParameter;
                float _TWStrength;
                float4 _PCCenter;
                TEXTURE2D(_NoiseMap);
                SAMPLER(sampler_NoiseMap);
                float4 _NoiseMap_TexelSize;
                float4 _NoiseMap_ST;
                float _noisemapEnabled;
                float _NoiseMapUVRotation;
                float4 _NoiseOffset;
                float _NoiseIntensity;
                float4 _DistortionDirection;
                TEXTURE2D(_NoiseMaskMap);
                SAMPLER(sampler_NoiseMaskMap);
                float4 _NoiseMaskMap_TexelSize;
                float4 _NoiseMaskMap_ST;
                float _noiseMaskMap_Toggle;
                float _TexDistortion_intensity;
                float _Emi_Distortion_intensity;
                float _MaskDistortion_intensity;
                float _Cutoff;
                float _AdditiveToPreMultiplyAlphaLerp;
                TEXTURE2D(_VertexOffset_Map);
                SAMPLER(sampler_VertexOffset_Map);
                float4 _VertexOffset_Map_TexelSize;
                float4 _VertexOffset_Map_ST;
                float4 _VertexOffset_Vec;
                float4 _VertexOffset_CustomDir;
                float _VertexOffset_NormalDir_Toggle;
                float _VertexOffset_DirectionSpace;
                float _VertexOffset_Toggle;
                TEXTURE2D(_VertexOffset_MaskMap);
                SAMPLER(sampler_VertexOffset_MaskMap);
                float4 _VertexOffset_MaskMap_TexelSize;
                float4 _VertexOffset_MaskMap_ST;
                float4 _VertexOffset_MaskMap_Vec;
                float _VertexOffset_Mask_Toggle;
                float _NB_ColorChannelHi16;
                float _MatCapToggle;
                TEXTURE2D(_MatCapTex);
                SAMPLER(sampler_MatCapTex);
                float4 _MatCapColor;
                float4 _MatCapInfo;
                float _BumpMapToggle;
                TEXTURE2D(_BumpTex);
                SAMPLER(sampler_BumpTex);
                float4 _BumpTex_ST;
                float _BumpScale;
                float _FxLightMode;
                float4 _MaterialInfo;
                float4 _SpecularColor;
                float _ProgramNoise_Toggle;
                float _ProgramNoise_Simple_Toggle;
                float _ProgramNoise_Voronoi_Toggle;
                float _ProgramNoise_Rotate;
                float4 _DissolveVoronoi_Vec;
                float4 _DissolveVoronoi_Vec2;
                float4 _DissolveVoronoi_Vec3;
                float4 _DissolveVoronoi_Vec4;
                float _ProgramNoiseBaseBlendOpacity;
                float _MaskPNoiseBlendOpacity;
                float _DissolvePNoiseBlendOpacity;
                float _NB_PNoiseBlendLo16;
                float _NB_PNoiseBlendHi16;
                TEXTURE2D(_RigRTBk);
                SAMPLER(sampler_RigRTBk);
                TEXTURE2D(_RigLBtF);
                SAMPLER(sampler_RigLBtF);
                TEXTURE2D(_SixWayEmissionRamp);
                SAMPLER(sampler_SixWayEmissionRamp);
                float4 _SixWayInfo;
                float4 _SixWayEmissionColor;
                float _SixWayColorAbsorptionToggle;
                float _DistortPNoiseBlendOpacity;
                UNITY_TEXTURE_STREAMING_DEBUG_VARS;
                CBUFFER_END
                #define UNITY_ACCESS_HYBRID_INSTANCED_PROP(var, type) var
            
            // Graph Includes
            #include_with_pragmas "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBGraphBaseColor.hlsl"
            #include_with_pragmas "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBGraphVertexOffset.hlsl"
            #include_with_pragmas "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBGraphBaseUV.hlsl"
            
            // -- Property used by ScenePickingPass
            #ifdef SCENEPICKINGPASS
            float4 _SelectionID;
            #endif
            
            // -- Properties used by SceneSelectionPass
            #ifdef SCENESELECTIONPASS
            int _ObjectId;
            int _PassValue;
            #endif
            
            // Graph Functions
            // GraphFunctions: <None>
            
            // Custom interpolators pre vertex
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
            
            // Graph Vertex
            struct VertexDescription
                {
                    float3 Position;
                    float3 Normal;
                    float3 Tangent;
                    float3 SixBake0;
                    float3 SixBake1;
                    float3 SixBake2;
                    float3 SixBack0;
                    float3 SixBack1;
                    float3 SixBack2;
                    float4 SixTangentSigned;
                };
                
                VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
                {
                    VertexDescription description = (VertexDescription)0;
                    float3 _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Bake0_3_Vector3;
                    float3 _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Bake1_4_Vector3;
                    float3 _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Bake2_5_Vector3;
                    float3 _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Back0_6_Vector3;
                    float3 _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Back1_7_Vector3;
                    float3 _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Back2_8_Vector3;
                    float4 _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_TangentSigned_9_Vector4;
                    NBGraphSixWayBake_float(IN.WorldSpaceNormal, IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Bake0_3_Vector3, _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Bake1_4_Vector3, _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Bake2_5_Vector3, _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Back0_6_Vector3, _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Back1_7_Vector3, _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Back2_8_Vector3, _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_TangentSigned_9_Vector4);
                    float4 _UV_596a5b2640e15e43aff23ea3c9c87059_Out_0_Vector4 = IN.uv0;
                    float4 _UV_7fa8f9025e9059649e9bdc291afd4417_Out_0_Vector4 = IN.uv1;
                    float4 _UV_8576cf3978f855598e9e2c2e37cdc003_Out_0_Vector4 = IN.uv2;
                    UnityTexture2D _Property_6f7cd0164d485a30803eb055841cd1ce_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_VertexOffset_Map, sampler_VertexOffset_Map, _VertexOffset_Map_TexelSize, _VertexOffset_Map_ST, float4(0, 0, 0, 0));
                    float4 _Property_fd85324fa4f658d691990180b8f8793e_Out_0_Vector4 = _VertexOffset_Vec;
                    float4 _Property_ccc6a218db89506f996c9a8c85bf89d4_Out_0_Vector4 = _VertexOffset_CustomDir;
                    float _Property_8cf30747736a58bcb43ec6620008dd56_Out_0_Float = _VertexOffset_NormalDir_Toggle;
                    float _Property_d1162da7b0a7551b80f631805d2a7134_Out_0_Float = _VertexOffset_DirectionSpace;
                    float _Property_18ffb5b24a215c74989dff20b668c91b_Out_0_Float = _VertexOffset_Toggle;
                    UnityTexture2D _Property_33de3bd0609655b7b71629b91a283fa1_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_VertexOffset_MaskMap, sampler_VertexOffset_MaskMap, _VertexOffset_MaskMap_TexelSize, _VertexOffset_MaskMap_ST, float4(0, 0, 0, 0));
                    float4 _Property_c972b51415e4592b9f07b2c052809e33_Out_0_Vector4 = _VertexOffset_MaskMap_Vec;
                    float _Property_01f9ee3ac21a544aa8c1fa82346bd03e_Out_0_Float = _VertexOffset_Mask_Toggle;
                    float _Property_849d65e3adfb5e4f996dd29864f2c86f_Out_0_Float = _NB_Flags0Lo16;
                    float _Property_3198e5a482ff5fb79bfc8ef49e694023_Out_0_Float = _NB_Flags0Hi16;
                    float _Property_c9800dd9977850049d324b45be73cc33_Out_0_Float = _NB_Flags1Lo16;
                    float _Property_dea9b3365dc35d0daf45f162fe71ae92_Out_0_Float = _NB_Flags1Hi16;
                    float _Property_a457aad3783c559d96ddfe0cf5d647ea_Out_0_Float = _NB_WrapFlagsLo16;
                    float _Property_14f98c3cf7b7525d8340b90ff8e5fd35_Out_0_Float = _NB_WrapFlagsHi16;
                    float _Property_560a4b92f5295597bb1175a0f82a5c76_Out_0_Float = _NB_ColorChannelLo16;
                    float _Property_770d2aaf793952348bc222b930283f2b_Out_0_Float = _NB_ColorChannelHi16;
                    float _Property_efa6ef74db395e619e03f11b575a1029_Out_0_Float = _NB_UVModeFlag0Lo16;
                    float _Property_ef246b0486a15ce4a55423fe806ebb20_Out_0_Float = _NB_UVModeFlag0Hi16;
                    float _Property_214c5c87c6c754658d4b9d2883ae02c5_Out_0_Float = _NB_UVModeFlagType0Lo16;
                    float _Property_b9de1de3181c5b9b9fcdb4836a81b926_Out_0_Float = _NB_UVModeFlagType0Hi16;
                    float4 _Property_9e2af066867e551bb1eed3199acfe809_Out_0_Vector4 = _SharedUV_ST;
                    float4 _Property_614e89fe251b5185af927c4ee7175f5f_Out_0_Vector4 = _SharedUV_Vec;
                    float4 _Property_b2c755308b2759418ed105ab21db3493_Out_0_Vector4 = _TWParameter;
                    float _Property_132da21d369f53e9b55031e5415fe894_Out_0_Float = _TWStrength;
                    float4 _Property_3561d005d5bb5f4ab0d3e545e06a5780_Out_0_Vector4 = _PCCenter;
                    float3 _NBGraphVertexOffsetCustomFunction_395601997b2e51c795e7c1ec707a5553_OutPositionOS_33_Vector3;
                    float3 _NBGraphVertexOffsetCustomFunction_395601997b2e51c795e7c1ec707a5553_OutNormalOS_34_Vector3;
                    float3 _NBGraphVertexOffsetCustomFunction_395601997b2e51c795e7c1ec707a5553_OutTangentOS_35_Vector3;
                    float _NBGraphVertexOffsetCustomFunction_395601997b2e51c795e7c1ec707a5553_Supported_36_Float;
                    NBGraphVertexOffset_float(IN.ObjectSpacePosition, IN.ObjectSpaceNormal, IN.ObjectSpaceTangent, (IN.VertexColor.xyz), _UV_596a5b2640e15e43aff23ea3c9c87059_Out_0_Vector4, _UV_7fa8f9025e9059649e9bdc291afd4417_Out_0_Vector4, _UV_8576cf3978f855598e9e2c2e37cdc003_Out_0_Vector4, _Property_6f7cd0164d485a30803eb055841cd1ce_Out_0_Texture2D, _Property_fd85324fa4f658d691990180b8f8793e_Out_0_Vector4, (_Property_ccc6a218db89506f996c9a8c85bf89d4_Out_0_Vector4.xyz), _Property_8cf30747736a58bcb43ec6620008dd56_Out_0_Float, _Property_d1162da7b0a7551b80f631805d2a7134_Out_0_Float, _Property_18ffb5b24a215c74989dff20b668c91b_Out_0_Float, _Property_33de3bd0609655b7b71629b91a283fa1_Out_0_Texture2D, (_Property_c972b51415e4592b9f07b2c052809e33_Out_0_Vector4.xyz), _Property_01f9ee3ac21a544aa8c1fa82346bd03e_Out_0_Float, _Property_849d65e3adfb5e4f996dd29864f2c86f_Out_0_Float, _Property_3198e5a482ff5fb79bfc8ef49e694023_Out_0_Float, _Property_c9800dd9977850049d324b45be73cc33_Out_0_Float, _Property_dea9b3365dc35d0daf45f162fe71ae92_Out_0_Float, _Property_a457aad3783c559d96ddfe0cf5d647ea_Out_0_Float, _Property_14f98c3cf7b7525d8340b90ff8e5fd35_Out_0_Float, _Property_560a4b92f5295597bb1175a0f82a5c76_Out_0_Float, _Property_770d2aaf793952348bc222b930283f2b_Out_0_Float, _Property_efa6ef74db395e619e03f11b575a1029_Out_0_Float, _Property_ef246b0486a15ce4a55423fe806ebb20_Out_0_Float, _Property_214c5c87c6c754658d4b9d2883ae02c5_Out_0_Float, _Property_b9de1de3181c5b9b9fcdb4836a81b926_Out_0_Float, _Property_9e2af066867e551bb1eed3199acfe809_Out_0_Vector4, _Property_614e89fe251b5185af927c4ee7175f5f_Out_0_Vector4, _Property_b2c755308b2759418ed105ab21db3493_Out_0_Vector4, _Property_132da21d369f53e9b55031e5415fe894_Out_0_Float, _Property_3561d005d5bb5f4ab0d3e545e06a5780_Out_0_Vector4, _NBGraphVertexOffsetCustomFunction_395601997b2e51c795e7c1ec707a5553_OutPositionOS_33_Vector3, _NBGraphVertexOffsetCustomFunction_395601997b2e51c795e7c1ec707a5553_OutNormalOS_34_Vector3, _NBGraphVertexOffsetCustomFunction_395601997b2e51c795e7c1ec707a5553_OutTangentOS_35_Vector3, _NBGraphVertexOffsetCustomFunction_395601997b2e51c795e7c1ec707a5553_Supported_36_Float);
                    description.Position = _NBGraphVertexOffsetCustomFunction_395601997b2e51c795e7c1ec707a5553_OutPositionOS_33_Vector3;
                    description.Normal = _NBGraphVertexOffsetCustomFunction_395601997b2e51c795e7c1ec707a5553_OutNormalOS_34_Vector3;
                    description.Tangent = _NBGraphVertexOffsetCustomFunction_395601997b2e51c795e7c1ec707a5553_OutTangentOS_35_Vector3;
                    description.SixBake0 = _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Bake0_3_Vector3;
                    description.SixBake1 = _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Bake1_4_Vector3;
                    description.SixBake2 = _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Bake2_5_Vector3;
                    description.SixBack0 = _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Back0_6_Vector3;
                    description.SixBack1 = _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Back1_7_Vector3;
                    description.SixBack2 = _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Back2_8_Vector3;
                    description.SixTangentSigned = _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_TangentSigned_9_Vector4;
                    return description;
                }
            
            // Custom interpolators, pre surface
            #ifdef FEATURES_GRAPH_VERTEX
            Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
            {
            output.SixBake0 = input.SixBake0;
            output.SixBake1 = input.SixBake1;
            output.SixBake2 = input.SixBake2;
            output.SixBack0 = input.SixBack0;
            output.SixBack1 = input.SixBack1;
            output.SixBack2 = input.SixBack2;
            output.SixTangentSigned = input.SixTangentSigned;
            return output;
            }
            #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
            #endif
            
            // Graph Pixel
            struct SurfaceDescription
                {
                    float3 BaseColor;
                    float Alpha;
                    float AlphaClipThreshold;
                    float2 NBDistortionSignedRG;
                    float NBDistortionNoiseMask;
                };
                
                SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
                {
                    SurfaceDescription surface = (SurfaceDescription)0;
                    float4 _Property_13840ad57da543e2b33275638ce1189e_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_Color) : _Color;
                    float _Property_eafd46b75a1746f093439de914a3717c_Out_0_Float = _NB_Flags0Lo16;
                    float _Property_02448b2e80a0440b804602e588eb2727_Out_0_Float = _NB_Flags0Hi16;
                    float _Property_52b95ce95e564cb083a7953179be1554_Out_0_Float = _NB_Flags1Lo16;
                    float _Property_6abc2ac395a54d7f99af7850796b2c20_Out_0_Float = _NB_Flags1Hi16;
                    float2 _Property_4fbc64ac55184ee8b2348b861c93db8b_Out_0_Vector2 = _NB_DistortionNoise;
                    float _Property_9099f47df6d24486a9e0bb9dd8fa7cf8_Out_0_Float = _NB_DistortionIntensity;
                    float _Property_01827f8348b040028326054a1623cc84_Out_0_Float = _NB_DistortionMode;
                    float _Property_87436152ca164e73877206654813b186_Out_0_Float = _NB_DistortionAlphaPow;
                    float _Property_9aab3b8bff1a4b39a348ae3ea124a0e3_Out_0_Float = _NB_DistortionAlphaMultiplier;
                    float _Property_a31f37f37774434e8b5383ecef8f5072_Out_0_Float = _NB_DistortionAlphaAdd;
                    UnityTexture2D _Property_e1e8e8a73a5f5e1c9e969abcb9782cd2_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_MaskMap, sampler_MaskMap, _MaskMap_TexelSize, _MaskMap_ST, float4(0, 0, 0, 0));
                    float _Property_26acbca87fca52f4a5ea05d6fcf09e48_Out_0_Float = _Mask_Toggle;
                    float4 _Property_c7f5afaa32df59eb9c6b2561b57aa26f_Out_0_Vector4 = _MaskMapVec;
                    float4 _Property_6e138238299250348f63a5ee2f1f3d5b_Out_0_Vector4 = _MaskRefineVec;
                    float _Property_20f932f599b85641aeb7c2ea908ed484_Out_0_Float = _NB_ColorChannelLo16;
                    UnityTexture2D _Property_b4f86e887f26523f845f932a27414b5b_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_DissolveMap, sampler_DissolveMap, _DissolveMap_TexelSize, _DissolveMap_ST, float4(0, 0, 0, 0));
                    float _Property_a70b9f8477c75d2a98fd83c15f287a37_Out_0_Float = _Dissolve_Toggle;
                    float4 _Property_a05025df3a5c534f88355f30cb99d2d3_Out_0_Vector4 = _Dissolve;
                    float4 _UV_dde6e6f3492e4fa689d7f4a88f3b492b_Out_0_Vector4 = IN.uv0;
                    float4 _Property_7a2d908f5a2f4990ae14fd870e5a22c9_Out_0_Vector4 = _BaseMap_ST;
                    float _Property_45764d3c363e48539c14ce1d9b8eb3bb_Out_0_Float = _BaseMapUVRotation;
                    float _Property_4c0c6ce883a14260b681ac41d0f78472_Out_0_Float = _BaseMapUVRotationSpeed;
                    float4 _Property_ea0189e7a35b4e44a9e5775b9d1ec406_Out_0_Vector4 = _BaseMapMaskMapOffset;
                    float4 _UV_a8924bd05fcf4a28878b71b6b3b05cd7_Out_0_Vector4 = IN.uv1;
                    float4 _UV_ce3ab892df664c348236f6bce976c116_Out_0_Vector4 = IN.uv2;
                    float _Property_3ca4059fa35141669dc7e92d919136a2_Out_0_Float = _NB_UVModeFlag0Lo16;
                    float _Property_320511e8014a49c4a7e44e21e9c57f02_Out_0_Float = _NB_UVModeFlag0Hi16;
                    float _Property_a91f4458b51f4b488287925809b8a798_Out_0_Float = _NB_UVModeFlagType0Lo16;
                    float _Property_06eb05a4a820436ab686475e2d043b4d_Out_0_Float = _NB_UVModeFlagType0Hi16;
                    float4 _Property_03cf5077fa564f27841d641d9c62f6a3_Out_0_Vector4 = _SharedUV_ST;
                    float4 _Property_338f6b6c652440289b94c105205125f7_Out_0_Vector4 = _SharedUV_Vec;
                    float4 _Property_df09eceb3f6847288ccb0e2f2b6beb4c_Out_0_Vector4 = _TWParameter;
                    float _Property_4cd2fdd2348a4b7795e93a4e7e997131_Out_0_Float = _TWStrength;
                    float4 _Property_65a8673d6744483e8aaf1ea1d5d4d4ca_Out_0_Vector4 = _PCCenter;
                    float2 _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_Out_5_Vector2;
                    float2 _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_MaskUV_22_Vector2;
                    float2 _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_Mask2UV_23_Vector2;
                    float2 _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_Mask3UV_24_Vector2;
                    float2 _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_EmissionUV_25_Vector2;
                    float2 _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_DissolveUV_26_Vector2;
                    float2 _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_DissolveMaskUV_27_Vector2;
                    float2 _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_ColorBlendUV_28_Vector2;
                    float2 _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_RampColorUV_29_Vector2;
                    float2 _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_NoiseUV_30_Vector2;
                    float2 _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_NoiseMaskUV_31_Vector2;
                    float2 _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_BumpUV_32_Vector2;
                    float2 _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_ProgramNoiseUV_33_Vector2;
                    NBGraphBaseUV_float(_UV_dde6e6f3492e4fa689d7f4a88f3b492b_Out_0_Vector4, _Property_7a2d908f5a2f4990ae14fd870e5a22c9_Out_0_Vector4, _Property_45764d3c363e48539c14ce1d9b8eb3bb_Out_0_Float, _Property_4c0c6ce883a14260b681ac41d0f78472_Out_0_Float, _Property_ea0189e7a35b4e44a9e5775b9d1ec406_Out_0_Vector4, _UV_a8924bd05fcf4a28878b71b6b3b05cd7_Out_0_Vector4, _UV_ce3ab892df664c348236f6bce976c116_Out_0_Vector4, _Property_eafd46b75a1746f093439de914a3717c_Out_0_Float, _Property_02448b2e80a0440b804602e588eb2727_Out_0_Float, _Property_52b95ce95e564cb083a7953179be1554_Out_0_Float, _Property_6abc2ac395a54d7f99af7850796b2c20_Out_0_Float, _Property_3ca4059fa35141669dc7e92d919136a2_Out_0_Float, _Property_320511e8014a49c4a7e44e21e9c57f02_Out_0_Float, _Property_a91f4458b51f4b488287925809b8a798_Out_0_Float, _Property_06eb05a4a820436ab686475e2d043b4d_Out_0_Float, _Property_03cf5077fa564f27841d641d9c62f6a3_Out_0_Vector4, _Property_338f6b6c652440289b94c105205125f7_Out_0_Vector4, _Property_df09eceb3f6847288ccb0e2f2b6beb4c_Out_0_Vector4, _Property_4cd2fdd2348a4b7795e93a4e7e997131_Out_0_Float, _Property_65a8673d6744483e8aaf1ea1d5d4d4ca_Out_0_Vector4, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_Out_5_Vector2, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_MaskUV_22_Vector2, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_Mask2UV_23_Vector2, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_Mask3UV_24_Vector2, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_EmissionUV_25_Vector2, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_DissolveUV_26_Vector2, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_DissolveMaskUV_27_Vector2, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_ColorBlendUV_28_Vector2, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_RampColorUV_29_Vector2, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_NoiseUV_30_Vector2, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_NoiseMaskUV_31_Vector2, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_BumpUV_32_Vector2, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_ProgramNoiseUV_33_Vector2);
                    float _Property_c4d2b9d87084415fb9b51e578a0b9080_Out_0_Float = _MaskMapUVRotation;
                    float _Property_43ed7c33980543efb0a5f6f413791461_Out_0_Float = _MaskMapRotationSpeed;
                    float4 _Property_63ba51c6efaf4d659d2b6b921e7f1a43_Out_0_Vector4 = _MaskMapOffsetAnition;
                    float4 _Property_f9f92343294e499b8f4512de6aeefa5c_Out_0_Vector4 = _DissolveOffsetRotateDistort;
                    UnityTexture2D _Property_a5f2eeafb2c748ed9a1ec290960ab16f_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_DissolveMaskMap, sampler_DissolveMaskMap, _DissolveMaskMap_TexelSize, _DissolveMaskMap_ST, float4(0, 0, 0, 0));
                    float _Property_56d752220cbf4d96ab46764fc9aebb16_Out_0_Float = _DissolveMask_Toggle;
                    float _Property_e1702b0dd4cf43f18d778429097ef342_Out_0_Float = _DissolveMaskMode;
                    UnityTexture2D _Property_a6ea548184d045a89325bc16be701913_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_MaskMap2, sampler_MaskMap2, _MaskMap2_TexelSize, _MaskMap2_ST, float4(0, 0, 0, 0));
                    float _Property_7c78f411c5ff4fac85800c00002c0fbc_Out_0_Float = _Mask2_Toggle;
                    UnityTexture2D _Property_f9394d12cb9a412e8858ece414106043_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_MaskMap3, sampler_MaskMap3, _MaskMap3_TexelSize, _MaskMap3_ST, float4(0, 0, 0, 0));
                    float _Property_7e16bb82a3584c73a47c9dcc897f62a3_Out_0_Float = _Mask3_Toggle;
                    float4 _Property_282301309355467f98c2a0ca1c46538f_Out_0_Vector4 = _MaskMap3OffsetAnition;
                    float _Property_901675ce420344bb8ef7c1598f2a8c6c_Out_0_Float = _NB_WrapFlagsLo16;
                    float _Property_84ab548e17414d57ab8ab8e23e9c9a5d_Out_0_Float = _NB_WrapFlagsHi16;
                    float _Property_0a9b0dca2b7846869eb548347e06dfe0_Out_0_Float = _MaskMapGradientCount;
                    float4 _Property_bceef4027b6c4cc4b1d21c40d1e6b380_Out_0_Vector4 = _MaskMapGradientFloat0;
                    float4 _Property_c8f145683c5e4bc2b543907d534d50ac_Out_0_Vector4 = _MaskMapGradientFloat1;
                    float4 _Property_86555f566d7a4e47ad576dcbd5832f93_Out_0_Vector4 = _MaskMapGradientFloat2;
                    float _Property_3e87b9d2a706453cbd9014c75528eaa0_Out_0_Float = _MaskMap2GradientCount;
                    float4 _Property_53483bdbc2c74a8b824ea143d6eaec2b_Out_0_Vector4 = _MaskMap2GradientFloat0;
                    float4 _Property_4f05348fd4524abc964972190787b609_Out_0_Vector4 = _MaskMap2GradientFloat1;
                    float4 _Property_6235b1011ee0450c80fec0958cf7b4d4_Out_0_Vector4 = _MaskMap2GradientFloat2;
                    float _Property_a6bf239ac9f240709d42314fc1efd7b1_Out_0_Float = _MaskMap3GradientCount;
                    float4 _Property_bcd09d9bc52b4a4f9cb57238c0902210_Out_0_Vector4 = _MaskMap3GradientFloat0;
                    float4 _Property_f14563d83f83427a8730d060aca57e2e_Out_0_Vector4 = _MaskMap3GradientFloat1;
                    float4 _Property_43d61a210ee74ffda3c292b0848511af_Out_0_Vector4 = _MaskMap3GradientFloat2;
                    float _Property_5cdf8c4f8e5f4271bf734cfe073fcc52_Out_0_Float = _AlphaAll;
                    float4 _Property_adbdd97beb12402b87648f77f30ab4c0_Out_0_Vector4 = _ColorA;
                    float _Property_442020c8e09d56679b49c301f56b94a4_Out_0_Float = _BaseColorIntensityForTimeline;
                    float4 _Property_b2641bfb6535514caebe85f35a335537_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_BaseBackColor) : _BaseBackColor;
                    float _IsFrontFace_d1919b09d5db5f54aaa1b20fe1d91dbf_Out_0_Boolean = max(0, IN.FaceSign.x);
                    UnityTexture2D _Property_92eefc72bf7f56fd8f8e4084e22172be_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_EmissionMap, sampler_EmissionMap, _EmissionMap_TexelSize, _EmissionMap_ST, float4(0, 0, 0, 0));
                    float _Property_318d04aaa2195ad4b539c26b2c04eeac_Out_0_Float = _EmissionEnabled;
                    float4 _Property_7d76134eb0065dcf88906e1e451e441f_Out_0_Vector4 = _EmissionMapUVOffset;
                    float _Property_3540299c49785e3c86755a5fe22db945_Out_0_Float = _EmissionMapUVRotation;
                    float4 _Property_4af1e92a383f585dbcf0fb0884888164_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_EmissionMapColor) : _EmissionMapColor;
                    float _Property_10781681264d5230974041a749fa166e_Out_0_Float = _EmissionMapColorIntensity;
                    float _Property_6b0f493dcf575d0ab56773dc813aefa6_Out_0_Float = _EmissionAlphaIntensity;
                    UnityTexture2D _Property_d61bfdcc9d345709b6c4e37db8d8dc9d_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_ColorBlendMap, sampler_ColorBlendMap, _ColorBlendMap_TexelSize, _ColorBlendMap_ST, float4(0, 0, 0, 0));
                    float _Property_30534193d46e557494c8f844a330b5b4_Out_0_Float = _ColorBlendMap_Toggle;
                    float4 _Property_36317ab840af5f6db269cbc1733a86db_Out_0_Vector4 = _ColorBlendMapOffset;
                    float4 _Property_e78fc93d059558309f51829a464ff1ed_Out_0_Vector4 = _ColorBlendVec;
                    float4 _Property_a0161d17445e53a18e11ae4102584287_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_ColorBlendColor) : _ColorBlendColor;
                    float _Property_aa8cec6d7536592c83454ce0965f8b85_Out_0_Float = _ColorBlendColorIntensity;
                    UnityTexture2D _Property_2c26a69a61fc52a5939877dca4507b70_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_RampColorMap, sampler_RampColorMap, _RampColorMap_TexelSize, _RampColorMap_ST, float4(0, 0, 0, 0));
                    float _Property_668c759ef0f85daa97ec0fa2c458fd91_Out_0_Float = _RampColorToggle;
                    float _Property_0d0fcda8bbbf5e5dadddaf843017559f_Out_0_Float = _RampColorSourceMode;
                    float4 _Property_eb2d499a8f40574f91ca2bfbb37b0bf7_Out_0_Vector4 = _RampColorMapOffset;
                    float4 _Property_65576e0f99385f1fb9a7c67087c5b89d_Out_0_Vector4 = _RampColor0;
                    float4 _Property_55e82011a7bd54c7969e4044ab34a602_Out_0_Vector4 = _RampColor1;
                    float4 _Property_f5084aa30a6751de8d599e42fb5ffff5_Out_0_Vector4 = _RampColor2;
                    float4 _Property_8725b71298fd58698edbc87798ea15e9_Out_0_Vector4 = _RampColor3;
                    float4 _Property_04d0659f70de5bb8bf5972d05a3b68c8_Out_0_Vector4 = _RampColor4;
                    float4 _Property_4989a100e056527b92055a3c516a4eab_Out_0_Vector4 = _RampColor5;
                    float4 _Property_77bf5e95cff652cdaaa6bf93adeb0f93_Out_0_Vector4 = _RampColorAlpha0;
                    float4 _Property_c5d14f129a11542581793506ad8ba24e_Out_0_Vector4 = _RampColorAlpha1;
                    float4 _Property_e84364b4aa05540ebb986ab49893b436_Out_0_Vector4 = _RampColorAlpha2;
                    float _Property_7f00ce17ce115d2bb969973b132fe3e8_Out_0_Float = _RampColorCount;
                    float4 _Property_89e65c225052579ea3ac6095a234f65a_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_RampColorBlendColor) : _RampColorBlendColor;
                    float _Property_4750fb93d1d1562bad31b25dec3f8b25_Out_0_Float = _HueShift;
                    float _Property_5e6a32f15bf05438b9cc108706ad0625_Out_0_Float = _Contrast;
                    float4 _Property_866cffb762935a229d1b940e1f8b905c_Out_0_Vector4 = _ContrastMidColor;
                    float _Property_f97ecb43f95c5e37b841c32f67fa546e_Out_0_Float = _Saturability;
                    float4 _Property_713b0ac7fdd85eb2a23076a581e94638_Out_0_Vector4 = _BaseMapColorRefine;
                    float _Property_2fd1783a3bdc52f88effab68b88e0a47_Out_0_Float = _fresnelEnabled;
                    float4 _Property_760680a31d7b57b3a03b703fedd7dea7_Out_0_Vector4 = _FresnelUnit;
                    float4 _Property_2e33a988d6ea5eaea31eb783d2cb2026_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_FresnelColor) : _FresnelColor;
                    float4 _Property_990aa20b0fbe58bab561e7a26ecc62ad_Out_0_Vector4 = _FresnelRotation;
                    float4 _Property_e5767a3f300954768906d034fee505b2_Out_0_Vector4 = _Dissolve_Vec2;
                    float4 _Property_28013c8ed2865dbba415684e772fbe2f_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_DissolveLineColor) : _DissolveLineColor;
                    float _Property_872303b3673f5db6ba39a80132854337_Out_0_Float = _Dissolve_useRampMap_Toggle;
                    UnityTexture2D _Property_f712ace2c0ec59aca6e4ea56a5dc642b_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_DissolveRampMap, sampler_DissolveRampMap, _DissolveRampMap_TexelSize, _DissolveRampMap_ST, float4(0, 0, 0, 0));
                    float _Property_3f274a4912255dc2aa6e18641c408bd6_Out_0_Float = _DissolveRampSourceMode;
                    float4 _Property_2d934283df7757faadf21ff2aa713bb4_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_DissolveRampColor) : _DissolveRampColor;
                    float _Property_d2afb0df25d55430acab801c8a3dbfde_Out_0_Float = _DissolveRampCount;
                    float4 _Property_0dec3b42d61b5e7c862c4ca295f9e208_Out_0_Vector4 = _DissolveRampColor0;
                    float4 _Property_4b091596ce61549b96d0e88967383f48_Out_0_Vector4 = _DissolveRampColor1;
                    float4 _Property_e246b4e0fa405aca8b55e192c06587a0_Out_0_Vector4 = _DissolveRampColor2;
                    float4 _Property_7a5d7978834d5d4eb9428c0a196dfbd3_Out_0_Vector4 = _DissolveRampColor3;
                    float4 _Property_6ab07b2800785722806fc80f0a180a5e_Out_0_Vector4 = _DissolveRampColor4;
                    float4 _Property_5250e3a06c0252d6a6bb9743ea2f0ac1_Out_0_Vector4 = _DissolveRampColor5;
                    float4 _Property_64999d5c415b5a73b4a0c204bcf29377_Out_0_Vector4 = _DissolveRampAlpha0;
                    float4 _Property_a934aa24a47f5da2992d3e468dbd374d_Out_0_Vector4 = _DissolveRampAlpha1;
                    float4 _Property_629063539c105cb3a2e8d3eade7eefb1_Out_0_Vector4 = _DissolveRampAlpha2;
                    float _Property_fc9eb8a77e24500ab5421051111d6fda_Out_0_Float = _NB_ForceNoMipFlagsLo16;
                    float _Property_6311ff30de835a0da66e70c41e5eb4a9_Out_0_Float = _NB_ForceNoMipFlagsHi16;
                    float _Property_9439295f5abe51489892e22adc16c2b7_Out_0_Float = _NB_DissolveRampSTOverrideEnabled;
                    float4 _Property_e0d280ecac5f56c48b444399bdfee24d_Out_0_Vector4 = _NB_DissolveRampSTOverride;
                    float _Property_d24b5ab9f5354781acf90544fdd34e1a_Out_0_Float = _DistanceFade_Toggle;
                    float4 _Property_d525ffde86414d429eb7d33f95dbc125_Out_0_Vector4 = _Fade;
                    float _Property_2f1d59a7ccfd4e21bdcabf18c881577c_Out_0_Float = _SoftParticlesEnabled;
                    float4 _Property_c04e77b9696340d1b77e0c6a9910ffae_Out_0_Vector4 = _SoftParticleFadeParams;
                    float4 _ScreenPosition_86fbeeb2af2346549e9611c0551ed415_Out_0_Vector4 = float4(IN.NDCPosition.xy, 0, 0);
                    float _Property_67f9358d1a79442eb25e5a4b980feddb_Out_0_Float = _DepthOutline_Toggle;
                    float4 _Property_75062f76b29a4f08b37655dc233735d6_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_DepthOutline_Color) : _DepthOutline_Color;
                    float4 _Property_8fbc75cf62cb493ea789465b0e094396_Out_0_Vector4 = _DepthOutline_Vec;
                    UnityTexture2D _Property_8f072da64b1f45d992849b1e47b91c14_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_BaseMap, sampler_BaseMap, _BaseMap_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
                    UnityTexture2D _Property_a9a873c1f9545377b6d5603870d8ce7e_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_NoiseMap, sampler_NoiseMap, _NoiseMap_TexelSize, _NoiseMap_ST, float4(0, 0, 0, 0));
                    float _Property_05cfd7336ce55efda9ba538458718e94_Out_0_Float = _noisemapEnabled;
                    float _Property_de00f2b38f8d559eb2e11b9a8afbf0ab_Out_0_Float = _NoiseMapUVRotation;
                    float4 _Property_c1523df23e3f5ea7a2816bc39b189b9d_Out_0_Vector4 = _NoiseOffset;
                    float _Property_6745a2aa39ce5a6db6ef4145ce16fc4d_Out_0_Float = _NoiseIntensity;
                    float4 _Property_0692aaad7ae555198da5d2971b415df9_Out_0_Vector4 = _DistortionDirection;
                    UnityTexture2D _Property_7a1c57aac1ae5322904a7fbc9050f2ce_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_NoiseMaskMap, sampler_NoiseMaskMap, _NoiseMaskMap_TexelSize, _NoiseMaskMap_ST, float4(0, 0, 0, 0));
                    float _Property_931f026875de5593b8c7d6f47800f865_Out_0_Float = _noiseMaskMap_Toggle;
                    float _Property_cff65480aa2c5e98b542088b70270554_Out_0_Float = _TexDistortion_intensity;
                    float _Property_5f7732a02ece5e6fb25d3e1ecc105818_Out_0_Float = _Emi_Distortion_intensity;
                    float _Property_b04226779a985e25a46ae050e8c15dcc_Out_0_Float = _MaskDistortion_intensity;
                    float _Property_415818c2850c582fa48e2026af7e309e_Out_0_Float = _MatCapToggle;
                    UnityTexture2D _Property_53f494020d7651608ba56c8922ff1ab9_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_MatCapTex, sampler_MatCapTex, float4(1, 1, 1, 1), float4(1, 1, 0, 0), float4(0, 0, 0, 0));
                    float4 _Property_6df429d0561957cb84dfad2ee12aeb46_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_MatCapColor) : _MatCapColor;
                    float4 _Property_5c8f0472975b5156a2b33799226739c6_Out_0_Vector4 = _MatCapInfo;
                    float _Property_d2d631afc2c1544b83cb34681aa85a03_Out_0_Float = _BumpMapToggle;
                    UnityTexture2D _Property_8a8ba308b2a1563ea8c2419c29c5c7b9_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_BumpTex, sampler_BumpTex, float4(1, 1, 1, 1), _BumpTex_ST, float4(0, 0, 0, 0));
                    float _Property_6d9934d6abfd5c0697171ddfd0c97fdc_Out_0_Float = _BumpScale;
                    float _Property_e5ae484542c256ef9e2c843d58514935_Out_0_Float = _FxLightMode;
                    float4 _Property_6301978f07ae543781068ab22b637a6c_Out_0_Vector4 = _MaterialInfo;
                    float4 _Property_9f86734760a45426a1b7d524232798ab_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_SpecularColor) : _SpecularColor;
                    float _Property_665d05cdb47f5ac3b55f9494eebcfd24_Out_0_Float = _ProgramNoise_Toggle;
                    float _Property_97cf65b2fe9f579a9efe26a73d830bb9_Out_0_Float = _ProgramNoise_Simple_Toggle;
                    float _Property_46cf15a5712c5271ab621a4945709213_Out_0_Float = _ProgramNoise_Voronoi_Toggle;
                    float _Property_4d5ce3c87f6b5b5eb6b5488a205f5da2_Out_0_Float = _ProgramNoise_Rotate;
                    float4 _Property_73408a6e8749509dbf03b29118ea169a_Out_0_Vector4 = _DissolveVoronoi_Vec;
                    float4 _Property_4c7f65f090b75d33a899f4860a5eb28c_Out_0_Vector4 = _DissolveVoronoi_Vec2;
                    float4 _Property_cc5eb5485f3f503e991dd48cda2773d8_Out_0_Vector4 = _DissolveVoronoi_Vec3;
                    float4 _Property_ea6439cfbd84510292b048fc6619980b_Out_0_Vector4 = _DissolveVoronoi_Vec4;
                    float _Property_ead7fba0b75559f78e76a0fbf67cae12_Out_0_Float = _ProgramNoiseBaseBlendOpacity;
                    float _Property_13f2b053fdb9593aa51c8639f809a891_Out_0_Float = _MaskPNoiseBlendOpacity;
                    float _Property_9767ed7b0d615801a6878b0fc416e6f0_Out_0_Float = _DissolvePNoiseBlendOpacity;
                    float _Property_3750f20ccf7b5a7fb1d9e4bd3fb2b6ec_Out_0_Float = _NB_PNoiseBlendLo16;
                    float _Property_5d371e5c8c7459f280e4945d8a055cdc_Out_0_Float = _NB_PNoiseBlendHi16;
                    UnityTexture2D _Property_c125096410c15c18bbff38461fb105d3_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_RigRTBk, sampler_RigRTBk, float4(1, 1, 1, 1), float4(1, 1, 0, 0), float4(0, 0, 0, 0));
                    UnityTexture2D _Property_38036157e2ec5c659072d3660c785123_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_RigLBtF, sampler_RigLBtF, float4(1, 1, 1, 1), float4(1, 1, 0, 0), float4(0, 0, 0, 0));
                    UnityTexture2D _Property_269c61b4d8fe5a7684fc52010ece7432_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_SixWayEmissionRamp, sampler_SixWayEmissionRamp, float4(1, 1, 1, 1), float4(1, 1, 0, 0), float4(0, 0, 0, 0));
                    float4 _Property_89139d6b0c285de9a58e1f6df2e7d236_Out_0_Vector4 = _SixWayInfo;
                    float4 _Property_547327d8b0f75f868f2ecda76c814b21_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_SixWayEmissionColor) : _SixWayEmissionColor;
                    float _Property_d2fe351137c85eac9a617d3f05ff92b2_Out_0_Float = _SixWayColorAbsorptionToggle;
                    float _Property_25cf481f9d865f129398a908a10a4f11_Out_0_Float = _DistortPNoiseBlendOpacity;
                    float4 _NBGraphBaseColorCustomFunction_f712bc264a1c40848edc2dd476a4cada_Out_3_Vector4;
                    float2 _NBGraphBaseColorCustomFunction_f712bc264a1c40848edc2dd476a4cada_NBDistortionSignedRG_145_Vector2;
                    float _NBGraphBaseColorCustomFunction_f712bc264a1c40848edc2dd476a4cada_NBDistortionNoiseMask_146_Float;
                    NBGraphBaseColor_float(_NBGraphBaseColorCustomFunction_f712bc264a1c40848edc2dd476a4cada_SampledAlbedo_0_Vector4, _NBGraphBaseColorCustomFunction_f712bc264a1c40848edc2dd476a4cada_SelectedAlpha_1_Float, _Property_13840ad57da543e2b33275638ce1189e_Out_0_Vector4, _Property_eafd46b75a1746f093439de914a3717c_Out_0_Float, _Property_02448b2e80a0440b804602e588eb2727_Out_0_Float, _Property_52b95ce95e564cb083a7953179be1554_Out_0_Float, _Property_6abc2ac395a54d7f99af7850796b2c20_Out_0_Float, _Property_4fbc64ac55184ee8b2348b861c93db8b_Out_0_Vector2, _Property_9099f47df6d24486a9e0bb9dd8fa7cf8_Out_0_Float, _Property_01827f8348b040028326054a1623cc84_Out_0_Float, _Property_87436152ca164e73877206654813b186_Out_0_Float, _Property_9aab3b8bff1a4b39a348ae3ea124a0e3_Out_0_Float, _Property_a31f37f37774434e8b5383ecef8f5072_Out_0_Float, _Property_e1e8e8a73a5f5e1c9e969abcb9782cd2_Out_0_Texture2D, _Property_26acbca87fca52f4a5ea05d6fcf09e48_Out_0_Float, _Property_c7f5afaa32df59eb9c6b2561b57aa26f_Out_0_Vector4, _Property_6e138238299250348f63a5ee2f1f3d5b_Out_0_Vector4, _Property_20f932f599b85641aeb7c2ea908ed484_Out_0_Float, _Property_b4f86e887f26523f845f932a27414b5b_Out_0_Texture2D, _Property_a70b9f8477c75d2a98fd83c15f287a37_Out_0_Float, _Property_a05025df3a5c534f88355f30cb99d2d3_Out_0_Vector4, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_MaskUV_22_Vector2, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_DissolveUV_26_Vector2, _Property_c4d2b9d87084415fb9b51e578a0b9080_Out_0_Float, _Property_43ed7c33980543efb0a5f6f413791461_Out_0_Float, _Property_63ba51c6efaf4d659d2b6b921e7f1a43_Out_0_Vector4, _Property_f9f92343294e499b8f4512de6aeefa5c_Out_0_Vector4, _Property_a5f2eeafb2c748ed9a1ec290960ab16f_Out_0_Texture2D, _Property_56d752220cbf4d96ab46764fc9aebb16_Out_0_Float, _Property_e1702b0dd4cf43f18d778429097ef342_Out_0_Float, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_DissolveMaskUV_27_Vector2, _Property_a6ea548184d045a89325bc16be701913_Out_0_Texture2D, _Property_7c78f411c5ff4fac85800c00002c0fbc_Out_0_Float, _Property_f9394d12cb9a412e8858ece414106043_Out_0_Texture2D, _Property_7e16bb82a3584c73a47c9dcc897f62a3_Out_0_Float, _Property_282301309355467f98c2a0ca1c46538f_Out_0_Vector4, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_Mask2UV_23_Vector2, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_Mask3UV_24_Vector2, _Property_901675ce420344bb8ef7c1598f2a8c6c_Out_0_Float, _Property_84ab548e17414d57ab8ab8e23e9c9a5d_Out_0_Float, _Property_0a9b0dca2b7846869eb548347e06dfe0_Out_0_Float, _Property_bceef4027b6c4cc4b1d21c40d1e6b380_Out_0_Vector4, _Property_c8f145683c5e4bc2b543907d534d50ac_Out_0_Vector4, _Property_86555f566d7a4e47ad576dcbd5832f93_Out_0_Vector4, _Property_3e87b9d2a706453cbd9014c75528eaa0_Out_0_Float, _Property_53483bdbc2c74a8b824ea143d6eaec2b_Out_0_Vector4, _Property_4f05348fd4524abc964972190787b609_Out_0_Vector4, _Property_6235b1011ee0450c80fec0958cf7b4d4_Out_0_Vector4, _Property_a6bf239ac9f240709d42314fc1efd7b1_Out_0_Float, _Property_bcd09d9bc52b4a4f9cb57238c0902210_Out_0_Vector4, _Property_f14563d83f83427a8730d060aca57e2e_Out_0_Vector4, _Property_43d61a210ee74ffda3c292b0848511af_Out_0_Vector4, _Property_5cdf8c4f8e5f4271bf734cfe073fcc52_Out_0_Float, _Property_adbdd97beb12402b87648f77f30ab4c0_Out_0_Vector4, IN.VertexColor, _Property_442020c8e09d56679b49c301f56b94a4_Out_0_Float, _Property_b2641bfb6535514caebe85f35a335537_Out_0_Vector4, ((float) _IsFrontFace_d1919b09d5db5f54aaa1b20fe1d91dbf_Out_0_Boolean), _Property_92eefc72bf7f56fd8f8e4084e22172be_Out_0_Texture2D, _Property_318d04aaa2195ad4b539c26b2c04eeac_Out_0_Float, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_EmissionUV_25_Vector2, _Property_7d76134eb0065dcf88906e1e451e441f_Out_0_Vector4, _Property_3540299c49785e3c86755a5fe22db945_Out_0_Float, _Property_4af1e92a383f585dbcf0fb0884888164_Out_0_Vector4, _Property_10781681264d5230974041a749fa166e_Out_0_Float, _Property_6b0f493dcf575d0ab56773dc813aefa6_Out_0_Float, _Property_d61bfdcc9d345709b6c4e37db8d8dc9d_Out_0_Texture2D, _Property_30534193d46e557494c8f844a330b5b4_Out_0_Float, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_ColorBlendUV_28_Vector2, _Property_36317ab840af5f6db269cbc1733a86db_Out_0_Vector4, _Property_e78fc93d059558309f51829a464ff1ed_Out_0_Vector4, _Property_a0161d17445e53a18e11ae4102584287_Out_0_Vector4, _Property_aa8cec6d7536592c83454ce0965f8b85_Out_0_Float, _Property_2c26a69a61fc52a5939877dca4507b70_Out_0_Texture2D, _Property_668c759ef0f85daa97ec0fa2c458fd91_Out_0_Float, _Property_0d0fcda8bbbf5e5dadddaf843017559f_Out_0_Float, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_RampColorUV_29_Vector2, _Property_eb2d499a8f40574f91ca2bfbb37b0bf7_Out_0_Vector4, _Property_65576e0f99385f1fb9a7c67087c5b89d_Out_0_Vector4, _Property_55e82011a7bd54c7969e4044ab34a602_Out_0_Vector4, _Property_f5084aa30a6751de8d599e42fb5ffff5_Out_0_Vector4, _Property_8725b71298fd58698edbc87798ea15e9_Out_0_Vector4, _Property_04d0659f70de5bb8bf5972d05a3b68c8_Out_0_Vector4, _Property_4989a100e056527b92055a3c516a4eab_Out_0_Vector4, _Property_77bf5e95cff652cdaaa6bf93adeb0f93_Out_0_Vector4, _Property_c5d14f129a11542581793506ad8ba24e_Out_0_Vector4, _Property_e84364b4aa05540ebb986ab49893b436_Out_0_Vector4, _Property_7f00ce17ce115d2bb969973b132fe3e8_Out_0_Float, _Property_89e65c225052579ea3ac6095a234f65a_Out_0_Vector4, _Property_4750fb93d1d1562bad31b25dec3f8b25_Out_0_Float, _Property_5e6a32f15bf05438b9cc108706ad0625_Out_0_Float, _Property_866cffb762935a229d1b940e1f8b905c_Out_0_Vector4, _Property_f97ecb43f95c5e37b841c32f67fa546e_Out_0_Float, _Property_713b0ac7fdd85eb2a23076a581e94638_Out_0_Vector4, _Property_2fd1783a3bdc52f88effab68b88e0a47_Out_0_Float, _Property_760680a31d7b57b3a03b703fedd7dea7_Out_0_Vector4, _Property_2e33a988d6ea5eaea31eb783d2cb2026_Out_0_Vector4, _Property_990aa20b0fbe58bab561e7a26ecc62ad_Out_0_Vector4, IN.WorldSpaceNormal, IN.WorldSpaceViewDirection, _Property_e5767a3f300954768906d034fee505b2_Out_0_Vector4, _Property_28013c8ed2865dbba415684e772fbe2f_Out_0_Vector4, _Property_872303b3673f5db6ba39a80132854337_Out_0_Float, _Property_f712ace2c0ec59aca6e4ea56a5dc642b_Out_0_Texture2D, _Property_3f274a4912255dc2aa6e18641c408bd6_Out_0_Float, _Property_2d934283df7757faadf21ff2aa713bb4_Out_0_Vector4, _Property_d2afb0df25d55430acab801c8a3dbfde_Out_0_Float, _Property_0dec3b42d61b5e7c862c4ca295f9e208_Out_0_Vector4, _Property_4b091596ce61549b96d0e88967383f48_Out_0_Vector4, _Property_e246b4e0fa405aca8b55e192c06587a0_Out_0_Vector4, _Property_7a5d7978834d5d4eb9428c0a196dfbd3_Out_0_Vector4, _Property_6ab07b2800785722806fc80f0a180a5e_Out_0_Vector4, _Property_5250e3a06c0252d6a6bb9743ea2f0ac1_Out_0_Vector4, _Property_64999d5c415b5a73b4a0c204bcf29377_Out_0_Vector4, _Property_a934aa24a47f5da2992d3e468dbd374d_Out_0_Vector4, _Property_629063539c105cb3a2e8d3eade7eefb1_Out_0_Vector4, _Property_fc9eb8a77e24500ab5421051111d6fda_Out_0_Float, _Property_6311ff30de835a0da66e70c41e5eb4a9_Out_0_Float, _Property_9439295f5abe51489892e22adc16c2b7_Out_0_Float, _Property_e0d280ecac5f56c48b444399bdfee24d_Out_0_Vector4, _Property_d24b5ab9f5354781acf90544fdd34e1a_Out_0_Float, _Property_d525ffde86414d429eb7d33f95dbc125_Out_0_Vector4, IN.ViewSpacePosition, _Property_2f1d59a7ccfd4e21bdcabf18c881577c_Out_0_Float, _Property_c04e77b9696340d1b77e0c6a9910ffae_Out_0_Vector4, _ScreenPosition_86fbeeb2af2346549e9611c0551ed415_Out_0_Vector4, _Property_67f9358d1a79442eb25e5a4b980feddb_Out_0_Float, _Property_75062f76b29a4f08b37655dc233735d6_Out_0_Vector4, _Property_8fbc75cf62cb493ea789465b0e094396_Out_0_Vector4, _Property_8f072da64b1f45d992849b1e47b91c14_Out_0_Texture2D, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_Out_5_Vector2, _Property_a9a873c1f9545377b6d5603870d8ce7e_Out_0_Texture2D, _Property_05cfd7336ce55efda9ba538458718e94_Out_0_Float, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_NoiseUV_30_Vector2, _Property_de00f2b38f8d559eb2e11b9a8afbf0ab_Out_0_Float, _Property_c1523df23e3f5ea7a2816bc39b189b9d_Out_0_Vector4, _Property_6745a2aa39ce5a6db6ef4145ce16fc4d_Out_0_Float, _Property_0692aaad7ae555198da5d2971b415df9_Out_0_Vector4, _Property_7a1c57aac1ae5322904a7fbc9050f2ce_Out_0_Texture2D, _Property_931f026875de5593b8c7d6f47800f865_Out_0_Float, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_NoiseMaskUV_31_Vector2, _Property_cff65480aa2c5e98b542088b70270554_Out_0_Float, _Property_5f7732a02ece5e6fb25d3e1ecc105818_Out_0_Float, _Property_b04226779a985e25a46ae050e8c15dcc_Out_0_Float, _Property_415818c2850c582fa48e2026af7e309e_Out_0_Float, _Property_53f494020d7651608ba56c8922ff1ab9_Out_0_Texture2D, _Property_6df429d0561957cb84dfad2ee12aeb46_Out_0_Vector4, _Property_5c8f0472975b5156a2b33799226739c6_Out_0_Vector4, _Property_d2d631afc2c1544b83cb34681aa85a03_Out_0_Float, _Property_8a8ba308b2a1563ea8c2419c29c5c7b9_Out_0_Texture2D, _Property_6d9934d6abfd5c0697171ddfd0c97fdc_Out_0_Float, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_BumpUV_32_Vector2, IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, _Property_e5ae484542c256ef9e2c843d58514935_Out_0_Float, _Property_6301978f07ae543781068ab22b637a6c_Out_0_Vector4, _Property_9f86734760a45426a1b7d524232798ab_Out_0_Vector4, IN.WorldSpacePosition, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_ProgramNoiseUV_33_Vector2, _Property_665d05cdb47f5ac3b55f9494eebcfd24_Out_0_Float, _Property_97cf65b2fe9f579a9efe26a73d830bb9_Out_0_Float, _Property_46cf15a5712c5271ab621a4945709213_Out_0_Float, _Property_4d5ce3c87f6b5b5eb6b5488a205f5da2_Out_0_Float, _Property_73408a6e8749509dbf03b29118ea169a_Out_0_Vector4, _Property_4c7f65f090b75d33a899f4860a5eb28c_Out_0_Vector4, _Property_cc5eb5485f3f503e991dd48cda2773d8_Out_0_Vector4, _Property_ea6439cfbd84510292b048fc6619980b_Out_0_Vector4, _Property_ead7fba0b75559f78e76a0fbf67cae12_Out_0_Float, _Property_13f2b053fdb9593aa51c8639f809a891_Out_0_Float, _Property_9767ed7b0d615801a6878b0fc416e6f0_Out_0_Float, _Property_3750f20ccf7b5a7fb1d9e4bd3fb2b6ec_Out_0_Float, _Property_5d371e5c8c7459f280e4945d8a055cdc_Out_0_Float, _Property_c125096410c15c18bbff38461fb105d3_Out_0_Texture2D, _Property_38036157e2ec5c659072d3660c785123_Out_0_Texture2D, _Property_269c61b4d8fe5a7684fc52010ece7432_Out_0_Texture2D, _Property_89139d6b0c285de9a58e1f6df2e7d236_Out_0_Vector4, _Property_547327d8b0f75f868f2ecda76c814b21_Out_0_Vector4, _Property_d2fe351137c85eac9a617d3f05ff92b2_Out_0_Float, IN.SixBake0, IN.SixBake1, IN.SixBake2, IN.SixBack0, IN.SixBack1, IN.SixBack2, IN.SixTangentSigned, _Property_25cf481f9d865f129398a908a10a4f11_Out_0_Float, _NBGraphBaseColorCustomFunction_f712bc264a1c40848edc2dd476a4cada_Out_3_Vector4, _NBGraphBaseColorCustomFunction_f712bc264a1c40848edc2dd476a4cada_NBDistortionSignedRG_145_Vector2, _NBGraphBaseColorCustomFunction_f712bc264a1c40848edc2dd476a4cada_NBDistortionNoiseMask_146_Float);
                    float _Swizzle_52d10f3207ae43dc9038120fea622ca3_Out_1_Float = _NBGraphBaseColorCustomFunction_f712bc264a1c40848edc2dd476a4cada_Out_3_Vector4.w;
                    float _Property_b2c9094aad4345b78e06cfbf0854a2bd_Out_0_Float = _Cutoff;
                    surface.BaseColor = (_NBGraphBaseColorCustomFunction_f712bc264a1c40848edc2dd476a4cada_Out_3_Vector4.xyz);
                    surface.Alpha = _Swizzle_52d10f3207ae43dc9038120fea622ca3_Out_1_Float;
                    surface.AlphaClipThreshold = _Property_b2c9094aad4345b78e06cfbf0854a2bd_Out_0_Float;
                    surface.NBDistortionSignedRG = _NBGraphBaseColorCustomFunction_f712bc264a1c40848edc2dd476a4cada_NBDistortionSignedRG_145_Vector2;
                    surface.NBDistortionNoiseMask = _NBGraphBaseColorCustomFunction_f712bc264a1c40848edc2dd476a4cada_NBDistortionNoiseMask_146_Float;
                    return surface;
                }
            
            // --------------------------------------------------
            // Build Graph Inputs
            #ifdef HAVE_VFX_MODIFICATION
            #define VFX_SRP_ATTRIBUTES Attributes
            #define VFX_SRP_VARYINGS Varyings
            #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
            #endif
            VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
                {
                    VertexDescriptionInputs output;
                    ZERO_INITIALIZE(VertexDescriptionInputs, output);
                
                    output.ObjectSpaceNormal =                          input.normalOS;
                    output.WorldSpaceNormal =                           TransformObjectToWorldNormal(input.normalOS);
                    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
                    output.WorldSpaceTangent =                          TransformObjectToWorldDir(input.tangentOS.xyz);
                    output.ObjectSpaceBiTangent =                       normalize(cross(input.normalOS, input.tangentOS.xyz) * (input.tangentOS.w > 0.0f ? 1.0f : -1.0f) * GetOddNegativeScale());
                    output.WorldSpaceBiTangent =                        TransformObjectToWorldDir(output.ObjectSpaceBiTangent);
                    output.ObjectSpacePosition =                        input.positionOS;
                    output.uv0 =                                        input.uv0;
                    output.uv1 =                                        input.uv1;
                    output.uv2 =                                        input.uv2;
                    output.VertexColor =                                input.color;
                #if UNITY_ANY_INSTANCING_ENABLED
                #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
                #endif
                
                    return output;
                }
                SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
                {
                    SurfaceDescriptionInputs output;
                    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
                
                #ifdef HAVE_VFX_MODIFICATION
                #if VFX_USE_GRAPH_VALUES
                    uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
                    /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
                #endif
                    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
                
                #endif
                
                    output.SixBake0 = input.SixBake0;
                output.SixBake1 = input.SixBake1;
                output.SixBake2 = input.SixBake2;
                output.SixBack0 = input.SixBack0;
                output.SixBack1 = input.SixBack1;
                output.SixBack2 = input.SixBack2;
                output.SixTangentSigned = input.SixTangentSigned;
                
                    // must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
                    float3 unnormalizedNormalWS = input.normalWS;
                    const float renormFactor = 1.0 / length(unnormalizedNormalWS);
                
                    // use bitangent on the fly like in hdrp
                    // IMPORTANT! If we ever support Flip on double sided materials ensure bitangent and tangent are NOT flipped.
                    float crossSign = (input.tangentWS.w > 0.0 ? 1.0 : -1.0)* GetOddNegativeScale();
                    float3 bitang = crossSign * cross(input.normalWS.xyz, input.tangentWS.xyz);
                
                    output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;      // we want a unit length Normal Vector node in shader graph
                
                    // to pr               eserve mikktspace compliance we use same scale renormFactor as was used on the normal.
                    // This                is explained in section 2.2 in "surface gradient based bump mapping framework"
                    output.WorldSpaceTangent = renormFactor * input.tangentWS.xyz;
                    output.WorldSpaceBiTangent = renormFactor * bitang;
                
                    output.WorldSpaceViewDirection = GetWorldSpaceNormalizeViewDir(input.positionWS);
                    output.WorldSpacePosition = input.positionWS;
                    output.ViewSpacePosition = TransformWorldToView(input.positionWS);
                
                    #if UNITY_UV_STARTS_AT_TOP
                    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
                    #else
                    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
                    #endif
                
                    output.NDCPosition = output.PixelPosition.xy / _ScaledScreenParams.xy;
                    output.NDCPosition.y = 1.0f - output.NDCPosition.y;
                
                    output.uv0 = input.texCoord0;
                    output.uv1 = input.texCoord1;
                    output.uv2 = input.texCoord2;
                    output.VertexColor = input.color;
                #if UNITY_ANY_INSTANCING_ENABLED
                #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
                #endif
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
                #else
                #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                #endif
                    BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                
                        return output;
                }
                
            // --------------------------------------------------
            // Main
            
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
            #include "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/Passes/NBGraphForwardPass.hlsl"
            
            // --------------------------------------------------
            // Visual Effect Vertex Invocations
            #ifdef HAVE_VFX_MODIFICATION
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
            #endif
            
            ENDHLSL
            }
            Pass
            {
                Name "DepthOnly"
                Tags
                {
                    "LightMode" = "DepthOnly"
                }
            
            // Render State
            Cull [_Cull]
                ZTest LEqual
                ZWrite On
                ColorMask R
                Stencil
                {
                ReadMask [_StencilReadMask]
                WriteMask [_StencilWriteMask]
                Ref [_Stencil]
                CompFront [_StencilComp]
                ZFailFront [_StencilZFail]
                FailFront [_StencilFail]
                PassFront [_StencilOp]
                CompBack [_StencilComp]
                ZFailBack [_StencilZFail]
                FailBack [_StencilFail]
                PassBack [_StencilOp]
                }
            
            // Debug
            // <None>
            
            // --------------------------------------------------
            // Pass
            
            HLSLPROGRAM
            
            // Pragmas
            #pragma target 2.0
                #pragma multi_compile_instancing
                #pragma vertex vert
                #pragma fragment frag
            
            // Keywords
            #pragma shader_feature_local_fragment _ _ALPHATEST_ON
            // GraphKeywords: <None>
            
            // Defines
            
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define ATTRIBUTES_NEED_TEXCOORD1
            #define ATTRIBUTES_NEED_TEXCOORD2
            #define ATTRIBUTES_NEED_COLOR
            #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
            #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
            #define VARYINGS_NEED_POSITION_WS
            #define VARYINGS_NEED_NORMAL_WS
            #define VARYINGS_NEED_TANGENT_WS
            #define VARYINGS_NEED_TEXCOORD0
            #define VARYINGS_NEED_TEXCOORD1
            #define VARYINGS_NEED_TEXCOORD2
            #define VARYINGS_NEED_COLOR
            #define VARYINGS_NEED_CULLFACE
            #define FEATURES_GRAPH_VERTEX
            /* WARNING: $splice Could not find named fragment 'PassInstancing' */
            #define SHADERPASS SHADERPASS_DEPTHONLY
                #define SHADERGRAPH_PREVIEW_MAIN
            
            
            // custom interpolator pre-include
            /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
            
            // Includes
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
            
            // --------------------------------------------------
            // Structs and Packing
            
            // custom interpolators pre packing
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
            
            struct Attributes
                {
                     float3 positionOS : POSITION;
                     float3 normalOS : NORMAL;
                     float4 tangentOS : TANGENT;
                     float4 uv0 : TEXCOORD0;
                     float4 uv1 : TEXCOORD1;
                     float4 uv2 : TEXCOORD2;
                     float4 color : COLOR;
                    #if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
                     uint instanceID : INSTANCEID_SEMANTIC;
                    #endif
                };
                struct Varyings
                {
                     float4 positionCS : SV_POSITION;
                     float3 positionWS;
                     float3 normalWS;
                     float4 tangentWS;
                     float4 texCoord0;
                     float4 texCoord1;
                     float4 texCoord2;
                     float4 color;
                    #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
                     uint instanceID : CUSTOM_INSTANCE_ID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                    #endif
                     float3 SixBake0;
                     float3 SixBake1;
                     float3 SixBake2;
                     float3 SixBack0;
                     float3 SixBack1;
                     float3 SixBack2;
                     float4 SixTangentSigned;
                };
                struct SurfaceDescriptionInputs
                {
                     float3 WorldSpaceNormal;
                     float3 WorldSpaceTangent;
                     float3 WorldSpaceBiTangent;
                     float3 WorldSpaceViewDirection;
                     float3 ViewSpacePosition;
                     float3 WorldSpacePosition;
                     float2 NDCPosition;
                     float2 PixelPosition;
                     float4 uv0;
                     float4 uv1;
                     float4 uv2;
                     float4 VertexColor;
                     float FaceSign;
                     float3 SixBake0;
                     float3 SixBake1;
                     float3 SixBake2;
                     float3 SixBack0;
                     float3 SixBack1;
                     float3 SixBack2;
                     float4 SixTangentSigned;
                };
                struct VertexDescriptionInputs
                {
                     float3 ObjectSpaceNormal;
                     float3 WorldSpaceNormal;
                     float3 ObjectSpaceTangent;
                     float3 WorldSpaceTangent;
                     float3 ObjectSpaceBiTangent;
                     float3 WorldSpaceBiTangent;
                     float3 ObjectSpacePosition;
                     float4 uv0;
                     float4 uv1;
                     float4 uv2;
                     float4 VertexColor;
                };
                struct PackedVaryings
                {
                     float4 positionCS : SV_POSITION;
                     float4 tangentWS : INTERP0;
                     float4 texCoord0 : INTERP1;
                     float4 texCoord1 : INTERP2;
                     float4 texCoord2 : INTERP3;
                     float4 color : INTERP4;
                     float4 SixTangentSigned : INTERP5;
                     float4 packed_positionWS_SixBack1x : INTERP6;
                     float4 packed_normalWS_SixBack1y : INTERP7;
                     float4 packed_SixBake0_SixBack1z : INTERP8;
                     float4 packed_SixBake1_SixBack2x : INTERP9;
                     float4 packed_SixBake2_SixBack2y : INTERP10;
                     float4 packed_SixBack0_SixBack2z : INTERP11;
                    #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
                     uint instanceID : CUSTOM_INSTANCE_ID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                    #endif
                };
            
            PackedVaryings PackVaryings (Varyings input)
                {
                    PackedVaryings output;
                    ZERO_INITIALIZE(PackedVaryings, output);
                    output.positionCS = input.positionCS;
                    output.tangentWS.xyzw = input.tangentWS;
                    output.texCoord0.xyzw = input.texCoord0;
                    output.texCoord1.xyzw = input.texCoord1;
                    output.texCoord2.xyzw = input.texCoord2;
                    output.color.xyzw = input.color;
                    output.SixTangentSigned.xyzw = input.SixTangentSigned;
                    output.packed_positionWS_SixBack1x.xyz = input.positionWS;
                    output.packed_positionWS_SixBack1x.w = input.SixBack1.x;
                    output.packed_normalWS_SixBack1y.xyz = input.normalWS;
                    output.packed_normalWS_SixBack1y.w = input.SixBack1.y;
                    output.packed_SixBake0_SixBack1z.xyz = input.SixBake0;
                    output.packed_SixBake0_SixBack1z.w = input.SixBack1.z;
                    output.packed_SixBake1_SixBack2x.xyz = input.SixBake1;
                    output.packed_SixBake1_SixBack2x.w = input.SixBack2.x;
                    output.packed_SixBake2_SixBack2y.xyz = input.SixBake2;
                    output.packed_SixBake2_SixBack2y.w = input.SixBack2.y;
                    output.packed_SixBack0_SixBack2z.xyz = input.SixBack0;
                    output.packed_SixBack0_SixBack2z.w = input.SixBack2.z;
                    #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
                    output.instanceID = input.instanceID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
                    #endif
                    return output;
                }
                
                Varyings UnpackVaryings (PackedVaryings input)
                {
                    Varyings output;
                    output.positionCS = input.positionCS;
                    output.tangentWS = input.tangentWS.xyzw;
                    output.texCoord0 = input.texCoord0.xyzw;
                    output.texCoord1 = input.texCoord1.xyzw;
                    output.texCoord2 = input.texCoord2.xyzw;
                    output.color = input.color.xyzw;
                    output.SixTangentSigned = input.SixTangentSigned.xyzw;
                    output.positionWS = input.packed_positionWS_SixBack1x.xyz;
                    output.SixBack1.x = input.packed_positionWS_SixBack1x.w;
                    output.normalWS = input.packed_normalWS_SixBack1y.xyz;
                    output.SixBack1.y = input.packed_normalWS_SixBack1y.w;
                    output.SixBake0 = input.packed_SixBake0_SixBack1z.xyz;
                    output.SixBack1.z = input.packed_SixBake0_SixBack1z.w;
                    output.SixBake1 = input.packed_SixBake1_SixBack2x.xyz;
                    output.SixBack2.x = input.packed_SixBake1_SixBack2x.w;
                    output.SixBake2 = input.packed_SixBake2_SixBack2y.xyz;
                    output.SixBack2.y = input.packed_SixBake2_SixBack2y.w;
                    output.SixBack0 = input.packed_SixBack0_SixBack2z.xyz;
                    output.SixBack2.z = input.packed_SixBack0_SixBack2z.w;
                    #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
                    output.instanceID = input.instanceID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
                    #endif
                    return output;
                }
                
            
            // --------------------------------------------------
            // Graph
            
            // Graph Properties
            CBUFFER_START(UnityPerMaterial)
                float4 _NBGraphBaseColorCustomFunction_f712bc264a1c40848edc2dd476a4cada_SampledAlbedo_0_Vector4;
                float _NBGraphBaseColorCustomFunction_f712bc264a1c40848edc2dd476a4cada_SelectedAlpha_1_Float;
                TEXTURE2D(_BaseMap);
                SAMPLER(sampler_BaseMap);
                float4 _BaseMap_TexelSize;
                float4 _Color;
                float2 _NB_DistortionNoise;
                float _NB_DistortionIntensity;
                float _NB_DistortionMode;
                float _NB_Flags0Lo16;
                float _NB_Flags0Hi16;
                float _NB_Flags1Lo16;
                float _NB_Flags1Hi16;
                float _NB_DistortionAlphaPow;
                float _NB_DistortionAlphaMultiplier;
                float _NB_DistortionAlphaAdd;
                TEXTURE2D(_MaskMap);
                SAMPLER(sampler_MaskMap);
                float4 _MaskMap_TexelSize;
                float4 _MaskMap_ST;
                float _Mask_Toggle;
                float4 _MaskMapVec;
                float4 _MaskRefineVec;
                float _NB_ColorChannelLo16;
                TEXTURE2D(_DissolveMap);
                SAMPLER(sampler_DissolveMap);
                float4 _DissolveMap_TexelSize;
                float4 _DissolveMap_ST;
                float _Dissolve_Toggle;
                float4 _Dissolve;
                float4 _BaseMap_ST;
                float _BaseMapUVRotation;
                float _BaseMapUVRotationSpeed;
                float4 _BaseMapMaskMapOffset;
                float _MaskMapUVRotation;
                float _MaskMapRotationSpeed;
                float4 _MaskMapOffsetAnition;
                float4 _DissolveOffsetRotateDistort;
                TEXTURE2D(_DissolveMaskMap);
                SAMPLER(sampler_DissolveMaskMap);
                float4 _DissolveMaskMap_TexelSize;
                float4 _DissolveMaskMap_ST;
                float _DissolveMask_Toggle;
                float _DissolveMaskMode;
                TEXTURE2D(_MaskMap2);
                SAMPLER(sampler_MaskMap2);
                float4 _MaskMap2_TexelSize;
                float4 _MaskMap2_ST;
                float _Mask2_Toggle;
                TEXTURE2D(_MaskMap3);
                SAMPLER(sampler_MaskMap3);
                float4 _MaskMap3_TexelSize;
                float4 _MaskMap3_ST;
                float _Mask3_Toggle;
                float4 _MaskMap3OffsetAnition;
                float _NB_WrapFlagsLo16;
                float _NB_WrapFlagsHi16;
                float _MaskMapGradientCount;
                float4 _MaskMapGradientFloat0;
                float4 _MaskMapGradientFloat1;
                float4 _MaskMapGradientFloat2;
                float _MaskMap2GradientCount;
                float4 _MaskMap2GradientFloat0;
                float4 _MaskMap2GradientFloat1;
                float4 _MaskMap2GradientFloat2;
                float _MaskMap3GradientCount;
                float4 _MaskMap3GradientFloat0;
                float4 _MaskMap3GradientFloat1;
                float4 _MaskMap3GradientFloat2;
                float _AlphaAll;
                float4 _ColorA;
                float _BaseColorIntensityForTimeline;
                float4 _BaseBackColor;
                TEXTURE2D(_EmissionMap);
                SAMPLER(sampler_EmissionMap);
                float4 _EmissionMap_TexelSize;
                float4 _EmissionMap_ST;
                float _EmissionEnabled;
                float4 _EmissionMapUVOffset;
                float _EmissionMapUVRotation;
                float4 _EmissionMapColor;
                float _EmissionMapColorIntensity;
                float _EmissionAlphaIntensity;
                TEXTURE2D(_ColorBlendMap);
                SAMPLER(sampler_ColorBlendMap);
                float4 _ColorBlendMap_TexelSize;
                float4 _ColorBlendMap_ST;
                float _ColorBlendMap_Toggle;
                float4 _ColorBlendMapOffset;
                float4 _ColorBlendVec;
                float4 _ColorBlendColor;
                float _ColorBlendColorIntensity;
                TEXTURE2D(_RampColorMap);
                SAMPLER(sampler_RampColorMap);
                float4 _RampColorMap_TexelSize;
                float4 _RampColorMap_ST;
                float _RampColorToggle;
                float _RampColorSourceMode;
                float4 _RampColorMapOffset;
                float4 _RampColor0;
                float4 _RampColor1;
                float4 _RampColor2;
                float4 _RampColor3;
                float4 _RampColor4;
                float4 _RampColor5;
                float4 _RampColorAlpha0;
                float4 _RampColorAlpha1;
                float4 _RampColorAlpha2;
                float _RampColorCount;
                float4 _RampColorBlendColor;
                float _HueShift;
                float _Contrast;
                float4 _ContrastMidColor;
                float _Saturability;
                float4 _BaseMapColorRefine;
                float _fresnelEnabled;
                float4 _FresnelUnit;
                float4 _FresnelColor;
                float4 _FresnelRotation;
                float4 _Dissolve_Vec2;
                float4 _DissolveLineColor;
                float _Dissolve_useRampMap_Toggle;
                TEXTURE2D(_DissolveRampMap);
                SAMPLER(sampler_DissolveRampMap);
                float4 _DissolveRampMap_TexelSize;
                float4 _DissolveRampMap_ST;
                float _DissolveRampSourceMode;
                float4 _DissolveRampColor;
                float _DissolveRampCount;
                float4 _DissolveRampColor0;
                float4 _DissolveRampColor1;
                float4 _DissolveRampColor2;
                float4 _DissolveRampColor3;
                float4 _DissolveRampColor4;
                float4 _DissolveRampColor5;
                float4 _DissolveRampAlpha0;
                float4 _DissolveRampAlpha1;
                float4 _DissolveRampAlpha2;
                float _NB_ForceNoMipFlagsLo16;
                float _NB_ForceNoMipFlagsHi16;
                float _NB_DissolveRampSTOverrideEnabled;
                float4 _NB_DissolveRampSTOverride;
                float _DistanceFade_Toggle;
                float4 _Fade;
                float _SoftParticlesEnabled;
                float4 _SoftParticleFadeParams;
                float _DepthOutline_Toggle;
                float4 _DepthOutline_Color;
                float4 _DepthOutline_Vec;
                float _NB_UVModeFlag0Lo16;
                float _NB_UVModeFlag0Hi16;
                float _NB_UVModeFlagType0Lo16;
                float _NB_UVModeFlagType0Hi16;
                float4 _SharedUV_ST;
                float4 _SharedUV_Vec;
                float4 _TWParameter;
                float _TWStrength;
                float4 _PCCenter;
                TEXTURE2D(_NoiseMap);
                SAMPLER(sampler_NoiseMap);
                float4 _NoiseMap_TexelSize;
                float4 _NoiseMap_ST;
                float _noisemapEnabled;
                float _NoiseMapUVRotation;
                float4 _NoiseOffset;
                float _NoiseIntensity;
                float4 _DistortionDirection;
                TEXTURE2D(_NoiseMaskMap);
                SAMPLER(sampler_NoiseMaskMap);
                float4 _NoiseMaskMap_TexelSize;
                float4 _NoiseMaskMap_ST;
                float _noiseMaskMap_Toggle;
                float _TexDistortion_intensity;
                float _Emi_Distortion_intensity;
                float _MaskDistortion_intensity;
                float _Cutoff;
                float _AdditiveToPreMultiplyAlphaLerp;
                TEXTURE2D(_VertexOffset_Map);
                SAMPLER(sampler_VertexOffset_Map);
                float4 _VertexOffset_Map_TexelSize;
                float4 _VertexOffset_Map_ST;
                float4 _VertexOffset_Vec;
                float4 _VertexOffset_CustomDir;
                float _VertexOffset_NormalDir_Toggle;
                float _VertexOffset_DirectionSpace;
                float _VertexOffset_Toggle;
                TEXTURE2D(_VertexOffset_MaskMap);
                SAMPLER(sampler_VertexOffset_MaskMap);
                float4 _VertexOffset_MaskMap_TexelSize;
                float4 _VertexOffset_MaskMap_ST;
                float4 _VertexOffset_MaskMap_Vec;
                float _VertexOffset_Mask_Toggle;
                float _NB_ColorChannelHi16;
                float _MatCapToggle;
                TEXTURE2D(_MatCapTex);
                SAMPLER(sampler_MatCapTex);
                float4 _MatCapColor;
                float4 _MatCapInfo;
                float _BumpMapToggle;
                TEXTURE2D(_BumpTex);
                SAMPLER(sampler_BumpTex);
                float4 _BumpTex_ST;
                float _BumpScale;
                float _FxLightMode;
                float4 _MaterialInfo;
                float4 _SpecularColor;
                float _ProgramNoise_Toggle;
                float _ProgramNoise_Simple_Toggle;
                float _ProgramNoise_Voronoi_Toggle;
                float _ProgramNoise_Rotate;
                float4 _DissolveVoronoi_Vec;
                float4 _DissolveVoronoi_Vec2;
                float4 _DissolveVoronoi_Vec3;
                float4 _DissolveVoronoi_Vec4;
                float _ProgramNoiseBaseBlendOpacity;
                float _MaskPNoiseBlendOpacity;
                float _DissolvePNoiseBlendOpacity;
                float _NB_PNoiseBlendLo16;
                float _NB_PNoiseBlendHi16;
                TEXTURE2D(_RigRTBk);
                SAMPLER(sampler_RigRTBk);
                TEXTURE2D(_RigLBtF);
                SAMPLER(sampler_RigLBtF);
                TEXTURE2D(_SixWayEmissionRamp);
                SAMPLER(sampler_SixWayEmissionRamp);
                float4 _SixWayInfo;
                float4 _SixWayEmissionColor;
                float _SixWayColorAbsorptionToggle;
                float _DistortPNoiseBlendOpacity;
                UNITY_TEXTURE_STREAMING_DEBUG_VARS;
                CBUFFER_END
                #define UNITY_ACCESS_HYBRID_INSTANCED_PROP(var, type) var
            
            // Graph Includes
            #include_with_pragmas "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBGraphBaseColor.hlsl"
            #include_with_pragmas "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBGraphVertexOffset.hlsl"
            #include_with_pragmas "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBGraphBaseUV.hlsl"
            
            // -- Property used by ScenePickingPass
            #ifdef SCENEPICKINGPASS
            float4 _SelectionID;
            #endif
            
            // -- Properties used by SceneSelectionPass
            #ifdef SCENESELECTIONPASS
            int _ObjectId;
            int _PassValue;
            #endif
            
            // Graph Functions
            // GraphFunctions: <None>
            
            // Custom interpolators pre vertex
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
            
            // Graph Vertex
            struct VertexDescription
                {
                    float3 Position;
                    float3 Normal;
                    float3 Tangent;
                    float3 SixBake0;
                    float3 SixBake1;
                    float3 SixBake2;
                    float3 SixBack0;
                    float3 SixBack1;
                    float3 SixBack2;
                    float4 SixTangentSigned;
                };
                
                VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
                {
                    VertexDescription description = (VertexDescription)0;
                    float3 _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Bake0_3_Vector3;
                    float3 _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Bake1_4_Vector3;
                    float3 _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Bake2_5_Vector3;
                    float3 _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Back0_6_Vector3;
                    float3 _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Back1_7_Vector3;
                    float3 _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Back2_8_Vector3;
                    float4 _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_TangentSigned_9_Vector4;
                    NBGraphSixWayBake_float(IN.WorldSpaceNormal, IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Bake0_3_Vector3, _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Bake1_4_Vector3, _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Bake2_5_Vector3, _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Back0_6_Vector3, _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Back1_7_Vector3, _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Back2_8_Vector3, _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_TangentSigned_9_Vector4);
                    float4 _UV_596a5b2640e15e43aff23ea3c9c87059_Out_0_Vector4 = IN.uv0;
                    float4 _UV_7fa8f9025e9059649e9bdc291afd4417_Out_0_Vector4 = IN.uv1;
                    float4 _UV_8576cf3978f855598e9e2c2e37cdc003_Out_0_Vector4 = IN.uv2;
                    UnityTexture2D _Property_6f7cd0164d485a30803eb055841cd1ce_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_VertexOffset_Map, sampler_VertexOffset_Map, _VertexOffset_Map_TexelSize, _VertexOffset_Map_ST, float4(0, 0, 0, 0));
                    float4 _Property_fd85324fa4f658d691990180b8f8793e_Out_0_Vector4 = _VertexOffset_Vec;
                    float4 _Property_ccc6a218db89506f996c9a8c85bf89d4_Out_0_Vector4 = _VertexOffset_CustomDir;
                    float _Property_8cf30747736a58bcb43ec6620008dd56_Out_0_Float = _VertexOffset_NormalDir_Toggle;
                    float _Property_d1162da7b0a7551b80f631805d2a7134_Out_0_Float = _VertexOffset_DirectionSpace;
                    float _Property_18ffb5b24a215c74989dff20b668c91b_Out_0_Float = _VertexOffset_Toggle;
                    UnityTexture2D _Property_33de3bd0609655b7b71629b91a283fa1_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_VertexOffset_MaskMap, sampler_VertexOffset_MaskMap, _VertexOffset_MaskMap_TexelSize, _VertexOffset_MaskMap_ST, float4(0, 0, 0, 0));
                    float4 _Property_c972b51415e4592b9f07b2c052809e33_Out_0_Vector4 = _VertexOffset_MaskMap_Vec;
                    float _Property_01f9ee3ac21a544aa8c1fa82346bd03e_Out_0_Float = _VertexOffset_Mask_Toggle;
                    float _Property_849d65e3adfb5e4f996dd29864f2c86f_Out_0_Float = _NB_Flags0Lo16;
                    float _Property_3198e5a482ff5fb79bfc8ef49e694023_Out_0_Float = _NB_Flags0Hi16;
                    float _Property_c9800dd9977850049d324b45be73cc33_Out_0_Float = _NB_Flags1Lo16;
                    float _Property_dea9b3365dc35d0daf45f162fe71ae92_Out_0_Float = _NB_Flags1Hi16;
                    float _Property_a457aad3783c559d96ddfe0cf5d647ea_Out_0_Float = _NB_WrapFlagsLo16;
                    float _Property_14f98c3cf7b7525d8340b90ff8e5fd35_Out_0_Float = _NB_WrapFlagsHi16;
                    float _Property_560a4b92f5295597bb1175a0f82a5c76_Out_0_Float = _NB_ColorChannelLo16;
                    float _Property_770d2aaf793952348bc222b930283f2b_Out_0_Float = _NB_ColorChannelHi16;
                    float _Property_efa6ef74db395e619e03f11b575a1029_Out_0_Float = _NB_UVModeFlag0Lo16;
                    float _Property_ef246b0486a15ce4a55423fe806ebb20_Out_0_Float = _NB_UVModeFlag0Hi16;
                    float _Property_214c5c87c6c754658d4b9d2883ae02c5_Out_0_Float = _NB_UVModeFlagType0Lo16;
                    float _Property_b9de1de3181c5b9b9fcdb4836a81b926_Out_0_Float = _NB_UVModeFlagType0Hi16;
                    float4 _Property_9e2af066867e551bb1eed3199acfe809_Out_0_Vector4 = _SharedUV_ST;
                    float4 _Property_614e89fe251b5185af927c4ee7175f5f_Out_0_Vector4 = _SharedUV_Vec;
                    float4 _Property_b2c755308b2759418ed105ab21db3493_Out_0_Vector4 = _TWParameter;
                    float _Property_132da21d369f53e9b55031e5415fe894_Out_0_Float = _TWStrength;
                    float4 _Property_3561d005d5bb5f4ab0d3e545e06a5780_Out_0_Vector4 = _PCCenter;
                    float3 _NBGraphVertexOffsetCustomFunction_395601997b2e51c795e7c1ec707a5553_OutPositionOS_33_Vector3;
                    float3 _NBGraphVertexOffsetCustomFunction_395601997b2e51c795e7c1ec707a5553_OutNormalOS_34_Vector3;
                    float3 _NBGraphVertexOffsetCustomFunction_395601997b2e51c795e7c1ec707a5553_OutTangentOS_35_Vector3;
                    float _NBGraphVertexOffsetCustomFunction_395601997b2e51c795e7c1ec707a5553_Supported_36_Float;
                    NBGraphVertexOffset_float(IN.ObjectSpacePosition, IN.ObjectSpaceNormal, IN.ObjectSpaceTangent, (IN.VertexColor.xyz), _UV_596a5b2640e15e43aff23ea3c9c87059_Out_0_Vector4, _UV_7fa8f9025e9059649e9bdc291afd4417_Out_0_Vector4, _UV_8576cf3978f855598e9e2c2e37cdc003_Out_0_Vector4, _Property_6f7cd0164d485a30803eb055841cd1ce_Out_0_Texture2D, _Property_fd85324fa4f658d691990180b8f8793e_Out_0_Vector4, (_Property_ccc6a218db89506f996c9a8c85bf89d4_Out_0_Vector4.xyz), _Property_8cf30747736a58bcb43ec6620008dd56_Out_0_Float, _Property_d1162da7b0a7551b80f631805d2a7134_Out_0_Float, _Property_18ffb5b24a215c74989dff20b668c91b_Out_0_Float, _Property_33de3bd0609655b7b71629b91a283fa1_Out_0_Texture2D, (_Property_c972b51415e4592b9f07b2c052809e33_Out_0_Vector4.xyz), _Property_01f9ee3ac21a544aa8c1fa82346bd03e_Out_0_Float, _Property_849d65e3adfb5e4f996dd29864f2c86f_Out_0_Float, _Property_3198e5a482ff5fb79bfc8ef49e694023_Out_0_Float, _Property_c9800dd9977850049d324b45be73cc33_Out_0_Float, _Property_dea9b3365dc35d0daf45f162fe71ae92_Out_0_Float, _Property_a457aad3783c559d96ddfe0cf5d647ea_Out_0_Float, _Property_14f98c3cf7b7525d8340b90ff8e5fd35_Out_0_Float, _Property_560a4b92f5295597bb1175a0f82a5c76_Out_0_Float, _Property_770d2aaf793952348bc222b930283f2b_Out_0_Float, _Property_efa6ef74db395e619e03f11b575a1029_Out_0_Float, _Property_ef246b0486a15ce4a55423fe806ebb20_Out_0_Float, _Property_214c5c87c6c754658d4b9d2883ae02c5_Out_0_Float, _Property_b9de1de3181c5b9b9fcdb4836a81b926_Out_0_Float, _Property_9e2af066867e551bb1eed3199acfe809_Out_0_Vector4, _Property_614e89fe251b5185af927c4ee7175f5f_Out_0_Vector4, _Property_b2c755308b2759418ed105ab21db3493_Out_0_Vector4, _Property_132da21d369f53e9b55031e5415fe894_Out_0_Float, _Property_3561d005d5bb5f4ab0d3e545e06a5780_Out_0_Vector4, _NBGraphVertexOffsetCustomFunction_395601997b2e51c795e7c1ec707a5553_OutPositionOS_33_Vector3, _NBGraphVertexOffsetCustomFunction_395601997b2e51c795e7c1ec707a5553_OutNormalOS_34_Vector3, _NBGraphVertexOffsetCustomFunction_395601997b2e51c795e7c1ec707a5553_OutTangentOS_35_Vector3, _NBGraphVertexOffsetCustomFunction_395601997b2e51c795e7c1ec707a5553_Supported_36_Float);
                    description.Position = _NBGraphVertexOffsetCustomFunction_395601997b2e51c795e7c1ec707a5553_OutPositionOS_33_Vector3;
                    description.Normal = _NBGraphVertexOffsetCustomFunction_395601997b2e51c795e7c1ec707a5553_OutNormalOS_34_Vector3;
                    description.Tangent = _NBGraphVertexOffsetCustomFunction_395601997b2e51c795e7c1ec707a5553_OutTangentOS_35_Vector3;
                    description.SixBake0 = _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Bake0_3_Vector3;
                    description.SixBake1 = _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Bake1_4_Vector3;
                    description.SixBake2 = _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Bake2_5_Vector3;
                    description.SixBack0 = _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Back0_6_Vector3;
                    description.SixBack1 = _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Back1_7_Vector3;
                    description.SixBack2 = _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Back2_8_Vector3;
                    description.SixTangentSigned = _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_TangentSigned_9_Vector4;
                    return description;
                }
            
            // Custom interpolators, pre surface
            #ifdef FEATURES_GRAPH_VERTEX
            Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
            {
            output.SixBake0 = input.SixBake0;
            output.SixBake1 = input.SixBake1;
            output.SixBake2 = input.SixBake2;
            output.SixBack0 = input.SixBack0;
            output.SixBack1 = input.SixBack1;
            output.SixBack2 = input.SixBack2;
            output.SixTangentSigned = input.SixTangentSigned;
            return output;
            }
            #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
            #endif
            
            // Graph Pixel
            struct SurfaceDescription
                {
                    float Alpha;
                    float AlphaClipThreshold;
                };
                
                SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
                {
                    SurfaceDescription surface = (SurfaceDescription)0;
                    float4 _Property_13840ad57da543e2b33275638ce1189e_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_Color) : _Color;
                    float _Property_eafd46b75a1746f093439de914a3717c_Out_0_Float = _NB_Flags0Lo16;
                    float _Property_02448b2e80a0440b804602e588eb2727_Out_0_Float = _NB_Flags0Hi16;
                    float _Property_52b95ce95e564cb083a7953179be1554_Out_0_Float = _NB_Flags1Lo16;
                    float _Property_6abc2ac395a54d7f99af7850796b2c20_Out_0_Float = _NB_Flags1Hi16;
                    float2 _Property_4fbc64ac55184ee8b2348b861c93db8b_Out_0_Vector2 = _NB_DistortionNoise;
                    float _Property_9099f47df6d24486a9e0bb9dd8fa7cf8_Out_0_Float = _NB_DistortionIntensity;
                    float _Property_01827f8348b040028326054a1623cc84_Out_0_Float = _NB_DistortionMode;
                    float _Property_87436152ca164e73877206654813b186_Out_0_Float = _NB_DistortionAlphaPow;
                    float _Property_9aab3b8bff1a4b39a348ae3ea124a0e3_Out_0_Float = _NB_DistortionAlphaMultiplier;
                    float _Property_a31f37f37774434e8b5383ecef8f5072_Out_0_Float = _NB_DistortionAlphaAdd;
                    UnityTexture2D _Property_e1e8e8a73a5f5e1c9e969abcb9782cd2_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_MaskMap, sampler_MaskMap, _MaskMap_TexelSize, _MaskMap_ST, float4(0, 0, 0, 0));
                    float _Property_26acbca87fca52f4a5ea05d6fcf09e48_Out_0_Float = _Mask_Toggle;
                    float4 _Property_c7f5afaa32df59eb9c6b2561b57aa26f_Out_0_Vector4 = _MaskMapVec;
                    float4 _Property_6e138238299250348f63a5ee2f1f3d5b_Out_0_Vector4 = _MaskRefineVec;
                    float _Property_20f932f599b85641aeb7c2ea908ed484_Out_0_Float = _NB_ColorChannelLo16;
                    UnityTexture2D _Property_b4f86e887f26523f845f932a27414b5b_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_DissolveMap, sampler_DissolveMap, _DissolveMap_TexelSize, _DissolveMap_ST, float4(0, 0, 0, 0));
                    float _Property_a70b9f8477c75d2a98fd83c15f287a37_Out_0_Float = _Dissolve_Toggle;
                    float4 _Property_a05025df3a5c534f88355f30cb99d2d3_Out_0_Vector4 = _Dissolve;
                    float4 _UV_dde6e6f3492e4fa689d7f4a88f3b492b_Out_0_Vector4 = IN.uv0;
                    float4 _Property_7a2d908f5a2f4990ae14fd870e5a22c9_Out_0_Vector4 = _BaseMap_ST;
                    float _Property_45764d3c363e48539c14ce1d9b8eb3bb_Out_0_Float = _BaseMapUVRotation;
                    float _Property_4c0c6ce883a14260b681ac41d0f78472_Out_0_Float = _BaseMapUVRotationSpeed;
                    float4 _Property_ea0189e7a35b4e44a9e5775b9d1ec406_Out_0_Vector4 = _BaseMapMaskMapOffset;
                    float4 _UV_a8924bd05fcf4a28878b71b6b3b05cd7_Out_0_Vector4 = IN.uv1;
                    float4 _UV_ce3ab892df664c348236f6bce976c116_Out_0_Vector4 = IN.uv2;
                    float _Property_3ca4059fa35141669dc7e92d919136a2_Out_0_Float = _NB_UVModeFlag0Lo16;
                    float _Property_320511e8014a49c4a7e44e21e9c57f02_Out_0_Float = _NB_UVModeFlag0Hi16;
                    float _Property_a91f4458b51f4b488287925809b8a798_Out_0_Float = _NB_UVModeFlagType0Lo16;
                    float _Property_06eb05a4a820436ab686475e2d043b4d_Out_0_Float = _NB_UVModeFlagType0Hi16;
                    float4 _Property_03cf5077fa564f27841d641d9c62f6a3_Out_0_Vector4 = _SharedUV_ST;
                    float4 _Property_338f6b6c652440289b94c105205125f7_Out_0_Vector4 = _SharedUV_Vec;
                    float4 _Property_df09eceb3f6847288ccb0e2f2b6beb4c_Out_0_Vector4 = _TWParameter;
                    float _Property_4cd2fdd2348a4b7795e93a4e7e997131_Out_0_Float = _TWStrength;
                    float4 _Property_65a8673d6744483e8aaf1ea1d5d4d4ca_Out_0_Vector4 = _PCCenter;
                    float2 _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_Out_5_Vector2;
                    float2 _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_MaskUV_22_Vector2;
                    float2 _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_Mask2UV_23_Vector2;
                    float2 _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_Mask3UV_24_Vector2;
                    float2 _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_EmissionUV_25_Vector2;
                    float2 _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_DissolveUV_26_Vector2;
                    float2 _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_DissolveMaskUV_27_Vector2;
                    float2 _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_ColorBlendUV_28_Vector2;
                    float2 _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_RampColorUV_29_Vector2;
                    float2 _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_NoiseUV_30_Vector2;
                    float2 _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_NoiseMaskUV_31_Vector2;
                    float2 _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_BumpUV_32_Vector2;
                    float2 _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_ProgramNoiseUV_33_Vector2;
                    NBGraphBaseUV_float(_UV_dde6e6f3492e4fa689d7f4a88f3b492b_Out_0_Vector4, _Property_7a2d908f5a2f4990ae14fd870e5a22c9_Out_0_Vector4, _Property_45764d3c363e48539c14ce1d9b8eb3bb_Out_0_Float, _Property_4c0c6ce883a14260b681ac41d0f78472_Out_0_Float, _Property_ea0189e7a35b4e44a9e5775b9d1ec406_Out_0_Vector4, _UV_a8924bd05fcf4a28878b71b6b3b05cd7_Out_0_Vector4, _UV_ce3ab892df664c348236f6bce976c116_Out_0_Vector4, _Property_eafd46b75a1746f093439de914a3717c_Out_0_Float, _Property_02448b2e80a0440b804602e588eb2727_Out_0_Float, _Property_52b95ce95e564cb083a7953179be1554_Out_0_Float, _Property_6abc2ac395a54d7f99af7850796b2c20_Out_0_Float, _Property_3ca4059fa35141669dc7e92d919136a2_Out_0_Float, _Property_320511e8014a49c4a7e44e21e9c57f02_Out_0_Float, _Property_a91f4458b51f4b488287925809b8a798_Out_0_Float, _Property_06eb05a4a820436ab686475e2d043b4d_Out_0_Float, _Property_03cf5077fa564f27841d641d9c62f6a3_Out_0_Vector4, _Property_338f6b6c652440289b94c105205125f7_Out_0_Vector4, _Property_df09eceb3f6847288ccb0e2f2b6beb4c_Out_0_Vector4, _Property_4cd2fdd2348a4b7795e93a4e7e997131_Out_0_Float, _Property_65a8673d6744483e8aaf1ea1d5d4d4ca_Out_0_Vector4, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_Out_5_Vector2, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_MaskUV_22_Vector2, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_Mask2UV_23_Vector2, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_Mask3UV_24_Vector2, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_EmissionUV_25_Vector2, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_DissolveUV_26_Vector2, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_DissolveMaskUV_27_Vector2, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_ColorBlendUV_28_Vector2, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_RampColorUV_29_Vector2, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_NoiseUV_30_Vector2, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_NoiseMaskUV_31_Vector2, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_BumpUV_32_Vector2, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_ProgramNoiseUV_33_Vector2);
                    float _Property_c4d2b9d87084415fb9b51e578a0b9080_Out_0_Float = _MaskMapUVRotation;
                    float _Property_43ed7c33980543efb0a5f6f413791461_Out_0_Float = _MaskMapRotationSpeed;
                    float4 _Property_63ba51c6efaf4d659d2b6b921e7f1a43_Out_0_Vector4 = _MaskMapOffsetAnition;
                    float4 _Property_f9f92343294e499b8f4512de6aeefa5c_Out_0_Vector4 = _DissolveOffsetRotateDistort;
                    UnityTexture2D _Property_a5f2eeafb2c748ed9a1ec290960ab16f_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_DissolveMaskMap, sampler_DissolveMaskMap, _DissolveMaskMap_TexelSize, _DissolveMaskMap_ST, float4(0, 0, 0, 0));
                    float _Property_56d752220cbf4d96ab46764fc9aebb16_Out_0_Float = _DissolveMask_Toggle;
                    float _Property_e1702b0dd4cf43f18d778429097ef342_Out_0_Float = _DissolveMaskMode;
                    UnityTexture2D _Property_a6ea548184d045a89325bc16be701913_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_MaskMap2, sampler_MaskMap2, _MaskMap2_TexelSize, _MaskMap2_ST, float4(0, 0, 0, 0));
                    float _Property_7c78f411c5ff4fac85800c00002c0fbc_Out_0_Float = _Mask2_Toggle;
                    UnityTexture2D _Property_f9394d12cb9a412e8858ece414106043_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_MaskMap3, sampler_MaskMap3, _MaskMap3_TexelSize, _MaskMap3_ST, float4(0, 0, 0, 0));
                    float _Property_7e16bb82a3584c73a47c9dcc897f62a3_Out_0_Float = _Mask3_Toggle;
                    float4 _Property_282301309355467f98c2a0ca1c46538f_Out_0_Vector4 = _MaskMap3OffsetAnition;
                    float _Property_901675ce420344bb8ef7c1598f2a8c6c_Out_0_Float = _NB_WrapFlagsLo16;
                    float _Property_84ab548e17414d57ab8ab8e23e9c9a5d_Out_0_Float = _NB_WrapFlagsHi16;
                    float _Property_0a9b0dca2b7846869eb548347e06dfe0_Out_0_Float = _MaskMapGradientCount;
                    float4 _Property_bceef4027b6c4cc4b1d21c40d1e6b380_Out_0_Vector4 = _MaskMapGradientFloat0;
                    float4 _Property_c8f145683c5e4bc2b543907d534d50ac_Out_0_Vector4 = _MaskMapGradientFloat1;
                    float4 _Property_86555f566d7a4e47ad576dcbd5832f93_Out_0_Vector4 = _MaskMapGradientFloat2;
                    float _Property_3e87b9d2a706453cbd9014c75528eaa0_Out_0_Float = _MaskMap2GradientCount;
                    float4 _Property_53483bdbc2c74a8b824ea143d6eaec2b_Out_0_Vector4 = _MaskMap2GradientFloat0;
                    float4 _Property_4f05348fd4524abc964972190787b609_Out_0_Vector4 = _MaskMap2GradientFloat1;
                    float4 _Property_6235b1011ee0450c80fec0958cf7b4d4_Out_0_Vector4 = _MaskMap2GradientFloat2;
                    float _Property_a6bf239ac9f240709d42314fc1efd7b1_Out_0_Float = _MaskMap3GradientCount;
                    float4 _Property_bcd09d9bc52b4a4f9cb57238c0902210_Out_0_Vector4 = _MaskMap3GradientFloat0;
                    float4 _Property_f14563d83f83427a8730d060aca57e2e_Out_0_Vector4 = _MaskMap3GradientFloat1;
                    float4 _Property_43d61a210ee74ffda3c292b0848511af_Out_0_Vector4 = _MaskMap3GradientFloat2;
                    float _Property_5cdf8c4f8e5f4271bf734cfe073fcc52_Out_0_Float = _AlphaAll;
                    float4 _Property_adbdd97beb12402b87648f77f30ab4c0_Out_0_Vector4 = _ColorA;
                    float _Property_442020c8e09d56679b49c301f56b94a4_Out_0_Float = _BaseColorIntensityForTimeline;
                    float4 _Property_b2641bfb6535514caebe85f35a335537_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_BaseBackColor) : _BaseBackColor;
                    float _IsFrontFace_d1919b09d5db5f54aaa1b20fe1d91dbf_Out_0_Boolean = max(0, IN.FaceSign.x);
                    UnityTexture2D _Property_92eefc72bf7f56fd8f8e4084e22172be_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_EmissionMap, sampler_EmissionMap, _EmissionMap_TexelSize, _EmissionMap_ST, float4(0, 0, 0, 0));
                    float _Property_318d04aaa2195ad4b539c26b2c04eeac_Out_0_Float = _EmissionEnabled;
                    float4 _Property_7d76134eb0065dcf88906e1e451e441f_Out_0_Vector4 = _EmissionMapUVOffset;
                    float _Property_3540299c49785e3c86755a5fe22db945_Out_0_Float = _EmissionMapUVRotation;
                    float4 _Property_4af1e92a383f585dbcf0fb0884888164_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_EmissionMapColor) : _EmissionMapColor;
                    float _Property_10781681264d5230974041a749fa166e_Out_0_Float = _EmissionMapColorIntensity;
                    float _Property_6b0f493dcf575d0ab56773dc813aefa6_Out_0_Float = _EmissionAlphaIntensity;
                    UnityTexture2D _Property_d61bfdcc9d345709b6c4e37db8d8dc9d_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_ColorBlendMap, sampler_ColorBlendMap, _ColorBlendMap_TexelSize, _ColorBlendMap_ST, float4(0, 0, 0, 0));
                    float _Property_30534193d46e557494c8f844a330b5b4_Out_0_Float = _ColorBlendMap_Toggle;
                    float4 _Property_36317ab840af5f6db269cbc1733a86db_Out_0_Vector4 = _ColorBlendMapOffset;
                    float4 _Property_e78fc93d059558309f51829a464ff1ed_Out_0_Vector4 = _ColorBlendVec;
                    float4 _Property_a0161d17445e53a18e11ae4102584287_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_ColorBlendColor) : _ColorBlendColor;
                    float _Property_aa8cec6d7536592c83454ce0965f8b85_Out_0_Float = _ColorBlendColorIntensity;
                    UnityTexture2D _Property_2c26a69a61fc52a5939877dca4507b70_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_RampColorMap, sampler_RampColorMap, _RampColorMap_TexelSize, _RampColorMap_ST, float4(0, 0, 0, 0));
                    float _Property_668c759ef0f85daa97ec0fa2c458fd91_Out_0_Float = _RampColorToggle;
                    float _Property_0d0fcda8bbbf5e5dadddaf843017559f_Out_0_Float = _RampColorSourceMode;
                    float4 _Property_eb2d499a8f40574f91ca2bfbb37b0bf7_Out_0_Vector4 = _RampColorMapOffset;
                    float4 _Property_65576e0f99385f1fb9a7c67087c5b89d_Out_0_Vector4 = _RampColor0;
                    float4 _Property_55e82011a7bd54c7969e4044ab34a602_Out_0_Vector4 = _RampColor1;
                    float4 _Property_f5084aa30a6751de8d599e42fb5ffff5_Out_0_Vector4 = _RampColor2;
                    float4 _Property_8725b71298fd58698edbc87798ea15e9_Out_0_Vector4 = _RampColor3;
                    float4 _Property_04d0659f70de5bb8bf5972d05a3b68c8_Out_0_Vector4 = _RampColor4;
                    float4 _Property_4989a100e056527b92055a3c516a4eab_Out_0_Vector4 = _RampColor5;
                    float4 _Property_77bf5e95cff652cdaaa6bf93adeb0f93_Out_0_Vector4 = _RampColorAlpha0;
                    float4 _Property_c5d14f129a11542581793506ad8ba24e_Out_0_Vector4 = _RampColorAlpha1;
                    float4 _Property_e84364b4aa05540ebb986ab49893b436_Out_0_Vector4 = _RampColorAlpha2;
                    float _Property_7f00ce17ce115d2bb969973b132fe3e8_Out_0_Float = _RampColorCount;
                    float4 _Property_89e65c225052579ea3ac6095a234f65a_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_RampColorBlendColor) : _RampColorBlendColor;
                    float _Property_4750fb93d1d1562bad31b25dec3f8b25_Out_0_Float = _HueShift;
                    float _Property_5e6a32f15bf05438b9cc108706ad0625_Out_0_Float = _Contrast;
                    float4 _Property_866cffb762935a229d1b940e1f8b905c_Out_0_Vector4 = _ContrastMidColor;
                    float _Property_f97ecb43f95c5e37b841c32f67fa546e_Out_0_Float = _Saturability;
                    float4 _Property_713b0ac7fdd85eb2a23076a581e94638_Out_0_Vector4 = _BaseMapColorRefine;
                    float _Property_2fd1783a3bdc52f88effab68b88e0a47_Out_0_Float = _fresnelEnabled;
                    float4 _Property_760680a31d7b57b3a03b703fedd7dea7_Out_0_Vector4 = _FresnelUnit;
                    float4 _Property_2e33a988d6ea5eaea31eb783d2cb2026_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_FresnelColor) : _FresnelColor;
                    float4 _Property_990aa20b0fbe58bab561e7a26ecc62ad_Out_0_Vector4 = _FresnelRotation;
                    float4 _Property_e5767a3f300954768906d034fee505b2_Out_0_Vector4 = _Dissolve_Vec2;
                    float4 _Property_28013c8ed2865dbba415684e772fbe2f_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_DissolveLineColor) : _DissolveLineColor;
                    float _Property_872303b3673f5db6ba39a80132854337_Out_0_Float = _Dissolve_useRampMap_Toggle;
                    UnityTexture2D _Property_f712ace2c0ec59aca6e4ea56a5dc642b_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_DissolveRampMap, sampler_DissolveRampMap, _DissolveRampMap_TexelSize, _DissolveRampMap_ST, float4(0, 0, 0, 0));
                    float _Property_3f274a4912255dc2aa6e18641c408bd6_Out_0_Float = _DissolveRampSourceMode;
                    float4 _Property_2d934283df7757faadf21ff2aa713bb4_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_DissolveRampColor) : _DissolveRampColor;
                    float _Property_d2afb0df25d55430acab801c8a3dbfde_Out_0_Float = _DissolveRampCount;
                    float4 _Property_0dec3b42d61b5e7c862c4ca295f9e208_Out_0_Vector4 = _DissolveRampColor0;
                    float4 _Property_4b091596ce61549b96d0e88967383f48_Out_0_Vector4 = _DissolveRampColor1;
                    float4 _Property_e246b4e0fa405aca8b55e192c06587a0_Out_0_Vector4 = _DissolveRampColor2;
                    float4 _Property_7a5d7978834d5d4eb9428c0a196dfbd3_Out_0_Vector4 = _DissolveRampColor3;
                    float4 _Property_6ab07b2800785722806fc80f0a180a5e_Out_0_Vector4 = _DissolveRampColor4;
                    float4 _Property_5250e3a06c0252d6a6bb9743ea2f0ac1_Out_0_Vector4 = _DissolveRampColor5;
                    float4 _Property_64999d5c415b5a73b4a0c204bcf29377_Out_0_Vector4 = _DissolveRampAlpha0;
                    float4 _Property_a934aa24a47f5da2992d3e468dbd374d_Out_0_Vector4 = _DissolveRampAlpha1;
                    float4 _Property_629063539c105cb3a2e8d3eade7eefb1_Out_0_Vector4 = _DissolveRampAlpha2;
                    float _Property_fc9eb8a77e24500ab5421051111d6fda_Out_0_Float = _NB_ForceNoMipFlagsLo16;
                    float _Property_6311ff30de835a0da66e70c41e5eb4a9_Out_0_Float = _NB_ForceNoMipFlagsHi16;
                    float _Property_9439295f5abe51489892e22adc16c2b7_Out_0_Float = _NB_DissolveRampSTOverrideEnabled;
                    float4 _Property_e0d280ecac5f56c48b444399bdfee24d_Out_0_Vector4 = _NB_DissolveRampSTOverride;
                    float _Property_d24b5ab9f5354781acf90544fdd34e1a_Out_0_Float = _DistanceFade_Toggle;
                    float4 _Property_d525ffde86414d429eb7d33f95dbc125_Out_0_Vector4 = _Fade;
                    float _Property_2f1d59a7ccfd4e21bdcabf18c881577c_Out_0_Float = _SoftParticlesEnabled;
                    float4 _Property_c04e77b9696340d1b77e0c6a9910ffae_Out_0_Vector4 = _SoftParticleFadeParams;
                    float4 _ScreenPosition_86fbeeb2af2346549e9611c0551ed415_Out_0_Vector4 = float4(IN.NDCPosition.xy, 0, 0);
                    float _Property_67f9358d1a79442eb25e5a4b980feddb_Out_0_Float = _DepthOutline_Toggle;
                    float4 _Property_75062f76b29a4f08b37655dc233735d6_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_DepthOutline_Color) : _DepthOutline_Color;
                    float4 _Property_8fbc75cf62cb493ea789465b0e094396_Out_0_Vector4 = _DepthOutline_Vec;
                    UnityTexture2D _Property_8f072da64b1f45d992849b1e47b91c14_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_BaseMap, sampler_BaseMap, _BaseMap_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
                    UnityTexture2D _Property_a9a873c1f9545377b6d5603870d8ce7e_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_NoiseMap, sampler_NoiseMap, _NoiseMap_TexelSize, _NoiseMap_ST, float4(0, 0, 0, 0));
                    float _Property_05cfd7336ce55efda9ba538458718e94_Out_0_Float = _noisemapEnabled;
                    float _Property_de00f2b38f8d559eb2e11b9a8afbf0ab_Out_0_Float = _NoiseMapUVRotation;
                    float4 _Property_c1523df23e3f5ea7a2816bc39b189b9d_Out_0_Vector4 = _NoiseOffset;
                    float _Property_6745a2aa39ce5a6db6ef4145ce16fc4d_Out_0_Float = _NoiseIntensity;
                    float4 _Property_0692aaad7ae555198da5d2971b415df9_Out_0_Vector4 = _DistortionDirection;
                    UnityTexture2D _Property_7a1c57aac1ae5322904a7fbc9050f2ce_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_NoiseMaskMap, sampler_NoiseMaskMap, _NoiseMaskMap_TexelSize, _NoiseMaskMap_ST, float4(0, 0, 0, 0));
                    float _Property_931f026875de5593b8c7d6f47800f865_Out_0_Float = _noiseMaskMap_Toggle;
                    float _Property_cff65480aa2c5e98b542088b70270554_Out_0_Float = _TexDistortion_intensity;
                    float _Property_5f7732a02ece5e6fb25d3e1ecc105818_Out_0_Float = _Emi_Distortion_intensity;
                    float _Property_b04226779a985e25a46ae050e8c15dcc_Out_0_Float = _MaskDistortion_intensity;
                    float _Property_415818c2850c582fa48e2026af7e309e_Out_0_Float = _MatCapToggle;
                    UnityTexture2D _Property_53f494020d7651608ba56c8922ff1ab9_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_MatCapTex, sampler_MatCapTex, float4(1, 1, 1, 1), float4(1, 1, 0, 0), float4(0, 0, 0, 0));
                    float4 _Property_6df429d0561957cb84dfad2ee12aeb46_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_MatCapColor) : _MatCapColor;
                    float4 _Property_5c8f0472975b5156a2b33799226739c6_Out_0_Vector4 = _MatCapInfo;
                    float _Property_d2d631afc2c1544b83cb34681aa85a03_Out_0_Float = _BumpMapToggle;
                    UnityTexture2D _Property_8a8ba308b2a1563ea8c2419c29c5c7b9_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_BumpTex, sampler_BumpTex, float4(1, 1, 1, 1), _BumpTex_ST, float4(0, 0, 0, 0));
                    float _Property_6d9934d6abfd5c0697171ddfd0c97fdc_Out_0_Float = _BumpScale;
                    float _Property_e5ae484542c256ef9e2c843d58514935_Out_0_Float = _FxLightMode;
                    float4 _Property_6301978f07ae543781068ab22b637a6c_Out_0_Vector4 = _MaterialInfo;
                    float4 _Property_9f86734760a45426a1b7d524232798ab_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_SpecularColor) : _SpecularColor;
                    float _Property_665d05cdb47f5ac3b55f9494eebcfd24_Out_0_Float = _ProgramNoise_Toggle;
                    float _Property_97cf65b2fe9f579a9efe26a73d830bb9_Out_0_Float = _ProgramNoise_Simple_Toggle;
                    float _Property_46cf15a5712c5271ab621a4945709213_Out_0_Float = _ProgramNoise_Voronoi_Toggle;
                    float _Property_4d5ce3c87f6b5b5eb6b5488a205f5da2_Out_0_Float = _ProgramNoise_Rotate;
                    float4 _Property_73408a6e8749509dbf03b29118ea169a_Out_0_Vector4 = _DissolveVoronoi_Vec;
                    float4 _Property_4c7f65f090b75d33a899f4860a5eb28c_Out_0_Vector4 = _DissolveVoronoi_Vec2;
                    float4 _Property_cc5eb5485f3f503e991dd48cda2773d8_Out_0_Vector4 = _DissolveVoronoi_Vec3;
                    float4 _Property_ea6439cfbd84510292b048fc6619980b_Out_0_Vector4 = _DissolveVoronoi_Vec4;
                    float _Property_ead7fba0b75559f78e76a0fbf67cae12_Out_0_Float = _ProgramNoiseBaseBlendOpacity;
                    float _Property_13f2b053fdb9593aa51c8639f809a891_Out_0_Float = _MaskPNoiseBlendOpacity;
                    float _Property_9767ed7b0d615801a6878b0fc416e6f0_Out_0_Float = _DissolvePNoiseBlendOpacity;
                    float _Property_3750f20ccf7b5a7fb1d9e4bd3fb2b6ec_Out_0_Float = _NB_PNoiseBlendLo16;
                    float _Property_5d371e5c8c7459f280e4945d8a055cdc_Out_0_Float = _NB_PNoiseBlendHi16;
                    UnityTexture2D _Property_c125096410c15c18bbff38461fb105d3_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_RigRTBk, sampler_RigRTBk, float4(1, 1, 1, 1), float4(1, 1, 0, 0), float4(0, 0, 0, 0));
                    UnityTexture2D _Property_38036157e2ec5c659072d3660c785123_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_RigLBtF, sampler_RigLBtF, float4(1, 1, 1, 1), float4(1, 1, 0, 0), float4(0, 0, 0, 0));
                    UnityTexture2D _Property_269c61b4d8fe5a7684fc52010ece7432_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_SixWayEmissionRamp, sampler_SixWayEmissionRamp, float4(1, 1, 1, 1), float4(1, 1, 0, 0), float4(0, 0, 0, 0));
                    float4 _Property_89139d6b0c285de9a58e1f6df2e7d236_Out_0_Vector4 = _SixWayInfo;
                    float4 _Property_547327d8b0f75f868f2ecda76c814b21_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_SixWayEmissionColor) : _SixWayEmissionColor;
                    float _Property_d2fe351137c85eac9a617d3f05ff92b2_Out_0_Float = _SixWayColorAbsorptionToggle;
                    float _Property_25cf481f9d865f129398a908a10a4f11_Out_0_Float = _DistortPNoiseBlendOpacity;
                    float4 _NBGraphBaseColorCustomFunction_f712bc264a1c40848edc2dd476a4cada_Out_3_Vector4;
                    float2 _NBGraphBaseColorCustomFunction_f712bc264a1c40848edc2dd476a4cada_NBDistortionSignedRG_145_Vector2;
                    float _NBGraphBaseColorCustomFunction_f712bc264a1c40848edc2dd476a4cada_NBDistortionNoiseMask_146_Float;
                    NBGraphBaseColor_float(_NBGraphBaseColorCustomFunction_f712bc264a1c40848edc2dd476a4cada_SampledAlbedo_0_Vector4, _NBGraphBaseColorCustomFunction_f712bc264a1c40848edc2dd476a4cada_SelectedAlpha_1_Float, _Property_13840ad57da543e2b33275638ce1189e_Out_0_Vector4, _Property_eafd46b75a1746f093439de914a3717c_Out_0_Float, _Property_02448b2e80a0440b804602e588eb2727_Out_0_Float, _Property_52b95ce95e564cb083a7953179be1554_Out_0_Float, _Property_6abc2ac395a54d7f99af7850796b2c20_Out_0_Float, _Property_4fbc64ac55184ee8b2348b861c93db8b_Out_0_Vector2, _Property_9099f47df6d24486a9e0bb9dd8fa7cf8_Out_0_Float, _Property_01827f8348b040028326054a1623cc84_Out_0_Float, _Property_87436152ca164e73877206654813b186_Out_0_Float, _Property_9aab3b8bff1a4b39a348ae3ea124a0e3_Out_0_Float, _Property_a31f37f37774434e8b5383ecef8f5072_Out_0_Float, _Property_e1e8e8a73a5f5e1c9e969abcb9782cd2_Out_0_Texture2D, _Property_26acbca87fca52f4a5ea05d6fcf09e48_Out_0_Float, _Property_c7f5afaa32df59eb9c6b2561b57aa26f_Out_0_Vector4, _Property_6e138238299250348f63a5ee2f1f3d5b_Out_0_Vector4, _Property_20f932f599b85641aeb7c2ea908ed484_Out_0_Float, _Property_b4f86e887f26523f845f932a27414b5b_Out_0_Texture2D, _Property_a70b9f8477c75d2a98fd83c15f287a37_Out_0_Float, _Property_a05025df3a5c534f88355f30cb99d2d3_Out_0_Vector4, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_MaskUV_22_Vector2, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_DissolveUV_26_Vector2, _Property_c4d2b9d87084415fb9b51e578a0b9080_Out_0_Float, _Property_43ed7c33980543efb0a5f6f413791461_Out_0_Float, _Property_63ba51c6efaf4d659d2b6b921e7f1a43_Out_0_Vector4, _Property_f9f92343294e499b8f4512de6aeefa5c_Out_0_Vector4, _Property_a5f2eeafb2c748ed9a1ec290960ab16f_Out_0_Texture2D, _Property_56d752220cbf4d96ab46764fc9aebb16_Out_0_Float, _Property_e1702b0dd4cf43f18d778429097ef342_Out_0_Float, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_DissolveMaskUV_27_Vector2, _Property_a6ea548184d045a89325bc16be701913_Out_0_Texture2D, _Property_7c78f411c5ff4fac85800c00002c0fbc_Out_0_Float, _Property_f9394d12cb9a412e8858ece414106043_Out_0_Texture2D, _Property_7e16bb82a3584c73a47c9dcc897f62a3_Out_0_Float, _Property_282301309355467f98c2a0ca1c46538f_Out_0_Vector4, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_Mask2UV_23_Vector2, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_Mask3UV_24_Vector2, _Property_901675ce420344bb8ef7c1598f2a8c6c_Out_0_Float, _Property_84ab548e17414d57ab8ab8e23e9c9a5d_Out_0_Float, _Property_0a9b0dca2b7846869eb548347e06dfe0_Out_0_Float, _Property_bceef4027b6c4cc4b1d21c40d1e6b380_Out_0_Vector4, _Property_c8f145683c5e4bc2b543907d534d50ac_Out_0_Vector4, _Property_86555f566d7a4e47ad576dcbd5832f93_Out_0_Vector4, _Property_3e87b9d2a706453cbd9014c75528eaa0_Out_0_Float, _Property_53483bdbc2c74a8b824ea143d6eaec2b_Out_0_Vector4, _Property_4f05348fd4524abc964972190787b609_Out_0_Vector4, _Property_6235b1011ee0450c80fec0958cf7b4d4_Out_0_Vector4, _Property_a6bf239ac9f240709d42314fc1efd7b1_Out_0_Float, _Property_bcd09d9bc52b4a4f9cb57238c0902210_Out_0_Vector4, _Property_f14563d83f83427a8730d060aca57e2e_Out_0_Vector4, _Property_43d61a210ee74ffda3c292b0848511af_Out_0_Vector4, _Property_5cdf8c4f8e5f4271bf734cfe073fcc52_Out_0_Float, _Property_adbdd97beb12402b87648f77f30ab4c0_Out_0_Vector4, IN.VertexColor, _Property_442020c8e09d56679b49c301f56b94a4_Out_0_Float, _Property_b2641bfb6535514caebe85f35a335537_Out_0_Vector4, ((float) _IsFrontFace_d1919b09d5db5f54aaa1b20fe1d91dbf_Out_0_Boolean), _Property_92eefc72bf7f56fd8f8e4084e22172be_Out_0_Texture2D, _Property_318d04aaa2195ad4b539c26b2c04eeac_Out_0_Float, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_EmissionUV_25_Vector2, _Property_7d76134eb0065dcf88906e1e451e441f_Out_0_Vector4, _Property_3540299c49785e3c86755a5fe22db945_Out_0_Float, _Property_4af1e92a383f585dbcf0fb0884888164_Out_0_Vector4, _Property_10781681264d5230974041a749fa166e_Out_0_Float, _Property_6b0f493dcf575d0ab56773dc813aefa6_Out_0_Float, _Property_d61bfdcc9d345709b6c4e37db8d8dc9d_Out_0_Texture2D, _Property_30534193d46e557494c8f844a330b5b4_Out_0_Float, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_ColorBlendUV_28_Vector2, _Property_36317ab840af5f6db269cbc1733a86db_Out_0_Vector4, _Property_e78fc93d059558309f51829a464ff1ed_Out_0_Vector4, _Property_a0161d17445e53a18e11ae4102584287_Out_0_Vector4, _Property_aa8cec6d7536592c83454ce0965f8b85_Out_0_Float, _Property_2c26a69a61fc52a5939877dca4507b70_Out_0_Texture2D, _Property_668c759ef0f85daa97ec0fa2c458fd91_Out_0_Float, _Property_0d0fcda8bbbf5e5dadddaf843017559f_Out_0_Float, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_RampColorUV_29_Vector2, _Property_eb2d499a8f40574f91ca2bfbb37b0bf7_Out_0_Vector4, _Property_65576e0f99385f1fb9a7c67087c5b89d_Out_0_Vector4, _Property_55e82011a7bd54c7969e4044ab34a602_Out_0_Vector4, _Property_f5084aa30a6751de8d599e42fb5ffff5_Out_0_Vector4, _Property_8725b71298fd58698edbc87798ea15e9_Out_0_Vector4, _Property_04d0659f70de5bb8bf5972d05a3b68c8_Out_0_Vector4, _Property_4989a100e056527b92055a3c516a4eab_Out_0_Vector4, _Property_77bf5e95cff652cdaaa6bf93adeb0f93_Out_0_Vector4, _Property_c5d14f129a11542581793506ad8ba24e_Out_0_Vector4, _Property_e84364b4aa05540ebb986ab49893b436_Out_0_Vector4, _Property_7f00ce17ce115d2bb969973b132fe3e8_Out_0_Float, _Property_89e65c225052579ea3ac6095a234f65a_Out_0_Vector4, _Property_4750fb93d1d1562bad31b25dec3f8b25_Out_0_Float, _Property_5e6a32f15bf05438b9cc108706ad0625_Out_0_Float, _Property_866cffb762935a229d1b940e1f8b905c_Out_0_Vector4, _Property_f97ecb43f95c5e37b841c32f67fa546e_Out_0_Float, _Property_713b0ac7fdd85eb2a23076a581e94638_Out_0_Vector4, _Property_2fd1783a3bdc52f88effab68b88e0a47_Out_0_Float, _Property_760680a31d7b57b3a03b703fedd7dea7_Out_0_Vector4, _Property_2e33a988d6ea5eaea31eb783d2cb2026_Out_0_Vector4, _Property_990aa20b0fbe58bab561e7a26ecc62ad_Out_0_Vector4, IN.WorldSpaceNormal, IN.WorldSpaceViewDirection, _Property_e5767a3f300954768906d034fee505b2_Out_0_Vector4, _Property_28013c8ed2865dbba415684e772fbe2f_Out_0_Vector4, _Property_872303b3673f5db6ba39a80132854337_Out_0_Float, _Property_f712ace2c0ec59aca6e4ea56a5dc642b_Out_0_Texture2D, _Property_3f274a4912255dc2aa6e18641c408bd6_Out_0_Float, _Property_2d934283df7757faadf21ff2aa713bb4_Out_0_Vector4, _Property_d2afb0df25d55430acab801c8a3dbfde_Out_0_Float, _Property_0dec3b42d61b5e7c862c4ca295f9e208_Out_0_Vector4, _Property_4b091596ce61549b96d0e88967383f48_Out_0_Vector4, _Property_e246b4e0fa405aca8b55e192c06587a0_Out_0_Vector4, _Property_7a5d7978834d5d4eb9428c0a196dfbd3_Out_0_Vector4, _Property_6ab07b2800785722806fc80f0a180a5e_Out_0_Vector4, _Property_5250e3a06c0252d6a6bb9743ea2f0ac1_Out_0_Vector4, _Property_64999d5c415b5a73b4a0c204bcf29377_Out_0_Vector4, _Property_a934aa24a47f5da2992d3e468dbd374d_Out_0_Vector4, _Property_629063539c105cb3a2e8d3eade7eefb1_Out_0_Vector4, _Property_fc9eb8a77e24500ab5421051111d6fda_Out_0_Float, _Property_6311ff30de835a0da66e70c41e5eb4a9_Out_0_Float, _Property_9439295f5abe51489892e22adc16c2b7_Out_0_Float, _Property_e0d280ecac5f56c48b444399bdfee24d_Out_0_Vector4, _Property_d24b5ab9f5354781acf90544fdd34e1a_Out_0_Float, _Property_d525ffde86414d429eb7d33f95dbc125_Out_0_Vector4, IN.ViewSpacePosition, _Property_2f1d59a7ccfd4e21bdcabf18c881577c_Out_0_Float, _Property_c04e77b9696340d1b77e0c6a9910ffae_Out_0_Vector4, _ScreenPosition_86fbeeb2af2346549e9611c0551ed415_Out_0_Vector4, _Property_67f9358d1a79442eb25e5a4b980feddb_Out_0_Float, _Property_75062f76b29a4f08b37655dc233735d6_Out_0_Vector4, _Property_8fbc75cf62cb493ea789465b0e094396_Out_0_Vector4, _Property_8f072da64b1f45d992849b1e47b91c14_Out_0_Texture2D, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_Out_5_Vector2, _Property_a9a873c1f9545377b6d5603870d8ce7e_Out_0_Texture2D, _Property_05cfd7336ce55efda9ba538458718e94_Out_0_Float, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_NoiseUV_30_Vector2, _Property_de00f2b38f8d559eb2e11b9a8afbf0ab_Out_0_Float, _Property_c1523df23e3f5ea7a2816bc39b189b9d_Out_0_Vector4, _Property_6745a2aa39ce5a6db6ef4145ce16fc4d_Out_0_Float, _Property_0692aaad7ae555198da5d2971b415df9_Out_0_Vector4, _Property_7a1c57aac1ae5322904a7fbc9050f2ce_Out_0_Texture2D, _Property_931f026875de5593b8c7d6f47800f865_Out_0_Float, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_NoiseMaskUV_31_Vector2, _Property_cff65480aa2c5e98b542088b70270554_Out_0_Float, _Property_5f7732a02ece5e6fb25d3e1ecc105818_Out_0_Float, _Property_b04226779a985e25a46ae050e8c15dcc_Out_0_Float, _Property_415818c2850c582fa48e2026af7e309e_Out_0_Float, _Property_53f494020d7651608ba56c8922ff1ab9_Out_0_Texture2D, _Property_6df429d0561957cb84dfad2ee12aeb46_Out_0_Vector4, _Property_5c8f0472975b5156a2b33799226739c6_Out_0_Vector4, _Property_d2d631afc2c1544b83cb34681aa85a03_Out_0_Float, _Property_8a8ba308b2a1563ea8c2419c29c5c7b9_Out_0_Texture2D, _Property_6d9934d6abfd5c0697171ddfd0c97fdc_Out_0_Float, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_BumpUV_32_Vector2, IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, _Property_e5ae484542c256ef9e2c843d58514935_Out_0_Float, _Property_6301978f07ae543781068ab22b637a6c_Out_0_Vector4, _Property_9f86734760a45426a1b7d524232798ab_Out_0_Vector4, IN.WorldSpacePosition, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_ProgramNoiseUV_33_Vector2, _Property_665d05cdb47f5ac3b55f9494eebcfd24_Out_0_Float, _Property_97cf65b2fe9f579a9efe26a73d830bb9_Out_0_Float, _Property_46cf15a5712c5271ab621a4945709213_Out_0_Float, _Property_4d5ce3c87f6b5b5eb6b5488a205f5da2_Out_0_Float, _Property_73408a6e8749509dbf03b29118ea169a_Out_0_Vector4, _Property_4c7f65f090b75d33a899f4860a5eb28c_Out_0_Vector4, _Property_cc5eb5485f3f503e991dd48cda2773d8_Out_0_Vector4, _Property_ea6439cfbd84510292b048fc6619980b_Out_0_Vector4, _Property_ead7fba0b75559f78e76a0fbf67cae12_Out_0_Float, _Property_13f2b053fdb9593aa51c8639f809a891_Out_0_Float, _Property_9767ed7b0d615801a6878b0fc416e6f0_Out_0_Float, _Property_3750f20ccf7b5a7fb1d9e4bd3fb2b6ec_Out_0_Float, _Property_5d371e5c8c7459f280e4945d8a055cdc_Out_0_Float, _Property_c125096410c15c18bbff38461fb105d3_Out_0_Texture2D, _Property_38036157e2ec5c659072d3660c785123_Out_0_Texture2D, _Property_269c61b4d8fe5a7684fc52010ece7432_Out_0_Texture2D, _Property_89139d6b0c285de9a58e1f6df2e7d236_Out_0_Vector4, _Property_547327d8b0f75f868f2ecda76c814b21_Out_0_Vector4, _Property_d2fe351137c85eac9a617d3f05ff92b2_Out_0_Float, IN.SixBake0, IN.SixBake1, IN.SixBake2, IN.SixBack0, IN.SixBack1, IN.SixBack2, IN.SixTangentSigned, _Property_25cf481f9d865f129398a908a10a4f11_Out_0_Float, _NBGraphBaseColorCustomFunction_f712bc264a1c40848edc2dd476a4cada_Out_3_Vector4, _NBGraphBaseColorCustomFunction_f712bc264a1c40848edc2dd476a4cada_NBDistortionSignedRG_145_Vector2, _NBGraphBaseColorCustomFunction_f712bc264a1c40848edc2dd476a4cada_NBDistortionNoiseMask_146_Float);
                    float _Swizzle_52d10f3207ae43dc9038120fea622ca3_Out_1_Float = _NBGraphBaseColorCustomFunction_f712bc264a1c40848edc2dd476a4cada_Out_3_Vector4.w;
                    float _Property_b2c9094aad4345b78e06cfbf0854a2bd_Out_0_Float = _Cutoff;
                    surface.Alpha = _Swizzle_52d10f3207ae43dc9038120fea622ca3_Out_1_Float;
                    surface.AlphaClipThreshold = _Property_b2c9094aad4345b78e06cfbf0854a2bd_Out_0_Float;
                    return surface;
                }
            
            // --------------------------------------------------
            // Build Graph Inputs
            #ifdef HAVE_VFX_MODIFICATION
            #define VFX_SRP_ATTRIBUTES Attributes
            #define VFX_SRP_VARYINGS Varyings
            #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
            #endif
            VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
                {
                    VertexDescriptionInputs output;
                    ZERO_INITIALIZE(VertexDescriptionInputs, output);
                
                    output.ObjectSpaceNormal =                          input.normalOS;
                    output.WorldSpaceNormal =                           TransformObjectToWorldNormal(input.normalOS);
                    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
                    output.WorldSpaceTangent =                          TransformObjectToWorldDir(input.tangentOS.xyz);
                    output.ObjectSpaceBiTangent =                       normalize(cross(input.normalOS, input.tangentOS.xyz) * (input.tangentOS.w > 0.0f ? 1.0f : -1.0f) * GetOddNegativeScale());
                    output.WorldSpaceBiTangent =                        TransformObjectToWorldDir(output.ObjectSpaceBiTangent);
                    output.ObjectSpacePosition =                        input.positionOS;
                    output.uv0 =                                        input.uv0;
                    output.uv1 =                                        input.uv1;
                    output.uv2 =                                        input.uv2;
                    output.VertexColor =                                input.color;
                #if UNITY_ANY_INSTANCING_ENABLED
                #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
                #endif
                
                    return output;
                }
                SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
                {
                    SurfaceDescriptionInputs output;
                    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
                
                #ifdef HAVE_VFX_MODIFICATION
                #if VFX_USE_GRAPH_VALUES
                    uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
                    /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
                #endif
                    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
                
                #endif
                
                    output.SixBake0 = input.SixBake0;
                output.SixBake1 = input.SixBake1;
                output.SixBake2 = input.SixBake2;
                output.SixBack0 = input.SixBack0;
                output.SixBack1 = input.SixBack1;
                output.SixBack2 = input.SixBack2;
                output.SixTangentSigned = input.SixTangentSigned;
                
                    // must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
                    float3 unnormalizedNormalWS = input.normalWS;
                    const float renormFactor = 1.0 / length(unnormalizedNormalWS);
                
                    // use bitangent on the fly like in hdrp
                    // IMPORTANT! If we ever support Flip on double sided materials ensure bitangent and tangent are NOT flipped.
                    float crossSign = (input.tangentWS.w > 0.0 ? 1.0 : -1.0)* GetOddNegativeScale();
                    float3 bitang = crossSign * cross(input.normalWS.xyz, input.tangentWS.xyz);
                
                    output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;      // we want a unit length Normal Vector node in shader graph
                
                    // to pr               eserve mikktspace compliance we use same scale renormFactor as was used on the normal.
                    // This                is explained in section 2.2 in "surface gradient based bump mapping framework"
                    output.WorldSpaceTangent = renormFactor * input.tangentWS.xyz;
                    output.WorldSpaceBiTangent = renormFactor * bitang;
                
                    output.WorldSpaceViewDirection = GetWorldSpaceNormalizeViewDir(input.positionWS);
                    output.WorldSpacePosition = input.positionWS;
                    output.ViewSpacePosition = TransformWorldToView(input.positionWS);
                
                    #if UNITY_UV_STARTS_AT_TOP
                    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
                    #else
                    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
                    #endif
                
                    output.NDCPosition = output.PixelPosition.xy / _ScaledScreenParams.xy;
                    output.NDCPosition.y = 1.0f - output.NDCPosition.y;
                
                    output.uv0 = input.texCoord0;
                    output.uv1 = input.texCoord1;
                    output.uv2 = input.texCoord2;
                    output.VertexColor = input.color;
                #if UNITY_ANY_INSTANCING_ENABLED
                #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
                #endif
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
                #else
                #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                #endif
                    BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                
                        return output;
                }
                
            // --------------------------------------------------
            // Main
            
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DepthOnlyPass.hlsl"
            
            // --------------------------------------------------
            // Visual Effect Vertex Invocations
            #ifdef HAVE_VFX_MODIFICATION
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
            #endif
            
            ENDHLSL
            }
            Pass
            {
                Name "DepthNormalsOnly"
                Tags
                {
                    "LightMode" = "DepthNormalsOnly"
                }
            
            // Render State
            Cull [_Cull]
                ZTest LEqual
                ZWrite On
                Stencil
                {
                ReadMask [_StencilReadMask]
                WriteMask [_StencilWriteMask]
                Ref [_Stencil]
                CompFront [_StencilComp]
                ZFailFront [_StencilZFail]
                FailFront [_StencilFail]
                PassFront [_StencilOp]
                CompBack [_StencilComp]
                ZFailBack [_StencilZFail]
                FailBack [_StencilFail]
                PassBack [_StencilOp]
                }
            
            // Debug
            // <None>
            
            // --------------------------------------------------
            // Pass
            
            HLSLPROGRAM
            
            // Pragmas
            #pragma target 2.0
                #pragma multi_compile_instancing
                #pragma vertex vert
                #pragma fragment frag
            
            // Keywords
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
                #pragma shader_feature_fragment _ _SURFACE_TYPE_TRANSPARENT
                #pragma shader_feature_local_fragment _ _ALPHAPREMULTIPLY_ON
                #pragma shader_feature_local_fragment _ _ALPHAMODULATE_ON
                #pragma shader_feature_local_fragment _ _ALPHATEST_ON
            // GraphKeywords: <None>
            
            // Defines
            
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define ATTRIBUTES_NEED_TEXCOORD1
            #define ATTRIBUTES_NEED_TEXCOORD2
            #define ATTRIBUTES_NEED_COLOR
            #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
            #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
            #define VARYINGS_NEED_POSITION_WS
            #define VARYINGS_NEED_NORMAL_WS
            #define VARYINGS_NEED_TANGENT_WS
            #define VARYINGS_NEED_TEXCOORD0
            #define VARYINGS_NEED_TEXCOORD1
            #define VARYINGS_NEED_TEXCOORD2
            #define VARYINGS_NEED_COLOR
            #define VARYINGS_NEED_CULLFACE
            #define FEATURES_GRAPH_VERTEX
            /* WARNING: $splice Could not find named fragment 'PassInstancing' */
            #define SHADERPASS SHADERPASS_DEPTHNORMALSONLY
                #define SHADERGRAPH_PREVIEW_MAIN
            
            
            // custom interpolator pre-include
            /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
            
            // Includes
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
            
            // --------------------------------------------------
            // Structs and Packing
            
            // custom interpolators pre packing
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
            
            struct Attributes
                {
                     float3 positionOS : POSITION;
                     float3 normalOS : NORMAL;
                     float4 tangentOS : TANGENT;
                     float4 uv0 : TEXCOORD0;
                     float4 uv1 : TEXCOORD1;
                     float4 uv2 : TEXCOORD2;
                     float4 color : COLOR;
                    #if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
                     uint instanceID : INSTANCEID_SEMANTIC;
                    #endif
                };
                struct Varyings
                {
                     float4 positionCS : SV_POSITION;
                     float3 positionWS;
                     float3 normalWS;
                     float4 tangentWS;
                     float4 texCoord0;
                     float4 texCoord1;
                     float4 texCoord2;
                     float4 color;
                    #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
                     uint instanceID : CUSTOM_INSTANCE_ID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                    #endif
                     float3 SixBake0;
                     float3 SixBake1;
                     float3 SixBake2;
                     float3 SixBack0;
                     float3 SixBack1;
                     float3 SixBack2;
                     float4 SixTangentSigned;
                };
                struct SurfaceDescriptionInputs
                {
                     float3 WorldSpaceNormal;
                     float3 WorldSpaceTangent;
                     float3 WorldSpaceBiTangent;
                     float3 WorldSpaceViewDirection;
                     float3 ViewSpacePosition;
                     float3 WorldSpacePosition;
                     float2 NDCPosition;
                     float2 PixelPosition;
                     float4 uv0;
                     float4 uv1;
                     float4 uv2;
                     float4 VertexColor;
                     float FaceSign;
                     float3 SixBake0;
                     float3 SixBake1;
                     float3 SixBake2;
                     float3 SixBack0;
                     float3 SixBack1;
                     float3 SixBack2;
                     float4 SixTangentSigned;
                };
                struct VertexDescriptionInputs
                {
                     float3 ObjectSpaceNormal;
                     float3 WorldSpaceNormal;
                     float3 ObjectSpaceTangent;
                     float3 WorldSpaceTangent;
                     float3 ObjectSpaceBiTangent;
                     float3 WorldSpaceBiTangent;
                     float3 ObjectSpacePosition;
                     float4 uv0;
                     float4 uv1;
                     float4 uv2;
                     float4 VertexColor;
                };
                struct PackedVaryings
                {
                     float4 positionCS : SV_POSITION;
                     float4 tangentWS : INTERP0;
                     float4 texCoord0 : INTERP1;
                     float4 texCoord1 : INTERP2;
                     float4 texCoord2 : INTERP3;
                     float4 color : INTERP4;
                     float4 SixTangentSigned : INTERP5;
                     float4 packed_positionWS_SixBack1x : INTERP6;
                     float4 packed_normalWS_SixBack1y : INTERP7;
                     float4 packed_SixBake0_SixBack1z : INTERP8;
                     float4 packed_SixBake1_SixBack2x : INTERP9;
                     float4 packed_SixBake2_SixBack2y : INTERP10;
                     float4 packed_SixBack0_SixBack2z : INTERP11;
                    #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
                     uint instanceID : CUSTOM_INSTANCE_ID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                    #endif
                };
            
            PackedVaryings PackVaryings (Varyings input)
                {
                    PackedVaryings output;
                    ZERO_INITIALIZE(PackedVaryings, output);
                    output.positionCS = input.positionCS;
                    output.tangentWS.xyzw = input.tangentWS;
                    output.texCoord0.xyzw = input.texCoord0;
                    output.texCoord1.xyzw = input.texCoord1;
                    output.texCoord2.xyzw = input.texCoord2;
                    output.color.xyzw = input.color;
                    output.SixTangentSigned.xyzw = input.SixTangentSigned;
                    output.packed_positionWS_SixBack1x.xyz = input.positionWS;
                    output.packed_positionWS_SixBack1x.w = input.SixBack1.x;
                    output.packed_normalWS_SixBack1y.xyz = input.normalWS;
                    output.packed_normalWS_SixBack1y.w = input.SixBack1.y;
                    output.packed_SixBake0_SixBack1z.xyz = input.SixBake0;
                    output.packed_SixBake0_SixBack1z.w = input.SixBack1.z;
                    output.packed_SixBake1_SixBack2x.xyz = input.SixBake1;
                    output.packed_SixBake1_SixBack2x.w = input.SixBack2.x;
                    output.packed_SixBake2_SixBack2y.xyz = input.SixBake2;
                    output.packed_SixBake2_SixBack2y.w = input.SixBack2.y;
                    output.packed_SixBack0_SixBack2z.xyz = input.SixBack0;
                    output.packed_SixBack0_SixBack2z.w = input.SixBack2.z;
                    #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
                    output.instanceID = input.instanceID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
                    #endif
                    return output;
                }
                
                Varyings UnpackVaryings (PackedVaryings input)
                {
                    Varyings output;
                    output.positionCS = input.positionCS;
                    output.tangentWS = input.tangentWS.xyzw;
                    output.texCoord0 = input.texCoord0.xyzw;
                    output.texCoord1 = input.texCoord1.xyzw;
                    output.texCoord2 = input.texCoord2.xyzw;
                    output.color = input.color.xyzw;
                    output.SixTangentSigned = input.SixTangentSigned.xyzw;
                    output.positionWS = input.packed_positionWS_SixBack1x.xyz;
                    output.SixBack1.x = input.packed_positionWS_SixBack1x.w;
                    output.normalWS = input.packed_normalWS_SixBack1y.xyz;
                    output.SixBack1.y = input.packed_normalWS_SixBack1y.w;
                    output.SixBake0 = input.packed_SixBake0_SixBack1z.xyz;
                    output.SixBack1.z = input.packed_SixBake0_SixBack1z.w;
                    output.SixBake1 = input.packed_SixBake1_SixBack2x.xyz;
                    output.SixBack2.x = input.packed_SixBake1_SixBack2x.w;
                    output.SixBake2 = input.packed_SixBake2_SixBack2y.xyz;
                    output.SixBack2.y = input.packed_SixBake2_SixBack2y.w;
                    output.SixBack0 = input.packed_SixBack0_SixBack2z.xyz;
                    output.SixBack2.z = input.packed_SixBack0_SixBack2z.w;
                    #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
                    output.instanceID = input.instanceID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
                    #endif
                    return output;
                }
                
            
            // --------------------------------------------------
            // Graph
            
            // Graph Properties
            CBUFFER_START(UnityPerMaterial)
                float4 _NBGraphBaseColorCustomFunction_f712bc264a1c40848edc2dd476a4cada_SampledAlbedo_0_Vector4;
                float _NBGraphBaseColorCustomFunction_f712bc264a1c40848edc2dd476a4cada_SelectedAlpha_1_Float;
                TEXTURE2D(_BaseMap);
                SAMPLER(sampler_BaseMap);
                float4 _BaseMap_TexelSize;
                float4 _Color;
                float2 _NB_DistortionNoise;
                float _NB_DistortionIntensity;
                float _NB_DistortionMode;
                float _NB_Flags0Lo16;
                float _NB_Flags0Hi16;
                float _NB_Flags1Lo16;
                float _NB_Flags1Hi16;
                float _NB_DistortionAlphaPow;
                float _NB_DistortionAlphaMultiplier;
                float _NB_DistortionAlphaAdd;
                TEXTURE2D(_MaskMap);
                SAMPLER(sampler_MaskMap);
                float4 _MaskMap_TexelSize;
                float4 _MaskMap_ST;
                float _Mask_Toggle;
                float4 _MaskMapVec;
                float4 _MaskRefineVec;
                float _NB_ColorChannelLo16;
                TEXTURE2D(_DissolveMap);
                SAMPLER(sampler_DissolveMap);
                float4 _DissolveMap_TexelSize;
                float4 _DissolveMap_ST;
                float _Dissolve_Toggle;
                float4 _Dissolve;
                float4 _BaseMap_ST;
                float _BaseMapUVRotation;
                float _BaseMapUVRotationSpeed;
                float4 _BaseMapMaskMapOffset;
                float _MaskMapUVRotation;
                float _MaskMapRotationSpeed;
                float4 _MaskMapOffsetAnition;
                float4 _DissolveOffsetRotateDistort;
                TEXTURE2D(_DissolveMaskMap);
                SAMPLER(sampler_DissolveMaskMap);
                float4 _DissolveMaskMap_TexelSize;
                float4 _DissolveMaskMap_ST;
                float _DissolveMask_Toggle;
                float _DissolveMaskMode;
                TEXTURE2D(_MaskMap2);
                SAMPLER(sampler_MaskMap2);
                float4 _MaskMap2_TexelSize;
                float4 _MaskMap2_ST;
                float _Mask2_Toggle;
                TEXTURE2D(_MaskMap3);
                SAMPLER(sampler_MaskMap3);
                float4 _MaskMap3_TexelSize;
                float4 _MaskMap3_ST;
                float _Mask3_Toggle;
                float4 _MaskMap3OffsetAnition;
                float _NB_WrapFlagsLo16;
                float _NB_WrapFlagsHi16;
                float _MaskMapGradientCount;
                float4 _MaskMapGradientFloat0;
                float4 _MaskMapGradientFloat1;
                float4 _MaskMapGradientFloat2;
                float _MaskMap2GradientCount;
                float4 _MaskMap2GradientFloat0;
                float4 _MaskMap2GradientFloat1;
                float4 _MaskMap2GradientFloat2;
                float _MaskMap3GradientCount;
                float4 _MaskMap3GradientFloat0;
                float4 _MaskMap3GradientFloat1;
                float4 _MaskMap3GradientFloat2;
                float _AlphaAll;
                float4 _ColorA;
                float _BaseColorIntensityForTimeline;
                float4 _BaseBackColor;
                TEXTURE2D(_EmissionMap);
                SAMPLER(sampler_EmissionMap);
                float4 _EmissionMap_TexelSize;
                float4 _EmissionMap_ST;
                float _EmissionEnabled;
                float4 _EmissionMapUVOffset;
                float _EmissionMapUVRotation;
                float4 _EmissionMapColor;
                float _EmissionMapColorIntensity;
                float _EmissionAlphaIntensity;
                TEXTURE2D(_ColorBlendMap);
                SAMPLER(sampler_ColorBlendMap);
                float4 _ColorBlendMap_TexelSize;
                float4 _ColorBlendMap_ST;
                float _ColorBlendMap_Toggle;
                float4 _ColorBlendMapOffset;
                float4 _ColorBlendVec;
                float4 _ColorBlendColor;
                float _ColorBlendColorIntensity;
                TEXTURE2D(_RampColorMap);
                SAMPLER(sampler_RampColorMap);
                float4 _RampColorMap_TexelSize;
                float4 _RampColorMap_ST;
                float _RampColorToggle;
                float _RampColorSourceMode;
                float4 _RampColorMapOffset;
                float4 _RampColor0;
                float4 _RampColor1;
                float4 _RampColor2;
                float4 _RampColor3;
                float4 _RampColor4;
                float4 _RampColor5;
                float4 _RampColorAlpha0;
                float4 _RampColorAlpha1;
                float4 _RampColorAlpha2;
                float _RampColorCount;
                float4 _RampColorBlendColor;
                float _HueShift;
                float _Contrast;
                float4 _ContrastMidColor;
                float _Saturability;
                float4 _BaseMapColorRefine;
                float _fresnelEnabled;
                float4 _FresnelUnit;
                float4 _FresnelColor;
                float4 _FresnelRotation;
                float4 _Dissolve_Vec2;
                float4 _DissolveLineColor;
                float _Dissolve_useRampMap_Toggle;
                TEXTURE2D(_DissolveRampMap);
                SAMPLER(sampler_DissolveRampMap);
                float4 _DissolveRampMap_TexelSize;
                float4 _DissolveRampMap_ST;
                float _DissolveRampSourceMode;
                float4 _DissolveRampColor;
                float _DissolveRampCount;
                float4 _DissolveRampColor0;
                float4 _DissolveRampColor1;
                float4 _DissolveRampColor2;
                float4 _DissolveRampColor3;
                float4 _DissolveRampColor4;
                float4 _DissolveRampColor5;
                float4 _DissolveRampAlpha0;
                float4 _DissolveRampAlpha1;
                float4 _DissolveRampAlpha2;
                float _NB_ForceNoMipFlagsLo16;
                float _NB_ForceNoMipFlagsHi16;
                float _NB_DissolveRampSTOverrideEnabled;
                float4 _NB_DissolveRampSTOverride;
                float _DistanceFade_Toggle;
                float4 _Fade;
                float _SoftParticlesEnabled;
                float4 _SoftParticleFadeParams;
                float _DepthOutline_Toggle;
                float4 _DepthOutline_Color;
                float4 _DepthOutline_Vec;
                float _NB_UVModeFlag0Lo16;
                float _NB_UVModeFlag0Hi16;
                float _NB_UVModeFlagType0Lo16;
                float _NB_UVModeFlagType0Hi16;
                float4 _SharedUV_ST;
                float4 _SharedUV_Vec;
                float4 _TWParameter;
                float _TWStrength;
                float4 _PCCenter;
                TEXTURE2D(_NoiseMap);
                SAMPLER(sampler_NoiseMap);
                float4 _NoiseMap_TexelSize;
                float4 _NoiseMap_ST;
                float _noisemapEnabled;
                float _NoiseMapUVRotation;
                float4 _NoiseOffset;
                float _NoiseIntensity;
                float4 _DistortionDirection;
                TEXTURE2D(_NoiseMaskMap);
                SAMPLER(sampler_NoiseMaskMap);
                float4 _NoiseMaskMap_TexelSize;
                float4 _NoiseMaskMap_ST;
                float _noiseMaskMap_Toggle;
                float _TexDistortion_intensity;
                float _Emi_Distortion_intensity;
                float _MaskDistortion_intensity;
                float _Cutoff;
                float _AdditiveToPreMultiplyAlphaLerp;
                TEXTURE2D(_VertexOffset_Map);
                SAMPLER(sampler_VertexOffset_Map);
                float4 _VertexOffset_Map_TexelSize;
                float4 _VertexOffset_Map_ST;
                float4 _VertexOffset_Vec;
                float4 _VertexOffset_CustomDir;
                float _VertexOffset_NormalDir_Toggle;
                float _VertexOffset_DirectionSpace;
                float _VertexOffset_Toggle;
                TEXTURE2D(_VertexOffset_MaskMap);
                SAMPLER(sampler_VertexOffset_MaskMap);
                float4 _VertexOffset_MaskMap_TexelSize;
                float4 _VertexOffset_MaskMap_ST;
                float4 _VertexOffset_MaskMap_Vec;
                float _VertexOffset_Mask_Toggle;
                float _NB_ColorChannelHi16;
                float _MatCapToggle;
                TEXTURE2D(_MatCapTex);
                SAMPLER(sampler_MatCapTex);
                float4 _MatCapColor;
                float4 _MatCapInfo;
                float _BumpMapToggle;
                TEXTURE2D(_BumpTex);
                SAMPLER(sampler_BumpTex);
                float4 _BumpTex_ST;
                float _BumpScale;
                float _FxLightMode;
                float4 _MaterialInfo;
                float4 _SpecularColor;
                float _ProgramNoise_Toggle;
                float _ProgramNoise_Simple_Toggle;
                float _ProgramNoise_Voronoi_Toggle;
                float _ProgramNoise_Rotate;
                float4 _DissolveVoronoi_Vec;
                float4 _DissolveVoronoi_Vec2;
                float4 _DissolveVoronoi_Vec3;
                float4 _DissolveVoronoi_Vec4;
                float _ProgramNoiseBaseBlendOpacity;
                float _MaskPNoiseBlendOpacity;
                float _DissolvePNoiseBlendOpacity;
                float _NB_PNoiseBlendLo16;
                float _NB_PNoiseBlendHi16;
                TEXTURE2D(_RigRTBk);
                SAMPLER(sampler_RigRTBk);
                TEXTURE2D(_RigLBtF);
                SAMPLER(sampler_RigLBtF);
                TEXTURE2D(_SixWayEmissionRamp);
                SAMPLER(sampler_SixWayEmissionRamp);
                float4 _SixWayInfo;
                float4 _SixWayEmissionColor;
                float _SixWayColorAbsorptionToggle;
                float _DistortPNoiseBlendOpacity;
                UNITY_TEXTURE_STREAMING_DEBUG_VARS;
                CBUFFER_END
                #define UNITY_ACCESS_HYBRID_INSTANCED_PROP(var, type) var
            
            // Graph Includes
            #include_with_pragmas "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBGraphBaseColor.hlsl"
            #include_with_pragmas "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBGraphVertexOffset.hlsl"
            #include_with_pragmas "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBGraphBaseUV.hlsl"
            
            // -- Property used by ScenePickingPass
            #ifdef SCENEPICKINGPASS
            float4 _SelectionID;
            #endif
            
            // -- Properties used by SceneSelectionPass
            #ifdef SCENESELECTIONPASS
            int _ObjectId;
            int _PassValue;
            #endif
            
            // Graph Functions
            // GraphFunctions: <None>
            
            // Custom interpolators pre vertex
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
            
            // Graph Vertex
            struct VertexDescription
                {
                    float3 Position;
                    float3 Normal;
                    float3 Tangent;
                    float3 SixBake0;
                    float3 SixBake1;
                    float3 SixBake2;
                    float3 SixBack0;
                    float3 SixBack1;
                    float3 SixBack2;
                    float4 SixTangentSigned;
                };
                
                VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
                {
                    VertexDescription description = (VertexDescription)0;
                    float3 _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Bake0_3_Vector3;
                    float3 _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Bake1_4_Vector3;
                    float3 _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Bake2_5_Vector3;
                    float3 _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Back0_6_Vector3;
                    float3 _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Back1_7_Vector3;
                    float3 _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Back2_8_Vector3;
                    float4 _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_TangentSigned_9_Vector4;
                    NBGraphSixWayBake_float(IN.WorldSpaceNormal, IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Bake0_3_Vector3, _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Bake1_4_Vector3, _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Bake2_5_Vector3, _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Back0_6_Vector3, _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Back1_7_Vector3, _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Back2_8_Vector3, _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_TangentSigned_9_Vector4);
                    float4 _UV_596a5b2640e15e43aff23ea3c9c87059_Out_0_Vector4 = IN.uv0;
                    float4 _UV_7fa8f9025e9059649e9bdc291afd4417_Out_0_Vector4 = IN.uv1;
                    float4 _UV_8576cf3978f855598e9e2c2e37cdc003_Out_0_Vector4 = IN.uv2;
                    UnityTexture2D _Property_6f7cd0164d485a30803eb055841cd1ce_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_VertexOffset_Map, sampler_VertexOffset_Map, _VertexOffset_Map_TexelSize, _VertexOffset_Map_ST, float4(0, 0, 0, 0));
                    float4 _Property_fd85324fa4f658d691990180b8f8793e_Out_0_Vector4 = _VertexOffset_Vec;
                    float4 _Property_ccc6a218db89506f996c9a8c85bf89d4_Out_0_Vector4 = _VertexOffset_CustomDir;
                    float _Property_8cf30747736a58bcb43ec6620008dd56_Out_0_Float = _VertexOffset_NormalDir_Toggle;
                    float _Property_d1162da7b0a7551b80f631805d2a7134_Out_0_Float = _VertexOffset_DirectionSpace;
                    float _Property_18ffb5b24a215c74989dff20b668c91b_Out_0_Float = _VertexOffset_Toggle;
                    UnityTexture2D _Property_33de3bd0609655b7b71629b91a283fa1_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_VertexOffset_MaskMap, sampler_VertexOffset_MaskMap, _VertexOffset_MaskMap_TexelSize, _VertexOffset_MaskMap_ST, float4(0, 0, 0, 0));
                    float4 _Property_c972b51415e4592b9f07b2c052809e33_Out_0_Vector4 = _VertexOffset_MaskMap_Vec;
                    float _Property_01f9ee3ac21a544aa8c1fa82346bd03e_Out_0_Float = _VertexOffset_Mask_Toggle;
                    float _Property_849d65e3adfb5e4f996dd29864f2c86f_Out_0_Float = _NB_Flags0Lo16;
                    float _Property_3198e5a482ff5fb79bfc8ef49e694023_Out_0_Float = _NB_Flags0Hi16;
                    float _Property_c9800dd9977850049d324b45be73cc33_Out_0_Float = _NB_Flags1Lo16;
                    float _Property_dea9b3365dc35d0daf45f162fe71ae92_Out_0_Float = _NB_Flags1Hi16;
                    float _Property_a457aad3783c559d96ddfe0cf5d647ea_Out_0_Float = _NB_WrapFlagsLo16;
                    float _Property_14f98c3cf7b7525d8340b90ff8e5fd35_Out_0_Float = _NB_WrapFlagsHi16;
                    float _Property_560a4b92f5295597bb1175a0f82a5c76_Out_0_Float = _NB_ColorChannelLo16;
                    float _Property_770d2aaf793952348bc222b930283f2b_Out_0_Float = _NB_ColorChannelHi16;
                    float _Property_efa6ef74db395e619e03f11b575a1029_Out_0_Float = _NB_UVModeFlag0Lo16;
                    float _Property_ef246b0486a15ce4a55423fe806ebb20_Out_0_Float = _NB_UVModeFlag0Hi16;
                    float _Property_214c5c87c6c754658d4b9d2883ae02c5_Out_0_Float = _NB_UVModeFlagType0Lo16;
                    float _Property_b9de1de3181c5b9b9fcdb4836a81b926_Out_0_Float = _NB_UVModeFlagType0Hi16;
                    float4 _Property_9e2af066867e551bb1eed3199acfe809_Out_0_Vector4 = _SharedUV_ST;
                    float4 _Property_614e89fe251b5185af927c4ee7175f5f_Out_0_Vector4 = _SharedUV_Vec;
                    float4 _Property_b2c755308b2759418ed105ab21db3493_Out_0_Vector4 = _TWParameter;
                    float _Property_132da21d369f53e9b55031e5415fe894_Out_0_Float = _TWStrength;
                    float4 _Property_3561d005d5bb5f4ab0d3e545e06a5780_Out_0_Vector4 = _PCCenter;
                    float3 _NBGraphVertexOffsetCustomFunction_395601997b2e51c795e7c1ec707a5553_OutPositionOS_33_Vector3;
                    float3 _NBGraphVertexOffsetCustomFunction_395601997b2e51c795e7c1ec707a5553_OutNormalOS_34_Vector3;
                    float3 _NBGraphVertexOffsetCustomFunction_395601997b2e51c795e7c1ec707a5553_OutTangentOS_35_Vector3;
                    float _NBGraphVertexOffsetCustomFunction_395601997b2e51c795e7c1ec707a5553_Supported_36_Float;
                    NBGraphVertexOffset_float(IN.ObjectSpacePosition, IN.ObjectSpaceNormal, IN.ObjectSpaceTangent, (IN.VertexColor.xyz), _UV_596a5b2640e15e43aff23ea3c9c87059_Out_0_Vector4, _UV_7fa8f9025e9059649e9bdc291afd4417_Out_0_Vector4, _UV_8576cf3978f855598e9e2c2e37cdc003_Out_0_Vector4, _Property_6f7cd0164d485a30803eb055841cd1ce_Out_0_Texture2D, _Property_fd85324fa4f658d691990180b8f8793e_Out_0_Vector4, (_Property_ccc6a218db89506f996c9a8c85bf89d4_Out_0_Vector4.xyz), _Property_8cf30747736a58bcb43ec6620008dd56_Out_0_Float, _Property_d1162da7b0a7551b80f631805d2a7134_Out_0_Float, _Property_18ffb5b24a215c74989dff20b668c91b_Out_0_Float, _Property_33de3bd0609655b7b71629b91a283fa1_Out_0_Texture2D, (_Property_c972b51415e4592b9f07b2c052809e33_Out_0_Vector4.xyz), _Property_01f9ee3ac21a544aa8c1fa82346bd03e_Out_0_Float, _Property_849d65e3adfb5e4f996dd29864f2c86f_Out_0_Float, _Property_3198e5a482ff5fb79bfc8ef49e694023_Out_0_Float, _Property_c9800dd9977850049d324b45be73cc33_Out_0_Float, _Property_dea9b3365dc35d0daf45f162fe71ae92_Out_0_Float, _Property_a457aad3783c559d96ddfe0cf5d647ea_Out_0_Float, _Property_14f98c3cf7b7525d8340b90ff8e5fd35_Out_0_Float, _Property_560a4b92f5295597bb1175a0f82a5c76_Out_0_Float, _Property_770d2aaf793952348bc222b930283f2b_Out_0_Float, _Property_efa6ef74db395e619e03f11b575a1029_Out_0_Float, _Property_ef246b0486a15ce4a55423fe806ebb20_Out_0_Float, _Property_214c5c87c6c754658d4b9d2883ae02c5_Out_0_Float, _Property_b9de1de3181c5b9b9fcdb4836a81b926_Out_0_Float, _Property_9e2af066867e551bb1eed3199acfe809_Out_0_Vector4, _Property_614e89fe251b5185af927c4ee7175f5f_Out_0_Vector4, _Property_b2c755308b2759418ed105ab21db3493_Out_0_Vector4, _Property_132da21d369f53e9b55031e5415fe894_Out_0_Float, _Property_3561d005d5bb5f4ab0d3e545e06a5780_Out_0_Vector4, _NBGraphVertexOffsetCustomFunction_395601997b2e51c795e7c1ec707a5553_OutPositionOS_33_Vector3, _NBGraphVertexOffsetCustomFunction_395601997b2e51c795e7c1ec707a5553_OutNormalOS_34_Vector3, _NBGraphVertexOffsetCustomFunction_395601997b2e51c795e7c1ec707a5553_OutTangentOS_35_Vector3, _NBGraphVertexOffsetCustomFunction_395601997b2e51c795e7c1ec707a5553_Supported_36_Float);
                    description.Position = _NBGraphVertexOffsetCustomFunction_395601997b2e51c795e7c1ec707a5553_OutPositionOS_33_Vector3;
                    description.Normal = _NBGraphVertexOffsetCustomFunction_395601997b2e51c795e7c1ec707a5553_OutNormalOS_34_Vector3;
                    description.Tangent = _NBGraphVertexOffsetCustomFunction_395601997b2e51c795e7c1ec707a5553_OutTangentOS_35_Vector3;
                    description.SixBake0 = _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Bake0_3_Vector3;
                    description.SixBake1 = _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Bake1_4_Vector3;
                    description.SixBake2 = _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Bake2_5_Vector3;
                    description.SixBack0 = _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Back0_6_Vector3;
                    description.SixBack1 = _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Back1_7_Vector3;
                    description.SixBack2 = _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Back2_8_Vector3;
                    description.SixTangentSigned = _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_TangentSigned_9_Vector4;
                    return description;
                }
            
            // Custom interpolators, pre surface
            #ifdef FEATURES_GRAPH_VERTEX
            Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
            {
            output.SixBake0 = input.SixBake0;
            output.SixBake1 = input.SixBake1;
            output.SixBake2 = input.SixBake2;
            output.SixBack0 = input.SixBack0;
            output.SixBack1 = input.SixBack1;
            output.SixBack2 = input.SixBack2;
            output.SixTangentSigned = input.SixTangentSigned;
            return output;
            }
            #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
            #endif
            
            // Graph Pixel
            struct SurfaceDescription
                {
                    float Alpha;
                    float AlphaClipThreshold;
                };
                
                SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
                {
                    SurfaceDescription surface = (SurfaceDescription)0;
                    float4 _Property_13840ad57da543e2b33275638ce1189e_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_Color) : _Color;
                    float _Property_eafd46b75a1746f093439de914a3717c_Out_0_Float = _NB_Flags0Lo16;
                    float _Property_02448b2e80a0440b804602e588eb2727_Out_0_Float = _NB_Flags0Hi16;
                    float _Property_52b95ce95e564cb083a7953179be1554_Out_0_Float = _NB_Flags1Lo16;
                    float _Property_6abc2ac395a54d7f99af7850796b2c20_Out_0_Float = _NB_Flags1Hi16;
                    float2 _Property_4fbc64ac55184ee8b2348b861c93db8b_Out_0_Vector2 = _NB_DistortionNoise;
                    float _Property_9099f47df6d24486a9e0bb9dd8fa7cf8_Out_0_Float = _NB_DistortionIntensity;
                    float _Property_01827f8348b040028326054a1623cc84_Out_0_Float = _NB_DistortionMode;
                    float _Property_87436152ca164e73877206654813b186_Out_0_Float = _NB_DistortionAlphaPow;
                    float _Property_9aab3b8bff1a4b39a348ae3ea124a0e3_Out_0_Float = _NB_DistortionAlphaMultiplier;
                    float _Property_a31f37f37774434e8b5383ecef8f5072_Out_0_Float = _NB_DistortionAlphaAdd;
                    UnityTexture2D _Property_e1e8e8a73a5f5e1c9e969abcb9782cd2_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_MaskMap, sampler_MaskMap, _MaskMap_TexelSize, _MaskMap_ST, float4(0, 0, 0, 0));
                    float _Property_26acbca87fca52f4a5ea05d6fcf09e48_Out_0_Float = _Mask_Toggle;
                    float4 _Property_c7f5afaa32df59eb9c6b2561b57aa26f_Out_0_Vector4 = _MaskMapVec;
                    float4 _Property_6e138238299250348f63a5ee2f1f3d5b_Out_0_Vector4 = _MaskRefineVec;
                    float _Property_20f932f599b85641aeb7c2ea908ed484_Out_0_Float = _NB_ColorChannelLo16;
                    UnityTexture2D _Property_b4f86e887f26523f845f932a27414b5b_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_DissolveMap, sampler_DissolveMap, _DissolveMap_TexelSize, _DissolveMap_ST, float4(0, 0, 0, 0));
                    float _Property_a70b9f8477c75d2a98fd83c15f287a37_Out_0_Float = _Dissolve_Toggle;
                    float4 _Property_a05025df3a5c534f88355f30cb99d2d3_Out_0_Vector4 = _Dissolve;
                    float4 _UV_dde6e6f3492e4fa689d7f4a88f3b492b_Out_0_Vector4 = IN.uv0;
                    float4 _Property_7a2d908f5a2f4990ae14fd870e5a22c9_Out_0_Vector4 = _BaseMap_ST;
                    float _Property_45764d3c363e48539c14ce1d9b8eb3bb_Out_0_Float = _BaseMapUVRotation;
                    float _Property_4c0c6ce883a14260b681ac41d0f78472_Out_0_Float = _BaseMapUVRotationSpeed;
                    float4 _Property_ea0189e7a35b4e44a9e5775b9d1ec406_Out_0_Vector4 = _BaseMapMaskMapOffset;
                    float4 _UV_a8924bd05fcf4a28878b71b6b3b05cd7_Out_0_Vector4 = IN.uv1;
                    float4 _UV_ce3ab892df664c348236f6bce976c116_Out_0_Vector4 = IN.uv2;
                    float _Property_3ca4059fa35141669dc7e92d919136a2_Out_0_Float = _NB_UVModeFlag0Lo16;
                    float _Property_320511e8014a49c4a7e44e21e9c57f02_Out_0_Float = _NB_UVModeFlag0Hi16;
                    float _Property_a91f4458b51f4b488287925809b8a798_Out_0_Float = _NB_UVModeFlagType0Lo16;
                    float _Property_06eb05a4a820436ab686475e2d043b4d_Out_0_Float = _NB_UVModeFlagType0Hi16;
                    float4 _Property_03cf5077fa564f27841d641d9c62f6a3_Out_0_Vector4 = _SharedUV_ST;
                    float4 _Property_338f6b6c652440289b94c105205125f7_Out_0_Vector4 = _SharedUV_Vec;
                    float4 _Property_df09eceb3f6847288ccb0e2f2b6beb4c_Out_0_Vector4 = _TWParameter;
                    float _Property_4cd2fdd2348a4b7795e93a4e7e997131_Out_0_Float = _TWStrength;
                    float4 _Property_65a8673d6744483e8aaf1ea1d5d4d4ca_Out_0_Vector4 = _PCCenter;
                    float2 _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_Out_5_Vector2;
                    float2 _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_MaskUV_22_Vector2;
                    float2 _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_Mask2UV_23_Vector2;
                    float2 _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_Mask3UV_24_Vector2;
                    float2 _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_EmissionUV_25_Vector2;
                    float2 _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_DissolveUV_26_Vector2;
                    float2 _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_DissolveMaskUV_27_Vector2;
                    float2 _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_ColorBlendUV_28_Vector2;
                    float2 _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_RampColorUV_29_Vector2;
                    float2 _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_NoiseUV_30_Vector2;
                    float2 _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_NoiseMaskUV_31_Vector2;
                    float2 _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_BumpUV_32_Vector2;
                    float2 _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_ProgramNoiseUV_33_Vector2;
                    NBGraphBaseUV_float(_UV_dde6e6f3492e4fa689d7f4a88f3b492b_Out_0_Vector4, _Property_7a2d908f5a2f4990ae14fd870e5a22c9_Out_0_Vector4, _Property_45764d3c363e48539c14ce1d9b8eb3bb_Out_0_Float, _Property_4c0c6ce883a14260b681ac41d0f78472_Out_0_Float, _Property_ea0189e7a35b4e44a9e5775b9d1ec406_Out_0_Vector4, _UV_a8924bd05fcf4a28878b71b6b3b05cd7_Out_0_Vector4, _UV_ce3ab892df664c348236f6bce976c116_Out_0_Vector4, _Property_eafd46b75a1746f093439de914a3717c_Out_0_Float, _Property_02448b2e80a0440b804602e588eb2727_Out_0_Float, _Property_52b95ce95e564cb083a7953179be1554_Out_0_Float, _Property_6abc2ac395a54d7f99af7850796b2c20_Out_0_Float, _Property_3ca4059fa35141669dc7e92d919136a2_Out_0_Float, _Property_320511e8014a49c4a7e44e21e9c57f02_Out_0_Float, _Property_a91f4458b51f4b488287925809b8a798_Out_0_Float, _Property_06eb05a4a820436ab686475e2d043b4d_Out_0_Float, _Property_03cf5077fa564f27841d641d9c62f6a3_Out_0_Vector4, _Property_338f6b6c652440289b94c105205125f7_Out_0_Vector4, _Property_df09eceb3f6847288ccb0e2f2b6beb4c_Out_0_Vector4, _Property_4cd2fdd2348a4b7795e93a4e7e997131_Out_0_Float, _Property_65a8673d6744483e8aaf1ea1d5d4d4ca_Out_0_Vector4, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_Out_5_Vector2, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_MaskUV_22_Vector2, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_Mask2UV_23_Vector2, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_Mask3UV_24_Vector2, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_EmissionUV_25_Vector2, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_DissolveUV_26_Vector2, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_DissolveMaskUV_27_Vector2, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_ColorBlendUV_28_Vector2, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_RampColorUV_29_Vector2, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_NoiseUV_30_Vector2, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_NoiseMaskUV_31_Vector2, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_BumpUV_32_Vector2, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_ProgramNoiseUV_33_Vector2);
                    float _Property_c4d2b9d87084415fb9b51e578a0b9080_Out_0_Float = _MaskMapUVRotation;
                    float _Property_43ed7c33980543efb0a5f6f413791461_Out_0_Float = _MaskMapRotationSpeed;
                    float4 _Property_63ba51c6efaf4d659d2b6b921e7f1a43_Out_0_Vector4 = _MaskMapOffsetAnition;
                    float4 _Property_f9f92343294e499b8f4512de6aeefa5c_Out_0_Vector4 = _DissolveOffsetRotateDistort;
                    UnityTexture2D _Property_a5f2eeafb2c748ed9a1ec290960ab16f_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_DissolveMaskMap, sampler_DissolveMaskMap, _DissolveMaskMap_TexelSize, _DissolveMaskMap_ST, float4(0, 0, 0, 0));
                    float _Property_56d752220cbf4d96ab46764fc9aebb16_Out_0_Float = _DissolveMask_Toggle;
                    float _Property_e1702b0dd4cf43f18d778429097ef342_Out_0_Float = _DissolveMaskMode;
                    UnityTexture2D _Property_a6ea548184d045a89325bc16be701913_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_MaskMap2, sampler_MaskMap2, _MaskMap2_TexelSize, _MaskMap2_ST, float4(0, 0, 0, 0));
                    float _Property_7c78f411c5ff4fac85800c00002c0fbc_Out_0_Float = _Mask2_Toggle;
                    UnityTexture2D _Property_f9394d12cb9a412e8858ece414106043_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_MaskMap3, sampler_MaskMap3, _MaskMap3_TexelSize, _MaskMap3_ST, float4(0, 0, 0, 0));
                    float _Property_7e16bb82a3584c73a47c9dcc897f62a3_Out_0_Float = _Mask3_Toggle;
                    float4 _Property_282301309355467f98c2a0ca1c46538f_Out_0_Vector4 = _MaskMap3OffsetAnition;
                    float _Property_901675ce420344bb8ef7c1598f2a8c6c_Out_0_Float = _NB_WrapFlagsLo16;
                    float _Property_84ab548e17414d57ab8ab8e23e9c9a5d_Out_0_Float = _NB_WrapFlagsHi16;
                    float _Property_0a9b0dca2b7846869eb548347e06dfe0_Out_0_Float = _MaskMapGradientCount;
                    float4 _Property_bceef4027b6c4cc4b1d21c40d1e6b380_Out_0_Vector4 = _MaskMapGradientFloat0;
                    float4 _Property_c8f145683c5e4bc2b543907d534d50ac_Out_0_Vector4 = _MaskMapGradientFloat1;
                    float4 _Property_86555f566d7a4e47ad576dcbd5832f93_Out_0_Vector4 = _MaskMapGradientFloat2;
                    float _Property_3e87b9d2a706453cbd9014c75528eaa0_Out_0_Float = _MaskMap2GradientCount;
                    float4 _Property_53483bdbc2c74a8b824ea143d6eaec2b_Out_0_Vector4 = _MaskMap2GradientFloat0;
                    float4 _Property_4f05348fd4524abc964972190787b609_Out_0_Vector4 = _MaskMap2GradientFloat1;
                    float4 _Property_6235b1011ee0450c80fec0958cf7b4d4_Out_0_Vector4 = _MaskMap2GradientFloat2;
                    float _Property_a6bf239ac9f240709d42314fc1efd7b1_Out_0_Float = _MaskMap3GradientCount;
                    float4 _Property_bcd09d9bc52b4a4f9cb57238c0902210_Out_0_Vector4 = _MaskMap3GradientFloat0;
                    float4 _Property_f14563d83f83427a8730d060aca57e2e_Out_0_Vector4 = _MaskMap3GradientFloat1;
                    float4 _Property_43d61a210ee74ffda3c292b0848511af_Out_0_Vector4 = _MaskMap3GradientFloat2;
                    float _Property_5cdf8c4f8e5f4271bf734cfe073fcc52_Out_0_Float = _AlphaAll;
                    float4 _Property_adbdd97beb12402b87648f77f30ab4c0_Out_0_Vector4 = _ColorA;
                    float _Property_442020c8e09d56679b49c301f56b94a4_Out_0_Float = _BaseColorIntensityForTimeline;
                    float4 _Property_b2641bfb6535514caebe85f35a335537_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_BaseBackColor) : _BaseBackColor;
                    float _IsFrontFace_d1919b09d5db5f54aaa1b20fe1d91dbf_Out_0_Boolean = max(0, IN.FaceSign.x);
                    UnityTexture2D _Property_92eefc72bf7f56fd8f8e4084e22172be_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_EmissionMap, sampler_EmissionMap, _EmissionMap_TexelSize, _EmissionMap_ST, float4(0, 0, 0, 0));
                    float _Property_318d04aaa2195ad4b539c26b2c04eeac_Out_0_Float = _EmissionEnabled;
                    float4 _Property_7d76134eb0065dcf88906e1e451e441f_Out_0_Vector4 = _EmissionMapUVOffset;
                    float _Property_3540299c49785e3c86755a5fe22db945_Out_0_Float = _EmissionMapUVRotation;
                    float4 _Property_4af1e92a383f585dbcf0fb0884888164_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_EmissionMapColor) : _EmissionMapColor;
                    float _Property_10781681264d5230974041a749fa166e_Out_0_Float = _EmissionMapColorIntensity;
                    float _Property_6b0f493dcf575d0ab56773dc813aefa6_Out_0_Float = _EmissionAlphaIntensity;
                    UnityTexture2D _Property_d61bfdcc9d345709b6c4e37db8d8dc9d_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_ColorBlendMap, sampler_ColorBlendMap, _ColorBlendMap_TexelSize, _ColorBlendMap_ST, float4(0, 0, 0, 0));
                    float _Property_30534193d46e557494c8f844a330b5b4_Out_0_Float = _ColorBlendMap_Toggle;
                    float4 _Property_36317ab840af5f6db269cbc1733a86db_Out_0_Vector4 = _ColorBlendMapOffset;
                    float4 _Property_e78fc93d059558309f51829a464ff1ed_Out_0_Vector4 = _ColorBlendVec;
                    float4 _Property_a0161d17445e53a18e11ae4102584287_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_ColorBlendColor) : _ColorBlendColor;
                    float _Property_aa8cec6d7536592c83454ce0965f8b85_Out_0_Float = _ColorBlendColorIntensity;
                    UnityTexture2D _Property_2c26a69a61fc52a5939877dca4507b70_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_RampColorMap, sampler_RampColorMap, _RampColorMap_TexelSize, _RampColorMap_ST, float4(0, 0, 0, 0));
                    float _Property_668c759ef0f85daa97ec0fa2c458fd91_Out_0_Float = _RampColorToggle;
                    float _Property_0d0fcda8bbbf5e5dadddaf843017559f_Out_0_Float = _RampColorSourceMode;
                    float4 _Property_eb2d499a8f40574f91ca2bfbb37b0bf7_Out_0_Vector4 = _RampColorMapOffset;
                    float4 _Property_65576e0f99385f1fb9a7c67087c5b89d_Out_0_Vector4 = _RampColor0;
                    float4 _Property_55e82011a7bd54c7969e4044ab34a602_Out_0_Vector4 = _RampColor1;
                    float4 _Property_f5084aa30a6751de8d599e42fb5ffff5_Out_0_Vector4 = _RampColor2;
                    float4 _Property_8725b71298fd58698edbc87798ea15e9_Out_0_Vector4 = _RampColor3;
                    float4 _Property_04d0659f70de5bb8bf5972d05a3b68c8_Out_0_Vector4 = _RampColor4;
                    float4 _Property_4989a100e056527b92055a3c516a4eab_Out_0_Vector4 = _RampColor5;
                    float4 _Property_77bf5e95cff652cdaaa6bf93adeb0f93_Out_0_Vector4 = _RampColorAlpha0;
                    float4 _Property_c5d14f129a11542581793506ad8ba24e_Out_0_Vector4 = _RampColorAlpha1;
                    float4 _Property_e84364b4aa05540ebb986ab49893b436_Out_0_Vector4 = _RampColorAlpha2;
                    float _Property_7f00ce17ce115d2bb969973b132fe3e8_Out_0_Float = _RampColorCount;
                    float4 _Property_89e65c225052579ea3ac6095a234f65a_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_RampColorBlendColor) : _RampColorBlendColor;
                    float _Property_4750fb93d1d1562bad31b25dec3f8b25_Out_0_Float = _HueShift;
                    float _Property_5e6a32f15bf05438b9cc108706ad0625_Out_0_Float = _Contrast;
                    float4 _Property_866cffb762935a229d1b940e1f8b905c_Out_0_Vector4 = _ContrastMidColor;
                    float _Property_f97ecb43f95c5e37b841c32f67fa546e_Out_0_Float = _Saturability;
                    float4 _Property_713b0ac7fdd85eb2a23076a581e94638_Out_0_Vector4 = _BaseMapColorRefine;
                    float _Property_2fd1783a3bdc52f88effab68b88e0a47_Out_0_Float = _fresnelEnabled;
                    float4 _Property_760680a31d7b57b3a03b703fedd7dea7_Out_0_Vector4 = _FresnelUnit;
                    float4 _Property_2e33a988d6ea5eaea31eb783d2cb2026_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_FresnelColor) : _FresnelColor;
                    float4 _Property_990aa20b0fbe58bab561e7a26ecc62ad_Out_0_Vector4 = _FresnelRotation;
                    float4 _Property_e5767a3f300954768906d034fee505b2_Out_0_Vector4 = _Dissolve_Vec2;
                    float4 _Property_28013c8ed2865dbba415684e772fbe2f_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_DissolveLineColor) : _DissolveLineColor;
                    float _Property_872303b3673f5db6ba39a80132854337_Out_0_Float = _Dissolve_useRampMap_Toggle;
                    UnityTexture2D _Property_f712ace2c0ec59aca6e4ea56a5dc642b_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_DissolveRampMap, sampler_DissolveRampMap, _DissolveRampMap_TexelSize, _DissolveRampMap_ST, float4(0, 0, 0, 0));
                    float _Property_3f274a4912255dc2aa6e18641c408bd6_Out_0_Float = _DissolveRampSourceMode;
                    float4 _Property_2d934283df7757faadf21ff2aa713bb4_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_DissolveRampColor) : _DissolveRampColor;
                    float _Property_d2afb0df25d55430acab801c8a3dbfde_Out_0_Float = _DissolveRampCount;
                    float4 _Property_0dec3b42d61b5e7c862c4ca295f9e208_Out_0_Vector4 = _DissolveRampColor0;
                    float4 _Property_4b091596ce61549b96d0e88967383f48_Out_0_Vector4 = _DissolveRampColor1;
                    float4 _Property_e246b4e0fa405aca8b55e192c06587a0_Out_0_Vector4 = _DissolveRampColor2;
                    float4 _Property_7a5d7978834d5d4eb9428c0a196dfbd3_Out_0_Vector4 = _DissolveRampColor3;
                    float4 _Property_6ab07b2800785722806fc80f0a180a5e_Out_0_Vector4 = _DissolveRampColor4;
                    float4 _Property_5250e3a06c0252d6a6bb9743ea2f0ac1_Out_0_Vector4 = _DissolveRampColor5;
                    float4 _Property_64999d5c415b5a73b4a0c204bcf29377_Out_0_Vector4 = _DissolveRampAlpha0;
                    float4 _Property_a934aa24a47f5da2992d3e468dbd374d_Out_0_Vector4 = _DissolveRampAlpha1;
                    float4 _Property_629063539c105cb3a2e8d3eade7eefb1_Out_0_Vector4 = _DissolveRampAlpha2;
                    float _Property_fc9eb8a77e24500ab5421051111d6fda_Out_0_Float = _NB_ForceNoMipFlagsLo16;
                    float _Property_6311ff30de835a0da66e70c41e5eb4a9_Out_0_Float = _NB_ForceNoMipFlagsHi16;
                    float _Property_9439295f5abe51489892e22adc16c2b7_Out_0_Float = _NB_DissolveRampSTOverrideEnabled;
                    float4 _Property_e0d280ecac5f56c48b444399bdfee24d_Out_0_Vector4 = _NB_DissolveRampSTOverride;
                    float _Property_d24b5ab9f5354781acf90544fdd34e1a_Out_0_Float = _DistanceFade_Toggle;
                    float4 _Property_d525ffde86414d429eb7d33f95dbc125_Out_0_Vector4 = _Fade;
                    float _Property_2f1d59a7ccfd4e21bdcabf18c881577c_Out_0_Float = _SoftParticlesEnabled;
                    float4 _Property_c04e77b9696340d1b77e0c6a9910ffae_Out_0_Vector4 = _SoftParticleFadeParams;
                    float4 _ScreenPosition_86fbeeb2af2346549e9611c0551ed415_Out_0_Vector4 = float4(IN.NDCPosition.xy, 0, 0);
                    float _Property_67f9358d1a79442eb25e5a4b980feddb_Out_0_Float = _DepthOutline_Toggle;
                    float4 _Property_75062f76b29a4f08b37655dc233735d6_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_DepthOutline_Color) : _DepthOutline_Color;
                    float4 _Property_8fbc75cf62cb493ea789465b0e094396_Out_0_Vector4 = _DepthOutline_Vec;
                    UnityTexture2D _Property_8f072da64b1f45d992849b1e47b91c14_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_BaseMap, sampler_BaseMap, _BaseMap_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
                    UnityTexture2D _Property_a9a873c1f9545377b6d5603870d8ce7e_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_NoiseMap, sampler_NoiseMap, _NoiseMap_TexelSize, _NoiseMap_ST, float4(0, 0, 0, 0));
                    float _Property_05cfd7336ce55efda9ba538458718e94_Out_0_Float = _noisemapEnabled;
                    float _Property_de00f2b38f8d559eb2e11b9a8afbf0ab_Out_0_Float = _NoiseMapUVRotation;
                    float4 _Property_c1523df23e3f5ea7a2816bc39b189b9d_Out_0_Vector4 = _NoiseOffset;
                    float _Property_6745a2aa39ce5a6db6ef4145ce16fc4d_Out_0_Float = _NoiseIntensity;
                    float4 _Property_0692aaad7ae555198da5d2971b415df9_Out_0_Vector4 = _DistortionDirection;
                    UnityTexture2D _Property_7a1c57aac1ae5322904a7fbc9050f2ce_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_NoiseMaskMap, sampler_NoiseMaskMap, _NoiseMaskMap_TexelSize, _NoiseMaskMap_ST, float4(0, 0, 0, 0));
                    float _Property_931f026875de5593b8c7d6f47800f865_Out_0_Float = _noiseMaskMap_Toggle;
                    float _Property_cff65480aa2c5e98b542088b70270554_Out_0_Float = _TexDistortion_intensity;
                    float _Property_5f7732a02ece5e6fb25d3e1ecc105818_Out_0_Float = _Emi_Distortion_intensity;
                    float _Property_b04226779a985e25a46ae050e8c15dcc_Out_0_Float = _MaskDistortion_intensity;
                    float _Property_415818c2850c582fa48e2026af7e309e_Out_0_Float = _MatCapToggle;
                    UnityTexture2D _Property_53f494020d7651608ba56c8922ff1ab9_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_MatCapTex, sampler_MatCapTex, float4(1, 1, 1, 1), float4(1, 1, 0, 0), float4(0, 0, 0, 0));
                    float4 _Property_6df429d0561957cb84dfad2ee12aeb46_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_MatCapColor) : _MatCapColor;
                    float4 _Property_5c8f0472975b5156a2b33799226739c6_Out_0_Vector4 = _MatCapInfo;
                    float _Property_d2d631afc2c1544b83cb34681aa85a03_Out_0_Float = _BumpMapToggle;
                    UnityTexture2D _Property_8a8ba308b2a1563ea8c2419c29c5c7b9_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_BumpTex, sampler_BumpTex, float4(1, 1, 1, 1), _BumpTex_ST, float4(0, 0, 0, 0));
                    float _Property_6d9934d6abfd5c0697171ddfd0c97fdc_Out_0_Float = _BumpScale;
                    float _Property_e5ae484542c256ef9e2c843d58514935_Out_0_Float = _FxLightMode;
                    float4 _Property_6301978f07ae543781068ab22b637a6c_Out_0_Vector4 = _MaterialInfo;
                    float4 _Property_9f86734760a45426a1b7d524232798ab_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_SpecularColor) : _SpecularColor;
                    float _Property_665d05cdb47f5ac3b55f9494eebcfd24_Out_0_Float = _ProgramNoise_Toggle;
                    float _Property_97cf65b2fe9f579a9efe26a73d830bb9_Out_0_Float = _ProgramNoise_Simple_Toggle;
                    float _Property_46cf15a5712c5271ab621a4945709213_Out_0_Float = _ProgramNoise_Voronoi_Toggle;
                    float _Property_4d5ce3c87f6b5b5eb6b5488a205f5da2_Out_0_Float = _ProgramNoise_Rotate;
                    float4 _Property_73408a6e8749509dbf03b29118ea169a_Out_0_Vector4 = _DissolveVoronoi_Vec;
                    float4 _Property_4c7f65f090b75d33a899f4860a5eb28c_Out_0_Vector4 = _DissolveVoronoi_Vec2;
                    float4 _Property_cc5eb5485f3f503e991dd48cda2773d8_Out_0_Vector4 = _DissolveVoronoi_Vec3;
                    float4 _Property_ea6439cfbd84510292b048fc6619980b_Out_0_Vector4 = _DissolveVoronoi_Vec4;
                    float _Property_ead7fba0b75559f78e76a0fbf67cae12_Out_0_Float = _ProgramNoiseBaseBlendOpacity;
                    float _Property_13f2b053fdb9593aa51c8639f809a891_Out_0_Float = _MaskPNoiseBlendOpacity;
                    float _Property_9767ed7b0d615801a6878b0fc416e6f0_Out_0_Float = _DissolvePNoiseBlendOpacity;
                    float _Property_3750f20ccf7b5a7fb1d9e4bd3fb2b6ec_Out_0_Float = _NB_PNoiseBlendLo16;
                    float _Property_5d371e5c8c7459f280e4945d8a055cdc_Out_0_Float = _NB_PNoiseBlendHi16;
                    UnityTexture2D _Property_c125096410c15c18bbff38461fb105d3_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_RigRTBk, sampler_RigRTBk, float4(1, 1, 1, 1), float4(1, 1, 0, 0), float4(0, 0, 0, 0));
                    UnityTexture2D _Property_38036157e2ec5c659072d3660c785123_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_RigLBtF, sampler_RigLBtF, float4(1, 1, 1, 1), float4(1, 1, 0, 0), float4(0, 0, 0, 0));
                    UnityTexture2D _Property_269c61b4d8fe5a7684fc52010ece7432_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_SixWayEmissionRamp, sampler_SixWayEmissionRamp, float4(1, 1, 1, 1), float4(1, 1, 0, 0), float4(0, 0, 0, 0));
                    float4 _Property_89139d6b0c285de9a58e1f6df2e7d236_Out_0_Vector4 = _SixWayInfo;
                    float4 _Property_547327d8b0f75f868f2ecda76c814b21_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_SixWayEmissionColor) : _SixWayEmissionColor;
                    float _Property_d2fe351137c85eac9a617d3f05ff92b2_Out_0_Float = _SixWayColorAbsorptionToggle;
                    float _Property_25cf481f9d865f129398a908a10a4f11_Out_0_Float = _DistortPNoiseBlendOpacity;
                    float4 _NBGraphBaseColorCustomFunction_f712bc264a1c40848edc2dd476a4cada_Out_3_Vector4;
                    float2 _NBGraphBaseColorCustomFunction_f712bc264a1c40848edc2dd476a4cada_NBDistortionSignedRG_145_Vector2;
                    float _NBGraphBaseColorCustomFunction_f712bc264a1c40848edc2dd476a4cada_NBDistortionNoiseMask_146_Float;
                    NBGraphBaseColor_float(_NBGraphBaseColorCustomFunction_f712bc264a1c40848edc2dd476a4cada_SampledAlbedo_0_Vector4, _NBGraphBaseColorCustomFunction_f712bc264a1c40848edc2dd476a4cada_SelectedAlpha_1_Float, _Property_13840ad57da543e2b33275638ce1189e_Out_0_Vector4, _Property_eafd46b75a1746f093439de914a3717c_Out_0_Float, _Property_02448b2e80a0440b804602e588eb2727_Out_0_Float, _Property_52b95ce95e564cb083a7953179be1554_Out_0_Float, _Property_6abc2ac395a54d7f99af7850796b2c20_Out_0_Float, _Property_4fbc64ac55184ee8b2348b861c93db8b_Out_0_Vector2, _Property_9099f47df6d24486a9e0bb9dd8fa7cf8_Out_0_Float, _Property_01827f8348b040028326054a1623cc84_Out_0_Float, _Property_87436152ca164e73877206654813b186_Out_0_Float, _Property_9aab3b8bff1a4b39a348ae3ea124a0e3_Out_0_Float, _Property_a31f37f37774434e8b5383ecef8f5072_Out_0_Float, _Property_e1e8e8a73a5f5e1c9e969abcb9782cd2_Out_0_Texture2D, _Property_26acbca87fca52f4a5ea05d6fcf09e48_Out_0_Float, _Property_c7f5afaa32df59eb9c6b2561b57aa26f_Out_0_Vector4, _Property_6e138238299250348f63a5ee2f1f3d5b_Out_0_Vector4, _Property_20f932f599b85641aeb7c2ea908ed484_Out_0_Float, _Property_b4f86e887f26523f845f932a27414b5b_Out_0_Texture2D, _Property_a70b9f8477c75d2a98fd83c15f287a37_Out_0_Float, _Property_a05025df3a5c534f88355f30cb99d2d3_Out_0_Vector4, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_MaskUV_22_Vector2, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_DissolveUV_26_Vector2, _Property_c4d2b9d87084415fb9b51e578a0b9080_Out_0_Float, _Property_43ed7c33980543efb0a5f6f413791461_Out_0_Float, _Property_63ba51c6efaf4d659d2b6b921e7f1a43_Out_0_Vector4, _Property_f9f92343294e499b8f4512de6aeefa5c_Out_0_Vector4, _Property_a5f2eeafb2c748ed9a1ec290960ab16f_Out_0_Texture2D, _Property_56d752220cbf4d96ab46764fc9aebb16_Out_0_Float, _Property_e1702b0dd4cf43f18d778429097ef342_Out_0_Float, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_DissolveMaskUV_27_Vector2, _Property_a6ea548184d045a89325bc16be701913_Out_0_Texture2D, _Property_7c78f411c5ff4fac85800c00002c0fbc_Out_0_Float, _Property_f9394d12cb9a412e8858ece414106043_Out_0_Texture2D, _Property_7e16bb82a3584c73a47c9dcc897f62a3_Out_0_Float, _Property_282301309355467f98c2a0ca1c46538f_Out_0_Vector4, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_Mask2UV_23_Vector2, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_Mask3UV_24_Vector2, _Property_901675ce420344bb8ef7c1598f2a8c6c_Out_0_Float, _Property_84ab548e17414d57ab8ab8e23e9c9a5d_Out_0_Float, _Property_0a9b0dca2b7846869eb548347e06dfe0_Out_0_Float, _Property_bceef4027b6c4cc4b1d21c40d1e6b380_Out_0_Vector4, _Property_c8f145683c5e4bc2b543907d534d50ac_Out_0_Vector4, _Property_86555f566d7a4e47ad576dcbd5832f93_Out_0_Vector4, _Property_3e87b9d2a706453cbd9014c75528eaa0_Out_0_Float, _Property_53483bdbc2c74a8b824ea143d6eaec2b_Out_0_Vector4, _Property_4f05348fd4524abc964972190787b609_Out_0_Vector4, _Property_6235b1011ee0450c80fec0958cf7b4d4_Out_0_Vector4, _Property_a6bf239ac9f240709d42314fc1efd7b1_Out_0_Float, _Property_bcd09d9bc52b4a4f9cb57238c0902210_Out_0_Vector4, _Property_f14563d83f83427a8730d060aca57e2e_Out_0_Vector4, _Property_43d61a210ee74ffda3c292b0848511af_Out_0_Vector4, _Property_5cdf8c4f8e5f4271bf734cfe073fcc52_Out_0_Float, _Property_adbdd97beb12402b87648f77f30ab4c0_Out_0_Vector4, IN.VertexColor, _Property_442020c8e09d56679b49c301f56b94a4_Out_0_Float, _Property_b2641bfb6535514caebe85f35a335537_Out_0_Vector4, ((float) _IsFrontFace_d1919b09d5db5f54aaa1b20fe1d91dbf_Out_0_Boolean), _Property_92eefc72bf7f56fd8f8e4084e22172be_Out_0_Texture2D, _Property_318d04aaa2195ad4b539c26b2c04eeac_Out_0_Float, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_EmissionUV_25_Vector2, _Property_7d76134eb0065dcf88906e1e451e441f_Out_0_Vector4, _Property_3540299c49785e3c86755a5fe22db945_Out_0_Float, _Property_4af1e92a383f585dbcf0fb0884888164_Out_0_Vector4, _Property_10781681264d5230974041a749fa166e_Out_0_Float, _Property_6b0f493dcf575d0ab56773dc813aefa6_Out_0_Float, _Property_d61bfdcc9d345709b6c4e37db8d8dc9d_Out_0_Texture2D, _Property_30534193d46e557494c8f844a330b5b4_Out_0_Float, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_ColorBlendUV_28_Vector2, _Property_36317ab840af5f6db269cbc1733a86db_Out_0_Vector4, _Property_e78fc93d059558309f51829a464ff1ed_Out_0_Vector4, _Property_a0161d17445e53a18e11ae4102584287_Out_0_Vector4, _Property_aa8cec6d7536592c83454ce0965f8b85_Out_0_Float, _Property_2c26a69a61fc52a5939877dca4507b70_Out_0_Texture2D, _Property_668c759ef0f85daa97ec0fa2c458fd91_Out_0_Float, _Property_0d0fcda8bbbf5e5dadddaf843017559f_Out_0_Float, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_RampColorUV_29_Vector2, _Property_eb2d499a8f40574f91ca2bfbb37b0bf7_Out_0_Vector4, _Property_65576e0f99385f1fb9a7c67087c5b89d_Out_0_Vector4, _Property_55e82011a7bd54c7969e4044ab34a602_Out_0_Vector4, _Property_f5084aa30a6751de8d599e42fb5ffff5_Out_0_Vector4, _Property_8725b71298fd58698edbc87798ea15e9_Out_0_Vector4, _Property_04d0659f70de5bb8bf5972d05a3b68c8_Out_0_Vector4, _Property_4989a100e056527b92055a3c516a4eab_Out_0_Vector4, _Property_77bf5e95cff652cdaaa6bf93adeb0f93_Out_0_Vector4, _Property_c5d14f129a11542581793506ad8ba24e_Out_0_Vector4, _Property_e84364b4aa05540ebb986ab49893b436_Out_0_Vector4, _Property_7f00ce17ce115d2bb969973b132fe3e8_Out_0_Float, _Property_89e65c225052579ea3ac6095a234f65a_Out_0_Vector4, _Property_4750fb93d1d1562bad31b25dec3f8b25_Out_0_Float, _Property_5e6a32f15bf05438b9cc108706ad0625_Out_0_Float, _Property_866cffb762935a229d1b940e1f8b905c_Out_0_Vector4, _Property_f97ecb43f95c5e37b841c32f67fa546e_Out_0_Float, _Property_713b0ac7fdd85eb2a23076a581e94638_Out_0_Vector4, _Property_2fd1783a3bdc52f88effab68b88e0a47_Out_0_Float, _Property_760680a31d7b57b3a03b703fedd7dea7_Out_0_Vector4, _Property_2e33a988d6ea5eaea31eb783d2cb2026_Out_0_Vector4, _Property_990aa20b0fbe58bab561e7a26ecc62ad_Out_0_Vector4, IN.WorldSpaceNormal, IN.WorldSpaceViewDirection, _Property_e5767a3f300954768906d034fee505b2_Out_0_Vector4, _Property_28013c8ed2865dbba415684e772fbe2f_Out_0_Vector4, _Property_872303b3673f5db6ba39a80132854337_Out_0_Float, _Property_f712ace2c0ec59aca6e4ea56a5dc642b_Out_0_Texture2D, _Property_3f274a4912255dc2aa6e18641c408bd6_Out_0_Float, _Property_2d934283df7757faadf21ff2aa713bb4_Out_0_Vector4, _Property_d2afb0df25d55430acab801c8a3dbfde_Out_0_Float, _Property_0dec3b42d61b5e7c862c4ca295f9e208_Out_0_Vector4, _Property_4b091596ce61549b96d0e88967383f48_Out_0_Vector4, _Property_e246b4e0fa405aca8b55e192c06587a0_Out_0_Vector4, _Property_7a5d7978834d5d4eb9428c0a196dfbd3_Out_0_Vector4, _Property_6ab07b2800785722806fc80f0a180a5e_Out_0_Vector4, _Property_5250e3a06c0252d6a6bb9743ea2f0ac1_Out_0_Vector4, _Property_64999d5c415b5a73b4a0c204bcf29377_Out_0_Vector4, _Property_a934aa24a47f5da2992d3e468dbd374d_Out_0_Vector4, _Property_629063539c105cb3a2e8d3eade7eefb1_Out_0_Vector4, _Property_fc9eb8a77e24500ab5421051111d6fda_Out_0_Float, _Property_6311ff30de835a0da66e70c41e5eb4a9_Out_0_Float, _Property_9439295f5abe51489892e22adc16c2b7_Out_0_Float, _Property_e0d280ecac5f56c48b444399bdfee24d_Out_0_Vector4, _Property_d24b5ab9f5354781acf90544fdd34e1a_Out_0_Float, _Property_d525ffde86414d429eb7d33f95dbc125_Out_0_Vector4, IN.ViewSpacePosition, _Property_2f1d59a7ccfd4e21bdcabf18c881577c_Out_0_Float, _Property_c04e77b9696340d1b77e0c6a9910ffae_Out_0_Vector4, _ScreenPosition_86fbeeb2af2346549e9611c0551ed415_Out_0_Vector4, _Property_67f9358d1a79442eb25e5a4b980feddb_Out_0_Float, _Property_75062f76b29a4f08b37655dc233735d6_Out_0_Vector4, _Property_8fbc75cf62cb493ea789465b0e094396_Out_0_Vector4, _Property_8f072da64b1f45d992849b1e47b91c14_Out_0_Texture2D, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_Out_5_Vector2, _Property_a9a873c1f9545377b6d5603870d8ce7e_Out_0_Texture2D, _Property_05cfd7336ce55efda9ba538458718e94_Out_0_Float, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_NoiseUV_30_Vector2, _Property_de00f2b38f8d559eb2e11b9a8afbf0ab_Out_0_Float, _Property_c1523df23e3f5ea7a2816bc39b189b9d_Out_0_Vector4, _Property_6745a2aa39ce5a6db6ef4145ce16fc4d_Out_0_Float, _Property_0692aaad7ae555198da5d2971b415df9_Out_0_Vector4, _Property_7a1c57aac1ae5322904a7fbc9050f2ce_Out_0_Texture2D, _Property_931f026875de5593b8c7d6f47800f865_Out_0_Float, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_NoiseMaskUV_31_Vector2, _Property_cff65480aa2c5e98b542088b70270554_Out_0_Float, _Property_5f7732a02ece5e6fb25d3e1ecc105818_Out_0_Float, _Property_b04226779a985e25a46ae050e8c15dcc_Out_0_Float, _Property_415818c2850c582fa48e2026af7e309e_Out_0_Float, _Property_53f494020d7651608ba56c8922ff1ab9_Out_0_Texture2D, _Property_6df429d0561957cb84dfad2ee12aeb46_Out_0_Vector4, _Property_5c8f0472975b5156a2b33799226739c6_Out_0_Vector4, _Property_d2d631afc2c1544b83cb34681aa85a03_Out_0_Float, _Property_8a8ba308b2a1563ea8c2419c29c5c7b9_Out_0_Texture2D, _Property_6d9934d6abfd5c0697171ddfd0c97fdc_Out_0_Float, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_BumpUV_32_Vector2, IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, _Property_e5ae484542c256ef9e2c843d58514935_Out_0_Float, _Property_6301978f07ae543781068ab22b637a6c_Out_0_Vector4, _Property_9f86734760a45426a1b7d524232798ab_Out_0_Vector4, IN.WorldSpacePosition, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_ProgramNoiseUV_33_Vector2, _Property_665d05cdb47f5ac3b55f9494eebcfd24_Out_0_Float, _Property_97cf65b2fe9f579a9efe26a73d830bb9_Out_0_Float, _Property_46cf15a5712c5271ab621a4945709213_Out_0_Float, _Property_4d5ce3c87f6b5b5eb6b5488a205f5da2_Out_0_Float, _Property_73408a6e8749509dbf03b29118ea169a_Out_0_Vector4, _Property_4c7f65f090b75d33a899f4860a5eb28c_Out_0_Vector4, _Property_cc5eb5485f3f503e991dd48cda2773d8_Out_0_Vector4, _Property_ea6439cfbd84510292b048fc6619980b_Out_0_Vector4, _Property_ead7fba0b75559f78e76a0fbf67cae12_Out_0_Float, _Property_13f2b053fdb9593aa51c8639f809a891_Out_0_Float, _Property_9767ed7b0d615801a6878b0fc416e6f0_Out_0_Float, _Property_3750f20ccf7b5a7fb1d9e4bd3fb2b6ec_Out_0_Float, _Property_5d371e5c8c7459f280e4945d8a055cdc_Out_0_Float, _Property_c125096410c15c18bbff38461fb105d3_Out_0_Texture2D, _Property_38036157e2ec5c659072d3660c785123_Out_0_Texture2D, _Property_269c61b4d8fe5a7684fc52010ece7432_Out_0_Texture2D, _Property_89139d6b0c285de9a58e1f6df2e7d236_Out_0_Vector4, _Property_547327d8b0f75f868f2ecda76c814b21_Out_0_Vector4, _Property_d2fe351137c85eac9a617d3f05ff92b2_Out_0_Float, IN.SixBake0, IN.SixBake1, IN.SixBake2, IN.SixBack0, IN.SixBack1, IN.SixBack2, IN.SixTangentSigned, _Property_25cf481f9d865f129398a908a10a4f11_Out_0_Float, _NBGraphBaseColorCustomFunction_f712bc264a1c40848edc2dd476a4cada_Out_3_Vector4, _NBGraphBaseColorCustomFunction_f712bc264a1c40848edc2dd476a4cada_NBDistortionSignedRG_145_Vector2, _NBGraphBaseColorCustomFunction_f712bc264a1c40848edc2dd476a4cada_NBDistortionNoiseMask_146_Float);
                    float _Swizzle_52d10f3207ae43dc9038120fea622ca3_Out_1_Float = _NBGraphBaseColorCustomFunction_f712bc264a1c40848edc2dd476a4cada_Out_3_Vector4.w;
                    float _Property_b2c9094aad4345b78e06cfbf0854a2bd_Out_0_Float = _Cutoff;
                    surface.Alpha = _Swizzle_52d10f3207ae43dc9038120fea622ca3_Out_1_Float;
                    surface.AlphaClipThreshold = _Property_b2c9094aad4345b78e06cfbf0854a2bd_Out_0_Float;
                    return surface;
                }
            
            // --------------------------------------------------
            // Build Graph Inputs
            #ifdef HAVE_VFX_MODIFICATION
            #define VFX_SRP_ATTRIBUTES Attributes
            #define VFX_SRP_VARYINGS Varyings
            #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
            #endif
            VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
                {
                    VertexDescriptionInputs output;
                    ZERO_INITIALIZE(VertexDescriptionInputs, output);
                
                    output.ObjectSpaceNormal =                          input.normalOS;
                    output.WorldSpaceNormal =                           TransformObjectToWorldNormal(input.normalOS);
                    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
                    output.WorldSpaceTangent =                          TransformObjectToWorldDir(input.tangentOS.xyz);
                    output.ObjectSpaceBiTangent =                       normalize(cross(input.normalOS, input.tangentOS.xyz) * (input.tangentOS.w > 0.0f ? 1.0f : -1.0f) * GetOddNegativeScale());
                    output.WorldSpaceBiTangent =                        TransformObjectToWorldDir(output.ObjectSpaceBiTangent);
                    output.ObjectSpacePosition =                        input.positionOS;
                    output.uv0 =                                        input.uv0;
                    output.uv1 =                                        input.uv1;
                    output.uv2 =                                        input.uv2;
                    output.VertexColor =                                input.color;
                #if UNITY_ANY_INSTANCING_ENABLED
                #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
                #endif
                
                    return output;
                }
                SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
                {
                    SurfaceDescriptionInputs output;
                    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
                
                #ifdef HAVE_VFX_MODIFICATION
                #if VFX_USE_GRAPH_VALUES
                    uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
                    /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
                #endif
                    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
                
                #endif
                
                    output.SixBake0 = input.SixBake0;
                output.SixBake1 = input.SixBake1;
                output.SixBake2 = input.SixBake2;
                output.SixBack0 = input.SixBack0;
                output.SixBack1 = input.SixBack1;
                output.SixBack2 = input.SixBack2;
                output.SixTangentSigned = input.SixTangentSigned;
                
                    // must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
                    float3 unnormalizedNormalWS = input.normalWS;
                    const float renormFactor = 1.0 / length(unnormalizedNormalWS);
                
                    // use bitangent on the fly like in hdrp
                    // IMPORTANT! If we ever support Flip on double sided materials ensure bitangent and tangent are NOT flipped.
                    float crossSign = (input.tangentWS.w > 0.0 ? 1.0 : -1.0)* GetOddNegativeScale();
                    float3 bitang = crossSign * cross(input.normalWS.xyz, input.tangentWS.xyz);
                
                    output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;      // we want a unit length Normal Vector node in shader graph
                
                    // to pr               eserve mikktspace compliance we use same scale renormFactor as was used on the normal.
                    // This                is explained in section 2.2 in "surface gradient based bump mapping framework"
                    output.WorldSpaceTangent = renormFactor * input.tangentWS.xyz;
                    output.WorldSpaceBiTangent = renormFactor * bitang;
                
                    output.WorldSpaceViewDirection = GetWorldSpaceNormalizeViewDir(input.positionWS);
                    output.WorldSpacePosition = input.positionWS;
                    output.ViewSpacePosition = TransformWorldToView(input.positionWS);
                
                    #if UNITY_UV_STARTS_AT_TOP
                    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
                    #else
                    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
                    #endif
                
                    output.NDCPosition = output.PixelPosition.xy / _ScaledScreenParams.xy;
                    output.NDCPosition.y = 1.0f - output.NDCPosition.y;
                
                    output.uv0 = input.texCoord0;
                    output.uv1 = input.texCoord1;
                    output.uv2 = input.texCoord2;
                    output.VertexColor = input.color;
                #if UNITY_ANY_INSTANCING_ENABLED
                #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
                #endif
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
                #else
                #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                #endif
                    BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                
                        return output;
                }
                
            // --------------------------------------------------
            // Main
            
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/DepthNormalsOnlyPass.hlsl"
            
            // --------------------------------------------------
            // Visual Effect Vertex Invocations
            #ifdef HAVE_VFX_MODIFICATION
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
            #endif
            
            ENDHLSL
            }
            Pass
            {
                Name "GBuffer"
                Tags
                {
                    "LightMode" = "UniversalGBuffer"
                }
            
            // Render State
            Cull [_Cull]
                Blend [_SrcBlend] [_DstBlend], [_SrcBlendAlpha] [_DstBlendAlpha]
                ZTest [_ZTest]
                ZWrite [_ZWrite]
                Stencil
                {
                ReadMask [_StencilReadMask]
                WriteMask [_StencilWriteMask]
                Ref [_Stencil]
                CompFront [_StencilComp]
                ZFailFront [_StencilZFail]
                FailFront [_StencilFail]
                PassFront [_StencilOp]
                CompBack [_StencilComp]
                ZFailBack [_StencilZFail]
                FailBack [_StencilFail]
                PassBack [_StencilOp]
                }
            
            // Debug
            // <None>
            
            // --------------------------------------------------
            // Pass
            
            HLSLPROGRAM
            
            // Pragmas
            #pragma target 4.5
                #pragma exclude_renderers gles3 glcore
                #pragma multi_compile_instancing
                #pragma instancing_options renderinglayer
                #pragma vertex vert
                #pragma fragment frag
            
            // Keywords
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
                #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
                #pragma multi_compile_fragment _ _RENDER_PASS_ENABLED
                #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
                #pragma multi_compile _ SHADOWS_SHADOWMASK
                #pragma shader_feature_fragment _ _SURFACE_TYPE_TRANSPARENT
                #pragma shader_feature_local_fragment _ _ALPHAPREMULTIPLY_ON
                #pragma shader_feature_local_fragment _ _ALPHAMODULATE_ON
                #pragma shader_feature_local_fragment _ _ALPHATEST_ON
            // GraphKeywords: <None>
            
            // Defines
            
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define ATTRIBUTES_NEED_TEXCOORD1
            #define ATTRIBUTES_NEED_TEXCOORD2
            #define ATTRIBUTES_NEED_COLOR
            #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
            #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
            #define VARYINGS_NEED_POSITION_WS
            #define VARYINGS_NEED_NORMAL_WS
            #define VARYINGS_NEED_TANGENT_WS
            #define VARYINGS_NEED_TEXCOORD0
            #define VARYINGS_NEED_TEXCOORD1
            #define VARYINGS_NEED_TEXCOORD2
            #define VARYINGS_NEED_COLOR
            #define VARYINGS_NEED_CULLFACE
            #define FEATURES_GRAPH_VERTEX
            /* WARNING: $splice Could not find named fragment 'PassInstancing' */
            #define SHADERPASS SHADERPASS_GBUFFER
                #define SHADERGRAPH_PREVIEW_MAIN
            
            
            // custom interpolator pre-include
            /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
            
            // Includes
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
            
            // --------------------------------------------------
            // Structs and Packing
            
            // custom interpolators pre packing
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
            
            struct Attributes
                {
                     float3 positionOS : POSITION;
                     float3 normalOS : NORMAL;
                     float4 tangentOS : TANGENT;
                     float4 uv0 : TEXCOORD0;
                     float4 uv1 : TEXCOORD1;
                     float4 uv2 : TEXCOORD2;
                     float4 color : COLOR;
                    #if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
                     uint instanceID : INSTANCEID_SEMANTIC;
                    #endif
                };
                struct Varyings
                {
                     float4 positionCS : SV_POSITION;
                     float3 positionWS;
                     float3 normalWS;
                     float4 tangentWS;
                     float4 texCoord0;
                     float4 texCoord1;
                     float4 texCoord2;
                     float4 color;
                    #if !defined(LIGHTMAP_ON)
                     float3 sh;
                    #endif
                    #if defined(USE_APV_PROBE_OCCLUSION)
                     float4 probeOcclusion;
                    #endif
                    #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
                     uint instanceID : CUSTOM_INSTANCE_ID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                    #endif
                     float3 SixBake0;
                     float3 SixBake1;
                     float3 SixBake2;
                     float3 SixBack0;
                     float3 SixBack1;
                     float3 SixBack2;
                     float4 SixTangentSigned;
                };
                struct SurfaceDescriptionInputs
                {
                     float3 WorldSpaceNormal;
                     float3 WorldSpaceTangent;
                     float3 WorldSpaceBiTangent;
                     float3 WorldSpaceViewDirection;
                     float3 ViewSpacePosition;
                     float3 WorldSpacePosition;
                     float2 NDCPosition;
                     float2 PixelPosition;
                     float4 uv0;
                     float4 uv1;
                     float4 uv2;
                     float4 VertexColor;
                     float FaceSign;
                     float3 SixBake0;
                     float3 SixBake1;
                     float3 SixBake2;
                     float3 SixBack0;
                     float3 SixBack1;
                     float3 SixBack2;
                     float4 SixTangentSigned;
                };
                struct VertexDescriptionInputs
                {
                     float3 ObjectSpaceNormal;
                     float3 WorldSpaceNormal;
                     float3 ObjectSpaceTangent;
                     float3 WorldSpaceTangent;
                     float3 ObjectSpaceBiTangent;
                     float3 WorldSpaceBiTangent;
                     float3 ObjectSpacePosition;
                     float4 uv0;
                     float4 uv1;
                     float4 uv2;
                     float4 VertexColor;
                };
                struct PackedVaryings
                {
                     float4 positionCS : SV_POSITION;
                    #if !defined(LIGHTMAP_ON)
                     float3 sh : INTERP0;
                    #endif
                    #if defined(USE_APV_PROBE_OCCLUSION)
                     float4 probeOcclusion : INTERP1;
                    #endif
                     float4 tangentWS : INTERP2;
                     float4 texCoord0 : INTERP3;
                     float4 texCoord1 : INTERP4;
                     float4 texCoord2 : INTERP5;
                     float4 color : INTERP6;
                     float4 SixTangentSigned : INTERP7;
                     float4 packed_positionWS_SixBack1x : INTERP8;
                     float4 packed_normalWS_SixBack1y : INTERP9;
                     float4 packed_SixBake0_SixBack1z : INTERP10;
                     float4 packed_SixBake1_SixBack2x : INTERP11;
                     float4 packed_SixBake2_SixBack2y : INTERP12;
                     float4 packed_SixBack0_SixBack2z : INTERP13;
                    #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
                     uint instanceID : CUSTOM_INSTANCE_ID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                     uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                     uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                     FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
                    #endif
                };
            
            PackedVaryings PackVaryings (Varyings input)
                {
                    PackedVaryings output;
                    ZERO_INITIALIZE(PackedVaryings, output);
                    output.positionCS = input.positionCS;
                    #if !defined(LIGHTMAP_ON)
                    output.sh = input.sh;
                    #endif
                    #if defined(USE_APV_PROBE_OCCLUSION)
                    output.probeOcclusion = input.probeOcclusion;
                    #endif
                    output.tangentWS.xyzw = input.tangentWS;
                    output.texCoord0.xyzw = input.texCoord0;
                    output.texCoord1.xyzw = input.texCoord1;
                    output.texCoord2.xyzw = input.texCoord2;
                    output.color.xyzw = input.color;
                    output.SixTangentSigned.xyzw = input.SixTangentSigned;
                    output.packed_positionWS_SixBack1x.xyz = input.positionWS;
                    output.packed_positionWS_SixBack1x.w = input.SixBack1.x;
                    output.packed_normalWS_SixBack1y.xyz = input.normalWS;
                    output.packed_normalWS_SixBack1y.w = input.SixBack1.y;
                    output.packed_SixBake0_SixBack1z.xyz = input.SixBake0;
                    output.packed_SixBake0_SixBack1z.w = input.SixBack1.z;
                    output.packed_SixBake1_SixBack2x.xyz = input.SixBake1;
                    output.packed_SixBake1_SixBack2x.w = input.SixBack2.x;
                    output.packed_SixBake2_SixBack2y.xyz = input.SixBake2;
                    output.packed_SixBake2_SixBack2y.w = input.SixBack2.y;
                    output.packed_SixBack0_SixBack2z.xyz = input.SixBack0;
                    output.packed_SixBack0_SixBack2z.w = input.SixBack2.z;
                    #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
                    output.instanceID = input.instanceID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
                    #endif
                    return output;
                }
                
                Varyings UnpackVaryings (PackedVaryings input)
                {
                    Varyings output;
                    output.positionCS = input.positionCS;
                    #if !defined(LIGHTMAP_ON)
                    output.sh = input.sh;
                    #endif
                    #if defined(USE_APV_PROBE_OCCLUSION)
                    output.probeOcclusion = input.probeOcclusion;
                    #endif
                    output.tangentWS = input.tangentWS.xyzw;
                    output.texCoord0 = input.texCoord0.xyzw;
                    output.texCoord1 = input.texCoord1.xyzw;
                    output.texCoord2 = input.texCoord2.xyzw;
                    output.color = input.color.xyzw;
                    output.SixTangentSigned = input.SixTangentSigned.xyzw;
                    output.positionWS = input.packed_positionWS_SixBack1x.xyz;
                    output.SixBack1.x = input.packed_positionWS_SixBack1x.w;
                    output.normalWS = input.packed_normalWS_SixBack1y.xyz;
                    output.SixBack1.y = input.packed_normalWS_SixBack1y.w;
                    output.SixBake0 = input.packed_SixBake0_SixBack1z.xyz;
                    output.SixBack1.z = input.packed_SixBake0_SixBack1z.w;
                    output.SixBake1 = input.packed_SixBake1_SixBack2x.xyz;
                    output.SixBack2.x = input.packed_SixBake1_SixBack2x.w;
                    output.SixBake2 = input.packed_SixBake2_SixBack2y.xyz;
                    output.SixBack2.y = input.packed_SixBake2_SixBack2y.w;
                    output.SixBack0 = input.packed_SixBack0_SixBack2z.xyz;
                    output.SixBack2.z = input.packed_SixBack0_SixBack2z.w;
                    #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
                    output.instanceID = input.instanceID;
                    #endif
                    #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                    output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
                    #endif
                    #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                    output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
                    #endif
                    #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                    output.cullFace = input.cullFace;
                    #endif
                    return output;
                }
                
            
            // --------------------------------------------------
            // Graph
            
            // Graph Properties
            CBUFFER_START(UnityPerMaterial)
                float4 _NBGraphBaseColorCustomFunction_f712bc264a1c40848edc2dd476a4cada_SampledAlbedo_0_Vector4;
                float _NBGraphBaseColorCustomFunction_f712bc264a1c40848edc2dd476a4cada_SelectedAlpha_1_Float;
                TEXTURE2D(_BaseMap);
                SAMPLER(sampler_BaseMap);
                float4 _BaseMap_TexelSize;
                float4 _Color;
                float2 _NB_DistortionNoise;
                float _NB_DistortionIntensity;
                float _NB_DistortionMode;
                float _NB_Flags0Lo16;
                float _NB_Flags0Hi16;
                float _NB_Flags1Lo16;
                float _NB_Flags1Hi16;
                float _NB_DistortionAlphaPow;
                float _NB_DistortionAlphaMultiplier;
                float _NB_DistortionAlphaAdd;
                TEXTURE2D(_MaskMap);
                SAMPLER(sampler_MaskMap);
                float4 _MaskMap_TexelSize;
                float4 _MaskMap_ST;
                float _Mask_Toggle;
                float4 _MaskMapVec;
                float4 _MaskRefineVec;
                float _NB_ColorChannelLo16;
                TEXTURE2D(_DissolveMap);
                SAMPLER(sampler_DissolveMap);
                float4 _DissolveMap_TexelSize;
                float4 _DissolveMap_ST;
                float _Dissolve_Toggle;
                float4 _Dissolve;
                float4 _BaseMap_ST;
                float _BaseMapUVRotation;
                float _BaseMapUVRotationSpeed;
                float4 _BaseMapMaskMapOffset;
                float _MaskMapUVRotation;
                float _MaskMapRotationSpeed;
                float4 _MaskMapOffsetAnition;
                float4 _DissolveOffsetRotateDistort;
                TEXTURE2D(_DissolveMaskMap);
                SAMPLER(sampler_DissolveMaskMap);
                float4 _DissolveMaskMap_TexelSize;
                float4 _DissolveMaskMap_ST;
                float _DissolveMask_Toggle;
                float _DissolveMaskMode;
                TEXTURE2D(_MaskMap2);
                SAMPLER(sampler_MaskMap2);
                float4 _MaskMap2_TexelSize;
                float4 _MaskMap2_ST;
                float _Mask2_Toggle;
                TEXTURE2D(_MaskMap3);
                SAMPLER(sampler_MaskMap3);
                float4 _MaskMap3_TexelSize;
                float4 _MaskMap3_ST;
                float _Mask3_Toggle;
                float4 _MaskMap3OffsetAnition;
                float _NB_WrapFlagsLo16;
                float _NB_WrapFlagsHi16;
                float _MaskMapGradientCount;
                float4 _MaskMapGradientFloat0;
                float4 _MaskMapGradientFloat1;
                float4 _MaskMapGradientFloat2;
                float _MaskMap2GradientCount;
                float4 _MaskMap2GradientFloat0;
                float4 _MaskMap2GradientFloat1;
                float4 _MaskMap2GradientFloat2;
                float _MaskMap3GradientCount;
                float4 _MaskMap3GradientFloat0;
                float4 _MaskMap3GradientFloat1;
                float4 _MaskMap3GradientFloat2;
                float _AlphaAll;
                float4 _ColorA;
                float _BaseColorIntensityForTimeline;
                float4 _BaseBackColor;
                TEXTURE2D(_EmissionMap);
                SAMPLER(sampler_EmissionMap);
                float4 _EmissionMap_TexelSize;
                float4 _EmissionMap_ST;
                float _EmissionEnabled;
                float4 _EmissionMapUVOffset;
                float _EmissionMapUVRotation;
                float4 _EmissionMapColor;
                float _EmissionMapColorIntensity;
                float _EmissionAlphaIntensity;
                TEXTURE2D(_ColorBlendMap);
                SAMPLER(sampler_ColorBlendMap);
                float4 _ColorBlendMap_TexelSize;
                float4 _ColorBlendMap_ST;
                float _ColorBlendMap_Toggle;
                float4 _ColorBlendMapOffset;
                float4 _ColorBlendVec;
                float4 _ColorBlendColor;
                float _ColorBlendColorIntensity;
                TEXTURE2D(_RampColorMap);
                SAMPLER(sampler_RampColorMap);
                float4 _RampColorMap_TexelSize;
                float4 _RampColorMap_ST;
                float _RampColorToggle;
                float _RampColorSourceMode;
                float4 _RampColorMapOffset;
                float4 _RampColor0;
                float4 _RampColor1;
                float4 _RampColor2;
                float4 _RampColor3;
                float4 _RampColor4;
                float4 _RampColor5;
                float4 _RampColorAlpha0;
                float4 _RampColorAlpha1;
                float4 _RampColorAlpha2;
                float _RampColorCount;
                float4 _RampColorBlendColor;
                float _HueShift;
                float _Contrast;
                float4 _ContrastMidColor;
                float _Saturability;
                float4 _BaseMapColorRefine;
                float _fresnelEnabled;
                float4 _FresnelUnit;
                float4 _FresnelColor;
                float4 _FresnelRotation;
                float4 _Dissolve_Vec2;
                float4 _DissolveLineColor;
                float _Dissolve_useRampMap_Toggle;
                TEXTURE2D(_DissolveRampMap);
                SAMPLER(sampler_DissolveRampMap);
                float4 _DissolveRampMap_TexelSize;
                float4 _DissolveRampMap_ST;
                float _DissolveRampSourceMode;
                float4 _DissolveRampColor;
                float _DissolveRampCount;
                float4 _DissolveRampColor0;
                float4 _DissolveRampColor1;
                float4 _DissolveRampColor2;
                float4 _DissolveRampColor3;
                float4 _DissolveRampColor4;
                float4 _DissolveRampColor5;
                float4 _DissolveRampAlpha0;
                float4 _DissolveRampAlpha1;
                float4 _DissolveRampAlpha2;
                float _NB_ForceNoMipFlagsLo16;
                float _NB_ForceNoMipFlagsHi16;
                float _NB_DissolveRampSTOverrideEnabled;
                float4 _NB_DissolveRampSTOverride;
                float _DistanceFade_Toggle;
                float4 _Fade;
                float _SoftParticlesEnabled;
                float4 _SoftParticleFadeParams;
                float _DepthOutline_Toggle;
                float4 _DepthOutline_Color;
                float4 _DepthOutline_Vec;
                float _NB_UVModeFlag0Lo16;
                float _NB_UVModeFlag0Hi16;
                float _NB_UVModeFlagType0Lo16;
                float _NB_UVModeFlagType0Hi16;
                float4 _SharedUV_ST;
                float4 _SharedUV_Vec;
                float4 _TWParameter;
                float _TWStrength;
                float4 _PCCenter;
                TEXTURE2D(_NoiseMap);
                SAMPLER(sampler_NoiseMap);
                float4 _NoiseMap_TexelSize;
                float4 _NoiseMap_ST;
                float _noisemapEnabled;
                float _NoiseMapUVRotation;
                float4 _NoiseOffset;
                float _NoiseIntensity;
                float4 _DistortionDirection;
                TEXTURE2D(_NoiseMaskMap);
                SAMPLER(sampler_NoiseMaskMap);
                float4 _NoiseMaskMap_TexelSize;
                float4 _NoiseMaskMap_ST;
                float _noiseMaskMap_Toggle;
                float _TexDistortion_intensity;
                float _Emi_Distortion_intensity;
                float _MaskDistortion_intensity;
                float _Cutoff;
                float _AdditiveToPreMultiplyAlphaLerp;
                TEXTURE2D(_VertexOffset_Map);
                SAMPLER(sampler_VertexOffset_Map);
                float4 _VertexOffset_Map_TexelSize;
                float4 _VertexOffset_Map_ST;
                float4 _VertexOffset_Vec;
                float4 _VertexOffset_CustomDir;
                float _VertexOffset_NormalDir_Toggle;
                float _VertexOffset_DirectionSpace;
                float _VertexOffset_Toggle;
                TEXTURE2D(_VertexOffset_MaskMap);
                SAMPLER(sampler_VertexOffset_MaskMap);
                float4 _VertexOffset_MaskMap_TexelSize;
                float4 _VertexOffset_MaskMap_ST;
                float4 _VertexOffset_MaskMap_Vec;
                float _VertexOffset_Mask_Toggle;
                float _NB_ColorChannelHi16;
                float _MatCapToggle;
                TEXTURE2D(_MatCapTex);
                SAMPLER(sampler_MatCapTex);
                float4 _MatCapColor;
                float4 _MatCapInfo;
                float _BumpMapToggle;
                TEXTURE2D(_BumpTex);
                SAMPLER(sampler_BumpTex);
                float4 _BumpTex_ST;
                float _BumpScale;
                float _FxLightMode;
                float4 _MaterialInfo;
                float4 _SpecularColor;
                float _ProgramNoise_Toggle;
                float _ProgramNoise_Simple_Toggle;
                float _ProgramNoise_Voronoi_Toggle;
                float _ProgramNoise_Rotate;
                float4 _DissolveVoronoi_Vec;
                float4 _DissolveVoronoi_Vec2;
                float4 _DissolveVoronoi_Vec3;
                float4 _DissolveVoronoi_Vec4;
                float _ProgramNoiseBaseBlendOpacity;
                float _MaskPNoiseBlendOpacity;
                float _DissolvePNoiseBlendOpacity;
                float _NB_PNoiseBlendLo16;
                float _NB_PNoiseBlendHi16;
                TEXTURE2D(_RigRTBk);
                SAMPLER(sampler_RigRTBk);
                TEXTURE2D(_RigLBtF);
                SAMPLER(sampler_RigLBtF);
                TEXTURE2D(_SixWayEmissionRamp);
                SAMPLER(sampler_SixWayEmissionRamp);
                float4 _SixWayInfo;
                float4 _SixWayEmissionColor;
                float _SixWayColorAbsorptionToggle;
                float _DistortPNoiseBlendOpacity;
                UNITY_TEXTURE_STREAMING_DEBUG_VARS;
                CBUFFER_END
                #define UNITY_ACCESS_HYBRID_INSTANCED_PROP(var, type) var
            
            // Graph Includes
            #include_with_pragmas "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBGraphBaseColor.hlsl"
            #include_with_pragmas "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBGraphVertexOffset.hlsl"
            #include_with_pragmas "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBGraphBaseUV.hlsl"
            
            // -- Property used by ScenePickingPass
            #ifdef SCENEPICKINGPASS
            float4 _SelectionID;
            #endif
            
            // -- Properties used by SceneSelectionPass
            #ifdef SCENESELECTIONPASS
            int _ObjectId;
            int _PassValue;
            #endif
            
            // Graph Functions
            // GraphFunctions: <None>
            
            // Custom interpolators pre vertex
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
            
            // Graph Vertex
            struct VertexDescription
                {
                    float3 Position;
                    float3 Normal;
                    float3 Tangent;
                    float3 SixBake0;
                    float3 SixBake1;
                    float3 SixBake2;
                    float3 SixBack0;
                    float3 SixBack1;
                    float3 SixBack2;
                    float4 SixTangentSigned;
                };
                
                VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
                {
                    VertexDescription description = (VertexDescription)0;
                    float3 _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Bake0_3_Vector3;
                    float3 _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Bake1_4_Vector3;
                    float3 _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Bake2_5_Vector3;
                    float3 _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Back0_6_Vector3;
                    float3 _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Back1_7_Vector3;
                    float3 _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Back2_8_Vector3;
                    float4 _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_TangentSigned_9_Vector4;
                    NBGraphSixWayBake_float(IN.WorldSpaceNormal, IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Bake0_3_Vector3, _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Bake1_4_Vector3, _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Bake2_5_Vector3, _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Back0_6_Vector3, _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Back1_7_Vector3, _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Back2_8_Vector3, _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_TangentSigned_9_Vector4);
                    float4 _UV_596a5b2640e15e43aff23ea3c9c87059_Out_0_Vector4 = IN.uv0;
                    float4 _UV_7fa8f9025e9059649e9bdc291afd4417_Out_0_Vector4 = IN.uv1;
                    float4 _UV_8576cf3978f855598e9e2c2e37cdc003_Out_0_Vector4 = IN.uv2;
                    UnityTexture2D _Property_6f7cd0164d485a30803eb055841cd1ce_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_VertexOffset_Map, sampler_VertexOffset_Map, _VertexOffset_Map_TexelSize, _VertexOffset_Map_ST, float4(0, 0, 0, 0));
                    float4 _Property_fd85324fa4f658d691990180b8f8793e_Out_0_Vector4 = _VertexOffset_Vec;
                    float4 _Property_ccc6a218db89506f996c9a8c85bf89d4_Out_0_Vector4 = _VertexOffset_CustomDir;
                    float _Property_8cf30747736a58bcb43ec6620008dd56_Out_0_Float = _VertexOffset_NormalDir_Toggle;
                    float _Property_d1162da7b0a7551b80f631805d2a7134_Out_0_Float = _VertexOffset_DirectionSpace;
                    float _Property_18ffb5b24a215c74989dff20b668c91b_Out_0_Float = _VertexOffset_Toggle;
                    UnityTexture2D _Property_33de3bd0609655b7b71629b91a283fa1_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_VertexOffset_MaskMap, sampler_VertexOffset_MaskMap, _VertexOffset_MaskMap_TexelSize, _VertexOffset_MaskMap_ST, float4(0, 0, 0, 0));
                    float4 _Property_c972b51415e4592b9f07b2c052809e33_Out_0_Vector4 = _VertexOffset_MaskMap_Vec;
                    float _Property_01f9ee3ac21a544aa8c1fa82346bd03e_Out_0_Float = _VertexOffset_Mask_Toggle;
                    float _Property_849d65e3adfb5e4f996dd29864f2c86f_Out_0_Float = _NB_Flags0Lo16;
                    float _Property_3198e5a482ff5fb79bfc8ef49e694023_Out_0_Float = _NB_Flags0Hi16;
                    float _Property_c9800dd9977850049d324b45be73cc33_Out_0_Float = _NB_Flags1Lo16;
                    float _Property_dea9b3365dc35d0daf45f162fe71ae92_Out_0_Float = _NB_Flags1Hi16;
                    float _Property_a457aad3783c559d96ddfe0cf5d647ea_Out_0_Float = _NB_WrapFlagsLo16;
                    float _Property_14f98c3cf7b7525d8340b90ff8e5fd35_Out_0_Float = _NB_WrapFlagsHi16;
                    float _Property_560a4b92f5295597bb1175a0f82a5c76_Out_0_Float = _NB_ColorChannelLo16;
                    float _Property_770d2aaf793952348bc222b930283f2b_Out_0_Float = _NB_ColorChannelHi16;
                    float _Property_efa6ef74db395e619e03f11b575a1029_Out_0_Float = _NB_UVModeFlag0Lo16;
                    float _Property_ef246b0486a15ce4a55423fe806ebb20_Out_0_Float = _NB_UVModeFlag0Hi16;
                    float _Property_214c5c87c6c754658d4b9d2883ae02c5_Out_0_Float = _NB_UVModeFlagType0Lo16;
                    float _Property_b9de1de3181c5b9b9fcdb4836a81b926_Out_0_Float = _NB_UVModeFlagType0Hi16;
                    float4 _Property_9e2af066867e551bb1eed3199acfe809_Out_0_Vector4 = _SharedUV_ST;
                    float4 _Property_614e89fe251b5185af927c4ee7175f5f_Out_0_Vector4 = _SharedUV_Vec;
                    float4 _Property_b2c755308b2759418ed105ab21db3493_Out_0_Vector4 = _TWParameter;
                    float _Property_132da21d369f53e9b55031e5415fe894_Out_0_Float = _TWStrength;
                    float4 _Property_3561d005d5bb5f4ab0d3e545e06a5780_Out_0_Vector4 = _PCCenter;
                    float3 _NBGraphVertexOffsetCustomFunction_395601997b2e51c795e7c1ec707a5553_OutPositionOS_33_Vector3;
                    float3 _NBGraphVertexOffsetCustomFunction_395601997b2e51c795e7c1ec707a5553_OutNormalOS_34_Vector3;
                    float3 _NBGraphVertexOffsetCustomFunction_395601997b2e51c795e7c1ec707a5553_OutTangentOS_35_Vector3;
                    float _NBGraphVertexOffsetCustomFunction_395601997b2e51c795e7c1ec707a5553_Supported_36_Float;
                    NBGraphVertexOffset_float(IN.ObjectSpacePosition, IN.ObjectSpaceNormal, IN.ObjectSpaceTangent, (IN.VertexColor.xyz), _UV_596a5b2640e15e43aff23ea3c9c87059_Out_0_Vector4, _UV_7fa8f9025e9059649e9bdc291afd4417_Out_0_Vector4, _UV_8576cf3978f855598e9e2c2e37cdc003_Out_0_Vector4, _Property_6f7cd0164d485a30803eb055841cd1ce_Out_0_Texture2D, _Property_fd85324fa4f658d691990180b8f8793e_Out_0_Vector4, (_Property_ccc6a218db89506f996c9a8c85bf89d4_Out_0_Vector4.xyz), _Property_8cf30747736a58bcb43ec6620008dd56_Out_0_Float, _Property_d1162da7b0a7551b80f631805d2a7134_Out_0_Float, _Property_18ffb5b24a215c74989dff20b668c91b_Out_0_Float, _Property_33de3bd0609655b7b71629b91a283fa1_Out_0_Texture2D, (_Property_c972b51415e4592b9f07b2c052809e33_Out_0_Vector4.xyz), _Property_01f9ee3ac21a544aa8c1fa82346bd03e_Out_0_Float, _Property_849d65e3adfb5e4f996dd29864f2c86f_Out_0_Float, _Property_3198e5a482ff5fb79bfc8ef49e694023_Out_0_Float, _Property_c9800dd9977850049d324b45be73cc33_Out_0_Float, _Property_dea9b3365dc35d0daf45f162fe71ae92_Out_0_Float, _Property_a457aad3783c559d96ddfe0cf5d647ea_Out_0_Float, _Property_14f98c3cf7b7525d8340b90ff8e5fd35_Out_0_Float, _Property_560a4b92f5295597bb1175a0f82a5c76_Out_0_Float, _Property_770d2aaf793952348bc222b930283f2b_Out_0_Float, _Property_efa6ef74db395e619e03f11b575a1029_Out_0_Float, _Property_ef246b0486a15ce4a55423fe806ebb20_Out_0_Float, _Property_214c5c87c6c754658d4b9d2883ae02c5_Out_0_Float, _Property_b9de1de3181c5b9b9fcdb4836a81b926_Out_0_Float, _Property_9e2af066867e551bb1eed3199acfe809_Out_0_Vector4, _Property_614e89fe251b5185af927c4ee7175f5f_Out_0_Vector4, _Property_b2c755308b2759418ed105ab21db3493_Out_0_Vector4, _Property_132da21d369f53e9b55031e5415fe894_Out_0_Float, _Property_3561d005d5bb5f4ab0d3e545e06a5780_Out_0_Vector4, _NBGraphVertexOffsetCustomFunction_395601997b2e51c795e7c1ec707a5553_OutPositionOS_33_Vector3, _NBGraphVertexOffsetCustomFunction_395601997b2e51c795e7c1ec707a5553_OutNormalOS_34_Vector3, _NBGraphVertexOffsetCustomFunction_395601997b2e51c795e7c1ec707a5553_OutTangentOS_35_Vector3, _NBGraphVertexOffsetCustomFunction_395601997b2e51c795e7c1ec707a5553_Supported_36_Float);
                    description.Position = _NBGraphVertexOffsetCustomFunction_395601997b2e51c795e7c1ec707a5553_OutPositionOS_33_Vector3;
                    description.Normal = _NBGraphVertexOffsetCustomFunction_395601997b2e51c795e7c1ec707a5553_OutNormalOS_34_Vector3;
                    description.Tangent = _NBGraphVertexOffsetCustomFunction_395601997b2e51c795e7c1ec707a5553_OutTangentOS_35_Vector3;
                    description.SixBake0 = _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Bake0_3_Vector3;
                    description.SixBake1 = _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Bake1_4_Vector3;
                    description.SixBake2 = _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Bake2_5_Vector3;
                    description.SixBack0 = _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Back0_6_Vector3;
                    description.SixBack1 = _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Back1_7_Vector3;
                    description.SixBack2 = _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_Back2_8_Vector3;
                    description.SixTangentSigned = _NBGraphSixWayBakeCustomFunction_acd8dee617cf518c868888b21c993ace_TangentSigned_9_Vector4;
                    return description;
                }
            
            // Custom interpolators, pre surface
            #ifdef FEATURES_GRAPH_VERTEX
            Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
            {
            output.SixBake0 = input.SixBake0;
            output.SixBake1 = input.SixBake1;
            output.SixBake2 = input.SixBake2;
            output.SixBack0 = input.SixBack0;
            output.SixBack1 = input.SixBack1;
            output.SixBack2 = input.SixBack2;
            output.SixTangentSigned = input.SixTangentSigned;
            return output;
            }
            #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
            #endif
            
            // Graph Pixel
            struct SurfaceDescription
                {
                    float3 BaseColor;
                    float Alpha;
                    float AlphaClipThreshold;
                };
                
                SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
                {
                    SurfaceDescription surface = (SurfaceDescription)0;
                    float4 _Property_13840ad57da543e2b33275638ce1189e_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_Color) : _Color;
                    float _Property_eafd46b75a1746f093439de914a3717c_Out_0_Float = _NB_Flags0Lo16;
                    float _Property_02448b2e80a0440b804602e588eb2727_Out_0_Float = _NB_Flags0Hi16;
                    float _Property_52b95ce95e564cb083a7953179be1554_Out_0_Float = _NB_Flags1Lo16;
                    float _Property_6abc2ac395a54d7f99af7850796b2c20_Out_0_Float = _NB_Flags1Hi16;
                    float2 _Property_4fbc64ac55184ee8b2348b861c93db8b_Out_0_Vector2 = _NB_DistortionNoise;
                    float _Property_9099f47df6d24486a9e0bb9dd8fa7cf8_Out_0_Float = _NB_DistortionIntensity;
                    float _Property_01827f8348b040028326054a1623cc84_Out_0_Float = _NB_DistortionMode;
                    float _Property_87436152ca164e73877206654813b186_Out_0_Float = _NB_DistortionAlphaPow;
                    float _Property_9aab3b8bff1a4b39a348ae3ea124a0e3_Out_0_Float = _NB_DistortionAlphaMultiplier;
                    float _Property_a31f37f37774434e8b5383ecef8f5072_Out_0_Float = _NB_DistortionAlphaAdd;
                    UnityTexture2D _Property_e1e8e8a73a5f5e1c9e969abcb9782cd2_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_MaskMap, sampler_MaskMap, _MaskMap_TexelSize, _MaskMap_ST, float4(0, 0, 0, 0));
                    float _Property_26acbca87fca52f4a5ea05d6fcf09e48_Out_0_Float = _Mask_Toggle;
                    float4 _Property_c7f5afaa32df59eb9c6b2561b57aa26f_Out_0_Vector4 = _MaskMapVec;
                    float4 _Property_6e138238299250348f63a5ee2f1f3d5b_Out_0_Vector4 = _MaskRefineVec;
                    float _Property_20f932f599b85641aeb7c2ea908ed484_Out_0_Float = _NB_ColorChannelLo16;
                    UnityTexture2D _Property_b4f86e887f26523f845f932a27414b5b_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_DissolveMap, sampler_DissolveMap, _DissolveMap_TexelSize, _DissolveMap_ST, float4(0, 0, 0, 0));
                    float _Property_a70b9f8477c75d2a98fd83c15f287a37_Out_0_Float = _Dissolve_Toggle;
                    float4 _Property_a05025df3a5c534f88355f30cb99d2d3_Out_0_Vector4 = _Dissolve;
                    float4 _UV_dde6e6f3492e4fa689d7f4a88f3b492b_Out_0_Vector4 = IN.uv0;
                    float4 _Property_7a2d908f5a2f4990ae14fd870e5a22c9_Out_0_Vector4 = _BaseMap_ST;
                    float _Property_45764d3c363e48539c14ce1d9b8eb3bb_Out_0_Float = _BaseMapUVRotation;
                    float _Property_4c0c6ce883a14260b681ac41d0f78472_Out_0_Float = _BaseMapUVRotationSpeed;
                    float4 _Property_ea0189e7a35b4e44a9e5775b9d1ec406_Out_0_Vector4 = _BaseMapMaskMapOffset;
                    float4 _UV_a8924bd05fcf4a28878b71b6b3b05cd7_Out_0_Vector4 = IN.uv1;
                    float4 _UV_ce3ab892df664c348236f6bce976c116_Out_0_Vector4 = IN.uv2;
                    float _Property_3ca4059fa35141669dc7e92d919136a2_Out_0_Float = _NB_UVModeFlag0Lo16;
                    float _Property_320511e8014a49c4a7e44e21e9c57f02_Out_0_Float = _NB_UVModeFlag0Hi16;
                    float _Property_a91f4458b51f4b488287925809b8a798_Out_0_Float = _NB_UVModeFlagType0Lo16;
                    float _Property_06eb05a4a820436ab686475e2d043b4d_Out_0_Float = _NB_UVModeFlagType0Hi16;
                    float4 _Property_03cf5077fa564f27841d641d9c62f6a3_Out_0_Vector4 = _SharedUV_ST;
                    float4 _Property_338f6b6c652440289b94c105205125f7_Out_0_Vector4 = _SharedUV_Vec;
                    float4 _Property_df09eceb3f6847288ccb0e2f2b6beb4c_Out_0_Vector4 = _TWParameter;
                    float _Property_4cd2fdd2348a4b7795e93a4e7e997131_Out_0_Float = _TWStrength;
                    float4 _Property_65a8673d6744483e8aaf1ea1d5d4d4ca_Out_0_Vector4 = _PCCenter;
                    float2 _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_Out_5_Vector2;
                    float2 _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_MaskUV_22_Vector2;
                    float2 _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_Mask2UV_23_Vector2;
                    float2 _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_Mask3UV_24_Vector2;
                    float2 _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_EmissionUV_25_Vector2;
                    float2 _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_DissolveUV_26_Vector2;
                    float2 _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_DissolveMaskUV_27_Vector2;
                    float2 _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_ColorBlendUV_28_Vector2;
                    float2 _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_RampColorUV_29_Vector2;
                    float2 _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_NoiseUV_30_Vector2;
                    float2 _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_NoiseMaskUV_31_Vector2;
                    float2 _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_BumpUV_32_Vector2;
                    float2 _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_ProgramNoiseUV_33_Vector2;
                    NBGraphBaseUV_float(_UV_dde6e6f3492e4fa689d7f4a88f3b492b_Out_0_Vector4, _Property_7a2d908f5a2f4990ae14fd870e5a22c9_Out_0_Vector4, _Property_45764d3c363e48539c14ce1d9b8eb3bb_Out_0_Float, _Property_4c0c6ce883a14260b681ac41d0f78472_Out_0_Float, _Property_ea0189e7a35b4e44a9e5775b9d1ec406_Out_0_Vector4, _UV_a8924bd05fcf4a28878b71b6b3b05cd7_Out_0_Vector4, _UV_ce3ab892df664c348236f6bce976c116_Out_0_Vector4, _Property_eafd46b75a1746f093439de914a3717c_Out_0_Float, _Property_02448b2e80a0440b804602e588eb2727_Out_0_Float, _Property_52b95ce95e564cb083a7953179be1554_Out_0_Float, _Property_6abc2ac395a54d7f99af7850796b2c20_Out_0_Float, _Property_3ca4059fa35141669dc7e92d919136a2_Out_0_Float, _Property_320511e8014a49c4a7e44e21e9c57f02_Out_0_Float, _Property_a91f4458b51f4b488287925809b8a798_Out_0_Float, _Property_06eb05a4a820436ab686475e2d043b4d_Out_0_Float, _Property_03cf5077fa564f27841d641d9c62f6a3_Out_0_Vector4, _Property_338f6b6c652440289b94c105205125f7_Out_0_Vector4, _Property_df09eceb3f6847288ccb0e2f2b6beb4c_Out_0_Vector4, _Property_4cd2fdd2348a4b7795e93a4e7e997131_Out_0_Float, _Property_65a8673d6744483e8aaf1ea1d5d4d4ca_Out_0_Vector4, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_Out_5_Vector2, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_MaskUV_22_Vector2, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_Mask2UV_23_Vector2, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_Mask3UV_24_Vector2, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_EmissionUV_25_Vector2, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_DissolveUV_26_Vector2, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_DissolveMaskUV_27_Vector2, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_ColorBlendUV_28_Vector2, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_RampColorUV_29_Vector2, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_NoiseUV_30_Vector2, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_NoiseMaskUV_31_Vector2, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_BumpUV_32_Vector2, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_ProgramNoiseUV_33_Vector2);
                    float _Property_c4d2b9d87084415fb9b51e578a0b9080_Out_0_Float = _MaskMapUVRotation;
                    float _Property_43ed7c33980543efb0a5f6f413791461_Out_0_Float = _MaskMapRotationSpeed;
                    float4 _Property_63ba51c6efaf4d659d2b6b921e7f1a43_Out_0_Vector4 = _MaskMapOffsetAnition;
                    float4 _Property_f9f92343294e499b8f4512de6aeefa5c_Out_0_Vector4 = _DissolveOffsetRotateDistort;
                    UnityTexture2D _Property_a5f2eeafb2c748ed9a1ec290960ab16f_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_DissolveMaskMap, sampler_DissolveMaskMap, _DissolveMaskMap_TexelSize, _DissolveMaskMap_ST, float4(0, 0, 0, 0));
                    float _Property_56d752220cbf4d96ab46764fc9aebb16_Out_0_Float = _DissolveMask_Toggle;
                    float _Property_e1702b0dd4cf43f18d778429097ef342_Out_0_Float = _DissolveMaskMode;
                    UnityTexture2D _Property_a6ea548184d045a89325bc16be701913_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_MaskMap2, sampler_MaskMap2, _MaskMap2_TexelSize, _MaskMap2_ST, float4(0, 0, 0, 0));
                    float _Property_7c78f411c5ff4fac85800c00002c0fbc_Out_0_Float = _Mask2_Toggle;
                    UnityTexture2D _Property_f9394d12cb9a412e8858ece414106043_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_MaskMap3, sampler_MaskMap3, _MaskMap3_TexelSize, _MaskMap3_ST, float4(0, 0, 0, 0));
                    float _Property_7e16bb82a3584c73a47c9dcc897f62a3_Out_0_Float = _Mask3_Toggle;
                    float4 _Property_282301309355467f98c2a0ca1c46538f_Out_0_Vector4 = _MaskMap3OffsetAnition;
                    float _Property_901675ce420344bb8ef7c1598f2a8c6c_Out_0_Float = _NB_WrapFlagsLo16;
                    float _Property_84ab548e17414d57ab8ab8e23e9c9a5d_Out_0_Float = _NB_WrapFlagsHi16;
                    float _Property_0a9b0dca2b7846869eb548347e06dfe0_Out_0_Float = _MaskMapGradientCount;
                    float4 _Property_bceef4027b6c4cc4b1d21c40d1e6b380_Out_0_Vector4 = _MaskMapGradientFloat0;
                    float4 _Property_c8f145683c5e4bc2b543907d534d50ac_Out_0_Vector4 = _MaskMapGradientFloat1;
                    float4 _Property_86555f566d7a4e47ad576dcbd5832f93_Out_0_Vector4 = _MaskMapGradientFloat2;
                    float _Property_3e87b9d2a706453cbd9014c75528eaa0_Out_0_Float = _MaskMap2GradientCount;
                    float4 _Property_53483bdbc2c74a8b824ea143d6eaec2b_Out_0_Vector4 = _MaskMap2GradientFloat0;
                    float4 _Property_4f05348fd4524abc964972190787b609_Out_0_Vector4 = _MaskMap2GradientFloat1;
                    float4 _Property_6235b1011ee0450c80fec0958cf7b4d4_Out_0_Vector4 = _MaskMap2GradientFloat2;
                    float _Property_a6bf239ac9f240709d42314fc1efd7b1_Out_0_Float = _MaskMap3GradientCount;
                    float4 _Property_bcd09d9bc52b4a4f9cb57238c0902210_Out_0_Vector4 = _MaskMap3GradientFloat0;
                    float4 _Property_f14563d83f83427a8730d060aca57e2e_Out_0_Vector4 = _MaskMap3GradientFloat1;
                    float4 _Property_43d61a210ee74ffda3c292b0848511af_Out_0_Vector4 = _MaskMap3GradientFloat2;
                    float _Property_5cdf8c4f8e5f4271bf734cfe073fcc52_Out_0_Float = _AlphaAll;
                    float4 _Property_adbdd97beb12402b87648f77f30ab4c0_Out_0_Vector4 = _ColorA;
                    float _Property_442020c8e09d56679b49c301f56b94a4_Out_0_Float = _BaseColorIntensityForTimeline;
                    float4 _Property_b2641bfb6535514caebe85f35a335537_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_BaseBackColor) : _BaseBackColor;
                    float _IsFrontFace_d1919b09d5db5f54aaa1b20fe1d91dbf_Out_0_Boolean = max(0, IN.FaceSign.x);
                    UnityTexture2D _Property_92eefc72bf7f56fd8f8e4084e22172be_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_EmissionMap, sampler_EmissionMap, _EmissionMap_TexelSize, _EmissionMap_ST, float4(0, 0, 0, 0));
                    float _Property_318d04aaa2195ad4b539c26b2c04eeac_Out_0_Float = _EmissionEnabled;
                    float4 _Property_7d76134eb0065dcf88906e1e451e441f_Out_0_Vector4 = _EmissionMapUVOffset;
                    float _Property_3540299c49785e3c86755a5fe22db945_Out_0_Float = _EmissionMapUVRotation;
                    float4 _Property_4af1e92a383f585dbcf0fb0884888164_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_EmissionMapColor) : _EmissionMapColor;
                    float _Property_10781681264d5230974041a749fa166e_Out_0_Float = _EmissionMapColorIntensity;
                    float _Property_6b0f493dcf575d0ab56773dc813aefa6_Out_0_Float = _EmissionAlphaIntensity;
                    UnityTexture2D _Property_d61bfdcc9d345709b6c4e37db8d8dc9d_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_ColorBlendMap, sampler_ColorBlendMap, _ColorBlendMap_TexelSize, _ColorBlendMap_ST, float4(0, 0, 0, 0));
                    float _Property_30534193d46e557494c8f844a330b5b4_Out_0_Float = _ColorBlendMap_Toggle;
                    float4 _Property_36317ab840af5f6db269cbc1733a86db_Out_0_Vector4 = _ColorBlendMapOffset;
                    float4 _Property_e78fc93d059558309f51829a464ff1ed_Out_0_Vector4 = _ColorBlendVec;
                    float4 _Property_a0161d17445e53a18e11ae4102584287_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_ColorBlendColor) : _ColorBlendColor;
                    float _Property_aa8cec6d7536592c83454ce0965f8b85_Out_0_Float = _ColorBlendColorIntensity;
                    UnityTexture2D _Property_2c26a69a61fc52a5939877dca4507b70_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_RampColorMap, sampler_RampColorMap, _RampColorMap_TexelSize, _RampColorMap_ST, float4(0, 0, 0, 0));
                    float _Property_668c759ef0f85daa97ec0fa2c458fd91_Out_0_Float = _RampColorToggle;
                    float _Property_0d0fcda8bbbf5e5dadddaf843017559f_Out_0_Float = _RampColorSourceMode;
                    float4 _Property_eb2d499a8f40574f91ca2bfbb37b0bf7_Out_0_Vector4 = _RampColorMapOffset;
                    float4 _Property_65576e0f99385f1fb9a7c67087c5b89d_Out_0_Vector4 = _RampColor0;
                    float4 _Property_55e82011a7bd54c7969e4044ab34a602_Out_0_Vector4 = _RampColor1;
                    float4 _Property_f5084aa30a6751de8d599e42fb5ffff5_Out_0_Vector4 = _RampColor2;
                    float4 _Property_8725b71298fd58698edbc87798ea15e9_Out_0_Vector4 = _RampColor3;
                    float4 _Property_04d0659f70de5bb8bf5972d05a3b68c8_Out_0_Vector4 = _RampColor4;
                    float4 _Property_4989a100e056527b92055a3c516a4eab_Out_0_Vector4 = _RampColor5;
                    float4 _Property_77bf5e95cff652cdaaa6bf93adeb0f93_Out_0_Vector4 = _RampColorAlpha0;
                    float4 _Property_c5d14f129a11542581793506ad8ba24e_Out_0_Vector4 = _RampColorAlpha1;
                    float4 _Property_e84364b4aa05540ebb986ab49893b436_Out_0_Vector4 = _RampColorAlpha2;
                    float _Property_7f00ce17ce115d2bb969973b132fe3e8_Out_0_Float = _RampColorCount;
                    float4 _Property_89e65c225052579ea3ac6095a234f65a_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_RampColorBlendColor) : _RampColorBlendColor;
                    float _Property_4750fb93d1d1562bad31b25dec3f8b25_Out_0_Float = _HueShift;
                    float _Property_5e6a32f15bf05438b9cc108706ad0625_Out_0_Float = _Contrast;
                    float4 _Property_866cffb762935a229d1b940e1f8b905c_Out_0_Vector4 = _ContrastMidColor;
                    float _Property_f97ecb43f95c5e37b841c32f67fa546e_Out_0_Float = _Saturability;
                    float4 _Property_713b0ac7fdd85eb2a23076a581e94638_Out_0_Vector4 = _BaseMapColorRefine;
                    float _Property_2fd1783a3bdc52f88effab68b88e0a47_Out_0_Float = _fresnelEnabled;
                    float4 _Property_760680a31d7b57b3a03b703fedd7dea7_Out_0_Vector4 = _FresnelUnit;
                    float4 _Property_2e33a988d6ea5eaea31eb783d2cb2026_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_FresnelColor) : _FresnelColor;
                    float4 _Property_990aa20b0fbe58bab561e7a26ecc62ad_Out_0_Vector4 = _FresnelRotation;
                    float4 _Property_e5767a3f300954768906d034fee505b2_Out_0_Vector4 = _Dissolve_Vec2;
                    float4 _Property_28013c8ed2865dbba415684e772fbe2f_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_DissolveLineColor) : _DissolveLineColor;
                    float _Property_872303b3673f5db6ba39a80132854337_Out_0_Float = _Dissolve_useRampMap_Toggle;
                    UnityTexture2D _Property_f712ace2c0ec59aca6e4ea56a5dc642b_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_DissolveRampMap, sampler_DissolveRampMap, _DissolveRampMap_TexelSize, _DissolveRampMap_ST, float4(0, 0, 0, 0));
                    float _Property_3f274a4912255dc2aa6e18641c408bd6_Out_0_Float = _DissolveRampSourceMode;
                    float4 _Property_2d934283df7757faadf21ff2aa713bb4_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_DissolveRampColor) : _DissolveRampColor;
                    float _Property_d2afb0df25d55430acab801c8a3dbfde_Out_0_Float = _DissolveRampCount;
                    float4 _Property_0dec3b42d61b5e7c862c4ca295f9e208_Out_0_Vector4 = _DissolveRampColor0;
                    float4 _Property_4b091596ce61549b96d0e88967383f48_Out_0_Vector4 = _DissolveRampColor1;
                    float4 _Property_e246b4e0fa405aca8b55e192c06587a0_Out_0_Vector4 = _DissolveRampColor2;
                    float4 _Property_7a5d7978834d5d4eb9428c0a196dfbd3_Out_0_Vector4 = _DissolveRampColor3;
                    float4 _Property_6ab07b2800785722806fc80f0a180a5e_Out_0_Vector4 = _DissolveRampColor4;
                    float4 _Property_5250e3a06c0252d6a6bb9743ea2f0ac1_Out_0_Vector4 = _DissolveRampColor5;
                    float4 _Property_64999d5c415b5a73b4a0c204bcf29377_Out_0_Vector4 = _DissolveRampAlpha0;
                    float4 _Property_a934aa24a47f5da2992d3e468dbd374d_Out_0_Vector4 = _DissolveRampAlpha1;
                    float4 _Property_629063539c105cb3a2e8d3eade7eefb1_Out_0_Vector4 = _DissolveRampAlpha2;
                    float _Property_fc9eb8a77e24500ab5421051111d6fda_Out_0_Float = _NB_ForceNoMipFlagsLo16;
                    float _Property_6311ff30de835a0da66e70c41e5eb4a9_Out_0_Float = _NB_ForceNoMipFlagsHi16;
                    float _Property_9439295f5abe51489892e22adc16c2b7_Out_0_Float = _NB_DissolveRampSTOverrideEnabled;
                    float4 _Property_e0d280ecac5f56c48b444399bdfee24d_Out_0_Vector4 = _NB_DissolveRampSTOverride;
                    float _Property_d24b5ab9f5354781acf90544fdd34e1a_Out_0_Float = _DistanceFade_Toggle;
                    float4 _Property_d525ffde86414d429eb7d33f95dbc125_Out_0_Vector4 = _Fade;
                    float _Property_2f1d59a7ccfd4e21bdcabf18c881577c_Out_0_Float = _SoftParticlesEnabled;
                    float4 _Property_c04e77b9696340d1b77e0c6a9910ffae_Out_0_Vector4 = _SoftParticleFadeParams;
                    float4 _ScreenPosition_86fbeeb2af2346549e9611c0551ed415_Out_0_Vector4 = float4(IN.NDCPosition.xy, 0, 0);
                    float _Property_67f9358d1a79442eb25e5a4b980feddb_Out_0_Float = _DepthOutline_Toggle;
                    float4 _Property_75062f76b29a4f08b37655dc233735d6_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_DepthOutline_Color) : _DepthOutline_Color;
                    float4 _Property_8fbc75cf62cb493ea789465b0e094396_Out_0_Vector4 = _DepthOutline_Vec;
                    UnityTexture2D _Property_8f072da64b1f45d992849b1e47b91c14_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_BaseMap, sampler_BaseMap, _BaseMap_TexelSize, float4(1, 1, 0, 0), float4(0, 0, 0, 0));
                    UnityTexture2D _Property_a9a873c1f9545377b6d5603870d8ce7e_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_NoiseMap, sampler_NoiseMap, _NoiseMap_TexelSize, _NoiseMap_ST, float4(0, 0, 0, 0));
                    float _Property_05cfd7336ce55efda9ba538458718e94_Out_0_Float = _noisemapEnabled;
                    float _Property_de00f2b38f8d559eb2e11b9a8afbf0ab_Out_0_Float = _NoiseMapUVRotation;
                    float4 _Property_c1523df23e3f5ea7a2816bc39b189b9d_Out_0_Vector4 = _NoiseOffset;
                    float _Property_6745a2aa39ce5a6db6ef4145ce16fc4d_Out_0_Float = _NoiseIntensity;
                    float4 _Property_0692aaad7ae555198da5d2971b415df9_Out_0_Vector4 = _DistortionDirection;
                    UnityTexture2D _Property_7a1c57aac1ae5322904a7fbc9050f2ce_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_NoiseMaskMap, sampler_NoiseMaskMap, _NoiseMaskMap_TexelSize, _NoiseMaskMap_ST, float4(0, 0, 0, 0));
                    float _Property_931f026875de5593b8c7d6f47800f865_Out_0_Float = _noiseMaskMap_Toggle;
                    float _Property_cff65480aa2c5e98b542088b70270554_Out_0_Float = _TexDistortion_intensity;
                    float _Property_5f7732a02ece5e6fb25d3e1ecc105818_Out_0_Float = _Emi_Distortion_intensity;
                    float _Property_b04226779a985e25a46ae050e8c15dcc_Out_0_Float = _MaskDistortion_intensity;
                    float _Property_415818c2850c582fa48e2026af7e309e_Out_0_Float = _MatCapToggle;
                    UnityTexture2D _Property_53f494020d7651608ba56c8922ff1ab9_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_MatCapTex, sampler_MatCapTex, float4(1, 1, 1, 1), float4(1, 1, 0, 0), float4(0, 0, 0, 0));
                    float4 _Property_6df429d0561957cb84dfad2ee12aeb46_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_MatCapColor) : _MatCapColor;
                    float4 _Property_5c8f0472975b5156a2b33799226739c6_Out_0_Vector4 = _MatCapInfo;
                    float _Property_d2d631afc2c1544b83cb34681aa85a03_Out_0_Float = _BumpMapToggle;
                    UnityTexture2D _Property_8a8ba308b2a1563ea8c2419c29c5c7b9_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_BumpTex, sampler_BumpTex, float4(1, 1, 1, 1), _BumpTex_ST, float4(0, 0, 0, 0));
                    float _Property_6d9934d6abfd5c0697171ddfd0c97fdc_Out_0_Float = _BumpScale;
                    float _Property_e5ae484542c256ef9e2c843d58514935_Out_0_Float = _FxLightMode;
                    float4 _Property_6301978f07ae543781068ab22b637a6c_Out_0_Vector4 = _MaterialInfo;
                    float4 _Property_9f86734760a45426a1b7d524232798ab_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_SpecularColor) : _SpecularColor;
                    float _Property_665d05cdb47f5ac3b55f9494eebcfd24_Out_0_Float = _ProgramNoise_Toggle;
                    float _Property_97cf65b2fe9f579a9efe26a73d830bb9_Out_0_Float = _ProgramNoise_Simple_Toggle;
                    float _Property_46cf15a5712c5271ab621a4945709213_Out_0_Float = _ProgramNoise_Voronoi_Toggle;
                    float _Property_4d5ce3c87f6b5b5eb6b5488a205f5da2_Out_0_Float = _ProgramNoise_Rotate;
                    float4 _Property_73408a6e8749509dbf03b29118ea169a_Out_0_Vector4 = _DissolveVoronoi_Vec;
                    float4 _Property_4c7f65f090b75d33a899f4860a5eb28c_Out_0_Vector4 = _DissolveVoronoi_Vec2;
                    float4 _Property_cc5eb5485f3f503e991dd48cda2773d8_Out_0_Vector4 = _DissolveVoronoi_Vec3;
                    float4 _Property_ea6439cfbd84510292b048fc6619980b_Out_0_Vector4 = _DissolveVoronoi_Vec4;
                    float _Property_ead7fba0b75559f78e76a0fbf67cae12_Out_0_Float = _ProgramNoiseBaseBlendOpacity;
                    float _Property_13f2b053fdb9593aa51c8639f809a891_Out_0_Float = _MaskPNoiseBlendOpacity;
                    float _Property_9767ed7b0d615801a6878b0fc416e6f0_Out_0_Float = _DissolvePNoiseBlendOpacity;
                    float _Property_3750f20ccf7b5a7fb1d9e4bd3fb2b6ec_Out_0_Float = _NB_PNoiseBlendLo16;
                    float _Property_5d371e5c8c7459f280e4945d8a055cdc_Out_0_Float = _NB_PNoiseBlendHi16;
                    UnityTexture2D _Property_c125096410c15c18bbff38461fb105d3_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_RigRTBk, sampler_RigRTBk, float4(1, 1, 1, 1), float4(1, 1, 0, 0), float4(0, 0, 0, 0));
                    UnityTexture2D _Property_38036157e2ec5c659072d3660c785123_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_RigLBtF, sampler_RigLBtF, float4(1, 1, 1, 1), float4(1, 1, 0, 0), float4(0, 0, 0, 0));
                    UnityTexture2D _Property_269c61b4d8fe5a7684fc52010ece7432_Out_0_Texture2D = UnityBuildTexture2DStructInternal(_SixWayEmissionRamp, sampler_SixWayEmissionRamp, float4(1, 1, 1, 1), float4(1, 1, 0, 0), float4(0, 0, 0, 0));
                    float4 _Property_89139d6b0c285de9a58e1f6df2e7d236_Out_0_Vector4 = _SixWayInfo;
                    float4 _Property_547327d8b0f75f868f2ecda76c814b21_Out_0_Vector4 = IsGammaSpace() ? LinearToSRGB(_SixWayEmissionColor) : _SixWayEmissionColor;
                    float _Property_d2fe351137c85eac9a617d3f05ff92b2_Out_0_Float = _SixWayColorAbsorptionToggle;
                    float _Property_25cf481f9d865f129398a908a10a4f11_Out_0_Float = _DistortPNoiseBlendOpacity;
                    float4 _NBGraphBaseColorCustomFunction_f712bc264a1c40848edc2dd476a4cada_Out_3_Vector4;
                    float2 _NBGraphBaseColorCustomFunction_f712bc264a1c40848edc2dd476a4cada_NBDistortionSignedRG_145_Vector2;
                    float _NBGraphBaseColorCustomFunction_f712bc264a1c40848edc2dd476a4cada_NBDistortionNoiseMask_146_Float;
                    NBGraphBaseColor_float(_NBGraphBaseColorCustomFunction_f712bc264a1c40848edc2dd476a4cada_SampledAlbedo_0_Vector4, _NBGraphBaseColorCustomFunction_f712bc264a1c40848edc2dd476a4cada_SelectedAlpha_1_Float, _Property_13840ad57da543e2b33275638ce1189e_Out_0_Vector4, _Property_eafd46b75a1746f093439de914a3717c_Out_0_Float, _Property_02448b2e80a0440b804602e588eb2727_Out_0_Float, _Property_52b95ce95e564cb083a7953179be1554_Out_0_Float, _Property_6abc2ac395a54d7f99af7850796b2c20_Out_0_Float, _Property_4fbc64ac55184ee8b2348b861c93db8b_Out_0_Vector2, _Property_9099f47df6d24486a9e0bb9dd8fa7cf8_Out_0_Float, _Property_01827f8348b040028326054a1623cc84_Out_0_Float, _Property_87436152ca164e73877206654813b186_Out_0_Float, _Property_9aab3b8bff1a4b39a348ae3ea124a0e3_Out_0_Float, _Property_a31f37f37774434e8b5383ecef8f5072_Out_0_Float, _Property_e1e8e8a73a5f5e1c9e969abcb9782cd2_Out_0_Texture2D, _Property_26acbca87fca52f4a5ea05d6fcf09e48_Out_0_Float, _Property_c7f5afaa32df59eb9c6b2561b57aa26f_Out_0_Vector4, _Property_6e138238299250348f63a5ee2f1f3d5b_Out_0_Vector4, _Property_20f932f599b85641aeb7c2ea908ed484_Out_0_Float, _Property_b4f86e887f26523f845f932a27414b5b_Out_0_Texture2D, _Property_a70b9f8477c75d2a98fd83c15f287a37_Out_0_Float, _Property_a05025df3a5c534f88355f30cb99d2d3_Out_0_Vector4, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_MaskUV_22_Vector2, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_DissolveUV_26_Vector2, _Property_c4d2b9d87084415fb9b51e578a0b9080_Out_0_Float, _Property_43ed7c33980543efb0a5f6f413791461_Out_0_Float, _Property_63ba51c6efaf4d659d2b6b921e7f1a43_Out_0_Vector4, _Property_f9f92343294e499b8f4512de6aeefa5c_Out_0_Vector4, _Property_a5f2eeafb2c748ed9a1ec290960ab16f_Out_0_Texture2D, _Property_56d752220cbf4d96ab46764fc9aebb16_Out_0_Float, _Property_e1702b0dd4cf43f18d778429097ef342_Out_0_Float, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_DissolveMaskUV_27_Vector2, _Property_a6ea548184d045a89325bc16be701913_Out_0_Texture2D, _Property_7c78f411c5ff4fac85800c00002c0fbc_Out_0_Float, _Property_f9394d12cb9a412e8858ece414106043_Out_0_Texture2D, _Property_7e16bb82a3584c73a47c9dcc897f62a3_Out_0_Float, _Property_282301309355467f98c2a0ca1c46538f_Out_0_Vector4, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_Mask2UV_23_Vector2, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_Mask3UV_24_Vector2, _Property_901675ce420344bb8ef7c1598f2a8c6c_Out_0_Float, _Property_84ab548e17414d57ab8ab8e23e9c9a5d_Out_0_Float, _Property_0a9b0dca2b7846869eb548347e06dfe0_Out_0_Float, _Property_bceef4027b6c4cc4b1d21c40d1e6b380_Out_0_Vector4, _Property_c8f145683c5e4bc2b543907d534d50ac_Out_0_Vector4, _Property_86555f566d7a4e47ad576dcbd5832f93_Out_0_Vector4, _Property_3e87b9d2a706453cbd9014c75528eaa0_Out_0_Float, _Property_53483bdbc2c74a8b824ea143d6eaec2b_Out_0_Vector4, _Property_4f05348fd4524abc964972190787b609_Out_0_Vector4, _Property_6235b1011ee0450c80fec0958cf7b4d4_Out_0_Vector4, _Property_a6bf239ac9f240709d42314fc1efd7b1_Out_0_Float, _Property_bcd09d9bc52b4a4f9cb57238c0902210_Out_0_Vector4, _Property_f14563d83f83427a8730d060aca57e2e_Out_0_Vector4, _Property_43d61a210ee74ffda3c292b0848511af_Out_0_Vector4, _Property_5cdf8c4f8e5f4271bf734cfe073fcc52_Out_0_Float, _Property_adbdd97beb12402b87648f77f30ab4c0_Out_0_Vector4, IN.VertexColor, _Property_442020c8e09d56679b49c301f56b94a4_Out_0_Float, _Property_b2641bfb6535514caebe85f35a335537_Out_0_Vector4, ((float) _IsFrontFace_d1919b09d5db5f54aaa1b20fe1d91dbf_Out_0_Boolean), _Property_92eefc72bf7f56fd8f8e4084e22172be_Out_0_Texture2D, _Property_318d04aaa2195ad4b539c26b2c04eeac_Out_0_Float, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_EmissionUV_25_Vector2, _Property_7d76134eb0065dcf88906e1e451e441f_Out_0_Vector4, _Property_3540299c49785e3c86755a5fe22db945_Out_0_Float, _Property_4af1e92a383f585dbcf0fb0884888164_Out_0_Vector4, _Property_10781681264d5230974041a749fa166e_Out_0_Float, _Property_6b0f493dcf575d0ab56773dc813aefa6_Out_0_Float, _Property_d61bfdcc9d345709b6c4e37db8d8dc9d_Out_0_Texture2D, _Property_30534193d46e557494c8f844a330b5b4_Out_0_Float, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_ColorBlendUV_28_Vector2, _Property_36317ab840af5f6db269cbc1733a86db_Out_0_Vector4, _Property_e78fc93d059558309f51829a464ff1ed_Out_0_Vector4, _Property_a0161d17445e53a18e11ae4102584287_Out_0_Vector4, _Property_aa8cec6d7536592c83454ce0965f8b85_Out_0_Float, _Property_2c26a69a61fc52a5939877dca4507b70_Out_0_Texture2D, _Property_668c759ef0f85daa97ec0fa2c458fd91_Out_0_Float, _Property_0d0fcda8bbbf5e5dadddaf843017559f_Out_0_Float, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_RampColorUV_29_Vector2, _Property_eb2d499a8f40574f91ca2bfbb37b0bf7_Out_0_Vector4, _Property_65576e0f99385f1fb9a7c67087c5b89d_Out_0_Vector4, _Property_55e82011a7bd54c7969e4044ab34a602_Out_0_Vector4, _Property_f5084aa30a6751de8d599e42fb5ffff5_Out_0_Vector4, _Property_8725b71298fd58698edbc87798ea15e9_Out_0_Vector4, _Property_04d0659f70de5bb8bf5972d05a3b68c8_Out_0_Vector4, _Property_4989a100e056527b92055a3c516a4eab_Out_0_Vector4, _Property_77bf5e95cff652cdaaa6bf93adeb0f93_Out_0_Vector4, _Property_c5d14f129a11542581793506ad8ba24e_Out_0_Vector4, _Property_e84364b4aa05540ebb986ab49893b436_Out_0_Vector4, _Property_7f00ce17ce115d2bb969973b132fe3e8_Out_0_Float, _Property_89e65c225052579ea3ac6095a234f65a_Out_0_Vector4, _Property_4750fb93d1d1562bad31b25dec3f8b25_Out_0_Float, _Property_5e6a32f15bf05438b9cc108706ad0625_Out_0_Float, _Property_866cffb762935a229d1b940e1f8b905c_Out_0_Vector4, _Property_f97ecb43f95c5e37b841c32f67fa546e_Out_0_Float, _Property_713b0ac7fdd85eb2a23076a581e94638_Out_0_Vector4, _Property_2fd1783a3bdc52f88effab68b88e0a47_Out_0_Float, _Property_760680a31d7b57b3a03b703fedd7dea7_Out_0_Vector4, _Property_2e33a988d6ea5eaea31eb783d2cb2026_Out_0_Vector4, _Property_990aa20b0fbe58bab561e7a26ecc62ad_Out_0_Vector4, IN.WorldSpaceNormal, IN.WorldSpaceViewDirection, _Property_e5767a3f300954768906d034fee505b2_Out_0_Vector4, _Property_28013c8ed2865dbba415684e772fbe2f_Out_0_Vector4, _Property_872303b3673f5db6ba39a80132854337_Out_0_Float, _Property_f712ace2c0ec59aca6e4ea56a5dc642b_Out_0_Texture2D, _Property_3f274a4912255dc2aa6e18641c408bd6_Out_0_Float, _Property_2d934283df7757faadf21ff2aa713bb4_Out_0_Vector4, _Property_d2afb0df25d55430acab801c8a3dbfde_Out_0_Float, _Property_0dec3b42d61b5e7c862c4ca295f9e208_Out_0_Vector4, _Property_4b091596ce61549b96d0e88967383f48_Out_0_Vector4, _Property_e246b4e0fa405aca8b55e192c06587a0_Out_0_Vector4, _Property_7a5d7978834d5d4eb9428c0a196dfbd3_Out_0_Vector4, _Property_6ab07b2800785722806fc80f0a180a5e_Out_0_Vector4, _Property_5250e3a06c0252d6a6bb9743ea2f0ac1_Out_0_Vector4, _Property_64999d5c415b5a73b4a0c204bcf29377_Out_0_Vector4, _Property_a934aa24a47f5da2992d3e468dbd374d_Out_0_Vector4, _Property_629063539c105cb3a2e8d3eade7eefb1_Out_0_Vector4, _Property_fc9eb8a77e24500ab5421051111d6fda_Out_0_Float, _Property_6311ff30de835a0da66e70c41e5eb4a9_Out_0_Float, _Property_9439295f5abe51489892e22adc16c2b7_Out_0_Float, _Property_e0d280ecac5f56c48b444399bdfee24d_Out_0_Vector4, _Property_d24b5ab9f5354781acf90544fdd34e1a_Out_0_Float, _Property_d525ffde86414d429eb7d33f95dbc125_Out_0_Vector4, IN.ViewSpacePosition, _Property_2f1d59a7ccfd4e21bdcabf18c881577c_Out_0_Float, _Property_c04e77b9696340d1b77e0c6a9910ffae_Out_0_Vector4, _ScreenPosition_86fbeeb2af2346549e9611c0551ed415_Out_0_Vector4, _Property_67f9358d1a79442eb25e5a4b980feddb_Out_0_Float, _Property_75062f76b29a4f08b37655dc233735d6_Out_0_Vector4, _Property_8fbc75cf62cb493ea789465b0e094396_Out_0_Vector4, _Property_8f072da64b1f45d992849b1e47b91c14_Out_0_Texture2D, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_Out_5_Vector2, _Property_a9a873c1f9545377b6d5603870d8ce7e_Out_0_Texture2D, _Property_05cfd7336ce55efda9ba538458718e94_Out_0_Float, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_NoiseUV_30_Vector2, _Property_de00f2b38f8d559eb2e11b9a8afbf0ab_Out_0_Float, _Property_c1523df23e3f5ea7a2816bc39b189b9d_Out_0_Vector4, _Property_6745a2aa39ce5a6db6ef4145ce16fc4d_Out_0_Float, _Property_0692aaad7ae555198da5d2971b415df9_Out_0_Vector4, _Property_7a1c57aac1ae5322904a7fbc9050f2ce_Out_0_Texture2D, _Property_931f026875de5593b8c7d6f47800f865_Out_0_Float, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_NoiseMaskUV_31_Vector2, _Property_cff65480aa2c5e98b542088b70270554_Out_0_Float, _Property_5f7732a02ece5e6fb25d3e1ecc105818_Out_0_Float, _Property_b04226779a985e25a46ae050e8c15dcc_Out_0_Float, _Property_415818c2850c582fa48e2026af7e309e_Out_0_Float, _Property_53f494020d7651608ba56c8922ff1ab9_Out_0_Texture2D, _Property_6df429d0561957cb84dfad2ee12aeb46_Out_0_Vector4, _Property_5c8f0472975b5156a2b33799226739c6_Out_0_Vector4, _Property_d2d631afc2c1544b83cb34681aa85a03_Out_0_Float, _Property_8a8ba308b2a1563ea8c2419c29c5c7b9_Out_0_Texture2D, _Property_6d9934d6abfd5c0697171ddfd0c97fdc_Out_0_Float, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_BumpUV_32_Vector2, IN.WorldSpaceTangent, IN.WorldSpaceBiTangent, _Property_e5ae484542c256ef9e2c843d58514935_Out_0_Float, _Property_6301978f07ae543781068ab22b637a6c_Out_0_Vector4, _Property_9f86734760a45426a1b7d524232798ab_Out_0_Vector4, IN.WorldSpacePosition, _NBGraphBaseUVCustomFunction_3a9a7297f5dc4c46a1b595a6709c6f26_ProgramNoiseUV_33_Vector2, _Property_665d05cdb47f5ac3b55f9494eebcfd24_Out_0_Float, _Property_97cf65b2fe9f579a9efe26a73d830bb9_Out_0_Float, _Property_46cf15a5712c5271ab621a4945709213_Out_0_Float, _Property_4d5ce3c87f6b5b5eb6b5488a205f5da2_Out_0_Float, _Property_73408a6e8749509dbf03b29118ea169a_Out_0_Vector4, _Property_4c7f65f090b75d33a899f4860a5eb28c_Out_0_Vector4, _Property_cc5eb5485f3f503e991dd48cda2773d8_Out_0_Vector4, _Property_ea6439cfbd84510292b048fc6619980b_Out_0_Vector4, _Property_ead7fba0b75559f78e76a0fbf67cae12_Out_0_Float, _Property_13f2b053fdb9593aa51c8639f809a891_Out_0_Float, _Property_9767ed7b0d615801a6878b0fc416e6f0_Out_0_Float, _Property_3750f20ccf7b5a7fb1d9e4bd3fb2b6ec_Out_0_Float, _Property_5d371e5c8c7459f280e4945d8a055cdc_Out_0_Float, _Property_c125096410c15c18bbff38461fb105d3_Out_0_Texture2D, _Property_38036157e2ec5c659072d3660c785123_Out_0_Texture2D, _Property_269c61b4d8fe5a7684fc52010ece7432_Out_0_Texture2D, _Property_89139d6b0c285de9a58e1f6df2e7d236_Out_0_Vector4, _Property_547327d8b0f75f868f2ecda76c814b21_Out_0_Vector4, _Property_d2fe351137c85eac9a617d3f05ff92b2_Out_0_Float, IN.SixBake0, IN.SixBake1, IN.SixBake2, IN.SixBack0, IN.SixBack1, IN.SixBack2, IN.SixTangentSigned, _Property_25cf481f9d865f129398a908a10a4f11_Out_0_Float, _NBGraphBaseColorCustomFunction_f712bc264a1c40848edc2dd476a4cada_Out_3_Vector4, _NBGraphBaseColorCustomFunction_f712bc264a1c40848edc2dd476a4cada_NBDistortionSignedRG_145_Vector2, _NBGraphBaseColorCustomFunction_f712bc264a1c40848edc2dd476a4cada_NBDistortionNoiseMask_146_Float);
                    float _Swizzle_52d10f3207ae43dc9038120fea622ca3_Out_1_Float = _NBGraphBaseColorCustomFunction_f712bc264a1c40848edc2dd476a4cada_Out_3_Vector4.w;
                    float _Property_b2c9094aad4345b78e06cfbf0854a2bd_Out_0_Float = _Cutoff;
                    surface.BaseColor = (_NBGraphBaseColorCustomFunction_f712bc264a1c40848edc2dd476a4cada_Out_3_Vector4.xyz);
                    surface.Alpha = _Swizzle_52d10f3207ae43dc9038120fea622ca3_Out_1_Float;
                    surface.AlphaClipThreshold = _Property_b2c9094aad4345b78e06cfbf0854a2bd_Out_0_Float;
                    return surface;
                }
            
            // --------------------------------------------------
            // Build Graph Inputs
            #ifdef HAVE_VFX_MODIFICATION
            #define VFX_SRP_ATTRIBUTES Attributes
            #define VFX_SRP_VARYINGS Varyings
            #define VFX_SRP_SURFACE_INPUTS SurfaceDescriptionInputs
            #endif
            VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
                {
                    VertexDescriptionInputs output;
                    ZERO_INITIALIZE(VertexDescriptionInputs, output);
                
                    output.ObjectSpaceNormal =                          input.normalOS;
                    output.WorldSpaceNormal =                           TransformObjectToWorldNormal(input.normalOS);
                    output.ObjectSpaceTangent =                         input.tangentOS.xyz;
                    output.WorldSpaceTangent =                          TransformObjectToWorldDir(input.tangentOS.xyz);
                    output.ObjectSpaceBiTangent =                       normalize(cross(input.normalOS, input.tangentOS.xyz) * (input.tangentOS.w > 0.0f ? 1.0f : -1.0f) * GetOddNegativeScale());
                    output.WorldSpaceBiTangent =                        TransformObjectToWorldDir(output.ObjectSpaceBiTangent);
                    output.ObjectSpacePosition =                        input.positionOS;
                    output.uv0 =                                        input.uv0;
                    output.uv1 =                                        input.uv1;
                    output.uv2 =                                        input.uv2;
                    output.VertexColor =                                input.color;
                #if UNITY_ANY_INSTANCING_ENABLED
                #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
                #endif
                
                    return output;
                }
                SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
                {
                    SurfaceDescriptionInputs output;
                    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
                
                #ifdef HAVE_VFX_MODIFICATION
                #if VFX_USE_GRAPH_VALUES
                    uint instanceActiveIndex = asuint(UNITY_ACCESS_INSTANCED_PROP(PerInstance, _InstanceActiveIndex));
                    /* WARNING: $splice Could not find named fragment 'VFXLoadGraphValues' */
                #endif
                    /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
                
                #endif
                
                    output.SixBake0 = input.SixBake0;
                output.SixBake1 = input.SixBake1;
                output.SixBake2 = input.SixBake2;
                output.SixBack0 = input.SixBack0;
                output.SixBack1 = input.SixBack1;
                output.SixBack2 = input.SixBack2;
                output.SixTangentSigned = input.SixTangentSigned;
                
                    // must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
                    float3 unnormalizedNormalWS = input.normalWS;
                    const float renormFactor = 1.0 / length(unnormalizedNormalWS);
                
                    // use bitangent on the fly like in hdrp
                    // IMPORTANT! If we ever support Flip on double sided materials ensure bitangent and tangent are NOT flipped.
                    float crossSign = (input.tangentWS.w > 0.0 ? 1.0 : -1.0)* GetOddNegativeScale();
                    float3 bitang = crossSign * cross(input.normalWS.xyz, input.tangentWS.xyz);
                
                    output.WorldSpaceNormal = renormFactor * input.normalWS.xyz;      // we want a unit length Normal Vector node in shader graph
                
                    // to pr               eserve mikktspace compliance we use same scale renormFactor as was used on the normal.
                    // This                is explained in section 2.2 in "surface gradient based bump mapping framework"
                    output.WorldSpaceTangent = renormFactor * input.tangentWS.xyz;
                    output.WorldSpaceBiTangent = renormFactor * bitang;
                
                    output.WorldSpaceViewDirection = GetWorldSpaceNormalizeViewDir(input.positionWS);
                    output.WorldSpacePosition = input.positionWS;
                    output.ViewSpacePosition = TransformWorldToView(input.positionWS);
                
                    #if UNITY_UV_STARTS_AT_TOP
                    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x < 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
                    #else
                    output.PixelPosition = float2(input.positionCS.x, (_ProjectionParams.x > 0) ? (_ScaledScreenParams.y - input.positionCS.y) : input.positionCS.y);
                    #endif
                
                    output.NDCPosition = output.PixelPosition.xy / _ScaledScreenParams.xy;
                    output.NDCPosition.y = 1.0f - output.NDCPosition.y;
                
                    output.uv0 = input.texCoord0;
                    output.uv1 = input.texCoord1;
                    output.uv2 = input.texCoord2;
                    output.VertexColor = input.color;
                #if UNITY_ANY_INSTANCING_ENABLED
                #else // TODO: XR support for procedural instancing because in this case UNITY_ANY_INSTANCING_ENABLED is not defined and instanceID is incorrect.
                #endif
                #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
                #else
                #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                #endif
                    BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
                
                        return output;
                }
                
            // --------------------------------------------------
            // Main
            
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/UnlitGBufferPass.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GBufferOutputFormat.hlsl"
            
            // --------------------------------------------------
            // Visual Effect Vertex Invocations
            #ifdef HAVE_VFX_MODIFICATION
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/VisualEffectVertex.hlsl"
            #endif
            
            ENDHLSL
            }
        }
        CustomEditor "UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI"
        CustomEditorForRenderPipeline "NBShaderEditor.NBShaderGraphGUI" "UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset"
        FallBack "Hidden/Shader Graph/FallbackError"
    }