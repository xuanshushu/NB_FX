from pathlib import Path
import hashlib,json,sys
from isolation_serial_guard import ensure_idle,project
ensure_idle();work=Path(__file__).resolve().parent;root=work.parents[1];product=root/'Packages/NB_FX';clone=project/'Packages/NB_FX'
backup=work/'combined-before-root-uvp-validation';receipt=work/'root-uvp-isolation-installation.json'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest() if p.exists() else None
if sys.argv[1]=='restore':
    m=json.loads(receipt.read_text(encoding='utf-8'))
    for r in m['records']:
        assert sha(clone/r['path'])==r['afterSHA256'], 'Unexpected changes during root UVP verification: '+r['path']
        assert sha(backup/r['path'])==r['beforeSHA256']
    for r in m['records']:(clone/r['path']).write_bytes((backup/r['path']).read_bytes())
    (work/'root-uvp-combined-restoration.json').write_text(json.dumps({'scope':'Exact combined source restored after root UVP','files':len(m['records']),'graphSHA256':sha(clone/'NBShaders2/ShaderGraph/NBShaderGraph.shadergraph')},indent=2)+'\n',encoding='utf-8',newline='\n')
    print('Restored exact combined files: '+str(len(m['records'])));sys.exit()
assert sys.argv[1]=='root' and not backup.exists()
preview=work/'root-uvp-rebase/generated';m=json.loads((preview/'manifest.json').read_text(encoding='utf-8'))
for r in m['records']:assert sha(product/r['path'])==r['beforeSHA256'] and sha(preview/r['path'])==r['afterSHA256'],r['path']
selected={}
for folder in ['NBShaders2','XuanXuanRenderUtility','Tests/URP/Editor']:
    for p in (product/folder).rglob('*'):
        if p.is_file() and 'Samples' not in p.relative_to(product).parts:selected[p.relative_to(product).as_posix()]=p
for r in m['records']:selected[r['path']]=preview/r['path']
records=[]
for rel,source in selected.items():
    dest=clone/rel
    if sha(dest)==sha(source):continue
    assert dest.exists(),'Do not introduce untracked files in temporary scope: '+rel
    saved=backup/rel;saved.parent.mkdir(parents=True,exist_ok=True);saved.write_bytes(dest.read_bytes());records.append({'path':rel,'beforeSHA256':sha(dest),'afterSHA256':sha(source)})
for r in records:(clone/r['path']).write_bytes(selected[r['path']].read_bytes())
receipt.write_text(json.dumps({'scope':'Current root plus dynamic UVP only; full combined DN0/masks/F1/sourcefix source preserved, root untouched','records':records,'graphObjects':1132,'rootGraphSHA256':sha(product/'NBShaders2/ShaderGraph/NBShaderGraph.shadergraph'),'temporaryGraphSHA256':sha(clone/'NBShaders2/ShaderGraph/NBShaderGraph.shadergraph')},indent=2)+'\n',encoding='utf-8',newline='\n')
print(json.dumps({'temporaryChangedFiles':len(records),'combinedPreserved':True,'rootNotModified':True}))
