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
    public sealed class NBFXMainTexGUIEventHost : EditorWindow
    {
        internal Action Setup, Draw;
        internal Exception Failure;
        internal bool Initialized;
        internal string SetupCommand;
        internal readonly Dictionary<EventType, int> Counts = new Dictionary<EventType, int>();
        void OnGUI()
        {
            EventType original = Event.current.type;
            try
            {
                if (original == EventType.ExecuteCommand && Event.current.commandName == SetupCommand)
                {
                    var initialize = typeof(EditorStyles).GetMethod("UpdateSkinCache", BindingFlags.Static | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                    Assert.That(initialize, Is.Not.Null); initialize.Invoke(null, null);
                    Setup(); Initialized = true; Event.current.Use(); return;
                }
                if (!Initialized || Draw == null) return;
                Draw(); Counts.TryGetValue(original, out int count); Counts[original] = count + 1;
            }
            catch (Exception exception)
            {
                Failure = exception is TargetInvocationException invocation && invocation.InnerException != null ? invocation.InnerException : exception;
            }
        }
    }

    public sealed class G4GraphSharedMainTexUVGUIEventTests
    {
        const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        const string Graph = "Packages/com.xuanxuan.nb.fx/NBShaders2/ShaderGraph/NBShaderGraph.shadergraph";
        readonly List<Object> owned = new List<Object>();
        readonly List<string> assets = new List<string>();
        Type FindType(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name, false)).First(t => t != null);
        static void Guard()
        {
            Assert.That(Path.GetFullPath(Application.dataPath), Is.EqualTo(Path.GetFullPath(@"D:\UnityProject\NBUnityProject\.utmp\NBFXMeshValidation-20261002\Assets")).IgnoreCase);
            for (int index = 0; index < SceneManager.sceneCount; ++index)
            {
                var scene = SceneManager.GetSceneAt(index);
                Assert.That((scene.name + "/" + scene.path).IndexOf("TAI", StringComparison.OrdinalIgnoreCase), Is.LessThan(0));
            }
        }
        [OneTimeSetUp] public void Preflight()
        {
            Guard(); AssetDatabase.ImportAsset(Graph, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        }
        Material NewMaterial()
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(Graph); Assert.That(shader && shader.isSupported, Is.True);
            var material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave }; owned.Add(material);
            material.SetFloat("_NB_GraphGUIStateVersion", 2);
            material.SetFloat("_NB_UVModeFlag0Hi16", 0.125f);
            material.SetFloat("_NB_UVModeFlagType0Hi16", 0.25f);
            material.SetFloat("_NB_CustomDataFlag0Hi16", 48123.25f);
            material.SetFloat("_NB_Flags1Lo16", 4096.125f);
            material.SetFloat("_NB_Flags1Hi16", 2.125f);
            return material;
        }
        void SetUV(Material material, int mode)
        {
            // Test input preparation only. Test actions below enter the original item OnGUI.
            material.SetFloat("_NB_UVModeFlag0Lo16", mode & 3);
            material.SetFloat("_NB_UVModeFlagType0Lo16", mode / 4);
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
                var info = Activator.CreateInstance(FindType("NBShaderEditor.ShaderPropertyInfo"));
                info.GetType().GetField("Property").SetValue(info, property); info.GetType().GetField("Name").SetValue(info, property.name);
                info.GetType().GetField("Index").SetValue(info, materials[0].shader.FindPropertyIndex(property.name)); dictionary.Add(property.name, info);
            }
            return root;
        }
        object Item(object root, string kind)
        {
            var type = root.GetType(); Assert.That((bool)type.GetMethod("InitializeGraphMainTextureInputs", All).Invoke(root, null), Is.True);
            var main = type.GetField("_mainTexBlock", All).GetValue(root);
            if (kind == "main") return main;
            string field = kind == "uv" ? "_uvModeItem" : kind == "x" ? "_offsetXCustomDataItem" : "_offsetYCustomDataItem";
            if (kind == "special" || kind == "cylinder" || kind == "twirl" || kind == "polar")
            {
                var uv = main.GetType().GetField("_uvModeItem", All).GetValue(main); Assert.That(uv, Is.Not.Null);
                string child = kind == "special" ? "_specialUVChannelItem" : kind == "cylinder" ? "_cylinderRotateItem" : kind == "twirl" ? "_twirlBlock" : "_polarBlock";
                return uv.GetType().GetField(child, All).GetValue(uv);
            }
            var item = main.GetType().GetField(field, All).GetValue(main); Assert.That(item, Is.Not.Null, "Real factory must construct supported item."); return item;
        }
        void Check(NBFXMainTexGUIEventHost host) { if (host.Failure != null) ExceptionDispatchInfo.Capture(host.Failure).Throw(); }
        NBFXMainTexGUIEventHost Host(object root, string kind, out object item)
        {
            var host = ScriptableObject.CreateInstance<NBFXMainTexGUIEventHost>(); owned.Add(host);
            host.hideFlags = HideFlags.HideAndDontSave; host.position = new Rect(20, 20, 600, 480);
            host.titleContent = new GUIContent("NBFX MainTex actual event test"); host.SetupCommand = "NBFX_MainTex_Setup_" + Guid.NewGuid().ToString("N");
            object actual = null; host.Setup = () => actual = Item(root, kind);
            host.ShowUtility(); host.SendEvent(new Event { type = EventType.ExecuteCommand, commandName = host.SetupCommand });
            Check(host); Assert.That(host.Initialized, Is.True, "Require native GUIView delivery; no fake fallback.");
            item = actual; host.Draw = () => actual.GetType().GetMethod("OnGUI", All).Invoke(actual, null); return host;
        }
        void Send(NBFXMainTexGUIEventHost host, Event evt)
        {
            // Event.type can query Ignore outside the native OnGUI context.
            // Keep the requested native field for both dispatch and receipt counting.
            EventType type = evt.rawType;
            Assert.That(type, Is.Not.EqualTo(EventType.Ignore).And.Not.EqualTo(EventType.Used), "The requested event must be an actual dispatch type.");
            host.Counts.TryGetValue(type, out int before);
            Debug.Log("NBFX_MAIN_TEX_SEND_PRE case=" + TestContext.CurrentContext.Test.FullName + " requestedRawType=" + type + " queryType=" + evt.type + " count=" + before);
            host.SendEvent(evt);
            Debug.Log("NBFX_MAIN_TEX_SEND_POST case=" + TestContext.CurrentContext.Test.FullName + " requestedRawType=" + type + " rawType=" + evt.rawType + " queryType=" + evt.type);
            Assert.That(evt.rawType, Is.EqualTo(type), "The original event request must survive native dispatch; production Event.current may be Used.");
            Check(host);
            Assert.That(host.Counts.TryGetValue(type, out int after) && after > before, Is.True, "Production OnGUI must receive the actual event.");
        }
        string Snapshot(Material material) => EditorJsonUtility.ToJson(material) + "\n" + string.Join("|", material.shaderKeywords.OrderBy(k => k));
        void Popup(NBFXMainTexGUIEventHost host, object item, int selection, string[] options)
        {
            Send(host, new Event { type = EventType.Layout }); Send(host, new Event { type = EventType.Repaint });
            var rect = (Rect)item.GetType().GetField("ControlRect", All).GetValue(item);
            Send(host, new Event { type = EventType.MouseDown, button = 0, mousePosition = rect.center });
            var callbackType = typeof(EditorGUI).GetNestedType("PopupCallbackInfo", BindingFlags.NonPublic); Assert.That(callbackType, Is.Not.Null);
            var field = callbackType.GetField("instance", All); Assert.That(field, Is.Not.Null); var callback = field.GetValue(null); Assert.That(callback, Is.Not.Null);
            int before = host.Counts.TryGetValue(EventType.ExecuteCommand, out int count) ? count : 0;
            callbackType.GetMethod("SetEnumValueDelegate", All).Invoke(callback, new object[] { null, options, selection });
            Check(host); Assert.That(host.Counts.TryGetValue(EventType.ExecuteCommand, out int after) && after > before, Is.True);
            Assert.That(field.GetValue(null), Is.Null, "Original production Popup must consume official callback, not direct Commit.");
        }
        [TearDown] public void Cleanup()
        {
            foreach (var host in owned.OfType<NBFXMainTexGUIEventHost>()) { host.Draw = null; host.Setup = null; host.Close(); }
            foreach (string path in assets) { Assert.That(path.StartsWith("Assets/ResTemp/EditorTemp/NBFX_MainTex_", StringComparison.Ordinal), Is.True); AssetDatabase.DeleteAsset(path); }
            assets.Clear(); foreach (var item in owned.AsEnumerable().Reverse()) if (item) Object.DestroyImmediate(item); owned.Clear();
        }

        [TestCase("uv", EventType.Layout, TestName = "G4MainTexUV_MixedPreparedV2_LayoutReadOnly")]
        [TestCase("uv", EventType.Repaint, TestName = "G4MainTexUV_MixedPreparedV2_RepaintReadOnly")]
        [TestCase("x", EventType.Layout, TestName = "G4MainTexCDX_MixedPreparedV2_LayoutReadOnly")]
        [TestCase("x", EventType.Repaint, TestName = "G4MainTexCDX_MixedPreparedV2_RepaintReadOnly")]
        [TestCase("y", EventType.Layout, TestName = "G4MainTexCDY_MixedPreparedV2_LayoutReadOnly")]
        [TestCase("y", EventType.Repaint, TestName = "G4MainTexCDY_MixedPreparedV2_RepaintReadOnly")]
        [TestCase("special", EventType.Layout, TestName = "G4MainTexSpecial_NoneRaw_LayoutReadOnly")]
        [TestCase("special", EventType.Repaint, TestName = "G4MainTexSpecial_NoneRaw_RepaintReadOnly")]
        [TestCase("cylinder", EventType.Layout, TestName = "G4MainTexCylinder_Prepared_LayoutReadOnly")]
        [TestCase("cylinder", EventType.Repaint, TestName = "G4MainTexCylinder_Prepared_RepaintReadOnly")]
        public void Passive(string kind, EventType type)
        {
            var a = NewMaterial(); var b = NewMaterial(); SetUV(a, 0); SetUV(b, 6); a.SetFloat("_NB_CustomDataFlag0Lo16", 0); b.SetFloat("_NB_CustomDataFlag0Lo16", 255);
            var host = Host(Root(a, b), kind, out var item); string beforeA = Snapshot(a), beforeB = Snapshot(b);
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup(); string sentinel = "NBFX passive MainTex " + Guid.NewGuid(); Undo.SetCurrentGroupName(sentinel);
            try { Send(host, new Event { type = EventType.Layout }); Send(host, new Event { type = type }); Assert.That(Snapshot(a), Is.EqualTo(beforeA)); Assert.That(Snapshot(b), Is.EqualTo(beforeB)); Assert.That(Undo.GetCurrentGroupName(), Is.EqualTo(sentinel)); }
            finally { host.Draw = null; Undo.RevertAllDownToGroup(group); }
        }

        [TestCase("uv", TestName = "G4MainTexUV_ActualPopupSameFirst_UndoRedo")]
        [TestCase("x", TestName = "G4MainTexCDX_ActualPopupSameFirst_UndoRedo")]
        [TestCase("y", TestName = "G4MainTexCDY_ActualPopupSameFirst_UndoRedo")]
        public void SameFirst(string kind)
        {
            var a = NewMaterial(); var b = NewMaterial(); SetUV(a, 0); SetUV(b, 6); a.SetFloat("_NB_CustomDataFlag0Lo16", 0); b.SetFloat("_NB_CustomDataFlag0Lo16", 255);
            var host = Host(Root(a, b), kind, out var item); string beforeA = Snapshot(a), beforeB = Snapshot(b);
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
            try
            {
                string[] options = kind == "uv" ? new[] { "默认UV通道", "特殊UV通道", "极坐标|旋转", "圆柱无缝", "主贴图", "屏幕UV", "世界坐标", "局部本地坐标", "公共UV" }
                    : new[] { "关闭", "CustomData1.x", "CustomData1.y", "CustomData1.z", "CustomData1.w", "CustomData2.x", "CustomData2.y", "CustomData2.z", "CustomData2.w" };
                Popup(host, item, 0, options); Undo.FlushUndoRecordObjects(); Undo.CollapseUndoOperations(group);
                if (kind == "uv") { Assert.That(b.GetFloat("_NB_UVModeFlag0Lo16"), Is.Zero); Assert.That(b.GetFloat("_NB_UVModeFlagType0Lo16"), Is.Zero); }
                else Assert.That(((int)b.GetFloat("_NB_CustomDataFlag0Lo16") & (kind == "x" ? 15 : 240)), Is.Zero);
                Assert.That(a.GetFloat("_NB_CustomDataFlag0Hi16"), Is.EqualTo(48123.25f)); Assert.That(b.GetFloat("_NB_CustomDataFlag0Hi16"), Is.EqualTo(48123.25f));
                Assert.That(a.GetFloat("_NB_UVModeFlag0Hi16"), Is.EqualTo(.125f)); Assert.That(b.GetFloat("_NB_UVModeFlagType0Hi16"), Is.EqualTo(.25f));
                string afterA = Snapshot(a), afterB = Snapshot(b); host.Draw = null;
                Undo.PerformUndo(); Assert.That(Snapshot(a), Is.EqualTo(beforeA)); Assert.That(Snapshot(b), Is.EqualTo(beforeB));
                Undo.PerformRedo(); Assert.That(Snapshot(a), Is.EqualTo(afterA)); Assert.That(Snapshot(b), Is.EqualTo(afterB));
            }
            finally { host.Draw = null; Undo.RevertAllDownToGroup(group); }
        }

        [TestCase(0, TestName = "G4MainTexUV_OriginalResetButton_Default_UndoRedo")]
        public void ResetButton(int unused)
        {
            var a = NewMaterial(); var b = NewMaterial(); SetUV(a, 6); SetUV(b, 7);
            var host = Host(Root(a, b), "uv", out var item); string beforeA = Snapshot(a), beforeB = Snapshot(b);
            Send(host, new Event { type = EventType.Layout }); Send(host, new Event { type = EventType.Repaint });
            var rect = (Rect)item.GetType().GetField("ResetRect", All).GetValue(item); Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
            try
            {
                Send(host, new Event { type = EventType.MouseDown, button = 0, mousePosition = rect.center }); Send(host, new Event { type = EventType.MouseUp, button = 0, mousePosition = rect.center });
                Undo.FlushUndoRecordObjects(); Undo.CollapseUndoOperations(group);
                foreach (var material in new[] { a, b })
                {
                    Assert.That(material.GetFloat("_NB_UVModeFlag0Lo16"), Is.Zero);
                    Assert.That(material.GetFloat("_NB_UVModeFlagType0Lo16"), Is.Zero);
                }
                Assert.That(a.GetFloat("_NB_UVModeFlag0Hi16"), Is.EqualTo(.125f)); host.Draw = null;
                Undo.PerformUndo(); Assert.That(Snapshot(a), Is.EqualTo(beforeA)); Assert.That(Snapshot(b), Is.EqualTo(beforeB)); Undo.PerformRedo();
            }
            finally { host.Draw = null; Undo.RevertAllDownToGroup(group); }
        }


        [TestCase(0, TestName = "G4MainTexWholeBlockReset_TwirlPolarNativePreserved_UndoRedo")]
        public void WholeMainResetKeepsOriginalUVDescendants(int unused)
        {
            var a = NewMaterial(); var b = NewMaterial(); SetUV(a, 2); SetUV(b, 2);
            a.SetFloat("_NB_UVModeFlag0Lo16", 0x5552); b.SetFloat("_NB_UVModeFlag0Lo16", 0x4442);
            a.SetFloat("_NB_UVModeFlagType0Lo16", 0x5550); b.SetFloat("_NB_UVModeFlagType0Lo16", 0x4440);
            a.SetFloat("_NB_CustomDataFlag0Lo16", 0x7788); b.SetFloat("_NB_CustomDataFlag0Lo16", 0x8899);
            a.SetFloat("_NB_WrapFlagsLo16", 0x1235); b.SetFloat("_NB_WrapFlagsLo16", 0x2345);
            a.SetFloat("_NB_WrapFlagsHi16", 0x4567); b.SetFloat("_NB_WrapFlagsHi16", 0x5679);
            a.SetFloat("_NB_ColorChannelLo16", 0x3457); b.SetFloat("_NB_ColorChannelLo16", 0x4567);
            a.SetFloat("_NB_ForceNoMipFlagsLo16", 0x6789); b.SetFloat("_NB_ForceNoMipFlagsLo16", 0x789b);
            const int transforms = (1 << 8) | (1 << 9);
            a.SetFloat("_NB_Flags0Lo16", (0x9485 | transforms) + .125f);
            b.SetFloat("_NB_Flags0Lo16", (0x40a3 | transforms) + .25f);
            a.SetFloat("_NB_Flags0Hi16", 4321.75f); b.SetFloat("_NB_Flags0Hi16", 5321.5f);
            foreach (var material in new[] { a, b })
            {
                material.SetFloat("_UTwirlEnabled", 1); material.SetFloat("_PolarCoordinatesEnabled", 1);
                material.SetVector("_TWParameter", new Vector4(.25f, .5f, .75f, 1)); material.SetFloat("_TWStrength", .625f);
                material.SetVector("_PCCenter", new Vector4(.125f, .25f, .5f, .75f));
            }
            string[] preserved = { "_NB_Flags0Lo16", "_NB_Flags0Hi16", "_NB_Flags1Lo16", "_NB_Flags1Hi16", "_NB_UVModeFlag0Hi16", "_NB_UVModeFlagType0Hi16", "_NB_CustomDataFlag0Hi16" };
            var beforeRaw = new[] { a, b }.Select(material => preserved.ToDictionary(name => name, name => material.GetFloat(name))).ToArray();
            string[] masked = { "_NB_UVModeFlag0Lo16", "_NB_UVModeFlagType0Lo16", "_NB_CustomDataFlag0Lo16", "_NB_WrapFlagsLo16", "_NB_WrapFlagsHi16", "_NB_ColorChannelLo16", "_NB_ForceNoMipFlagsLo16" };
            int[] owned = { 3, 3, 255, 1, 1, 3, 1 };
            var beforeMasked = new[] { a, b }.Select(material => masked.Select(name => Mathf.RoundToInt(material.GetFloat(name))).ToArray()).ToArray();
            var host = Host(Root(a, b), "main", out var item); string beforeA = Snapshot(a), beforeB = Snapshot(b);
            Send(host, new Event { type = EventType.Layout }); Send(host, new Event { type = EventType.Repaint });
            var rect = (Rect)item.GetType().GetField("ResetRect", All).GetValue(item); Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
            try
            {
                Send(host, new Event { type = EventType.MouseDown, button = 0, mousePosition = rect.center });
                Send(host, new Event { type = EventType.MouseUp, button = 0, mousePosition = rect.center });
                Undo.FlushUndoRecordObjects(); Undo.CollapseUndoOperations(group);
                int index = 0;
                foreach (var material in new[] { a, b })
                {
                    Assert.That(Mathf.RoundToInt(material.GetFloat("_NB_UVModeFlag0Lo16")) & 3, Is.Zero, "The actual Main Reset button must reset Main UV source.");
                    Assert.That(Mathf.RoundToInt(material.GetFloat("_NB_UVModeFlagType0Lo16")) & 3, Is.Zero);
                    Assert.That(material.GetFloat("_UTwirlEnabled"), Is.EqualTo(1), "Original UV reset override does not recurse Twirl.");
                    Assert.That(material.GetFloat("_PolarCoordinatesEnabled"), Is.EqualTo(1), "Original UV reset override does not recurse Polar.");
                    Assert.That((Mathf.RoundToInt(material.GetFloat("_NB_Flags0Lo16")) & transforms), Is.EqualTo(transforms));
                    foreach (string name in preserved) Assert.That(material.GetFloat(name), Is.EqualTo(beforeRaw[index][name]), "Untouched raw field " + name);
                    for (int part = 0; part < masked.Length; ++part)
                        Assert.That(Mathf.RoundToInt(material.GetFloat(masked[part])) & ~owned[part], Is.EqualTo(beforeMasked[index][part] & ~owned[part]), "Every unowned bit " + masked[part]);
                    Assert.That(material.GetVector("_TWParameter"), Is.EqualTo(new Vector4(.25f, .5f, .75f, 1)));
                    Assert.That(material.GetFloat("_TWStrength"), Is.EqualTo(.625f));
                    Assert.That(material.GetVector("_PCCenter"), Is.EqualTo(new Vector4(.125f, .25f, .5f, .75f)));
                    ++index;
                }
                string afterA = Snapshot(a), afterB = Snapshot(b); Assert.That(afterA, Is.Not.EqualTo(beforeA)); Assert.That(afterB, Is.Not.EqualTo(beforeB)); host.Draw = null;
                Undo.PerformUndo(); Assert.That(Snapshot(a), Is.EqualTo(beforeA)); Assert.That(Snapshot(b), Is.EqualTo(beforeB));
                Undo.PerformRedo(); Assert.That(Snapshot(a), Is.EqualTo(afterA)); Assert.That(Snapshot(b), Is.EqualTo(afterB));
            }
            finally { host.Draw = null; Undo.RevertAllDownToGroup(group); }
        }

        [TestCase(0, TestName = "G4MainTexUV_SaveReimport_NewRootRawReadback")]
        public void SaveReimport(int unused)
        {
            var a = NewMaterial(); var b = NewMaterial(); SetUV(a, 0); SetUV(b, 6); var host = Host(Root(a, b), "uv", out var item);
            Popup(host, item, 7, new[] { "Default", "Special", "Polar", "Cylinder", "Main", "Screen", "World", "Object", "Shared" }); host.Draw = null;
            foreach (string folder in new[] { "Assets/ResTemp", "Assets/ResTemp/EditorTemp" })
                if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(Path.GetDirectoryName(folder).Replace('\\', '/'), Path.GetFileName(folder));
            string path = "Assets/ResTemp/EditorTemp/NBFX_MainTex_" + Guid.NewGuid().ToString("N") + ".mat";
            var saved = new Material(b) { hideFlags = HideFlags.None }; AssetDatabase.CreateAsset(saved, path); assets.Add(path);
            EditorUtility.SetDirty(saved); AssetDatabase.SaveAssets(); AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            var loaded = AssetDatabase.LoadAssetAtPath<Material>(path); Assert.That(loaded.GetFloat("_NB_UVModeFlag0Lo16"), Is.EqualTo(3)); Assert.That(loaded.GetFloat("_NB_UVModeFlagType0Lo16"), Is.EqualTo(1));
            Assert.That(loaded.GetFloat("_NB_UVModeFlag0Hi16"), Is.EqualTo(.125f)); Assert.That(loaded.GetFloat("_NB_CustomDataFlag0Hi16"), Is.EqualTo(48123.25f));
            var newHost = Host(Root(loaded), "uv", out var newItem); string before = Snapshot(loaded); Send(newHost, new Event { type = EventType.Layout }); Assert.That(Snapshot(loaded), Is.EqualTo(before));
        }

        [TestCase(0, TestName = "G4MainTexSpecialUV_ActualPopupSameFirst_RawUndo")]
        public void SpecialSameFirst(int unused)
        {
            var a = NewMaterial(); var b = NewMaterial(); SetUV(a, 1); SetUV(b, 1);
            a.SetFloat("_NB_Flags1Hi16", 6.125f); b.SetFloat("_NB_Flags1Hi16", 10.125f);
            var host = Host(Root(a, b), "special", out var item); string beforeA = Snapshot(a), beforeB = Snapshot(b);
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
            try
            {
                Popup(host, item, 0, new[] { "UV2_Texcoord1", "UV3_Texcoord2" }); Undo.FlushUndoRecordObjects(); Undo.CollapseUndoOperations(group);
                Assert.That(a.GetFloat("_NB_Flags1Hi16"), Is.EqualTo(6.125f), "Decoded no-op preserves finite noncanonical owned half.");
                Assert.That(((int)b.GetFloat("_NB_Flags1Hi16") & 12), Is.EqualTo(4)); Assert.That(b.GetFloat("_NB_Flags1Lo16"), Is.EqualTo(4096.125f));
                Assert.That(a.HasProperty("_SpecialUVChannelMode"), Is.False, "No Graph shadow scalar."); host.Draw = null;
                Undo.PerformUndo(); Assert.That(Snapshot(a), Is.EqualTo(beforeA)); Assert.That(Snapshot(b), Is.EqualTo(beforeB)); Undo.PerformRedo();
            }
            finally { host.Draw = null; Undo.RevertAllDownToGroup(group); }
        }

        [TestCase(true, TestName = "G4MainTexUV_CylinderDerived_All16FieldsKeep")]
        [TestCase(false, TestName = "G4MainTexUV_CylinderDerived_All16FieldsClear")]
        public void CylinderAggregate(bool otherCylinder)
        {
            var a = NewMaterial(); var b = NewMaterial(); SetUV(a, 6); SetUV(b, 7);
            foreach (var material in new[] { a, b })
            {
                material.SetFloat("_NB_UVModeFlag0Hi16", 49152.25f); // Logical slot15 mode-bits=3.
                material.SetFloat("_NB_UVModeFlagType0Hi16", otherCylinder ? .25f : 16384.25f); // Type0 =>Cylinder, type1 =>Object.
                material.SetFloat("_NB_Flags1Hi16", otherCylinder ? 2.125f : 18.125f);
            }
            var host = Host(Root(a, b), "uv", out var item);
            Popup(host, item, 0, new[] { "Default", "Special", "Polar", "Cylinder", "Main", "Screen", "World", "Object", "Shared" });
            foreach (var material in new[] { a, b })
            {
                Assert.That(((int)material.GetFloat("_NB_Flags1Hi16") & 16) != 0, Is.EqualTo(otherCylinder));
                Assert.That(material.GetFloat("_NB_UVModeFlag0Hi16"), Is.EqualTo(49152.25f));
                Assert.That(material.GetFloat("_NB_UVModeFlagType0Hi16"), Is.EqualTo(otherCylinder ? .25f : 16384.25f));
                Assert.That(material.GetFloat("_NB_Flags1Lo16"), Is.EqualTo(4096.125f));
            }
        }

        [TestCase(0, TestName = "G4MainTexCylinder_ActualComponentText_PerTargetMatrixUndo")]
        public void CylinderPerTarget(int unused)
        {
            var a = NewMaterial(); var b = NewMaterial(); SetUV(a, 3); SetUV(b, 3);
            a.SetVector("_CylinderUVRotate", new Vector4(1, 2, 3, 7)); b.SetVector("_CylinderUVRotate", new Vector4(4, 5, 6, 9));
            a.SetVector("_CylinderUVPosOffset", new Vector4(7, 8, 9, 10)); b.SetVector("_CylinderUVPosOffset", new Vector4(11, 12, 13, 14));
            var host = Host(Root(a, b), "cylinder", out var item); string beforeA = Snapshot(a), beforeB = Snapshot(b);
            Send(host, new Event { type = EventType.Layout }); Send(host, new Event { type = EventType.Repaint });
            var rect = (Rect)item.GetType().GetField("ControlRect", All).GetValue(item); rect.width = (rect.width - 8f) / 3f;
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
            try
            {
                Send(host, new Event { type = EventType.MouseDown, button = 0, clickCount = 2, mousePosition = rect.center });
                Send(host, new Event { type = EventType.MouseUp, button = 0, mousePosition = rect.center });
                Send(host, new Event { type = EventType.KeyDown, keyCode = KeyCode.A, control = true });
                Send(host, new Event { type = EventType.KeyDown, character = '2' });
                Send(host, new Event { type = EventType.KeyDown, keyCode = KeyCode.Return, character = '\n' });
                Undo.FlushUndoRecordObjects(); Undo.CollapseUndoOperations(group);
                Assert.That(a.GetVector("_CylinderUVRotate"), Is.EqualTo(new Vector4(2, 2, 3, 7)));
                Assert.That(b.GetVector("_CylinderUVRotate"), Is.EqualTo(new Vector4(2, 5, 6, 9)), "No first-target Y/Z/W collapse.");
                foreach (var material in new[] { a, b })
                {
                    Vector4 rotation = material.GetVector("_CylinderUVRotate"), offset = material.GetVector("_CylinderUVPosOffset");
                    var matrix = Matrix4x4.Translate(new Vector3(offset.x, offset.y, offset.z)) * Matrix4x4.Rotate(Quaternion.Euler(rotation.x, rotation.y, rotation.z));
                    for (int row = 0; row < 4; ++row) Assert.That(material.GetVector("_CylinderMatrix" + row), Is.EqualTo(matrix.GetRow(row)));
                }
                host.Draw = null; Undo.PerformUndo(); Assert.That(Snapshot(a), Is.EqualTo(beforeA)); Assert.That(Snapshot(b), Is.EqualTo(beforeB)); Undo.PerformRedo();
            }
            finally { host.Draw = null; Undo.RevertAllDownToGroup(group); }
        }

        [TestCase("twirl", TestName = "G4MainTexTwirl_ActualToggle_MirrorMaskUndo")]
        [TestCase("polar", TestName = "G4MainTexPolar_ActualToggle_MirrorMaskUndo")]
        public void UVToggle(string kind)
        {
            var a = NewMaterial(); var b = NewMaterial(); SetUV(a, 2); SetUV(b, 2);
            string property = kind == "twirl" ? "_UTwirlEnabled" : "_PolarCoordinatesEnabled";
            int bit = kind == "twirl" ? 512 : 256;
            a.SetFloat(property, 0); b.SetFloat(property, 1); a.SetFloat("_NB_Flags0Lo16", 0); b.SetFloat("_NB_Flags0Lo16", bit);
            a.SetFloat("_NB_Flags0Hi16", 35001.25f); b.SetFloat("_NB_Flags0Hi16", 35001.25f);
            var host = Host(Root(a, b), kind, out var item); string beforeA = Snapshot(a), beforeB = Snapshot(b);
            Send(host, new Event { type = EventType.Layout }); Send(host, new Event { type = EventType.Repaint });
            var rect = (Rect)item.GetType().GetField("ControlRect", All).GetValue(item); var click = new Vector2(rect.x + 6, rect.center.y);
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
            try
            {
                Send(host, new Event { type = EventType.MouseDown, button = 0, mousePosition = click }); Send(host, new Event { type = EventType.MouseUp, button = 0, mousePosition = click });
                Undo.FlushUndoRecordObjects(); Undo.CollapseUndoOperations(group);
                foreach (var material in new[] { a, b })
                {
                    Assert.That(material.GetFloat(property), Is.EqualTo(1)); Assert.That(((int)material.GetFloat("_NB_Flags0Lo16") & bit), Is.EqualTo(bit));
                    Assert.That(material.GetFloat("_NB_Flags0Hi16"), Is.EqualTo(35001.25f));
                }
                host.Draw = null; Undo.PerformUndo(); Assert.That(Snapshot(a), Is.EqualTo(beforeA)); Assert.That(Snapshot(b), Is.EqualTo(beforeB)); Undo.PerformRedo();
            }
            finally { host.Draw = null; Undo.RevertAllDownToGroup(group); }
        }
    }
}
