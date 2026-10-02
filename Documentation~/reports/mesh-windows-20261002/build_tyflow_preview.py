"""Preview six Tyflow modes using the existing algorithm in a shared kernel."""
from pathlib import Path
import hashlib,json,re
from graph_preview_helpers import GraphPreview
work=Path(__file__).resolve().parent;root=work.parents[1];package=root/'.utmp/NBFXMeshValidation-20261002/Packages/NB_FX'
out=work/'tyflow-preview-loop-fixed';assert not out.exists()
paths={'legacy':'XuanXuanRenderUtility/Shader/HLSL/TyflowVAT.hlsl','graph':'NBShaders2/ShaderGraph/NBShaderGraph.shadergraph','adapter':'NBShaders2/ShaderGraph/NBGraphVATSoftBody.hlsl'}
base=work/'tyflow-pre-install-backup'
raw={k:((base/p) if base.exists() else (package/p)).read_bytes() for k,p in paths.items()};text={k:v.decode('utf-8') for k,v in raw.items()}
source=text['legacy'];matches=list(re.finditer(r'(?m)^((?:inline\s+)?(?:float[234]?|half[234]?|int|void|TyflowVatMatrix3|TyflowVatTMParts))\s+(TyflowVat\w+|ApplyTyflowVAT)\(([^)]*)\)\s*\{',source))
functions=[]
for m in matches:
    end=m.end();level=1
    while level:level+=(source[end]=='{')-(source[end]=='}');end+=1
    functions.append({'type':m[1],'name':m[2],'args':m[3],'body':source[m.end():end-1]})
assert len(functions)==27,len(functions)
def one(s,old,new,n=1):assert s.count(old)==n,(old,s.count(old));return s.replace(old,new)
for f in functions:
    name=f['name'];body=f['body']
    if name=='TyflowVatGetMetaDataSize':
        body=one(body,'    #if defined(_TYFLOW_VAT_SKIN_PRSXYZ)\n    return 12;\n    #else','    if (config.mode == 5) return 12;');body=one(body,'    #endif','')
    elif name=='TyflowVatGetVertexTMPartsAtFrame':
        body=body.replace('#if defined(_TYFLOW_VAT_SKIN_R)','if (config.mode == 2)').replace('#elif defined(_TYFLOW_VAT_SKIN_PR)','else if (config.mode == 3)').replace('#elif defined(_TYFLOW_VAT_SKIN_PRSXYZ)','else if (config.mode == 5)').replace('#elif defined(_TYFLOW_VAT_SKIN_PRSAVE)','else if (config.mode == 4)')
        # Both original preprocessor chains need real block scope.
        lines=body.splitlines();result=[];open_branch=False
        for line in lines:
            if re.match(r'\s*(?:else )?if \(config.mode',line):
                if open_branch:result.append('    }')
                result.append(line+' {');open_branch=True
            elif '#endif' in line:
                assert open_branch;result.append('    }');open_branch=False
            else:result.append(line)
        assert not open_branch;body='\n'.join(result)
    elif name=='TyflowVatGetSkinTexcoord':
        f['args']='float4 uv1, float4 uv2, float4 uv3, float4 uv4, float4 uv5, float4 uv6, float4 uv7, int index'
        for a,b in [('input.Custom1.xy','uv1.xy'),('input.Custom2.xy','uv2.xy'),('input.vatTexcoord4','uv3.xy'),('input.vatTexcoord5','uv4.xy'),('input.vatTexcoord6','uv5.xy'),('input.vatTexcoord7','uv6.xy'),('input.vatTexcoord8','uv7.xy')]:body=body.replace(a,b)
        body=one(body,'    #if !defined(_FLIPBOOKBLENDING_ON)\n    if (index == 2) return uv3.xy;\n    #endif','    if (config.flipbook < 0.5 && index == 2) return uv3.xy;')
    elif name=='ApplyTyflowVAT':
        f['name']='TVAT_ApplyModesV1';f['args']='float4 uv0, float4 uv1, float4 uv2, float4 uv3, float4 uv4, float4 uv5, float4 uv6, float4 uv7, bool particle, float frameCustomData, inout float3 positionOS, inout float3 normalOS'
        body=one(body,'    #if defined(SHADOWS_DEPTH)','    if (config.shadows) {');body=one(body,'    #endif\n\n    float frameBase','    }\n\n    float frameBase')
        body=one(body,'    float frameCustomData = GetCustomData(NB_CUSTOM_DATA_FLAG_2, FLAGBIT_POS_2_CUSTOMDATA_VAT_FRAME, -1.0f, input.Custom1, input.Custom2);','')
        body=body.replace('CheckLocalFlags1(FLAG_BIT_PARTICLE_1_IS_PARTICLE_SYSTEM)','particle').replace('positionOS.xyz','positionOS').replace('input.texcoords.zw','uv0.zw').replace('input.Custom1.xy','uv1.xy').replace('input.Custom1.y','uv1.y')
        body=one(body,'TyflowVatGetSkinTexcoord(input, i)','TyflowVatGetSkinTexcoord(uv1,uv2,uv3,uv4,uv5,uv6,uv7, i)')
        body=one(body,'    #if defined(TYFLOW_VAT_SKIN_MODE)','    if (config.mode >= 2) {')
        body=one(body,'    #elif defined(_TYFLOW_VAT_ABSOLUTE) || defined(_TYFLOW_VAT_RELATIVE)','    } else if (config.mode <= 1)')
        body=one(body,'            #if defined(_TYFLOW_VAT_SKIN_PRSXYZ)\n            bool useInvStartTM = true;\n            #else\n            bool useInvStartTM = _DeformingSkin > 0.5f;\n            #endif','            bool useInvStartTM = config.mode == 5 || _DeformingSkin > 0.5f;')
        body=one(body,'        #if defined(_TYFLOW_VAT_RELATIVE)\n        positionOS += (vertexOffset * float4(-1, 1, 1, 1) * _ImportScale).xyz;\n        #else\n        positionOS = (vertexOffset * float4(-1, 1, 1, 1) * _ImportScale).xyz;\n        #endif',
         '        if (config.mode == 1) positionOS += (vertexOffset * float4(-1, 1, 1, 1) * _ImportScale).xyz;\n        else positionOS = (vertexOffset * float4(-1, 1, 1, 1) * _ImportScale).xyz;')
        body=one(body,'            #if !defined(SHADOWS_DEPTH)','            if (!config.shadows) {')
        body=one(body,'            #endif','            }')
        body=one(body,'        #if !defined(SHADOWS_DEPTH)','        if (!config.shadows) {')
        body=one(body,'        #endif','        }');body=one(body,'    #endif','')
        body=re.sub(r'\btime\b','config.timeY',body)
    assert '#' not in body and 'AttributesParticle' not in f['args'],name
    f['body']=body
