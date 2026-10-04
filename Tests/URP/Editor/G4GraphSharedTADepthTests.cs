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
using Object = UnityEngine.Object;

namespace NBFX.Baseline.Tests
{
    // Actual shared controls/native GUIView and original Mesh depth harness.
    // Native Tier Popup selection is manual: one test calls its original callback.
    // No replacement product loop, fake float OVZ gate, or old render-test edits.
    public sealed class G4GraphSharedTADepthTests
    {
        const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
        const string Tier = "_NBShaderFeatureTier", Marker = "_NB_GraphGUIStateVersion", Keyword = "_OVERRIDE_Z";
        const string ZToggle = "_ZOffset_Toggle", OToggle = "_OverrideZ_Toggle";
        const string Graph = G4SpecDebugFixture.GraphPath;
        static readonly string[] DepthFields = { "_TABigBlockItemFoldOut", "_ZOffsetBlockFoldOut", "_OverrideZBlockFoldOut",
            ZToggle, "_offsetFactor", "_offsetUnits", OToggle, "_OverrideZValue" };
        static readonly string[] CaseNames = {
            "G4SharedTADepth_FactoryFallbackReadOnly", "G4SharedTADepth_ZOffsetEffectiveMixed_ActualToggleUndo",
            "G4SharedTADepth_OVZMixedTier_ActualToggleUndo", "G4SharedTADepth_RawNewValidate_LegacyMissingTier",
            "G4SharedTADepth_InvalidSchema_NoPartialWrite", "G4SharedTADepth_TierCallback_FinalSyncUndo",
            "G4SharedTADepth_ResetSaveReimport", "G4SharedTADepth_TierDepthGPU_ortho", "G4SharedTADepth_TierDepthGPU_perspective"
        };
        readonly List<Object> owned = new List<Object>();
        readonly List<string> assets = new List<string>();
        string caseFolder;
        int testUndoGroup = -1;
        T Keep<T>(T item) where T : Object { owned.Add(item); return item; }
        static Type Find(string name) => G4SpecDebugFixture.FindType(name);
        static object Call(object target, string method, params object[] args)
            => target.GetType().GetMethods(All).Single(m => m.Name == method && m.GetParameters().Length == args.Length).Invoke(target, args);
        static object Static(Type type, string method, params object[] args)
            => type.GetMethods(All).Single(m => m.Name == method && m.GetParameters().Length == args.Length).Invoke(null, args);
        static object Field(object target, string name)
        {
            for (Type type = target.GetType(); type != null; type = type.BaseType)
            { var field = type.GetField(name, All | BindingFlags.DeclaredOnly); if (field != null) return field.GetValue(target); }
            throw new MissingFieldException(target.GetType().FullName, name);
        }
        static object Property(object target, string name) => target.GetType().GetProperty(name, All).GetValue(target);
        static object EnumTier(int tier) => Enum.ToObject(Find("NBShader.NBShaderFeatureTier"), tier);
        static Type Applier => Find("NBShaders2.Editor.FeatureLevel.NBShaderFeatureLevelMaterialApplier");
        static string Snapshot(Material material) => EditorJsonUtility.ToJson(material) + "\n" + string.Join("|", material.shaderKeywords.OrderBy(k => k));
        static void Guard()
        {
            Assert.That(Path.GetFullPath(Application.dataPath), Is.EqualTo(Path.GetFullPath(@"D:\UnityProject\NBUnityProject\.utmp\NBFXMeshValidation-20261002\Assets")).IgnoreCase);
            for (int i = 0; i < SceneManager.sceneCount; ++i)
            {
                var scene = SceneManager.GetSceneAt(i);
                Assert.That(!scene.isLoaded || (scene.name + "/" + scene.path).IndexOf("TAI", StringComparison.OrdinalIgnoreCase) < 0, Is.True, "Loaded TAI forbids dependent shader/material imports.");
            }
        }
        [OneTimeSetUp] public void Preflight()
        {
            Guard();
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(Graph);
            Assert.That(shader && shader.isSupported, Is.True, "Runner must import the exact candidate once before discovery; this fixture does not enqueue Graph reimports.");
            Assert.That(shader.keywordSpace.FindKeyword(Keyword).isValid, Is.True);
            foreach (string field in DepthFields)
            {
                int index = shader.FindPropertyIndex(field); Assert.That(index, Is.GreaterThanOrEqualTo(0), field);
                Assert.That(shader.GetPropertyType(index), Is.EqualTo(UnityEngine.Rendering.ShaderPropertyType.Float), field);
            }
            Assert.That(ShaderUtil.GetShaderMessages(shader).Any(m => m.severity.ToString() == "Error"), Is.False);
        }
        [SetUp] public void StartEvidence()
        {
            Guard();
            string root = Environment.GetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR");
            Assert.That(string.IsNullOrEmpty(root), Is.False, "Use the bounded runner's independent evidence directory.");
            int index = Array.IndexOf(CaseNames, TestContext.CurrentContext.Test.Name); Assert.That(index, Is.GreaterThanOrEqualTo(0));
            caseFolder = Path.Combine(root, "c" + index);
            Assert.That(Directory.Exists(caseFolder), Is.False, "Never overwrite a prior attempt."); Directory.CreateDirectory(caseFolder);
            Undo.IncrementCurrentGroup(); testUndoGroup = Undo.GetCurrentGroup();
            File.WriteAllText(Path.Combine(caseFolder, "start.json"), JsonUtility.ToJson(new Step { stage = "start", caseName = TestContext.CurrentContext.Test.FullName, unity = Application.unityVersion }, true));
        }
        [Serializable] sealed class Step
        {
            public string stage, caseName, unity, shader, material;
            public float tier, marker, zToggle, factor, units, overrideToggle, overrideValue;
            public bool keyword, displayEnabled, displayMixed;
            public string[] keywords;
        }
        void Record(string stage, Material material, bool displayEnabled = false, bool displayMixed = false)
        {
            var row = new Step { stage = stage, caseName = TestContext.CurrentContext.Test.FullName, unity = Application.unityVersion,
                shader = material.shader.name, material = EditorJsonUtility.ToJson(material), keywords = material.shaderKeywords,
                tier = material.HasProperty(Tier) ? material.GetFloat(Tier) : -1, marker = material.HasProperty(Marker) ? material.GetFloat(Marker) : -1,
                zToggle = material.HasProperty(ZToggle) ? material.GetFloat(ZToggle) : 0,
                factor = material.HasProperty("_offsetFactor") ? material.GetFloat("_offsetFactor") : 0,
                units = material.HasProperty("_offsetUnits") ? material.GetFloat("_offsetUnits") : 0,
                overrideToggle = material.GetFloat(OToggle), overrideValue = material.GetFloat("_OverrideZValue"), keyword = material.IsKeywordEnabled(Keyword),
                displayEnabled = displayEnabled, displayMixed = displayMixed };
            File.AppendAllText(Path.Combine(caseFolder, "steps.jsonl"), JsonUtility.ToJson(row) + "\n");
        }
        Material New(int marker = 2)
        {
            var material = Keep(G4SpecDebugFixture.NewGraph()); material.SetFloat(Marker, marker);
            material.SetFloat("_NB_CustomDataFlag3Hi16", 42123.25f); return material;
        }
        static string[] Policy(int tier)
        {
            var type = Find("NBShaders2.Editor.FeatureLevel.NBShaderFeatureLevelProjectSettings");
            var settings = type.GetProperty("instance", All | BindingFlags.FlattenHierarchy).GetValue(null);
            return ((IEnumerable<string>)Call(settings, "GetAllowedKeywordSetForBuildInfoNoSave", EnumTier(tier))).ToArray();
        }
        static int PolicyTier(bool allow)
        {
            var tiers = Enumerable.Range(0, 4).Where(i => Policy(i).Contains(Keyword) == allow).ToArray();
            Assert.That(tiers, Is.Not.Empty, "Existing project policy must contain an " + (allow ? "allowed" : "excluded") + " OVZ Tier; do not modify configuration to conceal bad inputs.");
            return tiers[0];
        }
        static void Sync(Material material) => G4SpecDebugFixture.Sync(material);
        static void Validate(Material material) => G4SpecDebugFixture.Validate(material);
        static bool ApplyNarrow(Material material, out bool changed)
        {
            object[] args = { material, false }; bool accepted = (bool)Static(Applier, "ApplyGraphSavedOverrideDepth", args); changed = (bool)args[1]; return accepted;
        }
        object Root(params Material[] materials)
        {
            var root = Activator.CreateInstance(Find("NBShaderEditor.NBShaderRootItem")); var type = root.GetType();
            var editor = Keep((MaterialEditor)Editor.CreateEditor(materials.Cast<Object>().ToArray(), typeof(MaterialEditor)));
            type.GetField("MatEditor", All).SetValue(root, editor); type.GetField("Mats", All).SetValue(root, materials.ToList());
            type.GetField("Shader", All).SetValue(root, materials[0].shader); Call(root, "InitFlags", materials.ToList());
            var dictionary = (IDictionary)Field(root, "PropertyInfoDic");
            var same = materials.Where(m => m.shader == materials[0].shader).Cast<Object>().ToArray();
            foreach (var property in MaterialEditor.GetMaterialProperties(same))
            {
                var info = Activator.CreateInstance(Find("NBShaderEditor.ShaderPropertyInfo")); var it = info.GetType();
                it.GetField("Property").SetValue(info, property); it.GetField("Name").SetValue(info, property.name);
                it.GetField("Index").SetValue(info, materials[0].shader.FindPropertyIndex(property.name)); dictionary.Add(property.name, info);
            }
            type.GetProperty("Context", All).SetValue(root, Activator.CreateInstance(Find("NBShaderEditor.NBShaderGUIContext"), root));
            type.GetProperty("SyncService", All).SetValue(root, Activator.CreateInstance(Find("NBShaderEditor.NBShaderSyncService"), root));
            Call(Property(root, "Context"), "Refresh"); return root;
        }
        static bool Ready(object root) => (bool)Call(root, "InitializeGraphTADepthInputs");
        object DepthItem(object root, string name)
        {
            Assert.That(Ready(root), Is.True); var block = Field(root, "_graphTADepthBlock");
            if (name == "block") return block;
            return ((IList)Field(block, "ChildrenItemList")).Cast<object>().Single(item => (string)Field(item, "PropertyName") == name);
        }
        NBFXMainTexGUIEventHost Host(object root, string name, out object item)
        {
            var host = Keep(ScriptableObject.CreateInstance<NBFXMainTexGUIEventHost>());
            host.hideFlags = HideFlags.HideAndDontSave; host.position = new Rect(20, 20, 620, 440);
            host.titleContent = new GUIContent("NBFX TA depth actual events"); host.SetupCommand = "NBFX_TA_" + Guid.NewGuid().ToString("N");
            object actual = null; host.Setup = () => actual = DepthItem(root, name);
            host.ShowUtility(); host.SendEvent(new Event { type = EventType.ExecuteCommand, commandName = host.SetupCommand });
            Check(host); Assert.That(host.Initialized, Is.True, "Require real native GUIView delivery.");
            item = actual; host.Draw = () => Call(actual, "OnGUI"); return host;
        }
        static void Check(NBFXMainTexGUIEventHost host) { if (host.Failure != null) ExceptionDispatchInfo.Capture(host.Failure).Throw(); }
        void Send(NBFXMainTexGUIEventHost host, Event evt)
        {
            EventType requested = evt.rawType; Assert.That(requested, Is.Not.EqualTo(EventType.Ignore).And.Not.EqualTo(EventType.Used));
            host.Counts.TryGetValue(requested, out int before); host.SendEvent(evt); Check(host);
            Assert.That(evt.rawType, Is.EqualTo(requested));
            Assert.That(host.Counts.TryGetValue(requested, out int after) && after > before, Is.True, "Original production OnGUI must receive the event.");
            File.AppendAllText(Path.Combine(caseFolder, "events.log"), requested + " " + before + "->" + after + "\n");
        }
        void Paint(NBFXMainTexGUIEventHost host) { Send(host, new Event { type = EventType.Layout }); Send(host, new Event { type = EventType.Repaint }); }
        void Click(NBFXMainTexGUIEventHost host, object item, string rectangle = "ControlRect")
        {
            Paint(host); var rect = (Rect)Field(item, rectangle); Assert.That(rect.width, Is.GreaterThan(0));
            Send(host, new Event { type = EventType.MouseDown, button = 0, mousePosition = rect.center });
            Send(host, new Event { type = EventType.MouseUp, button = 0, mousePosition = rect.center });
        }
        static void Display(object item, out bool enabled, out bool mixed)
        {
            object[] args = { false, false }; Assert.That(Call(item, "TryGetGraphZOffsetDisplay", args), Is.True);
            enabled = (bool)args[0]; mixed = (bool)args[1];
        }
        void UndoRedo(NBFXMainTexGUIEventHost host, int group, Material[] materials, string[] before)
        {
            Undo.FlushUndoRecordObjects(); Undo.CollapseUndoOperations(group); string[] after = materials.Select(Snapshot).ToArray(); host.Draw = null;
            Undo.PerformUndo(); for (int i = 0; i < materials.Length; ++i) Assert.That(Snapshot(materials[i]), Is.EqualTo(before[i]), "Exact full material Undo " + i);
            Undo.PerformRedo(); for (int i = 0; i < materials.Length; ++i) Assert.That(Snapshot(materials[i]), Is.EqualTo(after[i]), "Exact full material Redo " + i);
        }
        Material Probe(string omit = null, string wrongProperty = null, string wrongType = "Integer", bool tier = true, bool marker = true, bool keyword = true)
        {
            Guard(); var fields = new[] { "_NB_DistortionMode", "_NB_Flags0Lo16", "_NB_Flags0Hi16", "_NB_Flags1Lo16", "_NB_Flags1Hi16" }.Concat(DepthFields).ToList();
            if (tier) fields.Add(Tier); if (marker) fields.Add(Marker);
            string source = "Shader \"Hidden/NBFX/TA/" + Guid.NewGuid().ToString("N") + "\" { Properties { ";
            foreach (string field in fields.Distinct()) if (field != omit)
                source += field + "(\"" + field + "\"," + (field == wrongProperty ? wrongType : "Float") + ")=" + (field == Tier ? "3" : field == "_OverrideZValue" ? "7" : "0") + "\n";
            source += "} SubShader { Pass { HLSLPROGRAM\n#pragma vertex vert\n#pragma fragment frag\n" + (keyword ? "#pragma shader_feature_local _ _OVERRIDE_Z\n" : "") +
                "float4 vert(float4 p:POSITION):SV_POSITION{return p;}\nfloat4 frag():SV_Target{\n#ifdef _OVERRIDE_Z\nreturn float4(1,0,0,1);\n#else\nreturn float4(0,1,0,1);\n#endif\n}\nENDHLSL\n} } }";
            var shader = Keep(ShaderUtil.CreateShaderAsset(source, false)); Assert.That(shader, Is.Not.Null); shader.hideFlags = HideFlags.HideAndDontSave;
            Assert.That(ShaderUtil.GetShaderMessages(shader).Any(m => m.severity.ToString() == "Error"), Is.False);
            Assert.That(shader.keywordSpace.FindKeyword(Keyword).isValid, Is.EqualTo(keyword));
            return Keep(new Material(shader) { hideFlags = HideFlags.HideAndDontSave });
        }
        [TearDown] public void Cleanup()
        {
            try
            {
                foreach (var host in owned.OfType<NBFXMainTexGUIEventHost>()) if (host) { host.Draw = null; host.Setup = null; host.Close(); }
                if (testUndoGroup >= 0) Undo.RevertAllDownToGroup(testUndoGroup);
                foreach (string path in assets)
                {
                    Guard(); Assert.That(path.StartsWith("Assets/ResTemp/EditorTemp/NBFX_TA_", StringComparison.Ordinal), Is.True);
                    Assert.That(AssetDatabase.DeleteAsset(path), Is.True);
                }
            }
            finally { testUndoGroup = -1; assets.Clear(); foreach (var item in owned.AsEnumerable().Reverse()) if (item) Object.DestroyImmediate(item); owned.Clear(); }
        }

