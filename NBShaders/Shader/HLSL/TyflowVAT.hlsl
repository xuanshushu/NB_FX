#ifndef TYFLOW_VAT_INCLUDED
#define TYFLOW_VAT_INCLUDED

#define TYFLOW_VAT_SKIN_MAX_BONES 7

#if defined(_VAT) && defined(_VAT_TYFLOW) && \
    !defined(_TYFLOW_VAT_ABSOLUTE) && \
    !defined(_TYFLOW_VAT_RELATIVE) && \
    !defined(_TYFLOW_VAT_SKIN_R) && \
    !defined(_TYFLOW_VAT_SKIN_PR) && \
    !defined(_TYFLOW_VAT_SKIN_PRSAVE) && \
    !defined(_TYFLOW_VAT_SKIN_PRSXYZ)
    #define _TYFLOW_VAT_ABSOLUTE
#endif

#if defined(_TYFLOW_VAT_SKIN_R) || \
    defined(_TYFLOW_VAT_SKIN_PR) || \
    defined(_TYFLOW_VAT_SKIN_PRSAVE) || \
    defined(_TYFLOW_VAT_SKIN_PRSXYZ)
    #define TYFLOW_VAT_SKIN_MODE
#endif

struct TyflowVatMatrix3
{
    float3 row0;
    float3 row1;
    float3 row2;
    float3 row3;
};

struct TyflowVatTMParts
{
    float3 pos;
    float4 rot;
    float3 scale;
};

inline int TyflowVatUnpackIntRGBA(int4 bytes)
{
    return ((bytes.x << 24) + (bytes.y << 16) + (bytes.z << 8) + (bytes.w << 0));
}

inline float TyflowVatUnpackFloatRGBA(int4 bytes)
{
    int sign = (bytes.r & 128) > 0 ? -1 : 1;

    int expR = (bytes.r & 127) << 1;
    int expG = bytes.g >> 7;
    int exponent = expR + expG;

    int signifG = (bytes.g & 127) << 16;
    int signifB = bytes.b << 8;

    float significand = (signifG + signifB + bytes.a) / pow(2, 23);
    significand += 1;

    return sign * significand * pow(2, exponent - 127);
}

inline half TyflowVatUnpackHalfRGBA(int2 bytes)
{
    uint value = (bytes.x << 8) | bytes.y;

    uint sign = (value & 0x8000) > 0;
    int exponent = (value & 0x7C00) >> 10;
    uint mantissa = (value & 0x03FF);

    if ((value & 0x7FFF) == 0)
    {
        return sign ? -0 : 0;
    }

    if (exponent == 0x001F)
    {
        if (mantissa == 0)
        {
            return sign ? -1e+28 : 1e+28;
        }

        return 1e+28;
    }

    if (exponent > 0)
    {
        float result = pow(2.0, exponent - 15) * (1 + mantissa * (1 / 1024.0f));
        return sign ? -result : result;
    }

    float subnormal = pow(2.0, -24) * mantissa;
    return sign ? -subnormal : subnormal;
}

float4 TyflowVatSampleTexel(uint x, uint y)
{
    float2 uv = float2(
        ((float)x + 0.5f) * _VATTex_TexelSize.x,
        1.0f - (((float)y + 0.5f) * _VATTex_TexelSize.y));

    return SAMPLE_TEXTURE2D_LOD(_VATTex, sampler_point_clamp, uv, 0);
}

float4 TyflowVatSampleEncodedTexel(uint x, uint y)
{
    float4 sample = TyflowVatSampleTexel(x, y);

    if (_LinearToGamma > 0.5f)
    {
        sample.r = LinearToGammaSpaceExact(sample.r);
        sample.g = LinearToGammaSpaceExact(sample.g);
        sample.b = LinearToGammaSpaceExact(sample.b);
    }

    return sample;
}

int TyflowVatTex2DInt(int arrInx)
{
    uint width = (uint)_VATTex_TexelSize.z;
    uint x = (uint)arrInx % width;
    uint y = (uint)arrInx / width;

    float4 bytes = TyflowVatSampleEncodedTexel(x, y);
    int4 byteInts = int4(round(bytes.x * 255), round(bytes.y * 255), round(bytes.z * 255), round(bytes.w * 255));
    return TyflowVatUnpackIntRGBA(byteInts.wzyx);
}

