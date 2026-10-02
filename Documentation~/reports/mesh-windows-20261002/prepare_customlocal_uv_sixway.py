from pathlib import Path
import hashlib,json
work=Path(__file__).resolve().parent;root=work.parents[1];source=root/'.utmp/NBFXMeshValidation-20261002/Packages/NB_FX/Tests/URP/Editor/G4GraphCustomLocalTests.cs';before=source.read_bytes();s=before.decode('utf-8');backup=work/'G4GraphCustomLocalTests-before-uv-sixway.cs';assert not backup.exists();backup.write_bytes(before)
def one(old,new):
    global s
    assert s.count(old)==1,old;s=s.replace(old,new)
one('new[]{"passthrough","vo-normal","vo-world","houdini-soft","tyflow-relative"}',
 'new[]{"passthrough","vo-normal","vo-world","houdini-soft","tyflow-relative","object-uv-vertex","object-uv-fragment","sixway"}')
one('var configure=typeof(G4GraphVATTests).GetMethod("Configure",Private);','''var configure=typeof(G4GraphVATTests).GetMethod("Configure",Private);
            var oldAmbientMode=RenderSettings.ambientMode;var oldProbe=RenderSettings.ambientProbe;
            if(variant=="sixway")
            {
                var lightObject=Keep(new GameObject("CustomLocal SixWay light",typeof(Light)));SceneManager.MoveGameObjectToScene(lightObject,scene);
                var light=lightObject.GetComponent<Light>();light.type=LightType.Directional;light.intensity=1.35f;light.shadows=LightShadows.None;light.transform.rotation=Quaternion.Euler(42,31,0);
                var probe=new SphericalHarmonicsL2();probe.AddDirectionalLight(new Vector3(.4f,.5f,1).normalized,new Color(.35f,.24f,.14f),1);RenderSettings.ambientMode=AmbientMode.Custom;RenderSettings.ambientProbe=probe;
            }''')
one('configure.Invoke(null,new object[]{m,graph,"manual2",map,position,position,rotation,map,map,map});',
 'configure.Invoke(null,new object[]{m,graph,variant=="sixway"?"normal-sixway":"manual2",map,position,position,rotation,map,map,map});')
one('if(variant=="houdini-soft")','if(variant=="houdini-soft"||variant=="sixway")')
one('                if(graph)for(int row=0;row<4;row++)','''                if(variant.StartsWith("object-uv-",StringComparison.Ordinal))
                {
                    typeof(G4GraphPositionUVTests).GetMethod("SetRoute",Private).Invoke(null,new object[]{m,"main",7,false,variant=="object-uv-fragment"});
                    m.SetFloat("_ObjectSpaceUVModeSelector",1);m.SetFloat("_TWStrength",0);
                }
                if(graph)for(int row=0;row<4;row++)''')
one('finally{camera.targetTexture=null;RenderTexture.active=oldRT;rt.Release();}',
 'finally{camera.targetTexture=null;RenderTexture.active=oldRT;rt.Release();RenderSettings.ambientMode=oldAmbientMode;RenderSettings.ambientProbe=oldProbe;}')
source.write_text(s,encoding='utf-8',newline='\n')
variants=['object-uv-vertex','object-uv-fragment','sixway'];names=['NBFX.Baseline.Tests.G4GraphCustomLocalTests.G4CustomLocalABC_'+v+('_negative' if n else '_positive')+('_ortho' if o else '_perspective') for v in variants for n in [False,True] for o in [True,False]]
plan={'scope':'CustomLocal UVstage12 and SixWay4 grouped total12: objectvertex4/objectfragment4/SixWay4; nofullTBNclaim','unity':'6000.3.25f1','api':'Direct3D11','cases':12,
 'batches':[{'batch':1,'filter':';'.join('G4CustomLocalABC_'+v+'_' for v in variants),'expectedCasesFromActualDiscovery':12,'exactCaseNames':names}]}
(work/'customlocal-uv-sixway12-plan.json').write_text(json.dumps(plan,indent=2)+'\n',encoding='utf-8',newline='\n')
(work/'customlocal-uv-sixway-fixture.json').write_text(json.dumps({'scope':'Additional actual UV andSixWay positive/negative custommatrix fixture, original20 setup preserved','beforeSHA256':hashlib.sha256(before).hexdigest(),'afterSHA256':hashlib.sha256(source.read_bytes()).hexdigest()},indent=2)+'\n',encoding='utf-8',newline='\n')
print('Prepared additional CustomLocal UV/SixWay12')