        [Test] public void G4SharedTADepth_FactoryFallbackReadOnly()
        {
            var material = New(); var root = Root(material); string before = Snapshot(material);
            var host = Host(root, "block", out var block); Paint(host); Assert.That(Snapshot(material), Is.EqualTo(before));
            Assert.That(block.GetType().FullName, Is.EqualTo("NBShaderEditor.BigBlockItem"));
            var children = ((IList)Field(block, "ChildrenItemList")).Cast<object>().ToArray(); Assert.That(children.Length, Is.EqualTo(2));
            Assert.That(children.Select(x => (string)Field(x, "PropertyName")), Is.EquivalentTo(new[] { ZToggle, OToggle }));
            Assert.That(children.All(x => x.GetType().FullName == "NBShaderEditor.PropertyToggleBlockItem"), Is.True);
            Assert.That((IEnumerable<string>)Call(root, "GetSharedGraphPropertyNames"), Is.EquivalentTo(DepthFields)); host.Draw = null;
            foreach (string field in new[] { "_TABigBlockItemFoldOut", "_ZOffsetBlockFoldOut", "_OverrideZBlockFoldOut" })
            {
                var invalid = Probe(omit: field); var invalidRoot = Root(invalid); string saved = Snapshot(invalid);
                Assert.That(Ready(invalidRoot), Is.False); Assert.That((IEnumerable<string>)Call(invalidRoot, "GetSharedGraphPropertyNames"), Is.Empty);
                Assert.That(Snapshot(invalid), Is.EqualTo(saved));
            }
            var wrong = Probe(wrongProperty: "_ZOffsetBlockFoldOut"); Assert.That(Ready(Root(wrong)), Is.False);
            var native = Keep(new Material(AssetDatabase.LoadAssetAtPath<Shader>("Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/NBShader.shader")));
            before = Snapshot(material); Assert.That(Ready(Root(material, native)), Is.False); Assert.That(Ready(Root(material, Probe())), Is.False); Assert.That(Snapshot(material), Is.EqualTo(before));
            Record("factory-read-only", material);
        }

