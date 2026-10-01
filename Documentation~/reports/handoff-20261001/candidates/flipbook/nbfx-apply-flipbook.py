#!/usr/bin/env python3
"""F0 ordinary Mesh Flipbook dynamic PREVIEW ONLY; no package/Unity/Git writes.

Reads current CA/FG Graph, BaseUV, BaseColor and legacy includes every run;
appends Graph slots/properties/UV3 node without replacing serialized IDs.
"""
import copy,hashlib,json,uuid
from pathlib import Path
P=Path('/Users/bytedance/UnityProject/NBUnityProject/Packages/NB_FX')
F={
 'graph':P/'NBShaders2/ShaderGraph/NBShaderGraph.shadergraph',
 'baseuv':P/'NBShaders2/ShaderGraph/NBGraphBaseUV.hlsl',
 'color':P/'NBShaders2/ShaderGraph/NBGraphBaseColor.hlsl',
 'shared':P/'NBShaders2/Shader/HLSL/NBShaderUVV1.hlsl',
 'contract':P/'NBShaders2/Shader/HLSL/NBShaderSharedContractV1.hlsl',
 'legacy':P/'NBShaders2/Shader/HLSL/NBShaderInput.hlsl',
 'forward':P/'NBShaders2/Shader/HLSL/NBShaderForwardPass.hlsl',
 'helper':P/'XuanXuanRenderUtility/Runtime/AnimationSheetHelper.cs',
}
OUT=Path('/tmp/nbfx-flipbook-preview')
NS=uuid.UUID('fc8d41de-5b0e-439a-830d-a917b720b61b')
def uid(k):return uuid.uuid5(NS,'F0:'+k).hex
def one(s,a,b,n=1):
 c=s.count(a)
 if c!=n:raise AssertionError(f'needle {a[:100]!r}: {c} expected {n}')
 return s.replace(a,b)
def decode(s):
 d=json.JSONDecoder();i=0;o=[]
 while i<len(s):
  while i<len(s) and s[i].isspace():i+=1
  if i==len(s):break
  x,i=d.raw_decode(s,i);o.append(x)
 return o

def patch_shared(s):
 assert 'NBFX_ResolveFlipbookUVV1' not in s
 s=one(s,"""#else
    if ((parameters.flags1 & FLAG_BIT_PARTICLE_1_UV_FROM_MESH) != 0u)
""","""#else
    // Graph has no Flipbook variant. Keep the particle TEXCOORD3.yz
    // exception and preserve the old non-Flipbook path when zero.
    if (parameters.flipbookBlending != 0u &&
        (parameters.flags1 & FLAG_BIT_PARTICLE_1_IS_PARTICLE_SYSTEM) != 0u &&
        (parameters.flags1 & FLAG_BIT_PARTICLE_1_USE_TEXCOORD2) != 0u)
    {
        specialUVChannel = input.specialUVInTexcoord3;
    }
    else if (parameters.flipbookBlending == 0u &&
        (parameters.flags1 & FLAG_BIT_PARTICLE_1_UV_FROM_MESH) != 0u)
""")
 insert='''// Pure original animation-sheet secondary UV and blend-weight selection.
// These do not advance frames; UV0.zw/TEXCOORD3.x or AnimationSheetHelper
// already supply the next frame and fractional blend.
float2 NBFX_ResolveFlipbookUVV1(float4 meshTexcoord0,
    float4 animationSheetBlendST, bool helper)
{
    return helper ? meshTexcoord0.xy * animationSheetBlendST.xy +
        animationSheetBlendST.zw : meshTexcoord0.zw;
}
float NBFX_ResolveFlipbookWeightV1(float streamWeight,
    half helperWeight, bool helper)
{
    return helper ? (float)helperWeight : (float)(half)streamWeight;
}

'''
 assert s.endswith('#endif\n')
 return s[:-len('#endif\n')]+insert+'#endif\n'

def patch_contract(s):
 return one(s,"""    uint flags0;
    uint flags1;
""","""    uint flags0;
    uint flags1;
    uint flipbookBlending;
""",1)

