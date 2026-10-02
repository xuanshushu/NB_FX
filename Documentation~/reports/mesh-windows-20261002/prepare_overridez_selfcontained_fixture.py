"""Remove the depth fixture's accidental dependency on the isolation-only VAT fixture."""
from pathlib import Path
import hashlib, json
work=Path(__file__).resolve().parent
src=work/'G4GraphOverrideDepthTests.cs'
text=src.read_text()
text=text.replace('            var configure=typeof(G4GraphVATTests).GetMethod("Configure",BindingFlags.Static|BindingFlags.NonPublic);\n','')
old='configure.Invoke(null,new object[]{m,m==materials[2],"manual1",white,white,white,white,white,white,white});m.SetFloat("_VAT_Toggle",0);'
assert text.count(old)==1
text=text.replace(old,'ConfigureBase(m,m==materials[2],white);if(m.HasProperty("_VAT_Toggle"))m.SetFloat("_VAT_Toggle",0);')
old='var draw=typeof(G4GraphVATTests).GetMethod("Draw",BindingFlags.Static|BindingFlags.NonPublic);'
assert text.count(old)==1
text=text.replace(old,'')
old='Color[] Snap(Material m,string name)=>(Color[])draw.Invoke(null,new object[]{writer,m,camera,rt,read,folder,name});'
assert text.count(old)==1
text=text.replace(old,'Color[] Snap(Material m,string name)=>Draw(writer,m,camera,rt,read,folder,name);')
anchor='        static float Delta(Color[] a,Color[] b)'
assert text.count(anchor)==1
methods='''        static void ConfigureBase(Material mat,bool isGraph,Texture2D baseMap)
        {
            mat.SetTexture("_BaseMap",baseMap);mat.SetColor("_BaseColor",Color.white);mat.SetColor("_Color",Color.white);mat.SetColor("_ColorA",Color.white);
            mat.SetFloat("_AlphaAll",1);mat.SetFloat("_BaseColorIntensityForTimeline",1);mat.SetFloat("_Cull",(float)CullMode.Off);
            mat.SetFloat("_ZTest",(float)CompareFunction.LessEqual);mat.SetFloat("_ZWrite",0);mat.SetFloat("_SrcBlend",(float)BlendMode.One);mat.SetFloat("_DstBlend",(float)BlendMode.Zero);mat.SetFloat("_fogintensity",0);mat.renderQueue=3000;
            foreach(string pass in new[]{"SRPDefaultUnlit","SRPDEFAULTUNLIT","UniversalForward","DepthOnly","ShadowCaster","Universal2D","NBCameraOpaqueDistortPass","NBDeferredDistortPass"})mat.SetShaderPassEnabled(pass,false);
            mat.SetShaderPassEnabled(isGraph?"SRPDefaultUnlit":"UniversalForward",true);
            if(isGraph){mat.SetFloat("_Surface",1);mat.SetFloat("_SrcBlendAlpha",1);mat.SetFloat("_DstBlendAlpha",0);mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");mat.SetFloat("_NB_Flags1Lo16",0);mat.SetFloat("_NB_Flags1Hi16",0);}
            else{mat.EnableKeyword("_FX_LIGHT_MODE_UNLIT");mat.SetInteger("_W9ParticleShaderFlags",0);mat.SetInteger("_W9ParticleShaderFlags1",0);mat.SetFloat("_ColorMask",15);}
        }
        static Color[] Draw(MeshRenderer renderer,Material material,Camera camera,RenderTexture rt,Texture2D read,string folder,string name)
        {
            renderer.sharedMaterial=material;camera.Render();RenderTexture.active=rt;read.ReadPixels(new Rect(0,0,128,128),0,0,false);read.Apply(false,false);var frame=read.GetPixels();
            using(var stream=File.Create(Path.Combine(folder,name+".rgba32f")))using(var writer=new BinaryWriter(stream))foreach(Color px in frame){writer.Write(px.r);writer.Write(px.g);writer.Write(px.b);writer.Write(px.a);}return frame;
        }
'''
text=text.replace(anchor,methods+anchor)
out=work/'overridez-selfcontained-fixture-preview/Tests/URP/Editor/G4GraphOverrideDepthTests.cs'
assert not out.exists();out.parent.mkdir(parents=True,exist_ok=True);out.write_text(text,encoding='utf-8',newline='\n')
manifest={'scope':'Same strict depth16 fixture, independent of unintegrated VAT helpers; no assertion changes',
 'originalSHA256':hashlib.sha256(src.read_bytes()).hexdigest(),'afterSHA256':hashlib.sha256(out.read_bytes()).hexdigest(),
 'assertionLinesUnchanged':True,'path':'Tests/URP/Editor/G4GraphOverrideDepthTests.cs'}
beforeAssertions=[l.strip() for l in src.read_text().splitlines() if 'Assert.That' in l]
afterAssertions=[l.strip() for l in text.splitlines() if 'Assert.That' in l]
assert beforeAssertions==afterAssertions
(out.parents[3]/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n',encoding='utf-8',newline='\n')
print(json.dumps(manifest))
