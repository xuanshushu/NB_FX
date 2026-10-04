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
    public sealed class G4FresnelSharedGUIServiceTests
    {
        const BindingFlags All=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
        readonly List<Object> owned=new List<Object>();
        static Type Find(string name)=>G4SpecDebugFixture.FindType(name);
        static object Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name,All).Invoke(target,args);
        static object Value(object target,string name)
        {var f=target.GetType().GetField(name,All);return f!=null?f.GetValue(target):target.GetType().GetProperty(name,All).GetValue(target);}
        Material New()
        {
            var m=G4SpecDebugFixture.NewGraph();owned.Add(m);m.SetFloat("_fresnelEnabled",1);m.SetFloat("_NBShaderFeatureTier",3);
            m.SetFloat("_FresnelMode",0);m.SetFloat("_InvertFresnel_Toggle",0);m.SetFloat("_FresnelColorAffectByAlpha",0);
            m.SetFloat("_NB_Flags0Lo16",130);m.SetFloat("_NB_Flags0Hi16",137);
            return m;
        }
        object Root(params Material[] materials)
        {
            var root=Activator.CreateInstance(Find("NBShaderEditor.NBShaderRootItem"));var type=root.GetType();
            var editor=(MaterialEditor)Editor.CreateEditor(materials.Cast<Object>().ToArray(),typeof(MaterialEditor));owned.Add(editor);
            type.GetField("MatEditor",All).SetValue(root,editor);type.GetField("Mats",All).SetValue(root,materials.ToList());type.GetField("Shader",All).SetValue(root,materials[0].shader);
            Call(root,"InitFlags",materials.ToList());var dictionary=(IDictionary)Value(root,"PropertyInfoDic");
            foreach(var p in MaterialEditor.GetMaterialProperties(materials.Cast<Object>().ToArray()))
            {var info=Activator.CreateInstance(Find("NBShaderEditor.ShaderPropertyInfo"));info.GetType().GetField("Property").SetValue(info,p);info.GetType().GetField("Name").SetValue(info,p.name);info.GetType().GetField("Index").SetValue(info,materials[0].shader.FindPropertyIndex(p.name));dictionary.Add(p.name,info);}
            return root;
        }
        NBFXMainTexGUIEventHost Host(Material[] materials,string property,out object root,out object item)
        {
            var h=ScriptableObject.CreateInstance<NBFXMainTexGUIEventHost>();owned.Add(h);h.hideFlags=HideFlags.HideAndDontSave;h.position=new Rect(20,20,640,480);h.SetupCommand="NBFX_Fresnel_"+Guid.NewGuid().ToString("N");
            object r=null,p=null;h.Setup=()=>
            {
                r=Root(materials);
                Assert.That((bool)Call(r,"InitializeGraphMainTextureInputs"),Is.True);
                Assert.That((bool)Call(r,"InitializeGraphFresnelInputs"),Is.True);
                var leaf=Value(r,"_graphFresnelItem");
                p=property=="_fresnelEnabled"?leaf:((IEnumerable)Value(leaf,"ChildrenItemList")).Cast<object>().Single(c=>(string)Value(c,"PropertyName")==property);
            };
            h.ShowUtility();h.SendEvent(new Event{type=EventType.ExecuteCommand,commandName=h.SetupCommand});Check(h);Assert.That(h.Initialized,Is.True);
            root=r;item=p;h.Draw=()=>Call(p,"OnGUI");return h;
        }
        static void Check(NBFXMainTexGUIEventHost h){if(h.Failure!=null)ExceptionDispatchInfo.Capture(h.Failure).Throw();}
        static void Send(NBFXMainTexGUIEventHost h,Event e)
        {var type=e.rawType;h.Counts.TryGetValue(type,out int before);h.SendEvent(e);Check(h);Assert.That(h.Counts.TryGetValue(type,out int after)&&after>before,Is.True,"Original OnGUI must receive actual native event.");}
        static string Snapshot(Material m)=>EditorJsonUtility.ToJson(m)+"|"+string.Join("|",m.shaderKeywords.OrderBy(x=>x));
        static int Bit(string name)=>(int)Find("NBShader.NBShaderFlags").GetField(name,All).GetValue(null);
        static void AssertWords(Material m,int lo,int hi){Assert.That(m.GetFloat("_NB_Flags0Lo16"),Is.EqualTo(lo));Assert.That(m.GetFloat("_NB_Flags0Hi16"),Is.EqualTo(hi));}
        [OneTimeSetUp]public void Preflight()=>G4SpecDebugFixture.PreflightImport();
        [TearDown]public void Cleanup(){foreach(var h in owned.OfType<NBFXMainTexGUIEventHost>()){h.Draw=null;h.Setup=null;h.Close();}foreach(var o in owned.AsEnumerable().Reverse())if(o)Object.DestroyImmediate(o);owned.Clear();}

        [TestCase("FLAG_BIT_PARTICLE_FRESNEL_INVERT_ON","_InvertFresnel_Toggle",TestName="G4Fresnel_Service_InvertHi16_RawMirrorNeighborsNoop")]
        [TestCase("FLAG_BIT_PARTICLE_FRESNEL_FADE_ON","_FresnelMode",TestName="G4Fresnel_Service_FadeLo16_RawMirrorNeighborsNoop")]
        [TestCase("FLAG_BIT_PARTICLE_FRESNEL_COLOR_AFFETCT_BY_ALPHA","_FresnelColorAffectByAlpha",TestName="G4Fresnel_Service_AlphaLo16_RawMirrorNeighborsNoop")]
        public void DirectService(string name,string mirror)
        {
            var m=New();var h=Host(new[]{m},"_fresnelEnabled",out var root,out var item);h.Draw=null;var sync=Value(root,"SyncService");int bit=Bit(name);
            int lo=(int)m.GetFloat("_NB_Flags0Lo16"),hi=(int)m.GetFloat("_NB_Flags0Hi16");Assert.That(m.GetFloat(mirror),Is.Zero);
            Assert.That((bool)Call(sync,"TryApplyGraphFresnelFlagEdit",bit,true),Is.True);
            AssertWords(m,lo|(bit&65535),hi|(int)((uint)bit>>16));Assert.That(m.GetFloat(mirror),Is.EqualTo(1));
            string changed=Snapshot(m);Assert.That((bool)Call(sync,"TryApplyGraphFresnelFlagEdit",bit,true),Is.True);Assert.That(Snapshot(m),Is.EqualTo(changed));
            Assert.That((bool)Call(sync,"TryApplyGraphFresnelFlagEdit",bit,false),Is.True);AssertWords(m,lo,hi);Assert.That(m.GetFloat(mirror),Is.Zero);
        }
        [TestCase("_InvertFresnel_Toggle","FLAG_BIT_PARTICLE_FRESNEL_INVERT_ON",TestName="G4Fresnel_ActualInvertToggle_Hi16MixedUndoRedo")]
        [TestCase("_FresnelColorAffectByAlpha","FLAG_BIT_PARTICLE_FRESNEL_COLOR_AFFETCT_BY_ALPHA",TestName="G4Fresnel_ActualAlphaToggle_Lo16MixedUndoRedo")]
        public void ActualToggle(string property,string name)
        {
            var a=New();var b=New();b.SetFloat("_NB_Flags0Lo16",258);b.SetFloat("_NB_Flags0Hi16",73);
            var h=Host(new[]{a,b},property,out var root,out var item);int bit=Bit(name);
            int alo=(int)a.GetFloat("_NB_Flags0Lo16"),ahi=(int)a.GetFloat("_NB_Flags0Hi16"),blo=(int)b.GetFloat("_NB_Flags0Lo16"),bhi=(int)b.GetFloat("_NB_Flags0Hi16");
            string beforeA=Snapshot(a),beforeB=Snapshot(b);Undo.FlushUndoRecordObjects();Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            try
            {
                Send(h,new Event{type=EventType.Layout});Send(h,new Event{type=EventType.Repaint});var rect=(Rect)Value(item,"ControlRect");
                Send(h,new Event{type=EventType.MouseDown,button=0,mousePosition=rect.center});Send(h,new Event{type=EventType.MouseUp,button=0,mousePosition=rect.center});
                Assert.That(a.GetFloat(property),Is.EqualTo(1));Assert.That(b.GetFloat(property),Is.EqualTo(1));
                AssertWords(a,alo|(bit&65535),ahi|(int)((uint)bit>>16));AssertWords(b,blo|(bit&65535),bhi|(int)((uint)bit>>16));
                Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(group);string afterA=Snapshot(a),afterB=Snapshot(b);h.Draw=null;
                Undo.PerformUndo();Assert.That(Snapshot(a),Is.EqualTo(beforeA));Assert.That(Snapshot(b),Is.EqualTo(beforeB));Undo.PerformRedo();Assert.That(Snapshot(a),Is.EqualTo(afterA));Assert.That(Snapshot(b),Is.EqualTo(afterB));
            }
            finally{h.Draw=null;Undo.RevertAllDownToGroup(group);}
        }
        [Test]public void G4Fresnel_Marker2_ActualLayoutRepaint_AllStateReadOnly()
        {var m=New();var h=Host(new[]{m},"_fresnelEnabled",out var root,out var item);string before=Snapshot(m);Send(h,new Event{type=EventType.Layout});Send(h,new Event{type=EventType.Repaint});Assert.That(Snapshot(m),Is.EqualTo(before));}
    }
}
