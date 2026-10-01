#!/usr/bin/env python3
"""FG1 ordinary-Mesh dynamic PREVIEW ONLY. No package, Unity, or Git writes.

Re-read current Graph/CF every run so RF/DD/POM/CA inputs and IDs survive.
Root owns review, apply, import, GPU tests and evidence.
"""
import copy
import hashlib
import json
import uuid
from pathlib import Path

P = Path('/Users/bytedance/UnityProject/NBUnityProject/Packages/NB_FX')
GRAPH = P / 'NBShaders2/ShaderGraph/NBShaderGraph.shadergraph'
COLOR = P / 'NBShaders2/ShaderGraph/NBGraphBaseColor.hlsl'
OUT = Path('/tmp/nbfx-fog-gamma-preview')
NS = uuid.UUID('fd25b741-0415-4c63-aed3-636f5f164a85')

def uid(k):
    return uuid.uuid5(NS, 'FG1:' + k).hex

def replace_one(s, a, b, count=1):
    n = s.count(a)
    if n != count:
        raise AssertionError(f'needle {a[:90]!r}: {n}, expected {count}')
    return s.replace(a, b)

def decode(s):
    d = json.JSONDecoder(); i = 0; out = []
    while i < len(s):
        while i < len(s) and s[i].isspace(): i += 1
        if i == len(s): break
        x, i = d.raw_decode(s, i); out.append(x)
    return out

def patch_color(s):
    assert 'NBGraphFogVertex_float' not in s
    vertex = '''// Frozen NBShaderForwardPass computes this before _VERTEX_OFFSET, after
// ApplyVAT. SG PositionOS feeding NBGraphVertexOffset is the pre-offset source.
void NBGraphFogVertex_float(float3 PositionOS, out float FogFactor)
{
    FogFactor = ComputeFogFactor(TransformObjectToHClip(PositionOS).z);
}
void NBGraphFogVertex_half(half3 PositionOS, out half FogFactor)
{
    float computed;
    NBGraphFogVertex_float((float3)PositionOS, computed);
    FogFactor = (half)computed;
}

// NBShader's half3 fog path. The identity branch keeps all existing Graph
// fog-off materials unchanged, including non-8-bit intermediate values.
void NBGraphApplyFogV1(inout float3 rgb, float interpolatedFogFactor,
    float fogIntensity)
{
    bool active = false;
    #if defined(FOG_LINEAR_KEYWORD_DECLARED)
        if (FOG_LINEAR) active = true;
    #endif
    #if defined(FOG_EXP_KEYWORD_DECLARED)
        if (FOG_EXP) active = true;
    #endif
    #if defined(FOG_EXP2_KEYWORD_DECLARED)
        if (FOG_EXP2) active = true;
    #endif
    if (active && IsFogEnabled())
    {
        half3 beforeFog = (half3)rgb;
        half3 mixed = MixFog(beforeFog, (half)interpolatedFogFactor);
        rgb = (float3)lerp(beforeFog, mixed, (half)fogIntensity);
    }
}
void NBGraphApplyFogV1(inout half3 rgb, float interpolatedFogFactor,
    float fogIntensity)
{
    bool active = false;
    #if defined(FOG_LINEAR_KEYWORD_DECLARED)
        if (FOG_LINEAR) active = true;
    #endif
    #if defined(FOG_EXP_KEYWORD_DECLARED)
        if (FOG_EXP) active = true;
    #endif
    #if defined(FOG_EXP2_KEYWORD_DECLARED)
        if (FOG_EXP2) active = true;
    #endif
    if (active && IsFogEnabled())
    {
        half3 beforeFog = rgb;
        rgb = lerp(beforeFog, MixFog(rgb, (half)interpolatedFogFactor),
            (half)fogIntensity);
    }
}

'''
    s = replace_one(s, 'void NBGraphBaseColor_float(', vertex + 'void NBGraphBaseColor_float(')
    for precision in ('float', 'half'):
        st = s.index('void NBGraphBaseColor_' + precision + '(')
        out = s.index('    out ' + precision + '4 Out,', st)
        s = s[:out] + '''    float FogFactor, float FogIntensity,
''' + s[out:]
    # Pixel sequence: ColorA -> optional DecalAlpha -> Fog -> late ColorAdjustment
    # -> Gamma flag0 bit10 -> AlphaAll. Depth/Shadow skip all three new stages.
    marker = '''    if (!NB_GRAPH_DEPTH_SHADOW_PASS && DecalAlpha != 1.0)
        Out.a = (half)((half)Out.a * (half)DecalAlpha);
'''
    s = replace_one(s, marker, marker + '''    if (!NB_GRAPH_DEPTH_SHADOW_PASS)
        NBGraphApplyFogV1(Out.rgb, FogFactor, FogIntensity);
''', 2)
    for precision in ('float', 'half'):
        st = s.index('void NBGraphBaseColor_' + precision + '(')
        stop = s.index('\n}', st)
        section = s[st:stop]
        alpha = '    Out.a = saturate(Out.a * AlphaAll);' if precision == 'float' else \
            '    Out.a = saturate(Out.a * (half)AlphaAll);'
        gamma_rhs = '''(float3)(half3)LinearToGammaSpace((half3)Out.rgb)''' \
            if precision == 'float' else '''LinearToGammaSpace(Out.rgb)'''
        section = replace_one(section, alpha, '''    if (!NB_GRAPH_DEPTH_SHADOW_PASS &&
        (NBGraphDecodeUInt32(NB_Flags0Lo16, NB_Flags0Hi16) &
            FLAG_BIT_PARTICLE_LINEARTOGAMMA_ON) != 0u)
        Out.rgb = ''' + gamma_rhs + ''';
''' + alpha)
        s = s[:st] + section + s[stop:]
    return s

