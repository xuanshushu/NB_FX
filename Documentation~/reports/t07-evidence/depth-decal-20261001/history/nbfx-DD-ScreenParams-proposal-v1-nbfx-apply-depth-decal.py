#!/usr/bin/env python3
"""DepthDecal ordinary-Mesh candidate. Dynamic dry-run; never edits product.

Outputs previews under /tmp/nbfx-depth-decal-preview. Root owns application,
Unity verification and Git. It reads every current source at invocation, so
later Refraction/other CF inputs are kept rather than overwritten.
"""
import copy
import hashlib
import json
import re
import uuid
from pathlib import Path

P = Path('/Users/bytedance/UnityProject/NBUnityProject/Packages/NB_FX')
F = {
    'graph': P/'NBShaders2/ShaderGraph/NBShaderGraph.shadergraph',
    'uv': P/'NBShaders2/ShaderGraph/NBGraphBaseUV.hlsl',
    'color': P/'NBShaders2/ShaderGraph/NBGraphBaseColor.hlsl',
    'legacy': P/'NBShaders2/Shader/HLSL/NBShaderForwardPass.hlsl',
}
OUT = Path('/tmp/nbfx-depth-decal-preview')
NS = uuid.UUID('44eb43d7-03ab-45d1-9f7d-a2b0a66c343f')

def uid(k): return uuid.uuid5(NS, 'DepthDecal:' + k).hex
def once(s, a, b, count=1):
    n=s.count(a)
    if n!=count: raise AssertionError(f'needle {a[:90]!r}: got {n}, expected {count}')
    return s.replace(a,b)
def sha(b): return hashlib.sha256(b).hexdigest()
def decode(s):
    d=json.JSONDecoder();i=0;v=[]
    while i<len(s):
        while i<len(s) and s[i].isspace():i+=1
        if i==len(s):break
        x,i=d.raw_decode(s,i);v.append(x)
    return v

PURE='''#ifndef NB_SHADER_DEPTH_DECAL_V1
#define NB_SHADER_DEPTH_DECAL_V1

#include "Packages/com.xuanxuan.nb.fx/XuanXuanRenderUtility/Shader/HLSL/XuanXuan_Utility.hlsl"

// Only the original object-space cube/UV/alpha math. Both ShaderLab and SG
// hosts own their depth sample, world reconstruction and World->Object path.
struct NBFX_DepthDecalProjectionV1
{
    float2 uv;
    half alpha;
};

NBFX_DepthDecalProjectionV1 NBFX_ResolveDepthDecalV1(float3 fragobjectPos)
{
    float3 absFragObjectPos = abs(fragobjectPos);
    half clipValue = step(absFragObjectPos.x,0.5);
    clipValue *= step(absFragObjectPos.y,0.5);
    clipValue *= step(absFragObjectPos.z,0.5);
    half decalAlpha = NB_Remap (abs(fragobjectPos.y),0.1,0.5,1,0);
    decalAlpha = decalAlpha*decalAlpha;
    decalAlpha *= clipValue;
    NBFX_DepthDecalProjectionV1 result;
    result.uv = fragobjectPos.xz + 0.5;
    result.alpha = decalAlpha;
    return result;
}

#endif
'''

def patch_legacy(s):
    assert 'NBShaderDepthDecalV1.hlsl' not in s
    s=once(s,'    #include "NBShaderDissolveV3.hlsl"',
           '    #include "NBShaderDissolveV3.hlsl"\n    #include "NBShaderDepthDecalV1.hlsl"')
    old='''            float3 absFragObjectPos = abs(fragobjectPos);
            half clipValue = step(absFragObjectPos.x,0.5);
            clipValue *= step(absFragObjectPos.y,0.5);
            clipValue *= step(absFragObjectPos.z,0.5);
            half decalAlpha = NB_Remap (abs(fragobjectPos.y),0.1,0.5,1,0);
        decalAlpha = decalAlpha*decalAlpha;
            decalAlpha *= clipValue;
            float2 decalUV = fragobjectPos.xz + 0.5;'''
    new='''            NBFX_DepthDecalProjectionV1 depthDecal = NBFX_ResolveDepthDecalV1(fragobjectPos);
            half decalAlpha = depthDecal.alpha;
            float2 decalUV = depthDecal.uv;'''
    return once(s,old,new)

