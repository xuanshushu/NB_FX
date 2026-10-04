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
    public sealed class G4DissolveSharedGUIServiceTests
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
            var m=G4SpecDebugFixture.NewGraph();owned.Add(m);m.SetFloat("_Dissolve_Toggle",1);m.SetFloat("_DissolveMask_Toggle",1);m.SetFloat("_Dissolve_useRampMap_Toggle",1);m.SetFloat("_NBShaderFeatureTier",3);
            m.SetFloat("_DissolveMaskMode",0);m.SetFloat("_DissolveRampSourceMode",0);m.SetFloat("_DissolveLineMaskToggle",0);m.SetFloat("_DissolveRampColorBlendMode",0);
            m.SetFloat("_NB_Flags1Lo16",130);m.SetFloat("_NB_Flags1Hi16",137);
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
            var h=ScriptableObject.CreateInstance<NBFXMainTexGUIEventHost>();owned.Add(h);h.hideFlags=HideFlags.HideAndDontSave;h.position=new Rect(20,20,640,480);h.SetupCommand="NBFX_Dissolve_"+Guid.NewGuid().ToString("N");
            object r=null,p=null;h.Setup=()=>
            {
                r=Root(materials);
                Assert.That((bool)Call(r,"InitializeGraphMainTextureInputs"),Is.True);
                Assert.That((bool)Call(r,"InitializeGraphDissolveInputs"),Is.True);
                var leaf=Value(r,"_graphDissolveItem");
                p=property.StartsWith("uv:")?Descendants(leaf).Single(c=>c.GetType().Name=="UVModeSelectItem"&&(int)Value(c,"_uvModeBitPos")==int.Parse(property.Substring(3))):property=="_Dissolve_Toggle"?leaf:Descendants(leaf).Single(c=>(string)Value(c,"PropertyName")==property);
            };
            h.ShowUtility();h.SendEvent(new Event{type=EventType.ExecuteCommand,commandName=h.SetupCommand});Check(h);Assert.That(h.Initialized,Is.True);
            root=r;item=p;h.Draw=()=>Call(r,"DrawGraphDissolveInputs",p);return h;
        }
        static void Check(NBFXMainTexGUIEventHost h){if(h.Failure!=null)ExceptionDispatchInfo.Capture(h.Failure).Throw();}
        static void Send(NBFXMainTexGUIEventHost h,Event e)
        {var type=e.rawType;h.Counts.TryGetValue(type,out int before);h.SendEvent(e);Check(h);Assert.That(h.Counts.TryGetValue(type,out int after)&&after>before,Is.True,"Original OnGUI must receive actual native event.");}
        static string Snapshot(Material m)=>EditorJsonUtility.ToJson(m)+"|"+string.Join("|",m.shaderKeywords.OrderBy(x=>x));
        static int Bit(string name)=>(int)Find("NBShader.NBShaderFlags").GetField(name,All).GetValue(null);
        static void AssertWords(Material m,int lo,int hi){Assert.That(m.GetFloat("_NB_Flags1Lo16"),Is.EqualTo(lo));Assert.That(m.GetFloat("_NB_Flags1Hi16"),Is.EqualTo(hi));}
        [OneTimeSetUp]public void Preflight()=>G4SpecDebugFixture.PreflightImport();
        [TearDown]public void Cleanup(){foreach(var h in owned.OfType<NBFXMainTexGUIEventHost>()){h.Draw=null;h.Setup=null;h.Close();}foreach(var o in owned.AsEnumerable().Reverse())if(o)Object.DestroyImmediate(o);owned.Clear();}

        [TestCase("FLAG_BIT_PARTICLE_1_DISSOLVE_LINE_MASK","_DissolveLineMaskToggle",TestName="G4Dissolve_Service_LineLo16_RawMirrorNeighborsNoop")]
        [TestCase("FLAG_BIT_PARTICLE_1_DISSOLVE_RAMP_MULITPLY","_DissolveRampColorBlendMode",TestName="G4Dissolve_Service_RampMultiplyLo16_RawMirrorNeighborsNoop")]
        public void DirectService(string name,string mirror)
        {
            var m=New();var h=Host(new[]{m},"_Dissolve_Toggle",out var root,out var item);h.Draw=null;var sync=Value(root,"SyncService");int bit=Bit(name);
            int lo=(int)m.GetFloat("_NB_Flags1Lo16"),hi=(int)m.GetFloat("_NB_Flags1Hi16");Assert.That(m.GetFloat(mirror),Is.Zero);
            Assert.That((bool)Call(sync,"TryApplyGraphDissolveFlagEdit",bit,true),Is.True);
            AssertWords(m,lo|(bit&65535),hi|(int)((uint)bit>>16));Assert.That(m.GetFloat(mirror),Is.EqualTo(1));
            string changed=Snapshot(m);Assert.That((bool)Call(sync,"TryApplyGraphDissolveFlagEdit",bit,true),Is.True);Assert.That(Snapshot(m),Is.EqualTo(changed));
            Assert.That((bool)Call(sync,"TryApplyGraphDissolveFlagEdit",bit,false),Is.True);AssertWords(m,lo,hi);Assert.That(m.GetFloat(mirror),Is.Zero);
        }
        [TestCase("_DissolveLineMaskToggle","FLAG_BIT_PARTICLE_1_DISSOLVE_LINE_MASK",TestName="G4Dissolve_ActualLineToggle_Lo16MixedUndoRedo")]
        public void ActualToggle(string property,string name)
        {
            var a=New();var b=New();b.SetFloat("_NB_Flags1Lo16",258);b.SetFloat("_NB_Flags1Hi16",73);
            var h=Host(new[]{a,b},property,out var root,out var item);int bit=Bit(name);
            int alo=(int)a.GetFloat("_NB_Flags1Lo16"),ahi=(int)a.GetFloat("_NB_Flags1Hi16"),blo=(int)b.GetFloat("_NB_Flags1Lo16"),bhi=(int)b.GetFloat("_NB_Flags1Hi16");
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
        [Test]public void G4Dissolve_Marker2_ActualLayoutRepaint_AllStateReadOnly()
        {var m=New();var h=Host(new[]{m},"_Dissolve_Toggle",out var root,out var item);string before=Snapshot(m);Send(h,new Event{type=EventType.Layout});Send(h,new Event{type=EventType.Repaint});Assert.That(Snapshot(m),Is.EqualTo(before));}

        [Explicit("Native Windows Popup selection requires manual desktop interaction; retained interrupted run is not a pass.")]
        [Test]public void G4Dissolve_ActualRampSourcePopup_RawEnumUndoRedo()
        {
            var m=New();var h=Host(new[]{m},"_DissolveRampSourceMode",out var root,out var item);string before=Snapshot(m);
            Undo.FlushUndoRecordObjects();Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            try
            {
                Send(h,new Event{type=EventType.Layout});Send(h,new Event{type=EventType.Repaint});var rect=(Rect)Value(item,"ControlRect");
                Send(h,new Event{type=EventType.MouseDown,button=0,mousePosition=rect.center});
                var callbackType=typeof(EditorGUI).GetNestedType("PopupCallbackInfo",BindingFlags.NonPublic);Assert.That(callbackType,Is.Not.Null);
                var field=callbackType.GetField("instance",All);Assert.That(field,Is.Not.Null);var callback=field.GetValue(null);Assert.That(callback,Is.Not.Null);
                callbackType.GetMethod("SetEnumValueDelegate",All).Invoke(callback,new object[]{null,new[]{"渐变","贴图"},1});Check(h);Assert.That(field.GetValue(null),Is.Null);
                Assert.That(m.GetFloat("_DissolveRampSourceMode"),Is.EqualTo(1));Assert.That(m.GetFloat("_Dissolve_useRampMap_Toggle"),Is.EqualTo(1));
                Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(group);string after=Snapshot(m);h.Draw=null;
                Undo.PerformUndo();Assert.That(Snapshot(m),Is.EqualTo(before));Undo.PerformRedo();Assert.That(Snapshot(m),Is.EqualTo(after));
            }
            finally{h.Draw=null;Undo.RevertAllDownToGroup(group);}
        }
        [Test]public void G4Dissolve_DirectLineLoEdit_UneditedHiNoncanonicalBits()
        {
            var m=New();var h=Host(new[]{m},"_Dissolve_Toggle",out var root,out var item);h.Draw=null;m.SetFloat("_NB_Flags1Hi16",65536.25f);
            int bits=BitConverter.ToInt32(BitConverter.GetBytes(m.GetFloat("_NB_Flags1Hi16")),0);var sync=Value(root,"SyncService");
            Assert.That((bool)Call(sync,"TryApplyGraphDissolveFlagEdit",Bit("FLAG_BIT_PARTICLE_1_DISSOLVE_LINE_MASK"),true),Is.True);
            Assert.That(m.GetFloat("_DissolveLineMaskToggle"),Is.EqualTo(1));Assert.That(BitConverter.ToInt32(BitConverter.GetBytes(m.GetFloat("_NB_Flags1Hi16")),0),Is.EqualTo(bits));
        }

        static void Choose(NBFXMainTexGUIEventHost h,object item,int value)
        {
            Send(h,new Event{type=EventType.Layout});Send(h,new Event{type=EventType.Repaint});var rect=(Rect)Value(item,"ControlRect");
            Send(h,new Event{type=EventType.MouseDown,button=0,mousePosition=rect.center});
            var callbackType=typeof(EditorGUI).GetNestedType("PopupCallbackInfo",BindingFlags.NonPublic);Assert.That(callbackType,Is.Not.Null);
            var field=callbackType.GetField("instance",All);var callback=field.GetValue(null);Assert.That(callback,Is.Not.Null);
            callbackType.GetMethod("SetEnumValueDelegate",All).Invoke(callback,new object[]{null,new[]{"UV0","Special","Polar","Cylinder","Main","Screen","World","Object","Common"},value});Check(h);Assert.That(field.GetValue(null),Is.Null);
        }
        [Explicit("Native Windows Popup selection requires manual desktop interaction; retained interrupted run is not a pass.")]
        [TestCase(14,TestName="G4Dissolve_ActualUVPopup_MapLoSliceCylinderUndoRestore")]
        [TestCase(16,TestName="G4Dissolve_ActualUVPopup_MaskHiSliceCylinderUndoRestore")]
        public void ActualUV(int position)
        {
            var m=New();m.SetFloat("_NB_UVModeFlag0Lo16",129);m.SetFloat("_NB_UVModeFlag0Hi16",8);
            m.SetFloat("_NB_UVModeFlagType0Lo16",0);m.SetFloat("_NB_UVModeFlagType0Hi16",4);
            var h=Host(new[]{m},"uv:"+position,out var root,out var item);string before=Snapshot(m);
            string edited=position<16?"_NB_UVModeFlag0Lo16":"_NB_UVModeFlag0Hi16";int shift=position<16?position:position-16;
            int lo=(int)m.GetFloat("_NB_UVModeFlag0Lo16"),hi=(int)m.GetFloat("_NB_UVModeFlag0Hi16");int flagsHi=(int)m.GetFloat("_NB_Flags1Hi16");
            Undo.FlushUndoRecordObjects();Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            try
            {
                Choose(h,item,3);
                Assert.That(m.GetFloat(edited),Is.EqualTo((position<16?lo:hi)|(3<<shift)));
                Assert.That(m.GetFloat(position<16?"_NB_UVModeFlag0Hi16":"_NB_UVModeFlag0Lo16"),Is.EqualTo(position<16?hi:lo));
                Assert.That(m.GetFloat("_NB_UVModeFlagType0Lo16"),Is.Zero);Assert.That(m.GetFloat("_NB_UVModeFlagType0Hi16"),Is.EqualTo(4));
                Assert.That(m.GetFloat("_NB_Flags1Hi16"),Is.EqualTo(flagsHi|(Bit("FLAG_BIT_PARTICLE_1_CYLINDER_CORDINATE")>>16)));
                Assert.That(((int)m.GetFloat("_NB_UVModeFlag0Lo16")&3),Is.EqualTo(lo&3),"Dissolve edit must retain MainTex slot.");
                Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(group);string after=Snapshot(m);h.Draw=null;
                Undo.PerformUndo();Assert.That(Snapshot(m),Is.EqualTo(before));Undo.PerformRedo();Assert.That(Snapshot(m),Is.EqualTo(after));
                h.Draw=()=>Call(root,"DrawGraphDissolveInputs",item);Choose(h,item,0);Assert.That(Snapshot(m),Is.EqualTo(before),"Original own-slot, aggregate bit and fold must restore.");
            }
            finally{h.Draw=null;Undo.RevertAllDownToGroup(group);}
        }
        // Service proof only: no native Popup input and no claim of menu selection.
        static void SetDissolveUVService(object root,int position,int value)
        {
            object mode=Enum.ToObject(Find("NBShader.NBShaderFlags").GetNestedType("UVMode",All),value);
            Assert.That((bool)Call(Value(root,"SyncService"),"TryApplyGraphDissolveUVMode",position,mode,true),Is.True);
        }
        [TestCase(14,TestName="G4Dissolve_ServiceUV_MapLoSliceCylinderUndoRestore")]
        [TestCase(16,TestName="G4Dissolve_ServiceUV_MaskHiSliceCylinderUndoRestore")]
        public void ServiceUV(int position)
        {
            var m=New();m.SetFloat("_NB_UVModeFlag0Lo16",129);m.SetFloat("_NB_UVModeFlag0Hi16",8);
            m.SetFloat("_NB_UVModeFlagType0Lo16",0);m.SetFloat("_NB_UVModeFlagType0Hi16",4);
            var h=Host(new[]{m},"uv:"+position,out var root,out var item);string before=Snapshot(m);
            string edited=position<16?"_NB_UVModeFlag0Lo16":"_NB_UVModeFlag0Hi16";int shift=position<16?position:position-16;
            int lo=(int)m.GetFloat("_NB_UVModeFlag0Lo16"),hi=(int)m.GetFloat("_NB_UVModeFlag0Hi16");int flagsHi=(int)m.GetFloat("_NB_Flags1Hi16");
            Undo.FlushUndoRecordObjects();Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            try
            {
                SetDissolveUVService(root,position,3);
                Assert.That(m.GetFloat(edited),Is.EqualTo((position<16?lo:hi)|(3<<shift)));
                Assert.That(m.GetFloat(position<16?"_NB_UVModeFlag0Hi16":"_NB_UVModeFlag0Lo16"),Is.EqualTo(position<16?hi:lo));
                Assert.That(m.GetFloat("_NB_UVModeFlagType0Lo16"),Is.Zero);Assert.That(m.GetFloat("_NB_UVModeFlagType0Hi16"),Is.EqualTo(4));
                Assert.That(m.GetFloat("_NB_Flags1Hi16"),Is.EqualTo(flagsHi|(Bit("FLAG_BIT_PARTICLE_1_CYLINDER_CORDINATE")>>16)));
                Assert.That(((int)m.GetFloat("_NB_UVModeFlag0Lo16")&3),Is.EqualTo(lo&3),"Dissolve edit must retain MainTex slot.");
                Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(group);string after=Snapshot(m);h.Draw=null;
                Undo.PerformUndo();Assert.That(Snapshot(m),Is.EqualTo(before));Undo.PerformRedo();Assert.That(Snapshot(m),Is.EqualTo(after));
                h.Draw=()=>Call(root,"DrawGraphDissolveInputs",item);SetDissolveUVService(root,position,0);Assert.That(Snapshot(m),Is.EqualTo(before),"Original own-slot, aggregate bit and fold must restore.");
            }
            finally{h.Draw=null;Undo.RevertAllDownToGroup(group);}
        }
        [Test]public void G4Dissolve_CylinderMultiMaterial_ActualPassiveDistinctMatrices()
        {
            var a=New();var b=New();
            foreach(var m in new[]{a,b}){m.SetFloat("_NB_UVModeFlag0Lo16",3<<14);m.SetFloat("_DissolveUVModeFoldOut",1);m.SetFloat("_NB_Flags1Hi16",153);}
            a.SetVector("_CylinderUVRotate",new Vector4(10,20,30,0));b.SetVector("_CylinderUVRotate",new Vector4(40,50,60,0));
            a.SetVector("_CylinderUVPosOffset",new Vector4(1,2,3,0));b.SetVector("_CylinderUVPosOffset",new Vector4(4,5,6,0));
            for(int i=0;i<4;i++){a.SetVector("_CylinderMatrix"+i,new Vector4(i+1,i+2,i+3,i+4));b.SetVector("_CylinderMatrix"+i,new Vector4(-i-1,-i-2,-i-3,-i-4));}
            var h=Host(new[]{a,b},"uv:14",out var root,out var item);string sa=Snapshot(a),sb=Snapshot(b);
            Send(h,new Event{type=EventType.Layout});Send(h,new Event{type=EventType.Repaint});
            Assert.That(Snapshot(a),Is.EqualTo(sa));Assert.That(Snapshot(b),Is.EqualTo(sb));
            Assert.That(a.GetVector("_CylinderMatrix0"),Is.Not.EqualTo(b.GetVector("_CylinderMatrix0")));
        }
        [Test]public void G4Dissolve_AllExpanded_OutOfRangeVectorSliders_ActualPassiveReadOnly()
        {
            var m=New();foreach(string n in new[]{"_DissolveBlockFoldOut","_DissolveMapFoldOut","_DissolveUVModeFoldOut","_DissolveLineFoldOut","_DissolveRampFoldOut","_DissolveMaskFoldOut","_DissolveMaskUVModeFoldOut"})m.SetFloat(n,1);
            m.SetFloat("_DissolveLineMaskToggle",1);m.SetFloat("_NB_Flags1Lo16",162);
            m.SetVector("_Dissolve",new Vector4(8,1,.5f,.5f));m.SetVector("_Dissolve_Vec2",new Vector4(8,9,.2f,.3f));
            m.SetVector("DissolveXRangeVec",new Vector4(-1,2,0,0));m.SetVector("Dissolve2XRangeVec",new Vector4(0,1,0,0));m.SetVector("Dissolve2YRangeVec",new Vector4(0,1,0,0));
            var h=Host(new[]{m},"_Dissolve_Toggle",out var root,out var item);string before=Snapshot(m);
            Send(h,new Event{type=EventType.Layout});Send(h,new Event{type=EventType.Repaint});Assert.That(Snapshot(m),Is.EqualTo(before));
        }
        [Test]public void G4Dissolve_Tier15Registry_ParentChildrenMapFallbackCompleteIntentRestore()
        {
            var m=New();m.SetFloat("_DissolveRampSourceMode",1);
            var applier=Find("NBShaders2.Editor.FeatureLevel.NBShaderFeatureLevelMaterialApplier");
            string[] gates={"_NB_TierAllowMask","_NB_TierAllowMask2","_NB_TierAllowMask3","_NB_TierAllowNoise","_NB_TierAllowNoiseMask","_NB_TierAllowProgramNoise","_NB_TierAllowProgramSimple","_NB_TierAllowProgramVoronoi","_NB_TierAllowFresnel","_NB_TierAllowEmission","_NB_TierAllowColorBlend","_NB_TierAllowDissolve","_NB_TierAllowDissolveMask","_NB_TierAllowDissolveRamp","_NB_TierAllowDissolveRampMap"};
            Assert.That((string[])applier.GetField("GraphSupportedGateProperties",All).GetValue(null),Is.EqualTo(gates),"This fixture is the actual accepted Overlay11 plus Dissolve4 registry contract.");
            for(int i=0;i<11;i++)m.SetFloat(gates[i],i%2);
            foreach(string n in gates.Skip(11))m.SetFloat(n,1);
            var snapshot=typeof(G4GraphGuiFeatureIntentTests).GetNestedType("Snapshot",BindingFlags.NonPublic).GetMethod("Read",All).Invoke(null,new object[]{m});
            string[][] policies={new[]{"_DISSOLVE_MASK","_DISSOLVE_RAMP","_DISSOLVE_RAMP_MAP"},new[]{"_DISSOLVE","_DISSOLVE_RAMP","_DISSOLVE_RAMP_MAP"},new[]{"_DISSOLVE","_DISSOLVE_MASK"},new[]{"_DISSOLVE","_DISSOLVE_MASK","_DISSOLVE_RAMP"},new[]{"_DISSOLVE","_DISSOLVE_MASK","_DISSOLVE_RAMP","_DISSOLVE_RAMP_MAP"}};
            int[][] expected={new[]{0,0,0,0},new[]{1,0,1,1},new[]{1,1,0,0},new[]{1,1,1,0},new[]{1,1,1,1}};
            for(int state=0;state<policies.Length;state++)
            {
                object[] args={m,Enum.ToObject(Find("NBShader.NBShaderFeatureTier"),3),policies[state],false};
                Assert.That((bool)applier.GetMethod("ApplyGraphDissolveGroup",All).Invoke(null,args),Is.True);
                for(int i=0;i<15;i++)Assert.That(m.GetFloat(gates[i]),Is.EqualTo(i<11?i%2:expected[state][i-11]));
                Assert.That(m.GetFloat("_DissolveRampSourceMode"),Is.EqualTo(1));
                snapshot.GetType().GetMethod("AssertSame",All).Invoke(snapshot,new object[]{m,"Full serialized raw intent, keywords and all non-owned properties unchanged",gates.Skip(11).ToArray()});
            }
        }
    }
}
