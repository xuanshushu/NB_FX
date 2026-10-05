using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
namespace NBFX.Baseline.Tests
{
    // CPU/original control events only. Particle CA GPU incidents remain open.
    public sealed class G4ChromaticSharedGUITests
    {
        const BindingFlags All=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
        readonly G4BackFirstSharedLifecycleTests roots=new G4BackFirstSharedLifecycleTests();
        readonly List<Object> owned=new List<Object>();
        static Type Find(string n)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(n,false)).First(t=>t!=null);
        static object Call(object target,string name,params object[] args)
        {
            var type=target is Type t?t:target.GetType();var method=type.GetMethod(name,All);Assert.That(method,Is.Not.Null);
            try{return method.Invoke(target is Type?null:target,args);}catch(TargetInvocationException e){ExceptionDispatchInfo.Capture(e.InnerException??e).Throw();throw;}
        }
        static object Value(object target,string name){var f=target.GetType().GetField(name,All);return f!=null?f.GetValue(target):target.GetType().GetProperty(name,All).GetValue(target);}
        static string Snapshot(Material m)=>EditorJsonUtility.ToJson(m);
        static void UnrelatedManualState(Material m)
        {
            m.SetFloat("_NB_TierAllowMask",.25f);m.SetFloat("_NB_TierAllowNoise",.75f);m.SetFloat("_NB_TierAllowLighting",.5f);
            var declared=(string[])Find("NBShaders2.Editor.FeatureLevel.NBShaderFeatureLevelMaterialApplier").GetField("GraphDeclaredKeywordNames",All).GetValue(null);
            Assert.That(declared.Length,Is.EqualTo(9));foreach(string keyword in declared)m.EnableKeyword(keyword);
            m.EnableKeyword("_OVERRIDE_Z");
        }

        object Root(params Material[] m)=>Call(roots,"Root",(object)m);
        Material New()
        {
            var s=AssetDatabase.LoadAssetAtPath<Shader>("Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBShaderGraph.shadergraph");Assert.That(s&&s.isSupported,Is.True);
            var m=new Material(s){hideFlags=HideFlags.HideAndDontSave};owned.Add(m);m.SetFloat("_NBShaderFeatureTier",3);m.SetFloat("_Surface",1);m.SetFloat("_AlphaClip",0);
            m.SetFloat("_Distortion_Choraticaberrat_Toggle",1);m.SetFloat("_noisemapEnabled",1);
            Assert.That(Call(Find("NBShaderEditor.NBShaderSyncService"),"TryInitializeGraphSupportedGateTierOnAssign",m),Is.True);return m;
        }
        static IEnumerable<object> Descendants(object parent)
        {foreach(object c in (IEnumerable)Value(parent,"ChildrenItemList")){yield return c;foreach(var n in Descendants(c))yield return n;}}
        NBFXMainTexGUIEventHost Host(Material[] m,string property,out object root,out object item)
        {
            var h=ScriptableObject.CreateInstance<NBFXMainTexGUIEventHost>();owned.Add(h);h.hideFlags=HideFlags.HideAndDontSave;h.position=new Rect(20,20,640,480);h.SetupCommand="NBFX_CA_SHARED_"+Guid.NewGuid().ToString("N");object r=null,p=null;
            h.Setup=()=>{r=Root(m);Assert.That(Call(r,"InitializeGraphChromaticInputs"),Is.True);var leaf=Value(r,"_graphChromaticItem");p=property=="_Distortion_Choraticaberrat_Toggle"?leaf:Descendants(leaf).Single(c=>(string)Value(c,"PropertyName")==property);};
            h.ShowUtility();h.SendEvent(new Event{type=EventType.ExecuteCommand,commandName=h.SetupCommand});Check(h);Assert.That(h.Initialized,Is.True);root=r;item=p;h.Draw=()=>Call(r,"DrawGraphChromaticInputs",p);return h;
        }
        static void Check(NBFXMainTexGUIEventHost h){if(h.Failure!=null)ExceptionDispatchInfo.Capture(h.Failure).Throw();}
        static void Send(NBFXMainTexGUIEventHost h,Event e){var k=e.rawType;h.Counts.TryGetValue(k,out int before);h.SendEvent(e);Check(h);Assert.That(h.Counts.TryGetValue(k,out int after)&&after>before,Is.True);}
        [TearDown]public void Cleanup(){foreach(var h in owned.OfType<NBFXMainTexGUIEventHost>()){h.Draw=null;h.Setup=null;h.Close();}roots.Cleanup();foreach(var o in owned.AsEnumerable().Reverse())if(o)Object.DestroyImmediate(o);owned.Clear();}

