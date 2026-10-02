from pathlib import Path
import hashlib,json
from isolation_serial_guard import ensure_idle,project
ensure_idle();work=Path(__file__).resolve().parent;clone=project/'Packages/NB_FX';preview=work/'gui-tier-masks-preview/combined-after-dn0-generated'
manifest=json.loads((preview/'manifest.json').read_text())
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
graph,hlsl=manifest['records'];assert graph['path'].endswith('.shadergraph') and hlsl['path'].endswith('.hlsl')
assert sha(clone/hlsl['path'])==hlsl['afterSHA256'], 'Installed HLSL must match dynamic candidate'
assert sha(preview/hlsl['path'])==hlsl['afterSHA256']
dest=clone/graph['path'];assert sha(dest)==graph['beforeSHA256']
backup=work/'gui-mask-graph-pre-install-backup'/graph['path'];assert not backup.exists();backup.parent.mkdir(parents=True,exist_ok=True);backup.write_bytes(dest.read_bytes())
assert sha(preview/graph['path'])==graph['afterSHA256'];dest.write_bytes((preview/graph['path']).read_bytes())
assert sha(dest)==graph['afterSHA256']
(work/'gui-mask-complete-isolation-installation.json').write_text(json.dumps({'scope':'Complete graph installation after partial HLSL/Applier installation. Failed first43 retained; no source assertion changes. Root untouched.',
 'records':manifest['records'],'inputGraphObjects':manifest['objectsBefore'],'installedGraphObjects':manifest['objectsAfter'],'oldCFPortsAndEdgesPreserved':manifest['oldCFPortsAndEdgesPreserved']},indent=2)+'\n',encoding='utf-8',newline='\n')
print(json.dumps({'installedGraphObjects':manifest['objectsAfter'],'graphSHA256':sha(dest),'HLSLExactMatch':True}))
