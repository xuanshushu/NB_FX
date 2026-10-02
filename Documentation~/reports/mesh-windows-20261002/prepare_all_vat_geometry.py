from pathlib import Path
import hashlib,json
work=Path(__file__).resolve().parent;root=work.parents[1];package=root/'.utmp/NBFXMeshValidation-20261002/Packages/NB_FX';source=package/'Tests/URP/Editor/G4GraphVATTests.cs'
before=source.read_bytes();s=before.decode('utf-8')
def one(old,new):
    global s
    assert s.count(old)==1,old;s=s.replace(old,new)
one('        Shader _graphOverrideShader;','''        Shader _graphOverrideShader;
        // Shared directed geometry fixture; optional setup preserves old V0 cases.
        public Action<Material,bool> GeometrySetup;
        public Action<Mesh> GeometryMeshSetup;
        public Action<Material,bool,float> GeometryFrameState;
        public string GeometryCaseId;''')
one('Path.Combine(project, "Temp/NBFXVATGeometry"), (shadow ? "shadow" : "depth") + (ortho ? "-ortho" : "-perspective"))',
 'Path.Combine(project, "Temp/NBFXVATGeometry"), (string.IsNullOrEmpty(GeometryCaseId)?"":GeometryCaseId+"-") + (shadow ? "shadow" : "depth") + (ortho ? "-ortho" : "-perspective"))')
one('            actor.GetComponent<MeshFilter>().sharedMesh = mesh;\n            var writer = actor.GetComponent<MeshRenderer>();',
 '            GeometryMeshSetup?.Invoke(mesh);\n            actor.GetComponent<MeshFilter>().sharedMesh = mesh;\n            var writer = actor.GetComponent<MeshRenderer>();')
one('                    Configure(m,m==c,"manual1",map,position,second,rotation,map,map,map);\n                    m.SetFloat("_Surface",0);',
 '                    Configure(m,m==c,"manual1",map,position,second,rotation,map,map,map);\n                    GeometrySetup?.Invoke(m,m==c);\n                    m.SetFloat("_Surface",0);')
one('{ SetVAT(m,false);writer.sharedMaterial=m;for(int i=0;i<4;i++)camera.Render(); }',
 '{ if(GeometryFrameState!=null)GeometryFrameState(m,m==c,0);else SetVAT(m,false);writer.sharedMaterial=m;for(int i=0;i<4;i++)camera.Render(); }')
one('{ SetVAT(m,enabled);m.SetFloat("_displayFrame",frame);writer.sharedMaterial=m;for(int i=0;i<3;i++)camera.Render();return Draw(writer,m,camera,rt,readback,folder,label); }',
 '{ if(GeometryFrameState!=null)GeometryFrameState(m,m==c,enabled?frame:0);else {SetVAT(m,enabled);m.SetFloat("_displayFrame",frame);}writer.sharedMaterial=m;for(int i=0;i<3;i++)camera.Render();return Draw(writer,m,camera,rt,readback,folder,label); }')
one('caseId=shadow?"actual-shadow-geometry":"actual-depth-geometry",',
 'caseId=(string.IsNullOrEmpty(GeometryCaseId)?"":GeometryCaseId+"-")+(shadow?"actual-shadow-geometry":"actual-depth-geometry"),')
target=work/'all-vat-geometry-preview/Tests/URP/Editor/G4GraphVATTests.cs';target.parent.mkdir(parents=True,exist_ok=True);target.write_text(s,encoding='utf-8',newline='\n')
(target.parents[3]/'manifest.json').write_text(json.dumps({'scope':'Test-only optional callbacks reuse existing selected DepthOnly/ShadowCaster fixture. Original V0 cases retain original setup. Unique per-mode folders. Product kernel unchanged.',
 'path':source.relative_to(package).as_posix(),'beforeSHA256':hashlib.sha256(before).hexdigest(),'afterSHA256':hashlib.sha256(target.read_bytes()).hexdigest()},indent=2)+'\n',encoding='utf-8',newline='\n')
print('Prepared existing directed fixture callbacks; not installed during active native run')