        [Test] public void G4SharedTADepth_ZOffsetEffectiveMixed_ActualToggleUndo()
        {
            var a = New(); var b = New(); a.SetFloat(ZToggle, 0); b.SetFloat(ZToggle, 0); a.SetFloat("_offsetFactor", 2); b.SetFloat("_offsetUnits", -3);
            var host = Host(Root(a, b), ZToggle, out var item); string[] before = { Snapshot(a), Snapshot(b) };
            Paint(host); Display(item, out bool enabled, out bool mixed); Assert.That(enabled, Is.True); Assert.That(mixed, Is.False);
            Assert.That(Snapshot(a), Is.EqualTo(before[0])); Assert.That(Snapshot(b), Is.EqualTo(before[1])); Record("legacy-display-on", a, enabled, mixed);
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
            try
            {
                Click(host, item);
                foreach (var material in new[] { a, b })
                {
                    Assert.That(material.GetFloat(ZToggle), Is.Zero, "Stored 0->0 must still enter the off side effect.");
                    Assert.That(material.GetFloat("_offsetFactor"), Is.Zero); Assert.That(material.GetFloat("_offsetUnits"), Is.Zero);
                }
                Record("actual-off-clears-both", b); UndoRedo(host, group, new[] { a, b }, before);
            }
            finally { host.Draw = null; Undo.RevertAllDownToGroup(group); }
            var c = New(); var d = New(); d.SetFloat("_offsetUnits", 4);
            var mixedHost = Host(Root(c, d), ZToggle, out var mixedItem); Display(mixedItem, out enabled, out mixed);
            Assert.That(enabled, Is.False); Assert.That(mixed, Is.True, "Mixed must use per-material effective state, despite both stored toggles being zero.");
            before = new[] { Snapshot(c), Snapshot(d) }; Undo.IncrementCurrentGroup(); group = Undo.GetCurrentGroup();
            try
            {
                Click(mixedHost, mixedItem); Assert.That(c.GetFloat(ZToggle), Is.EqualTo(1)); Assert.That(d.GetFloat(ZToggle), Is.EqualTo(1));
                Assert.That(c.GetFloat("_offsetFactor") + c.GetFloat("_offsetUnits"), Is.Zero, "On cannot invent an offset."); Assert.That(d.GetFloat("_offsetUnits"), Is.EqualTo(4));
                Display(mixedItem, out enabled, out mixed); Assert.That(enabled, Is.True); Assert.That(mixed, Is.False);
                Record("mixed-to-on", d, enabled, mixed); UndoRedo(mixedHost, group, new[] { c, d }, before);
            }
            finally { mixedHost.Draw = null; Undo.RevertAllDownToGroup(group); }
        }

