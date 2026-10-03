#ifndef HOUDINI_VAT_INCLUDED
#define HOUDINI_VAT_INCLUDED
#include "Packages/com.xuanxuan.nb.fx/XuanXuanRenderUtility/Shader/HLSL/HoudiniVATKernelV1.hlsl"

#include "HoudiniVATMathV1.hlsl"

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
#ifndef NBSHADER_INPUT
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
#endif

// ─────────────────────────────────────────────────────────────────────
// 工具函数
// ─────────────────────────────────────────────────────────────────────

// 用单位四元数旋转向量
// 公式: v' = v + 2 * cross(q.xyz, q.w*v + cross(q.xyz, v))


// 从 3 个最小组件重建单位四元数（RigidBody 专用）
// maxComp (0-3) 标识被省略的分量


// 从 posTexture.a 解码 5-bit spheremap 压缩法线


// Lookup table 解码：从 RGBA 得到高精度采样 UV


// 计算 VAT 采样 UV


// 2D hash 随机 [0, 1]


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
    HVAT_ComputeMeshFrameSelectionV1(totalFrames, _Time.y,
        _gameTimeAtFirstFrame, _houdiniFPS, _playbackSpeed,
        _B_autoPlayback, _displayFrame, selectedFrame, frameAlpha);

    if (CheckLocalFlags1(FLAG_BIT_PARTICLE_1_IS_PARTICLE_SYSTEM))
    {
        float frameCustomData = GetCustomData(NB_CUSTOM_DATA_FLAG_2, FLAGBIT_POS_2_CUSTOMDATA_VAT_FRAME, -1.0, input.Custom1, input.Custom2);
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
    int mode = -1;
#if defined(_HOUDINI_VAT_SOFTBODY)
    mode = 0;
#elif defined(_HOUDINI_VAT_RIGIDBODY)
    mode = 1;
#elif defined(_HOUDINI_VAT_DYNAMIC_REMESH)
    mode = 2;
#elif defined(_HOUDINI_VAT_PARTICLE_SPRITE)
    mode = 3;
#endif
    float selectedFrame, frameAlpha;
    HVAT_ComputeFrameSelection(input, selectedFrame, frameAlpha);
    HVAT_ConfigV1 config;
    config._B_CAN_SPIN = _B_CAN_SPIN;
    config._B_LOAD_COL_TEX = _B_LOAD_COL_TEX;
    config._B_LOAD_POS_TWO_TEX = _B_LOAD_POS_TWO_TEX;
    config._B_UNLOAD_ROT_TEX = _B_UNLOAD_ROT_TEX;
    config._B_hideOverlappingOrigin = _B_hideOverlappingOrigin;
    config._B_interpolate = _B_interpolate;
    config._B_pscaleAreInPosA = _B_pscaleAreInPosA;
    config._B_spinFromHeading = _B_spinFromHeading;
    config._animateFirstFrame = _animateFirstFrame;
    config._boundMaxX = _boundMaxX;
    config._boundMaxY = _boundMaxY;
    config._boundMaxZ = _boundMaxZ;
    config._boundMinX = _boundMinX;
    config._boundMinY = _boundMinY;
    config._boundMinZ = _boundMinZ;
    config._frameCount = _frameCount;
    config._globalPscaleMul = _globalPscaleMul;
    config._heightBaseScale = _heightBaseScale;
    config._originRadius = _originRadius;
    config._scaleByVelAmount = _scaleByVelAmount;
    config._spinPhase = _spinPhase;
    config._widthBaseScale = _widthBaseScale;
    float3 animatedPositionOS = positionOS.xyz;
    HVAT_ApplyModesV1(mode, CheckLocalFlags1(FLAG_BIT_PARTICLE_1_IS_PARTICLE_SYSTEM),
        input.texcoords, input.Custom1, input.Custom2, float4(input.vatTexcoord5,0,0),
        selectedFrame, frameAlpha, config,
        TEXTURE2D_ARGS(_posTexture, sampler_posTexture),
        TEXTURE2D_ARGS(_posTexture2, sampler_posTexture2),
        TEXTURE2D_ARGS(_rotTexture, sampler_rotTexture),
        TEXTURE2D_ARGS(_colTexture, sampler_colTexture),
        TEXTURE2D_ARGS(_lookupTable, sampler_lookupTable),
        animatedPositionOS, normalOS);
    positionOS.xyz = animatedPositionOS;
}

#endif // HOUDINI_VAT_INCLUDED