        [Test]public void G4CAShared_ActualParentToggleUndo()
        {
            var m=New();UnrelatedManualState(m);var h=Host(new[]{m},"_Distortion_Choraticaberrat_Toggle",out var root,out var item);string before=Snapshot(m);Undo.FlushUndoRecordObjects();Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            try{Send(h,new Event{type=EventType.Layout});Send(h,new Event{type=EventType.Repaint});var r=(Rect)Value(item,"ControlRect");Send(h,new Event{type=EventType.MouseDown,button=0,mousePosition=r.center});Send(h,new Event{type=EventType.MouseUp,button=0,mousePosition=r.center});Assert.That(m.GetFloat("_Distortion_Choraticaberrat_Toggle"),Is.Zero);Assert.That(m.GetFloat("_NB_TierAllowChromaticAberration"),Is.Zero);Assert.That(m.GetFloat("_NB_TierAllowMask"),Is.EqualTo(.25f));Assert.That(m.GetFloat("_NB_TierAllowNoise"),Is.EqualTo(.75f));Assert.That(m.GetFloat("_NB_TierAllowLighting"),Is.EqualTo(.5f));Assert.That(m.IsKeywordEnabled("_OVERRIDE_Z"),Is.True);Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(group);string after=Snapshot(m);h.Draw=null;Undo.PerformUndo();Assert.That(Snapshot(m),Is.EqualTo(before));Undo.PerformRedo();Assert.That(Snapshot(m),Is.EqualTo(after));
                // Existing parent Reset event; preserve the original toggle/Undo assertions above.
                var sync=Value(root,"SyncService");
                m.SetFloat("_NB_Flags0Lo16",64);m.SetFloat("_NB_Flags0Hi16",65536.25f);
                m.SetFloat("_NB_CustomDataFlag0Hi16",15);m.SetFloat("_NB_CustomDataFlag0Lo16",65536.25f);
                m.SetVector("_DistortionDirection",new Vector4(1,2,.3f,4));
                Assert.That(Call(sync,"TryApplyGraphChromaticToggle",true),Is.True);
                Assert.That(Call(sync,"TryApplyGraphChromaticNoiseFlag",true),Is.True);
                var component=Enum.ToObject(Find("NBShader.NBShaderFlags").GetNestedType("CutomDataComponent",All),1);
                Assert.That(Call(sync,"TryApplyGraphChromaticCustomData",component),Is.True);
                Assert.That(Call(sync,"TryApplyGraphChromaticIntensity",.9f),Is.True);
                Assert.That(m.GetFloat("_NB_TierAllowChromaticAberration"),Is.EqualTo(1));
                Assert.That(m.GetFloat("_NB_Flags0Lo16"),Is.EqualTo(66));
                Assert.That(m.GetFloat("_NB_CustomDataFlag0Hi16"),Is.EqualTo(61455));
                Vector4 direction=m.GetVector("_DistortionDirection");
                var snapshotType=typeof(G4GraphGuiFeatureIntentTests).GetNestedType("Snapshot",BindingFlags.NonPublic);
                Assert.That(snapshotType,Is.Not.Null);var ownedSnapshot=Call(snapshotType,"Read",m);
                string beforeReset=Snapshot(m);Undo.FlushUndoRecordObjects();Undo.IncrementCurrentGroup();int resetGroup=Undo.GetCurrentGroup();
                string resetCommand="NBFX_CA_RESET_"+Guid.NewGuid().ToString("N");
                h.Draw=()=>{Call(root,"DrawGraphChromaticInputs",item);if(Event.current.type==EventType.ExecuteCommand&&Event.current.commandName==resetCommand)Call(item,"ExecuteReset",false);};
                Send(h,new Event{type=EventType.ExecuteCommand,commandName=resetCommand});h.Draw=null;
                Assert.That(m.GetFloat("_Distortion_Choraticaberrat_Toggle"),Is.Zero);
                Assert.That(m.GetFloat("_NB_TierAllowChromaticAberration"),Is.Zero);
                Assert.That(m.GetFloat("_NB_Flags0Lo16"),Is.EqualTo(64));
                Assert.That(m.GetFloat("_Distortion_Choraticaberrat_WithNoise_Toggle"),Is.Zero);
                Assert.That(m.GetFloat("_NB_CustomDataFlag0Hi16"),Is.EqualTo(15));
                Vector4 resetDirection=m.GetVector("_DistortionDirection");
                Assert.That(resetDirection.x,Is.EqualTo(direction.x));Assert.That(resetDirection.y,Is.EqualTo(direction.y));Assert.That(resetDirection.w,Is.EqualTo(direction.w));
                Assert.That(resetDirection.z,Is.EqualTo(m.shader.GetPropertyDefaultVectorValue(m.shader.FindPropertyIndex("_DistortionDirection")).z));
                Call(ownedSnapshot,"AssertSame",m,"CA parent Reset owned domain",new[]{"_Distortion_Choraticaberrat_Toggle","_NB_TierAllowChromaticAberration","_NB_Flags0Lo16","_Distortion_Choraticaberrat_WithNoise_Toggle","_NB_CustomDataFlag0Hi16","_DistortionDirection"});
                Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(resetGroup);string afterReset=Snapshot(m);
                Undo.PerformUndo();Assert.That(Snapshot(m),Is.EqualTo(beforeReset));Undo.PerformRedo();Assert.That(Snapshot(m),Is.EqualTo(afterReset));
            }finally{h.Draw=null;Undo.RevertAllDownToGroup(group);}
        }
        [Test]public void G4CAShared_ActualWithNoiseOwnLowBitUndo()
        {
            var m=New();m.SetFloat("_NB_Flags0Lo16",64);m.SetFloat("_NB_Flags0Hi16",65536.25f);m.SetFloat("_Distortion_Choraticaberrat_WithNoise_Toggle",0);
            var h=Host(new[]{m},"_Distortion_Choraticaberrat_WithNoise_Toggle",out _,out var item);string before=Snapshot(m);Undo.FlushUndoRecordObjects();Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            try{Send(h,new Event{type=EventType.Layout});Send(h,new Event{type=EventType.Repaint});var r=(Rect)Value(item,"ControlRect");Send(h,new Event{type=EventType.MouseDown,button=0,mousePosition=r.center});Send(h,new Event{type=EventType.MouseUp,button=0,mousePosition=r.center});Assert.That(m.GetFloat("_NB_Flags0Lo16"),Is.EqualTo(66));Assert.That(m.GetFloat("_NB_Flags0Hi16"),Is.EqualTo(65536.25f));Assert.That(m.GetFloat("_Distortion_Choraticaberrat_WithNoise_Toggle"),Is.EqualTo(1));Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(group);string after=Snapshot(m);h.Draw=null;Undo.PerformUndo();Assert.That(Snapshot(m),Is.EqualTo(before));Undo.PerformRedo();Assert.That(Snapshot(m),Is.EqualTo(after));}finally{h.Draw=null;Undo.RevertAllDownToGroup(group);}
        }
        [Test]public void G4CAShared_IntensityPreservesMixedOtherComponents()
        {
            var a=New();var b=New();a.SetVector("_DistortionDirection",new Vector4(1,2,.3f,4));b.SetVector("_DistortionDirection",new Vector4(5,6,.7f,8));var r=Root(a,b);Assert.That(Call(r,"InitializeGraphChromaticInputs"),Is.True);var sync=Value(r,"SyncService");string sa=Snapshot(a),sb=Snapshot(b);Undo.FlushUndoRecordObjects();Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            try{Assert.That(Call(sync,"TryApplyGraphChromaticIntensity",.9f),Is.True);Assert.That(a.GetVector("_DistortionDirection"),Is.EqualTo(new Vector4(1,2,.9f,4)));Assert.That(b.GetVector("_DistortionDirection"),Is.EqualTo(new Vector4(5,6,.9f,8)));Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(group);string aa=Snapshot(a),bb=Snapshot(b);Assert.That(Call(sync,"TryApplyGraphChromaticIntensity",.9f),Is.True);Assert.That(Snapshot(a),Is.EqualTo(aa));Assert.That(Snapshot(b),Is.EqualTo(bb));Undo.PerformUndo();Assert.That(Snapshot(a),Is.EqualTo(sa));Assert.That(Snapshot(b),Is.EqualTo(sb));Undo.PerformRedo();Assert.That(Snapshot(a),Is.EqualTo(aa));Assert.That(Snapshot(b),Is.EqualTo(bb));}finally{Undo.RevertAllDownToGroup(group);}
        }
        [Test]public void G4CAShared_CustomDataHighNibblePreservesOtherHalf()
        {
            var m=New();m.SetFloat("_NB_CustomDataFlag0Hi16",15);m.SetFloat("_NB_CustomDataFlag0Lo16",65536.25f);var root=Root(m);Assert.That(Call(root,"InitializeGraphChromaticInputs"),Is.True);var sync=Value(root,"SyncService");var component=Enum.ToObject(Find("NBShader.NBShaderFlags").GetNestedType("CutomDataComponent",All),1);string before=Snapshot(m);Undo.FlushUndoRecordObjects();Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            try{Assert.That(Call(sync,"TryApplyGraphChromaticCustomData",component),Is.True);Assert.That(m.GetFloat("_NB_CustomDataFlag0Hi16"),Is.EqualTo(61455));Assert.That(m.GetFloat("_NB_CustomDataFlag0Lo16"),Is.EqualTo(65536.25f));Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(group);string after=Snapshot(m);Undo.PerformUndo();Assert.That(Snapshot(m),Is.EqualTo(before));Undo.PerformRedo();Assert.That(Snapshot(m),Is.EqualTo(after));}finally{Undo.RevertAllDownToGroup(group);}
        }
        [Test]public void G4CAShared_SameEffectiveGateDenyRestoreNoop()
        {
            var m=New();UnrelatedManualState(m);var apply=Find("NBShaders2.Editor.FeatureLevel.NBShaderFeatureLevelMaterialApplier");var tier=Enum.ToObject(Find("NBShader.NBShaderFeatureTier"),3);string before=Snapshot(m);object[] args={m,tier,new string[0],false};Assert.That(Call(apply,"ApplyGraphChromaticGroup",args),Is.True);Assert.That(m.GetFloat("_NB_TierAllowChromaticAberration"),Is.Zero);Assert.That(m.GetFloat("_Distortion_Choraticaberrat_Toggle"),Is.EqualTo(1));args=new object[]{m,tier,new[]{"_CHROMATIC_ABERRATION"},false};Assert.That(Call(apply,"ApplyGraphChromaticGroup",args),Is.True);Assert.That(m.GetFloat("_NB_TierAllowChromaticAberration"),Is.EqualTo(1));Assert.That(Snapshot(m),Is.EqualTo(before));Assert.That(Call(apply,"ApplyGraphChromaticGroup",args),Is.True);Assert.That(Snapshot(m),Is.EqualTo(before));
            m.SetFloat("_NB_TierAllowChromaticAberration",float.NaN);string invalid=Snapshot(m);Assert.That(Call(apply,"ApplyGraphChromaticGroup",args),Is.False);Assert.That(Snapshot(m),Is.EqualTo(invalid));
        }
        [Test]public void G4CAShared_PassiveMixedUnknownSchemaNoWrites()
        {
            var a=New();var b=New();a.SetFloat("_ChromaticAberrationFoldOut",1);b.SetFloat("_ChromaticAberrationFoldOut",1);a.SetVector("_DistortionDirection",new Vector4(1,2,-4,5));b.SetVector("_DistortionDirection",new Vector4(6,7,3,8));var h=Host(new[]{a,b},"_Distortion_Choraticaberrat_Toggle",out _,out _);string sa=Snapshot(a),sb=Snapshot(b);Send(h,new Event{type=EventType.Layout});Send(h,new Event{type=EventType.Repaint});Assert.That(Snapshot(a),Is.EqualTo(sa));Assert.That(Snapshot(b),Is.EqualTo(sb));h.Draw=null;b.SetFloat("_NBShaderFeatureTier",3.5f);var root=Root(a,b);sa=Snapshot(a);sb=Snapshot(b);Assert.That(Call(Value(root,"SyncService"),"TryApplyGraphChromaticToggle",false),Is.False);Assert.That(Snapshot(a),Is.EqualTo(sa));Assert.That(Snapshot(b),Is.EqualTo(sb));
        }
    }
}
