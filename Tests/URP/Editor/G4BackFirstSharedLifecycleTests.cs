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
    // Shared original Toggle and migrated modern back-pass lifecycle only.
    // No implicit migration, target conversion, render-state matrix or GPU claim.
    public sealed class G4BackFirstSharedLifecycleTests
    {
        const BindingFlags All = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        const string Package = "Packages/com.xuanxuan.nb.fx/";
        readonly List<Object> owned = new List<Object>();
        readonly List<string> assets = new List<string>();
        static Type Find(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name, false)).First(t => t != null);
        static object Call(object target, string method, params object[] args)
        {
            var type = target is Type t ? t : target.GetType();
            var member = type.GetMethod(method, All); Assert.That(member, Is.Not.Null, type + "." + method);
            try { return member.Invoke(target is Type ? null : target, args); }
            catch (TargetInvocationException e) { ExceptionDispatchInfo.Capture(e.InnerException ?? e).Throw(); throw; }
        }
        static object Value(object target, string name)
        {
            var field = target.GetType().GetField(name, All);
            return field != null ? field.GetValue(target) : target.GetType().GetProperty(name, All).GetValue(target);
        }
        static Type Sync => Find("NBShaderEditor.NBShaderSyncService");
        static object Tier => Enum.ToObject(Find("NBShader.NBShaderFeatureTier"), 3);
        static string[] Raw => (string[])Find("NBShader.NBShaderFeatureCatalog").GetField("RawKeywords", All).GetValue(null);
        static string Snapshot(Material material) => EditorJsonUtility.ToJson(material) + "|" + string.Join("|", material.shaderKeywords.OrderBy(k => k));

        Material New(bool modern = true, bool migrated = true, bool main = true)
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(Package + "Tests/URP/Graphs/NBBackFirst" + (modern ? "Modern" : "Legacy") + ".shadergraph");
            Assert.That(shader && shader.isSupported, Is.True);
            var material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave }; owned.Add(material);
            material.SetFloat("_NBShaderFeatureTier", 3); material.SetFloat("_Surface", 1); material.SetFloat("_AlphaClip", 0);
            Assert.That(Call(Sync, "TryInitializeGraphSupportedGateTierOnAssign", material), Is.True);
            if (modern && migrated) Assert.That(Call(Sync, "TryMigrateGraphColorPassState", material, main), Is.True);
            return material;
        }
        object Root(params Material[] materials)
        {
            var root = Activator.CreateInstance(Find("NBShaderEditor.NBShaderRootItem")); var type = root.GetType();
            var editor = (MaterialEditor)Editor.CreateEditor(materials.Cast<Object>().ToArray(), typeof(MaterialEditor)); owned.Add(editor);
            type.GetField("MatEditor", All).SetValue(root, editor); type.GetField("Mats", All).SetValue(root, materials.ToList());
            type.GetField("Shader", All).SetValue(root, materials[0].shader); Call(root, "InitFlags", materials.ToList());
            var dictionary = (IDictionary)Value(root, "PropertyInfoDic");
            // Keep real multi-target Editor/Mats. Cross-shader negative inputs
            // collect only actual first-shader properties; route guards reject
            // the other real shader before any unavailable property is edited.
            Object[] propertyTargets = materials.All(m => m.shader == materials[0].shader)
                ? materials.Cast<Object>().ToArray() : new Object[] { materials[0] };
            foreach (var p in MaterialEditor.GetMaterialProperties(propertyTargets))
            {
                var info = Activator.CreateInstance(Find("NBShaderEditor.ShaderPropertyInfo"));
                info.GetType().GetField("Property", All).SetValue(info, p); info.GetType().GetField("Name", All).SetValue(info, p.name);
                info.GetType().GetField("Index", All).SetValue(info, materials[0].shader.FindPropertyIndex(p.name)); dictionary.Add(p.name, info);
            }
            type.GetProperty("Context", All).SetValue(root, Activator.CreateInstance(Find("NBShaderEditor.NBShaderGUIContext"), root));
            type.GetProperty("SyncService", All).SetValue(root, Activator.CreateInstance(Sync, root));
            Call(Value(root, "Context"), "Refresh"); return root;
        }
        NBFXMainTexGUIEventHost Host(Material[] materials, out object root, out object item)
        {
            var host = ScriptableObject.CreateInstance<NBFXMainTexGUIEventHost>(); owned.Add(host); host.hideFlags = HideFlags.HideAndDontSave;
            host.position = new Rect(20, 20, 640, 480); host.SetupCommand = "NBFX_BF_SHARED_" + Guid.NewGuid().ToString("N");
            object r = null, p = null;
            host.Setup = () => { r = Root(materials); Assert.That(Call(r, "InitializeGraphBackFirstInputs"), Is.True); p = Value(r, "_graphBackFirstItem"); };
            host.ShowUtility(); host.SendEvent(new Event { type = EventType.ExecuteCommand, commandName = host.SetupCommand }); Check(host);
            Assert.That(host.Initialized, Is.True); root = r; item = p; host.Draw = () => Call(r, "DrawGraphBackFirstInputs"); return host;
        }
        static void Check(NBFXMainTexGUIEventHost host) { if (host.Failure != null) ExceptionDispatchInfo.Capture(host.Failure).Throw(); }
        static void Send(NBFXMainTexGUIEventHost host, Event e)
        {
            var kind = e.rawType; host.Counts.TryGetValue(kind, out int before); host.SendEvent(e); Check(host);
            Assert.That(host.Counts.TryGetValue(kind, out int after) && after > before, Is.True, "Actual original OnGUI receives the native event.");
        }
        static void Validate(Material material) => Call(Activator.CreateInstance(Find("NBShaderEditor.NBShaderGraphGUI")), "ValidateMaterial", material);
        static void AssertBack(Material material, bool enabled)
        {
            Assert.That(material.GetShaderPassEnabled("SRPDefaultUnlit"), Is.EqualTo(enabled));
            Assert.That(material.GetFloat("_NB_BackFirstEffective"), Is.EqualTo(enabled ? 1f : 0f));
        }
        static void SetBack(Material material, bool enabled)
        {
            material.SetFloat("_BackFirstPassToggle", enabled ? 1f : 0f);
            object[] args = { material, Tier, Raw, new[] { "pass.backFirst" }, false };
            Assert.That(Call(Sync, "ApplyGraphOwnedBackFirstPassState", args), Is.True); AssertBack(material, enabled);
        }
        [TearDown] public void Cleanup()
        {
            foreach (var h in owned.OfType<NBFXMainTexGUIEventHost>()) { h.Draw = null; h.Setup = null; h.Close(); }
            foreach (string path in assets) AssetDatabase.DeleteAsset(path); assets.Clear();
            foreach (var o in owned.AsEnumerable().Reverse()) if (o) Object.DestroyImmediate(o); owned.Clear();
        }

        [Test] public void G4BackFirstShared_ActualToggleMultiUndoPreservesMainAndNB()
        {
            var a = New(); var b = New(main: false);
            foreach (var m in new[] { a, b }) m.SetFloat("_Cull", 0);
            a.SetShaderPassEnabled("NBDeferredDistortPass", false); b.SetShaderPassEnabled("NBDeferredDistortPass", true);
            a.SetShaderPassEnabled("NBCameraOpaqueDistortPass", true); b.SetShaderPassEnabled("NBCameraOpaqueDistortPass", false);
            var host = Host(new[] { a, b }, out var root, out var item); string beforeA = Snapshot(a), beforeB = Snapshot(b);
            Undo.FlushUndoRecordObjects(); Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
            try
            {
                Send(host, new Event { type = EventType.Layout }); Send(host, new Event { type = EventType.Repaint });
                var rect = (Rect)Value(item, "ControlRect");
                Send(host, new Event { type = EventType.MouseDown, button = 0, mousePosition = rect.center });
                Send(host, new Event { type = EventType.MouseUp, button = 0, mousePosition = rect.center });
                foreach (var m in new[] { a, b }) { Assert.That(m.GetFloat("_BackFirstPassToggle"), Is.EqualTo(1)); Assert.That(m.GetFloat("_Cull"), Is.EqualTo(2)); AssertBack(m, true); }
                Assert.That(a.GetShaderPassEnabled("UniversalForward"), Is.True); Assert.That(b.GetShaderPassEnabled("UniversalForward"), Is.False);
                Assert.That(a.GetShaderPassEnabled("NBDeferredDistortPass"), Is.False); Assert.That(b.GetShaderPassEnabled("NBDeferredDistortPass"), Is.True);
                Assert.That(a.GetShaderPassEnabled("NBCameraOpaqueDistortPass"), Is.True); Assert.That(b.GetShaderPassEnabled("NBCameraOpaqueDistortPass"), Is.False);
                Undo.FlushUndoRecordObjects(); Undo.CollapseUndoOperations(group); string afterA = Snapshot(a), afterB = Snapshot(b); host.Draw = null;
                Assert.That(Call(Value(root, "SyncService"), "TryApplyGraphBackFirstToggle", true), Is.True);
                Assert.That(Snapshot(a), Is.EqualTo(afterA)); Assert.That(Snapshot(b), Is.EqualTo(afterB));
                Undo.PerformUndo(); Assert.That(Snapshot(a), Is.EqualTo(beforeA)); Assert.That(Snapshot(b), Is.EqualTo(beforeB));
                Undo.PerformRedo(); Assert.That(Snapshot(a), Is.EqualTo(afterA)); Assert.That(Snapshot(b), Is.EqualTo(afterB));
            }
            finally { host.Draw = null; Undo.RevertAllDownToGroup(group); }
        }

        [Test] public void G4BackFirstShared_PassiveNoncanonicalToggleAllStateReadOnly()
        {
            var a = New(); var b = New(main: false); a.SetFloat("_BackFirstPassToggle", .25f); b.SetFloat("_BackFirstPassToggle", .75f);
            var host = Host(new[] { a, b }, out _, out _); string beforeA = Snapshot(a), beforeB = Snapshot(b);
            Send(host, new Event { type = EventType.Layout }); Send(host, new Event { type = EventType.Repaint });
            Assert.That(Snapshot(a), Is.EqualTo(beforeA)); Assert.That(Snapshot(b), Is.EqualTo(beforeB));
        }

        [Test] public void G4BackFirstShared_ValidateSurfaceClipRestoreBackOnly()
        {
            var m = New(main: false); SetBack(m, true); Validate(m);
            m.SetShaderPassEnabled("NBDeferredDistortPass", false); m.SetShaderPassEnabled("NBCameraOpaqueDistortPass", true);
            // URP compares effective queue, so a transparent shader's default
            // raw=-1 can survive the first Validate. Normalize through real
            // legal surface transitions before taking the restoration baseline.
            // This also stabilizes DepthOnly's serialized disabled-pass order.
            m.SetFloat("_Surface", 0); Validate(m);
            m.SetFloat("_Surface", 1); Validate(m);
            string restored = Snapshot(m);
            m.SetFloat("_Surface", 0); Validate(m); AssertBack(m, false);
            m.SetFloat("_Surface", 1); m.SetFloat("_AlphaClip", 1); Validate(m); AssertBack(m, false);
            m.SetFloat("_AlphaClip", 0); Validate(m); AssertBack(m, true);
            Assert.That(m.GetShaderPassEnabled("UniversalForward"), Is.False);
            Assert.That(m.GetShaderPassEnabled("NBDeferredDistortPass"), Is.False); Assert.That(m.GetShaderPassEnabled("NBCameraOpaqueDistortPass"), Is.True);
            Assert.That(m.GetFloat("_BackFirstPassToggle"), Is.EqualTo(1)); Assert.That(m.GetFloat("_NB_GraphPassMigrationComplete"), Is.EqualTo(1));
            Assert.That(Snapshot(m), Is.EqualTo(restored), "Actual URP+NB Validate lifecycle restores the complete material.");
        }

        [Test] public void G4BackFirstShared_TierTransactionRepairsDeniedBackWithoutMain()
        {
            var m = New(main: false); SetBack(m, true); var root = Root(m); string before = Snapshot(m);
            object[] deny = { m, Tier, Raw, new string[0], false };
            Assert.That(Call(Sync, "ApplyGraphOwnedBackFirstPassState", deny), Is.True); AssertBack(m, false);
            Assert.That(m.GetFloat("_BackFirstPassToggle"), Is.EqualTo(1)); Assert.That(m.GetShaderPassEnabled("UniversalForward"), Is.False);
            Assert.That(Call(Value(root, "SyncService"), "TryApplyGraphSupportedGateTier", Tier, Raw), Is.True);
            AssertBack(m, true); Assert.That(Snapshot(m), Is.EqualTo(before), "The real Tier transaction restores only its registered/owned state.");
            Assert.That(Call(Value(root, "SyncService"), "TryApplyGraphSupportedGateTier", Tier, Raw), Is.True);
            Assert.That(Snapshot(m), Is.EqualTo(before));
        }

        [Test] public void G4BackFirstShared_SaveReloadMigratedDisabledMain()
        {
            var m = New(main: false); SetBack(m, true); Validate(m);
            string folder = "Assets/ResTemp/EditorTemp"; Directory.CreateDirectory(Path.Combine(Application.dataPath, "ResTemp/EditorTemp"));
            AssetDatabase.ImportAsset(folder, ImportAssetOptions.ForceSynchronousImport);
            string path = folder + "/NBFX_BackFirst_Shared_" + Guid.NewGuid().ToString("N") + ".mat";
            var saved = new Material(m) { hideFlags = HideFlags.None }; AssetDatabase.CreateAsset(saved, path); assets.Add(path);
            EditorUtility.SetDirty(saved); AssetDatabase.SaveAssets(); string before = Snapshot(saved);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            var loaded = AssetDatabase.LoadAssetAtPath<Material>(path); Assert.That(loaded, Is.Not.Null); Assert.That(Snapshot(loaded), Is.EqualTo(before));
            Validate(loaded); AssertBack(loaded, true); Assert.That(loaded.GetShaderPassEnabled("UniversalForward"), Is.False);
            Assert.That(Snapshot(loaded), Is.EqualTo(before), "Save/reopen/Validate never repeats migration or adopts the main pass.");
        }

        [Test] public void G4BackFirstShared_UnmigratedAndMixedLegacyRejectNoWrites()
        {
            var unmigrated = New(migrated: false); var ready = New(); var legacy = New(modern: false);
            var single = Root(unmigrated); string before = Snapshot(unmigrated);
            Assert.That(Call(Value(single, "SyncService"), "TryApplyGraphBackFirstToggle", true), Is.False);
            Validate(unmigrated); Assert.That(unmigrated.GetFloat("_NB_GraphPassMigrationComplete"), Is.Zero);
            Assert.That(unmigrated.GetFloat("_NB_BackFirstEffective"), Is.Zero);
            // Validate's official surface normalization is separate; the rejected
            // Toggle itself must have changed no serialized field.
            var rejected = New(migrated: false); var rejectedRoot = Root(rejected); string exact = Snapshot(rejected);
            Assert.That(Call(Value(rejectedRoot, "SyncService"), "TryApplyGraphBackFirstToggle", true), Is.False); Assert.That(Snapshot(rejected), Is.EqualTo(exact));
            object[] modernRoute = { ready, -1 }, legacyRoute = { legacy, -1 };
            var catalog = Find("NBShader.NBShaderPassFeatureCatalog");
            Assert.That(Call(catalog, "TryGetGraphColorRouting", modernRoute), Is.True);
            Assert.That(Call(catalog, "TryGetGraphColorRouting", legacyRoute), Is.True);
            Assert.That((int)modernRoute[1], Is.EqualTo(1)); Assert.That((int)legacyRoute[1], Is.Zero);
            var mixed = Root(ready, legacy); string readyBefore = Snapshot(ready), legacyBefore = Snapshot(legacy);
            var mixedEditor = (MaterialEditor)Value(mixed, "MatEditor");
            Assert.That(mixedEditor.targets, Is.EquivalentTo(new Object[] { ready, legacy }));
            Assert.That((IEnumerable)Value(mixed, "Mats"), Is.EquivalentTo(new[] { ready, legacy }));
            Assert.That(Call(Value(mixed, "SyncService"), "TryApplyGraphBackFirstToggle", true), Is.False);
            Assert.That(Snapshot(ready), Is.EqualTo(readyBefore)); Assert.That(Snapshot(legacy), Is.EqualTo(legacyBefore));
        }
        [Test] public void G4BackFirstShared_ActualAdoptCurrentMainMultiUndoUnknownReject()
        {
            var a = New(migrated: false); var b = New(migrated: false);
            a.SetShaderPassEnabled("UniversalForward", true); b.SetShaderPassEnabled("UniversalForward", false);
            a.SetFloat("_BackFirstPassToggle", 0); b.SetFloat("_BackFirstPassToggle", 1);
            a.SetShaderPassEnabled("NBDeferredDistortPass", false); b.SetShaderPassEnabled("NBDeferredDistortPass", true);
            a.SetShaderPassEnabled("NBCameraOpaqueDistortPass", true); b.SetShaderPassEnabled("NBCameraOpaqueDistortPass", false);
            var unknown = New(migrated: false); unknown.SetFloat("_NB_GraphPassMigrationComplete", 3);
            var deniedRoot = Root(a, unknown); string beforeValid = Snapshot(a), beforeUnknown = Snapshot(unknown);
            Assert.That(Call(Value(deniedRoot, "SyncService"), "TryAdoptGraphBackFirstCurrentMain"), Is.False);
            Assert.That(Snapshot(a), Is.EqualTo(beforeValid)); Assert.That(Snapshot(unknown), Is.EqualTo(beforeUnknown));
            var host = ScriptableObject.CreateInstance<NBFXMainTexGUIEventHost>(); owned.Add(host);
            host.hideFlags = HideFlags.HideAndDontSave; host.position = new Rect(20, 20, 640, 480);
            host.SetupCommand = "NBFX_BF_ADOPT_" + Guid.NewGuid().ToString("N"); object root = null;
            host.Setup = () => { root = Root(a, b); Assert.That(Call(root, "InitializeGraphBackFirstInputs"), Is.False, "Unmigrated input is intentionally not the ready Toggle."); };
            host.ShowUtility(); host.SendEvent(new Event { type = EventType.ExecuteCommand, commandName = host.SetupCommand }); Check(host);
            Assert.That(host.Initialized, Is.True); host.Draw = () => Call(root, "DrawGraphBackFirstInputs");
            string beforeA = Snapshot(a), beforeB = Snapshot(b);
            Undo.FlushUndoRecordObjects(); Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
            try
            {
                Send(host, new Event { type = EventType.Layout }); Send(host, new Event { type = EventType.Repaint });
                Assert.That(Snapshot(a), Is.EqualTo(beforeA)); Assert.That(Snapshot(b), Is.EqualTo(beforeB), "Showing an adoption button owns no material.");
                var rect = (Rect)Value(root, "_graphBackFirstAdoptRect"); Assert.That(rect.width, Is.GreaterThan(1));
                Send(host, new Event { type = EventType.MouseDown, button = 0, mousePosition = rect.center });
                Send(host, new Event { type = EventType.MouseUp, button = 0, mousePosition = rect.center });
                foreach (var m in new[] { a, b }) Assert.That(m.GetFloat("_NB_GraphPassMigrationComplete"), Is.EqualTo(1));
                Assert.That(a.GetShaderPassEnabled("UniversalForward"), Is.True); Assert.That(b.GetShaderPassEnabled("UniversalForward"), Is.False);
                Assert.That(a.GetFloat("_BackFirstPassToggle"), Is.Zero); Assert.That(b.GetFloat("_BackFirstPassToggle"), Is.EqualTo(1));
                AssertBack(a, false); AssertBack(b, true);
                Assert.That(a.GetShaderPassEnabled("NBDeferredDistortPass"), Is.False); Assert.That(b.GetShaderPassEnabled("NBDeferredDistortPass"), Is.True);
                Assert.That(a.GetShaderPassEnabled("NBCameraOpaqueDistortPass"), Is.True); Assert.That(b.GetShaderPassEnabled("NBCameraOpaqueDistortPass"), Is.False);
                Undo.FlushUndoRecordObjects(); Undo.CollapseUndoOperations(group); string afterA = Snapshot(a), afterB = Snapshot(b); host.Draw = null;
                Assert.That(Call(Value(root, "SyncService"), "TryAdoptGraphBackFirstCurrentMain"), Is.False, "Adoption is explicit once, never repeats ownership.");
                Assert.That(Snapshot(a), Is.EqualTo(afterA)); Assert.That(Snapshot(b), Is.EqualTo(afterB));
                Undo.PerformUndo(); Assert.That(Snapshot(a), Is.EqualTo(beforeA)); Assert.That(Snapshot(b), Is.EqualTo(beforeB));
                Undo.PerformRedo(); Assert.That(Snapshot(a), Is.EqualTo(afterA)); Assert.That(Snapshot(b), Is.EqualTo(afterB));
            }
            finally { host.Draw = null; Undo.RevertAllDownToGroup(group); }
        }
    }
}
