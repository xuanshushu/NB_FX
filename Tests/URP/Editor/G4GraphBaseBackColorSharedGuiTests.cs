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
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace NBFX.Baseline.Tests
{
    // Original shared block and existing event/snapshot fixture only; no native popup or render framework.
    public sealed class G4GraphBaseBackColorSharedGuiTests
    {
        const BindingFlags All=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance;
        const string Fold="_BaseBackColorFoldOut",Toggle="_BaseBackColor_Toggle",ColorName="_BaseBackColor",Hi="_NB_Flags0Hi16",Lo="_NB_Flags0Lo16";
        const int Mask=1<<12;
        readonly List<Object> owned=new List<Object>();readonly List<G4GraphPersistentGateTierTests> helpers=new List<G4GraphPersistentGateTierTests>();
        static Type Find(string n)=>G4SpecDebugFixture.FindType(n);
        static MethodInfo Unique(Type t,string n,params Type[] sig)
        {var list=t.GetMethods(All).Where(m=>m.Name==n&&m.GetParameters().Select(p=>p.ParameterType).SequenceEqual(sig)).ToArray();Assert.That(list.Length,Is.EqualTo(1),t.FullName+"."+n+" exact signature");return list[0];}
        static object Invoke(MethodInfo m,object target,params object[] args)
        {try{return m.Invoke(target,args);}catch(TargetInvocationException e){ExceptionDispatchInfo.Capture(e.InnerException??e).Throw();throw;}}
        static object Call(object o,string n,params object[] args)
        {
            Type[] sig;switch(n)
            {case "HasGraphBaseBackColorEditSchema":case "InitializeGraphBaseBackColorInputs":case "RefreshGraphMainTexPropertyReferences":sig=Type.EmptyTypes;break;
             case "TryApplyGraphBaseBackColorToggle":sig=new[]{typeof(bool)};break;
             case "DrawGraphBaseBackColorInputs":sig=new[]{Find("NBShaderEditor.ShaderGUIItem")};break;
             default:Assert.Fail("Unreviewed signature "+n);return null;}
            var m=Unique(o.GetType(),n,sig);Assert.That(m.IsStatic,Is.False);return Invoke(m,o,args);
        }
        static object Field(object o,string n)
        {for(Type t=o.GetType();t!=null;t=t.BaseType){var f=t.GetField(n,All|BindingFlags.DeclaredOnly);if(f!=null)return f.GetValue(o);}Assert.Fail(n);return null;}
        static object Sync(object root)=>root.GetType().GetProperty("SyncService",All).GetValue(root);
        sealed class Snapshot
        {
            object data;static Type Shared=>typeof(G4GraphGuiFeatureIntentTests).GetNestedType("Snapshot",BindingFlags.NonPublic);
            public static Snapshot Read(Material m)=>new Snapshot{data=Invoke(Unique(Shared,"Read",typeof(Material)),null,m)};
            public void Same(Material m,string label,params string[] allow)=>Invoke(Unique(Shared,"AssertSame",typeof(Material),typeof(string),typeof(string[])),data,m,label,allow);
        }
        [OneTimeSetUp]public void Guard()
        {Assert.That(Path.GetFullPath(Application.dataPath),Is.EqualTo(Path.GetFullPath(@"D:\UnityProject\NBUnityProject\.utmp\NBFXMeshValidation-20261002\Assets")).IgnoreCase);for(int i=0;i<SceneManager.sceneCount;++i){var s=SceneManager.GetSceneAt(i);Assert.That((s.name+"/"+s.path).IndexOf("TAI",StringComparison.OrdinalIgnoreCase),Is.LessThan(0));}}
        Material Material()
        {var shader=AssetDatabase.LoadAssetAtPath<Shader>(G4SpecDebugFixture.GraphPath);Assert.That(shader&&shader.isSupported,Is.True);var m=new Material(shader){hideFlags=HideFlags.HideAndDontSave};owned.Add(m);m.SetFloat("_NB_GraphGUIStateVersion",2);m.SetFloat("_NBShaderFeatureTier",3);G4SpecDebugFixture.Validate(m);m.SetFloat(Fold,1);return m;}
        object Root(params Material[] materials)
        {var h=new G4GraphPersistentGateTierTests();helpers.Add(h);return Invoke(Unique(typeof(G4GraphPersistentGateTierTests),"Root",typeof(Material[])),h,(object)materials);}
        Material Probe(bool wrongColor)
        {
            string text="Shader \"Hidden/NBFX/BackColorSchemaProbe\" { Properties { ";
            foreach(string p in new[]{"_NB_DistortionMode","_NB_Flags0Lo16","_NB_Flags0Hi16","_NB_Flags1Lo16","_NB_Flags1Hi16","_NB_GraphGUIStateVersion",Toggle})text+=p+"(\""+p+"\",Float)="+(p=="_NB_GraphGUIStateVersion"?"2":"0")+" ";
            if(wrongColor)text+=Fold+"(\"Fold\",Float)=0 "+ColorName+"(\"Wrong Color\",Float)=0 ";else text+=ColorName+"(\"Color\",Color)=(1,1,1,1) ";
            text+="} SubShader { Pass { } } }";var shader=ShaderUtil.CreateShaderAsset(text,false);Assert.That(shader,Is.Not.Null);shader.hideFlags=HideFlags.HideAndDontSave;owned.Add(shader);var material=new Material(shader){hideFlags=HideFlags.HideAndDontSave};owned.Add(material);return material;
        }
        static bool Display(object root,out bool enabled,out bool mixed)
        {var sync=Sync(root);object[] args={false,false};bool result=(bool)Invoke(Unique(sync.GetType(),"TryGetGraphBaseBackColorDisplay",typeof(bool).MakeByRefType(),typeof(bool).MakeByRefType()),sync,args);enabled=(bool)args[0];mixed=(bool)args[1];return result;}
        NBFXMainTexGUIEventHost Host(object root)
        {
            var host=ScriptableObject.CreateInstance<NBFXMainTexGUIEventHost>();owned.Add(host);host.hideFlags=HideFlags.HideAndDontSave;host.position=new Rect(20,20,650,650);host.SetupCommand="NBFX_BackColor_"+Guid.NewGuid().ToString("N");host.Setup=()=>Assert.That(Call(root,"InitializeGraphBaseBackColorInputs"),Is.True);host.ShowUtility();host.SendEvent(new Event{type=EventType.ExecuteCommand,commandName=host.SetupCommand});Check(host);Assert.That(host.Initialized,Is.True,"Actual original host setup");host.Draw=()=>Call(root,"DrawGraphBaseBackColorInputs",(object)null);return host;
        }
        static void Check(NBFXMainTexGUIEventHost host){if(host.Failure!=null)ExceptionDispatchInfo.Capture(host.Failure).Throw();}
        static void Send(NBFXMainTexGUIEventHost host,Event e)
        {var raw=e.rawType;host.Counts.TryGetValue(raw,out int before);host.SendEvent(e);Check(host);Assert.That(e.rawType,Is.EqualTo(raw));Assert.That(host.Counts.TryGetValue(raw,out int after)&&after>before,Is.True,"Actual Draw event count");}
        static int Raw(Material value)=>Mathf.RoundToInt(Mathf.Clamp(value.GetFloat(Hi),0,65535));
        [TearDown]public void Cleanup()
        {foreach(var host in owned.OfType<NBFXMainTexGUIEventHost>()){host.Draw=null;host.Setup=null;host.Close();}foreach(var h in helpers)h.Cleanup();helpers.Clear();foreach(var value in owned.AsEnumerable().Reverse())if(value)Object.DestroyImmediate(value);owned.Clear();}
        [Test]public void G4BackColor_CPU_ActualSchemaMissingWrongTypeAndSecondFutureAtomicReject()
        {
            var a=Material();Assert.That(a.shader.GetPropertyType(a.shader.FindPropertyIndex(Fold)),Is.EqualTo(ShaderPropertyType.Float));Assert.That(a.shader.GetPropertyType(a.shader.FindPropertyIndex(Toggle)),Is.EqualTo(ShaderPropertyType.Float));Assert.That(a.shader.GetPropertyType(a.shader.FindPropertyIndex(ColorName)),Is.EqualTo(ShaderPropertyType.Color));
            Assert.That(Call(Sync(Root(a)),"HasGraphBaseBackColorEditSchema"),Is.True);
            foreach(bool wrong in new[]{false,true}){var p=Probe(wrong);var before=Snapshot.Read(p);var root=Root(p);Assert.That(Call(Sync(root),"HasGraphBaseBackColorEditSchema"),Is.False);Assert.That(Call(Sync(root),"TryApplyGraphBaseBackColorToggle",true),Is.False);before.Same(p,"Missing Fold/wrong Color type opaque");}
            var b=Material();var r=Root(a,b);b.SetFloat("_NB_GraphGUIStateVersion",3);var sa=Snapshot.Read(a);var sb=Snapshot.Read(b);Assert.That(Call(Sync(r),"TryApplyGraphBaseBackColorToggle",true),Is.False);sa.Same(a,"Reject all targets before first owned write");sb.Same(b,"Unknown second marker opaque");
        }
        [Test]public void G4BackColor_GUI_RawMirrorConflictMixedPassiveReadOnly()
        {
            var a=Material();var b=Material();a.SetFloat(Hi,Mask+123.25f);a.SetFloat(Toggle,0);b.SetFloat(Hi,123.25f);b.SetFloat(Toggle,3.25f);a.SetFloat(Lo,99999.25f);b.SetFloat(Lo,-3.75f);a.SetColor(ColorName,new Color(2,.3f,.4f,.6f));b.SetColor(ColorName,new Color(.2f,3,.4f,.7f));var root=Root(a,b);var host=Host(root);Assert.That(Display(root,out bool enabled,out bool mixed),Is.True);Assert.That(enabled,Is.True);Assert.That(mixed,Is.True);
            var sa=Snapshot.Read(a);var sb=Snapshot.Read(b);Send(host,new Event{type=EventType.Layout});Send(host,new Event{type=EventType.Repaint});sa.Same(a,"Original raw-on/mirror-off header and Color passive");sb.Same(b,"Raw-off/mirror-on mixed selection remains exact");Assert.That(Field(root,"_graphBaseBackColorItem").GetType().FullName,Is.EqualTo("NBShaderEditor.PropertyToggleBlockItem"));Assert.That(((System.Collections.IList)Field(Field(root,"_graphBaseBackColorItem"),"ChildrenItemList")).Count,Is.EqualTo(1));
        }
        [Test]public void G4BackColor_GUI_ActualOriginalToggleMultiselectCompleteUndoRedo()
        {
            var a=Material();var b=Material();a.SetFloat(Hi,123.25f);b.SetFloat(Hi,65535.25f);a.SetFloat(Toggle,0);b.SetFloat(Toggle,1);a.SetFloat(Lo,-3.75f);b.SetFloat(Lo,99999.25f);var root=Root(a,b);var host=Host(root);Send(host,new Event{type=EventType.Layout});Send(host,new Event{type=EventType.Repaint});var rect=(Rect)Field(Field(root,"_graphBaseBackColorItem"),"ControlRect");Assert.That(rect.width>0&&rect.height>0,Is.True);int ra=Raw(a),rb=Raw(b);var sa=Snapshot.Read(a);var sb=Snapshot.Read(b);Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            try{var p=new Vector2(rect.x+6,rect.center.y);Send(host,new Event{type=EventType.MouseDown,button=0,mousePosition=p});Send(host,new Event{type=EventType.MouseUp,button=0,mousePosition=p});Assert.That((Raw(a)&Mask)!=0,Is.EqualTo((Raw(b)&Mask)!=0));Assert.That(a.GetFloat(Toggle),Is.EqualTo((Raw(a)&Mask)!=0?1:0));Assert.That(b.GetFloat(Toggle),Is.EqualTo((Raw(b)&Mask)!=0?1:0));Assert.That((Raw(a)^ra)&~Mask,Is.Zero);Assert.That((Raw(b)^rb)&~Mask,Is.Zero);Assert.That(a.GetFloat(Toggle),Is.EqualTo(1),"First raw-off original checkbox becomes on");sa.Same(a,"Owned half+mirror only",Hi,Toggle);sb.Same(b,"All other raw/Color/fold/gates/keywords/passes unchanged",Hi,Toggle);Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(group);var aa=Snapshot.Read(a);var ab=Snapshot.Read(b);host.Draw=null;Undo.PerformUndo();sa.Same(a,"Actual Toggle complete independent A Undo");sb.Same(b,"Actual Toggle B Undo");Undo.PerformRedo();aa.Same(a,"Toggle A Redo");ab.Same(b,"Toggle B Redo");}
            finally{host.Draw=null;Undo.RevertAllDownToGroup(group);}
        }
        [Test]public void G4BackColor_CPU_DecodedNoopKeepsRawHalfOwnedMirrorUndoRedo()
        {
            var value=Material();value.SetFloat(Hi,65536.25f);value.SetFloat(Lo,-3.75f);value.SetFloat(Toggle,2.25f);var root=Root(value);var before=Snapshot.Read(value);Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            try{Assert.That(Call(Sync(root),"TryApplyGraphBaseBackColorToggle",true),Is.True);Assert.That(value.GetFloat(Hi),Is.EqualTo(65536.25f));Assert.That(value.GetFloat(Toggle),Is.EqualTo(1));before.Same(value,"Decoded no-op preserves complete noncanonical raw halves",Toggle);Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(group);var after=Snapshot.Read(value);Undo.PerformUndo();before.Same(value,"Owned mirror edit Undo");Undo.PerformRedo();after.Same(value,"Mirror edit Redo");}
            finally{Undo.RevertAllDownToGroup(group);}
        }
        [Test]public void G4BackColor_GUI_ActualOriginalResetColorDefaultsCompleteUndoRedo()
        {
            var a=Material();var b=Material();a.SetFloat(Hi,Mask+123.25f);b.SetFloat(Hi,Mask+8192+77.25f);a.SetFloat(Toggle,1);b.SetFloat(Toggle,1);a.SetColor(ColorName,new Color(.2f,.3f,.4f,.5f));b.SetColor(ColorName,new Color(.6f,.7f,.8f,.9f));var root=Root(a,b);var host=Host(root);Send(host,new Event{type=EventType.Layout});Send(host,new Event{type=EventType.Repaint});var reset=(Rect)Field(Field(root,"_graphBaseBackColorItem"),"ResetRect");Assert.That(reset.width>0&&reset.height>0,Is.True);int ra=Raw(a),rb=Raw(b);var sa=Snapshot.Read(a);var sb=Snapshot.Read(b);Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            try{Send(host,new Event{type=EventType.MouseDown,button=0,mousePosition=reset.center});Send(host,new Event{type=EventType.MouseUp,button=0,mousePosition=reset.center});foreach(var value in new[]{a,b}){Assert.That(Raw(value)&Mask,Is.Zero);Assert.That(value.GetFloat(Toggle),Is.Zero);Assert.That((Vector4)value.GetColor(ColorName),Is.EqualTo(value.shader.GetPropertyDefaultVectorValue(value.shader.FindPropertyIndex(ColorName))));}Assert.That((Raw(a)^ra)&~Mask,Is.Zero);Assert.That((Raw(b)^rb)&~Mask,Is.Zero);sa.Same(a,"Original reset owns Color default+mirror+bit only",ColorName,Toggle,Hi);sb.Same(b,"Fold and all unrelated state preserved",ColorName,Toggle,Hi);Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(group);var aa=Snapshot.Read(a);var ab=Snapshot.Read(b);host.Draw=null;Undo.PerformUndo();sa.Same(a,"Original reset Color/raw A Undo");sb.Same(b,"Original reset B Undo");Undo.PerformRedo();aa.Same(a,"Original reset A Redo");ab.Same(b,"Original reset B Redo");}
            finally{host.Draw=null;Undo.RevertAllDownToGroup(group);}
        }
    }
}