uniforms=sorted(set(re.findall(r'\b_[A-Za-z]\w*\b','\n'.join(f['body'] for f in functions))))
assert set(uniforms)=={'_VATTex','_VATTex_TexelSize','_LinearToGamma','_RGBAEncoded','_RGBAHalf','_DeformingSkin','_ImportScale','_AffectsShadows','_Frame','_Frames','_Autoplay','_AutoplaySpeed','_Loop','_InterpolateLoop','_FrameInterpolation','_SkinBoneCount','_VATIncludesNormals'},uniforms
fields=[p for p in uniforms if p!='_VATTex']
for f in functions:
    for p in fields:f['body']=re.sub(r'\b'+p+r'\b','config.'+p,f['body'])
    f['body']=f['body'].replace('SAMPLE_TEXTURE2D_LOD(_VATTex, sampler_point_clamp,','SAMPLE_TEXTURE2D_LOD(vatTexture, samplerVAT,')
names={f['name'] for f in functions};dependent={f['name'] for f in functions if 'config.' in f['body'] or 'vatTexture' in f['body']}
while True:
    extended=dependent|{f['name'] for f in functions if any(re.search(r'\b'+n+r'\(',f['body']) for n in dependent)}
    if extended==dependent:break
    dependent=extended
param='TVAT_ConfigV1 config, TEXTURE2D_PARAM(vatTexture, samplerVAT)';args='config, TEXTURE2D_ARGS(vatTexture,samplerVAT)'
for f in functions:
    for name in sorted(dependent,key=len,reverse=True):
        f['body']=re.sub(r'\b'+name+r'\(\s*\)',name+'('+args+')',f['body'])
        f['body']=re.sub(r'\b'+name+r'\((?!config,)',name+'('+args+', ',f['body'])
    if f['name'] in dependent:f['args']=param+(', '+f['args'] if f['args'].strip() else '')
