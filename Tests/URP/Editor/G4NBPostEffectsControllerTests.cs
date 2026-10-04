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
    public sealed class G4NBPostEffectsControllerTests
    {
        const string Package="Packages/com.xuanxuan.nb.fx/";
        const string GraphPath=Package+"NBShaders2/ShaderGraph/NBShaderGraph.shadergraph";
        const int Size=128,Layer=2;
        static readonly BindingFlags PublicStatic=BindingFlags.Public|BindingFlags.Static;
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
            foreach(string mode in new[]{"opaque","deferred"})foreach(string effect in new[]{"ca","speed-polar","speed-screen","radial","vignette","overlay-multiply","overlay-polar","overlay-mask","overlay-polar-mask","flash-texture","flash-texture-polar","combo-ca-radial-speed","combo-overlay-flash"})foreach(bool ortho in new[]{true,false})
                yield return new TestCaseData(mode,effect,ortho).SetName("NBPostEffectsABC_"+mode+"_"+effect+(ortho?"_ortho":"_perspective"));
        }
        [OneTimeSetUp] public void ImportGraph()
            => AssetDatabase.ImportAsset(GraphPath,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
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
            public float[] repeat,distortionResponse,controllerResponse,controllerUnionResponse,transparentResponse,accumulationResponse,managerDisableResponse;
            public BranchResponse[] independentBranches;
            public int[] finalVisible;
            public int[] flagsOn,controllerIndices;
            public float[] maskAccumulationResponse,maskStrengthResponse,copyAlphaMin,copyAlphaMax;
        }
        [Serializable] sealed class BranchResponse {public string branch;public float[] response;}
        [Serializable] sealed class State
        {
            public string phase,shader;
            public int postFlags,activeControllers,toggleMask;public int[] activeEffectMasks;
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
        {foreach(var a in arrays)foreach(var p in a)for(int c=0;c<4;c++)if(float.IsNaN(p[c])||float.IsInfinity(p[c]))return false;float invalid=BitConverter.ToSingle(new byte[]{0xcd,0xcd,0xcd,0xcd},0);foreach(var a in arrays)if(a.Length==0||a.All(p=>p.r==invalid&&p.g==invalid&&p.b==invalid&&p.a==invalid))return false;return true;}

        [TestCaseSource(nameof(Cases))]
        public void EffectsCatalogueThroughProductionControllerManagerABC(string mode,string effect,bool ortho)
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
            var overlay=Texture("NBPost catalogue actual overlay",8,(x,y)=>new Color(.15f+.6f*x/7f,.2f+.65f*y/7f,.2f+.3f*((x+y)%3),.75f));
            var speedTexture=Texture("NBPost catalogue speed",8,(x,y)=>new Color(.2f+.6f*x/7f,.15f+.7f*y/7f,.5f,1));
            var overlayMask=Texture("NBPost catalogue mask",8,(x,y)=>new Color(.15f+.8f*y/7f,0,0,1));
            var flashTexture=Texture("NBPost catalogue flash",8,(x,y)=>new Color(.1f+.55f*x/7f+.3f*y/7f,.5f,.5f,1));
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
            var postField=nb.GetType().GetField("NBPostProcessMaterial",PublicStatic);Assert.That(postField,Is.Not.Null);
            Material post=null,postSnapshot=null;Dictionary<FieldInfo,object> oldStatics=null;Component manager=null;var controllers=new Component[2];var controllerGOs=new GameObject[2];var states=new List<State>();var payloads=new List<Color[]>();
            int expectedEffect=RequiredBits(effect);bool multiCamera=false;Camera secondary=null;var branches=new List<BranchResponse>();
            try
            {
                // Warm renderer after observer list registration, before singleton
                // snapshot. Rebuilding NB feature must not invalidate live flags.
                writer.enabled=writer2.enabled=false;for(int i=0;i<4;i++)camera.Render();
                post=postField.GetValue(null)as Material;Assert.That(post&&!AssetDatabase.Contains(post),Is.True,"Only modify original runtime Uber material, never an asset.");postSnapshot=Keep(new Material(post));
                oldStatics=managerType.GetFields(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static).Where(f=>!f.IsLiteral&&!f.IsInitOnly).ToDictionary(f=>f,f=>f.GetValue(null));
                void ConfigureController(Component c,int i)
                {
                    foreach(string toggle in AllToggles)Field(c,toggle,false);
                    Field(c,"customScreenCenterPos",new Vector2(.37f,.61f));
                    Field(c,"caFromDistort",.7f);Field(c,"chromaticAberrationIntensity",i==0?.3f:.65f);Field(c,"chromaticAberrationPos",.08f);Field(c,"chromaticAberrationRange",1.2f);
                    Field(c,"distortScreenUVMode",effect=="speed-screen");Field(c,"distortSpeedTexture",speedTexture);Field(c,"distortTextureMidValue",.2f);Field(c,"distortSpeedTexSt",new Vector4(2,1,.125f,.125f));
                    Field(c,"distortSpeedIntensity",i==0?.3f:.65f);Field(c,"distortSpeedPosition",.05f);Field(c,"distortSpeedRange",1f);Field(c,"distortSpeedMoveSpeedX",0f);Field(c,"distortSpeedMoveSpeed",0f);
                    Field(c,"radialBlurFromDistort",.7f);Field(c,"radialBlurSampleCount",6);Field(c,"radialBlurIntensity",i==0?.3f:.65f);Field(c,"radialBlurPos",.05f);Field(c,"radialBlurRange",1.2f);
                    Field(c,"vignetteColor",new Color(.03f,.04f,.08f,1));Field(c,"vignetteIntensity",i==0?.3f:.65f);Field(c,"vignetteRoundness",1f);Field(c,"vignetteSmothness",.35f);Field(c,"vignetteFill",.15f);
                    Field(c,"overlayTexture",overlay);Field(c,"overlayTextureSt",new Vector4(1.5f,1.25f,.125f,.125f));Field(c,"overlayTextureAnim",Vector2.zero);
                    Field(c,"overlayTexturePolarCoordMode",effect.Contains("polar")&&effect.StartsWith("overlay")||effect=="combo-overlay-flash");Field(c,"overlayTextureBlendMode",0);Field(c,"overlayTextureIntensity",i==0?.4f:.7f);
                    Field(c,"overlayMaskTexture",effect.Contains("mask")||effect=="combo-overlay-flash"?overlayMask:null);Field(c,"overlayMaskTextureSt",new Vector4(1,1,0,0));
                    Field(c,"flashTextureToggle",true);Field(c,"flashTexture",flashTexture);Field(c,"flashTextureScaleOffset",new Vector4(1.5f,1.25f,.125f,.125f));Field(c,"flashTexturePolarCoordMode",effect=="flash-texture-polar"||effect=="combo-overlay-flash");
                    Field(c,"flashVec",Vector2.zero);Field(c,"flashVecZW",new Vector2(.1f,.4f));Field(c,"flashTextureMaskIntensity",.6f);Field(c,"flashTextureIntensity",.75f);Field(c,"flashDeSaturateIntensity",1f);
                    Field(c,"flashIntensity",i==0?.3f:.65f);Field(c,"flashInvertIntensity",0f);Field(c,"flashContrast",.4f);Field(c,"flashGradientRange",.2f);Field(c,"flashColor",new Color(.05f,.02f,.15f,1));Field(c,"blackFlashColor",Color.white);
                    foreach(string toggle in ActiveToggles(effect))Field(c,toggle,true);
                }
                for(int i=0;i<2;i++)
                {
                    controllerGOs[i]=Keep(new GameObject("Owned NB Controller "+i));controllerGOs[i].SetActive(false);SceneManager.MoveGameObjectToScene(controllerGOs[i],scene);
                    controllers[i]=controllerGOs[i].AddComponent(controllerType);
                    ConfigureController(controllers[i],i);
                    controllerGOs[i].SetActive(true);
                }
                manager=(Component)managerType.GetProperty("Instance",PublicStatic).GetValue(null);Assert.That(manager,Is.Not.Null);SceneManager.MoveGameObjectToScene(manager.gameObject,scene);
                int idx0=Index(controllers[0]),idx1=Index(controllers[1]);Assert.That(idx0!=idx1&&idx0>=0&&idx1<31,Is.True);
                void Tick()
                {for(int warm=0;warm<3;warm++){foreach(var c in controllers)if(c&&((Behaviour)c).isActiveAndEnabled)Invoke(c,"ControllerEditorUpdate");if(manager&&((Behaviour)manager).isActiveAndEnabled)Invoke(manager,"EditorUpdate");}}
                State StateAt(string phase)
                {
                    var r=new State{phase=phase,shader=post.shader.name,postFlags=post.GetInteger("_NBPostProcessFlags"),activeControllers=(int)managerType.GetField("_controllerIndexFlags",InstanceAny).GetValue(manager),toggleMask=(int)Static(managerType,MaskName(PrimaryToggle(effect))),effectIntensity=PrimaryIntensity(post,effect),activeEffectMasks=ActiveToggles(effect).Select(t=>(int)Static(managerType,MaskName(t))).ToArray()};states.Add(r);return r;
                }
                void Effects(bool enabled)
                {foreach(var c in controllers)if(c)foreach(string toggle in ActiveToggles(effect))Field(c,toggle,enabled);Tick();}
                Color[] Snap(int which,string label,int view=-1,bool two=true,Camera useCamera=null)
                {
                    writer.sharedMaterial=materials[which];writer2.sharedMaterial=second[which];writer.enabled=true;writer2.enabled=two;observer.view=view;observer.SetActive(view>=0);
                    var cam=useCamera?useCamera:camera;for(int n=0;n<4;n++){Tick();cam.Render();}
                    Assert.That(postField.GetValue(null),Is.SameAs(post),"Renderer rebuild replaced runtime Uber during a capture.");
                    var old=RenderTexture.active;RenderTexture.active=target;read.ReadPixels(new Rect(0,0,Size,Size),0,0,false);read.Apply(false,false);var px=read.GetPixels();RenderTexture.active=old;
                    using(var stream=File.Create(Path.Combine(folder,label+".rgba32f")))using(var bw=new BinaryWriter(stream))foreach(var c in px){bw.Write(c.r);bw.Write(c.g);bw.Write(c.b);bw.Write(c.a);}payloads.Add(px);return px;
                }
                Tick();ValidateModifierState(post,effect);var fullState=StateAt("two Controllers on");Assert.That(fullState.toggleMask,Is.EqualTo((1<<idx0)|(1<<idx1)));Assert.That(fullState.postFlags&1,Is.Not.Zero);Assert.That(fullState.postFlags&expectedEffect,Is.EqualTo(expectedEffect));Assert.That(fullState.effectIntensity,Is.EqualTo(ExpectedIntensity(effect,true)));Assert.That(fullState.activeEffectMasks.All(v=>v==((1<<idx0)|(1<<idx1))),Is.True);
                var on=new Color[3][];var repeat=new Color[3][];var off=new Color[3][];var strength0=new Color[3][];var one=new Color[3][];var noTransparent=new Color[3][];var masks=new Color[3][];var maskOne=new Color[3][];var maskZero=new Color[3][];var copies=new Color[3][];var opaques=new Color[3][];var controllerSingle=new Color[3][];var managerDisabled=new Color[3][];
                for(int i=0;i<3;i++)
                {
                    Effects(true);on[i]=Snap(i,"ABC"[i]+"-final");repeat[i]=Snap(i,"ABC"[i]+"-repeat");
                    Effects(false);off[i]=Snap(i,"ABC"[i]+"-controller-off");var offState=StateAt("toggle off "+i);Assert.That(offState.postFlags&1,Is.Zero);Effects(true);
                    materials[i].SetFloat(i==2?"_NB_DistortionIntensity":"_ScreenDistortIntensity",0);second[i].SetFloat(i==2?"_NB_DistortionIntensity":"_ScreenDistortIntensity",0);strength0[i]=Snap(i,"ABC"[i]+"-strength0");materials[i].SetFloat(i==2?"_NB_DistortionIntensity":"_ScreenDistortIntensity",.35f);second[i].SetFloat(i==2?"_NB_DistortionIntensity":"_ScreenDistortIntensity",.35f);
                    one[i]=Snap(i,"ABC"[i]+"-one-mesh",-1,false);transparentBG.GetComponent<MeshRenderer>().enabled=false;noTransparent[i]=Snap(i,"ABC"[i]+"-without-transparent-background");transparentBG.GetComponent<MeshRenderer>().enabled=true;
                    masks[i]=Snap(i,"ABC"[i]+"-mask",0);var maskRepeat=Snap(i,"ABC"[i]+"-mask-repeat",0);Assert.That(Delta(masks[i],maskRepeat),Is.Zero);
                    maskOne[i]=Snap(i,"ABC"[i]+"-mask-one-mesh",0,false);
                    materials[i].SetFloat(i==2?"_NB_DistortionIntensity":"_ScreenDistortIntensity",0);second[i].SetFloat(i==2?"_NB_DistortionIntensity":"_ScreenDistortIntensity",0);
                    maskZero[i]=Snap(i,"ABC"[i]+"-mask-strength0",0);materials[i].SetFloat(i==2?"_NB_DistortionIntensity":"_ScreenDistortIntensity",.35f);second[i].SetFloat(i==2?"_NB_DistortionIntensity":"_ScreenDistortIntensity",.35f);
                    copies[i]=Snap(i,"ABC"[i]+"-screen-copy",1);var copyRepeat=Snap(i,"ABC"[i]+"-screen-copy-repeat",1);Assert.That(Delta(copies[i],copyRepeat),Is.Zero);
                    opaques[i]=Snap(i,"ABC"[i]+"-opaque-copy",2);var opaqueRepeat=Snap(i,"ABC"[i]+"-opaque-copy-repeat",2);Assert.That(Delta(opaques[i],opaqueRepeat),Is.Zero);
                    ((Behaviour)controllers[1]).enabled=false;Tick();var singleState=StateAt("second Controller disabled "+i);Assert.That(singleState.toggleMask,Is.EqualTo(1<<idx0));Assert.That(singleState.effectIntensity,Is.EqualTo(ExpectedIntensity(effect,false)));controllerSingle[i]=Snap(i,"ABC"[i]+"-one-controller");((Behaviour)controllers[1]).enabled=true;Tick();Assert.That(Index(controllers[1]),Is.EqualTo(idx1));
                    var restored=Snap(i,"ABC"[i]+"-controller-reenabled");Assert.That(Delta(restored,on[i]),Is.Zero);
                    ((Behaviour)manager).enabled=false;managerDisabled[i]=Snap(i,"ABC"[i]+"-manager-disabled");Assert.That(post.GetInteger("_NBPostProcessFlags")&1,Is.Zero);Assert.That(Delta(managerDisabled[i],off[i]),Is.Zero);
                    ((Behaviour)manager).enabled=true;Tick();var managerRestored=Snap(i,"ABC"[i]+"-manager-reenabled");Assert.That(Delta(managerRestored,on[i]),Is.Zero);
                    if(i==0)
                    {
                        Object.DestroyImmediate(controllers[1]);Tick();var destroyed=StateAt("second Controller destroyed");Assert.That(destroyed.activeControllers,Is.EqualTo(1<<idx0));Assert.That(destroyed.toggleMask,Is.EqualTo(1<<idx0));
                        controllerGOs[1].SetActive(false);controllers[1]=controllerGOs[1].AddComponent(controllerType);ConfigureController(controllers[1],1);controllerGOs[1].SetActive(true);Tick();Assert.That(Index(controllers[1]),Is.EqualTo(idx1));
                        var rebuilt=Snap(i,"A-controller-recreated");Assert.That(Delta(rebuilt,on[i]),Is.Zero);
                    }
                }
                void Branch(string name,Action disable,Action restore)
                {
                    Effects(true);disable();Tick();var frames=new Color[3][];for(int i=0;i<3;i++)frames[i]=Snap(i,"ABC"[i]+"-branch-"+name);
                    Assert.That(Delta(frames[0],frames[1])+Delta(frames[1],frames[2]),Is.Zero);
                    var response=Enumerable.Range(0,3).Select(i=>Delta(on[i],frames[i])).ToArray();foreach(float d in response)Assert.That(d,Is.GreaterThan(.01f),"Independent "+name+" branch response");
                    branches.Add(new BranchResponse{branch=name,response=response});restore();Tick();for(int i=0;i<3;i++){var restored=Snap(i,"ABC"[i]+"-branch-restored-"+name);Assert.That(Delta(restored,on[i]),Is.Zero);}
                }
                if(effect.StartsWith("combo",StringComparison.Ordinal))
                    foreach(string toggle in ActiveToggles(effect)){string selected=toggle;Branch(selected,()=>{foreach(var c in controllers)Field(c,selected,false);},()=>Effects(true));}
                if(effect.StartsWith("overlay",StringComparison.Ordinal))
                {
                    Branch("overlay-multiply",()=>{Effects(false);foreach(var c in controllers)Field(c,"overlayTextureBlendMode",1);Effects(true);},()=>{Effects(false);foreach(var c in controllers)Field(c,"overlayTextureBlendMode",0);Effects(true);});
                    if(effect.Contains("polar"))Branch("overlay-polar",()=>{Effects(false);foreach(var c in controllers)Field(c,"overlayTexturePolarCoordMode",false);Effects(true);},()=>{Effects(false);foreach(var c in controllers)Field(c,"overlayTexturePolarCoordMode",true);Effects(true);});
                    if(effect.Contains("mask"))Branch("overlay-mask",()=>{Effects(false);foreach(var c in controllers)Field(c,"overlayMaskTexture",null);Effects(true);},()=>{Effects(false);foreach(var c in controllers)Field(c,"overlayMaskTexture",overlayMask);Effects(true);});
                }
                if(effect.StartsWith("flash",StringComparison.Ordinal))
                {
                    Branch("flash-texture",()=>{foreach(var c in controllers)Field(c,"flashTextureToggle",false);},()=>{foreach(var c in controllers)Field(c,"flashTextureToggle",true);});
                    if(effect.Contains("polar"))Branch("flash-polar",()=>{Effects(false);foreach(var c in controllers)Field(c,"flashTexturePolarCoordMode",false);Effects(true);},()=>{Effects(false);foreach(var c in controllers)Field(c,"flashTexturePolarCoordMode",true);Effects(true);});
                }
                if(multiCamera)
                {
                    var go=Keep(new GameObject("Owned NB secondary camera"));SceneManager.MoveGameObjectToScene(go,scene);secondary=go.AddComponent<Camera>();secondary.CopyFrom(camera);secondary.scene=scene;secondary.transform.SetPositionAndRotation(camera.transform.position+new Vector3(.4f,0,0),camera.transform.rotation);secondary.targetTexture=target;var d=go.AddComponent<UniversalAdditionalCameraData>();d.SetRenderer(0);d.requiresColorTexture=true;d.renderPostProcessing=false;
                    var secondFrames=new Color[3][];for(int i=0;i<3;i++){Effects(true);secondFrames[i]=Snap(i,"ABC"[i]+"-second-camera",-1,true,secondary);var rr=Snap(i,"ABC"[i]+"-second-camera-repeat",-1,true,secondary);Assert.That(Delta(secondFrames[i],rr),Is.Zero);var primary=Snap(i,"ABC"[i]+"-primary-after-second-camera");Assert.That(Delta(primary,on[i]),Is.Zero);Assert.That(Delta(secondFrames[i],on[i]),Is.GreaterThan(.01f));}
                    Assert.That(Delta(secondFrames[0],secondFrames[1])+Delta(secondFrames[1],secondFrames[2]),Is.Zero);
                }
                controllerGOs[0].SetActive(false);controllerGOs[1].SetActive(false);Tick();var stopped=StateAt("all Controller components disabled");Assert.That(stopped.activeControllers,Is.Zero);Assert.That(stopped.toggleMask,Is.Zero);Assert.That(stopped.postFlags&1,Is.Zero);Assert.That(stopped.postFlags&expectedEffect,Is.Zero);Assert.That(stopped.activeEffectMasks.All(v=>v==0),Is.True);
                for(int i=0;i<3;i++){var cleared=Snap(i,"ABC"[i]+"-all-controllers-reset");Assert.That(Delta(cleared,off[i]),Is.Zero);}
                var r=new Record{scope="New real Controller effect catalogue + component/Manager disable, destroy/recreate and final reset through original NB renderer/RT/Uber. Existing observer reads globals only. Static motion inputs; RenderGraph/currentdownsampling; no original12 replacement or full combinations/Compatibility/Player/perf/VFX/Cinemachine claim",api=SystemInfo.graphicsDeviceType.ToString(),unity=Application.unityVersion,gpu=SystemInfo.graphicsDeviceName,mode=mode,effect=effect,downsampling=nb.GetType().GetField("downSampling").GetValue(nb).ToString(),orthographic=ortho,finite=Finite(payloads),controllerLifecycleRestored=stopped.activeControllers==0,renderGraph=true,managerDisableResponse=Enumerable.Range(0,3).Select(i=>Delta(on[i],managerDisabled[i])).ToArray(),independentBranches=branches.ToArray(),
                    abFinal=Delta(on[0],on[1]),bcFinal=Delta(on[1],on[2]),abMask=Delta(masks[0],masks[1]),bcMask=Delta(masks[1],masks[2]),abCopy=Delta(copies[0],copies[1]),bcCopy=Delta(copies[1],copies[2]),abOpaque=Delta(opaques[0],opaques[1]),bcOpaque=Delta(opaques[1],opaques[2]),
                    repeat=Enumerable.Range(0,3).Select(i=>Delta(on[i],repeat[i])).ToArray(),distortionResponse=Enumerable.Range(0,3).Select(i=>Delta(on[i],strength0[i])).ToArray(),controllerResponse=Enumerable.Range(0,3).Select(i=>Delta(on[i],off[i])).ToArray(),controllerUnionResponse=Enumerable.Range(0,3).Select(i=>Delta(on[i],controllerSingle[i])).ToArray(),transparentResponse=Enumerable.Range(0,3).Select(i=>Delta(on[i],noTransparent[i])).ToArray(),accumulationResponse=Enumerable.Range(0,3).Select(i=>Delta(on[i],one[i])).ToArray(),finalVisible=on.Select(Visible).ToArray(),flagsOn=new[]{fullState.postFlags},controllerIndices=new[]{idx0,idx1},
                    maskAccumulationResponse=Enumerable.Range(0,3).Select(i=>Delta(masks[i],maskOne[i])).ToArray(),maskStrengthResponse=Enumerable.Range(0,3).Select(i=>Delta(masks[i],maskZero[i])).ToArray(),copyAlphaMin=copies.Select(a=>a.Min(c=>c.a)).ToArray(),copyAlphaMax=copies.Select(a=>a.Max(c=>c.a)).ToArray()};
                File.WriteAllText(Path.Combine(folder,"metrics.json"),JsonUtility.ToJson(r,true));File.WriteAllText(Path.Combine(folder,"controller-states.json"),JsonUtility.ToJson(new States{states=states.ToArray()},true));
                var package=UnityEditor.PackageManager.PackageInfo.FindForAssetPath(GraphPath);string[] inputs={"NBPostProcessing/Runtime/NBPostProcess.cs","NBPostProcessing/Runtime/PostProcessingController.cs","NBPostProcessing/Runtime/PostProcessingManager.cs","NBPostProcessing/Runtime/DisturbanceMaskRenderPass.cs","NBPostProcessing/Runtime/ScreenColorRenderPass.cs","NBPostProcessing/Runtime/RenderCameraOpaqueDistortObjectPass.cs","NBPostProcessing/Runtime/NBPostProcessRenderPass.cs","NBPostProcessing/Shader/NBPostProcessUber.shader","Tests/URP/Editor/G4NBPostEffectsControllerTests.cs"};
                var sg=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Unity.ShaderGraph.Editor").GetType("UnityEditor.ShaderGraph.ShaderGraphImporter",true);var get=sg.GetMethods(BindingFlags.Static|BindingFlags.NonPublic).Single(m=>m.Name=="GetShaderText"&&m.GetParameters().Length==4&&m.GetParameters()[3].IsOut);object[] args={GraphPath,null,null,null};string generated=(string)get.Invoke(null,args);string genPath=Path.Combine(folder,"generated-current-graph.shader");File.WriteAllText(genPath,generated);
                var graph=materials[2].shader;var passNames=new List<string>();var lightModes=new List<string>();for(int i=0;i<materials[2].passCount;i++){passNames.Add(materials[2].GetPassName(i));lightModes.Add(graph.FindPassTagValue(0,i,new ShaderTagId("LightMode")).name);}
                string[] required={"_NB_DistortionMode","_NB_DistortionIntensity","_NB_Flags0Lo16","_NB_Flags1Lo16","_noisemapEnabled","_NoiseMap"};foreach(var n in required)Assert.That(materials[2].HasProperty(n),Is.True,n);
                var provenance=new Provenance{resolvedPackage=package.resolvedPath,graphSHA256=SHA(Path.Combine(package.resolvedPath,"NBShaders2/ShaderGraph/NBShaderGraph.shadergraph")),generatedShaderSHA256=SHA(genPath),rendererAssetSHA256=SHA(rdPath),pipelineAssetSHA256=SHA(pipelinePath),sourcePaths=inputs,sourceSHA256=inputs.Select(x=>SHA(Path.Combine(package.resolvedPath,x))).ToArray(),requiredGraphInputs=required,passNames=passNames.ToArray(),lightModes=lightModes.ToArray(),originalNBFeatureActive=nb.isActive,cameraRenderPostProcessing=cameraData.renderPostProcessing,volumeRequired=false,managerControllerOwned=true};File.WriteAllText(Path.Combine(folder,"provenance.json"),JsonUtility.ToJson(provenance,true));
                Assert.That(r.finite,Is.True);Assert.That(r.abFinal+r.bcFinal+r.abMask+r.bcMask+r.abCopy+r.bcCopy+r.abOpaque+r.bcOpaque,Is.Zero);
                foreach(var d in r.repeat)Assert.That(d,Is.Zero);foreach(var n in r.finalVisible)Assert.That(n,Is.GreaterThan(150));
                foreach(var d in r.distortionResponse)Assert.That(d,Is.GreaterThan(.005f));foreach(var d in r.controllerResponse)Assert.That(d,Is.GreaterThan(.01f));foreach(var d in r.managerDisableResponse)Assert.That(d,Is.GreaterThan(.01f));foreach(var d in r.controllerUnionResponse)Assert.That(d,Is.GreaterThan(.01f));foreach(var d in r.transparentResponse)Assert.That(d,Is.GreaterThan(.01f));foreach(var d in r.accumulationResponse)Assert.That(d,Is.GreaterThan(.005f));
                foreach(float alpha in r.copyAlphaMin)Assert.That(alpha,Is.LessThan(.9f),"Scene copy must contain actual partially transparent background alpha.");
                if(mode=="deferred")
                {
                    Assert.That(masks.All(a=>a.Any(c=>Mathf.Abs(c.r)+Mathf.Abs(c.g)>.005f)),Is.True,"Zero native deferred mask cannot prove parity.");
                    foreach(float d in r.maskStrengthResponse)Assert.That(d,Is.GreaterThan(.005f));
                    foreach(float d in r.maskAccumulationResponse)Assert.That(d,Is.GreaterThan(.005f));
                }
                else
                    Assert.That(masks.All(a=>a.All(c=>c.r==0&&c.g==0)),Is.True,"Opaque mode must not accidentally draw the native Deferred mask.");
            }
            finally
            {
                foreach(var go in controllerGOs)if(go)go.SetActive(false);
                if(!manager&&oldStatics!=null)manager=managerType.GetField("_instance",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null)as Component;
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
                if(cleanupObserver&&cleanupRenderer)
                {cleanupRenderer.rendererFeatures.Remove(cleanupObserver);cleanupRenderer.SetDirty();}
                for(int i=owned.Count-1;i>=0;--i)if(owned[i])Object.DestroyImmediate(owned[i]);
                owned.Clear();RenderTexture.active=cleanupOldRT;
                if(cleanupScene.IsValid()&&cleanupScene.isLoaded)EditorSceneManager.ClosePreviewScene(cleanupScene);
                cleanupObserver=null;cleanupRenderer=null;cleanupScene=default;
            }
        }

        static readonly string[] AllToggles={"chromaticAberrationToggle","distortSpeedToggle","radialBlurToggle","vignetteToggle","overlayTextureToggle","flashToggle"};
        static string[] ActiveToggles(string effect)
        {
            if(effect=="ca")return new[]{"chromaticAberrationToggle"};
            if(effect.StartsWith("speed"))return new[]{"distortSpeedToggle"};
            if(effect=="radial")return new[]{"radialBlurToggle"};
            if(effect=="vignette")return new[]{"vignetteToggle"};
            if(effect.StartsWith("overlay"))return new[]{"overlayTextureToggle"};
            if(effect.StartsWith("flash"))return new[]{"flashToggle"};
            if(effect=="combo-ca-radial-speed")return new[]{"chromaticAberrationToggle","radialBlurToggle","distortSpeedToggle"};
            if(effect=="combo-overlay-flash")return new[]{"overlayTextureToggle","flashToggle"};
            throw new ArgumentException(effect);
        }
        static string PrimaryToggle(string effect)=>ActiveToggles(effect)[0];
        static string MaskName(string toggle)
        {switch(toggle){case "chromaticAberrationToggle":return "chromaticAberrationToggles";case "distortSpeedToggle":return "distortSpeedToggles";case "radialBlurToggle":return "radialBlurToggles";case "vignetteToggle":return "vignetteToggles";case "overlayTextureToggle":return "overlayTextureToggles";case "flashToggle":return "flashToggles";default:throw new ArgumentException(toggle);}}
        static int RequiredBits(string effect)
        {
            var flags=RuntimeType("NBShader.NBPostProcessFlags");int result=0;
            foreach(string toggle in ActiveToggles(effect))
            {string n=toggle=="chromaticAberrationToggle"?"FLAG_BIT_CHORATICABERRAT":toggle=="distortSpeedToggle"?"FLAG_BIT_DISTORT_SPEED":toggle=="radialBlurToggle"?"FLAG_BIT_RADIALBLUR":toggle=="vignetteToggle"?"FLAG_BIT_VIGNETTE":toggle=="overlayTextureToggle"?"FLAG_BIT_OVERLAYTEXTURE":"FLAG_BIT_FLASH";result|=(int)flags.GetField(n,PublicStatic).GetValue(null);}return result;
        }
        static float PrimaryIntensity(Material m,string effect)
        {switch(PrimaryToggle(effect)){case "chromaticAberrationToggle":return m.GetVector("_ChromaticAberrationVec").x;case "distortSpeedToggle":return m.GetVector("_SpeedDistortVec").x;case "radialBlurToggle":return m.GetVector("_RadialBlurVec").x;case "vignetteToggle":return m.GetVector("_VignetteVec").x;case "overlayTextureToggle":return m.GetFloat("_TextureOverlayIntensity");default:return m.GetFloat("_FlashIntensity");}}
        static float ExpectedIntensity(string effect,bool two)
        {if(PrimaryToggle(effect)=="overlayTextureToggle")return two?.7f:.4f;if(PrimaryToggle(effect)=="radialBlurToggle")return (two?.65f:.3f)*.1f/6f;return two?.65f:.3f;}
        static void ValidateModifierState(Material post,string effect)
        {
            int flags=post.GetInteger("_NBPostProcessFlags");
            if(effect.StartsWith("speed"))Assert.That((flags&(1<<9))!=0,Is.EqualTo(effect=="speed-screen"));
            if(effect.StartsWith("overlay")){Assert.That(post.GetInteger("_TextureOverlayBlendMode"),Is.Zero);Assert.That((flags&(1<<7))!=0,Is.EqualTo(effect.Contains("polar")));Assert.That((flags&(1<<8))!=0,Is.EqualTo(effect.Contains("mask")));}
            if(effect.StartsWith("flash")){Assert.That(flags&(1<<13),Is.Not.Zero);Assert.That((flags&(1<<12))!=0,Is.EqualTo(effect.Contains("polar")));}
        }

        [Serializable] sealed class States {public State[] states;}
    }
}
