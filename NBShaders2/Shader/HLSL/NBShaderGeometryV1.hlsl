#ifndef NB_SHADER_GEOMETRY_V1
#define NB_SHADER_GEOMETRY_V1

#include "NBShaderSharedContractV1.hlsl"

// The host prepares texture channels, StartFromZero centering, mask weight,
// and any non-normalized world-to-object direction conversion before this call.
half3 NBFX_ComputeVertexOffsetOSV1(NBFX_VertexOffsetPreparedV1 input)
{
    half3 offsetOS;
    switch (input.directionMode)
    {
        case 1:
            offsetOS = input.normalOS * input.intensity * input.sampledScalar * input.maskWeight;
            break;
        case 2:
        case 3:
            // RGB carries displacement magnitude; do not normalize.
            offsetOS = input.directionOS * input.intensity * input.maskWeight;
            break;
        case 0:
        default:
            offsetOS = input.customDirectionOS * input.intensity * input.sampledScalar * input.maskWeight;
            break;
    }

    return offsetOS;
}

#endif
