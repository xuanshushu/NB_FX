#!/usr/bin/env python3
"""Review-only dynamic CA1 static pass clamp preview; writes /tmp only.
Reads latest live package and imports original CA1 pure patch functions,
retaining all current RF/DD/POM properties/ports. Does not write original script.
"""
from pathlib import Path
import importlib.util,hashlib,json,difflib,argparse
P=Path('/Users/bytedance/UnityProject/NBUnityProject/Packages/NB_FX').resolve()
ap=argparse.ArgumentParser();ap.add_argument('--output',default='/tmp/nbfx-ca1-static-pass-preview');a=ap.parse_args();O=Path(a.output).resolve()
assert Path('/tmp').resolve() in O.parents and P not in O.parents, 'Output must remain a /tmp child'
script=Path('/tmp/nbfx-apply-chromatic.py');scriptbytes=script.read_bytes()
spec=importlib.util.spec_from_file_location('ca1_original_readonly',str(script));m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
source={k:p.read_bytes() for k,p in m.F.items()}
subrel='NBShaders2/ShaderGraph/Editor/NBGraphUnlitSubTarget.cs';subsource=(P/subrel).read_bytes()
def one(s,x,y,count=1):
 assert s.count(x)==count,(x[:90],s.count(x),count)
 return s.replace(x,y)
ca={'helper':m.helper(),'legacy':m.patch_legacy(source['legacy'].decode('utf-8')),'color':m.patch_color(source['color'].decode('utf-8'))}
ca['graph'],ga=m.patch_graph(source['graph'].decode('utf-8'))
# Generalize the existing, already-used static define helper. This is NOT a
# material keyword, and clones never inherit the main Forward POM define.
sub=subsource.decode('utf-8')
sub=one(sub,'WithNBMainForwardDefine(WithNBLightingKeywords(WithNBFragmentInclude(pass, "NBGraphForwardPass.hlsl")))',
 'WithNBPassDefine(WithNBLightingKeywords(WithNBFragmentInclude(pass, "NBGraphForwardPass.hlsl")), "NB_GRAPH_MAIN_FORWARD")')
sub=one(sub,'static PassDescriptor WithNBMainForwardDefine(PassDescriptor pass)',
 'static PassDescriptor WithNBPassDefine(PassDescriptor pass, string referenceName)')
sub=one(sub,'referenceName = "NB_GRAPH_MAIN_FORWARD",','referenceName = referenceName,')
sub=one(sub,'            passes.Add(WithNBFragmentInclude(pass, fragmentInclude));',
 '''            // CameraOpaque forces BaseMap Clamp independently of material
            // mode. GraphDefines precedes CF code; PostGraph includes do not.
            if (lightMode == "NBCameraOpaqueDistortPass")
                pass = WithNBPassDefine(pass, "NB_GRAPH_CAMERA_OPAQUE_PASS");
            passes.Add(WithNBFragmentInclude(pass, fragmentInclude));''')
# Only CA BaseMap triple and ordinary/off BaseMap. Rig/SixWay unchanged.
selector='''// The original CameraOpaque pass forces BASEMAP wrap=Clamp even when
// the material's saved wrap says Repeat. This is pass identity, not mode.
uint NBGraphBaseMapWrapMode(uint wrapFlags)
{
#if defined(NB_GRAPH_CAMERA_OPAQUE_PASS)
    return 1u;
#else
    return NBGraphMaskWrapMode(wrapFlags, FLAG_BIT_WRAPMODE_BASEMAP);
#endif
}

'''
h=ca['color'];assert 'NBGraphBaseMapWrapMode' not in h
adapterComment='// Match the legacy raw three-sample path, not Graph\'s HDR-decoded\n// single BaseMap sample. The UV parameters are half2 before sampling.\n'
h=one(h,adapterComment,selector+adapterComment)
h=one(h,'    uint wrap = NBGraphMaskWrapMode(wrapFlags, FLAG_BIT_WRAPMODE_BASEMAP);','    uint wrap = NBGraphBaseMapWrapMode(wrapFlags);')
h=one(h,'''        baseSample = NBGraphSampleMap(BaseMap, baseUV,
            NBGraphMaskWrapMode(wrapFlags, FLAG_BIT_WRAPMODE_BASEMAP),''',
 '''        baseSample = NBGraphSampleMap(BaseMap, baseUV,
            NBGraphBaseMapWrapMode(wrapFlags),''',2)
# Existing CA graph and legacy extraction untouched by this static-pass delta.
assert all(p.read_bytes()==source[k] for k,p in m.F.items()) and (P/subrel).read_bytes()==subsource and script.read_bytes()==scriptbytes,'Input changed during review; rerun latest'
O.mkdir(parents=True,exist_ok=True)
rel={'helper':'NBShaders2/Shader/HLSL/NBShaderChromaticV1.hlsl','legacy':'NBShaders2/Shader/HLSL/NBShaderInput.hlsl','color':'NBShaders2/ShaderGraph/NBGraphBaseColor.hlsl','graph':'NBShaders2/ShaderGraph/NBShaderGraph.shadergraph','subtarget':subrel}
records=[]
for k,text in dict(ca,color=h,subtarget=sub).items():
 dest=O/rel[k];dest.parent.mkdir(parents=True,exist_ok=True);dest.write_text(text)
 prior=ca['color'] if k=='color' else subsource.decode('utf-8') if k=='subtarget' else ca[k]
 original=source[k] if k in source else subsource if k=='subtarget' else None
 dp=O/'diffs'/(rel[k].replace('/','__')+'.diff');dp.parent.mkdir(parents=True,exist_ok=True)
 dp.write_text(''.join(difflib.unified_diff(prior.splitlines(True),text.splitlines(True),fromfile='CA1-dynamic-base/'+rel[k] if k=='color' else str(P/rel[k]),tofile=str(dest))))
 records.append({'key':k,'target':str(P/rel[k]),'sourceSHA256':hashlib.sha256(original).hexdigest() if original else None,'baseCASHA256':hashlib.sha256(ca[k].encode()).hexdigest() if k in ca else None,'candidateSHA256':hashlib.sha256(dest.read_bytes()).hexdigest(),'diff':str(dp),'diffBase':'CA1 already-applied preview' if k=='color' else 'live product' if k=='subtarget' else 'unchanged CA1 preview'})
manifest={'scope':'Review-only candidate; NO Unity/import/shader/test pass claim','originalCAScriptSHA256':hashlib.sha256(scriptbytes).hexdigest(),'graphAudit':ga,'files':records,'GraphCA5PortsUnchangedByStaticFix':True,'RigSixWayUnchangedByStaticFix':True,'newKeywordAxes':0,'newCFPortsFromStaticFix':0,'NoUniformDistortionModeAsPassIdentity':True}
(O/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n')
(O/'NBGraphBaseMapWrapMode.extracted.hlsl').write_text(selector)
print(json.dumps(manifest,ensure_ascii=False,indent=2))
