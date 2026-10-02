from pathlib import Path
import hashlib,json,sys
from isolation_serial_guard import ensure_idle,project
ensure_idle();work=Path(__file__).resolve().parent;root=work.parents[1]
product=root/'Packages/NB_FX';clone=project/'Packages/NB_FX'
backup=work/'combined-before-root-flipbook-validation';receipt=work/'root-flipbook-isolation-installation.json'
def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest() if path.exists() else None
if sys.argv[1]=='restore':
    manifest=json.loads(receipt.read_text(encoding='utf-8'))
    for record in manifest['records']:
        assert sha(clone/record['path'])==record['afterSHA256'],record['path']
        assert sha(backup/record['path'])==record['beforeSHA256'],record['path']
    for record in manifest['records']:(clone/record['path']).write_bytes((backup/record['path']).read_bytes())
    (work/'root-flipbook-combined-restoration.json').write_text(json.dumps({'scope':'Exact original combined candidate bytes restored after root F0 validation','files':len(manifest['records']),'graphSHA256':sha(clone/'NBShaders2/ShaderGraph/NBShaderGraph.shadergraph'),'subTargetSHA256':sha(clone/'NBShaders2/ShaderGraph/Editor/NBGraphUnlitSubTarget.cs')},indent=2)+'\n',encoding='utf-8')
    print('Restored exact combined source: '+str(len(manifest['records'])));sys.exit()
assert sys.argv[1]=='root' and not backup.exists()
preview=work/'root-flipbook-rebase/generated';manifest=json.loads((preview/'manifest.json').read_text(encoding='utf-8'))
for record in manifest['records']:
    assert sha(product/record['path'])==record['beforeSHA256'],record['path']
    assert sha(preview/record['path'])==record['afterSHA256'],record['path']
selected={}
for folder in ['NBShaders2','XuanXuanRenderUtility','Tests/URP/Editor']:
    for path in (product/folder).rglob('*'):
        if path.is_file() and 'Samples' not in path.relative_to(product).parts:selected[path.relative_to(product).as_posix()]=path
for record in manifest['records']:selected[record['path']]=preview/record['path']
records=[]
for relative,source in selected.items():
    dest=clone/relative
    if sha(dest)==sha(source):continue
    assert dest.exists(),'Temporary scope may only replace existing isolation files: '+relative
    saved=backup/relative;saved.parent.mkdir(parents=True,exist_ok=True);saved.write_bytes(dest.read_bytes())
    records.append({'path':relative,'beforeSHA256':sha(dest),'afterSHA256':sha(source)})
for record in records:(clone/record['path']).write_bytes(selected[record['path']].read_bytes())
receipt.write_text(json.dumps({'scope':'Current root OVZ+UVP plus dynamically appended F0/Helper/SRP only; combined DN0/masks/VAT/customdata/customlocal/ZOffset retained in exact backup. Root untouched.','records':records,'graphObjects':manifest['graphObjectsAfter'],'rootGraphSHA256':sha(product/'NBShaders2/ShaderGraph/NBShaderGraph.shadergraph'),'temporaryGraphSHA256':sha(clone/'NBShaders2/ShaderGraph/NBShaderGraph.shadergraph')},indent=2)+'\n',encoding='utf-8')
print(json.dumps({'changedFiles':len(records),'temporaryRootGraphObjects':manifest['graphObjectsAfter'],'rootProductWritten':False}))
