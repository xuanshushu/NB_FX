using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using NBShader;
using Object=UnityEngine.Object;

namespace NBFX.PlayerValidation
{
    // Separate observational entry. Frozen correctness harness/config source stays byte-identical.
    public sealed class NBFXMeshPerformanceSampler:MonoBehaviour
    {
        public NBFXMeshPlayerConfig config;
        const int WarmFrames=120,SampleFrames=240,Repeats=4;
        static readonly string[] PairOrder={"BC","CB","CB","BC"};
        static readonly string[] CounterNames={"Main Thread","Render Thread","GPU Frame Time","Draw Calls Count","SetPass Calls Count","Batches Count"};
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        NBFXMeshPlayerHarness world;new Camera camera;MeshRenderer actor,background,transparent;
        PostProcessingController controller;NBPostProcess feature;
        RenderPipelineAsset previousPipeline;int oldVsync,oldRate,oldInterval;bool oldFeature;
        readonly List<Block> blocks=new List<Block>();
        readonly Dictionary<string,string> renderedInputs=new Dictionary<string,string>();
        readonly List<ProfilerRecorder> recorders=new List<ProfilerRecorder>();
        readonly List<Counter> counters=new List<Counter>();
        readonly FrameTiming[] timing=new FrameTiming[1];
        readonly WaitForEndOfFrame endOfFrame=new WaitForEndOfFrame();
        string folder;bool finished,disposed,configurationCaptured;string failure;
        [Serializable] sealed class Counter {public string requestedName,name,category,unit,availability,error;public bool valid;}
        [Serializable] struct Sample
        {
            public int frame;public uint returnedTimings;public ulong timestamp;
            public bool freshTiming,cpuAvailable,gpuAvailable;
            public double cpuMs,cpuMainMs,cpuRenderMs,cpuPresentWaitMs,gpuMs;
            public long mainThread,renderThread,gpuCounter,drawCalls,setPassCalls,batches;
            public int recorderValidMask;
        }
        [Serializable] sealed class Block
        {
            public string inputIdentity,route,role,status,shader,lightMode;public string[] keywords;
            public int repetition,warmFrames,sampleFrames,firstFrame,lastFrame,visibleMeshCount,materialPassInventory;
            public double actualSampleSeconds;public bool originalNBPostEnabled,controllerEnabled;
            public Sample[] samples;public Counter[] counters;public string preflightImageSHA256;
        }
        [Serializable] sealed class Receipt
        {
            public string status,error,scope,buildIdentity,sourceLockSHA256,unity,platform,api,gpu,graphicsDeviceVersion,cpu,arguments,downsampling;
            public bool applicationIsEditor,frameTimingEnabled,configurationRestored,performanceBudgetDefined;
            public int pointerBytes,warmFrames,sampleFrames,repeats,renderWidth,renderHeight,screenWidth,screenHeight,vsync,targetFrameRate,renderFrameInterval;
            public ulong cpuTimerFrequency,gpuTimerFrequency;
            public string[] pairOrder;public Counter[] counters;public Block[] blocks;
            public string[] unavailableMetrics;
        }
        static bool HasArg(string n)=>Array.IndexOf(Environment.GetCommandLineArgs(),n)>=0;
        static string Arg(string n){var a=Environment.GetCommandLineArgs();int i=Array.IndexOf(a,n);return i>=0&&i+1<a.Length?a[i+1]:null;}
        static void Need(bool value,string why){if(!value)throw new InvalidOperationException(why);}
        T Field<T>(string n)=>(T)typeof(NBFXMeshPlayerHarness).GetField(n,Private).GetValue(world);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void SelectEntry()
        {
            if(!HasArg("--nbfx-performance"))return;
            SceneManager.sceneLoaded+=OnEntryScene;
        }
        static void OnEntryScene(Scene scene,LoadSceneMode mode)
        {
            SceneManager.sceneLoaded-=OnEntryScene;
            foreach(var h in Object.FindObjectsByType<NBFXMeshPlayerHarness>(FindObjectsInactive.Include,FindObjectsSortMode.None))h.enabled=false;
            SceneManager.LoadSceneAsync("NBFXPerformance",LoadSceneMode.Single);
        }
        void Start()
        {
            folder=Arg("--nbfx-perf-evidence");
            if(string.IsNullOrEmpty(folder)){Debug.LogError("Missing --nbfx-perf-evidence");Application.Quit(2);return;}
            folder=Path.GetFullPath(folder);Directory.CreateDirectory(folder);
            try
            {
                Need(!Application.isEditor&&Application.platform==RuntimePlatform.WindowsPlayer&&IntPtr.Size==8,"Actual Windows64 Player required.");
                Need(SystemInfo.graphicsDeviceType==GraphicsDeviceType.Direct3D11,"Fixed D3D11 experiment required.");
                Need(config&&config.cases!=null&&config.cases.Length==6,"Retained original inputs missing.");
                previousPipeline=QualitySettings.renderPipeline;oldVsync=QualitySettings.vSyncCount;oldRate=Application.targetFrameRate;oldInterval=OnDemandRendering.renderFrameInterval;configurationCaptured=true;
                QualitySettings.renderPipeline=config.pipeline;QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;OnDemandRendering.renderFrameInterval=1;
                Need(!GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>().enableRenderCompatibilityMode,"Performance protocol is RG only.");
                feature=config.pipeline.rendererDataList[0].rendererFeatures.OfType<NBPostProcess>().Single();oldFeature=feature.isActive;
                var host=new GameObject("Frozen harness workload provider");host.SetActive(false);world=host.AddComponent<NBFXMeshPlayerHarness>();world.config=config;world.enabled=false;
                // Setup is the existing test-world construction only, not production Update/Init.
                typeof(NBFXMeshPlayerHarness).GetMethod("Setup",Private).Invoke(world,null);
                camera=Field<Camera>("camera");actor=Field<MeshRenderer>("actor");background=Field<MeshRenderer>("background");transparent=Field<MeshRenderer>("transparentBackground");controller=Field<PostProcessingController>("controller");
                camera.allowDynamicResolution=false;camera.enabled=true;
                Need(camera.targetTexture&&camera.targetTexture.width==128&&camera.targetTexture.height==128&&!camera.targetTexture.sRGB,"Frozen 128x128 linear workload target changed.");
                File.WriteAllText(Path.Combine(folder,"started.json"),JsonUtility.ToJson(BuildReceipt("STARTED"),true));
                StartCoroutine(Drive());
            }
            catch(Exception e){failure=e.ToString();Finish(2);}
        }
        IEnumerator Drive()
        {
            var stack=new Stack<IEnumerator>();stack.Push(Run());
            while(stack.Count>0)
            {
                bool more=false;object value=null;Exception error=null;
                try{var top=stack.Peek();more=top.MoveNext();if(more)value=top.Current;else(stack.Pop()as IDisposable)?.Dispose();}
                catch(Exception e){error=e;}
                if(error!=null){failure=error.ToString();while(stack.Count>0)(stack.Pop()as IDisposable)?.Dispose();Finish(2);yield break;}
                if(more){if(value is IEnumerator nested)stack.Push(nested);else yield return value;}
            }
            Finish(0);
        }
        IEnumerator Run()
        {
            foreach(var input in config.cases.Where(c=>c.orthographic))
            {
                Need(new[]{"Forward","NBCameraOpaqueDistortPass","NBDeferredDistortPass"}.Contains(input.route),"Unexpected workload axis.");
                camera.orthographic=true;bool post=input.route!="Forward";
                feature.SetActive(post);background.enabled=post;transparent.enabled=post;controller.gameObject.SetActive(false);actor.enabled=true;actor.sharedMaterial=input.currentOn;
                // Initial renderer/resource warmup outside measured blocks.
                for(int i=0;i<8;i++)yield return endOfFrame;
                controller.gameObject.SetActive(post);
                for(int repetition=0;repetition<Repeats;repetition++)foreach(char role in PairOrder[repetition])
                {
                    actor.sharedMaterial=role=='B'?input.currentOn:input.graphOn;
                    var material=actor.sharedMaterial;Need(material&&material.shader.isSupported,"Retained B/C shader unavailable.");
                    int pass=material.FindPass(input.route=="Forward"?(role=='B'?"UniversalForward":"Universal Forward"):input.route);Need(pass>=0,"Original intended pass missing.");
                    string tag=material.shader.FindPassTagValue(0,pass,new ShaderTagId("LightMode")).name;if(string.IsNullOrEmpty(tag))tag="SRPDefaultUnlit";
                    Need(material.GetShaderPassEnabled(tag),"Actual selected LightMode is disabled.");
                    StopRecorders();CreateRecorders();
                    string imageSHA=null;
                    for(int i=0;i<WarmFrames;i++)
                    {
                        FrameTimingManager.CaptureFrameTimings();yield return endOfFrame;
                        if(i==WarmFrames/2-1)imageSHA=ValidateRenderedInput(input.route,repetition,role);
                    }
                    FrameTimingManager.GetLatestTimings(1,timing);ulong last=timing[0].frameStartTimestamp;
                    var block=new Block{inputIdentity=input.identity,route=input.route,role=role.ToString(),repetition=repetition,warmFrames=WarmFrames,sampleFrames=SampleFrames,shader=material.shader.name,lightMode=tag,keywords=material.shaderKeywords,visibleMeshCount=post?3:1,materialPassInventory=material.passCount,originalNBPostEnabled=feature.isActive,controllerEnabled=controller.isActiveAndEnabled,samples=new Sample[SampleFrames],status="COLLECTED"};
                    block.preflightImageSHA256=imageSHA;block.counters=counters.ToArray();blocks.Add(block);block.firstFrame=Time.frameCount;double start=Time.realtimeSinceStartupAsDouble;
                    // TIMED-BEGIN: fixed B/C state, preallocated buffers. No I/O, readback,
                    // RPC, material changes, log, reflection, LINQ, or string formatting.
                    for(int i=0;i<SampleFrames;i++)
                    {
                        FrameTimingManager.CaptureFrameTimings();yield return endOfFrame;
                        uint count=FrameTimingManager.GetLatestTimings(1,timing);FrameTiming t=timing[0];
                        bool fresh=count>0&&t.frameStartTimestamp>last;if(fresh)last=t.frameStartTimestamp;
                        Sample s=new Sample{frame=Time.frameCount,returnedTimings=count,timestamp=t.frameStartTimestamp,freshTiming=fresh,cpuAvailable=fresh&&t.cpuFrameTime>0&&Finite(t.cpuFrameTime),gpuAvailable=fresh&&t.gpuFrameTime>0&&Finite(t.gpuFrameTime),cpuMs=t.cpuFrameTime,cpuMainMs=t.cpuMainThreadFrameTime,cpuRenderMs=t.cpuRenderThreadFrameTime,cpuPresentWaitMs=t.cpuMainThreadPresentWaitTime,gpuMs=t.gpuFrameTime};
                        for(int n=0;n<recorders.Count;n++)
                        {
                            var r=recorders[n];if(!r.Valid||!r.IsRunning||r.Count==0)continue;
                            s.recorderValidMask|=1<<n;long v=r.LastValue;
                            switch(n){case 0:s.mainThread=v;break;case 1:s.renderThread=v;break;case 2:s.gpuCounter=v;break;case 3:s.drawCalls=v;break;case 4:s.setPassCalls=v;break;case 5:s.batches=v;break;}
                        }
                        block.samples[i]=s;
                    }
                    // TIMED-END
                    block.actualSampleSeconds=Time.realtimeSinceStartupAsDouble-start;block.lastFrame=Time.frameCount;StopRecorders();
                    Need(block.lastFrame-block.firstFrame>=SampleFrames,"Real Player frames did not advance.");
                    Need(actor.sharedMaterial==material&&camera.targetTexture.width==128&&camera.targetTexture.height==128&&QualitySettings.vSyncCount==0&&OnDemandRendering.renderFrameInterval==1,"Workload changed during sample window.");
                    File.WriteAllText(Path.Combine(folder,input.route+"-r"+repetition+"-"+role+".json"),JsonUtility.ToJson(block,true));
                }
            }
        }
        static bool Finite(double v)=>!double.IsNaN(v)&&!double.IsInfinity(v);
        string ValidateRenderedInput(string route,int repetition,char role)
        {
            // Outside the timed window, followed by another 60 fixed warm frames.
            // Reject a silently non-rendering or different workload before comparing timings.
            var texture=Field<Texture2D>("read");var old=RenderTexture.active;Color[] pixels;
            try{RenderTexture.active=camera.targetTexture;texture.ReadPixels(new Rect(0,0,128,128),0,0,false);texture.Apply(false,false);pixels=texture.GetPixels();}finally{RenderTexture.active=old;}
            Need(pixels.All(p=>new[]{p.r,p.g,p.b,p.a}.All(v=>!float.IsNaN(v)&&!float.IsInfinity(v)&&v!=-23.203125f&&v!=-431602080f)),"Performance preflight buffer unhealthy.");
            Need(pixels.Count(p=>Mathf.Max(p.r,Mathf.Max(p.g,p.b))>.15f)>150,"Performance workload invisible.");
            byte[] bytes;using(var stream=new MemoryStream()){using(var writer=new BinaryWriter(stream)){foreach(var p in pixels){writer.Write(p.r);writer.Write(p.g);writer.Write(p.b);writer.Write(p.a);}bytes=stream.ToArray();}}
            string hash;using(var digest=System.Security.Cryptography.SHA256.Create())hash=BitConverter.ToString(digest.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();
            File.WriteAllBytes(Path.Combine(folder,route+"-r"+repetition+"-"+role+"-preflight.rgba32f"),bytes);
            if(renderedInputs.TryGetValue(route,out string expected))Need(hash==expected,"Measured B/C/repeat workload does not render the same frame.");else renderedInputs.Add(route,hash);
            return hash;
        }
        void CreateRecorders()
        {
            var available=new List<ProfilerRecorderHandle>();ProfilerRecorderHandle.GetAvailable(available);
            counters.Clear();recorders.Clear();
            foreach(var requested in CounterNames)
            {
                var matches=available.Where(h=>ProfilerRecorderHandle.GetDescription(h).Name==requested).ToArray();
                var c=new Counter{requestedName=requested,availability=matches.Length==0?"UNAVAILABLE":matches.Length>1?"AMBIGUOUS":"AVAILABLE"};var r=default(ProfilerRecorder);
                if(matches.Length==1)
                {
                    var d=ProfilerRecorderHandle.GetDescription(matches[0]);c.name=d.Name;c.category=d.Category.Name;c.unit=d.UnitType.ToString();
                    try{r=new ProfilerRecorder(matches[0],1,ProfilerRecorderOptions.StartImmediately|ProfilerRecorderOptions.WrapAroundWhenCapacityReached|ProfilerRecorderOptions.SumAllSamplesInFrame);c.valid=r.Valid;if(!c.valid)c.availability="INVALID_HANDLE";}catch(Exception e){c.error=e.Message;c.availability="START_FAILED";}
                }
                recorders.Add(r);counters.Add(c);
            }
        }
        void StopRecorders(){foreach(var r in recorders){var value=r;if(value.Valid){value.Stop();value.Dispose();}}recorders.Clear();}
        Receipt BuildReceipt(string status)=>new Receipt{status=status,error=failure,scope="Three frozen orthographic workloads; whole-frame FTM and enumerated profiler counters. Observation only, no user performance budget/pass verdict. CPU frame includes waits; render-thread metric is not pure draw submission. Material pass inventory is not executed pass count.",buildIdentity=config?config.buildIdentity:null,sourceLockSHA256=config?config.sourceLockSHA256:null,unity=Application.unityVersion,platform=Application.platform.ToString(),api=SystemInfo.graphicsDeviceType.ToString(),gpu=SystemInfo.graphicsDeviceName,graphicsDeviceVersion=SystemInfo.graphicsDeviceVersion,cpu=SystemInfo.processorType,arguments=string.Join(" ",Environment.GetCommandLineArgs()),downsampling=feature?feature.downSampling.ToString():null,applicationIsEditor=Application.isEditor,pointerBytes=IntPtr.Size,frameTimingEnabled=FrameTimingManager.IsFeatureEnabled(),cpuTimerFrequency=FrameTimingManager.GetCpuTimerFrequency(),gpuTimerFrequency=FrameTimingManager.GetGpuTimerFrequency(),warmFrames=WarmFrames,sampleFrames=SampleFrames,repeats=Repeats,pairOrder=PairOrder,renderWidth=128,renderHeight=128,screenWidth=Screen.width,screenHeight=Screen.height,vsync=0,targetFrameRate=-1,renderFrameInterval=1,counters=counters.ToArray(),blocks=blocks.ToArray(),performanceBudgetDefined=false,unavailableMetrics=new[]{"Exact executed shader/pass inventory (Draw/SetPass counters are separate metrics)","Per-draw GPU time and shader instruction/texture-sampling count","AssetBundle/SVC loaded variant retention"}};
        void Cleanup()
        {
            if(disposed)return;disposed=true;StopRecorders();
            if(controller)controller.gameObject.SetActive(false);if(camera)camera.enabled=false;
            if(world){foreach(var o in Field<List<Object>>("owned"))if(o)Object.Destroy(o);Object.Destroy(world.gameObject);}
            if(feature)feature.SetActive(oldFeature);if(configurationCaptured){QualitySettings.renderPipeline=previousPipeline;QualitySettings.vSyncCount=oldVsync;Application.targetFrameRate=oldRate;OnDemandRendering.renderFrameInterval=oldInterval;}
        }
        void Finish(int exit)
        {
            if(finished)return;finished=true;
            try
            {
                var receipt=BuildReceipt(exit==0?"COLLECTED_NO_BUDGET_VERDICT":"FAILED_OR_UNAVAILABLE");
                Cleanup();receipt.configurationRestored=!configurationCaptured||(QualitySettings.renderPipeline==previousPipeline&&QualitySettings.vSyncCount==oldVsync&&Application.targetFrameRate==oldRate&&OnDemandRendering.renderFrameInterval==oldInterval);
                File.WriteAllText(Path.Combine(folder,"performance-result.json"),JsonUtility.ToJson(receipt,true));
            }
            finally{Application.Quit(exit);}
        }
        void OnDestroy(){Cleanup();}
    }
}
