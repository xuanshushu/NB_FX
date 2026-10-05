using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace NBFX.Baseline.Tests
{
    // Existing realURP camera, F0 material setup and VAT raw-draw helper only.
    // No forced renderer feature/DrawRenderer or product asset changes.
    public sealed class G4BackFirstSharedLifecycleGpuTests
    {
        const string Package = "Packages/com.xuanxuan.nb.fx/";
        const BindingFlags Flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        readonly List<Object> owned = new List<Object>();
        T Keep<T>(T value) where T : Object { owned.Add(value); return value; }
        static Type Find(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name, false)).First(t => t != null);
        static object Invoke(MethodInfo method, params object[] args)
        {
            Assert.That(method, Is.Not.Null);
            try { return method.Invoke(null, args); }
            catch (TargetInvocationException e) { ExceptionDispatchInfo.Capture(e.InnerException ?? e).Throw(); throw; }
        }
        static object Service(string method, params object[] args) => Invoke(Find("NBShaderEditor.NBShaderSyncService").GetMethod(method, Flags), args);
        [OneTimeSetUp] public void Warm()
        {
            Assert.That(Application.dataPath.Replace('\\', '/'), Is.EqualTo("D:/UnityProject/NBUnityProject/.utmp/NBFXMeshValidation-20261002/Assets").IgnoreCase);
            for (int i = 0; i < SceneManager.sceneCount; ++i)
                Assert.That((SceneManager.GetSceneAt(i).name + "/" + SceneManager.GetSceneAt(i).path).IndexOf("TAI", StringComparison.OrdinalIgnoreCase), Is.LessThan(0));
            // Existing installed graph imports are reused; real camera below initializes URP before pass transactions.
        }
        [TearDown] public void Clean() { for (int i = owned.Count - 1; i >= 0; --i) if (owned[i]) Object.DestroyImmediate(owned[i]); owned.Clear(); }
        static IEnumerable<TestCaseData> Cases()
        {
            yield return new TestCaseData("modern-on", false, true)
                .SetName("G4BackFirstSharedGPU_TierDenyRestoreAlphaOrder_positive_ortho");
        }
        object MakeGraphRoot(Material material)
        {
            var type = Find("NBShaderEditor.NBShaderRootItem");var root=Activator.CreateInstance(type);
            var flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
            var editor=Keep((MaterialEditor)Editor.CreateEditor(material,typeof(MaterialEditor)));
            type.GetField("MatEditor",flags).SetValue(root,editor);type.GetField("Mats",flags).SetValue(root,new List<Material>{material});
            type.GetField("Shader",flags).SetValue(root,material.shader);type.GetMethod("InitFlags",flags).Invoke(root,new object[]{new List<Material>{material}});
            var dictionary=(System.Collections.IDictionary)type.GetField("PropertyInfoDic",flags).GetValue(root);
            foreach(var p in MaterialEditor.GetMaterialProperties(new Object[]{material}))
            {
                var info=Activator.CreateInstance(Find("NBShaderEditor.ShaderPropertyInfo"));info.GetType().GetField("Property",flags).SetValue(info,p);
                info.GetType().GetField("Name",flags).SetValue(info,p.name);info.GetType().GetField("Index",flags).SetValue(info,material.shader.FindPropertyIndex(p.name));dictionary.Add(p.name,info);
            }
            var context=Activator.CreateInstance(Find("NBShaderEditor.NBShaderGUIContext"),root);type.GetProperty("Context",flags).SetValue(root,context);
            type.GetProperty("SyncService",flags).SetValue(root,Activator.CreateInstance(Find("NBShaderEditor.NBShaderSyncService"),root));
            context.GetType().GetMethod("Refresh",flags).Invoke(context,null);return root;
        }
        Mesh TwinFaces()
        {
            var mesh = Keep(new Mesh());
            mesh.vertices = new[] { new Vector3(-1,-1,.04f), new Vector3(1,-1,.04f), new Vector3(-1,1,.04f), new Vector3(1,1,.04f),
                new Vector3(-1,-1,-.04f), new Vector3(1,-1,-.04f), new Vector3(-1,1,-.04f), new Vector3(1,1,-.04f) };
            mesh.triangles = new[] { 0,1,2, 2,1,3, 6,5,4, 7,5,6 };
            mesh.colors = Enumerable.Repeat(new Color(0,1,0,.5f),4).Concat(Enumerable.Repeat(new Color(1,0,0,.5f),4)).ToArray();
            mesh.SetUVs(0, Enumerable.Repeat(new Vector4(.5f,.5f,.5f,.5f),8).ToList());
            for (int i = 1; i <= 4; ++i) mesh.SetUVs(i, Enumerable.Repeat(Vector4.zero,8).ToList());
            mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds(); return mesh;
        }
        [Serializable] sealed class Metric
        {
            public string stage, api, unity, scope;
            public bool negative, ortho, finite, nonCDCD;
            public float ab, bc, repeat, backResponseA, backResponseB, backResponseC, orderErrorA, orderErrorB, orderErrorC;
            public int visibleA, visibleB, visibleC;
            public float lifecycleDeniedMainError, lifecycleRestoreError;
            public bool lifecycleMainPreserved;
            public string[] passReadback;
        }
        static float Delta(Color[] a, Color[] b) => a.Zip(b, (x,y) => Enumerable.Range(0,4).Max(i => Mathf.Abs(x[i]-y[i]))).Max();
        static int Visible(Color[] a, Color[] b) => a.Zip(b, (x,y) => Enumerable.Range(0,4).Any(i => x[i]!=y[i]) ? 1 : 0).Sum();
        [TestCaseSource(nameof(Cases))]
        public void NativeAndVersionedGraph_ActualDefaultAndBackFirstOrder(string stage, bool negative, bool ortho)
        {
            var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset; Assert.That(pipeline, Is.Not.Null);
            var data = pipeline.rendererDataList[0] as UniversalRendererData; Assert.That(data, Is.Not.Null);
            Assert.That(data.transparentLayerMask.value & (1<<4), Is.Not.Zero);
            string rendererFile = Path.Combine(Path.GetDirectoryName(Application.dataPath), AssetDatabase.GetAssetPath(data)); byte[] rendererBefore = File.ReadAllBytes(rendererFile);
            var nb = data.rendererFeatures.First(f => f && f.GetType().FullName == "NBShader.NBPostProcess"); bool oldNb = nb.isActive;
            var scene = EditorSceneManager.NewPreviewScene(); var actor = Keep(new GameObject("BackFirst twin faces", typeof(MeshFilter), typeof(MeshRenderer)));
            var go = Keep(new GameObject("BackFirst real camera")); var camera = go.AddComponent<Camera>(); go.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;
            SceneManager.MoveGameObjectToScene(actor, scene); SceneManager.MoveGameObjectToScene(go, scene); camera.scene = scene;
            actor.layer = 4; actor.transform.localScale = new Vector3(negative ? -1 : 1, 1, 1); actor.GetComponent<MeshFilter>().sharedMesh = TwinFaces();
            var renderer = actor.GetComponent<MeshRenderer>(); renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            camera.enabled = false; camera.orthographic = ortho; camera.orthographicSize = 1.5f; camera.fieldOfView = 42; camera.nearClipPlane = .1f; camera.farClipPlane = 20;
            camera.transform.position = new Vector3(0,0,4); camera.transform.rotation = Quaternion.LookRotation(Vector3.back); camera.cullingMask = 1<<4;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0,0,0,1); camera.allowHDR = true; camera.allowMSAA = false;
            var rt = Keep(new RenderTexture(128,128,24,RenderTextureFormat.ARGBHalf,RenderTextureReadWrite.Linear)); var read = Keep(new Texture2D(128,128,TextureFormat.RGBAHalf,false,true));
            camera.targetTexture = rt; var oldRT = RenderTexture.active; bool oldAsync = ShaderUtil.allowAsyncCompilation;
            string folder = Path.Combine(Environment.GetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR") ?? Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp/NBFXBackFirstOptIn"), stage + (negative ? "-negative" : "-positive") + (ortho ? "-ortho" : "-perspective")); Directory.CreateDirectory(folder);
            bool modern = stage != "legacy-main";
            string[] paths = { "Tests/Baseline/Frozen/NBShaders2/Shader/NBShader.shader", "NBShaders2/Shader/NBShader.shader", "Tests/URP/Graphs/NBBackFirst" + (modern ? "Modern" : "Legacy") + ".shadergraph" };
            var materials = paths.Select(p => Keep(new Material(AssetDatabase.LoadAssetAtPath<Shader>(Package+p)) { hideFlags = HideFlags.HideAndDontSave })).ToArray();
            var frames = new List<Color[]>(); var draw = typeof(G4GraphVATTests).GetMethod("Draw",Flags); var configure = typeof(G4GraphFlipbookTests).GetMethod("Configure",Flags);
            Color[] Snap(int k,string label) { renderer.sharedMaterial = materials[k]; for (int i=0;i<3;++i) camera.Render(); var p=(Color[])Invoke(draw, renderer,materials[k],camera,rt,read,folder,label); Assert.That(renderer.sharedMaterial,Is.SameAs(materials[k])); frames.Add(p); return p; }
            string Main(int k) => k==2 && !modern ? "SRPDefaultUnlit" : "UniversalForward";
            try
            {
                ShaderUtil.allowAsyncCompilation = false; nb.SetActive(false); Assert.That(rt.Create() && !rt.sRGB, Is.True);
                for (int k=0;k<3;++k)
                {
                    var m=materials[k]; Assert.That(m.shader && m.shader.isSupported, Is.True);
                    Invoke(configure,m,k==2,"Forward",Texture2D.whiteTexture,Texture2D.whiteTexture,false,false,.25f);
                    if(stage=="modern-on")
                    {
                        // F0 Configure ignores vertex color by design. This order
                        // probe needs its real TwinFaces RGB and half alpha.
                        var flagsType=Find("NBShader.NBShaderFlags");
                        var flags=Activator.CreateInstance(flagsType,new object[]{m});
                        int ignoreVertex=(int)flagsType.GetField("FLAG_BIT_PARTICLE_1_IGNORE_VERTEX_COLOR",Flags).GetValue(null);
                        flagsType.GetMethod("ClearFlagBits").Invoke(flags,new object[]{ignoreVertex,null,1});
                        Assert.That((bool)flagsType.GetMethod("CheckFlagBits").Invoke(flags,new object[]{ignoreVertex,null,1}),Is.False);
                    }
                    m.SetFloat("_FlipbookBlending",0); m.DisableKeyword("_FLIPBOOKBLENDING_ON"); m.SetFloat("_Cull",2); m.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha); m.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
                    m.SetFloat("_SrcBlendAlpha",(float)BlendMode.SrcAlpha); m.SetFloat("_DstBlendAlpha",(float)BlendMode.OneMinusSrcAlpha); m.SetFloat("_ZWrite",0); m.SetFloat("_ZTest",(float)CompareFunction.LessEqual); m.SetFloat("_AlphaAll",1);
                    m.SetColor("_ColorA",Color.white); m.SetColor(k==2?"_Color":"_BaseColor",Color.white);
                    m.SetShaderPassEnabled(Main(k),true);
                    if(k<2)m.SetShaderPassEnabled("SRPDefaultUnlit",false);
                    else if(modern)
                    {
                        m.SetFloat("_NB_GraphGUIStateVersion",2); m.SetFloat("_Surface",1); m.SetFloat("_AlphaClip",0);
                        // Constructor default-enabled back is deliberately left
                        // enabled before migration: effective0 must clip it.
                        m.SetShaderPassEnabled("SRPDefaultUnlit",true);
                        Assert.That(m.GetFloat("_NB_BackFirstEffective"),Is.Zero);
                    }
                }
                var tier = Enum.ToObject(Find("NBShader.NBShaderFeatureTier"), 3);
                var graphRoot = MakeGraphRoot(materials[2]);
                var graphSync = graphRoot.GetType().GetProperty("SyncService", BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).GetValue(graphRoot);
                var rawKeywords = (string[])Find("NBShader.NBShaderFeatureCatalog").GetField("RawKeywords", Flags).GetValue(null);
                Assert.That((bool)graphSync.GetType().GetMethod("TryApplyGraphSupportedGateTier", BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).Invoke(graphSync,new object[]{tier,rawKeywords}),Is.True);
                renderer.enabled=false; var empty=Snap(0,"empty"); var er=Snap(0,"empty-repeat"); renderer.enabled=true; Assert.That(Delta(empty,er),Is.Zero);
                var mains=new Color[3][]; var mainRepeat=new Color[3][];
                for(int k=0;k<3;++k) { mains[k]=Snap(k,"ABC"[k]+"-main-default"); mainRepeat[k]=Snap(k,"ABC"[k]+"-main-repeat"); }
                var mtr=new Metric{stage=stage,negative=negative,ortho=ortho,api=SystemInfo.graphicsDeviceType.ToString(),unity=Application.unityVersion,scope="Actual ordinaryMesh nativeA/currentB/versionedGraphC realURP twinfaces: legacy raw-main, modern defaultoff constructor, or BackFirst noncommutative alpha order. No fullGUI/DisableMain/NBController/Player/perf/Gate claim.",ab=Delta(mains[0],mains[1]),bc=Delta(mains[1],mains[2]),repeat=Enumerable.Range(0,3).Max(k=>Delta(mains[k],mainRepeat[k])),visibleA=Visible(mains[0],empty),visibleB=Visible(mains[1],empty),visibleC=Visible(mains[2],empty)};
                if(stage=="modern-on")
                {
                    Assert.That((bool)Service("TryMigrateGraphColorPassState",materials[2],true),Is.True);
                    materials[2].SetFloat("_BackFirstPassToggle",1);
                    Assert.That((bool)Service("TryApplyGraphBackFirstPassIntent",materials[2],tier,null,new[]{"pass.backFirst"}),Is.True);
                    for(int k=0;k<2;++k) { materials[k].SetFloat("_BackFirstPassToggle",1); materials[k].SetShaderPassEnabled("SRPDefaultUnlit",true); }
                    var full=new Color[3][]; var backs=new Color[3][];
                    for(int k=0;k<3;++k) { full[k]=Snap(k,"ABC"[k]+"-back-first-on"); var repeat=Snap(k,"ABC"[k]+"-back-first-repeat"); mtr.repeat=Mathf.Max(mtr.repeat,Delta(full[k],repeat)); materials[k].SetShaderPassEnabled(Main(k),false); backs[k]=Snap(k,"ABC"[k]+"-back-only"); materials[k].SetShaderPassEnabled(Main(k),true); }
                    mtr.ab=Mathf.Max(mtr.ab,Mathf.Max(Delta(full[0],full[1]),Delta(backs[0],backs[1]))); mtr.bc=Mathf.Max(mtr.bc,Mathf.Max(Delta(full[1],full[2]),Delta(backs[1],backs[2])));
                    // Refresh the exact original MaterialProperty snapshot after direct setup edits.
                    graphRoot = MakeGraphRoot(materials[2]);
                    graphSync = graphRoot.GetType().GetProperty("SyncService", BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).GetValue(graphRoot);
                    string graphBefore = EditorJsonUtility.ToJson(materials[2]);
                    bool mainBefore = materials[2].GetShaderPassEnabled("UniversalForward");
                    object[] denied = {materials[2],tier,rawKeywords,new string[0],false};
                    Assert.That((bool)Find("NBShaderEditor.NBShaderSyncService").GetMethod("ApplyGraphOwnedBackFirstPassState",Flags).Invoke(null,denied),Is.True);
                    Assert.That(materials[2].GetFloat("_BackFirstPassToggle"),Is.EqualTo(1));
                    Assert.That(materials[2].GetFloat("_NB_BackFirstEffective"),Is.Zero);
                    Assert.That(materials[2].GetShaderPassEnabled("SRPDefaultUnlit"),Is.False);
                    var cDenied=Snap(2,"C-tier-denied");var cDeniedRepeat=Snap(2,"C-tier-denied-repeat");
                    Assert.That((bool)graphSync.GetType().GetMethod("TryApplyGraphSupportedGateTier",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).Invoke(graphSync,new object[]{tier,rawKeywords}),Is.True);
                    var cRestored=Snap(2,"C-tier-restored");var cRestoredRepeat=Snap(2,"C-tier-restored-repeat");
                    mtr.lifecycleDeniedMainError=Delta(cDenied,mains[2]);mtr.lifecycleRestoreError=Delta(cRestored,full[2]);
                    mtr.lifecycleMainPreserved=materials[2].GetShaderPassEnabled("UniversalForward")==mainBefore;
                    Assert.That(mtr.lifecycleDeniedMainError+mtr.lifecycleRestoreError,Is.Zero);
                    Assert.That(Delta(cDenied,cDeniedRepeat)+Delta(cRestored,cRestoredRepeat),Is.Zero);
                    Assert.That(mtr.lifecycleMainPreserved,Is.True);
                    Assert.That(EditorJsonUtility.ToJson(materials[2]),Is.EqualTo(graphBefore),"Actual Tier restore preserves the complete original material.");
                    mtr.backResponseA=Delta(full[0],mains[0]); mtr.backResponseB=Delta(full[1],mains[1]); mtr.backResponseC=Delta(full[2],mains[2]);
                    float Order(int k) { int i=64*128+64; Assert.That(mains[k][i].a,Is.EqualTo(.75f)); Assert.That(backs[k][i].a,Is.EqualTo(.75f)); var expected=new Color(mains[k][i].r+backs[k][i].r*.5f,mains[k][i].g+backs[k][i].g*.5f,mains[k][i].b+backs[k][i].b*.5f,.625f); var wrong=new Color(backs[k][i].r+mains[k][i].r*.5f,backs[k][i].g+mains[k][i].g*.5f,backs[k][i].b+mains[k][i].b*.5f,.625f); float err=Enumerable.Range(0,4).Max(c=>Mathf.Abs(full[k][i][c]-expected[c])); Assert.That(Enumerable.Range(0,4).Max(c=>Mathf.Abs(full[k][i][c]-wrong[c])),Is.GreaterThan(.1f)); return err; }
                    mtr.orderErrorA=Order(0); mtr.orderErrorB=Order(1); mtr.orderErrorC=Order(2);
                }
                else if(stage=="legacy-main")
                {
                    for(int k=0;k<3;++k)materials[k].SetShaderPassEnabled(Main(k),false);
                    for(int k=0;k<3;++k)Assert.That(Delta(Snap(k,"ABC"[k]+"-old-raw-main-disabled"),empty),Is.Zero);
                }
                mtr.finite=frames.SelectMany(p=>p).All(p=>Enumerable.Range(0,4).All(c=>!float.IsNaN(p[c])&&!float.IsInfinity(p[c])));
                mtr.nonCDCD=frames.All(pixels=>!pixels.All(p=>Enumerable.Range(0,4).All(c=>p[c]==-23.203125f)));
                mtr.passReadback=materials.Select(m=>m.shader.name+" SRP="+m.GetShaderPassEnabled("SRPDefaultUnlit")+" Universal="+m.GetShaderPassEnabled("UniversalForward")).ToArray(); File.WriteAllText(Path.Combine(folder,"metrics.json"),JsonUtility.ToJson(mtr,true)); Debug.Log("NBFX_BACKFIRST_OPTIN_GPU "+JsonUtility.ToJson(mtr));
                Assert.That(mtr.finite,Is.True); Assert.That(mtr.nonCDCD,Is.True); Assert.That(mtr.ab+mtr.bc,Is.Zero); Assert.That(mtr.repeat,Is.Zero); Assert.That(mtr.visibleA,Is.GreaterThan(150)); Assert.That(mtr.visibleB,Is.GreaterThan(150)); Assert.That(mtr.visibleC,Is.GreaterThan(150));
                if(stage=="modern-on") { Assert.That(mtr.backResponseA,Is.GreaterThan(.01f)); Assert.That(mtr.backResponseB,Is.GreaterThan(.01f)); Assert.That(mtr.backResponseC,Is.GreaterThan(.01f)); Assert.That(mtr.orderErrorA+mtr.orderErrorB+mtr.orderErrorC,Is.Zero); }
            }
            finally { nb.SetActive(oldNb); ShaderUtil.allowAsyncCompilation=oldAsync; camera.targetTexture=null; RenderTexture.active=oldRT; rt.Release(); EditorSceneManager.ClosePreviewScene(scene); Assert.That(File.ReadAllBytes(rendererFile),Is.EqualTo(rendererBefore)); }
        }
    }
}
