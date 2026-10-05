using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace NBFX.Baseline.Tests
{
    public sealed class G4GraphBaseShadowSharedGuiTests
    {
        const BindingFlags All=BindingFlags.Static|BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        readonly List<Object> owned=new List<Object>();readonly List<G4GraphPersistentGateTierTests> helpers=new List<G4GraphPersistentGateTierTests>();
        static Type Find(string n)=>G4SpecDebugFixture.FindType(n);
        static MethodInfo Unique(Type t,string n,params Type[] sig){var m=t.GetMethods(All).Where(x=>x.Name==n&&x.GetParameters().Select(p=>p.ParameterType).SequenceEqual(sig)).ToArray();Assert.That(m.Length,Is.EqualTo(1),t.FullName+"."+n);return m[0];}
        static object Invoke(MethodInfo m,object target,params object[] args){try{return m.Invoke(target,args);}catch(TargetInvocationException e){ExceptionDispatchInfo.Capture(e.InnerException??e).Throw();throw;}}
        static object Call(object target,string n,params object[] args)
        {Type[] sig;switch(n){case "HasGraphBaseShadowSchema":case "InitializeGraphBaseShadowInputs":sig=Type.EmptyTypes;break;case "DrawGraphBaseShadowInputs":sig=new[]{Find("NBShaderEditor.ShaderGUIItem")};break;case "TryWriteGraphBaseShadow":sig=new[]{typeof(string),typeof(bool)};break;case "TryApplyGraphPortalToggle":sig=new[]{typeof(string),typeof(bool),typeof(bool)};break;default:Assert.Fail(n);return null;}return Invoke(Unique(target.GetType(),n,sig),target,args);}
        static object Field(object o,string n){for(Type t=o.GetType();t!=null;t=t.BaseType){var f=t.GetField(n,All|BindingFlags.DeclaredOnly);if(f!=null)return f.GetValue(o);}Assert.Fail(n);return null;}
        static object Sync(object root)=>root.GetType().GetProperty("SyncService",All).GetValue(root);
        static IEnumerable<object> Children(object item){yield return item;foreach(object child in (IEnumerable)Field(item,"ChildrenItemList"))foreach(var sub in Children(child))yield return sub;}
        static readonly string[] ExtraPassTags={"SRPDefaultUnlit","UniversalForward"};
        sealed class Snapshot
        {
            object data;Dictionary<string,bool> extra;static Type T=>typeof(G4GraphGuiFeatureIntentTests).GetNestedType("Snapshot",All);
            public static Snapshot Read(Material m)=>new Snapshot{data=Invoke(Unique(T,"Read",typeof(Material)),null,m),extra=ExtraPassTags.ToDictionary(tag=>tag,tag=>m.GetShaderPassEnabled(tag))};
            public void Same(Material m,string label,params string[] except){Invoke(Unique(T,"AssertSame",typeof(Material),typeof(string),typeof(string[])),data,m,label,except);foreach(var pass in extra)Assert.That(m.GetShaderPassEnabled(pass.Key),Is.EqualTo(pass.Value),label+" actual LightMode "+pass.Key);}
        }
        [OneTimeSetUp]public void Guard(){Assert.That(Path.GetFullPath(Application.dataPath),Is.EqualTo(Path.GetFullPath(@"D:\UnityProject\NBUnityProject\.utmp\NBFXMeshValidation-20261002\Assets")).IgnoreCase);for(int i=0;i<SceneManager.sceneCount;++i){var s=SceneManager.GetSceneAt(i);Assert.That((s.name+"/"+s.path).IndexOf("TAI",StringComparison.OrdinalIgnoreCase),Is.LessThan(0));}}
        Material New(){var shader=AssetDatabase.LoadAssetAtPath<Shader>(G4SpecDebugFixture.GraphPath);Assert.That(shader&&shader.isSupported,Is.True);var m=new Material(shader){hideFlags=HideFlags.HideAndDontSave};owned.Add(m);m.SetFloat("_NB_GraphGUIStateVersion",2);m.SetFloat("_NBShaderFeatureTier",3);m.SetFloat("_Surface",1);m.SetFloat("_Blend",0);G4SpecDebugFixture.Validate(m);m.SetFloat("_NB_Flags1Lo16",1);m.SetFloat("_BaseOptionBigBlockItemFoldOut",1);return m;}
        object Root(params Material[] materials){var helper=new G4GraphPersistentGateTierTests();helpers.Add(helper);return Invoke(Unique(typeof(G4GraphPersistentGateTierTests),"Root",typeof(Material[])),helper,(object)materials);}
        NBFXMainTexGUIEventHost Host(object root)
        {var h=ScriptableObject.CreateInstance<NBFXMainTexGUIEventHost>();owned.Add(h);h.hideFlags=HideFlags.HideAndDontSave;h.position=new Rect(20,20,650,650);h.SetupCommand="NBFX_BaseShadow_"+Guid.NewGuid().ToString("N");h.Setup=()=>Assert.That(Call(root,"InitializeGraphBaseShadowInputs"),Is.True);h.ShowUtility();h.SendEvent(new Event{type=EventType.ExecuteCommand,commandName=h.SetupCommand});Check(h);Assert.That(h.Initialized,Is.True);h.Draw=()=>Invoke(Unique(Field(root,"_graphAffectsShadowsItem").GetType(),"OnGUI"),Field(root,"_graphAffectsShadowsItem"));return h;}
        static void Check(NBFXMainTexGUIEventHost h){if(h.Failure!=null)ExceptionDispatchInfo.Capture(h.Failure).Throw();}
        static void Send(NBFXMainTexGUIEventHost h,Event e){var raw=e.rawType;h.Counts.TryGetValue(raw,out int before);h.SendEvent(e);Check(h);Assert.That(e.rawType,Is.EqualTo(raw));Assert.That(h.Counts.TryGetValue(raw,out int after)&&after>before,Is.True);}
        [TearDown]public void Cleanup(){foreach(var h in owned.OfType<NBFXMainTexGUIEventHost>()){h.Draw=null;h.Setup=null;h.Close();}foreach(var x in helpers)x.Cleanup();helpers.Clear();foreach(var x in owned.AsEnumerable().Reverse())if(x)Object.DestroyImmediate(x);owned.Clear();}
        static Type Runtime=>Find("NBShader.NBShaderFeatureRuntime");
        static object Tier(int n)=>Enum.ToObject(Find("NBShader.NBShaderFeatureTier"),n);
        static string[] RawKeywords=>(string[])Find("NBShader.NBShaderFeatureCatalog").GetField("RawKeywords",All).GetValue(null);
        static string[] RawPasses=>(string[])Find("NBShader.NBShaderPassFeatureCatalog").GetField("RawPassFeatureIds",All).GetValue(null);
        static bool Project(IEnumerable<Material> m,string[] passes,out bool changed)
        {object[] args={m,Tier(3),RawKeywords,passes,false};bool result=(bool)Invoke(Unique(Runtime,"TryApplyGraphOwnedProjection",typeof(IEnumerable<Material>),Tier(3).GetType(),typeof(IEnumerable<string>),typeof(IEnumerable<string>),typeof(bool).MakeByRefType()),null,args);changed=(bool)args[4];return result;}
        Material Expected(Material m)
        {var e=new Material(m){hideFlags=HideFlags.HideAndDontSave};owned.Add(e);foreach(string tag in ((string[])typeof(G4GraphGuiFeatureIntentTests).GetField("PassNames",All).GetValue(null)).Concat(ExtraPassTags))e.SetShaderPassEnabled(tag,m.GetShaderPassEnabled(tag));return e;}
        static void Caster(Material m,bool enabled){m.SetFloat("_CastShadows",enabled?1:0);m.SetShaderPassEnabled("ShadowCaster",enabled);}
        static int Lo(Material m)=>Mathf.RoundToInt(m.GetFloat("_NB_Flags1Lo16"))&65535;
        static void Bit(Material m,int mask,bool on){m.SetFloat("_NB_Flags1Lo16",(Lo(m)&~mask)|(on?mask:0));}
        void Click(NBFXMainTexGUIEventHost h,object root,object item,bool reset=false)
        {h.Draw=()=>Call(root,"DrawGraphBaseShadowInputs",item);Send(h,new Event{type=EventType.Layout});Send(h,new Event{type=EventType.Repaint});Rect rect=(Rect)Field(item,reset?"ResetRect":"ControlRect");Assert.That(rect.width>0&&rect.height>0,Is.True);Send(h,new Event{type=EventType.MouseDown,button=0,mousePosition=rect.center});Send(h,new Event{type=EventType.MouseUp,button=0,mousePosition=rect.center});}
        [Test]public void G4Shadow_CPU_NewFloatAndFutureInvalidLastAtomicRefusal()
        {
            var a=New();var b=New();Assert.That(a.shader.GetPropertyType(a.shader.FindPropertyIndex("_TransparentShadowDitherToggle")),Is.EqualTo(ShaderPropertyType.Float));Assert.That(a.shader.GetPropertyDefaultFloatValue(a.shader.FindPropertyIndex("_TransparentShadowDitherToggle")),Is.Zero);
            var root=Root(a,b);Assert.That(Call(root,"InitializeGraphBaseShadowInputs"),Is.True);Assert.That(Field(root,"_graphAffectsShadowsItem").GetType().Name,Is.EqualTo("ToggleItem"));
            b.SetFloat("_TransparentShadowDitherToggle",float.NaN);var sa=Snapshot.Read(a);var sb=Snapshot.Read(b);bool changed;Assert.That(Project(new[]{a,b},RawPasses,out changed),Is.False);Assert.That(changed,Is.False);Assert.That(Call(Sync(root),"TryWriteGraphBaseShadow","_AffectsShadows",true),Is.False);sa.Same(a,"Invalid last Dither rejects entire Graph selection");sb.Same(b,"Illegal field preserved");
            b.SetFloat("_TransparentShadowDitherToggle",0);b.SetFloat("_NB_GraphGUIStateVersion",3);sa=Snapshot.Read(a);sb=Snapshot.Read(b);Assert.That(Project(new[]{a,b},RawPasses,out changed),Is.False);Assert.That(changed,Is.False);sa.Same(a,"Future last marker rejects before first write");sb.Same(b,"Future marker preserved");
            var syncType=Find("NBShaderEditor.NBShaderSyncService");var coverage=Unique(syncType,"TryApplyGraphShadowCoverageForSurface",typeof(IList<Material>),typeof(bool).MakeByRefType());
            // Valid first target would clear coverage, but future second target rejects ALL NB writes.
            a.SetFloat("_Surface",0);a.SetFloat("_NB_Flags1Lo16",3);a.SetFloat("_TransparentShadowDitherToggle",1);b.SetFloat("_Surface",0);b.SetFloat("_NB_Flags1Lo16",3);b.SetFloat("_TransparentShadowDitherToggle",1);
            sa=Snapshot.Read(a);sb=Snapshot.Read(b);object[] coverageArgs={new[]{a,b},false};Assert.That((bool)Invoke(coverage,null,coverageArgs),Is.False);Assert.That(coverageArgs[1],Is.False);sa.Same(a,"Future last Surface selection NB raw remains unchanged");sb.Same(b,"Official Surface authority independent; future NB raw preserved");
            b.SetFloat("_NB_GraphGUIStateVersion",2);b.SetFloat("_NB_Flags0Lo16",float.NaN);sa=Snapshot.Read(a);sb=Snapshot.Read(b);coverageArgs=new object[]{new[]{a,b},false};Assert.That((bool)Invoke(coverage,null,coverageArgs),Is.False);Assert.That(coverageArgs[1],Is.False);sa.Same(a,"Invalid last flags Surface whole selection refuses");sb.Same(b,"Invalid raw half is never canonicalized by Surface helper");
            b.SetFloat("_NB_Flags0Lo16",0);sa=Snapshot.Read(a);sb=Snapshot.Read(b);coverageArgs=new object[]{new[]{a,b},false};Assert.That((bool)Invoke(coverage,null,coverageArgs),Is.True);Assert.That(coverageArgs[1],Is.True);Assert.That(Lo(a)&3,Is.Zero);Assert.That(Lo(b)&3,Is.Zero);Assert.That(a.GetFloat("_TransparentShadowDitherToggle"),Is.Zero);Assert.That(b.GetFloat("_TransparentShadowDitherToggle"),Is.Zero);sa.Same(a,"Valid Surface action only coverage+mirror A","_NB_Flags1Lo16","_TransparentShadowDitherToggle");sb.Same(b,"Valid Surface action only coverage+mirror B","_NB_Flags1Lo16","_TransparentShadowDitherToggle");
        }
        [Test]public void G4Shadow_CPU_RuntimeDenyRestoreOnlyCasterRawBitAndManualCastAuthority()
        {
            var m=New();m.SetFloat("_AffectsShadows",1);m.SetFloat("_TransparentShadowDitherToggle",0);m.SetFloat("_NB_Flags1Lo16",3);m.SetFloat("_NB_Flags1Hi16",70000.125f);bool changed;Assert.That(Project(new[]{m},RawPasses,out changed),Is.True);Caster(m,false);m.SetShaderPassEnabled("SRPDefaultUnlit",false);m.SetShaderPassEnabled("UniversalForward",false);var manual=Snapshot.Read(m);G4SpecDebugFixture.Validate(m);manual.Same(m,"Ordinary Validate preserves manual Cast and raw bit1/mirror conflict");
            var expected=Expected(m);Caster(expected,true);Assert.That(Project(new[]{m},RawPasses,out changed),Is.True);Assert.That(changed,Is.True);Snapshot.Read(expected).Same(m,"Explicit Runtime reprojects raw Affects only into Cast/ShadowCaster");Assert.That(Lo(m)&2,Is.EqualTo(2));Assert.That(m.GetFloat("_TransparentShadowDitherToggle"),Is.Zero);
            expected=Expected(m);Caster(expected,false);Assert.That(Project(new[]{m},Array.Empty<string>(),out changed),Is.True);Snapshot.Read(expected).Same(m,"Pass deny never changes raw Dither/coverage/Affects/SavedTier");expected=Expected(m);Caster(expected,true);Assert.That(Project(new[]{m},RawPasses,out changed),Is.True);Snapshot.Read(expected).Same(m,"Pass restore keeps raw intent");var restored=Snapshot.Read(m);Assert.That(Project(new[]{m},RawPasses,out changed),Is.True);Assert.That(changed,Is.False);restored.Same(m,"Complete no-op");
            var native=new Material(Shader.Find("Effects/NBShader")){hideFlags=HideFlags.HideAndDontSave};owned.Add(native);var nativeBefore=Snapshot.Read(native);object[] args={native,Tier(3),RawKeywords,RawPasses,null};Assert.That((bool)Invoke(Unique(Runtime,"CanApplyGraphOwnedShadowPassState",typeof(Material),Tier(3).GetType(),typeof(IEnumerable<string>),typeof(IEnumerable<string>),Find("NBShader.NBShaderPassIntent").MakeByRefType()),null,args),Is.False);nativeBefore.Same(native,"Graph projector refuses Native");
            var other=new Material(Shader.Find("Hidden/InternalErrorShader")){hideFlags=HideFlags.HideAndDontSave};owned.Add(other);args=new object[]{other,Tier(3),RawKeywords,RawPasses,null};Assert.That((bool)Invoke(Unique(Runtime,"CanApplyGraphOwnedShadowPassState",typeof(Material),Tier(3).GetType(),typeof(IEnumerable<string>),typeof(IEnumerable<string>),Find("NBShader.NBShaderPassIntent").MakeByRefType()),null,args),Is.True);Assert.That(args[4],Is.Null,"Absent capability owns no Caster");
        }
        [Test]public void G4Shadow_GUI_MixedRawDitherPassiveActualToggleUndoRedo()
        {
            var a=New();var b=New();a.SetFloat("_AffectsShadows",1);b.SetFloat("_AffectsShadows",1);a.SetFloat("_NB_Flags1Lo16",3);b.SetFloat("_NB_Flags1Lo16",1);a.SetFloat("_TransparentShadowDitherToggle",0);b.SetFloat("_TransparentShadowDitherToggle",1);a.SetFloat("_NB_Flags1Hi16",70000.125f);b.SetFloat("_NB_Flags1Hi16",90000.25f);Caster(a,true);Caster(b,true);var root=Root(a,b);var h=Host(root);var item=Field(root,"_graphTransparentShadowDitherItem");h.Draw=()=>Call(root,"DrawGraphBaseShadowInputs",item);var sa=Snapshot.Read(a);var sb=Snapshot.Read(b);Send(h,new Event{type=EventType.Layout});Send(h,new Event{type=EventType.Repaint});sa.Same(a,"Raw-bit/mirror conflict passive A");sb.Same(b,"Mixed passive B");Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            try{Click(h,root,item);bool on=(Lo(a)&2)!=0;Assert.That((Lo(b)&2)!=0,Is.EqualTo(on));Assert.That(a.GetFloat("_TransparentShadowDitherToggle"),Is.EqualTo(on?1:0));Assert.That(b.GetFloat("_TransparentShadowDitherToggle"),Is.EqualTo(on?1:0));Assert.That(a.GetFloat("_TransparentShadowDitherToggle")!=0||b.GetFloat("_TransparentShadowDitherToggle")!=1,Is.True,"Actual click changes at least one stale mirror");sa.Same(a,"Only raw Dither bit/mirror A","_TransparentShadowDitherToggle","_NB_Flags1Lo16");sb.Same(b,"Only raw Dither bit/mirror B","_TransparentShadowDitherToggle","_NB_Flags1Lo16");Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(group);var aa=Snapshot.Read(a);var ab=Snapshot.Read(b);h.Draw=null;Undo.PerformUndo();sa.Same(a,"Dither Undo A");sb.Same(b,"Dither Undo B");Undo.PerformRedo();aa.Same(a,"Dither Redo A");ab.Same(b,"Dither Redo B");}finally{h.Draw=null;Undo.RevertAllDownToGroup(group);}
        }
        [Test]public void G4Shadow_GUI_ActualAffectsIgnoreAndOriginalResetUndoRedo()
        {
            var m=New();m.SetFloat("_NB_Flags1Hi16",70000.125f);m.SetFloat("_NB_Flags1Lo16",1);m.SetFloat("_IgnoreVetexColor_Toggle",0);m.SetFloat("_AffectsShadows",0);Caster(m,false);var root=Root(m);var h=Host(root);var before=Snapshot.Read(m);var expected=Expected(m);Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            try{Click(h,root,Field(root,"_graphAffectsShadowsItem"));Assert.That(m.GetFloat("_AffectsShadows"),Is.EqualTo(1));Assert.That(m.GetFloat("_CastShadows"),Is.EqualTo(1));Assert.That(m.GetShaderPassEnabled("ShadowCaster"),Is.True);expected.SetFloat("_AffectsShadows",1);Caster(expected,true);Snapshot.Read(expected).Same(m,"Affects owns only raw intent+derived caster");Click(h,root,Field(root,"_graphIgnoreVertexColorItem"));Assert.That(Lo(m)&512,Is.EqualTo(512));Assert.That(m.GetFloat("_IgnoreVetexColor_Toggle"),Is.EqualTo(1));expected.SetFloat("_IgnoreVetexColor_Toggle",1);Bit(expected,512,true);Snapshot.Read(expected).Same(m,"Ignore owns bit9 and mirror only");Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(group);var after=Snapshot.Read(m);h.Draw=null;Undo.PerformUndo();before.Same(m,"Two actual controls full Undo");Undo.PerformRedo();after.Same(m,"Two actual controls full Redo");Click(h,root,Field(root,"_graphAffectsShadowsItem"),true);Click(h,root,Field(root,"_graphIgnoreVertexColorItem"),true);Assert.That(m.GetFloat("_AffectsShadows"),Is.Zero);Assert.That(m.GetFloat("_IgnoreVetexColor_Toggle"),Is.Zero);Assert.That(Lo(m)&512,Is.Zero);Assert.That(m.GetFloat("_CastShadows"),Is.Zero);Assert.That(m.GetShaderPassEnabled("ShadowCaster"),Is.False);}finally{h.Draw=null;Undo.RevertAllDownToGroup(group);}
        }
        [Test]public void G4Shadow_CPU_PortalMaskClearOffCannotReviveAndUndoRedo()
        {
            var m=New();m.SetFloat("_TransparentShadowDitherToggle",1);m.SetFloat("_NB_Flags1Lo16",3|512);m.SetFloat("_NB_Flags1Hi16",70000.125f);m.SetFloat("_Portal_Toggle",0);m.SetFloat("_Portal_MaskToggle",0);var root=Root(m);var before=Snapshot.Read(m);Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            try{Assert.That(Call(Sync(root),"TryApplyGraphPortalToggle","_Portal_Toggle",true,false),Is.True);Assert.That(Call(Sync(root),"TryApplyGraphPortalToggle","_Portal_MaskToggle",true,false),Is.True);Assert.That(m.GetFloat("_Surface"),Is.Zero);Assert.That(Lo(m)&3,Is.Zero);Assert.That(m.GetFloat("_TransparentShadowDitherToggle"),Is.Zero);Assert.That(Call(Sync(root),"TryApplyGraphPortalToggle","_Portal_Toggle",false,true),Is.True);Assert.That(m.GetFloat("_Surface"),Is.EqualTo(1));Assert.That(Lo(m)&3,Is.EqualTo(1));Assert.That(Lo(m)&512,Is.EqualTo(512));Assert.That(m.GetFloat("_NB_Flags1Hi16"),Is.EqualTo(70000.125f));Assert.That(m.GetFloat("_TransparentShadowDitherToggle"),Is.Zero);Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(group);var after=Snapshot.Read(m);Undo.PerformUndo();before.Same(m,"Original presets+Dither mirror full Undo");Undo.PerformRedo();after.Same(m,"Original presets never revive cleared Dither");}finally{Undo.RevertAllDownToGroup(group);}
        }
    }
}