        [Test] public void G4SharedTADepth_OVZMixedTier_ActualToggleUndo()
        {
            int low = PolicyTier(false), high = PolicyTier(true); var a = New(); var b = New();
            a.SetFloat(Tier, low); b.SetFloat(Tier, high); a.SetFloat(OToggle, 0); b.SetFloat(OToggle, 0); Sync(a); Sync(b);
            var host = Host(Root(a, b), OToggle, out var item); string[] before = { Snapshot(a), Snapshot(b) };
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
            try
            {
                Click(host, item); Sync(a); Sync(b);
                Assert.That(a.GetFloat(OToggle), Is.EqualTo(1)); Assert.That(b.GetFloat(OToggle), Is.EqualTo(1));
                Assert.That(a.IsKeywordEnabled(Keyword), Is.False); Assert.That(b.IsKeywordEnabled(Keyword), Is.True);
                Assert.That(a.GetFloat(Tier), Is.EqualTo(low)); Assert.That(b.GetFloat(Tier), Is.EqualTo(high));
                Assert.That(a.GetFloat("_NB_CustomDataFlag3Hi16"), Is.EqualTo(42123.25f));
                Record("mixed-low", a); Record("mixed-high", b); UndoRedo(host, group, new[] { a, b }, before);
            }
            finally { host.Draw = null; Undo.RevertAllDownToGroup(group); }
        }

