#ifndef SIX_WAY_SMOKE_LIT_HLSL
#define SIX_WAY_SMOKE_LIT_HLSL
//这部分尽量借鉴 UnityEditor.VFX.HDRP.SixWaySmokeLit

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "ParticlesUnlitInputNew.hlsl"

// Generated from UnityEditor.VFX.HDRP.SixWaySmokeLit+BSDFData
// PackingRules = Exact
struct BSDFData
{
    uint materialFeatures;
    float absorptionRange;
    real4 diffuseColor;
    real3 fresnel0;
    real ambientOcclusion;
    float3 normalWS;
    float4 tangentWS;
    real3 geomNormalWS;
    real3 rigRTBk;
    real3 rigLBtF;
    real3 bakeDiffuseLighting0;//rigRTBk.x
    real3 bakeDiffuseLighting1;//rigRTBk.y
    real3 bakeDiffuseLighting2;// bsdfData.tangentWS.w > 0.0f ? rigRTBk.z : rigLBtF.z
    real3 backBakeDiffuseLighting0;//rigLBtF.x
    real3 backBakeDiffuseLighting1;//rigLBtF.y
    real3 backBakeDiffuseLighting2;// bsdfData.tangentWS.w > 0.0f ? rigLBtF.z : rigRTBk.z

    //-----NBShaders-----
    real3 emission;
    real emissionInput;//NegativeTex.a
    real alpha;//PositiveTex.a
    
};

#define ABSORPTION_EPSILON max(REAL_MIN, 1e-5)

real3 ComputeDensityScales(real3 absorptionColor)
{
    absorptionColor.rgb = max(ABSORPTION_EPSILON, absorptionColor.rgb);

    // Empirical value used to parametrize absorption from color
    const real absorptionStrength = 0.2f;
    return 1.0f + log2(absorptionColor.rgb) / log2(absorptionStrength);
}

real3 GetTransmissionWithAbsorption(real transmission, real4 absorptionColor, real absorptionRange)
{
    #if defined(VFX_SIX_WAY_ABSORPTION)
        real3 densityScales = ComputeDensityScales(absorptionColor.rgb);

        #ifdef VFX_BLENDMODE_PREMULTIPLY
            absorptionRange *= (absorptionColor.a > 0) ? absorptionColor.a : 1.0f;
        #endif

        // real3 outTransmission = GetTransmissionWithAbsorption(transmission, densityScales, absorptionRange);
        real3 outTransmission = pow(saturate(transmission / absorptionRange), densityScales);
        outTransmission *= absorptionRange;

        return outTransmission;
    #else
        return transmission.xxx * absorptionColor.rgb; // simple multiply
    #endif
}

void ModifyBakedDiffuseLighting(BSDFData bsdfData, inout float3 bakeDiffuseLighting)
{
    bakeDiffuseLighting = 0;

    // Scale to be energy conserving: Total energy = 4*pi; divided by 6 directions
    float scale = 4.0f * PI / 6.0f;

    float3 frontBakeDiffuseLighting = bsdfData.tangentWS.w > 0.0f ? bsdfData.bakeDiffuseLighting2 : bsdfData.backBakeDiffuseLighting2;
    float3 backBakeDiffuseLighting = bsdfData.tangentWS.w > 0.0f ? bsdfData.backBakeDiffuseLighting2 : bsdfData.bakeDiffuseLighting2;

    float3x3 bakeDiffuseLightingMat;
    bakeDiffuseLightingMat[0] = bsdfData.bakeDiffuseLighting0;
    bakeDiffuseLightingMat[1] = bsdfData.bakeDiffuseLighting1;
    bakeDiffuseLightingMat[2] = frontBakeDiffuseLighting;
    bakeDiffuseLighting += GetTransmissionWithAbsorption(bsdfData.rigRTBk.x, bsdfData.diffuseColor, bsdfData.absorptionRange) * bakeDiffuseLightingMat[0];
    bakeDiffuseLighting += GetTransmissionWithAbsorption(bsdfData.rigRTBk.y, bsdfData.diffuseColor, bsdfData.absorptionRange) * bakeDiffuseLightingMat[1];
    bakeDiffuseLighting += GetTransmissionWithAbsorption(bsdfData.rigRTBk.z, bsdfData.diffuseColor, bsdfData.absorptionRange) * bakeDiffuseLightingMat[2];

    bakeDiffuseLightingMat[0] = bsdfData.backBakeDiffuseLighting0;
    bakeDiffuseLightingMat[1] = bsdfData.backBakeDiffuseLighting1;
    bakeDiffuseLightingMat[2] = backBakeDiffuseLighting;
    bakeDiffuseLighting += GetTransmissionWithAbsorption(bsdfData.rigLBtF.x, bsdfData.diffuseColor, bsdfData.absorptionRange) * bakeDiffuseLightingMat[0];
    bakeDiffuseLighting += GetTransmissionWithAbsorption(bsdfData.rigLBtF.y, bsdfData.diffuseColor, bsdfData.absorptionRange) * bakeDiffuseLightingMat[1];
    bakeDiffuseLighting += GetTransmissionWithAbsorption(bsdfData.rigLBtF.z, bsdfData.diffuseColor, bsdfData.absorptionRange) * bakeDiffuseLightingMat[2];

    bakeDiffuseLighting *= scale;
    
}

