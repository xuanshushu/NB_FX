"""Strictly guarded reader+Mask rebase from the package at invocation.

No schema filler, staged bypass, legacy whole-file/whole-Graph replacement,
installation, Unity or Git operation. Current root is expected to be refused
until real F0, CustomData and VAT consumer slices provide the required schema.
"""
from pathlib import Path
import argparse, ast, hashlib, json, re

R = 'NBShaders2/Runtime/NBShaderMaterialIntentResolver.cs'
A = 'NBShaders2/Editor/FeatureLevel/NBShaderFeatureLevelMaterialApplier.cs'
G = 'NBShaders2/ShaderGraph/NBShaderGraph.shadergraph'
H = 'NBShaders2/ShaderGraph/NBGraphBaseColor.hlsl'
I = 'NBShaders2/Editor/NBShaderGraphRootItem.cs'

def span(source, signature):
    start = source.index(signature); brace = source.index('{', start); depth = 0
    for end in range(brace, len(source)):
        depth += (source[end] == '{') - (source[end] == '}')
        if depth == 0: return start, end + 1
    raise AssertionError('Unclosed method ' + signature)

def method(source, signature):
    start, end = span(source, signature); return source[start:end]

def objects(text):
    decoder = json.JSONDecoder(); result = []; cursor = 0
    while cursor < len(text):
        while cursor < len(text) and text[cursor].isspace(): cursor += 1
        if cursor == len(text): break
        value, cursor = decoder.raw_decode(text, cursor); result.append(value)
    return result

def schema_audit(graph, canonical_reader):
    props = {o.get('m_OverrideReferenceName') or o.get('m_DefaultReferenceName'): o
             for o in graph if 'ShaderProperty' in o.get('m_Type', '')}
    bindings = re.findall(r'new KeywordToggleBinding\("([^"]+)", "([^"]+)"(?:, (false))?\)', canonical_reader)
    assert len(bindings) == 37
    needed = {prop for prop, _, unsupported in bindings if not unsupported}
    prefix_block = re.search(r'GraphPackedWordPrefixes =\s*\{(.*?)\};', canonical_reader, re.S).group(1)
    for prefix in re.findall(r'"([^"]+)"', prefix_block):
        needed.update([prefix + 'Lo16', prefix + 'Hi16'])
    needed.update(['_NB_GraphGUIStateVersion', '_NB_DistortionMode', '_VAT_Toggle',
                   '_FxLightMode', '_DistortMode', '_RampColorSourceMode', '_DissolveRampSourceMode',
                   '_VATMode', '_HoudiniVATSubMode', '_TyFlowVATSubMode'])
    missing = sorted(needed - props.keys())
    wrong = sorted(name for name in needed & props.keys()
                   if not props[name]['m_Type'].endswith('.Vector1ShaderProperty') or props[name].get('m_FloatType', 0) in (1, 2))
    targets = [o for o in graph if o.get('m_Type', '').endswith('.UniversalTarget')]
    native = len(targets) == 1 and targets[0].get('m_AllowMaterialOverride') is True
    return {'missingRequiredRealFields': missing, 'wrongScalarShaderPropertyKinds': wrong,
            'nativeURPOverrideSourceAvailable': native,
            'nativeSurfaceClipBlendActualFloatType': 'Must still be verified in Unity; Target material override only checked here',
            'unavailableNineKeywords': [keyword for _, keyword, unsupported in bindings if unsupported],
            'completeSourceSchema': not missing and not wrong and native,
            'shaderConsumerVerification': 'Source fields are not proof of renderer behavior; slice provenance and real tests remain required'}

