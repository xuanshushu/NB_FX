"""Install the reviewed VAT slice only after its actual serial evidence exists."""
from pathlib import Path
import hashlib, json, subprocess, time
import xml.etree.ElementTree as ET

work = Path(__file__).resolve().parent
root = work.parents[1]
product = root / 'Packages/NB_FX'
clone = root / '.utmp/NBFXMeshValidation-20261002/Packages/NB_FX'
ready = work / 'root-vat-product-ready-preview'
runs = root / '.utmp/nbfx-mesh-current-6000.3.25f1-d3d11-20261002'

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest() if path.exists() else None

def completed(name, count, require_all_passed=True):
    run = runs / name
    summary = json.loads((run / 'batch-summary.json').read_text(encoding='utf-8'))
    assert summary['completedBatches'] == summary['expectedBatches']
    assert summary['uniqueActualCases'] == count
    cases = []
    for result in summary['results']:
        cases.extend(ET.parse(run / ('batch-' + str(result['batch'])) / 'results.xml').findall('.//test-case'))
    assert len(cases) == count and len({c.get('fullname') for c in cases}) == count
    assert all(c.get('result') not in ('Skipped', 'Inconclusive') for c in cases)
    if require_all_passed:
        assert all(c.get('result') == 'Passed' for c in cases), name
    return {c.get('fullname') for c in cases}

def completed_remaining146():
    original = runs / 'root-vat-rest146-isolated-1'
    summary = json.loads((original / 'batch-summary.json').read_text(encoding='utf-8'))
    assert summary['completedBatches'] == 17 and summary['uniqueActualCases'] == 136
    assert not ET.parse(original / 'batch-18/results.xml').findall('.//test-case')
    names = set()
    for result in summary['results']:
        cases = ET.parse(original / ('batch-' + str(result['batch'])) / 'results.xml').findall('.//test-case')
        assert len(cases) == result['cases'] and all(c.get('result') not in ('Skipped', 'Inconclusive') for c in cases)
        batch_names = {c.get('fullname') for c in cases}
        assert not names.intersection(batch_names)
        names.update(batch_names)
    tail = completed('root-vat-rest146-tail10-method-filter-isolated-1', 10, False)
    assert not names.intersection(tail)
    names.update(tail)
    plan = json.loads((work / 'root-vat-rebase/root-vat-rest146-plan.json').read_text(encoding='utf-8'))
    assert names == {n for b in plan['batches'] for n in b['exactCaseNames']}
    return names

def completed_impact177():
    original = runs / 'root-vat-impact177-isolated-1'
    summary = json.loads((original / 'batch-summary.json').read_text(encoding='utf-8'))
    assert summary['completedBatches'] == 5 and summary['uniqueActualCases'] == 60
    names = set()
    for batch in range(1, 7):
        cases = ET.parse(original / ('batch-' + str(batch)) / 'results.xml').findall('.//test-case')
        assert len(cases) == (12 if batch <= 5 else 10)
        assert all(c.get('result') not in ('Skipped', 'Inconclusive') for c in cases)
        batch_names = {c.get('fullname') for c in cases}
        assert not names.intersection(batch_names)
        names.update(batch_names)
    tail = completed('root-vat-impact177-tail107-method-filter-isolated-1', 107, False)
    assert not names.intersection(tail)
    names.update(tail)
    plan = json.loads((work / 'root-vat-rebase/root-vat-impact177-plan.json').read_text(encoding='utf-8'))
    assert names == {n for b in plan['batches'] for n in b['exactCaseNames']}
    return names

