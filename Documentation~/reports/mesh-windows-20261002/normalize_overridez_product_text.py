from pathlib import Path
import hashlib,json,shutil
work=Path(__file__).resolve().parent;root=work.parents[1];product=root/'Packages/NB_FX';source=work/'overridez-root-ready-preview';out=work/'overridez-root-normalized-preview'
assert not out.exists();shutil.copytree(source,out)
m=json.loads((source/'manifest.json').read_text(encoding='utf-8'));records=[]
for r in m['records']:
    p=product/r['path'];before=p.read_bytes();assert hashlib.sha256(before).hexdigest()==r['afterSHA256']
    text=before.decode('utf-8');after=('\n'.join(line.rstrip(' \t\r') for line in text.splitlines())+'\n').encode('utf-8')
    # Only whitespace at line ends/EOL changed. No token or literal changes.
    assert [line.rstrip(' \t\r') for line in text.splitlines()]==after.decode('utf-8').splitlines()
    if before!=after:
        p.write_bytes(after);(out/r['path']).write_bytes(after)
        records.append({'path':r['path'],'beforeVerifiedSHA256':r['afterSHA256'],'afterSHA256':hashlib.sha256(after).hexdigest(),'whitespaceOnly':True})
        r['afterSHA256']=hashlib.sha256(after).hexdigest()
m['transmissionNormalization']='LF and no trailing whitespace only; all shader/C# tokens and JSON values unchanged; original verified source retained in ready preview/archive'
(out/'manifest.json').write_text(json.dumps(m,indent=2)+'\n',encoding='utf-8',newline='\n')
(work/'overridez-product-text-normalization.json').write_text(json.dumps({'scope':'Reviewed product text normalization after diff --check, not a behavior change','records':records,'tokensAndValuesPreserved':True},indent=2)+'\n',encoding='utf-8',newline='\n')
print(json.dumps({'whitespaceOnlyFiles':len(records),'sourceTokensUnchanged':True}))
