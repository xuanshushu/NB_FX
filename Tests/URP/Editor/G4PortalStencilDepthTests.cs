using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace NBFX.Baseline.Tests
{
    // Reuses the existing actual URP render-state Fixture, its supported
    // stencil attachment and capture method. No new rendering framework.
    public sealed class G4PortalStencilDepthTests
    {
        const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
        readonly G4BackFirstSharedLifecycleTests roots = new G4BackFirstSharedLifecycleTests();
        Shader background, writer, probe;
        readonly List<Object> nativeBridges = new List<Object>();
        readonly List<FrozenProjectionReceipt> frozenReceipts = new List<FrozenProjectionReceipt>();
        static Type Original => typeof(G4GraphRenderStateTests);
        static Type Find(string n) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(n, false)).First(t => t != null);
        static object Call(object target, string name, params object[] args)
        {
            var type = target is Type t ? t : target.GetType(); bool isStatic = target is Type;
            var methods = type.GetMethods(All).Where(m => m.Name == name && m.IsStatic == isStatic &&
                m.GetParameters().Length == args.Length && m.GetParameters().Select((p,i) => args[i] == null ||
                    p.ParameterType.IsInstanceOfType(args[i])).All(matches => matches)).ToArray();
            Assert.That(methods.Length, Is.EqualTo(1), "Exact instance/static and parameter signature: " + type + "." + name);
            var method = methods.Single(); Assert.That(method, Is.Not.Null);
            return method.Invoke(isStatic ? null : target, args);
        }
        static object Value(object target, string name)
        {
            var f = target.GetType().GetField(name, All); return f != null ? f.GetValue(target) : target.GetType().GetProperty(name, All).GetValue(target);
        }
        object Root(Material m) => Call(roots, "Root", (object)new[] { m });
        void Portal(Material m, bool graph)
        {
            var sync = Value(Root(m), "SyncService");
            if (graph) Assert.That(Call(sync, "TryApplyGraphPortalState"), Is.True);
            else NativeOperation(m, "Portal", material => Call(Value(Root(material), "SyncService"), "ApplyPortalState"));
        }
        void ForceMaskDepth(Material m, bool graph, bool enabled)
        {
            m.SetFloat(graph ? "_ZWriteControl" : "_ForceZWriteToggle", enabled ? 1 : 2);
            if (graph) Call(Activator.CreateInstance(Find("NBShaderEditor.NBShaderGraphGUI")), "ValidateMaterial", m);
            else NativeOperation(m, "ForceMaskDepth", material => Call(Value(Root(material), "SyncService"), "SyncMaterialState"));
        }
        [Serializable] sealed class FrozenProjectionReceipt
        {
            public string operation, frozenAsset, currentAsset;
            public bool rawIntentMatchedBefore, rawStateMatchedAfter;
            public string[] keywords, passNames;
            public bool[] passEnabled;
        }
        void NativeOperation(Material target, string operation, Action<Material> apply)
        {
            const string frozenAsset = "Packages/com.xuanxuan.nb.fx/Tests/Baseline/Frozen/NBShaders2/Shader/NBShader.shader";
            if (AssetDatabase.GetAssetPath(target.shader) != frozenAsset) { apply(target); return; }
            const string currentAsset = "Packages/com.xuanxuan.nb.fx/NBShaders2/Shader/NBShader.shader";
            var current = AssetDatabase.LoadAssetAtPath<Shader>(currentAsset); Assert.That(current && current.isSupported, Is.True);
            var bridge = new Material(current) { hideFlags = HideFlags.HideAndDontSave }; nativeBridges.Add(bridge);
            // CopyPropertiesFromMaterial copies serialized property storage;
            // Frozen has no declaration/storage for these two newer Current fields.
            Assert.That(target.HasProperty("_AnimationSheetHelperBlendIntensity"), Is.False);
            Assert.That(target.HasProperty("_BaseMap_AnimationSheetBlend_ST"), Is.False);
            float defaultBlendIntensity = bridge.GetFloat("_AnimationSheetHelperBlendIntensity");
            Vector4 defaultBlendST = bridge.GetVector("_BaseMap_AnimationSheetBlend_ST");
            bridge.CopyPropertiesFromMaterial(target);
            bridge.SetFloat("_AnimationSheetHelperBlendIntensity", defaultBlendIntensity);
            bridge.SetVector("_BaseMap_AnimationSheetBlend_ST", defaultBlendST);
            string[] floats = { "_Portal_Toggle", "_Portal_MaskToggle", "_TransparentMode", "_ForceZWriteToggle", "_Blend", "_ZTest", "_ZWrite",
                "_Stencil", "_StencilComp", "_StencilOp", "_StencilFail", "_StencilZFail", "_StencilReadMask", "_StencilWriteMask", "_StencilKeyIndex", "_CustomStencilTest", "_QueueBias" };
            string[] words = { "_W9ParticleShaderFlags", "_W9ParticleShaderFlags1", "_W9ParticleShaderWrapFlags", "_W9ParticleShaderColorChannelFlag",
                "_W9ParticleCustomDataFlag0", "_W9ParticleCustomDataFlag1", "_W9ParticleCustomDataFlag2", "_W9ParticleCustomDataFlag3" };
            void AssertRawSame(string phase)
            {
                foreach (string p in floats) Assert.That(bridge.GetFloat(p), Is.EqualTo(target.GetFloat(p)), phase + " " + p);
                foreach (string p in words) Assert.That(bridge.GetInteger(p), Is.EqualTo(target.GetInteger(p)), phase + " " + p);
                Assert.That(bridge.rawRenderQueue, Is.EqualTo(target.rawRenderQueue), phase + " raw queue");
            }
            AssertRawSame("Actual same raw Native intent before operation");
            apply(bridge); apply(target);
            AssertRawSame("Original Native state writes after operation");
            // Frozen's baseline shader is intentionally not the current runtime
            // classifier host. Only its falsely cleared keyword/pass projection
            // is recovered from this true Native host; shader/math stays frozen.
            target.shaderKeywords = bridge.shaderKeywords;
            var catalog = Find("NBShader.NBShaderPassFeatureCatalog");
            var infos = ((System.Collections.IEnumerable)catalog.GetField("RawPassFeatures", All).GetValue(null)).Cast<object>();
            var names = infos.Select(i => (string)i.GetType().GetField("passName", All).GetValue(i)).Concat(new[] { "UniversalForward" }).Distinct().ToArray();
            foreach (string name in names) target.SetShaderPassEnabled(name, bridge.GetShaderPassEnabled(name));
            Assert.That(target.shaderKeywords, Is.EquivalentTo(bridge.shaderKeywords));
            foreach (string name in names) Assert.That(target.GetShaderPassEnabled(name), Is.EqualTo(bridge.GetShaderPassEnabled(name)), name);
            frozenReceipts.Add(new FrozenProjectionReceipt { operation = operation, frozenAsset = frozenAsset, currentAsset = currentAsset,
                rawIntentMatchedBefore = true, rawStateMatchedAfter = true, keywords = bridge.shaderKeywords, passNames = names,
                passEnabled = names.Select(bridge.GetShaderPassEnabled).ToArray() });
        }
        [OneTimeSetUp] public void ExistingAuxiliaryShaders()
        {
            Assert.That((GraphicsFormat)Call(Original, "SupportedStencilFormat"), Is.Not.EqualTo(GraphicsFormat.None));
            Shader Make(string n, string color) => (Shader)Call(Original, "MakeAuxiliaryShader", n, "", "RGBA", color);
            background = Make("PortalBackdrop", "half4(0.125,0.25,0.5,1)");
            writer = Make("PortalUnusedWriter", "half4(0,0,0,1)"); probe = Make("PortalUnusedProbe", "half4(0,0,0,1)");
        }
        [OneTimeTearDown] public void CleanupShaders() { foreach (var s in new[] { background, writer, probe }) if (s) Object.DestroyImmediate(s); }
        [TearDown] public void CleanupEditors()
        {
            roots.Cleanup(); foreach (var m in nativeBridges) if (m) Object.DestroyImmediate(m); nativeBridges.Clear(); frozenReceipts.Clear();
        }
        [Serializable] sealed class Metrics
        {
            public string unity, api, scope;
            public bool orthographic, finite, nonCDCD, stencilAttachment;
            public float ab, bc, repeat, noMaskAB, noMaskBC, depthAB, depthBC, restore;
            public float[] stencilResponse, depthResponse;
            public int[] visible;
            public int rawFrames;
        }
        static float Delta(Color[] a, Color[] b) => a.Zip(b, (x, y) => Enumerable.Range(0, 4).Max(i => Mathf.Abs(x[i] - y[i]))).Max();
        static int Visible(Color[] a, Color[] b) => a.Zip(b, (x, y) => Enumerable.Range(0, 4).Any(i => x[i] != y[i]) ? 1 : 0).Sum();
        static bool Finite(Color[] a) => a.All(c => Enumerable.Range(0, 4).All(i => !float.IsNaN(c[i]) && !float.IsInfinity(c[i])));

        [TestCase(true, TestName = "G4PortalStencilDepth_ActualABC_ortho")]
        [TestCase(false, TestName = "G4PortalStencilDepth_ActualABC_perspective")]
        public void ActualStencilMaskAndForceDepthResponse(bool ortho)
        {
            var ft = Original.GetNestedType("Fixture", All); Assert.That(ft, Is.Not.Null);
            var fixture = Activator.CreateInstance(ft, All, null, new object[] { background, writer, probe, ortho, true }, null);
            var graph = (Material)ft.GetField("Graph", All).GetValue(fixture); var current = (Material)ft.GetField("Legacy", All).GetValue(fixture);
            var target = (RenderTexture)ft.GetField("Target", All).GetValue(fixture);
            var writerObject = (GameObject)ft.GetField("_writerObject", All).GetValue(fixture); var maskRenderer = writerObject.GetComponent<MeshRenderer>();
            var frozenShader = AssetDatabase.LoadAssetAtPath<Shader>("Packages/com.xuanxuan.nb.fx/Tests/Baseline/Frozen/NBShaders2/Shader/NBShader.shader");
            Assert.That(frozenShader && frozenShader.isSupported, Is.True);
            var frozen = new Material(frozenShader); var objects = new[] { frozen, current, graph };
            var masks = objects.Select(m => new Material(m.shader)).ToArray(); var extra = new List<Object> { frozen }; extra.AddRange(masks);
            var configure = ft.GetMethod("ConfigureMaterial", All); Assert.That(configure, Is.Not.Null);
            string folder = Path.Combine(Environment.GetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR") ?? Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp/NBFXPortal"), ortho ? "portal-ortho" : "portal-perspective"); Directory.CreateDirectory(folder);
            var frames = new List<Color[]>();
            Color[] Capture(Material m, string n)
            {
                var p = (Color[])ft.GetMethod("Capture", All).Invoke(fixture, new object[] { m, Path.Combine(folder, n) }); frames.Add(p); return p;
            }
            try
            {
                ft.GetProperty("ProbeEnabled", All).SetValue(fixture, false); ft.GetProperty("WriterEnabled", All).SetValue(fixture, false);
                for (int k = 0; k < 3; ++k)
                {
                    bool g = k == 2;
                    foreach (Material m in new[] { objects[k], masks[k] })
                    {
                        configure.Invoke(null, new object[] { m, g }); m.SetFloat("_NBShaderFeatureTier", 3);
                        m.SetFloat("_Portal_Toggle", 1); m.SetFloat("_Portal_MaskToggle", m == masks[k] ? 1 : 0);
                        m.SetFloat("_Blend", 0); m.SetFloat("_Cutoff", .5f);
                        m.SetColor(g ? "_Color" : "_BaseColor", m == masks[k] ? new Color(0, 0, 0, 1) : new Color(1, 0, 0, 1));
                        if (g)
                        {
                            m.SetFloat("_Surface", m == masks[k] ? 1 : 0); m.SetFloat("_AlphaClip", 0); m.SetFloat("_ZWriteControl", 0); m.SetFloat("_QueueControl", 1);
                            Assert.That(Call(Find("NBShaderEditor.NBShaderSyncService"), "TryInitializeGraphSupportedGateTierOnAssign", m), Is.True);
                        }
                        else { m.SetFloat("_TransparentMode", m == masks[k] ? 1 : 0); m.SetFloat("_ForceZWriteToggle", 0); }
                        m.renderQueue = m == masks[k] ? 2000 : 3000; Portal(m, g);
                    }
                    Assert.That(masks[k].GetFloat("_ZWrite"), Is.Zero, "Portal mask must not populate actual depth.");
                    Assert.That(objects[k].GetFloat("_StencilComp"), Is.EqualTo(3)); Assert.That(masks[k].GetFloat("_StencilOp"), Is.EqualTo(2));
                    // Establish the real depth-pass serialization baseline before
                    // asserting a complete on/off restoration (not a tolerance).
                    ForceMaskDepth(masks[k], g, true); ForceMaskDepth(masks[k], g, false);
                }
                var empty = Capture(null, "empty"); var emptyRepeat = Capture(null, "empty-repeat"); Assert.That(Delta(empty, emptyRepeat), Is.Zero);
                var on = new Color[3][]; var onRepeat = new Color[3][]; var noMask = new Color[3][]; var depth = new Color[3][]; var restored = new Color[3][];
                float repeat = 0;
                for (int k = 0; k < 3; ++k)
                {
                    maskRenderer.sharedMaterial = masks[k]; ft.GetProperty("WriterEnabled", All).SetValue(fixture, true);
                    on[k] = Capture(objects[k], "ABC"[k] + "-portal"); onRepeat[k] = Capture(objects[k], "ABC"[k] + "-portal-repeat"); repeat = Mathf.Max(repeat, Delta(on[k], onRepeat[k]));
                    ft.GetProperty("WriterEnabled", All).SetValue(fixture, false);
                    noMask[k] = Capture(objects[k], "ABC"[k] + "-no-mask"); repeat = Mathf.Max(repeat, Delta(noMask[k], Capture(objects[k], "ABC"[k] + "-no-mask-repeat")));
                    Assert.That(Delta(noMask[k], empty), Is.Zero, "Without a real stencil mask, Equal200 object must not draw.");
                    ft.GetProperty("WriterEnabled", All).SetValue(fixture, true);
                    string beforeMask = EditorJsonUtility.ToJson(masks[k]); ForceMaskDepth(masks[k], k == 2, true);
                    Assert.That(masks[k].GetFloat("_ZWrite"), Is.EqualTo(1)); depth[k] = Capture(objects[k], "ABC"[k] + "-mask-forced-depth");
                    repeat = Mathf.Max(repeat, Delta(depth[k], Capture(objects[k], "ABC"[k] + "-mask-forced-depth-repeat")));
                    ForceMaskDepth(masks[k], k == 2, false); restored[k] = Capture(objects[k], "ABC"[k] + "-restored");
                    repeat = Mathf.Max(repeat, Delta(restored[k], Capture(objects[k], "ABC"[k] + "-restored-repeat")));
                    Assert.That(EditorJsonUtility.ToJson(masks[k]), Is.EqualTo(beforeMask), "Exact mask force-depth state restore.");
                }
                var metric = new Metrics { unity = Application.unityVersion, api = SystemInfo.graphicsDeviceType.ToString(), scope = "Original realURP RenderState Fixture; actual Frozen/current/Graph Portal mask stencil and main-pass ForceZWrite depth response. No Shadow/default SSAO/Controller/VFX/Player/perf claim.", orthographic = ortho, finite = frames.All(Finite), nonCDCD = frames.All(p => !p.All(c => Enumerable.Range(0, 4).All(i => c[i] == -23.203125f))), stencilAttachment = target.descriptor.depthStencilFormat == (GraphicsFormat)Call(Original, "SupportedStencilFormat"), ab = Delta(on[0], on[1]), bc = Delta(on[1], on[2]), repeat = repeat, noMaskAB = Delta(noMask[0], noMask[1]), noMaskBC = Delta(noMask[1], noMask[2]), depthAB = Delta(depth[0], depth[1]), depthBC = Delta(depth[1], depth[2]), restore = Enumerable.Range(0, 3).Max(k => Delta(on[k], restored[k])), stencilResponse = Enumerable.Range(0, 3).Select(k => Delta(on[k], noMask[k])).ToArray(), depthResponse = Enumerable.Range(0, 3).Select(k => Delta(on[k], depth[k])).ToArray(), visible = Enumerable.Range(0, 3).Select(k => Visible(on[k], empty)).ToArray(), rawFrames = frames.Count };
                File.WriteAllText(Path.Combine(folder, "frozen-projection-receipts.json"), "[" + string.Join(",", frozenReceipts.Select(r => JsonUtility.ToJson(r))) + "]");
                File.WriteAllText(Path.Combine(folder, "metrics.json"), JsonUtility.ToJson(metric, true)); Debug.Log("NBFX_PORTAL_STENCIL_DEPTH " + JsonUtility.ToJson(metric));
                Assert.That(metric.finite && metric.nonCDCD && metric.stencilAttachment, Is.True);
                Assert.That(metric.ab + metric.bc + metric.noMaskAB + metric.noMaskBC + metric.depthAB + metric.depthBC, Is.Zero);
                Assert.That(metric.repeat + metric.restore, Is.Zero); Assert.That(metric.rawFrames, Is.EqualTo(26));
                foreach (int pixels in metric.visible) Assert.That(pixels, Is.GreaterThan(128));
                foreach (float response in metric.stencilResponse) Assert.That(response, Is.GreaterThan(.04f));
                foreach (float response in metric.depthResponse) Assert.That(response, Is.GreaterThan(.04f));
            }
            finally { foreach (var m in extra) if (m) Object.DestroyImmediate(m); ((IDisposable)fixture).Dispose(); }
        }
    }
}