        [Test] public void G4SharedTADepth_RawNewValidate_LegacyMissingTier()
        {
            foreach (bool allowed in new[] { false, true })
            {
                var material = New(0); material.SetFloat(Tier, PolicyTier(allowed)); material.SetFloat(OToggle, 1); material.SetFloat("_OverrideZValue", 7);
                material.SetFloat("_NB_Flags0Lo16", -3.75f); material.SetFloat("_NB_Flags1Hi16", 65536.25f);
                Validate(material); Assert.That(material.GetFloat(Marker), Is.Zero); Assert.That(material.GetFloat(OToggle), Is.EqualTo(1));
                Assert.That(material.IsKeywordEnabled(Keyword), Is.EqualTo(allowed)); Assert.That(material.GetFloat("_NB_Flags0Lo16"), Is.EqualTo(-3.75f));
                Assert.That(material.GetFloat("_NB_Flags1Hi16"), Is.EqualTo(65536.25f)); Assert.That(material.GetFloat("_NB_CustomDataFlag3Hi16"), Is.EqualTo(42123.25f));
                Record("raw-new-validate-" + allowed, material);
            }
            foreach (bool marker in new[] { false, true })
            {
                var legacy = Probe(tier: false, marker: marker); legacy.SetFloat(OToggle, 1); Sync(legacy);
                Assert.That(legacy.IsKeywordEnabled(Keyword), Is.True); Assert.That(legacy.HasProperty(Tier), Is.False);
                if (marker) Assert.That(legacy.GetFloat(Marker), Is.Zero);
                legacy.SetFloat(OToggle, 0); Sync(legacy); Assert.That(legacy.IsKeywordEnabled(Keyword), Is.False); Record("legacy-no-tier-marker-" + marker, legacy);
            }
        }

