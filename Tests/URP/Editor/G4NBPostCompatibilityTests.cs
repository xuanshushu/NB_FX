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
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
using Random=UnityEngine.Random;

namespace NBFX.Baseline.Tests
{
    // Versioned path comparison only. The existing 12/52 case catalogues stay unchanged.
    public sealed class G4NBPostCompatibilityTests
    {
        static IEnumerable<TestCaseData> Cases()
        {
            foreach(bool compatibility in new[]{false,true})
            {
                string path=compatibility?"Compatibility":"RG";
                yield return new TestCaseData("full","deferred","overlay",true,compatibility).SetName("NBPostPathV1_"+path+"_full_deferred_overlay_ortho");
                yield return new TestCaseData("full","opaque","flash",false,compatibility).SetName("NBPostPathV1_"+path+"_full_opaque_flash_perspective");
                yield return new TestCaseData("effects","opaque","ca",true,compatibility).SetName("NBPostPathV1_"+path+"_effects_opaque_ca_ortho");
            }
        }
        [TearDown] public void ForceCleanup()=>NBPostPathScope.CleanupActive();
        [TestCaseSource(nameof(Cases))]
        public void OriginalInputsThroughSelectedPath(string family,string mode,string effect,bool ortho,bool compatibility)
        {
            string root=Environment.GetEnvironmentVariable("NBFX_COMPAT_EVIDENCE_DIR");
            Assert.That(string.IsNullOrEmpty(root),Is.False,"Supply a new short evidence directory for this batch.");
            string id=(compatibility?"cm":"rg")+"-"+(family=="effects"?"ca":mode=="deferred"?"do":"of");
            var scope=new NBPostPathScope(Path.Combine(root,id),compatibility);
            try
            {
                Environment.SetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR",Path.Combine(scope.folder,"raw"));
                if(family=="full")new G4NBPostFullControllerTests().RunPathCore(mode,effect,ortho,compatibility,scope);
                else new G4NBPostEffectsControllerTests().RunPathCore(mode,effect,ortho,compatibility,scope);
            }
            finally{scope.Dispose();}
        }
    }

