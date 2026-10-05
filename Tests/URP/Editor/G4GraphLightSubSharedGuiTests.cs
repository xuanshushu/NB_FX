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
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace NBFX.Baseline.Tests
{
    // Existing shared event host, material snapshot and Root only. No native popup or new render harness.
    public sealed class G4GraphLightSubSharedGuiTests
    {
        const BindingFlags All=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
        readonly List<Object> owned=new List<Object>();
        readonly List<G4GraphPersistentGateTierTests> helpers=new List<G4GraphPersistentGateTierTests>();
        static Type Find(string name)=>G4SpecDebugFixture.FindType(name);
        static MethodInfo Unique(Type type,string name,params Type[] signature)
        {var methods=type.GetMethods(All).Where(m=>m.Name==name&&m.GetParameters().Select(p=>p.ParameterType).SequenceEqual(signature)).ToArray();Assert.That(methods.Length,Is.EqualTo(1),type.FullName+"."+name+" exact signature");return methods[0];}
        static object Call(object o,string name,params object[] args)
        {
            Type[] signature;
            switch(name)
            {case "InitializeGraphLightModeInputs":case "RefreshGraphMainTexPropertyReferences":signature=Type.EmptyTypes;break;
             case "DrawGraphLightInputs":signature=new[]{Find("NBShaderEditor.ShaderGUIItem")};break;
             case "TryApplyGraphLightSubToggle":signature=new[]{typeof(string),typeof(bool?)};break;
             default:Assert.Fail("Unreviewed reflection signature "+name);return null;}
            var method=Unique(o.GetType(),name,signature);Assert.That(method.IsStatic,Is.False);
            try{return method.Invoke(o,args);}catch(TargetInvocationException e){ExceptionDispatchInfo.Capture(e.InnerException??e).Throw();throw;}
        }
        static object Field(object o,string name)
        {for(Type t=o.GetType();t!=null;t=t.BaseType){var field=t.GetField(name,All|BindingFlags.DeclaredOnly);if(field!=null)return field.GetValue(o);}Assert.Fail(name);return null;}
        static object Sync(object root)=>root.GetType().GetProperty("SyncService",All).GetValue(root);
        static IEnumerable<object> Children(object item)
        {yield return item;foreach(object child in (IEnumerable)Field(item,"ChildrenItemList"))foreach(var sub in Children(child))yield return sub;}
        static string[] Declared()=>(string[])Find("NBShaders2.Editor.FeatureLevel.NBShaderFeatureLevelMaterialApplier").GetField("GraphDeclaredKeywordNames",All).GetValue(null);
        sealed class Snapshot
        {
            object data;static Type Shared=>typeof(G4GraphGuiFeatureIntentTests).GetNestedType("Snapshot",BindingFlags.NonPublic);
            public static Snapshot Read(Material value)=>new Snapshot{data=Unique(Shared,"Read",typeof(Material)).Invoke(null,new object[]{value})};
            public void Same(Material value,string label,params string[] allowed)
            {var m=Unique(Shared,"AssertSame",typeof(Material),typeof(string),typeof(string[]));Assert.That(m,Is.Not.Null);try{m.Invoke(data,new object[]{value,label,allowed});}catch(TargetInvocationException e){ExceptionDispatchInfo.Capture(e.InnerException??e).Throw();throw;}}
        }
        [OneTimeSetUp]public void Guard()
        {Assert.That(Path.GetFullPath(Application.dataPath),Is.EqualTo(Path.GetFullPath(@"D:\UnityProject\NBUnityProject\.utmp\NBFXMeshValidation-20261002\Assets")).IgnoreCase);for(int i=0;i<SceneManager.sceneCount;++i){var s=SceneManager.GetSceneAt(i);Assert.That((s.name+"/"+s.path).IndexOf("TAI",StringComparison.OrdinalIgnoreCase),Is.LessThan(0));}}
        Material Material(int mode=1,int tier=3)
        {var shader=AssetDatabase.LoadAssetAtPath<Shader>(G4SpecDebugFixture.GraphPath);Assert.That(shader&&shader.isSupported,Is.True);var m=new Material(shader){hideFlags=HideFlags.HideAndDontSave};owned.Add(m);m.SetFloat("_NB_GraphGUIStateVersion",2);m.SetFloat("_NBShaderFeatureTier",tier);m.SetFloat("_FxLightMode",mode);m.SetFloat("_LightBigBlockItemFoldOut",1);m.SetFloat("_BlinnPhongSpecularToggle",0);m.SetFloat("_SixWayColorAbsorptionToggle",0);G4SpecDebugFixture.Validate(m);return m;}
        object Root(params Material[] values)
        {var h=new G4GraphPersistentGateTierTests();helpers.Add(h);return Unique(typeof(G4GraphPersistentGateTierTests),"Root",typeof(Material[])).Invoke(h,new object[]{values});}
        NBFXMainTexGUIEventHost Host(object root)
        {
            var host=ScriptableObject.CreateInstance<NBFXMainTexGUIEventHost>();owned.Add(host);host.hideFlags=HideFlags.HideAndDontSave;host.position=new Rect(20,20,650,700);host.SetupCommand="NBFX_Light_Sub_"+Guid.NewGuid().ToString("N");
            host.Setup=()=>Assert.That(Call(root,"InitializeGraphLightModeInputs"),Is.True);host.ShowUtility();host.SendEvent(new Event{type=EventType.ExecuteCommand,commandName=host.SetupCommand});if(host.Failure!=null)ExceptionDispatchInfo.Capture(host.Failure).Throw();Assert.That(host.Initialized,Is.True,"Actual setup command must reach original host");host.Draw=()=>Call(root,"DrawGraphLightInputs",(object)null);return host;
        }
        static void Send(NBFXMainTexGUIEventHost host,Event e)
        {var raw=e.rawType;host.Counts.TryGetValue(raw,out int before);host.SendEvent(e);if(host.Failure!=null)ExceptionDispatchInfo.Capture(host.Failure).Throw();Assert.That(e.rawType,Is.EqualTo(raw));Assert.That(host.Counts.TryGetValue(raw,out int after)&&after>before,Is.True,"Actual event rawType count must advance");}
        [TearDown]public void Cleanup()
        {foreach(var host in owned.OfType<NBFXMainTexGUIEventHost>()){host.Draw=null;host.Setup=null;host.Close();}foreach(var h in helpers)h.Cleanup();helpers.Clear();foreach(var o in owned.AsEnumerable().Reverse())if(o)Object.DestroyImmediate(o);owned.Clear();}
        static void KeywordPolicy(Material value,string keyword)
        {
            var type=Find("NBShaders2.Editor.FeatureLevel.NBShaderFeatureLevelMaterialApplier");object[] args={value,null};Assert.That((bool)Unique(type,"TryReadGraphSavedSupportedGateTier",typeof(Material),Find("NBShader.NBShaderMaterialIntentResult").MakeByRefType()).Invoke(null,args),Is.True);var effective=(string[])args[1].GetType().GetField("effectiveKeywords",All).GetValue(args[1]);Assert.That(value.IsKeywordEnabled(keyword),Is.EqualTo(effective.Contains(keyword)),"Actual saved-policy keyword authority");
        }
        static void OnlyLight(Material before,Material after,string rawName)
        {
            var expected=new Material(before){hideFlags=HideFlags.HideAndDontSave};
            try{foreach(string keyword in Declared()){if(after.IsKeywordEnabled(keyword))expected.EnableKeyword(keyword);else expected.DisableKeyword(keyword);}Snapshot.Read(expected).Same(after,"Light edit owns only selected raw toggle, existing Lighting gate and declared9; all other keyword/pass/flags preserved",rawName,"_NB_TierAllowLighting");}
            finally{Object.DestroyImmediate(expected);}
        }
        [Test]public void G4LightSub_CPU_MixedSavedTierAndFutureSecondAtomicReject()
        {
            var a=Material(1,0);var b=Material(1,3);var root=Root(a,b);var sa=Snapshot.Read(a);b.SetFloat("_NB_GraphGUIStateVersion",3);var sb=Snapshot.Read(b);Assert.That(Call(Sync(root),"TryApplyGraphLightSubToggle","_BlinnPhongSpecularToggle",true),Is.False);sa.Same(a,"Bad second marker rejects before first raw/keyword/gate write");sb.Same(b,"Unknown marker opaque");
            b.SetFloat("_NB_GraphGUIStateVersion",2);var ca=new Material(a);var cb=new Material(b);owned.Add(ca);owned.Add(cb);Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            try{Assert.That(Call(Sync(root),"TryApplyGraphLightSubToggle","_BlinnPhongSpecularToggle",true),Is.True);Assert.That(a.GetFloat("_BlinnPhongSpecularToggle"),Is.EqualTo(1));Assert.That(b.GetFloat("_BlinnPhongSpecularToggle"),Is.EqualTo(1));KeywordPolicy(a,"_SPECULAR_COLOR");KeywordPolicy(b,"_SPECULAR_COLOR");OnlyLight(ca,a,"_BlinnPhongSpecularToggle");OnlyLight(cb,b,"_BlinnPhongSpecularToggle");Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(group);var aa=Snapshot.Read(a);var ab=Snapshot.Read(b);Undo.PerformUndo();Snapshot.Read(ca).Same(a,"Mixed Low Undo");Snapshot.Read(cb).Same(b,"Mixed Ultra Undo");Undo.PerformRedo();aa.Same(a,"Mixed Low Redo");ab.Same(b,"Mixed Ultra Redo");}
            finally{Undo.RevertAllDownToGroup(group);}
        }
        [Test]public void G4LightSub_GUI_AllThreeOriginalSubtreesMixedPassiveReadOnly()
        {
            foreach(int mode in new[]{1,3,4})
            {
                var a=Material(mode);var b=Material(mode);a.SetFloat("_BlinnPhongSpecularToggle",3.25f);b.SetFloat("_BlinnPhongSpecularToggle",0);a.SetFloat("_SixWayColorAbsorptionToggle",2.25f);b.SetFloat("_SixWayColorAbsorptionToggle",0);
                a.SetVector("_MaterialInfo",new Vector4(2.25f,-1.75f,.375f,.875f));b.SetVector("_MaterialInfo",new Vector4(-3.25f,2.75f,.625f,.125f));a.SetVector("_SixWayInfo",new Vector4(2.25f,3.75f,.125f,.875f));b.SetVector("_SixWayInfo",new Vector4(-2.25f,-3.75f,.625f,.375f));a.SetFloat("_NB_Flags1Hi16",123.25f);b.SetFloat("_NB_Flags1Hi16",65536.25f);
                var root=Root(a,b);var host=Host(root);var sa=Snapshot.Read(a);var sb=Snapshot.Read(b);Send(host,new Event{type=EventType.Layout});Send(host,new Event{type=EventType.Repaint});sa.Same(a,"Actual shared original mode "+mode+" passive, including out-of-range vectors and noncanonical raw halves");sb.Same(b,"Mixed passive values are independent");host.Draw=null;host.Close();
            }
        }
        void ActualToggle(string property,int mode,string keyword)
        {
            var value=Material(mode);var root=Root(value);var host=Host(root);var control=Children(Field(root,"_graphLightModeBlock")).Single(o=>o.GetType().Name=="NBShaderKeywordToggleItem"&&(string)Field(o,"PropertyName")==property);host.Draw=()=>Call(root,"DrawGraphLightInputs",control);Send(host,new Event{type=EventType.Layout});Send(host,new Event{type=EventType.Repaint});var rect=(Rect)Field(control,"ControlRect");Assert.That(rect.width>0&&rect.height>0,Is.True);var before=new Material(value);owned.Add(before);Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            try{var p=new Vector2(rect.x+6,rect.center.y);Send(host,new Event{type=EventType.MouseDown,button=0,mousePosition=p});Send(host,new Event{type=EventType.MouseUp,button=0,mousePosition=p});Assert.That(value.GetFloat(property),Is.EqualTo(1));KeywordPolicy(value,keyword);OnlyLight(before,value,property);Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(group);var after=Snapshot.Read(value);host.Draw=null;Undo.PerformUndo();Snapshot.Read(before).Same(value,"Real original Toggle complete Undo");Undo.PerformRedo();after.Same(value,"Real original Toggle complete Redo");}
            finally{host.Draw=null;Undo.RevertAllDownToGroup(group);}
        }
        [Test]public void G4LightSub_GUI_ActualSpecularToggleCompleteUndoRedo()=>ActualToggle("_BlinnPhongSpecularToggle",1,"_SPECULAR_COLOR");
        [Test]public void G4LightSub_GUI_ActualAbsorptionToggleCompleteUndoRedo()=>ActualToggle("_SixWayColorAbsorptionToggle",4,"VFX_SIX_WAY_ABSORPTION");
        [Test]public void G4LightSub_GUI_ActualNoMipAndRampResetOwnedUndoRedo()
        {
            var value=Material(4);value.SetFloat("_NB_ForceNoMipFlagsLo16",128.25f);value.SetFloat("_NB_ForceNoMipFlagsHi16",99999.25f);var root=Root(value);var host=Host(root);var list=Children(Field(root,"_graphLightModeBlock")).ToArray();var force=list.Single(o=>o.GetType().Name=="ForceNoMipItem"&&(int)Field(o,"_forceNoMipFlagBits")==4);host.Draw=()=>Call(root,"DrawGraphLightInputs",force);Send(host,new Event{type=EventType.Layout});Send(host,new Event{type=EventType.Repaint});var rect=(Rect)Field(force,"ControlRect");var before=Snapshot.Read(value);Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            try{var p=new Vector2(rect.x+6,rect.center.y);Send(host,new Event{type=EventType.MouseDown,button=0,mousePosition=p});Send(host,new Event{type=EventType.MouseUp,button=0,mousePosition=p});Assert.That((int)value.GetFloat("_NB_ForceNoMipFlagsLo16"),Is.EqualTo(132));before.Same(value,"Root owns real NoMip event before generic packed writer","_NB_ForceNoMipFlagsLo16");Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(group);var after=Snapshot.Read(value);host.Draw=null;Undo.PerformUndo();before.Same(value,"NoMip untouched raw high-half and all state Undo");Undo.PerformRedo();after.Same(value,"NoMip complete Redo");}
            finally{host.Draw=null;Undo.RevertAllDownToGroup(group);}
            var map=new Texture2D(2,2){hideFlags=HideFlags.HideAndDontSave};owned.Add(map);value.SetTexture("_SixWayEmissionRamp",map);value.SetFloat("_NB_Flags1Hi16",8192+123.25f);Call(Sync(root),"RefreshGraphMainTexPropertyReferences");var ramp=list.Single(o=>o.GetType().Name=="TextureItem"&&(string)Field(o,"_texturePropertyName")=="_SixWayEmissionRamp");host.Draw=()=>Call(root,"DrawGraphLightInputs",ramp);Send(host,new Event{type=EventType.Layout});Send(host,new Event{type=EventType.Repaint});var texture=Field(Field(ramp,"_groupItem"),"_textureItem");var reset=(Rect)Field(texture,"ResetRect");Assert.That(reset.width>0&&reset.height>0,Is.True);before=Snapshot.Read(value);Undo.IncrementCurrentGroup();group=Undo.GetCurrentGroup();
            try{Send(host,new Event{type=EventType.MouseDown,button=0,mousePosition=reset.center});Send(host,new Event{type=EventType.MouseUp,button=0,mousePosition=reset.center});Assert.That(value.GetTexture("_SixWayEmissionRamp"),Is.Not.EqualTo(map));Assert.That(((int)value.GetFloat("_NB_Flags1Hi16")&8192)!=0,Is.EqualTo(value.GetTexture("_SixWayEmissionRamp")!=null));before.Same(value,"Real texture Reset changes only Ramp and original word1bit29","_SixWayEmissionRamp","_NB_Flags1Hi16");Assert.That(((int)value.GetFloat("_NB_Flags1Hi16")&8191),Is.EqualTo(123));Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(group);var after=Snapshot.Read(value);host.Draw=null;Undo.PerformUndo();before.Same(value,"Ramp reset and original raw bit Undo");Undo.PerformRedo();after.Same(value,"Ramp reset complete Redo");}
            finally{host.Draw=null;Undo.RevertAllDownToGroup(group);}
        }
    }
}
