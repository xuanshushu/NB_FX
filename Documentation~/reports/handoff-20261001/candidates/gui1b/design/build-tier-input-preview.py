#!/usr/bin/env python3
"""GUI2 two-consumer HLSL/Graph gate PREVIEW ONLY, no CPU resolver/Tier claim.
Reads latest Graph/CF, preserves all old IDs/ports/properties/Targets. /tmp output only.
No new bitfield protocol or keywords: two hidden ordinary Float allow inputs, defaults1.
"""
from pathlib import Path
import argparse,copy,difflib,hashlib,json,uuid
ap=argparse.ArgumentParser();ap.add_argument('--source-package',default='/Users/bytedance/UnityProject/NBUnityProject/Packages/NB_FX');ap.add_argument('--output',required=True);a=ap.parse_args()
r=Path(a.source_package).resolve();out=Path(a.output).resolve();assert Path('/tmp').resolve() in out.parents and r not in out.parents
G='NBShaders2/ShaderGraph/NBShaderGraph.shadergraph';H='NBShaders2/ShaderGraph/NBGraphBaseColor.hlsl'
sourceG=(r/G).read_bytes();sourceH=(r/H).read_bytes()
s=sourceG.decode('utf-8');dec=json.JSONDecoder();o=[];pos=0
while pos<len(s):
 while pos<len(s) and s[pos].isspace():pos+=1
 if pos>=len(s):break
 v,pos=dec.raw_decode(s,pos);o.append(v)
old=copy.deepcopy(o);by={x.get('m_ObjectId'):x for x in o}
props=[by[x['m_Id']] for x in o[0]['m_Properties']]
def name(x):return x.get('m_OverrideReferenceName') or x.get('m_DefaultReferenceName')
cf=next(x for x in o if x.get('m_Type','').endswith('CustomFunctionNode') and x.get('m_FunctionName')=='NBGraphBaseColor')
slots=[by[x['m_Id']] for x in cf['m_Slots']];maxid=max(x['m_Id'] for x in slots)
manifest=[]
for i,(logical,sourceprop,label,consumer) in enumerate([
 ('_NOISEMAP','_noisemapEnabled','NBGraphTierAllowNoise','NoiseEnabled'),
 ('_NOISE_MASKMAP','_noiseMaskMap_Toggle','NBGraphTierAllowNoiseMask','NoiseMaskToggle')]):
 ref=label.replace('NBGraph', '_NB_', 1)
 assert not any(name(p)==ref for p in props), 'Derived input collision: audit source before preview'
 source=next(p for p in props if name(p)==sourceprop)
 assert source['m_Type']=='UnityEditor.ShaderGraph.Internal.Vector1ShaderProperty'
 p=copy.deepcopy(source);p['m_ObjectId']=uuid.uuid4().hex;p['m_Guid']={'m_GuidSerialized':str(uuid.uuid4())};p['m_Name']=label;p['m_DefaultReferenceName']=ref;p['m_OverrideReferenceName']=ref;p['m_Hidden']=True;p['m_Value']=1.0;p['m_FloatType']=0
 o.append(p);props.append(p);o[0]['m_Properties'].append({'m_Id':p['m_ObjectId']})
 # Clone the actual source PropertyNode and its existing scalar output schema,
 # not a guessed serialization shape from a different Shader Graph release.
 pn=next(x for x in o if x.get('m_Type','').endswith('PropertyNode') and x.get('m_Property',{}).get('m_Id')==source['m_ObjectId'])
 newpn=copy.deepcopy(pn);newpn['m_ObjectId']=uuid.uuid4().hex;newpn['m_Property']={'m_Id':p['m_ObjectId']}
 newrefs=[]
 for sr in pn['m_Slots']:
  oslot=by[sr['m_Id']];ns=copy.deepcopy(oslot);ns['m_ObjectId']=uuid.uuid4().hex;ns['m_DisplayName']=label;ns['m_ShaderOutputName']=label;newrefs.append({'m_Id':ns['m_ObjectId']});o.append(ns)
 newpn['m_Slots']=newrefs;o.append(newpn);o[0]['m_Nodes'].append({'m_Id':newpn['m_ObjectId']})
 sourceInput=next(x for x in slots if x.get('m_DisplayName')==consumer)
 ns=copy.deepcopy(sourceInput);ns['m_ObjectId']=uuid.uuid4().hex;ns['m_Id']=maxid+i+1;ns['m_DisplayName']=label;ns['m_ShaderOutputName']=label;ns['m_Value']=1.0;ns['m_DefaultValue']=1.0
 o.append(ns);cf['m_Slots'].append({'m_Id':ns['m_ObjectId']})
 outputSlot=by[pn['m_Slots'][0]['m_Id']]['m_Id']
 o[0]['m_Edges'].append({'m_OutputSlot':{'m_Node':{'m_Id':newpn['m_ObjectId']},'m_SlotId':outputSlot},'m_InputSlot':{'m_Node':{'m_Id':cf['m_ObjectId']},'m_SlotId':ns['m_Id']}})
 manifest.append({'logicalManagedKeyword':logical,'serializedIntentSource':sourceprop,'allowUniform':ref,'portLabel':label,'appendedCFPortId':ns['m_Id'],'consumerParameter':consumer,'default':1.0})
