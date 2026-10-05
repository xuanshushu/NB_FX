"""Read-only final-source snapshot. Capture only on main Agent's explicit review.

Does not start Unity or build. Final-combination acceptance is an explicit CLI
argument, never inferred from older passing Editor XML or current Graph fields.
"""
from pathlib import Path
import argparse
import hashlib
import json
import re

OWNER = Path(__file__).resolve().parent
ISOLATION = Path('D:/UnityProject/NBUnityProject/.utmp/NBFXMeshValidation-20261002').resolve()


def finite_player_settings_states(captured, native_prepared):
    # Retain every byte except these exact known scalar/layout edits.
    text=captured.decode('utf-8'); native=native_prepared.decode('utf-8')
    nl='\r\n' if '\r\n' in text else '\n'
    lines=['  adjustIOSFPSUsingThermalState: 1','  thermalStateSeriousIOSFPS: 30','  thermalStateCriticalIOSFPS: 15']
    for line in lines: assert native.replace('\r\n','\n').split('\n').count(line)==1
    block=nl.join(lines)+nl;keys=[line.split(':')[0]+':' for line in lines]
    canonical=text
    if not any(k in text for k in keys):
        anchor='  preserveFramebufferAlpha: 0'+nl;assert canonical.count(anchor)==1;canonical=canonical.replace(anchor,anchor+block)
    else:
        for line in lines:assert text.replace('\r\n','\n').split('\n').count(line)==1
    empty='  scriptingDefineSymbols: {}'+nl;expanded='  scriptingDefineSymbols:'+nl+'    Standalone: '+nl
    if empty in canonical:assert canonical.count(empty)==1;canonical=canonical.replace(empty,expanded)
    assert canonical.count(expanded)==1 and canonical.count(block)==1
    original=canonical.replace(block,'').replace(expanded,empty)
    thermal_only=canonical.replace(expanded,empty)
    assert text in (original,canonical,thermal_only),'Unknown/mixed representation'
    frame=r'(?m)^  enableFrameTimingStats: [01](?=\r?$)';assert len(re.findall(frame,canonical))==1
    assert len(re.findall(r'(?m)^  enableFrameTimingStats: 1(?=\r?$)',native))==1
    return [re.sub(frame,'  enableFrameTimingStats: '+str(v),layout).encode('utf-8') for layout in (original,canonical,thermal_only) for v in (0,1)]


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--project', required=True)
    p.add_argument('--package', required=True)
    p.add_argument('--pipeline-asset', required=True)
    p.add_argument('--renderer-asset', required=True)
    p.add_argument('--installed-harness-directory', required=True)
    p.add_argument('--identity', required=True)
    p.add_argument('--prepared-dependencies', required=True)
    p.add_argument('--output', required=True)
    p.add_argument('--reviewed-final-combination', action='store_true', required=True)
    a = p.parse_args()
    project, package, out = Path(a.project).resolve(), Path(a.package).resolve(), Path(a.output).resolve()
    assert project == ISOLATION and project in package.parents
    assert OWNER in out.parents and not out.exists() and re.fullmatch(r'[A-Za-z0-9-]{1,60}', a.identity)
    dependencies = json.loads(Path(a.prepared_dependencies).read_text(encoding='utf-8-sig'))
    paths = [package / 'package.json', Path(a.prepared_dependencies).resolve()]
    paths += [Path(d['absolutePath']).resolve() for d in dependencies['dependencies']]
    paths += [(project / dependencies[k]).resolve() for k in ('pipeline','renderer','globalSettings')]
    paths += [OWNER / name for name in ('execution-settings.json','manifest.json','capture-plan.json','run_extra_player.py')]
    paths += [package / 'Tests/URP/Editor/G4GraphOverrideDepthTests.cs', package / 'Tests/URP/Editor/G4GraphOverrideDepthTests.cs.meta']
    for name in ('com.xuanxuan.nb.shaders2','com.xuanxuan.nb.shaders2.Editor','com.xuanxuan.render.utility','com.xuanxuan.nb.postprocessing','NBFX.Player.Validation.Runtime','NBFX.Player.Validation.Editor','NBFX.G2.URP.Editor.Tests','Unity.RenderPipelines.Universal.Runtime','Unity.RenderPipelines.Core.Runtime','Unity.ShaderGraph.Editor'):
        paths.append(project / 'Library/ScriptAssemblies' / (name + '.dll'))
    # Actual reflection providers invoked by the existing Builder (configuration
    # plus the already validated real-camera warm), and their assembly contract.
    for rel in ('Tests/URP/Editor/G4GraphScreenNoiseTests.cs', 'Tests/URP/Editor/G4GraphFlipbookTests.cs',
                'Tests/URP/Editor/G4GraphGuiFeatureIntentTests.cs', 'Tests/URP/Editor/NBFX.G2.URP.Editor.Tests.asmdef'):
        paths += [package / rel, package / (rel + '.meta')]
    for module in ('NBShaders2', 'XuanXuanRenderUtility', 'NBPostProcessing'):
        paths += [f for f in (package / module).rglob('*') if f.is_file() and f.suffix.lower() in {'.cs', '.hlsl', '.shader', '.shadergraph', '.shadersubgraph', '.compute', '.cginc', '.asmdef', '.asmref', '.meta', '.asset', '.mat', '.json'}]
    for rel in (a.pipeline_asset, a.renderer_asset, 'ProjectSettings/GraphicsSettings.asset',
                'ProjectSettings/QualitySettings.asset', 'ProjectSettings/ProjectSettings.asset', 'ProjectSettings/ProjectVersion.txt'):
        f = (project / rel).resolve()
        assert project in f.parents and f.exists()
        paths.append(f)
        if Path(str(f) + '.meta').exists(): paths.append(Path(str(f) + '.meta'))
    # Includes TimeManager, Tier policy and dependency versions; Helper is in
    # the utility module above. No older TA/Overlay source is silently reused.
    paths += list((project / 'ProjectSettings').glob('*.asset'))
    paths += [project / 'Packages/manifest.json', project / 'Packages/packages-lock.json']
    paths += [OWNER / 'capture_player_input_lock.py', OWNER / 'run_player.py']
    assert all(f.exists() for f in paths), 'Every declared source dependency must exist'
    paths += [f for d in ('Runtime', 'Editor') for f in (OWNER / d).rglob('*') if f.is_file()]
    installed = (project / a.installed_harness_directory).resolve()
    assert project / 'Assets' in installed.parents and installed.is_dir()
    for module in ('Runtime', 'Editor'):
        for source in (OWNER / module).rglob('*'):
            if source.is_file():
                target = installed / source.relative_to(OWNER)
                assert target.exists() and target.read_bytes() == source.read_bytes(), 'Installed harness drift: ' + str(target)
                paths.append(target)
    settings_path=project/'ProjectSettings/ProjectSettings.asset'
    captured=settings_path.read_bytes();native_path=Path(a.prepared_dependencies).resolve().parent/'native-player-prepared.yaml';native=native_path.read_bytes()
    journal_path=native_path.parent/'build-prepared.json';journal=json.loads(journal_path.read_text(encoding='utf-8-sig'))
    original=next(f for f in journal['settings'] if f['path']=='ProjectSettings/ProjectSettings.asset');original_path=native_path.parent/original['backup']
    assert hashlib.sha256(original_path.read_bytes()).hexdigest()==original['sha256']
    assert captured in finite_player_settings_states(original_path.read_bytes(),native), 'Unknown disk drift since original preparation'
    states=finite_player_settings_states(captured,native)
    assert captured in states
    captured_path=out.with_name(out.stem+'-player-settings-captured.bytes');assert not captured_path.exists()
    out.parent.mkdir(parents=True,exist_ok=True);captured_path.write_bytes(captured)
    paths += [captured_path,native_path,journal_path,original_path]
    assert journal['defaultProfileCaptured'] and journal['defaultGlobalPreparedJSON']
    global_original=next(f for f in journal['settings'] if f['path']==journal['defaultGlobalPath'])
    global_backup=native_path.parent/global_original['backup'];assert hashlib.sha256(global_backup.read_bytes()).hexdigest()==global_original['sha256']
    paths.append(global_backup)
    paths += [project/journal['defaultProfilePath'],project/(journal['defaultProfilePath']+'.meta'),Path(journal['defaultEnsureAssembly']),Path(journal['defaultProfileUtilsAssembly']),Path(journal['defaultEnsureSource'])]
    owned={'path':settings_path.as_posix(),'capturedBytesPath':captured_path.as_posix(),'capturedSHA256':hashlib.sha256(captured).hexdigest(),'nativePreparedPath':native_path.as_posix(),'nativePreparedSHA256':hashlib.sha256(native).hexdigest(),'allowedSHA256':[hashlib.sha256(b).hexdigest() for b in states]}
    sources = [{'absolutePath': f.as_posix(), 'sha256': hashlib.sha256(f.read_bytes()).hexdigest()}
               for f in sorted(set(paths))]
    assert next(s['sha256'] for s in sources if s['absolutePath']==settings_path.as_posix())==owned['capturedSHA256'], 'Settings changed during capture; preserve receipt and inspect, no implicit relock'
    data = {'identity': a.identity, 'project': project.as_posix(), 'sources': sources,
            'finalCombinationAccepted': a.reviewed_final_combination, 'ownedPlayerSettings': owned,
            'scope': 'source/build input only; no compile/build/Player verdict'}
    out.parent.mkdir(parents=True, exist_ok=True)
    out.write_text(json.dumps(data, indent=2) + '\n', encoding='utf-8', newline='\n')
    print(json.dumps({'sourceFiles': len(sources), 'output': out.as_posix(), 'built': False}))


if __name__ == '__main__':
    main()