        [Test] public void G4SharedTADepth_InvalidSchema_NoPartialWrite()
        {
            Action<Material> refuses = material => {
                string before = Snapshot(material); bool changed; Assert.That(ApplyNarrow(material, out changed), Is.False); Assert.That(changed, Is.False); Assert.That(Snapshot(material), Is.EqualTo(before));
            };
            foreach (float value in new[] { float.NaN, float.PositiveInfinity, -.25f, .5f, 4f })
            { var material = New(); material.SetFloat(Tier, value); material.SetFloat(OToggle, 1); material.EnableKeyword(Keyword); refuses(material); }
            foreach (string field in new[] { Marker, OToggle, "_OverrideZValue" })
            { var material = New(); material.SetFloat(field, field == Marker ? 3 : float.NaN); material.EnableKeyword(Keyword); refuses(material); }
            refuses(Probe(wrongProperty: Tier)); refuses(Probe(wrongProperty: Tier, wrongType: "Range(0,3)")); refuses(Probe(keyword: false));
            var a = New(); var b = New(); b.SetFloat(Marker, 3); var root = Root(a, b); string first = Snapshot(a), second = Snapshot(b);
            Assert.That(Call(Property(root, "SyncService"), "TryApplyGraphTADepthToggle", OToggle, true), Is.False);
            Assert.That(Call(Property(root, "SyncService"), "TryApplyGraphSupportedGateTier", EnumTier(0), Array.Empty<string>()), Is.False);
            Assert.That(Snapshot(a), Is.EqualTo(first)); Assert.That(Snapshot(b), Is.EqualTo(second));
        }

        [Test] public void G4SharedTADepth_TierCallback_FinalSyncUndo()
        {
            int low = PolicyTier(false), high = PolicyTier(true); var a = New(); var b = New();
            foreach (var material in new[] { a, b }) { material.SetFloat(OToggle, 1); material.SetFloat(Tier, high); Validate(material); }
            var root = Root(a, b); var host = Host(root, OToggle, out var item); var toolbar = Activator.CreateInstance(Find("NBShaderEditor.NBShaderGUIToolBar"), root);
            string[] before = { Snapshot(a), Snapshot(b) }; Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
            try
            {
                Call(toolbar, "SetFeatureTier", EnumTier(low)); foreach (var material in new[] { a, b }) { Sync(material); Assert.That(material.IsKeywordEnabled(Keyword), Is.False); Assert.That(material.GetFloat(OToggle), Is.EqualTo(1)); Assert.That(material.GetFloat(Tier), Is.EqualTo(low)); }
                Record("original-tier-callback-low-final-sync", a); UndoRedo(host, group, new[] { a, b }, before);
            }
            finally { host.Draw = null; Undo.RevertAllDownToGroup(group); }
            // Explicit allowed-set differs deliberately from saved project policy.
            Assert.That(Call(Property(root, "SyncService"), "TryApplyGraphSupportedGateTier", EnumTier(low), new[] { Keyword }), Is.True);
            Assert.That(a.IsKeywordEnabled(Keyword), Is.True, "No hard-coded minimum Tier overrides the supplied legal allowed-set.");
            Sync(a); Assert.That(a.IsKeywordEnabled(Keyword), Is.False, "Final GUI Sync uses actual saved project policy and cannot reopen excluded OVZ.");
            Assert.That(Call(Property(root, "SyncService"), "TryApplyGraphSupportedGateTier", EnumTier(high), Array.Empty<string>()), Is.True);
            Assert.That(a.IsKeywordEnabled(Keyword), Is.False); Sync(a); Assert.That(a.IsKeywordEnabled(Keyword), Is.True);
            var nativeShader = AssetDatabase.LoadAssetAtPath<Shader>("Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/NBShader.shader");
            var assigned = Keep(new Material(nativeShader) { hideFlags = HideFlags.HideAndDontSave }); assigned.SetFloat(Tier, low); assigned.SetFloat(OToggle, 1);
            var gui = Activator.CreateInstance(Find("NBShaderEditor.NBShaderGraphGUI")); Call(gui, "AssignNewShaderToMaterial", assigned, nativeShader, a.shader);
            Assert.That(assigned.shader, Is.SameAs(a.shader)); Assert.That(assigned.GetFloat(Tier), Is.EqualTo(low)); Assert.That(assigned.GetFloat(OToggle), Is.EqualTo(1)); Assert.That(assigned.IsKeywordEnabled(Keyword), Is.False);
            File.WriteAllText(Path.Combine(caseFolder, "manual-boundary.txt"), "SetFeatureTier is the actual menu callback. Native popup choice was not clicked; manual menu selection remains pending. Invalid-target preflight and Undo are actual. No injected post-preflight failure/rollback coverage is claimed.");
        }