float TyflowVatTex2DFloat(int arrInx)
{
    uint width = (uint)_VATTex_TexelSize.z;
    uint x = (uint)arrInx % width;
    uint y = (uint)arrInx / width;

    float4 bytes = TyflowVatSampleEncodedTexel(x, y);
    int4 byteInts = int4(round(bytes.x * 255), round(bytes.y * 255), round(bytes.z * 255), round(bytes.w * 255));
    return TyflowVatUnpackFloatRGBA(byteInts.wzyx);
}

half TyflowVatTex2DHalf(float arrInxF)
{
    uint arrInx = (uint)floor(arrInxF + 0.1f);
    uint width = (uint)_VATTex_TexelSize.z;
    uint x = arrInx % width;
    uint y = arrInx / width;

    float4 bytes = TyflowVatSampleEncodedTexel(x, y);
    int4 byteInts = int4(round(bytes.x * 255), round(bytes.y * 255), round(bytes.z * 255), round(bytes.w * 255));

    if (abs(arrInxF - round(arrInxF)) > 0.25f)
    {
        return TyflowVatUnpackHalfRGBA(byteInts.wz);
    }

    return TyflowVatUnpackHalfRGBA(byteInts.yx);
}

half2 TyflowVatTex2DHalfs2(float arrInxF)
{
    uint arrInx = (uint)floor(arrInxF + 0.1f);
    uint width = (uint)_VATTex_TexelSize.z;
    uint x = arrInx % width;
    uint y = arrInx / width;

    float4 bytes = TyflowVatSampleEncodedTexel(x, y);
    int4 byteInts = int4(round(bytes.x * 255), round(bytes.y * 255), round(bytes.z * 255), round(bytes.w * 255));

    return half2(TyflowVatUnpackHalfRGBA(byteInts.yx), TyflowVatUnpackHalfRGBA(byteInts.wz));
}

float3 TyflowVatMultiplyPosition(TyflowVatMatrix3 matrixValue, float3 pos)
{
    return float3(
        pos.x * matrixValue.row0[0] + pos.y * matrixValue.row1[0] + pos.z * matrixValue.row2[0] + matrixValue.row3[0],
        pos.x * matrixValue.row0[1] + pos.y * matrixValue.row1[1] + pos.z * matrixValue.row2[1] + matrixValue.row3[1],
        pos.x * matrixValue.row0[2] + pos.y * matrixValue.row1[2] + pos.z * matrixValue.row2[2] + matrixValue.row3[2]);
}

TyflowVatMatrix3 TyflowVatQuaternionToTM(float4 quat)
{
    TyflowVatMatrix3 matrixValue;

    float x = quat.x;
    float y = quat.y;
    float z = quat.z;
    float w = quat.w;

    matrixValue.row0[0] = 1 - 2 * (y * y + z * z);
    matrixValue.row0[1] = 2 * (x * y + z * w);
    matrixValue.row0[2] = 2 * (x * z - y * w);

    matrixValue.row1[0] = 2 * (x * y - z * w);
    matrixValue.row1[1] = 1 - 2 * (x * x + z * z);
    matrixValue.row1[2] = 2 * (y * z + x * w);

    matrixValue.row2[0] = 2 * (x * z + y * w);
    matrixValue.row2[1] = 2 * (y * z - x * w);
    matrixValue.row2[2] = 1 - 2 * (x * x + y * y);

    matrixValue.row3 = float3(0, 0, 0);

    return matrixValue;
}

TyflowVatMatrix3 TyflowVatScaleTM(TyflowVatMatrix3 matrixValue, float3 scale)
{
    matrixValue.row0 *= scale.x;
    matrixValue.row1 *= scale.y;
    matrixValue.row2 *= scale.z;
    return matrixValue;
}

TyflowVatMatrix3 TyflowVatTranslateTM(TyflowVatMatrix3 matrixValue, float3 translation)
{
    matrixValue.row3 = translation;
    return matrixValue;
}

float4 TyflowVatQlerp(float4 a, float4 b, float blend)
{
    float s1 = 1.0f - blend;
    float s2 = dot(a, b) < 0.0f ? -blend : blend;

    return normalize(float4(
        s1 * a.x + s2 * b.x,
        s1 * a.y + s2 * b.y,
        s1 * a.z + s2 * b.z,
        s1 * a.w + s2 * b.w));
}

int TyflowVatGetMetaDataSize()
{
    #if defined(_TYFLOW_VAT_SKIN_PRSXYZ)
    return 12;
    #else
    if (_DeformingSkin > 0.5f)
    {
        return 12;
    }

    return 3;
    #endif
}

float3 TyflowVatGetTMPos(float startIndex)
{
    float3 result;
    for (int i = 0; i < 3; i++)
    {
        result[i] = TyflowVatTex2DFloat(startIndex + i);
    }

    return result;
}