def patch_legacy(s):
 a='''        #ifdef _FLIPBOOKBLENDING_ON //开启序列帧融合
        if(CheckLocalFlags1(FLAG_BIT_PARTICLE_1_ANIMATION_SHEET_HELPER))
        {
            // float2 baseMapUV = (baseUVs.mainTexUV - _BaseMap_ST.zw)/_BaseMap_ST.xy;
            //走AnimationSheetHelper脚本的情况，永远和baseMap同步。
            particleUVs.animBlendUV = meshTexcoord0.xy*_BaseMap_AnimationSheetBlend_ST.xy+_BaseMap_AnimationSheetBlend_ST.zw;
        }
        else
        {
            //走粒子的情况
            particleUVs.animBlendUV = meshTexcoord0.zw;
        }
        #endif'''
 b='''        #ifdef _FLIPBOOKBLENDING_ON //开启序列帧融合
        particleUVs.animBlendUV = NBFX_ResolveFlipbookUVV1(meshTexcoord0,
            _BaseMap_AnimationSheetBlend_ST,
            CheckLocalFlags1(FLAG_BIT_PARTICLE_1_ANIMATION_SHEET_HELPER));
        #endif'''
 return one(s,a,b)

def patch_forward(s):
 a='''        if (CheckLocalFlags1(FLAG_BIT_PARTICLE_1_ANIMATION_SHEET_HELPER))
        {
            blendUv.z = _AnimationSheetHelperBlendIntensity;
        }
        else
        {
            blendUv.z = input.normalWSAndAnimBlend.w;
        }'''
 b='''        blendUv.z = NBFX_ResolveFlipbookWeightV1(
            input.normalWSAndAnimBlend.w,
            _AnimationSheetHelperBlendIntensity,
            CheckLocalFlags1(FLAG_BIT_PARTICLE_1_ANIMATION_SHEET_HELPER));'''
 return one(s,a,b)

def patch_baseuv(s):
 # All new inputs follow existing PixelPosition/DepthDecalToggle; outputs
 # remain after input list in the HLSL signature, as CustomFunction emits.
 for precision in ('float','half'):
  old=f'''    {precision}4 PixelPosition, {precision} DepthDecalToggle,
    out {precision}2 Out,'''
  new=f'''    {precision}4 PixelPosition, {precision} DepthDecalToggle,
    {precision}4 UV3, {precision} FlipbookToggle,
    {precision}4 AnimationSheetBlendST,
    {precision} AnimationSheetBlendIntensity,
    out {precision}2 Out,'''
  s=one(s,old,new)
  old=f'''    out {precision}2 BumpUV, out {precision}2 ProgramNoiseUV,
    out {precision} DecalAlpha)'''
  new=f'''    out {precision}2 BumpUV, out {precision}2 ProgramNoiseUV,
    out {precision} DecalAlpha, out {precision}2 BlendUV,
    out {precision} BlendWeight)'''
  s=one(s,old,new)
 marker='''    BaseUVs resolved = NBFX_BuildBaseUVsV1(input, parameters);
    Out = resolved.mainTexUV;'''
 repl='''    BaseUVs resolved = NBFX_BuildBaseUVsV1(input, parameters);
    bool animationHelper = (flags1 & FLAG_BIT_PARTICLE_1_ANIMATION_SHEET_HELPER) != 0u;
    // input.meshTexcoord0.xy may already be replaced by DepthDecal.
    BlendUV = NBFX_ResolveFlipbookUVV1(input.meshTexcoord0,
        AnimationSheetBlendST, animationHelper);
    BlendWeight = NBFX_ResolveFlipbookWeightV1(UV3.x,
        (half)AnimationSheetBlendIntensity, animationHelper);
    Out = resolved.mainTexUV;'''
 s=one(s,marker,repl)
 s=one(s,'''    input.custom1 = UV1;
    input.custom2 = UV2;''','''    input.custom1 = UV1;
    input.custom2 = UV2;
    input.specialUVInTexcoord3 = UV3.yz;''')
 s=one(s,'''    parameters.flags1 = flags1 &
        (FLAG_BIT_PARTICLE_1_UV_FROM_MESH | FLAG_BIT_PARTICLE_1_USE_TEXCOORD1 |
         FLAG_BIT_PARTICLE_1_USE_TEXCOORD2);''','''    parameters.flipbookBlending = FlipbookToggle > 0.5 ? 1u : 0u;
    parameters.flags1 = flags1 &
        (FLAG_BIT_PARTICLE_1_UV_FROM_MESH | FLAG_BIT_PARTICLE_1_USE_TEXCOORD1 |
         FLAG_BIT_PARTICLE_1_USE_TEXCOORD2 | FLAG_BIT_PARTICLE_1_IS_PARTICLE_SYSTEM);''')
 s=one(s,'''    float decalAlphaResolved;
    NBGraphBaseUV_float''','''    float decalAlphaResolved, blendWeightResolved;
    float2 blendUVResolved;
    NBGraphBaseUV_float''')
 s=one(s,'''        (float4)PixelPosition, (float)DepthDecalToggle,
        resolved,''','''        (float4)PixelPosition, (float)DepthDecalToggle,
        (float4)UV3, (float)FlipbookToggle,
        (float4)AnimationSheetBlendST,
        (float)AnimationSheetBlendIntensity,
        resolved,''')
 s=one(s,'''programNoiseResolved, decalAlphaResolved);''','''programNoiseResolved, decalAlphaResolved,
        blendUVResolved, blendWeightResolved);''')
 s=one(s,'''    DecalAlpha = (half)decalAlphaResolved;''','''    DecalAlpha = (half)decalAlphaResolved;
    BlendUV = (half2)blendUVResolved;
    BlendWeight = (half)blendWeightResolved;''')
 return s

