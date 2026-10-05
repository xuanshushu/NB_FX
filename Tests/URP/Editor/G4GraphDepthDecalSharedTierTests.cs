using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace NBFX.Baseline.Tests
{
    // Reuses original leaves, actual Root event owner, state snapshots and ABC
    // harness. Native popup selection remains manual; no simulated popup pass.
    public sealed class G4GraphDepthDecalSharedTierTests
    {
        const BindingFlags All=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance;
        const string Gate="_NB_TierAllowDepthDecal";
        static readonly string[] Linked={"_Stencil","_StencilComp","_StencilOp","_StencilFail","_StencilZFail","_StencilReadMask","_StencilWriteMask","_StencilKeyIndex","_CustomStencilTest","_Cull","_ZTest"};
        readonly List<Object> owned=new List<Object>();
        readonly List<G4GraphPersistentGateTierTests> helpers=new List<G4GraphPersistentGateTierTests>();
        static Type TypeOf(string name)=>G4SpecDebugFixture.FindType(name);
        static object Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name,All).Invoke(target,args);
        static object Property(object target,string name)=>target.GetType().GetProperty(name,All).GetValue(target);
        static object Field(object target,string name)
        {
            for(Type type=target.GetType();type!=null;type=type.BaseType)
            {var field=type.GetField(name,All|BindingFlags.DeclaredOnly);if(field!=null)return field.GetValue(target);}
            Assert.Fail("Missing original field "+name);return null;
        }
        sealed class Snapshot
        {
            readonly object value;
            static Type Shared=>typeof(G4GraphGuiFeatureIntentTests).GetNestedType("Snapshot",BindingFlags.NonPublic);
            Snapshot(object value){this.value=value;}
            public static Snapshot Read(Material m)=>new Snapshot(Shared.GetMethod("Read",All).Invoke(null,new object[]{m}));
            public void AssertSame(Material m,string label,params string[] allowed)=>Shared.GetMethod("AssertSame",All).Invoke(value,new object[]{m,label,allowed});
        }
        [OneTimeSetUp] public void Preflight()
        {
            Assert.That(Path.GetFullPath(Application.dataPath),Is.EqualTo(Path.GetFullPath(@"D:\UnityProject\NBUnityProject\.utmp\NBFXMeshValidation-20261002\Assets")).IgnoreCase);
            for(int i=0;i<SceneManager.sceneCount;++i)
            {var scene=SceneManager.GetSceneAt(i);Assert.That((scene.name+"/"+scene.path).IndexOf("TAI",StringComparison.OrdinalIgnoreCase),Is.LessThan(0));}
            var shader=AssetDatabase.LoadAssetAtPath<Shader>(G4SpecDebugFixture.GraphPath);
            Assert.That(shader&&shader.isSupported,Is.True); // Lease owner compiles/imports; this never reimports.
        }
        Material Material()
        {
            var m=G4SpecDebugFixture.NewGraph();owned.Add(m);
            m.SetFloat("_NB_Flags0Lo16",2.25f);m.SetFloat("_NB_Flags0Hi16",.25f);
            m.SetFloat("_NB_Flags1Hi16",2.25f);m.SetFloat("_NB_UVModeFlag0Lo16",17.25f);
            m.SetFloat("_NB_UVModeFlag0Hi16",2.25f);m.SetFloat("_NB_UVModeFlagType0Hi16",.25f);
            m.SetFloat("_NB_CustomDataFlag3Hi16",123.25f);if(m.HasProperty("_MeshSourceMode"))m.SetFloat("_MeshSourceMode",1);
            m.SetFloat("_NB_ForceNoMipFlagsLo16",1.25f);m.SetFloat("_NB_ForceNoMipFlagsHi16",123.25f);
            return m;
        }
        object Root(params Material[] materials)
        {var helper=new G4GraphPersistentGateTierTests();helpers.Add(helper);return helper.GetType().GetMethod("Root",All).Invoke(helper,new object[]{materials});}
        static IEnumerable<object> Descendants(object item)
        {yield return item;foreach(object child in (System.Collections.IEnumerable)Field(item,"ChildrenItemList"))foreach(var nested in Descendants(child))yield return nested;}
        NBFXMainTexGUIEventHost Host(object root,out object item)
        {
            var h=ScriptableObject.CreateInstance<NBFXMainTexGUIEventHost>();owned.Add(h);
            h.hideFlags=HideFlags.HideAndDontSave;h.position=new Rect(20,20,680,900);
            h.SetupCommand="NBFX_DepthDecal_Setup_"+Guid.NewGuid().ToString("N");object actual=null;
            h.Setup=()=>{Assert.That(Call(root,"InitializeGraphDepthDecalInputs"),Is.True);actual=Field(root,"_graphDepthDecalItem");};
            h.ShowUtility();h.SendEvent(new Event{type=EventType.ExecuteCommand,commandName=h.SetupCommand});Check(h);Assert.That(h.Initialized,Is.True);
            item=actual;h.Draw=()=>Call(root,"DrawGraphDepthDecalInputs");return h;
        }
        static void Check(NBFXMainTexGUIEventHost h){if(h.Failure!=null)ExceptionDispatchInfo.Capture(h.Failure).Throw();}
        static void Send(NBFXMainTexGUIEventHost h,Event e)
        {
            var type=e.rawType;h.Counts.TryGetValue(type,out int before);h.SendEvent(e);Check(h);
            Assert.That(e.rawType,Is.EqualTo(type));Assert.That(h.Counts.TryGetValue(type,out int after)&&after>before,Is.True,"Actual original Root receives event.");
        }
        static string[] Raw()=>(string[])TypeOf("NBShader.NBShaderFeatureCatalog").GetField("RawKeywords",All).GetValue(null);
        static bool Apply(Material m,bool full,out bool changed)
        {
            object[] args={m,Enum.ToObject(TypeOf("NBShader.NBShaderFeatureTier"),3),full?Raw():Raw().Where(k=>k!="_DEPTH_DECAL").ToArray(),false};
            bool result=(bool)TypeOf("NBShaders2.Editor.FeatureLevel.NBShaderFeatureLevelMaterialApplier").GetMethod("ApplyGraphDepthDecalGroup",All).Invoke(null,args);changed=(bool)args[3];return result;
        }
        [TearDown] public void Cleanup()
        {
            foreach(var h in owned.OfType<NBFXMainTexGUIEventHost>()){h.Draw=null;h.Setup=null;h.Close();}
            foreach(var helper in helpers)helper.Cleanup();helpers.Clear();
            foreach(var o in owned.AsEnumerable().Reverse())if(o)Object.DestroyImmediate(o);owned.Clear();
        }
        void Edit(Material material,bool enabled)
        {
            // Original Native callback consumes its already-edited property.
            // Construct fresh MaterialProperty snapshots after direct test setup.
            material.SetFloat("_DepthDecal_Toggle",enabled?1:0);
            var root=Root(material);Call(Property(root,"SyncService"),"ApplyDepthDecalEnabled",enabled);
        }
        Material Native()
        {
            var shader=AssetDatabase.LoadAssetAtPath<Shader>("Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/NBShader.shader");Assert.That(shader,Is.Not.Null);
            var m=new Material(shader){hideFlags=HideFlags.HideAndDontSave};owned.Add(m);m.SetFloat("_NBShaderFeatureTier",3);return m;
        }
        static void AssertLinked(Material graph,Material native,bool enabled)
        {
            foreach(string p in Linked)Assert.That(graph.GetFloat(p),Is.EqualTo(native.GetFloat(p)),p);
            Assert.That(graph.GetFloat("_Cull"),Is.EqualTo(enabled?1:2),"Original RenderFace.Back=1/CullFront, Front=2/CullBack.");
            Assert.That(graph.GetFloat("_ZTest"),Is.EqualTo(enabled?7:4));Assert.That(graph.GetFloat("_CustomStencilTest"),Is.EqualTo(enabled?1:0));
            Assert.That(graph.GetFloat("_Stencil"),Is.EqualTo(enabled?2:0));Assert.That(graph.GetFloat("_StencilComp"),Is.EqualTo(enabled?7:8));
        }
        [Test] public void G4DecalShared_CPU_OriginalPresetLinkedStateAndQueue()
        {
            var graph=Material();var native=Native();graph.SetFloat("_QueueControl",1);graph.SetFloat("_QueueOffset",137);graph.SetFloat("_ColorMask",13);graph.renderQueue=3137;native.renderQueue=3137;
            foreach(bool enabled in new[]{true,false})
            {
                var before=Snapshot.Read(graph);Edit(native,enabled);Edit(graph,enabled);AssertLinked(graph,native,enabled);
                Assert.That(graph.rawRenderQueue,Is.EqualTo(3137));Assert.That(native.rawRenderQueue,Is.EqualTo(3137));
                before.AssertSame(graph,"Explicit original linked states only; QCM queue/ColorMask/pass/keywords retained.",Linked.Concat(new[]{"_DepthDecal_Toggle",Gate}).ToArray());
                Assert.That(graph.GetFloat(Gate),Is.EqualTo(enabled?1:0));
            }
        }
        [Test] public void G4DecalShared_CPU_TierPreservesLinkedStateUnknownSchemaNoWrite()
        {
            var m=Material();Edit(m,true);bool changed;var before=Snapshot.Read(m);
            Assert.That(Apply(m,false,out changed),Is.True);Assert.That(m.GetFloat(Gate),Is.EqualTo(0));Assert.That(m.GetFloat("_DepthDecal_Toggle"),Is.EqualTo(1));before.AssertSame(m,"Tier deny changes only effective toggle, never resets Stencil/Cull/ZTest.",Gate);
            Assert.That(Apply(m,true,out changed),Is.True);before.AssertSame(m,"Exact gate restore.");Assert.That(Apply(m,true,out changed),Is.True);Assert.That(changed,Is.False);
            m.SetFloat("_NB_GraphGUIStateVersion",9);var root=Root(m);before=Snapshot.Read(m);
            Assert.That(Call(Property(root,"SyncService"),"TryApplyGraphDepthDecalEnabled",false),Is.False);before.AssertSame(m,"Unknown schema is not normalized or written.");
        }
        [TestCase(false,TestName="G4DecalShared_GUI_EnableLinkedStateUndoRedo")]
        [TestCase(true,TestName="G4DecalShared_GUI_DisableLinkedStateUndoRedo")]
        public void ActualToggle(bool startEnabled)
        {
            var m=Material();if(startEnabled)Edit(m,true);m.renderQueue=3123;
            var root=Root(m);var h=Host(root,out var item);Assert.That(item.GetType().Name,Is.EqualTo("ToggleItem"));
            Send(h,new Event{type=EventType.Layout});Send(h,new Event{type=EventType.Repaint});
            var rect=(Rect)Field(item,"ControlRect");Assert.That(rect.width>0&&rect.height>0,Is.True);var pos=new Vector2(rect.x+6,rect.center.y);var before=Snapshot.Read(m);Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            try
            {
                Send(h,new Event{type=EventType.MouseDown,button=0,mousePosition=pos});Send(h,new Event{type=EventType.MouseUp,button=0,mousePosition=pos});bool enabled=!startEnabled;
                Assert.That(m.GetFloat("_DepthDecal_Toggle"),Is.EqualTo(enabled?1:0));Assert.That(m.GetFloat(Gate),Is.EqualTo(enabled?1:0));
                Assert.That(m.GetFloat("_Cull"),Is.EqualTo(enabled?1:2));Assert.That(m.GetFloat("_ZTest"),Is.EqualTo(enabled?7:4));Assert.That(m.GetFloat("_Stencil"),Is.EqualTo(enabled?2:0));Assert.That(m.rawRenderQueue,Is.EqualTo(3123));
                Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(group);var after=Snapshot.Read(m);h.Draw=null;Undo.PerformUndo();before.AssertSame(m,"Complete original linked-state toggle Undo.");Undo.PerformRedo();after.AssertSame(m,"Complete original linked-state toggle Redo.");
            }
            finally{h.Draw=null;Undo.RevertAllDownToGroup(group);}
        }
        static object Original(string name,params object[] args)=>typeof(G4GraphDepthDecalTests).GetMethod(name,All).Invoke(null,args);
        [Serializable] sealed class Metrics
        {
            public string scope="Only current Native B/Graph C explicit GUI linked-state projection and Tier deny/restore on a real floor/cube. No Frozen GUI parity, NB distortion routes, Player or complete Decal claim.";
            public bool finite;public float[] bc,repeat,response,restore;public int[] visible;
        }
        [Test] public void G4DecalShared_GPU_LinkedStateTierDenyRestore_ortho()
        {
            using(var h=new G4SpecDebugFixture.Harness("decal-shared-linked-tier-ortho",true))
            {
                var gradient=(Texture2D)Original("Gradient");owned.Add(gradient);var noise=h.Constant(new Color(.65f,.35f,0,1));
                var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);h.renderer.GetComponent<MeshFilter>().sharedMesh=cube.GetComponent<MeshFilter>().sharedMesh;Object.DestroyImmediate(cube);
                h.renderer.transform.position=new Vector3(.11f,.25f,-.07f);h.renderer.transform.rotation=Quaternion.Euler(0,23,0);h.renderer.transform.localScale=Vector3.one;
                var floor=GameObject.CreatePrimitive(PrimitiveType.Plane);owned.Add(floor);floor.layer=h.renderer.gameObject.layer;SceneManager.MoveGameObjectToScene(floor,h.camera.scene);
                var shader=Shader.Find("Universal Render Pipeline/Unlit");Assert.That(shader,Is.Not.Null);var fm=new Material(shader);owned.Add(fm);fm.SetTexture("_BaseMap",gradient);fm.SetColor("_BaseColor",new Color(.6f,.65f,.7f,1));fm.SetFloat("_Surface",0);fm.SetFloat("_ZWrite",1);fm.SetFloat("_Cull",0);fm.renderQueue=2000;floor.GetComponent<MeshRenderer>().sharedMaterial=fm;
                h.camera.orthographicSize=1.25f;h.camera.transform.position=new Vector3(.28f,1.8f,2.6f);h.camera.transform.LookAt(new Vector3(.05f,.05f,0));h.camera.GetUniversalAdditionalCameraData().requiresDepthTexture=true;
                var b=h.materials[1];var c=h.materials[2];Original("Configure",b,false,"Forward",gradient,noise);Original("Configure",c,true,"Forward",gradient,noise);
                Edit(b,true);Edit(c,true);AssertLinked(c,b,true);var empty=h.Snap("empty");var before=Snapshot.Read(c);
                var frames=new Color[3][][];var repeats=new Color[3][][];
                for(int stage=0;stage<3;++stage)
                {
                    bool on=stage!=1,changed;G4SpecDebugFixture.SetKeyword(b,"_DEPTH_DECAL",on);Assert.That(Apply(c,on,out changed),Is.True);before.AssertSame(c,"Tier cannot change original explicit linked states.",Gate);
                    frames[stage]=new[]{h.Snap("B-"+stage,b),h.Snap("C-"+stage,c)};repeats[stage]=new[]{h.Snap("B-"+stage+"-repeat",b),h.Snap("C-"+stage+"-repeat",c)};
                }
                var m=new Metrics{finite=G4SpecDebugFixture.Finite(empty)&&frames.SelectMany(f=>f).Concat(repeats.SelectMany(f=>f)).All(G4SpecDebugFixture.Finite),bc=frames.Select(f=>G4SpecDebugFixture.Delta(f[0],f[1])).ToArray(),repeat=Enumerable.Range(0,3).SelectMany(s=>Enumerable.Range(0,2).Select(i=>G4SpecDebugFixture.Delta(frames[s][i],repeats[s][i]))).ToArray(),response=Enumerable.Range(0,2).Select(i=>G4SpecDebugFixture.Delta(frames[0][i],frames[1][i])).ToArray(),restore=Enumerable.Range(0,2).Select(i=>G4SpecDebugFixture.Delta(frames[0][i],frames[2][i])).ToArray(),visible=frames.SelectMany(f=>f).Select(f=>G4SpecDebugFixture.Visible(f,empty)).ToArray()};
                File.WriteAllText(Path.Combine(h.folder,"metrics.json"),JsonUtility.ToJson(m,true));
                foreach(var material in new[]{b,c})Assert.That(ShaderUtil.GetShaderMessages(material.shader).Any(x=>x.severity.ToString()=="Error"),Is.False);
                Assert.That(m.finite,Is.True);Assert.That(m.bc.Concat(m.repeat).Concat(m.restore).All(v=>v==0),Is.True,"Original strict BC/repeat/restore zero.");Assert.That(m.visible.All(v=>v>128),Is.True);Assert.That(m.response.All(v=>v>.01f),Is.True,"B and C each respond strongly with original linked states.");before.AssertSame(c,"Full restore.");
            }
        }
    }
}
