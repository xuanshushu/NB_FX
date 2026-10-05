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
    public sealed class G4GraphColorAdjustmentRampSharedTests
    {
        const BindingFlags All=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance;
        const string RampGate="_NB_TierAllowColorRamp",MapGate="_NB_TierAllowColorRampMap";
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
            return m;
        }
        object Root(params Material[] materials)
        {var helper=new G4GraphPersistentGateTierTests();helpers.Add(helper);return helper.GetType().GetMethod("Root",All).Invoke(helper,new object[]{materials});}
        static IEnumerable<object> Descendants(object item)
        {yield return item;foreach(object child in (System.Collections.IEnumerable)Field(item,"ChildrenItemList"))foreach(var nested in Descendants(child))yield return nested;}
        NBFXMainTexGUIEventHost Host(object root,bool ramp,out object item)
        {
            var h=ScriptableObject.CreateInstance<NBFXMainTexGUIEventHost>();owned.Add(h);
            h.hideFlags=HideFlags.HideAndDontSave;h.position=new Rect(20,20,680,900);
            h.SetupCommand="NBFX_Color_Setup_"+Guid.NewGuid().ToString("N");object actual=null;
            h.Setup=()=>{Assert.That(Call(root,ramp?"InitializeGraphColorRampInputs":"InitializeGraphColorAdjustmentInputs"),Is.True);actual=Field(root,ramp?"_graphColorRampItem":"_graphColorAdjustmentBlock");};
            h.ShowUtility();h.SendEvent(new Event{type=EventType.ExecuteCommand,commandName=h.SetupCommand});Check(h);Assert.That(h.Initialized,Is.True);
            item=actual;h.Draw=()=>Call(root,ramp?"DrawGraphColorRampInputs":"DrawGraphColorAdjustmentInputs",new object[]{null});return h;
        }
        static void Check(NBFXMainTexGUIEventHost h){if(h.Failure!=null)ExceptionDispatchInfo.Capture(h.Failure).Throw();}
        static void Send(NBFXMainTexGUIEventHost h,Event e)
        {
            var type=e.rawType;h.Counts.TryGetValue(type,out int before);h.SendEvent(e);Check(h);
            Assert.That(e.rawType,Is.EqualTo(type));Assert.That(h.Counts.TryGetValue(type,out int after)&&after>before,Is.True,"Actual original Root receives event.");
        }
        static string[] Raw()=>(string[])TypeOf("NBShader.NBShaderFeatureCatalog").GetField("RawKeywords",All).GetValue(null);
        static bool Apply(Material m,int stage,out bool changed)
        {
            var allowed=Raw().Where(k=>stage!=1||k!="_COLOR_RAMP_MAP").Where(k=>stage!=2||k!="_COLOR_RAMP").ToArray();
            object[] args={m,Enum.ToObject(TypeOf("NBShader.NBShaderFeatureTier"),3),allowed,false};
            bool result=(bool)TypeOf("NBShaders2.Editor.FeatureLevel.NBShaderFeatureLevelMaterialApplier").GetMethod("ApplyGraphColorRampGroup",All).Invoke(null,args);changed=(bool)args[3];return result;
        }
        [TearDown] public void Cleanup()
        {
            foreach(var h in owned.OfType<NBFXMainTexGUIEventHost>()){h.Draw=null;h.Setup=null;h.Close();}
            foreach(var helper in helpers)helper.Cleanup();helpers.Clear();
            foreach(var o in owned.AsEnumerable().Reverse())if(o)Object.DestroyImmediate(o);owned.Clear();
        }
        [Test] public void G4ColorShared_CPU_OriginalTypesAndMixedPassive()
        {
            var a=Material();var b=Material();
            foreach(var m in new[]{a,b})foreach(string fold in new[]{"_BaseColorAdjustmentFoldOut","_HueShiftFoldOut","_SaturabilityFoldOut","_ContrastFoldOut","_BaseMapColorRefineFoldOut","_RampColorBlockFoldOut"})m.SetFloat(fold,1);
            a.SetFloat("_HueShift",.125f);b.SetFloat("_HueShift",.75f);a.SetFloat("_HueShift_Toggle",1);a.SetFloat("_NB_Flags0Hi16",8);
            a.SetFloat("_RampColorToggle",1);b.SetFloat("_RampColorToggle",0);
            var root=Root(a,b);var beforeA=Snapshot.Read(a);var beforeB=Snapshot.Read(b);
            var h=Host(root,false,out var block);Assert.That(block.GetType().Name,Is.EqualTo("BlockItem"));
            Assert.That(Descendants(block).Count(o=>o.GetType().Name=="CustomDataSelectItem"),Is.EqualTo(3),"Original CD leaves remain present with original Particle visibility.");
            foreach(var slider in Descendants(block).Where(o=>o.GetType().Name=="ShaderGUISliderItem"))Assert.That((bool)Field(slider,"WriteOnlyOnInteractiveChange"),Is.True);
            Send(h,new Event{type=EventType.Layout});Send(h,new Event{type=EventType.Repaint});h.Draw=null;
            var rh=Host(root,true,out var ramp);Assert.That(ramp.GetType().Name,Is.EqualTo("RampColorFeatureItem"));
            var uv=Descendants(ramp).Single(o=>o.GetType().Name=="UVModeSelectItem");Assert.That((int)Field(uv,"_uvModeBitPos"),Is.EqualTo(26));
            Assert.That(Descendants(ramp).Single(o=>o.GetType().Name=="GradientItem"),Is.Not.Null);
            Send(rh,new Event{type=EventType.Layout});Send(rh,new Event{type=EventType.Repaint});rh.Draw=null;
            beforeA.AssertSame(a,"Mixed passive paint must retain every A field, keyword and pass.");beforeB.AssertSame(b,"Mixed passive paint must retain every B field, keyword and pass.");
            foreach(string name in new[]{"_RampColorCount",RampGate,MapGate,"_BaseColorAdjustmentFoldOut","_RampColorBlockFoldOut","_RampColorUVModeFoldOut"})Assert.That(a.shader.GetPropertyType(a.shader.FindPropertyIndex(name)),Is.EqualTo(ShaderPropertyType.Float),name);
            Assert.That(a.shader.GetPropertyDefaultVectorValue(a.shader.FindPropertyIndex("SaturabilityRangeVec")),Is.EqualTo(new Vector4(0,1,0,0)));
            Assert.That(a.shader.GetPropertyDefaultFloatValue(a.shader.FindPropertyIndex(RampGate)),Is.EqualTo(1));Assert.That(a.shader.GetPropertyDefaultFloatValue(a.shader.FindPropertyIndex(MapGate)),Is.EqualTo(1));
        }
        [Test] public void G4ColorShared_CPU_RawFlagsUV26UndoRedo()
        {
            var m=Material();var root=Root(m);Assert.That(Call(root,"InitializeGraphColorAdjustmentInputs"),Is.True);Assert.That(Call(root,"InitializeGraphColorRampInputs"),Is.True);
            var sync=Property(root,"SyncService");var before=Snapshot.Read(m);Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            try
            {
                foreach(int bit in new[]{1,1<<19,1<<22,1<<29})Assert.That(Call(sync,"TryApplyGraphColorAdjustmentFlagEdit",bit,0,true),Is.True);
                foreach(int bit in new[]{1<<24,1<<27})Assert.That(Call(sync,"TryApplyGraphColorAdjustmentFlagEdit",bit,1,true),Is.True);
                Assert.That(Call(sync,"TryApplyGraphColorRampBlendEdit",true),Is.True);
                object mode=Enum.ToObject(TypeOf("NBShader.NBShaderFlags+UVMode"),6);
                Assert.That(Call(sync,"TryApplyGraphColorRampUVMode",mode,true),Is.True);
                Assert.That((Mathf.RoundToInt(m.GetFloat("_NB_UVModeFlag0Hi16"))>>10)&3,Is.EqualTo(2));
                Assert.That((Mathf.RoundToInt(m.GetFloat("_NB_UVModeFlagType0Hi16"))>>10)&3,Is.EqualTo(1));
                foreach(string name in new[]{"_ColorAdjustmentOnlyAffectMainTex","_HueShift_Toggle","_ChangeSaturability_Toggle","_Contrast_Toggle","_BaseMapColorRefine_Toggle","_ColorMultiAlpha","_RampColorBlendMode"})Assert.That(m.GetFloat(name),Is.EqualTo(1),name);
                Assert.That(m.GetFloat("_NB_UVModeFlag0Lo16"),Is.EqualTo(17.25f));Assert.That(m.GetFloat("_NB_CustomDataFlag3Hi16"),Is.EqualTo(123.25f));
                Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(group);var after=Snapshot.Read(m);Undo.PerformUndo();before.AssertSame(m,"Owned flags+UV transaction exact undo.");Undo.PerformRedo();after.AssertSame(m,"Owned flags+UV transaction exact redo.");
                var noOp=Snapshot.Read(m);Assert.That(Call(sync,"TryApplyGraphColorAdjustmentFlagEdit",1<<19,0,true),Is.True);noOp.AssertSame(m,"Repeated flag edit no-op.");
            }
            finally{Undo.RevertAllDownToGroup(group);}
        }
        [Test] public void G4ColorShared_CPU_RampParentMapDenyRestore()
        {
            var m=Material();m.SetFloat("_RampColorToggle",1);m.SetFloat("_RampColorSourceMode",1);bool changed;
            Assert.That(Apply(m,0,out changed),Is.True);var before=Snapshot.Read(m);
            Assert.That(Apply(m,1,out changed),Is.True);Assert.That(m.GetFloat(RampGate),Is.EqualTo(1));Assert.That(m.GetFloat(MapGate),Is.EqualTo(0));Assert.That(m.GetFloat("_RampColorSourceMode"),Is.EqualTo(1));before.AssertSame(m,"Map denied preserves raw source intent.",MapGate);
            Assert.That(Apply(m,2,out changed),Is.True);Assert.That(m.GetFloat(RampGate),Is.EqualTo(0));Assert.That(m.GetFloat(MapGate),Is.EqualTo(0),"Parent removes child.");before.AssertSame(m,"Parent denied affects only two derived gates.",RampGate,MapGate);
            Assert.That(Apply(m,0,out changed),Is.True);before.AssertSame(m,"Exact restore.");Assert.That(Apply(m,0,out changed),Is.True);Assert.That(changed,Is.False);
        }
        [TestCase(false,TestName="G4ColorShared_GUI_HueToggleUndoRedo")]
        [TestCase(true,TestName="G4ColorShared_GUI_RampToggleUndoRedo")]
        public void ActualToggle(bool ramp)
        {
            var m=Material();m.SetFloat("_BaseColorAdjustmentFoldOut",1);m.SetFloat("_HueShiftFoldOut",0);
            var root=Root(m);var h=Host(root,ramp,out var block);Send(h,new Event{type=EventType.Layout});Send(h,new Event{type=EventType.Repaint});
            object item=ramp?block:Descendants(block).Single(o=>o.GetType().Name=="PropertyToggleBlockItem"&&(string)Field(o,"PropertyName")=="_HueShift_Toggle");
            var rect=(Rect)Field(item,"ControlRect");Assert.That(rect.width>0&&rect.height>0,Is.True);var pos=new Vector2(rect.x+6,rect.center.y);var before=Snapshot.Read(m);Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            try
            {
                Send(h,new Event{type=EventType.MouseDown,button=0,mousePosition=pos});Send(h,new Event{type=EventType.MouseUp,button=0,mousePosition=pos});
                Assert.That(m.GetFloat(ramp?"_RampColorToggle":"_HueShift_Toggle"),Is.EqualTo(1));
                if(ramp){Assert.That(m.GetFloat(RampGate),Is.EqualTo(1));Assert.That(m.GetFloat(MapGate),Is.EqualTo(0));}
                else Assert.That(Mathf.RoundToInt(m.GetFloat("_NB_Flags0Hi16"))&8,Is.EqualTo(8));
                Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(group);var after=Snapshot.Read(m);h.Draw=null;Undo.PerformUndo();before.AssertSame(m,"Actual Root-owned click undo.");Undo.PerformRedo();after.AssertSame(m,"Actual Root-owned click redo.");
            }
            finally{h.Draw=null;Undo.RevertAllDownToGroup(group);}
        }
        [TestCase(false,TestName="G4ColorShared_GPU_AdjustmentFlag_ortho")]
        [TestCase(true,TestName="G4ColorShared_GPU_RampMapFallback_ortho")]
        public void ActualConsumer(bool ramp)
        {
            string id=ramp?"color-ramp-map-fallback":"color-adjustment-flag";
            using(var h=new G4SpecDebugFixture.Harness(id,true))
            {
                var empty=h.Snap("empty");var map=h.Constant(new Color(.8f,.2f,.1f,1));
                for(int i=0;i<3;++i)
                {
                    var m=h.materials[i];m.SetFloat("_Contrast",.25f);m.SetColor("_ContrastMidColor",Color.white);
                    if(ramp)
                    {
                        m.SetFloat("_RampColorToggle",1);m.SetFloat("_RampColorSourceMode",1);m.SetTexture("_RampColorMap",map);
                        m.SetColor("_RampColor0",new Color(1,0,0,0));m.SetColor("_RampColor1",new Color(0,1,0,1));m.SetVector("_RampColorAlpha0",new Vector4(1,0,1,1));m.SetColor("_RampColorBlendColor",Color.white);
                        if(i==2)m.SetFloat("_RampColorCount",131074);else m.SetInteger("_RampColorCount",131074);
                    }
                    G4SpecDebugFixture.Harness.RestoreForward(m,i==2);
                }
                var root=Root(h.materials[2]);Assert.That(Call(root,ramp?"InitializeGraphColorRampInputs":"InitializeGraphColorAdjustmentInputs"),Is.True);var sync=Property(root,"SyncService");
                int count=ramp?4:3;var frames=new Color[count][][];var repeats=new Color[count][][];
                for(int stage=0;stage<count;++stage)
                {
                    bool on=stage!=1;bool changed;
                    if(ramp)Assert.That(Apply(h.materials[2],stage==3?0:stage,out changed),Is.True);
                    else Assert.That(Call(sync,"TryApplyGraphColorAdjustmentFlagEdit",1<<24,1,on),Is.True);
                    frames[stage]=new Color[3][];repeats[stage]=new Color[3][];
                    for(int i=0;i<3;++i)
                    {
                        var m=h.materials[i];
                        if(i!=2)
                        {
                            if(ramp){G4SpecDebugFixture.SetKeyword(m,"_COLOR_RAMP",stage!=2);G4SpecDebugFixture.SetKeyword(m,"_COLOR_RAMP_MAP",stage==0||stage==3);}
                            else{int flags=m.GetInteger("_W9ParticleShaderFlags1");m.SetInteger("_W9ParticleShaderFlags1",on?flags|(1<<24):flags&~(1<<24));}
                        }
                        frames[stage][i]=h.Snap("ABC"[i]+"-"+stage,m);repeats[stage][i]=h.Snap("ABC"[i]+"-"+stage+"-repeat",m);
                    }
                    if(ramp)Assert.That(h.materials[2].GetFloat("_RampColorSourceMode"),Is.EqualTo(1),"GPU fallback must retain serialized Map intent.");
                }
                var metrics=new G4SpecDebugFixture.Metrics{caseId=id,scope="Ordinary orthographic Mesh original shared flags or parent/map Tier fallback; no new popup/Player/VFX claim.",finite=G4SpecDebugFixture.Finite(empty)&&frames.SelectMany(f=>f).Concat(repeats.SelectMany(f=>f)).All(G4SpecDebugFixture.Finite),ab=frames.Select(f=>G4SpecDebugFixture.Delta(f[0],f[1])).ToArray(),bc=frames.Select(f=>G4SpecDebugFixture.Delta(f[1],f[2])).ToArray(),repeat=Enumerable.Range(0,count).SelectMany(s=>Enumerable.Range(0,3).Select(i=>G4SpecDebugFixture.Delta(frames[s][i],repeats[s][i]))).ToArray(),response=Enumerable.Range(0,3).Select(i=>G4SpecDebugFixture.Delta(frames[0][i],frames[1][i])).ToArray(),restore=Enumerable.Range(0,3).Select(i=>G4SpecDebugFixture.Delta(frames[0][i],frames[count-1][i])).ToArray(),visible=frames.SelectMany(f=>f).Select(f=>G4SpecDebugFixture.Visible(f,empty)).ToArray()};
                h.SaveAndAssert(metrics);Assert.That(metrics.response.All(v=>v>.01f),Is.True,"Each ABC control must respond strongly.");
                if(ramp)Assert.That(Enumerable.Range(0,3).All(i=>G4SpecDebugFixture.Delta(frames[1][i],frames[2][i])>.01f),Is.True,"Gradient fallback also remains a real active effect.");
            }
        }
    }
}
