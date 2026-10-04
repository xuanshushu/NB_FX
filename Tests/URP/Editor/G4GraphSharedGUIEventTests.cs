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
    [Serializable] internal sealed class NBFXSharedGUIEventStep
    {
        public string caseName, stage, utc, eventType, rawEventType, commandName, shaderName, shaderAssetPath, materialJson;
        public int sequence, hostInstanceId;
        public bool initialized, vatDeclared, vatEnabledString;
        public float mode, vat, flipbook, customData2Lo16, customData3Hi16;
        public string[] declaredKeywordNames, shaderKeywords, enabledLocalKeywordNames;
    }
    internal static class NBFXSharedGUIEventStepLog
    {
        static string file, caseName; static int sequence;
        internal static void Begin(string name)
        {
            caseName = name; sequence = 0;
            string evidence = Environment.GetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR");
            if (string.IsNullOrEmpty(evidence)) throw new InvalidOperationException("Real event diagnostic requires the runner evidence directory.");
            string directory = Path.Combine(evidence, "shared-gui-event-steps"); Directory.CreateDirectory(directory);
            string label = new string(name.Where(char.IsLetterOrDigit).Take(48).ToArray());
            file = Path.Combine(directory, label + "-" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".jsonl");
            using (var created = new FileStream(file, FileMode.CreateNew, FileAccess.Write)) { }
            Write("CaseStart");
        }
        internal static void Write(string stage, NBFXSharedGUIEventHost host = null, Event evt = null, Material material = null)
        {
            if (file == null) throw new InvalidOperationException("Native event step logging was not initialized.");
            var row = new NBFXSharedGUIEventStep { caseName = caseName, stage = stage,
                utc = DateTime.UtcNow.ToString("O"), sequence = ++sequence,
                hostInstanceId = host ? host.GetInstanceID() : 0, initialized = host && host.Initialized,
                eventType = evt == null ? "" : evt.type.ToString(), rawEventType = evt == null ? "" : evt.rawType.ToString(), commandName = evt == null ? "" : evt.commandName };
            if (material)
            {
                row.shaderName = material.shader.name; row.shaderAssetPath = AssetDatabase.GetAssetPath(material.shader);
                row.declaredKeywordNames = material.shader.keywordSpace.keywordNames;
                row.vatDeclared = material.shader.keywordSpace.FindKeyword("_VAT").isValid;
                row.vatEnabledString = material.IsKeywordEnabled("_VAT");
                row.shaderKeywords = material.shaderKeywords;
                row.enabledLocalKeywordNames = material.enabledKeywords.Select(k => k.name).ToArray();
                row.mode = material.GetFloat("_FxLightMode"); row.vat = material.GetFloat("_VAT_Toggle");
                row.flipbook = material.GetFloat("_FlipbookBlending"); row.customData2Lo16 = material.GetFloat("_NB_CustomDataFlag2Lo16");
                row.customData3Hi16 = material.GetFloat("_NB_CustomDataFlag3Hi16"); row.materialJson = EditorJsonUtility.ToJson(material);
            }
            File.AppendAllText(file, JsonUtility.ToJson(row) + "\n");
        }
        internal static void Materials(string stage, Material a, Material b)
        { Write(stage + ":A", material: a); Write(stage + ":B", material: b); }
    }

    // Test-only real native GUIView host. Production Item.OnGUI performs the
    // controller draw, mixed/animated scopes and OnEndChange; no Commit call.
    public sealed class NBFXSharedGUIEventHost : EditorWindow
    {
        internal Action Setup;
        internal Action Draw;
        internal Exception Failure;
        internal bool Initialized;
        internal string SetupCommand;
        internal readonly Dictionary<EventType, int> Counts = new Dictionary<EventType, int>();

        void OnGUI()
        {
            var current = Event.current;
            var originalType = current.type;
            try
            {
                NBFXSharedGUIEventStepLog.Write("OnGUI:Enter", this, current);
                if (current.type == EventType.ExecuteCommand && current.commandName == SetupCommand)
                {
                    var initialize = typeof(EditorStyles).GetMethod("UpdateSkinCache", BindingFlags.Static | BindingFlags.NonPublic,
                        null, Type.EmptyTypes, null);
                    Assert.That(initialize, Is.Not.Null, "Use the current official skin initialization inside native OnGUI.");
                    NBFXSharedGUIEventStepLog.Write("OnGUI:BeforeSkinCache", this, current);
                    initialize.Invoke(null, null);
                    NBFXSharedGUIEventStepLog.Write("OnGUI:AfterSkinCacheBeforeSetup", this, current);
                    Setup(); Initialized = true;
                    NBFXSharedGUIEventStepLog.Write("OnGUI:AfterSetup", this, current);
                    current.Use(); return;
                }
                if (!Initialized || Draw == null) return;
                NBFXSharedGUIEventStepLog.Write("OnGUI:BeforeProductionDraw", this, current);
                Draw();
                NBFXSharedGUIEventStepLog.Write("OnGUI:AfterProductionDraw", this, current);
                Counts.TryGetValue(originalType, out int count); Counts[originalType] = count + 1;
                NBFXSharedGUIEventStepLog.Write("OnGUI:Counted", this, current);
            }
            catch (Exception exception)
            {
                NBFXSharedGUIEventStepLog.Write("OnGUI:Exception:" + exception.GetType().FullName, this, current);
                Failure = exception is TargetInvocationException invocation && invocation.InnerException != null
                    ? invocation.InnerException : exception;
            }
        }
    }

    public sealed class G4GraphSharedGUIEventTests
    {
        const BindingFlags All = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        const string Graph = "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBShaderGraph.shadergraph";
        const string Mode = "_FxLightMode", Flipbook = "_FlipbookBlending", VAT = "_VAT_Toggle";
        readonly List<Object> owned = new List<Object>();

        Type FindType(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name, false)).First(t => t != null);
        static void Guard()
        {
            Assert.That(Path.GetFullPath(Application.dataPath), Is.EqualTo(Path.GetFullPath(
                @"D:\UnityProject\NBUnityProject\.utmp\NBFXMeshValidation-20261002\Assets")).IgnoreCase);
            for (int i = 0; i < SceneManager.sceneCount; ++i)
            {
                var scene = SceneManager.GetSceneAt(i);
                Assert.That((scene.name + "/" + scene.path).IndexOf("TAI", StringComparison.OrdinalIgnoreCase), Is.LessThan(0));
            }
        }
        [OneTimeSetUp] public void Preflight() { Guard(); NBFXSharedGUIEventStepLog.Begin("NBFX.Baseline.Tests.G4GraphSharedGUIEventTests:Preflight"); NBFXSharedGUIEventStepLog.Write("Preflight:BeforeOriginalGraphImport"); AssetDatabase.ImportAsset(Graph, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport); NBFXSharedGUIEventStepLog.Write("Preflight:AfterOriginalGraphImport"); }
        [SetUp] public void StartRealEventStepLog() { NBFXSharedGUIEventStepLog.Begin(TestContext.CurrentContext.Test.FullName); }
        Material NewMaterial()
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(Graph); Assert.That(shader && shader.isSupported, Is.True);
            var material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            material.SetFloat("_NB_GraphGUIStateVersion", 2); owned.Add(material); return material;
        }
        object Root(params Material[] materials)
        {
            var root = Activator.CreateInstance(FindType("NBShaderEditor.NBShaderRootItem")); var type = root.GetType();
            var editor = (MaterialEditor)Editor.CreateEditor(materials.Cast<Object>().ToArray(), typeof(MaterialEditor)); owned.Add(editor);
            type.GetField("MatEditor", All).SetValue(root, editor); type.GetField("Mats", All).SetValue(root, materials.ToList());
            type.GetField("Shader", All).SetValue(root, materials[0].shader); type.GetMethod("InitFlags", All).Invoke(root, new object[] { materials.ToList() });
            var dictionary = (IDictionary)type.GetField("PropertyInfoDic", All).GetValue(root);
            foreach (var property in MaterialEditor.GetMaterialProperties(materials.Cast<Object>().ToArray()))
            {
                var info = Activator.CreateInstance(FindType("NBShaderEditor.ShaderPropertyInfo")); var infoType = info.GetType();
                infoType.GetField("Property").SetValue(info, property); infoType.GetField("Name").SetValue(info, property.name);
                infoType.GetField("Index").SetValue(info, materials[0].shader.FindPropertyIndex(property.name)); dictionary.Add(property.name, info);
            }
            return root;
        }
        object GetItem(object root, string kind)
        {
            var type = root.GetType();
            Assert.That((bool)type.GetMethod(kind == "light" ? "InitializeGraphLightModeInputs" : "InitializeGraphFlipbookInputs", All).Invoke(root, null), Is.True);
            if (kind == "flipbook") return type.GetField("_graphFlipbookItem", All).GetValue(root);
            var block = type.GetField("_graphLightModeBlock", All).GetValue(root);
            return ((IList)block.GetType().GetField("ChildrenItemList", All).GetValue(block))[0];
        }
        void SyncLight(Material material) => FindType("NBShaderEditor.NBShaderGraphGUI").GetMethod("SyncSixWayKeywords", All).Invoke(null, new object[] { material });
        string Snapshot(Material material) => EditorJsonUtility.ToJson(material) + "\n" + string.Join("|", material.shaderKeywords.OrderBy(k => k));
        void AssertMixed(object item)
        {
            var info = item.GetType().GetField("PropertyInfo", All).GetValue(item);
            var property = (MaterialProperty)info.GetType().GetField("Property", All).GetValue(info);
            Assert.That(property.hasMixedValue, Is.True, "The actual Item must start with a real mixed MaterialProperty.");
        }
        void CheckHost(NBFXSharedGUIEventHost host) { if (host.Failure != null) ExceptionDispatchInfo.Capture(host.Failure).Throw(); }
        NBFXSharedGUIEventHost Host(object root, string kind, out object item)
        {
            var host = ScriptableObject.CreateInstance<NBFXSharedGUIEventHost>(); owned.Add(host);
            host.hideFlags = HideFlags.HideAndDontSave; host.titleContent = new GUIContent("NBFX shared GUI event test");
            host.position = new Rect(20, 20, 480, 130); host.SetupCommand = "NBFX_SharedGUI_Setup_" + Guid.NewGuid().ToString("N");
            object actualItem = null; host.Setup = () => { NBFXSharedGUIEventStepLog.Write("Host:SetupBeforeOriginalFactory", host); actualItem = GetItem(root, kind); NBFXSharedGUIEventStepLog.Write("Host:SetupAfterOriginalFactory", host); };
            NBFXSharedGUIEventStepLog.Write("Host:BeforeShowUtility", host);
            host.ShowUtility();
            NBFXSharedGUIEventStepLog.Write("Host:AfterShowUtilityBeforeSetupSend", host);
            host.SendEvent(new Event { type = EventType.ExecuteCommand, commandName = host.SetupCommand });
            NBFXSharedGUIEventStepLog.Write("Host:AfterSetupSend", host);
            CheckHost(host); Assert.That(host.Initialized, Is.True, "Real native OnGUI setup must run; no synthetic fallback.");
            item = actualItem;
            host.Draw = () => actualItem.GetType().GetMethod("OnGUI", All).Invoke(actualItem, null);
            return host;
        }
        void Send(NBFXSharedGUIEventHost host, Event evt)
        {
            // Event.type is a native GUI query; outside OnGUI this Editor reports
            // mouseDown as Ignore. Preserve the actual requested native field.
            EventType type = evt.rawType;
            Assert.That(type, Is.Not.EqualTo(EventType.Ignore).And.Not.EqualTo(EventType.Used), "The requested event must be an actual dispatch type.");
            NBFXSharedGUIEventStepLog.Write("Send:RequestedRawType:" + type, host, evt);
            host.Counts.TryGetValue(type, out int count);
            NBFXSharedGUIEventStepLog.Write("Send:BeforeOriginalSendEvent", host, evt);
            host.SendEvent(evt);
            NBFXSharedGUIEventStepLog.Write("Send:AfterOriginalSendEvent", host, evt);
            Assert.That(evt.rawType, Is.EqualTo(type), "The original event request must survive native dispatch; production Event.current may be Used.");
            CheckHost(host);
            Assert.That(host.Counts.TryGetValue(type, out int after) && after > count, Is.True, "Native source Item.OnGUI must draw this event.");
        }
        void Materials(string kind, int first, out Material a, out Material b)
        {
            a = NewMaterial(); b = NewMaterial();
            if (kind == "light")
            {
                a.SetFloat(Mode, first); b.SetFloat(Mode, first == 0 ? 4 : 2); SyncLight(a); SyncLight(b);
            }
            else
            {
                a.SetFloat(Flipbook, first); b.SetFloat(Flipbook, 1 - first); a.SetFloat(VAT, 1 - first); b.SetFloat(VAT, first);
                if (first == 0) a.EnableKeyword("_VAT"); else b.EnableKeyword("_VAT");
            }
            a.SetFloat("_NB_CustomDataFlag2Lo16", 32769); b.SetFloat("_NB_CustomDataFlag3Hi16", 32768);
        }
        [TearDown] public void Cleanup()
        {
            foreach (var host in owned.OfType<NBFXSharedGUIEventHost>()) { host.Draw = null; host.Setup = null; host.Close(); }
            foreach (var item in owned.AsEnumerable().Reverse()) if (item) Object.DestroyImmediate(item); owned.Clear();
        }

        [TestCase("light", 0, EventType.Layout, TestName = "G4SharedGUI_LightMixedFirst0_LayoutReadOnly")]
        [TestCase("light", 0, EventType.Repaint, TestName = "G4SharedGUI_LightMixedFirst0_RepaintReadOnly")]
        [TestCase("light", 4, EventType.Layout, TestName = "G4SharedGUI_LightMixedFirst4_LayoutReadOnly")]
        [TestCase("light", 4, EventType.Repaint, TestName = "G4SharedGUI_LightMixedFirst4_RepaintReadOnly")]
        [TestCase("flipbook", 0, EventType.Layout, TestName = "G4SharedGUI_FlipbookMixedFirstFalse_LayoutReadOnly")]
        [TestCase("flipbook", 0, EventType.Repaint, TestName = "G4SharedGUI_FlipbookMixedFirstFalse_RepaintReadOnly")]
        [TestCase("flipbook", 1, EventType.Layout, TestName = "G4SharedGUI_FlipbookMixedFirstTrue_LayoutReadOnly")]
        [TestCase("flipbook", 1, EventType.Repaint, TestName = "G4SharedGUI_FlipbookMixedFirstTrue_RepaintReadOnly")]
        public void PassiveNativeDrawDoesNotCommit(string kind, int first, EventType eventType)
        {
            Materials(kind, first, out var a, out var b); var root = Root(a, b); var host = Host(root, kind, out var item); AssertMixed(item);
            string beforeA = Snapshot(a), beforeB = Snapshot(b); Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
            string sentinel = "NBFX passive GUI must not register Undo " + Guid.NewGuid().ToString("N"); Undo.SetCurrentGroupName(sentinel);
            try
            {
                // Layout precedes Repaint, as in native IMGUI. Both traverse production Item.OnGUI.
                Send(host, new Event { type = EventType.Layout });
                Send(host, new Event { type = eventType });
                Assert.That(Snapshot(a), Is.EqualTo(beforeA)); Assert.That(Snapshot(b), Is.EqualTo(beforeB));
                Assert.That(Undo.GetCurrentGroupName(), Is.EqualTo(sentinel), "Passive draw must not call RegisterPropertyChangeUndo.");
            }
            finally { host.Draw = null; Undo.RevertAllDownToGroup(group); }
        }

        [TestCase(0, TestName = "G4SharedGUI_LightNativePopupSelectFirst0_UndoRedo")]
        [TestCase(4, TestName = "G4SharedGUI_LightNativePopupSelectFirst4_UndoRedo")]
        public void NativePopupSelectionRunsSameTransaction(int first)
        {
            Materials("light", first, out var a, out var b); NBFXSharedGUIEventStepLog.Materials("Popup:OriginalBaseline", a, b); int beforeB = (int)b.GetFloat(Mode); var root = Root(a, b); var host = Host(root, "light", out var item);
            AssertMixed(item); string beforeA = Snapshot(a), beforeMaterialB = Snapshot(b);
            Send(host, new Event { type = EventType.Layout }); Send(host, new Event { type = EventType.Repaint });
            Assert.That(Snapshot(a), Is.EqualTo(beforeA)); Assert.That(Snapshot(b), Is.EqualTo(beforeMaterialB)); AssertMixed(item);
            var rect = (Rect)item.GetType().GetField("ControlRect", All).GetValue(item);
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
            try
            {
                Send(host, new Event { type = EventType.MouseDown, button = 0, mousePosition = rect.center });
                // Replay the official menu's own callback after real MouseDown created
                // its native PopupCallbackInfo. It delivers PopupMenuChanged back into
                // production Item.OnGUI/DrawController; never calls our Commit helper.
                var popupType = typeof(EditorGUI).GetNestedType("PopupCallbackInfo", BindingFlags.NonPublic); Assert.That(popupType, Is.Not.Null);
                var instanceField = popupType.GetField("instance", All); Assert.That(instanceField, Is.Not.Null);
                var callback = instanceField.GetValue(null); Assert.That(callback, Is.Not.Null, "Real popup MouseDown must create the official callback instance.");
                int beforeCommands = host.Counts.TryGetValue(EventType.ExecuteCommand, out int count) ? count : 0;
                NBFXSharedGUIEventStepLog.Write("Popup:BeforeOriginalCallback", host);
                popupType.GetMethod("SetEnumValueDelegate", All).Invoke(callback, new object[] { null, new[] { "Unlit", "BlinnPhong", "HalfLambert", "PBR", "SixWay" }, first });
                NBFXSharedGUIEventStepLog.Write("Popup:AfterOriginalCallback", host);
                CheckHost(host); Assert.That(host.Counts.TryGetValue(EventType.ExecuteCommand, out int after) && after > beforeCommands, Is.True);
                Assert.That(instanceField.GetValue(null), Is.Null, "The production popup must consume its official callback.");
                Undo.FlushUndoRecordObjects(); Undo.CollapseUndoOperations(group);
                Assert.That(a.GetFloat(Mode), Is.EqualTo(first)); Assert.That(b.GetFloat(Mode), Is.EqualTo(first));
                Assert.That(a.IsKeywordEnabled("EVALUATE_SH_VERTEX"), Is.EqualTo(first == 4)); Assert.That(b.IsKeywordEnabled("EVALUATE_SH_VERTEX"), Is.EqualTo(first == 4));
                Assert.That(a.GetFloat("_NB_CustomDataFlag2Lo16"), Is.EqualTo(32769)); Assert.That(b.GetFloat("_NB_CustomDataFlag3Hi16"), Is.EqualTo(32768));
                NBFXSharedGUIEventStepLog.Materials("Undo:BeforeOriginalUndo", a, b); host.Draw = null; Undo.PerformUndo(); NBFXSharedGUIEventStepLog.Materials("Undo:AfterOriginalUndo", a, b); Assert.That(b.GetFloat(Mode), Is.EqualTo(beforeB)); Assert.That(b.IsKeywordEnabled("EVALUATE_SH_VERTEX"), Is.EqualTo(beforeB == 4));
                NBFXSharedGUIEventStepLog.Materials("Redo:BeforeOriginalRedo", a, b); Undo.PerformRedo(); NBFXSharedGUIEventStepLog.Materials("Redo:AfterOriginalRedo", a, b); Assert.That(b.GetFloat(Mode), Is.EqualTo(first)); Assert.That(b.IsKeywordEnabled("EVALUATE_SH_VERTEX"), Is.EqualTo(first == 4));
            }
            finally { host.Draw = null; host.Close(); Undo.RevertAllDownToGroup(group); }
        }

        [TestCase(0, TestName = "G4SharedGUI_FlipbookNativeClickMixedFirstFalse_UndoRedo_V2DeclaredKeywordBaseline")]
        [TestCase(1, TestName = "G4SharedGUI_FlipbookNativeClickMixedFirstTrue_UndoRedo_V2DeclaredKeywordBaseline")]
        public void NativeToggleClickRunsExistingMutualExclusion(int first)
        {
            Materials("flipbook", first, out var a, out var b); NBFXSharedGUIEventStepLog.Materials("Toggle:OriginalBaseline", a, b);
            var keywordA = a.shader.keywordSpace.FindKeyword("_VAT"); var keywordB = b.shader.keywordSpace.FindKeyword("_VAT");
            bool originalKeywordA = a.IsKeywordEnabled("_VAT"), originalKeywordB = b.IsKeywordEnabled("_VAT");
            if (keywordA.isValid) { Assert.That(originalKeywordA, Is.EqualTo(first == 0), "Declared original VAT A must preserve enable contract."); Assert.That(a.IsKeywordEnabled(keywordA), Is.EqualTo(originalKeywordA)); }
            if (keywordB.isValid) { Assert.That(originalKeywordB, Is.EqualTo(first == 1), "Declared original VAT B must preserve enable contract."); Assert.That(b.IsKeywordEnabled(keywordB), Is.EqualTo(originalKeywordB)); }
            var root = Root(a, b); var host = Host(root, "flipbook", out var item);
            AssertMixed(item); string beforeA = Snapshot(a), beforeB = Snapshot(b);
            Send(host, new Event { type = EventType.Layout }); Send(host, new Event { type = EventType.Repaint });
            Assert.That(Snapshot(a), Is.EqualTo(beforeA)); Assert.That(Snapshot(b), Is.EqualTo(beforeB)); AssertMixed(item);
            var rect = (Rect)item.GetType().GetField("ControlRect", All).GetValue(item); var click = new Vector2(rect.x + 6, rect.center.y);
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
            try
            {
                Send(host, new Event { type = EventType.MouseDown, button = 0, mousePosition = click });
                Send(host, new Event { type = EventType.MouseUp, button = 0, mousePosition = click });
                Undo.FlushUndoRecordObjects(); Undo.CollapseUndoOperations(group);
                Assert.That(a.GetFloat(Flipbook), Is.EqualTo(1)); Assert.That(b.GetFloat(Flipbook), Is.EqualTo(1));
                Assert.That(a.GetFloat(VAT) + b.GetFloat(VAT), Is.Zero); Assert.That(a.IsKeywordEnabled("_VAT") || b.IsKeywordEnabled("_VAT"), Is.False);
                Assert.That(a.GetFloat("_NB_CustomDataFlag2Lo16"), Is.EqualTo(32769)); Assert.That(b.GetFloat("_NB_CustomDataFlag3Hi16"), Is.EqualTo(32768));
                NBFXSharedGUIEventStepLog.Materials("Undo:BeforeOriginalUndo", a, b); host.Draw = null; Undo.PerformUndo(); NBFXSharedGUIEventStepLog.Materials("Undo:AfterOriginalUndo", a, b); Assert.That(a.GetFloat(Flipbook), Is.EqualTo(first)); Assert.That(b.GetFloat(Flipbook), Is.EqualTo(1 - first));
                Assert.That(a.GetFloat(VAT), Is.EqualTo(1 - first)); Assert.That(b.GetFloat(VAT), Is.EqualTo(first));
                Assert.That(a.IsKeywordEnabled("_VAT"), Is.EqualTo(originalKeywordA), "Undo must restore actual pre-edit A keyword baseline.");
                Assert.That(b.IsKeywordEnabled("_VAT"), Is.EqualTo(originalKeywordB), "Undo must restore actual pre-edit B keyword baseline.");
                if (keywordA.isValid) Assert.That(a.IsKeywordEnabled(keywordA), Is.EqualTo(originalKeywordA));
                if (keywordB.isValid) Assert.That(b.IsKeywordEnabled(keywordB), Is.EqualTo(originalKeywordB));
                Assert.That(a.GetFloat("_NB_CustomDataFlag2Lo16"), Is.EqualTo(32769)); Assert.That(b.GetFloat("_NB_CustomDataFlag3Hi16"), Is.EqualTo(32768));
                NBFXSharedGUIEventStepLog.Materials("Redo:BeforeOriginalRedo", a, b); Undo.PerformRedo(); NBFXSharedGUIEventStepLog.Materials("Redo:AfterOriginalRedo", a, b); Assert.That(a.GetFloat(VAT) + b.GetFloat(VAT), Is.Zero); Assert.That(a.GetFloat(Flipbook) + b.GetFloat(Flipbook), Is.EqualTo(2));
                Assert.That(a.GetFloat("_NB_CustomDataFlag2Lo16"), Is.EqualTo(32769)); Assert.That(b.GetFloat("_NB_CustomDataFlag3Hi16"), Is.EqualTo(32768));
                Assert.That(a.IsKeywordEnabled("_VAT") || b.IsKeywordEnabled("_VAT"), Is.False);
                if (keywordA.isValid) Assert.That(a.IsKeywordEnabled(keywordA), Is.False);
                if (keywordB.isValid) Assert.That(b.IsKeywordEnabled(keywordB), Is.False);
            }
            finally { host.Draw = null; Undo.RevertAllDownToGroup(group); }
        }
    }
}
