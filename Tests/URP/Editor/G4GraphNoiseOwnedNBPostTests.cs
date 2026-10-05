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
    // Full original NBPostProcess + production Controller/Manager integration.
    // Observer views existing global RTs; it never selects/draws NB objects.
    public sealed class G4GraphNoiseOwnedNBPostTests
    {
        const string Package="Packages/com.xuanxuan.nb.fx/";
        const string GraphPath=Package+"NBShaders2/ShaderGraph/NBShaderGraph.shadergraph";
        const int Size=128,Layer=2;
        static readonly BindingFlags PublicStatic=BindingFlags.Public|BindingFlags.Static;
        const BindingFlags All=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
        readonly List<G4GraphPersistentGateTierTests> guiHelpers=new List<G4GraphPersistentGateTierTests>();
        static readonly BindingFlags InstanceAny=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
        readonly List<Object> owned=new List<Object>();
        ScriptableRendererData cleanupRenderer;
        NBPostFullRTObserver cleanupObserver;
        Scene cleanupScene;
        RenderTexture cleanupOldRT;
        T Keep<T>(T o) where T:Object {owned.Add(o);return o;}
        static Type RuntimeType(string name)
            => AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name,false)).First(t=>t!=null);
        static IEnumerable<TestCaseData> Cases()
        {
            foreach(string mode in new[]{"deferred","opaque"})
                yield return new TestCaseData(mode,"overlay",true).SetName("G4NoiseScreen_NBPost_ActualOwner_"+(mode=="deferred"?"Mode1":"Mode2")+"_ortho");
        }
        [OneTimeSetUp] public void Preflight()
        {
            Assert.That(Path.GetFullPath(Application.dataPath),Is.EqualTo(Path.GetFullPath(@"D:\UnityProject\NBUnityProject\.utmp\NBFXMeshValidation-20261002\Assets")).IgnoreCase);
            for(int i=0;i<SceneManager.sceneCount;++i){var scene=SceneManager.GetSceneAt(i);Assert.That((scene.name+"/"+scene.path).IndexOf("TAI",StringComparison.OrdinalIgnoreCase),Is.LessThan(0));}
            // Existing installed graphs are already imported; no refresh/import here.
        }
        object Root(params Material[] materials)
        {
            var helper=new G4GraphPersistentGateTierTests();guiHelpers.Add(helper);
            return typeof(G4GraphPersistentGateTierTests).GetMethod("Root",All).Invoke(helper,new object[]{materials});
        }
        static object Sync(object root)=>root.GetType().GetProperty("SyncService",All).GetValue(root);
        static object Call(object target,string name,params object[] arguments)
        {
            var method=target.GetType().GetMethod(name,All);Assert.That(method,Is.Not.Null,name);
            try{return method.Invoke(target,arguments);}catch(TargetInvocationException e){System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e.InnerException??e).Throw();throw;}
        }
        static object Tier()=>Enum.ToObject(RuntimeType("NBShader.NBShaderFeatureTier"),3);
        static string[] Raw()=> (string[])RuntimeType("NBShader.NBShaderFeatureCatalog").GetField("RawKeywords",All).GetValue(null);
        sealed class MaterialSnapshot
        {
            readonly object value;static Type Shared=>typeof(G4GraphGuiFeatureIntentTests).GetNestedType("Snapshot",BindingFlags.NonPublic);
            MaterialSnapshot(object value){this.value=value;}
            public static MaterialSnapshot Read(Material material){var m=Shared.GetMethod("Read",All);Assert.That(m,Is.Not.Null);return new MaterialSnapshot(m.Invoke(null,new object[]{material}));}
            public void Same(Material material,string label){var m=Shared.GetMethod("AssertSame",All);Assert.That(m,Is.Not.Null);m.Invoke(value,new object[]{material,label,Array.Empty<string>()});}
        }
        [Serializable] sealed class OwnershipRecord
        {
            public string scope,mode,api,unity;
            public bool finite,originalNBPostActive,orthographic;
            public float ab,bc,repeat,restore,screenIntensityResponse,tierResponse,maskRepeat,maskRestore,maskMagnitude,deniedMaskMagnitude;
            public int visibleOn,visibleDenied,visibleRestored;
            public string[] phase,mainLightMode,selectedLightMode;
            public bool[] mainEnabled,deferredEnabled,opaqueEnabled;
        }
        static float MagnitudeRG(Color[] pixels)=>pixels.Max(p=>Mathf.Max(Mathf.Abs(p.r),Mathf.Abs(p.g)));
        static void AssertOwnedPasses(Material material,int selectedMode,bool denied,List<bool> main,List<bool> deferred,List<bool> opaque,List<string> mainTags,List<string> selectedTags)
        {
            int forward=material.FindPass("Universal Forward");Assert.That(forward,Is.GreaterThanOrEqualTo(0),material.shader.name);
            string tag=material.shader.FindPassTagValue(0,forward,new ShaderTagId("LightMode")).name;if(string.IsNullOrEmpty(tag))tag="SRPDefaultUnlit";
            Assert.That(tag,Is.EqualTo("SRPDefaultUnlit"),"This bounded test uses the accepted central legacy routing; modern route has separate evidence.");
            string selected=selectedMode==1?"NBDeferredDistortPass":"NBCameraOpaqueDistortPass";int index=material.FindPass(selected);Assert.That(index,Is.GreaterThanOrEqualTo(0));
            string selectedTag=material.shader.FindPassTagValue(0,index,new ShaderTagId("LightMode")).name;Assert.That(selectedTag,Is.EqualTo(selected));
            bool a=material.GetShaderPassEnabled(tag),b=material.GetShaderPassEnabled("NBDeferredDistortPass"),c=material.GetShaderPassEnabled("NBCameraOpaqueDistortPass");
            Assert.That(a,Is.EqualTo(denied));Assert.That(b,Is.EqualTo(!denied&&selectedMode==1));Assert.That(c,Is.EqualTo(!denied&&selectedMode==2));
            Assert.That(material.GetFloat("_NB_GraphScreenPassMigrationComplete"),Is.EqualTo(1));Assert.That(material.GetFloat("_NB_DistortionMode"),Is.EqualTo(selectedMode));Assert.That(material.GetFloat("_DisableMainPassToggle"),Is.EqualTo(1));
            main.Add(a);deferred.Add(b);opaque.Add(c);mainTags.Add(tag);selectedTags.Add(selectedTag);
        }
        static void Field(object o,string name,object value)
        {
            var f=o.GetType().GetField(name,InstanceAny);Assert.That(f,Is.Not.Null,name);
            f.SetValue(o,f.FieldType.IsEnum?Enum.ToObject(f.FieldType,value):value);
        }
        static void Invoke(object o,string name)
        {
            var m=o.GetType().GetMethod(name,InstanceAny);Assert.That(m,Is.Not.Null,name);
            try {m.Invoke(o,null);}catch(TargetInvocationException e){System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e.InnerException??e).Throw();}
        }
        static int Index(Component c)=>(int)c.GetType().GetProperty("index").GetValue(c);
        static object Static(Type t,string n)=>t.GetField(n,PublicStatic).GetValue(null);
        Texture2D Texture(string name,int size,Func<int,int,Color> pixel)
        {
            var t=Keep(new Texture2D(size,size,TextureFormat.RGBAHalf,false,true){name=name});
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)t.SetPixel(x,y,pixel(x,y));t.Apply(false);
            t.filterMode=FilterMode.Point;t.wrapMode=TextureWrapMode.Clamp;return t;
        }
        [Serializable] sealed class Record
        {
            public string scope,api,unity,gpu,mode,effect,downsampling;
            public bool orthographic,finite,controllerLifecycleRestored,renderGraph;
            public float abFinal,bcFinal,abMask,bcMask,abCopy,bcCopy,abOpaque,bcOpaque;
            public float[] repeat,distortionResponse,controllerResponse,controllerUnionResponse,transparentResponse,accumulationResponse;
            public int[] finalVisible;
            public int[] flagsOn,controllerIndices;
            public float[] maskAccumulationResponse,maskStrengthResponse,copyAlphaMin,copyAlphaMax;
        }
        [Serializable] sealed class State
        {
            public string phase,shader;
            public int postFlags,activeControllers,toggleMask;
            public float effectIntensity;
        }
        [Serializable] sealed class Provenance
        {
            public string resolvedPackage,graphSHA256,generatedShaderSHA256,rendererAssetSHA256,pipelineAssetSHA256;
            public string[] sourcePaths,sourceSHA256,requiredGraphInputs;
            public string[] passNames,lightModes;
            public bool originalNBFeatureActive,cameraRenderPostProcessing,volumeRequired,managerControllerOwned;
        }
        static string SHA(string path)
        {using(var h=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(h.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
        static float Delta(Color[] a,Color[] b)
        {float d=0;for(int i=0;i<a.Length;i++)for(int c=0;c<4;c++)d=Mathf.Max(d,Mathf.Abs(a[i][c]-b[i][c]));return d;}
        static int Visible(Color[] a)
        {int n=0;foreach(var p in a)if(Mathf.Max(p.r,Mathf.Max(p.g,p.b))>.15f)n++;return n;}
        static bool Finite(IEnumerable<Color[]> arrays)
        {foreach(var a in arrays)foreach(var p in a)for(int c=0;c<4;c++)if(float.IsNaN(p[c])||float.IsInfinity(p[c]))return false;return true;}

        [TestCaseSource(nameof(Cases))]
        public void ActualOwnershipWithOriginalNBPost(string mode,string effect,bool ortho)
        {
            Assert.That(GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>().enableRenderCompatibilityMode,Is.False,"First slice measures actual RenderGraph; Compatibility is a separate matrix.");
            var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;Assert.That(pipeline,Is.Not.Null);
            var data=pipeline.rendererDataList[0];Assert.That(data,Is.Not.Null);
            var nb=data.rendererFeatures.FirstOrDefault(f=>f&&f.GetType().FullName=="NBShader.NBPostProcess");Assert.That(nb,Is.Not.Null);Assert.That(nb.isActive,Is.True);
            var managerType=RuntimeType("NBShader.PostProcessingManager");var controllerType=RuntimeType("NBShader.PostProcessingController");
            foreach(Type t in new[]{managerType,controllerType})
                Assert.That(Resources.FindObjectsOfTypeAll(t).OfType<Component>().Any(c=>c&&c.gameObject.scene.IsValid()&&c.gameObject.scene.isLoaded),Is.False,"Owned isolation requires no existing loaded Manager/Controller; do not alter user components.");
            string project=Path.GetDirectoryName(Application.dataPath),folder=Path.Combine(Environment.GetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR")??Path.Combine(project,"Temp/NBFXNBPostFull"),mode+"-"+effect+(ortho?"-ortho":"-perspective"));Directory.CreateDirectory(folder);
            string rdPath=Path.Combine(project,AssetDatabase.GetAssetPath(data)),pipelinePath=Path.Combine(project,AssetDatabase.GetAssetPath(pipeline));byte[] rdBytes=File.ReadAllBytes(rdPath),pipelineBytes=File.ReadAllBytes(pipelinePath);
            cleanupRenderer=data;cleanupObserver=null;cleanupScene=default;cleanupOldRT=RenderTexture.active;
            try
            {
            var scene=EditorSceneManager.NewPreviewScene();cleanupScene=scene;var oldRT=RenderTexture.active;
            var observer=Keep(ScriptableObject.CreateInstance<NBPostFullRTObserver>());observer.hideFlags=HideFlags.HideAndDontSave;cleanupObserver=observer;
            var viewShader=AssetDatabase.LoadAssetAtPath<Shader>(Package+"Tests/URP/Editor/G2MaskView.shader");Assert.That(viewShader&&viewShader.isSupported,Is.True);
            observer.material=Keep(new Material(viewShader));observer.Create();observer.SetActive(false);data.rendererFeatures.Add(observer);data.SetDirty();
            var target=Keep(new RenderTexture(Size,Size,24,RenderTextureFormat.ARGBHalf,RenderTextureReadWrite.Linear));var read=Keep(new Texture2D(Size,Size,TextureFormat.RGBAHalf,false,true));
            var cameraGO=Keep(new GameObject("NBPost full primary camera"));SceneManager.MoveGameObjectToScene(cameraGO,scene);var camera=cameraGO.AddComponent<Camera>();camera.scene=scene;
            camera.orthographic=ortho;camera.orthographicSize=2;camera.fieldOfView=45;camera.nearClipPlane=.1f;camera.farClipPlane=20;camera.transform.position=new Vector3(0,0,8);camera.transform.rotation=Quaternion.Euler(0,180,0);
            camera.cullingMask=1<<Layer;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.03f,.05f,.1f,.125f);camera.allowHDR=true;camera.allowMSAA=false;camera.targetTexture=target;
            var cameraData=cameraGO.AddComponent<UniversalAdditionalCameraData>();cameraData.SetRenderer(0);cameraData.requiresColorTexture=true;cameraData.renderPostProcessing=false;target.Create();Assert.That(target.IsCreated()&&!target.sRGB,Is.True);
            var gradient=Texture("NBPost full gradient",64,(x,y)=>new Color(.08f+.8f*x/63,.1f+.7f*y/63,.15f+.55f*((x+2*y)%64)/63,1));
            var noise=Texture("NBPost full constant noise",2,(x,y)=>new Color(.75f,.5f,0,1));var mask=Texture("NBPost full mask",2,(x,y)=>Color.white);
            var overlay=Texture("NBPost real Controller overlay",2,(x,y)=>new Color(.65f,.15f,.8f,.75f));
            var opaqueBG=Keep(GameObject.CreatePrimitive(PrimitiveType.Quad));var transparentBG=Keep(GameObject.CreatePrimitive(PrimitiveType.Quad));var actors=new[]{Keep(GameObject.CreatePrimitive(PrimitiveType.Quad)),Keep(GameObject.CreatePrimitive(PrimitiveType.Quad))};
            foreach(var go in new[]{opaqueBG,transparentBG,actors[0],actors[1]}){SceneManager.MoveGameObjectToScene(go,scene);go.layer=Layer;go.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;go.GetComponent<MeshRenderer>().receiveShadows=false;}
            var bgShader=Shader.Find("Universal Render Pipeline/Unlit");Assert.That(bgShader,Is.Not.Null);var bgMat=Keep(new Material(bgShader));var transMat=Keep(new Material(bgShader));
            foreach(var m in new[]{bgMat,transMat}){m.SetTexture("_BaseMap",gradient);m.SetFloat("_Cull",0);m.SetFloat("_ZTest",4);m.SetFloat("_ZWrite",0);}
            bgMat.SetColor("_BaseColor",Color.white);bgMat.SetFloat("_Surface",0);bgMat.SetFloat("_SrcBlend",1);bgMat.SetFloat("_DstBlend",0);bgMat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");bgMat.renderQueue=2000;
            transMat.SetColor("_BaseColor",new Color(.75f,.85f,.65f,.35f));transMat.SetFloat("_Surface",1);transMat.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);transMat.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);transMat.SetFloat("_SrcBlendAlpha",1);transMat.SetFloat("_DstBlendAlpha",(float)BlendMode.OneMinusSrcAlpha);transMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");transMat.renderQueue=2900;
            opaqueBG.transform.position=new Vector3(-1.5f,0,1);opaqueBG.transform.localScale=new Vector3(3,6,1);opaqueBG.GetComponent<MeshRenderer>().sharedMaterial=bgMat;
            transparentBG.transform.position=new Vector3(0,0,1.5f);transparentBG.transform.localScale=new Vector3(6,6,1);transparentBG.GetComponent<MeshRenderer>().sharedMaterial=transMat;
            var writer=actors[0].GetComponent<MeshRenderer>();var writer2=actors[1].GetComponent<MeshRenderer>();actors[0].transform.position=new Vector3(-.25f,0,2);actors[1].transform.position=new Vector3(.25f,0,2.1f);foreach(var go in actors)go.transform.localScale=new Vector3(2,2,1);
            string pass=mode=="deferred"?"NBDeferredDistortPass":"NBCameraOpaqueDistortPass";
            var materials=new[]{Keep(new Material(AssetDatabase.LoadAssetAtPath<Shader>(Package+"Tests/Baseline/Frozen/NBShaders2/Shader/NBShader.shader"))),Keep(new Material(AssetDatabase.LoadAssetAtPath<Shader>(Package+"NBShaders2/Shader/NBShader.shader"))),Keep(new Material(AssetDatabase.LoadAssetAtPath<Shader>(GraphPath)))};
            var configure=typeof(G4GraphScreenNoiseTests).GetMethod("Configure",BindingFlags.Static|BindingFlags.NonPublic);Assert.That(configure,Is.Not.Null);
            foreach(var m in materials)
            {configure.Invoke(null,new object[]{m,m==materials[2],noise,mask,pass,"noise-a-half"});m.SetFloat("_AlphaAll",.5f);m.SetFloat(m==materials[2]?"_NB_DistortionIntensity":"_ScreenDistortIntensity",.35f);}
            var second=materials.Select(m=>Keep(new Material(m))).ToArray();foreach(var m in second){m.SetFloat("_AlphaAll",.25f);m.SetVector("_DistortionDirection",new Vector4(-.25f,.4f,0,0));m.renderQueue=3001;}
            // Configure may set fixture pass defaults once. From here the actual product transaction is authoritative.
            foreach(var m in new[]{materials[2],second[2]}){m.SetFloat("_NB_GraphGUIStateVersion",2);m.SetFloat("_NBShaderFeatureTier",3);m.SetFloat("_NB_GraphScreenPassMigrationComplete",0);G4SpecDebugFixture.Validate(m);}
            var root=Root(materials[2],second[2]);var sync=Sync(root);Assert.That(Call(root,"InitializeGraphNoiseInputs"),Is.True);
            int selectedMode=mode=="deferred"?1:2;Assert.That(Call(sync,"TryAdoptGraphScreenEdit",selectedMode,(bool?)true),Is.True);
            var materialSnapshot=MaterialSnapshot.Read(materials[2]);var secondSnapshot=MaterialSnapshot.Read(second[2]);
            var postField=nb.GetType().GetField("NBPostProcessMaterial",PublicStatic);Assert.That(postField,Is.Not.Null);
            Material post=null,postSnapshot=null;Dictionary<FieldInfo,object> oldStatics=null;Component manager=null;var controllers=new Component[2];var controllerGOs=new GameObject[2];var states=new List<State>();var payloads=new List<Color[]>();
            int expectedEffect=effect.Contains("flash")?8:4;bool flash=effect.Contains("flash");bool multiCamera=effect.StartsWith("multicamera",StringComparison.Ordinal);Camera secondary=null;
            try
            {
                // Warm renderer after observer list registration, before singleton
                // snapshot. Rebuilding NB feature must not invalidate live flags.
                writer.enabled=writer2.enabled=false;for(int i=0;i<4;i++)camera.Render();
                post=postField.GetValue(null)as Material;Assert.That(post&&!AssetDatabase.Contains(post),Is.True,"Only modify original runtime Uber material, never an asset.");postSnapshot=Keep(new Material(post));
                oldStatics=managerType.GetFields(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static).Where(f=>!f.IsLiteral&&!f.IsInitOnly).ToDictionary(f=>f,f=>f.GetValue(null));
                for(int i=0;i<2;i++)
                {
                    controllerGOs[i]=Keep(new GameObject("Owned NB Controller "+i));controllerGOs[i].SetActive(false);SceneManager.MoveGameObjectToScene(controllerGOs[i],scene);
                    controllers[i]=controllerGOs[i].AddComponent(controllerType);
                    Field(controllers[i],"overlayTextureToggle",!flash);Field(controllers[i],"overlayTexture",overlay);Field(controllers[i],"overlayTextureSt",new Vector4(1,1,0,0));Field(controllers[i],"overlayTextureAnim",Vector2.zero);Field(controllers[i],"overlayTexturePolarCoordMode",false);Field(controllers[i],"overlayTextureBlendMode",1);Field(controllers[i],"overlayTextureIntensity",i==0?.4f:.7f);
                    Field(controllers[i],"flashToggle",flash);Field(controllers[i],"flashTextureToggle",false);Field(controllers[i],"flashIntensity",i==0?.3f:.65f);Field(controllers[i],"flashInvertIntensity",0f);Field(controllers[i],"flashContrast",.25f);Field(controllers[i],"flashGradientRange",.25f);Field(controllers[i],"flashColor",new Color(.05f,.02f,.15f,1));Field(controllers[i],"blackFlashColor",Color.white);
                    controllerGOs[i].SetActive(true);
                }
                manager=(Component)managerType.GetProperty("Instance",PublicStatic).GetValue(null);Assert.That(manager,Is.Not.Null);SceneManager.MoveGameObjectToScene(manager.gameObject,scene);
                int idx0=Index(controllers[0]),idx1=Index(controllers[1]);Assert.That(idx0!=idx1&&idx0>=0&&idx1<31,Is.True);
                void Tick()
                {for(int warm=0;warm<3;warm++){foreach(var c in controllers)if(c&&c.gameObject.activeInHierarchy)Invoke(c,"ControllerEditorUpdate");Invoke(manager,"EditorUpdate");}}
                State StateAt(string phase)
                {
                    var r=new State{phase=phase,shader=post.shader.name,postFlags=post.GetInteger("_NBPostProcessFlags"),activeControllers=(int)managerType.GetField("_controllerIndexFlags",InstanceAny).GetValue(manager),toggleMask=(int)Static(managerType,flash?"flashToggles":"overlayTextureToggles"),effectIntensity=post.GetFloat(flash?"_FlashIntensity":"_TextureOverlayIntensity")};states.Add(r);return r;
                }
                void Effects(bool enabled)
                {foreach(var c in controllers){Field(c,flash?"flashToggle":"overlayTextureToggle",enabled);}Tick();}
                Color[] Snap(int which,string label,int view=-1,bool two=true,Camera useCamera=null)
                {
                    writer.sharedMaterial=materials[which];writer2.sharedMaterial=second[which];writer.enabled=true;writer2.enabled=two;observer.view=view;observer.SetActive(view>=0);
                    var cam=useCamera?useCamera:camera;for(int n=0;n<4;n++){Tick();cam.Render();}
                    Assert.That(postField.GetValue(null),Is.SameAs(post),"Renderer rebuild replaced runtime Uber during a capture.");
                    var old=RenderTexture.active;RenderTexture.active=target;read.ReadPixels(new Rect(0,0,Size,Size),0,0,false);read.Apply(false,false);var px=read.GetPixels();RenderTexture.active=old;
                    using(var stream=File.Create(Path.Combine(folder,label+".rgba32f")))using(var bw=new BinaryWriter(stream))foreach(var c in px){bw.Write(c.r);bw.Write(c.g);bw.Write(c.b);bw.Write(c.a);}payloads.Add(px);return px;
                }
                // No directed LightMode feature: actual original NBPost remains active.
                Effects(false);Tick();StateAt("Controllers off: only real screen distortion");
                var on=new Color[3][];var repeats=new Color[3][];var strengthZero=new Color[3][];
                for(int i=0;i<3;++i)
                {
                    on[i]=Snap(i,"ABC"[i]+"-owner-on");repeats[i]=Snap(i,"ABC"[i]+"-owner-repeat");
                    materials[i].SetFloat(i==2?"_NB_DistortionIntensity":"_ScreenDistortIntensity",0);second[i].SetFloat(i==2?"_NB_DistortionIntensity":"_ScreenDistortIntensity",0);
                    strengthZero[i]=Snap(i,"ABC"[i]+"-screen-intensity-zero");
                    materials[i].SetFloat(i==2?"_NB_DistortionIntensity":"_ScreenDistortIntensity",.35f);second[i].SetFloat(i==2?"_NB_DistortionIntensity":"_ScreenDistortIntensity",.35f);
                }
                var main=new List<bool>();var deferred=new List<bool>();var opaque=new List<bool>();var mainTags=new List<string>();var selectedTags=new List<string>();
                AssertOwnedPasses(materials[2],selectedMode,false,main,deferred,opaque,mainTags,selectedTags);
                var ownedMask=Snap(2,"C-owned-mask",0);var maskRepeat=Snap(2,"C-owned-mask-repeat",0);
                Assert.That(Call(sync,"TryApplyGraphSupportedGateTier",Tier(),Raw().Where(k=>k!="_SCREEN_DISTORT_MODE").ToArray()),Is.True);
                AssertOwnedPasses(materials[2],selectedMode,true,main,deferred,opaque,mainTags,selectedTags);
                var denied=Snap(2,"C-owned-tier-denied");var deniedRepeat=Snap(2,"C-owned-tier-denied-repeat");var deniedMask=Snap(2,"C-owned-tier-denied-mask",0);
                Assert.That(Call(sync,"TryApplyGraphSupportedGateTier",Tier(),Raw()),Is.True);
                AssertOwnedPasses(materials[2],selectedMode,false,main,deferred,opaque,mainTags,selectedTags);
                var restored=Snap(2,"C-owned-tier-restored");var restoredMask=Snap(2,"C-owned-tier-restored-mask",0);
                materialSnapshot.Same(materials[2],"Actual owned Tier restoration preserves all raw/aliases/keywords/passes");secondSnapshot.Same(second[2],"Same multi-material transaction completely restores second writer");
                var r=new OwnershipRecord{scope="Single orthographic camera, mode1/2, original active NBPost RenderGraph. Real Root/Sync adoption then owned Tier deny/restore; observer only reads existing global RT. No pass writes after adoption; no modern-route/Player/perf/Gate claim.",mode=mode,api=SystemInfo.graphicsDeviceType.ToString(),unity=Application.unityVersion,finite=Finite(payloads),originalNBPostActive=nb.isActive,orthographic=ortho,
                    ab=Delta(on[0],on[1]),bc=Delta(on[1],on[2]),repeat=Enumerable.Range(0,3).Select(i=>Delta(on[i],repeats[i])).Concat(new[]{Delta(denied,deniedRepeat)}).Max(),restore=Delta(on[2],restored),screenIntensityResponse=Delta(on[2],strengthZero[2]),tierResponse=Delta(on[2],denied),maskRepeat=Delta(ownedMask,maskRepeat),maskRestore=Delta(ownedMask,restoredMask),maskMagnitude=MagnitudeRG(ownedMask),deniedMaskMagnitude=MagnitudeRG(deniedMask),visibleOn=Visible(on[2]),visibleDenied=Visible(denied),visibleRestored=Visible(restored),phase=new[]{"adopted","owned Tier denied","owned Tier restored"},mainLightMode=mainTags.ToArray(),selectedLightMode=selectedTags.ToArray(),mainEnabled=main.ToArray(),deferredEnabled=deferred.ToArray(),opaqueEnabled=opaque.ToArray()};
                File.WriteAllText(Path.Combine(folder,"metrics.json"),JsonUtility.ToJson(r,true));File.WriteAllText(Path.Combine(folder,"controller-states.json"),JsonUtility.ToJson(new States{states=states.ToArray()},true));
                Assert.That(r.finite,Is.True);Assert.That(r.originalNBPostActive,Is.True);Assert.That(r.ab+r.bc,Is.Zero);Assert.That(r.repeat+r.restore+r.maskRepeat+r.maskRestore,Is.Zero);
                Assert.That(r.visibleOn,Is.GreaterThan(150));Assert.That(r.visibleDenied,Is.GreaterThan(150));Assert.That(r.visibleRestored,Is.GreaterThan(150));
                Assert.That(r.screenIntensityResponse,Is.GreaterThan(.005f),"Original NBPost default chain must genuinely consume the selected screen pass");Assert.That(r.tierResponse,Is.GreaterThan(.005f),"Real owned Tier transaction must change actual default-chain output");
                Assert.That(r.deniedMaskMagnitude,Is.Zero,"Tier denies NBDeferred submission, rather than comparing two empty masks as success");
                if(selectedMode==1)Assert.That(r.maskMagnitude,Is.GreaterThan(.005f),"Deferred owner must produce a nonzero actual NBPost mask");else Assert.That(r.maskMagnitude,Is.Zero,"Mode2 must not accidentally submit NBDeferred");
                var package=UnityEditor.PackageManager.PackageInfo.FindForAssetPath(GraphPath);string[] inputs={"NBPostProcessing/Runtime/NBPostProcess.cs","NBPostProcessing/Runtime/PostProcessingController.cs","NBPostProcessing/Runtime/PostProcessingManager.cs","NBPostProcessing/Runtime/DisturbanceMaskRenderPass.cs","NBPostProcessing/Runtime/ScreenColorRenderPass.cs","NBPostProcessing/Runtime/RenderCameraOpaqueDistortObjectPass.cs","NBPostProcessing/Runtime/NBPostProcessRenderPass.cs","NBPostProcessing/Shader/NBPostProcessUber.shader","Tests/URP/Editor/G4GraphNoiseOwnedNBPostTests.cs"};
                var sg=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Unity.ShaderGraph.Editor").GetType("UnityEditor.ShaderGraph.ShaderGraphImporter",true);var get=sg.GetMethods(BindingFlags.Static|BindingFlags.NonPublic).Single(m=>m.Name=="GetShaderText"&&m.GetParameters().Length==4&&m.GetParameters()[3].IsOut);object[] args={GraphPath,null,null,null};string generated=(string)get.Invoke(null,args);string genPath=Path.Combine(folder,"generated-current-graph.shader");File.WriteAllText(genPath,generated);
                var graph=materials[2].shader;var passNames=new List<string>();var lightModes=new List<string>();for(int i=0;i<materials[2].passCount;i++){passNames.Add(materials[2].GetPassName(i));lightModes.Add(graph.FindPassTagValue(0,i,new ShaderTagId("LightMode")).name);}
                string[] required={"_NB_DistortionMode","_NB_DistortionIntensity","_NB_Flags0Lo16","_NB_Flags1Lo16","_noisemapEnabled","_NoiseMap"};foreach(var n in required)Assert.That(materials[2].HasProperty(n),Is.True,n);
                var provenance=new Provenance{resolvedPackage=package.resolvedPath,graphSHA256=SHA(Path.Combine(package.resolvedPath,"NBShaders2/ShaderGraph/NBShaderGraph.shadergraph")),generatedShaderSHA256=SHA(genPath),rendererAssetSHA256=SHA(rdPath),pipelineAssetSHA256=SHA(pipelinePath),sourcePaths=inputs,sourceSHA256=inputs.Select(x=>SHA(Path.Combine(package.resolvedPath,x))).ToArray(),requiredGraphInputs=required,passNames=passNames.ToArray(),lightModes=lightModes.ToArray(),originalNBFeatureActive=nb.isActive,cameraRenderPostProcessing=cameraData.renderPostProcessing,volumeRequired=false,managerControllerOwned=true};File.WriteAllText(Path.Combine(folder,"provenance.json"),JsonUtility.ToJson(provenance,true));
            }
            finally
            {
                foreach(var go in controllerGOs)if(go)go.SetActive(false);
                if(manager)Object.DestroyImmediate(manager.gameObject);
                if(oldStatics!=null)foreach(var kv in oldStatics)kv.Key.SetValue(null,kv.Value);
                if(post&&postSnapshot)post.CopyPropertiesFromMaterial(postSnapshot);
                camera.targetTexture=null;if(secondary)secondary.targetTexture=null;RenderTexture.active=oldRT;target.Release();
                data.rendererFeatures.Remove(observer);data.SetDirty();
                for(int i=owned.Count-1;i>=0;i--)if(owned[i])Object.DestroyImmediate(owned[i]);owned.Clear();EditorSceneManager.ClosePreviewScene(scene);
                Assert.That(File.ReadAllBytes(rdPath),Is.EqualTo(rdBytes),"Test wrote RendererData asset");Assert.That(File.ReadAllBytes(pipelinePath),Is.EqualTo(pipelineBytes),"Test wrote pipeline asset");
            }
            }
            finally
            {
                // Guard partial setup failures as well as the successful inner
                // integration scope. All listed objects were created by this test.
                foreach(var helper in guiHelpers)helper.Cleanup();guiHelpers.Clear();
                if(cleanupObserver&&cleanupRenderer)
                {cleanupRenderer.rendererFeatures.Remove(cleanupObserver);cleanupRenderer.SetDirty();}
                for(int i=owned.Count-1;i>=0;--i)if(owned[i])Object.DestroyImmediate(owned[i]);
                owned.Clear();RenderTexture.active=cleanupOldRT;
                if(cleanupScene.IsValid()&&cleanupScene.isLoaded)EditorSceneManager.ClosePreviewScene(cleanupScene);
                cleanupObserver=null;cleanupRenderer=null;cleanupScene=default;
            }
        }
        [Serializable] sealed class States {public State[] states;}
    }
}
