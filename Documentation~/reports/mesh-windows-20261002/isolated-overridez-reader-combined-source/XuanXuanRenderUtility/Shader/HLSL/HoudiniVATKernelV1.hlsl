#ifndef NB_FX_HOUDINI_VAT_KERNEL_V1_INCLUDED
#define NB_FX_HOUDINI_VAT_KERNEL_V1_INCLUDED
#include "Packages/com.xuanxuan.nb.fx/XuanXuanRenderUtility/Shader/HLSL/HoudiniVATMathV1.hlsl"
// Arithmetic and sampling order extracted from the existing four-mode path.
// Legacy supplies a compile-time mode; Graph supplies its existing mode input.
struct HVAT_ConfigV1
{
    float _B_CAN_SPIN;
    float _B_LOAD_COL_TEX;
    float _B_LOAD_POS_TWO_TEX;
    float _B_UNLOAD_ROT_TEX;
    float _B_hideOverlappingOrigin;
    float _B_interpolate;
    float _B_pscaleAreInPosA;
    float _B_spinFromHeading;
    float _animateFirstFrame;
    float _boundMaxX;
    float _boundMaxY;
    float _boundMaxZ;
    float _boundMinX;
    float _boundMinY;
    float _boundMinZ;
    float _frameCount;
    float _globalPscaleMul;
    float _heightBaseScale;
    float _originRadius;
    float _scaleByVelAmount;
    float _spinPhase;
    float _widthBaseScale;
};
void HVAT_ApplyModesV1(int mode, bool particle, float4 uv0, float4 uv1,
    float4 uv2, float4 uv4, float selectedFrame, float frameAlpha,
    HVAT_ConfigV1 config,
    TEXTURE2D_PARAM(_posTexture, sampler_posTexture),
    TEXTURE2D_PARAM(_posTexture2, sampler_posTexture2),
    TEXTURE2D_PARAM(_rotTexture, sampler_rotTexture),
    TEXTURE2D_PARAM(_colTexture, sampler_colTexture),
    TEXTURE2D_PARAM(_lookupTable, sampler_lookupTable),
    inout float3 positionOS, inout float3 normalOS)
{

    // ── 共享 Bounds 常量（不依赖 UV 的部分） ──
    float comparisonBoundMaxb = (frac(config._boundMaxZ * 10.0) >= 0.5) ? 1.0 : 0.0;
    float oneMinusBoundMaxR   = 1.0 - frac(config._boundMaxX * (-10.0));
    float boundMinMul10z      = config._boundMinZ * 10.0;
    float oneMinusBoundMinB   = 1.0 - (ceil(boundMinMul10z) - boundMinMul10z);
    float pscaleDenom         = max(1.0 - frac(config._boundMaxY * 10.0), 1e-5);

    // ── 帧选择 ──


    float totalFrames = config._frameCount;

    float3 boundsMax = float3(config._boundMaxX, config._boundMaxY, config._boundMaxZ);
    float3 boundsMin = float3(config._boundMinX, config._boundMinY, config._boundMinZ);

    // ─────────────────────────────────────────────────────────────────
    // Sub Mode 0: SoftBody — 加法位移 + 四元数法线 / 压缩法线
    // ─────────────────────────────────────────────────────────────────
if (mode == 0)
    {
        float2 vatUV1 = (particle ? uv0.zw : uv1.xy);
        float uv1r = vatUV1.r;
        float uv1g = vatUV1.g;
        float multiplyBoundMinB = uv1r * oneMinusBoundMinB;

        float2 texUV = HVAT_VatUV(selectedFrame, uv1r, uv1g,
                                  oneMinusBoundMaxR, multiplyBoundMinB, totalFrames);

        float4 posSample = SAMPLE_TEXTURE2D_LOD(_posTexture, sampler_posTexture, texUV, 0);
        float4 pos2 = 0;
        if (config._B_LOAD_POS_TWO_TEX > 0.5)
            pos2 = SAMPLE_TEXTURE2D_LOD(_posTexture2, sampler_posTexture2, texUV, 0);
        float4 rotSample = 0;
        if (config._B_UNLOAD_ROT_TEX <= 0.5)
            rotSample = SAMPLE_TEXTURE2D_LOD(_rotTexture, sampler_rotTexture, texUV, 0);
        float3 softBodyPositionOS = positionOS;
        HVAT_ApplySoftBodySamplesV1(softBodyPositionOS, normalOS,
            posSample, pos2, rotSample, config._B_LOAD_POS_TWO_TEX, config._B_UNLOAD_ROT_TEX,
            comparisonBoundMaxb, boundsMax, boundsMin);
        positionOS = softBodyPositionOS;

        return;
    }

    // ─────────────────────────────────────────────────────────────────
    // Sub Mode 1: RigidBody — Pivot 旋转 + Pscale + 帧间插值
    // ─────────────────────────────────────────────────────────────────
else if (mode == 1)
    {
        if (particle)
        {
            return;
        }

        float2 vatUV1 = (particle ? uv0.zw : uv1.xy);
        float uv1r = vatUV1.r;
        float uv1g = vatUV1.g;
        float multiplyBoundMinB = uv1r * oneMinusBoundMinB;

        // 当前帧和下一帧 UV
        float2 texUV      = HVAT_VatUV(selectedFrame,       uv1r, uv1g,
                                       oneMinusBoundMaxR, multiplyBoundMinB, totalFrames);
        float2 texUV_next = HVAT_VatUV(selectedFrame + 1.0, uv1r, uv1g,
                                       oneMinusBoundMaxR, multiplyBoundMinB, totalFrames);

        // 采样旋转
        float4 rotSample   = SAMPLE_TEXTURE2D_LOD(_rotTexture, sampler_rotTexture, texUV, 0);
        float4 rotRemapped = (rotSample - 0.5) * 2.0;
        float4 rotFinal    = comparisonBoundMaxb ? rotSample : rotRemapped;

        // 采样位置
        float4 posSample = SAMPLE_TEXTURE2D_LOD(_posTexture, sampler_posTexture, texUV, 0);
        float3 posRGB    = posSample.rgb;
        float  posA      = posSample.a;

        // 四元数 maxComp 索引（从 posA 整数部分）
        float quatMaxIdxScaled = posA * 4.0;
        float quatMaxIdx       = floor(quatMaxIdxScaled);

        // 解码四元数
        float4 q = HVAT_DecodeQuaternion(rotFinal.rgb, quatMaxIdx);

        // 解码位置
        float3 posRawForDecode = posRGB;
        if (config._B_LOAD_POS_TWO_TEX > 0.5)
        {
            float4 pos2 = SAMPLE_TEXTURE2D_LOD(_posTexture2, sampler_posTexture2, texUV, 0);
            posRawForDecode = posRGB + pos2.rgb * 0.01;
        }

        float3 posDecoded = posRawForDecode * (boundsMax - boundsMin) + boundsMin;
        float3 piecePos   = comparisonBoundMaxb ? posRawForDecode : posDecoded;

        // Pscale
        float pscaleFromPosA = (1.0 - frac(quatMaxIdxScaled)) / pscaleDenom;
        float pscale         = (config._B_pscaleAreInPosA > 0.5) ? pscaleFromPosA : 1.0;
        float totalScale     = config._globalPscaleMul * pscale;

        // Rest pivot 从 UV2/UV3
        // Custom2 = TEXCOORD2 (uv2), vatTexcoord5 = TEXCOORD4 (uv3)
        float3 restPivot = float3(-uv2.r, uv4.r, 1.0 - uv4.g);

        // 旋转局部偏移
        float3 localOffset  = positionOS - restPivot;
        float3 rotatedLocal = HVAT_RotateByQuat(localOffset, q);
        float3 scaledLocal  = rotatedLocal * totalScale;

        // 帧间插值
        float3 finalPiecePos = piecePos;
        if (config._B_interpolate > 0.5)
        {
            float4 posSampleNext = SAMPLE_TEXTURE2D_LOD(_posTexture, sampler_posTexture, texUV_next, 0);
            float3 posRGBNext    = posSampleNext.rgb;
            if (config._B_LOAD_POS_TWO_TEX > 0.5)
            {
                float4 pos2Next = SAMPLE_TEXTURE2D_LOD(_posTexture2, sampler_posTexture2, texUV_next, 0);
                posRGBNext += pos2Next.rgb * 0.01;
            }
            float3 posDecodedNext = posRGBNext * (boundsMax - boundsMin) + boundsMin;
            float3 piecePosNext   = comparisonBoundMaxb ? posRGBNext : posDecodedNext;
            finalPiecePos = lerp(piecePos, piecePosNext, frameAlpha);
        }

        // 组装最终位置
        float3 animatedPos = scaledLocal + finalPiecePos;

        // 首帧复位
        float wrappedForCheck = fmod(selectedFrame - 1.0, totalFrames);
        bool isRestFrame = (wrappedForCheck < 0.5) && !(config._animateFirstFrame > 0.5);
        float3 finalPosOS = isRestFrame ? positionOS : animatedPos;

        // 崩塌：无 piece 关联
        finalPosOS = (uv1g <= 0.1) ? float3(0, 0, 0) : finalPosOS;

        // 法线
        float3 rotatedNormalOS = isRestFrame
                                 ? normalOS
                                 : normalize(HVAT_RotateByQuat(normalOS, q));
        normalOS = rotatedNormalOS;

        positionOS = finalPosOS;
        return;
    }

    // ─────────────────────────────────────────────────────────────────
    // Sub Mode 2: DynamicRemeshing — Lookup Table → 绝对位置
    // ─────────────────────────────────────────────────────────────────
else if (mode == 2)
    {
        // 使用 uv0 (texcoords.r/g) 作为 piece UV
        float uv0r = uv0.r;
        float uv0g = uv0.g;
        float multiplyBoundMinB = uv0r * oneMinusBoundMinB;

        float2 vatUV = HVAT_VatUV(selectedFrame, uv0r, uv0g,
                                  oneMinusBoundMaxR, multiplyBoundMinB, totalFrames);

        // Lookup Table
        float4 lookupSample = SAMPLE_TEXTURE2D_LOD(_lookupTable, sampler_lookupTable, vatUV, 0);
        float2 lookupUV     = HVAT_DecodeLookupUV(lookupSample, config._boundMinX);

        // 采样位置（绝对坐标）
        float4 posSample = SAMPLE_TEXTURE2D_LOD(_posTexture, sampler_posTexture, lookupUV, 0);
        float3 posRGB    = posSample.rgb;
        float  posA      = posSample.a;

        // 双纹理高精度位置
        if (config._B_LOAD_POS_TWO_TEX > 0.5)
        {
            float4 pos2 = SAMPLE_TEXTURE2D_LOD(_posTexture2, sampler_posTexture2, lookupUV, 0);
            posRGB += pos2.rgb * 0.01;
        }

        // 解码绝对位置
        float3 posDecoded = posRGB * (boundsMax - boundsMin) + boundsMin;
        float3 finalPosOS = comparisonBoundMaxb ? posRGB : posDecoded;

        // 无有效 piece 塌陷
        finalPosOS = (uv0g <= 0.1) ? float3(0, 0, 0) : finalPosOS;

        positionOS = finalPosOS;

        // 法线
        if (config._B_UNLOAD_ROT_TEX > 0.5)
        {
            normalOS = normalize(HVAT_DecodeCompressedNormal(posA));
        }
        else
        {
            float4 rotSample = SAMPLE_TEXTURE2D_LOD(_rotTexture, sampler_rotTexture, lookupUV, 0);
            float4 rotFinal  = comparisonBoundMaxb ? rotSample : (rotSample - 0.5) * 2.0;
            normalOS = normalize(HVAT_RotateByQuat(float3(0.0, 1.0, 0.0), rotFinal));
        }

        return;
    }

    // ─────────────────────────────────────────────────────────────────
else if (mode == 3)
    // Sub Mode 3: ParticleSprite — Billboard + Spin + Origin Mask
    // ─────────────────────────────────────────────────────────────────
    {
        float2 vatUV1 = (particle ? uv0.zw : uv1.xy);
        float uv1r = vatUV1.r;
        float uv1g = vatUV1.g;
        float multiplyBoundMinB = uv1r * oneMinusBoundMinB;

        // 当前帧 + 下一帧 UV
        float2 vatUV      = HVAT_VatUV(selectedFrame,       uv1r, uv1g,
                                       oneMinusBoundMaxR, multiplyBoundMinB, totalFrames);
        float2 vatUV_next = HVAT_VatUV(selectedFrame + 1.0, uv1r, uv1g,
                                       oneMinusBoundMaxR, multiplyBoundMinB, totalFrames);

        // 采样位置
        float4 posSample      = SAMPLE_TEXTURE2D_LOD(_posTexture, sampler_posTexture, vatUV,      0);
        float4 posSample_next = SAMPLE_TEXTURE2D_LOD(_posTexture, sampler_posTexture, vatUV_next, 0);

        float3 posRGB      = posSample.rgb;
        float3 posRGB_next = posSample_next.rgb;
        float  posA        = posSample.a;
        float  posA_next   = posSample_next.a;

        // 双纹理高精度位置
        if (config._B_LOAD_POS_TWO_TEX > 0.5)
        {
            float4 pos2      = SAMPLE_TEXTURE2D_LOD(_posTexture2, sampler_posTexture2, vatUV,      0);
            float4 pos2_next = SAMPLE_TEXTURE2D_LOD(_posTexture2, sampler_posTexture2, vatUV_next, 0);
            posRGB      += pos2.rgb      * 0.01;
            posRGB_next += pos2_next.rgb * 0.01;
        }

        // 解码粒子中心
        float3 posDecoded      = posRGB      * (boundsMax - boundsMin) + boundsMin;
        float3 posDecoded_next = posRGB_next * (boundsMax - boundsMin) + boundsMin;

        float3 particlePos      = comparisonBoundMaxb ? posRGB      : posDecoded;
        float3 particlePos_next = comparisonBoundMaxb ? posRGB_next : posDecoded_next;

        // 帧间插值
        float3 particlePosF = (config._B_interpolate > 0.5)
                              ? lerp(particlePos, particlePos_next, frameAlpha)
                              : particlePos;

        // Pscale
        float posA_f    = (config._B_interpolate > 0.5) ? lerp(posA, posA_next, frameAlpha) : posA;
        float pscaleRaw = posA_f / pscaleDenom;

        // 每粒子随机缩放
        float perParticleRandom    = HVAT_HashRandom2D(float2(uv1r, uv1g));
        float additionalPscaleMul  = 1.0 + perParticleRandom;

        // 原点遮挡 mask
        float distThis = distance(particlePos,      float3(0, 0, 0));
        float distNext = distance(particlePos_next, float3(0, 0, 0));
        float maskThis = saturate(sign(distThis - config._originRadius));
        float maskNext = saturate(sign(distNext - config._originRadius));
        float maskF    = (config._B_interpolate > 0.5) ? lerp(maskThis, maskNext, frameAlpha) : maskThis;

        float pscaleFinal;
        if (config._B_pscaleAreInPosA > 0.5)
        {
            pscaleFinal = (config._B_hideOverlappingOrigin > 0.5)
                          ? pscaleRaw * config._globalPscaleMul * additionalPscaleMul * maskF
                          : pscaleRaw * config._globalPscaleMul * additionalPscaleMul;
        }
        else
        {
            pscaleFinal = (config._B_hideOverlappingOrigin > 0.5)
                          ? config._globalPscaleMul * additionalPscaleMul * maskF
                          : config._globalPscaleMul * additionalPscaleMul;
        }

        // Billboard 方向轴
        float3 viewRight  = float3(1, 0, 0);
        float3 viewUp     = float3(0, 1, 0);
        float  velStretch = 1.0;
        float3 headingViewDir = float3(0, 0, 0);

        if (config._B_CAN_SPIN > 0.5)
        {
            // 从颜色通道读取 heading
            if (config._B_LOAD_COL_TEX > 0.5)
            {
                float4 colThis = SAMPLE_TEXTURE2D_LOD(_colTexture, sampler_colTexture, vatUV, 0);
                headingViewDir = float3(-colThis.r, colThis.g, colThis.b);
            }

            if (config._B_spinFromHeading > 0.5)
            {
                float2 hXY  = headingViewDir.xy;
                float  hLen = length(hXY);
                float2 hDir = (hLen > 1e-5) ? hXY / hLen : float2(1, 0);
                viewRight  = float3(hDir.x, hDir.y, 0);
                viewUp     = cross(viewRight, float3(0, 0, -1));
                velStretch = config._scaleByVelAmount * hLen;
            }
            else
            {
                float angle = frac(config._spinPhase) * 6.283185;
                float c = cos(angle);
                float s = sin(angle);
                viewRight = float3(c, s, 0);
                viewUp    = cross(viewRight, float3(0, 0, -1));
            }
        }

        // 视图空间 → 世界空间 → 物体空间
        float3 worldRight = mul((float3x3)UNITY_MATRIX_I_V, viewRight);
        float3 worldUp    = mul((float3x3)UNITY_MATRIX_I_V, viewUp);
        float3 worldFwd   = mul((float3x3)UNITY_MATRIX_I_V, float3(0, 0, 1));

        float3 rightOS  = normalize(TransformWorldToObjectDir(worldRight));
        float3 upOS     = normalize(TransformWorldToObjectDir(worldUp));
        float3 normalOS_new = normalize(TransformWorldToObjectDir(worldFwd));

        // Billboard 顶点偏移
        float cornerX = uv0.r - 0.5;
        float cornerY = uv0.g - 0.5;

        float3 offsetX = rightOS * cornerX * config._widthBaseScale  * pscaleFinal;
        float3 offsetY = upOS    * cornerY * config._heightBaseScale * pscaleFinal * velStretch;

        float3 finalPosOS = particlePosF + offsetX + offsetY;

        // 崩塌
        finalPosOS = (uv1g <= 0.1) ? float3(0, 0, 0) : finalPosOS;

        positionOS = finalPosOS;
        normalOS       = normalOS_new;
        return;
    }

}
#endif
