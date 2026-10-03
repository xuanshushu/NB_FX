"""Reviewed Debug12 checkpoint application, default read-only preflight.

--apply copies only sealed JSON/HTML/script metadata, backs up original report
bytes, appends one section and one index row, and appends new SHA index entries.
Never invokes Git/Unity, writes product sources, or modifies CURRENT_STATE.
"""
from __future__ import annotations

import argparse
import datetime
import hashlib
import json
import re
import sys
from pathlib import Path, PurePosixPath

OWNER = Path(__file__).resolve().parent
ROOT = OWNER.parents[1]
PACKAGE = ROOT / 'Packages/NB_FX'
PLAN = OWNER / 'apply-debug-checkpoint-plan.json'
ARCHIVE_REL = 'Documentation~/reports/mesh-windows-20261002/root-debug12-checkpoint-20261003'
ARCHIVE_BASE_REL = 'Documentation~/reports/mesh-windows-20261002'
REPORT_REL = 'Documentation~/reports/mesh-resume-multiagent-20261003.html'
INDEX_REL = 'Documentation~/reports/index.html'
EXPECTED_PACKAGE = '26fb7bbf4d77a77e44f7f7f154d5d5271f5ebf64'
EXPECTED_MAIN = '31ec6d24e2edfbe5dabd7067d36135ed8acf896a'
EXPECTED_GRAPH = '3fb5f6a3dad74732d758058e28b0b0df933189f32c9441fc1fe0cfdfaafd5f29'
SECTION_ID = 'root-debug12-integrated-20261003'
INDEX_ID = 'debug12-checkpoint-index-20261003'
EXPECTED_SOURCES = {
    'NBShaders2/ShaderGraph/NBShaderGraph.shadergraph',
    'NBShaders2/ShaderGraph/NBGraphBaseColor.hlsl',
    'NBShaders2/ShaderGraph/NBGraphVertexOffset.hlsl',
    'NBShaders2/ShaderGraph/Passes/NBGraphForwardPass.hlsl',
    'NBShaders2/ShaderGraph/Editor/NBGraphUnlitSubTarget.cs',
    'NBShaders2/Editor/NBShaderGraphGUI.cs',
    'NBShaders2/Runtime/NBShaderMaterialIntentResolver.cs',
    'Tests/URP/Editor/G4GraphNormalizedIntentReaderTests.cs',
    'Tests/URP/Editor/G4GraphDebugTests.cs',
    'Tests/URP/Editor/G4GraphDebugTests.cs.meta',
    'Tests/URP/Editor/G4GraphDebugCustomLocalTests.cs',
    'Tests/URP/Editor/G4GraphDebugCustomLocalTests.cs.meta',
}


