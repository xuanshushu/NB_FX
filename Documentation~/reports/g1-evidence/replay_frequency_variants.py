#!/usr/bin/env python3
"""Read-only source inventory: parameter frequency and ShaderLab pragma groups."""
from __future__ import annotations
import hashlib
import json
import pathlib
import re
from collections import Counter
ROOT=next(p for p in pathlib.Path(__file__).resolve().parents if (p / 'Packages/NB_FX/package.json').is_file())
OUT=pathlib.Path(__file__).resolve().parent
shader=ROOT/'Packages/NB_FX/NBShaders2/Shader/NBShader.shader'
source=shader.read_text()
catalog_file=ROOT/'Packages/NB_FX/NBShaders2/Runtime/NBShaderFeatureCatalog.cs'
catalog_body=catalog_file.read_text().split('RawKeywords =',1)[1].split('};',1)[0]
managed_keywords=set(re.findall(r'"([_A-Z0-9]+)"',catalog_body))
lines=source.splitlines()
starts=[i for i,line in enumerate(lines) if re.match(r'^\s*Pass\s*$',line)]
passes=[]
for j,start in enumerate(starts):
    stop=starts[j+1] if j+1<len(starts) else len(lines)
    chunk=lines[start:stop]
    name=next((m.group(1) for line in chunk if (m:=re.match(r'^\s*Name\s+"([^"]+)"',line))),None)
    lightmode=next((m.group(1) for line in chunk if (m:=re.search(r'"LightMode"\s*=\s*"([^"]+)"',line))),None)
    groups=[]; other=[]
    for offset,line in enumerate(chunk):
        m=re.match(r'^\s*#pragma\s+(shader_feature\w*|multi_compile\w*)\s*(.*)$',line)
        if not m:continue
        directive=m.group(1)
        tokens=m.group(2).split('//',1)[0].split()
        record={'line':start+offset+1,'directive':directive,'tokens':tokens,'namedKeywords':[t for t in tokens if t!='_']}
        if directive=='multi_compile_fog':other.append(record)
        else:groups.append(record)
    categories=Counter('shader_feature' if g['directive'].startswith('shader_feature') else 'multi_compile' for g in groups)
    keywords=sorted({k for g in groups for k in g['namedKeywords']})
    passes.append({'name':name,'lightMode':lightmode,'line':start+1,'groups':groups,'specialPragmas':other,
      'groupCount':len(groups),'shaderFeatureGroups':categories['shader_feature'],
      'multiCompileGroups':categories['multi_compile'],'uniqueNamedKeywordCount':len(keywords),
      'uniqueNamedKeywords':keywords,'managedNamedKeywordCount':len(set(keywords)&managed_keywords),
      'catalogExternalNamedKeywords':sorted(set(keywords)-managed_keywords),
      'hasIncludeWithPragmas':any('#include_with_pragmas' in line and not line.lstrip().startswith('//') for line in chunk)})

freq=[
 {'source':'NBShaderInput.hlsl:222-251','class':'per-material uniform','fields':'flags0/1, wrap, ForceNoMip, CustomData selectors0..3, UV low/type, Channel, PNoise, feature scalars/textures','proved':'Declared inside UnityPerMaterial CBUFFER; Material/GUI setters write packed integers. Not particle-varying by declaration.'},
 {'source':'ShaderFlagsBase.cs:43-70','class':'per-renderer MPB override candidate','fields':'same packed material properties through MaterialPropertyBlock SetInt/SetInteger','proved':'Runtime writer API path exists. Actual MPB inheritance, VFX applicability, batching and GPU behavior not tested.'},
 {'source':'NBShaderInput.hlsl:1393-1394,1436-1437; NBShaderForwardPass.hlsl:117-122; NBShaderFlags.hlsl:178-225','class':'per-vertex/custom stream, interpolated fragment; potentially per-particle provider','fields':'Custom1/Custom2 input TEXCOORD1/2 -> varyings TEXCOORD8/9 -> GetCustomData source','proved':'Vertex-to-fragment path shown. Per-particle VFX binding is not proved by this source.'},
 {'source':'NBShader.shader:616-690,751-825,880-901,938-961,998-1072,1122-1197,1245-1274','class':'compile-time variant','fields':'shader_feature(_local/_vertex/_fragment), multi_compile, fog','proved':'Seven ShaderLab passes declare keyword groups; exact compiled/imported/stripped variants unmeasured.'},
 {'source':'NBShader.shader:589-1226; NBShaderPassFeatureCatalog.cs:11-27; NBGFUnlitSubTarget.cs:43-61','class':'pass identity/render pipeline selection','fields':'LightMode values incl NBCameraOpaqueDistortPass and NBDeferredDistortPass','proved':'Production NBShader pass labels and GF-created Graph pass labels are source-visible; Graph Player pass presence was proved by GF, full product feature path not.'},
 {'source':'NBGFDistortPass.hlsl:10,28-42; NBGFPlayerProbe.cs:82,114-160','class':'GF-only global uniform','fields':'_NBGFMode','proved':'Global mode differentiates controlled probe passes. Not an NBShader product parameter contract.'},
 {'source':'GF_URP_VFX_NBSubTarget.shadergraph:5-12,349-358,978-987','class':'Graph-exposed material properties in GF only','fields':'BaseMap and Color','proved':'Two Graph properties and zero declared Graph keywords; no NB packed properties/CustomData bridge in GF asset.'},
]
strip={'source':'NBShaderVariantStripper.cs:22-45,65-111','predicate':'shader.name == NBShaderFeatureLevelCatalog.ShaderName','targetName':'Effects/NBShader','implication':'Existing tier/pass variant stripper is restricted to production NBShader. It cannot be assumed to strip a differently named SG/VFX generated shader.','catalog':'NBShaderFeatureCatalog.cs:14,23-93; NBShaderPassFeatureCatalog.cs:11-27'}
result={'scope':'static ShaderLab/source inventory, not compiled variant count','shaderFile':str(shader.relative_to(ROOT)),'shaderSha256':hashlib.sha256(shader.read_bytes()).hexdigest(),
 'passCount':len(passes),'passes':passes,'frequencyMap':freq,'existingStripper':strip,
 'managedKeywordCatalog':{'path':str(catalog_file.relative_to(ROOT)),'count':len(managed_keywords),'keywords':sorted(managed_keywords)},
 'limitations':['Keyword groups are counted lexically; conditional pragma branches, Unity generated SG/VFX pragmas, platform exclusions, mutual exclusivity, SRP variants and Unity stripping are not evaluated.','No shader variant compilation, Player build, GPU readback or VFX parameter-frequency test was run.','D21 current Unity 6000.3/URP17.3 only; older-version compatibility is deferred.']}
path=OUT/'frequency_variant_inventory.json';path.write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n')
print(json.dumps({'output':str(path.relative_to(ROOT)),'passCount':len(passes),'perPass':[{'name':p['name'],'groups':p['groupCount'],'shaderFeature':p['shaderFeatureGroups'],'multiCompile':p['multiCompileGroups'],'uniqueNamedKeywords':p['uniqueNamedKeywordCount'],'catalogExternal':p['catalogExternalNamedKeywords']} for p in passes]},ensure_ascii=False))
