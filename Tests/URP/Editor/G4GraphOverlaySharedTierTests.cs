using System;
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
    // Existing two widgets and existing rendered material/core; no new pipeline.
    public sealed class G4GraphOverlaySharedTierTests
    {
        const BindingFlags All=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
        static readonly string[] Toggles={"_EmissionEnabled","_ColorBlendMap_Toggle"};
        static readonly string[] Gates={"_NB_TierAllowEmission","_NB_TierAllowColorBlend"};
        readonly List<Object> owned=new List<Object>();
        readonly List<G4GraphPersistentGateTierTests> helpers=new List<G4GraphPersistentGateTierTests>();
        static Type TypeOf(string name)=>G4SpecDebugFixture.FindType(name);
        static object Call(object o,string name,params object[] args)=>o.GetType().GetMethod(name,All).Invoke(o,args);
        static object Property(object o,string name)=>o.GetType().GetProperty(name,All).GetValue(o);
        static object Field(object o,string name)
        {for(Type t=o.GetType();t!=null;t=t.BaseType){var f=t.GetField(name,All|BindingFlags.DeclaredOnly);if(f!=null)return f.GetValue(o);}Assert.Fail("Missing real field "+name);return null;}
        [OneTimeSetUp] public void Preflight()
        {
            Assert.That(Path.GetFullPath(Application.dataPath),Is.EqualTo(Path.GetFullPath(@"D:\UnityProject\NBUnityProject\.utmp\NBFXMeshValidation-20261002\Assets")).IgnoreCase);
            for(int i=0;i<SceneManager.sceneCount;++i){var scene=SceneManager.GetSceneAt(i);Assert.That((scene.name+"/"+scene.path).IndexOf("TAI",StringComparison.OrdinalIgnoreCase),Is.LessThan(0));}
            var shader=AssetDatabase.LoadAssetAtPath<Shader>("Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBShaderGraph.shadergraph");Assert.That(shader&&shader.isSupported,Is.True);
            // Products already imported by Root. Do not ForceUpdate the graphs.
        }
        sealed class Snapshot
        {
            readonly object value;static Type Shared=>typeof(G4GraphGuiFeatureIntentTests).GetNestedType("Snapshot",BindingFlags.NonPublic);
            Snapshot(object value){this.value=value;}
            public static Snapshot Read(Material m)=>new Snapshot(Shared.GetMethod("Read",All).Invoke(null,new object[]{m}));
            public void AssertSame(Material m,string label,params string[] allowed)=>Shared.GetMethod("AssertSame",All).Invoke(value,new object[]{m,label,allowed});
        }
        Material Material(){var m=G4SpecDebugFixture.NewGraph();owned.Add(m);m.SetFloat("_NB_Flags0Hi16",53214.25f);m.SetFloat("_NB_Flags1Lo16",1234.25f);m.SetFloat("_NB_CustomDataFlag3Lo16",43210.25f);return m;}
        object Root(params Material[] materials)
        {var helper=new G4GraphPersistentGateTierTests();helpers.Add(helper);return helper.GetType().GetMethod("Root",All).Invoke(helper,new object[]{materials});}
        static object Tier(int value)=>Enum.ToObject(TypeOf("NBShader.NBShaderFeatureTier"),value);
        static string[] Raw()=> (string[])TypeOf("NBShader.NBShaderFeatureCatalog").GetField("RawKeywords",All).GetValue(null);
        static bool Apply(Material m,bool full,out bool changed)
        {object[] args={m,Tier(3),full?Raw():Array.Empty<string>(),false};var result=(bool)TypeOf("NBShaders2.Editor.FeatureLevel.NBShaderFeatureLevelMaterialApplier").GetMethod("ApplyGraphOverlayPair",All).Invoke(null,args);changed=(bool)args[3];return result;}
        [TearDown] public void Cleanup()
        {
            foreach(var h in owned.OfType<NBFXMainTexGUIEventHost>()){h.Draw=null;h.Setup=null;h.Close();}
            foreach(var h in helpers)h.Cleanup();helpers.Clear();foreach(var o in owned.AsEnumerable().Reverse())if(o)Object.DestroyImmediate(o);owned.Clear();
        }
        [TestCase(0,TestName="G4Overlay_CPU_Emission_DenyRestoreNoOp")]
        [TestCase(1,TestName="G4Overlay_CPU_ColorBlend_DenyRestoreNoOp")]
        public void DenyRestorePreservesAllIntent(int layer)
        {
            var m=Material();m.SetFloat(Toggles[layer],1);bool changed;Assert.That(Apply(m,true,out changed),Is.True);var before=Snapshot.Read(m);
            Assert.That(Apply(m,false,out changed),Is.True);Assert.That(m.GetFloat(Gates[layer]),Is.EqualTo(0));Assert.That(m.GetFloat(Toggles[layer]),Is.EqualTo(1));before.AssertSame(m,"Policy denies only two derived gates",Gates);
            Assert.That(Apply(m,true,out changed),Is.True);before.AssertSame(m,"All original intent/raw/keyword/pass and projected state restore");Assert.That(Apply(m,true,out changed),Is.True);Assert.That(changed,Is.False);
        }
        NBFXMainTexGUIEventHost Host(object root,int layer,out object actual)
        {
            var h=ScriptableObject.CreateInstance<NBFXMainTexGUIEventHost>();owned.Add(h);h.hideFlags=HideFlags.HideAndDontSave;h.position=new Rect(20,20,640,680);h.SetupCommand="NBFX_Overlay_Setup_"+Guid.NewGuid().ToString("N");object item=null;
            h.Setup=()=>{Assert.That(Call(root,"InitializeGraphOverlayInputs"),Is.True);item=Field(root,layer==0?"_graphEmissionItem":"_graphColorBlendItem");};h.ShowUtility();h.SendEvent(new Event{type=EventType.ExecuteCommand,commandName=h.SetupCommand});Check(h);Assert.That(h.Initialized,Is.True);actual=item;h.Draw=()=>Call(root,"DrawGraphOverlayInputs");return h;
        }
        static void Check(NBFXMainTexGUIEventHost h){if(h.Failure!=null)ExceptionDispatchInfo.Capture(h.Failure).Throw();}
        static void Send(NBFXMainTexGUIEventHost h,Event e)
        {var type=e.rawType;Assert.That(type,Is.Not.EqualTo(EventType.Ignore).And.Not.EqualTo(EventType.Used));h.Counts.TryGetValue(type,out int before);h.SendEvent(e);Check(h);Assert.That(e.rawType,Is.EqualTo(type));Assert.That(h.Counts.TryGetValue(type,out int after)&&after>before,Is.True,"Original native GUI must receive the event.");}
        [TestCase(0,EventType.Layout,TestName="G4Overlay_GUI_Emission_LayoutReadOnly")]
        [TestCase(0,EventType.Repaint,TestName="G4Overlay_GUI_Emission_RepaintReadOnly")]
        [TestCase(1,EventType.Layout,TestName="G4Overlay_GUI_ColorBlend_LayoutReadOnly")]
        [TestCase(1,EventType.Repaint,TestName="G4Overlay_GUI_ColorBlend_RepaintReadOnly")]
        public void Passive(int layer,EventType type)
        {
            var m=Material();var root=Root(m);var h=Host(root,layer,out var item);var before=Snapshot.Read(m);if(type==EventType.Repaint)Send(h,new Event{type=EventType.Layout});Send(h,new Event{type=type});
            Assert.That(item.GetType().Name,Is.EqualTo(layer==0?"EmissionFeatureItem":"ColorBlendFeatureItem"));before.AssertSame(m,"Original complete Overlay widget passive event is read-only");
        }
        [TestCase(0,TestName="G4Overlay_GUI_Emission_ActualToggleUndo")]
        [TestCase(1,TestName="G4Overlay_GUI_ColorBlend_ActualToggleUndo")]
        public void ActualToggle(int layer)
        {
            var m=Material();m.SetFloat(Toggles[layer],0);G4SpecDebugFixture.Validate(m);var root=Root(m);var h=Host(root,layer,out var item);Send(h,new Event{type=EventType.Layout});Send(h,new Event{type=EventType.Repaint});
            var before=Snapshot.Read(m);var rect=(Rect)Field(item,"ControlRect");var click=new Vector2(rect.x+6,rect.center.y);Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            try
            {
                // Actual Root.DrawGraphOverlayInputs owns the pre-event Undo.
                Send(h,new Event{type=EventType.MouseDown,button=0,mousePosition=click});Send(h,new Event{type=EventType.MouseUp,button=0,mousePosition=click});
                Assert.That(m.GetFloat(Toggles[layer]),Is.EqualTo(1));Assert.That(m.GetFloat(Gates[layer]),Is.EqualTo(1));
                string[] allGates=(string[])TypeOf("NBShaders2.Editor.FeatureLevel.NBShaderFeatureLevelMaterialApplier").GetField("GraphSupportedGateProperties",All).GetValue(null);before.AssertSame(m,"Only clicked real intent and supported derived state",allGates.Concat(new[]{Toggles[layer]}).ToArray());
                Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(group);var after=Snapshot.Read(m);h.Draw=null;Undo.PerformUndo();before.AssertSame(m,"Actual toggle full Undo");Undo.PerformRedo();after.AssertSame(m,"Actual toggle full Redo");
            }
            finally{h.Draw=null;Undo.RevertAllDownToGroup(group);}
        }
        [TestCase(0,TestName="G4Overlay_CPU_Emission_OriginalBlendAlphaFlags")]
        [TestCase(1,TestName="G4Overlay_CPU_ColorBlend_OriginalBlendAlphaFlags")]
        public void PackedModes(int layer)
        {
            var m=Material();var root=Root(m);var h=Host(root,layer,out var item);h.Draw=null;var sync=Property(root,"SyncService");var before=Snapshot.Read(m);
            int blendBit=layer==0?1<<5:1<<16,blendWord=layer==0?0:1,alphaBit=layer==0?unchecked((int)0x80000000):1<<25,alphaWord=layer==0?1:0;
            Assert.That(Call(sync,"TryApplyGraphOverlayFlagEdit",blendBit,blendWord,true),Is.True);Assert.That(Call(sync,"TryApplyGraphOverlayFlagEdit",alphaBit,alphaWord,true),Is.True);
            Assert.That(m.GetFloat(layer==0?"_EmissionBlendMode":"_ColorBlendMode"),Is.EqualTo(layer==0?1:0));Assert.That(m.GetFloat(layer==0?"_EmissionAlphaMultiplyMode":"_ColorBlendAlphaMultiplyMode"),Is.EqualTo(1));
            string lo=layer==0?"_NB_Flags0Lo16":"_NB_Flags1Hi16",hi=layer==0?"_NB_Flags1Hi16":"_NB_Flags0Hi16";
            before.AssertSame(m,"Exact original packed meanings, only owned half/mirrors",lo,hi,layer==0?"_EmissionBlendMode":"_ColorBlendMode",layer==0?"_EmissionAlphaMultiplyMode":"_ColorBlendAlphaMultiplyMode");
        }
        [TestCase(0,TestName="G4Overlay_CPU_Emission_UVAndWord3CD")]
        [TestCase(1,TestName="G4Overlay_CPU_ColorBlend_UVAndWord3CD")]
        public void UVAndCD(int layer)
        {
            var m=Material();var root=Root(m);var h=Host(root,layer,out var item);h.Draw=null;var sync=Property(root,"SyncService");var before=Snapshot.Read(m);
            int pos=layer==0?12:18;string fold=layer==0?"_EmissionUVModeFoldOut":"_ColorBlendUVModeFoldOut";
            var mode=Enum.ToObject(TypeOf("NBShader.NBShaderFlags").GetNestedType("UVMode",All),6);Assert.That(Call(sync,"TryApplyGraphOverlayUVMode",pos,mode,fold,true),Is.True);
            Type component=TypeOf("NBShader.NBShaderFlags").GetNestedType("CutomDataComponent",All);Assert.That(Call(sync,"TryApplyGraphOverlayCustomData",layer==0?16:24,3,Enum.Parse(component,"CustomData1X")),Is.True);
            var flags=Activator.CreateInstance(TypeOf("NBShader.NBShaderFlags"),new object[]{m});Assert.That(Convert.ToInt32(Call(flags,"GetUVMode",pos,0)),Is.EqualTo(6));Assert.That(Call(flags,"GetCustomDataFlag",layer==0?16:24,3).ToString(),Is.EqualTo("CustomData1X"));
            before.AssertSame(m,"UV slice and word3 nibble only, other raw data/state untouched",layer==0?"_NB_UVModeFlag0Lo16":"_NB_UVModeFlag0Hi16",layer==0?"_NB_UVModeFlagType0Lo16":"_NB_UVModeFlagType0Hi16","_NB_CustomDataFlag3Hi16","_NB_Flags1Hi16",fold);
            var after=Snapshot.Read(m);Assert.That(Call(sync,"TryApplyGraphOverlayUVMode",pos,mode,fold,true),Is.True);Assert.That(Call(sync,"TryApplyGraphOverlayCustomData",layer==0?16:24,3,Enum.Parse(component,"CustomData1X")),Is.True);after.AssertSame(m,"Repeat UV/CD owns no extra stored data");
        }
        [TestCase(0,true,TestName="G4Overlay_GPU_Emission_Forward_ortho")]
        [TestCase(0,false,TestName="G4Overlay_GPU_Emission_Forward_perspective")]
        [TestCase(1,true,TestName="G4Overlay_GPU_ColorBlend_Forward_ortho")]
        [TestCase(1,false,TestName="G4Overlay_GPU_ColorBlend_Forward_perspective")]
        public void ForwardDenyRestore(int layer,bool ortho)
        {
            string id="overlay-tier-"+layer+(ortho?"-ortho":"-perspective");using(var h=new G4SpecDebugFixture.Harness(id,ortho))
            {
                var empty=h.Snap("empty");var map=new Texture2D(1,1,TextureFormat.RGBAFloat,false,true){hideFlags=HideFlags.HideAndDontSave};owned.Add(map);map.SetPixel(0,0,new Color(.2f,.35f,.5f,.8f));map.Apply(false);
                for(int i=0;i<3;++i)
                {
                    typeof(G4GraphTextureNoiseTests).GetMethod("Configure",All).Invoke(null,new object[]{h.materials[i],i==2,layer==0?"emission":"overlay2",map,Texture2D.blackTexture,Texture2D.whiteTexture});
                    h.materials[i].SetTexture("_BaseMap",Texture2D.whiteTexture);h.materials[i].SetFloat("_noisemapEnabled",0);G4SpecDebugFixture.SetKeyword(h.materials[i],"_NOISEMAP",false);
                    if(i==2)G4SpecDebugFixture.Validate(h.materials[i]);G4SpecDebugFixture.Harness.RestoreForward(h.materials[i],i==2);
                }
                bool changed;Assert.That(Apply(h.materials[2],true,out changed),Is.True);var intent=Snapshot.Read(h.materials[2]);var frames=new Color[3][][];var repeats=new Color[3][][];
                for(int s=0;s<3;++s)
                {
                    Assert.That(Apply(h.materials[2],s!=1,out changed),Is.True);intent.AssertSame(h.materials[2],"Policy preserves saved intent",Gates);frames[s]=new Color[3][];repeats[s]=new Color[3][];
                    for(int i=0;i<3;++i){if(i!=2)G4SpecDebugFixture.SetKeyword(h.materials[i],layer==0?"_EMISSION":"_COLORMAPBLEND",s!=1);frames[s][i]=h.Snap("ABC"[i]+"-"+s,h.materials[i]);repeats[s][i]=h.Snap("ABC"[i]+"-"+s+"-repeat",h.materials[i]);}
                }
                var metrics=new G4SpecDebugFixture.Metrics{caseId=id,scope="Two actual Overlay consumers; same shared arithmetic, Forward finite/visible/repeat/strict0 strong denial/restoration, no fullUVP/Pass/VFX claim",finite=G4SpecDebugFixture.Finite(empty)&&frames.SelectMany(f=>f).Concat(repeats.SelectMany(f=>f)).All(G4SpecDebugFixture.Finite),ab=frames.Select(f=>G4SpecDebugFixture.Delta(f[0],f[1])).ToArray(),bc=frames.Select(f=>G4SpecDebugFixture.Delta(f[1],f[2])).ToArray(),repeat=Enumerable.Range(0,3).SelectMany(s=>Enumerable.Range(0,3).Select(i=>G4SpecDebugFixture.Delta(frames[s][i],repeats[s][i]))).ToArray(),response=Enumerable.Range(0,3).Select(i=>G4SpecDebugFixture.Delta(frames[0][i],frames[1][i])).ToArray(),restore=Enumerable.Range(0,3).Select(i=>G4SpecDebugFixture.Delta(frames[0][i],frames[2][i])).ToArray(),visible=frames.SelectMany(f=>f).Select(f=>G4SpecDebugFixture.Visible(f,empty)).ToArray()};h.SaveAndAssert(metrics);Assert.That(metrics.response.All(v=>v>.01f),Is.True);intent.AssertSame(h.materials[2],"Complete source intent and derived restore");
            }
        }
    }
}
