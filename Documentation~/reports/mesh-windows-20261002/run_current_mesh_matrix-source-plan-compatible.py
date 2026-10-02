"""Run the discovered existing Mesh matrix in seven independent serial Editors.

Uses the official Unity CLI, keeps every verdict and artifact, and never retries
strict failures or changes test assertions. Only the disposable project is used.
"""
from pathlib import Path
import json
import os
import subprocess
import sys
import time
import xml.etree.ElementTree as ET

work = Path(__file__).resolve().parent
root = work.parents[1]
project = root / '.utmp' / 'NBFXMeshValidation-20261002'
cli = Path('C:/Users/Admin/AppData/Local/Unity/bin/unity.exe')
editor = Path('C:/Program Files/Unity/Hub/Editor/6000.3.25f1/Editor/Unity.exe')
plan_path = Path(sys.argv[1]) if len(sys.argv) > 1 else work / 'current-mesh-batch-plan.json'
plan = json.loads(plan_path.read_text(encoding='utf-8'))
destination = Path(sys.argv[2]).resolve() if len(sys.argv) > 2 else root / '.utmp' / 'nbfx-mesh-current-6000.3.25f1-d3d11-20261002' / 'post-ca-fg-regression-1'
assert not destination.exists(), 'Never overwrite earlier evidence'
assert project.is_relative_to(root / '.utmp')
assert destination.is_relative_to(root / '.utmp'), 'Evidence destination must remain task-local'
destination.mkdir(parents=True)
results = []
all_names = set()
for batch in plan['batches']:
    expected_cases = batch.get('expectedCasesFromActualDiscovery', batch.get('expectedCasesFromSource'))
    assert expected_cases == len(batch['exactCaseNames']), 'Explicit plan identity/count mismatch'
    identity_source = batch.get('identitySource', plan.get('identitySource',
        'source-declared' if 'expectedCasesFromSource' in batch else 'prior CLI test discovery'))
    folder = destination / ('batch-' + str(batch['batch']))
    folder.mkdir()
    xml = folder / 'results.xml'
    command = [str(cli), 'test', str(project), '--mode', 'EditMode',
               '--editor-path', str(editor), '--filter', batch['filter'],
               '--output', str(xml), '--timeout', '1800', '--json',
               '--no-banner', '--non-interactive', '--'] + plan.get('editorArgs', []) + ['-logFile', str(folder / 'unity.log')]
    environment = os.environ.copy()
    environment['NBFX_MESH_EVIDENCE_DIR'] = str(folder / 'captures')
    environment['NBFX_ISOLATED_PROJECT_DIR'] = str(project)
    environment['UNITY_BURST_DISABLE_COMPILATION'] = '1'
    environment['UNITY_MCP_ALLOW_BATCH'] = '1'
    environment.pop('NBFX_RT_DIAGNOSTIC', None)
    (folder / 'invocation.json').write_text(json.dumps({
        'argv': command, 'expectedCases': expected_cases, 'identitySource': identity_source,
        'evidenceDirectory': environment['NBFX_MESH_EVIDENCE_DIR'],
        'noNographics': True, 'noQuitFlag': True, 'serialFreshEditor': True,
        'noRetries': True, 'shaderCompileSync': environment.get('NBFX_SHADER_COMPILE_SYNC') == '1',
        'isolatedProjectEnvironment': environment['NBFX_ISOLATED_PROJECT_DIR']}, indent=2) + '\n', encoding='utf-8', newline='\n')
    print('START batch=' + str(batch['batch']) + ' expected=' + str(expected_cases), flush=True)
    started = time.time()
    with (folder / 'cli.stdout.json').open('wb') as stdout, (folder / 'cli.stderr.log').open('wb') as stderr:
        run = subprocess.run(command, cwd=project, env=environment, stdout=stdout, stderr=stderr)
    if not xml.exists():
        raise RuntimeError('No verdict XML for batch ' + str(batch['batch']) + '; exit=' + str(run.returncode))
    tree = ET.parse(xml)
    cases = tree.findall('.//test-case')
    names = [c.attrib['fullname'] for c in cases]
    assert len(names) == len(set(names)), 'Duplicate actual test identity'
    assert set(names) == set(batch['exactCaseNames']), 'Actual tests differ from discovery selection'
    assert not all_names.intersection(names), 'Cross-batch duplicate identity'
    all_names.update(names)
    counts = {state: sum(c.attrib.get('result') == state for c in cases)
              for state in ('Passed', 'Failed', 'Skipped', 'Inconclusive')}
    result = {'batch': batch['batch'], 'cliExitCode': run.returncode,
              'cases': len(cases), 'counts': counts, 'seconds': round(time.time() - started, 3),
              'xml': str(xml), 'failedNames': [c.attrib['fullname'] for c in cases if c.attrib.get('result') == 'Failed']}
    assert run.returncode in (0, 8), 'Infrastructure CLI exit rather than strict test verdict'
    results.append(result)
    (destination / 'batch-summary.json').write_text(json.dumps({
        'scope': plan['scope'], 'unity': plan['unity'], 'api': plan['api'],
        'completedBatches': len(results), 'expectedBatches': len(plan['batches']),
        'uniqueActualCases': len(all_names), 'results': results,
        'fullMeshGatePassed': False}, indent=2) + '\n', encoding='utf-8', newline='\n')
    print('FINISH ' + json.dumps({k: result[k] for k in ('batch', 'cliExitCode', 'cases', 'counts', 'seconds')}), flush=True)
assert len(all_names) == plan['cases']
print('COMPLETE existing matrix: ' + json.dumps({
    'uniqueCases': len(all_names),
    'counts': {s: sum(x['counts'][s] for x in results) for s in ('Passed','Failed','Skipped','Inconclusive')},
    'notFullMeshFeatureCoverage': True}), flush=True)
