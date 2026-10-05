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
    public sealed class G4GraphBaseNumericSharedGuiTests
    {
        const BindingFlags All=BindingFlags.Static|BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        readonly List<Object> owned=new List<Object>();readonly List<G4GraphPersistentGateTierTests> helpers=new List<G4GraphPersistentGateTierTests>();
        static Type Find(string n)=>G4SpecDebugFixture.FindType(n);
        static MethodInfo Unique(Type t,string n,params Type[] sig){var m=t.GetMethods(All).Where(x=>x.Name==n&&x.GetParameters().Select(p=>p.ParameterType).SequenceEqual(sig)).ToArray();Assert.That(m.Length,Is.EqualTo(1),t.FullName+"."+n);return m[0];}
        static object Invoke(MethodInfo m,object target,params object[] args){try{return m.Invoke(target,args);}catch(TargetInvocationException e){ExceptionDispatchInfo.Capture(e.InnerException??e).Throw();throw;}}
        static object Call(object target,string n,params object[] args)
        {Type[] sig;switch(n){case "HasGraphBaseNumericSchema":case "InitializeGraphBaseNumericInputs":case "RefreshGraphMainTexPropertyReferences":sig=Type.EmptyTypes;break;case "DrawGraphBaseNumericInputs":sig=new[]{Find("NBShaderEditor.ShaderGUIItem")};break;case "TryWriteGraphBaseNumeric":sig=new[]{typeof(string),typeof(float)};break;case "TryWriteGraphAlphaRange":sig=new[]{typeof(float),typeof(float),typeof(bool),typeof(bool)};break;case "TryResetGraphBaseNumeric":sig=new[]{typeof(string)};break;default:Assert.Fail(n);return null;}return Invoke(Unique(target.GetType(),n,sig),target,args);}
        static object Field(object o,string n){for(Type t=o.GetType();t!=null;t=t.BaseType){var f=t.GetField(n,All|BindingFlags.DeclaredOnly);if(f!=null)return f.GetValue(o);}Assert.Fail(n);return null;}
        static object Sync(object root)=>root.GetType().GetProperty("SyncService",All).GetValue(root);
        static IEnumerable<object> Children(object item){yield return item;foreach(object child in (IEnumerable)Field(item,"ChildrenItemList"))foreach(var sub in Children(child))yield return sub;}
        sealed class Snapshot
        {object data;static Type T=>typeof(G4GraphGuiFeatureIntentTests).GetNestedType("Snapshot",All);public static Snapshot Read(Material m)=>new Snapshot{data=Invoke(Unique(T,"Read",typeof(Material)),null,m)};public void Same(Material m,string label,params string[] except)=>Invoke(Unique(T,"AssertSame",typeof(Material),typeof(string),typeof(string[])),data,m,label,except);}
        [OneTimeSetUp]public void Guard(){Assert.That(Path.GetFullPath(Application.dataPath),Is.EqualTo(Path.GetFullPath(@"D:\UnityProject\NBUnityProject\.utmp\NBFXMeshValidation-20261002\Assets")).IgnoreCase);for(int i=0;i<SceneManager.sceneCount;++i){var s=SceneManager.GetSceneAt(i);Assert.That((s.name+"/"+s.path).IndexOf("TAI",StringComparison.OrdinalIgnoreCase),Is.LessThan(0));}}
        Material New(){var shader=AssetDatabase.LoadAssetAtPath<Shader>(G4SpecDebugFixture.GraphPath);Assert.That(shader&&shader.isSupported,Is.True);var m=new Material(shader){hideFlags=HideFlags.HideAndDontSave};owned.Add(m);m.SetFloat("_NB_GraphGUIStateVersion",2);m.SetFloat("_NBShaderFeatureTier",3);G4SpecDebugFixture.Validate(m);m.SetFloat("_BaseOptionBigBlockItemFoldOut",1);return m;}
        object Root(params Material[] materials){var helper=new G4GraphPersistentGateTierTests();helpers.Add(helper);return Invoke(Unique(typeof(G4GraphPersistentGateTierTests),"Root",typeof(Material[])),helper,(object)materials);}
        NBFXMainTexGUIEventHost Host(object root)
        {var h=ScriptableObject.CreateInstance<NBFXMainTexGUIEventHost>();owned.Add(h);h.hideFlags=HideFlags.HideAndDontSave;h.position=new Rect(20,20,650,650);h.SetupCommand="NBFX_BaseNumeric_"+Guid.NewGuid().ToString("N");h.Setup=()=>Assert.That(Call(root,"InitializeGraphBaseNumericInputs"),Is.True);h.ShowUtility();h.SendEvent(new Event{type=EventType.ExecuteCommand,commandName=h.SetupCommand});Check(h);Assert.That(h.Initialized,Is.True);h.Draw=()=>Call(root,"DrawGraphBaseNumericInputs",(object)null);return h;}
        static void Check(NBFXMainTexGUIEventHost h){if(h.Failure!=null)ExceptionDispatchInfo.Capture(h.Failure).Throw();}
        static void Send(NBFXMainTexGUIEventHost h,Event e){var raw=e.rawType;h.Counts.TryGetValue(raw,out int before);h.SendEvent(e);Check(h);Assert.That(e.rawType,Is.EqualTo(raw));Assert.That(h.Counts.TryGetValue(raw,out int after)&&after>before,Is.True);}
        [TearDown]public void Cleanup(){foreach(var h in owned.OfType<NBFXMainTexGUIEventHost>()){h.Draw=null;h.Setup=null;h.Close();}foreach(var x in helpers)x.Cleanup();helpers.Clear();foreach(var x in owned.AsEnumerable().Reverse())if(x)Object.DestroyImmediate(x);owned.Clear();}
        [Test]public void G4BaseNumeric_CPU_ActualTypesOriginalFactoriesUnknownSecondReject()
        {
            var a=New();var b=New();Assert.That(a.shader.GetPropertyType(a.shader.FindPropertyIndex("_BaseColorIntensityForTimeline")),Is.EqualTo(ShaderPropertyType.Range));Assert.That(a.shader.GetPropertyType(a.shader.FindPropertyIndex("_AlphaAll")),Is.EqualTo(ShaderPropertyType.Float));Assert.That(a.shader.GetPropertyType(a.shader.FindPropertyIndex("_fogintensity")),Is.EqualTo(ShaderPropertyType.Float));Assert.That(a.shader.GetPropertyType(a.shader.FindPropertyIndex("AlphaAllRangeVec")),Is.EqualTo(ShaderPropertyType.Vector));Assert.That(a.shader.GetPropertyDefaultVectorValue(a.shader.FindPropertyIndex("AlphaAllRangeVec")),Is.EqualTo(new Vector4(0,1,0,0)));var root=Root(a,b);var h=Host(root);var children=Children(Field(root,"_graphBaseNumericBlock")).Skip(1).ToArray();Assert.That(children.Length,Is.EqualTo(3));Assert.That(children.Select(x=>x.GetType().Name),Is.EqualTo(new[]{"ShaderGUIFloatItem","ShaderGUISliderItem","ShaderGUISliderItem"}));h.Draw=null;
            b.SetFloat("_NB_GraphGUIStateVersion",3);var sa=Snapshot.Read(a);var sb=Snapshot.Read(b);Assert.That(Call(Sync(root),"TryWriteGraphBaseNumeric","_AlphaAll",.5f),Is.False);sa.Same(a,"Future second marker rejects before first write");sb.Same(b,"Opaque future marker");Assert.That(Call(Sync(root),"TryWriteGraphBaseNumeric","_Color",.5f),Is.False);Assert.That(Call(Sync(root),"TryWriteGraphBaseNumeric","_AlphaAll",float.NaN),Is.False);
        }
        [Test]public void G4BaseNumeric_GUI_MixedOutOfRangeWholeBlockPassiveReadOnly()
        {
            var a=New();var b=New();a.SetFloat("_BaseColorIntensityForTimeline",2.25f);b.SetFloat("_BaseColorIntensityForTimeline",.125f);a.SetFloat("_AlphaAll",3.25f);b.SetFloat("_AlphaAll",-.25f);a.SetFloat("_fogintensity",2.25f);b.SetFloat("_fogintensity",-.25f);a.SetVector("AlphaAllRangeVec",new Vector4(-1,1,11,12));b.SetVector("AlphaAllRangeVec",new Vector4(-2,2,21,22));var root=Root(a,b);var h=Host(root);var sa=Snapshot.Read(a);var sb=Snapshot.Read(b);Send(h,new Event{type=EventType.Layout});Send(h,new Event{type=EventType.Repaint});sa.Same(a,"Original three controls passive preserve unclamped values and own range");sb.Same(b,"Mixed values never copy first material");
        }
        [Test]public void G4BaseNumeric_CPU_AlphaRangeComponentOwnershipClampAndUndoRedo()
        {
            var a=New();var b=New();a.SetFloat("_AlphaAll",.25f);b.SetFloat("_AlphaAll",.75f);a.SetVector("AlphaAllRangeVec",new Vector4(0,1,11,12));b.SetVector("AlphaAllRangeVec",new Vector4(-1,2,21,22));var root=Root(a,b);var sa=Snapshot.Read(a);var sb=Snapshot.Read(b);Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            try{Assert.That(Call(Sync(root),"TryWriteGraphAlphaRange",.5f,999f,true,false),Is.True);Assert.That(a.GetVector("AlphaAllRangeVec"),Is.EqualTo(new Vector4(.5f,1,11,12)));Assert.That(b.GetVector("AlphaAllRangeVec"),Is.EqualTo(new Vector4(.5f,2,21,22)));Assert.That(a.GetFloat("_AlphaAll"),Is.EqualTo(.5f));Assert.That(b.GetFloat("_AlphaAll"),Is.EqualTo(.75f));sa.Same(a,"Only edited range component+original value clamp","AlphaAllRangeVec","_AlphaAll");sb.Same(b,"Other range and zw independent","AlphaAllRangeVec","_AlphaAll");Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(group);var aa=Snapshot.Read(a);var ab=Snapshot.Read(b);Undo.PerformUndo();sa.Same(a,"Range A Undo");sb.Same(b,"Range B Undo");Undo.PerformRedo();aa.Same(a,"Range A Redo");ab.Same(b,"Range B Redo");}
            finally{Undo.RevertAllDownToGroup(group);}
        }
        [Test]public void G4BaseNumeric_GUI_ActualOverallAlphaSliderCompleteUndoRedo()
        {
            var value=New();value.SetFloat("_AlphaAll",.125f);var root=Root(value);var h=Host(root);var item=Children(Field(root,"_graphBaseNumericBlock")).Single(x=>(string)Field(x,"PropertyName")=="_AlphaAll");h.Draw=()=>Call(root,"DrawGraphBaseNumericInputs",item);Send(h,new Event{type=EventType.Layout});Send(h,new Event{type=EventType.Repaint});var rect=(Rect)Field(item,"ControlRect");Assert.That(rect.width>90&&rect.height>0,Is.True);var before=Snapshot.Read(value);Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            try{var point=new Vector2(rect.center.x,rect.center.y);Send(h,new Event{type=EventType.MouseDown,button=0,mousePosition=point});Send(h,new Event{type=EventType.MouseUp,button=0,mousePosition=point});Assert.That(Mathf.Abs(value.GetFloat("_AlphaAll")-.125f),Is.GreaterThan(.01f));Assert.That(value.GetFloat("_AlphaAll"),Is.InRange(0,1));before.Same(value,"Actual original Slider writes only alpha","_AlphaAll");Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(group);var after=Snapshot.Read(value);h.Draw=null;Undo.PerformUndo();before.Same(value,"Actual Slider full Undo");Undo.PerformRedo();after.Same(value,"Actual Slider full Redo");}
            finally{h.Draw=null;Undo.RevertAllDownToGroup(group);}
        }
        [Test]public void G4BaseNumeric_GUI_ActualThreeOriginalResetDefaultsAndUndoRedo()
        {
            var value=New();value.SetFloat("_BaseColorIntensityForTimeline",2.25f);value.SetFloat("_AlphaAll",.25f);value.SetVector("AlphaAllRangeVec",new Vector4(-1,2,11,12));value.SetFloat("_fogintensity",.25f);var root=Root(value);var h=Host(root);var children=Children(Field(root,"_graphBaseNumericBlock")).Skip(1).ToArray();var before=Snapshot.Read(value);Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            try{foreach(var item in children){h.Draw=()=>Call(root,"DrawGraphBaseNumericInputs",item);Send(h,new Event{type=EventType.Layout});Send(h,new Event{type=EventType.Repaint});var reset=(Rect)Field(item,"ResetRect");Assert.That(reset.width>0&&reset.height>0,Is.True);Send(h,new Event{type=EventType.MouseDown,button=0,mousePosition=reset.center});Send(h,new Event{type=EventType.MouseUp,button=0,mousePosition=reset.center});}foreach(string n in new[]{"_BaseColorIntensityForTimeline","_AlphaAll","_fogintensity"})Assert.That(value.GetFloat(n),Is.EqualTo(value.shader.GetPropertyDefaultFloatValue(value.shader.FindPropertyIndex(n))));Assert.That(value.GetVector("AlphaAllRangeVec"),Is.EqualTo(new Vector4(0,1,0,0)));before.Same(value,"Same original peritem Reset default fields only","_BaseColorIntensityForTimeline","_AlphaAll","_fogintensity","AlphaAllRangeVec");Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(group);var after=Snapshot.Read(value);h.Draw=null;Undo.PerformUndo();before.Same(value,"Original three Reset full Undo");Undo.PerformRedo();after.Same(value,"Original three Reset full Redo");}
            finally{h.Draw=null;Undo.RevertAllDownToGroup(group);}
        }
    }
}