float4 TyflowVatGetTMRot(float startIndex)
{
    half2 rotHalfs1 = TyflowVatTex2DHalfs2(startIndex);
    half2 rotHalfs2 = TyflowVatTex2DHalfs2(startIndex + 1);

    return float4(-rotHalfs1.x, -rotHalfs1.y, -rotHalfs2.x, rotHalfs2.y);
}

float3 TyflowVatGetTMScaleXYZ(float startIndex)
{
    half2 scaleHalfs1 = TyflowVatTex2DHalfs2(startIndex);
    half2 scaleHalfs2 = TyflowVatTex2DHalfs2(startIndex + 1);

    return float3(scaleHalfs1.x, scaleHalfs1.y, scaleHalfs2.x);
}

float3 TyflowVatGetTMScaleAve(float startIndex)
{
    half scaleHalf = TyflowVatTex2DHalf(startIndex);
    return float3(scaleHalf, scaleHalf, scaleHalf);
}

TyflowVatMatrix3 TyflowVatTMFromParts(TyflowVatTMParts parts)
{
    TyflowVatMatrix3 matrixValue = TyflowVatQuaternionToTM(parts.rot);
    matrixValue = TyflowVatScaleTM(matrixValue, parts.scale);
    matrixValue = TyflowVatTranslateTM(matrixValue, parts.pos);
    return matrixValue;
}

TyflowVatMatrix3 TyflowVatGetVertexInvTM(int tmInx)
{
    TyflowVatMatrix3 matrixValue;
    int pixelsPerTM = TyflowVatGetMetaDataSize();

    float tmRowInx = 2 + (pixelsPerTM * tmInx);
    matrixValue.row0 = TyflowVatGetTMPos(tmRowInx);
    tmRowInx += 3;
    matrixValue.row1 = TyflowVatGetTMPos(tmRowInx);
    tmRowInx += 3;
    matrixValue.row2 = TyflowVatGetTMPos(tmRowInx);
    tmRowInx += 3;
    matrixValue.row3 = TyflowVatGetTMPos(tmRowInx);

    return matrixValue;
}

TyflowVatTMParts TyflowVatGetVertexTMPartsAtFrame(int tmInx, int frame, int numTMs)
{
    float4 rot = float4(0, 0, 0, 1);
    float3 pos = float3(0, 0, 0);
    float3 scale = float3(1, 1, 1);

    float pixelsPerTM = 0;
    #if defined(_TYFLOW_VAT_SKIN_R)
    pixelsPerTM = 2;
    #elif defined(_TYFLOW_VAT_SKIN_PR)
    pixelsPerTM = 5;
    #elif defined(_TYFLOW_VAT_SKIN_PRSXYZ)
    pixelsPerTM = 7;
    #elif defined(_TYFLOW_VAT_SKIN_PRSAVE)
    pixelsPerTM = 6;
    #endif

    float frameTMInx = (2 + numTMs * TyflowVatGetMetaDataSize()) + (frame * numTMs * pixelsPerTM) + (pixelsPerTM * tmInx);

    #if defined(_TYFLOW_VAT_SKIN_R)
    pos = TyflowVatGetTMPos(2 + tmInx * TyflowVatGetMetaDataSize());
    rot = TyflowVatGetTMRot(frameTMInx);
    #elif defined(_TYFLOW_VAT_SKIN_PR)
    pos = TyflowVatGetTMPos(frameTMInx);
    frameTMInx += 3;
    rot = TyflowVatGetTMRot(frameTMInx);
    #elif defined(_TYFLOW_VAT_SKIN_PRSXYZ)
    pos = TyflowVatGetTMPos(frameTMInx);
    frameTMInx += 3;
    rot = TyflowVatGetTMRot(frameTMInx);
    frameTMInx += 2;
    scale = TyflowVatGetTMScaleXYZ(frameTMInx);
    #elif defined(_TYFLOW_VAT_SKIN_PRSAVE)
    pos = TyflowVatGetTMPos(frameTMInx);
    frameTMInx += 3;
    rot = TyflowVatGetTMRot(frameTMInx);
    frameTMInx += 2;
    scale = TyflowVatGetTMScaleAve(frameTMInx);
    #endif

    TyflowVatTMParts parts;
    parts.pos = pos;
    parts.rot = rot;
    parts.scale = scale;
    return parts;
}

