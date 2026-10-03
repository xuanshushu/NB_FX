"""Restore only the twenty exact pre-VAT bytes in the owned stopped clone."""
from pathlib import Path
import hashlib, json
from isolation_serial_guard import ensure_idle, project

ensure_idle()
work = Path(__file__).resolve().parent
name = 'root-vat-isolation-ready-preview'
manifest = json.loads((work / (name + '-isolation-installation.json')).read_text(encoding='utf-8'))
backup = work / (name + '-pre-install-backup')
clone = project / 'Packages/NB_FX'

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest() if path.exists() else None

assert len(manifest['records']) == 20
for record in manifest['records']:
    assert sha(clone / record['path']) == record['afterSHA256'], record['path']
    assert record['beforeSHA256'] is not None
    assert sha(backup / record['path']) == record['beforeSHA256'], record['path']
for record in manifest['records']:
    (clone / record['path']).write_bytes((backup / record['path']).read_bytes())
receipt = {'scope': 'Exact pre-VAT RootCD1204 scope restored; next CD9 then F0 backup22 restores original combined scope',
           'files': 20, 'graphSHA256': sha(clone / 'NBShaders2/ShaderGraph/NBShaderGraph.shadergraph')}
assert receipt['graphSHA256'] == '5c65d4215451aed92176c59f483a3b3eddfc3418b3bed749e71bac3bb21e46b7'
(work / 'root-vat-isolation-restoration.json').write_text(json.dumps(receipt, indent=2) + '\n', encoding='utf-8')
print(json.dumps(receipt))
