#!/usr/bin/env python3
"""POM1 ordinary Mesh dynamic PREVIEW only; never writes package products.

Every run reads live Graph/CF/legacy/SubTarget so DepthDecal/RF append-only
changes are preserved. Parent reviews and applies previews, owns Unity/Git.
"""
import copy, hashlib, json, re, uuid
from pathlib import Path
P=Path('/Users/bytedance/UnityProject/NBUnityProject/Packages/NB_FX')
F={'graph':P/'NBShaders2/ShaderGraph/NBShaderGraph.shadergraph',
   'color':P/'NBShaders2/ShaderGraph/NBGraphBaseColor.hlsl',
   'legacy':P/'NBShaders2/Shader/HLSL/NBShaderInput.hlsl',
   'subtarget':P/'NBShaders2/ShaderGraph/Editor/NBGraphUnlitSubTarget.cs'}
OUT=Path('/tmp/nbfx-parallax-preview')
NS=uuid.UUID('9cd3106a-70e3-46b0-a7ba-e967cf245ec0')
def uid(key):return uuid.uuid5(NS,'POM1:'+key).hex
def one(s,a,b,n=1):
    c=s.count(a)
    if c!=n:raise AssertionError(f'needle {a[:100]!r}: {c}, expected {n}')
    return s.replace(a,b)
def decode(s):
    d=json.JSONDecoder();i=0;o=[]
    while i<len(s):
        while i<len(s) and s[i].isspace():i+=1
        if i==len(s):break
        x,i=d.raw_decode(s,i);o.append(x)
    return o

def helper():
    return '''#ifndef NB_SHADER_PARALLAX_V1_INCLUDED
#define NB_SHADER_PARALLAX_V1_INCLUDED

// Both hosts declare these four named sampler states before this include.
// This is the original NB per-feature wrap/LOD sampler, not the texture
// asset's sampler or ShaderGraph's HDR-decoded sample.
half4 NBFX_SampleParallaxRawV1(Texture2D map, float2 uv,
    uint wrapMode, bool forceLod0)
{
#if defined(SHADER_TARGET_GLSL) || defined(SHADER_API_GLES) || defined(SHADER_API_GLES3)
    switch (wrapMode)
    {
        case 0: uv = frac(uv); break;
        case 1: uv = saturate(uv); break;
        case 2: uv = float2(frac(uv.x), saturate(uv.y)); break;
        case 3: uv = float2(saturate(uv.x), frac(uv.y)); break;
    }
    half4 sampled;
    UNITY_BRANCH
    if (forceLod0) sampled = SAMPLE_TEXTURE2D_LOD(map, sampler_linear_clamp, uv, 0);
    else sampled = SAMPLE_TEXTURE2D(map, sampler_linear_clamp, uv);
    return sampled;
#else
    half4 sampled;
    switch (wrapMode)
    {
        case 0:
            UNITY_BRANCH
            if (forceLod0) sampled = SAMPLE_TEXTURE2D_LOD(map, sampler_linear_repeat, uv, 0);
            else sampled = SAMPLE_TEXTURE2D(map, sampler_linear_repeat, uv);
            break;
        case 1:
            UNITY_BRANCH
            if (forceLod0) sampled = SAMPLE_TEXTURE2D_LOD(map, sampler_linear_clamp, uv, 0);
            else sampled = SAMPLE_TEXTURE2D(map, sampler_linear_clamp, uv);
            break;
        case 2:
            UNITY_BRANCH
            if (forceLod0) sampled = SAMPLE_TEXTURE2D_LOD(map, sampler_linear_RepeatU_ClampV, uv, 0);
            else sampled = SAMPLE_TEXTURE2D(map, sampler_linear_RepeatU_ClampV, uv);
            break;
        case 3:
            UNITY_BRANCH
            if (forceLod0) sampled = SAMPLE_TEXTURE2D_LOD(map, sampler_linear_ClampU_RepeatV, uv, 0);
            else sampled = SAMPLE_TEXTURE2D(map, sampler_linear_ClampU_RepeatV, uv);
            break;
        default:
            UNITY_BRANCH
            if (forceLod0) sampled = SAMPLE_TEXTURE2D_LOD(map, sampler_linear_repeat, uv, 0);
            else sampled = SAMPLE_TEXTURE2D(map, sampler_linear_repeat, uv);
            break;
    }
    return sampled;
#endif
}

// Literal extraction of NBShaderInput.hlsl ParallaxOcclusionMapping's
// arithmetic, float/half parameters, [loop] and sample sequence.
// No clamp/safety branch is added to the legacy numerical contract.
float2 NBFX_ParallaxOcclusionMappingV1(Texture2D map,
    float2 texCoords, float3 viewDir, half4 mapST, half intensity,
    half4 layerVec, uint wrapMode, bool forceLod0)
{
    texCoords = texCoords * mapST + mapST.zw;
    const float minLayers = layerVec.x;
    const float maxLayers = layerVec.y;
    float numLayers = lerp(maxLayers, minLayers, abs(dot(half3(0.0, 0.0, 1.0), viewDir)));
    float layerDepth = 1.0 / numLayers;
    float currentLayerDepth = 0.0;
    float2 P = viewDir.xy / viewDir.z * intensity;
    float2 deltaTexCoords = P / numLayers;
    float2 currentTexCoords = texCoords;
    float currentDepthMapValue = NBFX_SampleParallaxRawV1(map, currentTexCoords, wrapMode, forceLod0).r;
    currentLayerDepth = clamp(currentLayerDepth, 0, 1);
    int i = 0;
    [loop]
    while (currentLayerDepth < currentDepthMapValue && i < numLayers)
    {
        currentTexCoords -= deltaTexCoords;
        currentDepthMapValue = NBFX_SampleParallaxRawV1(map, currentTexCoords, wrapMode, forceLod0).r;
        currentLayerDepth += layerDepth;
        i++;
    }
    float2 prevTexCoords = currentTexCoords + deltaTexCoords;
    float afterDepth = currentDepthMapValue - currentLayerDepth;
    float beforeDepth = NBFX_SampleParallaxRawV1(map, prevTexCoords, wrapMode, forceLod0).r - currentLayerDepth + layerDepth;
    float weight = afterDepth / (afterDepth - beforeDepth);
    float2 finalTexCoords = prevTexCoords * weight + currentTexCoords * (1.0 - weight);
    return finalTexCoords;
}

#endif
'''

