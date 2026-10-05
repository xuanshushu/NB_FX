"""Launch an actual built Windows64 Player and reject Editor/missing/raw failures.

Does not build, install, edit Unity projects or change source. Main Agent invokes
only after reviewed final source, successful actual BuildPlayer and idle Editor.
"""
from pathlib import Path
import argparse
import datetime
import hashlib
import json
import math
import struct
import subprocess
import time

OWNER = Path('D:/UnityProject/NBUnityProject/.utmp/nbp8').resolve()
BASE_IDS = ['PlayerBC_' + route + '_' + camera for route in
            ('Forward', 'NBCameraOpaqueDistortPass', 'NBDeferredDistortPass')
            for camera in ('ortho', 'perspective')]
AUTO_IDS = ['PlayerBC_FlipbookAutoLifecycle_' + camera for camera in ('ortho', 'perspective')]
AUTO_STAGES = ('init', 'auto', 'pause', 'resume', 'disabled', 'restart', 'run2')


def expected_raw_names():
    names = set()
    for identity in BASE_IDS:
        for role in ('B', 'C'):
            for stage in ('on', 'repeat', 'control', 'strength0', 'controller-off'):
                names.add(identity + '-' + role + '-' + stage + '.rgba32f')
            if '_Forward_' not in identity:
                for stage in ('mask', 'copy', 'opaque'):
                    names.add(identity + '-' + role + '-' + stage + '.rgba32f')
    assert len(names) == 84
    for identity in AUTO_IDS:
        for stage in AUTO_STAGES:
            for role in ('B', 'Br', 'C', 'Cr'):
                names.add(identity + '-' + stage + '-' + role + '.rgba32f')
    assert len(names) == 140
    return names


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--build-directory', required=True)
    parser.add_argument('--evidence', required=True)
    parser.add_argument('--timeout-seconds', type=int, default=600)
    args = parser.parse_args()
    build, evidence = Path(args.build_directory).resolve(), Path(args.evidence).resolve()
    assert OWNER in build.parents and OWNER in evidence.parents and not evidence.exists()
    assert 30 <= args.timeout_seconds <= 900
    receipt_path = build / 'build-receipt.json'
    receipt = json.loads(receipt_path.read_text(encoding='utf-8-sig'))
    assert receipt['ranBuildPlayer'] and receipt['actualStandaloneWindows64'] and receipt['buildResult'] == 'Succeeded'
    assert receipt['totalErrors']==0, 'A Succeeded label cannot waive actual build/shader errors'
    assert receipt['sourceLockStillMatchesAfterBuild'], 'Source lock contract failed'
    if receipt.get('sourceLockContract'):
        assert receipt['sourceLockContract']=='strict-other-inputs+finite-owned-PlayerSettings-Global-v2' and receipt['nonOwnedInputsBytesUnchangedAfterBuild'] and receipt['ownedPlayerSettingsAllowedAfterBuild'] and receipt['ownedGlobalSettingsAllowedAfterBuild'], 'Only the explicit owned PlayerSettings/Global states may differ in bytes'
        assert isinstance(receipt['allInputBytesUnchangedAfterBuild'],bool), 'Physical byte equality must be reported separately'
    assert receipt['expectedRuntimeCases'] == 8 and receipt['expectedRawCaptures'] == 140
    assert receipt['automaticFlipbookIdentities'] == AUTO_IDS
    executable = build / 'NBFXMeshValidation.exe'
    raw_exe = executable.read_bytes()
    assert raw_exe[:2] == b'MZ'
    pe = struct.unpack_from('<I', raw_exe, 0x3c)[0]
    assert raw_exe[pe:pe + 4] == b'PE\0\0' and struct.unpack_from('<H', raw_exe, pe + 4)[0] == 0x8664, 'Real Windows x64 exe required'
    evidence.mkdir()
    command = [str(executable), '-batchmode', '-force-d3d11', '-logFile', str(evidence / 'Player.log'),
               '--nbfx-evidence', str(evidence)]
    started = datetime.datetime.now(datetime.timezone.utc).isoformat()
    start = time.monotonic()
    timeout = False
    process_exit = None
    try:
        completed = subprocess.run(command, cwd=build, timeout=args.timeout_seconds,
                                   stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
                                   creationflags=getattr(subprocess, 'CREATE_NO_WINDOW', 0), check=False)
        process_exit = completed.returncode
        (evidence / 'process-output.log').write_bytes(completed.stdout)
    except subprocess.TimeoutExpired as error:
        timeout = True
        (evidence / 'process-output.log').write_bytes(error.stdout or b'')
    issues = []
    if timeout:
        issues.append('Player timeout; no pass credit')
    if process_exit != 0:
        issues.append(f'Native process exit={process_exit}')
    result_path = evidence / 'player-result.json'
    result = None
    if result_path.exists():
        result = json.loads(result_path.read_text(encoding='utf-8-sig'))
        if result.get('applicationIsEditor') or result.get('platform') != 'WindowsPlayer' or result.get('pointerBytes') != 8:
            issues.append('Not actual Windows64 Player')
        if result.get('api') != 'Direct3D11':
            issues.append('Different graphics API; separate unverified matrix')
        if result.get('exitCode') != 0 or result.get('buildIdentity') != receipt['identity'] or result.get('sourceLockSHA256') != receipt['sourceLockSHA256']:
            issues.append('Runtime exit/identity/source lock mismatch')
        cases = result.get('cases', [])
        if len(cases) != 6 or sorted(c.get('identity', '') for c in cases) != sorted(BASE_IDS) or any(c.get('status') != 'PASSED' for c in cases):
            issues.append('Original six exact runtime case results not all Passed')
        automatic = result.get('automaticFlipbookCases', [])
        if len(automatic) != 2 or sorted(c.get('identity', '') for c in automatic) != sorted(AUTO_IDS):
            issues.append('Two exact automatic Flipbook identities missing/duplicated')
        for case in automatic:
            if case.get('status') != 'PASSED' or any(not case.get(key) for key in
                ('runtimeInstancesDistinct', 'pauseHeld', 'disabledHeld', 'restartReset', 'destroyCleared', 'sourceHelperFieldsUnchanged', 'clockRestored')):
                issues.append('Automatic lifecycle not fully passed: ' + case.get('identity', '<missing>'))
            if case.get('pauseFrames', 0) < 4 or case.get('disabledFrames', 0) < 4:
                issues.append('Automatic lifecycle lacks four real pause/disable frames')
            stages = case.get('stages', [])
            if [s.get('name') for s in stages] != list(AUTO_STAGES):
                issues.append('Automatic capture stage set/order incomplete')
            for stage in stages:
                if any(stage.get(k) != 0 for k in ('bc', 'bRepeat', 'cRepeat')) or min(stage.get('bVisible', 0), stage.get('cVisible', 0)) <= 150:
                    issues.append('Automatic strict parity/repeat/visibility failure')
                if any(stage.get(role, {}).get('manualPlay') for role in ('b', 'c')):
                    issues.append('Manual Helper playback cannot prove automatic Update')
            if any(not math.isfinite(case.get(key, float('nan'))) or case.get(key, 0) <= .03 for key in
                ('automaticResponseB', 'automaticResponseC', 'resumeResponseB', 'resumeResponseC', 'restartResponseB', 'restartResponseC')):
                issues.append('Automatic progression/resume/restart response weak or missing')
    else:
        issues.append('Missing Player JSON; Editor XML cannot replace it')
    raw_audit = []
    for path in sorted(evidence.glob('*.rgba32f')):
        payload = path.read_bytes()
        if len(payload) != 128 * 128 * 4 * 4:
            issues.append('Wrong raw byte length: ' + path.name)
            continue
        values = struct.unpack('<' + 'f' * (128 * 128 * 4), payload)
        finite = all(math.isfinite(x) for x in values)
        poison_count = sum(x in (-23.203125, -431602080.0) for x in values)
        healthy = poison_count == 0
        raw_audit.append({'name': path.name, 'SHA256': digest(path), 'finite': finite, 'CDCDValues': poison_count})
        if not finite or not healthy:
            issues.append('Invalid GPU raw: ' + path.name)
    if {x['name'] for x in raw_audit} != expected_raw_names():
        issues.append(f'Expected exact original84 + automatic56 raw names, got{len(raw_audit)}')
    verdict = {'scope': 'actual StandaloneWindows64 original6/84 plus automaticFlipbook2/56; no Editor inference',
               'startedUTC': started, 'elapsedSeconds': time.monotonic() - start, 'command': command,
               'exeSHA256': digest(executable), 'buildReceiptSHA256': digest(receipt_path),
               'nativeProcessExitCode': process_exit, 'timedOut': timeout, 'issues': issues,
               'rawAudit': raw_audit, 'status': 'PASSED' if not issues else 'FAILED_OR_UNVERIFIED'}
    (evidence / 'process-verdict.json').write_text(json.dumps(verdict, indent=2) + '\n', encoding='utf-8', newline='\n')
    print(json.dumps({'nativeProcessExitCode': process_exit, 'status': verdict['status'], 'issues': issues}))
    raise SystemExit(0 if not issues else 1)


if __name__ == '__main__':
    main()
