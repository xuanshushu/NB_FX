using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace NBFX.Baseline.Tests
{
    // Actual AnimationSheetHelper drives every current/next ST and weight.
    // Exact NB LightMode output via existing directed RenderGraph feature;
    // does not replace automatic NBPostProcess/Controller/Manager acceptance.
    public sealed class G4FlipbookHelperNBPassTests
    {
        const string Package="Packages/com.xuanxuan.nb.fx/", Deferred="NBDeferredDistortPass",Opaque="NBCameraOpaqueDistortPass";
        const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic;
        const BindingFlags Instance=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        const int Size=128,Min=40,Max=88;
        Type helperType;
        [OneTimeSetUp]public void Warm()
        {
            new G4GraphGuiFeatureIntentTests().WarmImportedGraphInRealUrpCamera();
            helperType=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("AnimationSheetHelper",false)).First(t=>t!=null);
        }
        static IEnumerable<TestCaseData> Cases()
        {
            foreach(string route in new[]{Deferred,Opaque})foreach(int mode in new[]{0,3,4,5,6,7})foreach(bool ortho in new[]{true,false})
                yield return new TestCaseData(route,mode,false,ortho).SetName("G4FlipbookHelperGPU_"+route+"_direct"+mode+(ortho?"_ortho":"_perspective"));
            foreach(string route in new[]{Deferred,Opaque})foreach(bool ortho in new[]{true,false})
                yield return new TestCaseData(route,3,true,ortho).SetName("G4FlipbookHelperGPU_"+route+"_shared3"+(ortho?"_ortho":"_perspective"));
        }
        static object F0(string name,params object[] args)=>typeof(G4GraphFlipbookTests).GetMethod(name,Static).Invoke(null,args);
        static void UVP(string name,params object[] args)=>typeof(G4GraphPositionUVTests).GetMethod(name,Static).Invoke(null,args);
        void Set(Component h,string name,object value)=>helperType.GetField(name,Instance).SetValue(h,value);
        T Get<T>(Component h,string name)=>(T)helperType.GetField(name,Instance).GetValue(h);
        void Call(Component h,string name)=>helperType.GetMethod(name,Instance).Invoke(h,null);
        struct Diff {public float max;public int pixels;}
        static Diff Compare(Color[] a,Color[] b)
        {
            var d=new Diff();for(int y=Min;y<Max;y++)for(int x=Min;x<Max;x++)
            {var p=a[y*Size+x]-b[y*Size+x];float m=Mathf.Max(Mathf.Abs(p.r),Mathf.Abs(p.g),Mathf.Abs(p.b),Mathf.Abs(p.a));d.max=Mathf.Max(d.max,m);if(m>0)d.pixels++;}return d;
        }
        [Serializable]sealed class Metrics
        {public string route,scope,api,unity;public int mode,visibleA,visibleB,visibleC,responsePixelsA,responsePixelsB,responsePixelsC;public bool shared,ortho,finite;public float ab,bc,repeat,responseA,responseB,responseC,cycleA,cycleB,cycleC;}
        [TestCaseSource(nameof(Cases))]
        public void ActualHelperCycleOnTwoNBPasses(string route,int mode,bool shared,bool ortho)
        {
            Assert.That(GraphicsSettings.currentRenderPipeline,Is.InstanceOf<UniversalRenderPipelineAsset>());
            Assert.That(GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>().enableRenderCompatibilityMode,Is.False);
            var data=((UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline).rendererDataList[0] as UniversalRendererData;
            Assert.That(data,Is.Not.Null);var nb=data.rendererFeatures.First(f=>f&&f.GetType().FullName=="NBShader.NBPostProcess");bool oldNB=nb.isActive;
            string assetFile=Path.Combine(Path.GetDirectoryName(Application.dataPath),AssetDatabase.GetAssetPath(data));byte[] before=File.ReadAllBytes(assetFile);
            string folder=Path.Combine(Environment.GetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR")??Path.Combine(Path.GetDirectoryName(Application.dataPath),"Temp/NBFXHelperGPU"),"helper-nbpass",route+"-"+(shared?"shared":"direct")+mode+(ortho?"-ortho":"-perspective"));Directory.CreateDirectory(folder);
            var owned=new List<Object>();T Keep<T>(T item)where T:Object{owned.Add(item);return item;}
            Scene scene=EditorSceneManager.NewPreviewScene();var actor=Keep(new GameObject("Actual helper NB Pass",typeof(MeshFilter),typeof(MeshRenderer)));actor.SetActive(false);
            var background=Keep(GameObject.CreatePrimitive(PrimitiveType.Quad));var cameraObject=Keep(new GameObject("Actual helper NB Pass camera"));
            var camera=cameraObject.AddComponent<Camera>();var cameraData=cameraObject.AddComponent<UniversalAdditionalCameraData>();
            var renderer=actor.GetComponent<MeshRenderer>();var mesh=Keep((Mesh)F0("BuildMesh",.25f));actor.GetComponent<MeshFilter>().sharedMesh=mesh;
            var atlas=Keep((Texture2D)F0("MakeAtlas"));var noise=Keep((Texture2D)F0("MakeNoise"));var backgroundMap=Keep((Texture2D)F0("MakeBackground"));
            var materials=new[]{Keep(new Material(AssetDatabase.LoadAssetAtPath<Shader>(Package+"Tests/Baseline/Frozen/NBShaders2/Shader/NBShader.shader"))),Keep(new Material(AssetDatabase.LoadAssetAtPath<Shader>(Package+"NBShaders2/Shader/NBShader.shader"))),Keep(new Material(AssetDatabase.LoadAssetAtPath<Shader>(Package+"NBShaders2/ShaderGraph/NBShaderGraph.shadergraph")))};
            var backdrop=Keep(new Material(Shader.Find("Universal Render Pipeline/Unlit")));var rt=Keep(new RenderTexture(Size,Size,24,RenderTextureFormat.ARGBHalf,RenderTextureReadWrite.Linear));var read=Keep(new Texture2D(Size,Size,TextureFormat.RGBAHalf,false,true));var oldRT=RenderTexture.active;G4ScreenNoiseDirectedFeature directed=null;
            var helper=actor.AddComponent(helperType) as Behaviour;Assert.That(helper,Is.Not.Null);Set(helper,"xSize",2);Set(helper,"ySize",1);Set(helper,"manualPlay",true);Set(helper,"manualPlayePos",.125f);
            try
            {
                foreach(var go in new[]{actor,background,cameraObject})SceneManager.MoveGameObjectToScene(go,scene);camera.scene=scene;
                actor.layer=G4GraphScreenNoiseTests.ForegroundLayer;actor.transform.position=new Vector3(0,0,2);actor.transform.rotation=Quaternion.Euler(0,27,0);actor.transform.localScale=new Vector3(2,2,1);
                renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.sharedMaterial=materials[2];
                background.layer=2;background.transform.position=new Vector3(0,0,1);background.transform.localScale=new Vector3(6,6,1);background.GetComponent<MeshRenderer>().enabled=route!=Deferred;
                backdrop.SetTexture("_BaseMap",backgroundMap);backdrop.SetColor("_BaseColor",Color.white);backdrop.SetFloat("_Cull",0);backdrop.renderQueue=2000;background.GetComponent<MeshRenderer>().sharedMaterial=backdrop;
                camera.orthographic=ortho;camera.orthographicSize=1.5f;camera.fieldOfView=53.13f;camera.nearClipPlane=.1f;camera.farClipPlane=20;camera.transform.position=new Vector3(0,0,5);camera.transform.rotation=Quaternion.Euler(0,180,0);camera.cullingMask=(1<<2)|(1<<actor.layer);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.09f,.15f,.22f,.35f);camera.allowHDR=true;camera.allowMSAA=false;camera.targetTexture=rt;cameraData.requiresColorTexture=true;cameraData.renderPostProcessing=false;
                rt.Create();Assert.That(rt.IsCreated()&&!rt.sRGB,Is.True);nb.SetActive(false);
                directed=ScriptableObject.CreateInstance<G4ScreenNoiseDirectedFeature>();directed.hideFlags=HideFlags.HideAndDontSave;directed.targetCamera=camera;directed.selectedPass=route;directed.Create();directed.SetActive(true);data.rendererFeatures.Add(directed);data.SetDirty();
                foreach(var m in materials)
                {
                    F0("Configure",m,m==materials[2],route,atlas,noise,false,false,.25f);
                    UVP("SetupCoordinates",m,m==materials[2],1);UVP("SetRoute",m,"main",mode,shared,false);
                    Assert.That(m.FindPass(route),Is.GreaterThanOrEqualTo(0));
                }
                foreach(string property in new[]{"_FlipbookBlending","_BaseMap_AnimationSheetBlend_ST","_AnimationSheetHelperBlendIntensity"})Assert.That(materials[2].HasProperty(property),Is.True,property);
                actor.SetActive(true);
                Color[] Capture(string label)=>(Color[])typeof(G4GraphPositionUVTests).GetMethod("Capture",Static).Invoke(null,new object[]{camera,rt,read,folder,label});
                renderer.enabled=false;var empty=Capture("background");var emptyRepeat=Capture("background-repeat");Assert.That(Compare(empty,emptyRepeat).max,Is.Zero);renderer.enabled=true;
                var frames=new List<Color[]>();frames.Add(empty);frames.Add(emptyRepeat);var on=new Color[3][][];var off=new Color[3][];var repeats=new Color[3][][];
                float[] positions={.125f,.375f,.625f,.875f};
                Color[] Snap(int k,float position,bool enabled,string label)
                {
                    var m=materials[k];renderer.sharedMaterial=m;Set(helper,"manualPlayePos",position);Call(helper,"Init");Call(helper,"Update");
                    Assert.That(Get<Material>(helper,"mat"),Is.EqualTo(m));Assert.That(Get<int>(helper,"frameIndex"),Is.EqualTo(position<.5f?0:1));
                    Assert.That(m.GetFloat("_AnimationSheetHelperBlendIntensity"),Is.EqualTo(position==.125f||position==.625f?.25f:.75f));
                    // Only feature intent is set here. The real helper owns its
                    // bit15, current/next ST and weight for every GPU capture.
                    m.SetFloat("_FlipbookBlending",enabled?1:0);if(k<2){if(enabled)m.EnableKeyword("_FLIPBOOKBLENDING_ON");else m.DisableKeyword("_FLIPBOOKBLENDING_ON");}
                    var frame=Capture(label);frames.Add(frame);return frame;
                }
                Snap(2,.125f,true,"C-real-helper-warm");
                for(int k=0;k<3;k++)
                {
                    on[k]=new Color[4][];repeats[k]=new Color[4][];
                    for(int f=0;f<4;f++){on[k][f]=Snap(k,positions[f],true,"ABC"[k]+"-frame"+f);repeats[k][f]=Snap(k,positions[f],true,"ABC"[k]+"-frame"+f+"-repeat");}
                    off[k]=Snap(k,.125f,false,"ABC"[k]+"-flipbook-off");
                }
                var mtr=new Metrics{route=route,mode=mode,shared=shared,ortho=ortho,api=SystemInfo.graphicsDeviceType.ToString(),unity=Application.unityVersion,scope="Actual AnimationSheetHelper 2x1 full frame wrap +UVP direct0/3/4/5/6/7/shared3; exact NB LightModes via existing directed feature. Not automatic NBPostController/Manager, Player or perf evidence.",finite=frames.SelectMany(x=>x).All(p=>new[]{p.r,p.g,p.b,p.a}.All(v=>!float.IsNaN(v)&&!float.IsInfinity(v)))};
                for(int f=0;f<4;f++){mtr.ab=Mathf.Max(mtr.ab,Compare(on[0][f],on[1][f]).max);mtr.bc=Mathf.Max(mtr.bc,Compare(on[1][f],on[2][f]).max);for(int k=0;k<3;k++)mtr.repeat=Mathf.Max(mtr.repeat,Compare(on[k][f],repeats[k][f]).max);}
                mtr.ab=Mathf.Max(mtr.ab,Compare(off[0],off[1]).max);mtr.bc=Mathf.Max(mtr.bc,Compare(off[1],off[2]).max);
                var da=Compare(on[0][0],off[0]);var db=Compare(on[1][0],off[1]);var dc=Compare(on[2][0],off[2]);mtr.responseA=da.max;mtr.responseB=db.max;mtr.responseC=dc.max;mtr.responsePixelsA=da.pixels;mtr.responsePixelsB=db.pixels;mtr.responsePixelsC=dc.pixels;mtr.visibleA=Compare(on[0][0],empty).pixels;mtr.visibleB=Compare(on[1][0],empty).pixels;mtr.visibleC=Compare(on[2][0],empty).pixels;mtr.cycleA=Compare(on[0][0],on[0][2]).max;mtr.cycleB=Compare(on[1][0],on[1][2]).max;mtr.cycleC=Compare(on[2][0],on[2][2]).max;
                File.WriteAllText(Path.Combine(folder,"metrics.json"),JsonUtility.ToJson(mtr,true));Debug.Log("NBFX_HELPER_NBPASS "+JsonUtility.ToJson(mtr));
                Assert.That(mtr.finite,Is.True);Assert.That(mtr.visibleA,Is.GreaterThan(100));Assert.That(mtr.visibleB,Is.GreaterThan(100));Assert.That(mtr.visibleC,Is.GreaterThan(100));Assert.That(mtr.repeat,Is.Zero);Assert.That(mtr.ab+mtr.bc,Is.Zero);
                Assert.That(mtr.responseA,Is.GreaterThan(.005f));Assert.That(mtr.responseB,Is.GreaterThan(.005f));Assert.That(mtr.responseC,Is.GreaterThan(.005f));Assert.That(mtr.responsePixelsA,Is.GreaterThan(20));Assert.That(mtr.responsePixelsB,Is.GreaterThan(20));Assert.That(mtr.responsePixelsC,Is.GreaterThan(20));Assert.That(mtr.cycleA,Is.GreaterThan(.005f));Assert.That(mtr.cycleB,Is.GreaterThan(.005f));Assert.That(mtr.cycleC,Is.GreaterThan(.005f));
            }
            finally
            {
                if(directed){data.rendererFeatures.Remove(directed);Object.DestroyImmediate(directed);}nb.SetActive(oldNB);data.SetDirty();camera.targetTexture=null;RenderTexture.active=oldRT;rt.Release();
                for(int i=owned.Count-1;i>=0;i--)if(owned[i])Object.DestroyImmediate(owned[i]);EditorSceneManager.ClosePreviewScene(scene);Assert.That(File.ReadAllBytes(assetFile),Is.EqualTo(before),"Helper GPU test changed renderer asset");
            }
        }
    }
}