def patch_legacy(s):
    assert 'NBShaderParallaxV1.hlsl' not in s
    s=one(s,'    half GetColorChannel(half4 color, int bitPos)',
        '    #include "NBShaderParallaxV1.hlsl"\n\n    half GetColorChannel(half4 color, int bitPos)')
    a=s.index('    float2 ParallaxOcclusionMapping(float2 texCoords, float3 viewDir, bool forceLod0)')
    b=s.index('    {',a);depth=0;end=None
    for i in range(b,len(s)):
        if s[i]=='{':depth+=1
        elif s[i]=='}':
            depth-=1
            if depth==0:end=i+1;break
    assert end
    replacement='''    float2 ParallaxOcclusionMapping(float2 texCoords, float3 viewDir, bool forceLod0)
    {
        return NBFX_ParallaxOcclusionMappingV1(_ParallaxMapping_Map,
            texCoords, viewDir, _ParallaxMapping_Map_ST,
            _ParallaxMapping_Intensity, _ParallaxMapping_Vec,
            CheckLocalWrapFlags(FLAG_BIT_WRAPMODE_PARALLAXMAPPINGMAP), forceLod0);
    }'''
    return s[:a]+replacement+s[end:]

def patch_subtarget(s):
    assert 'NB_GRAPH_MAIN_FORWARD' not in s
    s=one(s,'passes.Add(forward ? WithNBLightingKeywords(WithNBFragmentInclude(pass, "NBGraphForwardPass.hlsl")) :',
      'passes.Add(forward ? WithNBMainForwardDefine(WithNBLightingKeywords(WithNBFragmentInclude(pass, "NBGraphForwardPass.hlsl"))) :')
    marker='        static PassDescriptor WithNBDistortionBlocks(PassDescriptor pass)'
    method='''        // Static pass define, not a material keyword or new variant axis.
        // Copy the delegated collection: two NB distortion clones use the
        // original pass and must never inherit POM from main Forward.
        static PassDescriptor WithNBMainForwardDefine(PassDescriptor pass)
        {
            var defines = pass.defines == null ? new DefineCollection() :
                new DefineCollection(pass.defines);
            defines.Add(new KeywordDescriptor
            {
                referenceName = "NB_GRAPH_MAIN_FORWARD",
                type = KeywordType.Boolean,
                definition = KeywordDefinition.Predefined,
            }, 1);
            pass.defines = defines;
            return pass;
        }

'''+marker
    return one(s,marker,method)

