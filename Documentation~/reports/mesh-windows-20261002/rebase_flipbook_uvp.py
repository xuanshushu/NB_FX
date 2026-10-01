"""Rebase the archived F0 preview onto current UVP+GUI1B isolated source.

Preview only. Preserve the source/stage wrapper and append real UV3/Flipbook
inputs to its vertex consumers as well as the original fragment interfaces.
"""
from pathlib import Path
import copy
import hashlib
import json
import runpy
import uuid

work = Path(__file__).resolve().parent
package = work.parents[1] / '.utmp/NBFXMeshValidation-20261002/Packages/NB_FX'
module = runpy.run_path(str(work / 'flipbook/rebound-preview.py'), run_name='nbfx_f0_preview')
g = module['main'].__globals__
old_package = g['P']
g['P'] = package
g['F'] = {key: package / value.relative_to(old_package) for key, value in g['F'].items()}
g['F']['vertex'] = package / 'NBShaders2/ShaderGraph/NBGraphVertexOffset.hlsl'
g['OUT'] = work / 'flipbook/uvp-gui1b-rebased-preview'
one = g['one']

def patch_baseuv(text):
    for precision in ('float', 'half'):
        start = text.index('void NBGraphBaseUV_' + precision + '(')
        output = text.index('    out ', start)
        text = text[:output] + f'''    {precision}4 UV3, {precision} FlipbookToggle,
    {precision}4 AnimationSheetBlendST, {precision} AnimationSheetBlendIntensity,
''' + text[output:]
        text = one(text, f'    out {precision} DecalAlpha)',
                   f'    out {precision} DecalAlpha, out {precision}2 BlendUV, out {precision} BlendWeight)')
    # UVP's one parameter builder remains the host binding authority.
    text = one(text, '    float4 CylinderMatrix3)', '    float4 CylinderMatrix3, float FlipbookToggle)')
    text = one(text, '         FLAG_BIT_PARTICLE_1_USE_TEXCOORD2 | FLAG_BIT_PARTICLE_1_CYLINDER_CORDINATE);',
               '         FLAG_BIT_PARTICLE_1_USE_TEXCOORD2 | FLAG_BIT_PARTICLE_1_CYLINDER_CORDINATE | FLAG_BIT_PARTICLE_1_IS_PARTICLE_SYSTEM);')
    text = one(text, '    parameters.timeY = _Time.y;',
               '    parameters.flipbookBlending = FlipbookToggle > 0.5 ? 1u : 0u;\n    parameters.timeY = _Time.y;')
    text = one(text, 'CylinderMatrix0, CylinderMatrix1, CylinderMatrix2, CylinderMatrix3);',
               'CylinderMatrix0, CylinderMatrix1, CylinderMatrix2, CylinderMatrix3, FlipbookToggle);', 2)
    vertex_start = text.index('void NBGraphUVVertex_float(')
    vertex_out = text.index('    out ', vertex_start)
    text = text[:vertex_out] + '    float4 UV3, float FlipbookToggle,\n' + text[vertex_out:]
    # Both UVP vertex and main fragment bind the actual TEXCOORD3.yz stream.
    text = one(text, '    input.custom2 = UV2;',
               '    input.custom2 = UV2;\n    input.specialUVInTexcoord3 = UV3.yz;', 2)
    start = text.index('void NBGraphBaseUV_float(')
    end = text.index('void NBGraphBaseUV_half(', start)
    section = text[start:end]
    marker = '    BaseUVs resolved = NBFX_BuildBaseUVsV1(input, parameters);'
    section = one(section, marker, marker + '''
    uint animationFlags1 = NBGraphDecodeUInt32(NB_Flags1Lo16, NB_Flags1Hi16);
    bool animationHelper = (animationFlags1 & FLAG_BIT_PARTICLE_1_ANIMATION_SHEET_HELPER) != 0u;
    BlendUV = NBFX_ResolveFlipbookUVV1(input.meshTexcoord0, AnimationSheetBlendST, animationHelper);
    BlendWeight = NBFX_ResolveFlipbookWeightV1(UV3.x, (half)AnimationSheetBlendIntensity, animationHelper);''')
    text = text[:start] + section + text[end:]
    text = one(text, '    float decalAlphaResolved;',
               '    float decalAlphaResolved, blendWeightResolved;\n    float2 blendUVResolved;')
    text = one(text, 'CylinderMatrix2, CylinderMatrix3, ParallaxToggle,',
               'CylinderMatrix2, CylinderMatrix3, ParallaxToggle,\n        (float4)UV3, (float)FlipbookToggle, (float4)AnimationSheetBlendST, (float)AnimationSheetBlendIntensity,')
    text = one(text, 'programNoiseResolved, decalAlphaResolved);',
               'programNoiseResolved, decalAlphaResolved, blendUVResolved, blendWeightResolved);')
    text = one(text, '    DecalAlpha = (half)decalAlphaResolved;',
               '    DecalAlpha = (half)decalAlphaResolved;\n    BlendUV = (half2)blendUVResolved;\n    BlendWeight = (half)blendWeightResolved;')
    return text

