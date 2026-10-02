"""Read-only incident review and exact-identity serial rerun plans. Never launch Unity."""
from pathlib import Path
import hashlib, json, struct, re

ROOT = Path('D:/UnityProject/NBUnityProject')
OUT = Path(__file__).resolve().parent
INCIDENT = ROOT / '.utmp/nbfx-mesh-current-6000.3.25f1-d3d11-20261002/root-flipbook-gui-ovz136-isolated-1'
PKG = ROOT / '.utmp/NBFXMeshValidation-20261002/Packages/NB_FX'
ORIGINAL = ROOT / '.utmp/nbfx-resume-20261002/overridez-keyword-authority-preview/plan.json'

def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()
def save(name, data):
    path = OUT / name
    assert not path.exists(), 'Never replace prior incident/plan evidence'
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2)+'\n', encoding='utf-8', newline='\n')

logfile = INCIDENT / 'batch-3/unity.log'
lines = logfile.read_text(encoding='utf-8', errors='replace').splitlines()
first = next((i+1, s) for i,s in enumerate(lines) if re.search(r'887a0005',s,re.I))
assert first[0] == 591
assert not (INCIDENT/'batch-3/results.xml').exists()
assert not (INCIDENT/'batch-4').exists()
folders = [p.name for p in (INCIDENT/'batch-3/captures').iterdir() if p.is_dir()]
assert folders == ['keyword-off-toggle-on-d3-ortho']
assert not list((INCIDENT/'batch-3/captures').rglob('*.rgba32f'))
assert not list((INCIDENT/'batch-3/captures').rglob('metrics.json'))
sources = ['NBShaders2/ShaderGraph/NBShaderGraph.shadergraph',
           'NBShaders2/ShaderGraph/NBGraphOverrideDepth.hlsl',
           'NBShaders2/ShaderGraph/NBGraphBaseUV.hlsl',
           'NBShaders2/ShaderGraph/NBGraphBaseColor.hlsl',
           'NBShaders2/ShaderGraph/NBGraphVertexOffset.hlsl',
           'NBShaders2/ShaderGraph/Editor/NBGraphUnlitSubTarget.cs',
           'NBShaders2/Shader/NBShader.shader',
           'NBShaders2/Shader/HLSL/NBShaderForwardPass.hlsl',
           'Tests/URP/Editor/G4GraphOverrideDepthTests.cs',
           'Tests/URP/Editor/G4GraphGuiFeatureIntentTests.cs']
source_hashes = {p:sha(PKG/p) for p in sources}
save('incident-audit.json', {
  'scope':'Read-only log/invocation/capture-directory audit; no Unity/GPU/dump operation',
  'incidentDirectory':str(INCIDENT), 'unity':'6000.3.25f1', 'api':'Direct3D11',
  'logSha256':sha(logfile), 'logLines':len(lines),
  'renderer':{'name':'Intel(R) UHD Graphics 770','driver':'32.0.101.7040',
              'logLines':[111,112,113,116],'threading':'kGfxThreadingModeNonThreaded'},
  'correctProject':str(ROOT/'.utmp/NBFXMeshValidation-20261002'),
  'commandProjectLines':[34,35,48,49],
  'sourceSha256ReadAfterIncident':source_hashes,
  'timeline':[
    {'lines':[277,280,281],'observation':'Script compilation completed, ExitCode0/Tundra build success; no managed compile error before device failure.'},
    {'lines':[513,517],'observation':'OVZ OneTimeSetUp calls real Graph camera warm; importer reports interpolation warning. Warm source:50,97 imports and renders four times. Log has no pre/post GPU health telemetry for this warm.'},
    {'lines':[591,598,599],'observation':'First observable GPU error 0x887a0005 creating 2x2 RGBAHalf white texture, OVZ fixture:50; KeywordAuthorityABC calls RenderOverrideZABC. This call can observe an already removed device; not proof of its originating shader/draw.'},
    {'lines':[644,804,963,1067,1216],'observation':'Subsequent invalid-view 80070057, additional texture/RT/buffer 887a0005, failed resource assertion: cascade after first observed device failure.'},
    {'lines':[1998,2000,2009,2020,2021],'observation':'Native crash while URP Submit/Camera.Render, managed test route remains KeywordAuthorityABC.'},
    {'lines':[2213,2228],'observation':'Logged native stack D3DKMTOpenResource -> D3D11DynamicConstantBuffer::D3D11BufferUpdate -> DrawBuffersBatchMode -> ScriptableBatchRenderer/SRPBatcher -> ScriptableRenderContext Submit. It is after device failure; not an isolated SRP compatibility verdict or shader root cause.'},
    {'lines':[2322,2324],'observation':'Crash handler then MCP socket closure. Closure is later context, not first failure.'}
  ],
  'managedCaseIdentity':{
    'likelyExactName':'NBFX.Baseline.Tests.G4GraphOverrideDepthTests.G4OverrideZKeywordABC_keyword_off_toggle_on_d3_ortho',
    'basis':'Only capture subdirectory created by fixture:43 is keyword-off-toggle-on-d3-ortho; at first fault managed stack is KeywordAuthorityABC. No XML confirms any completed identity.',
    'provesShaderCulprit':False},
  'outcome':{'classification':'GPU device removal followed by native crash; invalid/incomplete test execution',
             'cliEditorExitSigned':-1073741819, 'cliEditorExitHex':'0xC0000005',
             'xmlAbsent':True, 'completedOVZCases':0, 'OVZResultsUnknown':24,
             'sourceNativeLaterBatchNotRunCases':2, 'completedGUIEvidenceCases':110,
             'rawFramesBeforeCrash':0, 'metricsBeforeCrash':0, 'fullMeshGatePassed':False},
  'limits':['No dump/all-thread analysis performed by this subagent; parent owns that workflow.',
            'Logged symbolized crash stack is available, but exact device-removal reason/first triggering GPU workload is unknown.',
            'No access-token/license/host identifiers copied into this artifact.',
            'Prior F0/UVP/downstream/GUI success remains bounded evidence, not OVZ success or incident resolution.']
})

