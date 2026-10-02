"""Adapt the existing CustomData delta builder to a fresh Root package.

Requires real F0 and exact input SHA. No dummy VAT, no wholeGraph replacement,
no package writes. VAT frame consumer is deferred when no VAT node exists.
"""
from pathlib import Path
import argparse, ast, hashlib, json

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--current-root-package', required=True)
    ap.add_argument('--expected-source-sha256-json', required=True)
    ap.add_argument('--output', required=True)
    args = ap.parse_args(); root_package = Path(args.current_root_package).resolve(); output = Path(args.output).resolve()
    owner = Path(__file__).resolve().parent; home = owner.parent
    assert owner in output.parents and root_package not in output.parents and not output.exists()
    expected = json.loads(Path(args.expected_source_sha256_json).read_text())
    for rel, digest in expected.items():
        assert hashlib.sha256((root_package / rel).read_bytes()).hexdigest() == digest, 'Source hash drift: ' + rel
    required = ['NBShaders2/ShaderGraph/NBShaderGraph.shadergraph', 'NBShaders2/ShaderGraph/NBGraphBaseUV.hlsl',
                'NBShaders2/ShaderGraph/NBGraphBaseColor.hlsl', 'NBShaders2/ShaderGraph/NBGraphVertexOffset.hlsl',
                'NBShaders2/Runtime/NBShaderFlags.cs', 'NBShaders2/Shader/HLSL/NBShaderSharedContractV2.hlsl', 'NBShaders2/Shader/HLSL/NBShaderUVV2.hlsl']
    assert set(required).issubset(expected), 'All seven consumed sources require exact SHA'
    uv = (root_package / required[1]).read_text()
    assert 'float4 CylinderMatrix3, float FlipbookToggle)' in uv, 'Integrate actual F0 first; do not add fake Flipbook input'
    assert '_NB_CustomDataFlag1Lo16' not in (root_package / required[0]).read_text(), 'CD already present; review instead of duplicate'
    original_builder = home / 'build_customdata_preview.py'
    source = original_builder.read_text()
    # Thin source adapter around the existing delta implementation, not a new
    # resolver/protocol/GUI or a copied frozen candidate Graph.
    source = source.replace("package=work.parents[1]/'.utmp/NBFXMeshValidation-20261002/Packages/NB_FX'", 'package=ROOT_PACKAGE', 1)
    source = source.replace("out=work/'customdata-preview';assert not out.exists()", 'out=OUTPUT;assert not out.exists()', 1)
    source = source.replace("raw={k:(package/p).read_bytes() for k,p in files.items()}",
        "vat_source_present=(package/files['vat']).exists()\nif not vat_source_present: files.pop('vat')\nraw={k:(package/p).read_bytes() for k,p in files.items()}", 1)
    # Existing root has no VAT output. Do not invent VAT fields/nodes merely
    # to satisfy the later GUI reader schema. Storage word2 is still real.
    begin = source.index("vat=next(o for o in objects if o.get('m_FunctionName')=='NBGraphVATSoftBody')")
    end = source.index("allowed={root['m_ObjectId'],*changed_nodes}", begin)
    vat_graph = source[begin:end]
    source = source[:begin] + "vat_node_present=any(o.get('m_FunctionName')=='NBGraphVATSoftBody' for o in objects)\nif vat_node_present:\n" + ''.join('    ' + line if line.strip() else line for line in vat_graph.splitlines(True)) + source[end:]
    begin = source.index("vat_text=append_signature(texts['vat']")
    end = source.index("runtime=texts['runtime']", begin)
    vat_hlsl = source[begin:end]
    source = source[:begin] + 'if vat_source_present and vat_node_present:\n' + ''.join('    ' + line if line.strip() else line for line in vat_hlsl.splitlines(True)) + source[end:]
    # New live SG Float words follow the proven non-hidden Noise/Mask schema.
    # NB custom Inspector owns hiding; hidden+exposed breaks VFX importer slots.
    source = source.replace("prop['m_Hidden']=True", "prop['m_Hidden']=False")
    # Root does not yet have the isolated Noise Tier pair. Inject immediately
    # before the actual Noise block, preserving its same run-time condition.
    old_marker = "marker='    NoiseMaskToggle *= NBGraphTierAllowNoiseMask > 0.5 ? 1.0 : 0.0;'\n    pos=color_text.index(marker,start)+len(marker)"
    new_marker = "marker='    if (NoiseEnabled > 0.5)'\n    pos=color_text.index(marker,start)"
    assert source.count(old_marker) == 1; source = source.replace(old_marker, new_marker)
    # Full CF ABI check before the inherited writer touches any candidate file.
    validation = '''
def parameter_names(text,function):
    start=text.index('void '+function+'(');brace=text.index('{',start);signature=text[start:brace]
    args=signature[signature.index('(')+1:signature.rindex(')')]
    return [re.search(r'([A-Za-z_]\\w*)\\s*$',p.strip()).group(1) for p in args.split(',')]
for function,key in [('NBGraphUVVertex','uv'),('NBGraphBaseUV','uv'),('NBGraphVertexOffset','vo'),('NBGraphBaseColor','color')]:
    node=next(o for o in objects if o.get('m_FunctionName')==function)
    slots=[by[r['m_Id']] for r in node['m_Slots']]
    call=[o['m_DisplayName'] for o in slots if o['m_SlotType']==0]+[o['m_DisplayName'] for o in slots if o['m_SlotType']==1]
    assert call==parameter_names(texts[key],function+'_float'),('CF ABI order mismatch',function)
    if 'void '+function+'_half(' in texts[key]: assert call==parameter_names(texts[key],function+'_half')
'''
    anchor = 'records=[]\nfor key,rel in files.items():'
    assert source.count(anchor) == 1; source = source.replace(anchor, validation + '\n' + anchor)
    ast.parse(source)
    namespace = {'__name__': 'root_customdata_delta', '__file__': str(original_builder), 'ROOT_PACKAGE': root_package, 'OUTPUT': output}
    exec(compile(source, str(original_builder), 'exec'), namespace)
    manifest_path = output / 'manifest.json'; manifest = json.loads(manifest_path.read_text())
    manifest['sourceContext'] = {'inputPackage': str(root_package), 'exactInputSHA256': expected,
        'F0Required': True, 'VATFrameConsumer': 'present real VAT node' if namespace.get('vat_node_present') else 'deferred: no real VAT node; no fake fields added',
        'CFArgumentOrderChecked': True, 'noCentralWrite': True, 'originalBuilderSHA256': hashlib.sha256(original_builder.read_bytes()).hexdigest()}
    manifest['identitySource'] = 'source, unrun'; manifest['files'] = manifest['records']
    manifest_path.write_text(json.dumps(manifest, indent=2) + '\n')
    print(json.dumps(manifest['sourceContext'], indent=2))

if __name__ == '__main__': main()
