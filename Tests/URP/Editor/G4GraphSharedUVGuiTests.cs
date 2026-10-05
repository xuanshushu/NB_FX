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
    // Existing original SharedUV leaf and the original event host; no native Popup event.
    public sealed class G4GraphSharedUVGuiTests
    {
        const BindingFlags All=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
        readonly List<Object> owned=new List<Object>();readonly List<G4GraphPersistentGateTierTests> helpers=new List<G4GraphPersistentGateTierTests>();
        static Type Find(string name)=>G4SpecDebugFixture.FindType(name);
        static object Call(object target,string name,params object[] args)
        {var method=target.GetType().GetMethod(name,All);Assert.That(method,Is.Not.Null,name);try{return method.Invoke(target,args);}catch(TargetInvocationException e){System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e.InnerException??e).Throw();throw;}}
        static object Field(object target,string name)
        {for(Type t=target.GetType();t!=null;t=t.BaseType){var field=t.GetField(name,All|BindingFlags.DeclaredOnly);if(field!=null)return field.GetValue(target);}Assert.Fail(name);return null;}
        static object Sync(object root)=>root.GetType().GetProperty("SyncService",All).GetValue(root);
        static object UV(int value)=>Enum.ToObject(Find("NBShader.NBShaderFlags").GetNestedType("UVMode",All),value);
        static object CD(int value)=>Enum.ToObject(Find("NBShader.NBShaderFlags").GetNestedType("CutomDataComponent",All),value);
        static int Half(Material m,string name)=>Mathf.RoundToInt(Mathf.Clamp(m.GetFloat(name),0,65535));
        sealed class Snapshot
        {
            readonly object value;static Type Shared=>typeof(G4GraphGuiFeatureIntentTests).GetNestedType("Snapshot",BindingFlags.NonPublic);
            Snapshot(object value){this.value=value;}public static Snapshot Read(Material m)=>new Snapshot(Shared.GetMethod("Read",All).Invoke(null,new object[]{m}));
            public void Same(Material m,string label,params string[] allow){var method=Shared.GetMethod("AssertSame",All);Assert.That(method,Is.Not.Null);method.Invoke(value,new object[]{m,label,allow});}
        }
        [OneTimeSetUp]public void Preflight()
        {
            Assert.That(Path.GetFullPath(Application.dataPath),Is.EqualTo(Path.GetFullPath(@"D:\UnityProject\NBUnityProject\.utmp\NBFXMeshValidation-20261002\Assets")).IgnoreCase);
            for(int i=0;i<SceneManager.sceneCount;++i){var scene=SceneManager.GetSceneAt(i);Assert.That((scene.name+"/"+scene.path).IndexOf("TAI",StringComparison.OrdinalIgnoreCase),Is.LessThan(0));}
        }
        Material Material()
        {var shader=AssetDatabase.LoadAssetAtPath<Shader>(G4SpecDebugFixture.GraphPath);Assert.That(shader&&shader.isSupported,Is.True);var m=new Material(shader){hideFlags=HideFlags.HideAndDontSave};owned.Add(m);m.SetFloat("_NB_GraphGUIStateVersion",2);m.SetFloat("_NBShaderFeatureTier",3);G4SpecDebugFixture.Validate(m);return m;}
        object Root(params Material[] materials)
        {var helper=new G4GraphPersistentGateTierTests();helpers.Add(helper);return typeof(G4GraphPersistentGateTierTests).GetMethod("Root",All).Invoke(helper,new object[]{materials});}
        [TearDown]public void Cleanup()
        {foreach(var h in owned.OfType<NBFXMainTexGUIEventHost>()){h.Draw=null;h.Setup=null;h.Close();}foreach(var helper in helpers)helper.Cleanup();helpers.Clear();foreach(var o in owned.AsEnumerable().Reverse())if(o)Object.DestroyImmediate(o);owned.Clear();}
        [Test]public void G4SharedUV_CPU_ActualSchemaOriginalLeafReadOnlyNoNewGate()
        {
            var m=Material();var root=Root(m);var before=Snapshot.Read(m);
            Assert.That((IEnumerable<string>)Call(root,"GetSharedGraphPropertyNames"),Does.Not.Contain("_SharedUV_ST"));
            foreach(string name in new[]{"_SharedUVToggle","_SharedUVBlockFoldOut","_SharedUVModeFoldOut"})
            {int index=m.shader.FindPropertyIndex(name);Assert.That(index,Is.GreaterThanOrEqualTo(0));Assert.That(m.shader.GetPropertyType(index),Is.EqualTo(UnityEngine.Rendering.ShaderPropertyType.Float));Assert.That(m.shader.GetPropertyDefaultFloatValue(index),Is.Zero);}
            Assert.That(Call(root,"InitializeGraphSharedUVInputs"),Is.True);before.Same(m,"Original factory construction/schema readiness are read-only");
            Assert.That(Field(root,"_graphSharedUVItem").GetType(),Is.EqualTo(Find("NBShaderEditor.SharedUVFeatureItem")));
            foreach(string name in new[]{"_SharedUVToggle","_SharedUVBlockFoldOut","_SharedUVModeFoldOut","_SharedUV_ST","_SharedUV_Vec"})Assert.That((IEnumerable<string>)Call(root,"GetSharedGraphPropertyNames"),Does.Contain(name));
            var applier=Find("NBShaders2.Editor.FeatureLevel.NBShaderFeatureLevelMaterialApplier");Assert.That((string[])applier.GetField("GraphSupportedGateProperties",All).GetValue(null),Does.Not.Contain("_NB_TierAllowSharedUV"));
            Assert.That((int)Find("NBShader.NBShaderFlags").GetField("FLAG_BIT_UVMODE_POS_0_SHAREDUV",All).GetValue(null),Is.EqualTo(30));
        }
        [Test]public void G4SharedUV_CPU_UV30AllNineModesExactOwnedBitsUndoRedo()
        {
            var m=Material();m.SetFloat("_NB_UVModeFlag0Hi16",1330.25f);m.SetFloat("_NB_UVModeFlagType0Hi16",909.25f);var root=Root(m);Assert.That(Call(root,"InitializeGraphSharedUVInputs"),Is.True);var before=Snapshot.Read(m);int oldBits=Half(m,"_NB_UVModeFlag0Hi16"),oldTypes=Half(m,"_NB_UVModeFlagType0Hi16"),oldDerived=Half(m,"_NB_Flags1Hi16");Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            try
            {
                for(int mode=0;mode<9;++mode)
                {
                    Assert.That(Call(Sync(root),"TryApplyGraphSharedFeatureUVMode",30,UV(mode),"_SharedUVModeFoldOut",true),Is.True);
                    Assert.That(Half(m,"_NB_UVModeFlag0Hi16"),Is.EqualTo((oldBits&16383)|((mode&3)<<14)));Assert.That(Half(m,"_NB_UVModeFlagType0Hi16"),Is.EqualTo((oldTypes&16383)|((mode/4)<<14)));
                    Assert.That(Half(m,"_NB_Flags1Hi16")&~28,Is.EqualTo(oldDerived&~28));
                    before.Same(m,"Only shared UV30/high14-15 + same existing UV-derived bits + its fold may change","_NB_UVModeFlag0Hi16","_NB_UVModeFlagType0Hi16","_NB_Flags1Hi16","_SharedUVModeFoldOut");
                }
                Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(group);var after=Snapshot.Read(m);Undo.PerformUndo();before.Same(m,"Actual UV transaction full raw Undo");Undo.PerformRedo();after.Same(m,"Actual UV transaction full raw Redo");
            }
            finally{Undo.RevertAllDownToGroup(group);}
        }
        [Test]public void G4SharedUV_CPU_CDWord3XYAllComponentsMultiselectUndoRedo()
        {
            var a=Material();var b=Material();a.SetFloat("_NB_CustomDataFlag3Hi16",65536.25f);b.SetFloat("_NB_CustomDataFlag3Hi16",321.75f);a.SetFloat("_NB_CustomDataFlag3Lo16",0x21);b.SetFloat("_NB_CustomDataFlag3Lo16",0x43);var root=Root(a,b);Assert.That(Call(root,"InitializeGraphSharedUVInputs"),Is.True);var sa=Snapshot.Read(a);var sb=Snapshot.Read(b);Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            try
            {
                string[] fields={"CustomData1XBit","CustomData1YBit","CustomData1ZBit","CustomData1WBit","CustomData2XBit","CustomData2YBit","CustomData2ZBit","CustomData2WBit"};int[] values=new[]{0}.Concat(fields.Select(n=>(int)Find("NBShader.NBShaderFlags").GetField(n,All).GetValue(null))).ToArray();
                for(int component=0;component<9;++component)foreach(int position in new[]{8,12})
                {
                    int oldA=Half(a,"_NB_CustomDataFlag3Lo16"),oldB=Half(b,"_NB_CustomDataFlag3Lo16");int bits=15<<position;
                    Assert.That(Call(Sync(root),"TryApplyGraphSharedUVCustomData",position,3,CD(component)),Is.True);
                    Assert.That(Half(a,"_NB_CustomDataFlag3Lo16"),Is.EqualTo((oldA&~bits)|(values[component]<<position)));Assert.That(Half(b,"_NB_CustomDataFlag3Lo16"),Is.EqualTo((oldB&~bits)|(values[component]<<position)));
                    sa.Same(a,"CDword3 only original X/Y two low-half nibbles; high raw untouched","_NB_CustomDataFlag3Lo16");sb.Same(b,"Same original component mapping on second selection","_NB_CustomDataFlag3Lo16");
                }
                Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(group);var aa=Snapshot.Read(a);var ab=Snapshot.Read(b);Undo.PerformUndo();sa.Same(a,"First complete CD Undo");sb.Same(b,"Second complete CD Undo");Undo.PerformRedo();aa.Same(a,"First complete CD Redo");ab.Same(b,"Second complete CD Redo");
            }
            finally{Undo.RevertAllDownToGroup(group);}
        }
        [Test]public void G4SharedUV_CPU_FutureSchemaAndWrongSlotAtomicReject()
        {
            var a=Material();var b=Material();var root=Root(a,b);Assert.That(Call(root,"InitializeGraphSharedUVInputs"),Is.True);b.SetFloat("_NB_GraphGUIStateVersion",3);var sa=Snapshot.Read(a);var sb=Snapshot.Read(b);
            Assert.That(Call(Sync(root),"TryApplyGraphSharedUVMode",UV(1),true),Is.False);Assert.That(Call(Sync(root),"TryApplyGraphSharedUVCustomData",8,3,CD(1)),Is.False);sa.Same(a,"Future second marker rejects before first write");sb.Same(b,"Unknown schema preserved");
            b.SetFloat("_NB_GraphGUIStateVersion",2);sa=Snapshot.Read(a);sb=Snapshot.Read(b);Assert.That(Call(Sync(root),"TryApplyGraphSharedUVCustomData",16,3,CD(1)),Is.False);sa.Same(a,"Overlay nibble is not owned by SharedUV");sb.Same(b,"Wrong slot leaves selection untouched");
        }
        NBFXMainTexGUIEventHost Host(object root)
        {
            var host=ScriptableObject.CreateInstance<NBFXMainTexGUIEventHost>();owned.Add(host);host.hideFlags=HideFlags.HideAndDontSave;host.position=new Rect(20,20,640,840);host.SetupCommand="NBFX_SharedUV_Setup_"+Guid.NewGuid().ToString("N");host.Setup=()=>Assert.That(Call(root,"InitializeGraphSharedUVInputs"),Is.True);host.ShowUtility();host.SendEvent(new Event{type=EventType.ExecuteCommand,commandName=host.SetupCommand});Check(host);Assert.That(host.Initialized,Is.True);host.Draw=()=>Call(root,"DrawGraphSharedUVInputs",new object[]{null});return host;
        }
        static void Check(NBFXMainTexGUIEventHost host){if(host.Failure!=null)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(host.Failure).Throw();}
        static void Send(NBFXMainTexGUIEventHost host,Event evt)
        {var type=evt.rawType;host.Counts.TryGetValue(type,out int before);host.SendEvent(evt);Check(host);Assert.That(evt.rawType,Is.EqualTo(type));Assert.That(host.Counts.TryGetValue(type,out int after)&&after>before,Is.True);}
        [Test]public void G4SharedUV_GUI_WholeLeafMixedPassiveReadOnly()
        {
            var a=Material();var b=Material();a.SetFloat("_SharedUVBlockFoldOut",1);b.SetFloat("_SharedUVBlockFoldOut",1);a.SetFloat("_SharedUVToggle",1);b.SetFloat("_SharedUVToggle",0);a.SetVector("_SharedUV_ST",new Vector4(2,3,4,5));b.SetVector("_SharedUV_ST",new Vector4(7,8,9,10));a.SetVector("_SharedUV_Vec",new Vector4(-2,3,999,-999));b.SetVector("_SharedUV_Vec",new Vector4(2,-3,-999,999));var root=Root(a,b);var host=Host(root);var sa=Snapshot.Read(a);var sb=Snapshot.Read(b);Send(host,new Event{type=EventType.Layout});Send(host,new Event{type=EventType.Repaint});sa.Same(a,"Actual original whole SharedUV leaf passive mixed paint does not assimilate values");sb.Same(b,"Second mixed material stays exact");
        }
        [Test]public void G4SharedUV_GUI_ActualOriginalToggleCompleteUndoRedo()
        {
            var m=Material();m.SetFloat("_SharedUVBlockFoldOut",1);var root=Root(m);var host=Host(root);Send(host,new Event{type=EventType.Layout});Send(host,new Event{type=EventType.Repaint});var item=Field(root,"_graphSharedUVItem");var rect=(Rect)Field(item,"ControlRect");Assert.That(rect.width>0&&rect.height>0,Is.True);var click=new Vector2(rect.x+6,rect.center.y);var before=Snapshot.Read(m);Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            try
            {
                Send(host,new Event{type=EventType.MouseDown,button=0,mousePosition=click});Send(host,new Event{type=EventType.MouseUp,button=0,mousePosition=click});Assert.That(m.GetFloat("_SharedUVToggle"),Is.EqualTo(1));before.Same(m,"Original no-op runtime SharedUV header only edits saved UI toggle","_SharedUVToggle");
                Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(group);var after=Snapshot.Read(m);host.Draw=null;Undo.PerformUndo();before.Same(m,"Actual Root pre-event transaction full Undo");Undo.PerformRedo();after.Same(m,"Actual original toggle complete Redo");
            }
            finally{host.Draw=null;Undo.RevertAllDownToGroup(group);}
        }
    }
}