def patch_uv(s):
    assert 'DepthDecalToggle' not in s
    s=once(s,'#include "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBGraphFlags.hlsl"',
      '#include "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBGraphFlags.hlsl"\n'
      '#include "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/HLSL/NBShaderDepthDecalV1.hlsl"\n'
      '#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"\n'
      '#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"')
    s=once(s,'// The BaseMap Graph sample owns no implicit ST.',
      '''// The official SG Pixel ScreenPosition output is not raw SV_POSITION:
// BuildSurfaceDescriptionInputs optionally flips its Y with _ScreenParams.
// Invert that exact transform before applying the ShaderLab denominator.
float2 NBGraphDecalRasterUV(float4 pixelPosition)
{
    float2 pixel = pixelPosition.xy;
    #if UNITY_UV_STARTS_AT_TOP
        if (_ProjectionParams.x < 0) pixel.y = _ScreenParams.y - pixel.y;
    #else
        if (_ProjectionParams.x > 0) pixel.y = _ScreenParams.y - pixel.y;
    #endif
    return pixel / _ScaledScreenParams.xy;
}

// The BaseMap Graph sample owns no implicit ST.''')
    # CustomFunctionNode emits inputs in serialized m_Slots order, *then*
    # outputs. Keep every old argument intact and append the two new inputs.
    s=once(s,'    float4 TWParameter, float TWStrength, float4 PCCenter,\n    out float2 Out,',
      '    float4 TWParameter, float TWStrength, float4 PCCenter,\n'
      '    float4 PixelPosition, float DepthDecalToggle,\n    out float2 Out,')
    s=once(s,'    half4 TWParameter, half TWStrength, half4 PCCenter,\n    out half2 Out,',
      '    half4 TWParameter, half TWStrength, half4 PCCenter,\n'
      '    half4 PixelPosition, half DepthDecalToggle,\n    out half2 Out,')
    s=once(s,'    out float2 BumpUV, out float2 ProgramNoiseUV)',
      '    out float2 BumpUV, out float2 ProgramNoiseUV,\n    out float DecalAlpha)')
    s=once(s,'    out half2 BumpUV, out half2 ProgramNoiseUV)',
      '    out half2 BumpUV, out half2 ProgramNoiseUV,\n    out half DecalAlpha)')
    s=once(s,'    input.meshTexcoord0 = UV;', '''    input.meshTexcoord0 = UV;
    DecalAlpha = 1.0;
    // Only the delegated URP Unlit Forward and its two exact NB distortion
    // clones have the old _DEPTH_DECAL variant. Exclude DepthOnly,
    // ShadowCaster, 2D, Meta, MotionVectors and preview.
    #if !defined(SHADERGRAPH_PREVIEW) && defined(SHADERPASS) && (SHADERPASS == SHADERPASS_UNLIT)
    if (DepthDecalToggle > 0.5)
    {
        float2 screenUV = NBGraphDecalRasterUV(PixelPosition);
        float sceneZBufferDepth = SampleSceneDepth(screenUV);
        #if !UNITY_REVERSED_Z
            sceneZBufferDepth = lerp(UNITY_NEAR_CLIP_VALUE, 1, sceneZBufferDepth);
        #endif
        float3 fragWorldPos = ComputeWorldSpacePosition(screenUV,
            sceneZBufferDepth, UNITY_MATRIX_I_VP);
        // ShaderLab's _CUSTOM_LOCAL_TRANSFORM alternative matrix is a
        // separate pending Graph-host protocol, not silently emulated.
        float3 fragobjectPos = TransformWorldToObject(fragWorldPos);
        NBFX_DepthDecalProjectionV1 depthDecal = NBFX_ResolveDepthDecalV1(fragobjectPos);
        input.meshTexcoord0.xy = depthDecal.uv;
        DecalAlpha = depthDecal.alpha;
    }
    #endif''')
    s=once(s,'    float2 colorBlendResolved, rampColorResolved, noiseResolved, noiseMaskResolved, bumpResolved, programNoiseResolved;',
      '    float2 colorBlendResolved, rampColorResolved, noiseResolved, noiseMaskResolved, bumpResolved, programNoiseResolved;\n    float decalAlphaResolved;')
    s=once(s,'        (float)TWParameter, (float)TWStrength, (float4)PCCenter,',
      '        (float)TWParameter, (float)TWStrength, (float4)PCCenter,') if False else s
    s=once(s,'        (float4)TWParameter, (float)TWStrength, (float4)PCCenter,\n        resolved,',
      '        (float4)TWParameter, (float)TWStrength, (float4)PCCenter,\n'
      '        (float4)PixelPosition, (float)DepthDecalToggle,\n        resolved,')
    s=once(s,'        colorBlendResolved, rampColorResolved, noiseResolved, noiseMaskResolved, bumpResolved, programNoiseResolved);',
      '        colorBlendResolved, rampColorResolved, noiseResolved, noiseMaskResolved, bumpResolved, programNoiseResolved, decalAlphaResolved);')
    s=once(s,'    ProgramNoiseUV = (half2)programNoiseResolved;',
      '    ProgramNoiseUV = (half2)programNoiseResolved;\n    DecalAlpha = (half)decalAlphaResolved;')
    return s

