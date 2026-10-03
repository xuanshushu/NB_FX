using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace NBFX.Baseline.Tests
{
    public sealed class G4GraphDebugCustomLocalTests
    {
        [OneTimeSetUp] public void Preflight() => G4SpecDebugFixture.PreflightImport();
        static IEnumerable<TestCaseData> Cases()
        {
            foreach (bool negative in new[] { false, true }) foreach (bool ortho in new[] { true, false })
                yield return new TestCaseData(negative, ortho).SetName("G4DebugCL_GPU_" + (negative ? "negative" : "positive") + (ortho ? "_ortho" : "_perspective"));
        }
        [TestCaseSource(nameof(Cases))]
        public void OriginalWorldDirectionUnderCustomLocal(bool negative, bool ortho)
        {
            string id = "debug-cl-" + (negative ? "negative" : "positive") + (ortho ? "-ortho" : "-perspective");
            using (var harness = new G4SpecDebugFixture.Harness(id, ortho))
            {
                var configure = typeof(G4GraphDebugTests).GetMethod("Configure", BindingFlags.Static | BindingFlags.NonPublic); Assert.That(configure, Is.Not.Null);
                var matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(13, 17, 11), new Vector3(negative ? -1.25f : 1.25f, .75f, 1.5f));
                var filter = harness.renderer.GetComponent<MeshFilter>(); var mesh = UnityEngine.Object.Instantiate(filter.sharedMesh);
                var original = filter.sharedMesh; filter.sharedMesh = mesh; mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 20);
                try
                {
                    var empty = harness.Snap("empty"); var frame = new Color[3][][]; var repeated = new Color[3][][];
                    for (int state = 0; state < 3; ++state)
                    {
                        configure.Invoke(null, new object[] { harness, 5, state != 1 }); frame[state] = new Color[3][]; repeated[state] = new Color[3][];
                        for (int m = 0; m < 3; ++m)
                        {
                            var material = harness.materials[m];
                            if (m == 2)
                            {
                                Assert.That(material.HasProperty("_NB_CustomLocalTransform"), Is.True); material.SetFloat("_NB_CustomLocalTransform", 1);
                                for (int row = 0; row < 4; ++row) { material.SetVector("_NB_CustomLocalToWorld" + row, matrix.GetRow(row)); material.SetVector("_NB_CustomWorldToLocal" + row, matrix.inverse.GetRow(row)); }
                            }
                            else { material.SetMatrix("_CustomLocalTransformLocalToWorld", matrix); material.SetMatrix("_CustomLocalTransformWorldToLocal", matrix.inverse); material.EnableKeyword("_CUSTOM_LOCAL_TRANSFORM"); }
                            frame[state][m] = harness.Snap("ABC"[m] + "-state" + state, material); repeated[state][m] = harness.Snap("ABC"[m] + "-state" + state + "-repeat", material);
                        }
                    }
                    var metrics = new G4SpecDebugFixture.Metrics {
                        caseId = id, scope = "Real world simulation attributes and CustomLocal matrix normal/direction, VertexOffset debug on/off/restored, reflected/nonuniform matrix, two cameras. Same shared direction function; strict0/finite/visible/repeat/strong response. Preview unrun; not whole CustomLocal or Debug Gate.",
                        finite = G4SpecDebugFixture.Finite(empty) && frame.SelectMany(s => s).Concat(repeated.SelectMany(s => s)).All(G4SpecDebugFixture.Finite),
                        ab = frame.Select(s => G4SpecDebugFixture.Delta(s[0], s[1])).ToArray(), bc = frame.Select(s => G4SpecDebugFixture.Delta(s[1], s[2])).ToArray(),
                        repeat = Enumerable.Range(0, 3).SelectMany(s => Enumerable.Range(0, 3).Select(m => G4SpecDebugFixture.Delta(frame[s][m], repeated[s][m]))).ToArray(),
                        response = Enumerable.Range(0, 3).Select(m => G4SpecDebugFixture.Delta(frame[0][m], frame[1][m])).ToArray(),
                        restore = Enumerable.Range(0, 3).Select(m => G4SpecDebugFixture.Delta(frame[0][m], frame[2][m])).ToArray(),
                        visible = frame.SelectMany(s => s).Select(p => G4SpecDebugFixture.Visible(p, empty)).ToArray() };
                    harness.SaveAndAssert(metrics); Assert.That(metrics.response.All(v => v > .001f), Is.True, "Debug must respond on the real custom-space geometry.");
                }
                finally { filter.sharedMesh = original; UnityEngine.Object.DestroyImmediate(mesh); }
            }
        }
    }
}
