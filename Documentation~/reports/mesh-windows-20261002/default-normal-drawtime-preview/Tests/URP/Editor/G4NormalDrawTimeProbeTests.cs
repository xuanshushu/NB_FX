using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace NBFX.Baseline.Tests
{
    // Diagnostic shaders live only in memory. No Shader/HLSL/Graph asset writes.
    // Basic cases do not depend on isolated VAT test classes or VAT interfaces.
    public sealed class G4NormalDrawTimeProbeTests
    {
        const string Package="Packages/com.xuanxuan.nb.fx/";
        const string GraphPath=Package+"NBShaders2/ShaderGraph/NBShaderGraph.shadergraph";
        const int Size=128,Layer=2;
        readonly List<Object> owned=new List<Object>();
        T Keep<T>(T x)where T:Object {owned.Add(x);return x;}
        [OneTimeSetUp] public void ImportGraph()=>AssetDatabase.ImportAsset(GraphPath,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
        [TestCase(false,true,TestName="NormalDrawTimeABC_basic_ortho")]
        [TestCase(false,false,TestName="NormalDrawTimeABC_basic_perspective")]
        [TestCase(true,true,TestName="NormalDrawTimeABC_softbody_ortho")]
        [TestCase(true,false,TestName="NormalDrawTimeABC_softbody_perspective")]
        public void RealForwardDrawReceivesScaleTextureAndDecodedNormals(bool softbody,bool ortho)
        {
            var package=UnityEditor.PackageManager.PackageInfo.FindForAssetPath(GraphPath);Assert.That(package,Is.Not.Null);string resolved=package.resolvedPath;
            var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;Assert.That(pipeline,Is.Not.Null);var rd=pipeline.rendererDataList[0];Assert.That(rd.rendererFeatures.Any(f=>f&&f.isActive&&f.GetType().Name.Contains("AmbientOcclusion")),Is.True);
            string project=Path.GetDirectoryName(Application.dataPath),rendererFile=Path.Combine(project,AssetDatabase.GetAssetPath(rd));byte[] rendererBefore=File.ReadAllBytes(rendererFile);
            string folder=Path.Combine(Environment.GetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR")??Path.Combine(project,"Temp/NormalDrawTime"),(softbody?"softbody":"basic")+(ortho?"-ortho":"-perspective"));Directory.CreateDirectory(folder);
            var scene=EditorSceneManager.NewPreviewScene();RenderTexture oldRT=RenderTexture.active;var oldSun=RenderSettings.sun;var oldAmbient=RenderSettings.ambientLight;var oldMode=RenderSettings.ambientMode;
            var nb=rd.rendererFeatures.FirstOrDefault(f=>f&&f.GetType().FullName=="NBShader.NBPostProcess");bool oldNB=nb&&nb.isActive;
            Camera camera=null;MeshRenderer actorRenderer=null;Material originalSlot=null;Action<ScriptableRenderContext,Camera> callback=null;
            try
            {
                var actor=Keep(GameObject.CreatePrimitive(PrimitiveType.Quad));var floor=Keep(GameObject.CreatePrimitive(PrimitiveType.Plane));var cameraGO=Keep(new GameObject("Draw-time normal camera"));var sunGO=Keep(new GameObject("Draw-time normal sun",typeof(Light)));
                foreach(var go in new[]{actor,floor,cameraGO,sunGO}){SceneManager.MoveGameObjectToScene(go,scene);go.layer=Layer;}
                actor.transform.SetPositionAndRotation(new Vector3(0,1,0),Quaternion.Euler(-90,0,0));actor.transform.localScale=new Vector3(1.5f,1.5f,1);floor.transform.localScale=new Vector3(.55f,1,.55f);
                actorRenderer=actor.GetComponent<MeshRenderer>();actorRenderer.shadowCastingMode=ShadowCastingMode.On;originalSlot=actorRenderer.sharedMaterial;var floorRenderer=floor.GetComponent<MeshRenderer>();floorRenderer.shadowCastingMode=ShadowCastingMode.Off;
                var floorMat=Keep(new Material(Shader.Find("Universal Render Pipeline/Lit")));floorMat.SetColor("_BaseColor",Color.white);floorRenderer.sharedMaterial=floorMat;
                camera=cameraGO.AddComponent<Camera>();camera.scene=scene;camera.orthographic=ortho;camera.orthographicSize=3.5f;camera.fieldOfView=45;camera.nearClipPlane=.1f;camera.farClipPlane=25;camera.transform.position=new Vector3(0,4.5f,-5.5f);camera.transform.LookAt(Vector3.zero);camera.cullingMask=1<<Layer;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.allowHDR=true;camera.allowMSAA=false;cameraGO.AddComponent<UniversalAdditionalCameraData>().renderShadows=true;
                var sun=sunGO.GetComponent<Light>();sun.type=LightType.Directional;sun.shadows=LightShadows.Hard;sun.shadowNormalBias=0;sun.intensity=2;sun.transform.rotation=Quaternion.Euler(50,-30,0);sun.cullingMask=1<<Layer;RenderSettings.sun=sun;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=Color.black;
                var target=Keep(new RenderTexture(Size,Size,24,RenderTextureFormat.ARGBHalf,RenderTextureReadWrite.Linear));var read=Keep(new Texture2D(Size,Size,TextureFormat.RGBAHalf,false,true));camera.targetTexture=target;target.Create();Assert.That(target.IsCreated()&&!target.sRGB,Is.True);
                var bump=Keep(new Texture2D(2,2,TextureFormat.RGBAHalf,false,true){name="NormalDrawTime actual RGBAHalf bump"});bump.SetPixels(Enumerable.Repeat(new Color(.2f,.7f,1,1),4).ToArray());bump.filterMode=FilterMode.Point;bump.wrapMode=TextureWrapMode.Clamp;bump.Apply(false);var white=Keep(new Texture2D(2,2,TextureFormat.RGBAHalf,false,true));white.SetPixels(Enumerable.Repeat(Color.white,4).ToArray());white.Apply(false);
                var materials=new[]{Keep(new Material(AssetDatabase.LoadAssetAtPath<Shader>(Package+"Tests/Baseline/Frozen/NBShaders2/Shader/NBShader.shader"))),Keep(new Material(AssetDatabase.LoadAssetAtPath<Shader>(Package+"NBShaders2/Shader/NBShader.shader"))),Keep(new Material(AssetDatabase.LoadAssetAtPath<Shader>(GraphPath)))};
                Texture2D vatPosition=null,vatRotation=null;
                if(softbody)
                {
                    foreach(string n in new[]{"_VAT_Toggle","_posTexture","_rotTexture","_displayFrame","_frameCount"})Assert.That(materials[2].HasProperty(n),Is.True,"SoftBody diagnostic requires actual combined interface; root basic cannot stand in.");
                    vatPosition=Keep(new Texture2D(8,8,TextureFormat.RGBAHalf,false,true));for(int y=0;y<8;y++)for(int x=0;x<8;x++)vatPosition.SetPixel(x,y,new Color(.50f+.045f*x-.015f*y,.50f+.018f*y-.012f*x,.50f+.025f*((x+2*y)%5),.5f));vatPosition.filterMode=FilterMode.Point;vatPosition.wrapMode=TextureWrapMode.Clamp;vatPosition.Apply(false);
                    vatRotation=Keep(new Texture2D(2,2,TextureFormat.RGBAHalf,false,true));vatRotation.SetPixels(Enumerable.Repeat(new Color(.5f,.5f,.5f,1),4).ToArray());vatRotation.Apply(false);
                    var mesh=Keep(Object.Instantiate(actor.GetComponent<MeshFilter>().sharedMesh));mesh.uv2=new[]{new Vector2(.20f,.66f),new Vector2(.80f,.66f),new Vector2(.20f,.86f),new Vector2(.80f,.86f)};actor.GetComponent<MeshFilter>().sharedMesh=mesh;
                }
                foreach(var m in materials)
                {
                    bool graph=m==materials[2];m.shaderKeywords=graph?Array.Empty<string>():new[]{"_FX_LIGHT_MODE_BLINN_PHONG","_NORMALMAP","_FRESNEL"};m.SetTexture("_BaseMap",white);m.SetTexture("_BumpTex",bump);m.SetColor(graph?"_Color":"_BaseColor",Color.white);m.SetColor("_ColorA",Color.white);m.SetFloat("_BaseColorIntensityForTimeline",1);m.SetFloat("_AlphaAll",1);m.SetFloat("_BumpMapToggle",1);m.SetFloat("_BumpScale",1);m.SetFloat("_FxLightMode",1);m.SetVector("_MaterialInfo",new Vector4(.6f,.75f,0,0));m.SetColor("_SpecularColor",new Color(.8f,.7f,.6f,1));m.SetFloat("_fresnelEnabled",1);m.SetVector("_FresnelUnit",new Vector4(.5f,1,.75f,0));m.SetColor("_FresnelColor",new Color(.8f,.2f,.1f,1));m.SetVector("_FresnelRotation",Vector4.zero);m.SetFloat("_Cull",0);m.SetFloat("_ZTest",4);m.SetFloat("_ZWrite",1);m.SetFloat("_SrcBlend",1);m.SetFloat("_DstBlend",0);m.SetFloat("_ColorMask",15);m.SetFloat("_AffectsShadows",1);m.SetFloat("_Cutoff",.5f);m.DisableKeyword("_ALPHATEST_ON");m.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");m.DisableKeyword("_OVERRIDE_Z");m.renderQueue=2100;
                    if(graph){m.SetFloat("_Surface",0);m.SetFloat("_ZWriteControl",0);m.SetFloat("_CastShadows",1);m.SetFloat("_NB_Flags1Lo16",512);m.SetFloat("_NB_Flags1Hi16",0);m.SetFloat("_NB_ColorChannelLo16",3);m.SetFloat("_SrcBlendAlpha",1);m.SetFloat("_DstBlendAlpha",0);}
                    else{m.SetInteger("_W9ParticleShaderFlags",0);m.SetInteger("_W9ParticleShaderFlags1",512);m.SetInteger("_W9ParticleShaderColorChannelFlag",3);}
                    foreach(string tag in new[]{"SRPDefaultUnlit","UniversalForward","NBCameraOpaqueDistortPass","NBDeferredDistortPass","Universal2D"})m.SetShaderPassEnabled(tag,false);
                    m.SetShaderPassEnabled("DepthOnly",true);m.SetShaderPassEnabled("ShadowCaster",true);if(graph)m.SetShaderPassEnabled("DepthNormalsOnly",true);
                    if(m.HasProperty("_VAT_Toggle"))m.SetFloat("_VAT_Toggle",softbody?1:0);
                    if(softbody){m.SetTexture("_posTexture",vatPosition);m.SetTexture("_rotTexture",vatRotation);m.SetFloat("_VATMode",0);m.SetFloat("_HoudiniVATSubMode",0);m.SetFloat("_frameCount",2);m.SetFloat("_displayFrame",1);m.SetFloat("_B_autoPlayback",0);m.SetFloat("_B_UNLOAD_ROT_TEX",0);m.SetFloat("_B_LOAD_POS_TWO_TEX",0);m.SetFloat("_boundMinX",-1);m.SetFloat("_boundMinY",-1);m.SetFloat("_boundMinZ",-1);m.SetFloat("_boundMaxX",1);m.SetFloat("_boundMaxY",1);m.SetFloat("_boundMaxZ",1);if(!graph){m.EnableKeyword("_VAT");m.EnableKeyword("_VAT_HOUDINI");m.EnableKeyword("_HOUDINI_VAT_SOFTBODY");}}
                }
                if(nb)nb.SetActive(false);
                // Warm real Graph then read the actual tag. Display Name is
                // used only to find an index, never as the material enable key.
                actorRenderer.sharedMaterial=materials[2];for(int n=0;n<4;n++)camera.Render();foreach(var m in materials)EnableActualMain(m);
                var receipts=new List<DrawReceipt>();string phase="";
                callback=(context,cam)=>{if(cam==camera)receipts.Add(Receipt(actorRenderer,phase,"beginCameraRendering"));};RenderPipelineManager.beginCameraRendering+=callback;
                var originalFrames=new List<Color[]>();foreach(var m in materials)foreach(float scale in new[]{1f,0f})
                {m.SetFloat("_BumpScale",scale);phase=m.shader.name+" original scale"+scale;originalFrames.Add(Capture(actorRenderer,m,camera,target,read,folder,"original-"+Array.IndexOf(materials,m)+"-s"+scale,receipts,phase));}
                // Diagnostic RGB is observed only on the actor. Original floor
                // frames above retain actual default SSAO/Depth/Shadow behavior.
                floorRenderer.enabled=false;
                var uniformFrames=new List<Color[]>();var sampleFrames=new List<Color[]>();var tsFrames=new List<Color[]>();var wsFrames=new List<Color[]>();
                for(int host=0;host<3;host++)for(int probe=0;probe<4;probe++)
                {
                    bool graph=host==2;string shaderText=graph?GeneratedGraph():File.ReadAllText(Path.Combine(resolved,host==0?"Tests/Baseline/Frozen/NBShaders2/Shader/NBShader.shader":"NBShaders2/Shader/NBShader.shader"));string source=BuildProbe(shaderText,resolved,host,probe);File.WriteAllText(Path.Combine(folder,"host"+host+"-probe"+probe+".shader"),source);
                    var diagnostic=Keep(ShaderUtil.CreateShaderAsset(source,false));diagnostic.hideFlags=HideFlags.HideAndDontSave;Assert.That(diagnostic&&diagnostic.isSupported,Is.True);Assert.That(ShaderUtil.GetShaderMessages(diagnostic).Any(x=>x.severity.ToString()=="Error"),Is.False);
                    var mat=Keep(new Material(diagnostic){hideFlags=HideFlags.HideAndDontSave});mat.CopyPropertiesFromMaterial(materials[host]);mat.shaderKeywords=materials[host].shaderKeywords;mat.renderQueue=materials[host].renderQueue;
                    for(int originalPass=0;originalPass<materials[host].passCount;++originalPass)
                    {
                        string raw=materials[host].shader.FindPassTagValue(0,originalPass,new ShaderTagId("LightMode")).name;
                        string tag=string.IsNullOrEmpty(raw)?"SRPDefaultUnlit":raw;
                        mat.SetShaderPassEnabled(tag,materials[host].GetShaderPassEnabled(tag));
                    }
                    EnableActualMain(mat);
                    var frames=new List<Color[]>();foreach(float scale in new[]{1f,0f})
                    {
                        mat.SetFloat("_BumpScale",scale);phase="host"+host+" probe"+probe+" scale"+scale;var a=Capture(actorRenderer,mat,camera,target,read,folder,"h"+host+"-p"+probe+"-s"+scale,receipts,phase);var b=Capture(actorRenderer,mat,camera,target,read,folder,"h"+host+"-p"+probe+"-s"+scale+"-repeat",receipts,phase);Assert.That(Delta(a,b),Is.Zero);frames.Add(a);
                    }
                    var roi=Enumerable.Range(0,frames[0].Length).Where(i=>frames[0][i].a>.9f).ToArray();Assert.That(roi.Length,Is.GreaterThan(150));
                    if(probe==0){uniformFrames.AddRange(frames);foreach(int i in roi){Assert.That(frames[0][i].r,Is.EqualTo(1f));Assert.That(frames[1][i].r,Is.EqualTo(1f));Assert.That(frames[0][i].g,Is.EqualTo(1f));Assert.That(frames[1][i].g,Is.Zero);Assert.That(frames[0][i].b,Is.EqualTo(1f));Assert.That(frames[1][i].b,Is.EqualTo(1f));}}
                    if(probe==1){sampleFrames.AddRange(frames);var expected=bump.GetPixel(0,0);foreach(var a in frames)foreach(int i in roi)for(int c=0;c<4;c++)Assert.That(a[i][c],Is.EqualTo(expected[c]));}
                    if(probe==2){tsFrames.AddRange(frames);Assert.That(Delta(frames[0],frames[1]),Is.GreaterThan(.01f),"Decoded actual normalTS did not consume scale; raw + CPU draw receipts retained.");}
                    if(probe==3){wsFrames.AddRange(frames);Assert.That(Delta(frames[0],frames[1]),Is.GreaterThan(.01f),"Decoded actual normalWS did not consume scale.");}
                }
                var report=new Report{scope="Draw-time diagnostic only, exact original vertex/pass/include/source copied in memory; original failures unchanged; basic root or SoftBody interface labelled; no G4 release claim",softbody=softbody,orthographic=ortho,finite=Finite(originalFrames.Concat(uniformFrames).Concat(sampleFrames).Concat(tsFrames).Concat(wsFrames)),receipts=receipts.ToArray(),originalAB=Delta(originalFrames[0],originalFrames[2])+Delta(originalFrames[1],originalFrames[3]),originalBC=Delta(originalFrames[2],originalFrames[4])+Delta(originalFrames[3],originalFrames[5]),originalResponse=new[]{Delta(originalFrames[0],originalFrames[1]),Delta(originalFrames[2],originalFrames[3]),Delta(originalFrames[4],originalFrames[5])},ssaoActive=rd.rendererFeatures.Any(f=>f&&f.isActive&&f.GetType().Name.Contains("AmbientOcclusion")),generatedGraphSHA256=HashText(GeneratedGraph())};File.WriteAllText(Path.Combine(folder,"drawtime-report.json"),JsonUtility.ToJson(report,true));Assert.That(report.finite,Is.True);
                // These diagnostics never accept an original B/C difference.
                // All payloads have been recorded before the strict final verdict.
                Assert.That(report.originalAB+report.originalBC,Is.Zero,"Original full default chain still differs; diagnostic probe success is not a feature/Gate release.");
            }
            finally
            {
                if(callback!=null)RenderPipelineManager.beginCameraRendering-=callback;if(actorRenderer)actorRenderer.sharedMaterial=originalSlot;if(camera)camera.targetTexture=null;RenderTexture.active=oldRT;RenderSettings.sun=oldSun;RenderSettings.ambientMode=oldMode;RenderSettings.ambientLight=oldAmbient;if(nb)nb.SetActive(oldNB);for(int i=owned.Count-1;i>=0;i--)if(owned[i])Object.DestroyImmediate(owned[i]);owned.Clear();EditorSceneManager.ClosePreviewScene(scene);Assert.That(File.ReadAllBytes(rendererFile),Is.EqualTo(rendererBefore),"Diagnostic saved RendererData asset");
            }
        }
        [Serializable] sealed class DrawReceipt
        {
            public string phase,boundary,shader,mainDisplayName,actualLightMode,effectiveMaterialTag,textureName,textureSHA;
            public bool actualMainEnabled,normalKeyword,blinnKeyword,mpbEmpty;
            public float bumpToggle,bumpScale,lightMode;
            public int materialID,textureID,width,height;
            public Color[] cpuTexels;
        }
        [Serializable] sealed class Report {public string scope,generatedGraphSHA256;public bool softbody,orthographic,finite,ssaoActive;public DrawReceipt[] receipts;public float originalAB,originalBC;public float[] originalResponse;}
        static string MainTag(Material m,out int index)
        {
            index=m.FindPass("UniversalForward");if(index<0)index=m.FindPass("Universal Forward");Assert.That(index,Is.GreaterThanOrEqualTo(0));string raw=m.shader.FindPassTagValue(0,index,new ShaderTagId("LightMode")).name;return string.IsNullOrEmpty(raw)?"SRPDefaultUnlit":raw;
        }
        static void EnableActualMain(Material m){int index;string tag=MainTag(m,out index);m.SetShaderPassEnabled(tag,true);Assert.That(m.GetShaderPassEnabled(tag),Is.True);}
        static DrawReceipt Receipt(MeshRenderer renderer,string phase,string boundary)
        {
            var m=renderer.sharedMaterial;int index;string tag=MainTag(m,out index);var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);var tex=m.GetTexture("_BumpTex")as Texture2D;var pixels=tex?tex.GetPixels():Array.Empty<Color>();
            return new DrawReceipt{phase=phase,boundary=boundary,shader=m.shader.name,materialID=m.GetInstanceID(),mainDisplayName=m.GetPassName(index),actualLightMode=m.shader.FindPassTagValue(0,index,new ShaderTagId("LightMode")).name,effectiveMaterialTag=tag,actualMainEnabled=m.GetShaderPassEnabled(tag),normalKeyword=m.IsKeywordEnabled("_NORMALMAP"),blinnKeyword=m.IsKeywordEnabled("_FX_LIGHT_MODE_BLINN_PHONG"),mpbEmpty=block.isEmpty,bumpToggle=m.HasProperty("_BumpMapToggle")?m.GetFloat("_BumpMapToggle"):-1,bumpScale=m.GetFloat("_BumpScale"),lightMode=m.GetFloat("_FxLightMode"),textureName=tex?tex.name:"null",textureID=tex?tex.GetInstanceID():0,width=tex?tex.width:0,height=tex?tex.height:0,cpuTexels=pixels,textureSHA=HashText(string.Join(";",pixels.Select(p=>p.ToString("R"))))};
        }
        static Color[] Capture(MeshRenderer renderer,Material material,Camera camera,RenderTexture target,Texture2D read,string folder,string label,List<DrawReceipt> receipts,string phase)
        {
            renderer.sharedMaterial=material;receipts.Add(Receipt(renderer,phase,"afterSetFloat beforeCameraRender"));for(int i=0;i<4;i++)camera.Render();var old=RenderTexture.active;RenderTexture.active=target;read.ReadPixels(new Rect(0,0,Size,Size),0,0,false);read.Apply(false,false);var px=read.GetPixels();RenderTexture.active=old;receipts.Add(Receipt(renderer,phase,"afterReadPixels beforeControlRestore"));File.WriteAllText(Path.Combine(folder,label+"-draw-receipts.json"),JsonUtility.ToJson(new Report{receipts=receipts.ToArray()},true));using(var f=File.Create(Path.Combine(folder,label+".rgba32f")))using(var b=new BinaryWriter(f))foreach(var p in px){b.Write(p.r);b.Write(p.g);b.Write(p.b);b.Write(p.a);}Assert.That(Finite(new[]{px}),Is.True);return px;
        }
        static float Delta(Color[] a,Color[] b){float x=0;for(int i=0;i<a.Length;i++)for(int c=0;c<4;c++)x=Mathf.Max(x,Mathf.Abs(a[i][c]-b[i][c]));return x;}
        static bool Finite(IEnumerable<Color[]> arrays){foreach(var a in arrays)foreach(var p in a)for(int c=0;c<4;c++)if(float.IsNaN(p[c])||float.IsInfinity(p[c]))return false;return true;}
        static string HashText(string s){using(var h=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(h.ComputeHash(System.Text.Encoding.UTF8.GetBytes(s))).Replace("-","").ToLowerInvariant();}
        static string GeneratedGraph()
        {
            var type=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Unity.ShaderGraph.Editor").GetType("UnityEditor.ShaderGraph.ShaderGraphImporter",true);var method=type.GetMethods(BindingFlags.Static|BindingFlags.NonPublic).Single(m=>m.Name=="GetShaderText"&&m.GetParameters().Length==4&&m.GetParameters()[3].IsOut);object[] args={GraphPath,null,null,null};return (string)method.Invoke(null,args);
        }
        static string AbsoluteIncludes(string text,string file,string resolved)
        {
            return Regex.Replace(text,@"(#include(?:_with_pragmas)?\s+\""([^\""\r\n]+)\"")",m=>{string path=m.Groups[2].Value;if(path.StartsWith("Packages/",StringComparison.Ordinal)||path.StartsWith("Assets/",StringComparison.Ordinal))return m.Value;string full=Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file),path));if(!File.Exists(full))return m.Value;string rel=Path.GetRelativePath(resolved,full).Replace('\\','/');Assert.That(rel.StartsWith("..",StringComparison.Ordinal),Is.False);return m.Value.Replace(path,Package+rel);});
        }
        public static string BuildProbe(string shader,string resolved,int host,int mode)
        {
            bool graph=host==2;string relative=graph?"NBShaders2/ShaderGraph/NBGraphBaseColor.hlsl":(host==0?"Tests/Baseline/Frozen/":"")+"NBShaders2/Shader/HLSL/NBShaderForwardPass.hlsl";string file=Path.Combine(resolved,relative);string hlsl=File.ReadAllText(file);
            if(graph)
            {
                hlsl=hlsl.Replace("out half smoothnessWeight)","out half smoothnessWeight, out half4 nbDTSample)");
                string a="half4 sampled = NBGraphSampleRawMap(map, uv,";Assert.That(hlsl.Contains(a),Is.True);int i=hlsl.IndexOf(a,StringComparison.Ordinal),j=hlsl.IndexOf(';',i);hlsl=hlsl.Insert(j+1,"\n    nbDTSample=sampled;\n");
                hlsl=hlsl.Replace("float3 normalForFeatures = (float3)NormalWS;","half4 nbDTSample=(half4)-2;\n    float3 normalForFeatures = (float3)NormalWS;");
                string calls="lightingSmoothnessWeight);";Assert.That(hlsl.Split(new[]{calls},StringSplitOptions.None).Length,Is.EqualTo(3));
                string output=mode==0?"float4(BumpMapToggle,BumpScale,FxLightMode,1)":mode==1?"float4(nbDTSample)":mode==2?"float4(lightingNormalTS*.5h+.5h,1)":"float4(normalForFeatures*.5+.5,1)";
                hlsl=hlsl.Replace(calls,"lightingSmoothnessWeight,nbDTSample);\n    Out="+output+";return;\n");
            }
            else
            {
                int n=hlsl.IndexOf("//预先处理好法线贴图部分",StringComparison.Ordinal);Assert.That(n,Is.GreaterThanOrEqualTo(0));
                if(mode==0)hlsl=hlsl.Insert(n,"#ifdef _NORMALMAP\n half dtNormal=1;\n#else\n half dtNormal=0;\n#endif\n#ifdef _FX_LIGHT_MODE_BLINN_PHONG\n half dtLight=1;\n#else\n half dtLight=0;\n#endif\n return MakeParticleFragmentOutput(half4(dtNormal,_BumpScale,dtLight,1));\n");
                else if(mode==1){int a=hlsl.IndexOf("half4 normalMapSample",n),b=hlsl.IndexOf(';',a);hlsl=hlsl.Insert(b+1,"\n return MakeParticleFragmentOutput(normalMapSample);\n");}
                else
                {
                    string marker=host==0?"input.normalWSAndAnimBlend.xyz =  normalize(TransformTangentToWorld(normalTS, tangentToWorld));":"input.normalWSAndAnimBlend.xyz = mappedNormalWS;";Assert.That(hlsl.Contains(marker),Is.True);hlsl=hlsl.Replace(marker,marker+"\n return MakeParticleFragmentOutput(half4("+(mode==2?"normalTS":"input.normalWSAndAnimBlend.xyz")+"*.5h+.5h,1));\n");
                }
            }
            hlsl=AbsoluteIncludes(hlsl,file,resolved);string shaderFile=Path.Combine(resolved,host==0?"Tests/Baseline/Frozen/NBShaders2/Shader/NBShader.shader":"NBShaders2/Shader/NBShader.shader");if(!graph)shader=AbsoluteIncludes(shader,shaderFile,resolved);
            string passMarker=graph?"Name \"Universal Forward\"":"Name \"UniversalForward\"";int pass=shader.IndexOf(passMarker,StringComparison.Ordinal);Assert.That(pass,Is.GreaterThanOrEqualTo(0));int begin=shader.IndexOf("HLSLPROGRAM",pass),end=shader.IndexOf("ENDHLSL",begin);string program=shader.Substring(begin,end-begin);
            string include=(graph?"#include_with_pragmas":"#include")+" \""+Package+relative+"\"";Assert.That(program.Split(new[]{include},StringSplitOptions.None).Length,Is.EqualTo(2));program=program.Replace(include,hlsl);shader=shader.Remove(begin,end-begin).Insert(begin,program);int q=shader.IndexOf('"'),qq=shader.IndexOf('"',q+1);return shader.Remove(q+1,qq-q-1).Insert(q+1,"Hidden/Codex/ShaderDebug/NormalDrawTimeH"+host+"P"+mode+Guid.NewGuid().ToString("N"));
        }
    }
}
