using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NBFX.Baseline.Tests
{
    public sealed class G4GraphDebugTests
    {
        [OneTimeSetUp] public void Preflight() => G4SpecDebugFixture.PreflightImport();
        static IEnumerable<TestCaseData> CPUCases()
        {
            for (int feature = 0; feature < 6; ++feature) foreach (bool parent in new[] { false, true })
                yield return new TestCaseData(feature, parent).SetName("G4Debug_CPU_" + feature + "_parent" + (parent ? "on" : "off"));
        }
        [TestCaseSource(nameof(CPUCases))]
        public void SameReaderParentDependencyAndKeywordSync(int feature, bool parent)
        {
            var material = G4SpecDebugFixture.NewGraph();
            try
            {
                for (int i = 0; i < 6; ++i) { material.SetFloat(G4SpecDebugFixture.DebugProps[i], i == feature ? 1 : 0); G4SpecDebugFixture.SetKeyword(material, G4SpecDebugFixture.DebugKeywords[i], true); }
                material.SetFloat(G4SpecDebugFixture.Parents[feature], parent ? 1 : 0);
                var before = G4SpecDebugFixture.Properties(material); G4SpecDebugFixture.Sync(material);
                for (int i = 0; i < 6; ++i) Assert.That(material.IsKeywordEnabled(G4SpecDebugFixture.DebugKeywords[i]), Is.EqualTo(i == feature && parent));
                Assert.That(G4SpecDebugFixture.Properties(material), Is.EquivalentTo(before), "Debug sync may not change intent, flags or other properties.");
                G4SpecDebugFixture.Validate(material); Assert.That(material.IsKeywordEnabled(G4SpecDebugFixture.DebugKeywords[feature]), Is.EqualTo(parent));
                Assert.That(material.GetFloat(G4SpecDebugFixture.DebugProps[feature]), Is.EqualTo(1), "Parent filtering preserves saved child intent.");
            }
            finally { Object.DestroyImmediate(material); }
        }
        [TestCase(TestName = "G4Debug_CPU_multiIntentPreserved")]
        public void ExistingMultipleToggleIntentIsNotInventedIntoAnEnum()
        {
            var material = G4SpecDebugFixture.NewGraph();
            try
            {
                for (int i = 0; i < 6; ++i) { material.SetFloat(G4SpecDebugFixture.DebugProps[i], 1); material.SetFloat(G4SpecDebugFixture.Parents[i], 1); }
                var before = G4SpecDebugFixture.Properties(material); G4SpecDebugFixture.Sync(material);
                Assert.That(G4SpecDebugFixture.DebugKeywords.All(material.IsKeywordEnabled), Is.True); Assert.That(G4SpecDebugFixture.Properties(material), Is.EquivalentTo(before));
                // Native has one seven-state axis; multi-enabled nearest-variant routing is not claimed by this CPU intent check.
            }
            finally { Object.DestroyImmediate(material); }
        }
        [TestCase(TestName = "G4Debug_CPU_unknownMarkerRefusesNoop")]
        public void FutureMarkerIsRefused()
        {
            var material = G4SpecDebugFixture.NewGraph();
            try
            {
                material.SetFloat(G4SpecDebugFixture.Version, 3); foreach (string keyword in G4SpecDebugFixture.DebugKeywords) material.EnableKeyword(keyword);
                var props = G4SpecDebugFixture.Properties(material); var keywords = material.shaderKeywords.OrderBy(k => k).ToArray();
                object result; string[] missing; Assert.That(G4SpecDebugFixture.Read(material, out result, out missing), Is.False);
                G4SpecDebugFixture.Sync(material); Assert.That(G4SpecDebugFixture.Properties(material), Is.EquivalentTo(props)); Assert.That(material.shaderKeywords.OrderBy(k => k), Is.EqualTo(keywords));
            }
            finally { Object.DestroyImmediate(material); }
        }
        [TestCase(TestName = "G4Debug_CPU_missingSchemaRefusesNoop")]
        public void MissingSchemaIsRefused()
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            try
            {
                var props = G4SpecDebugFixture.Properties(material); var keywords = material.shaderKeywords.OrderBy(k => k).ToArray();
                object result; string[] missing; Assert.That(G4SpecDebugFixture.Read(material, out result, out missing), Is.False);
                G4SpecDebugFixture.Sync(material); Assert.That(G4SpecDebugFixture.Properties(material), Is.EquivalentTo(props)); Assert.That(material.shaderKeywords.OrderBy(k => k), Is.EqualTo(keywords));
            }
            finally { Object.DestroyImmediate(material); }
        }
        [TestCase(TestName = "G4Debug_CPU_supported35PassesEmpty")]
        public void RealNewConsumerCapability()
        {
            var material = G4SpecDebugFixture.NewGraph();
            try
            {
                for (int i = 0; i < 6; ++i) { material.SetFloat(G4SpecDebugFixture.DebugProps[i], 1); material.SetFloat(G4SpecDebugFixture.Parents[i], 1); }
                material.SetFloat("_BlinnPhongSpecularToggle", 1); object result; string[] unavailable;
                var before = G4SpecDebugFixture.Properties(material); Assert.That(G4SpecDebugFixture.Read(material, out result, out unavailable), Is.True);
                Assert.That(unavailable, Is.EquivalentTo(new[] { "_SHARED_UV", "_STENCIL_WITHOUT_PLAYER" }));
                var keywords = (string[])result.GetType().GetField("effectiveKeywords").GetValue(result);
                Assert.That(G4SpecDebugFixture.DebugKeywords.Concat(new[] { "_SPECULAR_COLOR" }).All(keywords.Contains), Is.True);
                Assert.That((Array)result.GetType().GetField("passes").GetValue(result), Is.Empty, "Reader does not pretend to project Pass/Tier contracts.");
                Assert.That(G4SpecDebugFixture.Properties(material), Is.EquivalentTo(before));
            }
            finally { Object.DestroyImmediate(material); }
        }
        [TestCase(TestName = "G4Debug_CPU_sixDeclaredLocalKeywords")]
        public void SixRealDeclaredKeywords()
        {
            var material = G4SpecDebugFixture.NewGraph();
            try
            {
                var names = material.shader.keywordSpace.keywords.Select(k => k.name).ToArray();
                Assert.That(G4SpecDebugFixture.DebugKeywords.All(names.Contains), Is.True);
                foreach (string name in G4SpecDebugFixture.DebugProps)
                { int index = material.shader.FindPropertyIndex(name); Assert.That(index, Is.GreaterThanOrEqualTo(0)); Assert.That(material.shader.GetPropertyType(index), Is.EqualTo(UnityEngine.Rendering.ShaderPropertyType.Float)); Assert.That(material.GetFloat(name), Is.Zero); }
            }
            finally { Object.DestroyImmediate(material); }
        }
        [TestCase(TestName = "G4Debug_CPU_repeatSyncNoop")]
        public void RepeatSyncIsNoop()
        {
            var material = G4SpecDebugFixture.NewGraph();
            try
            {
                G4SpecDebugFixture.Sync(material); var props = G4SpecDebugFixture.Properties(material); var keywords = material.shaderKeywords.OrderBy(k => k).ToArray();
                G4SpecDebugFixture.Sync(material); Assert.That(G4SpecDebugFixture.Properties(material), Is.EquivalentTo(props)); Assert.That(material.shaderKeywords.OrderBy(k => k), Is.EqualTo(keywords));
            }
            finally { Object.DestroyImmediate(material); }
        }

        static IEnumerable<TestCaseData> GPUCases()
        {
            for (int feature = 0; feature < 6; ++feature) foreach (bool ortho in new[] { true, false })
                yield return new TestCaseData(feature, ortho).SetName("G4Debug_GPU_" + feature + (ortho ? "_ortho" : "_perspective"));
        }
        static void Configure(G4SpecDebugFixture.Harness harness, int feature, bool on)
        {
            var map = harness.Constant(new Color(.625f, .625f, .625f, .625f));
            var noise = harness.Constant(new Color(.75f, .25f, .5f, 1));
            var offset = harness.Constant(new Color(.75f, .75f, .75f, .75f));
            var mask = harness.Constant(new Color(.5f, .5f, .5f, .5f));
            for (int m = 0; m < 3; ++m)
            {
                var material = harness.materials[m];
                for (int i = 0; i < 6; ++i)
                {
                    material.SetFloat(G4SpecDebugFixture.DebugProps[i], i == feature && on ? 1 : 0);
                    material.SetFloat(G4SpecDebugFixture.Parents[i], i == feature ? 1 : 0);
                    if (m != 2) { G4SpecDebugFixture.SetKeyword(material, G4SpecDebugFixture.DebugKeywords[i], i == feature && on); G4SpecDebugFixture.SetKeyword(material, G4SpecDebugFixture.ParentKeywords[i], i == feature); }
                }
                material.SetTexture("_MaskMap", mask); material.SetVector("_MaskMapVec", new Vector4(1, 0, 0, 0));
                material.SetTexture("_DissolveMap", map); material.SetVector("_Dissolve", new Vector4(.25f, 2, 0, .2f)); material.SetVector("_DissolveOffsetRotateDistort", Vector4.zero);
                material.SetTexture("_NoiseMap", noise); material.SetFloat("_NoiseIntensity", .5f); material.SetVector("_DistortionDirection", new Vector4(.5f, .75f, 0, 0));
                material.SetFloat("_ProgramNoise_Simple_Toggle", feature == 1 ? 1 : 0); material.SetFloat("_ProgramNoise_Voronoi_Toggle", 0);
                material.SetVector("_DissolveVoronoi_Vec", new Vector4(3, 3, 2, 2)); material.SetVector("_DissolveVoronoi_Vec2", Vector4.zero); material.SetVector("_DissolveVoronoi_Vec3", Vector4.zero); material.SetVector("_DissolveVoronoi_Vec4", new Vector4(.1f, .2f, 0, 0));
                if (m != 2) { G4SpecDebugFixture.SetKeyword(material, "_PROGRAM_NOISE_SIMPLE", feature == 1); material.DisableKeyword("_PROGRAM_NOISE_VORONOI"); }
                material.SetVector("_FresnelUnit", new Vector4(.1f, 2, .8f, 0)); material.SetColor("_FresnelColor", new Color(.1f, .2f, .3f, 1)); material.SetVector("_FresnelRotation", Vector4.zero);
                material.SetTexture("_VertexOffset_Map", offset); material.SetVector("_VertexOffset_Vec", new Vector4(0, 0, .25f, 0)); material.SetFloat("_VertexOffset_NormalDir_Toggle", 0); material.SetFloat("_VertexOffset_DirectionSpace", 0);
                if (m == 2) G4SpecDebugFixture.Validate(material); G4SpecDebugFixture.Harness.RestoreForward(material, m == 2);
                Assert.That(material.IsKeywordEnabled(G4SpecDebugFixture.DebugKeywords[feature]), Is.EqualTo(on));
            }
        }
        static G4SpecDebugFixture.Metrics Measure(string id, string scope, Color[] empty, Color[][][] frame, Color[][][] repeated, int responseA = 0, int responseB = 1)
        {
            return new G4SpecDebugFixture.Metrics { caseId = id, scope = scope,
                finite = G4SpecDebugFixture.Finite(empty) && frame.SelectMany(s => s).Concat(repeated.SelectMany(s => s)).All(G4SpecDebugFixture.Finite),
                ab = frame.Select(s => G4SpecDebugFixture.Delta(s[0], s[1])).ToArray(), bc = frame.Select(s => G4SpecDebugFixture.Delta(s[1], s[2])).ToArray(),
                repeat = Enumerable.Range(0, frame.Length).SelectMany(s => Enumerable.Range(0, 3).Select(m => G4SpecDebugFixture.Delta(frame[s][m], repeated[s][m]))).ToArray(),
                response = Enumerable.Range(0, 3).Select(m => G4SpecDebugFixture.Delta(frame[responseA][m], frame[responseB][m])).ToArray(),
                visible = frame.SelectMany(s => s).Select(p => G4SpecDebugFixture.Visible(p, empty)).ToArray() };
        }
        [TestCaseSource(nameof(GPUCases))]
        public void ActualOriginalMiddleStageAndIndependentOffControl(int feature, bool ortho)
        {
            string id = "debug-" + feature + (ortho ? "-ortho" : "-perspective");
            using (var harness = new G4SpecDebugFixture.Harness(id, ortho))
            {
                var empty = harness.Snap("empty"); var frame = new Color[3][][]; var repeated = new Color[3][][];
                for (int state = 0; state < 3; ++state)
                {
                    Configure(harness, feature, state != 1); frame[state] = new Color[3][]; repeated[state] = new Color[3][];
                    for (int m = 0; m < 3; ++m) { frame[state][m] = harness.Snap("ABC"[m] + "-state" + state, harness.materials[m]); repeated[state][m] = harness.Snap("ABC"[m] + "-state" + state + "-repeat", harness.materials[m]); }
                }
                var metrics = Measure(id, "One original Debug stage on/off/restored; true parent consumer and real Forward pipeline; full-frame strict0 finite/visible/repeat/strong response. Multi-keyword routing/complete combos/Tier/GUI/VFX/Player/perf deferred.", empty, frame, repeated);
                metrics.restore = Enumerable.Range(0, 3).Select(m => G4SpecDebugFixture.Delta(frame[0][m], frame[2][m])).ToArray();
                harness.SaveAndAssert(metrics); Assert.That(metrics.response.All(v => v > .001f), Is.True, "True intermediate output requires a strong independent on/off response in A/B/C.");
            }
        }
        [TestCase(true, TestName = "G4Debug_GPU_clipBypass_ortho")]
        [TestCase(false, TestName = "G4Debug_GPU_clipBypass_perspective")]
        public void OriginalDebugSkipsFinalClip(bool ortho)
        {
            string id = "debug-clip-bypass" + (ortho ? "-ortho" : "-perspective");
            using (var harness = new G4SpecDebugFixture.Harness(id, ortho))
            {
                var empty = harness.Snap("empty"); var frame = new Color[2][][]; var repeated = new Color[2][][];
                for (int state = 0; state < 2; ++state)
                {
                    Configure(harness, 0, state == 0); frame[state] = new Color[3][]; repeated[state] = new Color[3][];
                    for (int m = 0; m < 3; ++m)
                    {
                        var material = harness.materials[m]; material.SetFloat("_Cutoff", 2); material.EnableKeyword("_ALPHATEST_ON");
                        material.SetFloat("_AlphaAll", .125f); material.SetFloat("_AdditiveToPreMultiplyAlphaLerp", 0); material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                        if (m == 2) material.SetFloat("_AlphaClip", 1);
                        frame[state][m] = harness.Snap("ABC"[m] + "-state" + state, material); repeated[state][m] = harness.Snap("ABC"[m] + "-state" + state + "-repeat", material);
                    }
                }
                var metrics = Measure(id, "Original Debug bypasses AlphaAll/final premultiply-alpha scale/clip. Debug-off control is expected to be fully clipped; visible assertion applies to on state, and zero clipped control is separately proved.", empty, frame, repeated);
                metrics.visible = frame[0].Select(p => G4SpecDebugFixture.Visible(p, empty)).ToArray(); harness.SaveAndAssert(metrics);
                Assert.That(metrics.response.All(v => v > .1f), Is.True); Assert.That(frame[1].All(p => G4SpecDebugFixture.Delta(p, empty) == 0), Is.True, "Clip control must really remove the Mesh output.");
                Assert.That(frame[0].All(p => p.Count(c => c.a == 1 && c.r > .1f) > 128), Is.True, "Debug alpha must remain exactly one despite downstream AlphaAll and additive alpha controls.");
            }
        }
        [TestCase(true, TestName = "G4Debug_GPU_overrideDepth_ortho")]
        [TestCase(false, TestName = "G4Debug_GPU_overrideDepth_perspective")]
        public void DebugStillWritesOverrideZDepth(bool ortho)
        {
            string id = "debug-override-depth" + (ortho ? "-ortho" : "-perspective");
            using (var harness = new G4SpecDebugFixture.Harness(id, ortho))
            {
                harness.AddProbe(); var empty = harness.Snap("probe-alone"); Configure(harness, 0, true);
                var frame = new Color[2][][]; var repeated = new Color[2][][];
                for (int state = 0; state < 2; ++state)
                {
                    frame[state] = new Color[3][]; repeated[state] = new Color[3][];
                    for (int m = 0; m < 3; ++m)
                    {
                        var material = harness.materials[m]; material.SetFloat("_OverrideZ_Toggle", 1); material.SetFloat("_OverrideZValue", state == 0 ? 3 : 7); material.EnableKeyword("_OVERRIDE_Z"); material.SetFloat("_ZWrite", 1);
                        frame[state][m] = harness.Snap("ABC"[m] + "-state" + state, material); repeated[state][m] = harness.Snap("ABC"[m] + "-state" + state + "-repeat", material);
                    }
                }
                var metrics = Measure(id, "Original Debug early return still uses MakeParticleFragmentOutput SVDepth. Actual later green probe differentiates 3/7 eye depths; no source-presence proxy.", empty, frame, repeated);
                // Far state legitimately equals the probe-only control. Near state must expose the actor.
                metrics.visible = frame[0].Select(p => G4SpecDebugFixture.Visible(p, empty)).ToArray(); harness.SaveAndAssert(metrics);
                Assert.That(metrics.response.All(v => v > .1f), Is.True);
                Assert.That(frame[0].All(p => p.Count(c => c.r > .1f && c.g > .1f && c.b > .1f) > 128), Is.True, "Near Debug actor must actually be visible.");
                Assert.That(frame[1].All(p => G4SpecDebugFixture.Delta(p, empty) == 0), Is.True, "Far OverrideZ actor must be occluded by the later real probe.");
            }
        }
    }
}
