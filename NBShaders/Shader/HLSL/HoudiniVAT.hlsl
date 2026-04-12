#ifndef HOUDINI_VAT_INCLUDED
#define HOUDINI_VAT_INCLUDED

// ─────────────────────────────────────────────────────────────────────
// Houdini VAT 3.0 — 统一实现（SoftBody / RigidBody / DynamicRemeshing / ParticleSprite）
// 集成于 ParticleBase.shader 的粒子着色器管线
// ─────────────────────────────────────────────────────────────────────

// ── 纹理声明 ──────────────────────────────────────────────────────────
TEXTURE2D(_posTexture);   SAMPLER(sampler_posTexture);
TEXTURE2D(_posTexture2);  SAMPLER(sampler_posTexture2);
TEXTURE2D(_rotTexture);   SAMPLER(sampler_rotTexture);
TEXTURE2D(_colTexture);   SAMPLER(sampler_colTexture);
TEXTURE2D(_lookupTable);  SAMPLER(sampler_lookupTable);

#if defined(_VAT) && defined(_VAT_HOUDINI) && \
    !defined(_HOUDINI_VAT_SOFTBODY) && \
    !defined(_HOUDINI_VAT_RIGIDBODY) && \
    !defined(_HOUDINI_VAT_DYNAMIC_REMESH) && \
    !defined(_HOUDINI_VAT_PARTICLE_SPRITE)
    #define _HOUDINI_VAT_SOFTBODY
#endif

// ── extern 材质属性（Unity 自动从材质中查找同名值） ───────────────────

// Playback
extern float _B_autoPlayback;
extern float _gameTimeAtFirstFrame;
extern float _playbackSpeed;
extern float _houdiniFPS;
extern float _displayFrame;
extern float _B_interpolate;
extern float _animateFirstFrame;
extern float _frameCount;

// Bounds
extern float _boundMinX, _boundMinY, _boundMinZ;
extern float _boundMaxX, _boundMaxY, _boundMaxZ;

// Scale
extern float _globalPscaleMul;
extern float _B_pscaleAreInPosA;

// Particle Sprite
extern float _widthBaseScale;
extern float _heightBaseScale;
extern float _B_hideOverlappingOrigin;
extern float _originRadius;
extern float _B_CAN_SPIN;
extern float _B_spinFromHeading;
extern float _spinPhase;
extern float _scaleByVelAmount;
extern float _particleTexUScale;
extern float _particleTexVScale;

// Flags
extern float _B_LOAD_POS_TWO_TEX;
extern float _B_UNLOAD_ROT_TEX;
extern float _B_LOAD_COL_TEX;
extern float _B_LOAD_LOOKUP_TABLE;

// ─────────────────────────────────────────────────────────────────────
// 工具函数
// ─────────────────────────────────────────────────────────────────────

// 用单位四元数旋转向量
// 公式: v' = v + 2 * cross(q.xyz, q.w*v + cross(q.xyz, v))
float3 HVAT_RotateByQuat(float3 v, float4 q)
{
    float3 t = cross(q.xyz, v);
    return v + cross(q.xyz, t + v * q.w) * 2.0;
}

// 从 3 个最小组件重建单位四元数（RigidBody 专用）
// maxComp (0-3) 标识被省略的分量
float4 HVAT_DecodeQuaternion(float3 xyz, float maxComp)
{
    float w = sqrt(saturate(1.0 - dot(xyz, xyz)));
    float4 q = float4(xyz.x, xyz.y, xyz.z, w);
    int mc = (int)maxComp;
    if      (mc == 1) q = float4(    w,  xyz.y,  xyz.z,  xyz.x);
    else if (mc == 2) q = float4(xyz.x,     -w,  xyz.z, -xyz.y);
    else if (mc == 3) q = float4(xyz.x,  xyz.y,     -w, -xyz.z);
    return q;
}

