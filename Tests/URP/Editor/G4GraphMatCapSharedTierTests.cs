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
    // Reuses original leaves, actual Root event owner, state snapshots and ABC
    // harness. Native popup selection remains manual; no simulated popup pass.
    public sealed class G4GraphMatCapSharedTierTests
    {
        const BindingFlags All=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance;
        const string Gate="_NB_TierAllowMatCap";
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
            h.SetupCommand="NBFX_MatCap_Setup_"+Guid.NewGuid().ToString("N");object actual=null;
            h.Setup=()=>{Assert.That(Call(root,"InitializeGraphMatCapInputs"),Is.True);actual=Field(root,"_graphMatCapBlock");};
            h.ShowUtility();h.SendEvent(new Event{type=EventType.ExecuteCommand,commandName=h.SetupCommand});Check(h);Assert.That(h.Initialized,Is.True);
            item=actual;h.Draw=()=>Call(root,"DrawGraphMatCapInputs",new object[]{null});return h;
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
            object[] args={m,Enum.ToObject(TypeOf("NBShader.NBShaderFeatureTier"),3),full?Raw():Raw().Where(k=>k!="_MATCAP").ToArray(),false};
            bool result=(bool)TypeOf("NBShaders2.Editor.FeatureLevel.NBShaderFeatureLevelMaterialApplier").GetMethod("ApplyGraphMatCapGroup",All).Invoke(null,args);changed=(bool)args[3];return result;
        }
        [TearDown] public void Cleanup()
        {
            foreach(var h in owned.OfType<NBFXMainTexGUIEventHost>()){h.Draw=null;h.Setup=null;h.Close();}
            foreach(var helper in helpers)helper.Cleanup();helpers.Clear();
            foreach(var o in owned.AsEnumerable().Reverse())if(o)Object.DestroyImmediate(o);owned.Clear();
        }
        [Test] public void G4MatCapShared_CPU_OriginalTypesMixedPassiveSixWay()
        {
            var a=Material();var b=Material();
            foreach(var m in new[]{a,b}){m.SetFloat("_MatCapToggle",1);m.SetFloat("_MatCapFoldOut",1);m.SetFloat("_FxLightMode",0);}
            a.SetVector("_MatCapInfo",new Vector4(.25f,4,5,6));b.SetVector("_MatCapInfo",new Vector4(.75f,7,8,9));
            var root=Root(a,b);var beforeA=Snapshot.Read(a);var beforeB=Snapshot.Read(b);var h=Host(root,out var block);
            Assert.That(block.GetType().Name,Is.EqualTo("PropertyToggleBlockItem"));var children=Descendants(block).ToArray();
            var texture=children.Single(o=>o.GetType().Name=="TextureItem");Assert.That(Field(Field(texture,"_groupItem"),"_scaleOffsetItem"),Is.Null,"Original MatCap has no ST control.");
            Assert.That(children.Any(o=>new[]{"UVModeSelectItem","WrapModeItem","CustomDataSelectItem","ColorChannelSelectItem"}.Contains(o.GetType().Name)),Is.False);
            var noMip=children.Single(o=>o.GetType().Name=="ForceNoMipItem");Assert.That((int)Field(noMip,"_forceNoMipFlagBits"),Is.EqualTo(1<<5));
            foreach(string name in new[]{"_MatCapToggle","_MatCapFoldOut",Gate})Assert.That(a.shader.GetPropertyType(a.shader.FindPropertyIndex(name)),Is.EqualTo(ShaderPropertyType.Float));
            Assert.That(a.shader.GetPropertyType(a.shader.FindPropertyIndex("_MatCapTex")),Is.EqualTo(ShaderPropertyType.Texture));
            Assert.That(a.shader.GetPropertyType(a.shader.FindPropertyIndex("_MatCapColor")),Is.EqualTo(ShaderPropertyType.Color));
            Assert.That(a.shader.GetPropertyType(a.shader.FindPropertyIndex("_MatCapInfo")),Is.EqualTo(ShaderPropertyType.Vector));
            Assert.That(a.shader.GetPropertyDefaultFloatValue(a.shader.FindPropertyIndex(Gate)),Is.EqualTo(1));
            Send(h,new Event{type=EventType.Layout});Send(h,new Event{type=EventType.Repaint});
            beforeA.AssertSame(a,"Mixed passive MatCap A retains every field/keyword/pass.");beforeB.AssertSame(b,"Mixed passive MatCap B retains every field/keyword/pass.");
            foreach(var m in new[]{a,b})m.SetFloat("_FxLightMode",4);Call(Property(root,"SyncService"),"RefreshGraphMainTexPropertyReferences");Call(Property(root,"Context"),"Refresh");Assert.That(Convert.ToInt32(Property(Property(root,"Context"),"FxLightMode")),Is.EqualTo(4),"Refreshed actual MaterialProperty snapshot contains SixWay.");
            Assert.That(((Func<bool>)Field(block,"_isVisible"))(),Is.False,"Preserve original non-SixWay visibility.");
            beforeA=Snapshot.Read(a);beforeB=Snapshot.Read(b);Send(h,new Event{type=EventType.Layout});Send(h,new Event{type=EventType.Repaint});h.Draw=null;
            beforeA.AssertSame(a,"Hidden SixWay leaf is passive.");beforeB.AssertSame(b,"Hidden SixWay leaf is passive.");
        }
        [Test] public void G4MatCapShared_CPU_TierDenyRestoreAndFinalSync()
        {
            var m=Material();m.SetFloat("_MatCapToggle",1);bool changed;
            Assert.That(Apply(m,true,out changed),Is.True);var before=Snapshot.Read(m);
            Assert.That(Apply(m,false,out changed),Is.True);Assert.That(m.GetFloat(Gate),Is.EqualTo(0));Assert.That(m.GetFloat("_MatCapToggle"),Is.EqualTo(1));before.AssertSame(m,"Only derived MatCap gate changes.",Gate);
            Assert.That(Apply(m,true,out changed),Is.True);before.AssertSame(m,"Exact gate restore.");Assert.That(Apply(m,true,out changed),Is.True);Assert.That(changed,Is.False);
            // Actual saved policy and final Graph validator must agree. The test
            // reads project policy instead of assuming a hardcoded tier budget.
            var settings=TypeOf("NBShaders2.Editor.FeatureLevel.NBShaderFeatureLevelProjectSettings");
            var instance=settings.GetProperty("instance",All|BindingFlags.FlattenHierarchy).GetValue(null);
            for(int tier=0;tier<=3;++tier)
            {
                var level=Enum.ToObject(TypeOf("NBShader.NBShaderFeatureTier"),tier);
                var allowed=((IEnumerable<string>)settings.GetMethod("GetAllowedKeywordSetForBuildInfoNoSave",All).Invoke(instance,new[]{level})).ToArray();
                m.SetFloat("_NBShaderFeatureTier",tier);G4SpecDebugFixture.Validate(m);
                Assert.That(m.GetFloat(Gate),Is.EqualTo(allowed.Contains("_MATCAP")?1f:0f));Assert.That(m.GetFloat("_MatCapToggle"),Is.EqualTo(1));
            }
            Assert.That(m.GetFloat("_NB_ForceNoMipFlagsHi16"),Is.EqualTo(123.25f));
        }
        [TestCase(false,TestName="G4MatCapShared_GUI_ActualToggleUndoRedo")]
        [TestCase(true,TestName="G4MatCapShared_GUI_ActualNoMipUndoRedo")]
        public void ActualToggle(bool noMip)
        {
            var m=Material();m.SetFloat("_MatCapToggle",noMip?1:0);m.SetFloat("_MatCapFoldOut",noMip?1:0);m.SetFloat("_FxLightMode",0);
            var root=Root(m);var h=Host(root,out var block);Send(h,new Event{type=EventType.Layout});Send(h,new Event{type=EventType.Repaint});
            object item=noMip?Descendants(block).Single(o=>o.GetType().Name=="ForceNoMipItem"):block;
            var rect=(Rect)Field(item,"ControlRect");Assert.That(rect.width>0&&rect.height>0,Is.True);var pos=new Vector2(rect.x+6,rect.center.y);var before=Snapshot.Read(m);Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            try
            {
                Send(h,new Event{type=EventType.MouseDown,button=0,mousePosition=pos});Send(h,new Event{type=EventType.MouseUp,button=0,mousePosition=pos});
                if(noMip)
                {Assert.That(Mathf.RoundToInt(m.GetFloat("_NB_ForceNoMipFlagsLo16")),Is.EqualTo(33));before.AssertSame(m,"Original NoMip bit5 only.","_NB_ForceNoMipFlagsLo16");}
                else{Assert.That(m.GetFloat("_MatCapToggle"),Is.EqualTo(1));Assert.That(m.GetFloat(Gate),Is.EqualTo(1));}
                Assert.That(m.GetFloat("_NB_ForceNoMipFlagsHi16"),Is.EqualTo(123.25f));
                Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(group);var after=Snapshot.Read(m);h.Draw=null;Undo.PerformUndo();before.AssertSame(m,"Actual Root event-owner full Undo.");Undo.PerformRedo();after.AssertSame(m,"Actual Root event-owner full Redo.");
            }
            finally{h.Draw=null;Undo.RevertAllDownToGroup(group);}
        }
        static object Original(string name,params object[] args)=>typeof(G4GraphMatCapTests).GetMethod(name,All).Invoke(null,args);
        [Test] public void G4MatCapShared_GPU_TierDenyRestore_ortho()
        {
            const string id="matcap-shared-tier-ortho";
            using(var h=new G4SpecDebugFixture.Harness(id,true))
            {
                var empty=h.Snap("empty");var texture=(Texture2D)Original("MakeMatCapTexture",false);owned.Add(texture);
                var baseMap=h.Constant(new Color(.2f,.4f,.6f,1));var emission=h.Constant(Color.black);
                for(int i=0;i<3;++i)Original("Configure",h.materials[i],i==2,"mix-add",baseMap,emission);
                var state=Original("MakeState","mix-add",false);
                Original("ApplyState",h.materials[2],h.materials[0],texture,state);Original("ApplyState",h.materials[2],h.materials[1],texture,state);
                for(int i=0;i<3;++i)G4SpecDebugFixture.Harness.RestoreForward(h.materials[i],i==2);
                bool changed;Assert.That(Apply(h.materials[2],true,out changed),Is.True);var before=Snapshot.Read(h.materials[2]);
                var frames=new Color[3][][];var repeats=new Color[3][][];
                for(int stage=0;stage<3;++stage)
                {
                    Assert.That(Apply(h.materials[2],stage!=1,out changed),Is.True);before.AssertSame(h.materials[2],"Only MatCap allow changes; input, bit5, pass and sampler remain.",Gate);
                    frames[stage]=new Color[3][];repeats[stage]=new Color[3][];
                    for(int i=0;i<3;++i)
                    {
                        if(i!=2)G4SpecDebugFixture.SetKeyword(h.materials[i],"_MATCAP",stage!=1);
                        frames[stage][i]=h.Snap("ABC"[i]+"-"+stage,h.materials[i]);repeats[stage][i]=h.Snap("ABC"[i]+"-"+stage+"-repeat",h.materials[i]);
                    }
                }
                var metrics=new G4SpecDebugFixture.Metrics{caseId=id,scope="Original MatCap mix-add input through parent allow; ordinary orthographic Mesh only; no repeat of old MatCap46 or frozen-failure waiver.",finite=G4SpecDebugFixture.Finite(empty)&&frames.SelectMany(f=>f).Concat(repeats.SelectMany(f=>f)).All(G4SpecDebugFixture.Finite),ab=frames.Select(f=>G4SpecDebugFixture.Delta(f[0],f[1])).ToArray(),bc=frames.Select(f=>G4SpecDebugFixture.Delta(f[1],f[2])).ToArray(),repeat=Enumerable.Range(0,3).SelectMany(s=>Enumerable.Range(0,3).Select(i=>G4SpecDebugFixture.Delta(frames[s][i],repeats[s][i]))).ToArray(),response=Enumerable.Range(0,3).Select(i=>G4SpecDebugFixture.Delta(frames[0][i],frames[1][i])).ToArray(),restore=Enumerable.Range(0,3).Select(i=>G4SpecDebugFixture.Delta(frames[0][i],frames[2][i])).ToArray(),visible=frames.SelectMany(f=>f).Select(f=>G4SpecDebugFixture.Visible(f,empty)).ToArray()};
                h.SaveAndAssert(metrics);Assert.That(metrics.response.All(v=>v>.01f),Is.True,"Actual independent ABC MatCap response.");before.AssertSame(h.materials[2],"Full input/gate restore.");
            }
        }
    }
}