        [Test] public void G4SharedTADepth_ResetSaveReimport()
        {
            var material = New(); material.SetFloat(Tier, PolicyTier(true)); material.SetFloat(ZToggle, 1); material.SetFloat("_offsetFactor", 2); material.SetFloat("_offsetUnits", -3);
            material.SetFloat(OToggle, 1); material.SetFloat("_OverrideZValue", 7); foreach (string fold in DepthFields.Take(3)) material.SetFloat(fold, 1); Sync(material);
            var host = Host(Root(material), "block", out var block); string before = Snapshot(material); Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
            try
            {
                Click(host, block, "ResetRect"); Assert.That(material.GetFloat(ZToggle), Is.Zero); Assert.That(material.GetFloat("_offsetFactor"), Is.Zero); Assert.That(material.GetFloat("_offsetUnits"), Is.Zero);
                Assert.That(material.GetFloat(OToggle), Is.Zero); Assert.That(material.IsKeywordEnabled(Keyword), Is.False);
                Assert.That(material.GetFloat("_OverrideZValue"), Is.EqualTo(material.shader.GetPropertyDefaultFloatValue(material.shader.FindPropertyIndex("_OverrideZValue"))));
                UndoRedo(host, group, new[] { material }, new[] { before });
            }
            finally { host.Draw = null; Undo.RevertAllDownToGroup(group); }
            // Persist the actually restored values from the tested Undo group.
            Guard();
            foreach (string folder in new[] { "Assets/ResTemp", "Assets/ResTemp/EditorTemp" })
                if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(Path.GetDirectoryName(folder).Replace('\\', '/'), Path.GetFileName(folder));
            string path = "Assets/ResTemp/EditorTemp/NBFX_TA_" + Guid.NewGuid().ToString("N") + ".mat";
            var saved = new Material(material) { hideFlags = HideFlags.None }; AssetDatabase.CreateAsset(saved, path); assets.Add(path);
            EditorUtility.SetDirty(saved); AssetDatabase.SaveAssetIfDirty(saved); Guard(); AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            var loaded = AssetDatabase.LoadAssetAtPath<Material>(path); Assert.That(loaded, Is.Not.Null);
            foreach (string field in DepthFields.Concat(new[] { Tier, Marker, "_NB_CustomDataFlag3Hi16" })) Assert.That(loaded.GetFloat(field), Is.EqualTo(material.GetFloat(field)), field);
            Assert.That(loaded.IsKeywordEnabled(Keyword), Is.EqualTo(material.IsKeywordEnabled(Keyword)));
            var reopened = Host(Root(loaded), "block", out var reopenedBlock); before = Snapshot(loaded); Paint(reopened); Assert.That(Snapshot(loaded), Is.EqualTo(before)); reopened.Draw = null;
            Record("save-exact-reimport-new-root", loaded);
            File.WriteAllText(Path.Combine(caseFolder, "reload-boundary.txt"), "Exact material save/reimport and new GUI root verified here. A full scripting-domain reload is not requested from within a running NUnit case.");
        }

