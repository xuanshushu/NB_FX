from pathlib import Path
import hashlib,json
work=Path(__file__).resolve().parent;root=work.parents[1];test=root/'.utmp/NBFXMeshValidation-20261002/Packages/NB_FX/Tests/URP/Editor/G4GraphTyflowVATTests.cs';before=test.read_bytes();s=before.decode('utf-8')
backup=work/'G4GraphTyflowVATTests-before-positive-gamma.cs';assert not backup.exists();backup.write_bytes(before)
def one(old,new):
    global s
    assert s.count(old)==1,old;s=s.replace(old,new)
one('new[]{"raw","half","float","custom-frame","normals"}:new[]{"rigid","deform7","interpolated","deform7-interpolated"}',
 'new[]{"raw","half","float","custom-frame","normals","half-gamma","float-gamma"}:new[]{"rigid","deform7","interpolated","deform7-interpolated","gamma"}')
one('Texture2D Texture(int width,int height,Func<int,int,Color> pixel,TextureFormat format)',
 'Texture2D Texture(int width,int height,Func<int,int,Color> pixel,TextureFormat format,bool linear=true)')
one('new Texture2D(width,height,format,false,true)','new Texture2D(width,height,format,false,linear)')
one('if(variant=="float")return Texture','if(variant.StartsWith("float",StringComparison.Ordinal))return Texture')
one('if(variant=="half")return Texture','if(variant.StartsWith("half",StringComparison.Ordinal))return Texture')
one('EncodedFloat(floats[i]):Color.clear;},TextureFormat.RGBA32);','EncodedFloat(floats[i]):Color.clear;},TextureFormat.RGBA32,!variant.EndsWith("gamma",StringComparison.Ordinal));')
one('floats[i+1]:0):Color.clear;},TextureFormat.RGBA32);','floats[i+1]:0):Color.clear;},TextureFormat.RGBA32,!variant.EndsWith("gamma",StringComparison.Ordinal));')
one('table[(15-y)*16+x],TextureFormat.RGBA32);','table[(15-y)*16+x],TextureFormat.RGBA32,!variant.EndsWith("gamma",StringComparison.Ordinal));')
one('m.SetFloat("_LinearToGamma",0);m.SetFloat("_RGBAEncoded",variant=="float"||variant=="half"?1:0);m.SetFloat("_RGBAHalf",variant=="half"?1:0);',
 'm.SetFloat("_LinearToGamma",variant.EndsWith("gamma",StringComparison.Ordinal)?1:0);m.SetFloat("_RGBAEncoded",variant.StartsWith("float",StringComparison.Ordinal)||variant.StartsWith("half",StringComparison.Ordinal)?1:0);m.SetFloat("_RGBAHalf",variant.StartsWith("half",StringComparison.Ordinal)?1:0);')
test.write_text(s,encoding='utf-8',newline='\n')
(work/'tyflow-positive-gamma-fixture.json').write_text(json.dumps({'scope':'Additional positive gamma cases use real sRGB encoded textures. Existing52 names retain exact prior setup; assertions unchanged. No product-source change.',
 'beforeSHA256':hashlib.sha256(before).hexdigest(),'afterSHA256':hashlib.sha256(test.read_bytes()).hexdigest(),'additionalCases':16},indent=2)+'\n',encoding='utf-8',newline='\n')
plan={'scope':'Tyflow positive gamma16: actual sRGB encoded half/float andfour skin modes, original exact conversion; notfullVAT','unity':'6000.3.25f1','api':'Direct3D11','cases':16,'batches':[]}
for batch,group in enumerate([range(2),range(2,6)],1):
    names=['NBFX.Baseline.Tests.G4GraphTyflowVATTests.G4TyflowABC_m'+str(mode)+'_'+v+('_ortho' if o else '_perspective') for mode in group for v in (['half-gamma','float-gamma'] if mode<2 else ['gamma']) for o in [True,False]]
    plan['batches'].append({'batch':batch,'filter':';'.join(n.split('.')[-1] for n in names),'expectedCasesFromActualDiscovery':len(names),'exactCaseNames':names})
(work/'tyflow-positive-gamma16-plan.json').write_text(json.dumps(plan,indent=2)+'\n',encoding='utf-8',newline='\n')
print('Positive gamma16 prepared; original52 case setup retained')