def patch_color(s):
 assert 'NBGraphSampleFlipbookBaseV1' not in s
 adapter='''// The two samples share the exact BaseMap wrap, HDR decode and force-LOD0
// path. Alpha selection/masks/lighting consume the already-blended half4.
half4 NBGraphSampleFlipbookBaseV1(UnityTexture2D map,
    float2 primaryUV, float2 blendUV, float blendWeight,
    float flipbookToggle, uint wrapFlags, uint noMipFlags)
{
    uint wrap = NBGraphBaseMapWrapMode(wrapFlags);
    bool lod0 = (noMipFlags & FLAG_BIT_FORCE_NO_MIP_BASEMAP) != 0u;
    half4 first = NBGraphSampleMap(map, primaryUV, wrap, lod0);
    if (flipbookToggle <= 0.5) return first;
    half4 second = NBGraphSampleMap(map, blendUV, wrap, lod0);
    return lerp(first, second, blendWeight);
}

'''
 s=one(s,'void NBGraphBaseColor_float(',adapter+'void NBGraphBaseColor_float(')
 for precision in ('float','half'):
  st=s.index('void NBGraphBaseColor_'+precision+'(')
  out=s.index('    out '+precision+'4 Out,',st)
  s=s[:out]+'''    float FlipbookToggle, float2 BlendUV, float BlendWeight,
'''+s[out:]
 # One base sample per precision; CA-on branch remains above the new call.
 old='''        baseSample = NBGraphSampleMap(BaseMap, baseUV,
            NBGraphBaseMapWrapMode(wrapFlags),
            (noMipFlags & FLAG_BIT_FORCE_NO_MIP_BASEMAP) != 0u);'''
 new='''        baseSample = NBGraphSampleFlipbookBaseV1(BaseMap, baseUV,
            BlendUV + mainTexNoise, BlendWeight, FlipbookToggle,
            wrapFlags, noMipFlags);'''
 return one(s,old,new,2)

