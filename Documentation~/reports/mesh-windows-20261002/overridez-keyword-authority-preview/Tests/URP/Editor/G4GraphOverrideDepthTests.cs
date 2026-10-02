using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace NBFX.Baseline.Tests
{
    public sealed class G4GraphOverrideDepthTests
    {
        const string Package="Packages/com.xuanxuan.nb.fx/";readonly List<Object> owned=new List<Object>();Shader frozen,current,graph;
        [OneTimeSetUp] public void Warm()
        {new G4GraphGuiFeatureIntentTests().WarmImportedGraphInRealUrpCamera();frozen=AssetDatabase.LoadAssetAtPath<Shader>(Package+"Tests/Baseline/Frozen/NBShaders2/Shader/NBShader.shader");current=AssetDatabase.LoadAssetAtPath<Shader>(Package+"NBShaders2/Shader/NBShader.shader");graph=AssetDatabase.LoadAssetAtPath<Shader>(Package+"NBShaders2/ShaderGraph/NBShaderGraph.shadergraph");}
        T Keep<T>(T x) where T:Object {owned.Add(x);return x;}
        [TearDown] public void Cleanup(){for(int i=owned.Count-1;i>=0;i--)if(owned[i])Object.DestroyImmediate(owned[i]);owned.Clear();}
        static IEnumerable<TestCaseData> Cases()
        {foreach(bool on in new[]{false,true})foreach(float depth in new[]{.05f,3f,7f,50f})foreach(bool ortho in new[]{true,false})yield return new TestCaseData(on,depth,ortho).SetName("G4OverrideZABC_"+(on?"on":"off")+"_d"+depth.ToString(System.Globalization.CultureInfo.InvariantCulture)+(ortho?"_ortho":"_perspective"));}
        [Serializable] sealed class Metrics
        {public string api,unityVersion,scope;public bool finite,orthographic,enabled,keywordAuthorityMismatch,toggleEnabled;public float depth,ab,bc,abControl,bcControl,aRepeat,bRepeat,cRepeat,aResponse,bResponse,cResponse;public int probeVisible,actorVisible;}
        [TestCaseSource(nameof(Cases))]
        public void OverrideZ_RealDepthOcclusionABC(bool enabled,float depth,bool ortho)
        {
            RenderOverrideZABC(enabled,depth,ortho,null);
        }
        static IEnumerable<TestCaseData> KeywordAuthorityCases()
        {
            foreach(bool keywordOn in new[]{false,true})foreach(float depth in new[]{3f,7f})foreach(bool ortho in new[]{true,false})
                yield return new TestCaseData(keywordOn,!keywordOn,depth,ortho).SetName("G4OverrideZKeywordABC_keyword_"+(keywordOn?"on":"off")+"_toggle_"+(keywordOn?"off":"on")+"_d"+depth.ToString(System.Globalization.CultureInfo.InvariantCulture)+(ortho?"_ortho":"_perspective"));
        }
        [TestCaseSource(nameof(KeywordAuthorityCases))]
        public void OverrideZ_KeywordAuthorityABC(bool keywordOn,bool toggleOn,float depth,bool ortho)
        {
            RenderOverrideZABC(keywordOn,depth,ortho,toggleOn);
        }
        void RenderOverrideZABC(bool enabled,float depth,bool ortho,bool? toggleOverride)
        {
            string folder=Path.Combine(Environment.GetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR")??Path.Combine(Path.GetDirectoryName(Application.dataPath),"Temp/NBFXOverrideZ"),(toggleOverride.HasValue?"keyword-"+(enabled?"on":"off")+"-toggle-"+(toggleOverride.Value?"on":"off"):(enabled?"on":"off"))+"-d"+depth.ToString(System.Globalization.CultureInfo.InvariantCulture)+(ortho?"-ortho":"-perspective"));Directory.CreateDirectory(folder);
            var actor=Keep(GameObject.CreatePrimitive(PrimitiveType.Quad));var probe=Keep(GameObject.CreatePrimitive(PrimitiveType.Quad));var cameraObject=Keep(new GameObject("OverrideZ realdepth camera"));var camera=cameraObject.AddComponent<Camera>();cameraObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing=false;
            var scene=SceneManager.GetActiveScene();Assert.That(scene.IsValid()&&scene.isLoaded,Is.True);foreach(var go in new[]{actor,probe,cameraObject})SceneManager.MoveGameObjectToScene(go,scene);camera.scene=scene;
            actor.layer=4;probe.layer=4;actor.transform.localScale=new Vector3(1.5f,1.5f,1);probe.transform.position=new Vector3(0,0,-1);probe.transform.localScale=new Vector3(2,2,1);
            var writer=actor.GetComponent<MeshRenderer>();writer.shadowCastingMode=ShadowCastingMode.Off;writer.receiveShadows=false;var probeRenderer=probe.GetComponent<MeshRenderer>();probeRenderer.shadowCastingMode=ShadowCastingMode.Off;
            var probeMat=Keep(new Material(Shader.Find("Universal Render Pipeline/Unlit")));probeMat.SetColor("_BaseColor",Color.green);probeMat.SetFloat("_Surface",1);probeMat.SetFloat("_ZWrite",0);probeMat.SetFloat("_ZTest",(float)CompareFunction.LessEqual);probeMat.SetFloat("_Cull",0);probeMat.SetFloat("_SrcBlend",1);probeMat.SetFloat("_DstBlend",0);probeMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");probeMat.renderQueue=3001;probeRenderer.sharedMaterial=probeMat;
            camera.orthographic=ortho;camera.orthographicSize=1.4f;camera.fieldOfView=42;camera.nearClipPlane=.1f;camera.farClipPlane=25;camera.transform.position=new Vector3(0,0,5);camera.transform.rotation=Quaternion.LookRotation(Vector3.back);camera.cullingMask=1<<4;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.blue;camera.allowHDR=true;camera.allowMSAA=false;
            var white=Keep(new Texture2D(2,2,TextureFormat.RGBAHalf,false,true));white.SetPixels(Enumerable.Repeat(Color.white,4).ToArray());white.Apply(false);var materials=new[]{Keep(new Material(frozen)),Keep(new Material(current)),Keep(new Material(graph))};
            foreach(var m in materials)
            {
                ConfigureBase(m,m==materials[2],white);if(m.HasProperty("_VAT_Toggle"))m.SetFloat("_VAT_Toggle",0);m.SetColor("_BaseColor",Color.red);m.SetColor("_Color",Color.red);m.renderQueue=3000;m.SetFloat("_ZWrite",1);m.SetFloat("_ZTest",(float)CompareFunction.LessEqual);m.SetFloat("_SrcBlend",1);m.SetFloat("_DstBlend",0);m.SetFloat("_AlphaClip",0);m.DisableKeyword("_ALPHATEST_ON");
            }
            Type guiType=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("NBShaderEditor.NBShaderGraphGUI",false)).First(t=>t!=null);var gui=Activator.CreateInstance(guiType);var validate=guiType.GetMethod("ValidateMaterial");
            void State(Material m,float value)
            {
                m.SetFloat("_OverrideZ_Toggle",(toggleOverride??enabled)?1:0);m.SetFloat("_OverrideZValue",value);
                if(m==materials[2])
                {
                    // Validate owns normal GUI cases. These additional cases
                    // then apply the same explicit runtime keyword to A/B/C.
                    if(toggleOverride.HasValue)
                    {
                        validate.Invoke(gui,new object[]{m});
                        if(enabled)m.EnableKeyword("_OVERRIDE_Z");else m.DisableKeyword("_OVERRIDE_Z");
                    }
                    else validate.Invoke(gui,new object[]{m});Assert.That(m.IsKeywordEnabled("_OVERRIDE_Z"),Is.EqualTo(enabled));m.renderQueue=3000;m.SetFloat("_ZWrite",1);m.SetFloat("_SrcBlend",1);m.SetFloat("_DstBlend",0);m.SetFloat("_SrcBlendAlpha",1);m.SetFloat("_DstBlendAlpha",0);
                }
                else if(enabled)m.EnableKeyword("_OVERRIDE_Z");else m.DisableKeyword("_OVERRIDE_Z");
            }
            var rt=Keep(new RenderTexture(128,128,24,RenderTextureFormat.ARGBHalf,RenderTextureReadWrite.Linear));var read=Keep(new Texture2D(128,128,TextureFormat.RGBAHalf,false,true));var oldRT=RenderTexture.active;camera.targetTexture=rt;rt.Create();
            Color[] Snap(Material m,string name)=>Draw(writer,m,camera,rt,read,folder,name);
            try
            {
                foreach(var m in materials)State(m,depth);writer.sharedMaterial=materials[2];for(int i=0;i<4;i++)camera.Render();Color[][] frames=new Color[3][],repeat=new Color[3][],control=new Color[3][];
                for(int i=0;i<3;i++){State(materials[i],depth);frames[i]=Snap(materials[i],"ABC"[i]+"-main");repeat[i]=Snap(materials[i],"ABC"[i]+"-repeat");State(materials[i],depth<6?7:3);control[i]=Snap(materials[i],"ABC"[i]+"-control");}
                var mtr=new Metrics{api=SystemInfo.graphicsDeviceType.ToString(),unityVersion=Application.unityVersion,scope=(toggleOverride.HasValue?"Runtime keyword/property mismatch. ":"")+"NormalForward conditionalSVDepth andactualgreenprobe occlusion; clampednear/far, disabledkeyword propertyinvariance. NoBackfaceextraPass/VFX/Player/perfclaim",enabled=enabled,keywordAuthorityMismatch=toggleOverride.HasValue,toggleEnabled=toggleOverride??enabled,orthographic=ortho,depth=depth,
                    finite=frames.Concat(repeat).Concat(control).SelectMany(x=>x).All(p=>new[]{p.r,p.g,p.b,p.a}.All(v=>!float.IsNaN(v)&&!float.IsInfinity(v))),ab=Delta(frames[0],frames[1]),bc=Delta(frames[1],frames[2]),abControl=Delta(control[0],control[1]),bcControl=Delta(control[1],control[2]),aRepeat=Delta(frames[0],repeat[0]),bRepeat=Delta(frames[1],repeat[1]),cRepeat=Delta(frames[2],repeat[2]),aResponse=Delta(frames[0],control[0]),bResponse=Delta(frames[1],control[1]),cResponse=Delta(frames[2],control[2]),actorVisible=frames[2].Count(p=>p.r>.5f&&p.g<.1f),probeVisible=frames[2].Count(p=>p.g>.5f&&p.r<.1f)};
                File.WriteAllText(Path.Combine(folder,"metrics.json"),JsonUtility.ToJson(mtr,true));Debug.Log("NBFX_G4_OVERRIDEZ "+JsonUtility.ToJson(mtr));Assert.That(mtr.finite,Is.True);Assert.That(mtr.ab+mtr.bc+mtr.abControl+mtr.bcControl,Is.Zero);Assert.That(mtr.aRepeat+mtr.bRepeat+mtr.cRepeat,Is.Zero);
                if(enabled){Assert.That(mtr.aResponse,Is.GreaterThan(.1f));Assert.That(mtr.bResponse,Is.GreaterThan(.1f));Assert.That(mtr.cResponse,Is.GreaterThan(.1f));if(depth<6)Assert.That(mtr.actorVisible,Is.GreaterThan(150));else Assert.That(mtr.probeVisible,Is.GreaterThan(150));}
                else{Assert.That(mtr.aResponse+mtr.bResponse+mtr.cResponse,Is.Zero);Assert.That(mtr.actorVisible,Is.GreaterThan(150));}
            }
            finally{camera.targetTexture=null;RenderTexture.active=oldRT;rt.Release();}
        }
        static void ConfigureBase(Material mat,bool isGraph,Texture2D baseMap)
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
        static float Delta(Color[] a,Color[] b){float m=0;for(int i=0;i<a.Length;i++){var d=a[i]-b[i];m=Mathf.Max(m,Mathf.Abs(d.r),Mathf.Abs(d.g),Mathf.Abs(d.b),Mathf.Abs(d.a));}return m;}
    }
}
