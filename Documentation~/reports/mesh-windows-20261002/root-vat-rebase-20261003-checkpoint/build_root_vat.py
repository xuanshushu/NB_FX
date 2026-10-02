"""Dynamic ordinary-Mesh VAT preview. No package, Unity, Git, or installer writes.

Capture and review a fresh input lock AFTER real Root F0 and four-word CD exist,
then build into a new child directory of this script's owner directory. Only the
two VAT node closures come from the tested archive; GraphData and every existing
node, property, slot and connection come from the supplied current package.
"""
from pathlib import Path
import argparse
import copy
import hashlib
import json
import re
import uuid

OWNER = Path(__file__).resolve().parent
GRAPH = 'NBShaders2/ShaderGraph/NBShaderGraph.shadergraph'
SUBTARGET = 'NBShaders2/ShaderGraph/Editor/NBGraphUnlitSubTarget.cs'
LEGACY = 'NBShaders2/Shader/NBShader.shader'
NS = uuid.UUID('a395ef28-ac40-47fa-b6a7-291d1ad41dfe')
ORIGINAL_WRAPPERS = {
    'XuanXuanRenderUtility/Shader/HLSL/HoudiniVAT.hlsl': '5984e8aecc20c3fe99f929eab385f94f8c29b7c07be47fb4b7090ea063346317',
    'XuanXuanRenderUtility/Shader/HLSL/TyflowVAT.hlsl': '5091f80d00b150c8af7f7b71022162720474181fae37b41c587fdc74effa22d7',
}
REWIRES = {
    'NBGraphVertexOffset': ('PositionOS', 'NormalOS'),
    'NBGraphFogVertex': ('PositionOS',),
    'NBGraphUVVertex': ('PositionOS',),
    'NBGraphSixWayBake': ('NormalWS', 'TangentWS', 'BitangentWS'),
}


def sha(data):
    return hashlib.sha256(data).hexdigest()


def fail(message):
    raise AssertionError(message)


def unique(values, label):
    values = list(values)
    if len(values) != 1:
        fail(f'{label}: expected one, found {len(values)}')
    return values[0]


def decode(text):
    decoder = json.JSONDecoder()
    i, objects = 0, []
    while i < len(text):
        while i < len(text) and text[i].isspace():
            i += 1
        if i < len(text):
            obj, i = decoder.raw_decode(text, i)
            objects.append(obj)
    return objects


def serialize(objects):
    return '\n\n'.join(json.dumps(o, indent=4, ensure_ascii=False) for o in objects) + '\n'


def slot(node, name, by):
    return unique((by[q['m_Id']] for q in node['m_Slots']
                   if by[q['m_Id']]['m_DisplayName'] == name), node.get('m_FunctionName', '') + ':' + name)


def incoming(root, node, name, by):
    sid = slot(node, name, by)['m_Id']
    edge = unique((e for e in root['m_Edges']
                   if e['m_InputSlot']['m_Node']['m_Id'] == node['m_ObjectId']
                   and e['m_InputSlot']['m_SlotId'] == sid), 'incoming:' + name)
    return copy.deepcopy(edge['m_OutputSlot'])


def cf(objects, name):
    return unique((o for o in objects if o.get('m_FunctionName') == name), name)


def abi(objects):
    by = {o['m_ObjectId']: o for o in objects}
    return {o['m_FunctionName']: {
        'nodeId': o['m_ObjectId'], 'sourceGUID': o['m_FunctionSource'],
        'orderedSlots': [[s['m_DisplayName'], s['m_Type'], s['m_SlotType'], s['m_Id']]
                         for q in o['m_Slots'] for s in [by[q['m_Id']]]]
    } for o in objects if o.get('m_FunctionName')}