structs=source[source.index('struct TyflowVatMatrix3'):source.index('inline int TyflowVatUnpackIntRGBA')]
cg=Path('C:/Program Files/Unity/Hub/Editor/6000.3.25f1/Editor/Data/Resources/CGIncludes/UnityCG.cginc').read_text(encoding='utf-8')
start=cg.index('inline float LinearToGammaSpaceExact');end=cg.index('\n}',start)+2
gamma=cg[start:end].replace('LinearToGammaSpaceExact','TyflowVatLinearToGammaExact')
kernel='''#ifndef NB_FX_TYFLOW_VAT_KERNEL_V1_INCLUDED
#define NB_FX_TYFLOW_VAT_KERNEL_V1_INCLUDED
#define TYFLOW_VAT_SKIN_MAX_BONES 7
// Existing byte decoders, frame selection and transforms; no new packed format.
'''+gamma+'''\nstruct TVAT_ConfigV1
{
    int mode;
    bool shadows;
    float flipbook;
    float timeY;
'''+''.join('    '+('float4' if p=='_VATTex_TexelSize' else 'float')+' '+p+';\n' for p in fields)+'};\n'+structs
kernel+='\n\n'.join(f['type']+' '+f['name']+'('+f['args']+')\n{'+f['body'].replace('LinearToGammaSpaceExact','TyflowVatLinearToGammaExact')+'\n}' for f in functions)+'\n#endif\n'
kernel=one(kernel,'        [unroll]','''#if defined(NB_GRAPH_VAT_HOST)
        // Graph has a uniform mode; keep the exact bounded seven-bone loop.
        [loop]
#else
        [unroll]
#endif''')
assert kernel.count('{')==kernel.count('}')
kernel_path='XuanXuanRenderUtility/Shader/HLSL/TyflowVATKernelV1.hlsl';paths['kernel']=kernel_path;text['kernel']=kernel
paths['kernel_meta']=kernel_path+'.meta';text['kernel_meta']='fileFormatVersion: 2\nguid: 8af27f6952e652a1bef58727ea268dd5\nShaderIncludeImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'
wrapper=source[:source.index('struct TyflowVatMatrix3')]+'''#include "Packages/com.xuanxuan.nb.fx/'''+kernel_path+'''"
void ApplyTyflowVAT(AttributesParticle input, inout float4 positionOS, inout float3 normalOS)
{
    TVAT_ConfigV1 config;
    config.mode = -1;
'''
for i,m in enumerate(['ABSOLUTE','RELATIVE','SKIN_R','SKIN_PR','SKIN_PRSAVE','SKIN_PRSXYZ']):wrapper+=('#if' if i==0 else '#elif')+' defined(_TYFLOW_VAT_'+m+')\n    config.mode = '+str(i)+';\n'
wrapper+='''#endif
    if (config.mode < 0) return;
#if defined(SHADOWS_DEPTH)
    config.shadows = true;
#else
    config.shadows = false;
#endif
    float4 uv3 = 0;
#if defined(_FLIPBOOKBLENDING_ON)
    config.flipbook = 1;
#else
    config.flipbook = 0;
    uv3 = float4(input.vatTexcoord4,0,0);
#endif
    config.timeY = time;
'''+''.join('    config.'+p+' = '+p+';\n' for p in fields)+'''    float frameCustomData = GetCustomData(NB_CUSTOM_DATA_FLAG_2, FLAGBIT_POS_2_CUSTOMDATA_VAT_FRAME,-1.0f,input.Custom1,input.Custom2);
    float3 animatedPositionOS = positionOS.xyz;
    TVAT_ApplyModesV1(config,TEXTURE2D_ARGS(_VATTex,sampler_point_clamp),
        input.texcoords,input.Custom1,input.Custom2,uv3,float4(input.vatTexcoord5,0,0),
        float4(input.vatTexcoord6,0,0),float4(input.vatTexcoord7,0,0),float4(input.vatTexcoord8,0,0),
        CheckLocalFlags1(FLAG_BIT_PARTICLE_1_IS_PARTICLE_SYSTEM),frameCustomData,animatedPositionOS,normalOS);
    positionOS.xyz = animatedPositionOS;
}
#endif
''';text['legacy']=wrapper
extra=[('_TyFlowVATSubMode','TyFlowVATSubMode',0),('_DeformingSkin','DeformingSkin',0),('_SkinBoneCount','SkinBoneCount',2),('_RGBAEncoded','RGBAEncoded',1),('_RGBAHalf','RGBAHalf',1),('_LinearToGamma','LinearToGamma',1),('_VATIncludesNormals','VATIncludesNormals',0),('_ImportScale','ImportScale',1),('_AffectsShadows','AffectsShadows',1),('_Frame','Frame',0),('_Frames','Frames',1),('_Autoplay','Autoplay',0),('_AutoplaySpeed','AutoplaySpeed',1),('_Loop','Loop',0),('_InterpolateLoop','InterpolateLoop',0),('_FrameInterpolation','FrameInterpolation',1)]
shader=(package/'NBShaders2/Shader/NBShader.shader').read_text(encoding='utf-8')
for p,n,v in extra:
    actual=re.search(re.escape(p)+r'\([^\n]*?,\s*Float\)\s*=\s*([-+.\d]+)',shader);assert actual,p
    # Read current serialized property defaults rather than guessing.