def patch_color(s):
    assert 'DecalAlpha' not in s
    # Exact two top-level function signatures; find the first out argument
    # after all current inputs, regardless of later appended Refraction slots.
    for precision in ('float','half'):
        start=s.index('void NBGraphBaseColor_'+precision+'(')
        out=s.index('    out '+precision+'4 Out,',start)
        s=s[:out]+'    '+precision+' DecalAlpha,\n'+s[out:]
    s=once(s,'    Out.a *= ColorA.a;',
      '    Out.a *= ColorA.a;\n'
      '    // Alpha=1 is the disabled/depth-shadow identity. Do not inject a\n'
      '    // new half truncation into every existing Graph material.\n'
      '    if (!NB_GRAPH_DEPTH_SHADOW_PASS && DecalAlpha != 1.0)\n'
      '        Out.a = (half)((half)Out.a * (half)DecalAlpha);',2)
    return s

def graph_patch(s):
    old=decode(s);o=copy.deepcopy(old);r=o[0]
    by={x['m_ObjectId']:x for x in o if 'm_ObjectId'in x}
    uv=next(x for x in o if x.get('m_FunctionName')=='NBGraphBaseUV')
    cf=next(x for x in o if x.get('m_FunctionName')=='NBGraphBaseColor')
    cat=next(x for x in o if x.get('m_Type')=='UnityEditor.ShaderGraph.CategoryData')
    assert not any(x.get('m_OverrideReferenceName')=='_DepthDecal_Toggle' for x in o)
    assert not any(by[q['m_Id']]['m_DisplayName']=='DecalAlpha' for q in uv['m_Slots'])
    ids={x['m_ObjectId'] for x in o if 'm_ObjectId'in x}
    def add(x):
        assert x['m_ObjectId'] not in ids
        ids.add(x['m_ObjectId']);o.append(x);by[x['m_ObjectId']]=x
    def port(node,template,name,slotid,value=None):
        t=next(by[q['m_Id']] for q in node['m_Slots'] if by[q['m_Id']]['m_DisplayName']==template)
        x=copy.deepcopy(t);x['m_ObjectId']=uid(node['m_ObjectId']+':'+name)
        x['m_Id']=slotid;x['m_DisplayName']=x['m_ShaderOutputName']=name
        if value is not None:x['m_Value']=x['m_DefaultValue']=copy.deepcopy(value)
        node['m_Slots'].append({'m_Id':x['m_ObjectId']});add(x);return x
    def edge(src,srcslot,dst,dstslot):
        r['m_Edges'].append({'m_OutputSlot':{'m_Node':{'m_Id':src},'m_SlotId':srcslot},
            'm_InputSlot':{'m_Node':{'m_Id':dst},'m_SlotId':dstslot}})
    # _DepthDecal_Toggle is the original material property, not an invented
    # keyword/packed-int shadow. Clone the currently generated scalar schema.
    t=next(x for x in o if x.get('m_OverrideReferenceName')=='_DepthOutline_Toggle')
    n=next(x for x in o if x.get('m_Property',{}).get('m_Id')==t['m_ObjectId'])
    p=copy.deepcopy(t);p['m_ObjectId']=uid('prop');p['m_Guid']={'m_GuidSerialized':str(uuid.uuid5(NS,'DepthDecal:prop-guid'))}
    p['m_Name']=p['m_RefNameGeneratedByDisplayName']='DepthDecalToggle'
    p['m_DefaultReferenceName']=p['m_OverrideReferenceName']='_DepthDecal_Toggle';p['m_Value']=0.0
    pn=copy.deepcopy(n);pn['m_ObjectId']=uid('prop-node');pn['m_Property']={'m_Id':p['m_ObjectId']}
    pn['m_DrawState']['m_Position']['y']=27200.0
    pout=copy.deepcopy(by[n['m_Slots'][0]['m_Id']]);pout['m_ObjectId']=uid('prop-out')
    pout['m_DisplayName']='DepthDecalToggle';pn['m_Slots']=[{'m_Id':pout['m_ObjectId']}]
    for x in (p,pn,pout):add(x)
    r['m_Properties'].append({'m_Id':p['m_ObjectId']});r['m_Nodes'].append({'m_Id':pn['m_ObjectId']})
    cat['m_ChildObjectList'].append({'m_Id':p['m_ObjectId']})
    # SG Screen Position / Pixel mode (enum value 4) from the same official
    # node serialized in the current graph. Need raster position, not NDC.
    pix_t=next(x for x in o if x.get('m_Type')=='UnityEditor.ShaderGraph.ScreenPositionNode')
    pix=copy.deepcopy(pix_t);pix['m_ObjectId']=uid('pixel-node');pix['m_ScreenSpaceType']=4
    pix['m_Name']='Screen Position Pixel';pix['m_DrawState']['m_Position']['y']=27320.0
    pixout=copy.deepcopy(by[pix_t['m_Slots'][0]['m_Id']]);pixout['m_ObjectId']=uid('pixel-out')
    pix['m_Slots']=[{'m_Id':pixout['m_ObjectId']}]
    for x in (pix,pixout):add(x)
    r['m_Nodes'].append({'m_Id':pix['m_ObjectId']})

    nextuv=max(by[q['m_Id']]['m_Id'] for q in uv['m_Slots'])+1
    # Append inputs after old m_Slots and output last. Official CF generator
    # separately filters inputs/outputs but preserves their serialized order.
    pixelin=port(uv,'SharedUVVec','PixelPosition',nextuv,dict(x=0,y=0,z=0,w=0));nextuv+=1
    togglein=port(uv,'TWStrength','DepthDecalToggle',nextuv,0.0);nextuv+=1
    # Scalar output schema from an existing CF output, not a made-up type.
    out_t=next(by[q['m_Id']] for q in cf['m_Slots'] if by[q['m_Id']]['m_DisplayName']=='NBDistortionNoiseMask')
    decalout=copy.deepcopy(out_t);decalout['m_ObjectId']=uid('uv:DecalAlpha');decalout['m_Id']=nextuv
    decalout['m_DisplayName']=decalout['m_ShaderOutputName']='DecalAlpha'
    decalout['m_Value']=decalout['m_DefaultValue']=1.0
    uv['m_Slots'].append({'m_Id':decalout['m_ObjectId']});add(decalout)
    nextcf=max(by[q['m_Id']]['m_Id'] for q in cf['m_Slots'])+1
    decalinput=port(cf,'PNoiseDistortBlendOpacity','DecalAlpha',nextcf,1.0)
    edge(pn['m_ObjectId'],pout['m_Id'],uv['m_ObjectId'],togglein['m_Id'])
    edge(pix['m_ObjectId'],pixout['m_Id'],uv['m_ObjectId'],pixelin['m_Id'])
    edge(uv['m_ObjectId'],decalout['m_Id'],cf['m_ObjectId'],decalinput['m_Id'])
    changed={a['m_ObjectId'] for a,b in zip(o[:len(old)],old) if a!=b and 'm_ObjectId'in a}
    assert changed=={r['m_ObjectId'],cat['m_ObjectId'],uv['m_ObjectId'],cf['m_ObjectId']},changed
    assert len(o)==len(old)+9,(len(old),len(o))
    return '\n\n'.join(json.dumps(x,ensure_ascii=False,indent=4) for x in o)+'\n',{
        'oldObjectCount':len(old),'newObjectCount':len(o),
        'modifiedExistingObjectIds':sorted(changed),
        'uvNewSlotIds':[pixelin['m_Id'],togglein['m_Id'],decalout['m_Id']],
        'colorNewSlotId':decalinput['m_Id'],
        'newObjectIds':[x['m_ObjectId'] for x in o[len(old):]],
    }

def main():
    sources={k:v.read_bytes() for k,v in F.items()}
    previews={}
    previews['graph'],audit=graph_patch(sources['graph'].decode())
    previews['uv']=patch_uv(sources['uv'].decode())
    previews['color']=patch_color(sources['color'].decode())
    previews['legacy']=patch_legacy(sources['legacy'].decode())
    previews['pure']=PURE
    OUT.mkdir(parents=True,exist_ok=True)
    for k,v in previews.items():
        filename='NBShaderDepthDecalV1.hlsl' if k=='pure' else F[k].name
        (OUT/filename).write_text(v)
    (OUT/'NBShaderDepthDecalV1.hlsl.meta').write_text('fileFormatVersion: 2\nguid: d8a936402b654b6da32670e4af2fc645\n')
    audit.update({'scope':'Preview-only ordinary Mesh DepthDecal; no Unity/Gate claim',
      'sourceSha256':{k:sha(v) for k,v in sources.items()},
      'previewSha256':{k:sha(v.encode()) for k,v in previews.items()},
      'previewDirectory':str(OUT)})
    (OUT/'audit.json').write_text(json.dumps(audit,indent=2)+'\n')
    print(json.dumps(audit,indent=2))

if __name__=='__main__':main()
