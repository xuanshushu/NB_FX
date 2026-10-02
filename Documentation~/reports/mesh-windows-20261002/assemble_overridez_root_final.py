from pathlib import Path
import hashlib,json,shutil,sys
work=Path(__file__).resolve().parent;product=work.parents[1]/'Packages/NB_FX'
source=work/'overridez-root-rebased-preview';out=work/'overridez-root-keyword-authority-preview'
def write_plans():
    plan=json.loads((work/'overridez-keyword-authority-preview/plan.json').read_text())
    for b in plan['batches']:
        b['expectedCasesFromActualDiscovery']=b.pop('expectedCasesFromSource')
        b['identitySource']='Explicit source case names; actual XML must match; not live discovery'
    (work/'overridez-keyword-authority24-plan.json').write_text(json.dumps(plan,indent=2)+'\n',encoding='utf-8',newline='\n')
    names=[n for b in plan['batches'] for n in b['exactCaseNames'] if 'G4OverrideZKeywordABC_' in n]
    assert len(names)==8
    subset={**plan,'scope':'OverrideZ runtime keyword/toggle mismatch authority8; original normal16 recorded separately','cases':8,'batches':[{'batch':1,'filter':';'.join(names),'expectedCasesFromActualDiscovery':8,'identitySource':'Explicit source TestCase names; actual XML must match','exactCaseNames':names}]}
    (work/'overridez-keyword-authority8-plan.json').write_text(json.dumps(subset,indent=2)+'\n',encoding='utf-8',newline='\n')
    print('Prepared source-declared exact plans: expanded24 / additional8')
if '--complete-plan' in sys.argv:
    assert out.exists() and (out/'manifest.json').exists()
    write_plans();sys.exit()
assert not out.exists();shutil.copytree(source,out)
m=json.loads((source/'manifest.json').read_text())
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest() if p.exists() else None
authority=work/'overridez-keyword-authority-preview'
adapter='NBShaders2/ShaderGraph/NBGraphOverrideDepth.hlsl';(out/adapter).write_bytes((authority/adapter).read_bytes())
for r in m['records']:
    if r['path']==adapter:r['afterSHA256']=sha(out/adapter)
files=[('Tests/URP/Editor/G4GraphOverrideDepthTests.cs',authority),
 ('Tests/URP/Editor/G4GraphOverrideDepthLifecycleTests.cs',work/'overridez-gui-lifecycle-preview'),
 ('Tests/URP/Editor/G4GraphOverrideDepthLifecycleTests.cs.meta',work/'overridez-gui-lifecycle-preview')]
for rel,origin in files:
    target=out/rel;target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes((origin/rel).read_bytes());m['records'].append({'path':rel,'beforeSHA256':sha(product/rel),'afterSHA256':sha(target)})
rel='Tests/URP/Editor/G4GraphOverrideDepthTests.cs.meta';origin=work.parents[1]/'.utmp/NBFXMeshValidation-20261002/Packages/NB_FX'/rel
target=out/rel;target.write_bytes(origin.read_bytes());m['records'].append({'path':rel,'beforeSHA256':sha(product/rel),'afterSHA256':sha(target)})
m['scope']='Current-root-derived OverrideZ slice; original keyword authority, assignment lifecycle, strict standalone depth and mismatch fixtures. Pending root slice Unity verification.'
m['rootIntegration']=False;m['keywordAuthority']='Matches original Shader _OVERRIDE_Z variant even when serialized toggle disagrees'
(out/'manifest.json').write_text(json.dumps(m,indent=2)+'\n',encoding='utf-8',newline='\n')
write_plans()
print(json.dumps({'finalSliceFiles':len(m['records']),'graphObjects':m['graphObjectsAfter'],'authorityCases':8}))
