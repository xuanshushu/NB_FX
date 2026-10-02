"""Patch the SubTarget bytes read at invocation; never write package source.

Only Forward and the two existing NB color passes gain Offset. Depth, shadow,
DepthNormals and internal URP passes retain their original descriptors.
"""
from pathlib import Path
import argparse, hashlib, json

RELATIVE = 'NBShaders2/ShaderGraph/Editor/NBGraphUnlitSubTarget.cs'

def method_span(source, signature):
    start = source.index(signature)
    brace = source.index('{', start)
    depth = 0
    for end in range(brace, len(source)):
        depth += (source[end] == '{') - (source[end] == '}')
        if depth == 0:
            return start, end + 1
    raise AssertionError('Unclosed method: ' + signature)

def patch(source):
    assert '_offsetFactor' not in source and 'WithNBZOffset(' not in source, 'Offset already present; review instead of duplicating'
    start, end = method_span(source, '        static RenderStateCollection WithNBRenderStates(')
    block = source[start:end]
    anchor = '                    states.Add(item.descriptor, item.fieldConditions);'
    assert block.count(anchor) == 1
    replacement = ('                    states.Add(colorMask && item.descriptor.type == RenderStateType.ZTest\n'
                   '                        ? WithNBZOffset(item.descriptor) : item.descriptor, item.fieldConditions);')
    source = source[:start] + block.replace(anchor, replacement) + source[end:]
    # This exact ZTest exists only in the two NB distortion color-pass clones.
    start, end = method_span(source, '        static void AddDistortionPass(')
    block = source[start:end]
    anchor = '                RenderState.ZTest("[_ZTest]"),'
    assert block.count(anchor) == 1
    source = source[:start] + block.replace(anchor, '                WithNBZOffset(RenderState.ZTest("[_ZTest]")),') + source[end:]
    anchor = '        static RenderStateDescriptor NBStencilState()'
    assert source.count(anchor) == 1
    helper = '''        // SG 17.3 has no Offset descriptor type. Preserve the real ZTest
        // descriptor and its conditions; append one ShaderLab Offset command.
        // Only existing color-pass call sites reach this helper.
        static RenderStateDescriptor WithNBZOffset(RenderStateDescriptor descriptor)
        {
            if (descriptor.type != RenderStateType.ZTest ||
                !descriptor.value.StartsWith("ZTest ", StringComparison.Ordinal) ||
                descriptor.value.IndexOf("Offset", StringComparison.OrdinalIgnoreCase) >= 0)
                throw new InvalidOperationException("NB FX Graph: expected one original ZTest state for color Offset.");
            descriptor.value += "\\nOffset [_offsetFactor], [_offsetUnits]";
            return descriptor;
        }

'''
    source = source.replace(anchor, helper + anchor)
    anchor = '            collector.AddFloatProperty("_ColorMask", 15.0f);'
    assert source.count(anchor) == 1
    source = source.replace(anchor, '            collector.AddFloatProperty("_offsetFactor", 0.0f);\n'
                                   '            collector.AddFloatProperty("_offsetUnits", 0.0f);\n' + anchor)
    return source

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--source-package', required=True)
    ap.add_argument('--output', required=True)
    args = ap.parse_args()
    package = Path(args.source_package).resolve()
    output = Path(args.output).resolve()
    owner = Path(__file__).resolve().parent
    assert owner in output.parents and package not in output.parents and not output.exists()
    before = (package / RELATIVE).read_bytes()
    after = patch(before.decode('utf-8-sig')).encode('utf-8')
    assert (package / RELATIVE).read_bytes() == before, 'Source moved during generation'
    path = output / RELATIVE; path.parent.mkdir(parents=True); path.write_bytes(after)
    record = {'path': RELATIVE, 'beforeSHA256': hashlib.sha256(before).hexdigest(),
              'afterSHA256': hashlib.sha256(after).hexdigest()}
    manifest = {'scope': 'ZOffset two-property render-state preview only; no install/Unity verification',
                'inputPackage': str(package), 'records': [record], 'files': [record],
                'properties': {'_offsetFactor': {'type': 'Float', 'default': 0}, '_offsetUnits': {'type': 'Float', 'default': 0}},
                'includedPasses': ['Universal Forward', 'NBCameraOpaqueDistortPass', 'NBDeferredDistortPass'],
                'excludedScope': ['Graph BackFirst/Universal2D are absent; no parity claim', 'DepthOnly', 'ShadowCaster', 'DepthNormalsOnly', 'internal URP passes'],
                'originalConditionsPreserved': True, 'newKeywords': 0, 'newPasses': 0, 'GraphAssetChanged': False}
    (output / 'manifest.json').write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(manifest, indent=2))

if __name__ == '__main__': main()