original = json.loads(ORIGINAL.read_text(encoding='utf-8'))
names = original['batches'][0]['exactCaseNames']
assert len(names)==len(set(names))==24
# First reproduce the identifiable startup identity in a small group. Every ID is unchanged.
groups = [('keyword-off-toggle-on',names[16:20]), ('keyword-on-toggle-off',names[20:24]),
          ('off-depth0.05-3',names[0:4]), ('off-depth7-50',names[4:8]),
          ('on-depth0.05-3',names[8:12]), ('on-depth7-50',names[12:16])]
assert set(sum([v for _,v in groups],[])) == set(names)
guard = {
  'sourceChangesAllowed':False, 'assertionChangesAllowed':False, 'toleranceChangesAllowed':False,
  'freshEditorPerBatch':True,'serialExclusiveProject':True,'noRetries':True,
  'beforeLaunch':['Parent checks exact installed source SHA values match sourceSha256; owns isolation idle/live scene inspection; no TAI Domain Reload.',
                  'After device-removal/crash, parent completes crash diagnostics and confirms a healthy restart before running this plan.'],
  'requiresExternalGuardBeforeNextBatch':True,
  'runnerLimit':'Existing run_current_mesh_matrix.py checks only *-readback-health.json after XML; OVZ Draw emits rgba32f and metrics without those health files. A plan flag alone does not provide raw/log health guarding. Run each one-batch plan and audit before next, or use an audited parent guard runner.',
  'stopSubsequentBatchesOn':['Any device-removed/reset/hung GPU token including 887a0005/887a0006/887a0007, failed D3D resource creation, native crash or 0xC0000005.',
                           'Missing/incomplete XML, wrong/duplicate identity set, infrastructure exit, missing frames/metrics.',
                           'Any nonfinite float or all-CDCD readback; no readback is not health success.'],
  'rawFrameContract':{'perCase':9,'perBatch':36,'total':216,'width':128,'height':128,
                      'channels':4,'format':'little-endian RGBA float32','bytesPerFrame':262144,
                      'requiredNames':[f'{side}-{kind}.rgba32f' for side in 'ABC' for kind in ['main','repeat','control']],
                      'mustBeFinite':True,'rejectAllCDCD32':True},
  'metricsContract':{'perCase':1, 'finite':True,
     'parity':'ab + bc + abControl + bcControl == 0',
     'repeat':'aRepeat + bRepeat + cRepeat == 0',
     'keywordOn':'each a/b/cResponse > .1; actorVisible>150 at depth<6, otherwise probeVisible>150',
     'keywordOff':'aResponse+bResponse+cResponse==0 AND actorVisible>150',
     'zeroKeywordOffResponseIsExpected':True},
  'strictFailureHandling':'Record unchanged strict XML failure separately from invalid GPU/infra failure. Low BCdiff never passes the zero assertion. Do not add original crashed24 to retry24 counts.',
  'artifactRule':'New destination per rerun; retain original crash log/absent XML state and completed110 GUI XML; no overwrite.'}
batches=[]
for i,(label,ids) in enumerate(groups,1):
  b={'batch':i,'label':label,'filter':';'.join(ids),'expectedCasesFromSource':4,
     'identitySource':'Exact original OVZ24 source identities; regrouped only', 'exactCaseNames':ids}
  batches.append(b)
  save(f'one-batch-{i}-plan.json',{
    'scope':f'RootF0 incident follow-up OVZ24 group {i}/6,4 unique existing identities; incomplete original remains failed infrastructure evidence',
    'unity':'6000.3.25f1','api':'Direct3D11','cases':4,'batches':[b],
    'sourceSha256':source_hashes,'stopOnReadbackHealthFailure':True,
    'healthGuard':guard,'fullMeshGatePassed':False})
save('ovz24-health-guarded-plan.json',{
  'scope':'OVZ24 original strict identities split6x4 after RootF0 incident, unchanged installed source; separate rerun evidence',
  'unity':'6000.3.25f1','api':'Direct3D11','cases':24,'batches':batches,
  'originalPlan':str(ORIGINAL),'originalPlanSha256':sha(ORIGINAL),
  'sourceSha256':source_hashes,'stopOnReadbackHealthFailure':True,'healthGuard':guard,
  'status':'Prepared only; never executed by this subagent','fullMeshGatePassed':False})
print(json.dumps({'incidentLines':len(lines),'sourceFiles':len(source_hashes),
                  'rerunUniqueIdentities':len(names),'batches':len(batches),
                  'originalCrashPasses':0,'output':str(OUT)},ensure_ascii=False))
