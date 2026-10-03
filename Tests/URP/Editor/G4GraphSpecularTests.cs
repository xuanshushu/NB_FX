using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace NBFX.Baseline.Tests
{
    public sealed class G4GraphSpecularTests
    {
        const string Toggle = "_BlinnPhongSpecularToggle", Keyword = "_SPECULAR_COLOR";
        [OneTimeSetUp] public void Preflight() => G4SpecDebugFixture.PreflightImport();
        static IEnumerable<TestCaseData> CPUCases()
        {
            for (int mode = 0; mode <= 4; ++mode) foreach (bool on in new[] { false, true })
                yield return new TestCaseData(mode, on).SetName("G4Specular_CPU_mode" + mode + "_" + (on ? "on" : "off"));
        }
        [TestCaseSource(nameof(CPUCases))]
        public void ExistingToggleAuthority(int mode, bool on)
        {
            var material = G4SpecDebugFixture.NewGraph();
            try
            {
                material.SetFloat("_FxLightMode", mode); material.SetFloat(Toggle, on ? 1 : 0);
                G4SpecDebugFixture.SetKeyword(material, Keyword, !on);
                var props = G4SpecDebugFixture.Properties(material);
                G4SpecDebugFixture.Sync(material);
                Assert.That(material.IsKeywordEnabled(Keyword), Is.EqualTo(on), "Original serialized keyword intent is independent of UI lighting-mode visibility.");
                Assert.That(G4SpecDebugFixture.Properties(material), Is.EquivalentTo(props));
                var before = material.shaderKeywords.OrderBy(k => k).ToArray(); G4SpecDebugFixture.Sync(material);
                Assert.That(material.shaderKeywords.OrderBy(k => k), Is.EqualTo(before), "Idempotent keyword sync");
                G4SpecDebugFixture.Validate(material);
                Assert.That(material.GetFloat(Toggle), Is.EqualTo(on ? 1 : 0)); Assert.That(material.IsKeywordEnabled(Keyword), Is.EqualTo(on), "Actual Graph GUI validation entry");
            }
            finally { Object.DestroyImmediate(material); }
        }
        [TestCase(TestName = "G4Specular_CPU_realFloatDefault0")]
        public void RealFloatDefault()
        {
            var material = G4SpecDebugFixture.NewGraph();
            try
            {
                int index = material.shader.FindPropertyIndex(Toggle); Assert.That(index, Is.GreaterThanOrEqualTo(0));
                Assert.That(material.shader.GetPropertyType(index), Is.EqualTo(ShaderPropertyType.Float)); Assert.That(material.GetFloat(Toggle), Is.Zero);
                Assert.That(material.GetFloat("_NB_Flags0Lo16"), Is.Zero, "No new packed bit is allocated.");
            }
            finally { Object.DestroyImmediate(material); }
        }
        [TestCase(TestName = "G4Specular_CPU_declaredLocalKeyword")]
        public void DeclaredLocalKeyword()
        {
            var material = G4SpecDebugFixture.NewGraph();
            try { Assert.That(material.shader.keywordSpace.keywords.Any(k => k.name == Keyword), Is.True, "Material.IsKeywordEnabled alone cannot prove shader declaration."); }
            finally { Object.DestroyImmediate(material); }
        }
        static IEnumerable<TestCaseData> GPUCases()
        {
            foreach (int mode in new[] { 0, 1, 2, 3 }) foreach (bool ortho in new[] { true, false })
                yield return new TestCaseData(mode, ortho).SetName("G4Specular_GPU_mode" + mode + (ortho ? "_ortho" : "_perspective"));
        }
        [TestCaseSource(nameof(GPUCases))]
        public void TrueSpecularTermAndIndependentColorControl(int mode, bool ortho)
        {
            string id = "specular-mode" + mode + (ortho ? "-ortho" : "-perspective");
            using (var harness = new G4SpecDebugFixture.Harness(id, ortho))
            {
                var empty = harness.Snap("empty"); var frame = new Color[4][][]; var repeated = new Color[4][][];
                for (int state = 0; state < 4; ++state)
                {
                    bool on = state != 0; bool dark = state == 2;
                    frame[state] = new Color[3][]; repeated[state] = new Color[3][];
                    for (int m = 0; m < 3; ++m)
                    {
                        var material = harness.materials[m]; material.SetFloat("_FxLightMode", mode);
                        material.SetFloat(Toggle, on ? 1 : 0); material.SetColor("_SpecularColor", dark ? Color.black : new Color(.8f, .7f, .6f, 1));
                        material.SetVector("_MaterialInfo", new Vector4(.6f, .5f, 0, 0));
                        if (m == 2) G4SpecDebugFixture.Validate(material);
                        else
                        {
                            foreach (string k in new[] { "_FX_LIGHT_MODE_UNLIT", "_FX_LIGHT_MODE_BLINN_PHONG", "_FX_LIGHT_MODE_HALF_LAMBERT", "_FX_LIGHT_MODE_PBR" }) material.DisableKeyword(k);
                            material.EnableKeyword(new[] { "_FX_LIGHT_MODE_UNLIT", "_FX_LIGHT_MODE_BLINN_PHONG", "_FX_LIGHT_MODE_HALF_LAMBERT", "_FX_LIGHT_MODE_PBR" }[mode]);
                            G4SpecDebugFixture.SetKeyword(material, Keyword, on);
                        }
                        Assert.That(material.IsKeywordEnabled(Keyword), Is.EqualTo(on)); G4SpecDebugFixture.Harness.RestoreForward(material, m == 2);
                        frame[state][m] = harness.Snap("ABC"[m] + "-state" + state, material); repeated[state][m] = harness.Snap("ABC"[m] + "-state" + state + "-repeat", material);
                    }
                }
                var metrics = new G4SpecDebugFixture.Metrics {
                    caseId = id, scope = "Original Specular axis; off/on/zeroSpecularColor/restored; Blinn/Half strong response and Unlit/PBR invariant; main Forward only. No complete Tier/GUI/VFX/Player/perf claim.",
                    finite = G4SpecDebugFixture.Finite(empty) && frame.SelectMany(s => s).Concat(repeated.SelectMany(s => s)).All(G4SpecDebugFixture.Finite),
                    ab = frame.Select(s => G4SpecDebugFixture.Delta(s[0], s[1])).ToArray(), bc = frame.Select(s => G4SpecDebugFixture.Delta(s[1], s[2])).ToArray(),
                    repeat = Enumerable.Range(0, 4).SelectMany(s => Enumerable.Range(0, 3).Select(m => G4SpecDebugFixture.Delta(frame[s][m], repeated[s][m]))).ToArray(),
                    response = Enumerable.Range(0, 3).SelectMany(m => new[] { G4SpecDebugFixture.Delta(frame[0][m], frame[1][m]), G4SpecDebugFixture.Delta(frame[1][m], frame[2][m]) }).ToArray(),
                    restore = Enumerable.Range(0, 3).Select(m => G4SpecDebugFixture.Delta(frame[1][m], frame[3][m])).ToArray(),
                    visible = frame.SelectMany(s => s).Select(p => G4SpecDebugFixture.Visible(p, empty)).ToArray() };
                harness.SaveAndAssert(metrics);
                if (mode == 1 || mode == 2)
                {
                    Assert.That(metrics.response.All(v => v > .001f), Is.True, "Independent toggle and SpecularColor controls require a real response in A/B/C.");
                    Assert.That(Enumerable.Range(0, 3).All(m => G4SpecDebugFixture.Delta(frame[0][m], frame[2][m]) == 0), Is.True, "Zero specular color removes the term, equivalent to original off state.");
                }
                else Assert.That(metrics.response.All(v => v == 0), Is.True, "Original Unlit/PBR behavior must not change.");
            }
        }
    }
}
