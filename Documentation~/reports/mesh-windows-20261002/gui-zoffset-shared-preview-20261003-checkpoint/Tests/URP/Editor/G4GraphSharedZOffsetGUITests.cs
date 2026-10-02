using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace NBFX.Baseline.Tests
{
    // Backend transactions on real MaterialEditor/MaterialProperty/shared item;
    // not a claim of visible Inspector interaction or Offset GPU coverage.
    public sealed class G4GraphSharedZOffsetGUITests
    {
        const BindingFlags All=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
        readonly List<Object> owned=new List<Object>();
        Type Type(string name)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name,false)).First(t=>t!=null);
        object Root(params Material[] mats)
        {
            object root=Activator.CreateInstance(Type("NBShaderEditor.NBShaderRootItem"));var rt=root.GetType();
            var editor=(MaterialEditor)Editor.CreateEditor(mats.Cast<Object>().ToArray(),typeof(MaterialEditor));owned.Add(editor);
            rt.GetField("MatEditor",All).SetValue(root,editor);rt.GetField("Mats",All).SetValue(root,mats.ToList());rt.GetField("Shader",All).SetValue(root,mats[0].shader);
            rt.GetMethod("InitFlags",All).Invoke(root,new object[]{mats.ToList()});
            var dic=(IDictionary)rt.GetField("PropertyInfoDic",All).GetValue(root);
            foreach(var p in MaterialEditor.GetMaterialProperties(mats.Cast<Object>().ToArray()))
            {
                var info=Activator.CreateInstance(Type("NBShaderEditor.ShaderPropertyInfo"));info.GetType().GetField("Property").SetValue(info,p);info.GetType().GetField("Name").SetValue(info,p.name);info.GetType().GetField("Index").SetValue(info,mats[0].shader.FindPropertyIndex(p.name));dic.Add(p.name,info);
            }
            return root;
        }
        Material Material(string omit=null,bool wrongType=false)
        {
            string source="Shader \"Hidden/NBFX/SharedZOffsetGUISchema\" { Properties { _NB_DistortionMode(\"Host\",Float)=0 _NB_Flags0Lo16(\"F0L\",Float)=0 _NB_Flags0Hi16(\"F0H\",Float)=0 _NB_Flags1Lo16(\"F1L\",Float)=0 _NB_Flags1Hi16(\"F1H\",Float)=0 ";
            foreach(string n in new[]{"_ZOffsetBlockFoldOut","_ZOffset_Toggle","_offsetFactor","_offsetUnits"})if(n!=omit)source+=n+"(\""+n+"\","+(wrongType&&n=="_offsetUnits"?"Integer":"Float")+")=0 ";
            source+="} SubShader { Pass { } } }";var shader=ShaderUtil.CreateShaderAsset(source,false);Assert.That(shader,Is.Not.Null);shader.hideFlags=HideFlags.HideAndDontSave;owned.Add(shader);var m=new Material(shader){hideFlags=HideFlags.HideAndDontSave};owned.Add(m);return m;
        }
        bool Ready(object root)=>(bool)root.GetType().GetMethod("InitializeGraphZOffsetInputs",All).Invoke(root,null);
        object Block(object root)=>root.GetType().GetField("_graphZOffsetBlock",All).GetValue(root);
        MaterialProperty Property(object root,string name)
        {
            var info=((IDictionary)root.GetType().GetField("PropertyInfoDic",All).GetValue(root))[name];return (MaterialProperty)info.GetType().GetField("Property").GetValue(info);
        }
        void Toggle(object root,bool value)
        {
            var block=Block(root);var type=block.GetType();((Action)type.GetField("_onBeforeValueChanged",All).GetValue(block))();
            Property(root,"_ZOffset_Toggle").floatValue=value?1:0;type.GetMethod("ApplySideEffects",All).Invoke(block,new object[]{value});type.GetMethod("OnEndChange",All).Invoke(block,null);
        }
        [TearDown]public void Cleanup(){foreach(var o in owned.AsEnumerable().Reverse())if(o)Object.DestroyImmediate(o);owned.Clear();}

        [TestCase("_offsetFactor",TestName="G4SharedZOffsetGUI_MissingFactor")]
        [TestCase("_offsetUnits",TestName="G4SharedZOffsetGUI_MissingUnits")]
        [TestCase("_ZOffset_Toggle",TestName="G4SharedZOffsetGUI_MissingToggle")]
        [TestCase("_ZOffsetBlockFoldOut",TestName="G4SharedZOffsetGUI_MissingFoldout")]
        public void MissingRealProperty_RejectsWithoutMutation(string omit)
        {
            var m=Material(omit);string before=EditorJsonUtility.ToJson(m);var r=Root(m);Assert.That(Ready(r),Is.False);Assert.That(EditorJsonUtility.ToJson(m),Is.EqualTo(before));
        }
        [Test]public void IntegerRenderProperty_Rejects(){var m=Material(null,true);var r=Root(m);Assert.That(Ready(r),Is.False);}
        [Test]public void FactoryConstructsOnlyExistingZOffsetItems_NoTARequirements()
        {
            var m=Material();string before=EditorJsonUtility.ToJson(m);var r=Root(m);Assert.That(Ready(r),Is.True);var block=Block(r);Assert.That(block.GetType().FullName,Is.EqualTo("NBShaderEditor.PropertyToggleBlockItem"));
            var names=(IEnumerable<string>)r.GetType().GetMethod("GetSharedGraphPropertyNames",All).Invoke(r,null);Assert.That(names.ToArray(),Is.EquivalentTo(new[]{"_ZOffsetBlockFoldOut","_ZOffset_Toggle","_offsetFactor","_offsetUnits"}));Assert.That(EditorJsonUtility.ToJson(m),Is.EqualTo(before));
        }
        [Test]public void MultiSelectClose_UndoRedoRestoresToggleAndBothOffsetsTogether()
        {
            var a=Material();var b=new Material(a.shader){hideFlags=HideFlags.HideAndDontSave};owned.Add(b);a.SetFloat("_ZOffset_Toggle",1);b.SetFloat("_ZOffset_Toggle",1);a.SetFloat("_offsetFactor",2);a.SetFloat("_offsetUnits",-7);b.SetFloat("_offsetFactor",9);b.SetFloat("_offsetUnits",3);
            var root=Root(a,b);Assert.That(Ready(root),Is.True);Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Shared ZOffset transaction");
            try{Toggle(root,false);Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(group);foreach(var m in new[]{a,b}){Assert.That(m.GetFloat("_ZOffset_Toggle"),Is.Zero);Assert.That(m.GetFloat("_offsetFactor"),Is.Zero);Assert.That(m.GetFloat("_offsetUnits"),Is.Zero);}Undo.PerformUndo();Assert.That(a.GetFloat("_ZOffset_Toggle"),Is.EqualTo(1));Assert.That(b.GetFloat("_ZOffset_Toggle"),Is.EqualTo(1));Assert.That(a.GetFloat("_offsetFactor"),Is.EqualTo(2));Assert.That(a.GetFloat("_offsetUnits"),Is.EqualTo(-7));Assert.That(b.GetFloat("_offsetFactor"),Is.EqualTo(9));Assert.That(b.GetFloat("_offsetUnits"),Is.EqualTo(3));Undo.PerformRedo();Assert.That(a.GetFloat("_offsetFactor"),Is.Zero);Assert.That(b.GetFloat("_offsetUnits"),Is.Zero);}
            finally{Undo.RevertAllDownToGroup(group);}
        }
        [Test]public void EnablePreservesValues_AndDirectResetUsesSameCallback()
        {
            var m=Material();m.SetFloat("_offsetFactor",2);m.SetFloat("_offsetUnits",-3);var r=Root(m);Assert.That(Ready(r),Is.True);Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            try{Toggle(r,true);Assert.That(m.GetFloat("_offsetFactor"),Is.EqualTo(2));Assert.That(m.GetFloat("_offsetUnits"),Is.EqualTo(-3));Block(r).GetType().GetMethod("ExecuteReset",All).Invoke(Block(r),new object[]{false});Assert.That(m.GetFloat("_ZOffset_Toggle"),Is.Zero);Assert.That(m.GetFloat("_offsetFactor"),Is.Zero);Assert.That(m.GetFloat("_offsetUnits"),Is.Zero);}
            finally{Undo.RevertAllDownToGroup(group);}
        }
        [Test]public void MixedGraphLegacyHost_IsReadOnly_NoPartialReset()
        {
            var graph=Material();var shader=Shader.Find("Unlit/Color");Assert.That(shader,Is.Not.Null);var legacy=new Material(shader){hideFlags=HideFlags.HideAndDontSave};owned.Add(legacy);
            graph.SetFloat("_offsetFactor",17);string a=EditorJsonUtility.ToJson(graph),b=EditorJsonUtility.ToJson(legacy);var root=Root(graph,legacy);Assert.That(Ready(root),Is.False);Assert.That(EditorJsonUtility.ToJson(graph),Is.EqualTo(a));Assert.That(EditorJsonUtility.ToJson(legacy),Is.EqualTo(b));
        }
    }
}