def patch_helper(s):
 # Preserve legacy integer operations byte-for-byte, adding a Graph split-word
 # branch to the three existing helpers; callers keep the same control flow.
 marker='''    private static readonly int NBShaderFlags1Id = Shader.PropertyToID("_NBShaderFlags1");'''
 add='''    private static readonly int GraphFlags0LoId = Shader.PropertyToID("_NB_Flags0Lo16");
    private static readonly int GraphFlags0HiId = Shader.PropertyToID("_NB_Flags0Hi16");
    private static readonly int GraphFlags1LoId = Shader.PropertyToID("_NB_Flags1Lo16");
    private static readonly int GraphFlags1HiId = Shader.PropertyToID("_NB_Flags1Hi16");'''
 s=one(s,marker,marker+'\n'+add)
 marker='''    private static void SetFlagBits(Material material, int propertyId, int bits)
'''
 add='''    private static bool IsGraphFlagWord(Material material, int propertyId)
    {
        return material && (propertyId == GraphFlags0LoId || propertyId == GraphFlags1LoId) &&
            material.HasProperty(GraphFlags0LoId) && material.HasProperty(GraphFlags0HiId) &&
            material.HasProperty(GraphFlags1LoId) && material.HasProperty(GraphFlags1HiId);
    }

    private static uint ReadGraphFlagWord(Material material, int lowId, int highId)
    {
        uint lo = (uint)Mathf.RoundToInt(Mathf.Clamp(material.GetFloat(lowId),0,65535));
        uint hi = (uint)Mathf.RoundToInt(Mathf.Clamp(material.GetFloat(highId),0,65535));
        return lo | (hi << 16);
    }

    private static void WriteGraphFlagWord(Material material, int lowId, int highId, uint value)
    {
        material.SetFloat(lowId, value & 65535u);
        material.SetFloat(highId, (value >> 16) & 65535u);
    }

'''
 s=one(s,marker,add+marker)
 s=one(s,'''        material.SetInteger(propertyId, material.GetInteger(propertyId) | bits);''','''        if (IsGraphFlagWord(material, propertyId))
        {
            int highId = propertyId == GraphFlags0LoId ? GraphFlags0HiId : GraphFlags1HiId;
            WriteGraphFlagWord(material, propertyId, highId,
                ReadGraphFlagWord(material, propertyId, highId) | (uint)bits);
            return;
        }
        material.SetInteger(propertyId, material.GetInteger(propertyId) | bits);''')
 s=one(s,'''        material.SetInteger(propertyId, material.GetInteger(propertyId) & ~bits);''','''        if (IsGraphFlagWord(material, propertyId))
        {
            int highId = propertyId == GraphFlags0LoId ? GraphFlags0HiId : GraphFlags1HiId;
            WriteGraphFlagWord(material, propertyId, highId,
                ReadGraphFlagWord(material, propertyId, highId) & ~(uint)bits);
            return;
        }
        material.SetInteger(propertyId, material.GetInteger(propertyId) & ~bits);''')
 s=one(s,'''        return material && (material.GetInteger(propertyId) & bits) != 0;''','''        if (IsGraphFlagWord(material, propertyId))
        {
            int highId = propertyId == GraphFlags0LoId ? GraphFlags0HiId : GraphFlags1HiId;
            return (ReadGraphFlagWord(material, propertyId, highId) & (uint)bits) != 0u;
        }
        return material && (material.GetInteger(propertyId) & bits) != 0;''')
 marker='''    private static void GetShaderFlagIds(Material material, out int flagsId, out int flags1Id)
    {
'''
 repl=marker+'''        if (material && material.HasProperty(GraphFlags0LoId) &&
            material.HasProperty(GraphFlags0HiId) && material.HasProperty(GraphFlags1LoId) &&
            material.HasProperty(GraphFlags1HiId))
        {
            flagsId = GraphFlags0LoId;
            flags1Id = GraphFlags1LoId;
            return;
        }
'''
 return one(s,marker,repl)

