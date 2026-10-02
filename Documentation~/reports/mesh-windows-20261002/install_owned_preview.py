"""Install only a manifested preview into the owned, stopped validation project."""
from pathlib import Path
import hashlib,json,sys
from isolation_serial_guard import ensure_idle,project
ensure_idle();work=Path(__file__).resolve().parent
preview=Path(sys.argv[1]).resolve();assert preview.is_relative_to(work)
clone=project/'Packages/NB_FX';manifest=json.loads((preview/'manifest.json').read_text())
backup=work/(preview.name+'-pre-install-backup');assert not backup.exists()
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest() if p.exists() else None
records=manifest.get('records',manifest.get('files'))
assert isinstance(records,list) and records
for r in records:
    assert not Path(r['path']).is_absolute() and '..' not in Path(r['path']).parts
    assert sha(preview/r['path'])==r['afterSHA256'],r['path']
    assert sha(clone/r['path'])==r['beforeSHA256'],r['path']
for r in records:
    dest=clone/r['path'];old=backup/r['path'];old.parent.mkdir(parents=True,exist_ok=True)
    if dest.exists():old.write_bytes(dest.read_bytes())
    dest.parent.mkdir(parents=True,exist_ok=True);dest.write_bytes((preview/r['path']).read_bytes())
    assert sha(dest)==r['afterSHA256']
(work/(preview.name+'-isolation-installation.json')).write_text(json.dumps({'scope':'Only manifested paths in stopped owned isolation; root unchanged','records':records},indent=2)+'\n',encoding='utf-8',newline='\n')
print(json.dumps({'installedFiles':len(records),'preview':preview.name,'rootUnchanged':True}))
