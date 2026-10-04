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
    // Existing original primitives on a real NB material and native event host.
    // This isolates the opt-in writer boundary; no Root/Graph block Undo claim.
    public sealed class G4SharedSliderPassiveWriteTests
    {
        const BindingFlags All=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
        readonly List<Object> owned=new List<Object>();
        readonly List<G4GraphPersistentGateTierTests> helpers=new List<G4GraphPersistentGateTierTests>();
        static Type TypeOf(string name)=>G4SpecDebugFixture.FindType(name);
        static object Call(object o,string name,params object[] args)=>o.GetType().GetMethod(name,All).Invoke(o,args);
        static object Field(object o,string name)
        {for(Type t=o.GetType();t!=null;t=t.BaseType){var f=t.GetField(name,All|BindingFlags.DeclaredOnly);if(f!=null)return f.GetValue(o);}Assert.Fail("Missing real field "+name);return null;}
        [OneTimeSetUp] public void Preflight()
        {
            Assert.That(Path.GetFullPath(Application.dataPath),Is.EqualTo(Path.GetFullPath(@"D:\UnityProject\NBUnityProject\.utmp\NBFXMeshValidation-20261002\Assets")).IgnoreCase);
            for(int i=0;i<SceneManager.sceneCount;++i){var s=SceneManager.GetSceneAt(i);Assert.That((s.name+"/"+s.path).IndexOf("TAI",StringComparison.OrdinalIgnoreCase),Is.LessThan(0));}
        }
        sealed class Snapshot
        {
            readonly object value;static Type Shared=>typeof(G4GraphGuiFeatureIntentTests).GetNestedType("Snapshot",BindingFlags.NonPublic);
            Snapshot(object value){this.value=value;}
            public static Snapshot Read(Material m)=>new Snapshot(Shared.GetMethod("Read",All).Invoke(null,new object[]{m}));
            public void AssertSame(Material m,string label,params string[] allowed)=>Shared.GetMethod("AssertSame",All).Invoke(value,new object[]{m,label,allowed});
        }
        Material Material(bool vector,int id,bool outOfRange)
        {
            var shader=AssetDatabase.LoadAssetAtPath<Shader>("Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/NBShader.shader");Assert.That(shader,Is.Not.Null);
            var m=new Material(shader){hideFlags=HideFlags.HideAndDontSave};owned.Add(m);
            float value=outOfRange?2+id:.125f;var range=new Vector4(0,1+.25f*id,11+id,13+id);
            if(vector){m.SetVector("_Dissolve",new Vector4(value,.375f+id,.625f+id,.875f+id));m.SetVector("DissolveXRangeVec",range);}
            else{m.SetFloat("_ParallaxMapping_Intensity",value);m.SetVector("_ParallaxMapping_IntensityRangeVec",range);}
            return m;
        }
        object Root(params Material[] materials)
        {var helper=new G4GraphPersistentGateTierTests();helpers.Add(helper);return helper.GetType().GetMethod("Root",All).Invoke(helper,new object[]{materials});}
        NBFXMainTexGUIEventHost Host(object root,bool vector,bool optIn,out object actual)
        {
            var h=ScriptableObject.CreateInstance<NBFXMainTexGUIEventHost>();owned.Add(h);h.hideFlags=HideFlags.HideAndDontSave;h.position=new Rect(20,20,640,280);h.SetupCommand="NBFX_SharedSlider_Setup_"+Guid.NewGuid().ToString("N");object item=null;
            h.Setup=()=>{
                if(vector)item=Activator.CreateInstance(TypeOf("NBShaderEditor.VectorComponentRangeSliderItem"),new object[]{root,null,"_Dissolve",0,"DissolveXRangeVec",new Func<GUIContent>(()=>new GUIContent("Actual original range component")),null});
                else
                {
                    item=Activator.CreateInstance(TypeOf("NBShaderEditor.ShaderGUISliderItem"),new object[]{root,null,null});
                    item.GetType().GetField("PropertyName",All).SetValue(item,"_ParallaxMapping_Intensity");
                    item.GetType().GetField("RangePropertyName",All).SetValue(item,"_ParallaxMapping_IntensityRangeVec");
                    item.GetType().GetField("GuiContent",All).SetValue(item,new GUIContent("Actual original float range"));Call(item,"InitTriggerByChild");
                }
                var option=item.GetType().GetField("WriteOnlyOnInteractiveChange",All);Assert.That(option,Is.Not.Null);Assert.That((bool)option.GetValue(item),Is.False,"Unchanged constructor default keeps Native behavior.");if(optIn)option.SetValue(item,true);
            };
            h.ShowUtility();h.SendEvent(new Event{type=EventType.ExecuteCommand,commandName=h.SetupCommand});Check(h);Assert.That(h.Initialized,Is.True);actual=item;h.Draw=()=>Call(item,"OnGUI");return h;
        }
        static void Check(NBFXMainTexGUIEventHost h){if(h.Failure!=null)ExceptionDispatchInfo.Capture(h.Failure).Throw();}
        static void Send(NBFXMainTexGUIEventHost h,Event e)
        {var type=e.rawType;h.Counts.TryGetValue(type,out int before);h.SendEvent(e);Check(h);Assert.That(e.rawType,Is.EqualTo(type));Assert.That(h.Counts.TryGetValue(type,out int after)&&after>before,Is.True,"Actual native primitive must receive event.");}
        [TearDown] public void Cleanup()
        {foreach(var h in owned.OfType<NBFXMainTexGUIEventHost>()){h.Draw=null;h.Setup=null;h.Close();}foreach(var helper in helpers)helper.Cleanup();helpers.Clear();foreach(var o in owned.AsEnumerable().Reverse())if(o)Object.DestroyImmediate(o);owned.Clear();}
        [TestCase(false,false,TestName="G4SharedSlider_Float_OutOfRangePassiveReadOnly")]
        [TestCase(false,true,TestName="G4SharedSlider_Float_MixedOutOfRangePassiveReadOnly")]
        [TestCase(true,false,TestName="G4SharedSlider_Vector_OutOfRangePassiveReadOnly")]
        [TestCase(true,true,TestName="G4SharedSlider_Vector_MixedOutOfRangePassiveReadOnly")]
        public void OptInPassiveReadOnly(bool vector,bool mixed)
        {
            var a=Material(vector,0,true);var b=mixed?Material(vector,1,true):null;var root=mixed?Root(a,b):Root(a);var h=Host(root,vector,true,out var item);var beforeA=Snapshot.Read(a);var beforeB=b?Snapshot.Read(b):null;
            Send(h,new Event{type=EventType.Layout});Send(h,new Event{type=EventType.Repaint});beforeA.AssertSame(a,"Opt-in preserves complete first material during Layout/Repaint, including out-of-range saved value.");if(b)beforeB.AssertSame(b,"Opt-in preserves distinct second material and range during passive mixed selection.");
            var rect=(Rect)Field(item,"ControlRect");Assert.That(rect.width>0&&rect.height>0,Is.True,"Original real control drew.");
        }
        [TestCase(false,TestName="G4SharedSlider_Float_ActualSliderInputWrites")]
        [TestCase(true,TestName="G4SharedSlider_Vector_ActualSliderInputWrites")]
        public void OptInRealInputWrites(bool vector)
        {
            var m=Material(vector,0,false);var root=Root(m);var h=Host(root,vector,true,out var item);Send(h,new Event{type=EventType.Layout});Send(h,new Event{type=EventType.Repaint});var before=Snapshot.Read(m);
            var rect=(Rect)Field(item,"ControlRect");var click=new Vector2(rect.center.x,rect.center.y);Send(h,new Event{type=EventType.MouseDown,button=0,mousePosition=click});Send(h,new Event{type=EventType.MouseUp,button=0,mousePosition=click});
            float value=vector?m.GetVector("_Dissolve").x:m.GetFloat("_ParallaxMapping_Intensity");Assert.That(float.IsNaN(value)||float.IsInfinity(value),Is.False);Assert.That(Mathf.Abs(value-.125f),Is.GreaterThan(.05f),"Real slider event changes its owned saved value.");
            before.AssertSame(m,"Actual input owns only original float or selected vector component.",vector?"_Dissolve":"_ParallaxMapping_Intensity");if(vector)Assert.That(m.GetVector("_Dissolve"),Is.EqualTo(new Vector4(value,.375f,.625f,.875f)));
        }
        [TestCase(false,TestName="G4SharedSlider_Float_DefaultFalsePreservesLegacyClamp")]
        [TestCase(true,TestName="G4SharedSlider_Vector_DefaultFalsePreservesLegacyClamp")]
        public void NativeDefaultUnchanged(bool vector)
        {
            var m=Material(vector,0,true);var root=Root(m);var h=Host(root,vector,false,out var item);Send(h,new Event{type=EventType.Layout});Send(h,new Event{type=EventType.Repaint});
            Assert.That(vector?m.GetVector("_Dissolve").x:m.GetFloat("_ParallaxMapping_Intensity"),Is.EqualTo(1f),"Explicitly retains original Native default-false clamp behavior.");
            Assert.That((bool)item.GetType().GetField("WriteOnlyOnInteractiveChange",All).GetValue(item),Is.False);
        }
    }
}
