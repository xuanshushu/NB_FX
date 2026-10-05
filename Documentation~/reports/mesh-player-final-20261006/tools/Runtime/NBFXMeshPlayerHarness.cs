using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using NBShader;
using Object=UnityEngine.Object;

namespace NBFX.PlayerValidation
{
    // Ordinary standalone MonoBehaviour, no UNITY_EDITOR or NUnit dependency.
    // Production Controller.Update/Manager.LateUpdate execute through real Player
    // frames. Existing NBPostProcess does all NB rendering and composition.
    public sealed class NBFXMeshPlayerHarness : MonoBehaviour
    {
        public NBFXMeshPlayerConfig config;
        const int Size=128,Layer=2;
        string folder;
        Camera camera;
        MeshRenderer actor,background,transparentBackground;
        PostProcessingController controller;
        NBPostProcess feature;
        RenderTexture target;
        NBFXPlayerRTObserver rtObserver;ScriptableRendererData observerRenderer;int observedToken;bool oldFeatureActive;
        Texture2D read;
        readonly List<Object> owned=new List<Object>();
        readonly List<CaseResult> results=new List<CaseResult>();
        readonly List<FlipbookResult> flipbookResults=new List<FlipbookResult>();
        [Serializable] sealed class CaseResult
        {
            public string identity,route,status,error;
            public bool finite;
            public int bVisible,cVisible;
            public float onDelta,controlDelta,strengthDelta,bRepeat,cRepeat,bResponse,cResponse,bControllerResponse,cControllerResponse;
            public float maskDelta,copyDelta,opaqueDelta;
        }
        [Serializable] sealed class RunReceipt
        {
            public string buildIdentity,sourceLockSHA256,sourceReceiptJSON,unity,api,gpu,platform,executableArguments,cleanupError;
            public bool cleanupCompleted;
            public bool applicationIsEditor,actualNBFeature,renderGraph;
            public int processId,pointerBytes,exitCode;
            public CaseResult[] cases;
            public FlipbookResult[] automaticFlipbookCases;
        }
        [Serializable] sealed class Inputs
        {
            public string identity,label,shader,actualLightMode,downsampling;
            public string[] keywords;
            public float intensity,alphaAll,srcBlend,dstBlend,srcBlendAlpha,dstBlendAlpha,blend,surface;
            public bool hasSeparateAlpha;
            public int controllerIndex,postFlags,overlayToggles;
            public bool controllerActive,actualPassRawEnabled,originalNBFeatureActive;
        }
        T Keep<T>(T o) where T:Object {owned.Add(o);return o;}
        static void Require(bool condition,string message)
        {if(!condition)throw new InvalidOperationException(message);}
        static string Argument(string name)
        {
            string[] a=Environment.GetCommandLineArgs();int index=Array.IndexOf(a,name);
            return index>=0&&index+1<a.Length?a[index+1]:null;
        }
        void Start()
        {
            folder=Argument("--nbfx-evidence");
            if(string.IsNullOrEmpty(folder)){Debug.LogError("Missing --nbfx-evidence; no Player evidence written.");Application.Quit(2);return;}
            folder=Path.GetFullPath(folder);Directory.CreateDirectory(folder);
            try
            {
                Require(!Application.isEditor&&Application.platform==RuntimePlatform.WindowsPlayer&&IntPtr.Size==8,"Editor/other architecture cannot prove StandaloneWindows64.");
                Require(config&&config.cases!=null&&config.cases.Length==6,"Serialized build config/cases absent.");
                Require(config.automaticFlipbookCases!=null&&config.automaticFlipbookCases.Length==2,"Two automatic Flipbook cases must be retained.");
                Require(SystemInfo.graphicsDeviceType==GraphicsDeviceType.Direct3D11,"This first receipt is Windows Direct3D11 only.");
                Require(config.graphShader&&config.currentShader&&config.graphShader.isSupported&&config.currentShader.isSupported,"Retained current/Graph shader unsupported or stripped.");
                Require(config.uberShader&&config.colorBlitShader,"Shader.Find contract refs must be retained by serialized config.");
                QualitySettings.renderPipeline=config.pipeline; // runtime memory only
                Require(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset,"Actual URP unavailable.");
                Require(!GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>().enableRenderCompatibilityMode,"First slice uses RenderGraph.");
                var data=config.pipeline.rendererDataList[0];
                feature=data.rendererFeatures.OfType<NBPostProcess>().SingleOrDefault();Require(feature&&feature.isActive,"Original active NBPostProcess required.");
                Setup();
                StartCoroutine(Run());
            }
            catch(Exception e){Finish(2,e.ToString());}
        }
        void Setup()
        {
            var go=Keep(new GameObject("Standalone real NBFX camera"));camera=go.AddComponent<Camera>();camera.enabled=false;
            camera.transform.position=new Vector3(0,0,8);camera.transform.rotation=Quaternion.Euler(0,180,0);
            camera.nearClipPlane=.1f;camera.farClipPlane=20;camera.orthographicSize=2;camera.fieldOfView=45;
            camera.cullingMask=1<<Layer;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.03f,.05f,.1f,.125f);
            camera.allowHDR=true;camera.allowMSAA=false;
            var additional=go.AddComponent<UniversalAdditionalCameraData>();additional.SetRenderer(0);additional.requiresColorTexture=true;additional.renderPostProcessing=false;
            target=Keep(new RenderTexture(Size,Size,24,RenderTextureFormat.ARGBHalf,RenderTextureReadWrite.Linear));target.Create();Require(target.IsCreated()&&!target.sRGB,"RGBAHalf target invalid.");camera.targetTexture=target;
            // PerformanceSampler also calls Setup. Correctness alone owns this
            // observer; performance creates no feature and queues no copy pass.
            if(!string.IsNullOrEmpty(Argument("--nbfx-evidence")))
            {
                Require(config.nbPostReadbackMaterial&&config.nbPostReadbackMaterial.passCount==3,"Serialized original readback material/passes missing.");
                observerRenderer=config.pipeline.rendererDataList[0];oldFeatureActive=feature.isActive;
                rtObserver=ScriptableObject.CreateInstance<NBFXPlayerRTObserver>();rtObserver.hideFlags=HideFlags.HideAndDontSave;
                rtObserver.Initialize(camera,Size,config.nbPostReadbackMaterial);observerRenderer.rendererFeatures.Add(rtObserver);observerRenderer.SetDirty();
            }
            read=Keep(new Texture2D(Size,Size,TextureFormat.RGBAHalf,false,true));
            MeshRenderer Quad(string name,Vector3 pos,Vector3 scale,Material material)
            {
                var obj=Keep(GameObject.CreatePrimitive(PrimitiveType.Quad));obj.name=name;obj.layer=Layer;obj.transform.position=pos;obj.transform.localScale=scale;
                var r=obj.GetComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;return r;
            }
            background=Quad("actual opaque gradient",new Vector3(-1.5f,0,1),new Vector3(3,6,1),config.backgroundMaterial);
            transparentBackground=Quad("actual transparent gradient",new Vector3(0,0,1.5f),new Vector3(6,6,1),config.transparentBackgroundMaterial);
            actor=Quad("ordinary Mesh B/C writer",new Vector3(0,0,2),new Vector3(2,2,1),null);
            var control=Keep(new GameObject("actual production Controller"));control.SetActive(false);controller=control.AddComponent<PostProcessingController>();
            controller.overlayTextureToggle=true;controller.overlayTexture=config.overlay;controller.overlayTextureSt=new Vector4(1,1,0,0);
            controller.overlayTextureAnim=Vector2.zero;controller.overlayTexturePolarCoordMode=false;controller.overlayTextureBlendMode=OverlayTextureBlendMode.Add;
            controller.overlayTextureIntensity=.7f;controller.overlayMaskTexture=Texture2D.whiteTexture;controller.overlayMaskTextureSt=new Vector4(1,1,0,0);
        }
        IEnumerator Run()
        {
            actor.enabled=false;
            for(int warm=0;warm<4;warm++)
            {
                Exception warmError=null;
                try{camera.Render();}catch(Exception e){warmError=e;}
                if(warmError!=null){Finish(2,warmError.ToString());yield break;}
                yield return null;
            }
            if(!NBPostProcess.NBPostProcessMaterial){Finish(2,"Original runtime Uber material absent; retained Shader.Find refs failed.");yield break;}
            foreach(PlayerCase c in config.cases)
            {
                var r=new CaseResult{identity=c.identity,route=c.route,status="FAILED"};results.Add(r);
                // Each nested iterator is advanced under a catch so every failure
                // yields a real result and an eventual nonzero Player exit code.
                var stack=new Stack<IEnumerator>();stack.Push(One(c,r));
                while(stack.Count>0)
                {
                    object current=null;bool advanced=false;
                    try
                    {
                        var task=stack.Peek();advanced=task.MoveNext();
                        if(advanced)current=task.Current;else stack.Pop();
                    }
                    catch(Exception e){r.error=e.ToString();DisposeIterators(stack,error=>r.error+="\nCleanup: "+error);}
                    if(advanced)
                    {
                        if(current is IEnumerator nested)stack.Push(nested);
                        else yield return current;
                    }
                }
                File.WriteAllText(Path.Combine(folder,c.identity+"-result.json"),JsonUtility.ToJson(r,true));
            }
            // Preserve earlier strict failures, but still execute independent automatic cases.
            foreach(PlayerFlipbookCase c in config.automaticFlipbookCases)
            {
                var r=new FlipbookResult{identity=c.identity,status="FAILED"};flipbookResults.Add(r);
                var stack=new Stack<IEnumerator>();stack.Push(AutomaticFlipbook(c,r));
                while(stack.Count>0)
                {
                    object current=null;bool advanced=false;
                    try{var task=stack.Peek();advanced=task.MoveNext();if(advanced)current=task.Current;else stack.Pop();}
                    catch(Exception e)
                    {
                        r.error=e.ToString();
                        // Dispose the real iterator so its clock/material cleanup
                        // also runs on a strict assertion failure, not only success.
                        DisposeIterators(stack,error=>r.error+="\nCleanup: "+error);
                    }
                    if(advanced){if(current is IEnumerator nested)stack.Push(nested);else yield return current;}
                }
                File.WriteAllText(Path.Combine(folder,c.identity+"-result.json"),JsonUtility.ToJson(r,true));
                // A strict first-camera failure does not suppress the independent second camera.
            }
            Finish(results.Any(r=>r.status!="PASSED")||flipbookResults.Any(r=>r.status!="PASSED")?1:0,null);
        }
        static void DisposeIterators(Stack<IEnumerator> stack,Action<string> record)
        {while(stack.Count>0){try{(stack.Pop() as IDisposable)?.Dispose();}catch(Exception e){record(e.ToString());}}}
        IEnumerator One(PlayerCase c,CaseResult r)
        {
            bool priorFeature=feature.isActive,priorActor=actor.enabled,priorBackground=background.enabled,priorTransparent=transparentBackground.enabled;
            try
            {
            camera.orthographic=c.orthographic;actor.enabled=true;
            bool post=c.route!="Forward";feature.SetActive(post);background.enabled=post;transparentBackground.enabled=post;
            controller.gameObject.SetActive(false);
            // Warm the original feature before the production Component registers.
            actor.sharedMaterial=c.currentOn;for(int i=0;i<4;i++){camera.Render();yield return null;}
            controller.gameObject.SetActive(post);if(post)Require(PostProcessingManager.Instance,"Real Manager singleton absent.");
            Color[] b=null,br=null,b0=null,bs=null,bc=null,d=null,dr=null,d0=null,ds=null,dc=null;
            Color[] bm=null,dm=null,copyB=null,copyC=null,opaqueB=null,opaqueC=null;
            IEnumerator Snap(Material material,bool effect,string label,Action<Color[]> assign)
            {
                actor.sharedMaterial=material;controller.overlayTextureToggle=effect;
                for(int i=0;i<4;i++)
                {
                    yield return new WaitForEndOfFrame(); // real Update + LateUpdate
                    if(post&&i==3&&(label=="B-repeat"||label=="C-repeat"))
                    {Require(rtObserver,"Correctness in-graph observer missing.");observedToken=rtObserver.Request();}
                    camera.Render();
                }
                Require(material&&material.shader.isSupported,"Player material unsupported.");
                WriteInputs(c,label,material);
                assign(Read(target,c.identity+"-"+label));
            }
            yield return Snap(c.currentOn,post,"B-on",x=>b=x);yield return Snap(c.currentOn,post,"B-repeat",x=>br=x);
            if(post){bm=ReadGlobal("_DisturbanceMaskTex",c.identity+"-B-mask");copyB=ReadGlobal("_ScreenColorCopy1",c.identity+"-B-copy");opaqueB=ReadGlobal("_CameraOpaqueTexture",c.identity+"-B-opaque");}
            yield return Snap(c.currentControl,post,"B-control",x=>b0=x);yield return Snap(c.currentStrength0,post,"B-strength0",x=>bs=x);
            yield return Snap(c.currentOn,false,"B-controller-off",x=>bc=x);
            yield return Snap(c.graphOn,post,"C-on",x=>d=x);yield return Snap(c.graphOn,post,"C-repeat",x=>dr=x);
            if(post){dm=ReadGlobal("_DisturbanceMaskTex",c.identity+"-C-mask");copyC=ReadGlobal("_ScreenColorCopy1",c.identity+"-C-copy");opaqueC=ReadGlobal("_CameraOpaqueTexture",c.identity+"-C-opaque");}
            yield return Snap(c.graphControl,post,"C-control",x=>d0=x);yield return Snap(c.graphStrength0,post,"C-strength0",x=>ds=x);
            yield return Snap(c.graphOn,false,"C-controller-off",x=>dc=x);
            r.finite=true;r.bVisible=Visible(b);r.cVisible=Visible(d);r.onDelta=Delta(b,d);r.controlDelta=Delta(b0,d0);r.strengthDelta=Delta(bs,ds);
            r.bRepeat=Delta(b,br);r.cRepeat=Delta(d,dr);r.bResponse=Delta(b,post?bs:b0);r.cResponse=Delta(d,post?ds:d0);
            r.bControllerResponse=Delta(b,bc);r.cControllerResponse=Delta(d,dc);
            Require(r.bVisible>150&&r.cVisible>150,"Finite blank output cannot prove parity.");
            Require(r.onDelta+r.controlDelta+r.strengthDelta==0,"B/C full-frame strict mismatch.");Require(r.bRepeat+r.cRepeat==0,"Player repeat mismatch.");
            Require(r.bResponse>(post?.005f:.03f)&&r.cResponse>(post?.005f:.03f),"Original strong response control failed.");
            if(post)
            {
                Require(r.bControllerResponse>.01f&&r.cControllerResponse>.01f,"Real Controller/Manager response weak/missing.");
                Require(Delta(bc,dc)==0,"Controller-off B/C mismatch.");
                r.maskDelta=Delta(bm,dm);r.copyDelta=Delta(copyB,copyC);r.opaqueDelta=Delta(opaqueB,opaqueC);
                Require(r.maskDelta+r.copyDelta+r.opaqueDelta==0,"Original native RT B/C mismatch.");
                if(c.route=="NBDeferredDistortPass")Require(bm.Any(v=>Mathf.Abs(v.r)+Mathf.Abs(v.g)>.005f)&&dm.Any(v=>Mathf.Abs(v.r)+Mathf.Abs(v.g)>.005f),"Invisible/zero deferred mask.");
                Require(copyB.Min(x=>x.a)<.9f&&copyC.Min(x=>x.a)<.9f,"Copy lacks actual transparent alpha.");
            }
            controller.gameObject.SetActive(false);r.status="PASSED";
            }
            finally{controller.gameObject.SetActive(false);rtObserver?.Cancel();feature.SetActive(priorFeature);actor.enabled=priorActor;background.enabled=priorBackground;transparentBackground.enabled=priorTransparent;}
        }
        Color[] ReadGlobal(string name,string label)
        {
            Require(rtObserver,"Correctness in-graph observer missing.");
            File.WriteAllText(Path.Combine(folder,label+"-observer.json"),rtObserver.EvidenceJSON());
            Texture texture=rtObserver.Readable(name,observedToken);
            Require(texture is RenderTexture native&&native.IsCreated()&&native.width>0&&native.height>0,"UNVERIFIED: original transient global RT unavailable: "+name);
            // The original global was copied inside its RG lifetime. No graph-
            // external Shader.GetGlobalTexture or fallback/extra camera render.
            return Read((RenderTexture)texture,label);
        }
        Color[] Read(RenderTexture source,string label)
        {
            var old=RenderTexture.active;RenderTexture.active=source;read.ReadPixels(new Rect(0,0,Size,Size),0,0,false);read.Apply(false,false);RenderTexture.active=old;
            var data=read.GetPixels();
            using(var stream=File.Create(Path.Combine(folder,label+".rgba32f")))using(var writer=new BinaryWriter(stream))
                foreach(Color p in data){writer.Write(p.r);writer.Write(p.g);writer.Write(p.b);writer.Write(p.a);}
            Require(data.All(p=>Enumerable.Range(0,4).All(i=>!float.IsNaN(p[i])&&!float.IsInfinity(p[i]))),"Nonfinite actual Player raw.");
            Require(!data.Any(p=>Enumerable.Range(0,4).Any(i=>p[i]==-23.203125f||p[i]==-431602080f)),"Invalid half/float CDCD raw.");return data;
        }
        static float Delta(Color[] a,Color[] b)
        {Require(a!=null&&b!=null&&a.Length==b.Length,"Missing raw cannot pass.");float d=0;for(int i=0;i<a.Length;i++)for(int c=0;c<4;c++)d=Mathf.Max(d,Mathf.Abs(a[i][c]-b[i][c]));return d;}
        static int Visible(Color[] a)=>a.Count(p=>Mathf.Max(p.r,Mathf.Max(p.g,p.b))>.15f);
        void WriteInputs(PlayerCase c,string label,Material m)
        {
            int index=m.FindPass(c.route=="Forward"?(m.shader==config.graphShader?"Universal Forward":"UniversalForward"):c.route);Require(index>=0,"Required actual pass stripped.");
            string mode=m.shader.FindPassTagValue(0,index,new ShaderTagId("LightMode")).name;if(string.IsNullOrEmpty(mode))mode="SRPDefaultUnlit";
            Require(m.GetShaderPassEnabled(mode),"Required actual LightMode disabled.");
            var input=new Inputs{identity=c.identity,label=label,shader=m.shader.name,actualLightMode=mode,keywords=m.shaderKeywords,
                intensity=m.GetFloat(m.shader==config.graphShader?"_NB_DistortionIntensity":"_ScreenDistortIntensity"),alphaAll=m.GetFloat("_AlphaAll"),
                srcBlend=m.GetFloat("_SrcBlend"),dstBlend=m.GetFloat("_DstBlend"),srcBlendAlpha=m.HasProperty("_SrcBlendAlpha")?m.GetFloat("_SrcBlendAlpha"):-1,dstBlendAlpha=m.HasProperty("_DstBlendAlpha")?m.GetFloat("_DstBlendAlpha"):-1,hasSeparateAlpha=m.HasProperty("_SrcBlendAlpha")&&m.HasProperty("_DstBlendAlpha"),blend=m.GetFloat("_Blend"),surface=m.HasProperty("_Surface")?m.GetFloat("_Surface"):m.GetFloat("_TransparentMode"),
                controllerIndex=controller.index,controllerActive=controller.isActiveAndEnabled,postFlags=NBPostProcess.NBPostProcessMaterial?NBPostProcess.NBPostProcessMaterial.GetInteger("_NBPostProcessFlags"):0,
                overlayToggles=PostProcessingManager.overlayTextureToggles,downsampling=feature.downSampling.ToString(),actualPassRawEnabled=m.GetShaderPassEnabled(mode),originalNBFeatureActive=feature.isActive};
            File.WriteAllText(Path.Combine(folder,c.identity+"-"+label+"-inputs.json"),JsonUtility.ToJson(input,true));
        }
        void Finish(int code,string error)
        {
            var cleanupErrors=new List<string>();
            void Cleanup(Action action){try{action();}catch(Exception e){cleanupErrors.Add(e.ToString());}}
            if(rtObserver)
            {
                Cleanup(()=>rtObserver.Cancel());
                Cleanup(()=>{if(observerRenderer){observerRenderer.rendererFeatures.Remove(rtObserver);observerRenderer.SetDirty();}});
                Cleanup(()=>rtObserver.Dispose());
                Cleanup(()=>Object.Destroy(rtObserver));rtObserver=null;
            }
            if(feature&&observerRenderer)Cleanup(()=>feature.SetActive(oldFeatureActive));
            int exit=cleanupErrors.Count==0?code:Math.Max(1,code);
            string cleanupError=cleanupErrors.Count==0?null:string.Join("\n",cleanupErrors);
            try
            {
                if(error!=null)File.WriteAllText(Path.Combine(folder,"fatal.txt"),error);
                if(cleanupError!=null)File.WriteAllText(Path.Combine(folder,"cleanup-failure.txt"),cleanupError);
                var receipt=new RunReceipt{buildIdentity=config?config.buildIdentity:null,sourceLockSHA256=config?config.sourceLockSHA256:null,sourceReceiptJSON=config?config.sourceReceiptJSON:null,
                    unity=Application.unityVersion,api=SystemInfo.graphicsDeviceType.ToString(),gpu=SystemInfo.graphicsDeviceName,platform=Application.platform.ToString(),applicationIsEditor=Application.isEditor,
                    pointerBytes=IntPtr.Size,processId=System.Diagnostics.Process.GetCurrentProcess().Id,executableArguments=string.Join(" ",Environment.GetCommandLineArgs()),
                    actualNBFeature=feature&&feature.isActive,renderGraph=true,cases=results.ToArray(),automaticFlipbookCases=flipbookResults.ToArray(),exitCode=exit,cleanupCompleted=cleanupErrors.Count==0,cleanupError=cleanupError};
                File.WriteAllText(Path.Combine(folder,"player-result.json"),JsonUtility.ToJson(receipt,true));
            }
            catch(Exception e){Debug.LogException(e);exit=2;}
            finally{Application.Quit(exit);}
        }

