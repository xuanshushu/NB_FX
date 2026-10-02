"""Offline preservation/ABI tests using an actual archive-derived input in memory.

No candidate installation, source writes, Editor, or rendering. This deliberately
does not claim any result for the future Root combination.
"""
from pathlib import Path
import ast
import copy
import hashlib
import json
import runpy

OWNER = Path(__file__).resolve().parent
module = runpy.run_path(str(OWNER / 'build_root_vat.py'), run_name='nbfx_offline_builder_test')
archive = Path(json.loads((OWNER / 'resource-provenance.json').read_text(encoding='utf-8'))['archive'])
original = module['decode']((archive / module['GRAPH']).read_text(encoding='utf-8-sig'))
before = copy.deepcopy(original)
by = {o['m_ObjectId']: o for o in before}
root = before[0]
vat = module['cf'](before, 'NBGraphVATSoftBody')
basis = module['cf'](before, 'NBGraphVATWorldBasis')
raw_position = module['incoming'](root, vat, 'PositionOS', by)
raw_normal = module['incoming'](root, vat, 'NormalOS', by)
raw_basis = {label: module['incoming'](root, basis, 'Raw' + label, by)
             for label in ('NormalWS', 'TangentWS', 'BitangentWS')}
for function, labels in module['REWIRES'].items():
    n = module['cf'](before, function)
    for label in labels:
        sid = module['slot'](n, label, by)['m_Id']
        e = module['unique']((e for e in root['m_Edges']
                             if e['m_InputSlot']['m_Node']['m_Id'] == n['m_ObjectId']
                             and e['m_InputSlot']['m_SlotId'] == sid), 'selftest consumer')
        e['m_OutputSlot'] = copy.deepcopy(raw_basis[label] if function == 'NBGraphSixWayBake'
                                         else raw_normal if label == 'NormalOS' else raw_position)
remove = {n['m_ObjectId'] for n in (vat, basis)}
remove.update(q['m_Id'] for n in (vat, basis) for q in n['m_Slots'])
root['m_Edges'] = [e for e in root['m_Edges']
                   if e['m_InputSlot']['m_Node']['m_Id'] not in remove
                   and e['m_OutputSlot']['m_Node']['m_Id'] not in remove]
resource, provenance = module['load_resources']()
rb = {o['m_ObjectId']: o for o in resource['objects']}
all_resource_properties = {o['m_ObjectId'] for o in resource['objects'] if 'm_OverrideReferenceName' in o}
referenced_outputs = {e['m_OutputSlot']['m_Node']['m_Id'] for e in root['m_Edges']}
for n in before:
    p = by.get(n.get('m_Property', {}).get('m_Id'))
    if p and p['m_ObjectId'] in all_resource_properties and n['m_ObjectId'] not in referenced_outputs:
        ref = p['m_OverrideReferenceName']
        if ref.startswith('_NB_') or ref == '_FlipbookBlending':
            continue
        remove.add(p['m_ObjectId'])
        remove.add(n['m_ObjectId'])
        remove.update(q['m_Id'] for q in n['m_Slots'])
    if n['m_Type'].endswith('.UVNode') and n['m_OutputChannel'] >= 4 and n['m_ObjectId'] not in referenced_outputs:
        remove.add(n['m_ObjectId'])
        remove.update(q['m_Id'] for q in n['m_Slots'])
before = [o for o in before if o['m_ObjectId'] not in remove]
for key in ('m_Nodes', 'm_Properties'):
    root[key] = [q for q in root[key] if q['m_Id'] not in remove]
for o in before:
    if 'm_ChildObjectList' in o:
        o['m_ChildObjectList'] = [q for q in o['m_ChildObjectList'] if q['m_Id'] not in remove]