def append_reader(source, canonical):
    assert 'TryResolveGraphSupportedKeywordIntent' not in source and 'graphSupported' not in source
    root_bindings = re.findall(r'new KeywordToggleBinding\("([^"]+)", "([^"]+)"\)', source)
    canonical_bindings = re.findall(r'new KeywordToggleBinding\("([^"]+)", "([^"]+)"(?:, (false))?\)', canonical)
    assert root_bindings == [(p, k) for p, k, _ in canonical_bindings], '37 shared binding contract drifted'
    for prop, keyword, unsupported in canonical_bindings:
        if unsupported:
            old = 'new KeywordToggleBinding("' + prop + '", "' + keyword + '")'
            assert source.count(old) == 1; source = source.replace(old, old[:-1] + ', false)')
    ctor = '            public KeywordToggleBinding(string propertyName, string keyword)'
    assert source.count(ctor) == 1
    source = source.replace(ctor, ctor[:-1] + ', bool graphSupported = true)')
    start = source.index('        private struct KeywordToggleBinding')
    segment = source[start:]
    segment = segment.replace('            public readonly string keyword;', '            public readonly string keyword;\n            public readonly bool graphSupported;', 1)
    segment = segment.replace('                this.keyword = keyword;', '                this.keyword = keyword;\n                this.graphSupported = graphSupported;', 1)
    source = source[:start] + segment

    # Route the existing legacy modes and Graph modes through the same helper;
    # preserve legacy identity, alpha/blend, time, screen and pass contracts.
    legacy_start, legacy_end = span(source, '        private static HashSet<string> ResolveIntendedManagedKeywords(')
    legacy = source[legacy_start:legacy_end]
    mode_start = legacy.index('            switch (GetInt(material, "_FxLightMode"')
    screen_start = legacy.index('            if (!IsUIEffectMeshSource(meshMode)', mode_start)
    common = method(canonical, '        private static void ResolveCommonModeKeywords(')
    common_body = common[common.index('{') + 1:common.rindex('}')]
    root_common_body = legacy[mode_start:screen_start] + '\n            ResolveVatKeywords(material, keywords);\n'
    assert ''.join(root_common_body.split()) == ''.join(common_body.split()), 'Existing common enum semantics drifted'
    legacy = legacy[:mode_start] + '            ResolveCommonModeKeywords(material, keywords);\n\n' + legacy[screen_start:]
    assert legacy.count('            ResolveVatKeywords(material, keywords);') == 1
    legacy = legacy.replace('            ResolveVatKeywords(material, keywords);', '', 1)
    source = source[:legacy_start] + legacy + source[legacy_end:]
    begin = canonical.index('        // Read-only Mesh capability v1:')
    end = canonical.index('        private static HashSet<string> ResolveIntendedToggleKeywords(', begin)
    capability = canonical[begin:end]
    toggle = method(canonical, '        private static HashSet<string> ResolveIntendedToggleKeywords(')
    pair = ''
    if 'TryResolveGraphNoisePair' not in source:
        pair = method(canonical, '        public static bool TryResolveGraphNoisePair(') + '\n\n'
    anchor = '        private static HashSet<string> ResolveIntendedManagedKeywords('
    assert source.count(anchor) == 1
    return source.replace(anchor, pair + capability + '\n' + toggle + '\n\n' + common + '\n\n' + anchor)

