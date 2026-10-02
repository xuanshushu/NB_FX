"""Generate Mask1/2/3 gate candidate from the package bytes at invocation.

Run only in the main Agent's serial window. No package or central Graph write.
The output must be a fresh subdirectory of this owned preview directory.
"""
from pathlib import Path
import argparse, copy, hashlib, json, re, uuid

G = 'NBShaders2/ShaderGraph/NBShaderGraph.shadergraph'
H = 'NBShaders2/ShaderGraph/NBGraphBaseColor.hlsl'
MAPPING = [
    ('_MASKMAP_ON', '_Mask_Toggle', '_NB_TierAllowMask', 'NBGraphTierAllowMask', 'MaskToggle'),
    ('_MASKMAP2_ON', '_Mask2_Toggle', '_NB_TierAllowMask2', 'NBGraphTierAllowMask2', 'Mask2Toggle'),
    ('_MASKMAP3_ON', '_Mask3_Toggle', '_NB_TierAllowMask3', 'NBGraphTierAllowMask3', 'Mask3Toggle'),
]

def function_signature_and_body(source, precision):
    start = source.index('void NBGraphBaseColor_' + precision + '(')
    brace = source.index('{', start)
    depth = 0
    for end in range(brace, len(source)):
        depth += (source[end] == '{') - (source[end] == '}')
        if depth == 0:
            return source[start:brace], source[brace + 1:end]
    raise AssertionError('Unclosed NBGraphBaseColor function body')

def function_parameter_names(source, precision):
    signature, _ = function_signature_and_body(source, precision)
    arguments = signature[signature.index('(') + 1:signature.rindex(')')]
    return [re.search(r'([A-Za-z_]\w*)\s*$', parameter.strip()).group(1)
            for parameter in arguments.split(',')]

