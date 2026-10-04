using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NBFX.Baseline.Tests
{
    // Bounded ProgramNoise3 projection. Reuses original Tier snapshot + Debug renderer.
    public sealed class G4GraphProgramNoiseTierTests
    {
        const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static readonly string[] Toggles = { "_ProgramNoise_Toggle", "_ProgramNoise_Simple_Toggle", "_ProgramNoise_Voronoi_Toggle" };
        static readonly string[] Allows = { "_NB_TierAllowProgramNoise", "_NB_TierAllowProgramSimple", "_NB_TierAllowProgramVoronoi" };
        readonly List<Object> owned = new List<Object>();
        [OneTimeSetUp] public void Preflight() => G4SpecDebugFixture.PreflightImport();
        static Type FindType(string name) => G4SpecDebugFixture.FindType(name);
        Material Material() { var material = G4SpecDebugFixture.NewGraph(); owned.Add(material); return material; }
        sealed class Snapshot
        {
            readonly object value;
            static Type Shared => typeof(G4GraphGuiFeatureIntentTests).GetNestedType("Snapshot", BindingFlags.NonPublic);
            Snapshot(object value) { this.value = value; }
            public static Snapshot Read(Material material) => new Snapshot(Shared.GetMethod("Read", Static).Invoke(null, new object[] { material }));
            public void AssertSame(Material material, string label, params string[] allowed) => Shared.GetMethod("AssertSame", Instance).Invoke(value, new object[] { material, label, allowed });
        }
        static string[] Policy(string name)
        {
            if (name == "full") return (string[])FindType("NBShader.NBShaderFeatureCatalog").GetField("RawKeywords", Static).GetValue(null);
            if (name == "parent") return new[] { "_PROGRAM_NOISE" };
            if (name == "children") return new[] { "_PROGRAM_NOISE_SIMPLE", "_PROGRAM_NOISE_VORONOI" };
            return new string[0];
        }
        static bool Apply(Material material, string policy, out bool changed, int tier = 3)
        {
            var method = FindType("NBShaders2.Editor.FeatureLevel.NBShaderFeatureLevelMaterialApplier").GetMethod("ApplyGraphProgramNoiseGroup", Static); Assert.That(method, Is.Not.Null);
            object[] args = { material, Enum.ToObject(FindType("NBShader.NBShaderFeatureTier"), tier), Policy(policy), false };
            bool accepted = (bool)method.Invoke(null, args); changed = (bool)args[3]; return accepted;
        }
        [TearDown] public void Cleanup() { foreach (var item in owned.AsEnumerable().Reverse()) if (item) Object.DestroyImmediate(item); owned.Clear(); }
        static IEnumerable<TestCaseData> CPUCases()
        {
            foreach (string policy in new[] { "full", "parent", "children", "none" }) foreach (int intent in new[] { 3, 5, 7, 6 })
                yield return new TestCaseData(policy, intent).SetName("G4PNoiseTier_CPU_" + policy + "_i" + intent);
        }
        [TestCaseSource(nameof(CPUCases))]
        public void ParentAndSubtypeGatesPreserveCompleteIntent(string policy, int intent)
        {
            var material = Material(); for (int i = 0; i < 3; ++i) { material.SetFloat(Toggles[i], (intent >> i) & 1); material.SetFloat(Allows[i], .25f); }
            material.SetFloat("_NB_Flags0Lo16", -3.75f); material.SetFloat("_NB_Flags1Hi16", 65536.25f); material.SetFloat("_NB_PNoiseBlendHi16", 12345.125f);
            var before = Snapshot.Read(material); bool changed; Assert.That(Apply(material, policy, out changed), Is.True); Assert.That(changed, Is.True);
            bool parent = (intent & 1) != 0 && (policy == "full" || policy == "parent");
            Assert.That(material.GetFloat(Allows[0]), Is.EqualTo(parent ? 1 : 0));
            Assert.That(material.GetFloat(Allows[1]), Is.EqualTo(parent && (intent & 2) != 0 && policy == "full" ? 1 : 0));
            Assert.That(material.GetFloat(Allows[2]), Is.EqualTo(parent && (intent & 4) != 0 && policy == "full" ? 1 : 0));
            before.AssertSame(material, "Only derived three Floats change", Allows); Assert.That(Apply(material, policy, out changed), Is.True); Assert.That(changed, Is.False); before.AssertSame(material, "Repeat projection is no-op", Allows);
        }
        [TestCase(0, TestName = "G4PNoiseTier_CPU_RestoreGeneralGuard")]
        public void RestoreAndGeneralGuard(int unused)
        {
            var material = Material(); foreach (string toggle in Toggles) material.SetFloat(toggle, 1); var before = Snapshot.Read(material); bool changed;
            Assert.That(Apply(material, "none", out changed, 0), Is.True); Assert.That(Allows.All(n => material.GetFloat(n) == 0), Is.True);
            Assert.That(Apply(material, "full", out changed), Is.True); before.AssertSame(material, "Original intent fully restores");
            var method = FindType("NBShaders2.Editor.FeatureLevel.NBShaderFeatureLevelMaterialApplier").GetMethods(Static).Single(m => m.Name == "Apply" && m.GetParameters().Length == 5);
            object[] args = { material, Enum.ToObject(FindType("NBShader.NBShaderFeatureTier"), 0), true, true, false }; Assert.That(method.Invoke(null, args), Is.False); Assert.That(args[4], Is.False); before.AssertSame(material, "General Graph Apply still protected");
        }
        [TestCase(3f, TestName = "G4PNoiseTier_CPU_UnknownMarker")]
        public void UnknownMarkerRejects(float value)
        { var material = Material(); material.SetFloat("_NB_GraphGUIStateVersion", value); var before = Snapshot.Read(material); bool changed; Assert.That(Apply(material, "none", out changed), Is.False); Assert.That(changed, Is.False); before.AssertSame(material, "Unknown schema is atomic"); }
        [TestCase(false, TestName = "G4PNoiseTier_CPU_MissingGate")]
        [TestCase(true, TestName = "G4PNoiseTier_CPU_IntegerGate")]
        public void GateSchemaRejects(bool integer)
        {
            string text = "Shader \"Hidden/NBFX/PNoiseTierSchema\" { Properties { _NB_TierAllowProgramNoise(\"P\",Float)=1 _NB_TierAllowProgramSimple(\"S\",Float)=1 " + (integer ? "_NB_TierAllowProgramVoronoi(\"V\",Integer)=1 " : "") + "} SubShader { Pass { } } }";
            var shader = ShaderUtil.CreateShaderAsset(text, false); Assert.That(shader, Is.Not.Null); shader.hideFlags = HideFlags.HideAndDontSave; owned.Add(shader); var material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave }; owned.Add(material); var before = Snapshot.Read(material);
            bool changed; Assert.That(Apply(material, "none", out changed), Is.False); Assert.That(changed, Is.False); before.AssertSame(material, "Missing/wrong gate never partially writes");
        }
        static IEnumerable<TestCaseData> GPUCases()
        { foreach (string mode in new[] { "simple", "voronoi", "both" }) foreach (bool ortho in new[] { true, false }) yield return new TestCaseData(mode, ortho).SetName("G4PNoiseTier_GPU_" + mode + (ortho ? "_ortho" : "_perspective")); }
        static void Configure(G4SpecDebugFixture.Harness harness, string mode)
        {
            // Original actual Debug feature1 setup owns scene/material/core values.
            typeof(G4GraphDebugTests).GetMethod("Configure", Static).Invoke(null, new object[] { harness, 1, true });
            for (int m = 0; m < 3; ++m)
            {
                var material = harness.materials[m]; material.SetFloat(Toggles[1], mode != "voronoi" ? 1 : 0); material.SetFloat(Toggles[2], mode != "simple" ? 1 : 0);
                if (m != 2) { G4SpecDebugFixture.SetKeyword(material, "_PROGRAM_NOISE_SIMPLE", mode != "voronoi"); G4SpecDebugFixture.SetKeyword(material, "_PROGRAM_NOISE_VORONOI", mode != "simple"); }
                else G4SpecDebugFixture.Validate(material);
                G4SpecDebugFixture.Harness.RestoreForward(material, m == 2);
            }
        }
        static G4SpecDebugFixture.Metrics Measure(string id, string scope, Color[] empty, Color[][][] frames, Color[][][] repeats)
        {
            return new G4SpecDebugFixture.Metrics { caseId = id, scope = scope, finite = G4SpecDebugFixture.Finite(empty) && frames.SelectMany(s => s).Concat(repeats.SelectMany(s => s)).All(G4SpecDebugFixture.Finite),
                ab = frames.Select(s => G4SpecDebugFixture.Delta(s[0], s[1])).ToArray(), bc = frames.Select(s => G4SpecDebugFixture.Delta(s[1], s[2])).ToArray(), repeat = Enumerable.Range(0, frames.Length).SelectMany(s => Enumerable.Range(0, 3).Select(m => G4SpecDebugFixture.Delta(frames[s][m], repeats[s][m]))).ToArray(),
                response = Enumerable.Range(0, 3).Select(m => G4SpecDebugFixture.Delta(frames[0][m], frames[1][m])).ToArray(), restore = Enumerable.Range(0, 3).Select(m => G4SpecDebugFixture.Delta(frames[0][m], frames[2][m])).ToArray(), visible = frames.SelectMany(s => s).Select(p => G4SpecDebugFixture.Visible(p, empty)).ToArray() };
        }
        [TestCaseSource(nameof(GPUCases))]
        public void RealProgramNoiseDebugGateAndRestoration(string mode, bool ortho)
        {
            string id = "pnoise-tier-" + mode + (ortho ? "-ortho" : "-perspective"); using (var harness = new G4SpecDebugFixture.Harness(id, ortho))
            {
                var empty = harness.Snap("empty"); Configure(harness, mode); bool initialChanged; Assert.That(Apply(harness.materials[2], "full", out initialChanged), Is.True); var intent = Snapshot.Read(harness.materials[2]); var frames = new Color[3][][]; var repeats = new Color[3][][];
                for (int state = 0; state < 3; ++state)
                {
                    bool changed; Assert.That(Apply(harness.materials[2], state == 1 ? "none" : "full", out changed), Is.True); intent.AssertSame(harness.materials[2], "Saved intent/flags preserved", Allows); frames[state] = new Color[3][]; repeats[state] = new Color[3][];
                    for (int m = 0; m < 3; ++m)
                    {
                        if (m != 2) { G4SpecDebugFixture.SetKeyword(harness.materials[m], "_PROGRAM_NOISE", state != 1); G4SpecDebugFixture.SetKeyword(harness.materials[m], "_PROGRAM_NOISE_SIMPLE", state != 1 && mode != "voronoi"); G4SpecDebugFixture.SetKeyword(harness.materials[m], "_PROGRAM_NOISE_VORONOI", state != 1 && mode != "simple"); }
                        frames[state][m] = harness.Snap("ABC"[m] + "-state" + state, harness.materials[m]); repeats[state][m] = harness.Snap("ABC"[m] + "-state" + state + "-repeat", harness.materials[m]);
                    }
                }
                var metrics = Measure(id, "Same Debug core, three PNoise modes, full/stripped/restored; strong independent control; full-frame strict0/finite/visible/repeat. Not whole Tier/Pass/VFX/Player.", empty, frames, repeats); harness.SaveAndAssert(metrics); Assert.That(metrics.response.All(v => v > .001f), Is.True); intent.AssertSame(harness.materials[2], "Complete restore");
            }
        }
        [TestCase(true, TestName = "G4PNoiseTier_GPU_StrippedDebugUsesClip_ortho")]
        [TestCase(false, TestName = "G4PNoiseTier_GPU_StrippedDebugUsesClip_perspective")]
        public void StrippedProgramNoiseCannotKeepDebugBypass(bool ortho)
        {
            string id = "pnoise-tier-clip" + (ortho ? "-ortho" : "-perspective"); using (var harness = new G4SpecDebugFixture.Harness(id, ortho))
            {
                var empty = harness.Snap("empty"); Configure(harness, "simple"); var frames = new Color[3][][]; var repeats = new Color[3][][];
                for (int state = 0; state < 3; ++state)
                {
                    bool changed; Assert.That(Apply(harness.materials[2], state == 1 ? "none" : "full", out changed), Is.True); frames[state] = new Color[3][]; repeats[state] = new Color[3][];
                    for (int m = 0; m < 3; ++m)
                    {
                        var material = harness.materials[m]; material.SetFloat("_Cutoff", 2); material.EnableKeyword("_ALPHATEST_ON"); if (m == 2) material.SetFloat("_AlphaClip", 1);
                        else { G4SpecDebugFixture.SetKeyword(material, "_PROGRAM_NOISE", state != 1); G4SpecDebugFixture.SetKeyword(material, "_PROGRAM_NOISE_SIMPLE", state != 1); }
                        frames[state][m] = harness.Snap("ABC"[m] + "-state" + state, material); repeats[state][m] = harness.Snap("ABC"[m] + "-state" + state + "-repeat", material);
                    }
                }
                var metrics = Measure(id, "Debug-on full/restored stays visible; stripped ProgramNoise must execute normal final clip. Zero control is explicitly required, never counted as visible equivalence.", empty, frames, repeats); metrics.visible = frames[0].Concat(frames[2]).Select(p => G4SpecDebugFixture.Visible(p, empty)).ToArray(); harness.SaveAndAssert(metrics);
                Assert.That(metrics.response.All(v => v > .1f), Is.True); Assert.That(frames[1].All(p => G4SpecDebugFixture.Delta(p, empty) == 0), Is.True, "Actual clipped stripped control");
            }
        }
    }
}