//世界空间到切线空间方向转换
float3 TransformToLocalFrame(float3 L, BSDFData bsdfData)
{
    float3 zVec = -bsdfData.normalWS;
    float3 xVec = bsdfData.tangentWS.xyz;
    float3 yVec = -cross(zVec, xVec) * bsdfData.tangentWS.w;//原代码没有负值，实际测试需要负值
    float3x3 tbn = float3x3(xVec, yVec, zVec);
    return mul(tbn, L);
}

CBSDF EvaluateBSDF(float3 L, BSDFData bsdfData)
{
    CBSDF cbsdf;
    ZERO_INITIALIZE(CBSDF, cbsdf);

    float3 dir = TransformToLocalFrame(L, bsdfData);
    float3 weights = dir >= 0 ? bsdfData.rigRTBk.xyz : bsdfData.rigLBtF.xyz;
    float3 sqrDir = dir*dir;

    cbsdf.diffR = GetTransmissionWithAbsorption(dot(sqrDir, weights), bsdfData.diffuseColor, bsdfData.absorptionRange);

    return cbsdf;
}


//这一步最好在面板上做完
half GetAbsorptionRange(float absorptionStrenth)
{
    return  INV_PI + saturate(absorptionStrenth) * (1 - INV_PI);
}




//---------NBShaderUtility-----------

//UseInVerTex
void GetSixWayBakeDiffuseLight(real3 normalWS,real3 tangentWS,real3 biTangentWS,
    inout  half3 bakeDiffuseLighting0,inout half3 bakeDiffuseLighting1,inout half3 bakeDiffuseLighting2,
    inout half3 backBakeDiffuseLighting0,inout half3 backBakeDiffuseLighting1,inout half3 backBakeDiffuseLighting2)
{
    bakeDiffuseLighting0 = SampleSHVertex(tangentWS);
    bakeDiffuseLighting1 = SampleSHVertex(biTangentWS);
    bakeDiffuseLighting2 = SampleSHVertex(-normalWS);
    backBakeDiffuseLighting0 = SampleSHVertex(-tangentWS);
    backBakeDiffuseLighting1 = SampleSHVertex(-biTangentWS);
    backBakeDiffuseLighting2 = SampleSHVertex(normalWS);
}

LightingData CreateSixWayLightingData(InputData inputData, half3 emission)
{
    LightingData lightingData;

    lightingData.giColor = inputData.bakedGI;
    lightingData.emissionColor = emission;
    lightingData.vertexLightingColor = 0;
    lightingData.mainLightColor = 0;
    lightingData.additionalLightsColor = 0;

    return lightingData;
}

void  GetSixWayEmission(inout  BSDFData bsdfData,Texture2D rampMap,half4 emissionColor,bool isRampMap)
{
    float input = pow(bsdfData.emissionInput,_SixWayInfo.y);
    half3 emission =  emissionColor * emissionColor.a;
    if (isRampMap)
    {
        half4 rampSample = rampMap.Sample(sampler_linear_clamp,half2(input,0.5));
        emission = emission * rampSample * rampSample.a;
    }
    else
    {
        emission *= input;
    }
    bsdfData.emission = emission;
}

half3 LightingSixWay(Light light,InputData inputData, BSDFData bsdfData)
{
    half3 cbsdf_R = EvaluateBSDF(light.direction,bsdfData).diffR;
    half3 radiance = light.color * light.distanceAttenuation * light.shadowAttenuation;
    return PI * cbsdf_R * radiance;
}


