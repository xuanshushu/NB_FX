#ifndef NB_FX_TYFLOW_VAT_KERNEL_V1_INCLUDED
#define NB_FX_TYFLOW_VAT_KERNEL_V1_INCLUDED
#define TYFLOW_VAT_SKIN_MAX_BONES 7
// Existing byte decoders, frame selection and transforms; no new packed format.
inline float TyflowVatLinearToGammaExact (float value)
{
    if (value <= 0.0F)
        return 0.0F;
    else if (value <= 0.0031308F)
        return 12.92F * value;
    else if (value < 1.0F)
        return 1.055F * pow(value, 0.4166667F) - 0.055F;
    else
        return pow(value, 0.45454545F);
}
struct TVAT_ConfigV1
{
    int mode;
    bool shadows;
    float flipbook;
    float timeY;
    float _AffectsShadows;
    float _Autoplay;
    float _AutoplaySpeed;
    float _DeformingSkin;
    float _Frame;
    float _FrameInterpolation;
    float _Frames;
    float _ImportScale;
    float _InterpolateLoop;
    float _LinearToGamma;
    float _Loop;
    float _RGBAEncoded;
    float _RGBAHalf;
    float _SkinBoneCount;
    float _VATIncludesNormals;
    float4 _VATTex_TexelSize;
};
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

float4 TyflowVatSampleTexel(TVAT_ConfigV1 config, TEXTURE2D_PARAM(vatTexture, samplerVAT), uint x, uint y)
{
    float2 uv = float2(
        ((float)x + 0.5f) * config._VATTex_TexelSize.x,
        1.0f - (((float)y + 0.5f) * config._VATTex_TexelSize.y));

    return SAMPLE_TEXTURE2D_LOD(vatTexture, samplerVAT, uv, 0);

}

float4 TyflowVatSampleEncodedTexel(TVAT_ConfigV1 config, TEXTURE2D_PARAM(vatTexture, samplerVAT), uint x, uint y)
{
    float4 sample = TyflowVatSampleTexel(config, TEXTURE2D_ARGS(vatTexture,samplerVAT), x, y);

    if (config._LinearToGamma > 0.5f)
    {
        sample.r = TyflowVatLinearToGammaExact(sample.r);
        sample.g = TyflowVatLinearToGammaExact(sample.g);
        sample.b = TyflowVatLinearToGammaExact(sample.b);
    }

    return sample;

}

int TyflowVatTex2DInt(TVAT_ConfigV1 config, TEXTURE2D_PARAM(vatTexture, samplerVAT), int arrInx)
{
    uint width = (uint)config._VATTex_TexelSize.z;
    uint x = (uint)arrInx % width;
    uint y = (uint)arrInx / width;

    float4 bytes = TyflowVatSampleEncodedTexel(config, TEXTURE2D_ARGS(vatTexture,samplerVAT), x, y);
    int4 byteInts = int4(round(bytes.x * 255), round(bytes.y * 255), round(bytes.z * 255), round(bytes.w * 255));
    return TyflowVatUnpackIntRGBA(byteInts.wzyx);

}

float TyflowVatTex2DFloat(TVAT_ConfigV1 config, TEXTURE2D_PARAM(vatTexture, samplerVAT), int arrInx)
{
    uint width = (uint)config._VATTex_TexelSize.z;
    uint x = (uint)arrInx % width;
    uint y = (uint)arrInx / width;

    float4 bytes = TyflowVatSampleEncodedTexel(config, TEXTURE2D_ARGS(vatTexture,samplerVAT), x, y);
    int4 byteInts = int4(round(bytes.x * 255), round(bytes.y * 255), round(bytes.z * 255), round(bytes.w * 255));
    return TyflowVatUnpackFloatRGBA(byteInts.wzyx);

}

half TyflowVatTex2DHalf(TVAT_ConfigV1 config, TEXTURE2D_PARAM(vatTexture, samplerVAT), float arrInxF)
{
    uint arrInx = (uint)floor(arrInxF + 0.1f);
    uint width = (uint)config._VATTex_TexelSize.z;
    uint x = arrInx % width;
    uint y = arrInx / width;

    float4 bytes = TyflowVatSampleEncodedTexel(config, TEXTURE2D_ARGS(vatTexture,samplerVAT), x, y);
    int4 byteInts = int4(round(bytes.x * 255), round(bytes.y * 255), round(bytes.z * 255), round(bytes.w * 255));

    if (abs(arrInxF - round(arrInxF)) > 0.25f)
    {
        return TyflowVatUnpackHalfRGBA(byteInts.wz);
    }

    return TyflowVatUnpackHalfRGBA(byteInts.yx);

}