def pom_adapter():
    return '''// POM uses the pre-normalmap fragment basis. Current URP SharedCode already
// forms fragment BitangentWS with tangentOS.w * GetOddNegativeScale().
// This differs from the vertex-stage SixWay bake adapter: do not apply odd twice.
float2 NBGraphApplyParallax(UnityTexture2D map, float2 baseUV,
    float intensity, float4 layerVec, float3 normalWS,
    float3 tangentWS, float3 bitangentWS, float3 viewDirWS,
    float isFrontFace, uint wrapFlags, uint noMipFlags)
{
    half3 rawN = (half3)normalWS;
    half3 tangent = (half3)tangentWS;
    half tangentSign = dot((half3)bitangentWS,
        cross(rawN, tangent)) < 0.0h ? -1.0h : 1.0h;
    half3 facedN = isFrontFace > 0.5 ? rawN : -rawN;
    half3 bitangent = tangentSign * cross(facedN, tangent);
    half3x3 tangentToWorld = half3x3(tangent, bitangent, facedN);
    float3 tangentViewDir = (float3)SafeNormalize(mul(tangentToWorld, (half3)viewDirWS));
    return NBFX_ParallaxOcclusionMappingV1(map.tex, baseUV,
        tangentViewDir, (half4)map.scaleTranslate, (half)intensity,
        (half4)layerVec,
        NBGraphMaskWrapMode(wrapFlags, FLAG_BIT_WRAPMODE_PARALLAXMAPPINGMAP),
        (noMipFlags & FLAG_BIT_FORCE_NO_MIP_PARALLAXMAPPINGMAP) != 0u);
}

'''

def patch_color(s):
    assert 'NBGraphApplyParallax' not in s
    s=one(s,'#include "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBGraphSampling.hlsl"',
      '#include "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBGraphSampling.hlsl"\n'
      '#include "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/HLSL/NBShaderParallaxV1.hlsl"')
    s=one(s,'void NBGraphBaseColor_float(',pom_adapter()+'void NBGraphBaseColor_float(')
    for precision in ('float','half'):
        start=s.index('void NBGraphBaseColor_'+precision+'(')
        out=s.index('    out '+precision+'4 Out,',start)
        s=s[:out]+'''    UnityTexture2D ParallaxMappingMap, float ParallaxMappingToggle,
    float ParallaxMappingIntensity, float4 ParallaxMappingVec,
'''+s[out:]
    before='''    float2 baseUV = BaseMapUV + mainTexNoise;'''
    after='''    // ShaderLab saves originUV before POM. Do not alter other feature UVs.
    float2 baseUVPreNoise = BaseMapUV;
#if defined(NB_GRAPH_MAIN_FORWARD) && !NB_GRAPH_DEPTH_SHADOW_PASS
    if (ParallaxMappingToggle > 0.5)
        baseUVPreNoise = NBGraphApplyParallax(ParallaxMappingMap,
            BaseMapUV, ParallaxMappingIntensity, ParallaxMappingVec,
            NormalWS, TangentWS, BitangentWS, ViewDirWS,
            IsFrontFace, wrapFlags, noMipFlags);
#endif
    float2 baseUV = baseUVPreNoise + mainTexNoise;'''
    s=one(s,before,after,2)
    return s

