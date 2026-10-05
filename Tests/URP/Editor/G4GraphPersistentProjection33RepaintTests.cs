using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace NBFX.Baseline.Tests
{
    // Explicit current31Bool+2typed passive counterpart of old Tier9 Repaint.
    // No generic extra-property waiver, no new projection or native Popup.
    public sealed class G4GraphPersistentProjection33RepaintTests
    {
        const BindingFlags All=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
        static readonly string[] Gates={"_NB_TierAllowMask","_NB_TierAllowMask2","_NB_TierAllowMask3","_NB_TierAllowNoise","_NB_TierAllowNoiseMask","_NB_TierAllowProgramNoise","_NB_TierAllowProgramSimple","_NB_TierAllowProgramVoronoi","_NB_TierAllowFresnel","_NB_TierAllowEmission","_NB_TierAllowColorBlend","_NB_TierAllowDissolve","_NB_TierAllowDissolveMask","_NB_TierAllowDissolveRamp","_NB_TierAllowDissolveRampMap","_NB_TierAllowParallax","_NB_TierAllowNormalMap","_NB_TierAllowColorRamp","_NB_TierAllowColorRampMap","_NB_TierAllowMatCap","_NB_TierAllowDistanceFade","_NB_TierAllowSoftParticles","_NB_TierAllowDepthOutline","_NB_TierAllowRefraction","_NB_TierAllowVertexOffset","_NB_TierAllowVertexOffsetMask","_NB_TierAllowDepthDecal","_NB_TierAllowLighting","_NB_TierAllowVAT","_NB_TierAllowFlipbook","_NB_TierAllowChromaticAberration"};
        static readonly string[] Typed={"_NB_TierVATFamily","_NB_TierVATSubMode"};
        static readonly string[] OriginalToggles={"_Mask_Toggle","_Mask2_Toggle","_Mask3_Toggle","_noisemapEnabled","_noiseMaskMap_Toggle","_ProgramNoise_Toggle","_ProgramNoise_Simple_Toggle","_ProgramNoise_Voronoi_Toggle","_fresnelEnabled"};
        readonly List<Object> owned=new List<Object>();readonly G4GraphPersistentGateTierTests helper=new G4GraphPersistentGateTierTests();
        static Type Find(string n)=>G4SpecDebugFixture.FindType(n);
        static MethodInfo Unique(Type t,string n,params Type[] signature){var matches=t.GetMethods(All).Where(m=>m.Name==n&&m.GetParameters().Select(p=>p.ParameterType).SequenceEqual(signature)).ToArray();Assert.That(matches.Length,Is.EqualTo(1),t.FullName+"."+n);return matches[0];}
        static object Invoke(MethodInfo m,object target,params object[] args){try{return m.Invoke(target,args);}catch(TargetInvocationException e){ExceptionDispatchInfo.Capture(e.InnerException??e).Throw();throw;}}
        [OneTimeSetUp]public void Preflight()
        {
            Assert.That(Path.GetFullPath(Application.dataPath),Is.EqualTo(Path.GetFullPath(@"D:\UnityProject\NBUnityProject\.utmp\NBFXMeshValidation-20261002\Assets")).IgnoreCase);
            for(int i=0;i<SceneManager.sceneCount;++i){var scene=SceneManager.GetSceneAt(i);Assert.That((scene.name+"/"+scene.path).IndexOf("TAI",StringComparison.OrdinalIgnoreCase),Is.LessThan(0));}
            G4SpecDebugFixture.PreflightImport();
            Type runtime=Find("NBShader.NBShaderFeatureRuntime"),editor=Find("NBShaders2.Editor.FeatureLevel.NBShaderFeatureLevelMaterialApplier");
            Assert.That((string[])runtime.GetField("GraphSupportedGateProperties",All).GetValue(null),Is.EqualTo(Gates),"Exact current31 Boolean ownership; no extra waiver");
            Assert.That((string[])runtime.GetField("GraphSupportedTypedProjectionProperties",All).GetValue(null),Is.EqualTo(Typed),"Exact typed pair ownership");
            Assert.That((string[])runtime.GetField("GraphSupportedProjectionProperties",All).GetValue(null),Is.EqualTo(Gates.Concat(Typed).ToArray()),"Exact33 union order");
            foreach(string field in new[]{"GraphSupportedGateProperties","GraphSupportedTypedProjectionProperties","GraphSupportedProjectionProperties"})Assert.That(ReferenceEquals(runtime.GetField(field,All).GetValue(null),editor.GetField(field,All).GetValue(null)),Is.True,field+" one owner table");
        }
        sealed class Snapshot
        {
            object data;Dictionary<string,bool> tags;static Type T=>typeof(G4GraphGuiFeatureIntentTests).GetNestedType("Snapshot",All);
            public static Snapshot Read(Material m)=>new Snapshot{data=Invoke(Unique(T,"Read",typeof(Material)),null,m),tags=new[]{"SRPDefaultUnlit","UniversalForward"}.ToDictionary(x=>x,x=>m.GetShaderPassEnabled(x))};
            public void Same(Material m,string label){Invoke(Unique(T,"AssertSame",typeof(Material),typeof(string),typeof(string[])),data,m,label,Array.Empty<string>());foreach(var tag in tags)Assert.That(m.GetShaderPassEnabled(tag.Key),Is.EqualTo(tag.Value),label+" LightMode "+tag.Key);}
        }
        Material NewMaterial()
        {
            var m=G4SpecDebugFixture.NewGraph();owned.Add(m);Assert.That(m.HasProperty("_NBShaderFeatureTier"),Is.True);
            foreach(string toggle in OriginalToggles)m.SetFloat(toggle,1);foreach(string gate in Gates)m.SetFloat(gate,.25f);
            m.SetFloat("_NB_TierVATFamily",-2);m.SetFloat("_NB_TierVATSubMode",0);
            m.SetFloat("_NB_Flags0Lo16",-3.75f);m.SetFloat("_NB_Flags1Hi16",65536.25f);m.SetFloat("_NB_CustomDataFlag3Hi16",53214.125f);m.SetFloat("_NB_PNoiseBlendHi16",12345.125f);
            m.SetFloat("_NB_GraphScreenPassMigrationComplete",0);m.SetFloat("_NB_GraphPassMigrationComplete",0);m.SetFloat("_AffectsShadows",1);m.SetFloat("_CastShadows",0);m.SetShaderPassEnabled("ShadowCaster",false);
            m.SetShaderPassEnabled("SRPDefaultUnlit",false);m.SetShaderPassEnabled("UniversalForward",false);return m;
        }
        readonly Dictionary<string,int?> originalPrefs=new Dictionary<string,int?>();
        NBFXMainTexGUIEventHost FullHost(Material material)
        {
            var editor=(MaterialEditor)Editor.CreateEditor(material,typeof(MaterialEditor));owned.Add(editor);
            var extension=Find("UnityEditor.Rendering.MaterialEditorExtension");string key=(string)Invoke(Unique(extension,"GetEditorPrefsKey",typeof(MaterialEditor)),null,editor);
            originalPrefs.Add(key,EditorPrefs.HasKey(key)?EditorPrefs.GetInt(key):(int?)null);
            foreach(uint bit in new uint[]{1,2,4,8})Invoke(Unique(extension,"SetIsAreaExpanded",typeof(MaterialEditor),typeof(uint),typeof(bool)),null,editor,bit,bit==2);
            var gui=Activator.CreateInstance(Find("NBShaderEditor.NBShaderGraphGUI"));
            var host=ScriptableObject.CreateInstance<NBFXMainTexGUIEventHost>();owned.Add(host);host.hideFlags=HideFlags.HideAndDontSave;host.position=new Rect(20,20,650,700);host.SetupCommand="NBFX_FullProjection33_"+Guid.NewGuid().ToString("N");host.Setup=()=>{};
            host.ShowUtility();host.SendEvent(new Event{type=EventType.ExecuteCommand,commandName=host.SetupCommand});Check(host);Assert.That(host.Initialized,Is.True);
            host.Draw=()=>Invoke(Unique(gui.GetType(),"OnGUI",typeof(MaterialEditor),typeof(MaterialProperty[])),gui,editor,MaterialEditor.GetMaterialProperties(editor.targets));return host;
        }
        static void Send(NBFXMainTexGUIEventHost host,Event evt)
        {
            EventType requested=evt.rawType;host.Counts.TryGetValue(requested,out int before);host.SendEvent(evt);Check(host);Assert.That(evt.rawType,Is.EqualTo(requested));Assert.That(host.Counts.TryGetValue(requested,out int after)&&after>before,Is.True,"Real native OnGUI delivery must reach complete outer GraphGUI/official URP bridge");
        }
        [Test]public void G4PersistentProjection33V2_FullGraphGUI_Repaint_UnprojectedReadOnly()
        {
            var material=NewMaterial();foreach(string toggle in OriginalToggles)material.SetFloat(toggle,0);
            // Same original FullGraphGUI inactive material baseline: official validation first,
            // then exact current unprojected Boolean default1 gates. Ready typed pair is retained.
            G4SpecDebugFixture.Validate(material);foreach(string gate in Gates)material.SetFloat(gate,1);
            material.SetFloat("_NB_TierVATFamily",1);material.SetFloat("_NB_TierVATSubMode",5);
            var host=FullHost(material);foreach(string gate in Gates)material.SetFloat(gate,1);var before=Snapshot.Read(material);
            Send(host,new Event{type=EventType.Layout});Send(host,new Event{type=EventType.Repaint});
            before.Same(material,"Complete outer GraphGUI Repaint cannot normalize default1 inactive31gates/2typed, raw words, declared9/OVZ/Caster or unowned Screen/Back");
            Assert.That(Gates.All(g=>material.GetFloat(g)==1),Is.True);Assert.That(material.GetFloat(Typed[0]),Is.EqualTo(1));Assert.That(material.GetFloat(Typed[1]),Is.EqualTo(5));
        }
        static void Check(NBFXMainTexGUIEventHost host){if(host.Failure!=null)ExceptionDispatchInfo.Capture(host.Failure).Throw();}
        [TearDown]public void Cleanup(){foreach(var host in owned.OfType<NBFXMainTexGUIEventHost>()){host.Draw=null;host.Setup=null;host.Close();}foreach(var row in originalPrefs){if(row.Value.HasValue)EditorPrefs.SetInt(row.Key,row.Value.Value);else EditorPrefs.DeleteKey(row.Key);}originalPrefs.Clear();helper.Cleanup();foreach(var item in owned.AsEnumerable().Reverse())if(item)Object.DestroyImmediate(item);owned.Clear();}
    }
}
