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
    public sealed class G4MaskProgramSharedGUITests
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
        {
            var m=G4SpecDebugFixture.NewGraph();owned.Add(m);m.SetFloat("_Mask_Toggle",1);m.SetFloat("_ProgramNoise_Toggle",1);m.SetFloat("_NBShaderFeatureTier",3);return m;
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
            var h=ScriptableObject.CreateInstance<NBFXMainTexGUIEventHost>();owned.Add(h);h.hideFlags=HideFlags.HideAndDontSave;h.position=new Rect(20,20,640,480);h.SetupCommand="NBFX_MaskProgram_"+Guid.NewGuid().ToString("N");
            object r=null,p=null;h.Setup=()=>
            {
                r=Root(materials);
                Assert.That((bool)Call(r,"InitializeGraphMainTextureInputs"),Is.True);
                Assert.That((bool)Call(r,"InitializeGraphMaskProgramInputs"),Is.True);
                var leaf=Value(r,property=="uv:28"?"_graphProgramNoiseItem":"_graphMaskItem");
                p=property.StartsWith("uv:")?Descendants(leaf).Single(c=>c.GetType().Name=="UVModeSelectItem"&&(int)Value(c,"_uvModeBitPos")==int.Parse(property.Substring(3))):property=="_Mask_Toggle"?leaf:Descendants(leaf).Single(c=>(string)Value(c,"PropertyName")==property);
            };
            h.ShowUtility();h.SendEvent(new Event{type=EventType.ExecuteCommand,commandName=h.SetupCommand});Check(h);Assert.That(h.Initialized,Is.True);
            root=r;item=p;h.Draw=()=>Call(r,"DrawGraphMaskProgramInputs",p);return h;
        }
        static void Check(NBFXMainTexGUIEventHost h){if(h.Failure!=null)ExceptionDispatchInfo.Capture(h.Failure).Throw();}
        static void Send(NBFXMainTexGUIEventHost h,Event e)
        {var type=e.rawType;h.Counts.TryGetValue(type,out int before);h.SendEvent(e);Check(h);Assert.That(h.Counts.TryGetValue(type,out int after)&&after>before,Is.True,"Original OnGUI must receive actual native event.");}
        static string Snapshot(Material m)=>EditorJsonUtility.ToJson(m)+"|"+string.Join("|",m.shaderKeywords.OrderBy(x=>x));
        static int Bit(string name)=>(int)Find("NBShader.NBShaderFlags").GetField(name,All).GetValue(null);
        static void AssertWords(Material m,int lo,int hi){Assert.That(m.GetFloat("_NB_Flags1Lo16"),Is.EqualTo(lo));Assert.That(m.GetFloat("_NB_Flags1Hi16"),Is.EqualTo(hi));}

        [TearDown]public void Cleanup(){foreach(var h in owned.OfType<NBFXMainTexGUIEventHost>()){h.Draw=null;h.Setup=null;h.Close();}foreach(var o in owned.AsEnumerable().Reverse())if(o)Object.DestroyImmediate(o);owned.Clear();}
        [Test]public void G4MaskProgram_RawRotationBit7_Marker2MixedPassiveNoSeed()
        {
            var a=New();var b=New();a.SetFloat("_NB_Flags0Lo16",130);b.SetFloat("_NB_Flags0Lo16",2);
            foreach(var m in new[]{a,b}){m.SetFloat("_NB_Flags0Hi16",65536.25f);m.SetFloat("_Mask_RotationToggle",0);}
            var h=Host(new[]{a,b},"_Mask_RotationToggle",out var root,out var item);string sa=Snapshot(a),sb=Snapshot(b);
            Send(h,new Event{type=EventType.Layout});Send(h,new Event{type=EventType.Repaint});Assert.That(Snapshot(a),Is.EqualTo(sa));Assert.That(Snapshot(b),Is.EqualTo(sb));
            var method=item.GetType().GetMethod("TryGetGraphRawMaskRotationDisplay",All);Assert.That(method,Is.Not.Null);object[] args={false,false};Assert.That((bool)method.Invoke(item,args),Is.True);Assert.That(args[0],Is.True);Assert.That(args[1],Is.True);
            Assert.That(a.GetFloat("_Mask_RotationToggle"),Is.Zero);Assert.That(b.GetFloat("_Mask_RotationToggle"),Is.Zero);
        }
        [Test]public void G4MaskProgram_RawRotationBit7_ActualToggleUndoRedoRestore()
        {
            var m=New();m.SetFloat("_NB_Flags0Lo16",130);m.SetFloat("_NB_Flags0Hi16",65536.25f);m.SetFloat("_Mask_RotationToggle",0);
            var h=Host(new[]{m},"_Mask_RotationToggle",out var root,out var item);string before=Snapshot(m);
            Send(h,new Event{type=EventType.Layout});Send(h,new Event{type=EventType.Repaint});Assert.That((bool)Value(item,"HasModified"),Is.True,"Raw bit7 drives modified/reset display even when new UI mirror default0.");
            Undo.FlushUndoRecordObjects();Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            try
            {
                var rect=(Rect)Value(item,"ControlRect");Send(h,new Event{type=EventType.MouseDown,button=0,mousePosition=rect.center});Send(h,new Event{type=EventType.MouseUp,button=0,mousePosition=rect.center});
                Assert.That(m.GetFloat("_NB_Flags0Lo16"),Is.EqualTo(2));Assert.That(m.GetFloat("_NB_Flags0Hi16"),Is.EqualTo(65536.25f));Assert.That(m.GetFloat("_Mask_RotationToggle"),Is.Zero);
                Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(group);string after=Snapshot(m);h.Draw=null;
                Undo.PerformUndo();Assert.That(Snapshot(m),Is.EqualTo(before));Undo.PerformRedo();Assert.That(Snapshot(m),Is.EqualTo(after));
            }
            finally{h.Draw=null;Undo.RevertAllDownToGroup(group);}
        }
        [TestCase(2,"_MaskUVModeFoldOut",TestName="G4MaskProgram_ServiceUV_Mask1LoOwnSliceRestore")]
        [TestCase(4,"_Mask2UVModeFoldOut",TestName="G4MaskProgram_ServiceUV_Mask2LoOwnSliceRestore")]
        [TestCase(6,"_Mask3UVModeFoldOut",TestName="G4MaskProgram_ServiceUV_Mask3LoOwnSliceRestore")]
        [TestCase(28,"_ProgramNoiseUVModeFoldOut",TestName="G4MaskProgram_ServiceUV_ProgramHiOwnSliceRestore")]
        public void ServiceUV(int position,string fold)
        {
            var m=New();bool low=position<16;string edited="_NB_UVModeFlag0"+(low?"Lo16":"Hi16"),untouched="_NB_UVModeFlag0"+(low?"Hi16":"Lo16");
            m.SetFloat(edited,0);m.SetFloat(untouched,65536.25f);m.SetFloat("_NB_UVModeFlagType0Lo16",0);m.SetFloat("_NB_UVModeFlagType0Hi16",0);m.SetFloat("_NB_Flags1Hi16",16);m.SetFloat(fold,0);
            var h=Host(new[]{m},"uv:"+position,out var root,out var item);h.Draw=null;var sync=Value(root,"SyncService");string before=Snapshot(m);int untouchedBits=BitConverter.ToInt32(BitConverter.GetBytes(m.GetFloat(untouched)),0);
            var enumType=Find("NBShader.NBShaderFlags").GetNestedType("UVMode",All);Undo.FlushUndoRecordObjects();Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            try
            {
                Assert.That((bool)Call(sync,"TryApplyGraphMaskProgramUVMode",position,Enum.ToObject(enumType,3),fold,true),Is.True);
                Assert.That(m.GetFloat(edited),Is.EqualTo(3<<(position&15)));Assert.That(BitConverter.ToInt32(BitConverter.GetBytes(m.GetFloat(untouched)),0),Is.EqualTo(untouchedBits));
                Assert.That(m.GetFloat("_NB_UVModeFlagType0Lo16")+m.GetFloat("_NB_UVModeFlagType0Hi16"),Is.Zero);Assert.That(m.GetFloat("_NB_Flags1Hi16"),Is.EqualTo(16));Assert.That(m.GetFloat(fold),Is.EqualTo(1));
                string on=Snapshot(m);Assert.That((bool)Call(sync,"TryApplyGraphMaskProgramUVMode",position,Enum.ToObject(enumType,3),fold,true),Is.True);Assert.That(Snapshot(m),Is.EqualTo(on),"Decoded no-op must not canonicalize opposite half.");
                Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(group);Undo.PerformUndo();Assert.That(Snapshot(m),Is.EqualTo(before));Undo.PerformRedo();Assert.That(Snapshot(m),Is.EqualTo(on));
                Assert.That((bool)Call(sync,"TryApplyGraphMaskProgramUVMode",position,Enum.ToObject(enumType,0),fold,true),Is.True);Assert.That(Snapshot(m),Is.EqualTo(before));
            }
            finally{h.Draw=null;Undo.RevertAllDownToGroup(group);}
        }
    }
}