half2 TyflowVatTex2DHalfs2(TVAT_ConfigV1 config, TEXTURE2D_PARAM(vatTexture, samplerVAT), float arrInxF)
{
    uint arrInx = (uint)floor(arrInxF + 0.1f);
    uint width = (uint)config._VATTex_TexelSize.z;
    uint x = arrInx % width;
    uint y = arrInx / width;

    float4 bytes = TyflowVatSampleEncodedTexel(config, TEXTURE2D_ARGS(vatTexture,samplerVAT), x, y);
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

int TyflowVatGetMetaDataSize(TVAT_ConfigV1 config, TEXTURE2D_PARAM(vatTexture, samplerVAT))
{
    if (config.mode == 5) return 12;
    if (config._DeformingSkin > 0.5f)
    {
        return 12;
    }

    return 3;


}

float3 TyflowVatGetTMPos(TVAT_ConfigV1 config, TEXTURE2D_PARAM(vatTexture, samplerVAT), float startIndex)
{
    float3 result;
    for (int i = 0; i < 3; i++)
    {
        result[i] = TyflowVatTex2DFloat(config, TEXTURE2D_ARGS(vatTexture,samplerVAT), startIndex + i);
    }

    return result;

}

float4 TyflowVatGetTMRot(TVAT_ConfigV1 config, TEXTURE2D_PARAM(vatTexture, samplerVAT), float startIndex)
{
    half2 rotHalfs1 = TyflowVatTex2DHalfs2(config, TEXTURE2D_ARGS(vatTexture,samplerVAT), startIndex);
    half2 rotHalfs2 = TyflowVatTex2DHalfs2(config, TEXTURE2D_ARGS(vatTexture,samplerVAT), startIndex + 1);

    return float4(-rotHalfs1.x, -rotHalfs1.y, -rotHalfs2.x, rotHalfs2.y);

}

float3 TyflowVatGetTMScaleXYZ(TVAT_ConfigV1 config, TEXTURE2D_PARAM(vatTexture, samplerVAT), float startIndex)
{
    half2 scaleHalfs1 = TyflowVatTex2DHalfs2(config, TEXTURE2D_ARGS(vatTexture,samplerVAT), startIndex);
    half2 scaleHalfs2 = TyflowVatTex2DHalfs2(config, TEXTURE2D_ARGS(vatTexture,samplerVAT), startIndex + 1);

    return float3(scaleHalfs1.x, scaleHalfs1.y, scaleHalfs2.x);

}

float3 TyflowVatGetTMScaleAve(TVAT_ConfigV1 config, TEXTURE2D_PARAM(vatTexture, samplerVAT), float startIndex)
{
    half scaleHalf = TyflowVatTex2DHalf(config, TEXTURE2D_ARGS(vatTexture,samplerVAT), startIndex);
    return float3(scaleHalf, scaleHalf, scaleHalf);

}

TyflowVatMatrix3 TyflowVatTMFromParts(TyflowVatTMParts parts)
{
    TyflowVatMatrix3 matrixValue = TyflowVatQuaternionToTM(parts.rot);
    matrixValue = TyflowVatScaleTM(matrixValue, parts.scale);
    matrixValue = TyflowVatTranslateTM(matrixValue, parts.pos);
    return matrixValue;

}

TyflowVatMatrix3 TyflowVatGetVertexInvTM(TVAT_ConfigV1 config, TEXTURE2D_PARAM(vatTexture, samplerVAT), int tmInx)
{
    TyflowVatMatrix3 matrixValue;
    int pixelsPerTM = TyflowVatGetMetaDataSize(config, TEXTURE2D_ARGS(vatTexture,samplerVAT));

    float tmRowInx = 2 + (pixelsPerTM * tmInx);
    matrixValue.row0 = TyflowVatGetTMPos(config, TEXTURE2D_ARGS(vatTexture,samplerVAT), tmRowInx);
    tmRowInx += 3;
    matrixValue.row1 = TyflowVatGetTMPos(config, TEXTURE2D_ARGS(vatTexture,samplerVAT), tmRowInx);
    tmRowInx += 3;
    matrixValue.row2 = TyflowVatGetTMPos(config, TEXTURE2D_ARGS(vatTexture,samplerVAT), tmRowInx);
    tmRowInx += 3;
    matrixValue.row3 = TyflowVatGetTMPos(config, TEXTURE2D_ARGS(vatTexture,samplerVAT), tmRowInx);

    return matrixValue;

}

TyflowVatTMParts TyflowVatGetVertexTMPartsAtFrame(TVAT_ConfigV1 config, TEXTURE2D_PARAM(vatTexture, samplerVAT), int tmInx, int frame, int numTMs)
{
    float4 rot = float4(0, 0, 0, 1);
    float3 pos = float3(0, 0, 0);
    float3 scale = float3(1, 1, 1);

    float pixelsPerTM = 0;
    if (config.mode == 2) {
    pixelsPerTM = 2;
    }
    else if (config.mode == 3) {
    pixelsPerTM = 5;
    }
    else if (config.mode == 5) {
    pixelsPerTM = 7;
    }
    else if (config.mode == 4) {
    pixelsPerTM = 6;
    }

    float frameTMInx = (2 + numTMs * TyflowVatGetMetaDataSize(config, TEXTURE2D_ARGS(vatTexture,samplerVAT))) + (frame * numTMs * pixelsPerTM) + (pixelsPerTM * tmInx);

    if (config.mode == 2) {
    pos = TyflowVatGetTMPos(config, TEXTURE2D_ARGS(vatTexture,samplerVAT), 2 + tmInx * TyflowVatGetMetaDataSize(config, TEXTURE2D_ARGS(vatTexture,samplerVAT)));
    rot = TyflowVatGetTMRot(config, TEXTURE2D_ARGS(vatTexture,samplerVAT), frameTMInx);
    }
    else if (config.mode == 3) {
    pos = TyflowVatGetTMPos(config, TEXTURE2D_ARGS(vatTexture,samplerVAT), frameTMInx);
    frameTMInx += 3;
    rot = TyflowVatGetTMRot(config, TEXTURE2D_ARGS(vatTexture,samplerVAT), frameTMInx);
    }
    else if (config.mode == 5) {
    pos = TyflowVatGetTMPos(config, TEXTURE2D_ARGS(vatTexture,samplerVAT), frameTMInx);
    frameTMInx += 3;
    rot = TyflowVatGetTMRot(config, TEXTURE2D_ARGS(vatTexture,samplerVAT), frameTMInx);
    frameTMInx += 2;
    scale = TyflowVatGetTMScaleXYZ(config, TEXTURE2D_ARGS(vatTexture,samplerVAT), frameTMInx);
    }
    else if (config.mode == 4) {
    pos = TyflowVatGetTMPos(config, TEXTURE2D_ARGS(vatTexture,samplerVAT), frameTMInx);
    frameTMInx += 3;
    rot = TyflowVatGetTMRot(config, TEXTURE2D_ARGS(vatTexture,samplerVAT), frameTMInx);
    frameTMInx += 2;
    scale = TyflowVatGetTMScaleAve(config, TEXTURE2D_ARGS(vatTexture,samplerVAT), frameTMInx);
    }

    TyflowVatTMParts parts;
    parts.pos = pos;
    parts.rot = rot;
    parts.scale = scale;
    return parts;
}

float4 TyflowVatGetVertexValueAtFrame(TVAT_ConfigV1 config, TEXTURE2D_PARAM(vatTexture, samplerVAT), uint vertexIndex, int vertexCount, int frame, int frameOffset)
{
    float4 result = float4(0, 0, 0, 0);

    vertexIndex += (vertexCount * frame) + (vertexCount * frameOffset);

    if (config._RGBAEncoded > 0.5f)
    {
        if (config._RGBAHalf > 0.5f)
        {
            vertexIndex *= 3;
            for (int i = 0; i < 3; i++)
            {
                float arrInxF = (vertexIndex + i) * 0.5f;
                result[i] = TyflowVatTex2DHalf(config, TEXTURE2D_ARGS(vatTexture,samplerVAT), arrInxF);
            }
        }
        else
        {
            vertexIndex *= 3;
            for (int i = 0; i < 3; i++)
            {
                result[i] = TyflowVatTex2DFloat(config, TEXTURE2D_ARGS(vatTexture,samplerVAT), vertexIndex + i);
            }
        }
    }
    else
    {
        uint width = (uint)config._VATTex_TexelSize.z;
        uint x = vertexIndex % width;
        uint y = vertexIndex / width;
        result = TyflowVatSampleTexel(config, TEXTURE2D_ARGS(vatTexture,samplerVAT), x, y);
    }

    return result;

}

float3 TyflowVatGetLocalVertexPosFromTM(TVAT_ConfigV1 config, TEXTURE2D_PARAM(vatTexture, samplerVAT), float3 pos, TyflowVatMatrix3 invTM)
{
    float3 localPos = ((pos / config._ImportScale) * float3(-1, 1, 1));
    return TyflowVatMultiplyPosition(invTM, localPos);

}

float3 TyflowVatGetLocalVertexPosFromPos(TVAT_ConfigV1 config, TEXTURE2D_PARAM(vatTexture, samplerVAT), float3 pos, float boneInx)
{
    int tmInx = 2 + (int)round(boneInx) * TyflowVatGetMetaDataSize(config, TEXTURE2D_ARGS(vatTexture,samplerVAT));
    float3 tmPos = float3(0, 0, 0);

    for (int i = 0; i < 3; i++)
    {
        tmPos[i] = TyflowVatTex2DFloat(config, TEXTURE2D_ARGS(vatTexture,samplerVAT), tmInx + i);
    }

    return ((pos / config._ImportScale) * float3(-1, 1, 1)) - tmPos;

}

float2 TyflowVatGetSkinTexcoord(TVAT_ConfigV1 config, TEXTURE2D_PARAM(vatTexture, samplerVAT), float4 uv1, float4 uv2, float4 uv3, float4 uv4, float4 uv5, float4 uv6, float4 uv7, int index)
{
    if (index == 0) return uv1.xy;
    if (index == 1) return uv2.xy;
    if (config.flipbook < 0.5 && index == 2) return uv3.xy;
    if (index == 3) return uv4.xy;
    if (index == 4) return uv5.xy;
    if (index == 5) return uv6.xy;
    if (index == 6) return uv7.xy;
    return float2(0, 0);

}

void TVAT_ApplyModesV1(TVAT_ConfigV1 config, TEXTURE2D_PARAM(vatTexture, samplerVAT), float4 uv0, float4 uv1, float4 uv2, float4 uv3, float4 uv4, float4 uv5, float4 uv6, float4 uv7, bool particle, float frameCustomData, inout float3 positionOS, inout float3 normalOS)
{
    if (config.shadows) {
    if (config._AffectsShadows < 0.5f)
    {
        return;
    }
    }

    float frameBase = config._Frame;

    if (frameCustomData >= 0.0f)
    {
        frameBase = saturate(frameCustomData) * max(config._Frames - 1.0f, 0.0f);
    }

    float frame = abs(frameBase + ((config._Autoplay > 0.5f) ? (config.timeY * 30.0f * config._AutoplaySpeed) : 0.0f));
    frame = (config._Loop > 0.5f) ? fmod(frame, config._Frames) : min(frame, config._Frames - 1.0f);

    if ((config._Loop > 0.5f) && (config._InterpolateLoop < 0.5f) && (frame >= config._Frames - 1.0f))
    {
        frame = config._Frames - 1.0f;
    }

    uint frame0 = (uint)floor(frame);
    uint frame1 = (uint)ceil(frame) % (uint)config._Frames;
    float frameInterp = frame - frame0;

    if (config.mode >= 2) {
    if (particle)
    {
        return;
    }
    else
    {
        int numTMs = TyflowVatTex2DInt(config, TEXTURE2D_ARGS(vatTexture,samplerVAT), 1);
        float3 combinedPos = float3(0, 0, 0);
        float3 combinedNormal = float3(0, 0, 0);
        int loopCount = (config._DeformingSkin > 0.5f) ? (int)config._SkinBoneCount : 1;

#if defined(NB_GRAPH_VAT_HOST)
        // Graph has a uniform mode; keep the exact bounded seven-bone loop.
        [loop]
#else
        [unroll]
#endif
        for (int i = 0; i < TYFLOW_VAT_SKIN_MAX_BONES; i++)
        {
            if (i >= loopCount)
            {
                break;
            }

            float weight = 1.0f;
            float tmInx = round(uv1.y);

            if (config._DeformingSkin > 0.5f)
            {
                float2 texcoord = TyflowVatGetSkinTexcoord(config, TEXTURE2D_ARGS(vatTexture,samplerVAT), uv1,uv2,uv3,uv4,uv5,uv6,uv7, i);
                tmInx = round(texcoord.x);
                weight = texcoord.y;
            }

            TyflowVatTMParts tmParts0 = TyflowVatGetVertexTMPartsAtFrame(config, TEXTURE2D_ARGS(vatTexture,samplerVAT), (int)tmInx, frame0, numTMs);

            if (config._FrameInterpolation > 0.5f)
            {
                TyflowVatTMParts tmParts1 = TyflowVatGetVertexTMPartsAtFrame(config, TEXTURE2D_ARGS(vatTexture,samplerVAT), (int)tmInx, frame1, numTMs);
                tmParts0.pos = lerp(tmParts0.pos, tmParts1.pos, frameInterp);
                tmParts0.rot = TyflowVatQlerp(tmParts0.rot, tmParts1.rot, frameInterp);
                tmParts0.scale = lerp(tmParts0.scale, tmParts1.scale, frameInterp);
            }

            TyflowVatMatrix3 tm = TyflowVatTMFromParts(tmParts0);
            TyflowVatMatrix3 invStartTM;

            bool useInvStartTM = config.mode == 5 || config._DeformingSkin > 0.5f;

            if (useInvStartTM)
            {
                invStartTM = TyflowVatGetVertexInvTM(config, TEXTURE2D_ARGS(vatTexture,samplerVAT), (int)tmInx);
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
                localPos = TyflowVatGetLocalVertexPosFromTM(config, TEXTURE2D_ARGS(vatTexture,samplerVAT), positionOS, invStartTM);
            }
            else
            {
                localPos = TyflowVatGetLocalVertexPosFromPos(config, TEXTURE2D_ARGS(vatTexture,samplerVAT), positionOS, tmInx);
            }

            float3 pos = TyflowVatMultiplyPosition(tm, localPos) * float3(-1, 1, 1);
            combinedPos += (pos * config._ImportScale) * weight;

            if (!config.shadows) {
            {
                float3 animatedNormal = normalOS;
                tm.row3 = float3(0, 0, 0);

                if (useInvStartTM)
                {
                    invStartTM.row3 = float3(0, 0, 0);
                    float3 localNormal = TyflowVatGetLocalVertexPosFromTM(config, TEXTURE2D_ARGS(vatTexture,samplerVAT), animatedNormal, invStartTM);
                    animatedNormal = normalize(TyflowVatMultiplyPosition(tm, localNormal)) * float3(-1, 1, 1);
                }
                else
                {
                    tm = TyflowVatTranslateTM(tm, float3(0, 0, 0));
                    animatedNormal = normalize(TyflowVatMultiplyPosition(tm, animatedNormal * float3(-1, 1, 1))) * float3(-1, 1, 1);
                }

                combinedNormal += animatedNormal * weight;
            }
            }
        }

        positionOS = combinedPos;
        normalOS = combinedNormal;
    }
    } else if (config.mode <= 1)
    {
        float2 tyflowVatIndexData = particle
            ? uv0.zw
            : uv1.xy;
        uint vertexIndex = (uint)round(tyflowVatIndexData.x);
        uint vertexCount = (uint)round(tyflowVatIndexData.y);

        float4 vertexOffset0 = TyflowVatGetVertexValueAtFrame(config, TEXTURE2D_ARGS(vatTexture,samplerVAT), vertexIndex, vertexCount, frame0, 0);
        float4 vertexOffset = vertexOffset0;

        if (config._FrameInterpolation > 0.5f)
        {
            float4 vertexOffset1 = TyflowVatGetVertexValueAtFrame(config, TEXTURE2D_ARGS(vatTexture,samplerVAT), vertexIndex, vertexCount, frame1, 0);
            vertexOffset = lerp(vertexOffset0, vertexOffset1, frameInterp);
        }

        if (config.mode == 1) positionOS += (vertexOffset * float4(-1, 1, 1, 1) * config._ImportScale).xyz;
        else positionOS = (vertexOffset * float4(-1, 1, 1, 1) * config._ImportScale).xyz;

        if (!config.shadows) {
        if (config._VATIncludesNormals > 0.5f)
        {
            float4 normal0 = TyflowVatGetVertexValueAtFrame(config, TEXTURE2D_ARGS(vatTexture,samplerVAT), vertexIndex, vertexCount, frame0, (int)config._Frames);
            float4 animatedNormal = normal0;
            if (config._FrameInterpolation > 0.5f)
            {
                float4 normal1 = TyflowVatGetVertexValueAtFrame(config, TEXTURE2D_ARGS(vatTexture,samplerVAT), vertexIndex, vertexCount, frame1, (int)config._Frames);
                animatedNormal = lerp(normal0, normal1, frameInterp) * float4(-1, 1, 1, 1);
            }
            normalOS = normalize(animatedNormal.xyz);
        }
        }
    }


}
#endif
