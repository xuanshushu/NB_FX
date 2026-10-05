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
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace NBFX.Baseline.Tests
{
    // Reuses original leaves, actual Root event owner, state snapshots and ABC
    // harness. Native popup selection remains manual; no simulated popup pass.
    public sealed class G4GraphDepthSharedTierTests
    {
        const BindingFlags All=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance;
        static readonly string[] Toggles={"_DistanceFade_Toggle","_SoftParticlesEnabled","_DepthOutline_Toggle"};
        static readonly string[] Gates={"_NB_TierAllowDistanceFade","_NB_TierAllowSoftParticles","_NB_TierAllowDepthOutline"};
        static readonly string[] Keywords={"_DISTANCE_FADE","_SOFTPARTICLES_ON","_DEPTH_OUTLINE"};
        static readonly string[] Folds={"_DistanceFadeFoldOut","_SoftParticlesFoldOut","_DepthOutlineBlockFoldOut"};
        static readonly string[] Blocks={"_graphDistanceFadeBlock","_graphSoftParticlesBlock","_graphDepthOutlineItem"};
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
            m.SetFloat("_NB_ForceNoMipFlagsLo16",1.25f);m.SetFloat("_NB_ForceNoMipFlagsHi16",123.25f);
            return m;
        }
        object Root(params Material[] materials)
        {var helper=new G4GraphPersistentGateTierTests();helpers.Add(helper);return helper.GetType().GetMethod("Root",All).Invoke(helper,new object[]{materials});}
        static IEnumerable<object> Descendants(object item)
        {yield return item;foreach(object child in (System.Collections.IEnumerable)Field(item,"ChildrenItemList"))foreach(var nested in Descendants(child))yield return nested;}
        NBFXMainTexGUIEventHost Host(object root,int feature,out object item)
        {
            var h=ScriptableObject.CreateInstance<NBFXMainTexGUIEventHost>();owned.Add(h);
            h.hideFlags=HideFlags.HideAndDontSave;h.position=new Rect(20,20,680,900);
            h.SetupCommand="NBFX_Depth_Setup_"+Guid.NewGuid().ToString("N");object actual=null;
            h.Setup=()=>{Assert.That(Call(root,"InitializeGraphDepthFeaturesInputs"),Is.True);actual=Field(root,Blocks[feature]);};
            h.ShowUtility();h.SendEvent(new Event{type=EventType.ExecuteCommand,commandName=h.SetupCommand});Check(h);Assert.That(h.Initialized,Is.True);
            item=actual;h.Draw=()=>Call(root,"DrawGraphDepthFeaturesInputs",new object[]{null});return h;
        }
        static void Check(NBFXMainTexGUIEventHost h){if(h.Failure!=null)ExceptionDispatchInfo.Capture(h.Failure).Throw();}
        static void Send(NBFXMainTexGUIEventHost h,Event e)
        {
            var type=e.rawType;h.Counts.TryGetValue(type,out int before);h.SendEvent(e);Check(h);
            Assert.That(e.rawType,Is.EqualTo(type));Assert.That(h.Counts.TryGetValue(type,out int after)&&after>before,Is.True,"Actual original Root receives event.");
        }
        static string[] Raw()=>(string[])TypeOf("NBShader.NBShaderFeatureCatalog").GetField("RawKeywords",All).GetValue(null);
        static bool Apply(Material m,int denied,out bool changed)
        {
            var allowed=Raw().Where(k=>denied<0 || (denied<3?k!=Keywords[denied]:!Keywords.Contains(k))).ToArray();
            object[] args={m,Enum.ToObject(TypeOf("NBShader.NBShaderFeatureTier"),3),allowed,false};
            bool result=(bool)TypeOf("NBShaders2.Editor.FeatureLevel.NBShaderFeatureLevelMaterialApplier").GetMethod("ApplyGraphDepthFeaturesGroup",All).Invoke(null,args);changed=(bool)args[3];return result;
        }
        [TearDown] public void Cleanup()
        {
            foreach(var h in owned.OfType<NBFXMainTexGUIEventHost>()){h.Draw=null;h.Setup=null;h.Close();}
            foreach(var helper in helpers)helper.Cleanup();helpers.Clear();
            foreach(var o in owned.AsEnumerable().Reverse())if(o)Object.DestroyImmediate(o);owned.Clear();
        }
        [Test] public void G4DepthShared_CPU_OriginalLeavesTypesAndMixedPassive()
        {
            var a=Material();var b=Material();
            foreach(var m in new[]{a,b})for(int i=0;i<3;++i){m.SetFloat(Toggles[i],1);m.SetFloat(Folds[i],1);}
            a.SetVector("_Fade",new Vector4(2,4,71,81));b.SetVector("_Fade",new Vector4(3,6,72,82));
            a.SetVector("_SoftParticleFadeParams",new Vector4(.1f,.7f,73,83));b.SetVector("_SoftParticleFadeParams",new Vector4(.2f,.8f,74,84));
            a.SetVector("_DepthOutline_Vec",new Vector4(.1f,.9f,75,85));b.SetVector("_DepthOutline_Vec",new Vector4(.2f,.6f,76,86));
            var root=Root(a,b);var beforeA=Snapshot.Read(a);var beforeB=Snapshot.Read(b);var h=Host(root,0,out var first);
            for(int i=0;i<3;++i)
            {
                var block=Field(root,Blocks[i]);Assert.That(block.GetType().Name,Is.EqualTo(i==2?"DepthOutlineFeatureItem":"PropertyToggleBlockItem"));
                Assert.That(Descendants(block).Count(o=>o.GetType().Name=="Vector2LineItem"),Is.EqualTo(1));
                Assert.That(Descendants(block).Any(o=>Field(o,"PropertyName") as string=="_DepthDecal_Toggle"),Is.False);
                foreach(string n in new[]{Toggles[i],Folds[i],Gates[i]})Assert.That(a.shader.GetPropertyType(a.shader.FindPropertyIndex(n)),Is.EqualTo(ShaderPropertyType.Float),n);
                Assert.That(a.shader.GetPropertyDefaultFloatValue(a.shader.FindPropertyIndex(Gates[i])),Is.EqualTo(1));
            }
            foreach(string n in new[]{"_Fade","_SoftParticleFadeParams","_DepthOutline_Vec"})Assert.That(a.shader.GetPropertyType(a.shader.FindPropertyIndex(n)),Is.EqualTo(ShaderPropertyType.Vector));
            Assert.That(a.shader.GetPropertyType(a.shader.FindPropertyIndex("_DepthOutline_Color")),Is.EqualTo(ShaderPropertyType.Color));
            Send(h,new Event{type=EventType.Layout});Send(h,new Event{type=EventType.Repaint});h.Draw=null;
            beforeA.AssertSame(a,"All shared depth leaves mixed passive A preserve raw vectors and state.");beforeB.AssertSame(b,"All shared depth leaves mixed passive B preserve raw vectors and state.");
        }
        [Test] public void G4DepthShared_CPU_IndependentTierDenyRestoreFinalValidate()
        {
            var m=Material();foreach(string p in Toggles)m.SetFloat(p,1);bool changed;
            Assert.That(Apply(m,-1,out changed),Is.True);var before=Snapshot.Read(m);
            for(int denied=0;denied<3;++denied)
            {
                Assert.That(Apply(m,denied,out changed),Is.True);for(int i=0;i<3;++i){Assert.That(m.GetFloat(Gates[i]),Is.EqualTo(i==denied?0f:1f));Assert.That(m.GetFloat(Toggles[i]),Is.EqualTo(1));}
                before.AssertSame(m,"Exactly the denied leaf gate changes.",Gates[denied]);
                Assert.That(Apply(m,-1,out changed),Is.True);before.AssertSame(m,"Exact restore after independent deny.");
            }
            Assert.That(Apply(m,3,out changed),Is.True);Assert.That(Gates.All(g=>m.GetFloat(g)==0),Is.True);before.AssertSame(m,"Only three derived gates denied.",Gates);
            Assert.That(Apply(m,-1,out changed),Is.True);before.AssertSame(m,"Full restore.");Assert.That(Apply(m,-1,out changed),Is.True);Assert.That(changed,Is.False);
            var settings=TypeOf("NBShaders2.Editor.FeatureLevel.NBShaderFeatureLevelProjectSettings");var instance=settings.GetProperty("instance",All|BindingFlags.FlattenHierarchy).GetValue(null);
            for(int tier=0;tier<=3;++tier)
            {
                var level=Enum.ToObject(TypeOf("NBShader.NBShaderFeatureTier"),tier);
                var allowed=((IEnumerable<string>)settings.GetMethod("GetAllowedKeywordSetForBuildInfoNoSave",All).Invoke(instance,new[]{level})).ToArray();
                m.SetFloat("_NBShaderFeatureTier",tier);G4SpecDebugFixture.Validate(m);
                for(int i=0;i<3;++i){Assert.That(m.GetFloat(Gates[i]),Is.EqualTo(allowed.Contains(Keywords[i])?1f:0f));Assert.That(m.GetFloat(Toggles[i]),Is.EqualTo(1));}
            }
            Assert.That(m.GetFloat("_NB_CustomDataFlag3Hi16"),Is.EqualTo(123.25f));
        }
        [TestCase(0,TestName="G4DepthShared_GUI_DistanceToggleUndoRedo")]
        [TestCase(1,TestName="G4DepthShared_GUI_SoftToggleUndoRedo")]
        [TestCase(2,TestName="G4DepthShared_GUI_OutlineToggleUndoRedo")]
        public void ActualToggle(int feature)
        {
            var m=Material();var root=Root(m);var h=Host(root,feature,out var block);
            // Select the same original leaf through the real Root owner; no popup.
            h.Draw=()=>Call(root,"DrawGraphDepthFeaturesInputs",block);
            Send(h,new Event{type=EventType.Layout});Send(h,new Event{type=EventType.Repaint});
            var rect=(Rect)Field(block,"ControlRect");Assert.That(rect.width>0&&rect.height>0,Is.True);var pos=new Vector2(rect.x+6,rect.center.y);var before=Snapshot.Read(m);Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            try
            {
                Send(h,new Event{type=EventType.MouseDown,button=0,mousePosition=pos});Send(h,new Event{type=EventType.MouseUp,button=0,mousePosition=pos});
                Assert.That(m.GetFloat(Toggles[feature]),Is.EqualTo(1));Assert.That(m.GetFloat(Gates[feature]),Is.EqualTo(1));
                Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(group);var after=Snapshot.Read(m);h.Draw=null;Undo.PerformUndo();before.AssertSame(m,"Actual Root-owned depth toggle full Undo.");Undo.PerformRedo();after.AssertSame(m,"Actual Root-owned depth toggle full Redo.");
            }
            finally{h.Draw=null;Undo.RevertAllDownToGroup(group);}
        }
        static object Original(string name,params object[] args)=>typeof(G4GraphMeshParityTests).GetMethod(name,All).Invoke(null,args);
        [TestCase(0,TestName="G4DepthShared_GPU_DistanceDenyRestore_ortho")]
        [TestCase(1,TestName="G4DepthShared_GPU_SoftDepthControlDenyRestore_ortho")]
        [TestCase(2,TestName="G4DepthShared_GPU_OutlineDepthControlDenyRestore_ortho")]
        public void ActualConsumer(int feature)
        {
            string[] names={"distance","soft","depth-outline"};string id="depth-shared-"+names[feature]+"-ortho";
            using(var h=new G4SpecDebugFixture.Harness(id,true))
            {
                var texture=(Texture2D)Original("MakeTexture");owned.Add(texture);
                for(int i=0;i<3;++i)Original("Configure",h.materials[i],i==2,texture);
                foreach(int i in new[]{0,1})Original("ConfigureCase",names[feature],h.materials[2],h.materials[i],(uint)0,(uint)1,texture);
                for(int i=0;i<3;++i)G4SpecDebugFixture.Harness.RestoreForward(h.materials[i],i==2);
                h.renderer.transform.position=new Vector3(0,0,.5f);h.renderer.transform.rotation=Quaternion.identity;
                GameObject board=null;
                if(feature!=0)
                {
                    board=GameObject.CreatePrimitive(PrimitiveType.Quad);owned.Add(board);board.layer=h.renderer.gameObject.layer;
                    board.transform.position=new Vector3(0,0,.45f);board.transform.localScale=new Vector3(2,2,1);SceneManager.MoveGameObjectToScene(board,h.camera.scene);
                    var shader=Shader.Find("Universal Render Pipeline/Unlit");Assert.That(shader,Is.Not.Null);var material=new Material(shader);owned.Add(material);material.SetColor("_BaseColor",Color.white);material.SetFloat("_Cull",0);
                    var renderer=board.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
                }
                h.camera.GetUniversalAdditionalCameraData().requiresDepthTexture=true;
                var empty=h.Snap("empty");bool changed;Assert.That(Apply(h.materials[2],-1,out changed),Is.True);var before=Snapshot.Read(h.materials[2]);
                int count=feature==0?3:4;var frames=new Color[count][][];var repeats=new Color[count][][];
                for(int stage=0;stage<count;++stage)
                {
                    bool on=stage!=1;if(board)board.transform.position=new Vector3(0,0,stage==2?-.5f:.45f);
                    Assert.That(Apply(h.materials[2],on?-1:feature,out changed),Is.True);before.AssertSame(h.materials[2],"Only this feature's derived gate may change.",Gates[feature]);
                    frames[stage]=new Color[3][];repeats[stage]=new Color[3][];
                    for(int i=0;i<3;++i)
                    {
                        if(i!=2)G4SpecDebugFixture.SetKeyword(h.materials[i],Keywords[feature],on);
                        frames[stage][i]=h.Snap("ABC"[i]+"-"+stage,h.materials[i]);repeats[stage][i]=h.Snap("ABC"[i]+"-"+stage+"-repeat",h.materials[i]);
                    }
                }
                var metrics=new G4SpecDebugFixture.Metrics{caseId=id,scope="Original distance/soft/depth-outline inputs and near opaque board; independent Tier deny/restore. Soft/outline also move same opaque board as a real camera-depth control. No DepthDecal/Stencil/Player claim.",finite=G4SpecDebugFixture.Finite(empty)&&frames.SelectMany(f=>f).Concat(repeats.SelectMany(f=>f)).All(G4SpecDebugFixture.Finite),ab=frames.Select(f=>G4SpecDebugFixture.Delta(f[0],f[1])).ToArray(),bc=frames.Select(f=>G4SpecDebugFixture.Delta(f[1],f[2])).ToArray(),repeat=Enumerable.Range(0,count).SelectMany(s=>Enumerable.Range(0,3).Select(i=>G4SpecDebugFixture.Delta(frames[s][i],repeats[s][i]))).ToArray(),response=Enumerable.Range(0,3).Select(i=>G4SpecDebugFixture.Delta(frames[0][i],frames[1][i])).ToArray(),restore=Enumerable.Range(0,3).Select(i=>G4SpecDebugFixture.Delta(frames[0][i],frames[count-1][i])).ToArray(),visible=frames.SelectMany(f=>f).Select(f=>G4SpecDebugFixture.Visible(f,empty)).ToArray()};
                h.SaveAndAssert(metrics);Assert.That(metrics.response.All(v=>v>.01f),Is.True,"Independent actual ABC Tier response.");
                if(feature!=0)Assert.That(Enumerable.Range(0,3).All(i=>G4SpecDebugFixture.Delta(frames[0][i],frames[2][i])>.01f),Is.True,"Actual scene-depth source must respond to controlled opaque board distance.");
                before.AssertSame(h.materials[2],"All material inputs and gates restored.");
            }
        }
    }
}