def check_graph(objects):
    ids = [o['m_ObjectId'] for o in objects]
    assert len(ids) == len(set(ids)), 'Duplicate object IDs'
    by = {o['m_ObjectId']: o for o in objects}
    root = objects[0]
    node_ids = [q['m_Id'] for q in root['m_Nodes']]
    assert len(node_ids) == len(set(node_ids))
    for key in ('m_Nodes', 'm_Properties', 'm_CategoryData'):
        assert all(q['m_Id'] in by for q in root.get(key, [])), key
    inputs = set()
    graph = {n: [] for n in node_ids}
    for e in root['m_Edges']:
        for end in ('m_OutputSlot', 'm_InputSlot'):
            endpoint = e[end]
            nid = endpoint['m_Node']['m_Id']
            assert nid in graph, 'Edge node missing'
            n = by[nid]
            s = unique((by[q['m_Id']] for q in n['m_Slots']
                        if by[q['m_Id']]['m_Id'] == endpoint['m_SlotId']), 'edge slot')
            assert s['m_SlotType'] == (end == 'm_OutputSlot'), 'Wrong slot direction'
        inp = (e['m_InputSlot']['m_Node']['m_Id'], e['m_InputSlot']['m_SlotId'])
        assert inp not in inputs, 'Multiple edges into one input'
        inputs.add(inp)
        graph[e['m_OutputSlot']['m_Node']['m_Id']].append(inp[0])
    visiting, done = set(), set()
    def visit(n):
        assert n not in visiting, 'Graph cycle'
        if n in done:
            return
        visiting.add(n)
        for child in graph[n]:
            visit(child)
        visiting.remove(n)
        done.add(n)
    for n in graph:
        visit(n)


def signature(text, function):
    match = unique(re.finditer(r'void\s+' + re.escape(function) + r'\s*\((.*?)\)\s*\{', text, re.S), function)
    args = []
    for p in match.group(1).split(','):
        words = p.strip().split()
        assert len(words) in (2, 3), (function, p)
        is_out = words[0] == 'out'
        assert len(words) == (3 if is_out else 2)
        args.append([words[-1], words[-2], int(is_out)])
    return args


def check_hlsl_abis(package, objects, extra=None):
    extra = extra or {}
    by = {o['m_ObjectId']: o for o in objects}
    guid_files = {}
    for p in (package / 'NBShaders2/ShaderGraph').rglob('*.hlsl.meta'):
        m = re.search(r'^guid:\s*([a-f0-9]{32})\s*$', p.read_text(encoding='utf-8-sig'), re.M)
        assert m
        guid_files[m.group(1)] = p.with_suffix('')
    guid_files.update(extra)
    types = {'Vector1MaterialSlot': 'float', 'Vector2MaterialSlot': 'float2',
             'Vector3MaterialSlot': 'float3', 'Vector4MaterialSlot': 'float4',
             'Texture2DInputMaterialSlot': 'UnityTexture2D', 'UVMaterialSlot': 'float2'}
    checked = []
    for n in objects:
        if not n.get('m_FunctionName'):
            continue
        assert n['m_FunctionSource'] in guid_files, ('Unknown CF source GUID', n['m_FunctionName'])
        source = guid_files[n['m_FunctionSource']]
        text = source.read_text(encoding='utf-8-sig')
        slots = [by[q['m_Id']] for q in n['m_Slots']]
        ordered = [s for s in slots if s['m_SlotType'] == 0] + [s for s in slots if s['m_SlotType'] == 1]
        def slot_type(s):
            kind = s['m_Type'].split('.')[-1]
            if kind == 'DynamicVectorMaterialSlot':
                value = s['m_Value']
                if isinstance(value, (float, int)):
                    return 'float'
                assert isinstance(value, dict) and set(value) in ({'x', 'y'}, {'x', 'y', 'z'}, {'x', 'y', 'z', 'w'}), ('Unreviewed dynamic slot', s['m_DisplayName'])
                return 'float' + str(len(value))
            return types[kind]
        call = [[s['m_DisplayName'], slot_type(s), s['m_SlotType']] for s in ordered]
        assert call == signature(text, n['m_FunctionName'] + '_float'), ('CF float ABI mismatch', n['m_FunctionName'])
        if 'void ' + n['m_FunctionName'] + '_half(' in text:
            # Existing half wrappers intentionally keep packed Float words and
            # several sampler/UV inputs full precision. Do not narrow these.
            half = signature(text, n['m_FunctionName'] + '_half')
            assert call == [[name, kind.replace('half', 'float'), out] for name, kind, out in half], ('CF half ABI mismatch', n['m_FunctionName'])
        checked.append(n['m_FunctionName'])
    return checked


