from pathlib import Path
import hashlib,json
work=Path(__file__).resolve().parent;product=work.parents[1]/'Packages/NB_FX'
preview=work/'overridez-generated-source-preview';out=work/'overridez-root-keyword-authority-preview'
m=json.loads((out/'manifest.json').read_text())
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest() if p.exists() else None
for r in json.loads((preview/'manifest.json').read_text())['records']:
    assert not any(o['path']==r['path'] for o in m['records'])
    target=out/r['path'];target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes((preview/r['path']).read_bytes())
    m['records'].append({'path':r['path'],'beforeSHA256':sha(product/r['path']),'afterSHA256':sha(target)})
(out/'manifest.json').write_text(json.dumps(m,indent=2)+'\n',encoding='utf-8',newline='\n')
plan=json.loads((preview/'plan.json').read_text())
for b in plan['batches']:
    b['expectedCasesFromActualDiscovery']=b.pop('expectedCasesFromSource');b['identitySource']='Explicit source names; actual XML must match'
(work/'overridez-source2-plan.json').write_text(json.dumps(plan,indent=2)+'\n',encoding='utf-8',newline='\n')
print('Added installed generated-source and native SRP checks to root-derived slice')
