#ifndef NBFX_GRAPH_MOTION_VECTOR_INPUT_BRIDGE_INCLUDED
#define NBFX_GRAPH_MOTION_VECTOR_INPUT_BRIDGE_INCLUDED

// Keep all URP17.3 motion vertex/fragment behavior. NB VAT needs UV4/UV5
// as float4 in SG Attributes; URP previous-position/velocity use the SAME
// engine TEXCOORD4/5 with float3. Alias input layouts only, then forward xyz.
// This does not create separate UV and previous-skinned streams: Unity owns
// their original semantic convention. Ordinary Mesh is the validated scope.
#define MotionVectorPassAttributes NBGraphURPMotionVectorPassAttributes
#define vert NBGraphURPMotionVectorVertex
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/MotionVectorPass.hlsl"
#undef vert
#undef MotionVectorPassAttributes

struct NBGraphMotionVectorPassAttributes
{
#if defined(ATTRIBUTES_NEED_TEXCOORD4)
    float4 previousPositionOS : TEXCOORD4;
#else
    float3 previousPositionOS : TEXCOORD4;
#endif
#if defined(_ADD_PRECOMPUTED_VELOCITY)
    #if defined(ATTRIBUTES_NEED_TEXCOORD5)
        float4 alembicMotionVectorOS : TEXCOORD5;
    #else
        float3 alembicMotionVectorOS : TEXCOORD5;
    #endif
#endif
};

void vert(Attributes input, NBGraphMotionVectorPassAttributes passInput,
    out PackedMotionVectorPassVaryings packedMvOutput, out PackedVaryings packedOutput)
{
    NBGraphURPMotionVectorPassAttributes originalInput;
    originalInput.previousPositionOS=passInput.previousPositionOS.xyz;
#if defined(_ADD_PRECOMPUTED_VELOCITY)
    originalInput.alembicMotionVectorOS=passInput.alembicMotionVectorOS.xyz;
#endif
    NBGraphURPMotionVectorVertex(input,originalInput,packedMvOutput,packedOutput);
}
#endif