# Old CF changed only by appended slot refs, Graph root only appended lists.
for before,after in zip(old[1:],o[1:]):
 if before.get('m_ObjectId')==cf['m_ObjectId']:
  b=copy.deepcopy(before);c=copy.deepcopy(after);b.pop('m_Slots');c.pop('m_Slots');assert b==c
 else:assert before==after
b=copy.deepcopy(old[0]);c=copy.deepcopy(o[0]);
for k in ('m_Nodes','m_Properties','m_Edges'):b.pop(k);c.pop(k)
assert b==c
h=sourceH.decode('utf-8');originalH=h
for precision in ('float','half'):
 start=h.index('void NBGraphBaseColor_'+precision+'(');brace=h.index('{',start);sig=h[start:brace]
 anchor='    out '+precision+'4 Out';assert sig.count(anchor)==1
 changed=sig.replace(anchor,'    float NBGraphTierAllowNoise, float NBGraphTierAllowNoiseMask,\n'+anchor)
 guard='\n    // Read-only effective switches; serialized feature toggles/flags remain intent.\n    NoiseEnabled *= NBGraphTierAllowNoise > 0.5 ? 1.0 : 0.0;\n    NoiseMaskToggle *= NBGraphTierAllowNoiseMask > 0.5 ? 1.0 : 0.0;\n'
 h=h[:start]+changed+h[brace:brace+1]+guard+h[brace+1:]
assert (r/G).read_bytes()==sourceG and (r/H).read_bytes()==sourceH, 'Product changed during generation; rerun latest snapshot'
out.mkdir(parents=True,exist_ok=True);records=[]
for rel,original,new in [(G,s,'\n\n'.join(json.dumps(x,indent=4,ensure_ascii=False) for x in o)+'\n'),(H,originalH,h)]:
 p=out/rel;p.parent.mkdir(parents=True,exist_ok=True);p.write_text(new);dp=out/'diffs'/(rel.replace('/','__')+'.diff');dp.parent.mkdir(exist_ok=True);dp.write_text(''.join(difflib.unified_diff(original.splitlines(True),new.splitlines(True),fromfile=str(r/rel),tofile=str(p))))
 records.append({'path':rel,'sourceSha256':hashlib.sha256(original.encode('utf-8')).hexdigest(),'candidateSha256':hashlib.sha256(p.read_bytes()).hexdigest()})
result={'scope':'GUI2 INPUT-GATE DESIGN PREVIEW ONLY. CPU normalization/resolver/projector/pass mapping NOT implemented. No Tier GUI support claim. No Unity/shader compilation.', 'records':records,'inputs':manifest,'objectsBefore':len(old),'objectsAfter':len(o),'oldCFPortsUnchanged':True,'oldPropertiesValuesAndTargetsUnchanged':True,'newKeywords':0,'newPackedBits':0,'onlyTwoNormalFloatReadOnlyAllowInputs':True,'wholeGraphTierSupport':False}
(out/'source-manifest.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result,indent=2))