//光照流程--->原型为UniversalFragmentBlinnPhong
half4 UniversalFragmentSixWay(InputData inputData,BSDFData bsdfData)
{
    // #if defined(DEBUG_DISPLAY)
    // half4 debugColor;
    //
    // if (CanDebugOverrideOutputColor(inputData, surfaceData, debugColor))
    // {
    //     return debugColor;
    // }
    // #endif

#ifdef _LIGHT_LAYERS
    uint meshRenderingLayers = GetMeshRenderingLayer();
#endif
    half4 shadowMask = CalculateShadowMask(inputData);
    // AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(inputData, surfaceData);
    AmbientOcclusionFactor aoFactor;
    aoFactor.directAmbientOcclusion = 1;
    aoFactor.indirectAmbientOcclusion = 1;
    Light mainLight = GetMainLight(inputData, shadowMask, aoFactor);

    // MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI, aoFactor);

    // inputData.bakedGI *= surfaceData.albedo;

    // LightingData lightingData = CreateLightingData(inputData, surfaceData);
    LightingData lightingData = CreateSixWayLightingData(inputData,bsdfData.emission);
    
#ifdef _LIGHT_LAYERS
    if (IsMatchingLightLayer(mainLight.layerMask, meshRenderingLayers))
#endif
    {
        lightingData.mainLightColor += LightingSixWay(mainLight, inputData, bsdfData);
    }

    #if defined(_ADDITIONAL_LIGHTS)
    uint pixelLightCount = GetAdditionalLightsCount();

    #if USE_FORWARD_PLUS
    for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
    {
        FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK

        Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
#ifdef _LIGHT_LAYERS
        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
#endif
        {
            lightingData.additionalLightsColor += LightingSixWay(light, inputData, bsdfData);
        }
    }
    #endif

    LIGHT_LOOP_BEGIN(pixelLightCount)
        Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
#ifdef _LIGHT_LAYERS
        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
#endif
        {
            lightingData.additionalLightsColor += LightingSixWay(light, inputData, bsdfData);
        }
    LIGHT_LOOP_END
    #endif

    #if defined(_ADDITIONAL_LIGHTS_VERTEX)
    lightingData.vertexLightingColor += inputData.vertexLighting * surfaceData.albedo;
    #endif

    return CalculateFinalColor(lightingData, bsdfData.alpha);
}
half3 LightingHalfLambert(half3 lightColor, half3 lightDir, half3 normal)
{
    half NdotL = saturate(dot(normal, lightDir));
    NdotL = NdotL*0.5 + 0.5;
    return lightColor * NdotL;
}
half3 CalculateHalfLambertBlinnPhong(Light light, InputData inputData, SurfaceData surfaceData)
{
    half3 attenuatedLightColor = light.color * (light.distanceAttenuation * light.shadowAttenuation);
    half3 lightDiffuseColor = LightingHalfLambert(attenuatedLightColor, light.direction, inputData.normalWS);

    half3 lightSpecularColor = half3(0,0,0);
    #if defined(_SPECGLOSSMAP) || defined(_SPECULAR_COLOR)
    half smoothness = exp2(10 * surfaceData.smoothness + 1);

    lightSpecularColor += LightingSpecular(attenuatedLightColor, light.direction, inputData.normalWS, inputData.viewDirectionWS, half4(surfaceData.specular, 1), smoothness);
    #endif

    #if _ALPHAPREMULTIPLY_ON
    return lightDiffuseColor * surfaceData.albedo * surfaceData.alpha + lightSpecularColor;
    #else
    return lightDiffuseColor * surfaceData.albedo + lightSpecularColor;
    #endif
}
half4 UniversalFragmentHalfLambert(InputData inputData, half3 diffuse, half4 specularGloss, half smoothness, half3 emission, half alpha, half3 normalTS)
{

    SurfaceData surfaceData;

    surfaceData.albedo = diffuse;
    surfaceData.alpha = alpha;
    surfaceData.emission = emission;
    surfaceData.metallic = 0;
    surfaceData.occlusion = 1;
    surfaceData.smoothness = smoothness;
    surfaceData.specular = specularGloss.rgb;
    surfaceData.clearCoatMask = 0;
    surfaceData.clearCoatSmoothness = 1;
    surfaceData.normalTS = normalTS;

    
    // #if defined(DEBUG_DISPLAY)
    // half4 debugColor;
    //
    // if (CanDebugOverrideOutputColor(inputData, surfaceData, debugColor))
    // {
    //     return debugColor;
    // }
    // #endif

    #ifdef _LIGHT_LAYERS
    uint meshRenderingLayers = GetMeshRenderingLayer();
    #endif
    
    half4 shadowMask = CalculateShadowMask(inputData);
    AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(inputData, surfaceData);
    Light mainLight = GetMainLight(inputData, shadowMask, aoFactor);

    MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI, aoFactor);

    inputData.bakedGI *= surfaceData.albedo;

    LightingData lightingData = CreateLightingData(inputData, surfaceData);
#ifdef _LIGHT_LAYERS
    if (IsMatchingLightLayer(mainLight.layerMask, meshRenderingLayers))
#endif
    {
        lightingData.mainLightColor += CalculateHalfLambertBlinnPhong(mainLight, inputData, surfaceData);
    }

    #if defined(_ADDITIONAL_LIGHTS)
    uint pixelLightCount = GetAdditionalLightsCount();

    #if USE_FORWARD_PLUS
    for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
    {
        FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK

        Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
#ifdef _LIGHT_LAYERS
        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
#endif
        {
            lightingData.additionalLightsColor += CalculateBlinnPhong(light, inputData, surfaceData);
        }
    }
    #endif

    LIGHT_LOOP_BEGIN(pixelLightCount)
        Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);
#ifdef _LIGHT_LAYERS
        if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
#endif
        {
            lightingData.additionalLightsColor += CalculateHalfLambertBlinnPhong(light, inputData, surfaceData);
        }
    LIGHT_LOOP_END
    #endif

    #if defined(_ADDITIONAL_LIGHTS_VERTEX)
    lightingData.vertexLightingColor += inputData.vertexLighting * surfaceData.albedo;
    #endif

    return CalculateFinalColor(lightingData, surfaceData.alpha);
}
#endif
