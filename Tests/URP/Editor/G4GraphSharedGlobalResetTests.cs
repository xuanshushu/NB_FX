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
    public sealed class G4GraphSharedGlobalResetTests
    {
        const BindingFlags All=BindingFlags.Static|BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        readonly List<Object> owned=new List<Object>();readonly List<G4GraphPersistentGateTierTests> helpers=new List<G4GraphPersistentGateTierTests>();
        static Type Find(string n)=>G4SpecDebugFixture.FindType(n);
        static MethodInfo Unique(Type t,string n,params Type[] sig){var m=t.GetMethods(All).Where(x=>x.Name==n&&x.GetParameters().Select(p=>p.ParameterType).SequenceEqual(sig)).ToArray();Assert.That(m.Length,Is.EqualTo(1),t.FullName+"."+n);return m[0];}
        static object Invoke(MethodInfo m,object target,params object[] args){try{return m.Invoke(target,args);}catch(TargetInvocationException e){ExceptionDispatchInfo.Capture(e.InnerException??e).Throw();throw;}}
        static object Call(object target,string n,params object[] args)
        {Type[] sig;switch(n){case "GetToolbarResetRootItems":case "ExecuteResetAllItems":case "ResetAll":case "ResetDisabledFeatureChildren":case "ResetSpecialUVChannel":case "ResetTwirl":case "ResetPolar":case "HasGraphSharedResetSchema":sig=Type.EmptyTypes;break;case "TryResetGraphOwnedItems":sig=new[]{typeof(bool)};break;case "TryRunGraphSharedReset":sig=new[]{typeof(Action),typeof(bool)};break;default:Assert.Fail(n);return null;}return Invoke(Unique(target.GetType(),n,sig),target,args);}
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
        [TearDown]public void Cleanup(){foreach(var h in owned.OfType<NBFXMainTexGUIEventHost>()){h.Draw=null;h.Setup=null;h.Close();}foreach(var x in helpers)x.Cleanup();helpers.Clear();foreach(var x in owned.AsEnumerable().Reverse())if(x)Object.DestroyImmediate(x);owned.Clear();}
        static object Toolbar(object root)=>Activator.CreateInstance(Find("NBShaderEditor.NBShaderGUIToolBar"),new object[]{root});
        static object[] Roots(object root)=>((IEnumerable)Call(root,"GetToolbarResetRootItems")).Cast<object>().ToArray();
        static int Lo(Material m)=>Mathf.RoundToInt(m.GetFloat("_NB_Flags0Lo16"))&65535;
        static int Hi(Material m)=>Mathf.RoundToInt(m.GetFloat("_NB_Flags1Hi16"))&65535;
        static readonly Vector4 Foreign=new Vector4(12.25f,23.5f,34.75f,45.125f);
        void Fill(Material m)
        {
            m.SetFloat("_BumpMapToggle",1);m.SetTexture("_BumpTex",Texture2D.whiteTexture);m.SetFloat("_BumpScale",.25f);m.SetVector("BumpScaleRangeVec",new Vector4(-2,2,11,12));
            m.SetFloat("_MatCapToggle",1);m.SetTexture("_MatCapTex",Texture2D.whiteTexture);m.SetVector("_MatCapInfo",new Vector4(.25f,.75f,11,12));m.SetColor("_MatCapColor",new Color(.2f,.3f,.4f,.5f));
            m.SetFloat("_UTwirlEnabled",1);m.SetFloat("_PolarCoordinatesEnabled",1);m.SetFloat("_NB_Flags0Lo16",Lo(m)|256|512);m.SetFloat("_NB_Flags1Hi16",(Hi(m)&~12)|8);m.SetVector("_TWParameter",Foreign);m.SetFloat("_TWStrength",3.25f);m.SetVector("_PCCenter",Foreign);
            m.SetVector("_NB_CustomLocalToWorld0",Foreign);m.SetFloat("_NB_GraphScreenPassMigrationComplete",0);m.SetFloat("_NB_GraphPassMigrationComplete",0);
        }
        static void AssertUnowned(Material m,float saved,Vector4 foreign)
        {Assert.That(m.GetFloat("_NBShaderFeatureTier"),Is.EqualTo(saved));Assert.That(m.GetFloat("_NB_GraphGUIStateVersion"),Is.EqualTo(2));Assert.That(m.GetVector("_NB_CustomLocalToWorld0"),Is.EqualTo(foreign));}
        static void AssertDefaultFloat(Material m,string name)=>Assert.That(m.GetFloat(name),Is.EqualTo(m.shader.GetPropertyDefaultFloatValue(m.shader.FindPropertyIndex(name))),name);
        static void AssertDefaultVector(Material m,string name)=>Assert.That(m.GetVector(name),Is.EqualTo(m.shader.GetPropertyDefaultVectorValue(m.shader.FindPropertyIndex(name))),name);
        static void AssertMatCapOwnedComponentReset(Material material,Vector4 before)
        {
            Vector4 expected=before;expected.x=material.shader.GetPropertyDefaultVectorValue(material.shader.FindPropertyIndex("_MatCapInfo")).x;
            Assert.That(material.GetVector("_MatCapInfo"),Is.EqualTo(expected),"Original shared MatCap factory owns x only; yzw remain exact sentinels");
        }
        [Test]public void G4GraphReset_CPU_ActualRootListIncludesFullLightAndPassiveReadOnly()
        {
            var a=New();var b=New();Fill(a);Fill(b);var root=Root(a,b);var sa=Snapshot.Read(a);var sb=Snapshot.Read(b);var items=Roots(root);Assert.That(items.Length,Is.GreaterThanOrEqualTo(28));Assert.That(items.Distinct().Count(),Is.EqualTo(items.Length));Assert.That(items.Contains(Field(root,"_graphLightModeBlock")),Is.True);Assert.That(items.Contains(Field(root,"_graphNormalMapBlock")),Is.True);Assert.That(items.Contains(Field(root,"_graphMatCapBlock")),Is.True);Assert.That(items.Contains(Field(root,"_graphVATItem")),Is.True);Assert.That(items.Contains(Field(root,"_graphPortalItem")),Is.True);Assert.That(items.Contains(Field(root,"_graphTADepthBlock")),Is.True);Assert.That(Children(Field(root,"_graphBaseNumericBlock")).Any(x=>(string)Field(x,"PropertyName")=="_IgnoreVetexColor_Toggle"),Is.True);sa.Same(a,"Root list A has no paint mutation");sb.Same(b,"Root list B has no paint mutation");
        }
        [Test]public void G4GraphReset_Callback_OriginalResetAllBumpMatCapSpecialUVCompleteUndoRedo()
        {
            var a=New();var b=New();Fill(a);Fill(b);b.SetFloat("_NBShaderFeatureTier",2);b.SetVector("_NB_CustomLocalToWorld0",Foreign+Vector4.one);var root=Root(a,b);Assert.That(Roots(root).Length,Is.GreaterThanOrEqualTo(28));var matcapBefore=new[]{a,b}.ToDictionary(value=>value,value=>value.GetVector("_MatCapInfo"));var sa=Snapshot.Read(a);var sb=Snapshot.Read(b);float ta=a.GetFloat("_NBShaderFeatureTier"),tb=b.GetFloat("_NBShaderFeatureTier");Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            try{Call(Toolbar(root),"ResetAll");foreach(Material m in new[]{a,b}){AssertDefaultFloat(m,"_BumpMapToggle");AssertDefaultFloat(m,"_BumpScale");AssertDefaultVector(m,"BumpScaleRangeVec");Assert.That(m.GetTexture("_BumpTex"),Is.Null);AssertDefaultFloat(m,"_MatCapToggle");AssertMatCapOwnedComponentReset(m,matcapBefore[m]);Assert.That(m.GetTexture("_MatCapTex"),Is.Null);AssertDefaultFloat(m,"_UTwirlEnabled");AssertDefaultFloat(m,"_PolarCoordinatesEnabled");Assert.That(Lo(m)&(256|512),Is.Zero);Assert.That(Hi(m)&12,Is.EqualTo(4));AssertDefaultVector(m,"_TWParameter");AssertDefaultVector(m,"_PCCenter");AssertDefaultFloat(m,"_TWStrength");}AssertUnowned(a,ta,Foreign);AssertUnowned(b,tb,Foreign+Vector4.one);Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(group);var aa=Snapshot.Read(a);var ab=Snapshot.Read(b);Undo.PerformUndo();sa.Same(a,"Global callback complete Undo A");sb.Same(b,"Global callback complete Undo B");Undo.PerformRedo();aa.Same(a,"Global callback complete Redo A");ab.Same(b,"Global callback complete Redo B");}finally{Undo.RevertAllDownToGroup(group);}
        }
        [Test]public void G4GraphReset_Callback_DisabledChildrenOnlyKeepsActiveToggleAndUnownedIntentUndoRedo()
        {
            var m=New();Fill(m);m.SetFloat("_BumpMapToggle",0);m.SetFloat("_MatCapToggle",0);m.SetFloat("_Mask_Toggle",1);m.SetFloat("_Mask2_Toggle",0);m.SetFloat("_Mask3_Toggle",0);m.SetVector("_MaskMapVec",Foreign);m.SetTexture("_MaskMap",Texture2D.whiteTexture);var root=Root(m);Assert.That(Roots(root).Length,Is.GreaterThanOrEqualTo(28));var maskBefore=m.GetVector("_MaskMapVec");var matcapBefore=m.GetVector("_MatCapInfo");var before=Snapshot.Read(m);float tier=m.GetFloat("_NBShaderFeatureTier");Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            try{Call(Toolbar(root),"ResetDisabledFeatureChildren");Assert.That(m.GetFloat("_BumpMapToggle"),Is.Zero);Assert.That(m.GetFloat("_MatCapToggle"),Is.Zero);AssertDefaultFloat(m,"_BumpScale");AssertMatCapOwnedComponentReset(m,matcapBefore);Assert.That(m.GetTexture("_BumpTex"),Is.Null);Assert.That(m.GetTexture("_MatCapTex"),Is.Null);Assert.That(m.GetFloat("_Mask_Toggle"),Is.EqualTo(1));var maskExpected=maskBefore;var maskDefault=m.shader.GetPropertyDefaultVectorValue(m.shader.FindPropertyIndex("_MaskMapVec"));maskExpected.y=maskDefault.y;maskExpected.z=maskDefault.z;Assert.That(m.GetVector("_MaskMapVec"),Is.EqualTo(maskExpected),"Active main Mask x and unowned w stay exact; off Mask2/3 children own y/z reset");Assert.That(m.GetFloat("_Mask2_Toggle"),Is.Zero);Assert.That(m.GetFloat("_Mask3_Toggle"),Is.Zero);Assert.That(m.GetTexture("_MaskMap"),Is.EqualTo(Texture2D.whiteTexture));AssertUnowned(m,tier,Foreign);Assert.That(m.GetFloat("_UTwirlEnabled"),Is.EqualTo(1));Assert.That(Lo(m)&(256|512),Is.EqualTo(256|512));Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(group);var after=Snapshot.Read(m);Undo.PerformUndo();before.Same(m,"Disabled callback complete Undo");Undo.PerformRedo();after.Same(m,"Disabled callback complete Redo");}finally{Undo.RevertAllDownToGroup(group);}
        }
        [Test]public void G4GraphReset_CPU_InvalidLastRefusalAndActualFailureBeforeImageRestoration()
        {
            var a=New();var b=New();Fill(a);Fill(b);var root=Root(a,b);Assert.That(Roots(root).Length,Is.GreaterThanOrEqualTo(28));b.SetFloat("_TransparentShadowDitherToggle",float.NaN);var sa=Snapshot.Read(a);var sb=Snapshot.Read(b);Assert.That(Call(root,"TryResetGraphOwnedItems",false),Is.False);sa.Same(a,"Invalid last preflight all refuses A");sb.Same(b,"Invalid last preserves B");b.SetFloat("_TransparentShadowDitherToggle",0);
            var ownTexture=new Texture2D(2,2,TextureFormat.RGBA32,false,true){hideFlags=HideFlags.HideAndDontSave};owned.Add(ownTexture);a.SetTexture("_BumpTex",ownTexture);
            Assert.That(a.GetTexture("_MatCapTex"),Is.EqualTo(Texture2D.whiteTexture),"External builtin reference control");sa=Snapshot.Read(a);sb=Snapshot.Read(b);
            Action throws=()=>{a.SetTexture("_BumpTex",null);a.SetTexture("_MatCapTex",null);a.SetFloat("_AlphaAll",.125f);b.SetFloat("_AlphaAll",.25f);a.SetShaderPassEnabled("SRPDefaultUnlit",false);a.SetFloat("_CastShadows",0);a.SetShaderPassEnabled("ShadowCaster",false);b.renderQueue=3456;a.SetOverrideTag("RenderType","ForeignFailureTag");throw new InvalidOperationException("NBFX intentional reset callback failure");};
            var error=Assert.Throws<InvalidOperationException>(()=>Call(Sync(root),"TryRunGraphSharedReset",throws,true));Assert.That(error.Message,Is.EqualTo("NBFX intentional reset callback failure"));sa.Same(a,"Callback failure restores complete A including actual tags/Cast");Assert.That(a.GetTexture("_BumpTex"),Is.EqualTo(ownTexture),"Strong self-owned temporary texture survives exception rollback");Assert.That(a.GetTexture("_MatCapTex"),Is.EqualTo(Texture2D.whiteTexture),"External builtin texture survives exception rollback");sb.Same(b,"Callback failure restores complete B queue/properties");
            Action invalid=()=>b.SetFloat("_TransparentShadowDitherToggle",float.NaN);Assert.That(Call(Sync(root),"TryRunGraphSharedReset",invalid,false),Is.False);sa.Same(a,"Final schema invalid rejects+restores A");sb.Same(b,"Final schema invalid restores B raw NaN edit");
        }
    }
}
