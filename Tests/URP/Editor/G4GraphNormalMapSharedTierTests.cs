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
    // Existing original Bump factory, existing UV transaction and existing render harness.
    // UV/Wrap native-popup selections remain manual; tests never MouseDown a popup.
    public sealed class G4GraphNormalMapSharedTierTests
    {
        const BindingFlags All=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
        const string Toggle="_BumpMapToggle",Gate="_NB_TierAllowNormalMap",Mask="_BumpMapMaskMode";
        readonly List<Object> owned=new List<Object>();readonly List<G4GraphPersistentGateTierTests> helpers=new List<G4GraphPersistentGateTierTests>();
        static Type TypeOf(string name)=>G4SpecDebugFixture.FindType(name);
        static object Call(object o,string name,params object[] args)=>o.GetType().GetMethod(name,All).Invoke(o,args);
        static object Property(object o,string name)=>o.GetType().GetProperty(name,All).GetValue(o);
        static object Field(object o,string name)
        {for(Type t=o.GetType();t!=null;t=t.BaseType){var f=t.GetField(name,All|BindingFlags.DeclaredOnly);if(f!=null)return f.GetValue(o);}Assert.Fail("Missing actual original field "+name);return null;}
        [OneTimeSetUp] public void Preflight()
        {
            Assert.That(Path.GetFullPath(Application.dataPath),Is.EqualTo(Path.GetFullPath(@"D:\UnityProject\NBUnityProject\.utmp\NBFXMeshValidation-20261002\Assets")).IgnoreCase);
            for(int i=0;i<SceneManager.sceneCount;++i){var scene=SceneManager.GetSceneAt(i);Assert.That((scene.name+"/"+scene.path).IndexOf("TAI",StringComparison.OrdinalIgnoreCase),Is.LessThan(0));}
            var shader=AssetDatabase.LoadAssetAtPath<Shader>(G4SpecDebugFixture.GraphPath);Assert.That(shader&&shader.isSupported,Is.True); // Root owns installation/import; no forced reimport.
        }
        sealed class Snapshot
        {
            readonly object value;static Type Shared=>typeof(G4GraphGuiFeatureIntentTests).GetNestedType("Snapshot",BindingFlags.NonPublic);
            Snapshot(object value){this.value=value;}
            public static Snapshot Read(Material m)=>new Snapshot(Shared.GetMethod("Read",All).Invoke(null,new object[]{m}));
            public void AssertSame(Material m,string label,params string[] allowed)=>Shared.GetMethod("AssertSame",All).Invoke(value,new object[]{m,label,allowed});
        }
        Material Material()
        {var m=G4SpecDebugFixture.NewGraph();owned.Add(m);m.SetFloat("_NB_Flags0Hi16",2.25f);m.SetFloat("_NB_Flags0Lo16",17.25f);m.SetFloat("_NB_Flags1Hi16",64.25f);m.SetFloat("_NB_UVModeFlag0Lo16",17.25f);m.SetFloat("_NB_UVModeFlag0Hi16",2.25f);m.SetFloat("_NB_UVModeFlagType0Hi16",.25f);m.SetTexture("_BumpTex",Texture2D.whiteTexture);return m;}
        object Root(params Material[] materials)
        {var helper=new G4GraphPersistentGateTierTests();helpers.Add(helper);return helper.GetType().GetMethod("Root",All).Invoke(helper,new object[]{materials});}
        static object Tier(int value)=>Enum.ToObject(TypeOf("NBShader.NBShaderFeatureTier"),value);
        static string[] Raw()=> (string[])TypeOf("NBShader.NBShaderFeatureCatalog").GetField("RawKeywords",All).GetValue(null);
        static bool Apply(Material m,bool full,out bool changed)
        {object[] args={m,Tier(3),full?Raw():Array.Empty<string>(),false};bool result=(bool)TypeOf("NBShaders2.Editor.FeatureLevel.NBShaderFeatureLevelMaterialApplier").GetMethod("ApplyGraphNormalMapGroup",All).Invoke(null,args);changed=(bool)args[3];return result;}
        [TearDown] public void Cleanup()
        {foreach(var h in owned.OfType<NBFXMainTexGUIEventHost>()){h.Draw=null;h.Setup=null;h.Close();}foreach(var helper in helpers)helper.Cleanup();helpers.Clear();foreach(var o in owned.AsEnumerable().Reverse())if(o)Object.DestroyImmediate(o);owned.Clear();}
        static IEnumerable<object> Descendants(object item)
        {yield return item;foreach(object child in (System.Collections.IEnumerable)Field(item,"ChildrenItemList"))foreach(object descendant in Descendants(child))yield return descendant;}
        NBFXMainTexGUIEventHost Host(object root,out object actual)
        {
            var h=ScriptableObject.CreateInstance<NBFXMainTexGUIEventHost>();owned.Add(h);h.hideFlags=HideFlags.HideAndDontSave;h.position=new Rect(20,20,640,680);h.SetupCommand="NBFX_NormalMap_Setup_"+Guid.NewGuid().ToString("N");object item=null;
            h.Setup=()=>{Assert.That(Call(root,"InitializeGraphNormalMapInputs"),Is.True);item=Field(root,"_graphNormalMapBlock");};h.ShowUtility();h.SendEvent(new Event{type=EventType.ExecuteCommand,commandName=h.SetupCommand});Check(h);Assert.That(h.Initialized,Is.True);actual=item;h.Draw=()=>Call(root,"DrawGraphNormalMapInputs");return h;
        }
        static void Check(NBFXMainTexGUIEventHost h){if(h.Failure!=null)ExceptionDispatchInfo.Capture(h.Failure).Throw();}
        static void Send(NBFXMainTexGUIEventHost h,Event e)
        {var type=e.rawType;Assert.That(type,Is.Not.EqualTo(EventType.Ignore).And.Not.EqualTo(EventType.Used));h.Counts.TryGetValue(type,out int before);h.SendEvent(e);Check(h);Assert.That(e.rawType,Is.EqualTo(type));Assert.That(h.Counts.TryGetValue(type,out int after)&&after>before,Is.True,"Actual original Root GUI receives event.");}
        [Test] public void G4NormalShared_CPU_ActualTypesAndOriginalFactory()
        {
            var m=Material();var root=Root(m);var before=Snapshot.Read(m);var h=Host(root,out var block);h.Draw=null;before.AssertSame(m,"Factory/schema check never mutates Material.");
            Assert.That(block.GetType().Name,Is.EqualTo("PropertyToggleBlockItem"));foreach(string name in new[]{Toggle,Mask,"_BumpScale",Gate,"_BumpToggleFoldOut","_BumpTexFoldOut","_BumpUVModeFoldOut"})Assert.That(m.shader.GetPropertyType(m.shader.FindPropertyIndex(name)),Is.EqualTo(ShaderPropertyType.Float),name);
            Assert.That(m.shader.GetPropertyType(m.shader.FindPropertyIndex("BumpScaleRangeVec")),Is.EqualTo(ShaderPropertyType.Vector));Assert.That(m.shader.GetPropertyDefaultVectorValue(m.shader.FindPropertyIndex("BumpScaleRangeVec")),Is.EqualTo(new Vector4(-1,1,0,0)));
            foreach(string name in new[]{"_BumpToggleFoldOut","_BumpTexFoldOut","_BumpUVModeFoldOut"})Assert.That(m.shader.GetPropertyDefaultFloatValue(m.shader.FindPropertyIndex(name)),Is.EqualTo(0));
            var items=Descendants(block).ToArray();Assert.That(items.Count(o=>o.GetType().Name=="UVModeSelectItem"),Is.EqualTo(1));Assert.That(items.Any(o=>o.GetType().Name=="CustomDataSelectItem"),Is.False,"Original normal map owns no local CD nibble.");var uv=items.Single(o=>o.GetType().Name=="UVModeSelectItem");Assert.That((int)Field(uv,"_uvModeBitPos"),Is.EqualTo(24));var slider=items.Single(o=>o.GetType().Name=="ShaderGUISliderItem");Assert.That((bool)Field(slider,"WriteOnlyOnInteractiveChange"),Is.True);
        }
        [Test] public void G4NormalShared_CPU_DenyRestoreNoOp()
        {
            var m=Material();m.SetFloat(Toggle,1);bool changed;Assert.That(Apply(m,true,out changed),Is.True);var before=Snapshot.Read(m);Assert.That(Apply(m,false,out changed),Is.True);Assert.That(m.GetFloat(Gate),Is.EqualTo(0));before.AssertSame(m,"Only normal gate is denied; saved intent and words remain.",Gate);Assert.That(Apply(m,true,out changed),Is.True);before.AssertSame(m,"Full normal gate restore.");Assert.That(Apply(m,true,out changed),Is.True);Assert.That(changed,Is.False);
        }
        [Test] public void G4NormalShared_CPU_OriginalMaskAndUVServiceUndoRedo()
        {
            var m=Material();var root=Root(m);Assert.That(Call(root,"InitializeGraphNormalMapInputs"),Is.True);var sync=Property(root,"SyncService");var before=Snapshot.Read(m);Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            try
            {
                // Calls actual original owned service; never opens a native popup and never test-wraps RecordObjects.
                Assert.That(Call(sync,"TryApplyGraphNormalMapMaskEdit",true),Is.True);var mode=Enum.ToObject(TypeOf("NBShader.NBShaderFlags").GetNestedType("UVMode",All),3);Assert.That(Call(sync,"TryApplyGraphNormalMapUVMode",mode,true),Is.True);
                Assert.That(m.GetFloat(Mask),Is.EqualTo(1));Assert.That((Mathf.RoundToInt(m.GetFloat("_NB_Flags0Hi16"))&32),Is.EqualTo(32));Assert.That(m.GetFloat("_NB_Flags0Lo16"),Is.EqualTo(17.25f));Assert.That(m.GetFloat("_NB_UVModeFlag0Lo16"),Is.EqualTo(17.25f));Assert.That(m.GetFloat("_NB_UVModeFlagType0Hi16"),Is.EqualTo(.25f));
                Assert.That((Mathf.RoundToInt(m.GetFloat("_NB_UVModeFlag0Hi16"))&(3<<8)),Is.EqualTo(3<<8));Assert.That((Mathf.RoundToInt(m.GetFloat("_NB_UVModeFlag0Hi16"))&~(3<<8)),Is.EqualTo(2));
                before.AssertSame(m,"Original bit21/UVpos24 plus same derived UV state only.",Mask,"_NB_Flags0Hi16","_NB_UVModeFlag0Hi16","_NB_UVModeFlagType0Hi16","_NB_Flags1Hi16","_BumpUVModeFoldOut");
                var after=Snapshot.Read(m);Assert.That(Call(sync,"TryApplyGraphNormalMapMaskEdit",true),Is.True);Assert.That(Call(sync,"TryApplyGraphNormalMapUVMode",mode,true),Is.True);after.AssertSame(m,"Decoded no-op preserves every raw half and all state.");
                Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(group);Undo.PerformUndo();before.AssertSame(m,"Actual owned service full Undo.");Undo.PerformRedo();after.AssertSame(m,"Actual owned service full Redo.");
            }
            finally{Undo.RevertAllDownToGroup(group);}
        }
        [TestCase(false,TestName="G4NormalShared_GUI_CylinderOutOfRangePassiveReadOnly")]
        [TestCase(true,TestName="G4NormalShared_GUI_MixedCylinderOutOfRangePassiveReadOnly")]
        public void Passive(bool mixed)
        {
            var a=Material();a.SetFloat(Toggle,1);a.SetFloat("_BumpToggleFoldOut",1);a.SetFloat("_BumpTexFoldOut",1);a.SetFloat("_BumpUVModeFoldOut",1);a.SetFloat("_BumpScale",2);a.SetFloat("_NB_UVModeFlag0Hi16",3<<8);a.SetVector("_CylinderUVRotate",new Vector4(4,8,12,0));a.SetVector("_CylinderMatrix0",new Vector4(2,3,4,5));Material b=null;
            if(mixed){b=Material();b.SetFloat(Toggle,1);b.SetFloat("_BumpToggleFoldOut",1);b.SetFloat("_BumpTexFoldOut",1);b.SetFloat("_BumpUVModeFoldOut",1);b.SetFloat("_BumpScale",3);b.SetFloat("_NB_UVModeFlag0Hi16",3<<8);b.SetVector("BumpScaleRangeVec",new Vector4(-.5f,.5f,7,9));b.SetVector("_CylinderUVRotate",new Vector4(12,24,36,0));b.SetVector("_CylinderMatrix0",new Vector4(9,8,7,6));}
            var root=mixed?Root(a,b):Root(a);var h=Host(root,out var block);var sa=Snapshot.Read(a);var sb=b?Snapshot.Read(b):null;Send(h,new Event{type=EventType.Layout});Send(h,new Event{type=EventType.Repaint});sa.AssertSame(a,"Original whole normal subtree cannot normalize out-of-range or recompute Cylinder during passive paint.");if(b)sb.AssertSame(b,"Distinct mixed values/matrices/raw words stay exact.");
        }
        [TestCase(false,TestName="G4NormalShared_GUI_ActualMainToggleUndoRedo")]
        [TestCase(true,TestName="G4NormalShared_GUI_ActualMaskModeToggleUndoRedo")]
        public void ActualToggle(bool mask)
        {
            var m=Material();m.SetFloat(Toggle,mask?1:0);m.SetFloat(Mask,0);m.SetFloat("_BumpToggleFoldOut",mask?1:0);m.SetFloat("_BumpTexFoldOut",mask?1:0);G4SpecDebugFixture.Validate(m);var root=Root(m);var h=Host(root,out var block);Send(h,new Event{type=EventType.Layout});Send(h,new Event{type=EventType.Repaint});object item=mask?Descendants(block).Single(o=>o.GetType().Name=="ToggleItem"&&(string)Field(o,"PropertyName")==Mask):block;var rect=(Rect)Field(item,"ControlRect");Assert.That(rect.width>0&&rect.height>0,Is.True);var click=new Vector2(rect.x+6,rect.center.y);var before=Snapshot.Read(m);Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            try
            {
                // Actual Root pre-event Undo owner, only a non-menu original Toggle.
                Send(h,new Event{type=EventType.MouseDown,button=0,mousePosition=click});Send(h,new Event{type=EventType.MouseUp,button=0,mousePosition=click});Assert.That(m.GetFloat(mask?Mask:Toggle),Is.EqualTo(1));
                if(mask){Assert.That((Mathf.RoundToInt(m.GetFloat("_NB_Flags0Hi16"))&32),Is.EqualTo(32));before.AssertSame(m,"Only original mask-mode bit and mirror.",Mask,"_NB_Flags0Hi16");}
                else{Assert.That(m.GetFloat(Gate),Is.EqualTo(1));string[] gates=(string[])TypeOf("NBShaders2.Editor.FeatureLevel.NBShaderFeatureLevelMaterialApplier").GetField("GraphSupportedGateProperties",All).GetValue(null);before.AssertSame(m,"Only original normal toggle and supported derived gates.",gates.Concat(new[]{Toggle}).ToArray());}
                Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(group);var after=Snapshot.Read(m);h.Draw=null;Undo.PerformUndo();before.AssertSame(m,"Actual normal non-menu toggle complete Undo.");Undo.PerformRedo();after.AssertSame(m,"Actual normal non-menu toggle complete Redo.");
            }
            finally{h.Draw=null;Undo.RevertAllDownToGroup(group);}
        }
        static object Normal(string name,params object[] args)=>typeof(G4GraphNormalMapTests).GetMethod(name,All).Invoke(null,args);
        [TestCase(true,TestName="G4NormalShared_GPU_FresnelDenyRestore_ortho")]
        [TestCase(false,TestName="G4NormalShared_GPU_FresnelDenyRestore_perspective")]
        public void ForwardDenyRestore(bool ortho)
        {
            string id="normal-shared-tier-"+(ortho?"ortho":"perspective");using(var h=new G4SpecDebugFixture.Harness(id,ortho))
            {
                var empty=h.Snap("empty");var map=(Texture2D)Normal("MakeNormalTexture",false);owned.Add(map);var matcap=(Texture2D)Normal("MakeMatCapTexture");owned.Add(matcap);var mesh=(Mesh)Normal("BuildCurvedMesh");owned.Add(mesh);h.renderer.GetComponent<MeshFilter>().sharedMesh=mesh;
                for(int i=0;i<3;++i)Normal("Configure",h.materials[i],i==2,true,false,map,matcap);Normal("Apply",h.materials[0],h.materials[1],h.materials[2],Normal("MakeState","fresnel-uv0"));G4SpecDebugFixture.Validate(h.materials[2]);for(int i=0;i<3;++i)G4SpecDebugFixture.Harness.RestoreForward(h.materials[i],i==2);
                bool changed;Assert.That(Apply(h.materials[2],true,out changed),Is.True);var before=Snapshot.Read(h.materials[2]);var frames=new Color[3][][];var repeats=new Color[3][][];
                for(int stage=0;stage<3;++stage)
                {
                    Assert.That(Apply(h.materials[2],stage!=1,out changed),Is.True);before.AssertSame(h.materials[2],"Only independent normal gate changes, original Fresnel intent/flags/pass retained.",Gate);frames[stage]=new Color[3][];repeats[stage]=new Color[3][];
                    for(int i=0;i<3;++i){if(i!=2)G4SpecDebugFixture.SetKeyword(h.materials[i],"_NORMALMAP",stage!=1);frames[stage][i]=h.Snap("ABC"[i]+"-"+stage,h.materials[i]);repeats[stage][i]=h.Snap("ABC"[i]+"-"+stage+"-repeat",h.materials[i]);}
                }
                var metrics=new G4SpecDebugFixture.Metrics{caseId=id,scope="Only original normal sampling/TBN through independently responding Fresnel main Forward. Existing normal map/mesh/config shared. No all lighting/MatCap/refraction/depth/shadow/Player claim.",finite=G4SpecDebugFixture.Finite(empty)&&frames.SelectMany(f=>f).Concat(repeats.SelectMany(f=>f)).All(G4SpecDebugFixture.Finite),ab=frames.Select(f=>G4SpecDebugFixture.Delta(f[0],f[1])).ToArray(),bc=frames.Select(f=>G4SpecDebugFixture.Delta(f[1],f[2])).ToArray(),repeat=Enumerable.Range(0,3).SelectMany(s=>Enumerable.Range(0,3).Select(i=>G4SpecDebugFixture.Delta(frames[s][i],repeats[s][i]))).ToArray(),response=Enumerable.Range(0,3).Select(i=>G4SpecDebugFixture.Delta(frames[0][i],frames[1][i])).ToArray(),restore=Enumerable.Range(0,3).Select(i=>G4SpecDebugFixture.Delta(frames[0][i],frames[2][i])).ToArray(),visible=frames.SelectMany(f=>f).Select(f=>G4SpecDebugFixture.Visible(f,empty)).ToArray()};h.SaveAndAssert(metrics);Assert.That(metrics.response.All(v=>v>.01f),Is.True);before.AssertSame(h.materials[2],"Complete restored normal gate + original state.");
            }
        }
    }
}
