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
    public sealed class G4QCMSharedGUITests
    {
        const BindingFlags All=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
        readonly List<Object> owned=new List<Object>();
        static Type Find(string name)=>G4SpecDebugFixture.FindType(name);
        static object Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name,All).Invoke(target,args);
        static object Value(object target,string name)
        {var f=target.GetType().GetField(name,All);return f!=null?f.GetValue(target):target.GetType().GetProperty(name,All).GetValue(target);}
        static IEnumerable<object> Descendants(object item)
        {
            foreach(object child in (IEnumerable)Value(item,"ChildrenItemList"))
            {yield return child;foreach(object nested in Descendants(child))yield return nested;}
        }
        Material New()
        {var m=G4SpecDebugFixture.NewGraph();owned.Add(m);m.SetFloat("_NBShaderFeatureTier",3);return m;}
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
            var h=ScriptableObject.CreateInstance<NBFXMainTexGUIEventHost>();owned.Add(h);h.hideFlags=HideFlags.HideAndDontSave;h.position=new Rect(20,20,640,480);h.SetupCommand="NBFX_QCM_"+Guid.NewGuid().ToString("N");
            object r=null,p=null;h.Setup=()=>
            {
                r=Root(materials);
                Assert.That((bool)Call(r,"InitializeGraphMainTextureInputs"),Is.True);
                Assert.That((bool)Call(r,"InitializeGraphTADepthInputs"),Is.True);
                var leaf=Value(r,"_graphTADepthBlock");
                p=property=="TA"?leaf:Descendants(leaf).Single(c=>(string)Value(c,"PropertyName")==property);
            };
            h.ShowUtility();h.SendEvent(new Event{type=EventType.ExecuteCommand,commandName=h.SetupCommand});Check(h);Assert.That(h.Initialized,Is.True);
            root=r;item=p;h.Draw=()=>Call(r,"DrawGraphTAInputs",p);return h;
        }
        static void Check(NBFXMainTexGUIEventHost h){if(h.Failure!=null)ExceptionDispatchInfo.Capture(h.Failure).Throw();}
        static void Send(NBFXMainTexGUIEventHost h,Event e)
        {var type=e.rawType;h.Counts.TryGetValue(type,out int before);h.SendEvent(e);Check(h);Assert.That(h.Counts.TryGetValue(type,out int after)&&after>before,Is.True,"Original OnGUI must receive actual native event.");}
        static string Snapshot(Material m)=>EditorJsonUtility.ToJson(m)+"|"+string.Join("|",m.shaderKeywords.OrderBy(x=>x));
        static int Bit(string name)=>(int)Find("NBShader.NBShaderFlags").GetField(name,All).GetValue(null);
        static void AssertWords(Material m,int lo,int hi){Assert.That(m.GetFloat("_NB_Flags1Lo16"),Is.EqualTo(lo));Assert.That(m.GetFloat("_NB_Flags1Hi16"),Is.EqualTo(hi));}

        [TearDown]public void Cleanup(){foreach(var h in owned.OfType<NBFXMainTexGUIEventHost>()){h.Draw=null;h.Setup=null;h.Close();}foreach(var o in owned.AsEnumerable().Reverse())if(o)Object.DestroyImmediate(o);owned.Clear();}
        [Test]public void G4QCM_ActualConstructorPaint_MixedManualQueueEnumReadOnly()
        {
            var a=New();var b=New();a.renderQueue=3277;b.renderQueue=3299;
            foreach(var m in new[]{a,b}){m.SetFloat("_QueueControl",1);m.SetFloat("_QueueOffset",.25f);m.SetFloat("_ColorMask",15.25f);m.SetFloat("_StencilComp",8.25f);m.SetFloat("_CustomStencilTest",0);m.SetFloat("_TABigBlockItemFoldOut",1);m.SetFloat("_CustomStencilTestFoldOut",1);m.SetFloat("_StencilKeyIndex",999);}
            a.SetFloat("_Stencil",7);b.SetFloat("_Stencil",9);string sa=Snapshot(a),sb=Snapshot(b);
            var h=Host(new[]{a,b},"TA",out var root,out var item);Assert.That(Snapshot(a),Is.EqualTo(sa));Assert.That(Snapshot(b),Is.EqualTo(sb));
            Send(h,new Event{type=EventType.Layout});Send(h,new Event{type=EventType.Repaint});Assert.That(Snapshot(a),Is.EqualTo(sa));Assert.That(Snapshot(b),Is.EqualTo(sb));
        }
        [Test]public void G4QCM_QueueService_MultiSurfaceBaseUndoRedoReset()
        {
            var a=New();var b=New();a.SetFloat("_Surface",0);a.SetFloat("_AlphaClip",0);b.SetFloat("_Surface",1);a.renderQueue=2711;b.renderQueue=3277;
            var h=Host(new[]{a,b},"_QueueOffset",out var root,out var item);h.Draw=null;string sa=Snapshot(a),sb=Snapshot(b);var sync=Value(root,"SyncService");
            Undo.FlushUndoRecordObjects();Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            try
            {
                Assert.That((bool)Call(sync,"TryApplyGraphQCMQueue",17f,false),Is.True);Assert.That(a.renderQueue,Is.EqualTo(2017));Assert.That(b.renderQueue,Is.EqualTo(3017));foreach(var m in new[]{a,b}){Assert.That(m.GetFloat("_QueueControl"),Is.EqualTo(1));Assert.That(m.GetFloat("_QueueOffset"),Is.EqualTo(17));}
                Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(group);string aa=Snapshot(a),ab=Snapshot(b);Undo.PerformUndo();Assert.That(Snapshot(a),Is.EqualTo(sa));Assert.That(Snapshot(b),Is.EqualTo(sb));Undo.PerformRedo();Assert.That(Snapshot(a),Is.EqualTo(aa));Assert.That(Snapshot(b),Is.EqualTo(ab));
                Assert.That((bool)Call(sync,"TryApplyGraphQCMQueue",0f,true),Is.True);Assert.That(a.renderQueue,Is.EqualTo(2000));Assert.That(b.renderQueue,Is.EqualTo(3000));foreach(var m in new[]{a,b}){Assert.That(m.GetFloat("_QueueControl"),Is.Zero);Assert.That(m.GetFloat("_QueueOffset"),Is.Zero);}
            }
            finally{h.Draw=null;Undo.RevertAllDownToGroup(group);}
        }
        [Test]public void G4QCM_StencilService_DefaultPresetQueuePreserveUndo()
        {
            var m=New();m.renderQueue=3277;m.SetFloat("_QueueControl",1);m.SetFloat("_QueueOffset",.25f);m.SetFloat("_Stencil",7);m.SetFloat("_StencilComp",3);m.SetFloat("_StencilOp",2);m.SetFloat("_StencilFail",2);m.SetFloat("_StencilZFail",2);m.SetFloat("_CustomStencilTest",0);
            var h=Host(new[]{m},"_CustomStencilTest",out var root,out var item);h.Draw=null;var sync=Value(root,"SyncService");object[] state={false,false};Assert.That((bool)sync.GetType().GetMethod("TryGetGraphQCMStencilDisplay",All).Invoke(sync,state),Is.True);Assert.That(state[0],Is.True);string before=Snapshot(m);
            Undo.FlushUndoRecordObjects();Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            try
            {
                Assert.That((bool)Call(sync,"TryApplyGraphQCMStencilToggle",false),Is.True);Assert.That(m.renderQueue,Is.EqualTo(3277));Assert.That(m.GetFloat("_QueueOffset"),Is.EqualTo(.25f));Assert.That(m.GetFloat("_QueueControl"),Is.EqualTo(1));
                foreach(string name in new[]{"_Stencil","_StencilOp","_StencilFail","_StencilZFail","_StencilKeyIndex","_CustomStencilTest"})Assert.That(m.GetFloat(name),Is.Zero);Assert.That(m.GetFloat("_StencilComp"),Is.EqualTo(8));Assert.That(m.GetFloat("_StencilReadMask"),Is.EqualTo(255));Assert.That(m.GetFloat("_StencilWriteMask"),Is.EqualTo(255));
                Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(group);string after=Snapshot(m);Undo.PerformUndo();Assert.That(Snapshot(m),Is.EqualTo(before));Undo.PerformRedo();Assert.That(Snapshot(m),Is.EqualTo(after));
            }
            finally{h.Draw=null;Undo.RevertAllDownToGroup(group);}
        }
        [Test]public void G4QCM_QueueService_RejectedNonfiniteAllStatePreserved()
        {
            var m=New();m.renderQueue=3277;var h=Host(new[]{m},"_QueueOffset",out var root,out var item);h.Draw=null;var sync=Value(root,"SyncService");string before=Snapshot(m);
            Assert.That((bool)Call(sync,"TryApplyGraphQCMQueue",float.NaN,false),Is.False);Assert.That((bool)Call(sync,"TryApplyGraphQCMQueue",float.PositiveInfinity,false),Is.False);Assert.That((bool)Call(sync,"TryApplyGraphQCMQueue",5001f,false),Is.False);Assert.That(Snapshot(m),Is.EqualTo(before));
        }
    }
}