def append_hlsl_gates(source):
    assert 'NBGraphTierAllowMask' not in source, 'Gate already present; do not overwrite or duplicate a prior slice'
    _, half_body = function_signature_and_body(source, 'half')
    assert not re.search(r'NBGraphBaseColor_float\s*\(', half_body), \
        'Future half-to-float wrapper requires a separately reviewed forwarding ABI; do not gate twice or omit forwarding parameters'
    for precision in ('float', 'half'):
        start = source.index('void NBGraphBaseColor_' + precision + '(')
        brace = source.index('{', start)
        signature = source[start:brace]
        anchor = '    out ' + precision + '4 Out'
        assert signature.count(anchor) == 1
        parameters = '    float NBGraphTierAllowMask, float NBGraphTierAllowMask2, float NBGraphTierAllowMask3,\n'
        changed = signature.replace(anchor, parameters + anchor)
        guard = ('\n    // Derived Mask capability gates; serialized feature inputs remain intent.\n'
                 '    MaskToggle *= NBGraphTierAllowMask > 0.5 ? 1.0 : 0.0;\n'
                 '    Mask2Toggle *= NBGraphTierAllowMask2 > 0.5 ? 1.0 : 0.0;\n'
                 '    Mask3Toggle *= NBGraphTierAllowMask3 > 0.5 ? 1.0 : 0.0;\n')
        source = source[:start] + changed + source[brace:brace + 1] + guard + source[brace + 1:]
    return source

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--source-package', required=True)
    ap.add_argument('--output', required=True)
    args = ap.parse_args()
    package = Path(args.source_package).resolve()
    output = Path(args.output).resolve()
    owned = Path(__file__).resolve().parent
    assert owned in output.parents and output != owned, 'Output must be a fresh owned preview subdirectory'
    assert package != output and package not in output.parents
    assert not output.exists(), 'No overwrite of existing candidate'
    original_graph = (package / G).read_bytes()
    original_hlsl = (package / H).read_bytes()
    source = original_graph.decode('utf-8-sig')
    decoder = json.JSONDecoder(); objects = []; position = 0
    while position < len(source):
        while position < len(source) and source[position].isspace(): position += 1
        if position == len(source): break
        obj, position = decoder.raw_decode(source, position); objects.append(obj)
    old = copy.deepcopy(objects)
    by_id = {obj['m_ObjectId']: obj for obj in objects if 'm_ObjectId' in obj}
    root = objects[0]
    properties = [by_id[ref['m_Id']] for ref in root['m_Properties']]
    name = lambda obj: obj.get('m_OverrideReferenceName') or obj.get('m_DefaultReferenceName')
    cfs = [obj for obj in objects if obj.get('m_Type', '').endswith('CustomFunctionNode') and obj.get('m_FunctionName') == 'NBGraphBaseColor']
    assert len(cfs) == 1, 'Central CF ownership must be unambiguous'
    cf = cfs[0]; slots = [by_id[ref['m_Id']] for ref in cf['m_Slots']]
    maximum = max(slot['m_Id'] for slot in slots)
    additions = []; new_slot_refs = []
    for index, (keyword, intent, uniform, label, consumer) in enumerate(MAPPING):
        assert not any(name(prop) == uniform for prop in properties), 'Derived property collision'
        original_property = next(prop for prop in properties if name(prop) == intent)
        assert original_property['m_Type'] == 'UnityEditor.ShaderGraph.Internal.Vector1ShaderProperty'
        prop = copy.deepcopy(original_property)
        prop.update(m_ObjectId=uuid.uuid4().hex, m_Guid={'m_GuidSerialized': str(uuid.uuid4())},
                    m_Name=label, m_DefaultReferenceName=uniform, m_OverrideReferenceName=uniform,
                    m_Hidden=True, m_Value=1.0, m_FloatType=0)
        objects.append(prop); root['m_Properties'].append({'m_Id': prop['m_ObjectId']})
        node = next(obj for obj in objects if obj.get('m_Type', '').endswith('PropertyNode') and obj.get('m_Property', {}).get('m_Id') == original_property['m_ObjectId'])
        new_node = copy.deepcopy(node)
        new_node.update(m_ObjectId=uuid.uuid4().hex, m_Property={'m_Id': prop['m_ObjectId']})
        new_node['m_Slots'] = []
        for ref in node['m_Slots']:
            new_slot = copy.deepcopy(by_id[ref['m_Id']]); new_slot['m_ObjectId'] = uuid.uuid4().hex
            new_slot['m_DisplayName'] = new_slot['m_ShaderOutputName'] = label
            objects.append(new_slot); new_node['m_Slots'].append({'m_Id': new_slot['m_ObjectId']})
        objects.append(new_node); root['m_Nodes'].append({'m_Id': new_node['m_ObjectId']})
        original_slot = next(slot for slot in slots if slot.get('m_DisplayName') == consumer)
        assert original_slot['m_SlotType'] == 0, 'Consumer must be a scalar input'
        new_slot = copy.deepcopy(original_slot)
        new_slot.update(m_ObjectId=uuid.uuid4().hex, m_Id=maximum + index + 1,
                        m_DisplayName=label, m_ShaderOutputName=label, m_Value=1.0, m_DefaultValue=1.0)
        objects.append(new_slot)
        new_slot_ref = {'m_Id': new_slot['m_ObjectId']}
        cf['m_Slots'].append(new_slot_ref); new_slot_refs.append(new_slot_ref)
        root['m_Edges'].append({'m_OutputSlot': {'m_Node': {'m_Id': new_node['m_ObjectId']}, 'm_SlotId': by_id[node['m_Slots'][0]['m_Id']]['m_Id']},
                              'm_InputSlot': {'m_Node': {'m_Id': cf['m_ObjectId']}, 'm_SlotId': new_slot['m_Id']}})
        additions.append({'keyword': keyword, 'intentProperty': intent, 'derivedFloat': uniform, 'portLabel': label,
                          'portId': new_slot['m_Id'], 'consumerParameter': consumer, 'default': 1.0})
    for before, after in zip(old[1:], objects[1:]):
        b = copy.deepcopy(before); a = copy.deepcopy(after)
        if b.get('m_ObjectId') == cf['m_ObjectId']:
            # Raw slots contain outputs in the middle of old inputs. SG 17.3
            # generates inputs first, then outputs; never insert before the
            # first raw output and thereby change the input ABI.
            assert [ref for ref in a['m_Slots'] if ref not in new_slot_refs] == b['m_Slots']
            b.pop('m_Slots'); a.pop('m_Slots')
        assert a == b, 'Existing object changed beyond CF slot append'
    for key in ('m_Nodes', 'm_Properties', 'm_Edges'):
        assert root[key][:len(old[0][key])] == old[0][key], 'Existing lists or wiring changed'
    a = copy.deepcopy(root); b = copy.deepcopy(old[0])
    for key in ('m_Nodes', 'm_Properties', 'm_Edges'): a.pop(key); b.pop(key)
    assert a == b, 'Target/interpolator/root settings changed'
    hlsl = append_hlsl_gates(original_hlsl.decode('utf-8-sig'))
    generated_by_id = {obj['m_ObjectId']: obj for obj in objects if 'm_ObjectId' in obj}
    generated_slots = [generated_by_id[ref['m_Id']] for ref in cf['m_Slots']]
    input_slots = [slot for slot in generated_slots if slot['m_SlotType'] == 0]
    output_slots = [slot for slot in generated_slots if slot['m_SlotType'] == 1]
    # Mirrors the inspected official CustomFunctionNode.GenerateNodeCode:
    # GetInputSlots + GetOutputSlots, followed by two separate foreach calls.
    call_slots = input_slots + output_slots
    call_labels = [slot['m_DisplayName'] for slot in call_slots]
    for precision in ('float', 'half'):
        assert call_labels == function_parameter_names(hlsl, precision), \
            'Actual SG filtered call argument order does not match HLSL ' + precision + ' signature'
        _, body = function_signature_and_body(hlsl, precision)
        assert body.count('MaskToggle *= NBGraphTierAllowMask > 0.5') == 1, 'Gate must execute once per independent entry'
    abi_audit = {'argumentCount': len(call_labels), 'SGCallOrder': 'GetInputSlots then GetOutputSlots',
                 'floatSignatureMatchesEveryArgument': True, 'halfSignatureMatchesEveryArgument': True,
                 'halfToFloatWrapperPresent': False, 'last12': [
                     {'callArgumentIndex0': len(call_slots) - 12 + index, 'slotId': slot['m_Id'],
                      'slotLabel': slot['m_DisplayName'], 'slotType': slot['m_SlotType'],
                      'floatParameter': function_parameter_names(hlsl, 'float')[-12 + index],
                      'halfParameter': function_parameter_names(hlsl, 'half')[-12 + index]}
                     for index, slot in enumerate(call_slots[-12:])]}
    assert (package / G).read_bytes() == original_graph and (package / H).read_bytes() == original_hlsl, 'Source changed during generation'
    generated = {G: '\n\n'.join(json.dumps(obj, indent=4, ensure_ascii=False) for obj in objects) + '\n', H: hlsl}
    output.mkdir(parents=True)
    records = []
    for relative, text in generated.items():
        path = output / relative; path.parent.mkdir(parents=True, exist_ok=True); path.write_bytes(text.encode('utf-8'))
        old_bytes = original_graph if relative == G else original_hlsl
        records.append({'path': relative, 'beforeSHA256': hashlib.sha256(old_bytes).hexdigest(), 'afterSHA256': hashlib.sha256(path.read_bytes()).hexdigest()})
    manifest = {'scope': 'Mask1/2/3 gate preview, no installation or Unity execution', 'identitySource': 'source',
                'sourcePackage': str(package), 'records': records, 'files': records, 'mapping': additions,
                'builderRevision': 2, 'ABIAudit': abi_audit,
                'objectsBefore': len(old), 'objectsAfter': len(objects), 'oldCFPortsAndEdgesPreserved': True,
                'oldPropertiesAndTargetsPreserved': True, 'newPackedBits': 0, 'newKeywords': 0, 'completeGraphTier': False}
    (output / 'manifest.json').write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf-8')
    print(json.dumps({'manifest': str(output / 'manifest.json'), 'files': records, 'mapping': additions}, indent=2))

if __name__ == '__main__': main()