def patch_vertex(text):
    output = text.index('    out ', text.index('void NBGraphVertexOffset_float('))
    text = text[:output] + '    float4 UV3, float FlipbookToggle,\n' + text[output:]
    text = one(text, '    uvInput.custom2 = UV2;',
               '    uvInput.custom2 = UV2;\n    uvInput.specialUVInTexcoord3 = UV3.yz;')
    text = one(text, '            FLAG_BIT_PARTICLE_1_USE_TEXCOORD2 | FLAG_BIT_PARTICLE_1_CYLINDER_CORDINATE);',
               '            FLAG_BIT_PARTICLE_1_USE_TEXCOORD2 | FLAG_BIT_PARTICLE_1_CYLINDER_CORDINATE | FLAG_BIT_PARTICLE_1_IS_PARTICLE_SYSTEM);')
    return one(text, '    uvParams.timeY = _Time.y;',
               '    uvParams.flipbookBlending = FlipbookToggle > 0.5 ? 1u : 0u;\n    uvParams.timeY = _Time.y;')

original_graph_patch = g['patch_graph']
namespace = uuid.UUID('ca36b26e-3429-4b57-a414-08182afc5d11')
def patch_graph(text):
    first, audit = original_graph_patch(text)
    objects = g['decode'](first)
    root = objects[0]
    by = {o['m_ObjectId']: o for o in objects}
    uv = next(o for o in objects if o.get('m_FunctionName') == 'NBGraphBaseUV')
    def slot(node, name):
        return next(by[r['m_Id']] for r in node['m_Slots'] if by[r['m_Id']]['m_DisplayName'] == name)
    def incoming(node, name):
        sid = slot(node, name)['m_Id']
        edges = [e for e in root['m_Edges'] if e['m_InputSlot']['m_Node']['m_Id'] == node['m_ObjectId'] and e['m_InputSlot']['m_SlotId'] == sid]
        assert len(edges) == 1
        return copy.deepcopy(edges[0]['m_OutputSlot'])
    sources = {name: incoming(uv, name) for name in ('UV3', 'FlipbookToggle')}
    for name in ('NBGraphUVVertex', 'NBGraphVertexOffset'):
        node = next(o for o in objects if o.get('m_FunctionName') == name)
        max_id = max(by[r['m_Id']]['m_Id'] for r in node['m_Slots'])
        for i, port_name in enumerate(('UV3', 'FlipbookToggle'), 1):
            template = slot(node, 'UV2' if name == 'NBGraphUVVertex' and port_name == 'UV3' else
                            'UV2' if port_name == 'UV3' else
                            'TWStrength' if name == 'NBGraphUVVertex' else 'VertexOffsetToggle')
            port = copy.deepcopy(template)
            port['m_ObjectId'] = uuid.uuid5(namespace, name + ':' + port_name).hex
            assert port['m_ObjectId'] not in by
            port['m_Id'] = max_id + i
            port['m_DisplayName'] = port['m_ShaderOutputName'] = port_name
            port['m_StageCapability'] = 1
            node['m_Slots'].append({'m_Id': port['m_ObjectId']})
            by[port['m_ObjectId']] = port
            objects.append(port)
            root['m_Edges'].append({'m_OutputSlot': sources[port_name],
                                  'm_InputSlot': {'m_Node': {'m_Id': node['m_ObjectId']}, 'm_SlotId': port['m_Id']}})
    audit['rebasedOnUVP'] = True
    audit['vertexConsumersReceiveActualUV3AndFlipbook'] = True
    audit['objectsAfterUVPVertexBinding'] = len(objects)
    return '\n\n'.join(json.dumps(o, indent=4, ensure_ascii=False) for o in objects) + '\n', audit

g['patch_baseuv'] = patch_baseuv
g['patch_graph'] = patch_graph
raw = {key: path.read_bytes() for key, path in g['F'].items()}
graph, audit = patch_graph(raw['graph'].decode('utf-8'))
outputs = {'graph': graph, 'baseuv': patch_baseuv(raw['baseuv'].decode('utf-8')),
           'vertex': patch_vertex(raw['vertex'].decode('utf-8')),
           'color': g['patch_color'](raw['color'].decode('utf-8')),
           'shared': g['patch_shared'](raw['shared'].decode('utf-8')),
           'contract': g['patch_contract'](raw['contract'].decode('utf-8')),
           'legacy': g['patch_legacy'](raw['legacy'].decode('utf-8')),
           'forward': g['patch_forward'](raw['forward'].decode('utf-8')),
           'helper': g['patch_helper'](raw['helper'].decode('utf-8'))}
out = g['OUT']
assert not out.exists(), 'Never overwrite earlier preview'
out.mkdir(parents=True)
files = {}
for key, text in outputs.items():
    data = text.encode('utf-8')
    dest = out / g['F'][key].name
    dest.write_bytes(data)
    assert g['F'][key].read_bytes() == raw[key], 'Source changed during preview'
    files[key] = {'target': str(g['F'][key]), 'preview': str(dest),
                  'inputSHA256': hashlib.sha256(raw[key]).hexdigest(),
                  'previewSHA256': hashlib.sha256(data).hexdigest()}
(out / 'manifest.json').write_text(json.dumps({'scope':'F0 rebased UVP+GUI1B preview only',
    'graphAudit':audit, 'files':files, 'newKeywords':0,'applied':False,'ranUnity':False}, indent=2) + '\n',encoding='utf-8',newline='\n')
print(json.dumps({'files':len(files),'graphAudit':audit,'applied':False,'ranUnity':False}))