def patch_graph(s):
    old=decode(s);o=copy.deepcopy(old);root=o[0]
    by={x['m_ObjectId']:x for x in o if 'm_ObjectId'in x}
    cf=next(x for x in o if x.get('m_FunctionName')=='NBGraphBaseColor')
    cat=next(x for x in o if x.get('m_Type')=='UnityEditor.ShaderGraph.CategoryData')
    assert not any(x.get('m_OverrideReferenceName')=='_ParallaxMapping_Toggle' for x in o)
    ids=set(by)
    def add(x):
        assert x['m_ObjectId'] not in ids
        ids.add(x['m_ObjectId']);o.append(x);by[x['m_ObjectId']]=x
    def edge(src,ss,dst,ds):
        root['m_Edges'].append({'m_OutputSlot':{'m_Node':{'m_Id':src},'m_SlotId':ss},
                                'm_InputSlot':{'m_Node':{'m_Id':dst},'m_SlotId':ds}})
    def prop(ref,template,value,xy):
        t=next(x for x in old if x.get('m_OverrideReferenceName')==template)
        n=next(x for x in old if x.get('m_Property',{}).get('m_Id')==t['m_ObjectId'])
        p=copy.deepcopy(t);p['m_ObjectId']=uid('property:'+ref)
        p['m_Guid']={'m_GuidSerialized':str(uuid.uuid5(NS,'property-guid:'+ref))}
        name=ref.lstrip('_').replace('_','')
        p['m_Name']=p['m_RefNameGeneratedByDisplayName']=name
        p['m_DefaultReferenceName']=p['m_OverrideReferenceName']=ref
        if value is not None:p['m_Value']=copy.deepcopy(value)
        if ref=='_ParallaxMapping_Map':p['m_DefaultType']=0
        pn=copy.deepcopy(n);pn['m_ObjectId']=uid('node:'+ref)
        pn['m_Property']={'m_Id':p['m_ObjectId']}
        pn['m_DrawState']['m_Position']['x']=xy[0];pn['m_DrawState']['m_Position']['y']=xy[1]
        out=copy.deepcopy(by[n['m_Slots'][0]['m_Id']]);out['m_ObjectId']=uid('node-port:'+ref)
        out['m_DisplayName']=name
        if value is not None and 'm_Value' in out:
            out['m_Value']=copy.deepcopy(value);out['m_DefaultValue']=copy.deepcopy(value)
        pn['m_Slots']=[{'m_Id':out['m_ObjectId']}]
        for x in (p,pn,out):add(x)
        root['m_Properties'].append({'m_Id':p['m_ObjectId']});root['m_Nodes'].append({'m_Id':pn['m_ObjectId']})
        cat['m_ChildObjectList'].append({'m_Id':p['m_ObjectId']})
        return pn,out
    def port(name,template,value):
        t=next(by[q['m_Id']] for q in cf['m_Slots'] if by[q['m_Id']]['m_DisplayName']==template)
        v=copy.deepcopy(t);v['m_ObjectId']=uid('cf-port:'+name)
        v['m_Id']=max(by[q['m_Id']]['m_Id'] for q in cf['m_Slots'])+1
        v['m_DisplayName']=v['m_ShaderOutputName']=name
        if value is not None and 'm_Value' in v:
            v['m_Value']=copy.deepcopy(value);v['m_DefaultValue']=copy.deepcopy(value)
        cf['m_Slots'].append({'m_Id':v['m_ObjectId']});add(v)
        return v
    specs=[('_ParallaxMapping_Map','_BumpTex',None,'ParallaxMappingMap','BumpTex',None),
           ('_ParallaxMapping_Toggle','_DepthOutline_Toggle',0.0,'ParallaxMappingToggle','BumpMapToggle',0.0),
           ('_ParallaxMapping_Intensity','_BumpScale',0.05,'ParallaxMappingIntensity','BumpScale',0.05),
           ('_ParallaxMapping_Vec','_Dissolve',{'x':5.0,'y':30.0,'z':0.0,'w':0.0},'ParallaxMappingVec','Dissolve',{'x':5.0,'y':30.0,'z':0.0,'w':0.0})]
    for i,(ref,t,v,name,slot,val) in enumerate(specs):
        node,out=prop(ref,t,v,(620.0,28000.0+120*i))
        cp=port(name,slot,val)
        edge(node['m_ObjectId'],out['m_Id'],cf['m_ObjectId'],cp['m_Id'])
    modified={a['m_ObjectId'] for a,b in zip(o[:len(old)],old) if a!=b and 'm_ObjectId'in a}
    assert modified=={root['m_ObjectId'],cat['m_ObjectId'],cf['m_ObjectId']},modified
    assert len(o)==len(old)+16,(len(o),len(old))
    return '\n\n'.join(json.dumps(x,ensure_ascii=False,indent=4) for x in o)+'\n',{
        'originalObjects':len(old),'newObjects':len(o),'modifiedOldIds':sorted(modified),
        'newIds':[x['m_ObjectId'] for x in o[len(old):]],
        'newInputIds':[by[q['m_Id']]['m_Id'] for q in cf['m_Slots'][-4:]]}

def main():
    src={k:p.read_bytes() for k,p in F.items()}
    previews={}
    previews['helper']=helper()
    previews['legacy']=patch_legacy(src['legacy'].decode())
    previews['subtarget']=patch_subtarget(src['subtarget'].decode())
    previews['color']=patch_color(src['color'].decode())
    previews['graph'],audit=patch_graph(src['graph'].decode())
    OUT.mkdir(parents=True,exist_ok=True)
    target={'helper':'NBShaders2/Shader/HLSL/NBShaderParallaxV1.hlsl',
            'legacy':'NBShaders2/Shader/HLSL/NBShaderInput.hlsl',
            'subtarget':'NBShaders2/ShaderGraph/Editor/NBGraphUnlitSubTarget.cs',
            'color':'NBShaders2/ShaderGraph/NBGraphBaseColor.hlsl',
            'graph':'NBShaders2/ShaderGraph/NBShaderGraph.shadergraph'}
    info={'mode':'preview-only','note':'No product writes; root reviews and applies after other slices.',
          'graphAudit':audit,'files':{}}
    for k,s in previews.items():
        b=s.encode();dst=OUT/Path(target[k]).name;dst.write_bytes(b)
        info['files'][k]={'source':str(F[k]) if k in F else None,
                          'target':str(P/target[k]),'preview':str(dst),
                          'inputSHA256':hashlib.sha256(src[k]).hexdigest() if k in src else None,
                          'previewSHA256':hashlib.sha256(b).hexdigest()}
    (OUT/'manifest.json').write_text(json.dumps(info,indent=2)+'\n')
    print(json.dumps(info,indent=2))
if __name__=='__main__':main()