def require_features(objects, require_ovz=True):
    by = {o['m_ObjectId']: o for o in objects}
    root = objects[0]
    assert not any(o.get('m_FunctionName', '').startswith('NBGraphVAT') for o in objects), 'VAT already exists; do not replay'
    assert not any(o.get('m_FunctionName', '').startswith('NBGraphCustomLocal') for o in objects), 'CustomLocal already exists; VAT order needs specific review'
    for function in ('NBGraphVertexOffset', 'NBGraphUVVertex', 'NBGraphBaseUV'):
        n = cf(objects, function)
        uv = incoming(root, n, 'UV3', by)
        uvnode = by[uv['m_Node']['m_Id']]
        assert uvnode['m_Type'].endswith('.UVNode') and uvnode['m_OutputChannel'] == 3, 'F0 needs actual TEXCOORD3'
        flip = incoming(root, n, 'FlipbookToggle', by)
        flipnode = by[flip['m_Node']['m_Id']]
        assert by[flipnode['m_Property']['m_Id']]['m_OverrideReferenceName'] == '_FlipbookBlending', 'Real F0 Toggle required'
    for word in range(4):
        for half in ('Lo', 'Hi'):
            ref = f'_NB_CustomDataFlag{word}{half}16'
            p = unique((o for o in objects if o.get('m_OverrideReferenceName') == ref), ref)
            assert p['m_Type'].endswith('.Vector1ShaderProperty') and p.get('m_FloatType') == 0
            assert p['m_GeneratePropertyBlock'] and not p['m_Hidden'], 'Use actual Float property storage/schema'
            assert any(o.get('m_Property', {}).get('m_Id') == p['m_ObjectId'] for o in objects), ref + ':actual property node'
    # The downstream Float-word consumers must already be real, not only new blackboard properties.
    for function, word in [('NBGraphBaseColor', 1), ('NBGraphVertexOffset', 3)]:
        n = cf(objects, function)
        for half in ('Lo', 'Hi'):
            incoming(root, n, f'CustomDataFlag{word}{half}16', by)
    if require_ovz:
        cf(objects, 'NBGraphOverrideDepth')
    for function in REWIRES:
        cf(objects, function)


def source_inventory(package):
    paths = {package / 'package.json'}
    # Includes every imported current source/meta in the touched/shared stacks,
    # and also freezes runtime flags and GUI synchronization without editing them.
    for rel in ('NBShaders2', 'XuanXuanRenderUtility'):
        for p in (package / rel).rglob('*'):
            if p.is_file() and p.suffix.lower() in {'.cs', '.hlsl', '.shader', '.shadergraph', '.meta', '.asmdef', '.asmref'}:
                paths.add(p)
    return {p.relative_to(package).as_posix(): sha(p.read_bytes()) for p in sorted(paths)}


def owned_new_path(path):
    path = path.resolve()
    assert OWNER in path.parents and not path.exists(), 'Only a NEW path inside root-vat-rebase is writable'
    return path


def capture(package, output):
    objects = decode((package / GRAPH).read_text(encoding='utf-8-sig'))
    check_graph(objects)
    require_features(objects)
    checked = check_hlsl_abis(package, objects)
    hashes = source_inventory(package)
    lock = {'schema': 'NBFX-root-vat-input-v1', 'inputPackage': package.as_posix(),
            'sourceSHA256': hashes, 'graphABI': abi(objects), 'CFHLSLABIChecked': checked,
            'preconditions': 'Real Root OVZ/UVP/F0/four-word CD, no VAT/CustomLocal; capture is not acceptance or validation'}
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(lock, indent=2) + '\n', encoding='utf-8', newline='\n')
    return {'capturedSources': len(hashes), 'graphObjects': len(objects), 'output': output.as_posix()}


def load_resources():
    provenance = json.loads((OWNER / 'resource-provenance.json').read_text(encoding='utf-8'))
    for rel, expected in provenance['files'].items():
        assert sha((OWNER / 'resources/package' / rel).read_bytes()) == expected, 'Resource hash drift: ' + rel
    p = OWNER / 'resources/vat-node-templates.json'
    assert sha(p.read_bytes()) == provenance['nodeTemplatesSHA256']
    resource = json.loads(p.read_text(encoding='utf-8'))
    assert not any(o['m_Type'].endswith(('.GraphData', '.CategoryData', '.BlockNode')) for o in resource['objects'])
    return resource, provenance