def digest(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def safe_child(base: Path, name: str) -> Path:
    if not isinstance(name, str) or not name or ':' in name or '\\' in name:
        raise ValueError(f'Unsafe relative path: {name!r}')
    posix = PurePosixPath(name)
    if posix.is_absolute() or posix.as_posix() != name or any(x in ('.', '..', 'PackageRepo') for x in posix.parts):
        raise ValueError(f'Noncanonical or forbidden path: {name!r}')
    parent = base.resolve()
    path = base.joinpath(*posix.parts).resolve()
    if parent not in path.parents:
        raise ValueError(f'Path escaped intended directory: {name}')
    return path


def metadata_directory(repository: Path) -> Path:
    marker = repository / '.git'
    if marker.is_dir():
        directory = marker.resolve()
    else:
        text = marker.read_text(encoding='utf-8').strip()
        if not text.startswith('gitdir: '):
            raise ValueError(f'Unknown repository metadata mapping: {marker}')
        directory = (repository / text[8:]).resolve()
    if directory != (ROOT/'.git').resolve() and (ROOT/'.git').resolve() not in directory.parents:
        raise ValueError('Repository metadata escaped the fixed workspace Git directory')
    return directory


def metadata_head(repository: Path) -> tuple[str, str | None]:
    """Resolve loose or packed refs by reading metadata; never execute Git."""
    directory = metadata_directory(repository)
    value = (directory/'HEAD').read_text(encoding='utf-8').strip()
    reference = None
    if value.startswith('ref: '):
        reference = value[5:]
        if not reference.startswith('refs/'):
            raise ValueError('Unknown HEAD ref mapping')
        common = directory
        if (directory/'commondir').exists():
            common = (directory/(directory/'commondir').read_text().strip()).resolve()
            if common != (ROOT/'.git').resolve() and (ROOT/'.git').resolve() not in common.parents:
                raise ValueError('Common metadata directory escaped the workspace')
        ref_file = safe_child(common, reference)
        if ref_file.is_file():
            value = ref_file.read_text(encoding='utf-8').strip()
        else:
            packed = common/'packed-refs'
            matches = [line.split(' ',1)[0] for line in packed.read_text(encoding='utf-8').splitlines()
                       if not line.startswith(('#','^')) and line.endswith(' '+reference)]
            if len(matches) != 1:
                raise ValueError(f'Nonunique or missing packed ref: {reference}')
            value = matches[0]
    if not re.fullmatch('[0-9a-f]{40}', value):
        raise ValueError('HEAD is not an exact commit identity')
    return value, reference


def sealed_file(descriptor: dict, base: Path = OWNER) -> bytes:
    path = safe_child(base, descriptor['path'])
    data = path.read_bytes()
    if len(data) != descriptor['bytes'] or digest(data) != descriptor['SHA256']:
        raise ValueError(f'Reviewed private file changed: {path}')
    return data


def current_source_guard(plan: dict, locks: dict, facts: dict) -> None:
    main, main_ref = metadata_head(ROOT)
    package, package_ref = metadata_head(PACKAGE)
    if (main, package) != (EXPECTED_MAIN, EXPECTED_PACKAGE):
        raise ValueError('Current HEAD advanced beyond reviewed Debug12; no writes allowed')
    if (main_ref, package_ref) != (plan['expectedRefs']['main'], plan['expectedRefs']['package']):
        raise ValueError('HEAD-to-ref mapping changed since review')
    if (locks['mainHEAD'],locks['packageHEAD']) != (main,package):
        raise ValueError('Private fixed identity changed')
    if set(locks['source12SHA256']) != EXPECTED_SOURCES:
        raise ValueError('Reviewed source12 path set changed')
    if locks['source12SHA256']['NBShaders2/ShaderGraph/NBShaderGraph.shadergraph'] != EXPECTED_GRAPH:
        raise ValueError('Reviewed Root Graph identity changed')
    for relative, expected in locks['source12SHA256'].items():
        if digest(safe_child(PACKAGE,relative).read_bytes()) != expected:
            raise ValueError(f'Product source differs from reviewed Debug12: {relative}')
    for source in facts['NativeHoudini30']['sourceRootVsClone']:
        if digest(safe_child(PACKAGE,source['path']).read_bytes()) != source['rootSHA256']:
            raise ValueError('Root Native source changed beyond checkpoint snapshot; no writes allowed')


def unique(data: bytes, marker: bytes, label: str) -> int:
    if data.count(marker) != 1:
        raise ValueError(f'Unknown or nonunique {label}; no report update')
    return data.index(marker)


def id_absent(data: bytes, identifier: str) -> None:
    expression = rb'\bid\s*=\s*[\'"]'+re.escape(identifier.encode())+rb'[\'"]'
    if re.search(expression,data,re.I):
        raise ValueError(f'Checkpoint id already exists: {identifier}')


def insert_only(original: bytes, offset: int, payload: bytes) -> bytes:
    if payload in original:
        raise ValueError('Insertion payload already exists')
    result = original[:offset]+payload+original[offset:]
    if result.count(payload) != 1 or result.replace(payload,b'',1) != original:
        raise ValueError('Historical report bytes not exactly preserved by insertion')
    return result


def prepare_reports(plan: dict, locks: dict, fragments: dict) -> list[dict]:
    report_path, index_path = safe_child(PACKAGE,REPORT_REL),safe_child(PACKAGE,INDEX_REL)
    report, index = report_path.read_bytes(), index_path.read_bytes()
    for path,data in [(report_path,report),(index_path,index)]:
        if digest(data) != locks['centralReportOriginalSHA256'][str(path)]:
            raise ValueError(f'Original report SHA changed since review: {path}')
        data.decode('utf-8')
    id_absent(report,SECTION_ID); id_absent(index,INDEX_ID)
    section = fragments['report']
    row = fragments['indexRow']
    if section.count(f'id="{SECTION_ID}"'.encode()) != 1 or row.count(f'id="{INDEX_ID}"'.encode()) != 1:
        raise ValueError('Reviewed fragment id changed')
    if row.count(b'<td>') != 4 or row.count(b'<tr ') != 1 or row.count(b'</tr>') != 1:
        raise ValueError('Index row no longer has the reviewed four-column shape')
    report_at = unique(report,plan['anchors']['reportBefore'].encode('utf-8'),'report section anchor')
    current = unique(index,plan['anchors']['indexCurrent'].encode(),'index current section')
    gates = unique(index,plan['anchors']['indexGates'].encode(),'index gates section')
    if current >= gates:
        raise ValueError('Index sections changed order')
    current_bytes = index[current:gates]
    unique(current_bytes,plan['anchors']['indexColumnHeader'].encode('utf-8'),'index four-column header')
    if current_bytes.count(b'<tbody>') != 1 or current_bytes.count(b'</tbody>') != 1:
        raise ValueError('Index current table structure changed')
    row_at = current+current_bytes.index(b'<tbody>')+len(b'<tbody>')
    def payload(fragment:bytes, original:bytes) -> bytes:
        nl = b'\r\n' if b'\r\n' in original else b'\n'
        return nl+fragment.strip().replace(b'\r\n',b'\n').replace(b'\n',nl)+nl
    return [{'path':report_path,'original':report,'updated':insert_only(report,report_at,payload(section,report))},
            {'path':index_path,'original':index,'updated':insert_only(index,row_at,payload(row,index))}]


def prepare_archive(plan:dict, whitelist:dict, drafts:dict, updates:list[dict]) -> list[dict]:
    archive = safe_child(PACKAGE,ARCHIVE_REL)
    entries=[];targets=set()
    def add(relative:str, data:bytes, origin:str):
        target=safe_child(archive,relative)
        if target in targets or target.exists() or target.is_symlink():
            raise ValueError(f'Existing or duplicate archive target: {target}')
        targets.add(target)
        entries.append({'path':target,'data':data,'SHA256':digest(data),'origin':origin})
    if whitelist['count'] != 22 or len(whitelist['files']) != 22:
        raise ValueError('Reviewed22 whitelist count changed')
    for item in whitelist['files']:
        source=Path(item['sourceAbsolutePath']).resolve()
        if ROOT.resolve() not in source.parents or 'PackageRepo' in source.parts or source.suffix.lower()!='.json':
            raise ValueError(f'Unapproved whitelist source kind or boundary: {source}')
        relative=item['proposedPackageRelativePath']
        if not relative.startswith(ARCHIVE_REL+'/'):
            raise ValueError('Whitelist destination mapping changed')
        data=source.read_bytes()
        if len(data)!=item['bytes'] or digest(data)!=item['SHA256']:
            raise ValueError(f'Whitelist source SHA/bytes changed: {source}')
        add(relative[len(ARCHIVE_REL)+1:],data,str(source))
    for name,data in drafts.items():
        category='sources' if name.endswith(('.html','.py')) or name=='source12-sha256.json' else 'receipts'
        add(category+'/'+name,data,'sealed-private-draft:'+name)
    for update in updates:
        add('receipts/history-before-debug12-'+update['path'].name,update['original'],'historical-report-original-bytes')
    return entries


def prepare_sha_index(entries:list[dict]) -> dict:
    base=safe_child(PACKAGE,ARCHIVE_BASE_REL)
    path=safe_child(base,'sha256.txt')
    original=path.read_bytes()
    existing={}
    for line in original.decode('utf-8-sig').splitlines():
        if not line:continue
        match=re.fullmatch(r'([0-9a-f]{64})  (.+)',line)
        if not match:raise ValueError('Existing archive SHA index has an unknown line')
        name=match[2]
        if name.casefold() in existing:raise ValueError('Existing archive SHA index has duplicate paths')
        existing[name.casefold()]=match[1]
    added={}
    for item in entries:
        name=item['path'].relative_to(base).as_posix()
        if name.casefold() in existing or name.casefold() in {n.casefold() for n in added}:
            raise ValueError('New checkpoint path already appears in archive SHA index')
        added[name]=item['SHA256']
    tail=(''.join(added[n]+'  '+n+'\n' for n in sorted(added))).encode('utf-8')
    updated=original+(b'' if original.endswith(b'\n') else b'\n')+tail
    if not updated.startswith(original):raise ValueError('Existing SHA index bytes would be replaced')
    return {'path':path,'original':original,'updated':updated,'newEntries':len(added)}


def prepare() -> dict:
    raw_plan=PLAN.read_bytes();plan=json.loads(raw_plan.decode('utf-8'))
    if plan['expectedIdentity'] != {'packageHEAD':EXPECTED_PACKAGE,'mainHEAD':EXPECTED_MAIN,'rootGraphSHA256':EXPECTED_GRAPH}:
        raise ValueError('Application plan fixedidentity changed')
    sealed={name:sealed_file(x) for name,x in plan['sealedDraftFiles'].items()}
    locks=json.loads(sealed['source-head-lock.json'])
    facts=json.loads(sealed['checkpoint-facts.json'])
    whitelist=json.loads(sealed['receipt-audit-whitelist.json'])
    current_source_guard(plan,locks,facts)
    updates=prepare_reports(plan,locks,{'report':sealed['report-append.html'],'indexRow':sealed['index-row-append.html']})
    drafts={name:data for name,data in sealed.items() if name not in ['index-append.html']}
    drafts['apply-debug-checkpoint-plan.json']=raw_plan
    entries=prepare_archive(plan,whitelist,drafts,updates)
    receipt={'scope':'Debug12 checkpoint append-only documentation operation; no product/STATE/Unity/Git writes',
             'preparedUTC':datetime.datetime.now(datetime.timezone.utc).isoformat(),
             'expectedIdentity':plan['expectedIdentity'],'source12SHA256':locks['source12SHA256'],
             'reportUpdates':[{'path':str(x['path']),'beforeSHA256':digest(x['original']),
                               'afterSHA256':digest(x['updated']),'originalBytesStored':True,
                               'stripOnlyNewInsertionRestoresOriginalBytes':True} for x in updates],
             'copiedEntries':[{'path':str(x['path']),'SHA256':x['SHA256'],'bytes':len(x['data']),'origin':x['origin']} for x in entries],
             'strictPNoise1AndImpact44Retained':True,'noWholeGraphXMLRawOrHistorySourceCopy':True,
             'CURRENT_STATEWritten':False,'rootProductWritten':False,'ranGit':False,'ranUnity':False,'fullGateAccepted':False}
    receipt_data=(json.dumps(receipt,indent=2,ensure_ascii=False)+'\n').encode('utf-8')
    receipt_path=safe_child(PACKAGE,ARCHIVE_REL+'/receipts/checkpoint-documentation-application.json')
    if receipt_path.exists() or receipt_path.is_symlink():raise ValueError('Application receipt already exists')
    entries.append({'path':receipt_path,'data':receipt_data,'SHA256':digest(receipt_data),'origin':'generated-application-receipt'})
    sha_update=prepare_sha_index(entries)
    return {'plan':plan,'locks':locks,'facts':facts,'entries':entries,'updates':updates,'sha':sha_update}


def apply(prepared:dict) -> None:
    current_source_guard(prepared['plan'],prepared['locks'],prepared['facts'])
    for update in prepared['updates']+[prepared['sha']]:
        if update['path'].read_bytes()!=update['original']:
            raise ValueError('Report or SHA index changed after preflight; no writes performed')
    archive=safe_child(PACKAGE,ARCHIVE_REL)
    for item in prepared['entries']:
        item['path'].parent.mkdir(parents=True,exist_ok=True)
        if archive not in item['path'].resolve().parents:
            raise ValueError('Archive boundary changed during application; partial receipt files retained for review')
        with item['path'].open('xb') as stream:stream.write(item['data'])
        if digest(item['path'].read_bytes())!=item['SHA256']:
            raise ValueError('Archive readback SHA failed; no silent retry or overwrite')
    current_source_guard(prepared['plan'],prepared['locks'],prepared['facts'])
    for update in prepared['updates']+[prepared['sha']]:
        if update['path'].read_bytes()!=update['original']:
            raise ValueError('Report/SHA changed during copy; copied metadata retained, no further modification')
    for update in prepared['updates']+[prepared['sha']]:
        if update['path'].read_bytes()!=update['original']:
            raise ValueError('Concurrent documentation edit detected; stop for review')
        update['path'].write_bytes(update['updated'])
        if update['path'].read_bytes()!=update['updated']:
            raise ValueError('Documentation readback differs from prepared bytes')


def main() -> int:
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--apply',action='store_true',help='Root only: apply reviewed metadata archive/section/index/SHA insertions')
    args=parser.parse_args()
    prepared=prepare()
    print(json.dumps({'mode':'apply' if args.apply else 'read-only preflight',
                      'fixedPackageHEAD':EXPECTED_PACKAGE,'fixedMainHEAD':EXPECTED_MAIN,
                      'archive':ARCHIVE_REL,'whitelist22Verified':True,'source12Verified':True,
                      'metadataArchiveFiles':len(prepared['entries']),
                      'metadataArchiveBytes':sum(len(x['data']) for x in prepared['entries']),
                      'newSHAArchiveEntries':prepared['sha']['newEntries'],
                      'appendOnlyReportAndIndex':True,'originalReportBackups':2,
                      'CURRENT_STATEWritten':False,'ranGit':False,'ranUnity':False},ensure_ascii=False,indent=2))
    if args.apply:
        apply(prepared)
        print('Debug12 checkpoint applied; Root may normalize/stage with its separate v10 workflow.')
    return 0


if __name__=='__main__':
    try:raise SystemExit(main())
    except (KeyError,OSError,ValueError) as error:
        print(f'Debug12 checkpoint stopped safely: {error}',file=sys.stderr)
        raise SystemExit(1)