# These are explicit prerequisites, not a claim that they have run yet.
first = completed('root-vat-first16-isolated-1', 16, False)
rest = completed_remaining146()
assert not first.intersection(rest)
completed_impact177()
completed('root-vat-nbpost-controller12-isolated-1', 12)
approval = json.loads((work / 'root-vat-coordinator-review-ready.json').read_text(encoding='utf-8'))
assert approval['functional162Reviewed'] and approval['impact177Reviewed'] and approval['controller12Reviewed']
assert approval['sourceAndABIReviewed'] and not approval['unresolvedNewFunctionalOrReadbackFailure']
actual_failures = set()
for name in ('root-vat-first16-isolated-1', 'root-vat-rest146-isolated-1',
             'root-vat-rest146-tail10-method-filter-isolated-1', 'root-vat-impact177-isolated-1',
             'root-vat-impact177-tail107-method-filter-isolated-1'):
    for xml in (runs / name).glob('batch-*/results.xml'):
        actual_failures.update(c.get('fullname') for c in ET.parse(xml).findall('.//test-case') if c.get('result') == 'Failed')
# Microdifference records may remain Failed, but each needs an explicit healthy
# classification. Never make an empty/invisible/NaN/GPU or functional failure an
# exception here, and never rewrite XML or weaken an assertion.
assert actual_failures == set(approval['strictFailedIdentitiesRetained'])
assert actual_failures == set(approval['reviewedHealthyMicroOrInheritedFixtureIdentities'])
assert approval['rootGraphBeforeSHA256'] == sha(product / 'NBShaders2/ShaderGraph/NBShaderGraph.shadergraph')

manifest = json.loads((ready / 'manifest.json').read_text(encoding='utf-8'))
assert len(manifest['records']) == 20
for record in manifest['records']:
    assert sha(product / record['path']) == record['beforeSHA256'], record['path']
    assert sha(ready / record['path']) == record['afterSHA256'], record['path']
    # Product metadata may have an explicit formatting-only mapping from clone.
    assert sha(clone / record['path']) == record.get('validatedSHA256', record['afterSHA256']), record['path']

scene_path = work / 'main-scenes-before-root-vat-integration.json'
scene = json.loads(scene_path.read_text(encoding='utf-8-sig'))['data']['result']['result']
assert time.time() - scene_path.stat().st_mtime < 120
assert scene['project'] == 'D:/UnityProject/NBUnityProject/Assets'
assert not scene['compiling'] and not scene['updating']
assert all(not s['dirty'] and 'tai' not in (s['name'] + ' ' + s['path']).lower()
           for s in scene['scenes'] if s['loaded'])

protected_paths = ['NBShaders2/Editor/NBShaderGraphGUI.cs',
                   'XuanXuanRenderUtility/Runtime/AnimationSheetHelper.cs',
                   'NBShaders2/Shader/NBShader.shader',
                   'NBShaders2/Shader/HLSL/NBShaderFlags.hlsl',
                   'NBShaders2/ShaderGraph/NBGraphBaseColor.hlsl',
                   'NBShaders2/ShaderGraph/NBGraphBaseUV.hlsl',
                   'NBShaders2/ShaderGraph/NBGraphVertexOffset.hlsl']
protected = {p: sha(product / p) for p in protected_paths}
backup = work / 'root-vat-product-pre-install-backup'
assert not backup.exists()
for record in manifest['records']:
    path = product / record['path']
    if path.exists():
        saved = backup / record['path']
        saved.parent.mkdir(parents=True, exist_ok=True)
        saved.write_bytes(path.read_bytes())
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes((ready / record['path']).read_bytes())
assert protected == {p: sha(product / p) for p in protected_paths}
manifest.update({'scope': 'Reviewed current Root CD1204 plus VAT1449; functional162, impact177, limited originalController12 actual evidence. Animated combinations/default chain/CustomLocal/GUI/Player/perf remain separate.',
                 'rootWritten': True, 'noG3G4Approval': True,
                 'inputPackageHEAD': subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=product, text=True).strip(),
                 'protectedRootSHA256': protected})
(work / 'root-vat-product-installation.json').write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf-8')
print(json.dumps({'rootFilesInstalled': 20, 'graphObjects': 1449, 'protectedRootUnchanged': True, 'GatesPassed': False}))
