from pathlib import Path
import subprocess,json
out=Path('/tmp/nbfx-gui1b-preview');src=Path('/Users/bytedance/UnityProject/NBUnityProject/Packages/NB_FX');clone=Path('/tmp/NBFXG2DissolveMaskProbe-20260928');csdir=out/'offline-compile';csdir.mkdir(exist_ok=True)
name='com.xuanxuan.nb.shaders2.Editor';selected={'NBShaders2/Editor/ShaderGUIItems/NBShaderSyncService.cs'}
rsp=clone/'Library/Bee/artifacts/200b0aEDbg.dag'/f'{name}.rsp';rows=[]
for line in rsp.read_text().splitlines():
 if line.startswith('-out:'):line='-out:"'+str(csdir/(name+'.dll'))+'"'
 elif line.startswith('-refout:'):continue
 elif line.startswith('"Packages/com.xuanxuan.nb.fx/'):
  rel=line[1:-1][len('Packages/com.xuanxuan.nb.fx/'):];line='"'+str(out/rel if rel in selected else src/rel)+'"'
 rows.append(line)
p=csdir/(name+'.rsp');p.write_text('\n'.join(rows)+'\n')
sdk='/Applications/Unity/Hub/Editor/6000.3.18f1/Unity.app/Contents/Resources/Scripting/'
r=subprocess.run([sdk+'NetCoreRuntime/dotnet',sdk+'DotNetSdkRoslyn/csc.dll','@'+str(p)],cwd=clone,capture_output=True,text=True)
(csdir/(name+'.log')).write_text(r.stdout+r.stderr)
print('Offline-only Roslyn compile exit',r.returncode);print(r.stdout+r.stderr);raise SystemExit(r.returncode)