float4 TyflowVatGetVertexValueAtFrame(uint vertexIndex, int vertexCount, int frame, int frameOffset)
{
    float4 result = float4(0, 0, 0, 0);

    vertexIndex += (vertexCount * frame) + (vertexCount * frameOffset);

    if (_RGBAEncoded > 0.5f)
    {
        if (_RGBAHalf > 0.5f)
        {
            vertexIndex *= 3;
            for (int i = 0; i < 3; i++)
            {
                float arrInxF = (vertexIndex + i) * 0.5f;
                result[i] = TyflowVatTex2DHalf(arrInxF);
            }
        }
        else
        {
            vertexIndex *= 3;
            for (int i = 0; i < 3; i++)
            {
                result[i] = TyflowVatTex2DFloat(vertexIndex + i);
            }
        }
    }
    else
    {
        uint width = (uint)_VATTex_TexelSize.z;
        uint x = vertexIndex % width;
        uint y = vertexIndex / width;
        result = TyflowVatSampleTexel(x, y);
    }

    return result;
}

float3 TyflowVatGetLocalVertexPosFromTM(float3 pos, TyflowVatMatrix3 invTM)
{
    float3 localPos = ((pos / _ImportScale) * float3(-1, 1, 1));
    return TyflowVatMultiplyPosition(invTM, localPos);
}

float3 TyflowVatGetLocalVertexPosFromPos(float3 pos, float boneInx)
{
    int tmInx = 2 + (int)round(boneInx) * TyflowVatGetMetaDataSize();
    float3 tmPos = float3(0, 0, 0);

    for (int i = 0; i < 3; i++)
    {
        tmPos[i] = TyflowVatTex2DFloat(tmInx + i);
    }

    return ((pos / _ImportScale) * float3(-1, 1, 1)) - tmPos;
}

float2 TyflowVatGetSkinTexcoord(AttributesParticle input, int index)
{
    if (index == 0) return input.Custom1.xy;
    if (index == 1) return input.Custom2.xy;
    #if !defined(_FLIPBOOKBLENDING_ON)
    if (index == 2) return input.vatTexcoord4;
    #endif
    if (index == 3) return input.vatTexcoord5;
    if (index == 4) return input.vatTexcoord6;
    if (index == 5) return input.vatTexcoord7;
    if (index == 6) return input.vatTexcoord8;
    return float2(0, 0);
}