def append_applier(source, canonical):
    assert 'ApplyGraphMaskGroup' not in source
    group = method(canonical, '        public static bool ApplyGraphMaskGroup(')
    setter = method(canonical, '        static bool SetGraphAllowFloat(')
    anchor = '        private const string FeatureTierPropertyName = "_NBShaderFeatureTier";'
    assert source.count(anchor) == 1
    addition = '\n\n' + group + '\n\n'
    if 'static bool SetGraphAllowFloat(' not in source: addition += setter + '\n\n'
    return source.replace(anchor, anchor + addition)

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--source-package', required=True); ap.add_argument('--output', required=True)
    args = ap.parse_args(); package = Path(args.source_package).resolve(); out = Path(args.output).resolve(); owner = Path(__file__).resolve().parent
    assert owner in out.parents and package not in out.parents and not out.exists()
    home = owner.parent
    canonical_reader_path = home / 'gui-normalized-reader-preview' / R
    canonical_applier_path = home / 'gui-tier-masks-preview' / A
    canonical_reader = canonical_reader_path.read_text(); canonical_applier = canonical_applier_path.read_text()
    assert hashlib.sha256(canonical_reader_path.read_bytes()).hexdigest() == 'df566cb684ef40b293cf2ab8a8ed369f8218bab8279e3fda59c1452f6de036ce'
    assert hashlib.sha256(canonical_applier_path.read_bytes()).hexdigest() == 'e522fe36d2e554b3d7c5c0ad93cf83f8b5bf1ab67296173d127960237e3f08af'
    original = {rel: (package / rel).read_bytes() for rel in [R, A, G, H, I]}
    audit = schema_audit(objects(original[G].decode('utf-8-sig')), canonical_reader)
    assert audit['completeSourceSchema'], 'Actual renderer schema incomplete; do not generate reader/gates or fake properties: ' + json.dumps(audit)
    patched_reader = append_reader(original[R].decode('utf-8-sig'), canonical_reader)
    patched_applier = append_applier(original[A].decode('utf-8-sig'), canonical_applier)
    inspector = original[I].decode('utf-8-sig')
    if '!property.name.StartsWith("_NB_TierAllow", StringComparison.Ordinal)' not in inspector:
        anchor = '            => property != null && (property.propertyFlags &'
        assert inspector.count(anchor) == 1
        inspector = inspector.replace(anchor, '            => property != null && !property.name.StartsWith("_NB_TierAllow", StringComparison.Ordinal) && (property.propertyFlags &')
    for rel, value in original.items(): assert (package / rel).read_bytes() == value, 'Source moved during rebase'
    out.mkdir(parents=True)
    # Execute the vetted dynamic Mask generator against the fresh source. Its
    # own generated file is a candidate, never a package/central write.
    mask_path = home / 'gui-tier-masks-preview/build_graph_mask_gates.py'
    module = ast.parse(mask_path.read_text())
    safe = ast.Module(body=[n for n in module.body if not isinstance(n, ast.If)], type_ignores=[])
    # Reuse the generator with this task's owned output guard, rather than
    # creating secondary artifacts beside a different candidate task.
    namespace = {'__name__': 'root_gui_mask_rebase', '__file__': str(owner / 'mask_delegate.py')}
    exec(compile(safe, str(mask_path), 'exec'), namespace)
    import sys
    prior = sys.argv
    try:
        sys.argv = [str(mask_path), '--source-package', str(package), '--output', str(out / 'mask-generated')]
        namespace['main']()
    finally: sys.argv = prior
    generated = out / 'mask-generated'
    graph_objects = objects((generated / G).read_text())
    # Existing Noise-gate parity and historical VFX import compatibility:
    # all derived Mask inputs are real exposed Float, hidden by existing GUI
    # prefix filter rather than broken exposed+hidden VFX metadata.
    for obj in graph_objects:
        name = obj.get('m_OverrideReferenceName') or obj.get('m_DefaultReferenceName')
        if name in ['_NB_TierAllowMask', '_NB_TierAllowMask2', '_NB_TierAllowMask3']: obj['m_Hidden'] = False
    result = {R: patched_reader, A: patched_applier, I: inspector,
              G: '\n\n'.join(json.dumps(o, indent=4, ensure_ascii=False) for o in graph_objects) + '\n', H: (generated / H).read_text()}
    records = []
    for rel, value in original.items(): assert (package / rel).read_bytes() == value, 'Source moved before final candidate output'
    for rel, text in result.items():
        path = out / rel; path.parent.mkdir(parents=True, exist_ok=True); path.write_bytes(text.encode('utf-8'))
        records.append({'path': rel, 'beforeSHA256': hashlib.sha256(original[rel]).hexdigest(), 'afterSHA256': hashlib.sha256(path.read_bytes()).hexdigest()})
    manifest = {'scope': 'Strict complete-schema reader+Mask delta rebase; preview only', 'records': records, 'files': records,
                'schemaAudit': audit, 'inputPackage': str(package), 'wholeGraphReplacement': False,
                'unavailableNineExplicit': True, 'generalApplyToolbarValidateProtected': True, 'formalVFX': False,
                'SourceConsumerProvenanceStillNeedsMainReview': True}
    (out / 'manifest.json').write_text(json.dumps(manifest, indent=2) + '\n')
    print(json.dumps(manifest, indent=2))

if __name__ == '__main__': main()