    internal sealed class NBPostPathScope:IDisposable
    {
        internal static readonly BindingFlags All=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
        static readonly List<NBPostPathScope> Active=new List<NBPostPathScope>();
        internal readonly string folder;
        readonly bool compatibility;
        readonly ScriptableRendererData renderer;
        readonly ScriptableRendererFeature[] features;
        readonly string[] featureJSON;
        readonly Type managerType,controllerType,featureType,postPassType;
        readonly Dictionary<FieldInfo,object> managerStatics;
        readonly object flags;
        readonly Material flagsMaterial,oldPost,oldPassPost;
        readonly Material materialSnapshot;
        readonly string materialJSON,oldEnvironment,oldSceneJSON;
        readonly Random.State randomState;
        readonly RenderTexture previousRT;
        readonly Dictionary<string,byte[]> assetBytes=new Dictionary<string,byte[]>();
        readonly List<Component> components=new List<Component>();
        readonly List<NBPostFullRTObserver> observers=new List<NBPostFullRTObserver>();
        readonly List<Material> transientPosts=new List<Material>();
        readonly List<Action> fallbacks=new List<Action>();
        bool disposed;
        [Serializable] internal sealed class Before
        {
            public string project,unity,api,sceneJSON,environment,rendererPath,pipelinePath,globalSettingsPath,materialJSON,runnerBootstrapFingerprint;
            public string[] featureJSON,assetPaths,assetSHA256,staticFields,staticValues;
            public int[] featureIDs;
            public int oldPostID,oldPassPostID,oldFlagsMaterialID,nbCallbacks;
            public bool compatibility,rawCompatibility,rendererDirty;
        }
        [Serializable] internal sealed class Cleanup
        {
            public bool viaTearDown,rendererFeaturesRestored,featureStateRestored,assetsRestored,componentsRemoved,callbacksRemoved;
            public bool staticsRestored,flagsBindingRestored,materialRestored,sceneRestored,environmentRestored,pathRestored,renderTargetRestored,randomStateRestored,allRestored;
            public string[] errors;
        }
        internal static Type Find(string name)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name,false)).First(t=>t!=null);
        internal static string SHA(byte[] bytes){using(var h=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(h.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
        static string Value(object o)=>o is Object u?(u?u.GetType().FullName+":"+u.GetInstanceID():"<null>"):o==null?"<null>":o.ToString();
        static string Scenes()=>string.Join("\n",Enumerable.Range(0,SceneManager.sceneCount).Select(i=>{var s=SceneManager.GetSceneAt(i);return s.handle+"|"+s.name+"|"+s.path+"|"+s.isLoaded+"|"+s.isDirty+"|"+(s==SceneManager.GetActiveScene())+"|"+string.Join(",",s.GetRootGameObjects().Select(g=>g.GetInstanceID()).OrderBy(n=>n));}));
        static Delegate[] NBCallbacks()=>(EditorApplication.update?.GetInvocationList()??Array.Empty<Delegate>()).Where(d=>d.Method.DeclaringType?.FullName=="NBShader.PostProcessingManager"||d.Method.DeclaringType?.FullName=="NBShader.PostProcessingController").ToArray();
        internal NBPostPathScope(string directory,bool expectedCompatibility)
        {
            folder=Path.GetFullPath(directory);compatibility=expectedCompatibility;
            NBPostCompatibilityControl.Guard();
            NBPostCompatibilityControl.RequireCompiledPath();
            Assert.That(GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>().enableRenderCompatibilityMode,Is.EqualTo(compatibility));
            Assert.That((bool)typeof(UniversalRenderPipeline).GetField("useRenderGraph",All).GetValue(null),Is.EqualTo(!compatibility));
            Assert.That(File.Exists(Path.Combine(folder,"before.json")),Is.False,"Never overwrite an earlier attempt.");
            var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;Assert.That(pipeline,Is.Not.Null);
            renderer=pipeline.rendererDataList[0];Assert.That(renderer,Is.Not.Null);
            features=renderer.rendererFeatures.ToArray();featureJSON=features.Select(f=>f?EditorJsonUtility.ToJson(f):"<null>").ToArray();
            Assert.That(EditorUtility.IsDirty(renderer),Is.False,"Renderer must start clean.");
            managerType=Find("NBShader.PostProcessingManager");controllerType=Find("NBShader.PostProcessingController");featureType=Find("NBShader.NBPostProcess");postPassType=Find("NBShader.NBPostProcessRenderPass");
            foreach(var t in new[]{managerType,controllerType})Assert.That(Resources.FindObjectsOfTypeAll(t).OfType<Component>().Any(c=>c&&c.gameObject.scene.IsValid()&&c.gameObject.scene.isLoaded),Is.False,"No user NB lifecycle components may be loaded.");
            Assert.That(NBCallbacks(),Is.Empty,"No stale NB callbacks may be present.");
            oldPost=featureType.GetField("NBPostProcessMaterial",All).GetValue(null)as Material;
            oldPassPost=postPassType.GetField("_material",All).GetValue(null)as Material;
            Assert.That(oldPost&&!AssetDatabase.Contains(oldPost),Is.True,"Preflight must initialize the original renderer before this test.");
            materialJSON=EditorJsonUtility.ToJson(oldPost);
            managerStatics=managerType.GetFields(All).Where(f=>f.IsStatic&&!f.IsInitOnly&&!f.IsLiteral).ToDictionary(f=>f,f=>f.GetValue(null));
            flags=managerType.GetField("flags",All).GetValue(null);flagsMaterial=flags.GetType().GetMethod("GetMaterial",All).Invoke(flags,null)as Material;
            oldEnvironment=Environment.GetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR");oldSceneJSON=Scenes();randomState=Random.state;previousRT=RenderTexture.active;
            var global=UnityEditor.Rendering.EditorGraphicsSettings.GetRenderPipelineGlobalSettingsAsset<UniversalRenderPipeline>();
            foreach(var asset in new Object[]{renderer,pipeline,global}){string path=AssetDatabase.GetAssetPath(asset);Assert.That(string.IsNullOrEmpty(path),Is.False);assetBytes[path]=File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(Application.dataPath),path));}
            string bootstrap=null;
            if(SceneManager.sceneCount==1&&string.IsNullOrEmpty(SceneManager.GetActiveScene().path)&&SceneManager.GetActiveScene().GetRootGameObjects().Length==2)bootstrap=NBPostCompatibilityControl.RunnerFingerprint(SceneManager.GetActiveScene());
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder,"before.json"),JsonUtility.ToJson(new Before{project=Application.dataPath,unity=Application.unityVersion,api=SystemInfo.graphicsDeviceType.ToString(),sceneJSON=oldSceneJSON,environment=oldEnvironment,rendererPath=AssetDatabase.GetAssetPath(renderer),pipelinePath=AssetDatabase.GetAssetPath(pipeline),globalSettingsPath=AssetDatabase.GetAssetPath(global),materialJSON=materialJSON,featureJSON=featureJSON,featureIDs=features.Select(f=>f?f.GetInstanceID():0).ToArray(),assetPaths=assetBytes.Keys.ToArray(),assetSHA256=assetBytes.Values.Select(SHA).ToArray(),staticFields=managerStatics.Keys.Select(f=>f.Name).ToArray(),staticValues=managerStatics.Values.Select(Value).ToArray(),oldPostID=oldPost.GetInstanceID(),oldPassPostID=oldPassPost?oldPassPost.GetInstanceID():0,oldFlagsMaterialID=flagsMaterial?flagsMaterial.GetInstanceID():0,nbCallbacks=0,compatibility=compatibility,rawCompatibility=(bool)typeof(RenderGraphSettings).GetField("m_EnableRenderCompatibilityMode",All).GetValue(GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>()),rendererDirty=false},true));
            var beforeReceipt=JsonUtility.FromJson<Before>(File.ReadAllText(Path.Combine(folder,"before.json")));beforeReceipt.runnerBootstrapFingerprint=bootstrap;File.WriteAllText(Path.Combine(folder,"before.json"),JsonUtility.ToJson(beforeReceipt,true));
            Active.Add(this); // before first Unity mutation, including material snapshot construction
            materialSnapshot=new Material(oldPost){hideFlags=HideFlags.HideAndDontSave};
        }
        internal void RegisterFallback(Action cleanup)=>fallbacks.Add(cleanup);
        internal void Register(NBPostFullRTObserver observer){observers.Add(observer);observer.independentReadback=true;observer.nativeFeature=features.Single(f=>f&&f.GetType().FullName=="NBShader.NBPostProcess");}
        internal void Track(Component c){if(c&&!components.Contains(c))components.Add(c);}
        internal void TrackPost(Material p){if(p&&p!=oldPost&&!transientPosts.Contains(p))transientPosts.Add(p);}
        internal void Observe(string label,NBPostFullRTObserver observer,Camera camera,int view,int beforeQueue,int beforeRG,int beforeExecute,Material[] shaders,Color[] raw)
        {
            observer.WriteReceipt(Path.Combine(folder,label+"-path.json"));
            Assert.That(observer.queueReads-beforeQueue,Is.GreaterThanOrEqualTo(4),"Actual camera did not traverse the original feature queue.");
            Assert.That(observer.lastCameraID,Is.EqualTo(camera.GetInstanceID()));
            foreach(var name in new[]{"NBShader.RenderCameraOpaqueDistortObjectPass","NBShader.ScreenColorRenderPass","NBShader.DisturbanceMaskRenderPass","NBShader.NBPostProcessRenderPass"})Assert.That(observer.passTypes,Does.Contain(name));
            if(view>=0)
            {
                Assert.That(compatibility?observer.executeReads-beforeExecute:observer.graphReads-beforeRG,Is.GreaterThanOrEqualTo(4));
                Assert.That(compatibility?observer.graphReads-beforeRG:observer.executeReads-beforeExecute,Is.Zero);
                Assert.That(observer.lastReadView,Is.EqualTo(view));
                Assert.That(observer.observationHandle!=null&&observer.observationHandle.rt&&observer.observationHandle.rt.IsCreated(),Is.True);
                if(compatibility)Assert.That(observer.nativeRTs.All(r=>r.valid),Is.True,"Original compatibility RTHandle was absent/uncreated.");
            }
            var errors=shaders.Where(m=>m).SelectMany(m=>ShaderUtil.GetShaderMessages(m.shader).Where(e=>e.severity.ToString()=="Error").Select(e=>m.shader.name+": "+e.message)).ToArray();
            File.WriteAllLines(Path.Combine(folder,label+"-shader-errors.txt"),errors);Assert.That(errors,Is.Empty);
            float poison=BitConverter.ToSingle(new byte[]{0xcd,0xcd,0xcd,0xcd},0);
            Assert.That(raw.Length,Is.EqualTo(128*128));Assert.That(raw.All(c=>Enumerable.Range(0,4).All(k=>!float.IsNaN(c[k])&&!float.IsInfinity(c[k]))),Is.True);
            Assert.That(raw.All(c=>c.r==poison&&c.g==poison&&c.b==poison&&c.a==poison),Is.False);
        }
        public void Dispose()=>Dispose(false);
        void Dispose(bool viaTearDown)
        {
            if(disposed)return;
            File.WriteAllText(Path.Combine(folder,"cleanup-started.json"),"{\"viaTearDown\":"+(viaTearDown?"true":"false")+"}");
            var errors=new List<string>();Action<Action> attempt=a=>{try{a();}catch(Exception e){errors.Add(e.ToString());}};
            foreach(var action in fallbacks.AsEnumerable().Reverse())attempt(action);
            foreach(var c in components.Where(c=>c).ToArray())attempt(()=>Object.DestroyImmediate(c.gameObject));
            foreach(var o in observers)if(o)attempt(()=>{renderer.rendererFeatures.Remove(o);Object.DestroyImmediate(o);});
            attempt(()=>{foreach(var p in managerStatics)p.Key.SetValue(null,p.Value);flags.GetType().GetMethod("SetMaterial",All).Invoke(flags,new object[]{flagsMaterial});});
            attempt(()=>{if(oldPost&&materialSnapshot)oldPost.CopyPropertiesFromMaterial(materialSnapshot);featureType.GetField("NBPostProcessMaterial",All).SetValue(null,oldPost);postPassType.GetField("_material",All).SetValue(null,oldPassPost);});
            foreach(var p in transientPosts)if(p&&p!=oldPost&&p!=oldPassPost)attempt(()=>Object.DestroyImmediate(p));
            attempt(()=>{RenderTexture.active=previousRT;Random.state=randomState;Environment.SetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR",oldEnvironment);});
            if(materialSnapshot)attempt(()=>Object.DestroyImmediate(materialSnapshot));
            var r=new Cleanup{viaTearDown=viaTearDown,rendererFeaturesRestored=renderer.rendererFeatures.SequenceEqual(features),featureStateRestored=features.Select(f=>f?EditorJsonUtility.ToJson(f):"<null>").SequenceEqual(featureJSON),assetsRestored=assetBytes.All(p=>File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(Application.dataPath),p.Key)).SequenceEqual(p.Value)),componentsRemoved=components.All(c=>!c)&&!new[]{managerType,controllerType}.Any(t=>Resources.FindObjectsOfTypeAll(t).OfType<Component>().Any(c=>c&&c.gameObject.scene.IsValid()&&c.gameObject.scene.isLoaded)),callbacksRemoved=NBCallbacks().Length==0,staticsRestored=managerStatics.All(p=>Equals(p.Value,p.Key.GetValue(null))),flagsBindingRestored=ReferenceEquals(flagsMaterial,flags.GetType().GetMethod("GetMaterial",All).Invoke(flags,null)),materialRestored=oldPost&&EditorJsonUtility.ToJson(oldPost)==materialJSON&&ReferenceEquals(featureType.GetField("NBPostProcessMaterial",All).GetValue(null),oldPost)&&ReferenceEquals(postPassType.GetField("_material",All).GetValue(null),oldPassPost),sceneRestored=Scenes()==oldSceneJSON,environmentRestored=Environment.GetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR")==oldEnvironment,pathRestored=GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>().enableRenderCompatibilityMode==compatibility,renderTargetRestored=RenderTexture.active==previousRT,errors=errors.ToArray()};
            r.randomStateRestored=Random.state.Equals(randomState);
            r.allRestored=r.rendererFeaturesRestored&&r.featureStateRestored&&r.assetsRestored&&r.componentsRemoved&&r.callbacksRemoved&&r.staticsRestored&&r.flagsBindingRestored&&r.materialRestored&&r.sceneRestored&&r.environmentRestored&&r.pathRestored&&r.renderTargetRestored&&r.randomStateRestored&&errors.Count==0;
            File.WriteAllText(Path.Combine(folder,"cleanup.json"),JsonUtility.ToJson(r,true));
            disposed=true;Active.Remove(this);Assert.That(r.allRestored,Is.True,"See durable cleanup.json; do not dispatch another test until actual restoration is verified.");
        }
        internal static void CleanupActive(){foreach(var s in Active.ToArray())s.Dispose(true);}
        internal static bool HasActive=>Active.Count!=0;
    }
}
