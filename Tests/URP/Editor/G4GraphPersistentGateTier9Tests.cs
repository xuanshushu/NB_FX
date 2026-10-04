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
using Object = UnityEngine.Object;

namespace NBFX.Baseline.Tests
{
    // Existing Tier field/menu callback + same Root/Sync transaction, nine consumers, explicit new source contract.
    // No synthetic renderer and no native Popup automation.
    public sealed class G4GraphPersistentGateTier9Tests
    {
        const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        const string Tier = "_NBShaderFeatureTier";
        static readonly string[] Gates = { "_NB_TierAllowMask", "_NB_TierAllowMask2", "_NB_TierAllowMask3", "_NB_TierAllowNoise", "_NB_TierAllowNoiseMask", "_NB_TierAllowProgramNoise", "_NB_TierAllowProgramSimple", "_NB_TierAllowProgramVoronoi", "_NB_TierAllowFresnel" };
        static readonly string[] Keywords = { "_MASKMAP_ON", "_MASKMAP2_ON", "_MASKMAP3_ON", "_NOISEMAP", "_NOISE_MASKMAP", "_PROGRAM_NOISE", "_PROGRAM_NOISE_SIMPLE", "_PROGRAM_NOISE_VORONOI", "_FRESNEL" };
        static readonly string[] Toggles = { "_Mask_Toggle", "_Mask2_Toggle", "_Mask3_Toggle", "_noisemapEnabled", "_noiseMaskMap_Toggle", "_ProgramNoise_Toggle", "_ProgramNoise_Simple_Toggle", "_ProgramNoise_Voronoi_Toggle", "_fresnelEnabled" };
        readonly List<Object> owned = new List<Object>();
        readonly List<string> assets = new List<string>();
        readonly Dictionary<string, int?> originalPrefs = new Dictionary<string, int?>();
        static Type TypeOf(string name) => G4SpecDebugFixture.FindType(name);
        static object EnumTier(int value) => Enum.ToObject(TypeOf("NBShader.NBShaderFeatureTier"), value);
        static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, All).Invoke(target, args);
        static object Property(object target, string name) => target.GetType().GetProperty(name, All).GetValue(target);
        [OneTimeSetUp] public void Preflight()
        {
            G4SpecDebugFixture.PreflightImport();
            var actual=(string[])TypeOf("NBShaders2.Editor.FeatureLevel.NBShaderFeatureLevelMaterialApplier").GetField("GraphSupportedGateProperties",All).GetValue(null);
            Assert.That(actual,Is.EqualTo(Gates),"Explicit version9 fixture must match all registered gates; no generic extra-property waiver.");
        }
        sealed class Snapshot
        {
            readonly object value;
            static Type Shared => typeof(G4GraphGuiFeatureIntentTests).GetNestedType("Snapshot", BindingFlags.NonPublic);
            Snapshot(object value) { this.value = value; }
            public static Snapshot Read(Material material) => new Snapshot(Shared.GetMethod("Read", All).Invoke(null, new object[] { material }));
            public void AssertSame(Material material, string label, params string[] allowed) => Shared.GetMethod("AssertSame", All).Invoke(value, new object[] { material, label, allowed });
        }
        Material NewMaterial()
        {
            var material = G4SpecDebugFixture.NewGraph(); owned.Add(material);
            Assert.That(material.HasProperty(Tier), Is.True, "The existing persistent Tier property must actually compile into the Graph shader.");
            foreach (string toggle in Toggles) material.SetFloat(toggle, 1);
            foreach (string gate in Gates) material.SetFloat(gate, .25f);
            material.SetFloat("_NB_Flags0Lo16", -3.75f); material.SetFloat("_NB_Flags1Hi16", 65536.25f);
            material.SetFloat("_NB_CustomDataFlag3Hi16", 53214.125f); material.SetFloat("_NB_PNoiseBlendHi16", 12345.125f);
            return material;
        }
        object Root(params Material[] materials)
        {
            var root = Activator.CreateInstance(TypeOf("NBShaderEditor.NBShaderRootItem")); var type = root.GetType();
            var editor = (MaterialEditor)Editor.CreateEditor(materials.Cast<Object>().ToArray(), typeof(MaterialEditor)); owned.Add(editor);
            type.GetField("MatEditor", All).SetValue(root, editor); type.GetField("Mats", All).SetValue(root, materials.ToList());
            type.GetField("Shader", All).SetValue(root, materials[0].shader); Call(root, "InitFlags", materials.ToList());
            var dictionary = (IDictionary)type.GetField("PropertyInfoDic", All).GetValue(root);
            foreach (MaterialProperty property in materials.All(m => m.shader == materials[0].shader)
                ? MaterialEditor.GetMaterialProperties(materials.Cast<Object>().ToArray()) : Array.Empty<MaterialProperty>())
            {
                var info = Activator.CreateInstance(TypeOf("NBShaderEditor.ShaderPropertyInfo"));
                info.GetType().GetField("Property").SetValue(info, property); info.GetType().GetField("Name").SetValue(info, property.name);
                info.GetType().GetField("Index").SetValue(info, materials[0].shader.FindPropertyIndex(property.name)); dictionary.Add(property.name, info);
            }
            type.GetProperty("Context", All).SetValue(root, Activator.CreateInstance(TypeOf("NBShaderEditor.NBShaderGUIContext"), root));
            type.GetProperty("SyncService", All).SetValue(root, Activator.CreateInstance(TypeOf("NBShaderEditor.NBShaderSyncService"), root));
            Call(Property(root, "Context"), "Refresh"); return root;
        }
        static string[] Raw() => (string[])TypeOf("NBShader.NBShaderFeatureCatalog").GetField("RawKeywords", All).GetValue(null);
        static string[] ProjectPolicy(int tier)
        {
            var settingsType = TypeOf("NBShaders2.Editor.FeatureLevel.NBShaderFeatureLevelProjectSettings");
            var instance = settingsType.GetProperty("instance", All | BindingFlags.FlattenHierarchy).GetValue(null);
            return ((IEnumerable<string>)settingsType.GetMethod("GetAllowedKeywordSetForBuildInfoNoSave", All).Invoke(instance, new[] { EnumTier(tier) })).ToArray();
        }
        static string[] Effective(Material material, int tier, string[] policy)
        {
            object[] args = { material, EnumTier(tier), policy, null, null };
            Assert.That(TypeOf("NBShader.NBShaderMaterialIntentResolver").GetMethod("TryResolveGraphSupportedKeywordIntent", All).Invoke(null, args), Is.True);
            return (string[])args[3].GetType().GetField("effectiveKeywords").GetValue(args[3]);
        }
        static void AssertGates(Material material, string[] effective)
        { for (int i = 0; i < Gates.Length; ++i) Assert.That(material.GetFloat(Gates[i]), Is.EqualTo(effective.Contains(Keywords[i]) ? 1f : 0f), Gates[i]); }
        static bool Apply(object root, int tier, string[] policy) => (bool)Call(Property(root, "SyncService"), "TryApplyGraphSupportedGateTier", EnumTier(tier), policy);
        static object Toolbar(object root) => Activator.CreateInstance(TypeOf("NBShaderEditor.NBShaderGUIToolBar"), root);
        [TearDown] public void Cleanup()
        {
            foreach (var host in owned.OfType<NBFXMainTexGUIEventHost>()) { host.Draw = null; host.Setup = null; host.Close(); }
            foreach (var row in originalPrefs) { if (row.Value.HasValue) EditorPrefs.SetInt(row.Key,row.Value.Value); else EditorPrefs.DeleteKey(row.Key); }
            originalPrefs.Clear();
            foreach (string asset in assets) { Assert.That(asset.StartsWith("Assets/ResTemp/EditorTemp/NBFX_Tier_", StringComparison.Ordinal), Is.True); AssetDatabase.DeleteAsset(asset); }
            assets.Clear(); foreach (var item in owned.AsEnumerable().Reverse()) if (item) Object.DestroyImmediate(item); owned.Clear();
        }

