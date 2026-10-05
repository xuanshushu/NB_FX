using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace NBFX.Baseline.Tests
{
    // Real Materials + existing Root/Sync; never opens a native popup.
    public sealed class G4GraphNoiseScreenOwnershipTests
    {
        const BindingFlags All=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
        const string Mode="_NB_DistortionMode",Owner="_NB_GraphScreenPassMigrationComplete",Disable="_DisableMainPassToggle";
        readonly List<Object> owned=new List<Object>();readonly List<G4GraphPersistentGateTierTests> helpers=new List<G4GraphPersistentGateTierTests>();
        static Type TypeOf(string n)=>G4SpecDebugFixture.FindType(n);
        static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,All).Invoke(o,args);
        static object Property(object o,string n)=>o.GetType().GetProperty(n,All).GetValue(o);
        [OneTimeSetUp]public void Preflight()
        {
            Assert.That(Path.GetFullPath(Application.dataPath),Is.EqualTo(Path.GetFullPath(@"D:\UnityProject\NBUnityProject\.utmp\NBFXMeshValidation-20261002\Assets")).IgnoreCase);
            for(int i=0;i<SceneManager.sceneCount;++i){var s=SceneManager.GetSceneAt(i);Assert.That((s.name+"/"+s.path).IndexOf("TAI",StringComparison.OrdinalIgnoreCase),Is.LessThan(0));}
        }
        sealed class Snapshot
        {
            readonly object v;static Type Shared=>typeof(G4GraphGuiFeatureIntentTests).GetNestedType("Snapshot",BindingFlags.NonPublic);
            Snapshot(object v){this.v=v;}public static Snapshot Read(Material m)=>new Snapshot(Shared.GetMethod("Read",All).Invoke(null,new object[]{m}));
            public void Same(Material m,string label,params string[] allow)=>Shared.GetMethod("AssertSame",All).Invoke(v,new object[]{m,label,allow});
        }
        object Root(params Material[] m){var h=new G4GraphPersistentGateTierTests();helpers.Add(h);return h.GetType().GetMethod("Root",All).Invoke(h,new object[]{m});}
        Material Material(bool modern)
        {
            Shader shader=AssetDatabase.LoadAssetAtPath<Shader>(modern?"Packages/com.xuanxuan.nb.fx/Tests/URP/Graphs/NBBackFirstModern.shadergraph":G4SpecDebugFixture.GraphPath);Assert.That(shader&&shader.isSupported,Is.True);
            var m=new Material(shader){hideFlags=HideFlags.HideAndDontSave};owned.Add(m);m.SetFloat("_NB_GraphGUIStateVersion",2);m.SetFloat("_noisemapEnabled",1);m.SetFloat("_NBShaderFeatureTier",3);m.SetFloat(Owner,0);if(modern)m.SetFloat("_NB_GraphPassMigrationComplete",1);G4SpecDebugFixture.Validate(m);return m;
        }
        static object Tier(int n)=>Enum.ToObject(TypeOf("NBShader.NBShaderFeatureTier"),n);
        static string[] Raw()=> (string[])TypeOf("NBShader.NBShaderFeatureCatalog").GetField("RawKeywords",All).GetValue(null);
        static string Main(bool modern)=>modern?"UniversalForward":"SRPDefaultUnlit";
        static void Passes(Material m,bool modern,bool main,bool deferred,bool opaque)
        {Assert.That(m.GetShaderPassEnabled(Main(modern)),Is.EqualTo(main));Assert.That(m.GetShaderPassEnabled("NBDeferredDistortPass"),Is.EqualTo(deferred));Assert.That(m.GetShaderPassEnabled("NBCameraOpaqueDistortPass"),Is.EqualTo(opaque));}
        [TearDown]public void Cleanup(){foreach(var w in owned.OfType<NBFXMainTexGUIEventHost>()){w.Draw=null;w.Setup=null;w.Close();}foreach(var h in helpers)h.Cleanup();helpers.Clear();foreach(var o in owned.AsEnumerable().Reverse())if(o)Object.DestroyImmediate(o);owned.Clear();}
        [TestCase(false,TestName="G4NoiseScreen_CPU_Raw0LegacyValidateAndTierPreservePasses")]
        [TestCase(true,TestName="G4NoiseScreen_CPU_Raw0ModernValidateAndTierPreservePasses")]
        public void RawZeroPreserves(bool modern)
        {
            var m=Material(modern);m.SetFloat(Mode,1);m.SetShaderPassEnabled(Main(modern),false);m.SetShaderPassEnabled("NBDeferredDistortPass",false);m.SetShaderPassEnabled("NBCameraOpaqueDistortPass",true);G4SpecDebugFixture.Validate(m);var root=Root(m);var before=Snapshot.Read(m);
            object[] display={false,false};Assert.That(Call(Property(root,"SyncService"),"TryReadGraphScreenDisableMainDisplay",display),Is.True);Assert.That((bool)display[0],Is.True);Assert.That(Call(root,"InitializeGraphNoiseInputs"),Is.True);G4SpecDebugFixture.Validate(m);before.Same(m,"Unadopted schema init/Validate preserves complete raw state.");
            Assert.That(Call(Property(root,"SyncService"),"TryApplyGraphSupportedGateTier",Tier(3),Raw()),Is.True);before.Same(m,"Unadopted same Tier keeps manual main/NB pass state exact.");
        }
        [TestCase(false,TestName="G4NoiseScreen_CPU_LegacyAdoptModesUndoRedo")]
        [TestCase(true,TestName="G4NoiseScreen_CPU_ModernAdoptModesUndoRedo")]
        public void AdoptModes(bool modern)
        {
            var m=Material(modern);m.SetShaderPassEnabled(Main(modern),false);var root=Root(m);var sync=Property(root,"SyncService");bool back=modern&&m.GetShaderPassEnabled("SRPDefaultUnlit");var before=Snapshot.Read(m);Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            try
            {
                Assert.That(Call(sync,"TryAdoptGraphScreenEdit",1,null),Is.True);Assert.That(m.GetFloat(Owner),Is.EqualTo(1));Assert.That(m.GetFloat(Disable),Is.EqualTo(1));Passes(m,modern,false,true,false);
                Assert.That(Call(sync,"TryAdoptGraphScreenEdit",2,null),Is.True);Passes(m,modern,false,false,true);
                Assert.That(Call(sync,"TryAdoptGraphScreenEdit",0,null),Is.True);Assert.That(m.GetFloat(Disable),Is.EqualTo(0));Passes(m,modern,true,false,false);if(modern)Assert.That(m.GetShaderPassEnabled("SRPDefaultUnlit"),Is.EqualTo(back),"BackFirst remains independent.");
                Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(group);var after=Snapshot.Read(m);Undo.PerformUndo();before.Same(m,"Same actual adoption service restores all aliases/pass/raw/owner with Undo.");Undo.PerformRedo();after.Same(m,"Actual service complete Redo.");
            }
            finally{Undo.RevertAllDownToGroup(group);}
        }
        [TestCase(false,TestName="G4NoiseScreen_CPU_LegacyOwnedTierDenyRestore")]
        [TestCase(true,TestName="G4NoiseScreen_CPU_ModernOwnedTierDenyRestore")]
        public void TierDenyRestore(bool modern)
        {
            var m=Material(modern);var root=Root(m);var sync=Property(root,"SyncService");Assert.That(Call(sync,"TryAdoptGraphScreenEdit",1,(bool?)true),Is.True);var before=Snapshot.Read(m);
            Assert.That(Call(sync,"TryApplyGraphSupportedGateTier",Tier(3),Raw().Where(k=>k!="_SCREEN_DISTORT_MODE").ToArray()),Is.True);Passes(m,modern,true,false,false);Assert.That(m.GetFloat(Mode),Is.EqualTo(1));Assert.That(m.GetFloat(Disable),Is.EqualTo(1));
            Assert.That(Call(sync,"TryApplyGraphSupportedGateTier",Tier(3),Raw()),Is.True);before.Same(m,"Full screen Tier restore inclsaved mode/disable/main+NB and independent BackFirst.");
        }
        [Test]public void G4NoiseScreen_CPU_FutureMarkerAtomicReject()
        {
            var a=Material(false);var b=Material(false);b.SetFloat(Owner,2);var root=Root(a,b);var sa=Snapshot.Read(a);var sb=Snapshot.Read(b);Assert.That(Call(Property(root,"SyncService"),"TryAdoptGraphScreenEdit",1,null),Is.False);sa.Same(a,"Future second owner rejects before first alias/pass write.");sb.Same(b,"Future owner/raw all exact.");
        }
        [Test]public void G4NoiseScreen_CPU_ModernUnmigratedAtomicReject()
        {
            var m=Material(true);m.SetFloat("_NB_GraphPassMigrationComplete",0);var root=Root(m);var before=Snapshot.Read(m);Assert.That(Call(Property(root,"SyncService"),"TryAdoptGraphScreenEdit",1,null),Is.False);before.Same(m,"Screen action cannot silently adopt or migrate modern color routing.");
        }
        [Test]public void G4NoiseScreen_CPU_RefractionGateRawEnumPreserved()
        {
            var m=Material(false);m.SetFloat("_DistortMode",1);G4SpecDebugFixture.Validate(m);var before=Snapshot.Read(m);object[] args={m,Tier(3),Raw().Where(k=>k!="_DISTORT_REFRACTION").ToArray(),false};var type=TypeOf("NBShaders2.Editor.FeatureLevel.NBShaderFeatureLevelMaterialApplier");Assert.That(type.GetMethod("ApplyGraphRefractionGroup",All).Invoke(null,args),Is.True);Assert.That(m.GetFloat("_NB_TierAllowRefraction"),Is.EqualTo(0));Assert.That(m.GetFloat("_DistortMode"),Is.EqualTo(1));before.Same(m,"Only local refraction gate changes; screen alias/raw enum retained.","_NB_TierAllowRefraction");args[2]=Raw();Assert.That(type.GetMethod("ApplyGraphRefractionGroup",All).Invoke(null,args),Is.True);before.Same(m,"Refraction raw enum and complete restoration.");
        }
        [Test]public void G4NoiseScreen_CPU_MixedModesDisableOnlyPreservesModesUndoRedo()
        {
            var a=Material(false);var b=Material(false);a.SetFloat(Mode,1);b.SetFloat(Mode,2);a.SetShaderPassEnabled(Main(false),false);b.SetShaderPassEnabled(Main(false),false);var root=Root(a,b);var sync=Property(root,"SyncService");var sa=Snapshot.Read(a);var sb=Snapshot.Read(b);Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            try
            {
                // The actual DisableMain action/reset selects no Mode; each material retains its own alias.
                Assert.That(Call(sync,"TryAdoptGraphScreenEdit",null,(bool?)false),Is.True);Assert.That(a.GetFloat(Mode),Is.EqualTo(1));Assert.That(b.GetFloat(Mode),Is.EqualTo(2));Passes(a,false,true,true,false);Passes(b,false,true,false,true);
                Assert.That(Call(sync,"TryAdoptGraphScreenEdit",null,(bool?)true),Is.True);Assert.That(a.GetFloat(Mode),Is.EqualTo(1));Assert.That(b.GetFloat(Mode),Is.EqualTo(2));Passes(a,false,false,true,false);Passes(b,false,false,false,true);
                Assert.That(Call(sync,"TryAdoptGraphScreenEdit",null,(bool?)false),Is.True);Assert.That(a.GetFloat(Mode),Is.EqualTo(1));Assert.That(b.GetFloat(Mode),Is.EqualTo(2));Assert.That(a.GetFloat(Disable),Is.EqualTo(0));Assert.That(b.GetFloat(Disable),Is.EqualTo(0));Passes(a,false,true,true,false);Passes(b,false,true,false,true);
                Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(group);var aa=Snapshot.Read(a);var ab=Snapshot.Read(b);Undo.PerformUndo();sa.Same(a,"Mixed first mode and complete raw state Undo.");sb.Same(b,"Mixed second mode/raw all Undo.");Undo.PerformRedo();aa.Same(a,"Mixed first complete Redo.");ab.Same(b,"Mixed second complete Redo.");
            }
            finally{Undo.RevertAllDownToGroup(group);}
        }
        static object Field(object o,string name)
        {for(Type t=o.GetType();t!=null;t=t.BaseType){var f=t.GetField(name,All|BindingFlags.DeclaredOnly);if(f!=null)return f.GetValue(o);}Assert.Fail("Missing real field "+name);return null;}
        static IEnumerable<object> Descendants(object item)
        {yield return item;foreach(object child in (System.Collections.IEnumerable)Field(item,"ChildrenItemList"))foreach(object d in Descendants(child))yield return d;}
        NBFXMainTexGUIEventHost Host(object root,out object actual)
        {
            var h=ScriptableObject.CreateInstance<NBFXMainTexGUIEventHost>();owned.Add(h);h.hideFlags=HideFlags.HideAndDontSave;h.position=new Rect(20,20,640,840);h.SetupCommand="NBFX_Noise_Setup_"+Guid.NewGuid().ToString("N");object item=null;
            h.Setup=()=>{Assert.That(Call(root,"InitializeGraphNoiseInputs"),Is.True);item=Field(root,"_graphNoiseItem");};h.ShowUtility();h.SendEvent(new Event{type=EventType.ExecuteCommand,commandName=h.SetupCommand});Check(h);Assert.That(h.Initialized,Is.True);actual=item;h.Draw=()=>Call(root,"DrawGraphNoiseInputs");return h;
        }
        static void Check(NBFXMainTexGUIEventHost h){if(h.Failure!=null)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(h.Failure).Throw();}
        static void Send(NBFXMainTexGUIEventHost h,Event e)
        {var type=e.rawType;h.Counts.TryGetValue(type,out int before);h.SendEvent(e);Check(h);Assert.That(e.rawType,Is.EqualTo(type));Assert.That(h.Counts.TryGetValue(type,out int after)&&after>before,Is.True,"Real native Root event received.");}
        [Test]public void G4NoiseScreen_GUI_RawDisplayAndOutOfRangePassiveReadOnly()
        {
            var m=Material(false);m.SetFloat(Mode,1);m.SetFloat("_NoiseBlockFoldOut",1);m.SetFloat("_NoiseIntensity",3);m.SetFloat("_NB_DistortionIntensity",2);m.SetShaderPassEnabled(Main(false),false);m.SetShaderPassEnabled("NBCameraOpaqueDistortPass",true);G4SpecDebugFixture.Validate(m);var root=Root(m);var h=Host(root,out var item);var before=Snapshot.Read(m);
            Send(h,new Event{type=EventType.Layout});Send(h,new Event{type=EventType.Repaint});before.Same(m,"Raw screen display and original whole Noise subtree passive clamp protection: no floats/raw/passes/ownership rewritten.");Assert.That(m.GetFloat(Owner),Is.EqualTo(0));
            var aliases=Descendants(item).Where(o=>Field(o,"PropertyName")is string).Select(o=>(string)Field(o,"PropertyName")).ToArray();Assert.That(aliases,Does.Contain(Mode).And.Contain("_NB_DistortionIntensity"));Assert.That(aliases,Does.Not.Contain("_ScreenDistortModeToggle").And.Not.Contain("_ScreenDistortIntensity"));
        }
        [Test]public void G4NoiseScreen_GUI_ActualDisableMainTakeoverCompleteUndoRedo()
        {
            var m=Material(false);m.SetFloat(Mode,1);m.SetFloat("_NoiseBlockFoldOut",1);m.SetShaderPassEnabled(Main(false),false);G4SpecDebugFixture.Validate(m);var root=Root(m);var h=Host(root,out var item);Send(h,new Event{type=EventType.Layout});Send(h,new Event{type=EventType.Repaint});var control=Descendants(item).Single(o=>o.GetType().Name=="GraphDisableMainToggleItem");var rect=(Rect)Field(control,"ControlRect");Assert.That(rect.width>0&&rect.height>0,Is.True);var click=new Vector2(rect.x+6,rect.center.y);var before=Snapshot.Read(m);Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            try
            {
                // This is only a real non-popup Toggle click. The actual Root and Sync own Undo.
                Send(h,new Event{type=EventType.MouseDown,button=0,mousePosition=click});Send(h,new Event{type=EventType.MouseUp,button=0,mousePosition=click});Assert.That(m.GetFloat(Owner),Is.EqualTo(1));Assert.That(m.GetFloat(Disable),Is.EqualTo(0));Passes(m,false,true,true,false);
                Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(group);var after=Snapshot.Read(m);h.Draw=null;Undo.PerformUndo();before.Same(m,"Raw legacy pass and unowned state complete Undo after actual Toggle adoption.");Undo.PerformRedo();after.Same(m,"Actual control complete Redo inclaliases/raw/pass/ownership.");
            }
            finally{h.Draw=null;Undo.RevertAllDownToGroup(group);}
        }
        [Test]public void G4NoiseScreen_CPU_ModeAndRefractionEnumsIndependent()
        {
            var m=Material(false);m.SetFloat("_DistortMode",1);var root=Root(m);Assert.That(Call(Property(root,"SyncService"),"TryAdoptGraphScreenEdit",2,null),Is.True);Assert.That(m.GetFloat("_DistortMode"),Is.EqualTo(1));Assert.That(m.GetFloat(Mode),Is.EqualTo(2));
        }
    }
}
