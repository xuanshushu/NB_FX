#ifndef PARTICLE_VAT_INCLUDED
#define PARTICLE_VAT_INCLUDED

#include "Packages/com.xuanxuan.nb.fx/NBShaders/Shader/HLSL/HoudiniVAT.hlsl"
#include "Packages/com.xuanxuan.nb.fx/NBShaders/Shader/HLSL/TyflowVAT.hlsl"

void ApplyVAT(AttributesParticle input, inout float4 positionOS, inout float3 normalOS)
{
#if defined(_VAT)
    if (_VAT_Toggle < 0.5f)
    {
        return;
    }

    #if defined(_VAT_HOUDINI)
    ApplyHoudiniVAT(input, positionOS, normalOS);
    #elif defined(_VAT_TYFLOW)
    ApplyTyflowVAT(input, positionOS, normalOS);
    #endif
#endif
}

#endif
