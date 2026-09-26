#!/usr/bin/env python3
"""Read-only static NBShader property inventory; lexical candidates, not Unity semantic validation."""
import collections, hashlib, json, pathlib, re
ROOT=next(p for p in pathlib.Path(__file__).resolve().parents if (p / "Packages/NB_FX/package.json").is_file())
B=ROOT/'Packages/NB_FX/NBShaders2'
PACKAGE=ROOT/'Packages/NB_FX'
S=B/'Shader/NBShader.shader'
I=B/'Shader/HLSL/NBShaderInput.hlsl'
lines=S.read_text().splitlines()
start=next(i for i,l in enumerate(lines) if l.strip()=='Properties')
end=next(i for i,l in enumerate(lines) if l.strip()=='SubShader')
pattern=re.compile(r'^\s*(?:\[[^\]]*\]\s*)*([A-Za-z_]\w*)\s*\(\s*"([^"]*)"\s*,\s*(.+)\)\s*=\s*(.*?)\s*(?://.*)?$')
props=[];unparsed=[]
for i in range(start+1,end):
    line=lines[i]; m=pattern.match(line)
    if m:
        props.append({'name':m.group(1),'line':i+1,'label':m.group(2),'type':m.group(3).strip(),'default':m.group(4).strip(),'attributes':re.findall(r'\[([^\]]+)\]',line.split(m.group(1),1)[0])})
    elif re.match(r'^\s*(?:\[[^\]]*\]\s*)*[A-Za-z_]\w*\s*\(',line) and '=' in line and not line.lstrip().startswith('//'):
        unparsed.append({'line':i+1,'text':line})
ctext=I.read_text(); cb=ctext.split('CBUFFER_START(UnityPerMaterial)',1)[1].split('CBUFFER_END',1)[0]
cbfields=[]
for i,line in enumerate(ctext.splitlines(),1):
    if 'CBUFFER_START(UnityPerMaterial)' in line: cbstart=i
for i,line in enumerate(cb.splitlines()[1:],cbstart+1):
    m=re.match(r'^\s*((?:half|float|int|uint)(?:[1-4](?:x[1-4])?)?)\s+(_[A-Za-z0-9_]+)\s*;',line)
    if m: cbfields.append({'name':m.group(2),'line':i,'type':m.group(1)})
csfiles=sorted(p for p in PACKAGE.rglob('*.cs') if '/Tests/' not in str(p) and '/Documentation~/' not in str(p)); hsfiles=sorted((B/'Shader/HLSL').rglob('*.hlsl'))
tok=re.compile(r'(?<![A-Za-z0-9_])[A-Za-z_]\w*')
def no_comments(text):
    text=re.sub(r'/\*.*?\*/',lambda m: '\n'*m.group(0).count('\n'),text,flags=re.S)
    return re.sub(r'//[^\n]*','',text)
csref=collections.defaultdict(list);hsref=collections.defaultdict(list)
for group,files,dest in [('cs',csfiles,csref),('hlsl',hsfiles,hsref)]:
    for file in files:
        for lineno,line in enumerate(no_comments(file.read_text(errors='replace')).splitlines(),1):
            for name in set(tok.findall(line)):
                dest[name].append({'file':str(file.relative_to(ROOT)),'line':lineno})
bodylines=no_comments('\n'.join(lines[end:])).splitlines()
shaderbodyref=collections.defaultdict(list)
for offset,line in enumerate(bodylines,end+1):
    for name in set(tok.findall(line)):
        shaderbodyref[name].append(offset)
for p in props:
    n=p['name']; p['cbuffer']=[x for x in cbfields if x['name']==n]; p['cs_refs']=csref[n];p['hlsl_refs']=hsref[n];p['shader_body_refs']=shaderbodyref[n]
    p['nbshaders2_cs_refs']=[r for r in csref[n] if '/NBShaders2/' in r['file']]
    p['external_package_cs_refs']=[r for r in csref[n] if '/NBShaders2/' not in r['file']]
    p['classification'] = 'editor_or_render_state' if ('FoldOut' in n or 'FoldToggle' in n or n in ('_SrcBlend','_DstBlend','_ZWrite','_ZTest','_Cull','_QueueBias','_Stencil','_StencilComp','_StencilOp','_StencilFail','_StencilZFail')) else 'requires_semantic_review'
namecount=collections.Counter(p['name'] for p in props); pn=set(namecount); cn=set(f['name'] for f in cbfields)
summary={'source_sha256':{str(x.relative_to(ROOT)):hashlib.sha256(x.read_bytes()).hexdigest() for x in [S,I]},'properties_block_lines':[start+1,end+1], 'property_count':len(props),'property_unique_count':len(namecount),'property_duplicate_names':{n:c for n,c in namecount.items() if c>1}, 'property_type_counts':dict(collections.Counter(p['type'] for p in props)),'unparsed_property_like_count':len(unparsed),'cbuffer_count':len(cbfields),'cbuffer_unique_count':len(cn),'cbuffer_not_in_properties':sorted(cn-pn),'property_not_in_cbuffer_count':len(pn-cn),'property_no_cs_hlsl_shaderbody_ref':sorted(p['name'] for p in props if not p['cs_refs'] and not p['hlsl_refs'] and not p['shader_body_refs']),'property_hlsl_no_cbuffer':sorted(p['name'] for p in props if p['hlsl_refs'] and not p['cbuffer']),'property_no_cs_ref_count':sum(not p['cs_refs'] for p in props),'property_no_hlsl_ref_count':sum(not p['hlsl_refs'] for p in props),'property_no_shaderbody_ref_count':sum(not p['shader_body_refs'] for p in props),'property_no_shaderbody_hlsl_ref_count':sum(not p['shader_body_refs'] and not p['hlsl_refs'] for p in props),'property_no_any_ref_count':sum(not p['cs_refs'] and not p['hlsl_refs'] and not p['shader_body_refs'] for p in props),'cs_file_count':len(csfiles),'hlsl_file_count':len(hsfiles)}
out={'summary':summary,'properties':props,'cbuffer_fields':cbfields,'unparsed_property_like':unparsed,'note':'Raw lexical references include comments/declarations/quoted strings. No semantic claim of reads/writes or pass reachability.'}
path=pathlib.Path(__file__).with_name('properties-static.json');path.write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n')
print(json.dumps(summary,ensure_ascii=False,indent=2))