// 从 posTexture.a 解码 5-bit spheremap 压缩法线
float3 HVAT_DecodeCompressedNormal(float posA)
{
    float scaledA = posA * 1024.0;
    float xIdx    = floor(scaledA / 32.0);
    float yRaw    = scaledA - xIdx * 32.0;
    float xNorm   = xIdx / 31.5;
    float yNorm   = yRaw / 31.5;
    float2 xy     = float2(xNorm, yNorm) * 4.0 - 2.0;
    float d       = dot(xy, xy);
    float sqrtF   = sqrt(saturate(1.0 - d * 0.25));
    return float3(-sqrtF * xy.x, 1.0 - d * 0.5, sqrtF * xy.y);
}

// Lookup table 解码：从 RGBA 得到高精度采样 UV
float2 HVAT_DecodeLookupUV(float4 lookupSample, float boundMinX)
{
    float lookupHDR = (frac(-boundMinX * 10.0) >= 0.5) ? 1.0 : 0.0;
    float divisor   = lookupHDR ? 2048.0 : 255.0;
    float lookupX   = lookupSample.r + lookupSample.g / divisor;
    float lookupY   = 1.0 - (lookupSample.b + lookupSample.a / divisor);
    return float2(lookupX, lookupY);
}

// 计算 VAT 采样 UV
float2 HVAT_VatUV(float selectedFrame, float uv_r, float uv_g,
                  float oneMinusBoundMaxR, float multiplyBoundMinB, float totalFrames)
{
    float wrapped = fmod(selectedFrame - 1.0, totalFrames);
    float vBase   = (1.0 - uv_g) * oneMinusBoundMaxR
                    + (wrapped / totalFrames) * oneMinusBoundMaxR;
    return float2(multiplyBoundMinB, 1.0 - vBase);
}

// 2D hash 随机 [0, 1]
float HVAT_HashRandom2D(float2 seed)
{
    return frac(sin(dot(seed, float2(12.9898, 78.233))) * 43758.5453);
}

// ─────────────────────────────────────────────────────────────────────
// 帧选择
// ─────────────────────────────────────────────────────────────────────

float2 HVAT_GetVatUV1(AttributesParticle input)
{
    return CheckLocalFlags1(FLAG_BIT_PARTICLE_1_IS_PARTICLE_SYSTEM)
           ? input.texcoords.zw
           : input.Custom1.xy;
}

void HVAT_ComputeFrameSelection(AttributesParticle input, out float selectedFrame, out float frameAlpha)
{
    float totalFrames = _frameCount;
    float animTime    = (_Time.y - _gameTimeAtFirstFrame)
                        * (_houdiniFPS / (totalFrames - 0.01))
                        * _playbackSpeed;
    float frameFloat  = frac(animTime) * totalFrames;

    selectedFrame = _B_autoPlayback
                    ? floor(frameFloat) + 1.0
                    : floor(_displayFrame);
    frameAlpha    = frac(_B_autoPlayback ? frameFloat : _displayFrame);

    if (CheckLocalFlags1(FLAG_BIT_PARTICLE_1_IS_PARTICLE_SYSTEM))
    {
        float frameCustomData = GetCustomData(_W9ParticleCustomDataFlag2, FLAGBIT_POS_2_CUSTOMDATA_VAT_FRAME, -1.0, input.Custom1, input.Custom2);
        if (frameCustomData >= 0.0)
        {
            float customFrame = saturate(frameCustomData) * max(totalFrames - 1.0, 0.0) + 1.0;
            selectedFrame = floor(customFrame);
            frameAlpha = frac(customFrame);
        }
    }
}

// ─────────────────────────────────────────────────────────────────────
// 主函数：ApplyHoudiniVAT
// ─────────────────────────────────────────────────────────────────────

