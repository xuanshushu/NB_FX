"""Read-only full CBUFFER/Properties inventory; writes only its own audit JSON."""
import hashlib,json,re
from pathlib import Path
HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[2]/'Packages/NB_FX'
COMBINED=HERE.parents[2]/'.utmp/NBFXMeshValidation-20261002/Packages/NB_FX'
INPUT='NBShaders2/Shader/HLSL/NBShaderInput.hlsl'
SHADER='NBShaders2/Shader/NBShader.shader'
def digest(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def clean(text):return re.sub(r'/\*.*?\*/|//[^\n]*','',text,flags=re.S)
def fields(package):
    s=clean((package/INPUT).read_text(encoding='utf-8'))
    s=s[s.index('CBUFFER_START(UnityPerMaterial)'):s.index('CBUFFER_END')]
    return {m[2]:m[1] for m in re.finditer(r'\b((?:half|float|uint|int|bool)(?:[234](?:x[234])?)?)\s+(_\w+)\s*(?:\[[^\]]+\])?\s*;',s)}
def properties(path):
    text=path.read_text(encoding='utf-8');text=text[:text.index('SubShader')]
    return {m[1]:m[2].strip() for m in re.finditer(r'(?m)^\s*(?:\[[^\]]+\]\s*)*(_\w+)\s*\(\s*"[^"]*"\s*,\s*([^\)]+)\)',clean(text))}
before=fields(ROOT);candidate=fields(COMBINED)
props=properties(COMBINED/SHADER);fixed=properties(HERE/SHADER)
rows=[]
for name,typ in candidate.items():
    status='explicit' if name in props else 'unlisted'
    texture=None
    for suffix in ('_ST','_TexelSize'):
        if name.endswith(suffix) and name[:-len(suffix)] in props and props[name[:-len(suffix)]] in ('2D','3D','Cube','2DArray','CubeArray'):
            status='implicit texture companion';texture=name[:-len(suffix)]
    rows.append(dict(name=name,hlslType=typ,statusBefore=status,ShaderLabTypeBefore=props.get(name),texture=texture,
                     statusAfter='explicit' if name in fixed else status,ShaderLabTypeAfter=fixed.get(name)))
helpers=['_BaseMap_AnimationSheetBlend_ST','_AnimationSheetHelperBlendIntensity']
result=dict(scope='Full static UnityPerMaterial declaration inventory, not native compatibility proof',
 rootInputSHA256=digest(ROOT/INPUT),combinedInputSHA256=digest(COMBINED/INPUT),shaderBeforeSHA256=digest(COMBINED/SHADER),shaderAfterSHA256=digest(HERE/SHADER),
 addedCBFields={k:v for k,v in candidate.items() if k not in before},removedCBFields={k:v for k,v in before.items() if k not in candidate},
 changedCBTypes={k:[before[k],candidate[k]] for k in before.keys()&candidate.keys() if before[k]!=candidate[k]},
 fullCBRows=rows,unlistedExistingFields=[r for r in rows if r['statusBefore']=='unlisted'],
 helperLiveFields=[r for r in rows if r['name'] in helpers],
 explanation='No CBUFFER declaration/layout changes in F0 versus live root. Existing helper weight became live via shared NBFX_ResolveFlipbookWeightV1 call; both exact writer names lacked Properties. Correct hidden defaults zero match current Graph properties and prior unset material uniforms. Other historical unlisted fields are inventoried, not silently changed; native strict current/Graph0 must confirm no further active mismatch.',
 productWritten=False,ranUnity=False)
assert result['addedCBFields']=={} and result['removedCBFields']=={} and result['changedCBTypes']=={}
assert fixed.keys()-props.keys()==set(helpers)
assert all(name not in props and name in fixed for name in helpers)
(HERE/'cbuffer-properties-audit.json').write_text(json.dumps(result,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
print(json.dumps(dict(cbFields=len(rows),addedCBFields=0,helperMissingBefore=helpers,otherUnlisted=len(result['unlistedExistingFields'])-2,sourceOnly=True)))
