#ifndef NB_GRAPH_VERTEX_OFFSET_INCLUDED
#define NB_GRAPH_VERTEX_OFFSET_INCLUDED

#include "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/HLSL/NBShaderUVV1.hlsl"
#include "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/HLSL/NBShaderGeometryV1.hlsl"
#include "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBGraphFlags.hlsl"
#include "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBGraphSampling.hlsl"

half NBGraphVertexOffsetChannel(half4 value, uint channels, uint shift)
{
    uint channel = (channels >> shift) & 3u;
    return channel == 0u ? value.r : channel == 1u ? value.g :
        channel == 2u ? value.b : value.a;
}

uint NBGraphVertexOffsetUVMode(uint modes, uint types, uint shift)
{
    return (((types >> shift) & 3u) << 2u) | ((modes >> shift) & 3u);
}

bool NBGraphVertexOffsetSourceSupported(uint source)
{
    // All original modes now have Mesh-host inputs. Packed 9..15 retain
    // GetUVByUVMode's original default-UV0 fallback, not a new meaning.
    return source <= 15u;
}

// Vertex-only host. The shared GeometryV1 function owns all four direction
// modes; this adapter owns Graph texture resources, packed routing and LOD0.
// Normal/Tangent are deliberately unchanged, as in the ShaderLab VertexOffset
// path. CustomData, CustomLocalTransform, VAT and VFX are not represented here.
void NBGraphVertexOffset_float(
    float3 PositionOS, float3 NormalOS, float3 TangentOS,
    float3 VertexColorRGB, float4 UV0, float4 UV1, float4 UV2,
    UnityTexture2D VertexOffsetMap, float4 VertexOffsetVec,
    float3 VertexOffsetCustomDir, float VertexOffsetDirectionMode,
    float VertexOffsetDirectionSpace, float VertexOffsetToggle,
    UnityTexture2D VertexOffsetMaskMap, float3 VertexOffsetMaskVec,
    float VertexOffsetMaskToggle,
    float Flags0Lo16, float Flags0Hi16, float Flags1Lo16, float Flags1Hi16,
    float WrapLo16, float WrapHi16, float ChannelLo16, float ChannelHi16,
    float UVModeLo16, float UVModeHi16, float UVTypeLo16, float UVTypeHi16,
    float4 SharedUVST, float4 SharedUVVec,
    float4 TWParameter, float TWStrength, float4 PCCenter,
    float4 BaseMapST,
    float BaseMapUVRotation,
    float BaseMapUVRotationSpeed,
    float4 BaseMapMaskMapOffset,
    float WorldSelector,
    float ObjectSelector,
    float4 CylinderMatrix0,
    float4 CylinderMatrix1,
    float4 CylinderMatrix2,
    float4 CylinderMatrix3,
    out float3 OutPositionOS, out float3 OutNormalOS,
    out float3 OutTangentOS, out float Supported)
{
    OutPositionOS = PositionOS;
    OutNormalOS = NormalOS;
    OutTangentOS = TangentOS;
    Supported = 1.0;
    if (VertexOffsetToggle <= 0.5)
        return;

    uint modes = NBGraphDecodeUInt32(UVModeLo16, UVModeHi16);
    uint types = NBGraphDecodeUInt32(UVTypeLo16, UVTypeHi16);
    uint mapSource = NBGraphVertexOffsetUVMode(modes, types,
        FLAG_BIT_UVMODE_POS_0_VERTEX_OFFSET_MAP);
    uint maskSource = NBGraphVertexOffsetUVMode(modes, types,
        FLAG_BIT_UVMODE_POS_0_VERTEX_OFFSET_MASKMAP);
    uint sharedSource = NBGraphVertexOffsetUVMode(modes, types,
        FLAG_BIT_UVMODE_POS_0_SHAREDUV);
    if (!NBGraphVertexOffsetSourceSupported(mapSource) ||
        (VertexOffsetMaskToggle > 0.5 && !NBGraphVertexOffsetSourceSupported(maskSource)) ||
        ((mapSource == 8u || (VertexOffsetMaskToggle > 0.5 && maskSource == 8u)) &&
            !NBGraphVertexOffsetSourceSupported(sharedSource)))
    {
        Supported = 0.0;
        return;
    }

    uint flags0 = NBGraphDecodeUInt32(Flags0Lo16, Flags0Hi16);
    uint flags1 = NBGraphDecodeUInt32(Flags1Lo16, Flags1Hi16);
    uint wrapFlags = NBGraphDecodeUInt32(WrapLo16, WrapHi16);
    uint channels = NBGraphDecodeUInt32(ChannelLo16, ChannelHi16);
    NBFX_BaseUVInputV1 uvInput = (NBFX_BaseUVInputV1)0;
    uvInput.meshTexcoord0 = UV0;
    uvInput.custom1 = UV1;
    uvInput.custom2 = UV2;
    uvInput.positionOS = PositionOS;
    uvInput.positionWS = TransformObjectToWorld(PositionOS);
    float4 clipPosition = TransformObjectToHClip(PositionOS);
    uvInput.screenUV = clipPosition.xy / clipPosition.w;
    uvInput.screenUV = uvInput.screenUV * 0.5 + 0.5;
    NBFX_BaseUVParamsV1 uvParams = (NBFX_BaseUVParamsV1)0;
    uvParams.flags0 = flags0 &
        (FLAG_BIT_PARTICLE_UTWIRL_ON | FLAG_BIT_PARTICLE_POLARCOORDINATES_ON);
    uvParams.flags1 = flags1 &
        (FLAG_BIT_PARTICLE_1_UV_FROM_MESH | FLAG_BIT_PARTICLE_1_USE_TEXCOORD1 |
            FLAG_BIT_PARTICLE_1_USE_TEXCOORD2 | FLAG_BIT_PARTICLE_1_CYLINDER_CORDINATE);
    uvParams.uvModeFlag0 = modes;
    uvParams.uvModeFlagType0 = types;
    uvParams.baseMapST = BaseMapST;
    uvParams.baseMapUVRotation = (half)BaseMapUVRotation;
    uvParams.baseMapUVRotationSpeed = (half)BaseMapUVRotationSpeed;
    uvParams.baseMapMaskMapOffset = (half4)BaseMapMaskMapOffset;
    uvParams.worldSpaceUVModeSelector = (half)WorldSelector;
    uvParams.objectSpaceUVModeSelector = (half)ObjectSelector;
    uvParams.cylinderUVMatrix = float4x4(CylinderMatrix0, CylinderMatrix1,
        CylinderMatrix2, CylinderMatrix3);
    uvParams.uiMainTexST = float4(1, 1, 0, 0);
    uvParams.sharedUVST = (half4)SharedUVST;
    uvParams.sharedUVVec = (half4)SharedUVVec;
    uvParams.twirlParameter = TWParameter;
    uvParams.twirlStrength = TWStrength;
    uvParams.polarCenter = PCCenter;
    uvParams.timeY = _Time.y;
    BaseUVs baseUVs = NBFX_BuildBaseUVsV1(uvInput, uvParams);
    half2 mapUV = (half2)GetUVByUVMode(modes, types,
        FLAG_BIT_UVMODE_POS_0_VERTEX_OFFSET_MAP, baseUVs);
    half2 maskUV = (half2)GetUVByUVMode(modes, types,
        FLAG_BIT_UVMODE_POS_0_VERTEX_OFFSET_MASKMAP, baseUVs);

    // Texture scaleTranslate is the Graph counterpart of _Map_ST. Apply it
    // once, then ShaderLab time scroll. mode4 intentionally consumes already
    // transformed MainTexUV before this map's own ST, as the original does.
    half4 mapST = (half4)VertexOffsetMap.scaleTranslate;
    half4 maskST = (half4)VertexOffsetMaskMap.scaleTranslate;
    int directionMode = (int)round(VertexOffsetDirectionMode);
    half scalar = 1.0h;
    half3 direction = (half3)VertexColorRGB;
    if (directionMode != 2)
    {
        half2 transformed = mapUV * mapST.xy + mapST.zw;
        transformed = UVOffsetAnimaiton(transformed, (half2)VertexOffsetVec.xy, _Time.y);
        half4 sampleValue = NBGraphSampleRawMap(VertexOffsetMap, transformed,
            NBGraphMaskWrapMode(wrapFlags, FLAG_BIT_WRAPMODE_VERTEXOFFSETMAP), true);
        scalar = NBGraphVertexOffsetChannel(sampleValue, channels,
            FLAG_BIT_COLOR_CHANNEL_POS_0_VERTEX_OFFSET_MAP);
        direction = sampleValue.rgb;
    }
    if ((flags1 & FLAG_BIT_PARTICLE_1_VERTEXOFFSET_START_FROM_ZERO) == 0u)
    {
        scalar = scalar * 2.0h - 1.0h;
        direction = direction * 2.0h - 1.0h;
    }

    half maskWeight = 1.0h;
    if (VertexOffsetMaskToggle > 0.5)
    {
        half2 transformed = maskUV * maskST.xy + maskST.zw;
        transformed = UVOffsetAnimaiton(transformed, (half2)VertexOffsetMaskVec.xy, _Time.y);
        half4 sampleValue = NBGraphSampleRawMap(VertexOffsetMaskMap, transformed,
            NBGraphMaskWrapMode(wrapFlags, FLAG_BIT_WRAPMODE_VERTEXOFFSET_MASKMAP), true);
        half maskScalar = NBGraphVertexOffsetChannel(sampleValue, channels,
            FLAG_BIT_COLOR_CHANNEL_POS_0_VERTEX_OFFSET_MASKMAP);
        maskWeight = lerp(1.0h, maskScalar, (half)VertexOffsetMaskVec.z);
    }
    if ((directionMode == 2 || directionMode == 3) && VertexOffsetDirectionSpace > 0.5)
        direction = mul((float3x3)unity_WorldToObject, direction);

    NBFX_VertexOffsetPreparedV1 prepared = (NBFX_VertexOffsetPreparedV1)0;
    prepared.normalOS = (half3)NormalOS;
    prepared.directionOS = direction;
    prepared.customDirectionOS = (half3)VertexOffsetCustomDir;
    prepared.sampledScalar = scalar;
    prepared.maskWeight = maskWeight;
    prepared.intensity = (half)VertexOffsetVec.z;
    prepared.directionMode = directionMode;
    half3 offsetOS = NBFX_ComputeVertexOffsetOSV1(prepared);
    OutPositionOS = (float3)((half3)PositionOS + offsetOS);
}

#endif