        [Test] public void G4PersistentTier9_ActualFloatDefault3_NativeGuardsRemain()
        {
            var material = NewMaterial(); int index = material.shader.FindPropertyIndex(Tier); Assert.That(index, Is.GreaterThanOrEqualTo(0));
            Assert.That(material.shader.GetPropertyType(index), Is.EqualTo(UnityEngine.Rendering.ShaderPropertyType.Float));
            Assert.That(material.shader.GetPropertyDefaultFloatValue(index), Is.EqualTo(3f));
            Assert.That((material.shader.GetPropertyFlags(index) & UnityEngine.Rendering.ShaderPropertyFlags.HideInInspector) != 0, Is.True);
            var before = Snapshot.Read(material);
            object[] args = { material, EnumTier(0), true, true, false };
            var generic = TypeOf("NBShaders2.Editor.FeatureLevel.NBShaderFeatureLevelMaterialApplier").GetMethods(All).Single(m => m.Name == "Apply" && m.GetParameters().Length == 5);
            Assert.That(generic.Invoke(null, args), Is.False); Assert.That(args[4], Is.False);
            Assert.That(TypeOf("NBShaders2.Editor.FeatureLevel.NBShaderEditorQualityTierWatcher").GetMethod("CanMutateMaterial", All).Invoke(null, new object[] { material }), Is.False);
            var runtime = TypeOf("NBShader.NBShaderFeatureRuntime").GetMethods(All).Single(m => m.Name == "ApplyTier" && m.GetParameters().Length == 2 && m.GetParameters()[0].ParameterType == typeof(Material));
            runtime.Invoke(null, new[] { (object)material, EnumTier(0) }); before.AssertSame(material, "General Native Apply/quality/runtime still reject Graph");
        }
        [TestCase(0, TestName="G4PersistentTier9_OriginalMenuCallback_Low")]
        [TestCase(1, TestName="G4PersistentTier9_OriginalMenuCallback_Medium")]
        [TestCase(2, TestName="G4PersistentTier9_OriginalMenuCallback_High")]
        [TestCase(3, TestName="G4PersistentTier9_OriginalMenuCallback_Ultra")]
        public void OriginalMenuCallbackSavesOnlyTierAndEightGates(int tier)
        {
            var material = NewMaterial(); var before = Snapshot.Read(material); string[] expected = Effective(material, tier, ProjectPolicy(tier));
            var root = Root(material); Assert.That(Call(Property(root,"SyncService"),"HasGraphSupportedGateTierEditSchema"), Is.True);
            Call(Toolbar(root), "SetFeatureTier", EnumTier(tier)); Assert.That(material.GetFloat(Tier), Is.EqualTo(tier)); AssertGates(material, expected);
            Assert.That(Convert.ToInt32(Property(Property(root,"Context"),"CurrentTier")), Is.EqualTo(tier));
            before.AssertSame(material, "Original dropdown callback changes only persisted Tier and implemented gates", Gates.Concat(new[] { Tier }).ToArray());
        }
        [Test] public void G4PersistentTier9_MultiselectLowHigh_CompleteUndoRedo()
        {
            var a=NewMaterial(); var b=NewMaterial(); b.SetFloat(Tier,2); b.SetFloat("_Mask_Toggle",0); b.SetFloat("_ProgramNoise_Toggle",0);
            var root=Root(a,b); var beforeA=Snapshot.Read(a);var beforeB=Snapshot.Read(b); Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            try
            {
                Assert.That(Apply(root,0,Array.Empty<string>()),Is.True); Assert.That(Gates.All(p=>a.GetFloat(p)==0 && b.GetFloat(p)==0),Is.True);
                Assert.That(a.GetFloat(Tier),Is.EqualTo(0));Assert.That(b.GetFloat(Tier),Is.EqualTo(0));
                Assert.That(Apply(root,3,Raw()),Is.True);AssertGates(a,Effective(a,3,Raw()));AssertGates(b,Effective(b,3,Raw()));
                beforeA.AssertSame(a,"A raw intent preserved",Gates.Concat(new[]{Tier}).ToArray()); beforeB.AssertSame(b,"B raw intent preserved",Gates.Concat(new[]{Tier}).ToArray());
                Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(group);var afterA=Snapshot.Read(a);var afterB=Snapshot.Read(b);
                Undo.PerformUndo();beforeA.AssertSame(a,"Undo all A");beforeB.AssertSame(b,"Undo all B");
                Undo.PerformRedo();afterA.AssertSame(a,"Redo all A");afterB.AssertSame(b,"Redo all B");
            }
            finally { Undo.RevertAllDownToGroup(group); }
        }
        [Test] public void G4PersistentTier9_RepeatedProjection_NoOp()
        {
            var material=NewMaterial();var root=Root(material);Assert.That(Apply(root,3,Raw()),Is.True);var before=Snapshot.Read(material);
            Assert.That(Apply(root,3,Raw()),Is.True);before.AssertSame(material,"No-op includes persisted Tier, every gate, raw flags and original states");
        }
        [Test] public void G4PersistentTier9_BadSecondMarker_NoPartialMultiselect()
        {
            var a=NewMaterial();var b=NewMaterial();b.SetFloat("_NB_GraphGUIStateVersion",3);var root=Root(a,b);var beforeA=Snapshot.Read(a);var beforeB=Snapshot.Read(b);
            Assert.That(Apply(root,0,Array.Empty<string>()),Is.False);Call(Toolbar(root),"SetFeatureTier",EnumTier(0));
            beforeA.AssertSame(a,"Valid first cannot write before invalid second is rejected");beforeB.AssertSame(b,"Future marker retained");
        }
        [Test] public void G4PersistentTier9_MixedNativeGraph_NoMutation()
        {
            var graph=NewMaterial();var nativeShader=AssetDatabase.LoadAssetAtPath<Shader>("Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/NBShader.shader");Assert.That(nativeShader,Is.Not.Null);
            var native=new Material(nativeShader){hideFlags=HideFlags.HideAndDontSave};owned.Add(native);var root=Root(graph,native);var a=Snapshot.Read(graph);var b=Snapshot.Read(native);
            Assert.That(Apply(root,0,Array.Empty<string>()),Is.False);Call(Toolbar(root),"SetFeatureTier",EnumTier(0));a.AssertSame(graph,"Mixed Graph unchanged");b.AssertSame(native,"Mixed Native unchanged");
        }
        [Test] public void G4PersistentTier9_MissingPersistentFloat_NoFabrication()
        {
            string source="Shader \"Hidden/NBFX/TierSchemaMissing\" { Properties { _NB_DistortionMode(\"D\",Float)=0 _NB_Flags0Lo16(\"0L\",Float)=0 _NB_Flags0Hi16(\"0H\",Float)=0 _NB_Flags1Lo16(\"1L\",Float)=0 _NB_Flags1Hi16(\"1H\",Float)=0 } SubShader { Pass { } } }";
            var shader=ShaderUtil.CreateShaderAsset(source,false);Assert.That(shader,Is.Not.Null);owned.Add(shader);var material=new Material(shader){hideFlags=HideFlags.HideAndDontSave};owned.Add(material);
            var root=Root(material);var before=Snapshot.Read(material);Assert.That(material.HasProperty(Tier),Is.False);Assert.That(Apply(root,0,Array.Empty<string>()),Is.False);before.AssertSame(material,"No property fabrication or partial gates");
        }
        NBFXMainTexGUIEventHost Host(object root)
        {
            var host=ScriptableObject.CreateInstance<NBFXMainTexGUIEventHost>();owned.Add(host);host.hideFlags=HideFlags.HideAndDontSave;host.position=new Rect(20,20,600,240);
            host.titleContent=new GUIContent("NBFX existing Tier passive test");host.SetupCommand="NBFX_Tier_Setup_"+Guid.NewGuid().ToString("N");object toolbar=null;
            host.Setup=()=>toolbar=Toolbar(root);host.ShowUtility();host.SendEvent(new Event{type=EventType.ExecuteCommand,commandName=host.SetupCommand});Check(host);Assert.That(host.Initialized,Is.True);
            host.Draw=()=>Call(toolbar,"DrawGraphTierSelector");return host;
        }
        static void Check(NBFXMainTexGUIEventHost host){if(host.Failure!=null)ExceptionDispatchInfo.Capture(host.Failure).Throw();}
        static object Field(object target,string name)
        {
            for(Type type=target.GetType();type!=null;type=type.BaseType)
            {var field=type.GetField(name,All|BindingFlags.DeclaredOnly);if(field!=null)return field.GetValue(target);}
            Assert.Fail("Missing original field "+name);return null;
        }
        void Send(NBFXMainTexGUIEventHost host,Event evt)
        {
            EventType requested=evt.rawType;Assert.That(requested,Is.Not.EqualTo(EventType.Ignore).And.Not.EqualTo(EventType.Used));host.Counts.TryGetValue(requested,out int before);
            host.SendEvent(evt);Check(host);Assert.That(evt.rawType,Is.EqualTo(requested));Assert.That(host.Counts.TryGetValue(requested,out int after)&&after>before,Is.True,"Actual native event must reach the complete GraphGUI.");
        }
        NBFXMainTexGUIEventHost FullHost(Material material,bool surfaceInputs,out object gui,out MaterialEditor editor)
        {
            editor=(MaterialEditor)Editor.CreateEditor(material,typeof(MaterialEditor));owned.Add(editor);var actualEditor=editor;
            var extension=TypeOf("UnityEditor.Rendering.MaterialEditorExtension");string key=(string)extension.GetMethod("GetEditorPrefsKey",All).Invoke(null,new object[]{editor});
            if(!originalPrefs.ContainsKey(key))originalPrefs.Add(key,EditorPrefs.HasKey(key)?EditorPrefs.GetInt(key):(int?)null);
            // Exact URP17.3 Expandable masks from the current source, setup only.
            foreach(uint bit in new uint[]{1,2,4,8})extension.GetMethod("SetIsAreaExpanded",All).Invoke(null,new object[]{editor,bit,bit==2&&surfaceInputs});
            gui=Activator.CreateInstance(TypeOf("NBShaderEditor.NBShaderGraphGUI"));var actualGUI=gui;
            var host=ScriptableObject.CreateInstance<NBFXMainTexGUIEventHost>();owned.Add(host);host.hideFlags=HideFlags.HideAndDontSave;host.position=new Rect(20,20,650,700);
            host.titleContent=new GUIContent("NBFX complete Graph GUI event test");host.SetupCommand="NBFX_FullTier_Setup_"+Guid.NewGuid().ToString("N");host.Setup=()=>{};
            host.ShowUtility();host.SendEvent(new Event{type=EventType.ExecuteCommand,commandName=host.SetupCommand});Check(host);Assert.That(host.Initialized,Is.True);
            host.Draw=()=>Call(actualGUI,"OnGUI",actualEditor,MaterialEditor.GetMaterialProperties(actualEditor.targets));return host;
        }
        Material UnprojectedInactiveMaterial()
        {
            var material=NewMaterial();foreach(string toggle in Toggles)material.SetFloat(toggle,0);
            // Official surface/keywords have a valid baseline, then retain the
            // actual unprojected default1 gates to detect a paint/foldout write.
            G4SpecDebugFixture.Validate(material);foreach(string gate in Gates)material.SetFloat(gate,1);return material;
        }
        [TestCase(EventType.Layout,TestName="G4PersistentTier9_FullGraphGUI_Layout_UnprojectedReadOnly")]
        [TestCase(EventType.Repaint,TestName="G4PersistentTier9_FullGraphGUI_Repaint_UnprojectedReadOnly")]
        public void FullGraphGUIReadOnlyOnActualPassiveEvent(EventType type)
        {
            var material=UnprojectedInactiveMaterial();var host=FullHost(material,true,out var gui,out var editor);foreach(string gate in Gates)material.SetFloat(gate,1);var before=Snapshot.Read(material);
            if(type==EventType.Repaint)Send(host,new Event{type=EventType.Layout});Send(host,new Event{type=type});
            before.AssertSame(material,"Complete outer GraphGUI paint cannot normalize default1 inactive gates");Assert.That(Gates.All(g=>material.GetFloat(g)==1),Is.True);
        }
        [Test] public void G4PersistentTier9_FullGraphGUI_EditorPrefsFoldout_NoMaterialProjection()
        {
            var material=UnprojectedInactiveMaterial();var host=FullHost(material,false,out var gui,out var editor);foreach(string gate in Gates)material.SetFloat(gate,1);var extension=TypeOf("UnityEditor.Rendering.MaterialEditorExtension");
            Assert.That(extension.GetMethod("IsAreaExpanded",All).Invoke(null,new object[]{editor,(uint)1,uint.MaxValue}),Is.False);
            Send(host,new Event{type=EventType.Layout});Send(host,new Event{type=EventType.Repaint});var before=Snapshot.Read(material);
            // Core17.3 first header is a1px splitter +17px actual header;
            // x40,y9 is inside the title, away from help/context controls.
            Send(host,new Event{type=EventType.MouseDown,button=0,mousePosition=new Vector2(40,9)});
            Send(host,new Event{type=EventType.MouseUp,button=0,mousePosition=new Vector2(40,9)});
            Assert.That(extension.GetMethod("IsAreaExpanded",All).Invoke(null,new object[]{editor,(uint)1,uint.MaxValue}),Is.True,"Actual header must change its EditorPrefs bit, not a simulated setter.");
            before.AssertSame(material,"EditorPrefs foldout has no Material undo owner and must not derive gates");
        }
        [Test] public void G4PersistentTier9_FullGraphGUI_ActualFlipbookEdit_DerivedGatesUndoRedo()
        {
            var material=NewMaterial();material.SetFloat("_FlipbookBlending",0);material.SetFloat("_VAT_Toggle",0);G4SpecDebugFixture.Validate(material);
            foreach(string gate in Gates)material.SetFloat(gate,.25f);
            var host=FullHost(material,true,out var gui,out var editor);foreach(string gate in Gates)material.SetFloat(gate,.25f);Send(host,new Event{type=EventType.Layout});Send(host,new Event{type=EventType.Repaint});
            Assert.That(Gates.All(g=>material.GetFloat(g)==.25f),Is.True,"Full passive preparation must preserve the actual pre-edit raw gates.");
            var actualRoot=Field(gui,"_rootItem");Assert.That(actualRoot,Is.Not.Null);var item=Field(actualRoot,"_graphFlipbookItem");Assert.That(item,Is.Not.Null);
            var rect=(Rect)Field(item,"ControlRect");Assert.That(rect.height,Is.GreaterThan(0));Assert.That(rect.center.y,Is.InRange(0f,700f));var click=new Vector2(rect.x+6,rect.center.y);
            var before=Snapshot.Read(material);var expected=Effective(material,3,ProjectPolicy(3));Undo.FlushUndoRecordObjects();Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            try
            {
                Send(host,new Event{type=EventType.MouseDown,button=0,mousePosition=click});Send(host,new Event{type=EventType.MouseUp,button=0,mousePosition=click});
                Assert.That(material.GetFloat("_FlipbookBlending"),Is.EqualTo(1),"Original shared Flipbook controller must consume the actual native click.");AssertGates(material,expected);
                Assert.That(Gates.Any(g=>material.GetFloat(g)!=.25f),Is.True,"Actual GUI intent edit must execute the outer derived projection.");
                before.AssertSame(material,"Only real Flipbook intent and derived gates change",Gates.Concat(new[]{"_FlipbookBlending"}).ToArray());
                Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(group);var after=Snapshot.Read(material);host.Draw=null;
                Undo.PerformUndo();before.AssertSame(material,"One original edit Undo restores all eight raw gate values as well as intent/flags/state");
                Undo.PerformRedo();after.AssertSame(material,"Redo restores exact intent and derived values together");
            }
            finally{host.Draw=null;Undo.RevertAllDownToGroup(group);}
        }
        [TestCase(EventType.Layout,TestName="G4PersistentTier9_ActualLayout_ReadOnly")]
        [TestCase(EventType.Repaint,TestName="G4PersistentTier9_ActualRepaint_ReadOnly")]
        public void OriginalTierContentPassiveReadOnly(EventType type)
        {
            var a=NewMaterial();var b=NewMaterial();b.SetFloat(Tier,1);var root=Root(a,b);var host=Host(root);var beforeA=Snapshot.Read(a);var beforeB=Snapshot.Read(b);
            host.Counts.TryGetValue(type,out int before);var evt=new Event{type=type};Assert.That(evt.rawType,Is.EqualTo(type));host.SendEvent(evt);Check(host);
            Assert.That(host.Counts.TryGetValue(type,out int after)&&after>before,Is.True,"Real native OnGUI delivery is required.");
            beforeA.AssertSame(a,"Layout/Repaint A has no projection");beforeB.AssertSame(b,"Layout/Repaint B has no projection");
            Assert.That(Property(Property(root,"Context"),"CurrentTierMixed"),Is.True);
        }
        [Test] public void G4PersistentTier9_ValidateReprojectsSavedTier_AfterIntentEdit()
        {
            var material=NewMaterial();G4SpecDebugFixture.Validate(material);material.SetFloat(Tier,0);material.SetFloat("_Mask_Toggle",0);material.SetFloat("_ProgramNoise_Simple_Toggle",0);
            foreach(string gate in Gates)material.SetFloat(gate,.25f);var before=Snapshot.Read(material);var expected=Effective(material,0,ProjectPolicy(0));G4SpecDebugFixture.Validate(material);
            AssertGates(material,expected);before.AssertSame(material,"Actual Graph GUI Validate keeps Tier/intent/flags and only derives supported gates",Gates);
        }
        [Test] public void G4PersistentTier9_SaveReimport_ReopenPreservesTierAndGates()
        {
            var material=NewMaterial();G4SpecDebugFixture.Validate(material);var root=Root(material);Assert.That(Apply(root,1,ProjectPolicy(1)),Is.True);
            string folder=Path.Combine(Application.dataPath,"ResTemp/EditorTemp");Directory.CreateDirectory(folder);AssetDatabase.ImportAsset("Assets/ResTemp/EditorTemp",ImportAssetOptions.ForceSynchronousImport);
            string path="Assets/ResTemp/EditorTemp/NBFX_Tier_"+Guid.NewGuid().ToString("N")+".mat";
            var saved=new Material(material){hideFlags=HideFlags.None};AssetDatabase.CreateAsset(saved,path);assets.Add(path);EditorUtility.SetDirty(saved);AssetDatabase.SaveAssets();
            var before=Snapshot.Read(saved);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
            var loaded=AssetDatabase.LoadAssetAtPath<Material>(path);Assert.That(loaded,Is.Not.Null);before.AssertSame(loaded,"Persisted actual Material reopen");Assert.That(loaded.GetFloat(Tier),Is.EqualTo(1));
            var reopened=Root(loaded);Assert.That(Convert.ToInt32(Property(Property(reopened,"Context"),"CurrentTier")),Is.EqualTo(1));
        }