module['check_graph'](before)
module['require_features'](before, require_ovz=False)  # The tested VAT archive predates OVZ.
# This delta archive omits unchanged CF meta files; pair its own selected HLSL
# bytes with the corresponding GUIDs present in the archived Graph itself.
archive_sources = {
    module['cf'](original, fn)['m_FunctionSource']: archive / 'NBShaders2/ShaderGraph' / filename
    for fn, filename in [('NBGraphBaseColor', 'NBGraphBaseColor.hlsl'),
                         ('NBGraphBaseUV', 'NBGraphBaseUV.hlsl'),
                         ('NBGraphVertexOffset', 'NBGraphVertexOffset.hlsl')]
}
module['check_hlsl_abis'](archive, before, archive_sources)
legacy_text = (Path('Packages/NB_FX') / module['LEGACY']).read_text(encoding='utf-8-sig')
candidate, audit = module['inject'](before, resource, legacy_text)
module['check_hlsl_abis'](archive, candidate, archive_sources)
assert len(audit['changedExistingEdges']) == 7
assert audit['existingCFABIUnchanged'] and audit['allOtherExistingObjectsAndEdgesPreserved']
assert len(candidate) > len(before)
assert any(p['action'].startswith('added') for p in audit['properties'])
second, audit2 = module['inject'](before, resource, legacy_text)
assert module['serialize'](candidate) == module['serialize'](second), 'Deterministic preview'

negative = []
def rejects(label, action):
    try:
        action()
    except AssertionError:
        negative.append(label)
    else:
        raise AssertionError('Expected rejection: ' + label)

rejects('already-integrated VAT Graph', lambda: module['require_features'](original, require_ovz=False))
modified = copy.deepcopy(before)
prop = module['unique']((o for o in modified if o.get('m_OverrideReferenceName') == '_NB_CustomDataFlag2Lo16'), 'CD2')
prop['m_FloatType'] = 1
rejects('CD2 Int schema masquerading as Float', lambda: module['require_features'](modified, require_ovz=False))
modified = copy.deepcopy(before)
vo = module['cf'](modified, 'NBGraphVertexOffset')
by = {o['m_ObjectId']: o for o in modified}
uv3 = module['incoming'](modified[0], vo, 'UV3', by)
by[uv3['m_Node']['m_Id']]['m_OutputChannel'] = 2
rejects('F0 fake UV3 channel', lambda: module['require_features'](modified, require_ovz=False))
modified = copy.deepcopy(before)
n = module['cf'](modified, 'NBGraphBaseColor')
by = {o['m_ObjectId']: o for o in modified}
by[n['m_Slots'][0]['m_Id']]['m_DisplayName'] = 'StaleABI'
rejects('CF ordered HLSL ABI drift', lambda: module['check_hlsl_abis'](archive, modified, archive_sources))
rejects('SubTarget patch replay', lambda: module['patch_subtarget']('"NB_GRAPH_NO_VAT"'))
rejects('output outside owner', lambda: module['owned_new_path'](Path('Packages/NB_FX/must-not-create')))
ast.parse((OWNER / 'build_root_vat.py').read_text(encoding='utf-8'))
report = {'scope': 'offline builder preservation checks, not Unity compile/discovery/runtime',
          'archiveSHA256': resource['archiveGraphSHA256'], 'readWholeArchiveOnlyInMemory': True,
          'archivePredatesOVZ': True, 'publicCaptureAndBuildStillRequireActualOVZ': True,
          'writtenWholeGraphs': 0, 'inputObjects': len(before), 'outputObjectsInMemory': len(candidate),
          'preservedExistingCFABIs': len(module['abi'](before)), 'existingEdgesRewired': 7,
          'addedPropertyClosures': len({p['reference'] for p in audit['properties'] if p['action'].startswith('added')}),
          'exactLeafResources': len(provenance['files']), 'deterministic': True,
          'expectedRejects': negative, 'applied': False, 'ranUnity': False,
          'builderSHA256': hashlib.sha256((OWNER / 'build_root_vat.py').read_bytes()).hexdigest()}
(OWNER / 'selftest-report.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8', newline='\n')
print(json.dumps(report))