def patch_graph(s):
 old=decode(s);o=copy.deepcopy(old);root=o[0]
 by={x['m_ObjectId']:x for x in o if 'm_ObjectId' in x};ids=set(by)
 assert not any(x.get('m_OverrideReferenceName')=='_FlipbookBlending' for x in o)
 uv=next(x for x in o if x.get('m_FunctionName')=='NBGraphBaseUV')
 color=next(x for x in o if x.get('m_FunctionName')=='NBGraphBaseColor')
 cat=next(x for x in o if x.get('m_Type')=='UnityEditor.ShaderGraph.CategoryData')
 def add(x):
  assert x['m_ObjectId'] not in ids;xid=x['m_ObjectId'];ids.add(xid);by[xid]=x;o.append(x)
 def slot(node,name):return next(by[z['m_Id']] for z in node['m_Slots'] if by[z['m_Id']]['m_DisplayName']==name)
 def edge(src,srcid,dst,dstid):
  root['m_Edges'].append({'m_OutputSlot':{'m_Node':{'m_Id':src},'m_SlotId':srcid},
    'm_InputSlot':{'m_Node':{'m_Id':dst},'m_SlotId':dstid}})
 def prop(ref,template,value,hidden):
  t=next(x for x in old if x.get('m_OverrideReferenceName')==template)
  n=next(x for x in old if x.get('m_Property',{}).get('m_Id')==t['m_ObjectId'])
  p=copy.deepcopy(t);p['m_ObjectId']=uid('property:'+ref)
  p['m_Guid']={'m_GuidSerialized':str(uuid.uuid5(NS,'F0:property-guid:'+ref))}
  p['m_Name']=p['m_RefNameGeneratedByDisplayName']=ref.lstrip('_')
  p['m_DefaultReferenceName']=p['m_OverrideReferenceName']=ref
  p['m_Hidden']=hidden;p['m_Value']=copy.deepcopy(value)
  pn=copy.deepcopy(n);pn['m_ObjectId']=uid('property-node:'+ref)
  pn['m_Property']={'m_Id':p['m_ObjectId']}
  pn['m_DrawState']['m_Position']['y']=33500.0+len(root['m_Properties'])*40.0
  out=copy.deepcopy(by[n['m_Slots'][0]['m_Id']]);out['m_ObjectId']=uid('property-slot:'+ref)
  out['m_DisplayName']=ref.lstrip('_')
  if 'm_Value' in out:out['m_Value']=out['m_DefaultValue']=copy.deepcopy(value)
  pn['m_Slots']=[{'m_Id':out['m_ObjectId']}]
  for x in (p,pn,out):add(x)
  root['m_Properties'].append({'m_Id':p['m_ObjectId']});cat['m_ChildObjectList'].append({'m_Id':p['m_ObjectId']});root['m_Nodes'].append({'m_Id':pn['m_ObjectId']})
  return pn,out
 def port(node,key,name,template,io,value=None):
  t=slot(node,template);x=copy.deepcopy(t);x['m_ObjectId']=uid('slot:'+key)
  x['m_Id']=max(by[z['m_Id']]['m_Id'] for z in node['m_Slots'])+1
  x['m_DisplayName']=x['m_ShaderOutputName']=name;x['m_SlotType']=io
  if value is not None and 'm_Value' in x:x['m_Value']=x['m_DefaultValue']=copy.deepcopy(value)
  node['m_Slots'].append({'m_Id':x['m_ObjectId']});add(x);return x
 floatprop=prop('_FlipbookBlending','_VertexOffset_Toggle',0.0,False)
 vecprop=prop('_BaseMap_AnimationSheetBlend_ST','_SharedUV_ST',{'x':0.0,'y':0.0,'z':0.0,'w':0.0},True)
 weightprop=prop('_AnimationSheetHelperBlendIntensity','_VertexOffset_Toggle',0.0,True)
 uv3=copy.deepcopy(next(x for x in old if x.get('m_Type')=='UnityEditor.ShaderGraph.UVNode' and x.get('m_OutputChannel')==2))
 uv3['m_ObjectId']=uid('uv3-node');uv3['m_OutputChannel']=3
 uv3['m_DrawState']['m_Position']['y']=34000.0
 uout=copy.deepcopy(by[uv3['m_Slots'][0]['m_Id']]);uout['m_ObjectId']=uid('uv3-out')
 uv3['m_Slots']=[{'m_Id':uout['m_ObjectId']}];add(uout);add(uv3);root['m_Nodes'].append({'m_Id':uv3['m_ObjectId']})
 uv3input=port(uv,'uv3-input','UV3','UV2',0)
 toggleuvinput=port(uv,'toggle-uv-input','FlipbookToggle','TWStrength',0,0.0)
 stinput=port(uv,'blend-st-input','AnimationSheetBlendST','SharedUVST',0,dict(x=0.0,y=0.0,z=0.0,w=0.0))
 weightinput=port(uv,'blend-weight-input','AnimationSheetBlendIntensity','TWStrength',0,0.0)
 blenduv=port(uv,'blend-uv-output','BlendUV','NoiseUV',1)
 blendweight=port(uv,'blend-weight-output','BlendWeight','DecalAlpha',1)
 edge(uv3['m_ObjectId'],uout['m_Id'],uv['m_ObjectId'],uv3input['m_Id'])
 edge(floatprop[0]['m_ObjectId'],floatprop[1]['m_Id'],uv['m_ObjectId'],toggleuvinput['m_Id'])
 edge(vecprop[0]['m_ObjectId'],vecprop[1]['m_Id'],uv['m_ObjectId'],stinput['m_Id'])
 edge(weightprop[0]['m_ObjectId'],weightprop[1]['m_Id'],uv['m_ObjectId'],weightinput['m_Id'])
 toggleinput=port(color,'toggle-input','FlipbookToggle','ChromaticToggle',0,0.0)
 blenduvinput=port(color,'color-blend-uv-input','BlendUV','BaseMapUV',0)
 blendweightinput=port(color,'color-blend-weight-input','BlendWeight','FogFactor',0,0.0)
 edge(floatprop[0]['m_ObjectId'],floatprop[1]['m_Id'],color['m_ObjectId'],toggleinput['m_Id'])
 edge(uv['m_ObjectId'],blenduv['m_Id'],color['m_ObjectId'],blenduvinput['m_Id'])
 edge(uv['m_ObjectId'],blendweight['m_Id'],color['m_ObjectId'],blendweightinput['m_Id'])
 changed={a['m_ObjectId'] for a,b in zip(o[:len(old)],old) if a!=b and 'm_ObjectId' in a}
 assert changed=={root['m_ObjectId'],cat['m_ObjectId'],uv['m_ObjectId'],color['m_ObjectId']},changed
 return '\n\n'.join(json.dumps(x,ensure_ascii=False,indent=4) for x in o)+'\n',{'oldObjects':len(old),'newObjects':len(o),'changedOriginalIds':sorted(changed),'UV3Node':uv3['m_ObjectId'],'newBaseUVSlots':[uv3input['m_Id'],toggleuvinput['m_Id'],stinput['m_Id'],weightinput['m_Id'],blenduv['m_Id'],blendweight['m_Id']],'newColorSlots':[toggleinput['m_Id'],blenduvinput['m_Id'],blendweightinput['m_Id']]}

def main():
 raw={k:p.read_bytes() for k,p in F.items()}
 graph,audit=patch_graph(raw['graph'].decode())
 outputs={'graph':graph,'baseuv':patch_baseuv(raw['baseuv'].decode()),
  'color':patch_color(raw['color'].decode()),'shared':patch_shared(raw['shared'].decode()),
  'contract':patch_contract(raw['contract'].decode()),
  'legacy':patch_legacy(raw['legacy'].decode()),'forward':patch_forward(raw['forward'].decode()),
  'helper':patch_helper(raw['helper'].decode())}
 OUT.mkdir(parents=True,exist_ok=True);files={}
 for k,value in outputs.items():
  data=value.encode();dest=OUT/F[k].name;dest.write_bytes(data)
  files[k]={'target':str(F[k]),'preview':str(dest),'inputSHA256':hashlib.sha256(raw[k]).hexdigest(),'previewSHA256':hashlib.sha256(data).hexdigest()}
 manifest={'mode':'preview-only','noProductUnityGitWrites':True,'scope':'ordinary Mesh F0 Flipbook','graphAudit':audit,'files':files}
 (OUT/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n');print(json.dumps(manifest,indent=2))
if __name__=='__main__':main()
