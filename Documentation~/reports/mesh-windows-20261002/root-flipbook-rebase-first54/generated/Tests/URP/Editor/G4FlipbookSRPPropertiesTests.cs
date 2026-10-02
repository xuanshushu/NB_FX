using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace NBFX.Baseline.Tests
{
    public sealed class G4FlipbookSRPPropertiesTests
    {
        const string Package = "Packages/com.xuanxuan.nb.fx/";
        const string NextST = "_BaseMap_AnimationSheetBlend_ST";
        const string Weight = "_AnimationSheetHelperBlendIntensity";
        [Serializable] sealed class Row { public string role, reason; public int code; public bool known; }
        [Serializable] sealed class NativeReport { public bool apiKnown, warmPerformed; public Row[] shaders; }

        [Test]
        public void G4FlipbookSRP_source_properties()
        {
            string shaderPath = Package + "NBShaders2/Shader/NBShader.shader";
            var info = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(shaderPath);
            Assert.That(info, Is.Not.Null);
            string source = File.ReadAllText(Path.Combine(info.resolvedPath, "NBShaders2/Shader/NBShader.shader"));
            string input = File.ReadAllText(Path.Combine(info.resolvedPath, "NBShaders2/Shader/HLSL/NBShaderInput.hlsl"));
            string helper = File.ReadAllText(Path.Combine(info.resolvedPath, "XuanXuanRenderUtility/Runtime/AnimationSheetHelper.cs"));
            string buffer = input.Substring(input.IndexOf("CBUFFER_START(UnityPerMaterial)", StringComparison.Ordinal));
            buffer = buffer.Substring(0, buffer.IndexOf("CBUFFER_END", StringComparison.Ordinal));
            Assert.That(Regex.IsMatch(buffer, @"\bfloat4\s+" + NextST + @"\s*;"), Is.True);
            Assert.That(Regex.IsMatch(buffer, @"\bhalf\s+" + Weight + @"\s*;"), Is.True);
            Assert.That(Regex.IsMatch(source, @"\[HideInInspector\]\s*" + NextST + @"\s*\("), Is.True);
            Assert.That(Regex.IsMatch(source, @"\[HideInInspector\]\s*" + Weight + @"\s*\("), Is.True);
            foreach (string name in new[] { NextST, Weight })
                Assert.That(helper, Does.Contain("Shader.PropertyToID(\"" + name + "\")"));
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
            Assert.That(shader && shader.isSupported, Is.True);
            var material = new Material(shader);
            try
            {
                Assert.That(material.HasProperty(NextST) && material.HasProperty(Weight), Is.True);
                Assert.That(material.GetVector(NextST), Is.EqualTo(Vector4.zero));
                Assert.That(material.GetFloat(Weight), Is.Zero);
            }
            finally { UnityEngine.Object.DestroyImmediate(material); }
        }

        [Test]
        public void G4FlipbookSRP_native_current_graph_zero()
        {
            // Reuse the already verified real Mesh warm and exact native API
            // signature checks. Keep its original strict failures and raw JSON.
            new G4GraphOverrideDepthSourceTests().G4OverrideZSource_srp_batcher_codes();
            string root = Environment.GetEnvironmentVariable("NBFX_MESH_EVIDENCE_DIR");
            if (string.IsNullOrEmpty(root)) root = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Temp/NBFXOverrideZSource");
            var report = JsonUtility.FromJson<NativeReport>(File.ReadAllText(Path.Combine(root, "overridez-source/srp-batcher-subshader0.json")));
            Assert.That(report.apiKnown && report.warmPerformed, Is.True);
            Assert.That(report.shaders.Length, Is.EqualTo(3));
            foreach (var row in report.shaders)
            {
                Assert.That(row.known, Is.True, row.role);
                if (row.role == "current" || row.role == "graph")
                    Assert.That(row.code, Is.Zero, row.role + ": " + row.reason);
            }
            Assert.That(Array.Exists(report.shaders, r => r.role == "current") && Array.Exists(report.shaders, r => r.role == "graph"), Is.True);
        }
    }
}