def patch_graph(s):
    old = decode(s); o = copy.deepcopy(old); root = o[0]
    by = {x['m_ObjectId']: x for x in o if 'm_ObjectId' in x}
    assert not any(x.get('m_OverrideReferenceName') == '_fogintensity' for x in o)
    color = next(x for x in o if x.get('m_FunctionName') == 'NBGraphBaseColor')
    bake = next(x for x in o if x.get('m_FunctionName') == 'NBGraphSixWayBake')
    vertex = next(x for x in o if x.get('m_FunctionName') == 'NBGraphVertexOffset')
    cat = next(x for x in o if x.get('m_Type') == 'UnityEditor.ShaderGraph.CategoryData')
    ids = set(by)
    def add(x):
        assert x['m_ObjectId'] not in ids, x['m_ObjectId']
        ids.add(x['m_ObjectId']); o.append(x); by[x['m_ObjectId']] = x
    def edge(src, sid, dst, did):
        root['m_Edges'].append({'m_OutputSlot': {'m_Node': {'m_Id': src}, 'm_SlotId': sid},
            'm_InputSlot': {'m_Node': {'m_Id': dst}, 'm_SlotId': did}})
    def slot_node(node, name):
        return next(by[q['m_Id']] for q in node['m_Slots']
            if by[q['m_Id']]['m_DisplayName'] == name)
    def clone_slot(template, key, name, number, kind=None, stage=None):
        x = copy.deepcopy(template); x['m_ObjectId'] = uid('slot:' + key)
        x['m_Id'] = number; x['m_DisplayName'] = name
        x['m_ShaderOutputName'] = name
        if kind is not None:
            x['m_Type'] = 'UnityEditor.ShaderGraph.' + kind + 'MaterialSlot'
            default = 0.0 if kind == 'Vector1' else {'x': 0.0, 'y': 0.0, 'z': 0.0}
            x['m_Value'] = x['m_DefaultValue'] = copy.deepcopy(default)
        if stage is not None: x['m_StageCapability'] = stage
        add(x); return x
    # Original pre-VertexOffset OS position. Reuse the exact connected source.
    pos_in = slot_node(vertex, 'PositionOS')
    matches = [e['m_OutputSlot'] for e in root['m_Edges']
        if e['m_InputSlot']['m_Node']['m_Id'] == vertex['m_ObjectId'] and
           e['m_InputSlot']['m_SlotId'] == pos_in['m_Id']]
    assert len(matches) == 1
    original_position = matches[0]
    assert by[original_position['m_Node']['m_Id']].get('m_Type') == 'UnityEditor.ShaderGraph.PositionNode'
    assert by[original_position['m_Node']['m_Id']].get('m_Space') == 0
    fog = copy.deepcopy(bake)
    fog['m_ObjectId'] = uid('vertex-cf'); fog['m_Name'] = 'NBGraphFogVertex (Custom Function)'
    fog['m_FunctionName'] = 'NBGraphFogVertex'
    fog['m_DrawState']['m_Position']['y'] = 27300.0
    b_in = slot_node(bake, 'NormalWS'); b_out = slot_node(bake, 'Bake0')
    fog_in = clone_slot(b_in, 'vertex-input', 'PositionOS', 0, stage=1)
    fog_out = clone_slot(b_out, 'vertex-output', 'FogFactor', 1, kind='Vector1', stage=1)
    fog['m_Slots'] = [{'m_Id': fog_in['m_ObjectId']}, {'m_Id': fog_out['m_ObjectId']}]
    add(fog); root['m_Nodes'].append({'m_Id': fog['m_ObjectId']})
    edge(original_position['m_Node']['m_Id'], original_position['m_SlotId'],
         fog['m_ObjectId'], fog_in['m_Id'])
    template_block = next(x for x in o if x.get('m_SerializedDescriptor') ==
                          'VertexDescription.SixBake0#3')
    block = copy.deepcopy(template_block)
    block['m_ObjectId'] = uid('vertex-block')
    block['m_SerializedDescriptor'] = 'VertexDescription.NBFogFactor#1'
    block_slot = clone_slot(by[template_block['m_Slots'][0]['m_Id']], 'vertex-block-slot',
        'NBFogFactor', 0, kind='Vector1', stage=1)
    block['m_Slots'] = [{'m_Id': block_slot['m_ObjectId']}]
    add(block); root['m_Nodes'].append({'m_Id': block['m_ObjectId']})
    root['m_VertexContext']['m_Blocks'].append({'m_Id': block['m_ObjectId']})
    edge(fog['m_ObjectId'], fog_out['m_Id'], block['m_ObjectId'], block_slot['m_Id'])
    template_ci = next(x for x in o if x.get('customBlockNodeName') == 'SixBake0')
    ci = copy.deepcopy(template_ci)
    ci['m_ObjectId'] = uid('fragment-ci')
    ci['m_Name'] = 'NBFogFactor (Custom Interpolator)'
    ci['customBlockNodeName'] = 'NBFogFactor'; ci['serializedType'] = 1
    ci['m_DrawState']['m_Position']['y'] = 27300.0
    ci_slot = clone_slot(by[template_ci['m_Slots'][0]['m_Id']], 'fragment-ci-slot',
        'Out', 0, kind='Vector1', stage=2)
    ci['m_Slots'] = [{'m_Id': ci_slot['m_ObjectId']}]
    add(ci); root['m_Nodes'].append({'m_Id': ci['m_ObjectId']})
    # Old-name material property; ShaderLab default is exactly 1.
    p_template = next(x for x in old if x.get('m_OverrideReferenceName') == '_AlphaAll')
    n_template = next(x for x in old if x.get('m_Property', {}).get('m_Id') == p_template['m_ObjectId'])
    prop = copy.deepcopy(p_template); prop['m_ObjectId'] = uid('property')
    prop['m_Guid'] = {'m_GuidSerialized': str(uuid.uuid5(NS, 'FG1:property-guid'))}
    prop['m_Name'] = prop['m_RefNameGeneratedByDisplayName'] = 'fogintensity'
    prop['m_DefaultReferenceName'] = prop['m_OverrideReferenceName'] = '_fogintensity'
    prop['m_Value'] = 1.0
    prop_node = copy.deepcopy(n_template); prop_node['m_ObjectId'] = uid('property-node')
    prop_node['m_Property'] = {'m_Id': prop['m_ObjectId']}
    prop_node['m_DrawState']['m_Position']['y'] = 27420.0
    prop_slot = clone_slot(by[n_template['m_Slots'][0]['m_Id']], 'property-node-slot',
        'fogintensity', 0, kind='Vector1')
    prop_slot['m_SlotType'] = 1
    prop_slot['m_Value'] = prop_slot['m_DefaultValue'] = 1.0
    prop_node['m_Slots'] = [{'m_Id': prop_slot['m_ObjectId']}]
    for x in (prop, prop_node): add(x)
    root['m_Properties'].append({'m_Id': prop['m_ObjectId']})
    root['m_Nodes'].append({'m_Id': prop_node['m_ObjectId']})
    cat['m_ChildObjectList'].append({'m_Id': prop['m_ObjectId']})
    max_id = max(by[q['m_Id']]['m_Id'] for q in color['m_Slots'])
    color_template = slot_node(color, 'ParallaxMappingToggle')
    color_factor = clone_slot(color_template, 'color-fog-factor', 'FogFactor', max_id + 1,
                              kind='Vector1')
    color_intensity = clone_slot(color_template, 'color-fog-intensity', 'FogIntensity',
                                 max_id + 2, kind='Vector1')
    color_intensity['m_Value'] = color_intensity['m_DefaultValue'] = 1.0
    color['m_Slots'] += [{'m_Id': color_factor['m_ObjectId']},
                         {'m_Id': color_intensity['m_ObjectId']}]
    edge(ci['m_ObjectId'], ci_slot['m_Id'], color['m_ObjectId'], color_factor['m_Id'])
    edge(prop_node['m_ObjectId'], prop_slot['m_Id'], color['m_ObjectId'], color_intensity['m_Id'])
    modified = {a['m_ObjectId'] for a, b in zip(o[:len(old)], old)
                if a != b and 'm_ObjectId' in a}
    expected = {root['m_ObjectId'], cat['m_ObjectId'], color['m_ObjectId']}
    assert modified == expected, (modified, expected)
    assert o[:len(old)] == [copy.deepcopy(x) if x.get('m_ObjectId') not in modified else o[i]
                            for i, x in enumerate(old)]
    return '\n\n'.join(json.dumps(x, ensure_ascii=False, indent=4) for x in o) + '\n', {
        'oldObjects': len(old), 'newObjects': len(o),
        'oldModifiedIds': sorted(modified),
        'newIds': [x['m_ObjectId'] for x in o[len(old):]],
        'colorInputs': [color_factor['m_Id'], color_intensity['m_Id']],
        'preOffsetPositionNode': original_position['m_Node']['m_Id'],
    }

def main():
    raw = {'graph': GRAPH.read_bytes(), 'color': COLOR.read_bytes()}
    graph, audit = patch_graph(raw['graph'].decode())
    color = patch_color(raw['color'].decode())
    OUT.mkdir(parents=True, exist_ok=True)
    files = {}
    for key, target, contents in [('graph', GRAPH, graph), ('color', COLOR, color)]:
        data = contents.encode(); preview = OUT / target.name; preview.write_bytes(data)
        files[key] = {'target': str(target), 'preview': str(preview),
                      'inputSHA256': hashlib.sha256(raw[key]).hexdigest(),
                      'previewSHA256': hashlib.sha256(data).hexdigest()}
    manifest = {'mode': 'preview-only', 'noUnityOrGit': True,
                'scope': 'ordinary Mesh fog and packed-bit gamma',
                'graphAudit': audit, 'files': files}
    (OUT / 'manifest.json').write_text(json.dumps(manifest, indent=2) + '\n')
    print(json.dumps(manifest, indent=2))

if __name__ == '__main__': main()
