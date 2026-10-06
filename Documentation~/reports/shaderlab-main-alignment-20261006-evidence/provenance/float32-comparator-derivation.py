"""Offline comparator repair only; retains the actual v9 build/fixture identity."""
from pathlib import Path
import shutil
HERE=Path(__file__).resolve().parent
BASE=HERE.parent/'qualified-default-audit-preview9'
OUT=HERE.parent/'offline-float32-comparator10'
assert not OUT.exists()
OUT.mkdir()
for n in ['compare_player.py','material_compare.py','material-schema.json']:shutil.copy2(BASE/n,OUT/n)
p=OUT/'compare_player.py';t=p.read_text(encoding='utf-8')
old='HERE = Path(__file__).resolve().parent'
new='HERE = Path(__file__).resolve().parent\nBUILT_FIXTURE = HERE.parent / '+repr(BASE.name)
assert t.count(old)==1;t=t.replace(old,new)
for a,b in [("(HERE/'run_player.py')","(BUILT_FIXTURE/'run_player.py')"),("(HERE/'manifest.json')","(BUILT_FIXTURE/'manifest.json')"),("(HERE/file['path'])","(BUILT_FIXTURE/file['path'])"),("(HERE/'material-schema.json')","(BUILT_FIXTURE/'material-schema.json')")]:
    assert a in t,a;t=t.replace(a,b)
needle="delta = lambda a, b: max(abs(x-y) for x, y in zip(a,b))"
assert t.count(needle)==1
t=t.replace(needle,needle+'''
# Receipt responseMax is a C# System.Single: every subtraction rounds to
# binary32 before Abs/Max. This helper is ONLY for its numeric statistic.
# Full-frame cross-endpoint delta and every zero-error check remain unchanged.
def float32(value): return struct.unpack('<f',struct.pack('<f',value))[0]
def delta_statistic_float32(a,b): return max(abs(float32(x-y)) for x,y in zip(a,b))
def same_float32_statistic(actual,receipt): return struct.pack('<f',actual)==struct.pack('<f',receipt)
''')
old="delta(values['on'],values['control']) == r['responseMax']"
new="same_float32_statistic(delta_statistic_float32(values['on'],values['control']),r['responseMax'])"
assert t.count(old)==1;t=t.replace(old,new)
p.write_text(t,encoding='utf-8')
print('Offline responseMax typed recurrence repaired; built fixture and original verdict untouched.')