        [Serializable] sealed class DepthEvidence
        {
            public string scope, api, unity;
            public int blockedTier, allowedTier, size;
            public bool finite, healthy;
            public float[] responseBlocked, responseAllowed, keywordReferenceDelta;
            public int[] redPixels, greenPixels;
        }
        [TestCase(true, TestName = "G4SharedTADepth_TierDepthGPU_ortho")]
        [TestCase(false, TestName = "G4SharedTADepth_TierDepthGPU_perspective")]
        public void TierDepthGPU(bool ortho)
        {
            int low = PolicyTier(false), high = PolicyTier(true); string id = ortho ? "ta-o" : "ta-p";
            using (var h = new G4SpecDebugFixture.Harness(id, ortho))
            {
                h.renderer.transform.localScale = new Vector3(1.5f, 1.5f, 1); h.renderer.transform.rotation = Quaternion.identity;
                h.camera.orthographicSize = 1.4f; h.camera.fieldOfView = 42; h.camera.nearClipPlane = .1f; h.camera.farClipPlane = 25; h.camera.backgroundColor = Color.blue;
                Color[] empty = h.Snap("empty"); var probe = h.AddProbe(); probe.transform.localScale = new Vector3(2, 2, 1);
                var white = h.Constant(Color.white);
                for (int i = 0; i < 3; ++i)
                {
                    Static(typeof(G4GraphOverrideDepthTests), "ConfigureBase", h.materials[i], i == 2, white);
                    var material = h.materials[i]; material.SetFloat("_VAT_Toggle", 0); material.SetColor("_BaseColor", Color.red); material.SetColor("_Color", Color.red);
                    material.SetFloat(OToggle, 1); material.SetFloat("_AlphaClip", 0); material.DisableKeyword("_ALPHATEST_ON");
                }
                float originalCustomHalf = h.materials[2].GetFloat("_NB_CustomDataFlag3Hi16");
                var frames = new Color[4][][]; var repeats = new Color[4][][]; var references = new float[4]; var referenceFrames = new List<Color[]>();
                void State(Material material, int index, int tier, float depth, bool enabled)
                {
                    material.SetFloat(OToggle, 1); material.SetFloat("_OverrideZValue", depth);
                    if (index == 2) { material.SetFloat(Tier, tier); Validate(material); Sync(material); }
                    else G4SpecDebugFixture.SetKeyword(material, Keyword, enabled);
                    G4SpecDebugFixture.Harness.RestoreForward(material, index == 2); material.SetFloat("_ZWrite", 1);
                    Assert.That(material.IsKeywordEnabled(Keyword), Is.EqualTo(enabled)); Assert.That(material.GetFloat(OToggle), Is.EqualTo(1));
                    Record("gpu-before-capture-" + index + "-tier" + tier + "-depth" + depth, material);
                }
                for (int policy = 0; policy < 2; ++policy) for (int depthIndex = 0; depthIndex < 2; ++depthIndex)
                {
                    int state = policy * 2 + depthIndex, tier = policy == 0 ? low : high; bool enabled = policy == 1; float depth = depthIndex == 0 ? 3 : 7;
                    frames[state] = new Color[3][]; repeats[state] = new Color[3][];
                    for (int index = 0; index < 3; ++index)
                    {
                        State(h.materials[index], index, tier, depth, enabled);
                        frames[state][index] = h.Snap("ABC"[index] + "-s" + state, h.materials[index]);
                        repeats[state][index] = h.Snap("ABC"[index] + "-s" + state + "-repeat", h.materials[index]);
                    }
                    var reference = Keep(new Material(h.materials[2]) { hideFlags = HideFlags.HideAndDontSave });
                    // Independent explicit-keyword reference after real validation,
                    // like the original keyword-authority cases, not a fake GUI result.
                    reference.SetFloat(Tier, enabled ? low : high); Validate(reference);
                    Assert.That(reference.IsKeywordEnabled(Keyword), Is.EqualTo(!enabled));
                    G4SpecDebugFixture.Harness.RestoreForward(reference, true); reference.SetFloat("_ZWrite", 1);
                    G4SpecDebugFixture.SetKeyword(reference, Keyword, enabled);
                    var referenceFrame = h.Snap("reference-s" + state, reference); referenceFrames.Add(referenceFrame);
                    references[state] = G4SpecDebugFixture.Delta(frames[state][2], referenceFrame);
                }
                var all = frames.SelectMany(s => s).Concat(repeats.SelectMany(s => s)).Concat(referenceFrames).ToArray();
                var details = new DepthEvidence { scope = "Ordinary Mesh shared Tier OVZ. Original Harness/AddProbe/ConfigureBase, depth3/7, actor always active. No new nonzero ZOffset combination, NBPost, VFX or Player claim.",
                    api = SystemInfo.graphicsDeviceType.ToString(), unity = Application.unityVersion, blockedTier = low, allowedTier = high, size = h.camera.targetTexture.width,
                    finite = all.All(G4SpecDebugFixture.Finite), healthy = all.All(pixels => pixels.All(pixel => Enumerable.Range(0, 4).All(k => Mathf.Abs(pixel[k]) < 65504f))),
                    responseBlocked = Enumerable.Range(0, 3).Select(i => G4SpecDebugFixture.Delta(frames[0][i], frames[1][i])).ToArray(),
                    responseAllowed = Enumerable.Range(0, 3).Select(i => G4SpecDebugFixture.Delta(frames[2][i], frames[3][i])).ToArray(), keywordReferenceDelta = references,
                    redPixels = all.Select(p => p.Count(c => c.r > .5f && c.g < .1f)).ToArray(), greenPixels = all.Select(p => p.Count(c => c.g > .5f && c.r < .1f)).ToArray() };
                File.WriteAllText(Path.Combine(h.folder, "ta-depth.json"), JsonUtility.ToJson(details, true));
                var metrics = new G4SpecDebugFixture.Metrics { caseId = id, scope = details.scope, finite = details.finite && G4SpecDebugFixture.Finite(empty),
                    ab = frames.Select(p => G4SpecDebugFixture.Delta(p[0], p[1])).ToArray(), bc = frames.Select(p => G4SpecDebugFixture.Delta(p[1], p[2])).ToArray(),
                    repeat = Enumerable.Range(0, 4).SelectMany(s => Enumerable.Range(0, 3).Select(i => G4SpecDebugFixture.Delta(frames[s][i], repeats[s][i]))).ToArray(),
                    visible = all.Select(p => G4SpecDebugFixture.Visible(p, empty)).ToArray(), response = details.responseAllowed };
                h.SaveAndAssert(metrics); Assert.That(details.healthy, Is.True); Assert.That(references.All(d => d == 0), Is.True);
                Assert.That(details.responseBlocked.All(d => d == 0), Is.True); Assert.That(details.responseAllowed.All(d => d > .1f), Is.True);
                foreach (int index in Enumerable.Range(0, 3))
                {
                    Assert.That(frames[2][index].Count(c => c.r > .5f && c.g < .1f), Is.GreaterThan(150));
                    Assert.That(frames[3][index].Count(c => c.g > .5f && c.r < .1f), Is.GreaterThan(150));
                }
                Assert.That(h.materials[2].GetFloat(OToggle), Is.EqualTo(1)); Assert.That(h.materials[2].GetFloat("_NB_CustomDataFlag3Hi16"), Is.EqualTo(originalCustomHalf));
            }
        }
    }
}
