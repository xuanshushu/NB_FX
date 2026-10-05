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
    public sealed class G4GraphRemainingModeTAToolbarTests
    {
        const BindingFlags All=BindingFlags.Static|BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        readonly List<Object> owned=new List<Object>();readonly List<G4GraphPersistentGateTierTests> helpers=new List<G4GraphPersistentGateTierTests>();
        static Type Find(string n)=>G4SpecDebugFixture.FindType(n);
        static MethodInfo Unique(Type t,string n,params Type[] sig){var m=t.GetMethods(All).Where(x=>x.Name==n&&x.GetParameters().Select(p=>p.ParameterType).SequenceEqual(sig)).ToArray();Assert.That(m.Length,Is.EqualTo(1),t.FullName+"."+n);return m[0];}
        static object Invoke(MethodInfo m,object target,params object[] args){try{return m.Invoke(target,args);}catch(TargetInvocationException e){ExceptionDispatchInfo.Capture(e.InnerException??e).Throw();throw;}}
        static object Call(object target,string n,params object[] args)
        {Type[] sig;switch(n){case "InitializeGraphRemainingSharedUI":case "CanPasteGraph":case "TryPasteGraph":case "CollapseAll":case "CleanUnusedTextures":sig=Type.EmptyTypes;break;case "DrawGraphRemainingSharedUI":sig=new[]{Find("NBShaderEditor.ShaderGUIItem")};break;case "DrawGraphTierSelector":sig=Type.EmptyTypes;break;case "CanUseGraphSharedToolbar":sig=new[]{typeof(bool)};break;case "TryWriteGraphAdditiveBlend":sig=new[]{typeof(float),typeof(bool)};break;case "TryRunGraphKnownToolbarEdit":sig=new[]{typeof(Action)};break;default:Assert.Fail(n);return null;}return Invoke(Unique(target.GetType(),n,sig),target,args);}
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
        Material New(){var shader=AssetDatabase.LoadAssetAtPath<Shader>(G4SpecDebugFixture.GraphPath);Assert.That(shader&&shader.isSupported,Is.True);var m=new Material(shader){hideFlags=HideFlags.HideAndDontSave};owned.Add(m);m.SetFloat("_NB_GraphGUIStateVersion",2);m.SetFloat("_NBShaderFeatureTier",3);m.SetFloat("_Surface",1);m.SetFloat("_Blend",0);G4SpecDebugFixture.Validate(m);m.SetFloat("_Blend",2);m.SetFloat("_BigBlockModeSettingFoldOut",1);m.SetFloat("_ShaderKeywordFoldOut",1);m.SetFloat("_NB_Flags1Lo16",1);m.SetFloat("_BaseOptionBigBlockItemFoldOut",1);return m;}
        object Root(params Material[] materials){var helper=new G4GraphPersistentGateTierTests();helpers.Add(helper);return Invoke(Unique(typeof(G4GraphPersistentGateTierTests),"Root",typeof(Material[])),helper,(object)materials);}
        Material originalClipboard;Shader originalClipboardShader;
        [SetUp]public void PreserveOriginalClipboard()
        {
            var type=Find("NBShaderEditor.NBShaderGUIToolBar");originalClipboard=(Material)type.GetField("copiedMaterialSnapshot",All).GetValue(null);originalClipboardShader=(Shader)type.GetField("copiedShader",All).GetValue(null);
            type.GetField("copiedMaterialSnapshot",All).SetValue(null,null);type.GetField("copiedShader",All).SetValue(null,null);
        }
        [TearDown]public void Cleanup(){var type=Find("NBShaderEditor.NBShaderGUIToolBar");var temporary=(Material)type.GetField("copiedMaterialSnapshot",All).GetValue(null);if(temporary&&temporary!=originalClipboard)Object.DestroyImmediate(temporary);type.GetField("copiedMaterialSnapshot",All).SetValue(null,originalClipboard);type.GetField("copiedShader",All).SetValue(null,originalClipboardShader);foreach(var h in owned.OfType<NBFXMainTexGUIEventHost>()){h.Draw=null;h.Setup=null;h.Close();}foreach(var x in helpers)x.Cleanup();helpers.Clear();foreach(var x in owned.AsEnumerable().Reverse())if(x)Object.DestroyImmediate(x);owned.Clear();}
        static object Toolbar(object root)=>Activator.CreateInstance(Find("NBShaderEditor.NBShaderGUIToolBar"),new object[]{root});
        static void Copy(Material m)=>Invoke(Unique(Find("NBShaderEditor.NBShaderGUIToolBar"),"CopyMaterial",typeof(Material)),null,m);
        NBFXMainTexGUIEventHost Host(object root,bool all=false)
        {
            var h=ScriptableObject.CreateInstance<NBFXMainTexGUIEventHost>();owned.Add(h);h.hideFlags=HideFlags.HideAndDontSave;h.position=new Rect(20,20,680,700);h.SetupCommand="NBFX_RemainingGUI_"+Guid.NewGuid().ToString("N");h.Setup=()=>Assert.That(Call(root,"InitializeGraphRemainingSharedUI"),Is.True);h.ShowUtility();h.SendEvent(new Event{type=EventType.ExecuteCommand,commandName=h.SetupCommand});Check(h);Assert.That(h.Initialized,Is.True);var toolbar=Toolbar(root);h.Draw=()=>{Call(toolbar,"DrawGraphTierSelector");Call(root,"DrawGraphRemainingSharedUI",(object)null);};return h;
        }
        static void Check(NBFXMainTexGUIEventHost h){if(h.Failure!=null)ExceptionDispatchInfo.Capture(h.Failure).Throw();}
        static void Send(NBFXMainTexGUIEventHost h,Event e){var raw=e.rawType;h.Counts.TryGetValue(raw,out int before);h.SendEvent(e);Check(h);Assert.That(e.rawType,Is.EqualTo(raw));Assert.That(h.Counts.TryGetValue(raw,out int after)&&after>before,Is.True);}
        [Test]public void G4RemainingGUI_ModeTAAllToolbarPassiveAndLiveKeywordReadOnly()
        {
            var m=New();m.SetFloat("_AdditiveToPreMultiplyAlphaLerp",2.25f);m.SetFloat("_NB_TierAllowMask",.25f);var root=Root(m);var h=Host(root);var before=Snapshot.Read(m);Send(h,new Event{type=EventType.Layout});Send(h,new Event{type=EventType.Repaint});before.Same(m,"Original Mode/TA/full Toolbar paint leaves out-of-range scalar, raw and manual projections");
            var keywords=Children(Field(root,"_graphKeywordListBlock")).Single(x=>x.GetType().Name=="KeywordListItem");m.EnableKeyword("NB_DEBUG_MASK");var changed=Snapshot.Read(m);var actual=(string[])Invoke(Unique(keywords.GetType(),"GetCachedKeywords",typeof(Material)),keywords,m);Assert.That(actual,Does.Contain("NB_DEBUG_MASK"));Send(h,new Event{type=EventType.Layout});Send(h,new Event{type=EventType.Repaint});changed.Same(m,"Keywords list reads external actual shaderKeywords without writes");m.DisableKeyword("NB_DEBUG_MASK");actual=(string[])Invoke(Unique(keywords.GetType(),"GetCachedKeywords",typeof(Material)),keywords,m);Assert.That(actual,Does.Not.Contain("NB_DEBUG_MASK"));
        }
        [Test]public void G4RemainingGUI_OriginalAdditiveSliderActualEditResetCompleteUndoRedo()
        {
            var m=New();m.SetFloat("_AdditiveToPreMultiplyAlphaLerp",.875f);var root=Root(m);var h=Host(root);var slider=Children(Field(root,"_graphBlendModeBlock")).Single(x=>x.GetType().Name=="AddToPreMultiplySlider");h.Draw=()=>Call(root,"DrawGraphRemainingSharedUI",slider);Send(h,new Event{type=EventType.Layout});Send(h,new Event{type=EventType.Repaint});var rect=(Rect)Field(slider,"ControlRect");Assert.That(rect.width>90&&rect.height>0,Is.True);var before=Snapshot.Read(m);Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            try{Send(h,new Event{type=EventType.MouseDown,button=0,mousePosition=rect.center});Send(h,new Event{type=EventType.MouseUp,button=0,mousePosition=rect.center});Assert.That(Mathf.Abs(m.GetFloat("_AdditiveToPreMultiplyAlphaLerp")-.875f),Is.GreaterThan(.01f));before.Same(m,"Real original Slider writes only NB scalar","_AdditiveToPreMultiplyAlphaLerp");Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(group);var after=Snapshot.Read(m);h.Draw=null;Undo.PerformUndo();before.Same(m,"Scalar complete Undo");Undo.PerformRedo();after.Same(m,"Scalar complete Redo");h.Draw=()=>Call(root,"DrawGraphRemainingSharedUI",slider);Send(h,new Event{type=EventType.Layout});Send(h,new Event{type=EventType.Repaint});var reset=(Rect)Field(slider,"ResetRect");Send(h,new Event{type=EventType.MouseDown,button=0,mousePosition=reset.center});Send(h,new Event{type=EventType.MouseUp,button=0,mousePosition=reset.center});Assert.That(m.GetFloat("_AdditiveToPreMultiplyAlphaLerp"),Is.Zero,"Original Additive mode Reset preset0");}finally{h.Draw=null;Undo.RevertAllDownToGroup(group);}
        }
        [Test]public void G4RemainingGUI_OfficialBlendExplicitPresetMixedUnknownLastRefusal()
        {
            var a=New();var b=New();a.SetFloat("_AdditiveToPreMultiplyAlphaLerp",.25f);b.SetFloat("_AdditiveToPreMultiplyAlphaLerp",.75f);a.SetFloat("_Blend",1);b.SetFloat("_Blend",2);var before=new Dictionary<Material,float>{{a,2},{b,1}};var sa=Snapshot.Read(a);var sb=Snapshot.Read(b);var method=Unique(Find("NBShaderEditor.NBShaderSyncService"),"TryApplyGraphBlendPresetForOfficialEdit",typeof(IList<Material>),typeof(IDictionary<Material,float>),typeof(bool).MakeByRefType());object[] args={new[]{a,b},before,false};Assert.That((bool)Invoke(method,null,args),Is.True);Assert.That(args[2],Is.True);Assert.That(a.GetFloat("_AdditiveToPreMultiplyAlphaLerp"),Is.EqualTo(1));Assert.That(b.GetFloat("_AdditiveToPreMultiplyAlphaLerp"),Is.Zero);sa.Same(a,"Official explicit Premultiply updates only NB scalar","_AdditiveToPreMultiplyAlphaLerp");sb.Same(b,"Official explicit Additive updates only NB scalar","_AdditiveToPreMultiplyAlphaLerp");a.SetFloat("_AdditiveToPreMultiplyAlphaLerp",.25f);b.SetFloat("_NB_GraphGUIStateVersion",3);sa=Snapshot.Read(a);sb=Snapshot.Read(b);args=new object[]{new[]{a,b},before,false};Assert.That((bool)Invoke(method,null,args),Is.False);Assert.That(args[2],Is.False);sa.Same(a,"Future last refuses first NB preset");sb.Same(b,"Future raw preserved; official Blend permission separate");
        }
        [Test]public void G4RemainingGUI_SameShaderClipboardCompleteSnapshotUndoRejectFutureMixed()
        {
            var src=New();var dst=New();src.SetFloat("_AlphaAll",.375f);src.SetFloat("_AdditiveToPreMultiplyAlphaLerp",.625f);src.SetFloat("_NB_TierAllowMask",.25f);src.SetTexture("_MatCapTex",Texture2D.whiteTexture);src.SetShaderPassEnabled("SRPDefaultUnlit",false);src.SetFloat("_NB_GraphScreenPassMigrationComplete",0);Copy(src);var root=Root(dst);Assert.That(Call(root,"InitializeGraphRemainingSharedUI"),Is.True);var toolbar=Toolbar(root);var before=Snapshot.Read(dst);var expected=Snapshot.Read(src);Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            try{Assert.That(Call(toolbar,"CanPasteGraph"),Is.True);Assert.That(Call(toolbar,"TryPasteGraph"),Is.True);Assert.That(dst.shader,Is.EqualTo(src.shader));expected.Same(dst,"Same existing clipboard copies complete raw/manual state without shader switch or Tier reproject");Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(group);var after=Snapshot.Read(dst);Undo.PerformUndo();before.Same(dst,"Paste full Undo");Undo.PerformRedo();after.Same(dst,"Paste full Redo");src.SetFloat("_NB_GraphGUIStateVersion",3);Copy(src);var stable=Snapshot.Read(dst);Assert.That(Call(toolbar,"CanPasteGraph"),Is.False);Assert.That(Call(toolbar,"TryPasteGraph"),Is.False);stable.Same(dst,"Future clipboard rejects before write");Copy(dst);var multi=Root(src,dst);Assert.That(Call(Toolbar(multi),"CanPasteGraph"),Is.False,"Mixed/future and multi clipboard disabled");}finally{Undo.RevertAllDownToGroup(group);}
        }
        [Test]public void G4RemainingGUI_ActualCleanupOnlyOwnedClosedTexturesCompleteUndoRedo()
        {
            var m=New();m.SetFloat("_BumpMapToggle",0);m.SetFloat("_MatCapToggle",1);m.SetFloat("_NB_TierAllowMatCap",0);m.SetTexture("_BumpTex",Texture2D.whiteTexture);m.SetTexture("_MatCapTex",Texture2D.whiteTexture);m.SetVector("_NB_CustomLocalToWorld0",new Vector4(11,12,13,14));var root=Root(m);Assert.That(Call(root,"InitializeGraphRemainingSharedUI"),Is.True);var before=Snapshot.Read(m);Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            try{Call(Toolbar(root),"CleanUnusedTextures");Assert.That(m.GetTexture("_BumpTex"),Is.Null,"Real cleanup consumes closed owned Bump");Assert.That(m.GetTexture("_MatCapTex"),Is.EqualTo(Texture2D.whiteTexture),"Raw on MatCap survives denied Tier visibility");before.Same(m,"Cleanup is texture-only and leaves all raw/derived/keywords/manualPass/unowned state","_BumpTex");Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(group);var after=Snapshot.Read(m);Undo.PerformUndo();before.Same(m,"Cleanup complete Undo");Undo.PerformRedo();after.Same(m,"Cleanup complete Redo");}finally{Undo.RevertAllDownToGroup(group);}
        }
        static void AddUnknownSerializedFold(Material material)
        {
            using(var so=new SerializedObject(material))
            {
                var list=so.FindProperty("m_SavedProperties.m_Floats");Assert.That(list,Is.Not.Null);int index=list.arraySize;list.InsertArrayElementAtIndex(index);var entry=list.GetArrayElementAtIndex(index);entry.FindPropertyRelative("first").stringValue="_NBFX_UnownedFoldOut";entry.FindPropertyRelative("second").floatValue=7.25f;so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
        static void AssertUnknownSerializedFold(Material material)
        {
            using(var so=new SerializedObject(material))
            {
                var list=so.FindProperty("m_SavedProperties.m_Floats");bool found=false;for(int i=0;i<list.arraySize;++i){var entry=list.GetArrayElementAtIndex(i);if(entry.FindPropertyRelative("first").stringValue!="_NBFX_UnownedFoldOut")continue;found=true;Assert.That(entry.FindPropertyRelative("second").floatValue,Is.EqualTo(7.25f));}Assert.That(found,Is.True);
            }
        }
        [Test]public void G4RemainingGUI_ActualCollapseOnlyOwnedFoldsAndKnownTransactionRollback()
        {
            var m=New();m.SetFloat("_BumpTexFoldOut",1);m.SetFloat("_BumpUVModeFoldOut",1);m.SetVector("_NB_CustomLocalToWorld0",new Vector4(11,12,13,14));AddUnknownSerializedFold(m);var root=Root(m);Assert.That(Call(root,"InitializeGraphRemainingSharedUI"),Is.True);var before=Snapshot.Read(m);var allowed=m.shader.GetPropertyCount()>0?new List<string>():null;for(int i=0;i<m.shader.GetPropertyCount();++i){string name=m.shader.GetPropertyName(i);if(name.EndsWith("FoldOut",StringComparison.Ordinal))allowed.Add(name);}Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            try{Call(Toolbar(root),"CollapseAll");AssertUnknownSerializedFold(m);foreach(string name in new[]{"_BigBlockModeSettingFoldOut","_ShaderKeywordFoldOut","_BumpTexFoldOut","_BumpUVModeFoldOut"})Assert.That(m.GetFloat(name),Is.Zero,name);before.Same(m,"Collapse original folds only",allowed.ToArray());Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(group);var after=Snapshot.Read(m);Undo.PerformUndo();before.Same(m,"Collapse complete Undo");Undo.PerformRedo();after.Same(m,"Collapse complete Redo");}finally{Undo.RevertAllDownToGroup(group);}
            var old=Snapshot.Read(m);Action fail=()=>{m.SetTexture("_MatCapTex",null);m.SetFloat("_AlphaAll",.1f);throw new InvalidOperationException("NBFX toolkit rollback control");};m.SetTexture("_MatCapTex",Texture2D.whiteTexture);old=Snapshot.Read(m);Assert.Throws<InvalidOperationException>(()=>Call(Sync(root),"TryRunGraphKnownToolbarEdit",fail));old.Same(m,"Toolbar shares accepted whole-object/reference rollback");
        }
    }
}
