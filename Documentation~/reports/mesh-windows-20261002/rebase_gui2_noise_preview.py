from pathlib import Path
import subprocess
import sys
work=Path(__file__).resolve().parent
root=work.parents[1]
original=root/'Packages/NB_FX/Documentation~/reports/handoff-20261001/candidates/gui1b/design/build-tier-input-preview.py'
text=original.read_text(encoding='utf-8')
anchor="assert Path('/tmp').resolve() in out.parents and r not in out.parents"
assert text.count(anchor)==1
text=text.replace(anchor,"assert Path('D:/UnityProject/NBUnityProject/.utmp/nbfx-resume-20261002').resolve() in out.parents and r not in out.parents")
text=text.replace('from pathlib import Path\n','''from pathlib import Path
_nb_original_write_text = Path.write_text
def _nb_write_text_lf(self,data,encoding=None,errors=None,newline=None):
    return _nb_original_write_text(self,data,encoding=encoding or 'utf-8',errors=errors,newline='\\n')
Path.write_text = _nb_write_text_lf
''',1)
script=work/'gui2-noise-rebound-preview.py';script.write_text(text,encoding='utf-8',newline='\n')
output=work/'gui2-noise-preview';assert not output.exists()
with (work/'gui2-noise-preview.stdout.json').open('wb') as stdout:
    run=subprocess.run([sys.executable,'-X','utf8',str(script),'--source-package',str(root/'.utmp/NBFXMeshValidation-20261002/Packages/NB_FX'),'--output',str(output)],stdout=stdout,check=True)
import json
manifest=json.loads((output/'source-manifest.json').read_text(encoding='utf-8'))
print(json.dumps({k:manifest[k] for k in ('objectsBefore','objectsAfter','oldCFPortsUnchanged','newKeywords','newPackedBits','inputs')},indent=2))
