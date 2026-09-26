#!/usr/bin/env python3
import collections,json,pathlib,re,hashlib
root=next(p for p in pathlib.Path(__file__).resolve().parents if (p / "Packages/NB_FX/package.json").is_file());b=root/'Packages/NB_FX/NBShaders2'; s=b/'Shader/NBShader.shader'; cat=b/'Runtime/NBShaderFeatureCatalog.cs'; intent=b/'Runtime/NBShaderMaterialIntentResolver.cs'
lines=s.read_text().splitlines();ps=[]
for i,line in enumerate(lines):
 if re.match(r'^\s*Pass\s*$',line):
  block='\n'.join(lines[i:i+15]); m=re.search(r'Name\s+"([^"]+)"',block);l=re.search(r'"LightMode"\s*=\s*"([^"]+)"',block)
  ps.append({'line':i+1,'name':m.group(1) if m else None,'lightMode':l.group(1) if l else None,'keywords':{}})
allkw=collections.defaultdict(list);pragma=[]
for i,line in enumerate(lines,1):
 line=line.split('//',1)[0].strip()
 if not line.startswith('#pragma'):continue
 pieces=line.split();kind=pieces[1] if len(pieces)>1 else ''
 if not(kind.startswith('shader_feature') or kind.startswith('multi_compile')):continue
 p=max((p for p in ps if p['line']<i),key=lambda p:p['line'],default=None)
 if p is None:continue
 syms=[x for x in pieces[2:] if x!='_']
 pragma.append({'line':i,'pass':p['name'],'kind':kind,'symbols':syms})
 for kw in syms:
  allkw[kw].append({'line':i,'pass':p['name'],'kind':kind})
  p['keywords'][kw]=kind
catraw=cat.read_text().split('RawKeywords =',1)[1].split('};',1)[0];catalog=re.findall(r'"([A-Za-z_][A-Za-z0-9_]*)"',catraw)
intentraw=intent.read_text();bindings=[]
for i,line in enumerate(intentraw.splitlines(),1):
 m=re.search(r'new KeywordToggleBinding\("([^"]+)",\s*"([^"]+)"\)',line)
 if m:bindings.append({'property':m.group(1),'keyword':m.group(2),'line':i})
feature=set(k for k,v in allkw.items() if any(x['kind'].startswith('shader_feature') for x in v));multi=set(k for k,v in allkw.items() if any(x['kind'].startswith('multi_compile') for x in v))
summary={'source_sha256':{str(f.relative_to(root)):hashlib.sha256(f.read_bytes()).hexdigest() for f in (s,cat,intent)},'pass_count':len(ps),'passes':[{'name':p['name'],'line':p['line'],'lightMode':p['lightMode'],'keyword_count':len(p['keywords'])} for p in ps],'pragma_count':len(pragma),'pragma_kind_counts':dict(collections.Counter(x['kind'] for x in pragma)),'keyword_unique_count':len(allkw),'shader_feature_unique_count':len(feature),'multi_compile_unique_count':len(multi),'catalog_count':len(catalog),'catalog_missing_in_shader':sorted(set(catalog)-feature),'shader_feature_not_in_catalog':sorted(feature-set(catalog)),'keyword_toggle_binding_count':len(bindings),'multi_compile_keywords':sorted(multi)}
out={'summary':summary,'passes':ps,'pragmas':pragma,'keywords':allkw,'catalog':catalog,'intent_bindings':bindings,'note':'Static enabled pragma scan only; compiler/platform variant availability is not inferred.'}
(pathlib.Path(__file__).with_name('keywords-static.json')).write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n')
print(json.dumps(summary,ensure_ascii=False,indent=2))