extra=[(p,n,float(re.search(re.escape(p)+r'\([^\n]*?,\s*Float\)\s*=\s*([-+.\d]+)',shader)[1])) for p,n,_ in extra]
adapter=text['adapter'].replace('#include "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBGraphFlags.hlsl"','#include "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBGraphFlags.hlsl"\n#define NB_GRAPH_VAT_HOST 1\n#include "Packages/com.xuanxuan.nb.fx/'+kernel_path+'"\n#undef NB_GRAPH_VAT_HOST\n#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"\nSamplerState NBGraphVAT_point_clamp_sampler;')
old='    out float3 OutPositionOS, out float3 OutNormalOS, out float Supported)';assert adapter.count(old)==1
adapter=adapter.replace(old,'    UnityTexture2D VATTexture, float4 UV3, float4 UV5, float4 UV6, float4 UV7,\n    float FlipbookToggle,\n'+''.join('    float '+n+',\n' for p,n,v in extra)+old)
dispatch='''    if (round(VATMode) == 1.0)
    {
        if (TyFlowVATSubMode < 0.0 || TyFlowVATSubMode > 5.0) { Supported=0.0; return; }
        TVAT_ConfigV1 config;
        config.mode = (int)round(TyFlowVATSubMode);
#if defined(SHADERPASS) && ((SHADERPASS == SHADERPASS_DEPTHONLY) || (SHADERPASS == SHADERPASS_SHADOWCASTER))
        config.shadows = true;
#else
        config.shadows = false;
#endif
        config.flipbook = FlipbookToggle;
        config.timeY = _Time.y;
        config._VATTex_TexelSize = VATTexture.texelSize;
'''+''.join('        config.'+p+' = '+n+';\n' for p,n,v in extra if p in fields)+'''        float frameCustomData=GetCustomData(NBGraphDecodeUInt32(CustomDataFlag2Lo16,CustomDataFlag2Hi16),FLAGBIT_POS_2_CUSTOMDATA_VAT_FRAME,-1.0,UV1,Custom2);
        TVAT_ApplyModesV1(config,TEXTURE2D_ARGS(VATTexture.tex,NBGraphVAT_point_clamp_sampler),
            UV0,UV1,Custom2,UV3,UV4,UV5,UV6,UV7,
            (flags1 & FLAG_BIT_PARTICLE_1_IS_PARTICLE_SYSTEM)!=0u,frameCustomData,OutPositionOS,OutNormalOS);
        return;
    }
'''
anchor='    if (round(VATMode)!=0.0';assert adapter.count(anchor)==1;adapter=adapter.replace(anchor,dispatch+anchor)
adapter=adapter.replace('// Ordinary Mesh Houdini four-mode adapter. NB screen passes are statically\n// excluded. Tyflow modes remain explicitly unsupported by this adapter.', '// Ordinary Mesh VAT adapter: existing Houdini and Tyflow modes.\n// Both exact NB screen passes retain their static VAT exclusion.')
text['adapter']=adapter
g=GraphPreview((base if base.exists() else package)/paths['graph'],'3f9fdcdd-c996-4c7b-b3ca-83775e72cb22');vat=next(o for o in g.objects if o.get('m_FunctionName')=='NBGraphVATSoftBody')
g.input(vat,'VATTexture',g.property('_VATTex','VATTexture',None,True),'PosTexture')
for c in [3,5,6,7]:g.input(vat,'UV'+str(c),g.uv(c),'UV1')
g.input(vat,'FlipbookToggle',g.source_for_property('_FlipbookBlending'),'FrameCount')
for p,n,v in extra:g.input(vat,n,g.property(p,n,v),'FrameCount')
text['graph']=g.serialize([vat['m_ObjectId']]);records=[]
for k,p in paths.items():
    dest=out/p;dest.parent.mkdir(parents=True,exist_ok=True);dest.write_text(text[k],encoding='utf-8',newline='\n');records.append({'path':p,'beforeSHA256':hashlib.sha256(raw[k]).hexdigest() if k in raw else None,'afterSHA256':hashlib.sha256(dest.read_bytes()).hexdigest()})
(out/'manifest.json').write_text(json.dumps({'scope':'Preview only: six Tyflow modes shared unchanged decoders/frame/transform with existing ShaderLab; Frozen unchanged; no new keyword/bit/pass',
 'objectsBefore':len(g.before),'objectsAfter':len(g.objects),'records':records,'functions':len(functions),'resourceDependentFunctions':sorted(dependent),'uvChannels':[0,1,2,3,4,5,6,7]},indent=2)+'\n',encoding='utf-8',newline='\n')
print(json.dumps({'files':len(records),'objects':len(g.objects),'previewOnly':True}))