void ApplyHoudiniVAT(AttributesParticle input, inout float4 positionOS, inout float3 normalOS)
{
    // ── 共享 Bounds 常量（不依赖 UV 的部分） ──
    float comparisonBoundMaxb = (frac(_boundMaxZ * 10.0) >= 0.5) ? 1.0 : 0.0;
    float oneMinusBoundMaxR   = 1.0 - frac(_boundMaxX * (-10.0));
    float boundMinMul10z      = _boundMinZ * 10.0;
    float oneMinusBoundMinB   = 1.0 - (ceil(boundMinMul10z) - boundMinMul10z);
    float pscaleDenom         = max(1.0 - frac(_boundMaxY * 10.0), 1e-5);

    // ── 帧选择 ──
    float selectedFrame, frameAlpha;
    HVAT_ComputeFrameSelection(input, selectedFrame, frameAlpha);
    float totalFrames = _frameCount;

    float3 boundsMax = float3(_boundMaxX, _boundMaxY, _boundMaxZ);
    float3 boundsMin = float3(_boundMinX, _boundMinY, _boundMinZ);

    // ─────────────────────────────────────────────────────────────────
    // Sub Mode 0: SoftBody — 加法位移 + 四元数法线 / 压缩法线
    // ─────────────────────────────────────────────────────────────────
#if defined(_HOUDINI_VAT_SOFTBODY)
    {
        float2 vatUV1 = HVAT_GetVatUV1(input);
        float uv1r = vatUV1.r;
        float uv1g = vatUV1.g;
        float multiplyBoundMinB = uv1r * oneMinusBoundMinB;

        float2 texUV = HVAT_VatUV(selectedFrame, uv1r, uv1g,
                                  oneMinusBoundMaxR, multiplyBoundMinB, totalFrames);

        // 采样位置
        float4 posSample = SAMPLE_TEXTURE2D_LOD(_posTexture, sampler_posTexture, texUV, 0);
        float3 posRGB    = posSample.rgb;
        float  posA      = posSample.a;

        // 双纹理高精度位置
        if (_B_LOAD_POS_TWO_TEX > 0.5)
        {
            float4 pos2 = SAMPLE_TEXTURE2D_LOD(_posTexture2, sampler_posTexture2, texUV, 0);
            posRGB += pos2.rgb * 0.01;
        }

        // 解码位移
        float3 posDecoded = posRGB * (boundsMax - boundsMin) + boundsMin;
        float3 displacement = comparisonBoundMaxb ? posRGB : posDecoded;

        // SoftBody: 原始位置 + 位移
        positionOS.xyz += displacement;

        // 法线
        if (_B_UNLOAD_ROT_TEX > 0.5)
        {
            normalOS = normalize(HVAT_DecodeCompressedNormal(posA));
        }
        else
        {
            float4 rotSample = SAMPLE_TEXTURE2D_LOD(_rotTexture, sampler_rotTexture, texUV, 0);
            float4 rotFinal  = comparisonBoundMaxb ? rotSample : (rotSample - 0.5) * 2.0;
            normalOS = normalize(HVAT_RotateByQuat(float3(0.0, 1.0, 0.0), rotFinal));
        }

        return;
    }

    // ─────────────────────────────────────────────────────────────────
    // Sub Mode 1: RigidBody — Pivot 旋转 + Pscale + 帧间插值
    // ─────────────────────────────────────────────────────────────────
#elif defined(_HOUDINI_VAT_RIGIDBODY)
    {
        if (CheckLocalFlags1(FLAG_BIT_PARTICLE_1_IS_PARTICLE_SYSTEM))
        {
            return;
        }

        float2 vatUV1 = HVAT_GetVatUV1(input);
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
        if (_B_LOAD_POS_TWO_TEX > 0.5)
        {
            float4 pos2 = SAMPLE_TEXTURE2D_LOD(_posTexture2, sampler_posTexture2, texUV, 0);
            posRawForDecode = posRGB + pos2.rgb * 0.01;
        }

        float3 posDecoded = posRawForDecode * (boundsMax - boundsMin) + boundsMin;
        float3 piecePos   = comparisonBoundMaxb ? posRawForDecode : posDecoded;

        // Pscale
        float pscaleFromPosA = (1.0 - frac(quatMaxIdxScaled)) / pscaleDenom;
        float pscale         = (_B_pscaleAreInPosA > 0.5) ? pscaleFromPosA : 1.0;
        float totalScale     = _globalPscaleMul * pscale;

        // Rest pivot 从 UV2/UV3
        // Custom2 = TEXCOORD2 (uv2), vatTexcoord5 = TEXCOORD4 (uv3)
        float3 restPivot = float3(-input.Custom2.r, input.vatTexcoord5.r, 1.0 - input.vatTexcoord5.g);

        // 旋转局部偏移
        float3 localOffset  = positionOS.xyz - restPivot;
        float3 rotatedLocal = HVAT_RotateByQuat(localOffset, q);
        float3 scaledLocal  = rotatedLocal * totalScale;

        // 帧间插值
        float3 finalPiecePos = piecePos;
        if (_B_interpolate > 0.5)
        {
            float4 posSampleNext = SAMPLE_TEXTURE2D_LOD(_posTexture, sampler_posTexture, texUV_next, 0);
            float3 posRGBNext    = posSampleNext.rgb;
            if (_B_LOAD_POS_TWO_TEX > 0.5)
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
        bool isRestFrame = (wrappedForCheck < 0.5) && !(_animateFirstFrame > 0.5);
        float3 finalPosOS = isRestFrame ? positionOS.xyz : animatedPos;

        // 崩塌：无 piece 关联
        finalPosOS = (uv1g <= 0.1) ? float3(0, 0, 0) : finalPosOS;

        // 法线
        float3 rotatedNormalOS = isRestFrame
                                 ? normalOS
                                 : normalize(HVAT_RotateByQuat(normalOS, q));
        normalOS = rotatedNormalOS;

        positionOS.xyz = finalPosOS;
        return;
    }

    // ─────────────────────────────────────────────────────────────────
    // Sub Mode 2: DynamicRemeshing — Lookup Table → 绝对位置
    // ─────────────────────────────────────────────────────────────────
#elif defined(_HOUDINI_VAT_DYNAMIC_REMESH)
    {
        // 使用 uv0 (texcoords.r/g) 作为 piece UV
        float uv0r = input.texcoords.r;
        float uv0g = input.texcoords.g;
        float multiplyBoundMinB = uv0r * oneMinusBoundMinB;

        float2 vatUV = HVAT_VatUV(selectedFrame, uv0r, uv0g,
                                  oneMinusBoundMaxR, multiplyBoundMinB, totalFrames);

        // Lookup Table
        float4 lookupSample = SAMPLE_TEXTURE2D_LOD(_lookupTable, sampler_lookupTable, vatUV, 0);
        float2 lookupUV     = HVAT_DecodeLookupUV(lookupSample, _boundMinX);

        // 采样位置（绝对坐标）
        float4 posSample = SAMPLE_TEXTURE2D_LOD(_posTexture, sampler_posTexture, lookupUV, 0);
        float3 posRGB    = posSample.rgb;
        float  posA      = posSample.a;

        // 双纹理高精度位置
        if (_B_LOAD_POS_TWO_TEX > 0.5)
        {
            float4 pos2 = SAMPLE_TEXTURE2D_LOD(_posTexture2, sampler_posTexture2, lookupUV, 0);
            posRGB += pos2.rgb * 0.01;
        }

        // 解码绝对位置
        float3 posDecoded = posRGB * (boundsMax - boundsMin) + boundsMin;
        float3 finalPosOS = comparisonBoundMaxb ? posRGB : posDecoded;

        // 无有效 piece 塌陷
        finalPosOS = (uv0g <= 0.1) ? float3(0, 0, 0) : finalPosOS;

        positionOS.xyz = finalPosOS;

        // 法线
        if (_B_UNLOAD_ROT_TEX > 0.5)
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
#elif defined(_HOUDINI_VAT_PARTICLE_SPRITE)
    // Sub Mode 3: ParticleSprite — Billboard + Spin + Origin Mask
    // ─────────────────────────────────────────────────────────────────
    {
        float2 vatUV1 = HVAT_GetVatUV1(input);
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
        if (_B_LOAD_POS_TWO_TEX > 0.5)
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
        float3 particlePosF = (_B_interpolate > 0.5)
                              ? lerp(particlePos, particlePos_next, frameAlpha)
                              : particlePos;

        // Pscale
        float posA_f    = (_B_interpolate > 0.5) ? lerp(posA, posA_next, frameAlpha) : posA;
        float pscaleRaw = posA_f / pscaleDenom;

        // 每粒子随机缩放
        float perParticleRandom    = HVAT_HashRandom2D(float2(uv1r, uv1g));
        float additionalPscaleMul  = 1.0 + perParticleRandom;

        // 原点遮挡 mask
        float distThis = distance(particlePos,      float3(0, 0, 0));
        float distNext = distance(particlePos_next, float3(0, 0, 0));
        float maskThis = saturate(sign(distThis - _originRadius));
        float maskNext = saturate(sign(distNext - _originRadius));
        float maskF    = (_B_interpolate > 0.5) ? lerp(maskThis, maskNext, frameAlpha) : maskThis;

        float pscaleFinal;
        if (_B_pscaleAreInPosA > 0.5)
        {
            pscaleFinal = (_B_hideOverlappingOrigin > 0.5)
                          ? pscaleRaw * _globalPscaleMul * additionalPscaleMul * maskF
                          : pscaleRaw * _globalPscaleMul * additionalPscaleMul;
        }
        else
        {
            pscaleFinal = (_B_hideOverlappingOrigin > 0.5)
                          ? _globalPscaleMul * additionalPscaleMul * maskF
                          : _globalPscaleMul * additionalPscaleMul;
        }

        // Billboard 方向轴
        float3 viewRight  = float3(1, 0, 0);
        float3 viewUp     = float3(0, 1, 0);
        float  velStretch = 1.0;
        float3 headingViewDir = float3(0, 0, 0);

        if (_B_CAN_SPIN > 0.5)
        {
            // 从颜色通道读取 heading
            if (_B_LOAD_COL_TEX > 0.5)
            {
                float4 colThis = SAMPLE_TEXTURE2D_LOD(_colTexture, sampler_colTexture, vatUV, 0);
                headingViewDir = float3(-colThis.r, colThis.g, colThis.b);
            }

            if (_B_spinFromHeading > 0.5)
            {
                float2 hXY  = headingViewDir.xy;
                float  hLen = length(hXY);
                float2 hDir = (hLen > 1e-5) ? hXY / hLen : float2(1, 0);
                viewRight  = float3(hDir.x, hDir.y, 0);
                viewUp     = cross(viewRight, float3(0, 0, -1));
                velStretch = _scaleByVelAmount * hLen;
            }
            else
            {
                float angle = frac(_spinPhase) * 6.283185;
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
        float cornerX = input.texcoords.r - 0.5;
        float cornerY = input.texcoords.g - 0.5;

        float3 offsetX = rightOS * cornerX * _widthBaseScale  * pscaleFinal;
        float3 offsetY = upOS    * cornerY * _heightBaseScale * pscaleFinal * velStretch;

        float3 finalPosOS = particlePosF + offsetX + offsetY;

        // 崩塌
        finalPosOS = (uv1g <= 0.1) ? float3(0, 0, 0) : finalPosOS;

        positionOS.xyz = finalPosOS;
        normalOS       = normalOS_new;
        return;
    }
#endif
}

#endif // HOUDINI_VAT_INCLUDED
