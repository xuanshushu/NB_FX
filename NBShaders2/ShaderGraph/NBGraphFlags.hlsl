#ifndef NB_GRAPH_FLAGS_INCLUDED
#define NB_GRAPH_FLAGS_INCLUDED

#include "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/HLSL/NBShaderFlags.hlsl"

// SG 17.3 serializes its Integer blackboard value as float. A pair of full
// float 16-bit slices preserves every bit of the original uint protocol.
uint NBGraphDecodeUInt32(float lo16, float hi16)
{
    uint lo = (uint)round(clamp(lo16, 0.0, 65535.0));
    uint hi = (uint)round(clamp(hi16, 0.0, 65535.0));
    return lo | (hi << 16);
}

uint NBGraphFlags0() { return NBGraphDecodeUInt32(_NB_Flags0Lo16, _NB_Flags0Hi16); }
uint NBGraphFlags1() { return NBGraphDecodeUInt32(_NB_Flags1Lo16, _NB_Flags1Hi16); }

#endif