def legacy_defaults(text):
    defaults = {}
    for line in text.splitlines():
        m = re.match(r'\s*(?:\[[^\]]+\]\s*)*(_\w+)\s*\(.*?,\s*(?:Float|Int|Range\([^)]*\))\s*\)\s*=\s*([-+.\deE]+)', line)
        if m:
            defaults[m.group(1)] = float(m.group(2))
    return defaults


def inject(objects, resource, default_text):
    before = copy.deepcopy(objects)
    o = copy.deepcopy(objects)
    root = o[0]
    by = {x['m_ObjectId']: x for x in o}
    rb = {x['m_ObjectId']: x for x in resource['objects']}
    categories = [by[q['m_Id']] for q in root['m_CategoryData']]
    category = unique((c for c in categories if not c.get('m_Name')), 'default blackboard category')
    defaults = legacy_defaults(default_text)
    vo, six = cf(o, 'NBGraphVertexOffset'), cf(o, 'NBGraphSixWayBake')
    raw = {name: incoming(root, vo, name, by) for name in ('PositionOS', 'NormalOS')}
    rawbasis = {name: incoming(root, six, name, by) for name in ('NormalWS', 'TangentWS', 'BitangentWS')}
    for fn in ('NBGraphFogVertex', 'NBGraphUVVertex'):
        assert incoming(root, cf(o, fn), 'PositionOS', by) == raw['PositionOS'], 'Unexpected preVAT order: ' + fn
    mapped, property_audit, uv_audit = {}, [], []

    def new_id(old):
        return uuid.uuid5(NS, 'root-vat:' + old).hex

    def add(obj):
        assert obj['m_ObjectId'] not in by, 'New object ID collision'
        by[obj['m_ObjectId']] = obj
        o.append(obj)

    def clone_node(node):
        n = copy.deepcopy(node)
        n['m_ObjectId'] = new_id(node['m_ObjectId'])
        n['m_Group'] = {'m_Id': ''}
        n['m_Slots'] = []
        for q in node['m_Slots']:
            s = copy.deepcopy(rb[q['m_Id']])
            s['m_ObjectId'] = new_id(q['m_Id'])
            add(s)
            n['m_Slots'].append({'m_Id': s['m_ObjectId']})
        add(n)
        root['m_Nodes'].append({'m_Id': n['m_ObjectId']})
        return n

    # Clone only the tested two VAT CF nodes and slots. Everything else is
    # resolved to actual fresh inputs or a minimal missing property/UV closure.
    vat = clone_node(cf(resource['objects'], 'NBGraphVATSoftBody'))
    basis = clone_node(cf(resource['objects'], 'NBGraphVATWorldBasis'))
    mapped.update({n['m_FunctionName']: n for n in (vat, basis)})

    def prop_source(template_node, preferred=None):
        tp = rb[template_node['m_Property']['m_Id']]
        ref = tp['m_OverrideReferenceName']
        properties = [p for p in o if p.get('m_OverrideReferenceName') == ref]
        if properties:
            p = unique(properties, ref)
            assert p['m_Type'] == tp['m_Type'], 'Existing property type drift: ' + ref
            assert p['m_GeneratePropertyBlock'] and not p.get('m_PerRendererData'), ref
            if p['m_Type'].endswith('.Vector1ShaderProperty'):
                assert p.get('m_FloatType') == 0, 'VAT must consume actual Float: ' + ref
            nodes = [n for n in o if n.get('m_Property', {}).get('m_Id') == p['m_ObjectId']]
            assert nodes, ref + ':actual property node'
            pn = by[preferred['m_Node']['m_Id']] if preferred else nodes[0]
            assert pn in nodes, 'Actual fresh consumer source does not reference expected property: ' + ref
            property_audit.append({'reference': ref, 'action': 'reused entire existing property/node/slot'})
        else:
            assert not ref.startswith('_NB_CustomDataFlag'), 'CD storage prerequisite was not integrated'
            p = copy.deepcopy(tp)
            p['m_ObjectId'] = new_id(tp['m_ObjectId'])
            p['m_Guid'] = {'m_GuidSerialized': str(uuid.uuid5(NS, 'property-guid:' + ref))}
            if p['m_Type'].endswith('.Vector1ShaderProperty'):
                assert ref in defaults, 'Missing legacy source default: ' + ref
                assert p['m_Value'] == defaults[ref], ('Reviewed archive default drift', ref, p['m_Value'], defaults[ref])
                assert p.get('m_FloatType') == 0 and not p['m_Hidden']
            add(p)
            pn = clone_node(template_node)
            pn['m_Property'] = {'m_Id': p['m_ObjectId']}
            root['m_Properties'].append({'m_Id': p['m_ObjectId']})
            category['m_ChildObjectList'].append({'m_Id': p['m_ObjectId']})
            property_audit.append({'reference': ref, 'action': 'added tested Float/texture property closure', 'default': p['m_Value']})
        output = unique((by[q['m_Id']] for q in pn['m_Slots'] if by[q['m_Id']]['m_SlotType'] == 1), ref + ':out')
        return {'m_Node': {'m_Id': pn['m_ObjectId']}, 'm_SlotId': output['m_Id']}

    def uv_source(template_node):
        channel = template_node['m_OutputChannel']
        existing = [n for n in o if n['m_Type'].endswith('.UVNode') and n['m_OutputChannel'] == channel]
        if existing:
            # Channels already used by VO/F0 are the fresh consumer's actual
            # source. This also preserves any future distinction between nodes.
            if channel <= 3:
                return incoming(root, vo, 'UV' + str(channel), by)
            n = existing[0]
            action = 'reused existing UV node'
        else:
            n = clone_node(template_node)
            action = 'added tested raw TEXCOORD node'
        uv_audit.append({'channel': channel, 'action': action})
        return {'m_Node': {'m_Id': n['m_ObjectId']}, 'm_SlotId': 0}

    bound_sources = {}
    for fn, node in mapped.items():
        for q in node['m_Slots']:
            s = by[q['m_Id']]
            if s['m_SlotType'] != 0:
                continue
            label = s['m_DisplayName']
            template_source = resource['bindings'][fn][label]
            sn = rb[template_source['m_Node']['m_Id']]
            if fn == 'NBGraphVATSoftBody' and label in raw:
                src = raw[label]
            elif fn == 'NBGraphVATWorldBasis' and label.startswith('Raw'):
                src = rawbasis[label[3:]]
            elif sn.get('m_FunctionName') in mapped:
                src = {'m_Node': {'m_Id': mapped[sn['m_FunctionName']]['m_ObjectId']}, 'm_SlotId': template_source['m_SlotId']}
            elif 'm_Property' in sn:
                preferred = None
                if fn == 'NBGraphVATSoftBody' and label in ('Flags1Lo16', 'Flags1Hi16', 'CustomDataFlag2Lo16', 'CustomDataFlag2Hi16', 'FlipbookToggle'):
                    preferred = incoming(root, vo, label, by)
                src = prop_source(sn, preferred)
            elif sn['m_Type'].endswith('.UVNode'):
                src = uv_source(sn)
            else:
                fail('Unreviewed VAT dependency: ' + label)
            root['m_Edges'].append({'m_OutputSlot': copy.deepcopy(src), 'm_InputSlot': {'m_Node': {'m_Id': node['m_ObjectId']}, 'm_SlotId': s['m_Id']}})
            bound_sources[fn + ':' + label] = copy.deepcopy(src)

    rewires = []
    for function, labels in REWIRES.items():
        n = cf(o, function)
        for label in labels:
            sid = slot(n, label, by)['m_Id']
            edge = unique((e for e in root['m_Edges'] if e['m_InputSlot']['m_Node']['m_Id'] == n['m_ObjectId']
                           and e['m_InputSlot']['m_SlotId'] == sid), 'rewire:' + function + ':' + label)
            outnode = basis if function == 'NBGraphSixWayBake' else vat
            outlabel = label if outnode is basis else 'Out' + label
            new = {'m_Node': {'m_Id': outnode['m_ObjectId']}, 'm_SlotId': slot(outnode, outlabel, by)['m_Id']}
            rewires.append({'consumer': function, 'input': label, 'before': copy.deepcopy(edge['m_OutputSlot']), 'after': new})
            edge['m_OutputSlot'] = new

    # Every original object is preserved. Only GraphData lists/edges and the
    # existing default category child list can change; no CF signatures do.
    changed = [a['m_ObjectId'] for a, b in zip(o, before) if a != b]
    assert set(changed).issubset({root['m_ObjectId'], category['m_ObjectId']})
    assert [x['m_ObjectId'] for x in o[:len(before)]] == [x['m_ObjectId'] for x in before]
    for key in root:
        if key not in ('m_Nodes', 'm_Properties', 'm_Edges'):
            assert root[key] == before[0][key], ('Unexpected GraphData mutation', key)
    assert root['m_Nodes'][:len(before[0]['m_Nodes'])] == before[0]['m_Nodes']
    assert root['m_Properties'][:len(before[0]['m_Properties'])] == before[0]['m_Properties']
    oldcategory = next(x for x in before if x['m_ObjectId'] == category['m_ObjectId'])
    for key in category:
        if key != 'm_ChildObjectList':
            assert category[key] == oldcategory[key], ('Unexpected category mutation', key)
    assert category['m_ChildObjectList'][:len(oldcategory['m_ChildObjectList'])] == oldcategory['m_ChildObjectList']
    allowed = {(cf(before, f)['m_ObjectId'], slot(cf(before, f), name, {x['m_ObjectId']:x for x in before})['m_Id'])
               for f, names in REWIRES.items() for name in names}
    oldedges = root['m_Edges'][:len(before[0]['m_Edges'])]
    changededges = []
    for old, new in zip(before[0]['m_Edges'], oldedges):
        assert old['m_InputSlot'] == new['m_InputSlot']
        if old != new:
            dest = (old['m_InputSlot']['m_Node']['m_Id'], old['m_InputSlot']['m_SlotId'])
            assert dest in allowed, ('Forbidden existing edge mutation', dest)
            changededges.append(dest)
    assert set(changededges) == allowed, 'Exactly seven reviewed consumer rewires required'
    for oldn in before:
        if oldn.get('m_FunctionName'):
            assert abi(before)[oldn['m_FunctionName']] == abi(o)[oldn['m_FunctionName']]
    assert incoming(root, vat, 'UV3', by) == incoming(root, vo, 'UV3', by)
    assert incoming(root, vat, 'FlipbookToggle', by) == incoming(root, vo, 'FlipbookToggle', by)
    for half in ('Lo', 'Hi'):
        n = by[incoming(root, vat, 'CustomDataFlag2' + half + '16', by)['m_Node']['m_Id']]
        assert by[n['m_Property']['m_Id']]['m_OverrideReferenceName'] == '_NB_CustomDataFlag2' + half + '16'
    check_graph(o)
    return o, {'oldObjects': len(before), 'newObjects': len(o), 'changedOriginalObjectIds': changed,
               'changedExistingEdges': rewires, 'existingCFABIUnchanged': True,
               'allOtherExistingObjectsAndEdgesPreserved': True, 'actualF0UV3AndTogglePreserved': True,
               'actualCDWord2Reused': True, 'properties': property_audit, 'UVs': uv_audit,
               'boundNewInputs': bound_sources}


