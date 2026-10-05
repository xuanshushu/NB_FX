using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace NBFX.Baseline.Tests
{
    // Existing original Toggle/Root event host and original QCM render-state fixture only.
    public sealed class G4GraphStencilWithoutPlayerGuiTests
    {
        const BindingFlags All=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance;
        static readonly string[] Changed={"_Stencil","_StencilComp","_StencilOp","_StencilFail","_StencilZFail","_StencilReadMask","_StencilWriteMask","_StencilKeyIndex","_CustomStencilTest","_StencilWithoutPlayerToggle"};
        readonly List<Object> owned=new List<Object>();readonly List<G4GraphPersistentGateTierTests> helpers=new List<G4GraphPersistentGateTierTests>();
        static Type Find(string name)=>G4SpecDebugFixture.FindType(name);
        static object Call(object target,string name,params object[] args)
        {var method=target.GetType().GetMethod(name,All);Assert.That(method,Is.Not.Null,name);try{return method.Invoke(target,args);}catch(TargetInvocationException e){System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e.InnerException??e).Throw();throw;}}
        static object Field(object target,string name)
        {for(Type type=target.GetType();type!=null;type=type.BaseType){var field=type.GetField(name,All|BindingFlags.DeclaredOnly);if(field!=null)return field.GetValue(target);}Assert.Fail(name);return null;}
        static object Sync(object root)=>root.GetType().GetProperty("SyncService",All).GetValue(root);
        sealed class Snapshot
        {
            readonly object value;static Type Shared=>typeof(G4GraphGuiFeatureIntentTests).GetNestedType("Snapshot",BindingFlags.NonPublic);
            Snapshot(object value){this.value=value;}public static Snapshot Read(Material material){var method=Shared.GetMethod("Read",All);Assert.That(method,Is.Not.Null);return new Snapshot(method.Invoke(null,new object[]{material}));}
            public void Same(Material material,string label,params string[] allow){var method=Shared.GetMethod("AssertSame",All);Assert.That(method,Is.Not.Null);method.Invoke(value,new object[]{material,label,allow});}
        }
        [OneTimeSetUp]public void Preflight()
        {
            Assert.That(Path.GetFullPath(Application.dataPath),Is.EqualTo(Path.GetFullPath(@"D:\UnityProject\NBUnityProject\.utmp\NBFXMeshValidation-20261002\Assets")).IgnoreCase);
            for(int i=0;i<SceneManager.sceneCount;++i){var scene=SceneManager.GetSceneAt(i);Assert.That((scene.name+"/"+scene.path).IndexOf("TAI",StringComparison.OrdinalIgnoreCase),Is.LessThan(0));}
        }
        Material Material(bool native=false)
        {
            var shader=AssetDatabase.LoadAssetAtPath<Shader>(native?"Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/NBShader.shader":G4SpecDebugFixture.GraphPath);Assert.That(shader&&shader.isSupported,Is.True);var material=new Material(shader){hideFlags=HideFlags.HideAndDontSave};owned.Add(material);if(!native){material.SetFloat("_NB_GraphGUIStateVersion",2);material.SetFloat("_NBShaderFeatureTier",3);G4SpecDebugFixture.Validate(material);}return material;
        }
        object Root(params Material[] materials)
        {var helper=new G4GraphPersistentGateTierTests();helpers.Add(helper);return typeof(G4GraphPersistentGateTierTests).GetMethod("Root",All).Invoke(helper,new object[]{materials});}
        static void Preset(object sync,Material material,bool enabled)
        {
            string key=enabled?"ParticleWithoutPlayer":"ParticleBaseDefault";var config=Call(sync,"GetStencilValuesConfig");Assert.That(config,Is.Not.Null);var values=Call(config,"GetStencilValues",key);Assert.That(values,Is.Not.Null);
            string[] fields={"Ref","Comp","Pass","Fail","ZFail","ReadMask","WriteMask"};for(int i=0;i<fields.Length;++i)Assert.That(material.GetFloat(Changed[i]),Is.EqualTo(Convert.ToSingle(Field(values,fields[i]))),Changed[i]);Assert.That(material.GetFloat("_StencilKeyIndex"),Is.EqualTo(Convert.ToSingle(Call(config,"GetKeyIndex",key))));Assert.That(material.GetFloat("_CustomStencilTest"),Is.EqualTo(enabled?1:0));Assert.That(material.GetFloat("_StencilWithoutPlayerToggle"),Is.EqualTo(enabled?1:0));
        }
        [TearDown]public void Cleanup()
        {foreach(var host in owned.OfType<NBFXMainTexGUIEventHost>()){host.Draw=null;host.Setup=null;host.Close();}foreach(var helper in helpers)helper.Cleanup();helpers.Clear();foreach(var obj in owned.AsEnumerable().Reverse())if(obj)Object.DestroyImmediate(obj);owned.Clear();}
        [Test]public void G4StencilPlayer_CPU_ActualFloatSchemaFactoryAndGlobalGuard()
        {
            var graph=Material();var root=Root(graph);var before=Snapshot.Read(graph);int index=graph.shader.FindPropertyIndex("_StencilWithoutPlayerToggle");Assert.That(index,Is.GreaterThanOrEqualTo(0));Assert.That(graph.shader.GetPropertyType(index),Is.EqualTo(ShaderPropertyType.Float));Assert.That(graph.shader.GetPropertyDefaultFloatValue(index),Is.Zero);Assert.That(Call(root,"InitializeGraphStencilWithoutPlayerInputs"),Is.True);before.Same(graph,"Original factory/readiness cannot replace old manual stencil");
            Assert.That(Find("NBShaderEditor.NBShaderKeywordToggleItem").IsInstanceOfType(Field(root,"_graphStencilWithoutPlayerItem")),Is.True);Call(Sync(root),"ApplyStencilPreset","ParticleWithoutPlayer");before.Same(graph,"General ApplyStencilPreset Graph guard remains");
            var native=Material(true);var nativeRoot=Root(native);var nativeBefore=Snapshot.Read(native);var factory=Find("NBShaderEditor.BaseOptionBigBlockItem").GetMethod("CreateStencilWithoutPlayerItem",All);Assert.That(factory,Is.Not.Null);var item=factory.Invoke(null,new object[]{nativeRoot,null,null,null,false});Assert.That(item.GetType(),Is.EqualTo(Find("NBShaderEditor.NBShaderKeywordToggleItem")));nativeBefore.Same(native,"Default false retains exact original Native control type and read-only construction");
        }
        [Test]public void G4StencilPlayer_CPU_ExplicitPresetMixedQueuePreserveUndoRedo()
        {
            var a=Material();var b=Material();a.renderQueue=3277;b.renderQueue=3299;foreach(var material in new[]{a,b}){material.SetFloat("_QueueControl",1);material.SetFloat("_QueueOffset",.25f);material.SetFloat("_ColorMask",3.25f);material.SetFloat("_CustomStencilTestFoldOut",1);material.SetFloat("_StencilComp",8.25f);material.SetFloat("_StencilOp",2);material.SetFloat("_StencilFail",2);material.SetFloat("_StencilZFail",2);}a.SetFloat("_Stencil",7);b.SetFloat("_Stencil",9);var root=Root(a,b);Assert.That(Call(root,"InitializeGraphStencilWithoutPlayerInputs"),Is.True);var sa=Snapshot.Read(a);var sb=Snapshot.Read(b);Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            try
            {
                foreach(bool enabled in new[]{true,false})
                {Assert.That(Call(Sync(root),"TryApplyGraphStencilWithoutPlayer",enabled),Is.True);Preset(Sync(root),a,enabled);Preset(Sync(root),b,enabled);sa.Same(a,"First explicit preset only owns original stencil fields/two toggles",Changed);sb.Same(b,"Second explicit preset preserves Queue/ColorMask/all raw/pass/fold",Changed);}
                Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(group);var aa=Snapshot.Read(a);var ab=Snapshot.Read(b);Undo.PerformUndo();sa.Same(a,"Complete mixed preset Undo first");sb.Same(b,"Complete mixed preset Undo second");Undo.PerformRedo();aa.Same(a,"Complete mixed preset Redo first");ab.Same(b,"Complete mixed preset Redo second");
            }
            finally{Undo.RevertAllDownToGroup(group);}
        }
        [Test]public void G4StencilPlayer_CPU_FutureSecondSchemaAtomicReject()
        {
            var a=Material();var b=Material();var root=Root(a,b);Assert.That(Call(root,"InitializeGraphStencilWithoutPlayerInputs"),Is.True);b.SetFloat("_NB_GraphGUIStateVersion",3);var sa=Snapshot.Read(a);var sb=Snapshot.Read(b);Assert.That(Call(Sync(root),"TryApplyGraphStencilWithoutPlayer",true),Is.False);sa.Same(a,"Future second marker rejects before first preset/toggle write");sb.Same(b,"Future schema/raw state preserved");
        }
        NBFXMainTexGUIEventHost Host(object root)
        {
            var host=ScriptableObject.CreateInstance<NBFXMainTexGUIEventHost>();owned.Add(host);host.hideFlags=HideFlags.HideAndDontSave;host.position=new Rect(20,20,640,480);host.SetupCommand="NBFX_StencilPlayer_"+Guid.NewGuid().ToString("N");host.Setup=()=>Assert.That(Call(root,"InitializeGraphStencilWithoutPlayerInputs"),Is.True);host.ShowUtility();host.SendEvent(new Event{type=EventType.ExecuteCommand,commandName=host.SetupCommand});Check(host);Assert.That(host.Initialized,Is.True);host.Draw=()=>Call(root,"DrawGraphStencilWithoutPlayerInputs");return host;
        }
        static void Check(NBFXMainTexGUIEventHost host){if(host.Failure!=null)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(host.Failure).Throw();}
        static void Send(NBFXMainTexGUIEventHost host,Event evt)
        {var type=evt.rawType;host.Counts.TryGetValue(type,out int before);host.SendEvent(evt);Check(host);Assert.That(evt.rawType,Is.EqualTo(type));Assert.That(host.Counts.TryGetValue(type,out int after)&&after>before,Is.True);}
        [Test]public void G4StencilPlayer_GUI_MixedManualPresetPassiveReadOnly()
        {
            var a=Material();var b=Material();a.SetFloat("_StencilWithoutPlayerToggle",3.25f);b.SetFloat("_StencilWithoutPlayerToggle",0);a.SetFloat("_Stencil",7);b.SetFloat("_Stencil",9);a.renderQueue=3277;b.renderQueue=3299;var root=Root(a,b);var beforeA=Snapshot.Read(a);var beforeB=Snapshot.Read(b);var host=Host(root);Send(host,new Event{type=EventType.Layout});Send(host,new Event{type=EventType.Repaint});beforeA.Same(a,"Actual mixed Toggle paint cannot canonicalize saved UI or replace manual stencil");beforeB.Same(b,"Second mixed material remains exact");
        }
        [Test]public void G4StencilPlayer_GUI_ActualOriginalToggleAndResetCompleteUndoRedo()
        {
            var material=Material();material.SetFloat("_Stencil",7);material.SetFloat("_StencilComp",3);material.SetFloat("_CustomStencilTest",0);material.renderQueue=3277;var root=Root(material);var host=Host(root);Send(host,new Event{type=EventType.Layout});Send(host,new Event{type=EventType.Repaint});var item=Field(root,"_graphStencilWithoutPlayerItem");var control=(Rect)Field(item,"ControlRect");Assert.That(control.width>0&&control.height>0,Is.True);var before=Snapshot.Read(material);Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            try
            {
                var point=new Vector2(control.x+6,control.center.y);Send(host,new Event{type=EventType.MouseDown,button=0,mousePosition=point});Send(host,new Event{type=EventType.MouseUp,button=0,mousePosition=point});Preset(Sync(root),material,true);before.Same(material,"Real original Toggle only explicitly owns stencil preset/two toggles",Changed);
                Send(host,new Event{type=EventType.Layout});Send(host,new Event{type=EventType.Repaint});var reset=(Rect)Field(item,"ResetRect");Assert.That(reset.width>0&&reset.height>0,Is.True);Send(host,new Event{type=EventType.MouseDown,button=0,mousePosition=reset.center});Send(host,new Event{type=EventType.MouseUp,button=0,mousePosition=reset.center});Preset(Sync(root),material,false);before.Same(material,"Actual original Reset keeps queue/color/raw/pass/fold",Changed);
                Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(group);var after=Snapshot.Read(material);host.Draw=null;Undo.PerformUndo();before.Same(material,"Actual Root pre-write Undo restores original manual stencil");Undo.PerformRedo();after.Same(material,"Real Toggle/Reset complete Redo");
            }
            finally{host.Draw=null;Undo.RevertAllDownToGroup(group);}
        }
        [Serializable]sealed class Metrics
        {public string scope,api,unity;public bool finite,stencilAttachment;public float passBC,rejectBC,defaultBC,repeat,restore,response;public int passVisible,rejectVisible,excludedPixels;}
        [Test]public void G4StencilPlayer_GPU_ActualPresetGreaterAndDefaultRestore_ortho()
        {
            var original=typeof(G4GraphRenderStateTests);var make=original.GetMethod("MakeAuxiliaryShader",All);var format=(GraphicsFormat)original.GetMethod("SupportedStencilFormat",All).Invoke(null,null);Assert.That(format,Is.Not.EqualTo(GraphicsFormat.None));
            Shader Make(string name,string stencil,string mask,string output){var shader=(Shader)make.Invoke(null,new object[]{name,stencil,mask,output});owned.Add(shader);Assert.That(shader&&shader.isSupported,Is.True);return shader;}
            var backdrop=Make("PlayerPresetBackdrop","","RGBA","half4(0.125, 0.25, 0.5, 0.25)");var writer=Make("PlayerPresetWriter","Stencil { Ref [_WriterRef] Comp Always Pass Replace ReadMask 255 WriteMask [_WriterMask] }","0","half4(0, 0, 0, 0)");var probe=Make("PlayerPresetUnusedProbe","","RGBA","half4(0, 0, 0, 0)");
            var fixtureType=original.GetNestedType("Fixture",All);Assert.That(fixtureType,Is.Not.Null);var fixture=Activator.CreateInstance(fixtureType,All,null,new object[]{backdrop,writer,probe,true,true},null);var graph=(Material)Field(fixture,"Graph");var native=(Material)Field(fixture,"Legacy");var target=(RenderTexture)Field(fixture,"Target");object graphRoot=null,nativeRoot=null;
            string folder=Path.Combine(Environment.GetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR")??Path.Combine(Path.GetDirectoryName(Application.dataPath),"Temp/NBFXStencilPlayer"),"preset-ortho");Directory.CreateDirectory(folder);
            Color[] Capture(Material material,string label)=>(Color[])Call(fixture,"Capture",material,Path.Combine(folder,label));float Delta(Color[] a,Color[] b)=>a.Zip(b,(x,y)=>Enumerable.Range(0,4).Max(i=>Mathf.Abs(x[i]-y[i]))).Max();int Pixels(Color[] a,Color[] b)=>a.Zip(b,(x,y)=>Enumerable.Range(0,4).Any(i=>x[i]!=y[i])).Count(v=>v);bool Finite(Color[] pixels)=>pixels.All(p=>Enumerable.Range(0,4).All(i=>!float.IsNaN(p[i])&&!float.IsInfinity(p[i])));
            void Apply(bool enabled)
            {
                Assert.That(Call(Sync(graphRoot),"TryApplyGraphStencilWithoutPlayer",enabled),Is.True);Preset(Sync(graphRoot),graph,enabled);Call(Sync(nativeRoot),"ApplyStencilPreset",enabled?"ParticleWithoutPlayer":"ParticleBaseDefault");native.SetFloat("_CustomStencilTest",enabled?1:0);native.SetFloat("_StencilWithoutPlayerToggle",enabled?1:0);Preset(Sync(nativeRoot),native,enabled);
            }
            try
            {
                graph.SetFloat("_NB_GraphGUIStateVersion",2);graphRoot=Root(graph);nativeRoot=Root(native);Assert.That(Call(graphRoot,"InitializeGraphStencilWithoutPlayerInputs"),Is.True);
                fixtureType.GetProperty("WriterEnabled",All).SetValue(fixture,true);fixtureType.GetProperty("WriterMask",All).SetValue(fixture,255);fixtureType.GetProperty("ProbeEnabled",All).SetValue(fixture,false);fixtureType.GetProperty("WriterRef",All).SetValue(fixture,2);Apply(true);var empty=Capture(null,"empty");var passB=Capture(native,"B-preset-ref2");var passC=Capture(graph,"C-preset-ref2");
                fixtureType.GetProperty("WriterRef",All).SetValue(fixture,8);var rejectB=Capture(native,"B-preset-ref8");var rejectC=Capture(graph,"C-preset-ref8");var repeatB=Capture(native,"B-repeat");var repeatC=Capture(graph,"C-repeat");var on=Snapshot.Read(graph);Apply(false);var defaultB=Capture(native,"B-default");var defaultC=Capture(graph,"C-default");Apply(true);var restored=Capture(graph,"C-preset-restored");on.Same(graph,"Actual preset service complete restoration, without direct render-state overrides");
                var data=new Metrics{scope="One orthographic camera; existing real QCM writer/fixture. Native original preset helper and actual Graph scoped preset toggle transaction. Ref2 passes and Ref8 excludes only writer region; default off restores that region. No keyword/Tier/Player/Gate claim.",api=SystemInfo.graphicsDeviceType.ToString(),unity=Application.unityVersion,finite=new[]{empty,passB,passC,rejectB,rejectC,repeatB,repeatC,defaultB,defaultC,restored}.All(Finite),stencilAttachment=target.descriptor.depthStencilFormat==format,passBC=Delta(passB,passC),rejectBC=Delta(rejectB,rejectC),defaultBC=Delta(defaultB,defaultC),repeat=Mathf.Max(Delta(rejectB,repeatB),Delta(rejectC,repeatC)),restore=Delta(rejectC,restored),response=Delta(rejectC,defaultC),passVisible=Pixels(passC,empty),rejectVisible=Pixels(rejectC,empty),excludedPixels=Pixels(rejectC,defaultC)};File.WriteAllText(Path.Combine(folder,"metrics.json"),JsonUtility.ToJson(data,true));Assert.That(data.finite&&data.stencilAttachment,Is.True);Assert.That(data.passBC+data.rejectBC+data.defaultBC+data.repeat+data.restore,Is.Zero);Assert.That(data.passVisible,Is.GreaterThan(128));Assert.That(data.rejectVisible,Is.GreaterThan(128));Assert.That(data.excludedPixels,Is.GreaterThan(128));Assert.That(data.response,Is.GreaterThan(.04f));Assert.That(Delta(passC,defaultC),Is.Zero,"Original Greater5 accepts buffer2 across the front while default accepts all");
            }
            finally{((IDisposable)fixture).Dispose();}
        }
    }
}
