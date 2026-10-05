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
    public sealed class G4VertexOffsetSharedGUITests
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
        Material New(){var m=G4SpecDebugFixture.NewGraph();owned.Add(m);m.SetFloat("_VertexOffset_Toggle",1);m.SetFloat("_VertexOffset_Mask_Toggle",1);m.SetFloat("_VertexOffset_NormalDir_Toggle",0);m.SetFloat("_VertexOffset_DirectionSpace",0);m.SetFloat("_NBShaderFeatureTier",3);return m;}
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
            var h=ScriptableObject.CreateInstance<NBFXMainTexGUIEventHost>();owned.Add(h);h.hideFlags=HideFlags.HideAndDontSave;h.position=new Rect(20,20,640,480);h.SetupCommand="NBFX_VO_"+Guid.NewGuid().ToString("N");
            object r=null,p=null;h.Setup=()=>
            {
                r=Root(materials);
                Assert.That((bool)Call(r,"InitializeGraphMainTextureInputs"),Is.True);
                Assert.That((bool)Call(r,"InitializeGraphVertexOffsetInputs"),Is.True);
                var leaf=Value(r,"_graphVertexOffsetItem");
                p=property.StartsWith("uv:")?Descendants(leaf).Single(c=>c.GetType().Name=="UVModeSelectItem"&&(int)Value(c,"_uvModeBitPos")==int.Parse(property.Substring(3))):property=="_VertexOffset_Toggle"?leaf:Descendants(leaf).Single(c=>(string)Value(c,"PropertyName")==property);
            };
            h.ShowUtility();h.SendEvent(new Event{type=EventType.ExecuteCommand,commandName=h.SetupCommand});Check(h);Assert.That(h.Initialized,Is.True);
            root=r;item=p;h.Draw=()=>Call(r,"DrawGraphVertexOffsetInputs",p);return h;
        }
        static void Check(NBFXMainTexGUIEventHost h){if(h.Failure!=null)ExceptionDispatchInfo.Capture(h.Failure).Throw();}
        static void Send(NBFXMainTexGUIEventHost h,Event e)
        {var type=e.rawType;h.Counts.TryGetValue(type,out int before);h.SendEvent(e);Check(h);Assert.That(h.Counts.TryGetValue(type,out int after)&&after>before,Is.True,"Original OnGUI must receive actual native event.");}
        static string Snapshot(Material m)=>EditorJsonUtility.ToJson(m)+"|"+string.Join("|",m.shaderKeywords.OrderBy(x=>x));
        static int Bit(string name)=>(int)Find("NBShader.NBShaderFlags").GetField(name,All).GetValue(null);
        static void AssertWords(Material m,int lo,int hi){Assert.That(m.GetFloat("_NB_Flags1Lo16"),Is.EqualTo(lo));Assert.That(m.GetFloat("_NB_Flags1Hi16"),Is.EqualTo(hi));}

        [TearDown]public void Cleanup(){foreach(var h in owned.OfType<NBFXMainTexGUIEventHost>()){h.Draw=null;h.Setup=null;h.Close();}foreach(var o in owned.AsEnumerable().Reverse())if(o)Object.DestroyImmediate(o);owned.Clear();}
        [Test]public void G4VO_Marker2ExpandedMixed_ActualPassiveAllStateReadOnly()
        {var a=New();var b=New();foreach(var m in new[]{a,b})foreach(string f in new[]{"_VertexOffsetBlockFoldOut","_VertexOffsetUVModeFoldOut","_VertexOffsetMaskBlockFoldOut","_VertexOffsetMaskUVModeFoldOut"})m.SetFloat(f,1);b.SetVector("_VertexOffset_CustomDir",new Vector4(2,3,4,0));var h=Host(new[]{a,b},"_VertexOffset_Toggle",out var root,out var item);string sa=Snapshot(a),sb=Snapshot(b);Send(h,new Event{type=EventType.Layout});Send(h,new Event{type=EventType.Repaint});Assert.That(Snapshot(a),Is.EqualTo(sa));Assert.That(Snapshot(b),Is.EqualTo(sb));}
        [Test]public void G4VO_ActualParentToggle_PreservesRawMaskUndoRedo()
        {
            var m=New();var h=Host(new[]{m},"_VertexOffset_Toggle",out var root,out var item);string before=Snapshot(m);Undo.FlushUndoRecordObjects();Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            try{Send(h,new Event{type=EventType.Layout});Send(h,new Event{type=EventType.Repaint});var r=(Rect)Value(item,"ControlRect");Send(h,new Event{type=EventType.MouseDown,button=0,mousePosition=r.center});Send(h,new Event{type=EventType.MouseUp,button=0,mousePosition=r.center});Assert.That(m.GetFloat("_VertexOffset_Toggle"),Is.Zero);Assert.That(m.GetFloat("_VertexOffset_Mask_Toggle"),Is.EqualTo(1));Assert.That(m.GetFloat("_NB_TierAllowVertexOffset")+m.GetFloat("_NB_TierAllowVertexOffsetMask"),Is.Zero);Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(group);string after=Snapshot(m);h.Draw=null;Undo.PerformUndo();Assert.That(Snapshot(m),Is.EqualTo(before));Undo.PerformRedo();Assert.That(Snapshot(m),Is.EqualTo(after));}finally{h.Draw=null;Undo.RevertAllDownToGroup(group);}
        }
        [TestCase(20,"_VertexOffsetUVModeFoldOut",TestName="G4VO_ServiceUV_MapHiOwnSliceUndoRestore")]
        [TestCase(22,"_VertexOffsetMaskUVModeFoldOut",TestName="G4VO_ServiceUV_MaskHiOwnSliceUndoRestore")]
        public void UV(int pos,string fold)
        {
            var m=New();m.SetFloat("_NB_UVModeFlag0Hi16",0);m.SetFloat("_NB_UVModeFlag0Lo16",65536.25f);m.SetFloat("_NB_UVModeFlagType0Lo16",0);m.SetFloat("_NB_UVModeFlagType0Hi16",0);m.SetFloat("_NB_Flags1Hi16",16);m.SetFloat(fold,0);var h=Host(new[]{m},"uv:"+pos,out var root,out var item);h.Draw=null;var sync=Value(root,"SyncService");string before=Snapshot(m);var enumType=Find("NBShader.NBShaderFlags").GetNestedType("UVMode",All);Undo.FlushUndoRecordObjects();Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            try{Assert.That((bool)Call(sync,"TryApplyGraphVertexOffsetUV",pos,Enum.ToObject(enumType,3),fold,true),Is.True);Assert.That(m.GetFloat("_NB_UVModeFlag0Hi16"),Is.EqualTo(3<<(pos-16)));Assert.That(m.GetFloat("_NB_UVModeFlag0Lo16"),Is.EqualTo(65536.25f));Assert.That(m.GetFloat("_NB_Flags1Hi16"),Is.EqualTo(16));Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(group);string after=Snapshot(m);Assert.That((bool)Call(sync,"TryApplyGraphVertexOffsetUV",pos,Enum.ToObject(enumType,3),fold,true),Is.True);Assert.That(Snapshot(m),Is.EqualTo(after));Undo.PerformUndo();Assert.That(Snapshot(m),Is.EqualTo(before));Undo.PerformRedo();Assert.That(Snapshot(m),Is.EqualTo(after));Assert.That((bool)Call(sync,"TryApplyGraphVertexOffsetUV",pos,Enum.ToObject(enumType,0),fold,true),Is.True);Assert.That(Snapshot(m),Is.EqualTo(before));}finally{Undo.RevertAllDownToGroup(group);}
        }
        [TestCase(1,16,TestName="G4VO_ServiceCD_Word1HiOwnNibbleUndo")]
        [TestCase(3,0,TestName="G4VO_ServiceCD_Word3LoOwnNibbleUndo")]
        public void CD(int word,int pos)
        {
            var m=New();string edited="_NB_CustomDataFlag"+word+(pos<16?"Lo16":"Hi16"),other="_NB_CustomDataFlag"+word+(pos<16?"Hi16":"Lo16");m.SetFloat(edited,0);m.SetFloat(other,65536.25f);var h=Host(new[]{m},"_VertexOffset_Toggle",out var root,out var item);h.Draw=null;string before=Snapshot(m);var sync=Value(root,"SyncService");var enumType=Find("NBShader.NBShaderFlags").GetNestedType("CutomDataComponent",All);Undo.FlushUndoRecordObjects();Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            try{Assert.That((bool)Call(sync,"TryApplyGraphVertexOffsetCustomData",pos,word,Enum.ToObject(enumType,1)),Is.True);Assert.That(m.GetFloat(edited),Is.EqualTo(15<<(pos&15)));Assert.That(m.GetFloat(other),Is.EqualTo(65536.25f));Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(group);string after=Snapshot(m);Undo.PerformUndo();Assert.That(Snapshot(m),Is.EqualTo(before));Undo.PerformRedo();Assert.That(Snapshot(m),Is.EqualTo(after));Assert.That((bool)Call(sync,"TryApplyGraphVertexOffsetCustomData",pos,word,Enum.ToObject(enumType,0)),Is.True);Assert.That(Snapshot(m),Is.EqualTo(before));}finally{Undo.RevertAllDownToGroup(group);}
        }
        [Test]public void G4VO_ServiceStartHiBit25_UneditedNoncanonicalLoUndo()
        {var m=New();m.SetFloat("_NB_Flags1Lo16",65536.25f);m.SetFloat("_NB_Flags1Hi16",1);var h=Host(new[]{m},"_VertexOffset_Toggle",out var root,out var item);h.Draw=null;var sync=Value(root,"SyncService");Assert.That((bool)Call(sync,"TryApplyGraphVertexOffsetStart",true),Is.True);Assert.That(m.GetFloat("_NB_Flags1Hi16"),Is.EqualTo(513));Assert.That(m.GetFloat("_NB_Flags1Lo16"),Is.EqualTo(65536.25f));Assert.That(m.GetFloat("_VertexOffset_StartFromZero"),Is.EqualTo(1));}
        [Test]public void G4VO_ServiceRepairRawBit9_MirrorAndNoncanonicalHi()
        {var m=New();m.SetFloat("_VertexOffset_NormalDir_Toggle",2);m.SetFloat("_NB_Flags1Lo16",2);m.SetFloat("_NB_Flags1Hi16",65536.25f);m.SetFloat("_IgnoreVetexColor_Toggle",0);var h=Host(new[]{m},"_VertexOffset_Toggle",out var root,out var item);h.Draw=null;Assert.That((bool)Call(Value(root,"SyncService"),"TryRepairGraphVertexOffsetColor"),Is.True);Assert.That(m.GetFloat("_NB_Flags1Lo16"),Is.EqualTo(514));Assert.That(m.GetFloat("_NB_Flags1Hi16"),Is.EqualTo(65536.25f));Assert.That(m.GetFloat("_IgnoreVetexColor_Toggle"),Is.EqualTo(1));}
    }
}