        [Test] public void G4PersistentTier9_AssignLowMask2_FirstInitialize_ValidatedCounterfactual()
        {
            string[] policy=ProjectPolicy(0);
            Assert.That(policy.Contains("_MASKMAP_ON"),Is.True,"This real Low policy retains the parent Mask1.");
            Assert.That(policy.Contains("_MASKMAP2_ON"),Is.False,"The diagnostic requires real Low policy to forbid Mask2; no guessed Tier rule.");
            var nativeShader=AssetDatabase.LoadAssetAtPath<Shader>("Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/NBShader.shader");
            var graphShader=AssetDatabase.LoadAssetAtPath<Shader>("Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBShaderGraph.shadergraph");
            Assert.That(nativeShader&&graphShader&&graphShader.isSupported,Is.True);
            var material=new Material(nativeShader){hideFlags=HideFlags.HideAndDontSave};owned.Add(material);
            material.SetFloat(Tier,0);material.SetFloat("_Mask_Toggle",1);material.SetFloat("_Mask2_Toggle",1);material.SetFloat("_Mask3_Toggle",0);
            var assignGUI=Activator.CreateInstance(TypeOf("NBShaderEditor.NBShaderGraphGUI"));
            Call(assignGUI,"AssignNewShaderToMaterial",material,nativeShader,graphShader);
            Assert.That(material.shader,Is.SameAs(graphShader));Assert.That(material.GetFloat(Tier),Is.EqualTo(0));
            Assert.That(material.GetFloat("_Mask_Toggle"),Is.EqualTo(1));Assert.That(material.GetFloat("_Mask2_Toggle"),Is.EqualTo(1),"Saved intent must survive the actual shader assignment.");
            string assigned=LifecycleState(material,"assigned");
            // Actual complete Graph GUI invokes the original Root Initialize
            // inside a native GUIView with the existing legal seed transaction.
            var host=FullHost(material,true,out var gui,out var editor);Send(host,new Event{type=EventType.Layout});Send(host,new Event{type=EventType.Repaint});
            var root=Field(gui,"_rootItem");Assert.That(root,Is.Not.Null);
            Assert.That(Call(root,"InitializeGraphMainTextureInputs"),Is.True,"Use the actual original Root initializer, not a fabricated schema setter.");
            Assert.That(material.GetFloat("_NB_GraphGUIStateVersion"),Is.EqualTo(2));Assert.That(material.GetFloat(Tier),Is.EqualTo(0));
            string[] effective=Effective(material,0,policy);Assert.That(effective.Contains("_MASKMAP_ON"),Is.True);Assert.That(effective.Contains("_MASKMAP2_ON"),Is.False);
            Assert.That(material.GetFloat("_Mask2_Toggle"),Is.EqualTo(1),"Tier filtering keeps saved child intent.");
            float initializedGate=material.GetFloat("_NB_TierAllowMask2");string initialized=LifecycleState(material,"initialized");var before=Snapshot.Read(material);
            Call(gui,"ValidateMaterial",material);
            string validated=LifecycleState(material,"validated");Assert.That(material.GetFloat("_NB_TierAllowMask2"),Is.EqualTo(0),"The actual Validate counterfactual must establish the valid Low state.");
            Assert.That(material.GetFloat(Tier),Is.EqualTo(0));Assert.That(material.GetFloat("_Mask2_Toggle"),Is.EqualTo(1));
            before.AssertSame(material,"Explicit Validate owns only derived gates; raw intent/words/keywords/passes stay intact",Gates);
            string evidence=assigned+"\n"+initialized+"\n"+validated+"\npolicyLow="+string.Join(",",policy)+"\neffectiveAfterInit="+string.Join(",",effective);
            Debug.Log("NBFX_PERSISTENT_TIER_ASSIGN_INIT_DIAGNOSTIC\n"+evidence);
            // Fail only after all phases/control are recorded. Current source
            // is expected to expose the lifecycle gap, never mark it Passed.
            Assert.That(initializedGate,Is.EqualTo(0),"First real initialize must honor saved Low without requiring reimport or a second Tier selection.\n"+evidence);
        }
        [Test] public void G4PersistentTier9_FirstSchemaMultiInitialize_CompleteUndoAndReady2ReadOnly()
        {
            var a=NewMaterial();var b=NewMaterial();a.SetFloat("_NB_GraphGUIStateVersion",0);b.SetFloat("_NB_GraphGUIStateVersion",1);a.SetFloat(Tier,0);b.SetFloat(Tier,0);
            var root=Root(a,b);var sync=Property(root,"SyncService");var beforeA=Snapshot.Read(a);var beforeB=Snapshot.Read(b);var names=InitializationOwnedNames();
            Undo.FlushUndoRecordObjects();Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();
            try
            {
                Assert.That(Call(sync,"TryInitializeGraphSupportedGateTierState"),Is.True);
                Assert.That(a.GetFloat("_NB_GraphGUIStateVersion"),Is.EqualTo(2));Assert.That(b.GetFloat("_NB_GraphGUIStateVersion"),Is.EqualTo(2));
                Assert.That(a.GetFloat(Tier),Is.EqualTo(0));Assert.That(b.GetFloat(Tier),Is.EqualTo(0));AssertGates(a,Effective(a,0,ProjectPolicy(0)));AssertGates(b,Effective(b,0,ProjectPolicy(0)));
                beforeA.AssertSame(a,"A initialization changes only original mirrors/marker and eight derived values",names);beforeB.AssertSame(b,"B initialization preserves every raw word/keyword/pass",names);
                Undo.FlushUndoRecordObjects();Undo.CollapseUndoOperations(group);var afterA=Snapshot.Read(a);var afterB=Snapshot.Read(b);
                Undo.PerformUndo();beforeA.AssertSame(a,"Complete first initialization Undo A");beforeB.AssertSame(b,"Complete first initialization Undo B");
                Undo.PerformRedo();afterA.AssertSame(a,"Complete first initialization Redo A");afterB.AssertSame(b,"Complete first initialization Redo B");
                foreach(string gate in Gates){a.SetFloat(gate,.25f);b.SetFloat(gate,.75f);}var readyA=Snapshot.Read(a);var readyB=Snapshot.Read(b);
                Assert.That(Call(sync,"TryInitializeGraphSupportedGateTierState"),Is.True);readyA.AssertSame(a,"Ready marker2 never normalizes existing raw gates on paint A");readyB.AssertSame(b,"Ready marker2 read-only B");
            }
            finally{Undo.RevertAllDownToGroup(group);}
        }
        [Test] public void G4PersistentTier9_FirstSchemaInitialize_UnknownAndMissingSchemaAtomic()
        {
            var a=NewMaterial();var b=NewMaterial();a.SetFloat("_NB_GraphGUIStateVersion",0);b.SetFloat("_NB_GraphGUIStateVersion",3);var root=Root(a,b);var beforeA=Snapshot.Read(a);var beforeB=Snapshot.Read(b);
            Assert.That(Call(Property(root,"SyncService"),"TryInitializeGraphSupportedGateTierState"),Is.False);beforeA.AssertSame(a,"Unknown second target cannot partially seed the first");beforeB.AssertSame(b,"Future marker and gate values preserved");
            string text="Shader \"Hidden/NBFX/TierInitMissingSchema\" { Properties { _NB_DistortionMode(\"D\",Float)=0 _NB_Flags0Lo16(\"0L\",Float)=0 _NB_Flags0Hi16(\"0H\",Float)=0 _NB_Flags1Lo16(\"1L\",Float)=0 _NB_Flags1Hi16(\"1H\",Float)=0 _NB_GraphGUIStateVersion(\"Version\",Float)=0 _NBShaderFeatureTier(\"Tier\",Float)=0 _MainTexBigBlockItemFoldOut(\"M\",Float)=0 _BaseMapFoldOut(\"B\",Float)=0 } SubShader { Pass { } } }";
            var shader=ShaderUtil.CreateShaderAsset(text,false);Assert.That(shader,Is.Not.Null);owned.Add(shader);var incomplete=new Material(shader){hideFlags=HideFlags.HideAndDontSave};owned.Add(incomplete);var missingRoot=Root(incomplete);var before=Snapshot.Read(incomplete);
            Assert.That(Call(Property(missingRoot,"SyncService"),"TryInitializeGraphSupportedGateTierState"),Is.False);before.AssertSame(incomplete,"Missing real mirror/gate/reader schema cannot fabricate properties or commit marker2");
        }
        static string[] InitializationOwnedNames()
        {
            var names=new List<string>{"_NB_GraphGUIStateVersion"};var type=TypeOf("NBShaderEditor.NBShaderSyncService");
            foreach(string field in new[]{"ToggleFlagBindings","ModeFlagBindings"})
                foreach(object binding in (IEnumerable)type.GetField(field,All).GetValue(null))names.Add((string)Field(binding,"propertyName"));
            Assert.That(names.Count,Is.EqualTo(30));names.AddRange(Gates);return names.ToArray();
        }
        static string LifecycleState(Material material,string phase)
        {
            return phase+" shader="+material.shader.name+" marker="+material.GetFloat("_NB_GraphGUIStateVersion")+" savedTier="+material.GetFloat(Tier)+
                " maskIntent="+material.GetFloat("_Mask_Toggle")+" mask2Intent="+material.GetFloat("_Mask2_Toggle")+" gates="+
                string.Join(",",Gates.Select(g=>g+"="+material.GetFloat(g)));
        }
    }
}