        [Serializable] sealed class FlipbookState
        {
            public string shaderName;
            public string[] keywords;
            public int frameCount,helperFrame,materialInstance;
            public float realTime,timeScale,deltaTime,blend,flipbookToggle;
            public Vector4 currentST,nextST;
            public uint flags0,flags1;
            public bool helperEnabled,manualPlay;
        }
        [Serializable] sealed class FlipbookStage
        {
            public string name;
            public FlipbookState b,c;
            public float bc,bRepeat,cRepeat;
            public int bVisible,cVisible;
        }
        [Serializable] sealed class FlipbookResult
        {
            public string identity,status,error;
            public bool runtimeInstancesDistinct,pauseHeld,disabledHeld,restartReset,destroyCleared,sourceHelperFieldsUnchanged,clockRestored;
            public int pauseFrames,disabledFrames;
            public float automaticResponseB,automaticResponseC,resumeResponseB,resumeResponseC,restartResponseB,restartResponseC;
            public List<FlipbookStage> stages=new List<FlipbookStage>();
        }
        const uint HelperBit=1u<<15;
        static uint Word(Material material,int index)
        {
            string lo="_NB_Flags"+index+"Lo16",hi="_NB_Flags"+index+"Hi16";
            if(material.HasProperty(lo)&&material.HasProperty(hi))return (uint)material.GetFloat(lo)|((uint)material.GetFloat(hi)<<16);
            string alias=index==0?"_NBShaderFlags":"_NBShaderFlags1";
            return unchecked((uint)material.GetInteger(material.HasProperty(alias)?alias:index==0?"_W9ParticleShaderFlags":"_W9ParticleShaderFlags1"));
        }
        static FlipbookState State(AnimationSheetHelper helper,Material material)
        {
            return new FlipbookState{shaderName=material.shader.name,keywords=material.shaderKeywords,flipbookToggle=material.GetFloat("_FlipbookBlending"),frameCount=Time.frameCount,realTime=Time.realtimeSinceStartup,timeScale=Time.timeScale,deltaTime=Time.deltaTime,
                helperFrame=helper?helper.frameIndex:-1,helperEnabled=helper&&helper.isActiveAndEnabled,manualPlay=helper&&helper.manualPlay,
                materialInstance=material.GetInstanceID(),currentST=material.GetVector("_BaseMap_ST"),nextST=material.GetVector("_BaseMap_AnimationSheetBlend_ST"),
                blend=material.GetFloat("_AnimationSheetHelperBlendIntensity"),flags0=Word(material,0),flags1=Word(material,1)};
        }
        static void SameAnimation(FlipbookState a,FlipbookState b,string label)
        {
            Require(a.currentST.Equals(b.currentST)&&a.nextST.Equals(b.nextST)&&a.blend==b.blend&&a.flags0==b.flags0&&a.flags1==b.flags1,label);
        }
        static Vector4 GridFrame(int frame)=>new Vector4(.5f,.5f,(frame%2)*.5f,(1-frame/2)*.5f);
        static void ExpectedAnimation(AnimationSheetHelper helper,Material material,uint original0,uint original1,bool enabled)
        {
            var state=State(helper,material);
            Require(state.flipbookToggle==1f,"Saved Flipbook intent must remain enabled.");
            Require(state.helperEnabled==enabled&&!state.manualPlay&&helper.frameCount==4&&helper.speed==2f,"Use the existing automatic Helper, not manual time/Update.");
            Require(state.helperFrame>=0&&state.helperFrame<4&&state.blend>=0&&state.blend<1,"Finite four-frame state required.");
            Require(state.currentST.Equals(GridFrame(state.helperFrame))&&state.nextST.Equals(GridFrame((state.helperFrame+1)%4)),"Helper must supply actual current/next ST.");
            Require(state.flags0==original0&&(state.flags1&~HelperBit)==(original1&~HelperBit)&&((state.flags1&HelperBit)!=0)==enabled,"Only the Helper ownership bit may change.");
        }
        static float Phase(AnimationSheetHelper helper)=>helper.frameIndex+helper.mat.GetFloat("_AnimationSheetHelperBlendIntensity");
        static float Progress(float before,float after)=>Mathf.Repeat(after-before,4f);
        IEnumerator AutomaticFlipbook(PlayerFlipbookCase c,FlipbookResult result)
        {
            Require(c.current&&c.graph&&c.current.shader.isSupported&&c.graph.shader.isSupported,"Retained Flipbook variants missing.");
            float savedScale=Time.timeScale;Require(savedScale>0,"Player input must start with a running scaled clock.");
            bool oldFeature=feature.isActive,oldActor=actor.enabled,oldBackground=background.enabled,oldTransparent=transparentBackground.enabled;
            GameObject bGO=null,cGO=null;Material b=null,d=null;AnimationSheetHelper bh=null,ch=null;
            var sourceB=State(null,c.current);var sourceC=State(null,c.graph);
            try
            {
                feature.SetActive(false);actor.enabled=background.enabled=transparentBackground.enabled=false;controller.gameObject.SetActive(false);
                camera.orthographic=c.orthographic;
                AnimationSheetHelper Create(Material source,string role,out GameObject go)
                {
                    go=GameObject.CreatePrimitive(PrimitiveType.Quad);go.name="Player automatic Flipbook "+role;go.SetActive(false);go.layer=Layer;
                    go.transform.position=actor.transform.position;go.transform.rotation=actor.transform.rotation;go.transform.localScale=actor.transform.localScale;
                    var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=source;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
                    var helper=go.AddComponent<AnimationSheetHelper>();helper.xSize=2;helper.ySize=2;helper.manualPlay=false;helper.speed=2;
                    return helper;
                }
                bh=Create(c.current,"B",out bGO);ch=Create(c.graph,"C",out cGO);
                // Both real OnEnable calls happen in the same Player frame.
                bGO.SetActive(true);cGO.SetActive(true);b=bh.mat;d=ch.mat;
                Require(b&&d&&b!=c.current&&d!=c.graph&&b!=d,"Player Renderer.material must create distinct owned instances.");result.runtimeInstancesDistinct=true;
                var br=bGO.GetComponent<MeshRenderer>();var cr=cGO.GetComponent<MeshRenderer>();
                uint b0=sourceB.flags0,b1=sourceB.flags1,c0=sourceC.flags0,c1=sourceC.flags1;
                void CheckPair(bool enabled)
                {
                    ExpectedAnimation(bh,b,b0,b1,enabled);ExpectedAnimation(ch,d,c0,c1,enabled);
                    var x=State(bh,b);var y=State(ch,d);Require(x.helperFrame==y.helperFrame&&x.currentST.Equals(y.currentST)&&x.nextST.Equals(y.nextST)&&x.blend==y.blend,"Both actual automatic clocks must agree before rendering.");
                    Require(b.GetShaderPassEnabled(ForwardMode(b))&&d.GetShaderPassEnabled(ForwardMode(d)),"Actual Forward LightMode must remain enabled.");
                }
                Color[][] Capture(string stage)
                {
                    int frame=Time.frameCount;var sb=State(bh,b);var sc=State(ch,d);
                    Color[] Draw(bool current,string suffix)
                    {
                        br.enabled=current;cr.enabled=!current;camera.Render();return Read(target,c.identity+"-"+stage+"-"+suffix);
                    }
                    var x=Draw(true,"B");var xr=Draw(true,"Br");var y=Draw(false,"C");var yr=Draw(false,"Cr");
                    Require(Time.frameCount==frame,"Pair/repeat must stay in one real frame.");SameAnimation(sb,State(bh,b),"Rendering cannot advance B Helper.");SameAnimation(sc,State(ch,d),"Rendering cannot advance C Helper.");
                    var row=new FlipbookStage{name=stage,b=sb,c=sc,bc=Delta(x,y),bRepeat=Delta(x,xr),cRepeat=Delta(y,yr),bVisible=Visible(x),cVisible=Visible(y)};
                    result.stages.Add(row);File.AppendAllText(Path.Combine(folder,c.identity+"-stages.jsonl"),JsonUtility.ToJson(row)+"\n");
                    Require(row.bc==0&&row.bRepeat==0&&row.cRepeat==0,"Automatic Flipbook full-frame strict B/C/repeat mismatch.");Require(row.bVisible>150&&row.cVisible>150,"Automatic Flipbook must remain visible.");
                    return new[]{x,y};
                }
                string ForwardMode(Material material)
                {
                    int index=material.FindPass(material.shader==config.graphShader?"Universal Forward":"UniversalForward");Require(index>=0,"Forward variant/pass stripped.");
                    string mode=material.shader.FindPassTagValue(0,index,new ShaderTagId("LightMode")).name;return string.IsNullOrEmpty(mode)?"SRPDefaultUnlit":mode;
                }
                CheckPair(true);Require(bh.frameIndex==0&&ch.frameIndex==0&&State(bh,b).blend==0&&State(ch,d).blend==0,"Real OnEnable initializes the first frame.");
                var initial=Capture("init");int startFrame=Time.frameCount;float deadline=Time.realtimeSinceStartup+10;
                while(Phase(bh)<1.25f)
                {Require(Time.realtimeSinceStartup<deadline,"Automatic Player Update did not advance.");yield return new WaitForEndOfFrame();CheckPair(true);}
                Require(Time.frameCount>startFrame,"No hand Tick may replace actual frame advancement.");var automatic=Capture("auto");
                result.automaticResponseB=Delta(initial[0],automatic[0]);result.automaticResponseC=Delta(initial[1],automatic[1]);
                Require(result.automaticResponseB>.03f&&result.automaticResponseC>.03f,"Automatic frame progression needs strong pixel response.");
                var pauseB=State(bh,b);var pauseC=State(ch,d);float pausedPhase=Phase(bh);Time.timeScale=0;startFrame=Time.frameCount;
                for(int i=0;i<4;i++)
                {
                    yield return new WaitForEndOfFrame();Require(Time.deltaTime==0,"Pause must reach the actual engine clock.");CheckPair(true);
                    SameAnimation(pauseB,State(bh,b),"B moved during timeScale pause.");SameAnimation(pauseC,State(ch,d),"C moved during timeScale pause.");
                }
                result.pauseFrames=Time.frameCount-startFrame;Require(result.pauseFrames>=4,"Four actual paused frames required.");result.pauseHeld=true;
                var paused=Capture("pause");Require(Delta(automatic[0],paused[0])+Delta(automatic[1],paused[1])==0,"Paused rendering changed.");
                Time.timeScale=savedScale;deadline=Time.realtimeSinceStartup+10;
                while(Progress(pausedPhase,Phase(bh))<.5f)
                {Require(Time.realtimeSinceStartup<deadline,"Resume did not advance actual Update.");yield return new WaitForEndOfFrame();CheckPair(true);}
                var resumed=Capture("resume");result.resumeResponseB=Delta(paused[0],resumed[0]);result.resumeResponseC=Delta(paused[1],resumed[1]);
                Require(result.resumeResponseB>.03f&&result.resumeResponseC>.03f,"Resume needs strong pixel response.");
                var beforeDisableB=State(bh,b);var beforeDisableC=State(ch,d);bh.enabled=false;ch.enabled=false;CheckPair(false);
                var disabledB=State(bh,b);var disabledC=State(ch,d);
                Require(disabledB.currentST.Equals(beforeDisableB.currentST)&&disabledB.nextST.Equals(beforeDisableB.nextST)&&disabledB.blend==beforeDisableB.blend&&disabledC.currentST.Equals(beforeDisableC.currentST)&&disabledC.nextST.Equals(beforeDisableC.nextST)&&disabledC.blend==beforeDisableC.blend,"Disable must preserve sampled ST/blend.");
                startFrame=Time.frameCount;
                for(int i=0;i<4;i++)
                {
                    yield return new WaitForEndOfFrame();Require(Time.deltaTime>0,"Disabled check needs a running clock.");CheckPair(false);
                    SameAnimation(disabledB,State(bh,b),"Disabled B wrote animation.");SameAnimation(disabledC,State(ch,d),"Disabled C wrote animation.");
                }
                result.disabledFrames=Time.frameCount-startFrame;Require(result.disabledFrames>=4,"Four actual disabled frames required.");result.disabledHeld=true;Capture("disabled");
                bh.enabled=true;ch.enabled=true;CheckPair(true);
                Require(bh.mat==b&&ch.mat==d&&bh.frameIndex==0&&ch.frameIndex==0&&State(bh,b).blend==0&&State(ch,d).blend==0,"Reenable must reacquire the existing runtime instances and reset the clock/frame.");
                result.restartReset=true;var restart=Capture("restart");Require(Delta(initial[0],restart[0])+Delta(initial[1],restart[1])==0,"Reenabled first frame differs from original first frame.");
                deadline=Time.realtimeSinceStartup+10;
                while(Phase(bh)<1.25f)
                {Require(Time.realtimeSinceStartup<deadline,"Restarted automatic Update did not advance.");yield return new WaitForEndOfFrame();CheckPair(true);}
                var run2=Capture("run2");result.restartResponseB=Delta(restart[0],run2[0]);result.restartResponseC=Delta(restart[1],run2[1]);
                Require(result.restartResponseB>.03f&&result.restartResponseC>.03f,"Restart needs strong pixel response.");
                Object.Destroy(bh);Object.Destroy(ch);yield return null;yield return new WaitForEndOfFrame();
                Require(!bh&&!ch&&(Word(b,1)&HelperBit)==0&&(Word(d,1)&HelperBit)==0,"Real delayed destruction must release the Helper flag.");result.destroyCleared=true;
                SameAnimation(sourceB,State(null,c.current),"B source asset was changed by runtime instance lifecycle.");SameAnimation(sourceC,State(null,c.graph),"C source asset was changed by runtime instance lifecycle.");
                result.sourceHelperFieldsUnchanged=true;result.status="PASSED";
            }
            finally
            {
                Time.timeScale=savedScale;result.clockRestored=Time.timeScale==savedScale;
                if(bh)bh.enabled=false;if(ch)ch.enabled=false;
                if(bGO){bGO.SetActive(false);Object.Destroy(bGO);}if(cGO){cGO.SetActive(false);Object.Destroy(cGO);}
                if(b&&b!=c.current)Object.Destroy(b);if(d&&d!=c.graph)Object.Destroy(d);
                feature.SetActive(oldFeature);actor.enabled=oldActor;background.enabled=oldBackground;transparentBackground.enabled=oldTransparent;
            }
        }
    }
}
