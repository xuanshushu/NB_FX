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
    // Same original Parallax leaf, persistent Tier reader and existing GPU harness.
    public sealed class G4GraphParallaxSharedTierTests
    {
        const BindingFlags All=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
        const string Toggle="_ParallaxMapping_Toggle",Gate="_NB_TierAllowParallax",Vec="_ParallaxMapping_Vec",Intensity="_ParallaxMapping_Intensity",Range="_ParallaxMapping_IntensityRangeVec";
        readonly List<Object> owned=new List<Object>();readonly List<G4GraphPersistentGateTierTests> helpers=new List<G4GraphPersistentGateTierTests>();
        static Type TypeOf(string name)=>G4SpecDebugFixture.FindType(name);
        static object Call(object o,string name,params object[] args)=>o.GetType().GetMethod(name,All).Invoke(o,args);
        static object Property(object o,string name)=>o.GetType().GetProperty(name,All).GetValue(o);
        static object Field(object o,string name)
        {for(Type t=o.GetType();t!=null;t=t.BaseType){var f=t.GetField(name,All|BindingFlags.DeclaredOnly);if(f!=null)return f.GetValue(o);}Assert.Fail("Missing original field "+name);return null;}
        [OneTimeSetUp] public void Preflight()
        {
            Assert.That(Path.GetFullPath(Application.dataPath),Is.EqualTo(Path.GetFullPath(@"D:\UnityProject\NBUnityProject\.utmp\NBFXMeshValidation-20261002\Assets")).IgnoreCase);
            for(int i=0;i<SceneManager.sceneCount;++i){var scene=SceneManager.GetSceneAt(i);Assert.That((scene.name+"/"+scene.path).IndexOf("TAI",StringComparison.OrdinalIgnoreCase),Is.LessThan(0));}
            var shader=AssetDatabase.LoadAssetAtPath<Shader>(G4SpecDebugFixture.GraphPath);Assert.That(shader&&shader.isSupported,Is.True); // No force import after Root installed/imported products.
        }
        sealed class Snapshot
        {
            readonly object value;static Type Shared=>typeof(G4GraphGuiFeatureIntentTests).GetNestedType("Snapshot",BindingFlags.NonPublic);
            Snapshot(object value){this.value=value;}
            public static Snapshot Read(Material m)=>new Snapshot(Shared.GetMethod("Read",All).Invoke(null,new object[]{m}));
            public void AssertSame(Material m,string label,params string[] allowed)=>Shared.GetMethod("AssertSame",All).Invoke(value,new object[]{m,label,allowed});
        }
        Material Material(){var m=G4SpecDebugFixture.NewGraph();owned.Add(m);m.SetFloat("_NB_Flags0Hi16",53214.25f);m.SetFloat("_NB_ForceNoMipFlagsLo16",16.25f);m.SetFloat("_NB_ForceNoMipFlagsHi16",123.25f);return m;}
        object Root(params Material[] materials)
        {var helper=new G4GraphPersistentGateTierTests();helpers.Add(helper);return helper.GetType().GetMethod("Root",All).Invoke(helper,new object[]{materials});}
        static object Tier(int value)=>Enum.ToObject(TypeOf("NBShader.NBShaderFeatureTier"),value);
        static string[] Raw()=> (string[])TypeOf("NBShader.NBShaderFeatureCatalog").GetField("RawKeywords",All).GetValue(null);
        static bool Apply(Material m,bool full,out bool changed)
        {object[] args={m,Tier(3),full?Raw():Array.Empty<string>(),false};bool result=(bool)TypeOf("NBShaders2.Editor.FeatureLevel.NBShaderFeatureLevelMaterialApplier").GetMethod("ApplyGraphParallaxGroup",All).Invoke(null,args);changed=(bool)args[3];return result;}
        [TearDown] public void Cleanup()
        {foreach(var h in owned.OfType<NBFXMainTexGUIEventHost>()){h.Draw=null;h.Setup=null;h.Close();}foreach(var helper in helpers)helper.Cleanup();helpers.Clear();foreach(var o in owned.AsEnumerable().Reverse())if(o)Object.DestroyImmediate(o);owned.Clear();}
        NBFXMainTexGUIEventHost Host(object root,out object actual)
        {
            var h=ScriptableObject.CreateInstance<NBFXMainTexGUIEventHost>();owned.Add(h);h.hideFlags=HideFlags.HideAndDontSave;h.position=new Rect(20,20,640,680);h.SetupCommand="NBFX_Parallax_Setup_"+Guid.NewGuid().ToString("N");object item=null;
            h.Setup=()=>{Assert.That(Call(root,"InitializeGraphParallaxInputs"),Is.True);item=Field(root,"_graphParallaxItem");};h.ShowUtility();h.SendEvent(new Event{type=EventType.ExecuteCommand,commandName=h.SetupCommand});Check(h);Assert.That(h.Initialized,Is.True);actual=item;h.Draw=()=>Call(root,"DrawGraphParallaxInputs");return h;
        }
        static void Check(NBFXMainTexGUIEventHost h){if(h.Failure!=null)ExceptionDispatchInfo.Capture(h.Failure).Throw();}
        static void Send(NBFXMainTexGUIEventHost h,Event e)
        {var type=e.rawType;Assert.That(type,Is.Not.EqualTo(EventType.Ignore).And.Not.EqualTo(EventType.Used));h.Counts.TryGetValue(type,out int before);h.SendEvent(e);Check(h);Assert.That(e.rawType,Is.EqualTo(type));Assert.That(h.Counts.TryGetValue(type,out int after)&&after>before,Is.True,"Actual Root native GUI must receive event.");}
        [Test] public void G4ParallaxShared_CPU_ActualSchemaAndOriginalLeaf()
        {
            var m=Material();var root=Root(m);var h=Host(root,out var item);h.Draw=null;Assert.That(item.GetType().Name,Is.EqualTo("ParallaxFeatureItem"));
            foreach(string name in new[]{Toggle,Intensity,Gate,"_ParallaxBlockFoldOut"}){int index=m.shader.FindPropertyIndex(name);Assert.That(index,Is.GreaterThanOrEqualTo(0),name);Assert.That(m.shader.GetPropertyType(index),Is.EqualTo(ShaderPropertyType.Float),name);}
            foreach(string name in new[]{Vec,Range})Assert.That(m.shader.GetPropertyType(m.shader.FindPropertyIndex(name)),Is.EqualTo(ShaderPropertyType.Vector));
            Assert.That(m.shader.GetPropertyDefaultFloatValue(m.shader.FindPropertyIndex("_ParallaxBlockFoldOut")),Is.EqualTo(0));Assert.That(m.shader.GetPropertyDefaultVectorValue(m.shader.FindPropertyIndex(Range)),Is.EqualTo(new Vector4(0,.1f,0,0)));Assert.That(m.shader.GetPropertyDefaultFloatValue(m.shader.FindPropertyIndex(Gate)),Is.EqualTo(1));
            var children=((System.Collections.IEnumerable)Field(item,"ChildrenItemList")).Cast<object>().ToArray();Assert.That(children.Count(o=>o.GetType().Name=="WrapModeItem"),Is.EqualTo(1));Assert.That(children.Count(o=>o.GetType().Name=="ForceNoMipItem"),Is.EqualTo(1));Assert.That(children.Any(o=>o.GetType().Name=="UVModeSelectItem"||o.GetType().Name=="CustomDataSelectItem"),Is.False,"Original POM has no independent UV/CD contract.");
            var slider=children.Single(o=>o.GetType().Name=="ShaderGUISliderItem");Assert.That((bool)Field(slider,"WriteOnlyOnInteractiveChange"),Is.True);
        }
        [Test] public void G4ParallaxShared_CPU_DenyRestoreNoOpPreservesIntent()
        {
            var m=Material();m.SetFloat(Toggle,1);bool changed;Assert.That(Apply(m,true,out changed),Is.True);var before=Snapshot.Read(m);Assert.That(Apply(m,false,out changed),Is.True);Assert.That(m.GetFloat(Gate),Is.EqualTo(0));before.AssertSame(m,"Only owned POM gate denies; saved intent/raw/pass/keywords unchanged.",Gate);
            Assert.That(Apply(m,true,out changed),Is.True);before.AssertSame(m,"Complete POM gate restoration and intent.");Assert.That(Apply(m,true,out changed),Is.True);Assert.That(changed,Is.False);
        }
        [Test] public void G4ParallaxShared_CPU_FutureMixedSchemaAtomicNoWrite()
        {
            var a=Material();var b=Material();b.SetFloat("_NB_GraphGUIStateVersion",3);var root=Root(a,b);var sa=Snapshot.Read(a);var sb=Snapshot.Read(b);
            Assert.That(Call(root,"InitializeGraphParallaxInputs"),Is.False);Assert.That(Call(Property(root,"SyncService"),"TryApplyGraphParallaxEdit",true),Is.False);sa.AssertSame(a,"Valid first target remains exact after future second marker rejects all.");sb.AssertSame(b,"Future target raw/surface/pass/keyword remains exact.");
        }
        [TestCase(false,TestName="G4ParallaxShared_GUI_OutOfRangeInvalidLayersPassiveReadOnly")]
        [TestCase(true,TestName="G4ParallaxShared_GUI_MixedOutOfRangePassiveReadOnly")]
        public void Passive(bool mixed)
        {
            var a=Material();a.SetFloat(Toggle,1);a.SetFloat("_ParallaxBlockFoldOut",1);a.SetFloat(Intensity,.75f);a.SetVector(Vec,new Vector4(10,9,.375f,.625f));a.SetVector(Range,new Vector4(0,.1f,.25f,.5f));Material b=null;
            if(mixed){b=Material();b.SetFloat(Toggle,1);b.SetFloat("_ParallaxBlockFoldOut",1);b.SetFloat(Intensity,.875f);b.SetVector(Vec,new Vector4(14,12,.625f,.875f));b.SetVector(Range,new Vector4(0,.2f,.75f,.875f));}
            var root=mixed?Root(a,b):Root(a);var h=Host(root,out var item);var sa=Snapshot.Read(a);var sb=b?Snapshot.Read(b):null;Send(h,new Event{type=EventType.Layout});Send(h,new Event{type=EventType.Repaint});sa.AssertSame(a,"Full original POM subtree paint must preserve out-of-range intensity and invalid saved layers.");if(b)sb.AssertSame(b,"Passive mixed selection must preserve every distinct saved value and raw word.");
        }
        [Test] public void G4ParallaxShared_GUI_ActualToggleCompleteUndoRedo()
        {
            var m=Material();m.SetFloat(Toggle,0);G4SpecDebugFixture.Validate(m);var root=Root(m);var h=Host(root,out var item);Send(h,new Event{type=EventType.Layout});Send(h,new Event{type=EventType.Repaint});var before=Snapshot.Read(m);var rect=(Rect)Field(item,"ControlRect");var click=new Vector2(rect.x+6,rect.center.y);Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            try
            {
                // Real Root owns pre-event Undo; no test RecordObjects wrapper.
                Send(h,new Event{type=EventType.MouseDown,button=0,mousePosition=click});Send(h,new Event{type=EventType.MouseUp,button=0,mousePosition=click});Assert.That(m.GetFloat(Toggle),Is.EqualTo(1));Assert.That(m.GetFloat(Gate),Is.EqualTo(1));
                string[] gates=(string[])TypeOf("NBShaders2.Editor.FeatureLevel.NBShaderFeatureLevelMaterialApplier").GetField("GraphSupportedGateProperties",All).GetValue(null);before.AssertSame(m,"Only clicked intent and registered derived gates.",gates.Concat(new[]{Toggle}).ToArray());
                Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(group);var after=Snapshot.Read(m);h.Draw=null;Undo.PerformUndo();before.AssertSame(m,"Actual original POM toggle full Undo.");Undo.PerformRedo();after.AssertSame(m,"Actual original POM toggle full Redo.");
            }
            finally{h.Draw=null;Undo.RevertAllDownToGroup(group);}
        }
        [Test] public void G4ParallaxShared_GUI_ActualLayerInputNormalizationUndoRedo()
        {
            var m=Material();m.SetFloat(Toggle,1);m.SetFloat("_ParallaxBlockFoldOut",1);m.SetVector(Vec,new Vector4(5,30,.375f,.625f));G4SpecDebugFixture.Validate(m);var root=Root(m);var h=Host(root,out var item);Send(h,new Event{type=EventType.Layout});Send(h,new Event{type=EventType.Repaint});var before=Snapshot.Read(m);
            var children=((System.Collections.IEnumerable)Field(item,"ChildrenItemList")).Cast<object>();var minItem=children.Single(o=>o.GetType().Name=="VectorComponentItem"&&(int)Field(o,"_componentIndex")==0);var rect=(Rect)Field(minItem,"ControlRect");Assert.That(rect.width>0&&rect.height>0,Is.True);var click=new Vector2(rect.center.x,rect.center.y);Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            try
            {
                Send(h,new Event{type=EventType.MouseDown,button=0,mousePosition=click});Send(h,new Event{type=EventType.MouseUp,button=0,mousePosition=click});Vector4 changed=m.GetVector(Vec);Assert.That(changed.x,Is.GreaterThan(30));Assert.That(changed.y,Is.EqualTo(changed.x+1));Assert.That(changed.z,Is.EqualTo(.375f));Assert.That(changed.w,Is.EqualTo(.625f));before.AssertSame(m,"Only actual layer input + original max>=min+1 edit normalization owns Vec.",Vec);
                Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(group);var after=Snapshot.Read(m);h.Draw=null;Undo.PerformUndo();before.AssertSame(m,"Layer original input and derived max normalization full Undo.");Undo.PerformRedo();after.AssertSame(m,"Layer input + normalization full Redo.");
            }
            finally{h.Draw=null;Undo.RevertAllDownToGroup(group);}
        }
        static object POM(string name,params object[] args)=>typeof(G4GraphParallaxTests).GetMethod(name,All).Invoke(null,args);
        [TestCase(true,TestName="G4ParallaxShared_GPU_ForwardAlphaDenyRestore_ortho")]
        [TestCase(false,TestName="G4ParallaxShared_GPU_ForwardAlphaDenyRestore_perspective")]
        public void ForwardDenyRestore(bool ortho)
        {
            string id="parallax-shared-tier-"+(ortho?"ortho":"perspective");using(var h=new G4SpecDebugFixture.Harness(id,ortho))
            {
                var empty=h.Snap("empty");var baseMap=(Texture2D)POM("MakeBaseMap");owned.Add(baseMap);var height=(Texture2D)POM("MakeHeightMap");owned.Add(height);var mesh=(Mesh)POM("BuildMesh");owned.Add(mesh);h.renderer.GetComponent<MeshFilter>().sharedMesh=mesh;
                var pixels=baseMap.GetPixels();for(int i=0;i<pixels.Length;++i)pixels[i].a=.2f+.65f*(i%baseMap.width)/(baseMap.width-1f);baseMap.SetPixels(pixels);baseMap.Apply(true);
                for(int i=0;i<3;++i){POM("Configure",h.materials[i],i==2,false,baseMap,height);POM("Apply",h.materials[i],i==2,POM("MakeState","on"));if(i==2)G4SpecDebugFixture.Validate(h.materials[i]);G4SpecDebugFixture.Harness.RestoreForward(h.materials[i],i==2);}
                bool change;Assert.That(Apply(h.materials[2],true,out change),Is.True);var before=Snapshot.Read(h.materials[2]);var frames=new Color[3][][];var repeats=new Color[3][][];
                for(int stage=0;stage<3;++stage)
                {
                    Assert.That(Apply(h.materials[2],stage!=1,out change),Is.True);before.AssertSame(h.materials[2],"Only POM effective gate changes, saved UV/CD/flags/intent/pass untouched.",Gate);frames[stage]=new Color[3][];repeats[stage]=new Color[3][];
                    for(int i=0;i<3;++i){if(i!=2)G4SpecDebugFixture.SetKeyword(h.materials[i],"_PARALLAX_MAPPING",stage!=1);frames[stage][i]=h.Snap("ABC"[i]+"-"+stage,h.materials[i]);repeats[stage][i]=h.Snap("ABC"[i]+"-"+stage+"-repeat",h.materials[i]);}
                }
                var metrics=new G4SpecDebugFixture.Metrics{caseId=id,scope="Only existing main Forward POM/selected-alpha consumer with one derived Tier gate; same old meshes/textures/kernel. No POM depth/shadow/debug/Pass/Player completeness claim.",finite=G4SpecDebugFixture.Finite(empty)&&frames.SelectMany(f=>f).Concat(repeats.SelectMany(f=>f)).All(G4SpecDebugFixture.Finite),ab=frames.Select(f=>G4SpecDebugFixture.Delta(f[0],f[1])).ToArray(),bc=frames.Select(f=>G4SpecDebugFixture.Delta(f[1],f[2])).ToArray(),repeat=Enumerable.Range(0,3).SelectMany(s=>Enumerable.Range(0,3).Select(i=>G4SpecDebugFixture.Delta(frames[s][i],repeats[s][i]))).ToArray(),response=Enumerable.Range(0,3).Select(i=>G4SpecDebugFixture.Delta(frames[0][i],frames[1][i])).ToArray(),restore=Enumerable.Range(0,3).Select(i=>G4SpecDebugFixture.Delta(frames[0][i],frames[2][i])).ToArray(),visible=frames.SelectMany(f=>f).Select(f=>G4SpecDebugFixture.Visible(f,empty)).ToArray()};
                h.SaveAndAssert(metrics);Assert.That(metrics.response.All(v=>v>.01f),Is.True);for(int i=0;i<3;++i)Assert.That(Enumerable.Range(0,frames[0][i].Length).Max(p=>Mathf.Abs(frames[0][i][p].a-frames[1][i][p].a)),Is.GreaterThan(.001f),"Real selected alpha must independently respond to POM UV denial.");before.AssertSame(h.materials[2],"Full POM intent and derived gate restoration.");
            }
        }
    }
}