def patch_subtarget(text):
    assert '"NB_GRAPH_NO_VAT"' not in text, 'Screen VAT guard already present; review rather than replay'
    needle = '            if (lightMode == "NBCameraOpaqueDistortPass")\n                pass = WithNBPassDefine(pass, "NB_GRAPH_CAMERA_OPAQUE_PASS");'
    assert text.count(needle) == 1, 'SubTarget screen branch drift'
    return text.replace(needle, '            pass = WithNBPassDefine(pass, "NB_GRAPH_NO_VAT");\n' + needle, 1)


def build(package, lockpath, output):
    lock = json.loads(lockpath.read_text(encoding='utf-8-sig'))
    assert lock['schema'] == 'NBFX-root-vat-input-v1'
    assert lock['inputPackage'] == package.as_posix(), 'Lock belongs to another input package'
    assert source_inventory(package) == lock['sourceSHA256'], 'Source SHA or imported-source inventory drift; capture a reviewed fresh lock'
    objects = decode((package / GRAPH).read_text(encoding='utf-8-sig'))
    assert abi(objects) == lock['graphABI'], 'Graph ABI drift'
    require_features(objects)
    check_hlsl_abis(package, objects)
    resource, provenance = load_resources()
    for rel, digest in ORIGINAL_WRAPPERS.items():
        assert lock['sourceSHA256'][rel] == digest, 'Legacy wrapper changed; re-extract/review instead of copying stale code: ' + rel
    for rel in provenance['files']:
        if rel not in ORIGINAL_WRAPPERS:
            assert not (package / rel).exists(), 'Expected genuinely new VAT source: ' + rel
    candidate, audit = inject(objects, resource, (package / LEGACY).read_text(encoding='utf-8-sig'))
    adapter = OWNER / 'resources/package/NBShaders2/ShaderGraph/NBGraphVATSoftBody.hlsl'
    meta = adapter.with_suffix('.hlsl.meta').read_text(encoding='utf-8')
    guid = unique(re.findall(r'^guid:\s*([a-f0-9]{32})\s*$', meta, re.M), 'VAT adapter meta GUID')
    checked = check_hlsl_abis(package, candidate, {guid: adapter})
    # Read a fresh copy, but do not normalize CRLF before the exact-one patch.
    sub = (package / SUBTARGET).read_text(encoding='utf-8-sig')
    outputs = {rel: (OWNER / 'resources/package' / rel).read_bytes() for rel in provenance['files']}
    outputs[GRAPH] = serialize(candidate).encode('utf-8')
    outputs[SUBTARGET] = patch_subtarget(sub).encode('utf-8')
    assert source_inventory(package) == lock['sourceSHA256'], 'Input changed during preview build'
    output.mkdir(parents=True)
    records = []
    for rel, data in outputs.items():
        p = output / rel
        p.parent.mkdir(parents=True, exist_ok=True)
        p.write_bytes(data)
        records.append({'path': rel, 'target': (package / rel).as_posix(), 'preview': p.as_posix(),
                        'beforeSHA256': lock['sourceSHA256'].get(rel), 'candidateSHA256': sha(data)})
    assert source_inventory(package) == lock['sourceSHA256'], 'Input changed while writing preview; discard this unapproved output, never install it'
    manifest = {'scope': 'V1 common ordinary-Mesh Houdini4/Tyflow6 real interface; PREVIEW, unrun on this Root combination',
                'identitySource': 'fresh supplied Root source + exact tested VAT leaf sources, not inherited result',
                'inputPackage': package.as_posix(), 'inputLockSHA256': sha(lockpath.read_bytes()),
                'inputGraphSHA256': lock['sourceSHA256'][GRAPH], 'archiveGraphUsedOnlyForVATNodeTemplates': resource['archiveGraphSHA256'],
                'applied': False, 'ranUnity': False, 'newKeywordAxes': 0, 'officialPackageWrites': 0,
                'CFHLSLABIChecked': checked, 'graphAudit': audit, 'files': records,
                'next': 'Main Agent review, scene/occupation check, serial isolation install, actual compilation/discovery/render/source receipt; DN0 default SSAO after VAT as separate full-chain gate'}
    (output / 'manifest.json').write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf-8', newline='\n')
    return {'files': len(records), 'graphObjects': [len(objects), len(candidate)], 'rewiredEdges': len(audit['changedExistingEdges']),
            'ABIfunctions': len(checked), 'applied': False, 'ranUnity': False, 'output': output.as_posix()}


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument('--input-package', required=True)
    ap.add_argument('--capture-input-lock', help='Standalone readonly snapshot to a NEW owned JSON; review before building')
    ap.add_argument('--expected-input-lock')
    ap.add_argument('--output')
    args = ap.parse_args()
    package = Path(args.input_package).resolve()
    assert package.is_dir() and (package / 'package.json').exists()
    assert OWNER not in package.parents, 'Do not treat preview resources as a real package'
    if args.capture_input_lock:
        assert not args.expected_input_lock and not args.output
        result = capture(package, owned_new_path(Path(args.capture_input_lock)))
    else:
        assert args.expected_input_lock and args.output
        result = build(package, Path(args.expected_input_lock).resolve(), owned_new_path(Path(args.output)))
    print(json.dumps(result))


if __name__ == '__main__':
    main()