void ApplyTyflowVAT(AttributesParticle input, inout float4 positionOS, inout float3 normalOS)
{
    #if defined(SHADOWS_DEPTH)
    if (_AffectsShadows < 0.5f)
    {
        return;
    }
    #endif

    float frameBase = _Frame;
    float frameCustomData = GetCustomData(_W9ParticleCustomDataFlag2, FLAGBIT_POS_2_CUSTOMDATA_VAT_FRAME, -1.0f, input.Custom1, input.Custom2);
    if (frameCustomData >= 0.0f)
    {
        frameBase = saturate(frameCustomData) * max(_Frames - 1.0f, 0.0f);
    }

    float frame = abs(frameBase + ((_Autoplay > 0.5f) ? (time * 30.0f * _AutoplaySpeed) : 0.0f));
    frame = (_Loop > 0.5f) ? fmod(frame, _Frames) : min(frame, _Frames - 1.0f);

    if ((_Loop > 0.5f) && (_InterpolateLoop < 0.5f) && (frame >= _Frames - 1.0f))
    {
        frame = _Frames - 1.0f;
    }

    uint frame0 = (uint)floor(frame);
    uint frame1 = (uint)ceil(frame) % (uint)_Frames;
    float frameInterp = frame - frame0;

    #if defined(TYFLOW_VAT_SKIN_MODE)
    if (CheckLocalFlags1(FLAG_BIT_PARTICLE_1_IS_PARTICLE_SYSTEM))
    {
        return;
    }
    else
    {
        int numTMs = TyflowVatTex2DInt(1);
        float3 combinedPos = float3(0, 0, 0);
        float3 combinedNormal = float3(0, 0, 0);
        int loopCount = (_DeformingSkin > 0.5f) ? (int)_SkinBoneCount : 1;

        [unroll]
        for (int i = 0; i < TYFLOW_VAT_SKIN_MAX_BONES; i++)
        {
            if (i >= loopCount)
            {
                break;
            }

            float weight = 1.0f;
            float tmInx = round(input.Custom1.y);

            if (_DeformingSkin > 0.5f)
            {
                float2 texcoord = TyflowVatGetSkinTexcoord(input, i);
                tmInx = round(texcoord.x);
                weight = texcoord.y;
            }

            TyflowVatTMParts tmParts0 = TyflowVatGetVertexTMPartsAtFrame((int)tmInx, frame0, numTMs);

            if (_FrameInterpolation > 0.5f)
            {
                TyflowVatTMParts tmParts1 = TyflowVatGetVertexTMPartsAtFrame((int)tmInx, frame1, numTMs);
                tmParts0.pos = lerp(tmParts0.pos, tmParts1.pos, frameInterp);
                tmParts0.rot = TyflowVatQlerp(tmParts0.rot, tmParts1.rot, frameInterp);
                tmParts0.scale = lerp(tmParts0.scale, tmParts1.scale, frameInterp);
            }

            TyflowVatMatrix3 tm = TyflowVatTMFromParts(tmParts0);
            TyflowVatMatrix3 invStartTM;

            #if defined(_TYFLOW_VAT_SKIN_PRSXYZ)
            bool useInvStartTM = true;
            #else
            bool useInvStartTM = _DeformingSkin > 0.5f;
            #endif

            if (useInvStartTM)
            {
                invStartTM = TyflowVatGetVertexInvTM((int)tmInx);
            }
            else
            {
                invStartTM.row0 = float3(0, 0, 0);
                invStartTM.row1 = float3(0, 0, 0);
                invStartTM.row2 = float3(0, 0, 0);
                invStartTM.row3 = float3(0, 0, 0);
            }

            float3 localPos;
            if (useInvStartTM)
            {
                localPos = TyflowVatGetLocalVertexPosFromTM(positionOS.xyz, invStartTM);
            }
            else
            {
                localPos = TyflowVatGetLocalVertexPosFromPos(positionOS.xyz, tmInx);
            }

            float3 pos = TyflowVatMultiplyPosition(tm, localPos) * float3(-1, 1, 1);
            combinedPos += (pos * _ImportScale) * weight;

            #if !defined(SHADOWS_DEPTH)
            {
                float3 animatedNormal = normalOS;
                tm.row3 = float3(0, 0, 0);

                if (useInvStartTM)
                {
                    invStartTM.row3 = float3(0, 0, 0);
                    float3 localNormal = TyflowVatGetLocalVertexPosFromTM(animatedNormal, invStartTM);
                    animatedNormal = normalize(TyflowVatMultiplyPosition(tm, localNormal)) * float3(-1, 1, 1);
                }
                else
                {
                    tm = TyflowVatTranslateTM(tm, float3(0, 0, 0));
                    animatedNormal = normalize(TyflowVatMultiplyPosition(tm, animatedNormal * float3(-1, 1, 1))) * float3(-1, 1, 1);
                }

                combinedNormal += animatedNormal * weight;
            }
            #endif
        }

        positionOS.xyz = combinedPos;
        normalOS = combinedNormal;
    }
    #elif defined(_TYFLOW_VAT_ABSOLUTE) || defined(_TYFLOW_VAT_RELATIVE)
    {
        float2 tyflowVatIndexData = CheckLocalFlags1(FLAG_BIT_PARTICLE_1_IS_PARTICLE_SYSTEM)
            ? input.texcoords.zw
            : input.Custom1.xy;
        uint vertexIndex = (uint)round(tyflowVatIndexData.x);
        uint vertexCount = (uint)round(tyflowVatIndexData.y);

        float4 vertexOffset0 = TyflowVatGetVertexValueAtFrame(vertexIndex, vertexCount, frame0, 0);
        float4 vertexOffset = vertexOffset0;

        if (_FrameInterpolation > 0.5f)
        {
            float4 vertexOffset1 = TyflowVatGetVertexValueAtFrame(vertexIndex, vertexCount, frame1, 0);
            vertexOffset = lerp(vertexOffset0, vertexOffset1, frameInterp);
        }

        #if defined(_TYFLOW_VAT_RELATIVE)
        positionOS.xyz += (vertexOffset * float4(-1, 1, 1, 1) * _ImportScale).xyz;
        #else
        positionOS.xyz = (vertexOffset * float4(-1, 1, 1, 1) * _ImportScale).xyz;
        #endif

        #if !defined(SHADOWS_DEPTH)
        if (_VATIncludesNormals > 0.5f)
        {
            float4 normal0 = TyflowVatGetVertexValueAtFrame(vertexIndex, vertexCount, frame0, (int)_Frames);
            float4 normal1 = TyflowVatGetVertexValueAtFrame(vertexIndex, vertexCount, frame1, (int)_Frames);
            float4 animatedNormal = lerp(normal0, normal1, frameInterp) * float4(-1, 1, 1, 1);
            normalOS = normalize(animatedNormal.xyz);
        }
        #endif
    }
    #endif
}

#endif
