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
    public sealed class G4GraphVATSharedTierTests
    {
        const BindingFlags All=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance;
        static readonly string[][] Modes={new[]{"_HOUDINI_VAT_SOFTBODY","_HOUDINI_VAT_RIGIDBODY","_HOUDINI_VAT_DYNAMIC_REMESH","_HOUDINI_VAT_PARTICLE_SPRITE"},new[]{"_TYFLOW_VAT_ABSOLUTE","_TYFLOW_VAT_RELATIVE","_TYFLOW_VAT_SKIN_R","_TYFLOW_VAT_SKIN_PR","_TYFLOW_VAT_SKIN_PRSAVE","_TYFLOW_VAT_SKIN_PRSXYZ"}};
        static readonly string[] VATProjection={"_NB_TierAllowVAT","_NB_TierAllowFlipbook","_NB_TierVATFamily","_NB_TierVATSubMode"};
        readonly List<Object> owned=new List<Object>();readonly List<G4GraphPersistentGateTierTests> helpers=new List<G4GraphPersistentGateTierTests>();
        static Type Find(string name)=>G4SpecDebugFixture.FindType(name);
        static object Tier()=>Enum.ToObject(Find("NBShader.NBShaderFeatureTier"),3);
        static string[] Raw()=>(string[])Find("NBShader.NBShaderFeatureCatalog").GetField("RawKeywords",All).GetValue(null);
        static string Family(int family)=>family==0?"_VAT_HOUDINI":"_VAT_TYFLOW";
        static Type Applier=>Find("NBShaders2.Editor.FeatureLevel.NBShaderFeatureLevelMaterialApplier");
        static object Call(object target,string name,params object[] args)
        {var method=target.GetType().GetMethod(name,All);Assert.That(method,Is.Not.Null,name);try{return method.Invoke(target,args);}catch(TargetInvocationException e){System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e.InnerException??e).Throw();throw;}}
        static object Field(object target,string name)
        {for(Type type=target.GetType();type!=null;type=type.BaseType){var field=type.GetField(name,All|BindingFlags.DeclaredOnly);if(field!=null)return field.GetValue(target);}Assert.Fail(name);return null;}
        static object Sync(object root)=>root.GetType().GetProperty("SyncService",All).GetValue(root);
        static string[] Union()=>(string[])Applier.GetField("GraphSupportedProjectionProperties",All).GetValue(null);
        sealed class Snapshot
        {
            readonly object value;static Type Shared=>typeof(G4GraphGuiFeatureIntentTests).GetNestedType("Snapshot",BindingFlags.NonPublic);
            Snapshot(object value){this.value=value;}public static Snapshot Read(Material material)=>new Snapshot(Shared.GetMethod("Read",All).Invoke(null,new object[]{material}));
            public void Same(Material material,string label,params string[] allow){var method=Shared.GetMethod("AssertSame",All);Assert.That(method,Is.Not.Null);method.Invoke(value,new object[]{material,label,allow});}
        }
        [OneTimeSetUp]public void Preflight()
        {Assert.That(Path.GetFullPath(Application.dataPath),Is.EqualTo(Path.GetFullPath(@"D:\UnityProject\NBUnityProject\.utmp\NBFXMeshValidation-20261002\Assets")).IgnoreCase);for(int i=0;i<SceneManager.sceneCount;++i){var scene=SceneManager.GetSceneAt(i);Assert.That((scene.name+"/"+scene.path).IndexOf("TAI",StringComparison.OrdinalIgnoreCase),Is.LessThan(0));}}
        Material Material()
        {var shader=AssetDatabase.LoadAssetAtPath<Shader>(G4SpecDebugFixture.GraphPath);Assert.That(shader&&shader.isSupported,Is.True);var value=new Material(shader){hideFlags=HideFlags.HideAndDontSave};owned.Add(value);value.SetFloat("_NB_GraphGUIStateVersion",2);value.SetFloat("_NBShaderFeatureTier",3);G4SpecDebugFixture.Validate(value);return value;}
        object Root(params Material[] materials)
        {var helper=new G4GraphPersistentGateTierTests();helpers.Add(helper);return typeof(G4GraphPersistentGateTierTests).GetMethod("Root",All).Invoke(helper,new object[]{materials});}
        static bool Project(Material material,string[] allowed)
        {object[] args={material,Tier(),allowed,false};return (bool)Applier.GetMethod("ApplyGraphVATProjectionGroup",All).Invoke(null,args);}
        static void Select(Material material,int family,int mode,bool both=true)
        {material.SetFloat("_VAT_Toggle",1);material.SetFloat("_VATMode",family);material.SetFloat(family==0?"_HoudiniVATSubMode":"_TyFlowVATSubMode",mode);material.SetFloat("_FlipbookBlending",both?1:0);}
        static void State(Material material,float parent,float family,float mode,float flipbook)
        {Assert.That(material.GetFloat(VATProjection[0]),Is.EqualTo(parent));Assert.That(material.GetFloat(VATProjection[1]),Is.EqualTo(flipbook));Assert.That(material.GetFloat(VATProjection[2]),Is.EqualTo(family));Assert.That(material.GetFloat(VATProjection[3]),Is.EqualTo(mode));}
        [TearDown]public void Cleanup()
        {foreach(var host in owned.OfType<NBFXMainTexGUIEventHost>()){host.Draw=null;host.Setup=null;host.Close();}foreach(var helper in helpers)helper.Cleanup();helpers.Clear();foreach(var obj in owned.AsEnumerable().Reverse())if(obj)Object.DestroyImmediate(obj);owned.Clear();}
        [Test]public void G4VATTier_CPU_AllTenSelectedModesAndNativeFallback0()
        {
            for(int family=0;family<2;++family)for(int mode=0;mode<Modes[family].Length;++mode)
            {
                var value=Material();Select(value,family,mode);var before=Snapshot.Read(value);Assert.That(Project(value,Raw()),Is.True);State(value,1,family,mode,0);before.Same(value,"All ten full-policy selectors preserve original raw mode/flipbook/surface/pass/flags",VATProjection);
                Assert.That(Project(value,Raw().Where(k=>k!=Modes[family][mode]).ToArray()),Is.True);State(value,1,family,0,0);before.Same(value,"Native retained-family/submode-denied implicit fallback0 preserves raw mode",VATProjection);
            }
        }
        [Test]public void G4VATTier_CPU_ParentFamilyDeniedKeepsIntendedFlipbookPriority()
        {
            for(int family=0;family<2;++family)
            {
                var value=Material();Select(value,family,1);var before=Snapshot.Read(value);
                Assert.That(Project(value,Raw().Where(k=>k!=Family(family)).ToArray()),Is.True);State(value,1,-2,0,0);
                Assert.That(Project(value,Raw().Where(k=>k!="_VAT").ToArray()),Is.True);State(value,0,-2,0,0);Assert.That(value.GetFloat("_VAT_Toggle"),Is.EqualTo(1));Assert.That(value.GetFloat("_FlipbookBlending"),Is.EqualTo(1));before.Same(value,"VAT intended priority happens before filtering; no re-enable of raw Flipbook on family/parent denial",VATProjection);
            }
        }
        [Test]public void G4VATTier_CPU_TypedPairsStrictFiniteAndCompleteNoop()
        {
            var value=Material();float[][] legal={new[]{-1f,-1f},new[]{-2f,0f},new[]{0f,3f},new[]{1f,5f}};float[][] illegal={new[]{-3f,0f},new[]{-1f,0f},new[]{0f,-1f},new[]{0f,4f},new[]{1f,6f},new[]{.5f,0f},new[]{1f,float.NaN},new[]{float.PositiveInfinity,0f}};
            foreach(var row in legal.Concat(illegal))
            {value.SetFloat("_NB_TierVATFamily",row[0]);value.SetFloat("_NB_TierVATSubMode",row[1]);object[] args={value,false};Assert.That((bool)Applier.GetMethod("HasGraphVATProjectionState",All).Invoke(null,args),Is.EqualTo(legal.Contains(row)),"typed pair "+row[0]+"/"+row[1]);}
            Select(value,1,1,false);value.SetFloat("_NB_TierVATFamily",-1);value.SetFloat("_NB_TierVATSubMode",-1);Assert.That(Project(value,Raw()),Is.True);var before=Snapshot.Read(value);Assert.That(Project(value,Raw()),Is.True);before.Same(value,"Same actual typed/boolean projection is a complete material no-op");
        }
        [Test]public void G4VATTier_CPU_UnprojectedSentinelInitializesOnceThenReadOnly()
        {
            var value=Material();Select(value,1,1);value.SetFloat("_NB_TierVATFamily",-1);value.SetFloat("_NB_TierVATSubMode",-1);
            // Existing marker2 material: preserve old manually saved gates, declared OVZ and unadopted screen passes.
            value.SetFloat("_NB_TierAllowFresnel",2.25f);value.EnableKeyword("_OVERRIDE_Z");value.SetFloat("_NB_GraphScreenPassMigrationComplete",0);value.SetShaderPassEnabled("SRPDefaultUnlit",false);value.SetShaderPassEnabled("NBDeferredDistortPass",false);value.SetShaderPassEnabled("NBCameraOpaqueDistortPass",true);
            var root=Root(value);var before=Snapshot.Read(value);Assert.That(Call(root,"InitializeGraphVATInputs"),Is.True);State(value,1,1,1,0);before.Same(value,"Old marker2 first typed capability owns only four NEW projection fields; all old manual gates/OVZ/raw0 passes/raw both-on intent remain",VATProjection);Assert.That(value.GetFloat("_NB_TierAllowFresnel"),Is.EqualTo(2.25f));Assert.That(value.IsKeywordEnabled("_OVERRIDE_Z"),Is.True);Assert.That(value.GetShaderPassEnabled("SRPDefaultUnlit"),Is.False);Assert.That(value.GetShaderPassEnabled("NBCameraOpaqueDistortPass"),Is.True);var after=Snapshot.Read(value);Assert.That(Call(root,"InitializeGraphVATInputs"),Is.True);after.Same(value,"Ready typed state avoids all second initialization writes");
        }
        [Test]public void G4VATTier_CPU_MixedMutualEditsFrameCDAndCompleteUndoRedo()
        {
            var a=Material();var b=Material();Select(a,0,0);Select(b,1,1);a.SetFloat("_NB_CustomDataFlag2Hi16",123.25f);b.SetFloat("_NB_CustomDataFlag2Hi16",456.25f);var root=Root(a,b);Assert.That(Call(root,"InitializeGraphVATInputs"),Is.True);var sa=Snapshot.Read(a);var sb=Snapshot.Read(b);int oldA=Mathf.RoundToInt(Mathf.Clamp(a.GetFloat("_NB_CustomDataFlag2Hi16"),0,65535)),oldB=Mathf.RoundToInt(Mathf.Clamp(b.GetFloat("_NB_CustomDataFlag2Hi16"),0,65535));Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            try
            {
                Assert.That(Call(Sync(root),"TryApplyGraphVATToggle",true),Is.True);Assert.That(a.GetFloat("_FlipbookBlending")+b.GetFloat("_FlipbookBlending"),Is.Zero);
                Assert.That(Call(Sync(root),"TryApplyGraphFlipbookEdit",true),Is.True);Assert.That(a.GetFloat("_VAT_Toggle")+b.GetFloat("_VAT_Toggle"),Is.Zero);Assert.That(a.GetFloat("_FlipbookBlending"),Is.EqualTo(1));
                var component=Enum.ToObject(Find("NBShader.NBShaderFlags").GetNestedType("CutomDataComponent",All),8);int bits=(int)Find("NBShader.NBShaderFlags").GetField("CustomData2WBit",All).GetValue(null);Assert.That(Call(Sync(root),"TryApplyGraphVATFrameCustomData",component),Is.True);Assert.That((int)a.GetFloat("_NB_CustomDataFlag2Hi16"),Is.EqualTo((oldA&4095)|(bits<<12)));Assert.That((int)b.GetFloat("_NB_CustomDataFlag2Hi16"),Is.EqualTo((oldB&4095)|(bits<<12)));
                string[] allow=Union().Concat(new[]{"_VAT_Toggle","_FlipbookBlending","_NB_CustomDataFlag2Hi16"}).ToArray();sa.Same(a,"Explicit mutually exclusive intent plus only exact FrameCD high nibble",allow);sb.Same(b,"Second selection preserves every unowned field/word/keyword/pass",allow);
                Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(group);var aa=Snapshot.Read(a);var ab=Snapshot.Read(b);Undo.PerformUndo();sa.Same(a,"All typed/scalar/raw CD first material Undo");sb.Same(b,"Complete second material Undo");Undo.PerformRedo();aa.Same(a,"First complete typed union Redo");ab.Same(b,"Second complete typed union Redo");
            }
            finally{Undo.RevertAllDownToGroup(group);}
        }
        [Test]public void G4VATTier_CPU_FutureSecondTypedStateRejectsBeforeFirstWrite()
        {
            var a=Material();var b=Material();var root=Root(a,b);Assert.That(Call(root,"InitializeGraphVATInputs"),Is.True);b.SetFloat("_NB_TierVATFamily",2);var sa=Snapshot.Read(a);var sb=Snapshot.Read(b);Assert.That(Call(Sync(root),"TryApplyGraphVATToggle",true),Is.False);sa.Same(a,"Future second typed enum must reject before first raw/derived write");sb.Same(b,"Unknown typed state is not normalized by guessing");
        }
        [Test]public void G4VATTier_CPU_ActualOwnedRollbackRestoresTypedAndRawBeforeImage()
        {
            var value=Material();Select(value,1,1,false);var root=Root(value);Assert.That(Call(root,"InitializeGraphVATInputs"),Is.True);value.SetFloat("_NB_CustomDataFlag2Hi16",123.25f);var declared=(string[])Applier.GetField("GraphDeclaredKeywordNames",All).GetValue(null);for(int i=0;i<declared.Length;++i){if((i&1)==0)value.EnableKeyword(declared[i]);else value.DisableKeyword(declared[i]);}var before=Snapshot.Read(value);var copy=new Material(value){hideFlags=HideFlags.HideAndDontSave};owned.Add(copy);
            foreach(string name in Union())value.SetFloat(name,name=="_NB_TierVATFamily"?0:name=="_NB_TierVATSubMode"?3:1-value.GetFloat(name));value.SetFloat("_VAT_Toggle",0);value.SetFloat("_FlipbookBlending",1);value.SetFloat("_NB_CustomDataFlag2Hi16",61440);value.SetFloat("_Frame",17);value.SetFloat("_VATMode",0);
            foreach(string keyword in declared){if(value.IsKeywordEnabled(keyword))value.DisableKeyword(keyword);else value.EnableKeyword(keyword);}
            Call(Sync(root),"RestoreGraphVATTransaction",new List<Material>{copy});before.Same(value,"Actual owner rollback routine restores typed union, every owned raw VAT field and exact FrameCD before-image");
        }
        static IEnumerable<object> Descendants(object item)
        {yield return item;foreach(object child in (System.Collections.IEnumerable)Field(item,"ChildrenItemList"))foreach(object sub in Descendants(child))yield return sub;}
        NBFXMainTexGUIEventHost Host(object root,out object actual)
        {var host=ScriptableObject.CreateInstance<NBFXMainTexGUIEventHost>();owned.Add(host);host.hideFlags=HideFlags.HideAndDontSave;host.position=new Rect(20,20,720,900);host.SetupCommand="NBFX_VATTier_"+Guid.NewGuid().ToString("N");object item=null;host.Setup=()=>{Assert.That(Call(root,"InitializeGraphVATInputs"),Is.True);item=Field(root,"_graphVATItem");};host.ShowUtility();host.SendEvent(new Event{type=EventType.ExecuteCommand,commandName=host.SetupCommand});Check(host);Assert.That(host.Initialized,Is.True);actual=item;host.Draw=()=>Call(root,"DrawGraphVATInputs",new object[]{null});return host;}
        static void Check(NBFXMainTexGUIEventHost host){if(host.Failure!=null)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(host.Failure).Throw();}
        static void Send(NBFXMainTexGUIEventHost host,Event evt)
        {var type=evt.rawType;host.Counts.TryGetValue(type,out int before);host.SendEvent(evt);Check(host);Assert.That(evt.rawType,Is.EqualTo(type));Assert.That(host.Counts.TryGetValue(type,out int after)&&after>before,Is.True);}
        [Test]public void G4VATTier_GUI_HiddenFrameCDMixedChildBooleansPassiveReadOnly()
        {
            var a=Material();var b=Material();foreach(var value in new[]{a,b}){Select(value,0,0,false);value.SetFloat("_VATBlockFoldOut",1);value.SetFloat("_NB_Flags1Lo16",0);}a.SetFloat("_B_autoPlayback",3.25f);b.SetFloat("_B_autoPlayback",0);a.SetFloat("_NB_CustomDataFlag2Hi16",11*4096+.25f);b.SetFloat("_NB_CustomDataFlag2Hi16",8*4096+.25f);var root=Root(a,b);var host=Host(root,out var item);var sa=Snapshot.Read(a);var sb=Snapshot.Read(b);Send(host,new Event{type=EventType.Layout});Send(host,new Event{type=EventType.Repaint});sa.Same(a,"Actual whole original Houdini subtree: hidden FrameCD must not clear and passive Boolean must not canonicalize");sb.Same(b,"Mixed selection remains exact");Assert.That(Descendants(item).Count(x=>x.GetType().Name=="VatFrameCustomDataItem"),Is.EqualTo(2));
        }
        [Test]public void G4VATTier_GUI_ActualVATEnableExcludesFlipbookCompleteUndoRedo()
        {
            var value=Material();value.SetFloat("_FlipbookBlending",1);value.SetFloat("_VATBlockFoldOut",1);G4SpecDebugFixture.Validate(value);var root=Root(value);var host=Host(root,out var item);Send(host,new Event{type=EventType.Layout});Send(host,new Event{type=EventType.Repaint});var rect=(Rect)Field(item,"ControlRect");Assert.That(rect.width>0&&rect.height>0,Is.True);var before=Snapshot.Read(value);Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            try
            {var point=new Vector2(rect.x+6,rect.center.y);Send(host,new Event{type=EventType.MouseDown,button=0,mousePosition=point});Send(host,new Event{type=EventType.MouseUp,button=0,mousePosition=point});Assert.That(value.GetFloat("_VAT_Toggle"),Is.EqualTo(1));Assert.That(value.GetFloat("_FlipbookBlending"),Is.Zero);before.Same(value,"Real original header action owns mutual raw toggles and registered projection only",Union().Concat(new[]{"_VAT_Toggle","_FlipbookBlending"}).ToArray());Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(group);var after=Snapshot.Read(value);host.Draw=null;Undo.PerformUndo();before.Same(value,"Real header complete typed/raw Undo");Undo.PerformRedo();after.Same(value,"Actual original control complete Redo");}
            finally{host.Draw=null;Undo.RevertAllDownToGroup(group);}
        }
        [Test]public void G4VATTier_GUI_TyflowMeshFrameCDVisibleActualResetUndoRedo()
        {
            var value=Material();Select(value,1,1,false);value.SetFloat("_VATBlockFoldOut",1);value.SetFloat("_NB_Flags1Lo16",0);value.SetFloat("_NB_CustomDataFlag2Hi16",11*4096+123.25f);var root=Root(value);var host=Host(root,out var item);var controls=Descendants(item).Where(x=>x.GetType().Name=="VatFrameCustomDataItem").ToArray();var frame=controls.Single(x=>((Func<bool>)Field(x,"_isVisible"))());host.Draw=()=>Call(root,"DrawGraphVATInputs",frame);Send(host,new Event{type=EventType.Layout});Send(host,new Event{type=EventType.Repaint});var reset=(Rect)Field(frame,"ResetRect");Assert.That(reset.width>0&&reset.height>0,Is.True);var before=Snapshot.Read(value);Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            try
            {Send(host,new Event{type=EventType.MouseDown,button=0,mousePosition=reset.center});Send(host,new Event{type=EventType.MouseUp,button=0,mousePosition=reset.center});Assert.That((int)value.GetFloat("_NB_CustomDataFlag2Hi16")&61440,Is.Zero);before.Same(value,"Real non-popup FrameCD Reset owns only original nibble28", "_NB_CustomDataFlag2Hi16");Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(group);var after=Snapshot.Read(value);host.Draw=null;Undo.PerformUndo();before.Same(value,"Visible Mesh Tyflow CD reset complete Undo");Undo.PerformRedo();after.Same(value,"Actual Reset Redo");}
            finally{host.Draw=null;Undo.RevertAllDownToGroup(group);}
        }
    }
}
