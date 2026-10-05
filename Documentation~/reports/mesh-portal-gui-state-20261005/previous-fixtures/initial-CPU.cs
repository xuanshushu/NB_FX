using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NBFX.Baseline.Tests
{
    public sealed class G4PortalSharedStateTests
    {
        const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        readonly G4BackFirstSharedLifecycleTests roots = new G4BackFirstSharedLifecycleTests();
        readonly List<Object> owned = new List<Object>();
        static Type Type(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name, false)).First(t => t != null);
        static object Call(object target, string name, params object[] args)
        {
            var type = target is Type t ? t : target.GetType(); var method = type.GetMethod(name, All);
            Assert.That(method, Is.Not.Null, type + "." + name);
            try { return method.Invoke(target is Type ? null : target, args); }
            catch (TargetInvocationException e) { ExceptionDispatchInfo.Capture(e.InnerException ?? e).Throw(); throw; }
        }
        static object Value(object target, string name)
        {
            var field = target.GetType().GetField(name, All);
            return field != null ? field.GetValue(target) : target.GetType().GetProperty(name, All).GetValue(target);
        }
        object Root(params Material[] materials) => Call(roots, "Root", (object)materials);
        static string Snapshot(Material m) => EditorJsonUtility.ToJson(m);
        Material New(bool graph = true)
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>("Packages/com.xuanxuan.nb.fx/" + (graph ? "NBShaders2/ShaderGraph/NBShaderGraph.shadergraph" : "NBShaders2/Shader/NBShader.shader"));
            Assert.That(shader && shader.isSupported, Is.True);
            var material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave }; owned.Add(material);
            material.SetFloat("_NBShaderFeatureTier", 3);
            if (graph)
            {
                material.SetFloat("_Surface", 1); material.SetFloat("_AlphaClip", 0); material.SetFloat("_ZWriteControl", 0);
                Assert.That(Call(Type("NBShaderEditor.NBShaderSyncService"), "TryInitializeGraphSupportedGateTierOnAssign", material), Is.True);
            }
            else { material.SetFloat("_TransparentMode", 1); material.SetFloat("_ForceZWriteToggle", 0); }
            material.SetFloat("_Blend", 0); material.SetFloat("_ZTest", 8);
            material.SetFloat("_Portal_Toggle", 0); material.SetFloat("_Portal_MaskToggle", 1);
            return material;
        }
        NBFXMainTexGUIEventHost Host(Material[] materials, out object root, out object item)
        {
            var h = ScriptableObject.CreateInstance<NBFXMainTexGUIEventHost>(); owned.Add(h); h.hideFlags = HideFlags.HideAndDontSave;
            h.position = new Rect(20, 20, 640, 480); h.SetupCommand = "NBFX_PORTAL_" + Guid.NewGuid().ToString("N");
            object r = null, p = null;
            h.Setup = () => { r = Root(materials); Assert.That(Call(r, "InitializeGraphPortalInputs"), Is.True); p = Value(r, "_graphPortalItem"); };
            h.ShowUtility(); h.SendEvent(new Event { type = EventType.ExecuteCommand, commandName = h.SetupCommand }); Check(h);
            Assert.That(h.Initialized, Is.True); root = r; item = p; h.Draw = () => Call(r, "DrawGraphPortalInputs", p); return h;
        }
        static void Check(NBFXMainTexGUIEventHost h) { if (h.Failure != null) ExceptionDispatchInfo.Capture(h.Failure).Throw(); }
        static void Send(NBFXMainTexGUIEventHost h, Event e)
        {
            var kind = e.rawType; h.Counts.TryGetValue(kind, out int before); h.SendEvent(e); Check(h);
            Assert.That(h.Counts.TryGetValue(kind, out int after) && after > before, Is.True);
        }
        [TearDown] public void Cleanup()
        {
            foreach (var h in owned.OfType<NBFXMainTexGUIEventHost>()) { h.Draw = null; h.Setup = null; h.Close(); }
            roots.Cleanup(); foreach (var o in owned.AsEnumerable().Reverse()) if (o) Object.DestroyImmediate(o); owned.Clear();
        }

        [TestCase(-1, TestName = "G4PortalShared_ActualParentRawInheritedQueueUndo")]
        [TestCase(2837, TestName = "G4PortalShared_ActualParentExplicitQueueUndo")]
        public void ActualParentPreservesCurrentQueueAndCoverageOtherHalf(int queue)
        {
            var m = New(); m.renderQueue = queue; m.SetFloat("_QueueControl", 0); m.SetFloat("_QueueOffset", 17);
            m.SetFloat("_NB_Flags1Lo16", 515); m.SetFloat("_NB_Flags1Hi16", 65536.25f); m.SetFloat("_PortalBlockFoldOut", 1);
            var h = Host(new[] { m }, out _, out var item); string before = Snapshot(m);
            Undo.FlushUndoRecordObjects(); Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
            try
            {
                Send(h, new Event { type = EventType.Layout }); Send(h, new Event { type = EventType.Repaint }); Assert.That(Snapshot(m), Is.EqualTo(before));
                var rect = (Rect)Value(item, "ControlRect");
                Send(h, new Event { type = EventType.MouseDown, button = 0, mousePosition = rect.center }); Send(h, new Event { type = EventType.MouseUp, button = 0, mousePosition = rect.center });
                Assert.That(m.GetFloat("_Portal_Toggle"), Is.EqualTo(1)); Assert.That(m.GetFloat("_Portal_MaskToggle"), Is.EqualTo(1));
                Assert.That(m.GetFloat("_Surface"), Is.Zero); Assert.That(m.GetFloat("_AlphaClip"), Is.EqualTo(1)); Assert.That(m.GetFloat("_ZWriteControl"), Is.EqualTo(2));
                Assert.That(m.rawRenderQueue, Is.EqualTo(queue)); Assert.That(m.GetFloat("_QueueOffset"), Is.EqualTo(17)); Assert.That(m.GetFloat("_QueueControl"), Is.EqualTo(1));
                Assert.That(m.GetFloat("_NB_Flags1Lo16"), Is.EqualTo(512)); Assert.That(m.GetFloat("_NB_Flags1Hi16"), Is.EqualTo(65536.25f));
                Assert.That(m.GetFloat("_Stencil"), Is.EqualTo(200)); Assert.That(m.GetFloat("_StencilOp"), Is.EqualTo(2)); Assert.That(m.GetFloat("_ZWrite"), Is.Zero);
                Undo.FlushUndoRecordObjects(); Undo.CollapseUndoOperations(group); string after = Snapshot(m); h.Draw = null;
                Undo.PerformUndo(); Assert.That(Snapshot(m), Is.EqualTo(before)); Undo.PerformRedo(); Assert.That(Snapshot(m), Is.EqualTo(after));
            }
            finally { h.Draw = null; Undo.RevertAllDownToGroup(group); }
        }

        [Test] public void G4PortalShared_NativeThreeBranchesQueueAndDitherCycles()
        {
            var native = New(false); var graph = New();
            native.SetFloat("_TransparentShadowDitherToggle", 1); native.SetInteger("_W9ParticleShaderFlags1", 515);
            graph.SetFloat("_NB_Flags1Lo16", 515); graph.SetFloat("_NB_Flags1Hi16", 65536.25f);
            foreach (var m in new[] { native, graph }) m.renderQueue = 2837;
            native.SetFloat("_QueueBias", 17); graph.SetFloat("_QueueOffset", 17); graph.SetFloat("_QueueControl", 0);
            void Apply()
            {
                var nr = Root(native); var gr = Root(graph);
                Call(Value(nr, "SyncService"), "ApplyPortalState"); Assert.That(Call(Value(gr, "SyncService"), "TryApplyGraphPortalState"), Is.True);
                foreach (string p in new[] { "_Stencil", "_StencilComp", "_StencilOp", "_StencilFail", "_StencilZFail", "_StencilReadMask", "_StencilWriteMask", "_StencilKeyIndex", "_CustomStencilTest", "_ZTest", "_ZWrite" })
                    Assert.That(graph.GetFloat(p), Is.EqualTo(native.GetFloat(p)), "Original Native state " + p);
                Assert.That(native.rawRenderQueue, Is.EqualTo(2837)); Assert.That(graph.rawRenderQueue, Is.EqualTo(2837));
                Assert.That(graph.GetFloat("_QueueOffset"), Is.EqualTo(17)); Assert.That(graph.GetFloat("_NB_Flags1Hi16"), Is.EqualTo(65536.25f));
                Assert.That(((int)graph.GetFloat("_NB_Flags1Lo16") & 3), Is.EqualTo(native.GetInteger("_W9ParticleShaderFlags1") & 3));
            }
            Apply(); Assert.That((int)graph.GetFloat("_NB_Flags1Lo16") & 3, Is.EqualTo(3), "Direct transparent Off retains existing dither.");
            foreach (var m in new[] { native, graph }) { m.SetFloat("_Portal_Toggle", 1); m.SetFloat("_Portal_MaskToggle", 1); }
            Apply(); Assert.That(graph.GetFloat("_Surface"), Is.Zero); Assert.That(graph.GetFloat("_AlphaClip"), Is.EqualTo(1)); Assert.That((int)graph.GetFloat("_NB_Flags1Lo16") & 3, Is.Zero);
            foreach (var m in new[] { native, graph }) m.SetFloat("_Portal_MaskToggle", 0);
            Apply(); Assert.That(graph.GetFloat("_StencilComp"), Is.EqualTo(3)); Assert.That(graph.GetFloat("_Surface"), Is.Zero, "Non-mask On preserves CutOff surface.");
            foreach (var m in new[] { native, graph }) m.SetFloat("_Portal_Toggle", 0);
            Apply(); Assert.That(graph.GetFloat("_Surface"), Is.EqualTo(1)); Assert.That(graph.GetFloat("_AlphaClip"), Is.Zero);
            Assert.That(graph.GetFloat("_Blend"), Is.EqualTo(0)); Assert.That((int)graph.GetFloat("_NB_Flags1Lo16") & 3, Is.EqualTo(1), "Mask->Off must not resurrect cleared dither.");
        }

        [Test] public void G4PortalShared_PassiveAndUnknownMultiSchemaNoWrites()
        {
            var a = New(); var b = New(); foreach (var m in new[] { a, b }) { m.SetFloat("_PortalBlockFoldOut", 1); m.SetFloat("_QueueControl", 0); }
            a.SetFloat("_Portal_Toggle", .25f); b.SetFloat("_Portal_Toggle", .75f); a.SetFloat("_Portal_MaskToggle", .25f); b.SetFloat("_Portal_MaskToggle", .75f);
            var h = Host(new[] { a, b }, out _, out _); string sa = Snapshot(a), sb = Snapshot(b);
            Send(h, new Event { type = EventType.Layout }); Send(h, new Event { type = EventType.Repaint });
            Assert.That(Snapshot(a), Is.EqualTo(sa)); Assert.That(Snapshot(b), Is.EqualTo(sb)); h.Draw = null;
            b.SetFloat("_QueueControl", 3); var root = Root(a, b); sa = Snapshot(a); sb = Snapshot(b);
            Assert.That(Call(Value(root, "SyncService"), "TryApplyGraphPortalState"), Is.False);
            Assert.That(Snapshot(a), Is.EqualTo(sa)); Assert.That(Snapshot(b), Is.EqualTo(sb));
        }
    }
}
